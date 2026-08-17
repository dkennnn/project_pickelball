using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô vật phẩm trong cửa hàng / tủ đồ. Dùng chung cho vật phẩm nâng cấp
    /// (<see cref="Item"/>) và cho skin bóng/vợt (<see cref="ShopItem"/> theo chỉ số).
    /// </summary>
    public class ShopItemCellView : MonoBehaviour
    {
        /// <summary>Nền kiêm nút bấm của ô.</summary>
        [SerializeField] private Button bg;

        /// <summary>Ảnh vật phẩm.</summary>
        [SerializeField] private Image icon;

        /// <summary>Tên/loại vật phẩm.</summary>
        [SerializeField] private TextMeshProUGUI itemType;

        /// <summary>Ô chữ hiển thị cấp vật phẩm.</summary>
        [SerializeField] private TextMeshProUGUI level;

        /// <summary>Node bọc phần cấp; ẩn khi vật phẩm chưa mua.</summary>
        [SerializeField] private GameObject levelRoot;

        /// <summary>Biểu tượng báo có thể nâng cấp.</summary>
        [SerializeField] private GameObject upgradeIcon;

        /// <summary>Dấu hiệu vật phẩm đang được trang bị.</summary>
        [SerializeField] private GameObject selectedMark;

        /// <summary>Hiệu ứng làm xám khi chưa mua được.</summary>
        [SerializeField] private StarterKit.UIKit.GreyscaleUIHierarchy greyscale;

        /// <summary>Phát khi người chơi bấm vào ô đang gắn một <see cref="Item"/>.</summary>
        public event Action<Item> OnClicked;

        /// <summary>Phát khi người chơi bấm vào ô skin, tham số là chỉ số trong danh sách.</summary>
        public event Action<int> OnIndexClicked;

        /// <summary>Vật phẩm đang gắn vào ô; null nếu ô đang hiển thị skin.</summary>
        public Item BoundItem { get; private set; }

        /// <summary>Chỉ số skin đang gắn; -1 nếu ô đang hiển thị <see cref="Item"/>.</summary>
        public int BoundIndex { get; private set; } = -1;

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một vật phẩm nâng cấp vào ô.
        /// </summary>
        /// <param name="item">Vật phẩm cần hiển thị; null sẽ làm rỗng ô.</param>
        public void Bind(Item item)
        {
            Wire();

            BoundItem = item;
            BoundIndex = -1;

            if (item == null)
            {
                if (icon != null) icon.enabled = false;
                if (itemType != null) itemType.text = string.Empty;
                if (levelRoot != null) levelRoot.SetActive(false);
                if (upgradeIcon != null) upgradeIcon.SetActive(false);
                if (selectedMark != null) selectedMark.SetActive(false);
                return;
            }

            if (icon != null)
            {
                icon.enabled = item.itemSprite != null;
                if (item.itemSprite != null) icon.sprite = item.itemSprite;
            }

            if (itemType != null) itemType.text = string.IsNullOrEmpty(item.itemName) ? item.itemType.ToString() : item.itemName;
            if (level != null) level.text = "LVL." + (item.currentLevel + 1);
            if (levelRoot != null) levelRoot.SetActive(item.isPurchased);

            GameData gameData = ResolveGameData();
            bool canUpgrade = gameData != null && item.isPurchased && item.CanBeUpgrade(gameData);
            if (upgradeIcon != null) upgradeIcon.SetActive(canUpgrade);

            if (selectedMark != null) selectedMark.SetActive(item.isActive);
            if (greyscale != null) greyscale.SetGreyscale(!item.isPurchased);
        }

        /// <summary>
        /// Gắn dữ liệu một skin bóng/vợt vào ô.
        /// </summary>
        /// <param name="shopItem">Skin cần hiển thị; null sẽ làm rỗng ô.</param>
        /// <param name="index">Chỉ số của skin trong danh sách của <see cref="Shop"/>.</param>
        /// <param name="equipped">True nếu skin đang được trang bị.</param>
        public void Bind(ShopItem shopItem, int index, bool equipped = false)
        {
            Wire();

            BoundItem = null;
            BoundIndex = index;

            if (shopItem == null)
            {
                if (icon != null) icon.enabled = false;
                if (itemType != null) itemType.text = string.Empty;
                return;
            }

            if (icon != null)
            {
                icon.enabled = shopItem.icon != null;
                if (shopItem.icon != null) icon.sprite = shopItem.icon;
            }

            if (itemType != null) itemType.text = shopItem.isPurchased ? string.Empty : Utilities.FormatCount(shopItem.cost);
            if (levelRoot != null) levelRoot.SetActive(false);
            if (upgradeIcon != null) upgradeIcon.SetActive(false);
            if (selectedMark != null) selectedMark.SetActive(equipped);
            if (greyscale != null) greyscale.SetGreyscale(!shopItem.isPurchased);
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
            if (BoundItem != null) OnClicked?.Invoke(BoundItem);
            else if (BoundIndex >= 0) OnIndexClicked?.Invoke(BoundIndex);
        }

        private GameData ResolveGameData()
        {
            return GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
        }

        private void OnDestroy()
        {
            if (bg != null) bg.onClick.RemoveListener(HandleClick);
            OnClicked = null;
            OnIndexClicked = null;
        }
    }
}
