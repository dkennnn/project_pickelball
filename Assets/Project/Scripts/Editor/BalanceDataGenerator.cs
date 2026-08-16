using System.Collections.Generic;
using Pickleball.Data;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh toàn bộ asset ScriptableObject cân bằng game từ số liệu đã giải mã của bản gốc.
    /// Chạy lại nhiều lần an toàn: asset đã tồn tại sẽ được ghi đè giá trị thay vì tạo trùng.
    /// Mọi con số cân bằng chỉ nằm ở đây, không hardcode trong class runtime.
    /// </summary>
    public static class BalanceDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string GameFolder = RootFolder + "/Game";
        private const string ProfilesFolder = RootFolder + "/Profiles";

        /// <summary>Tạo/cập nhật toàn bộ asset dữ liệu cân bằng rồi lưu xuống đĩa.</summary>
        [MenuItem("Pickleball/Generate Balance Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(GameFolder);
            EnsureFolder(ProfilesFolder);

            GenerateGameSettings();
            GeneratePlayerProfileLimits();
            GeneratePlayerProfiles();
            GeneratePlayerLevels();
            GenerateStadiumsData();
            GenerateBoostersData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BalanceDataGenerator] Đã sinh xong dữ liệu cân bằng vào " + RootFolder);
        }

        // ---------------------------------------------------------------- GameSettings

        private static void GenerateGameSettings()
        {
            GameSettings settings = LoadOrCreate<GameSettings>(GameFolder + "/GameSettings.asset");

            settings.normalMaxScore = 6;
            settings.normalWinMargin = 2;
            settings.normalGameBetCoins = 100;

            settings.quickMaxScore = 3;
            settings.quickWinMargin = 0;
            settings.quickGameBetCoins = 50;

            settings.ballRadius = 0.06f;
            settings.serveTimeout = 10f;
            settings.EnergyBoostMultiplier = 2f;
            settings.BoosterMaxShots = 5;

            settings.selectedMode = MatchModeType.Normal;
            settings.matchModeConfigs = new List<MatchModeConfig>
            {
                MakeModeConfig(MatchModeType.Normal, "Normal Mode",
                    "Your main career mode. First to 6 points wins — play smart, climb the ranks, and own the court.",
                    0, false),
                MakeModeConfig(MatchModeType.QuickMatch, "Quick Match",
                    "Jump in fast! First to 3 points wins — perfect when you want a quick game without the full grind.",
                    0, false),
                MakeModeConfig(MatchModeType.LAN, "LAN Mode",
                    "Play with friends on the same network. Connect locally and battle it out together on the court.",
                    0, false),
                MakeModeConfig(MatchModeType.Target, "Target Mode",
                    "Aim for the targets and perfect your shots. This mode is coming soon — stay tuned!",
                    0, true)
            };

            settings.ResetToNormalSettings();
            EditorUtility.SetDirty(settings);
        }

        private static MatchModeConfig MakeModeConfig(MatchModeType mode, string title, string description,
            int unlockLevel, bool isComingSoon)
        {
            return new MatchModeConfig
            {
                modeType = mode,
                infoTitle = title,
                infoDescription = description,
                unlockLevel = unlockLevel,
                isComingSoon = isComingSoon
            };
        }

        // ---------------------------------------------------------- PlayerProfileLimits

        private static void GeneratePlayerProfileLimits()
        {
            PlayerProfileLimits limits = LoadOrCreate<PlayerProfileLimits>(ProfilesFolder + "/PlayerProfileLimits.asset");

            limits.minHeight = 1.5f; limits.maxHeight = 1.8f;

            limits.minMovementSpeed = 3.5f; limits.maxMovementSpeed = 5.0f;
            limits.minAgility = 1.5f; limits.maxAgility = 3.5f;
            limits.minTrackingAccuracy = 0.05f; limits.maxTrackingAccuracy = 1.0f;

            limits.minShotRange = 0.5f; limits.maxShotRange = 2.0f;
            limits.minVolleyRange = 2.0f; limits.maxVolleyRange = 6.0f;

            limits.minShotTakingRate = 0f; limits.maxShotTakingRate = 1f;

            limits.minStamina = 50f; limits.maxStamina = 150f;
            limits.minStaminaRechargeRate = 2f; limits.maxStaminaRechargeRate = 4f;
            limits.minStaminaDepletionRate = 6f; limits.maxStaminaDepletionRate = 8f;

            limits.minProbablityToTakeOutShots = 1f; limits.maxProbablityToTakeOutShots = 1f;
            limits.minMaxShotError = 0f; limits.maxMaxShotError = 0f;
            limits.minTrackingErrorProbability = 0f; limits.maxTrackingErrorProbability = 1f;

            limits.minShotPower = 0f; limits.maxShotPower = 1f;
            limits.minSpinAbility = 0f; limits.maxSpinAbility = 1f;
            limits.minSwingAbility = 0f; limits.maxSwingAbility = 1f;
            limits.minSwingSpinProbability = 0f; limits.maxSwingSpinProbability = 1f;

            EditorUtility.SetDirty(limits);
        }

        // --------------------------------------------------------------- PlayerProfiles

        private static void GeneratePlayerProfiles()
        {
            //          asset                 tên          spd  agi  acc  shR  vol  stam rech depl pOut sErr tErr pow  spin swng ssp  hgt   str
            WriteProfile("AI_0_Tutorial", "AI Player", 2.7f, 1.1f, 1.00f, 1.2f, 2.2f, 120f, 2.4f, 6.5f, 0.5f, 0.00f, 0.55f, 0.3f, 0.2f, 0.6f, 0.3f, 1.70f, 1.00f);
            WriteProfile("AI_1_Newbie", "AI Player", 2.0f, 0.6f, 0.10f, 0.7f, 2.0f, 100f, 2.0f, 6.0f, 0.7f, 0.20f, 0.80f, 0.1f, 0.0f, 0.2f, 0.0f, 1.60f, 1.00f);
            WriteProfile("AI_2_Amateur", "AI Player", 2.2f, 0.7f, 0.20f, 0.9f, 2.0f, 110f, 2.2f, 6.0f, 0.6f, 0.15f, 0.60f, 0.2f, 0.1f, 0.3f, 0.1f, 1.70f, 0.95f);
            WriteProfile("AI_3_Competitor", "AI Player", 2.7f, 1.1f, 0.30f, 1.2f, 2.2f, 120f, 2.4f, 6.5f, 0.5f, 0.20f, 0.55f, 0.3f, 0.2f, 0.6f, 0.3f, 1.70f, 0.95f);
            WriteProfile("AI_4_PowerHitter", "AI Player", 3.1f, 1.5f, 0.40f, 1.4f, 3.0f, 110f, 2.6f, 6.8f, 0.4f, 0.10f, 0.45f, 0.7f, 0.2f, 0.4f, 0.4f, 1.80f, 0.95f);
            WriteProfile("AI_5_Pro", "AI Player", 3.5f, 2.0f, 0.50f, 1.5f, 3.5f, 130f, 2.8f, 6.2f, 0.3f, 0.06f, 0.35f, 0.5f, 0.8f, 0.7f, 0.6f, 1.75f, 0.95f);
            WriteProfile("AI_6_VeryHard", "AI Player", 4.0f, 3.0f, 0.80f, 1.7f, 4.5f, 140f, 3.2f, 6.0f, 0.2f, 0.05f, 0.20f, 0.7f, 0.7f, 0.8f, 0.6f, 1.80f, 0.98f);
            WriteProfile("AI_7_Master", "AI Player", 4.5f, 3.5f, 0.95f, 1.9f, 6.0f, 150f, 3.5f, 7.0f, 0.1f, 0.00f, 0.00f, 1.0f, 1.0f, 1.0f, 0.8f, 1.80f, 1.00f);

            WriteProfile("Player_Default", "Player", 4.0f, 2.5f, 1.00f, 1.2f, 2.5f, 150f, 2.0f, 6.0f, 1.0f, 0.00f, 1.00f, 0.0f, 1.0f, 1.0f, 1.0f, 1.80f, 1.00f);
        }

        private static void WriteProfile(string assetName, string playerName,
            float movementSpeed, float agility, float trackingAccuracy,
            float shotRange, float volleyRange,
            float maxStamina, float staminaRechargeRate, float staminaDepletionRate,
            float probablityToTakeOutShots, float maxShotError, float trackingErrorProbablity,
            float shotPower, float spinAbility, float swingAbility, float swingSpinProbablity,
            float height, float shotTakingRate)
        {
            PlayerProfile profile = LoadOrCreate<PlayerProfile>(ProfilesFolder + "/" + assetName + ".asset");

            profile.playerName = playerName;
            profile.height = height;

            profile.MovementSpeed = movementSpeed;
            profile.Agility = agility;
            profile.TrackingAccuracy = trackingAccuracy;

            profile.shotRange = shotRange;
            profile.volleyRange = volleyRange;

            profile.shotTakingRate = shotTakingRate;

            profile.maxStamina = maxStamina;
            profile.staminaRechargeRate = staminaRechargeRate;
            profile.staminaDepletionRate = staminaDepletionRate;

            profile.ProbablityToTakeOutShots = probablityToTakeOutShots;
            profile.maxShotError = maxShotError;
            profile.TrackingErrorProbablity = trackingErrorProbablity;

            profile.shotPower = shotPower;
            profile.spinAbility = spinAbility;
            profile.swingAbility = swingAbility;
            profile.swingSpinProbablity = swingSpinProbablity;

            EditorUtility.SetDirty(profile);
        }

        // ----------------------------------------------------------------- PlayerLevels

        private static void GeneratePlayerLevels()
        {
            PlayerLevels playerLevels = LoadOrCreate<PlayerLevels>(GameFolder + "/PlayerLevels.asset");

            playerLevels.levels = new List<PlayerLevelData>
            {
                MakeLevel(1, 0,
                    Reward(RewardType.Coins, 100), Reward(RewardType.Gems, 10)),
                MakeLevel(2, 120,
                    Reward(RewardType.GripTazos, 10), Reward(RewardType.Coins, 200), Reward(RewardType.Gems, 20)),
                MakeLevel(3, 200,
                    Reward(RewardType.WorkoutTazos, 15), Reward(RewardType.PaddleTazos, 20)),
                MakeLevel(4, 300,
                    Reward(RewardType.Coins, 500), Reward(RewardType.BallVisual, 1)),
                MakeLevel(5, 500,
                    Reward(RewardType.StaminaBooster, 1), Reward(RewardType.Gems, 15)),
                MakeLevel(6, 750,
                    Reward(RewardType.Coins, 300), Reward(RewardType.GripTazos, 25), Reward(RewardType.PaddleTazos, 30)),
                MakeLevel(7, 1100,
                    Reward(RewardType.PaddleTazos, 15), Reward(RewardType.WorkoutTazos, 20), Reward(RewardType.StaminaBooster, 1)),
                MakeLevel(8, 1500,
                    Reward(RewardType.Coins, 600), Reward(RewardType.Gems, 25), Reward(RewardType.PaddleTazos, 50)),
                MakeLevel(9, 2000,
                    Reward(RewardType.Gems, 50), Reward(RewardType.RacketVisual, 1), Reward(RewardType.GripTazos, 30)),
                MakeLevel(10, 2500,
                    Reward(RewardType.WorkoutTazos, 50), Reward(RewardType.StaminaBooster, 2), Reward(RewardType.Coins, 500))
            };

            EditorUtility.SetDirty(playerLevels);
        }

        private static PlayerLevelData MakeLevel(int level, int experience, params DynamicReward[] rewards)
        {
            return new PlayerLevelData
            {
                level = level,
                experience = experience,
                rewards = new List<DynamicReward>(rewards),
                isRewardCollected = false
            };
        }

        private static DynamicReward Reward(RewardType type, int value)
        {
            return new DynamicReward(type, value);
        }

        // ----------------------------------------------------------------- StadiumsData

        private static void GenerateStadiumsData()
        {
            StadiumsData stadiumsData = LoadOrCreate<StadiumsData>(GameFolder + "/StadiumsData.asset");

            stadiumsData.stadiums = new List<StadiumData>
            {
                MakeStadium("Gully Grounds", "Stadium1", 0, 2),
                MakeStadium("Club Arena", "Stadium2", 3, 5),
                MakeStadium("Champion's Dome", "Stadium3", 6, 8),
                MakeStadium("Purple Stadium", "Stadium4", 9, 11),
                MakeStadium("Jungle Stadium", "Stadium5", 12, 14)
            };

            EditorUtility.SetDirty(stadiumsData);
        }

        private static StadiumData MakeStadium(string name, string sceneName, int minLevel, int maxLevel)
        {
            return new StadiumData
            {
                stadiumName = name,
                stadiumSceneName = sceneName,
                minLevel = minLevel,
                maxLevel = maxLevel
            };
        }

        // ----------------------------------------------------------------- BoostersData

        private static void GenerateBoostersData()
        {
            BoostersData boostersData = LoadOrCreate<BoostersData>(GameFolder + "/BoostersData.asset");

            boostersData.boosters = new List<BoosterCountData>
            {
                new BoosterCountData { boosterType = BoosterType.Stamina, count = 0 },
                new BoosterCountData { boosterType = BoosterType.Swing, count = 0 },
                new BoosterCountData { boosterType = BoosterType.Spin, count = 0 },
                new BoosterCountData { boosterType = BoosterType.Power, count = 0 },
                new BoosterCountData { boosterType = BoosterType.Speed, count = 0 }
            };

            boostersData.boosterImages = new List<BoosterImageData>
            {
                new BoosterImageData { boosterType = BoosterType.Stamina },
                new BoosterImageData { boosterType = BoosterType.Swing },
                new BoosterImageData { boosterType = BoosterType.Spin },
                new BoosterImageData { boosterType = BoosterType.Power },
                new BoosterImageData { boosterType = BoosterType.Speed }
            };

            EditorUtility.SetDirty(boostersData);
        }

        // ---------------------------------------------------------------------- Helpers

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
