using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Camera bám theo một <see cref="Transform"/> (thường là tay vợt cục bộ) với độ trễ mượt,
    /// đồng thời giới hạn vị trí camera trong một khung XZ để không lia ra ngoài sân vận động.
    /// <para>
    /// Toàn bộ tính toán chạy trong <c>LateUpdate</c> để bám đúng vị trí sau khi nhân vật đã di chuyển.
    /// </para>
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        /// <summary>Mục tiêu cần bám theo.</summary>
        [Header("Target")]
        public Transform target;

        /// <summary>Độ lệch của camera so với mục tiêu (world space).</summary>
        public Vector3 offset = new Vector3(0f, 6f, -8f);

        /// <summary>Độ mượt khi bám (đơn vị/giây); giá trị càng lớn càng bám sát.</summary>
        [Header("Smoothing")]
        public float smoothSpeed = 5f;

        /// <summary>True để camera luôn quay mặt về mục tiêu.</summary>
        public bool lookAtTarget = true;

        /// <summary>Độ mượt khi xoay về mục tiêu (đơn vị/giây).</summary>
        public float rotationSmoothSpeed = 8f;

        /// <summary>Giới hạn vị trí camera theo trục X: (min, max).</summary>
        [Header("Clamp")]
        public Vector2 clampX = new Vector2(-4f, 4f);

        /// <summary>Giới hạn vị trí camera theo trục Z: (min, max).</summary>
        public Vector2 clampZ = new Vector2(-14f, 14f);

        /// <summary>Bật để áp dụng giới hạn <see cref="clampX"/>/<see cref="clampZ"/>.</summary>
        public bool useClamp = true;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = ApplyClamp(target.position + offset);

            float t = smoothSpeed <= 0f ? 1f : 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);

            if (!lookAtTarget) return;

            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude < 0.000001f) return;

            Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rt = rotationSmoothSpeed <= 0f ? 1f : 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rt);
        }

        /// <summary>Đổi mục tiêu bám theo và nhảy ngay tới vị trí mới.</summary>
        /// <param name="newTarget">Mục tiêu mới; <c>null</c> sẽ tắt việc bám.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        /// <summary>Đặt camera vào đúng vị trí/hướng đích ngay lập tức, bỏ qua độ trễ.</summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            transform.position = ApplyClamp(target.position + offset);

            if (!lookAtTarget) return;

            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude < 0.000001f) return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        /// <summary>Ép vị trí camera nằm trong khung giới hạn XZ đã cấu hình.</summary>
        private Vector3 ApplyClamp(Vector3 position)
        {
            if (!useClamp) return position;

            position.x = Mathf.Clamp(position.x, Mathf.Min(clampX.x, clampX.y), Mathf.Max(clampX.x, clampX.y));
            position.z = Mathf.Clamp(position.z, Mathf.Min(clampZ.x, clampZ.y), Mathf.Max(clampZ.x, clampZ.y));
            return position;
        }
    }
}
