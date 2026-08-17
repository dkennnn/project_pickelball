using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tổng hợp toàn bộ cửa hàng: gói coin/gem, túi đồ, danh mục vật phẩm nâng cấp được
    /// (nhân vật / cán vợt / vợt / bài tập), skin bóng, skin vợt và nước tăng lực.
    /// <para>
    /// Đây là điểm vào duy nhất cho mọi giao dịch mua và nâng cấp. Sau mỗi giao dịch làm
    /// đổi chỉ số, asset này gọi <see cref="PlayerLoadout.UpdateProfile"/> để chỉ số nhân vật
    /// cập nhật ngay lập tức, rồi phát <see cref="OnShopChanged"/> cho UI vẽ lại.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "Shop", menuName = "ScriptableObjects/MyShop")]
    public class Shop : ScriptableObject
    {
        /// <summary>Chỉ số skin bóng đang trang bị trong <see cref="ballsList"/>.</summary>
        [Header("Current Data")]
        public int selectedBallIndex;

        /// <summary>Chỉ số skin vợt đang trang bị trong <see cref="racketsList"/>.</summary>
        public int selectedRacketIndex;

        /// <summary>Kho tazo dùng để nâng cấp vật phẩm.</summary>
        [Header("Data Containers")]
        public TazoData tazoData;

        /// <summary>Hub dữ liệu để tra và trừ ví tiền của người chơi.</summary>
        public GameData gameData;

        /// <summary>Trang bị đang chọn và chỉ số tổng hợp sinh ra từ đó.</summary>
        public PlayerLoadout playerLoadout;

        /// <summary>Danh sách gói gem bán bằng tiền thật (IAP).</summary>
        [Header("Gem Packs")]
        public List<GemPack> gemPacks;

        /// <summary>Danh sách gói coin mua bằng gem.</summary>
        [Header("Coin Packs")]
        public List<CoinPack> coinPacks;

        /// <summary>Danh sách túi đồ theo bậc, dùng để tra ảnh và bảng thưởng.</summary>
        [Header("Kitbags")]
        public List<Kitbag> kitbags;

        /// <summary>Bốn tab vật phẩm nâng cấp được, kèm vật phẩm đang trang bị của từng tab.</summary>
        [Header("Upgradable Shop Items")]
        public List<ShopCategoryData> shopCategories;

        /// <summary>Danh sách skin bóng.</summary>
        [Header("Balls")]
        public List<BallShopItem> ballsList;

        /// <summary>Danh sách skin vợt.</summary>
        [Header("Racket Variations")]
        public List<ShopItem> racketsList;

        /// <summary>Số nước tăng lực đang có.</summary>
        [Header("Energy Boosters")]
        public int energyDrinks;

        /// <summary>Giá coin của một chai nước tăng lực.</summary>
        public int energyDrinkCost;

        /// <summary>Phát mỗi khi trạng thái cửa hàng đổi (mua, nâng cấp, trang bị) để UI vẽ lại.</summary>
        public static event Action OnShopChanged;

        // ------------------------------------------------------------------
        // Skin bóng / skin vợt
        // ------------------------------------------------------------------

        /// <summary>
        /// Mua skin bóng bằng coin rồi trang bị luôn.
        /// Trả về <c>false</c> nếu chỉ số sai, đã sở hữu hoặc không đủ coin.
        /// </summary>
        /// <param name="index">Chỉ số skin bóng trong <see cref="ballsList"/>.</param>
        public bool BuyBall(int index)
        {
            if (!BuyItem(ballsList, index)) return false;

            EquipBall(index);
            return true;
        }

        /// <summary>
        /// Mua skin vợt bằng coin rồi trang bị luôn.
        /// Trả về <c>false</c> nếu chỉ số sai, đã sở hữu hoặc không đủ coin.
        /// </summary>
        /// <param name="index">Chỉ số skin vợt trong <see cref="racketsList"/>.</param>
        public bool BuyRacket(int index)
        {
            if (!BuyItem(racketsList, index)) return false;

            EquipRacket(index);
            return true;
        }

        /// <summary>Mở khoá skin bóng không mất coin (phần thưởng, quảng cáo, dữ liệu save).</summary>
        /// <param name="index">Chỉ số skin bóng.</param>
        public void UnlockBall(int index)
        {
            ShopItem ball = GetBall(index);
            if (ball == null || ball.isPurchased) return;

            ball.isPurchased = true;
            NotifyShopChanged();
        }

        /// <summary>Mở khoá skin vợt không mất coin (phần thưởng, quảng cáo, dữ liệu save).</summary>
        /// <param name="index">Chỉ số skin vợt.</param>
        public void UnlockRacket(int index)
        {
            ShopItem racket = GetRacket(index);
            if (racket == null || racket.isPurchased) return;

            racket.isPurchased = true;
            NotifyShopChanged();
        }

        /// <summary>Trang bị skin bóng. Bỏ qua nếu chỉ số sai hoặc chưa sở hữu.</summary>
        /// <param name="index">Chỉ số skin bóng.</param>
        public void EquipBall(int index)
        {
            if (!IsBallPurchased(index)) return;

            selectedBallIndex = index;
            NotifyShopChanged();
        }

        /// <summary>Trang bị skin vợt. Bỏ qua nếu chỉ số sai hoặc chưa sở hữu.</summary>
        /// <param name="index">Chỉ số skin vợt.</param>
        public void EquipRacket(int index)
        {
            if (!IsRacketPurchased(index)) return;

            selectedRacketIndex = index;
            NotifyShopChanged();
        }

        /// <summary>True khi skin bóng này đang được trang bị.</summary>
        /// <param name="ballIndex">Chỉ số skin bóng.</param>
        public bool IsBallEquiped(int ballIndex)
        {
            return selectedBallIndex == ballIndex;
        }

        /// <summary>True khi skin vợt này đang được trang bị.</summary>
        /// <param name="racketIndex">Chỉ số skin vợt.</param>
        public bool IsRacketEquiped(int racketIndex)
        {
            return selectedRacketIndex == racketIndex;
        }

        /// <summary>True khi người chơi đã sở hữu skin bóng này.</summary>
        /// <param name="ballIndex">Chỉ số skin bóng.</param>
        public bool IsBallPurchased(int ballIndex)
        {
            ShopItem ball = GetBall(ballIndex);
            return ball != null && ball.isPurchased;
        }

        /// <summary>True khi người chơi đã sở hữu skin vợt này.</summary>
        /// <param name="racketIndex">Chỉ số skin vợt.</param>
        public bool IsRacketPurchased(int racketIndex)
        {
            ShopItem racket = GetRacket(racketIndex);
            return racket != null && racket.isPurchased;
        }

        /// <summary>Lấy skin bóng theo chỉ số; <c>null</c> nếu chỉ số nằm ngoài danh sách.</summary>
        /// <param name="index">Chỉ số skin bóng.</param>
        public ShopItem GetBall(int index)
        {
            if (ballsList == null || index < 0 || index >= ballsList.Count) return null;
            return ballsList[index];
        }

        /// <summary>Lấy skin vợt theo chỉ số; <c>null</c> nếu chỉ số nằm ngoài danh sách.</summary>
        /// <param name="index">Chỉ số skin vợt.</param>
        public ShopItem GetRacket(int index)
        {
            if (racketsList == null || index < 0 || index >= racketsList.Count) return null;
            return racketsList[index];
        }

        // ------------------------------------------------------------------
        // Vật phẩm nâng cấp được
        // ------------------------------------------------------------------

        /// <summary>
        /// Nâng cấp một bậc bằng tazo rồi tính lại ngay chỉ số nhân vật.
        /// Không làm gì nếu vật phẩm rỗng hoặc chưa đủ điều kiện nâng cấp.
        /// </summary>
        /// <param name="item">Vật phẩm cần nâng cấp.</param>
        public void UpgradeItemWithTazos(Item item)
        {
            if (item == null || gameData == null) return;
            if (!item.CanUpgradeWithTazos(gameData)) return;

            item.UpgradeWithTazos(gameData);

            RefreshLoadout();
            NotifyShopChanged();
        }

        /// <summary>
        /// Nâng cấp một bậc bằng gem rồi tính lại ngay chỉ số nhân vật.
        /// Không làm gì nếu vật phẩm rỗng hoặc chưa đủ điều kiện nâng cấp.
        /// </summary>
        /// <param name="item">Vật phẩm cần nâng cấp.</param>
        public void UpgradeItemWithGems(Item item)
        {
            if (item == null || gameData == null) return;
            if (!item.CanUpgradeWithGems(gameData)) return;

            item.UpgradeWithGems(gameData);

            RefreshLoadout();
            NotifyShopChanged();
        }

        /// <summary>
        /// Mua một vật phẩm bằng coin rồi trang bị luôn.
        /// Trả về <c>false</c> nếu vật phẩm rỗng, đã sở hữu hoặc không đủ coin.
        /// </summary>
        /// <param name="item">Vật phẩm muốn mua.</param>
        public bool PurchaseItem(Item item)
        {
            if (item == null || gameData == null) return false;
            if (!item.CanPurchase(gameData)) return false;
            if (!gameData.TrySpendCoins(item.purchaseCoins)) return false;

            item.isPurchased = true;
            SetSelectedItem(item);
            return true;
        }

        /// <summary>
        /// True khi có ít nhất một vật phẩm đang trang bị còn nâng cấp được
        /// (đủ tazo hoặc đủ gem) — dùng để bật chấm đỏ trên nút Shop.
        /// </summary>
        public bool IsAnySelectedUpgradeAvailable()
        {
            if (shopCategories == null || gameData == null) return false;

            for (int i = 0; i < shopCategories.Count; i++)
            {
                ShopCategoryData category = shopCategories[i];
                Item item = category != null ? category.selectedItem : null;
                if (item == null) continue;

                if (item.CanUpgradeWithTazos(gameData)) return true;
                if (item.CanUpgradeWithGems(gameData)) return true;
            }
            return false;
        }

        /// <summary>
        /// Trang bị một vật phẩm: uỷ quyền cho <see cref="PlayerLoadout.SetSelectedItem"/>
        /// (nơi tính lại chỉ số) rồi đồng bộ <see cref="ShopCategoryData.selectedItem"/> của shop.
        /// </summary>
        /// <param name="item">Vật phẩm muốn trang bị.</param>
        public void SetSelectedItem(Item item)
        {
            if (item == null || !item.isPurchased) return;

            if (playerLoadout != null) playerLoadout.SetSelectedItem(item);

            if (shopCategories != null)
            {
                for (int i = 0; i < shopCategories.Count; i++)
                {
                    ShopCategoryData category = shopCategories[i];
                    if (category == null || category.categoryType != item.itemType) continue;

                    category.selectedItem = item;
                }
            }

            NotifyShopChanged();
        }

        /// <summary>True khi vật phẩm đang được trang bị ở tab tương ứng.</summary>
        /// <param name="item">Vật phẩm cần kiểm tra.</param>
        public bool IsSelectedItem(Item item)
        {
            if (item == null || shopCategories == null) return false;

            for (int i = 0; i < shopCategories.Count; i++)
            {
                ShopCategoryData category = shopCategories[i];
                if (category != null && category.selectedItem == item) return true;
            }
            return false;
        }

        /// <summary>
        /// Đồng bộ shop với <see cref="playerLoadout"/> lúc khởi động: chọn lại vật phẩm hợp lệ
        /// cho mỗi tab (ưu tiên vật phẩm đang chọn, nếu không thì vật phẩm đã sở hữu đầu tiên),
        /// đẩy sang loadout rồi tính lại chỉ số một lần cuối.
        /// </summary>
        public void SetupLoadout()
        {
            if (gameData != null && tazoData == null) tazoData = gameData.tazoData;
            if (gameData != null && playerLoadout == null) playerLoadout = gameData.playerLoadout;

            if (shopCategories != null)
            {
                for (int i = 0; i < shopCategories.Count; i++)
                {
                    ShopCategoryData category = shopCategories[i];
                    if (category == null) continue;

                    Item selected = category.selectedItem;
                    if (selected == null || !selected.isPurchased) selected = FindFirstPurchased(category);
                    if (selected == null) continue;

                    category.selectedItem = selected;
                    if (playerLoadout != null) playerLoadout.SetSelectedItem(selected);
                }
            }

            ClampSkinIndices();
            RefreshLoadout();
            NotifyShopChanged();
        }

        // ------------------------------------------------------------------
        // Nước tăng lực và gói coin
        // ------------------------------------------------------------------

        /// <summary>Cộng nước tăng lực vào kho (phần thưởng, quảng cáo, dữ liệu save).</summary>
        /// <param name="amount">Số lượng cộng thêm; bỏ qua nếu &lt;= 0.</param>
        public void AddEnergyDrink(int amount = 1)
        {
            if (amount <= 0) return;

            energyDrinks += amount;
            NotifyShopChanged();
        }

        /// <summary>Tiêu nước tăng lực. Trả về <c>false</c> khi kho không đủ.</summary>
        /// <param name="amount">Số lượng cần tiêu.</param>
        public bool RemoveEnergyDrink(int amount = 1)
        {
            if (amount <= 0) return true;
            if (energyDrinks < amount) return false;

            energyDrinks -= amount;
            NotifyShopChanged();
            return true;
        }

        /// <summary>Mua một chai nước tăng lực bằng coin. Trả về <c>false</c> khi không đủ coin.</summary>
        public bool BuyEnergyDrink()
        {
            if (gameData == null) return false;
            if (!gameData.TrySpendCoins(energyDrinkCost)) return false;

            AddEnergyDrink(1);
            return true;
        }

        /// <summary>
        /// Mua một gói coin bằng gem. Trả về <c>false</c> khi gói rỗng hoặc không đủ gem.
        /// </summary>
        /// <param name="pack">Gói coin muốn mua.</param>
        public bool BuyCoinPack(CoinPack pack)
        {
            if (pack == null || gameData == null) return false;
            if (!gameData.TrySpendGems(pack.costInGems)) return false;

            gameData.IncreaseCoins(pack.coinsProvided);
            NotifyShopChanged();
            return true;
        }

        // ------------------------------------------------------------------
        // Túi đồ
        // ------------------------------------------------------------------

        /// <summary>Ảnh của một bậc túi đồ; <c>null</c> nếu chưa khai báo bậc đó.</summary>
        /// <param name="kitbagType">Bậc túi cần tra.</param>
        public Sprite GetKitbagSprite(KitbagType kitbagType)
        {
            Kitbag kitbag = GetKitbag(kitbagType);
            return kitbag != null ? kitbag.kitbagImage : null;
        }

        /// <summary>Asset túi đồ của một bậc; <c>null</c> nếu chưa khai báo bậc đó.</summary>
        /// <param name="kitbagType">Bậc túi cần tra.</param>
        public Kitbag GetKitbag(KitbagType kitbagType)
        {
            if (kitbags == null) return null;

            for (int i = 0; i < kitbags.Count; i++)
            {
                Kitbag kitbag = kitbags[i];
                if (kitbag != null && kitbag.kitbagType == kitbagType) return kitbag;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Tiện ích test
        // ------------------------------------------------------------------

        /// <summary>
        /// Mở khoá và đưa mọi vật phẩm/skin lên mức cao nhất — chỉ dùng để kiểm thử vòng lặp,
        /// không tiêu tốn tài nguyên của người chơi.
        /// </summary>
        [ContextMenu("Max All Items")]
        public void MaxAllItems()
        {
            if (shopCategories != null)
            {
                for (int i = 0; i < shopCategories.Count; i++)
                {
                    ShopCategoryData category = shopCategories[i];
                    if (category == null || category.items == null) continue;

                    for (int j = 0; j < category.items.Count; j++)
                    {
                        Item item = category.items[j];
                        if (item == null) continue;

                        item.isPurchased = true;
                        item.currentLevel = item.maxLevel;
                    }
                }
            }

            if (ballsList != null)
            {
                for (int i = 0; i < ballsList.Count; i++)
                {
                    if (ballsList[i] != null) ballsList[i].isPurchased = true;
                }
            }

            if (racketsList != null)
            {
                for (int i = 0; i < racketsList.Count; i++)
                {
                    if (racketsList[i] != null) racketsList[i].isPurchased = true;
                }
            }

            RefreshLoadout();
            NotifyShopChanged();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Trừ coin và đánh dấu đã sở hữu một skin trong danh sách. Dùng chung cho bóng và vợt.</summary>
        /// <param name="items">Danh sách skin.</param>
        /// <param name="index">Chỉ số skin trong danh sách.</param>
        private bool BuyItem<T>(List<T> items, int index) where T : ShopItem
        {
            if (items == null || index < 0 || index >= items.Count) return false;
            if (gameData == null) return false;

            T item = items[index];
            if (item == null || item.isPurchased) return false;
            if (!gameData.TrySpendCoins(item.cost)) return false;

            item.isPurchased = true;
            return true;
        }

        /// <summary>Vật phẩm đã sở hữu đầu tiên trong một tab; <c>null</c> nếu chưa sở hữu vật phẩm nào.</summary>
        private static Item FindFirstPurchased(ShopCategoryData category)
        {
            if (category == null || category.items == null) return null;

            for (int i = 0; i < category.items.Count; i++)
            {
                Item item = category.items[i];
                if (item != null && item.isPurchased) return item;
            }
            return null;
        }

        /// <summary>Đưa chỉ số skin đang chọn về vùng hợp lệ và đã sở hữu.</summary>
        private void ClampSkinIndices()
        {
            if (!IsBallPurchased(selectedBallIndex)) selectedBallIndex = 0;
            if (!IsRacketPurchased(selectedRacketIndex)) selectedRacketIndex = 0;
        }

        /// <summary>Bắt <see cref="playerLoadout"/> tính lại chỉ số tổng ngay lập tức.</summary>
        private void RefreshLoadout()
        {
            if (playerLoadout != null) playerLoadout.UpdateProfile();
        }

        private static void NotifyShopChanged()
        {
            Action handler = OnShopChanged;
            if (handler != null) handler.Invoke();
        }
    }
}
