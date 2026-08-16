namespace Pickleball
{
    /// <summary>
    /// Trạng thái chờ: tay vợt đứng yên ở vị trí đích hiện tại, không nhận swipe.
    /// Mọi việc di chuyển vẫn do <see cref="BasePlayerController.ApplyMovement"/> lo ở Update của controller.
    /// </summary>
    public class IdleState : IPlayerState
    {
        private readonly PickleballPlayerController playerController;

        /// <summary>Tạo trạng thái chờ cho một tay vợt.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public IdleState(PickleballPlayerController controller)
        {
            playerController = controller;
        }

        /// <summary>Vào trạng thái chờ: ngừng ý định volley và đưa animation về tốc độ di chuyển thật.</summary>
        public void Enter()
        {
            if (playerController == null) return;

            playerController.SetAttemptingVolley(false);
            playerController.HideDestinationMarker();
        }

        /// <summary>Không có gì phải dọn.</summary>
        public void Exit()
        {
        }

        /// <summary>Cập nhật thông số tốc độ cho animation blend tree.</summary>
        public void Update()
        {
            if (playerController == null || playerController.animationController == null) return;

            float speed = playerController.movementSpeed <= 0f
                ? 0f
                : (playerController.transform.position - playerController.GetTargetPosition()).magnitude > 0.05f ? 1f : 0f;

            playerController.animationController.SetMoveSpeed(speed);
        }
    }
}
