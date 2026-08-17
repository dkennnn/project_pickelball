using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh asset <see cref="TutorialConfiguration"/> với đúng 11 bước onboarding của bản gốc.
    /// Chạy lại nhiều lần an toàn: asset đã tồn tại được ghi đè giá trị thay vì tạo trùng.
    /// </summary>
    public static class TutorialDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string TutorialFolder = RootFolder + "/Tutorial";
        private const string ConfigurationAssetPath = TutorialFolder + "/TutorialConfiguration.asset";

        /// <summary>Tạo/cập nhật asset cấu hình hướng dẫn rồi lưu xuống đĩa.</summary>
        [MenuItem("Pickleball/Generate Tutorial Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(TutorialFolder);

            TutorialConfiguration configuration = LoadOrCreate<TutorialConfiguration>(ConfigurationAssetPath);

            configuration.enableTutorialSystem = true;
            configuration.startingTutorial = TutorialType.MainMenuForcedPlay;
            configuration.tutorialSteps = BuildSteps();

            EditorUtility.SetDirty(configuration);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TutorialDataGenerator] Đã sinh " + configuration.tutorialSteps.Count +
                      " bước hướng dẫn vào " + ConfigurationAssetPath);
        }

        /// <summary>
        /// Chuỗi onboarding của bản gốc:
        /// MainMenuForcedPlay → WelcomeMessage → CourtSidesAndKitchenInfo → ReceiveInfo →
        /// CrossServeInfo → BasicHit → TargetedHit → KitchenHit → TwoPointEasyBotMatch →
        /// TutorialKitbagReward → ForcedGripUpgrade → TutorialCompleted.
        /// </summary>
        private static List<TutorialConfiguration.TutorialStepConfig> BuildSteps()
        {
            return new List<TutorialConfiguration.TutorialStepConfig>
            {
                Step(TutorialType.MainMenuForcedPlay, TutorialType.WelcomeMessage,
                    1.0f, false, ScreenType.MainMenu),

                Step(TutorialType.WelcomeMessage, TutorialType.CourtSidesAndKitchenInfo,
                    1.0f, false, ScreenType.Gameplay),

                Step(TutorialType.CourtSidesAndKitchenInfo, TutorialType.ReceiveInfo,
                    1.0f, false, ScreenType.Gameplay),

                Step(TutorialType.ReceiveInfo, TutorialType.CrossServeInfo,
                    0.5f, false, ScreenType.Gameplay),

                Step(TutorialType.CrossServeInfo, TutorialType.BasicHit,
                    1.0f, false, ScreenType.Gameplay),

                Step(TutorialType.BasicHit, TutorialType.TargetedHit,
                    1.5f, true, ScreenType.Gameplay),

                Step(TutorialType.TargetedHit, TutorialType.KitchenHit,
                    1.5f, true, ScreenType.Gameplay),

                Step(TutorialType.KitchenHit, TutorialType.TwoPointEasyBotMatch,
                    1.5f, true, ScreenType.Gameplay),

                Step(TutorialType.TwoPointEasyBotMatch, TutorialType.TutorialKitbagReward,
                    1.5f, true, ScreenType.Gameplay),

                Step(TutorialType.TutorialKitbagReward, TutorialType.ForcedGripUpgrade,
                    1.0f, false, ScreenType.RewardUI),

                Step(TutorialType.ForcedGripUpgrade, TutorialType.TutorialCompleted,
                    0.5f, false, ScreenType.DressingRoomUI)
            };
        }

        private static TutorialConfiguration.TutorialStepConfig Step(
            TutorialType type, TutorialType next, float delay, bool isGameplay, ScreenType requiredScreen)
        {
            return new TutorialConfiguration.TutorialStepConfig
            {
                tutorialType = type,
                isEnabled = true,
                isCompleted = false,
                nextTutorial = next,
                nextTutorialStartAfterDelay = delay,
                isGameplayTutorial = isGameplay,
                requiredUIScreen = requiredScreen
            };
        }

        // --------------------------------------------------------------------- Helpers

        /// <summary>Nạp asset tại đường dẫn, tạo mới nếu chưa có.</summary>
        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        /// <summary>Tạo thư mục asset nếu chưa tồn tại (hỗ trợ tạo lồng nhiều cấp).</summary>
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
