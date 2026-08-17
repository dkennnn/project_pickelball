using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 8 — cú dink vào kitchen: người chơi phải đánh bóng rơi vào vùng cấm volley của
    /// đối phương <see cref="RequiredKitchenHits"/> lần. Điểm rơi kiểm bằng
    /// <see cref="Court.IsInKitchen"/> tại thời điểm bóng chạm đất.
    /// </summary>
    public class KitchenHitTutorialState : BaseTutorialState
    {
        /// <summary>Số lần bóng phải rơi vào kitchen.</summary>
        public const int RequiredKitchenHits = 2;

        private const string Message = "Drop the ball softly into the Kitchen. {0} to go.";

        private BallController subscribedBall;
        private bool playerHitSinceLastBounce;
        private int successfulKitchenHits;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public KitchenHitTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.KitchenHit;

        /// <summary>Số cú rơi vào kitchen đã ghi nhận.</summary>
        public int SuccessfulKitchenHits => successfulKitchenHits;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            successfulKitchenHits = 0;
            playerHitSinceLastBounce = false;

            Subscribe();
            ShowKitchenHighlight();
            RefreshMessage();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            Unsubscribe();

            if (context != null && context.kitchenHighlight != null)
            {
                context.kitchenHighlight.SetActive(false);
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

        /// <summary>Bóng chạm đất sau cú đánh của người chơi: kiểm xem có nằm trong kitchen không.</summary>
        /// <param name="collisionType">Loại va chạm vừa xảy ra.</param>
        private void HandleBallCollision(BallController.CollisionType collisionType)
        {
            if (IsCompleted) return;
            if (collisionType != BallController.CollisionType.Ground) return;
            if (!playerHitSinceLastBounce) return;

            playerHitSinceLastBounce = false;

            Court court = CourtRef;
            BallController ball = BallRef;
            if (court == null || ball == null) return;

            Vector3 landing = ball.transform.position;
            if (!court.IsInKitchen(landing)) return;

            // Phải rơi ở nửa sân đối phương, không phải kitchen bên mình.
            bool playerOnPositiveCourt = context != null && context.tutorialPlayer != null &&
                                         context.tutorialPlayer.IsPositiveCourt;
            bool landedOnPositiveCourt = landing.z > 0f;
            if (landedOnPositiveCourt == playerOnPositiveCourt) return;

            successfulKitchenHits++;

            if (successfulKitchenHits >= RequiredKitchenHits)
            {
                ShowMessage("Perfect dink! That's how you win the Kitchen battle.");
                Complete();
                return;
            }

            RefreshMessage();
        }

        /// <summary>Bật khối highlight kitchen nếu đã gán trong context.</summary>
        private void ShowKitchenHighlight()
        {
            if (context == null || context.kitchenHighlight == null) return;

            Court court = CourtRef;
            if (court != null)
            {
                bool playerOnPositiveCourt = context.tutorialPlayer != null &&
                                             context.tutorialPlayer.IsPositiveCourt;

                CourtBounds bounds = court.GetKitchenBounds(!playerOnPositiveCourt);

                Transform t = context.kitchenHighlight.transform;
                t.position = new Vector3(bounds.center.x, t.position.y, bounds.center.z);

                Vector2 size = bounds.Size;
                t.localScale = new Vector3(size.x, t.localScale.y, size.y);
            }

            context.kitchenHighlight.SetActive(true);
        }

        private void RefreshMessage()
        {
            int remaining = Mathf.Max(0, RequiredKitchenHits - successfulKitchenHits);
            ShowMessage(string.Format(Message, remaining));
        }
    }
}
