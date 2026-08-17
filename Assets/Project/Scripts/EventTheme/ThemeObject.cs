using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Đánh dấu một node UI/scene có hai bộ mặt: bình thường và theo sự kiện (Halloween, Giáng sinh…).
    /// <para>
    /// Component tự đăng ký nghe <see cref="ThemeManager.OnThemeChanged"/> ở <c>OnEnable</c> và
    /// huỷ đăng ký ở <c>OnDisable</c>, nên bật/tắt bao nhiêu lần cũng không rò rỉ listener.
    /// </para>
    /// <para>
    /// Không có <see cref="ThemeManager"/> trong scene thì component vẫn chạy: nó dùng trạng thái
    /// mặc định (theme tắt) và không ném lỗi.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ThemeObject : MonoBehaviour
    {
        [Header("Theme Settings")]
        [Tooltip("Định danh theme mà node này thuộc về (ví dụ \"halloween\"). Để trống = áp dụng cho mọi theme.")]
        public string themeId = string.Empty;

        [Tooltip("Object hiện khi theme TẮT. Bỏ trống thì bỏ qua.")]
        public GameObject normalVisual;

        [Tooltip("Object hiện khi theme BẬT. Bỏ trống thì bỏ qua.")]
        public GameObject eventVisual;

        [Header("Sprite Swap (tuỳ chọn)")]
        [Tooltip("Image cần đổi sprite. Bỏ trống thì tự lấy Image trên GameObject này (nếu có).")]
        [SerializeField] private Image targetImage;

        [Tooltip("Sprite dùng khi theme TẮT. Bỏ trống thì giữ nguyên sprite hiện tại.")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Sprite dùng khi theme BẬT. Bỏ trống thì giữ nguyên sprite hiện tại.")]
        [SerializeField] private Sprite eventSprite;

        /// <summary>Trạng thái theme đã áp gần nhất.</summary>
        public bool CurrentThemeState { get; private set; }

        private bool subscribed;

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (normalSprite == null && targetImage != null) normalSprite = targetImage.sprite;
        }

        private void OnEnable()
        {
            Subscribe();
            ApplyTheme(ThemeManager.HasInstance && ThemeManager.Instance != null
                ? ThemeManager.Instance.isEventOngoing
                : false);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            ThemeManager.OnThemeChanged += OnThemeChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            ThemeManager.OnThemeChanged -= OnThemeChanged;
            subscribed = false;
        }

        private void OnThemeChanged(bool isEventOn)
        {
            ApplyTheme(isEventOn);
        }

        /// <summary>
        /// Áp trạng thái theme lên node: bật/tắt hai visual và đổi sprite nếu có cấu hình.
        /// </summary>
        /// <param name="isEventOn">True = sự kiện đang diễn ra (dùng bộ mặt event).</param>
        public void ApplyTheme(bool isEventOn)
        {
            CurrentThemeState = isEventOn;

            if (normalVisual != null) normalVisual.SetActive(!isEventOn);
            if (eventVisual != null) eventVisual.SetActive(isEventOn);

            if (targetImage == null) return;

            Sprite wanted = isEventOn ? eventSprite : normalSprite;
            if (wanted != null) targetImage.sprite = wanted;
        }
    }
}
