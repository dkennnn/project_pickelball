using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn chi tiết một vật phẩm: dùng lại <see cref="ItemCardUI"/> ở khổ lớn.
    /// Vật phẩm được truyền qua tham số <c>data</c> của <see cref="UIScreenBase.OnShow"/>.
    /// </summary>
    public class ShopItemDetailUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.ShopItemDetailUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn (tên vật phẩm).</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Thẻ vật phẩm khổ lớn.</summary>
        [SerializeField] private ItemCardUI itemCard;

        /// <summary>Vật phẩm đang hiển thị.</summary>
        public Item CurrentItem { get; private set; }

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);

            if (itemCard != null)
            {
                itemCard.OnUpgraded += HandleCardChanged;
                itemCard.OnPurchased += HandleCardChanged;
                itemCard.OnEquipped += HandleCardChanged;
            }
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            if (data is Item item) CurrentItem = item;
            Refresh();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            if (itemCard != null)
            {
                itemCard.OnUpgraded -= HandleCardChanged;
                itemCard.OnPurchased -= HandleCardChanged;
                itemCard.OnEquipped -= HandleCardChanged;
            }

            base.OnDestroy();
        }

        /// <summary>Vẽ lại thẻ vật phẩm theo dữ liệu mới nhất.</summary>
        public void Refresh()
        {
            if (title != null && CurrentItem != null)
            {
                title.text = string.IsNullOrEmpty(CurrentItem.itemName)
                    ? CurrentItem.itemType.ToString().ToUpperInvariant()
                    : CurrentItem.itemName.ToUpperInvariant();
            }

            if (itemCard != null) itemCard.Bind(CurrentItem);
        }

        private void HandleCardChanged(Item item)
        {
            Refresh();
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.CategoryDisplayUI, CurrentItem);
            else Hide();
        }
    }
}
