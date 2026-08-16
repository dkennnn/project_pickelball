namespace Pickleball
{
    /// <summary>
    /// Trạng thái chờ của AI: KHÔNG làm gì cả.
    /// <para>
    /// AI đứng yên tại vị trí trung tâm chiến thuật mà <c>PickleballAIController.CheckAndRecenter</c>
    /// đã đặt sau cú đánh trước; việc chạy tới đó do <see cref="BasePlayerController.ApplyMovement"/> lo,
    /// còn việc rời trạng thái do sự kiện <see cref="BallController.OnBallHit"/> của đối thủ kích hoạt.
    /// </para>
    /// </summary>
    public class AIIdleState : IPlayerState
    {
        private readonly PickleballAIController aiController;

        /// <summary>Tạo trạng thái chờ cho một AI.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public AIIdleState(PickleballAIController controller)
        {
            aiController = controller;
        }

        /// <summary>Controller sở hữu trạng thái này.</summary>
        public PickleballAIController Controller => aiController;

        /// <summary>Không bật gì nên không cần khởi tạo.</summary>
        public void Enter()
        {
        }

        /// <summary>Không giữ tài nguyên nào nên không cần dọn.</summary>
        public void Exit()
        {
        }

        /// <summary>Không xử lý theo frame.</summary>
        public void Update()
        {
        }
    }
}
