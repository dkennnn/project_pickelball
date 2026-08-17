using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Xoay đều quanh trục Z theo chiều kim đồng hồ.
    /// Dùng cho kim đồng hồ đếm ngược của Daily Challenge / Locker và các icon quay vòng.
    /// <para>
    /// Trong Unity trục Z dương quay ngược chiều kim đồng hồ, nên component tự đảo dấu:
    /// <see cref="degreesPerSecond"/> dương = quay theo chiều kim đồng hồ như tên gọi.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class RotateClockwise : MonoBehaviour
    {
        [Tooltip("Tốc độ xoay theo chiều kim đồng hồ, độ mỗi giây.")]
        [SerializeField] private float degreesPerSecond = 90f;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        [SerializeField] private bool enableOnStart = true;

        [Tooltip("Dùng unscaled time để kim vẫn chạy khi game đang pause.")]
        [SerializeField] private bool useUnscaledTime = true;

        private bool isRotating;

        /// <summary>Tốc độ xoay hiện tại (độ mỗi giây, chiều kim đồng hồ).</summary>
        public float DegreesPerSecond
        {
            get => degreesPerSecond;
            set => degreesPerSecond = value;
        }

        private void OnEnable()
        {
            isRotating = enableOnStart;
        }

        private void OnDisable()
        {
            isRotating = false;
        }

        private void Update()
        {
            if (!isRotating || Mathf.Approximately(degreesPerSecond, 0f)) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, -degreesPerSecond * dt, Space.Self);
        }

        /// <summary>Bật/tắt xoay.</summary>
        /// <param name="enabledRotation">True = xoay, false = đứng yên.</param>
        public void EnableRotation(bool enabledRotation)
        {
            isRotating = enabledRotation;
        }

        /// <summary>Dừng xoay (giữ nguyên góc hiện tại).</summary>
        public void DisableRotation()
        {
            isRotating = false;
        }

        /// <summary>Đưa góc xoay về 0 quanh trục Z.</summary>
        public void ResetRotation()
        {
            Vector3 e = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(e.x, e.y, 0f);
        }
    }
}
