using StarterKit.Utilities;
using TMPro;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Lớp phủ toàn màn hình dùng để chặn thao tác trong lúc chờ (loading, chuyển màn).
    /// Dùng bộ đếm tham chiếu nên nhiều nơi có thể cùng gọi
    /// <see cref="ShowBlocker"/>/<see cref="HideBlocker"/> mà không giẫm chân nhau.
    /// </summary>
    public class UIOverlayHandler : Singleton<UIOverlayHandler>
    {
        /// <summary>Node lớp phủ; bật lên là chặn toàn bộ thao tác.</summary>
        [SerializeField] private GameObject blocker;

        /// <summary>Vòng xoay chờ (tuỳ chọn).</summary>
        [SerializeField] private RectTransform spinner;

        /// <summary>Ô chữ hiển thị nội dung đang chờ (tuỳ chọn).</summary>
        [SerializeField] private TextMeshProUGUI messageText;

        /// <summary>Tốc độ quay của spinner, độ mỗi giây.</summary>
        [SerializeField] private float spinnerSpeed = 180f;

        /// <summary>Số lần <see cref="ShowBlocker"/> đang giữ lớp phủ.</summary>
        public int BlockerCount { get; private set; }

        /// <summary>True khi lớp phủ đang hiển thị.</summary>
        public bool IsBlocking => BlockerCount > 0;

        protected override void OnAwake()
        {
            base.OnAwake();
            ApplyState();
        }

        private void Update()
        {
            if (!IsBlocking || spinner == null) return;
            spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
        }

        /// <summary>Bật lớp phủ chặn thao tác (cộng vào bộ đếm tham chiếu).</summary>
        /// <param name="message">Nội dung hiển thị kèm; có thể để trống.</param>
        public void ShowBlocker(string message = null)
        {
            BlockerCount++;

            if (messageText != null) messageText.text = message ?? string.Empty;
            ApplyState();
        }

        /// <summary>Trừ một lượt giữ lớp phủ; tắt khi bộ đếm về 0.</summary>
        public void HideBlocker()
        {
            BlockerCount = Mathf.Max(0, BlockerCount - 1);
            ApplyState();
        }

        /// <summary>Ép tắt lớp phủ và đưa bộ đếm về 0.</summary>
        public void ForceHideBlocker()
        {
            BlockerCount = 0;
            ApplyState();
        }

        private void ApplyState()
        {
            if (blocker != null) blocker.SetActive(IsBlocking);
        }
    }
}
