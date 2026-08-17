using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Vừa xoay liên tục quanh trục Z vừa nhấp nhô scale theo sin.
    /// Dùng cho vòng hào quang sau vật phẩm hiếm, ngôi sao trang trí, badge khuyến mãi…
    /// <para>Không cần reference nào; chỉ tác động lên transform của chính GameObject.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class RotateAndScaleEffect : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Tốc độ xoay, độ mỗi giây. Số âm = xoay ngược chiều kim đồng hồ.")]
        public float rotateSpeed = 45f;

        [Tooltip("Trục xoay (mặc định Z — phù hợp cho UI 2D).")]
        public Vector3 rotationAxis = Vector3.forward;

        [Header("Scale Settings")]
        [Tooltip("Biên độ nhấp nhô scale. 0.1 nghĩa là dao động ±10% quanh scale gốc.")]
        public float scaleAmount = 0.1f;

        [Tooltip("Tốc độ nhấp nhô scale (số chu kỳ mỗi giây nhân 2π).")]
        public float scaleSpeed = 2f;

        [Tooltip("Dùng unscaled time để hiệu ứng vẫn chạy khi game đang pause.")]
        public bool useUnscaledTime = true;

        private Vector3 originalScale = Vector3.one;
        private bool isPlaying = true;
        private float elapsed;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
        }

        private void OnDisable()
        {
            // Trả scale về gốc để prefab không bị "đóng băng" ở một scale lẻ.
            transform.localScale = originalScale;
        }

        private void Update()
        {
            if (!isPlaying) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            if (rotationAxis != Vector3.zero && !Mathf.Approximately(rotateSpeed, 0f))
                transform.Rotate(rotationAxis.normalized, rotateSpeed * dt, Space.Self);

            if (!Mathf.Approximately(scaleAmount, 0f))
            {
                float factor = 1f + Mathf.Sin(elapsed * scaleSpeed) * scaleAmount;
                transform.localScale = originalScale * factor;
            }
        }

        /// <summary>Chạy lại hiệu ứng từ đầu.</summary>
        [ContextMenu("Start")]
        public void StartAnimation()
        {
            elapsed = 0f;
            isPlaying = true;
        }

        /// <summary>Dừng hiệu ứng và trả scale về gốc.</summary>
        [ContextMenu("Stop")]
        public void StopAnimation()
        {
            isPlaying = false;
            transform.localScale = originalScale;
        }

        /// <summary>Bật/tắt hiệu ứng bằng một lời gọi (tiện gắn vào UnityEvent).</summary>
        /// <param name="enableAnim">True = chạy, false = dừng.</param>
        public void ToggleAnimation(bool enableAnim)
        {
            if (enableAnim) StartAnimation();
            else StopAnimation();
        }
    }
}
