using System;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Popup báo mất kết nối. Bản dựng lại KHÔNG dò mạng thật (ngoài phạm vi) — popup chỉ
    /// hiển thị thông báo và gọi callback tương ứng cho nút Retry / Quit; bên gọi tự quyết
    /// định thử lại như thế nào.
    /// <para>
    /// Layout gốc <c>NoInternetPopup.json</c> chỉ có tiêu đề, icon và nội dung; hai nút
    /// bên dưới để trống trên prefab và chỉ hoạt động khi được gán tay.
    /// </para>
    /// </summary>
    public class NoInternetPopup : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.NoInternetPopup;

        /// <summary>Ô chữ tiêu đề (node <c>Popup/Title/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>Ô chữ nội dung (node <c>Popup/PopupContent/Text</c>).</summary>
        [SerializeField] private TextMeshProUGUI text;

        /// <summary>Biểu tượng wifi (node <c>Popup/PopupContent/Icon</c>).</summary>
        [SerializeField] private Image icon;

        /// <summary>Nút thử lại; layout gốc không có, phải gán tay nếu cần.</summary>
        [SerializeField] private Button retryButton;

        /// <summary>Nút thoát; layout gốc không có, phải gán tay nếu cần.</summary>
        [SerializeField] private Button quitButton;

        /// <summary>Tiêu đề mặc định.</summary>
        [SerializeField] private string defaultTitle = "NO INTERNET";

        /// <summary>Nội dung mặc định.</summary>
        [SerializeField] private string defaultMessage = "Please check your connection and try again.";

        /// <summary>Gọi lại khi bấm Retry; có thể null.</summary>
        public Action retryAction;

        /// <summary>Gọi lại khi bấm Quit; có thể null.</summary>
        public Action quitAction;

        private bool wired;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Wire();
        }

        /// <inheritdoc/>
        /// <param name="data">Chuỗi nội dung tuỳ chọn; null sẽ dùng nội dung mặc định.</param>
        public override void OnShow(object data)
        {
            Wire();

            if (titleText != null) titleText.text = defaultTitle;
            if (text != null) text.text = data as string ?? defaultMessage;
            if (icon != null) icon.enabled = icon.sprite != null;
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            retryAction = null;
            quitAction = null;
        }

        /// <summary>Nạp callback cho hai nút.</summary>
        /// <param name="message">Nội dung hiển thị; null sẽ dùng nội dung mặc định.</param>
        /// <param name="onRetry">Gọi lại khi bấm Retry.</param>
        /// <param name="onQuit">Gọi lại khi bấm Quit.</param>
        public void SetupPopup(string message, Action onRetry, Action onQuit = null)
        {
            Wire();

            if (titleText != null) titleText.text = defaultTitle;
            if (text != null) text.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

            retryAction = onRetry;
            quitAction = onQuit;
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            // Popup mất mạng không cho thoát bằng Back nếu chưa có nút Quit được gán.
            if (quitButton == null && quitAction == null) return;
            HandleQuit();
        }

        protected override void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);

            retryAction = null;
            quitAction = null;

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (retryButton != null) retryButton.onClick.AddListener(HandleRetry);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
        }

        private void HandleRetry()
        {
            Action callback = retryAction;

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void HandleQuit()
        {
            Action callback = quitAction;

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}
