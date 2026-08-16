using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái giao bóng: giữ bóng trong tay, hiện chỉ dẫn ô giao, chờ người chơi vuốt.
    /// <para>
    /// Trình tự một cú giao: <c>Enter</c> (cầm bóng) → người chơi vuốt (<see cref="OnSwipeReceived"/>)
    /// → animation event <see cref="OnThrowBall"/> (thả bóng khỏi tay)
    /// → animation event <see cref="OnShotHit"/> (vợt chạm bóng, bắn bóng đi).
    /// </para>
    /// <para>
    /// Khi chưa có rig/animation event, <see cref="BasePlayerController.HitBallAnimation"/> đóng vai trò
    /// đồng hồ dự phòng; cờ <c>hasThrownBall</c>/<c>hasHitBall</c> bảo đảm mỗi việc chỉ chạy đúng một lần.
    /// </para>
    /// </summary>
    public class ServeState : IPlayerState
    {
        private readonly PickleballPlayerController playerController;

        private bool hasThrownBall;
        private bool hasHitBall;
        private bool isServing;
        private SwipeData pendingSwipe;

        /// <summary>Độ trễ dự phòng (giây) từ lúc bắt đầu animation tới khung tung bóng.</summary>
        private const float FallbackThrowDelay = 0.2f;

        /// <summary>Tạo trạng thái giao bóng cho một tay vợt.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public ServeState(PickleballPlayerController controller)
        {
            playerController = controller;
        }

        /// <summary>Cầm bóng, bật chỉ dẫn ô giao, phát animation chuẩn bị giao và lắng nghe animation event.</summary>
        public void Enter()
        {
            if (playerController == null) return;

            hasThrownBall = false;
            hasHitBall = false;
            isServing = false;
            pendingSwipe = null;

            playerController.isSwipeUpdated = false;
            playerController.SetAttemptingVolley(false);
            playerController.HoldBall();
            playerController.ShowServeIndicator();

            if (playerController.animationController != null) playerController.animationController.PlayServe();

            playerController.AnimationThrowBall += OnThrowBall;
            playerController.AnimationShotHit += OnShotHit;
        }

        /// <summary>Huỷ đăng ký animation event và tắt chỉ dẫn ô giao.</summary>
        public void Exit()
        {
            if (playerController == null) return;

            playerController.AnimationThrowBall -= OnThrowBall;
            playerController.AnimationShotHit -= OnShotHit;

            playerController.HideServeIndicator();
            playerController.HideDestinationMarker();
        }

        /// <summary>Không cần xử lý theo frame — mọi thứ chạy theo swipe và animation event.</summary>
        public void Update()
        {
        }

        /// <summary>
        /// Nhận cú vuốt của người chơi. Chỉ <see cref="SwipePhase.End"/> mới kích hoạt cú giao;
        /// pha <see cref="SwipePhase.Progress"/> chỉ dùng để vẽ marker điểm rơi.
        /// </summary>
        /// <param name="swipeData">Dữ liệu cú vuốt.</param>
        public void OnSwipeReceived(SwipeData swipeData)
        {
            if (playerController == null || swipeData == null) return;
            if (isServing || hasHitBall) return;

            if (swipeData.SwipePhase != SwipePhase.End) return;

            isServing = true;
            pendingSwipe = swipeData;

            float shotPower = playerController.profile != null ? playerController.profile.shotPower : 0.5f;

            // Đồng hồ dự phòng: nếu animation event không tới thì vẫn tung bóng rồi đánh.
            DelayedAction.RunUnscaled(FallbackThrowDelay, OnThrowBall);
            playerController.HitBallAnimation(shotPower, OnShotHit);
        }

        /// <summary>Animation event: bóng rời tay người giao.</summary>
        public void OnThrowBall()
        {
            if (playerController == null || hasThrownBall) return;
            if (!isServing) return;

            hasThrownBall = true;
            playerController.UnholdBall();
        }

        /// <summary>Animation event: vợt chạm bóng — bắn quả giao đi.</summary>
        public void OnShotHit()
        {
            if (playerController == null || hasHitBall) return;
            if (!isServing || pendingSwipe == null) return;

            hasHitBall = true;

            OnThrowBall();
            HitBall(pendingSwipe);
        }

        /// <summary>Gọi <see cref="BasePlayerController.HitBall(string, SwipeData, float, float, float, float)"/> rồi báo cú giao đã xong.</summary>
        private void HitBall(SwipeData swipeData)
        {
            PlayerProfileProperties profile = playerController.profile;

            float halfLength = playerController.MatchCourt != null ? playerController.MatchCourt.HalfLength : 6.7056f;
            float playerDepth = halfLength > 0f
                ? Mathf.Clamp01(Mathf.Abs(playerController.transform.position.z) / halfLength)
                : 1f;

            float swingAbility = profile != null ? profile.swingAbility : 0f;
            float spinAbility = profile != null ? profile.spinAbility : 0f;
            float shotPowerFactor = profile != null ? profile.shotPower : 0.5f; // TODO P05: nhân thêm hệ số booster.

            playerController.HitBall(playerController.teamID, swipeData, playerDepth, swingAbility, spinAbility,
                                     shotPowerFactor);

            playerController.isSwipeUpdated = false;
            playerController.OnServe();
        }
    }
}
