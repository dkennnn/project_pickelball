using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Co RectTransform vào vùng an toàn của màn hình (tránh tai thỏ / thanh điều hướng).
    /// Kết quả được cache nên chỉ tính lại khi <see cref="Screen.safeArea"/> hoặc độ phân giải đổi.
    /// <para>
    /// <b>CHỈ chạy trong Play mode.</b> Trước đây component này có <c>[ExecuteAlways]</c>, và đó là
    /// một cái bẫy: ở edit mode <see cref="Screen.width"/>/<see cref="Screen.height"/>/
    /// <see cref="Screen.safeArea"/> trả về kích thước của <b>view đang vẽ</b> — có thể là Scene view,
    /// Inspector hay Game view tuỳ thời điểm gọi. Kết quả là anchor bị tính theo kích thước sai và
    /// ghi thẳng vào RectTransform, đẩy toàn bộ nội dung ra ngoài khung nhìn. Triệu chứng điển hình:
    /// Scene view thấy UI bình thường nhưng Game view chỉ còn ảnh nền.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        /// <summary>Bỏ qua phần lề trái/phải của vùng an toàn.</summary>
        [SerializeField] private bool ignoreHorizontal;

        /// <summary>Bỏ qua phần lề trên/dưới của vùng an toàn.</summary>
        [SerializeField] private bool ignoreVertical;

        private RectTransform rectTransform;
        private Rect lastSafeArea = new Rect(0f, 0f, 0f, 0f);
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        // Offset do bố cục gốc quy định (lề thiết kế), KHÔNG phải do vùng an toàn.
        // Phải giữ nguyên: ví dụ MainMenuUI/Parent thụt 200px từ mép trên để TopPanel bên trong
        // vươn ngược lên đúng 200px đó mà chạm mép màn hình. Xoá offset này là thanh trên bay ra ngoài.
        private Vector2 designOffsetMin;
        private Vector2 designOffsetMax;
        private bool designOffsetCaptured;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CaptureDesignOffsets();
        }

        /// <summary>Ghi lại offset gốc của prefab, chỉ một lần và trước khi component sửa gì.</summary>
        private void CaptureDesignOffsets()
        {
            if (designOffsetCaptured || rectTransform == null) return;
            designOffsetMin = rectTransform.offsetMin;
            designOffsetMax = rectTransform.offsetMax;
            designOffsetCaptured = true;
        }

        private void OnEnable()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            CaptureDesignOffsets();
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        /// <summary>
        /// Trả RectTransform về trạng thái phủ kín cha. Dùng khi thoát Play mode hoặc khi công cụ
        /// editor cần một khung xác định, không phụ thuộc vùng an toàn của thiết bị.
        /// </summary>
        public void ResetToFullRect()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Áp lại vùng an toàn ngay lập tức, bỏ qua cache.</summary>
        public void ForceRefresh()
        {
            Refresh(true);
        }

        private void Refresh(bool force)
        {
            if (rectTransform == null) return;

            // Ngoài Play mode, Screen.* không đáng tin (xem ghi chú ở đầu class) — không đụng vào rect.
            if (!Application.isPlaying) return;

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            ScreenOrientation orientation = Screen.orientation;

            if (!force
                && safeArea == lastSafeArea
                && screenSize == lastScreenSize
                && orientation == lastOrientation)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastOrientation = orientation;

            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            min.x /= screenSize.x;
            min.y /= screenSize.y;
            max.x /= screenSize.x;
            max.y /= screenSize.y;

            if (ignoreHorizontal)
            {
                min.x = 0f;
                max.x = 1f;
            }

            if (ignoreVertical)
            {
                min.y = 0f;
                max.y = 1f;
            }

            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;

            // Giữ lề thiết kế của prefab. Đặt về 0 sẽ phá bố cục gốc — xem ghi chú ở phần khai báo.
            rectTransform.offsetMin = designOffsetMin;
            rectTransform.offsetMax = designOffsetMax;
        }
    }
}
