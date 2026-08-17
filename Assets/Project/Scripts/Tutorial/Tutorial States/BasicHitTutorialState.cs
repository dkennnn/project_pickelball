using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 6 — đánh trả cơ bản: người chơi phải vuốt đánh trả thành công
    /// <see cref="RequiredHits"/> lần. Đếm qua sự kiện tĩnh
    /// <see cref="BallController.OnBallHit"/>, lọc theo teamID của người chơi.
    /// </summary>
    public class BasicHitTutorialState : BaseTutorialState
    {
        /// <summary>Số cú đánh trả cần đạt để qua bước.</summary>
        public const int RequiredHits = 3;

        private const string Message = "Swipe to return the ball. Land {0} more shots.";

        private int successfulHits;
        private bool subscribed;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public BasicHitTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.BasicHit;

        /// <summary>Số cú đánh trả đã ghi nhận trong bước này.</summary>
        public int SuccessfulHits => successfulHits;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            successfulHits = 0;
            Subscribe();
            RefreshMessage();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed) return;

            BallController.OnBallHit += HandleBallHit;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;

            BallController.OnBallHit -= HandleBallHit;
            subscribed = false;
        }

        /// <summary>Ghi nhận một cú đánh của người chơi thật.</summary>
        /// <param name="teamID">Đội vừa đánh bóng.</param>
        /// <param name="shotLocation">Vị trí tiếp xúc.</param>
        /// <param name="trajectory">Thông tin quỹ đạo cú đánh.</param>
        private void HandleBallHit(string teamID, Vector3 shotLocation, ShotTrajetoryInfo trajectory)
        {
            if (IsCompleted) return;
            if (string.IsNullOrEmpty(teamID) || teamID != PlayerTeamID) return;

            successfulHits++;

            if (successfulHits >= RequiredHits)
            {
                ShowMessage("Nice! You've got the basics down.");
                Complete();
                return;
            }

            RefreshMessage();
        }

        private void RefreshMessage()
        {
            int remaining = Mathf.Max(0, RequiredHits - successfulHits);
            ShowMessage(string.Format(Message, remaining));
        }
    }
}
