using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 5 — giải thích luật giao chéo sân: cú giao phải bay chéo vào ô giao đối diện
    /// và không được chạm vùng kitchen.
    /// </summary>
    public class CrossServeInfoTutorialState : BaseTutorialState
    {
        private const string Message =
            "Serves must travel diagonally into the opposite service box, clearing the Kitchen line.";

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public CrossServeInfoTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.CrossServeInfo;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            ShowMessage(Message);
            ShowCrossServeBoxHint();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (ElapsedTime >= FiveSeconds) Complete();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            if (context != null && context.targetIndicator != null)
            {
                context.targetIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Đặt vòng chỉ dẫn vào giữa ô giao chéo sân mà người chơi phải nhắm tới.
        /// Bỏ qua êm nếu chưa có sân hoặc chưa gán vòng chỉ dẫn.
        /// </summary>
        private void ShowCrossServeBoxHint()
        {
            if (context == null || context.targetIndicator == null) return;

            Court court = CourtRef;
            if (court == null) return;

            bool playerOnPositiveCourt = context.tutorialPlayer != null && context.tutorialPlayer.IsPositiveCourt;

            // Ô giao chéo sân = cùng ServeSide nhưng ở nửa sân đối diện (xem quy ước trong Court).
            ServeSide side = ServeSide.Right;
            GameManager gm = GameManagerRef;
            if (gm != null) side = gm.currentServeSide;

            CourtBounds box = court.GetServeBoxBounds(!playerOnPositiveCourt, side);

            Transform indicator = context.targetIndicator;
            indicator.position = new Vector3(box.center.x, indicator.position.y, box.center.z);
            indicator.gameObject.SetActive(true);
        }
    }
}
