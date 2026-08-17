using System;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thông điệp truyền vào <see cref="ConfirmationPopup"/> qua tham số <c>data</c> của
    /// <see cref="UIController.Show"/>.
    /// </summary>
    [Serializable]
    public class ConfirmData
    {
        /// <summary>Tiêu đề popup.</summary>
        public string title;

        /// <summary>Câu hỏi xác nhận.</summary>
        public string message;

        /// <summary>Chữ trên nút đồng ý; để trống sẽ dùng "YES".</summary>
        public string confirmText;

        /// <summary>Chữ trên nút từ chối; để trống sẽ dùng "NO".</summary>
        public string cancelText;

        /// <summary>Gọi lại khi người chơi bấm đồng ý; có thể null.</summary>
        public Action onConfirm;

        /// <summary>Gọi lại khi người chơi bấm từ chối hoặc bấm Back; có thể null.</summary>
        public Action onCancel;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public ConfirmData() { }

        /// <summary>Khởi tạo thông điệp xác nhận.</summary>
        /// <param name="title">Tiêu đề popup.</param>
        /// <param name="message">Câu hỏi xác nhận.</param>
        /// <param name="onConfirm">Gọi lại khi bấm đồng ý.</param>
        /// <param name="onCancel">Gọi lại khi bấm từ chối.</param>
        /// <param name="confirmText">Chữ trên nút đồng ý.</param>
        /// <param name="cancelText">Chữ trên nút từ chối.</param>
        public ConfirmData(
            string title,
            string message,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmText = null,
            string cancelText = null)
        {
            this.title = title;
            this.message = message;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            this.confirmText = confirmText;
            this.cancelText = cancelText;
        }
    }

    /// <summary>
    /// Popup hai nút Yes/No. Nhận <see cref="ConfirmData"/> qua <see cref="OnShow"/>;
    /// bấm Back tương đương bấm No.
    /// </summary>
    public class ConfirmationPopup : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.ConfirmationPopup;

        /// <summary>Ô chữ tiêu đề (node <c>Popup/Title/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>Ô chữ nội dung (node <c>Popup/PopupContent/Text</c>).</summary>
        [SerializeField] private TextMeshProUGUI text;

        /// <summary>Nút đồng ý (node <c>Popup/YesButton</c>).</summary>
        [SerializeField] private Button yesButton;

        /// <summary>Chữ trên nút đồng ý (node <c>Popup/YesButton/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI yesButtonText;

        /// <summary>Nút từ chối (node <c>Popup/NoButton</c>).</summary>
        [SerializeField] private Button noButton;

        /// <summary>Chữ trên nút từ chối (node <c>Popup/NoButton/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI noButtonText;

        /// <summary>Chữ mặc định trên nút đồng ý.</summary>
        [SerializeField] private string defaultConfirmText = "YES";

        /// <summary>Chữ mặc định trên nút từ chối.</summary>
        [SerializeField] private string defaultCancelText = "NO";

        /// <summary>Callback đang gắn cho nút đồng ý.</summary>
        public Action okButtonAction;

        /// <summary>Callback đang gắn cho nút từ chối.</summary>
        public Action cancelButtonAction;

        private bool wired;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Wire();
        }

        /// <inheritdoc/>
        /// <param name="data"><see cref="ConfirmData"/>, hoặc một <see cref="string"/> chỉ chứa câu hỏi.</param>
        public override void OnShow(object data)
        {
            Wire();

            if (data is ConfirmData confirm) Apply(confirm);
            else if (data is string message) Apply(new ConfirmData(null, message));
            else Apply(null);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            okButtonAction = null;
            cancelButtonAction = null;
        }

        /// <summary>Nạp thẳng nội dung cho popup mà không đi qua <see cref="UIController"/>.</summary>
        /// <param name="title">Tiêu đề popup.</param>
        /// <param name="description">Câu hỏi xác nhận.</param>
        /// <param name="okButtonMessage">Chữ trên nút đồng ý.</param>
        /// <param name="cancelButtonMessage">Chữ trên nút từ chối.</param>
        /// <param name="onConfirm">Gọi lại khi bấm đồng ý.</param>
        /// <param name="onCancel">Gọi lại khi bấm từ chối.</param>
        public void SetupPopup(
            string title,
            string description,
            string okButtonMessage,
            string cancelButtonMessage,
            Action onConfirm,
            Action onCancel)
        {
            Wire();
            Apply(new ConfirmData(title, description, onConfirm, onCancel, okButtonMessage, cancelButtonMessage));
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleCancel();
        }

        protected override void OnDestroy()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(HandleConfirm);
            if (noButton != null) noButton.onClick.RemoveListener(HandleCancel);

            okButtonAction = null;
            cancelButtonAction = null;

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void Apply(ConfirmData data)
        {
            okButtonAction = data != null ? data.onConfirm : null;
            cancelButtonAction = data != null ? data.onCancel : null;

            if (titleText != null) titleText.text = data != null && !string.IsNullOrEmpty(data.title) ? data.title : "CONFIRM";
            if (text != null) text.text = data != null && data.message != null ? data.message : string.Empty;

            if (yesButtonText != null)
            {
                yesButtonText.text = data != null && !string.IsNullOrEmpty(data.confirmText)
                    ? data.confirmText
                    : defaultConfirmText;
            }

            if (noButtonText != null)
            {
                noButtonText.text = data != null && !string.IsNullOrEmpty(data.cancelText)
                    ? data.cancelText
                    : defaultCancelText;
            }
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (yesButton != null) yesButton.onClick.AddListener(HandleConfirm);
            if (noButton != null) noButton.onClick.AddListener(HandleCancel);
        }

        private void HandleConfirm()
        {
            Close(okButtonAction);
        }

        private void HandleCancel()
        {
            Close(cancelButtonAction);
        }

        private void Close(Action callback)
        {
            okButtonAction = null;
            cancelButtonAction = null;

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}
