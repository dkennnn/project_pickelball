using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Cửa hàng (bản gốc là "IAPPurchaseUI"). Bản này KHÔNG làm IAP nên chỉ giữ đúng cấu trúc
    /// ba khối Kits / Coins / Gems, hiển thị ví tiền và cho mua gói coin bằng gem qua
    /// <see cref="Shop.BuyCoinPack"/>.
    /// </summary>
    public class ShopUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.IAPPurchase;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Lớp chặn thao tác trong lúc xử lý giao dịch.</summary>
        [SerializeField] private GameObject blockerImage;

        /// <summary>Node cha khối túi đồ.</summary>
        [SerializeField] private Transform categoryContainerKits;

        /// <summary>Node cha khối gói coin.</summary>
        [SerializeField] private Transform categoryContainerCoins;

        /// <summary>Node cha khối gói gem.</summary>
        [SerializeField] private Transform categoryContainerGems;

        /// <summary>Prefab một dòng sản phẩm (dùng chung cho cả ba khối).</summary>
        [SerializeField] private StatItemCell productCellPrefab;

        /// <summary>Số coin hiện có.</summary>
        [SerializeField] private TextMeshProUGUI coinsText;

        /// <summary>Số gem hiện có.</summary>
        [SerializeField] private TextMeshProUGUI gemsText;

        private readonly List<StatItemCell> kitCells = new List<StatItemCell>();
        private readonly List<StatItemCell> coinCells = new List<StatItemCell>();
        private readonly List<StatItemCell> gemCells = new List<StatItemCell>();

        private GameData gameData;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (title != null) title.text = "SHOP";
            if (blockerImage != null) blockerImage.SetActive(false);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            if (gameData != null)
            {
                gameData.OnTotalCoinsUpdated += HandleCoins;
                gameData.OnTotalGemsUpdated += HandleGems;
            }

            RefreshCurrencies();
            RefreshProducts();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < coinCells.Count; i++)
            {
                if (coinCells[i] != null) coinCells[i].OnClicked -= HandleCoinPackClicked;
            }

            kitCells.Clear();
            coinCells.Clear();
            gemCells.Clear();

            base.OnDestroy();
        }

        /// <summary>Vẽ lại số coin/gem đang có.</summary>
        public void RefreshCurrencies()
        {
            if (gameData == null) return;

            if (coinsText != null) coinsText.text = Utilities.FormatCount(gameData.totalCoins);
            if (gemsText != null) gemsText.text = Utilities.FormatCount(gameData.totalGems);
        }

        /// <summary>Dựng lại ba khối sản phẩm từ <see cref="Shop"/>.</summary>
        public void RefreshProducts()
        {
            Shop shop = gameData != null ? gameData.shopData : null;
            if (shop == null || productCellPrefab == null) return;

            BuildKitbags(shop);
            BuildCoinPacks(shop);
            BuildGemPacks(shop);
        }

        private void BuildKitbags(Shop shop)
        {
            if (categoryContainerKits == null) return;

            List<Kitbag> list = shop.kitbags;
            int count = list != null ? list.Count : 0;

            while (kitCells.Count < count) kitCells.Add(Instantiate(productCellPrefab, categoryContainerKits));

            for (int i = 0; i < kitCells.Count; i++)
            {
                StatItemCell cell = kitCells[i];
                if (cell == null) continue;

                bool used = i < count && list[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                Kitbag kitbag = list[i];
                cell.Bind(
                    string.IsNullOrEmpty(kitbag.kitbagName) ? kitbag.kitbagType.ToString() : kitbag.kitbagName,
                    Utilities.FormatCount(kitbag.costInGems) + " gems",
                    kitbag.kitbagImage);
            }
        }

        private void BuildCoinPacks(Shop shop)
        {
            if (categoryContainerCoins == null) return;

            List<CoinPack> list = shop.coinPacks;
            int count = list != null ? list.Count : 0;

            while (coinCells.Count < count)
            {
                StatItemCell cell = Instantiate(productCellPrefab, categoryContainerCoins);
                cell.OnClicked += HandleCoinPackClicked;
                coinCells.Add(cell);
            }

            for (int i = 0; i < coinCells.Count; i++)
            {
                StatItemCell cell = coinCells[i];
                if (cell == null) continue;

                bool used = i < count && list[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                CoinPack pack = list[i];
                cell.Bind(
                    string.IsNullOrEmpty(pack.packName) ? Utilities.FormatCount(pack.coinsProvided) : pack.packName,
                    Utilities.FormatCount(pack.costInGems) + " gems",
                    pack.ItemSprite);
            }
        }

        private void BuildGemPacks(Shop shop)
        {
            if (categoryContainerGems == null) return;

            List<GemPack> list = shop.gemPacks;
            int count = list != null ? list.Count : 0;

            while (gemCells.Count < count) gemCells.Add(Instantiate(productCellPrefab, categoryContainerGems));

            for (int i = 0; i < gemCells.Count; i++)
            {
                StatItemCell cell = gemCells[i];
                if (cell == null) continue;

                // Gói gem chỉ mua được bằng tiền thật; bản offline không làm IAP nên chỉ hiển thị.
                bool used = i < count && list[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                GemPack pack = list[i];
                cell.Bind(
                    string.IsNullOrEmpty(pack.packName) ? Utilities.FormatCount(pack.gemsProvided) : pack.packName,
                    string.IsNullOrEmpty(pack.costInUSD) ? "N/A" : pack.costInUSD,
                    pack.ItemSprite);
            }
        }

        private void HandleCoinPackClicked(string label)
        {
            Shop shop = gameData != null ? gameData.shopData : null;
            List<CoinPack> list = shop != null ? shop.coinPacks : null;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                CoinPack pack = list[i];
                if (pack == null) continue;

                string name = string.IsNullOrEmpty(pack.packName) ? Utilities.FormatCount(pack.coinsProvided) : pack.packName;
                if (name != label) continue;

                if (!shop.BuyCoinPack(pack) && ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough gems");
                RefreshCurrencies();
                return;
            }
        }

        private void HandleCoins(int total)
        {
            if (coinsText != null) coinsText.text = Utilities.FormatCount(total);
        }

        private void HandleGems(int total)
        {
            if (gemsText != null) gemsText.text = Utilities.FormatCount(total);
        }

        private void Unsubscribe()
        {
            if (gameData == null) return;

            gameData.OnTotalCoinsUpdated -= HandleCoins;
            gameData.OnTotalGemsUpdated -= HandleGems;
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
