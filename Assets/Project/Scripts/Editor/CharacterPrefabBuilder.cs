using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Biến prefab nhân vật GỐC (đã chép vào <c>Assets/Project/ArtFromOriginal/Prefabs</c>) thành
    /// prefab CHƠI ĐƯỢC trong <c>Assets/Project/Prefabs/Characters</c>.
    ///
    /// <para><b>Vì sao cần bước này</b>: prefab gốc tham chiếu script của bản gốc
    /// (<c>Yudiz.*</c>, <c>StarterKit.*</c>) bằng GUID không tồn tại trong project ta, nên mọi
    /// component logic hiện ra dạng <i>Missing (Mono Script)</i>. Phần mesh / material / animator /
    /// avatar thì vẫn dùng tốt. Công cụ này gỡ sạch component hỏng rồi gắn đúng bộ component của ta
    /// và nối các điểm gắn (tay cầm vợt, điểm cầm bóng, vòng chỉ dẫn giao bóng).</para>
    ///
    /// <para><b>Khác biệt tên node giữa các nhân vật</b> — đây là lý do phải dò theo nhiều tên:</para>
    /// <list type="bullet">
    /// <item><description>PlayerV3 / AIv3: <c>PaddleHolderRight</c>, <c>PaddleHolderLeft</c>,
    /// <c>BallHoldPointRight</c>, <c>BallHoldPointLeft</c>.</description></item>
    /// <item><description>PlayerV4_Female: <c>RightPaddleHolder</c>, <c>LeftPaddleHolder</c>
    /// (mỗi bên chứa một node con tên <c>PaddleHolder</c>), <c>RightBallHolder</c>,
    /// <c>LeftBallHolder</c>.</description></item>
    /// </list>
    ///
    /// <para><b>Idempotent</b>: chạy lại sẽ ghi đè prefab đã sinh trước đó.</para>
    /// </summary>
    public static class CharacterPrefabBuilder
    {
        // ------------------------------------------------------------------ Đường dẫn

        private const string SourcePrefabFolder = "Assets/Project/ArtFromOriginal/Prefabs";
        private const string ControllerFolder = "Assets/Project/ArtFromOriginal/Animations/Controllers";
        private const string OutputFolder = "Assets/Project/Prefabs/Characters";
        private const string ProfilesFolder = "Assets/Project/ScriptableObjects/Profiles";

        private const string MaleControllerPath = ControllerFolder + "/PlayerAnimationControllerv3.controller";
        private const string FemaleControllerPath = ControllerFolder + "/FemaleAnimationControllerv3.controller";

        // ------------------------------------------------------------------ Bảng mô tả nhân vật

        /// <summary>Mô tả một prefab nhân vật cần dựng.</summary>
        private sealed class CharacterRecipe
        {
            /// <summary>Tên file prefab gốc (không kèm đuôi).</summary>
            public string SourceName;

            /// <summary>Tên prefab kết quả (không kèm đuôi).</summary>
            public string OutputName;

            /// <summary>True nếu dùng <see cref="PickleballAIController"/> thay cho controller người chơi.</summary>
            public bool IsAI;

            /// <summary>Mã đội gán vào <c>teamID</c>.</summary>
            public string TeamID;

            /// <summary>Tên asset <see cref="PlayerProfile"/> tương ứng.</summary>
            public string ProfileAssetName;

            /// <summary>Animator controller dự phòng khi prefab gốc bỏ trống.</summary>
            public string FallbackControllerPath;
        }

        private static readonly CharacterRecipe[] Recipes =
        {
            new CharacterRecipe
            {
                SourceName = "PlayerV3_Character", OutputName = "Player_Male", IsAI = false, TeamID = "P1",
                ProfileAssetName = "Player_Default", FallbackControllerPath = MaleControllerPath
            },
            new CharacterRecipe
            {
                SourceName = "PlayerV4_Female_Character", OutputName = "Player_Female", IsAI = false, TeamID = "P1",
                ProfileAssetName = "Player_Default", FallbackControllerPath = FemaleControllerPath
            },
            new CharacterRecipe
            {
                SourceName = "AIv3_Character", OutputName = "AI_Opponent", IsAI = true, TeamID = "P2",
                ProfileAssetName = "AI_2_Amateur", FallbackControllerPath = MaleControllerPath
            }
        };

        // ------------------------------------------------------------------ Tên node cần dò

        /// <summary>Ứng viên tên node điểm cầm bóng tay phải, xếp theo độ ưu tiên.</summary>
        private static readonly string[] BallHoldRightNames = { "BallHoldPointRight", "RightBallHolder" };

        /// <summary>Ứng viên tên node điểm cầm bóng tay trái.</summary>
        private static readonly string[] BallHoldLeftNames = { "BallHoldPointLeft", "LeftBallHolder" };

        /// <summary>Ứng viên tên node gắn vợt tay phải.</summary>
        private static readonly string[] PaddleHolderRightNames = { "PaddleHolderRight", "RightPaddleHolder" };

        /// <summary>Ứng viên tên node gắn vợt tay trái.</summary>
        private static readonly string[] PaddleHolderLeftNames = { "PaddleHolderLeft", "LeftPaddleHolder" };

        /// <summary>Xương tay phải — phương án cuối khi không có node gắn vợt chuyên dụng.</summary>
        private const string RightHandBoneName = "mixamorig:RightHand";

        // ------------------------------------------------------------------ Entry point

        /// <summary>Dựng ba prefab nhân vật chơi được từ art gốc.</summary>
        [MenuItem("Pickleball/Art/Build Playable Character Prefabs")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(SourcePrefabFolder))
            {
                Debug.LogError("[CharacterPrefabBuilder] Chưa có " + SourcePrefabFolder +
                               " — chạy 'Pickleball/Art/Import Original Character Art' trước.");
                return;
            }

            EnsureFolder(OutputFolder);

            int built = 0;
            foreach (CharacterRecipe recipe in Recipes)
            {
                if (BuildOne(recipe)) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterPrefabBuilder] Đã dựng {built}/{Recipes.Length} prefab trong {OutputFolder}.");
        }

        // ------------------------------------------------------------------ Dựng một nhân vật

        /// <summary>Dựng một prefab nhân vật theo công thức và ghi log chi tiết từng field.</summary>
        /// <param name="recipe">Công thức nhân vật.</param>
        /// <returns>True nếu lưu prefab thành công.</returns>
        private static bool BuildOne(CharacterRecipe recipe)
        {
            string sourcePath = SourcePrefabFolder + "/" + recipe.SourceName + ".prefab";
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogError("[CharacterPrefabBuilder] Không nạp được prefab gốc: " + sourcePath);
                return false;
            }

            var log = new StringBuilder();
            log.AppendLine($"[CharacterPrefabBuilder] {recipe.OutputName} ← {recipe.SourceName}");

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            if (instance == null)
            {
                Debug.LogError("[CharacterPrefabBuilder] Không instantiate được " + sourcePath);
                return false;
            }

            try
            {
                // Phải unpack HOÀN TOÀN: không thể gỡ component khỏi một prefab instance còn liên kết,
                // và ta muốn prefab kết quả độc lập chứ không phải variant của art gốc.
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                                                  InteractionMode.AutomatedAction);

                instance.name = recipe.OutputName;
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                int removed = RemoveMissingScripts(instance);
                log.AppendLine($"  gỡ Missing (Mono Script): {removed} component");

                BuildComponents(instance, recipe, log);

                string outputPath = OutputFolder + "/" + recipe.OutputName + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);

                log.AppendLine(success && saved != null
                                   ? "  đã lưu: " + outputPath
                                   : "  LỖI khi lưu: " + outputPath);

                Debug.Log(log.ToString());
                return success && saved != null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Gỡ toàn bộ component <i>Missing (Mono Script)</i> trên mọi node của cây.
        /// </summary>
        /// <param name="root">Gốc cây cần dọn.</param>
        /// <returns>Tổng số component đã gỡ.</returns>
        private static int RemoveMissingScripts(GameObject root)
        {
            int removed = 0;

            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node == null) continue;
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
            }

            return removed;
        }

        // ------------------------------------------------------------------ Gắn component của ta

        /// <summary>Gắn bộ component gameplay và nối mọi tham chiếu tìm được.</summary>
        private static void BuildComponents(GameObject root, CharacterRecipe recipe, StringBuilder log)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            Transform avatarNode = animator != null ? animator.transform : null;

            if (animator == null) log.AppendLine("  !! KHÔNG tìm thấy Animator trong cây.");

            // --- Animator controller dự phòng ---
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var fallback = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(recipe.FallbackControllerPath);
                if (fallback != null)
                {
                    animator.runtimeAnimatorController = fallback;
                    log.AppendLine("  runtimeAnimatorController (trống) ← " + recipe.FallbackControllerPath);
                }
                else
                {
                    log.AppendLine("  !! runtimeAnimatorController trống và KHÔNG có " + recipe.FallbackControllerPath);
                }
            }
            else if (animator != null)
            {
                log.AppendLine("  runtimeAnimatorController giữ nguyên của bản gốc: " +
                               animator.runtimeAnimatorController.name);
            }

            // --- Các node đặc trưng ---
            Transform ballHoldRight = FindByNames(root.transform, BallHoldRightNames);
            Transform ballHoldLeft = FindByNames(root.transform, BallHoldLeftNames);
            Transform paddleHolderRight = ResolvePaddleHolder(root.transform, PaddleHolderRightNames);
            Transform paddleHolderLeft = ResolvePaddleHolder(root.transform, PaddleHolderLeftNames);
            Transform serveIndicator = FindByNames(root.transform, new[] { "ServiceIndicator", "ServeIndicator" });
            Transform turnIndicator = FindByNames(root.transform, new[] { "PlayerTurnIndicator" });

            if (paddleHolderRight == null)
            {
                paddleHolderRight = FindByNames(root.transform, new[] { RightHandBoneName });
                if (paddleHolderRight != null) log.AppendLine("  paddleHolder rơi về xương " + RightHandBoneName);
            }

            // --- AudioSource: dùng lại cái sẵn có trong prefab gốc nếu tìm thấy ---
            AudioSource audioSource = root.GetComponentInChildren<AudioSource>(true);
            if (audioSource == null)
            {
                audioSource = root.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                log.AppendLine("  AudioSource: thêm mới trên node gốc");
            }
            else
            {
                audioSource.playOnAwake = false;
                log.AppendLine("  AudioSource: dùng lại node '" + audioSource.gameObject.name + "'");
            }

            // --- PlayerAvatar ---
            PlayerAvatar playerAvatar = GetOrAdd<PlayerAvatar>(root);
            SkinnedMeshRenderer bodyRenderer = PickBodyRenderer(root);

            SerializedObject avatarSerialized = new SerializedObject(playerAvatar);
            SetReference(avatarSerialized, "characterRenderer", bodyRenderer, log);
            SetReference(avatarSerialized, "paddleHolderRight", paddleHolderRight, log);
            SetReference(avatarSerialized, "paddleHolderLeft", paddleHolderLeft, log);
            SetReference(avatarSerialized, "ballHoldPointRight", ballHoldRight, log);
            SetReference(avatarSerialized, "ballHoldPointLeft", ballHoldLeft, log);
            avatarSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- Animation controller cầu nối ---
            BasePlayerAnimationController animationController = GetOrAdd<BasePlayerAnimationController>(root);
            animationController.animator = animator;

            // --- RacquetAnimator ---
            RacquetAnimator racquetAnimator = GetOrAdd<RacquetAnimator>(root);
            racquetAnimator.racquetTransform = paddleHolderRight != null ? paddleHolderRight : root.transform;
            racquetAnimator.alignSpeed = 540f;

            // --- Controller chính ---
            BasePlayerController controller;
            if (recipe.IsAI)
            {
                PickleballAIController ai = GetOrAdd<PickleballAIController>(root);
                ai.difficulty = AIDifficulty.Amateur;
                AssignAIProfileConfigs(ai, log);
                controller = ai;
            }
            else
            {
                PickleballPlayerController player = GetOrAdd<PickleballPlayerController>(root);
                player.playerTurnIndicator = turnIndicator;
                player.groundTapParticle = FindParticle(root, "GroundTapVFX");
                controller = player;
            }

            // --- Bộ nhận animation event: phải nằm trên chính node có Animator ---
            if (avatarNode != null)
            {
                PlayerAnimationEventReceiver receiver = GetOrAdd<PlayerAnimationEventReceiver>(avatarNode.gameObject);
                receiver.owner = controller;
                log.AppendLine("  PlayerAnimationEventReceiver ← node '" + avatarNode.name + "'");
            }

            // --- Field của BasePlayerController ---
            controller.teamID = recipe.TeamID;
            controller.playerProfile =
                AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilesFolder + "/" + recipe.ProfileAssetName + ".asset");
            controller.profileLimits =
                AssetDatabase.LoadAssetAtPath<PlayerProfileLimits>(ProfilesFolder + "/PlayerProfileLimits.asset");
            controller.avatar = avatarNode;
            controller.playerAvatar = playerAvatar;
            controller.ballHoldPoint = ballHoldRight;
            controller.serveIndicator = serveIndicator;
            controller.paddleHolder = paddleHolderRight;
            controller.paddleRenderer = FindPaddleRenderer(paddleHolderRight);
            controller.animationController = animationController;
            controller.audioSource = audioSource;
            controller.SetCourtSide(recipe.IsAI);

            // --- Tổng kết field ---
            Report(log, "avatar", avatarNode);
            Report(log, "animationController", animationController);
            Report(log, "playerAvatar", playerAvatar);
            Report(log, "ballHoldPoint", ballHoldRight);
            Report(log, "paddleHolder", paddleHolderRight);
            Report(log, "paddleRenderer", controller.paddleRenderer);
            Report(log, "serveIndicator", serveIndicator);
            Report(log, "playerProfile", controller.playerProfile);
            Report(log, "profileLimits", controller.profileLimits);
            Report(log, "audioSource", audioSource);
            if (!recipe.IsAI) Report(log, "playerTurnIndicator", turnIndicator);
            log.AppendLine("  teamID = " + recipe.TeamID);
        }

        // ------------------------------------------------------------------ Dò node

        /// <summary>Tìm node con đầu tiên trùng CHÍNH XÁC một trong các tên (ưu tiên theo thứ tự danh sách).</summary>
        private static Transform FindByNames(Transform root, IReadOnlyList<string> names)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < names.Count; i++)
            {
                foreach (Transform node in all)
                {
                    if (node != null && node.name == names[i]) return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm node gắn vợt. Ở nhân vật nữ, <c>RightPaddleHolder</c> chỉ là node bọc ngoài còn điểm gắn
        /// thật là node con tên <c>PaddleHolder</c> — hàm này đi xuống một cấp khi gặp trường hợp đó.
        /// </summary>
        private static Transform ResolvePaddleHolder(Transform root, IReadOnlyList<string> names)
        {
            Transform holder = FindByNames(root, names);
            if (holder == null) return null;

            for (int i = 0; i < holder.childCount; i++)
            {
                Transform child = holder.GetChild(i);
                if (child.name == "PaddleHolder") return child;
            }

            return holder;
        }

        /// <summary>Renderer của cây vợt để đổi material/hoạ tiết theo shop.</summary>
        private static Renderer FindPaddleRenderer(Transform paddleHolder)
        {
            return paddleHolder == null ? null : paddleHolder.GetComponentInChildren<Renderer>(true);
        }

        /// <summary>
        /// Chọn <see cref="SkinnedMeshRenderer"/> đại diện cho thân nhân vật: ưu tiên node tên
        /// <c>body</c>, nếu không có thì lấy cái nhiều đỉnh nhất.
        /// </summary>
        private static SkinnedMeshRenderer PickBodyRenderer(GameObject root)
        {
            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0) return null;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer.name == "body") return renderer;
            }

            return renderers.OrderByDescending(r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).First();
        }

        /// <summary>Tìm <see cref="ParticleSystem"/> theo tên node.</summary>
        private static ParticleSystem FindParticle(GameObject root, string nodeName)
        {
            foreach (ParticleSystem particle in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle != null && particle.name == nodeName) return particle;
            }

            return null;
        }

        // ------------------------------------------------------------------ AI profile

        /// <summary>
        /// Nạp bảng <see cref="PlayerProfileConfigs"/> (class [Serializable] nội tuyến, KHÔNG phải asset):
        /// mỗi bậc độ khó trỏ tới đúng asset profile đã sinh sẵn.
        /// </summary>
        private static void AssignAIProfileConfigs(PickleballAIController ai, StringBuilder log)
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
                log.AppendLine("  !! KHÔNG tìm thấy playerProfileConfigs.aiProfileDataList");
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
                profiles.GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilesFolder + "/" + mapping[i].assetName + ".asset");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  playerProfileConfigs: đã nạp " + mapping.Length + " bậc độ khó");
        }

        // ------------------------------------------------------------------ Tiện ích

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        /// <summary>Ghi một tham chiếu vào SerializedObject đang mở và ghi log khi field không tồn tại.</summary>
        private static void SetReference(SerializedObject serialized, string propertyPath, Object value,
                                         StringBuilder log)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                log.AppendLine("  !! không có field '" + propertyPath + "' trên " +
                               serialized.targetObject.GetType().Name);
                return;
            }

            property.objectReferenceValue = value;
            if (value == null) log.AppendLine("  ?  " + propertyPath + " = (không tìm ra node)");
        }

        /// <summary>Ghi kết quả gán một field vào log: tên node tìm được hoặc cảnh báo KHÔNG tìm ra.</summary>
        private static void Report(StringBuilder log, string fieldName, Object value)
        {
            if (value == null)
            {
                log.AppendLine("  !! " + fieldName + " = KHÔNG tìm ra");
                return;
            }

            log.AppendLine("  ok " + fieldName + " = " + value.name);
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
