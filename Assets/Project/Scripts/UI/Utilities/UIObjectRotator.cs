using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Xoay liên tục một <see cref="RectTransform"/> (vòng loading, bánh răng trang trí).
    /// <para>
    /// Khác <see cref="RotateClockwise"/> ở hai điểm: tác động lên RectTransform của UI và
    /// xoay được quanh nhiều trục cùng lúc qua <see cref="rotationPerSecond"/>.
    /// </para>
    /// <para>
    /// Có thể gán <see cref="objectCanvas"/> để chỉ xoay khi canvas đó đang bật — tiết kiệm CPU
    /// cho các màn UI bị che khuất.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class UIObjectRotator : MonoBehaviour
    {
        [Tooltip("Chỉ xoay khi Canvas này đang bật. Bỏ trống thì luôn xoay.")]
        [SerializeField] private Canvas objectCanvas;

        [Tooltip("RectTransform cần xoay. Bỏ trống thì dùng RectTransform của GameObject này.")]
        [SerializeField] private RectTransform objectToRotate;

        [Tooltip("Tốc độ xoay theo từng trục, độ mỗi giây. Chỉ dùng Z là đủ cho UI phẳng.")]
        [SerializeField] private Vector3 rotationPerSecond = new Vector3(0f, 0f, -120f);

        [Tooltip("Xoay trong không gian local (mặc định) hay world.")]
        [SerializeField] private bool useLocalSpace = true;

        [Tooltip("Tự xoay ngay khi component được bật.")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("Dùng unscaled time để vòng loading vẫn quay khi game đang pause.")]
        [SerializeField] private bool useUnscaledTime = true;

        private bool isRotating;

        /// <summary>Tốc độ xoay hiện tại theo từng trục (độ mỗi giây).</summary>
        public Vector3 RotationPerSecond
        {
            get => rotationPerSecond;
            set => rotationPerSecond = value;
        }

        private void Awake()
        {
            if (objectToRotate == null) objectToRotate = GetComponent<RectTransform>();
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

        /// <summary>Đưa góc xoay về 0.</summary>
        public void ResetRotation()
        {
            if (objectToRotate == null) return;
            objectToRotate.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isRotating || objectToRotate == null) return;
            if (objectCanvas != null && !objectCanvas.isActiveAndEnabled) return;
            if (rotationPerSecond == Vector3.zero) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            objectToRotate.Rotate(rotationPerSecond * dt, useLocalSpace ? Space.Self : Space.World);
        }
    }
}
