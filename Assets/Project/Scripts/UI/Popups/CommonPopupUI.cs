using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Popup dùng chung, không có nội dung riêng — chỉ là khung nền + nút đóng.
    /// <para>
    /// Bản gốc gắn thẳng class <c>PopupUI</c> lên prefab "CommonPopupUI". Ở đây
    /// <see cref="PopupUI"/> là <c>abstract</c> (đúng vai trò lớp cơ sở), mà
    /// <c>UILayoutImporter</c> bỏ qua mọi type abstract, nên prefab đó sẽ thiếu component.
    /// Class cụ thể này lấp chỗ trống đó mà không phải bỏ <c>abstract</c> ở lớp cơ sở.
    /// </para>
    /// </summary>
    public class CommonPopupUI : PopupUI
    {
        /// <inheritdoc />
        public override ScreenType DefaultScreenType => ScreenType.AlertPopup;

        /// <summary>Nội dung tuỳ biến do màn khác nhét vào lúc mở.</summary>
        [SerializeField] private RectTransform contentRoot;

        /// <summary>Trả về chỗ để gắn nội dung động; có thể null nếu prefab chưa gán.</summary>
        public RectTransform ContentRoot => contentRoot;
    }
}
