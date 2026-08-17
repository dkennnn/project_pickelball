using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Co RectTransform vào vùng an toàn của màn hình (tránh tai thỏ / thanh điều hướng).
    /// Kết quả được cache nên chỉ tính lại khi <see cref="Screen.safeArea"/> hoặc độ phân giải đổi.
    /// </summary>
    [ExecuteAlways]
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

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        /// <summary>Áp lại vùng an toàn ngay lập tức, bỏ qua cache.</summary>
        public void ForceRefresh()
        {
            Refresh(true);
        }

        private void Refresh(bool force)
        {
            if (rectTransform == null) return;

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
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
