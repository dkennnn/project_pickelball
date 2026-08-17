using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Cuộn <see cref="RawImage.uvRect"/> theo một trục để tạo băng chạy vô tận
    /// (dải sáng nền menu chính, băng chữ chạy…).
    /// <para>Khác <see cref="ParallaxUVScroller"/> ở chỗ chỉ lo đúng một RawImage, một trục.</para>
    /// <para>Không có <see cref="RawImage"/> thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class UVRectScroller : MonoBehaviour
    {
        /// <summary>Hướng cuộn.</summary>
        public enum ScrollDirection
        {
            /// <summary>Cuộn ngang (trục U).</summary>
            Horizontal = 0,

            /// <summary>Cuộn dọc (trục V).</summary>
            Vertical = 1
        }

        [Header("Scrolling Settings")]
        [Tooltip("Trục cuộn.")]
        public ScrollDirection scrollDirection = ScrollDirection.Horizontal;

        [Tooltip("Tốc độ cuộn, đơn vị UV mỗi giây. Số âm = cuộn ngược chiều.")]
        public float scrollSpeed = 0.1f;

        [Tooltip("Ảnh cần cuộn. Bỏ trống thì tự lấy RawImage trên GameObject này.")]
        [SerializeField] private RawImage rawImage;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool playOnEnable = true;

        [Tooltip("Dùng unscaled time để băng vẫn chạy khi game đang pause.")]
        public bool useUnscaledTime = true;

        private bool isScrolling;

        private void Awake()
        {
            if (rawImage == null) rawImage = GetComponent<RawImage>();
        }

        private void OnEnable()
        {
            if (playOnEnable) StartScrolling();
        }

        private void OnDisable()
        {
            isScrolling = false;
        }

        /// <summary>Bắt đầu cuộn. No-op nếu không có RawImage.</summary>
        public void StartScrolling()
        {
            if (rawImage == null) rawImage = GetComponent<RawImage>();
            isScrolling = rawImage != null;
        }

        /// <summary>Dừng cuộn (giữ nguyên uvRect hiện tại).</summary>
        public void StopScrolling()
        {
            isScrolling = false;
        }

        /// <summary>Đưa uvRect về offset 0.</summary>
        public void ResetScroll()
        {
            if (rawImage == null) return;
            Rect r = rawImage.uvRect;
            r.x = 0f;
            r.y = 0f;
            rawImage.uvRect = r;
        }

        private void Update()
        {
            if (!isScrolling || rawImage == null) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float delta = scrollSpeed * dt;

            Rect r = rawImage.uvRect;
            if (scrollDirection == ScrollDirection.Horizontal) r.x = Mathf.Repeat(r.x + delta, 1f);
            else r.y = Mathf.Repeat(r.y + delta, 1f);

            rawImage.uvRect = r;
        }
    }
}
