using System.Collections.Generic;
using Pickleball.Data;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Bộ sưu tập: khối "Cards" liệt kê vật phẩm đã sở hữu theo từng nhóm,
    /// khối "Powerups" liệt kê số booster đang có trong kho.
    /// </summary>
    public class CollectionsUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.CollectionsUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Nhãn khối thẻ vật phẩm.</summary>
        [SerializeField] private TextMeshProUGUI cardTxt;

        /// <summary>Nhãn khối booster.</summary>
        [SerializeField] private TextMeshProUGUI powerupTxt;

        /// <summary>Node cha lưới thẻ vật phẩm.</summary>
        [SerializeField] private Transform cardsParent;

        /// <summary>Node cha lưới booster.</summary>
        [SerializeField] private Transform powerUpsParent;

        /// <summary>Prefab một ô thẻ vật phẩm.</summary>
        [SerializeField] private ShopItemCellView cardCellPrefab;

        /// <summary>Prefab một ô booster.</summary>
        [SerializeField] private StatItemCell powerupCellPrefab;

        private readonly List<ShopItemCellView> cardCells = new List<ShopItemCellView>();
        private readonly List<StatItemCell> powerupCells = new List<StatItemCell>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (cardTxt != null) cardTxt.text = "Cards";
            if (powerupTxt != null) powerupTxt.text = "Powerups";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            Shop.OnShopChanged += RefreshAll;
            RefreshAll();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Shop.OnShopChanged -= RefreshAll;
        }

        protected override void OnDestroy()
        {
            Shop.OnShopChanged -= RefreshAll;
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cardCells.Count; i++)
            {
                if (cardCells[i] != null) cardCells[i].OnClicked -= HandleCardClicked;
            }

            cardCells.Clear();
            powerupCells.Clear();

            base.OnDestroy();
        }

        /// <summary>Vẽ lại cả hai khối.</summary>
        public void RefreshAll()
        {
            RefreshCards();
            RefreshPowerups();
        }

        /// <summary>Dựng lại lưới thẻ vật phẩm từ toàn bộ nhóm trong <see cref="Shop"/>.</summary>
        public void RefreshCards()
        {
            if (cardsParent == null || cardCellPrefab == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            List<ShopCategoryData> categories = shop != null ? shop.shopCategories : null;

            List<Item> items = new List<Item>();
            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    ShopCategoryData category = categories[i];
                    if (category == null || category.items == null) continue;

                    for (int j = 0; j < category.items.Count; j++)
                    {
                        if (category.items[j] != null) items.Add(category.items[j]);
                    }
                }
            }

            while (cardCells.Count < items.Count)
            {
                ShopItemCellView cell = Instantiate(cardCellPrefab, cardsParent);
                cell.OnClicked += HandleCardClicked;
                cardCells.Add(cell);
            }

            for (int i = 0; i < cardCells.Count; i++)
            {
                ShopItemCellView cell = cardCells[i];
                if (cell == null) continue;

                bool used = i < items.Count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(items[i]);
            }
        }

        /// <summary>Dựng lại lưới booster từ kho booster của người chơi.</summary>
        public void RefreshPowerups()
        {
            if (powerUpsParent == null || powerupCellPrefab == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            BoostersData boostersData = gameData != null ? gameData.boostersData : null;
            List<BoosterCountData> list = boostersData != null ? boostersData.boosters : null;
            int count = list != null ? list.Count : 0;

            while (powerupCells.Count < count)
            {
                powerupCells.Add(Instantiate(powerupCellPrefab, powerUpsParent));
            }

            for (int i = 0; i < powerupCells.Count; i++)
            {
                StatItemCell cell = powerupCells[i];
                if (cell == null) continue;

                bool used = i < count && list[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                BoosterCountData entry = list[i];
                cell.Bind(
                    entry.boosterType.ToString().ToUpperInvariant(),
                    "x" + entry.count,
                    boostersData.GetImage(entry.boosterType));
            }
        }

        private void HandleCardClicked(Item item)
        {
            if (item == null) return;
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.ShopItemDetailUI, item);
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
