namespace Pickleball
{
    /// <summary>
    /// Hợp đồng của một trạng thái trong FSM của tay vợt.
    /// <para>
    /// Cố ý KHÔNG dùng <c>StarterKit.StateMachine.IState</c> để bám sát cấu trúc bản gốc:
    /// FSM của player được <see cref="BasePlayerController"/> tự quản bằng
    /// <c>Dictionary&lt;PlayerState, IPlayerState&gt;</c> chứ không qua FSMController.
    /// </para>
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>Được gọi một lần khi bước vào trạng thái.</summary>
        void Enter();

        /// <summary>Được gọi một lần khi rời khỏi trạng thái. Bắt buộc dọn sạch mọi thứ đã bật trong <see cref="Enter"/>.</summary>
        void Exit();

        /// <summary>Được gọi mỗi frame trong lúc trạng thái đang hoạt động.</summary>
        void Update();
    }
}
