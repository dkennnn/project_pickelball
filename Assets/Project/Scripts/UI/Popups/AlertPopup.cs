using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thông điệp truyền vào <see cref="AlertPopup"/> qua tham số <c>data</c> của
    /// <see cref="StarterKit.UIKit.UIController.Show"/>.
    /// <para>
    /// Bản gốc dùng khoá localization (<c>SetupPopup(titleKey, descriptionKey, ...)</c>);
    /// dựng lại không có bảng localization nên ở đây truyền thẳng chuỗi đã dịch sẵn.
    /// </para>
    /// </summary>
    [Serializable]
    public class AlertData
    {
        /// <summary>Tiêu đề popup.</summary>
        public string title;

        /// <summary>Nội dung thông báo.</summary>
        public string message;

        /// <summary>Chữ trên nút đóng; để trống sẽ dùng "OK".</summary>
        public string buttonText;

        /// <summary>Gọi lại sau khi người chơi bấm nút đóng; có thể null.</summary>
        public Action onClose;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public AlertData() { }

        /// <summary>Khởi tạo thông điệp cảnh báo.</summary>
        /// <param name="title">Tiêu đề popup.</param>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="buttonText">Chữ trên nút đóng; để trống sẽ dùng "OK".</param>
        /// <param name="onClose">Gọi lại sau khi bấm nút đóng.</param>
        public AlertData(string title, string message, string buttonText = null, Action onClose = null)
        {
            this.title = title;
            this.message = message;
            this.buttonText = buttonText;
            this.onClose = onClose;
        }
    }

    /// <summary>
    /// Popup cảnh báo một nút: tiêu đề, nội dung và nút OK.
    /// Nhận <see cref="AlertData"/> (hoặc một chuỗi thuần) qua <see cref="OnShow"/>.
    /// </summary>
    public class AlertPopup : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.AlertPopup;

        /// <summary>Ô chữ tiêu đề (node <c>Popup/Title/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>Ô chữ nội dung (node <c>Popup/PopupContent/Text</c>).</summary>
        [SerializeField] private TextMeshProUGUI text;

        /// <summary>Nút đóng popup (node <c>Popup/PopupBtn</c>).</summary>
        [SerializeField] private Button popupBtn;

        /// <summary>Chữ trên nút đóng (node <c>Popup/PopupBtn/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI popupBtnText;

        /// <summary>Chữ mặc định trên nút đóng khi dữ liệu không chỉ định.</summary>
        [SerializeField] private string defaultButtonText = "OK";

        /// <summary>Callback đang gắn cho nút đóng; đặt lại mỗi lần popup hiện.</summary>
        public Action okButtonAction;

        private bool wired;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Wire();
        }

        /// <inheritdoc/>
        /// <param name="data">
        /// <see cref="AlertData"/>, hoặc một <see cref="string"/> chỉ chứa nội dung thông báo.
        /// </param>
        public override void OnShow(object data)
        {
            Wire();

            if (data is AlertData alert) Apply(alert);
            else if (data is string message) Apply(new AlertData(null, message));
            else Apply(null);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            // Không giữ callback qua các lần mở khác nhau để tránh gọi nhầm chủ cũ.
            okButtonAction = null;
        }

        /// <summary>
        /// Nạp thẳng nội dung cho popup mà không đi qua <see cref="StarterKit.UIKit.UIController"/>.
        /// </summary>
        /// <param name="title">Tiêu đề popup.</param>
        /// <param name="description">Nội dung thông báo.</param>
        /// <param name="okButtonMessage">Chữ trên nút đóng.</param>
        /// <param name="onClose">Gọi lại sau khi bấm nút đóng.</param>
        public void SetupPopup(string title, string description, string okButtonMessage = null, Action onClose = null)
        {
            Wire();
            Apply(new AlertData(title, description, okButtonMessage, onClose));
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleOk();
        }

        protected override void OnDestroy()
        {
            if (popupBtn != null) popupBtn.onClick.RemoveListener(HandleOk);
            okButtonAction = null;

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void Apply(AlertData data)
        {
            okButtonAction = data != null ? data.onClose : null;

            if (titleText != null) titleText.text = data != null && !string.IsNullOrEmpty(data.title) ? data.title : "ALERT";
            if (text != null) text.text = data != null && data.message != null ? data.message : string.Empty;

            if (popupBtnText != null)
            {
                popupBtnText.text = data != null && !string.IsNullOrEmpty(data.buttonText)
                    ? data.buttonText
                    : defaultButtonText;
            }
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (popupBtn != null) popupBtn.onClick.AddListener(HandleOk);
        }

        private void HandleOk()
        {
            Action callback = okButtonAction;
            okButtonAction = null;

            if (StarterKit.UIKit.UIController.HasInstance) StarterKit.UIKit.UIController.Instance.Hide(screenType);
            else Hide();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}
