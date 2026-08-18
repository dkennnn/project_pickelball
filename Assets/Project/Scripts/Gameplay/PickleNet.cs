using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Lưới pickleball: vừa là collider chặn bóng, vừa tạo hiệu ứng gợn sóng (ripple) trên shader
    /// khi bị bóng đập vào.
    /// <para>
    /// Cường độ, bề rộng và thời lượng gợn sóng đều nội suy theo tốc độ bóng lúc va chạm,
    /// giữa <see cref="minBallSpeedThreshold"/> và <see cref="maxBallSpeed"/>: bóng chạm nhẹ chỉ
    /// rung khẽ, bóng đập mạnh làm lưới rung mạnh và lâu hơn.
    /// </para>
    /// <para>
    /// Nếu <see cref="rippleMaterial"/> chưa được gán (giai đoạn chưa có art), toàn bộ phần
    /// shader được bỏ qua an toàn — logic va chạm với rule engine vẫn chạy bình thường.
    /// </para>
    /// </summary>
    public class PickleNet : MonoBehaviour
    {
        [Header("Ripple Material")]
        [Tooltip("Material của mặt lưới có shader hỗ trợ các tham số ripple bên dưới. Bỏ trống = tắt hiệu ứng.")]
        public Material rippleMaterial;

        [Header("Ripple Runtime State")]
        [Tooltip("Bề rộng vòng sóng hiện tại (đơn vị world).")]
        public float rippleWidth = 0.35f;

        [Tooltip("Cường độ gợn sóng hiện tại.")]
        public float rippleStrength = 0.5f;

        [Tooltip("Hướng lan của sóng trên mặt lưới.")]
        public Vector3 rippleDirection = Vector3.forward;

        [Tooltip("Thời lượng (giây) của lần gợn sóng hiện tại; tính ra từ tốc độ bóng.")]
        public float maxRippleTime = 0.6f;

        [Tooltip("Độ sáng bổ sung của lưới khi đang gợn sóng.")]
        public float brightness = 1f;

        [Header("Ripple Tuning")]
        [Tooltip("Cường độ gợn sóng khi bóng chạm ở tốc độ minBallSpeedThreshold.")]
        public float minRippleStrength = 0.15f;

        [Tooltip("Cường độ gợn sóng khi bóng chạm ở tốc độ maxBallSpeed.")]
        public float maxRippleStrength = 1f;

        [Tooltip("Thời lượng gợn sóng (giây) ứng với cú chạm nhẹ nhất.")]
        public float minRippleTime = 0.25f;

        [Tooltip("Thời lượng gợn sóng (giây) ứng với cú đập mạnh nhất.")]
        public float maxRippleEffectTime = 0.9f;

        [Tooltip("Bề rộng vòng sóng ứng với cú chạm nhẹ nhất.")]
        public float minRippleWidth = 0.15f;

        [Tooltip("Bề rộng vòng sóng ứng với cú đập mạnh nhất.")]
        public float maxRippleWidth = 0.6f;

        [Tooltip("Dưới tốc độ này coi như bóng chỉ chạm khẽ — vẫn gợn nhưng ở mức tối thiểu.")]
        public float minBallSpeedThreshold = 2f;

        [Tooltip("Tốc độ bóng cho hiệu ứng cực đại; nhanh hơn nữa cũng không mạnh thêm.")]
        public float maxBallSpeed = 18f;

        /// <summary>Tên tham số độ sáng trên shader lưới.</summary>
        public string BrightnessParameter => "_Brightness";

        /// <summary>Tên tham số tiến độ sóng [0..1] trên shader lưới.</summary>
        public string RipplePercentageParameter => "_RipplePercentage";

        /// <summary>Tên tham số cường độ sóng trên shader lưới.</summary>
        public string RippleIntensityParameter => "_RippleIntensity";

        /// <summary>Tên tham số tâm sóng (world space) trên shader lưới.</summary>
        public string RippleCenterParameter => "_RippleCenter";

        /// <summary>Tên tham số bề rộng vòng sóng trên shader lưới.</summary>
        public string RippleWidthParameter => "_RippleWidth";

        // Cache PropertyToID: tra cứu bằng string mỗi frame là lãng phí.
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int RipplePercentageId = Shader.PropertyToID("_RipplePercentage");
        private static readonly int RippleIntensityId = Shader.PropertyToID("_RippleIntensity");
        private static readonly int RippleCenterId = Shader.PropertyToID("_RippleCenter");
        private static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");

        private Vector4 rippleOrigin;
        private bool isRippling;
        private float rippleTime;

        private void Start()
        {
            StopRipple();
        }

        /// <summary>Material đang dùng để vẽ gợn sóng; có thể null nếu chưa gán art.</summary>
        public Material GetNetMaterial()
        {
            return rippleMaterial;
        }

        /// <summary>
        /// Bắt đầu một đợt gợn sóng tại điểm va chạm.
        /// </summary>
        /// <param name="impactPoint">Điểm bóng đập vào lưới (world space).</param>
        /// <param name="ballSpeed">Tốc độ bóng lúc va chạm (m/s) — quyết định cường độ và thời lượng.</param>
        public void StartRipple(Vector3 impactPoint, float ballSpeed)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(minBallSpeedThreshold, maxBallSpeed, ballSpeed));

            rippleStrength = Mathf.Lerp(minRippleStrength, maxRippleStrength, t);
            rippleWidth = Mathf.Lerp(minRippleWidth, maxRippleWidth, t);
            maxRippleTime = Mathf.Lerp(minRippleTime, maxRippleEffectTime, t);
            rippleOrigin = new Vector4(impactPoint.x, impactPoint.y, impactPoint.z, 0f);
            rippleDirection = (impactPoint - transform.position).normalized;

            rippleTime = 0f;
            isRippling = true;

            ApplyRippleToMaterial(0f, rippleStrength);
        }

        /// <summary>Tắt hẳn hiệu ứng và trả shader về trạng thái nghỉ.</summary>
        private void StopRipple()
        {
            isRippling = false;
            rippleTime = 0f;
            ApplyRippleToMaterial(0f, 0f);
        }

        private void Update()
        {
            if (!isRippling) return;

            rippleTime += Time.deltaTime;

            if (maxRippleTime <= 0f || rippleTime >= maxRippleTime)
            {
                StopRipple();
                return;
            }

            float percentage = Mathf.Clamp01(rippleTime / maxRippleTime);
            // Sóng lan ra đồng thời tắt dần theo thời gian.
            float intensity = rippleStrength * (1f - percentage);
            ApplyRippleToMaterial(percentage, intensity);
        }

        private void ApplyRippleToMaterial(float percentage, float intensity)
        {
            if (rippleMaterial == null) return;

            rippleMaterial.SetFloat(BrightnessId, brightness);
            rippleMaterial.SetFloat(RipplePercentageId, percentage);
            rippleMaterial.SetFloat(RippleIntensityId, intensity);
            rippleMaterial.SetVector(RippleCenterId, rippleOrigin);
            rippleMaterial.SetFloat(RippleWidthId, rippleWidth);
        }

        // [ServerCallback] — Phase 2 sẽ chỉ chạy trên server và phát RpcOnNetCollisionEnter về client.
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag(StringConstants.BallTag)) return;

            // Bóng chưa vào cuộc (đang trong tay người giao / pha bóng đã khép lại) thì chỉ gợn lưới,
            // không báo lỗi luật. Cùng điều kiện với BallController.OnCollisionEnter.
            bool ballIsLive = !BallController.HasInstance
                              || (BallController.Instance.IsInPlay && !BallController.Instance.isBallHeld);

            float ballSpeed = collision.relativeVelocity.magnitude;
            StartRipple(
                collision.contactCount > 0 ? collision.GetContact(0).point : transform.position,
                ballSpeed);

            if (ballIsLive && GameManager.HasInstance) GameManager.Instance.OnBallHitNet();
        }
    }
}
