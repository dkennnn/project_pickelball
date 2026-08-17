using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô một nhóm vật phẩm (Character / Grip / Paddle / Workout) trong phòng thay đồ.
    /// Hiển thị vật phẩm đang trang bị của nhóm và báo khi nhóm có thể nâng cấp.
    /// </summary>
    public class ShopCategoryCellView : MonoBehaviour
    {
        /// <summary>Nền kiêm nút bấm của ô.</summary>
        [SerializeField] private Button bg;

        /// <summary>Ảnh vật phẩm đang trang bị của nhóm.</summary>
        [SerializeField] private Image icon;

        /// <summary>Tên nhóm vật phẩm.</summary>
        [SerializeField] private TextMeshProUGUI itemType;

        /// <summary>Ô chữ hiển thị cấp của vật phẩm đang trang bị.</summary>
        [SerializeField] private TextMeshProUGUI level;

        /// <summary>Node bọc phần cấp.</summary>
        [SerializeField] private GameObject levelRoot;

        /// <summary>Biểu tượng báo nhóm có vật phẩm nâng cấp được.</summary>
        [SerializeField] private GameObject upgradeIcon;

        /// <summary>Phát khi người chơi bấm vào nhóm.</summary>
        public event Action<ShopCategoryData> OnClicked;

        /// <summary>Nhóm vật phẩm đang gắn vào ô.</summary>
        public ShopCategoryData BoundCategory { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một nhóm vật phẩm vào ô.
        /// </summary>
        /// <param name="category">Nhóm cần hiển thị; null sẽ làm rỗng ô.</param>
        public void Bind(ShopCategoryData category)
        {
            Wire();
            BoundCategory = category;

            if (category == null)
            {
                if (itemType != null) itemType.text = string.Empty;
                if (icon != null) icon.enabled = false;
                if (levelRoot != null) levelRoot.SetActive(false);
                if (upgradeIcon != null) upgradeIcon.SetActive(false);
                return;
            }

            if (itemType != null)
            {
                itemType.text = string.IsNullOrEmpty(category.categoryName)
                    ? category.categoryType.ToString()
                    : category.categoryName;
            }

            Item selected = category.selectedItem;
            Sprite sprite = selected != null && selected.itemSprite != null ? selected.itemSprite : category.categoryIcon;

            if (icon != null)
            {
                icon.enabled = sprite != null;
                if (sprite != null) icon.sprite = sprite;
            }

            if (level != null && selected != null) level.text = "LVL." + (selected.currentLevel + 1);
            if (levelRoot != null) levelRoot.SetActive(selected != null && selected.isPurchased);

            if (upgradeIcon != null) upgradeIcon.SetActive(HasUpgradeAvailable(category));
        }

        private bool HasUpgradeAvailable(ShopCategoryData category)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (gameData == null || category == null || category.items == null) return false;

            for (int i = 0; i < category.items.Count; i++)
            {
                Item item = category.items[i];
                if (item == null || !item.isPurchased) continue;
                if (item.CanBeUpgrade(gameData)) return true;
            }
            return false;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (bg == null) bg = GetComponentInChildren<Button>(true);
            if (bg != null) bg.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(BoundCategory);
        }

        private void OnDestroy()
        {
            if (bg != null) bg.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
