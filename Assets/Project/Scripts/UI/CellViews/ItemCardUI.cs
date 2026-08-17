using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ vật phẩm đầy đủ: ảnh, cấp, 5-7 thanh chỉ số và bộ nút
    /// Mua / Nâng cấp bằng tazo / Nâng cấp bằng gem / Trang bị.
    /// Thẻ tự bấm thẳng vào <see cref="Shop"/> nên dùng được ở cả
    /// <see cref="CategoryDisplayUI"/> lẫn <see cref="ShopItemDetailUI"/>.
    /// </summary>
    public class ItemCardUI : MonoBehaviour
    {
        /// <summary>Ảnh vật phẩm.</summary>
        [SerializeField] private Image icon;

        /// <summary>Ô chữ hiển thị cấp hiện tại.</summary>
        [SerializeField] private TextMeshProUGUI levelNo;

        /// <summary>Node "MAX LEVEL" khi vật phẩm đã đạt bậc cao nhất.</summary>
        [SerializeField] private GameObject maxLevelReached;

        /// <summary>Nút nâng cấp bằng tazo.</summary>
        [SerializeField] private Button upgradeTazosButton;

        /// <summary>Số tazo cần cho lần nâng cấp kế tiếp.</summary>
        [SerializeField] private TextMeshProUGUI coinsText;

        /// <summary>Nút nâng cấp bằng gem.</summary>
        [SerializeField] private Button upgradeGemsButton;

        /// <summary>Số gem cần cho lần nâng cấp kế tiếp.</summary>
        [SerializeField] private TextMeshProUGUI gemsText;

        /// <summary>Nút trang bị vật phẩm.</summary>
        [SerializeField] private Button equipButton;

        /// <summary>Chữ trên nút trang bị (EQUIP / EQUIPPED).</summary>
        [SerializeField] private TextMeshProUGUI equipText;

        /// <summary>Nút mua vật phẩm bằng coin.</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Giá mua bằng coin.</summary>
        [SerializeField] private TextMeshProUGUI coinText;

        /// <summary>Node cha chứa các thanh chỉ số.</summary>
        [SerializeField] private Transform propertyBG;

        /// <summary>Prefab một thanh chỉ số.</summary>
        [SerializeField] private ProfilePropertyCellView propertyCellPrefab;

        /// <summary>Viền báo vật phẩm đang được trang bị.</summary>
        [SerializeField] private GameObject selectedCard;

        /// <summary>Hiệu ứng làm xám khi vật phẩm chưa mua.</summary>
        [SerializeField] private GreyscaleUIHierarchy greyscale;

        /// <summary>Nút bấm phủ cả thẻ (tuỳ chọn) để mở màn chi tiết.</summary>
        [SerializeField] private Button cardButton;

        /// <summary>Phát khi bấm vào thẻ.</summary>
        public event Action<Item> OnClicked;

        /// <summary>Phát sau khi nâng cấp bằng tazo thành công.</summary>
        public event Action<Item> OnUpgraded;

        /// <summary>Phát sau khi mua thành công.</summary>
        public event Action<Item> OnPurchased;

        /// <summary>Phát sau khi trang bị.</summary>
        public event Action<Item> OnEquipped;

        /// <summary>Vật phẩm đang gắn vào thẻ.</summary>
        public Item BoundItem { get; private set; }

        private readonly List<ProfilePropertyCellView> propertyCells = new List<ProfilePropertyCellView>();
        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một vật phẩm vào thẻ và vẽ lại toàn bộ.
        /// </summary>
        /// <param name="item">Vật phẩm cần hiển thị; null sẽ làm rỗng thẻ.</param>
        public void Bind(Item item)
        {
            Wire();
            BoundItem = item;
            Refresh();
        }

        /// <summary>Vẽ lại thẻ theo trạng thái mới nhất của vật phẩm đang gắn.</summary>
        public void Refresh()
        {
            Item item = BoundItem;
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            if (item == null)
            {
                SetActive(upgradeTazosButton, false);
                SetActive(upgradeGemsButton, false);
                SetActive(equipButton, false);
                SetActive(buyButton, false);
                if (maxLevelReached != null) maxLevelReached.SetActive(false);
                if (selectedCard != null) selectedCard.SetActive(false);
                return;
            }

            if (icon != null)
            {
                icon.enabled = item.itemSprite != null;
                if (item.itemSprite != null) icon.sprite = item.itemSprite;
            }

            if (levelNo != null) levelNo.text = "LVL." + (item.currentLevel + 1);

            bool maxed = item.currentLevel >= item.maxLevel;
            if (maxLevelReached != null) maxLevelReached.SetActive(item.isPurchased && maxed);

            bool owned = item.isPurchased;
            bool equipped = owned && IsEquipped(item);

            SetActive(buyButton, !owned);
            SetActive(upgradeTazosButton, owned && !maxed);
            SetActive(upgradeGemsButton, owned && !maxed);
            SetActive(equipButton, owned && !equipped);

            if (coinText != null) coinText.text = Utilities.FormatCount(item.purchaseCoins);
            if (coinsText != null) coinsText.text = Utilities.FormatCount(GetCost(item.tazosRequired, item.currentLevel));
            if (gemsText != null) gemsText.text = Utilities.FormatCount(GetCost(item.gemsRequired, item.currentLevel));
            if (equipText != null) equipText.text = equipped ? "EQUIPPED" : "EQUIP";

            if (buyButton != null) buyButton.interactable = gameData != null && item.CanPurchase(gameData);
            if (upgradeTazosButton != null) upgradeTazosButton.interactable = gameData != null && item.CanUpgradeWithTazos(gameData);
            if (upgradeGemsButton != null) upgradeGemsButton.interactable = gameData != null && item.CanUpgradeWithGems(gameData);

            if (selectedCard != null) selectedCard.SetActive(equipped);
            if (greyscale != null) greyscale.SetGreyscale(!owned);

            RefreshProperties(item, gameData);
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void RefreshProperties(Item item, GameData gameData)
        {
            if (propertyBG == null || propertyCellPrefab == null) return;

            List<ProfileProperty> values = item.GetPropertyValues(gameData);
            if (values == null) values = new List<ProfileProperty>();

            PropertiesData propertiesData = gameData != null ? gameData.propertiesData : null;

            while (propertyCells.Count < values.Count)
            {
                ProfilePropertyCellView cell = Instantiate(propertyCellPrefab, propertyBG);
                propertyCells.Add(cell);
            }

            for (int i = 0; i < propertyCells.Count; i++)
            {
                ProfilePropertyCellView cell = propertyCells[i];
                if (cell == null) continue;

                bool used = i < values.Count;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                ProfileProperty property = values[i];
                PropertyData display = propertiesData != null ? propertiesData.GetPropertyData(property.propertyType) : null;
                cell.Bind(property, display);
            }
        }

        private static bool IsEquipped(Item item)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            return shop != null && shop.IsSelectedItem(item);
        }

        private static int GetCost(int[] costs, int level)
        {
            if (costs == null || costs.Length == 0) return 0;
            if (level < 0 || level >= costs.Length) return 0;
            return costs[level];
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }

        private Shop ResolveShop()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            return gameData != null ? gameData.shopData : null;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (upgradeTazosButton != null) upgradeTazosButton.onClick.AddListener(HandleUpgradeTazos);
            if (upgradeGemsButton != null) upgradeGemsButton.onClick.AddListener(HandleUpgradeGems);
            if (equipButton != null) equipButton.onClick.AddListener(HandleEquip);
            if (buyButton != null) buyButton.onClick.AddListener(HandleBuy);
            if (cardButton != null) cardButton.onClick.AddListener(HandleCardClick);
        }

        private void HandleCardClick()
        {
            OnClicked?.Invoke(BoundItem);
        }

        private void HandleUpgradeTazos()
        {
            Shop shop = ResolveShop();
            if (shop == null || BoundItem == null) return;

            shop.UpgradeItemWithTazos(BoundItem);
            Refresh();
            OnUpgraded?.Invoke(BoundItem);
        }

        private void HandleUpgradeGems()
        {
            Shop shop = ResolveShop();
            if (shop == null || BoundItem == null) return;

            shop.UpgradeItemWithGems(BoundItem);
            Refresh();
            OnUpgraded?.Invoke(BoundItem);
        }

        private void HandleEquip()
        {
            Shop shop = ResolveShop();
            if (shop == null || BoundItem == null) return;

            shop.SetSelectedItem(BoundItem);
            Refresh();
            OnEquipped?.Invoke(BoundItem);
        }

        private void HandleBuy()
        {
            Shop shop = ResolveShop();
            if (shop == null || BoundItem == null) return;

            if (!shop.PurchaseItem(BoundItem))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough coins");
                return;
            }

            Refresh();
            OnPurchased?.Invoke(BoundItem);
        }

        private void OnDestroy()
        {
            if (upgradeTazosButton != null) upgradeTazosButton.onClick.RemoveListener(HandleUpgradeTazos);
            if (upgradeGemsButton != null) upgradeGemsButton.onClick.RemoveListener(HandleUpgradeGems);
            if (equipButton != null) equipButton.onClick.RemoveListener(HandleEquip);
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuy);
            if (cardButton != null) cardButton.onClick.RemoveListener(HandleCardClick);

            OnClicked = null;
            OnUpgraded = null;
            OnPurchased = null;
            OnEquipped = null;
        }
    }
}
