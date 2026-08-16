using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scene = UnityEngine.SceneManagement.Scene;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Dựng một scene greybox chạy được ngay: sân pickleball bằng primitive, quả bóng vật lý,
    /// một người chơi + một AI, camera và toàn bộ manager đã được gán tham chiếu.
    ///
    /// <para><b>Hệ toạ độ</b> (bám theo <see cref="Court"/>): mặt sân <c>y = 0</c>, lưới tại <c>z = 0</c>,
    /// sân trải theo trục Z, <c>x ∈ [-3.048, 3.048]</c>, <c>z ∈ [-6.7056, 6.7056]</c>,
    /// kitchen <c>|z| &lt; 2.1336</c>.</para>
    ///
    /// <para><b>Idempotent</b>: chạy lại sẽ ghi đè scene, prefab và material đã tạo lần trước.</para>
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        // ------------------------------------------------------------------ Đường dẫn asset

        private const string ScenesFolder = "Assets/Project/Scenes";
        private const string ScenePath = ScenesFolder + "/GreyboxMatch.unity";

        private const string PrefabsFolder = "Assets/Project/Prefabs";
        private const string GameplayPrefabsFolder = PrefabsFolder + "/Gameplay";
        private const string SimulationBallPrefabPath = GameplayPrefabsFolder + "/SimulationBall.prefab";

        private const string MaterialsFolder = "Assets/Project/Materials";
        private const string BallPhysicsMaterialPath = MaterialsFolder + "/GreyboxBallPhysics.asset";

        private const string ScriptableObjectsFolder = "Assets/Project/ScriptableObjects";
        private const string GameDataFolder = ScriptableObjectsFolder + "/Game";
        private const string ProfilesFolder = ScriptableObjectsFolder + "/Profiles";

        private const string GameSettingsPath = GameDataFolder + "/GameSettings.asset";
        private const string GameMessagesPath = GameDataFolder + "/GameMessages.asset";
        private const string ProfileLimitsPath = ProfilesFolder + "/PlayerProfileLimits.asset";
        private const string PlayerProfilePath = ProfilesFolder + "/Player_Default.asset";

        // ------------------------------------------------------------------ Hình học sân

        private const float CourtWidth = 6.096f;
        private const float CourtLength = 13.4112f;
        private const float HalfWidth = CourtWidth * 0.5f;      // 3.048
        private const float HalfLength = CourtLength * 0.5f;    // 6.7056
        private const float KitchenDepth = 2.1336f;
        private const float NetHeight = 0.8636f;

        private const float LineThickness = 0.05f;
        private const float LineHeight = 0.02f;
        private const float LineY = 0.011f;

        private const float BallRadius = 0.06f;
        private const float PlayerHeight = 1.8f;

        /// <summary>Vị trí spawn/giao bóng của người chơi (nửa sân âm) và AI (nửa sân dương).</summary>
        private static readonly Vector3 Player1Position = new Vector3(0f, 0f, -6.0f);
        private static readonly Vector3 Player2Position = new Vector3(0f, 0f, 6.0f);

        // ------------------------------------------------------------------ Entry point

        /// <summary>Dựng lại toàn bộ scene greybox và lưu vào <see cref="ScenePath"/>.</summary>
        [MenuItem("Pickleball/Build Greybox Match Scene")]
        public static void Build()
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(GameplayPrefabsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(GameDataFolder);

            // --- Asset dùng chung -----------------------------------------------------------
            Material groundMaterial = CreateMaterial("Greybox_CourtGreen", new Color(0.09f, 0.32f, 0.16f));
            Material outOfBoundsMaterial = CreateMaterial("Greybox_OutOfBounds", new Color(0.22f, 0.22f, 0.24f));
            Material lineMaterial = CreateMaterial("Greybox_CourtLine", new Color(0.94f, 0.94f, 0.94f));
            Material netMaterial = CreateMaterial("Greybox_Net", new Color(0.78f, 0.80f, 0.82f));
            Material postMaterial = CreateMaterial("Greybox_NetPost", new Color(0.35f, 0.35f, 0.38f));
            Material ballMaterial = CreateMaterial("Greybox_Ball", new Color(0.95f, 0.88f, 0.15f));
            Material playerMaterial = CreateMaterial("Greybox_Player", new Color(0.16f, 0.38f, 0.88f));
            Material aiMaterial = CreateMaterial("Greybox_AI", new Color(0.85f, 0.20f, 0.18f));
            Material paddleMaterial = CreateMaterial("Greybox_Paddle", new Color(0.18f, 0.18f, 0.20f));
            Material indicatorMaterial = CreateMaterial("Greybox_ServeIndicator", new Color(0.15f, 0.85f, 0.90f));
            Material trailMaterial = CreateTrailMaterial("Greybox_BallTrail", new Color(1f, 0.95f, 0.4f));

            PhysicsMaterial ballPhysics = CreateBallPhysicsMaterial();

            GameSettings gameSettings = AssetDatabase.LoadAssetAtPath<GameSettings>(GameSettingsPath);
            PlayerProfileLimits profileLimits = AssetDatabase.LoadAssetAtPath<PlayerProfileLimits>(ProfileLimitsPath);
            PlayerProfile playerProfile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(PlayerProfilePath);
            PlayerProfile aiProfile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilesFolder + "/AI_2_Amateur.asset");
            GameMessages gameMessages = CreateOrUpdateGameMessages();

            WarnIfNull(gameSettings, GameSettingsPath);
            WarnIfNull(profileLimits, ProfileLimitsPath);
            WarnIfNull(playerProfile, PlayerProfilePath);
            WarnIfNull(aiProfile, ProfilesFolder + "/AI_2_Amateur.asset");

            // --- Scene mới ------------------------------------------------------------------
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationBall simulationBallPrefab = BuildSimulationBallPrefab(ballPhysics);

            // ================================================================ ENVIRONMENT
            GameObject environment = new GameObject("--- ENVIRONMENT ---");

            CreateDirectionalLight(environment.transform);

            GameObject ground = CreateCube("Ground", environment.transform,
                new Vector3(0f, -0.1f, 0f), new Vector3(CourtWidth, 0.2f, CourtLength), groundMaterial, true);
            ground.tag = StringConstants.GroundTag;
            ground.layer = SafeLayer(StringConstants.GroundLayer);

            GameObject outOfBounds = CreateCube("OutOfBoundsFloor", environment.transform,
                new Vector3(0f, -0.35f, 0f), new Vector3(30f, 0.2f, 40f), outOfBoundsMaterial, true);
            outOfBounds.tag = "Untagged";

            BuildCourtLines(environment.transform, lineMaterial);

            GameObject net = CreateCube("Net", environment.transform,
                new Vector3(0f, NetHeight * 0.5f, 0f), new Vector3(CourtWidth, NetHeight, 0.05f), netMaterial, true);
            net.tag = StringConstants.NetTag;
            net.AddComponent<PickleNet>(); // rippleMaterial = null -> phần shader tự no-op.

            CreateCube("NetPost_Left", environment.transform,
                new Vector3(-HalfWidth, 0.5f, 0f), new Vector3(0.06f, 1f, 0.06f), postMaterial, false);
            CreateCube("NetPost_Right", environment.transform,
                new Vector3(HalfWidth, 0.5f, 0f), new Vector3(0.06f, 1f, 0.06f), postMaterial, false);

            // ================================================================ MANAGERS
            GameObject managers = new GameObject("--- MANAGERS ---");

            GameObject courtObject = new GameObject("Court");
            courtObject.transform.SetParent(managers.transform, false);
            courtObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            courtObject.transform.localScale = Vector3.one;
            Court court = courtObject.AddComponent<Court>();
            court.gameSettings = gameSettings;
            court.RecalculateDimentions();

            GameObject locations = new GameObject("Locations");
            locations.transform.SetParent(managers.transform, false);
            Transform player1Location = CreateMarker("Player1Location", locations.transform, Player1Position);
            Transform player2Location = CreateMarker("Player2Location", locations.transform, Player2Position);
            Transform positiveLookAt = CreateMarker("PositiveCourtLookAt", locations.transform,
                new Vector3(0f, 0f, HalfLength * 0.5f));
            Transform negativeLookAt = CreateMarker("NegativeCourtLookAt", locations.transform,
                new Vector3(0f, 0f, -HalfLength * 0.5f));

            // ================================================================ GAMEPLAY (dựng trước để tham chiếu)
            GameObject gameplay = new GameObject("--- GAMEPLAY ---");

            GameObject cameraObject = BuildCamera();
            cameraObject.transform.SetParent(gameplay.transform, false);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 8f, -13f), Quaternion.Euler(28f, 0f, 0f));
            Camera mainCamera = cameraObject.GetComponent<Camera>();

            GameObject ball = BuildBall(gameplay.transform, ballMaterial, trailMaterial, ballPhysics,
                simulationBallPrefab);

            GameObject player = BuildPlayer(gameplay.transform, "Player", Player1Position, false, playerMaterial,
                paddleMaterial, indicatorMaterial, "P1", playerProfile, profileLimits, null);

            GameObject aiPlayer = BuildPlayer(gameplay.transform, "AIPlayer", Player2Position, true, aiMaterial,
                paddleMaterial, indicatorMaterial, "P2", aiProfile, profileLimits, aiProfile);

            CameraFollow cameraFollow = cameraObject.GetComponent<CameraFollow>();
            cameraFollow.target = player.transform;
            cameraFollow.offset = new Vector3(0f, 8f, -7f);
            cameraFollow.smoothSpeed = 4f;
            cameraFollow.rotationSmoothSpeed = 6f;
            cameraFollow.lookAtTarget = true;
            cameraFollow.useClamp = true;
            cameraFollow.clampX = new Vector2(-4f, 4f);
            cameraFollow.clampZ = new Vector2(-16f, 16f);

            // ================================================================ Managers cần tham chiếu scene
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManagerObject.transform.SetParent(managers.transform, false);
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
            gameManager.settings = gameSettings;
            gameManager.gameMessages = gameMessages;
            gameManager.court = court;
            gameManager.profileLimits = profileLimits;
            gameManager.mainCamera = mainCamera;
            gameManager.player1Location = player1Location;
            gameManager.player2Location = player2Location;
            gameManager.positiveCourtLookAtLocation = positiveLookAt;
            gameManager.negativeCourtLookAtLocation = negativeLookAt;
            gameManager.player1TeamID = "P1";
            gameManager.player2TeamID = "P2";

            GameObject scoreManagerObject = new GameObject("ScoreManager");
            scoreManagerObject.transform.SetParent(managers.transform, false);
            ScoreManager scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            scoreManager.player1TeamID = "P1";
            scoreManager.player2TeamID = "P2";

            // settings / gameMessages là private [SerializeField] -> ghi qua SerializedObject.
            // Gán CẢ HAI trong cùng một SerializedObject rồi Apply một lần: tạo hai SerializedObject
            // liên tiếp cho cùng một target khiến lần Apply sau ghi đè lần Apply trước.
            SerializedObject scoreSerialized = new SerializedObject(scoreManager);
            SetSerializedReference(scoreSerialized, "settings", gameSettings);
            SetSerializedReference(scoreSerialized, "gameMessages", gameMessages);
            scoreSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Chốt lại qua property công khai: ở -batchmode, ApplyModifiedProperties của ScoreManager
            // không phải lúc nào cũng ghi kịp vào field managed trước khi scene được lưu.
            scoreManager.Settings = gameSettings;
            scoreManager.Messages = gameMessages;
            EditorUtility.SetDirty(scoreManager);

            GameObject inputManagerObject = new GameObject("InputManager");
            inputManagerObject.transform.SetParent(managers.transform, false);
            InputManager inputManager = inputManagerObject.AddComponent<InputManager>();
            inputManager.groundLayer = 1 << SafeLayer(StringConstants.GroundLayer);

            GameObject trajectoryObject = new GameObject("PhysicsTrajectoryHandler");
            trajectoryObject.transform.SetParent(managers.transform, false);
            PhysicsTrajectoryHandler trajectoryHandler = trajectoryObject.AddComponent<PhysicsTrajectoryHandler>();
            SetSerializedTransformList(trajectoryHandler, "_CollidersInScene",
                new[] { ground.transform, net.transform });

            GameObject boosterManagerObject = new GameObject("BoosterManager");
            boosterManagerObject.transform.SetParent(managers.transform, false);
            boosterManagerObject.AddComponent<BoosterManager>();

            // --- Lưu ------------------------------------------------------------------------
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterSceneInBuildSettings();

            Debug.Log(saved
                ? "[GreyboxSceneBuilder] Đã dựng và lưu scene tại " + ScenePath
                : "[GreyboxSceneBuilder] KHÔNG lưu được scene tại " + ScenePath);
        }

        // ------------------------------------------------------------------ Environment

        private static void CreateDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.color = Color.white;
        }

        /// <summary>
        /// Vẽ vạch sân bằng cube mỏng: 2 vạch kitchen, 2 vạch giữa (kitchen → baseline mỗi nửa),
        /// 2 vạch biên dọc và 2 vạch baseline. Toàn bộ đều KHÔNG có collider để không cản bóng.
        /// </summary>
        private static void BuildCourtLines(Transform parent, Material lineMaterial)
        {
            GameObject root = new GameObject("CourtLines");
            root.transform.SetParent(parent, false);

            // Vạch kitchen (non-volley line) hai bên lưới.
            CreateCube("KitchenLine_Positive", root.transform, new Vector3(0f, LineY, KitchenDepth),
                new Vector3(CourtWidth, LineHeight, LineThickness), lineMaterial, false);
            CreateCube("KitchenLine_Negative", root.transform, new Vector3(0f, LineY, -KitchenDepth),
                new Vector3(CourtWidth, LineHeight, LineThickness), lineMaterial, false);

            // Vạch giữa: chỉ chạy từ vạch kitchen tới baseline của từng nửa sân.
            float centerLineLength = HalfLength - KitchenDepth;
            float centerLineZ = KitchenDepth + centerLineLength * 0.5f;
            CreateCube("CenterLine_Positive", root.transform, new Vector3(0f, LineY, centerLineZ),
                new Vector3(LineThickness, LineHeight, centerLineLength), lineMaterial, false);
            CreateCube("CenterLine_Negative", root.transform, new Vector3(0f, LineY, -centerLineZ),
                new Vector3(LineThickness, LineHeight, centerLineLength), lineMaterial, false);

            // 4 vạch biên.
            CreateCube("Sideline_Left", root.transform, new Vector3(-HalfWidth, LineY, 0f),
                new Vector3(LineThickness, LineHeight, CourtLength), lineMaterial, false);
            CreateCube("Sideline_Right", root.transform, new Vector3(HalfWidth, LineY, 0f),
                new Vector3(LineThickness, LineHeight, CourtLength), lineMaterial, false);
            CreateCube("Baseline_Positive", root.transform, new Vector3(0f, LineY, HalfLength),
                new Vector3(CourtWidth, LineHeight, LineThickness), lineMaterial, false);
            CreateCube("Baseline_Negative", root.transform, new Vector3(0f, LineY, -HalfLength),
                new Vector3(CourtWidth, LineHeight, LineThickness), lineMaterial, false);
        }

        // ------------------------------------------------------------------ Camera

        private static GameObject BuildCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.14f, 0.18f, 0.24f);

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CameraFollow>();

            return cameraObject;
        }

        // ------------------------------------------------------------------ Ball

        private static GameObject BuildBall(Transform parent, Material ballMaterial, Material trailMaterial,
            PhysicsMaterial physicsMaterial, SimulationBall simulationBallPrefab)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.SetParent(parent, false);
            ball.transform.position = new Vector3(0f, 1f, -6f);
            ball.transform.localScale = Vector3.one * (BallRadius * 2f);
            ball.tag = StringConstants.BallTag;
            ball.layer = SafeLayer(StringConstants.BallLayer);

            Renderer ballRenderer = ball.GetComponent<Renderer>();
            ballRenderer.sharedMaterial = ballMaterial;

            SphereCollider sphereCollider = ball.GetComponent<SphereCollider>();
            sphereCollider.sharedMaterial = physicsMaterial;

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            ApplyBallRigidbodySettings(rb);

            TrailRenderer trail = ball.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.minVertexDistance = 0.02f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0f);
            trail.sharedMaterial = trailMaterial;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;

            AudioSource audioSource = ball.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            ball.AddComponent<EnhancedBounce>();

            BallController ballController = ball.AddComponent<BallController>();
            ballController.rb = rb;
            ballController.trailRenderer = trail;
            ballController.ballRenderer = ballRenderer;
            ballController.groundLayermask = 1 << SafeLayer(StringConstants.GroundLayer);
            ballController.initialParent = parent;
            ballController.simulationBallPrefab = simulationBallPrefab;

            return ball;
        }

        private static void ApplyBallRigidbodySettings(Rigidbody rb)
        {
            rb.mass = 0.026f;                                   // bóng pickleball thật ~26 g
            rb.linearDamping = 0.05f;                           // Unity 6: thay cho 'drag'
            rb.angularDamping = 0.05f;                          // Unity 6: thay cho 'angularDrag'
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // ------------------------------------------------------------------ Simulation ball prefab

        /// <summary>
        /// Tạo/ghi đè prefab bóng mô phỏng. Thông số vật lý PHẢI trùng quả bóng thật, kể cả
        /// <see cref="EnhancedBounce.bounceFactor"/>, nếu không quỹ đạo dự đoán sẽ lệch sau lần nảy đầu.
        /// </summary>
        private static SimulationBall BuildSimulationBallPrefab(PhysicsMaterial physicsMaterial)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "SimulationBall";
            temp.transform.localScale = Vector3.one * (BallRadius * 2f);

            MeshRenderer meshRenderer = temp.GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;

            SphereCollider sphereCollider = temp.GetComponent<SphereCollider>();
            sphereCollider.sharedMaterial = physicsMaterial;

            Rigidbody rb = temp.AddComponent<Rigidbody>();
            ApplyBallRigidbodySettings(rb);

            temp.AddComponent<EnhancedBounce>();

            SimulationBall simulationBall = temp.AddComponent<SimulationBall>();
            simulationBall.rb = rb;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, SimulationBallPrefabPath);
            Object.DestroyImmediate(temp);

            return prefab != null ? prefab.GetComponent<SimulationBall>() : null;
        }

        // ------------------------------------------------------------------ Players

        /// <summary>
        /// Dựng một tay vợt greybox.
        /// <para><b>Lưu ý quan trọng</b>: <c>BasePlayerController.ApplyMovement</c> ép
        /// <c>position.y = 0</c> mỗi frame, nên GỐC của tay vợt phải nằm ở <c>y = 0</c>;
        /// capsule cao 1.8 m là object con đặt tại local <c>y = 0.9</c>.</para>
        /// </summary>
        private static GameObject BuildPlayer(Transform parent, string name, Vector3 position, bool isAI,
            Material bodyMaterial, Material paddleMaterial, Material indicatorMaterial, string teamID,
            PlayerProfile profile, PlayerProfileLimits limits, PlayerProfile aiProfileForConfigs)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, isAI ? 180f : 0f, 0f));
            root.layer = SafeLayer(StringConstants.PlayerLayer);

            // --- Avatar (capsule 1.8 m, KHÔNG collider) ---
            GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "Avatar";
            avatar.transform.SetParent(root.transform, false);
            avatar.transform.localPosition = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            avatar.transform.localScale = new Vector3(0.5f, PlayerHeight * 0.5f, 0.5f);
            avatar.layer = root.layer;
            avatar.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            Object.DestroyImmediate(avatar.GetComponent<Collider>());

            // --- Điểm cầm bóng ---
            GameObject ballHoldPoint = new GameObject("BallHoldPoint");
            ballHoldPoint.transform.SetParent(root.transform, false);
            ballHoldPoint.transform.localPosition = new Vector3(0.35f, 1.2f, 0.15f);

            // --- Chỉ dẫn ô giao bóng (quad nằm ngang dưới chân) ---
            GameObject serveIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            serveIndicator.name = "ServeIndicator";
            serveIndicator.transform.SetParent(root.transform, false);
            serveIndicator.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            serveIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            serveIndicator.transform.localScale = Vector3.one * 0.6f;
            serveIndicator.GetComponent<Renderer>().sharedMaterial = indicatorMaterial;
            Object.DestroyImmediate(serveIndicator.GetComponent<Collider>());

            // --- Vợt ---
            GameObject paddleHolder = new GameObject("PaddleHolder");
            paddleHolder.transform.SetParent(root.transform, false);
            paddleHolder.transform.localPosition = new Vector3(0.45f, 1.15f, 0.2f);

            GameObject paddle = CreateCube("Paddle", paddleHolder.transform, Vector3.zero,
                new Vector3(0.02f, 0.28f, 0.2f), paddleMaterial, false);
            paddle.transform.localPosition = Vector3.zero;
            Renderer paddleRenderer = paddle.GetComponent<Renderer>();

            // --- Component ---
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            PlayerAvatar playerAvatar = root.AddComponent<PlayerAvatar>();
            SerializedObject avatarSerialized = new SerializedObject(playerAvatar);
            SetSerializedReference(avatarSerialized, "paddleHolderRight", paddleHolder.transform);
            SetSerializedReference(avatarSerialized, "ballHoldPointRight", ballHoldPoint.transform);
            avatarSerialized.ApplyModifiedPropertiesWithoutUndo();

            BasePlayerAnimationController animationController = root.AddComponent<BasePlayerAnimationController>();

            RacquetAnimator racquetAnimator = root.AddComponent<RacquetAnimator>();
            racquetAnimator.racquetTransform = paddleHolder.transform;
            racquetAnimator.alignSpeed = 540f;

            BasePlayerController controller;
            if (isAI)
            {
                PickleballAIController ai = root.AddComponent<PickleballAIController>();
                ai.difficulty = AIDifficulty.Amateur;
                AssignAIProfileConfigs(ai);
                controller = ai;
            }
            else
            {
                controller = root.AddComponent<PickleballPlayerController>();
            }

            controller.teamID = teamID;
            controller.playerProfile = profile;
            controller.profileLimits = limits;
            controller.avatar = avatar.transform;
            controller.playerAvatar = playerAvatar;
            controller.ballHoldPoint = ballHoldPoint.transform;
            controller.serveIndicator = serveIndicator.transform;
            controller.paddleHolder = paddleHolder.transform;
            controller.paddleRenderer = paddleRenderer;
            controller.animationController = animationController;
            controller.audioSource = audioSource;
            controller.SetCourtSide(position.z >= 0f);

            return root;
        }

        /// <summary>
        /// Nạp bảng <see cref="PlayerProfileConfigs"/> (class [Serializable] nội tuyến, KHÔNG phải asset)
        /// bằng SerializedObject: mỗi bậc độ khó trỏ tới đúng asset profile đã sinh sẵn.
        /// </summary>
        private static void AssignAIProfileConfigs(PickleballAIController ai)
        {
            (AIDifficulty difficulty, string assetName)[] mapping =
            {
                (AIDifficulty.Newbie, "AI_1_Newbie"),
                (AIDifficulty.Amateur, "AI_2_Amateur"),
                (AIDifficulty.Competitor, "AI_3_Competitor"),
                (AIDifficulty.Pro, "AI_5_Pro"),
                (AIDifficulty.Master, "AI_7_Master"),
                (AIDifficulty.Tutorial, "AI_0_Tutorial")
            };

            SerializedObject serialized = new SerializedObject(ai);
            SerializedProperty list = serialized.FindProperty("playerProfileConfigs.aiProfileDataList");
            if (list == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không tìm thấy playerProfileConfigs.aiProfileDataList.");
                return;
            }

            list.ClearArray();
            list.arraySize = mapping.Length;

            for (int i = 0; i < mapping.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("difficulty").enumValueIndex = (int)mapping[i].difficulty;

                SerializedProperty profiles = element.FindPropertyRelative("profiles");
                profiles.ClearArray();
                profiles.arraySize = 1;

                PlayerProfile asset =
                    AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilesFolder + "/" + mapping[i].assetName + ".asset");
                profiles.GetArrayElementAtIndex(0).objectReferenceValue = asset;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ GameMessages

        /// <summary>Tạo (hoặc cập nhật) asset thông báo với nội dung tiếng Anh cho đủ 8 lỗi luật.</summary>
        private static GameMessages CreateOrUpdateGameMessages()
        {
            GameMessages messages = AssetDatabase.LoadAssetAtPath<GameMessages>(GameMessagesPath);
            if (messages == null)
            {
                messages = ScriptableObject.CreateInstance<GameMessages>();
                AssetDatabase.CreateAsset(messages, GameMessagesPath);
            }

            messages.ruleMessages = new List<RuleMessage>
            {
                MakeRuleMessage(RuleType.ServeInNet, "rule.serve_in_net", "Serve hit the net!"),
                MakeRuleMessage(RuleType.ServeNotInArea, "rule.serve_not_in_area", "Serve missed the service box!"),
                MakeRuleMessage(RuleType.ServeVolley, "rule.serve_volley", "You must let the serve bounce!"),
                MakeRuleMessage(RuleType.DoubleBounceOnSide, "rule.double_bounce", "Double bounce!"),
                MakeRuleMessage(RuleType.VolleyInsideKitchen, "rule.volley_in_kitchen", "Volley inside the kitchen!"),
                MakeRuleMessage(RuleType.BounceOutOfCourt, "rule.out_of_court", "Ball out of bounds!"),
                MakeRuleMessage(RuleType.WrongPlayerTurn, "rule.wrong_turn", "Not your shot!"),
                MakeRuleMessage(RuleType.ServeTimeout, "rule.serve_timeout", "Serve clock expired!")
            };

            messages.deuceKey = "Deuce!";
            messages.matchPointKey = "Match Point!";
            messages.advantageKey = "Advantage!";
            messages.gameOverKey = "Game Over!";

            EditorUtility.SetDirty(messages);
            return messages;
        }

        private static RuleMessage MakeRuleMessage(RuleType type, string key, string text)
        {
            return new RuleMessage { ruleType = type, localizationKey = key, fallbackText = text };
        }

        // ------------------------------------------------------------------ Build settings

        /// <summary>Đưa scene greybox lên index 0 của Build Settings, giữ nguyên các scene khác.</summary>
        private static void RegisterSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] == null) continue;
                if (existing[i].path == ScenePath) continue;
                scenes.Add(existing[i]);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ Helper: primitive

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material, bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            if (!keepCollider)
            {
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }

            return cube;
        }

        private static Transform CreateMarker(string name, Transform parent, Vector3 localPosition)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            return marker.transform;
        }

        // ------------------------------------------------------------------ Helper: asset

        private static Material CreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(GetLitShader());
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = GetLitShader();
            }

            ApplyColor(material, color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTrailMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = GetLitShader();

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            ApplyColor(material, color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
        }

        private static Shader GetLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        private static PhysicsMaterial CreateBallPhysicsMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BallPhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial("GreyboxBallPhysics");
                AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            }

            material.bounciness = 0.5f;
            material.dynamicFriction = 0.4f;
            material.staticFriction = 0.4f;
            material.frictionCombine = PhysicsMaterialCombine.Multiply;
            material.bounceCombine = PhysicsMaterialCombine.Multiply;

            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ Helper: SerializedObject

        private static void SetSerializedObjectReference(Object target, string propertyPath, Object value)
        {
            if (target == null) return;

            SerializedObject serialized = new SerializedObject(target);
            if (!SetSerializedReference(serialized, propertyPath, value)) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Ghi một tham chiếu vào SerializedObject đang mở (chưa Apply).</summary>
        private static bool SetSerializedReference(SerializedObject serialized, string propertyPath, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"[GreyboxSceneBuilder] Không tìm thấy field '{propertyPath}' trên "
                                 + serialized.targetObject.GetType().Name + ".");
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static void SetSerializedTransformList(Object target, string propertyPath, Transform[] values)
        {
            if (target == null) return;

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"[GreyboxSceneBuilder] Không tìm thấy list '{propertyPath}' trên {target.GetType().Name}.");
                return;
            }

            property.ClearArray();
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Helper: misc

        /// <summary>
        /// Chỉ số layer theo tên.
        /// <para>Ở chế độ <c>-batchmode</c>, <see cref="LayerMask.NameToLayer"/> trả về <c>-1</c> cho các layer
        /// do người dùng định nghĩa (bảng layer chưa được nạp), nên có phương án dự phòng đọc thẳng
        /// <c>ProjectSettings/TagManager.asset</c>.</para>
        /// </summary>
        private static int SafeLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) return layer;

            layer = FindLayerInTagManager(layerName);
            if (layer >= 0) return layer;

            layer = FindLayerInTagManagerFile(layerName);
            if (layer >= 0) return layer;

            Debug.LogWarning($"[GreyboxSceneBuilder] Layer '{layerName}' chưa tồn tại — dùng Default.");
            return 0;
        }

        /// <summary>Phương án cuối: đọc trực tiếp file YAML <c>ProjectSettings/TagManager.asset</c>.</summary>
        private static int FindLayerInTagManagerFile(string layerName)
        {
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string path = System.IO.Path.Combine(projectRoot, "ProjectSettings", "TagManager.asset");
            if (!System.IO.File.Exists(path)) return -1;

            string[] lines = System.IO.File.ReadAllLines(path);
            bool insideLayers = false;
            int index = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (!insideLayers)
                {
                    if (line.TrimEnd() == "  layers:") insideLayers = true;
                    continue;
                }

                string trimmed = line.Trim();
                if (!trimmed.StartsWith("-")) break; // hết mảng layers

                string value = trimmed.Substring(1).Trim();
                if (value == layerName) return index;
                index++;
            }

            return -1;
        }

        private static int FindLayerInTagManager(string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return -1;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray) return -1;

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;
            }

            return -1;
        }

        private static void WarnIfNull(Object asset, string path)
        {
            if (asset == null) Debug.LogWarning("[GreyboxSceneBuilder] Không nạp được asset: " + path);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            int lastSlash = folderPath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            string parent = folderPath.Substring(0, lastSlash);
            string leaf = folderPath.Substring(lastSlash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
