using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Xoay một transform đều theo <see cref="rotationPerSecond"/> (độ mỗi giây trên từng trục).
    /// Dùng cho vật thể 3D trang trí: cúp xoay tròn, vật phẩm trong tủ đồ, biển quảng cáo.
    /// <para>Không cần reference nào.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class Rotater : MonoBehaviour
    {
        [Tooltip("Tốc độ xoay theo từng trục, độ mỗi giây.")]
        public Vector3 rotationPerSecond = new Vector3(0f, 45f, 0f);

        [Tooltip("Transform cần xoay. Bỏ trống thì dùng transform của GameObject này.")]
        public Transform targetTransform;

        [Tooltip("Xoay trong không gian local (mặc định) hay world.")]
        public bool useLocalSpace = true;

        [Tooltip("Tự xoay ngay khi component được bật.")]
        public bool playOnEnable = true;

        [Tooltip("Dùng unscaled time để vẫn xoay khi game đang pause.")]
        public bool useUnscaledTime;

        private bool isRotating;

        private void Awake()
        {
            if (targetTransform == null) targetTransform = transform;
        }

        private void OnEnable()
        {
            isRotating = playOnEnable;
        }

        private void OnDisable()
        {
            isRotating = false;
        }

        /// <summary>Bắt đầu xoay.</summary>
        public void StartRotating()
        {
            isRotating = true;
        }

        /// <summary>Dừng xoay (giữ nguyên góc hiện tại).</summary>
        public void StopRotating()
        {
            isRotating = false;
        }

        private void Update()
        {
            if (!isRotating || targetTransform == null) return;
            if (rotationPerSecond == Vector3.zero) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            targetTransform.Rotate(rotationPerSecond * dt, useLocalSpace ? Space.Self : Space.World);
        }
    }
}
