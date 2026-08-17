using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Lớp phủ hướng dẫn: bảng thông điệp, mũi tên chỉ, vùng tối che phần không tương tác được
    /// và nút Skip.
    /// <para>
    /// Bản gốc KHÔNG có entry riêng trong <see cref="ScreenType"/> cho màn này (lớp phủ luôn nằm
    /// trên cùng, không tham gia ngăn xếp UI), nên <see cref="DefaultScreenType"/> để
    /// <see cref="ScreenType.None"/>. Vì thế <see cref="UIController"/> không mở/đóng màn này —
    /// các state gọi thẳng <see cref="ShowMessage"/> / <see cref="HideMessage"/>.
    /// </para>
    /// </summary>
    public class TutorialUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.None;

        /// <inheritdoc/>
        public override bool IsPopup => true;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <summary>Bảng chứa thông điệp hướng dẫn.</summary>
        [Header("Message")]
        [SerializeField] private GameObject messagePanel;

        /// <summary>Chữ hướng dẫn.</summary>
        [SerializeField] private TextMeshProUGUI messageText;

        /// <summary>Mũi tên chỉ vào mục tiêu đang được highlight.</summary>
        [Header("Highlight")]
        [SerializeField] private RectTransform pointerArrow;

        /// <summary>Khung sáng bám theo mục tiêu.</summary>
        [SerializeField] private RectTransform highlightFrame;

        /// <summary>Vùng tối che phần màn hình không tương tác được.</summary>
        [SerializeField] private Image dimOverlay;

        /// <summary>Độ mờ của vùng tối khi đang chặn tương tác.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float dimAlpha = 0.65f;

        /// <summary>Khoảng đệm giữa khung sáng và mục tiêu, tính bằng pixel.</summary>
        [SerializeField] private Vector2 highlightPadding = new Vector2(16f, 16f);

        /// <summary>Khoảng cách mũi tên đặt phía trên mục tiêu, tính bằng pixel.</summary>
        [SerializeField] private float arrowOffset = 90f;

        /// <summary>Nút bỏ qua toàn bộ hướng dẫn.</summary>
        [Header("Skip")]
        [SerializeField] private Button skipButton;

        /// <summary>Mục tiêu đang được highlight; null nếu không có.</summary>
        public RectTransform CurrentHighlight { get; private set; }

        /// <summary>True khi bảng thông điệp đang hiện.</summary>
        public bool IsMessageVisible { get; private set; }

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (skipButton != null) skipButton.onClick.AddListener(HandleSkip);

            HideMessage();
            ClearHighlight();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            if (skipButton != null) skipButton.onClick.RemoveListener(HandleSkip);
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if (CurrentHighlight == null) return;

            // Mục tiêu có thể bị huỷ giữa chừng (đổi màn hình) → dọn khung sáng.
            if (!CurrentHighlight.gameObject.activeInHierarchy)
            {
                ClearHighlight();
                return;
            }

            FollowHighlight();
        }

        // ------------------------------------------------------------------
        // API cho các bước hướng dẫn
        // ------------------------------------------------------------------

        /// <summary>Hiện bảng thông điệp với nội dung cho trước.</summary>
        /// <param name="message">Nội dung cần hiện; rỗng thì ẩn bảng.</param>
        public void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                HideMessage();
                return;
            }

            if (messageText != null) messageText.text = message;
            if (messagePanel != null) messagePanel.SetActive(true);

            IsMessageVisible = true;
        }

        /// <summary>Ẩn bảng thông điệp.</summary>
        public void HideMessage()
        {
            if (messagePanel != null) messagePanel.SetActive(false);
            IsMessageVisible = false;
        }

        /// <summary>
        /// Bật khung sáng + mũi tên chỉ vào một mục tiêu UI, đồng thời bật vùng tối.
        /// </summary>
        /// <param name="target">Mục tiêu cần làm nổi bật; null thì gọi <see cref="ClearHighlight"/>.</param>
        public void SetHighlight(RectTransform target)
        {
            if (target == null)
            {
                ClearHighlight();
                return;
            }

            CurrentHighlight = target;

            if (highlightFrame != null) highlightFrame.gameObject.SetActive(true);
            if (pointerArrow != null) pointerArrow.gameObject.SetActive(true);
            SetDim(true);

            FollowHighlight();
        }

        /// <summary>Tắt khung sáng, mũi tên và vùng tối.</summary>
        public void ClearHighlight()
        {
            CurrentHighlight = null;

            if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
            if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);
            SetDim(false);
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Đưa khung sáng và mũi tên về đúng vị trí/kích thước của mục tiêu.</summary>
        private void FollowHighlight()
        {
            RectTransform target = CurrentHighlight;
            if (target == null) return;

            Vector2 size = target.rect.size + highlightPadding;

            if (highlightFrame != null)
            {
                highlightFrame.position = target.position;
                highlightFrame.sizeDelta = size;
            }

            if (pointerArrow != null)
            {
                Vector3 pos = target.position;
                pos.y += (size.y * 0.5f) + arrowOffset;
                pointerArrow.position = pos;
            }
        }

        /// <summary>Bật/tắt vùng tối che tương tác.</summary>
        /// <param name="visible">True để che.</param>
        private void SetDim(bool visible)
        {
            if (dimOverlay == null) return;

            Color c = dimOverlay.color;
            c.a = visible ? dimAlpha : 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = visible;
        }

        /// <summary>Nút Skip: bỏ qua toàn bộ hướng dẫn.</summary>
        private void HandleSkip()
        {
            if (!TutorialManager.HasInstance) return;
            TutorialManager.Instance.SkipAll();
        }
    }
}
