using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scene = UnityEngine.SceneManagement.Scene;
using Object = UnityEngine.Object;

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

        // --- Art gốc (tuỳ chọn) --------------------------------------------------------------
        private const string CharacterPrefabsFolder = PrefabsFolder + "/Characters";
        private const string PlayerMalePrefabPath = CharacterPrefabsFolder + "/Player_Male.prefab";
        private const string AIOpponentPrefabPath = CharacterPrefabsFolder + "/AI_Opponent.prefab";

        private const string OriginalArtPrefabsFolder = "Assets/Project/ArtFromOriginal/Prefabs";
        private const string OriginalPaddlePrefabPath = OriginalArtPrefabsFolder + "/Paddle 1.prefab";
        private const string OriginalBallPrefabPath = OriginalArtPrefabsFolder + "/Ball.prefab";
        private const string OriginalNetPrefabPath = OriginalArtPrefabsFolder + "/Net.prefab";

        private const string MaterialsFolder = "Assets/Project/Materials";
        private const string BallPhysicsMaterialPath = MaterialsFolder + "/GreyboxBallPhysics.asset";

        private const string ScriptableObjectsFolder = "Assets/Project/ScriptableObjects";
        private const string GameDataFolder = ScriptableObjectsFolder + "/Game";
        private const string ProfilesFolder = ScriptableObjectsFolder + "/Profiles";

        private const string GameSettingsPath = GameDataFolder + "/GameSettings.asset";
        private const string GameMessagesPath = GameDataFolder + "/GameMessages.asset";
        private const string GameDataPath = GameDataFolder + "/GameData.asset";
        private const string ProfileLimitsPath = ProfilesFolder + "/PlayerProfileLimits.asset";
        private const string PlayerProfilePath = ProfilesFolder + "/Player_Default.asset";

        // --- Tầng UI ------------------------------------------------------------------------
        private const string UIScreenPrefabsFolder = PrefabsFolder + "/UI/Screens";
        private const string TutorialUIPrefabPath = UIScreenPrefabsFolder + "/TutorialUI.prefab";
        private const string TutorialConfigurationPath = ScriptableObjectsFolder + "/Tutorial/TutorialConfiguration.asset";
        private const string TutorialKitbagExpectedPath = ScriptableObjectsFolder + "/Rewards/DynamicKitbag_Tutorial.asset";

        /// <summary>Màn hình <see cref="UIController"/> mở đầu tiên sau khi mọi màn hình đăng ký xong.</summary>
        private const ScreenType StartScreenType = ScreenType.MainMenu;

        /// <summary>Sorting order của canvas màn hình thường.</summary>
        private const int ScreenSortingOrder = 0;

        /// <summary>Sorting order của canvas popup — luôn chồng lên màn hình thường.</summary>
        private const int PopupSortingOrder = 100;

        /// <summary>Sorting order của lớp phủ hướng dẫn — trên cả popup.</summary>
        private const int TutorialSortingOrder = 200;

        /// <summary>
        /// Sorting order của các lớp phủ dịch vụ (toast, hiệu ứng UI). Chúng KHÔNG phải màn hình,
        /// không tham gia ngăn xếp và phải luôn nhìn thấy được nên nằm trên cùng.
        /// </summary>
        private const int OverlaySortingOrder = 300;

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

        // ------------------------------------------------------------------ Cờ chọn art

        /// <summary>
        /// Khi bật, builder sẽ dùng prefab nhân vật art gốc trong
        /// <c>Assets/Project/Prefabs/Characters</c> thay cho capsule primitive — với ĐÚNG vị trí,
        /// góc quay và <c>teamID</c> như bản greybox.
        /// <para>Không tìm thấy prefab thì tự động rơi về capsule, nên luồng greybox luôn chạy được.</para>
        /// <para>Hai menu item tự đặt cờ này trước khi dựng, nên giá trị mặc định chỉ ảnh hưởng
        /// khi có script khác gọi thẳng <see cref="BuildInternal"/>.</para>
        /// </summary>
        public static bool UseOriginalArt = true;

        // ------------------------------------------------------------------ Entry point

        /// <summary>Dựng scene greybox thuần primitive (không đụng tới art gốc).</summary>
        [MenuItem("Pickleball/Build Greybox Match Scene")]
        public static void Build()
        {
            UseOriginalArt = false;
            BuildInternal();
        }

        /// <summary>
        /// Dựng cùng một scene nhưng thay tay vợt capsule bằng prefab nhân vật art gốc
        /// (<c>Player_Male</c>, <c>AI_Opponent</c>) nếu đã sinh ra bằng
        /// <c>Pickleball/Art/Build Playable Character Prefabs</c>.
        /// </summary>
        [MenuItem("Pickleball/Build Match Scene (Original Art)")]
        public static void BuildWithOriginalArt()
        {
            UseOriginalArt = true;
            BuildInternal();
        }

        /// <summary>Dựng lại toàn bộ scene và lưu vào <see cref="ScenePath"/>.</summary>
        private static void BuildInternal()
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

            TryReplaceVisualWithOriginalArt(ball, OriginalBallPrefabPath, "bóng");
            TryReplaceVisualWithOriginalArt(net, OriginalNetPrefabPath, "lưới");

            GameObject player = TryBuildPlayerFromOriginalArt(gameplay.transform, "Player", Player1Position, false,
                                    PlayerMalePrefabPath, "P1", playerProfile, profileLimits)
                                ?? BuildPlayer(gameplay.transform, "Player", Player1Position, false, playerMaterial,
                                    paddleMaterial, indicatorMaterial, "P1", playerProfile, profileLimits, null);

            GameObject aiPlayer = TryBuildPlayerFromOriginalArt(gameplay.transform, "AIPlayer", Player2Position, true,
                                      AIOpponentPrefabPath, "P2", aiProfile, profileLimits)
                                  ?? BuildPlayer(gameplay.transform, "AIPlayer", Player2Position, true, aiMaterial,
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

            // --- Meta layer: save, bootstrap, thưởng sau trận ---------------------------------
            // Ba component này sống xuyên scene (IndestructibleSingleton) nên đặt ở object riêng.
            // Thứ tự khởi động do [DefaultExecutionOrder] quyết định:
            //   SavedDataHandler (-600) -> GameBootstrap (-500) -> phần còn lại.
            GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
            Shop shop = AssetDatabase.LoadAssetAtPath<Shop>(ScriptableObjectsFolder + "/Shop/Shop.asset");

            GameObject metaRoot = new GameObject("--- META ---");

            GameObject saveObject = new GameObject("SavedDataHandler");
            saveObject.transform.SetParent(metaRoot.transform, false);
            SavedDataHandler savedDataHandler = saveObject.AddComponent<SavedDataHandler>();
            savedDataHandler._gameData = gameData;
            savedDataHandler._shop = shop;

            GameObject bootstrapObject = new GameObject("GameBootstrap");
            bootstrapObject.transform.SetParent(metaRoot.transform, false);
            GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();
            bootstrap.gameData = gameData;
            bootstrap.shop = shop;
            bootstrap.gameSettings = gameSettings;
            bootstrap.savedDataHandler = savedDataHandler;

            GameObject rewardObject = new GameObject("MatchRewardHandler");
            rewardObject.transform.SetParent(metaRoot.transform, false);
            MatchRewardHandler rewardHandler = rewardObject.AddComponent<MatchRewardHandler>();
            rewardHandler.gameData = gameData;
            rewardHandler.settings = gameSettings;

            if (gameData == null || shop == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Chưa có GameData.asset hoặc Shop.asset — " +
                                 "chạy Pickleball/Generate Item Data, Generate Reward Data, Generate Shop Data rồi dựng lại scene.");
            }

            // ================================================================ UI
            BuildUILayer(gameData);

            // --- Lưu ------------------------------------------------------------------------
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterSceneInBuildSettings();

            Debug.Log(saved
                ? "[GreyboxSceneBuilder] Đã dựng và lưu scene tại " + ScenePath +
                  (UseOriginalArt ? " (chế độ ART GỐC)" : " (chế độ greybox)")
                : "[GreyboxSceneBuilder] KHÔNG lưu được scene tại " + ScenePath);
        }

        // ------------------------------------------------------------------ UI

        /// <summary>
        /// Dựng toàn bộ tầng UI dưới node <c>--- UI ---</c>: EventSystem, <see cref="UIController"/>,
        /// mọi prefab màn hình trong <see cref="UIScreenPrefabsFolder"/> và
        /// <see cref="TutorialManager"/>.
        ///
        /// <para><b>Vì sao mọi thứ đều là con của <c>--- UI ---</c></b>: <c>IndestructibleSingleton</c>
        /// chỉ gọi <c>DontDestroyOnLoad</c> khi <c>transform.parent == null</c>. Đặt
        /// <see cref="UIController"/> / <see cref="TutorialManager"/> làm con nên chúng sống theo
        /// scene — đúng như nhóm <c>--- META ---</c> đang làm — và mỗi lần nạp lại scene sẽ có một
        /// bộ UI sạch (quan trọng cho PlayMode test chạy nhiều lần).</para>
        /// </summary>
        /// <param name="gameData">Hub dữ liệu đã nạp ở nhóm META; có thể null.</param>
        private static void BuildUILayer(GameData gameData)
        {
            GameObject uiRoot = new GameObject("--- UI ---");

            CreateEventSystem(uiRoot.transform);
            CreateUIController(uiRoot.transform);

            TutorialUI tutorialUI;
            InstantiateScreenPrefabs(uiRoot.transform, out tutorialUI);

            CreateTutorialManager(uiRoot.transform, gameData, tutorialUI);
        }

        /// <summary>
        /// Dựng <c>EventSystem</c> + <c>StandaloneInputModule</c>.
        /// <para>Project đặt <c>activeInputHandler: 0</c> (Input Manager cũ) nên module đúng là
        /// <c>StandaloneInputModule</c>, KHÔNG phải <c>InputSystemUIInputModule</c>.</para>
        /// <para><b>Thiếu EventSystem thì mọi nút bấm đều chết</b> dù UI vẫn hiện bình thường.</para>
        /// </summary>
        private static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(parent, false);

            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        /// <summary>Dựng <see cref="UIController"/> và gán màn hình mở đầu qua SerializedObject.</summary>
        private static void CreateUIController(Transform parent)
        {
            GameObject controllerObject = new GameObject("UIController");
            controllerObject.transform.SetParent(parent, false);

            UIController controller = controllerObject.AddComponent<UIController>();

            // startScreen là [SerializeField] private -> ghi qua SerializedObject.
            // Dùng intValue chứ KHÔNG dùng enumValueIndex: enumValueIndex là thứ tự trong bảng tên,
            // còn ta cần đúng giá trị số của ScreenType.
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty startScreen = serialized.FindProperty("startScreen");
            if (startScreen != null)
            {
                startScreen.intValue = (int)StartScreenType;
            }
            else
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không tìm thấy field 'startScreen' trên UIController.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Đưa mọi prefab trong <see cref="UIScreenPrefabsFolder"/> vào scene bằng
        /// <c>PrefabUtility.InstantiatePrefab</c> để GIỮ LIÊN KẾT PREFAB —
        /// sửa prefab về sau là scene tự cập nhật.
        ///
        /// <para>Mỗi canvas được xếp tầng theo <see cref="UIScreenBase.IsPopup"/> và tắt sẵn, trừ
        /// màn hình mở đầu. Xem chú thích trong thân hàm về việc vì sao vẫn phải tắt tay.</para>
        /// </summary>
        /// <param name="parent">Node <c>--- UI ---</c>.</param>
        /// <param name="tutorialUI">Instance TutorialUI tìm được, hoặc null.</param>
        private static void InstantiateScreenPrefabs(Transform parent, out TutorialUI tutorialUI)
        {
            tutorialUI = null;

            if (!AssetDatabase.IsValidFolder(UIScreenPrefabsFolder))
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không thấy thư mục " + UIScreenPrefabsFolder +
                                 " — scene sẽ KHÔNG có màn hình UI nào.");
                return;
            }

            EnsureTutorialUIPrefab();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UIScreenPrefabsFolder });
            List<string> paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }

            // Sắp xếp để thứ tự node trong scene luôn giống nhau giữa các lần dựng.
            paths.Sort(StringComparer.Ordinal);

            int instantiated = 0;
            int screens = 0;
            bool startScreenFound = false;

            for (int i = 0; i < paths.Count; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefab == null) continue;

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (instance == null)
                {
                    Debug.LogWarning("[GreyboxSceneBuilder] Không instantiate được " + paths[i] + ".");
                    continue;
                }

                instantiated++;

                UIScreenBase screen = instance.GetComponent<UIScreenBase>();
                if (screen != null) screens++;

                if (tutorialUI == null)
                {
                    TutorialUI found = instance.GetComponent<TutorialUI>();
                    if (found != null) tutorialUI = found;
                }

                bool isOverlayHandler = IsServiceOverlay(instance, screen);
                bool isStartScreen = !startScreenFound && screen != null && !screen.IsPopup
                                     && ResolveScreenType(screen) == StartScreenType;
                if (isStartScreen) startScreenFound = true;

                Canvas canvas = instance.GetComponent<Canvas>();
                if (canvas == null) canvas = instance.GetComponentInChildren<Canvas>(true);

                if (canvas == null)
                {
                    Debug.LogWarning("[GreyboxSceneBuilder] " + paths[i] +
                                     " không có Canvas — màn hình này sẽ không vẽ được gì.");
                    continue;
                }

                canvas.sortingOrder = ResolveSortingOrder(instance, screen, isOverlayHandler);

                // Tắt sẵn mọi canvas trừ màn mở đầu và các lớp phủ dịch vụ.
                //
                // UIScreenBase.Awake đã tự gọi ApplyVisibility(false) nên với các prefab CÓ
                // UIScreenBase thì bước này là thừa lúc chạy — nhưng vẫn giữ vì:
                //   1) 5 prefab trong thư mục KHÔNG có UIScreenBase (LANCreationUI, LANLobbyUI,
                //      LANSelectionUI, UIToast, UI VFX). Nhóm LAN* sẽ phủ kín màn hình mãi mãi
                //      nếu không tắt tay.
                //   2) Scene lưu ra nhìn đúng ngay trong Editor, không phải bấm Play mới sạch.
                canvas.enabled = isStartScreen || isOverlayHandler;

                // Ghi lại override lên prefab instance để scene lưu đúng hai thay đổi ở trên.
                PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
            }

            if (!startScreenFound)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không tìm thấy prefab nào mang ScreenType." +
                                 StartScreenType + " trong " + UIScreenPrefabsFolder +
                                 " — bấm Play sẽ không thấy màn hình nào.");
            }

            Debug.Log("[GreyboxSceneBuilder] Tầng UI: " + instantiated + " prefab đã đặt vào scene, " +
                      screens + " trong số đó là UIScreenBase.");
        }

        /// <summary>
        /// Dựng prefab TutorialUI nếu chưa có, để tầng UI luôn đủ bộ chỉ với một lần bấm menu.
        /// </summary>
        private static void EnsureTutorialUIPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TutorialUIPrefabPath) != null) return;

            Debug.Log("[GreyboxSceneBuilder] Chưa có " + TutorialUIPrefabPath +
                      " — dựng tự động bằng TutorialUIBuilder.");
            TutorialUIBuilder.BuildTutorialUIPrefab();
        }

        /// <summary>
        /// True với các prefab lớp phủ dịch vụ (toast, hiệu ứng UI): chúng không phải màn hình,
        /// không nằm trong ngăn xếp và phải luôn bật canvas thì mới hiện được nội dung sinh ra
        /// lúc chạy.
        /// </summary>
        private static bool IsServiceOverlay(GameObject instance, UIScreenBase screen)
        {
            if (screen != null) return false;

            return instance.GetComponent<ToastHandler>() != null
                   || instance.GetComponent<UIVFXHandler>() != null;
        }

        /// <summary>
        /// Chọn sorting order cho canvas: lớp phủ dịch vụ 300, hướng dẫn 200, popup 100,
        /// màn hình thường 0.
        /// <para>Prefab không có <see cref="UIScreenBase"/> thì đoán theo tên có chứa "Popup".</para>
        /// </summary>
        private static int ResolveSortingOrder(GameObject instance, UIScreenBase screen, bool isOverlayHandler)
        {
            if (isOverlayHandler) return OverlaySortingOrder;
            if (screen is TutorialUI) return TutorialSortingOrder;
            if (screen != null) return screen.IsPopup ? PopupSortingOrder : ScreenSortingOrder;

            return instance.name.IndexOf("Popup", StringComparison.OrdinalIgnoreCase) >= 0
                ? PopupSortingOrder
                : ScreenSortingOrder;
        }

        /// <summary>
        /// <see cref="ScreenType"/> thực tế của một màn hình: ưu tiên giá trị gán trên prefab,
        /// rơi về <see cref="UIScreenBase.DefaultScreenType"/> — đúng cách
        /// <c>UIScreenBase.Awake</c> làm lúc chạy.
        /// </summary>
        private static ScreenType ResolveScreenType(UIScreenBase screen)
        {
            if (screen == null) return ScreenType.None;
            return screen.screenType != ScreenType.None ? screen.screenType : screen.DefaultScreenType;
        }

        /// <summary>
        /// Dựng <see cref="TutorialManager"/> và nối 4 tham chiếu bắt buộc. Asset nào thiếu thì chỉ
        /// cảnh báo kèm đường dẫn mong đợi — KHÔNG ném exception, để scene vẫn dựng xong.
        /// </summary>
        /// <param name="parent">Node <c>--- UI ---</c>.</param>
        /// <param name="gameData">Hub dữ liệu; có thể null.</param>
        /// <param name="tutorialUI">Instance TutorialUI trong scene; có thể null.</param>
        private static void CreateTutorialManager(Transform parent, GameData gameData, TutorialUI tutorialUI)
        {
            GameObject managerObject = new GameObject("TutorialManager");
            managerObject.transform.SetParent(parent, false);

            TutorialManager manager = managerObject.AddComponent<TutorialManager>();

            TutorialConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<TutorialConfiguration>(TutorialConfigurationPath);
            if (configuration == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không nạp được TutorialConfiguration — mong đợi tại " +
                                 TutorialConfigurationPath + " (chạy Pickleball/Generate Tutorial Data).");
            }
            manager.configuration = configuration;

            if (gameData == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] TutorialManager.gameData để trống — mong đợi tại " +
                                 GameDataPath + ".");
            }
            manager.gameData = gameData;

            DynamicKitbag kitbag = FindTutorialKitbag();
            if (kitbag == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không tìm thấy asset DynamicKitbag nào — mong đợi tại " +
                                 TutorialKitbagExpectedPath + "; bước thưởng tutorial sẽ tự bỏ qua.");
            }

            if (tutorialUI == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không có TutorialUI trong scene — mong đợi prefab tại " +
                                 TutorialUIPrefabPath + " (chạy Pickleball/UI/Build Tutorial UI Prefab).");
            }

            // tutorialKitbag / tutorialUI là [SerializeField] private -> ghi qua SerializedObject.
            SerializedObject serialized = new SerializedObject(manager);
            SetSerializedReference(serialized, "tutorialKitbag", kitbag);
            SetSerializedReference(serialized, "tutorialUI", tutorialUI);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Tìm túi thưởng hướng dẫn: ưu tiên asset có tên chứa "Tutorial", nếu không thì lấy
        /// asset <c>DynamicKitbag</c> đầu tiên tìm được.
        /// </summary>
        private static DynamicKitbag FindTutorialKitbag()
        {
            string[] guids = AssetDatabase.FindAssets("t:DynamicKitbag");
            if (guids == null || guids.Length == 0) return null;

            DynamicKitbag fallback = null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DynamicKitbag kitbag = AssetDatabase.LoadAssetAtPath<DynamicKitbag>(path);
                if (kitbag == null) continue;

                if (kitbag.name.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0) return kitbag;
                if (fallback == null) fallback = kitbag;
            }

            return fallback;
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

        // ------------------------------------------------------------------ Players (art gốc)

        /// <summary>
        /// Đặt một tay vợt vào scene bằng prefab nhân vật art gốc đã dựng sẵn ở
        /// <c>Assets/Project/Prefabs/Characters</c>.
        /// <para>
        /// Prefab đó ĐÃ mang sẵn <see cref="BasePlayerController"/>, <see cref="PlayerAvatar"/>,
        /// <see cref="RacquetAnimator"/>… (do <c>CharacterPrefabBuilder</c> gắn), nên ở đây chỉ cần
        /// đặt vị trí / góc quay / <c>teamID</c> / profile — giống hệt bản greybox đang làm.
        /// </para>
        /// </summary>
        /// <returns><c>null</c> khi tắt cờ <see cref="UseOriginalArt"/> hoặc chưa có prefab —
        /// người gọi sẽ rơi về <see cref="BuildPlayer"/>.</returns>
        private static GameObject TryBuildPlayerFromOriginalArt(Transform parent, string name, Vector3 position,
            bool isAI, string prefabPath, string teamID, PlayerProfile profile, PlayerProfileLimits limits)
        {
            if (!UseOriginalArt) return null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.Log("[GreyboxSceneBuilder] Chưa có " + prefabPath + " — dùng capsule greybox cho " + name + ".");
                return null;
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (root == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] Không instantiate được " + prefabPath + ".");
                return null;
            }

            root.name = name;
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, isAI ? 180f : 0f, 0f));
            SetLayerRecursively(root, SafeLayer(StringConstants.PlayerLayer));

            BasePlayerController controller = root.GetComponent<BasePlayerController>();
            if (controller == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] " + prefabPath +
                                 " không có BasePlayerController — huỷ, quay về capsule greybox.");
                Object.DestroyImmediate(root);
                return null;
            }

            controller.teamID = teamID;
            controller.playerProfile = profile;
            controller.profileLimits = limits;
            controller.SetCourtSide(position.z >= 0f);

            AttachOriginalPaddle(controller, name);

            Debug.Log("[GreyboxSceneBuilder] " + name + " dùng art gốc: " + prefabPath);
            return root;
        }

        /// <summary>
        /// Gắn prefab vợt gốc vào <c>paddleHolder</c> — CHỈ khi nút gắn chưa có sẵn mesh vợt.
        /// Prefab nhân vật gốc đã kèm mẫu vợt <c>Paddle_V2</c>, nên thường bước này bị bỏ qua và
        /// đó là điều mong muốn (tránh hai cây vợt chồng lên nhau).
        /// </summary>
        private static void AttachOriginalPaddle(BasePlayerController controller, string ownerName)
        {
            Transform holder = controller.paddleHolder;
            if (holder == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] " + ownerName + ": paddleHolder trống, không gắn được vợt.");
                return;
            }

            if (holder.GetComponentInChildren<Renderer>(true) != null)
            {
                if (controller.paddleRenderer == null)
                {
                    controller.paddleRenderer = holder.GetComponentInChildren<Renderer>(true);
                }

                return;
            }

            GameObject paddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPaddlePrefabPath);
            if (paddlePrefab == null)
            {
                Debug.LogWarning("[GreyboxSceneBuilder] " + ownerName + ": không có " + OriginalPaddlePrefabPath + ".");
                return;
            }

            GameObject paddle = (GameObject)PrefabUtility.InstantiatePrefab(paddlePrefab, holder);
            paddle.transform.localPosition = Vector3.zero;
            paddle.transform.localRotation = Quaternion.identity;

            controller.paddleRenderer = paddle.GetComponentInChildren<Renderer>(true);
            Debug.Log("[GreyboxSceneBuilder] " + ownerName + ": đã gắn " + OriginalPaddlePrefabPath + ".");
        }

        /// <summary>
        /// Thay phần NHÌN của một object primitive (bóng / lưới) bằng prefab art gốc nếu có:
        /// prefab được gắn làm con và renderer primitive bị tắt, còn collider + component gameplay
        /// giữ nguyên trên object cha.
        /// </summary>
        /// <param name="target">Object primitive đang mang logic (Ball, Net).</param>
        /// <param name="prefabPath">Đường dẫn prefab art gốc.</param>
        /// <param name="label">Nhãn tiếng Việt dùng trong log.</param>
        private static void TryReplaceVisualWithOriginalArt(GameObject target, string prefabPath, string label)
        {
            if (!UseOriginalArt || target == null) return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.Log("[GreyboxSceneBuilder] Không có prefab art gốc cho " + label + " (" + prefabPath +
                          ") — giữ primitive greybox.");
                return;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            MeshRenderer primitiveRenderer = target.GetComponent<MeshRenderer>();
            if (primitiveRenderer != null) primitiveRenderer.enabled = false;

            // BallController bật/tắt ball renderer để giấu bóng — phải trỏ sang renderer MỚI,
            // nếu không quả bóng sẽ vô hình vĩnh viễn.
            BallController ballController = target.GetComponent<BallController>();
            if (ballController != null)
            {
                Renderer replacement = visual.GetComponentInChildren<Renderer>(true);
                if (replacement != null) ballController.ballRenderer = replacement;
            }

            Debug.Log("[GreyboxSceneBuilder] " + label + " dùng art gốc: " + prefabPath);
        }

        /// <summary>Đặt layer cho một object và toàn bộ con cháu.</summary>
        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node != null) node.gameObject.layer = layer;
            }
        }

        // ------------------------------------------------------------------ Players (greybox)

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
