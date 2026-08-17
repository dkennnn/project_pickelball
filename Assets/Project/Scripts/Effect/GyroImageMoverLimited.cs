using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Dịch một ảnh UI theo con quay hồi chuyển (gyroscope) của thiết bị, trong biên
    /// <see cref="maxOffset"/> quanh vị trí gốc — tạo cảm giác chiều sâu cho background menu chính.
    /// <para>
    /// Thiết bị không có gyro (<see cref="SystemInfo.supportsGyroscope"/> = false, ví dụ Editor
    /// hay máy tính) thì ảnh **đứng yên** ở vị trí gốc và component không ném lỗi.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class GyroImageMoverLimited : MonoBehaviour
    {
        [Tooltip("Biên dịch chuyển tối đa quanh vị trí gốc, tính bằng pixel UI (x = ngang, y = dọc).")]
        public Vector2 maxOffset = new Vector2(40f, 25f);

        [Tooltip("Độ nhạy: nhân với giá trị trọng lực đọc từ gyro trước khi clamp.")]
        public float sensitivity = 1f;

        [Tooltip("Tốc độ bám theo vị trí đích. Số càng lớn càng bám sát, càng nhỏ càng mượt.")]
        public float followSpeed = 6f;

        [Tooltip("Đảo chiều dịch chuyển (ảnh chạy ngược hướng nghiêng máy).")]
        public bool invert = true;

        [Tooltip("RectTransform cần dịch. Bỏ trống thì dùng RectTransform của GameObject này.")]
        [SerializeField] private RectTransform rectTransform;

        /// <summary>Vị trí neo gốc, chụp lại lúc Awake.</summary>
        private Vector2 basePosition;

        /// <summary>True khi thiết bị thật sự có gyro và đã bật thành công.</summary>
        private bool gyroAvailable;

        private bool baseCaptured;

        /// <summary>Thiết bị hiện có hỗ trợ gyroscope hay không.</summary>
        public bool IsGyroAvailable => gyroAvailable;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                basePosition = rectTransform.anchoredPosition;
                baseCaptured = true;
            }

            EnableGyro();
        }

        private void OnEnable()
        {
            EnableGyro();
        }

        private void OnDisable()
        {
            // Trả về vị trí gốc để prefab không bị lệch khi bật lại.
            if (baseCaptured && rectTransform != null) rectTransform.anchoredPosition = basePosition;
        }

        private void EnableGyro()
        {
            if (!SystemInfo.supportsGyroscope)
            {
                gyroAvailable = false;
                return;
            }

            // Bật gyro chỉ khi thiết bị hỗ trợ — truy cập Input.gyro trên máy không hỗ trợ sẽ log cảnh báo.
            Input.gyro.enabled = true;
            gyroAvailable = true;
        }

        private void Update()
        {
            if (!gyroAvailable || rectTransform == null || !baseCaptured) return;

            // gravity nằm trong [-1,1] theo từng trục, đủ để suy ra độ nghiêng máy.
            Vector3 gravity = Input.gyro.gravity;

            float sign = invert ? -1f : 1f;
            float x = Mathf.Clamp(gravity.x * sensitivity, -1f, 1f) * maxOffset.x * sign;
            float y = Mathf.Clamp(gravity.y * sensitivity, -1f, 1f) * maxOffset.y * sign;

            Vector2 target = basePosition + new Vector2(x, y);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSpeed) * Time.deltaTime);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, t);
        }

        /// <summary>Đưa ảnh về vị trí gốc ngay lập tức.</summary>
        public void ResetPosition()
        {
            if (baseCaptured && rectTransform != null) rectTransform.anchoredPosition = basePosition;
        }
    }
}
