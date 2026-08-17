using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh toàn bộ asset phần thưởng, túi đồ, gói coin/gem và dữ liệu locker.
    /// Số liệu bám theo bản gốc đã giải mã (xem decoded_scriptableobjects_1/2.json).
    /// Chạy lại nhiều lần an toàn: asset đã tồn tại được ghi đè giá trị thay vì tạo trùng.
    /// </summary>
    public static class RewardDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string RewardsFolder = RootFolder + "/Rewards";
        private const string GameDataAssetPath = RootFolder + "/Game/GameData.asset";

        /// <summary>Tạo/cập nhật toàn bộ asset phần thưởng rồi lưu xuống đĩa.</summary>
        [MenuItem("Pickleball/Generate Reward Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(RewardsFolder);

            Dictionary<string, BulkReward> bulk = GenerateBulkRewards();
            List<Kitbag> kitbags = GenerateKitbags(bulk);

            GenerateCoinPacks();
            GenerateGemPacks();
            GenerateRewardsCollection(bulk, kitbags);
            GenerateTutorialKitbag();
            GenerateSlotsData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RewardDataGenerator] Đã sinh xong dữ liệu phần thưởng vào " + RewardsFolder);
        }

        // ------------------------------------------------------------------ BulkRewards

        /// <summary>
        /// Bảng thưởng dùng chung cho mọi túi đồ và quà hằng ngày.
        /// Bản gốc có 51 asset tách riêng cho từng bậc túi; ở đây gom lại thành một bộ
        /// 13 asset phủ đủ 11 RewardType, phân bậc bằng biến thể Small/Large của Coins và Gems.
        /// probability được chuẩn hoá về [0,1] (bản gốc dùng thang 0..40).
        /// </summary>
        private static Dictionary<string, BulkReward> GenerateBulkRewards()
        {
            Dictionary<string, BulkReward> map = new Dictionary<string, BulkReward>();

            Add(map, "Coins_Small", RewardType.Coins, 20, 150, 0.40f);
            Add(map, "Coins_Large", RewardType.Coins, 300, 600, 0.30f);
            Add(map, "Gems_Small", RewardType.Gems, 5, 15, 0.20f);
            Add(map, "Gems_Large", RewardType.Gems, 25, 50, 0.15f);
            Add(map, "GripTazos", RewardType.GripTazos, 5, 25, 0.35f);
            Add(map, "PaddleTazos", RewardType.PaddleTazos, 5, 25, 0.20f);
            Add(map, "WorkoutTazos", RewardType.WorkoutTazos, 5, 25, 0.12f);
            Add(map, "CharacterTazos", RewardType.CharacterTazos, 5, 20, 0.10f);
            Add(map, "StaminaBooster", RewardType.StaminaBooster, 1, 5, 0.25f);
            Add(map, "SpeedBooster", RewardType.SpeedBooster, 1, 3, 0.22f);
            Add(map, "SpinBooster", RewardType.SpinBooster, 1, 3, 0.22f);
            Add(map, "SwingBooster", RewardType.SwingBooster, 1, 3, 0.22f);
            Add(map, "PowerBooster", RewardType.PowerBooster, 1, 3, 0.22f);

            return map;
        }

        private static void Add(Dictionary<string, BulkReward> map, string key, RewardType type,
            int min, int max, float probability)
        {
            BulkReward reward = LoadOrCreate<BulkReward>(RewardsFolder + "/BulkReward_" + key + ".asset");

            reward.rewardType = type;
            reward.minQuantity = min;
            reward.maxQuantity = max;
            reward.probability = probability;

            EditorUtility.SetDirty(reward);
            map[key] = reward;
        }

        // --------------------------------------------------------------------- Kitbags

        /// <summary>
        /// Bốn túi đồ theo đúng bảng số liệu bản gốc. Bậc túi càng cao càng nhiều lượt bốc
        /// và càng nhiều dòng thưởng giá trị lớn trong bảng.
        /// </summary>
        private static List<Kitbag> GenerateKitbags(Dictionary<string, BulkReward> bulk)
        {
            Kitbag starter = MakeKitbag("StarterKit", KitbagType.AdsKitbag, "Starter Kit",
                0, 0, 5, 10, 2, 3, 0, false,
                Pick(bulk, "Coins_Small", "Gems_Small", "GripTazos", "PaddleTazos",
                    "WorkoutTazos", "StaminaBooster"));

            Kitbag rookie = MakeKitbag("RookieKit", KitbagType.Level1, "Rookie Kit",
                500, 50, 20, 30, 3, 4, 2, true,
                Pick(bulk, "Coins_Small", "Gems_Small", "GripTazos", "PaddleTazos",
                    "WorkoutTazos", "CharacterTazos", "StaminaBooster", "SpeedBooster",
                    "SpinBooster", "SwingBooster", "PowerBooster"));

            Kitbag pro = MakeKitbag("ProKit", KitbagType.Level2, "Pro Kit",
                1500, 150, 30, 40, 3, 7, 3, true,
                Pick(bulk, "Coins_Small", "Coins_Large", "Gems_Small", "Gems_Large",
                    "GripTazos", "PaddleTazos", "WorkoutTazos", "CharacterTazos",
                    "StaminaBooster", "SpeedBooster", "SpinBooster", "SwingBooster", "PowerBooster"));

            Kitbag champion = MakeKitbag("ChampionKit", KitbagType.Level3, "Champion Kit",
                3000, 300, 40, 50, 5, 9, 4, true,
                Pick(bulk, "Coins_Large", "Gems_Large", "GripTazos", "PaddleTazos",
                    "WorkoutTazos", "CharacterTazos", "StaminaBooster", "SpeedBooster",
                    "SpinBooster", "SwingBooster", "PowerBooster"));

            return new List<Kitbag> { starter, rookie, pro, champion };
        }

        private static Kitbag MakeKitbag(string assetName, KitbagType type, string displayName,
            int costInCoins, int costInGems, int minTazos, int maxTazos,
            int minPicks, int maxPicks, int totalAds, bool isAdAvailable, List<Reward> rewards)
        {
            Kitbag kitbag = LoadOrCreate<Kitbag>(RewardsFolder + "/Kitbag_" + assetName + ".asset");

            kitbag.kitbagType = type;
            kitbag.kitbagName = displayName;
            kitbag.costInCoins = costInCoins;
            kitbag.costInGems = costInGems;
            kitbag.minTazosProvided = minTazos;
            kitbag.maxTazosProvided = maxTazos;
            kitbag.minPicks = minPicks;
            kitbag.maxPicks = maxPicks;
            kitbag.totalAds = totalAds;
            kitbag.watchedAds = 0;
            kitbag.isAdAvailable = isAdAvailable;
            kitbag.isRewardCollected = false;
            kitbag.shouldShowInterstitial = false;
            kitbag.rewards = rewards;
            if (kitbag.rewardShowcaseImages == null) kitbag.rewardShowcaseImages = new List<Sprite>();

            EditorUtility.SetDirty(kitbag);
            return kitbag;
        }

        private static List<Reward> Pick(Dictionary<string, BulkReward> bulk, params string[] keys)
        {
            List<Reward> list = new List<Reward>();
            for (int i = 0; i < keys.Length; i++)
            {
                BulkReward reward;
                if (bulk.TryGetValue(keys[i], out reward)) list.Add(reward);
            }
            return list;
        }

        // ------------------------------------------------------------------- CoinPacks

        /// <summary>Sáu gói coin bán bằng gem, số liệu lấy nguyên từ bản gốc.</summary>
        private static void GenerateCoinPacks()
        {
            MakeCoinPack("Bonus", "Bonus Pack", 0, 100);
            MakeCoinPack("Bronze", "Bronze Pack", 50, 500);
            MakeCoinPack("Silver", "Silver Pack", 100, 1200);
            MakeCoinPack("Gold", "Gold Pack", 200, 3000);
            MakeCoinPack("Platinum", "Platinum Pack", 500, 8000);
            MakeCoinPack("Diamond", "Diamond Pack", 1000, 20000);
        }

        private static void MakeCoinPack(string assetName, string packName, int costInGems, int coinsProvided)
        {
            CoinPack pack = LoadOrCreate<CoinPack>(RewardsFolder + "/CoinPack_" + assetName + ".asset");

            pack.packName = packName;
            pack.costInGems = costInGems;
            pack.coinsProvided = coinsProvided;

            EditorUtility.SetDirty(pack);
        }

        // -------------------------------------------------------------------- GemPacks

        /// <summary>
        /// Sáu gói gem. Số gem giữ nguyên bản gốc, còn giá quy về USD và iapID
        /// đặt lại theo tiền tố riêng "pickleball." để không đụng product id của bản gốc.
        /// </summary>
        private static void GenerateGemPacks()
        {
            MakeGemPack("Bonus", "Bonus Pack", "ADS", 10);
            MakeGemPack("Mini", "Mini Pack", "$0.99", 100);
            MakeGemPack("Starter", "Starter Pack", "$4.99", 550);
            MakeGemPack("Value", "Value Pack", "$9.99", 1200);
            MakeGemPack("Premium", "Premium Pack", "$19.99", 2500);
            MakeGemPack("Ultimate", "Ultimate Pack", "$49.99", 7000);
        }

        private static void MakeGemPack(string assetName, string packName, string costInUSD, int gemsProvided)
        {
            GemPack pack = LoadOrCreate<GemPack>(RewardsFolder + "/GemPack_" + assetName + ".asset");

            pack.packName = packName;
            pack.costInUSD = costInUSD;
            pack.gemsProvided = gemsProvided;
            pack.iapID = "pickleball." + assetName.ToLowerInvariant() + "_pack";

            EditorUtility.SetDirty(pack);
        }

        // ------------------------------------------------------------- RewardsCollection

        /// <summary>Nối bảng thưởng hằng ngày và bốn túi đồ vào một asset tổng.</summary>
        private static void GenerateRewardsCollection(Dictionary<string, BulkReward> bulk, List<Kitbag> kitbags)
        {
            RewardsCollection collection =
                LoadOrCreate<RewardsCollection>(RewardsFolder + "/RewardsCollection.asset");

            collection.dailyRewards = Pick(bulk, "Coins_Small", "Gems_Small",
                "GripTazos", "PaddleTazos", "WorkoutTazos");

            collection.dailyChallengeRewards = Pick(bulk, "Coins_Large", "Gems_Large",
                "GripTazos", "PaddleTazos", "WorkoutTazos", "CharacterTazos",
                "StaminaBooster", "SpeedBooster", "SpinBooster", "SwingBooster", "PowerBooster");

            collection.kitbags = kitbags;

            EditorUtility.SetDirty(collection);
        }

        // --------------------------------------------------------------- DynamicKitbag

        /// <summary>Túi thưởng của tutorial: 2 lượt bốc từ 3 dòng thưởng, số liệu bản gốc.</summary>
        private static void GenerateTutorialKitbag()
        {
            DynamicKitbag kitbag = LoadOrCreate<DynamicKitbag>(RewardsFolder + "/DynamicKitbag_Tutorial.asset");

            kitbag.kitbagType = KitbagType.AdsKitbag;
            kitbag.kitbagName = "TutorialKitbag";
            kitbag.minPicks = 2;
            kitbag.maxPicks = 2;
            kitbag.rewards = new List<DynamicKitbagReward>
            {
                MakeDynamicReward(RewardType.Coins, 100, 150, 1f),
                MakeDynamicReward(RewardType.GripTazos, 20, 25, 1f),
                MakeDynamicReward(RewardType.Gems, 0, 10, 1f)
            };
            kitbag.uniqueRewards = new List<Reward>();

            EditorUtility.SetDirty(kitbag);
        }

        private static DynamicKitbagReward MakeDynamicReward(RewardType type, int min, int max, float probability)
        {
            return new DynamicKitbagReward
            {
                rewardType = type,
                minQuantity = min,
                maxQuantity = max,
                probability = probability
            };
        }

        // ------------------------------------------------------------------- SlotsData

        /// <summary>Locker 8 ô, mở sẵn 4 ô, thời gian chờ theo bậc túi giống bản gốc.</summary>
        private static void GenerateSlotsData()
        {
            SlotsData slotsData = LoadOrCreate<SlotsData>(RewardsFolder + "/SlotsData.asset");

            slotsData.maxSlots = 8;
            slotsData.defaultUnlockedSlots = 4;
            slotsData.gemsToUnlockSlot = 150;

            slotsData.kitbagSettings = new List<SlotsData.KitbagSettings>
            {
                MakeKitbagSettings(KitbagType.Level1, 5400f, 25),
                MakeKitbagSettings(KitbagType.Level2, 7200f, 50),
                MakeKitbagSettings(KitbagType.Level3, 10800f, 100),
                MakeKitbagSettings(KitbagType.DailyReward, 0f, 10),
                MakeKitbagSettings(KitbagType.AdsKitbag, 0f, 10)
            };

            // 8 ô rỗng, 4 ô đầu mở khoá sẵn.
            slotsData.ResetSlots();

            // Chỉ nối tới GameData nếu agent khác đã sinh asset đó; không tự tạo mới.
            GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataAssetPath);
            if (gameData == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:GameData");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    gameData = AssetDatabase.LoadAssetAtPath<GameData>(path);
                }
            }

            if (gameData != null) slotsData.gameData = gameData;
            else Debug.LogWarning("[RewardDataGenerator] Chưa tìm thấy asset GameData, " +
                                  "hãy gán tay vào SlotsData.gameData sau khi sinh xong.");

            EditorUtility.SetDirty(slotsData);
        }

        private static SlotsData.KitbagSettings MakeKitbagSettings(KitbagType type, float seconds, int gemsToSkip)
        {
            return new SlotsData.KitbagSettings
            {
                kitbagType = type,
                unlockTimeInSeconds = seconds,
                gemsToSkipTimer = gemsToSkip
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
