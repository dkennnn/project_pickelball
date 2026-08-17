using StarterKit.UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn cửa hàng theo đúng tên gốc <c>StarterKit.UI.IAPPurchaseUI</c>.
    /// <para>
    /// Toàn bộ logic ba khối Kits / Coins / Gems đã nằm ở <see cref="ShopUI"/> (viết trước,
    /// cùng đăng ký <see cref="ScreenType.IAPPurchase"/>), nên class này chỉ là lớp mỏng kế
    /// thừa để importer gắn được component đúng tên vào prefab <c>IAPPurchaseUI</c> mà không
    /// nhân đôi logic. Phần thêm ở đây chỉ là các widget riêng của layout gốc mà
    /// <see cref="ShopUI"/> không có: nút "No Ads" và <see cref="ScrollRect"/> của danh sách.
    /// </para>
    /// <para>
    /// Chỉ nên có MỘT trong hai component trên prefab: nếu cả hai cùng nằm trong scene,
    /// <see cref="UIController.Register"/> sẽ cảnh báo trùng <see cref="ScreenType.IAPPurchase"/>.
    /// </para>
    /// </summary>
    public class IAPPurchaseUI : ShopUI
    {
        /// <summary>Nút mở gói gỡ quảng cáo (node <c>Parent/NoAds</c>). Bản offline không có IAP nên chỉ báo toast.</summary>
        [SerializeField] private Button noAds;

        /// <summary>Vùng cuộn danh sách sản phẩm (node <c>Parent/Container/Scroll View</c>).</summary>
        [SerializeField] private ScrollRect scrollView;

        /// <inheritdoc/>
        public override void OnInit()
        {
            base.OnInit();

            if (noAds != null) noAds.onClick.AddListener(HandleNoAds);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            base.OnShow(data);

            // Mở màn luôn đưa danh sách về đầu cho khỏi giữ vị trí cuộn của lần trước.
            SetScroll(1f);
        }

        /// <summary>Đặt vị trí cuộn dọc của danh sách sản phẩm.</summary>
        /// <param name="value">1 là trên cùng, 0 là dưới cùng.</param>
        public void SetScroll(float value)
        {
            if (scrollView == null) return;
            scrollView.verticalNormalizedPosition = Mathf.Clamp01(value);
        }

        protected override void OnDestroy()
        {
            if (noAds != null) noAds.onClick.RemoveListener(HandleNoAds);
            base.OnDestroy();
        }

        private void HandleNoAds()
        {
            // Mua gỡ quảng cáo là IAP thật — ngoài phạm vi bản dựng lại.
            if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Purchases are not available in this build");
        }
    }
}
