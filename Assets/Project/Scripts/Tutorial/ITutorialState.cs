namespace Pickleball
{
    /// <summary>
    /// Hợp đồng của một bước hướng dẫn. Máy trạng thái <see cref="TutorialStateContext"/>
    /// gọi <see cref="Enter"/> một lần khi bước bắt đầu, gọi <see cref="Update"/> mỗi khung hình
    /// và gọi <see cref="Exit"/> khi bước kết thúc.
    /// <para>
    /// Bước tự báo hoàn thành bằng cách bật <see cref="IsCompleted"/>; context sẽ phát hiện
    /// ở lần <see cref="TutorialStateContext.Tick"/> kế tiếp và chuyển sang bước sau.
    /// </para>
    /// </summary>
    public interface ITutorialState
    {
        /// <summary>Bước hướng dẫn mà state này phụ trách.</summary>
        TutorialType Type { get; }

        /// <summary>Bắt đầu bước: hiện thông điệp, chặn input, đăng ký sự kiện...</summary>
        void Enter();

        /// <summary>Dọn dẹp bước: huỷ đăng ký sự kiện, mở lại input, ẩn thông điệp.</summary>
        void Exit();

        /// <summary>Được gọi mỗi khung hình trong lúc bước đang chạy.</summary>
        void Update();

        /// <summary>True khi người chơi đã làm xong yêu cầu của bước.</summary>
        bool IsCompleted { get; }
    }
}
