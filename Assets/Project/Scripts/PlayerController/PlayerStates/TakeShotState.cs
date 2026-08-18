using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đánh bóng — pha kịch tính nhất của gameplay.
    /// <para><b>Trình tự:</b></para>
    /// <list type="number">
    /// <item><description><c>Enter</c>: phát animation giơ vợt (Pre-shot), bắt đầu xoay vợt về điểm đón bóng
    /// và bật <b>slow-motion</b> bằng <see cref="LerpTimeScale"/> để người chơi kịp vuốt.</description></item>
    /// <item><description><see cref="OnPaddleAligned"/>: vợt đã khớp hướng — từ lúc này cú vuốt mới được
    /// chấp nhận thành cú đánh.</description></item>
    /// <item><description><c>Exit</c>: LUÔN khôi phục <see cref="Time.timeScale"/> về 1, kể cả khi thoát
    /// state bất thường (ghi điểm, hết trận, đối thủ phạm lỗi).</description></item>
    /// </list>
    /// </summary>
    public class TakeShotState : IPlayerState
    {
        private readonly PickleballPlayerController playerController;

        private bool isHitBallComplete;
        private bool paddleAligned;
        private float timer;
        private bool timerStarted;

        private Coroutine timeScaleRoutine;
        private RacquetAnimator racquetAnimator;

        /// <summary>Time scale trong lúc chờ người chơi vuốt.</summary>
        private const float SlowMotionTimeScale = 0.35f;

        /// <summary>Thời gian (thực) để chuyển vào/ra slow-motion.</summary>
        private const float SlowMotionBlendDuration = 0.15f;

        /// <summary>Thời gian tối đa (thực) ở trong pha đánh bóng trước khi bỏ cuộc.</summary>
        private const float MaxShotWindow = 1.5f;

        /// <summary>Thời gian dự phòng coi như vợt đã khớp khi chưa có <see cref="RacquetAnimator"/>.</summary>
        private const float FallbackPaddleAlignTime = 0.15f;

        /// <summary>Tạo trạng thái đánh bóng cho một tay vợt.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public TakeShotState(PickleballPlayerController controller)
        {
            playerController = controller;
        }

        /// <summary>Giơ vợt, xoay vợt về điểm đón bóng và vào slow-motion.</summary>
        public void Enter()
        {
            if (playerController == null) return;

            isHitBallComplete = false;
            paddleAligned = false;
            timer = 0f;
            timerStarted = true;

            playerController.AnimationPaddleAligned += OnPaddleAligned;
            playerController.AnimationShotHit += OnPaddleAligned;

            PlayPreShotAnimation();
            AlignPaddle();

            LerpTimeScale(SlowMotionTimeScale, SlowMotionBlendDuration);
        }

        /// <summary>
        /// Dọn sạch pha đánh bóng: dừng tween time scale, KHÔI PHỤC <see cref="Time.timeScale"/> = 1,
        /// trả vợt về tư thế gốc và huỷ đăng ký animation event.
        /// </summary>
        public void Exit()
        {
            if (playerController == null) return;

            playerController.AnimationPaddleAligned -= OnPaddleAligned;
            playerController.AnimationShotHit -= OnPaddleAligned;

            if (timeScaleRoutine != null)
            {
                playerController.StopCoroutine(timeScaleRoutine);
                timeScaleRoutine = null;
            }

            // Bắt buộc: kể cả thoát bất thường cũng phải trả lại tốc độ thời gian bình thường.
            Time.timeScale = 1f;

            if (racquetAnimator != null) racquetAnimator.ResetPose();
            playerController.ResetPaddlePose();
            playerController.HideDestinationMarker();

            timerStarted = false;
            playerController.isSwipeUpdated = false;
        }

        /// <summary>Chờ vợt khớp hướng + có cú vuốt hợp lệ thì đánh bóng; quá thời gian thì bỏ qua pha đánh.</summary>
        public void Update()
        {
            if (playerController == null || !timerStarted) return;

            timer += Time.unscaledDeltaTime;

            if (!paddleAligned && racquetAnimator == null && timer >= FallbackPaddleAlignTime)
            {
                OnPaddleAligned();
            }

            if (!isHitBallComplete && paddleAligned && playerController.isSwipeUpdated
                && playerController.swipeData != null
                && playerController.swipeData.SwipePhase == SwipePhase.End)
            {
                HitBall(playerController.swipeData);
                return;
            }

            if (timer >= MaxShotWindow && !isHitBallComplete)
            {
                // Người chơi không vuốt kịp -> bỏ lỡ cú đánh, luật sẽ do GameManager xử lý khi bóng nảy hai lần.
                playerController.ChangeState(PlayerState.Idle);
            }
        }

        /// <summary>Nhận cú vuốt trong pha đánh bóng; chỉ pha kết thúc mới tính là cú đánh.</summary>
        /// <param name="swipeData">Dữ liệu cú vuốt.</param>
        public void OnSwipeReceived(SwipeData swipeData)
        {
            if (playerController == null || swipeData == null) return;

            playerController.swipeData = swipeData;

            if (swipeData.SwipePhase != SwipePhase.End) return;

            playerController.isSwipeUpdated = true;

            if (paddleAligned && !isHitBallComplete) HitBall(swipeData);
        }

        /// <summary>Vợt đã xoay khớp hướng đón bóng — mở khoá cho cú đánh.</summary>
        public void OnPaddleAligned()
        {
            paddleAligned = true;
        }

        /// <summary>
        /// Nội suy <see cref="Time.timeScale"/> tới giá trị mong muốn bằng
        /// <see cref="Tweener.Value"/> ở chế độ unscaled (nếu dùng scaled time thì tween sẽ tự làm chậm chính nó).
        /// </summary>
        /// <param name="targetTimeScale">Time scale đích.</param>
        /// <param name="duration">Thời lượng chuyển tiếp tính bằng giây thực.</param>
        public void LerpTimeScale(float targetTimeScale, float duration)
        {
            if (playerController == null || !playerController.isActiveAndEnabled) return;

            if (timeScaleRoutine != null) playerController.StopCoroutine(timeScaleRoutine);

            timeScaleRoutine = playerController.StartCoroutine(
                Tweener.Value(Time.timeScale, targetTimeScale, duration,
                              value => Time.timeScale = value,
                              () => timeScaleRoutine = null,
                              true, Tweener.Linear));
        }

        /// <summary>Chọn và phát animation giơ vợt phù hợp với độ cao/bên của điểm đón bóng.</summary>
        private void PlayPreShotAnimation()
        {
            BasePlayerAnimationController animation = playerController.animationController;
            if (animation == null) return;

            Vector3 hitPoint = playerController.TargetBallHitPoint;
            float playerHeight = playerController.profile != null ? playerController.profile.height : 1.7f;
            ShotType shotType = playerController.isAttemptingVolley ? ShotType.Volley : ShotType.Groundstroke;

            ShotAnimationType animationType = animation.DetermineShotAnimation(
                shotType, playerController.GetShotSideForPoint(hitPoint), hitPoint.y, playerHeight);

            animation.PlayPreShot(animationType);
        }

        /// <summary>Bắt đầu xoay vợt về điểm đón bóng; không có <see cref="RacquetAnimator"/> thì dùng đồng hồ dự phòng.</summary>
        private void AlignPaddle()
        {
            if (racquetAnimator == null)
            {
                racquetAnimator = playerController.GetComponentInChildren<RacquetAnimator>(true);
            }

            if (racquetAnimator == null) return;

            racquetAnimator.AlignTo(playerController.TargetBallHitPoint, playerController.PaddleAlignSpeed,
                                    OnPaddleAligned);
        }

        /// <summary>Thực hiện cú đánh: chạy animation vung vợt rồi bắn bóng đúng khoảnh khắc chạm vợt.</summary>
        private void HitBall(SwipeData swipeData)
        {
            if (isHitBallComplete) return;
            isHitBallComplete = true;

            PlayerProfileProperties profile = playerController.profile;

            // playerDepth là TOẠ ĐỘ Z CÓ DẤU trong world space (mét) — xem BallController.HitBallServer.
            // Trước đây truyền giá trị chuẩn hoá 0..1 nên mọi cú đánh đều bị xếp loại "dink sát lưới".
            float playerDepth = playerController.transform.position.z;

            float swingAbility = profile != null ? profile.swingAbility : 0f;
            float spinAbility = profile != null ? profile.spinAbility : 0f;
            // Chỉ số thô (0..1) chỉ dùng cho tốc độ animation; lực đánh phải là hệ số quanh 1.0.
            float shotPowerStat = profile != null ? Mathf.Clamp01(profile.shotPower) : 0.5f;
            float shotPowerFactor = playerController.ShotPowerFactor; // TODO P05: nhân thêm hệ số booster.

            // Thoát slow-motion ngay khi vung vợt.
            LerpTimeScale(1f, SlowMotionBlendDuration);

            playerController.HitBallAnimation(shotPowerStat, () =>
            {
                playerController.HitBall(playerController.teamID, swipeData, playerDepth, swingAbility, spinAbility,
                                         shotPowerFactor);
                playerController.ChangeState(PlayerState.Idle);
            });
        }
    }
}
