using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đuổi theo bóng: chạy tới điểm đứng đã tính trong
    /// <see cref="BasePlayerController.FindOptimalTargetPositionForTrackingBall"/> và đếm ngược tới
    /// thời điểm chạm bóng.
    /// <para>
    /// Khi chỉ còn <c>preHitShotTime</c> giây nữa là bóng tới, state tự chuyển sang
    /// <see cref="PlayerState.TakeShot"/> để kịp chạy animation giơ vợt (Pre-shot).
    /// </para>
    /// </summary>
    public class TrackBallState : IPlayerState
    {
        private readonly PickleballPlayerController playerController;

        private bool isReadyToHit;
        private float timeToHitBall;
        private float preHitShotTime;
        private Vector3 playerPosition;

        /// <summary>Thời lượng mặc định của animation giơ vợt khi chưa có dữ liệu từ Animator (giây).</summary>
        private const float DefaultPreShotTime = 0.35f;

        /// <summary>Tạo trạng thái đuổi bóng cho một tay vợt.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public TrackBallState(PickleballPlayerController controller)
        {
            playerController = controller;
        }

        /// <summary>Ghi nhận thời điểm chạm bóng dự kiến và bắt đầu chạy tới điểm đón bóng.</summary>
        public void Enter()
        {
            if (playerController == null) return;

            isReadyToHit = false;
            playerPosition = playerController.transform.position;
            timeToHitBall = playerController.TargetBallHitTime;
            preHitShotTime = DefaultPreShotTime;

            playerController.isSwipeUpdated = false;

            if (playerController.animationController != null)
            {
                playerController.animationController.SetMoveSpeed(1f);
            }
        }

        /// <summary>Không giữ tài nguyên nào nên không cần dọn.</summary>
        public void Exit()
        {
        }

        /// <summary>
        /// Đếm ngược tới cú đánh. Hết thời gian mà quỹ đạo không hợp lệ (không với tới bóng)
        /// thì quay về <see cref="PlayerState.Idle"/>.
        /// </summary>
        public void Update()
        {
            if (playerController == null) return;

            // Không có điểm đón bóng hợp lệ -> đứng chờ.
            if (timeToHitBall <= 0f)
            {
                playerController.ChangeState(PlayerState.Idle);
                return;
            }

            float remaining = timeToHitBall - Time.time;

            if (remaining <= preHitShotTime)
            {
                isReadyToHit = true;
                playerController.ChangeState(PlayerState.TakeShot);
            }
        }

        /// <summary>
        /// Người chơi vuốt sớm khi còn đang chạy: ghi nhận cú vuốt và vào luôn
        /// <see cref="PlayerState.TakeShot"/> để <see cref="TakeShotState"/> xử lý cú đánh.
        /// </summary>
        /// <param name="swipeData">Dữ liệu cú vuốt.</param>
        public void OnSwipeReceived(SwipeData swipeData)
        {
            if (playerController == null || swipeData == null) return;
            if (swipeData.SwipePhase != SwipePhase.End) return;

            playerController.swipeData = swipeData;
            playerController.isSwipeUpdated = true;

            isReadyToHit = true;
            playerController.ChangeState(PlayerState.TakeShot);
        }

        /// <summary>True khi đã tới lúc chuyển sang pha đánh bóng.</summary>
        public bool IsReadyToHit => isReadyToHit;

        /// <summary>Vị trí tay vợt lúc bắt đầu đuổi bóng (dùng để đo quãng chạy).</summary>
        public Vector3 StartPosition => playerPosition;
    }
}
