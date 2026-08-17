using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Danh sách vật phẩm của một nhóm. Nhận <see cref="ShopCategoryData"/> hoặc
    /// <see cref="ShopItemType"/> qua tham số <c>data</c> của
    /// <see cref="UIScreenBase.OnShow"/> rồi dựng một <see cref="ItemCardUI"/> cho mỗi vật phẩm.
    /// </summary>
    public class CategoryDisplayUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.CategoryDisplayUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn (tên nhóm đang xem).</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Node cha chứa các thẻ vật phẩm.</summary>
        [SerializeField] private Transform content;

        /// <summary>Prefab một thẻ vật phẩm.</summary>
        [SerializeField] private ItemCardUI itemCardPrefab;

        /// <summary>Nhóm vật phẩm đang hiển thị.</summary>
        public ShopCategoryData CurrentCategory { get; private set; }

        private readonly List<ItemCardUI> cards = new List<ItemCardUI>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            if (data is ShopCategoryData category) CurrentCategory = category;
            else if (data is ShopItemType itemType) CurrentCategory = FindCategory(itemType);
            else if (data is Item item) CurrentCategory = FindCategory(item.itemType);

            Shop.OnShopChanged += RefreshItems;
            RefreshItems();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Shop.OnShopChanged -= RefreshItems;
        }

        protected override void OnDestroy()
        {
            Shop.OnShopChanged -= RefreshItems;
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) cards[i].OnEquipped -= HandleItemChanged;
            }
            cards.Clear();

            base.OnDestroy();
        }

        /// <summary>Dựng lại danh sách thẻ vật phẩm của nhóm đang xem.</summary>
        public void RefreshItems()
        {
            if (content == null || itemCardPrefab == null) return;

            List<Item> items = CurrentCategory != null ? CurrentCategory.items : null;
            int count = items != null ? items.Count : 0;

            if (title != null && CurrentCategory != null)
            {
                title.text = string.IsNullOrEmpty(CurrentCategory.categoryName)
                    ? CurrentCategory.categoryType.ToString().ToUpperInvariant()
                    : CurrentCategory.categoryName.ToUpperInvariant();
            }

            while (cards.Count < count)
            {
                ItemCardUI card = Instantiate(itemCardPrefab, content);
                card.OnEquipped += HandleItemChanged;
                cards.Add(card);
            }

            for (int i = 0; i < cards.Count; i++)
            {
                ItemCardUI card = cards[i];
                if (card == null) continue;

                bool used = i < count;
                card.gameObject.SetActive(used);
                if (used) card.Bind(items[i]);
            }
        }

        private void HandleItemChanged(Item item)
        {
            RefreshItems();
        }

        private static ShopCategoryData FindCategory(ShopItemType itemType)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            List<ShopCategoryData> list = shop != null ? shop.shopCategories : null;
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].categoryType == itemType) return list[i];
            }
            return null;
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.DressingRoomUI);
            else Hide();
        }
    }
}
