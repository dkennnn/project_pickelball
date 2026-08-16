using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quả bóng "ma" chỉ sống trong physics scene phụ của <see cref="PhysicsTrajectoryHandler"/>.
    /// <para>
    /// Prefab gắn script này PHẢI có <see cref="Rigidbody"/> + <see cref="Collider"/> và
    /// <b>cùng thông số vật lý với quả bóng thật</b> (mass, drag, physic material, collider radius),
    /// nếu không dự đoán sẽ lệch so với thực tế.
    /// </para>
    /// <para>
    /// Instance của nó KHÔNG bao giờ được render: <see cref="PhysicsTrajectoryHandler"/> tắt hết
    /// <see cref="Renderer"/> ngay khi tạo. Nó cũng không được phép gọi vào gameplay
    /// (<c>GameManager</c>, âm thanh, VFX...) — mọi thứ nó làm chỉ là ghi lại va chạm.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimulationBall : MonoBehaviour
    {
        /// <summary>Rigidbody của bóng mô phỏng; được lấy tự động trong <c>Awake</c>.</summary>
        public Rigidbody rb;

        /// <summary>Toàn bộ va chạm ghi được trong lần mô phỏng hiện tại, theo thứ tự thời gian.</summary>
        public List<SimulationBallCollisionData> Collisions = new List<SimulationBallCollisionData>();

        /// <summary>True khi bóng đã chạm đất ít nhất một lần trong lần mô phỏng hiện tại.</summary>
        public bool HasBounced;

        /// <summary>
        /// Mốc thời gian mô phỏng hiện tại (giây). <see cref="PhysicsTrajectoryHandler"/> cập nhật
        /// giá trị này TRƯỚC mỗi bước <c>Simulate</c> để va chạm ghi được đúng thời điểm.
        /// </summary>
        public float SimulatedTime;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Đặt bóng về trạng thái đầu và bắn đi với vận tốc/xoáy cho trước.
        /// Gọi trước vòng lặp <c>PhysicsScene.Simulate</c>.
        /// </summary>
        /// <param name="position">Vị trí xuất phát (world).</param>
        /// <param name="rotation">Hướng xuất phát.</param>
        /// <param name="velocity">Vận tốc tuyến tính ban đầu (m/s).</param>
        /// <param name="angularVelocity">Vận tốc góc ban đầu (rad/s).</param>
        /// <param name="torque">Mô-men xoáy cộng một lần duy nhất, đúng như <see cref="BallController"/> làm.</param>
        public void SimulateHitBall(Vector3 position, Quaternion rotation, Vector3 velocity,
            Vector3 angularVelocity, Vector3 torque)
        {
            ResetBall();

            if (rb == null) rb = GetComponent<Rigidbody>();

            transform.SetPositionAndRotation(position, rotation);

            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;

            // ForceMode.Force để khớp CHÍNH XÁC với rb.AddTorque(torque) mặc định trong BallController.
            if (torque.sqrMagnitude > Mathf.Epsilon) rb.AddTorque(torque, ForceMode.Force);
        }

        /// <summary>Xoá toàn bộ lịch sử va chạm và dừng hẳn bóng, sẵn sàng cho lần mô phỏng tiếp theo.</summary>
        public void ResetBall()
        {
            Collisions.Clear();
            HasBounced = false;
            SimulatedTime = 0f;

            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        /// <summary>Ghi lại va chạm và phân loại theo tag <c>Ground</c> / <c>Net</c>.</summary>
        private void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.gameObject;
            bool isGround = other.CompareTag(StringConstants.GroundTag);
            bool isNet = other.CompareTag(StringConstants.NetTag);

            Vector3 point = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            Collisions.Add(new SimulationBallCollisionData(point, SimulatedTime, isGround, isNet));

            if (isGround) HasBounced = true;
        }
    }
}
