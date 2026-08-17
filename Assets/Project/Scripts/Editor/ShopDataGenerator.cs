using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh asset <c>Shop.asset</c> — bảng dữ liệu cửa hàng nối toàn bộ vòng lặp kinh tế:
    /// gói coin/gem, túi đồ, bốn tab vật phẩm nâng cấp được, skin bóng, skin vợt và nước tăng lực.
    /// <para>
    /// Mọi tham chiếu đều được nạp lại từ đĩa (<c>ScriptableObjects/Game</c>,
    /// <c>ScriptableObjects/Shop</c>, <c>ScriptableObjects/Rewards</c>) nên chạy lại nhiều lần
    /// vẫn cho cùng kết quả. Tiến trình mua skin đã có sẵn trong asset được giữ nguyên,
    /// chỉ giá tiền là bị ghi đè theo bảng cân bằng bên dưới.
    /// </para>
    /// </summary>
    public static class ShopDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string ShopFolder = RootFolder + "/Shop";
        private const string GameFolder = RootFolder + "/Game";
        private const string RewardsFolder = RootFolder + "/Rewards";

        private const string ShopAssetPath = ShopFolder + "/Shop.asset";

        /// <summary>Giá coin của 5 skin bóng; phần tử đầu là skin mặc định miễn phí.</summary>
        private static readonly int[] BallCosts = { 0, 500, 1000, 1500, 2000 };

        /// <summary>Giá coin của 5 skin vợt; phần tử đầu là skin mặc định miễn phí.</summary>
        private static readonly int[] RacketCosts = { 0, 800, 1600, 2400, 3200 };

        /// <summary>Giá coin của một chai nước tăng lực.</summary>
        private const int EnergyDrinkCost = 250;

        /// <summary>Tạo/cập nhật <c>Shop.asset</c> rồi nối nó vào <c>GameData.asset</c>.</summary>
        [MenuItem("Pickleball/Generate Shop Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ShopFolder);

            Shop shop = LoadOrCreate<Shop>(ShopAssetPath);

            // ---- Tham chiếu dữ liệu -------------------------------------------------
            shop.tazoData = AssetDatabase.LoadAssetAtPath<TazoData>(GameFolder + "/TazoData.asset");
            shop.gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameFolder + "/GameData.asset");
            shop.playerLoadout = AssetDatabase.LoadAssetAtPath<PlayerLoadout>(ShopFolder + "/PlayerLoadout.asset");

            // ---- Gói tiền và túi đồ -------------------------------------------------
            shop.gemPacks = LoadAllSortedByName<GemPack>(RewardsFolder);
            shop.coinPacks = LoadAllSortedByName<CoinPack>(RewardsFolder);
            shop.kitbags = LoadAllSortedByName<Kitbag>(RewardsFolder);

            // ---- Bốn tab vật phẩm nâng cấp được -------------------------------------
            shop.shopCategories = new List<ShopCategoryData>
            {
                MakeCategory<Character>(ShopItemType.Character, "Characters"),
                MakeCategory<Grip>(ShopItemType.Grip, "Grips"),
                MakeCategory<Paddle>(ShopItemType.Paddle, "Paddles"),
                MakeCategory<Workout>(ShopItemType.Workout, "Workouts")
            };

            // ---- Skin bóng / skin vợt (chưa có art nên sprite & material để trống) ---
            shop.ballsList = BuildBalls(shop.ballsList);
            shop.racketsList = BuildRackets(shop.racketsList);
            shop.selectedBallIndex = 0;
            shop.selectedRacketIndex = 0;

            // ---- Nước tăng lực ------------------------------------------------------
            shop.energyDrinks = 0;
            shop.energyDrinkCost = EnergyDrinkCost;

            EditorUtility.SetDirty(shop);

            LinkGameData(shop);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ShopDataGenerator] Đã sinh {ShopAssetPath}: " +
                      $"{Count(shop.gemPacks)} gem pack, {Count(shop.coinPacks)} coin pack, " +
                      $"{Count(shop.kitbags)} kitbag, {Count(shop.shopCategories)} tab vật phẩm, " +
                      $"{Count(shop.ballsList)} skin bóng, {Count(shop.racketsList)} skin vợt.");
        }

        // ------------------------------------------------------------------- Categories

        /// <summary>
        /// Gom mọi vật phẩm cùng loại trong thư mục Shop thành một tab, sắp theo giá mua tăng dần
        /// và chọn sẵn vật phẩm rẻ nhất (thường là vật phẩm miễn phí có sẵn).
        /// </summary>
        private static ShopCategoryData MakeCategory<T>(ShopItemType categoryType, string categoryName) where T : Item
        {
            List<T> found = LoadAll<T>(ShopFolder);

            List<Item> items = new List<Item>();
            for (int i = 0; i < found.Count; i++)
            {
                if (found[i] != null) items.Add(found[i]);
            }

            items.Sort(CompareByPurchaseCoins);

            return new ShopCategoryData
            {
                categoryType = categoryType,
                categoryName = categoryName,
                items = items,
                selectedItem = items.Count > 0 ? items[0] : null
            };
        }

        /// <summary>Sắp vật phẩm theo giá mua tăng dần, giá bằng nhau thì theo tên để ổn định thứ tự.</summary>
        private static int CompareByPurchaseCoins(Item a, Item b)
        {
            if (a == null) return b == null ? 0 : 1;
            if (b == null) return -1;

            int byCost = a.purchaseCoins.CompareTo(b.purchaseCoins);
            if (byCost != 0) return byCost;

            return string.Compare(a.name, b.name, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------- Balls & Rackets

        /// <summary>Dựng 5 skin bóng theo bảng giá, giữ nguyên art và trạng thái sở hữu đã có.</summary>
        private static List<BallShopItem> BuildBalls(List<BallShopItem> existing)
        {
            List<BallShopItem> result = new List<BallShopItem>();

            for (int i = 0; i < BallCosts.Length; i++)
            {
                BallShopItem item = existing != null && i < existing.Count && existing[i] != null
                    ? existing[i]
                    : new BallShopItem();

                item.cost = BallCosts[i];
                if (BallCosts[i] == 0) item.isPurchased = true;

                result.Add(item);
            }

            return result;
        }

        /// <summary>Dựng 5 skin vợt theo bảng giá, giữ nguyên art và trạng thái sở hữu đã có.</summary>
        private static List<ShopItem> BuildRackets(List<ShopItem> existing)
        {
            List<ShopItem> result = new List<ShopItem>();

            for (int i = 0; i < RacketCosts.Length; i++)
            {
                ShopItem item = existing != null && i < existing.Count && existing[i] != null
                    ? existing[i]
                    : new ShopItem();

                item.cost = RacketCosts[i];
                if (RacketCosts[i] == 0) item.isPurchased = true;

                result.Add(item);
            }

            return result;
        }

        // ---------------------------------------------------------------------- Wiring

        /// <summary>Nối <c>Shop.asset</c> và <c>SlotsData.asset</c> vào <c>GameData.asset</c>.</summary>
        private static void LinkGameData(Shop shop)
        {
            GameData gameData = shop.gameData;
            if (gameData == null)
            {
                Debug.LogWarning("[ShopDataGenerator] Không tìm thấy GameData.asset — hãy chạy 'Pickleball/Generate Item Data' trước.");
                return;
            }

            gameData.shopData = shop;

            SlotsData slotsData = AssetDatabase.LoadAssetAtPath<SlotsData>(RewardsFolder + "/SlotsData.asset");
            if (slotsData != null) gameData.slotsData = slotsData;
            else Debug.LogWarning("[ShopDataGenerator] Không tìm thấy SlotsData.asset — hãy chạy 'Pickleball/Generate Reward Data' trước.");

            EditorUtility.SetDirty(gameData);
        }

        // --------------------------------------------------------------------- Helpers

        /// <summary>Nạp mọi asset thuộc kiểu yêu cầu trong một thư mục.</summary>
        private static List<T> LoadAll<T>(string folder) where T : ScriptableObject
        {
            List<T> result = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder)) return result;

            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            if (guids == null) return result;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) result.Add(asset);
            }

            return result;
        }

        /// <summary>Nạp mọi asset thuộc kiểu yêu cầu trong một thư mục và sắp theo tên asset.</summary>
        private static List<T> LoadAllSortedByName<T>(string folder) where T : ScriptableObject
        {
            List<T> result = LoadAll<T>(folder);
            result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return result;
        }

        /// <summary>Số phần tử của một danh sách, an toàn với <c>null</c>.</summary>
        private static int Count<T>(List<T> list)
        {
            return list != null ? list.Count : 0;
        }

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
