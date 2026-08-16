using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Xoay cây vợt hướng về điểm đón bóng với tốc độ <c>alignSpeed</c> (độ/giây).
    /// <para>
    /// Khi vợt đã khớp hướng trong sai số <see cref="alignedAngleThreshold"/>, callback
    /// <c>onAligned</c> được gọi ĐÚNG MỘT LẦN — đây chính là tín hiệu mở khoá cú đánh trong
    /// <see cref="TakeShotState"/>.
    /// </para>
    /// </summary>
    public class RacquetAnimator : MonoBehaviour
    {
        /// <summary>Transform của cây vợt; để trống thì dùng chính transform này.</summary>
        public Transform racquetTransform;

        /// <summary>Tốc độ xoay mặc định (độ/giây) khi lời gọi không truyền tốc độ hợp lệ.</summary>
        public float alignSpeed = 540f;

        /// <summary>Sai số góc (độ) coi như đã khớp hướng.</summary>
        public float alignedAngleThreshold = 5f;

        private Vector3 targetPoint;
        private float currentSpeed;
        private bool isAligning;
        private bool isAligned;
        private Action onAlignedCallback;
        private Quaternion originalLocalRotation;
        private bool poseCached;

        /// <summary>True khi vợt đã xoay khớp hướng tới điểm đón bóng.</summary>
        public bool IsAligned => isAligned;

        private void Awake()
        {
            if (racquetTransform == null) racquetTransform = transform;
            CachePose();
        }

        private void Update()
        {
            if (!isAligning || racquetTransform == null) return;

            Vector3 direction = targetPoint - racquetTransform.position;
            if (direction.sqrMagnitude < 0.000001f)
            {
                CompleteAlign();
                return;
            }

            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            racquetTransform.rotation = Quaternion.RotateTowards(racquetTransform.rotation, desired,
                                                                 currentSpeed * Time.deltaTime);

            if (Quaternion.Angle(racquetTransform.rotation, desired) <= alignedAngleThreshold) CompleteAlign();
        }

        /// <summary>
        /// Bắt đầu xoay vợt về một điểm trong world space.
        /// </summary>
        /// <param name="targetPoint">Điểm đón bóng cần hướng vợt tới.</param>
        /// <param name="speed">Tốc độ xoay (độ/giây); &lt;= 0 thì dùng <see cref="alignSpeed"/>.</param>
        /// <param name="onAligned">Callback chạy đúng một lần khi vợt đã khớp hướng.</param>
        public void AlignTo(Vector3 targetPoint, float speed, Action onAligned)
        {
            CachePose();

            this.targetPoint = targetPoint;
            currentSpeed = speed > 0f ? speed : alignSpeed;
            onAlignedCallback = onAligned;
            isAligning = true;
            isAligned = false;
        }

        /// <summary>Dừng xoay và trả vợt về tư thế gốc đã ghi nhớ.</summary>
        public void ResetPose()
        {
            isAligning = false;
            isAligned = false;
            onAlignedCallback = null;

            if (racquetTransform != null && poseCached) racquetTransform.localRotation = originalLocalRotation;
        }

        private void CompleteAlign()
        {
            isAligning = false;
            isAligned = true;

            Action callback = onAlignedCallback;
            onAlignedCallback = null;
            callback?.Invoke();
        }

        private void CachePose()
        {
            if (poseCached) return;
            if (racquetTransform == null) racquetTransform = transform;

            originalLocalRotation = racquetTransform.localRotation;
            poseCached = true;
        }
    }
}
