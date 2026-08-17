namespace Pickleball
{
    /// <summary>
    /// Bước 4 — giải thích tư thế đỡ giao bóng: đứng sau vạch cuối sân, chờ bóng nảy một lần
    /// rồi mới đánh trả (luật hai lần nảy).
    /// </summary>
    public class ReceiveInfoTutorialState : BaseTutorialState
    {
        private const string Message =
            "When receiving, stand behind the baseline and let the serve bounce once before you return it.";

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public ReceiveInfoTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.ReceiveInfo;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            ShowMessage(Message);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (ElapsedTime >= FiveSeconds) Complete();
        }
    }
}
