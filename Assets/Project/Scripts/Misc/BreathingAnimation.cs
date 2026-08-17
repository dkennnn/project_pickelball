using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Scale "thở" nhẹ theo hàm sin — dùng cho chấm đỏ báo notification, icon cần chú ý.
    /// <para>Không cần reference nào; chỉ tác động lên transform của chính GameObject.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class BreathingAnimation : MonoBehaviour
    {
        [Tooltip("Tỉ lệ phóng to đỉnh so với scale gốc. 1.1 nghĩa là to nhất bằng 110% scale gốc.")]
        public float scaleFactor = 1.1f;

        [Tooltip("Thời lượng một nhịp thở trọn vẹn (phình ra rồi thu về), tính bằng giây.")]
        public float duration = 1.2f;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool playOnEnable = true;

        [Tooltip("Dùng unscaled time để vẫn thở khi game đang pause.")]
        public bool useUnscaledTime = true;

        private Vector3 originalScale = Vector3.one;
        private bool isBreathing;
        private float elapsed;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
        }

        private void OnEnable()
        {
            if (playOnEnable) StartBreathing();
        }

        private void OnDisable()
        {
            StopBreathing();
        }

        /// <summary>Bắt đầu nhịp thở từ scale gốc.</summary>
        public void StartBreathing()
        {
            elapsed = 0f;
            isBreathing = true;
        }

        /// <summary>Dừng nhịp thở và trả về scale gốc.</summary>
        public void StopBreathing()
        {
            isBreathing = false;
            transform.localScale = originalScale;
        }

        private void Update()
        {
            if (!isBreathing) return;

            float period = Mathf.Max(0.05f, duration);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // sin đi từ 0 → 1 → 0 trong đúng một chu kỳ `duration`.
            float t = (Mathf.Sin((elapsed / period) * Mathf.PI * 2f - Mathf.PI * 0.5f) + 1f) * 0.5f;
            float factor = Mathf.Lerp(1f, scaleFactor, t);
            transform.localScale = originalScale * factor;
        }
    }
}
