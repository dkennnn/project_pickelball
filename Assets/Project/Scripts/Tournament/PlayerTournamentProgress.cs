using System;

namespace Pickleball
{
    /// <summary>
    /// Tiến độ của người chơi trong một giải đấu. Đây là phần dữ liệu được lưu/nạp
    /// giữa các phiên chơi.
    /// </summary>
    [Serializable]
    public class PlayerTournamentProgress
    {
        /// <summary>Id của giải đang tham gia; rỗng nghĩa là chưa vào giải nào.</summary>
        public string tournamentId;

        /// <summary>Vòng hiện tại (khớp <c>TournamentStage.stageNumber</c>, bắt đầu từ 0).</summary>
        public int currentStage;

        /// <summary>True khi người chơi đã thua và bị loại khỏi giải.</summary>
        public bool isEliminated;

        /// <summary>True khi người chơi đã vô địch giải.</summary>
        public bool isCompleted;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public PlayerTournamentProgress() { }

        /// <summary>Khởi tạo tiến độ mới cho một giải.</summary>
        /// <param name="tournamentId">Id giải đấu.</param>
        public PlayerTournamentProgress(string tournamentId)
        {
            Reset(tournamentId);
        }

        /// <summary>
        /// Đưa tiến độ về vòng đầu tiên của giải <paramref name="id"/>,
        /// xoá cờ bị loại và cờ vô địch.
        /// </summary>
        /// <param name="id">Id giải đấu; truyền chuỗi rỗng để xoá tiến độ.</param>
        public void Reset(string id)
        {
            tournamentId = id;
            currentStage = 0;
            isEliminated = false;
            isCompleted = false;
        }
    }
}
