using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 7 — đánh có mục tiêu: hiện một vòng đích trên nửa sân đối phương và yêu cầu
    /// người chơi đánh bóng rơi vào đó <see cref="RequiredHits"/> lần (đổi bên trái/phải
    /// sau mỗi lần trúng).
    /// <para>
    /// Điểm rơi được xác định bằng sự kiện <see cref="BallController.OnBallCollision"/> với
    /// <see cref="BallController.CollisionType.Ground"/> cộng vị trí bóng tại thời điểm chạm đất
    /// (<see cref="GameManager.OnBallBounced"/> không phải event nên không đăng ký được).
    /// </para>
    /// </summary>
    public class TargetedHitTutorialState : BaseTutorialState
    {
        /// <summary>Số lần cần đánh trúng vòng đích.</summary>
        public const int RequiredHits = 2;

        /// <summary>Bán kính vòng đích tính bằng mét.</summary>
        public const float TargetRadius = 1.2f;

        private const string Message = "Aim for the ring. {0} target(s) to go.";

        private BallController subscribedBall;
        private Vector3 targetPosition;
        private bool targetingLeftSide;
        private bool playerHitSinceLastBounce;
        private int successfulHits;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public TargetedHitTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.TargetedHit;

        /// <summary>Số lần đã đánh trúng vòng đích.</summary>
        public int SuccessfulHits => successfulHits;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            successfulHits = 0;
            targetingLeftSide = true;
            playerHitSinceLastBounce = false;

            Subscribe();
            PlaceTarget();
            RefreshMessage();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            Unsubscribe();

            if (context != null && context.targetIndicator != null)
            {
                context.targetIndicator.gameObject.SetActive(false);
            }
        }

        private void Subscribe()
        {
            BallController.OnBallHit += HandleBallHit;

            BallController ball = BallRef;
            if (ball == null) return;

            subscribedBall = ball;
            subscribedBall.OnBallCollision += HandleBallCollision;
        }

        private void Unsubscribe()
        {
            BallController.OnBallHit -= HandleBallHit;

            if (subscribedBall != null) subscribedBall.OnBallCollision -= HandleBallCollision;
            subscribedBall = null;
        }

        private void HandleBallHit(string teamID, Vector3 shotLocation, ShotTrajetoryInfo trajectory)
        {
            if (string.IsNullOrEmpty(teamID)) return;

            playerHitSinceLastBounce = teamID == PlayerTeamID;
        }

        /// <summary>Bóng chạm đất: nếu là cú đánh của người chơi thì đo khoảng cách tới đích.</summary>
        /// <param name="collisionType">Loại va chạm vừa xảy ra.</param>
        private void HandleBallCollision(BallController.CollisionType collisionType)
        {
            if (IsCompleted) return;
            if (collisionType != BallController.CollisionType.Ground) return;
            if (!playerHitSinceLastBounce) return;

            playerHitSinceLastBounce = false;

            BallController ball = BallRef;
            if (ball == null) return;

            Vector3 landing = ball.transform.position;
            landing.y = 0f;

            Vector3 target = targetPosition;
            target.y = 0f;

            if (Vector3.Distance(landing, target) > TargetRadius) return;

            successfulHits++;

            if (successfulHits >= RequiredHits)
            {
                ShowMessage("Great aim! Placement wins points.");
                Complete();
                return;
            }

            targetingLeftSide = !targetingLeftSide;
            PlaceTarget();
            RefreshMessage();
        }

        /// <summary>
        /// Đặt vòng đích vào nửa trái/phải của nửa sân đối phương. Không có sân thì đặt tại
        /// gốc toạ độ để bước vẫn kết thúc được.
        /// </summary>
        private void PlaceTarget()
        {
            Court court = CourtRef;
            if (court == null)
            {
                targetPosition = Vector3.zero;
                return;
            }

            bool playerOnPositiveCourt = context != null && context.tutorialPlayer != null &&
                                         context.tutorialPlayer.IsPositiveCourt;

            CourtBounds opponentHalf = court.GetHalfCourtBounds(!playerOnPositiveCourt);

            float x = court.HalfWidth * 0.5f * (targetingLeftSide ? -1f : 1f);
            targetPosition = new Vector3(x, 0f, opponentHalf.center.z);

            if (context == null || context.targetIndicator == null) return;

            Transform indicator = context.targetIndicator;
            indicator.position = new Vector3(targetPosition.x, indicator.position.y, targetPosition.z);
            indicator.gameObject.SetActive(true);
        }

        private void RefreshMessage()
        {
            int remaining = Mathf.Max(0, RequiredHits - successfulHits);
            ShowMessage(string.Format(Message, remaining));
        }
    }
}
