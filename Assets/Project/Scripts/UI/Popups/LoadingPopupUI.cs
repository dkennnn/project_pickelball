using TMPro;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Popup "đang tải" phủ toàn màn: một vòng xoay và một dòng chữ trạng thái.
    /// Chặn phím Back để người chơi không thoát giữa chừng.
    /// </summary>
    public class LoadingPopupUI : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.LoadingPopup;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <summary>Ảnh vòng xoay (node <c>Parent/LoaderImg</c>).</summary>
        [SerializeField] private RectTransform loaderImg;

        /// <summary>Dòng chữ trạng thái (node <c>Parent/LoadingTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI loadingTxt;

        /// <summary>Tốc độ quay của vòng xoay, tính bằng độ/giây (âm là quay theo chiều kim đồng hồ).</summary>
        [SerializeField] private float rotateSpeed = -180f;

        /// <summary>Chữ hiển thị khi bên gọi không truyền thông điệp nào.</summary>
        [SerializeField] private string defaultMessage = "LOADING...";

        /// <summary>Thông điệp đang hiển thị.</summary>
        public string Message { get; private set; }

        /// <inheritdoc/>
        /// <param name="data">Chuỗi thông điệp tuỳ chọn.</param>
        public override void OnShow(object data)
        {
            SetData(data as string);
        }

        /// <summary>
        /// Hiện popup kèm thông điệp. Nạp chữ trước rồi mới bật canvas nên không nháy chữ cũ.
        /// </summary>
        /// <param name="message">Thông điệp hiển thị; null sẽ dùng chữ mặc định.</param>
        public void Show(string message)
        {
            SetData(message);
            Show((object)message);
        }

        /// <summary>Đổi thông điệp mà không đụng tới trạng thái hiện/ẩn.</summary>
        /// <param name="message">Thông điệp hiển thị; null sẽ dùng chữ mặc định.</param>
        public void SetData(string message)
        {
            Message = string.IsNullOrEmpty(message) ? defaultMessage : message;
            if (loadingTxt != null) loadingTxt.text = Message;
        }

        private void Update()
        {
            if (!IsVisible || loaderImg == null) return;

            loaderImg.Rotate(0f, 0f, rotateSpeed * Time.unscaledDeltaTime);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            if (loaderImg != null) loaderImg.localRotation = Quaternion.identity;
        }
    }
}
