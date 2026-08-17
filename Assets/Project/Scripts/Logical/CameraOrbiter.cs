using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quay camera quanh một mục tiêu theo quỹ đạo tròn — dùng cho đoạn kết trận
    /// (camera lượn quanh người thắng) và cho màn xem trước vật phẩm trong tủ đồ.
    /// <para>
    /// Ghi chú tích hợp: <c>RuleEngine/GameManager.cs</c> ở bản dựng lại chỉ có
    /// <c>public GameObject endSequenceObject</c>, KHÔNG có field kiểu <c>CameraOrbiter</c>.
    /// Vì vậy API ở đây được thiết kế tự do; muốn nối vào cuối trận thì gắn component này lên
    /// object con của <c>endSequenceObject</c> và bật <see cref="orbitOnEnable"/>.
    /// </para>
    /// <para>Thiếu <see cref="target"/> thì component đứng yên, không ném lỗi.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraOrbiter : MonoBehaviour
    {
        [Tooltip("Transform được quay quanh. Bỏ trống thì component đứng yên.")]
        [SerializeField] private Transform target;

        [Tooltip("Transform sẽ di chuyển. Bỏ trống thì dùng transform của GameObject này.")]
        [SerializeField] private Transform orbitTransform;

        [Tooltip("Bán kính quỹ đạo, tính theo đơn vị world.")]
        [SerializeField] private float radius = 6f;

        [Tooltip("Độ cao so với mục tiêu, tính theo đơn vị world.")]
        [SerializeField] private float height = 2.5f;

        [Tooltip("Tốc độ quay quanh mục tiêu, độ mỗi giây.")]
        [SerializeField] private float degreesPerSecond = 25f;

        [Tooltip("Luôn xoay để nhìn về phía mục tiêu.")]
        [SerializeField] private bool lookAtTarget = true;

        [Tooltip("Điểm nhìn được nâng lên bao nhiêu so với gốc mục tiêu (nhìn vào ngực chứ không vào chân).")]
        [SerializeField] private float lookAtHeightOffset = 1f;

        [Tooltip("Tự bắt đầu quay khi component được bật (nếu đã có target).")]
        [SerializeField] private bool orbitOnEnable = true;

        [Tooltip("Dùng unscaled time để camera vẫn lượn khi game đang pause.")]
        [SerializeField] private bool useUnscaledTime = true;

        private float currentAngle;
        private bool isOrbiting;

        /// <summary>Mục tiêu đang được quay quanh; null nếu chưa đặt.</summary>
        public Transform Target => target;

        /// <summary>True khi camera đang lượn quanh mục tiêu.</summary>
        public bool IsOrbiting => isOrbiting && target != null;

        private void Awake()
        {
            if (orbitTransform == null) orbitTransform = transform;
        }

        private void OnEnable()
        {
            if (orbitOnEnable && target != null) StartOrbit(target);
        }

        private void OnDisable()
        {
            isOrbiting = false;
        }

        /// <summary>Bắt đầu quay quanh <paramref name="orbitTarget"/>.</summary>
        /// <param name="orbitTarget">Mục tiêu; null thì không làm gì.</param>
        public void StartOrbit(Transform orbitTarget)
        {
            if (orbitTarget == null) return;

            target = orbitTarget;
            if (orbitTransform == null) orbitTransform = transform;

            // Bắt đầu từ đúng góc hiện tại của camera để không bị "giật" một nhịp.
            Vector3 flat = orbitTransform.position - target.position;
            flat.y = 0f;
            currentAngle = flat.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg
                : 0f;

            isOrbiting = true;
            ApplyPosition();
        }

        /// <summary>Bắt đầu quay quanh mục tiêu đã gán sẵn trên Inspector.</summary>
        public void StartOrbit()
        {
            StartOrbit(target);
        }

        /// <summary>Dừng quay (camera đứng lại ở vị trí hiện tại).</summary>
        public void StopOrbit()
        {
            isOrbiting = false;
        }

        /// <summary>Đặt camera vào vị trí quỹ đạo ngay lập tức mà không bắt đầu quay.</summary>
        public void SetInitialPosition()
        {
            if (target == null) return;
            if (orbitTransform == null) orbitTransform = transform;
            ApplyPosition();
        }

        private void LateUpdate()
        {
            if (!isOrbiting || target == null || orbitTransform == null) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            currentAngle = Mathf.Repeat(currentAngle + degreesPerSecond * dt, 360f);
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, height, Mathf.Sin(rad) * radius);
            orbitTransform.position = target.position + offset;

            if (!lookAtTarget) return;
            orbitTransform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
        }
    }
}
