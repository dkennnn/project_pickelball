using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tăng độ nảy của bóng bằng tay thay vì dựa hoàn toàn vào PhysicMaterial.
    /// <para>
    /// PhysicMaterial chỉ cho <c>bounciness ≤ 1</c> và bị Unity làm mềm bởi solver, nên bóng
    /// pickleball nảy "ỉu" hơn cảm giác mong muốn. Script này chạy ngay sau khi solver đã xử lý
    /// va chạm, tách vận tốc thành thành phần <b>pháp tuyến</b> (dội lên) và <b>tiếp tuyến</b>
    /// (trượt theo mặt), rồi nhân riêng thành phần pháp tuyến với <see cref="bounceFactor"/>.
    /// Nhờ tách như vậy, bóng không bị đổi hướng ngang một cách phi lý.
    /// </para>
    /// <para>
    /// CẢNH BÁO: quả bóng mô phỏng của <see cref="PhysicsTrajectoryHandler"/> cũng phải gắn
    /// component này với CÙNG <see cref="bounceFactor"/>, nếu không đường bay sau lần nảy đầu
    /// tiên sẽ lệch khỏi dự đoán.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnhancedBounce : MonoBehaviour
    {
        /// <summary>
        /// Hệ số nhân cho thành phần vận tốc theo pháp tuyến sau va chạm.
        /// &gt; 1 = nảy cao hơn thực tế, 1 = giữ nguyên, &lt; 1 = giảm chấn.
        /// </summary>
        [Tooltip("Hệ số nhân thành phần vận tốc pháp tuyến sau mỗi va chạm. 1.05 = nảy 'đã tay' hơn ~5%.")]
        public float bounceFactor = 1.05f;

        [Tooltip("Ngưỡng tốc độ pháp tuyến tối thiểu để khuếch đại. Dưới ngưỡng này bóng đang lăn/rung, " +
                 "khuếch đại sẽ khiến bóng rung giật mãi không dừng.")]
        [SerializeField] private float minNormalSpeed = 0.2f;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (rb == null || rb.isKinematic || collision.contactCount == 0) return;

            Vector3 normal = collision.GetContact(0).normal;
            Vector3 velocity = rb.linearVelocity;

            // Thành phần dội lại theo pháp tuyến (solver đã đảo chiều nó xong).
            Vector3 normalComponent = Vector3.Project(velocity, normal);
            if (normalComponent.magnitude < minNormalSpeed) return;

            Vector3 tangentialComponent = velocity - normalComponent;
            rb.linearVelocity = tangentialComponent + normalComponent * bounceFactor;
        }
    }
}
