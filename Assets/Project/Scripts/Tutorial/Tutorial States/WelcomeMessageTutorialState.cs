namespace Pickleball
{
    /// <summary>
    /// Bước 2 — lời chào mở đầu. Chỉ hiện một thông điệp rồi tự hoàn thành sau
    /// <see cref="BaseTutorialState.ThreeSeconds"/> giây.
    /// </summary>
    public class WelcomeMessageTutorialState : BaseTutorialState
    {
        private const string Message = "Welcome! Let's learn Pickleball basics.";

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public WelcomeMessageTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.WelcomeMessage;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            ShowMessage(Message);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (ElapsedTime >= ThreeSeconds) Complete();
        }
    }
}
