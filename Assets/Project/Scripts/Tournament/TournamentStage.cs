using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>
    /// Một vòng đấu của giải: trận của người chơi và các trận AI đấu AI cùng vòng
    /// (dùng để vẽ đủ sơ đồ bracket).
    /// </summary>
    [Serializable]
    public class TournamentStage
    {
        /// <summary>Số thứ tự vòng, bắt đầu từ 0.</summary>
        public int stageNumber;

        /// <summary>Tên vòng hiển thị ("Quarter Finals", "Semi Finals", "Finals").</summary>
        public string stageName;

        /// <summary>Trận mà người chơi phải thắng để đi tiếp.</summary>
        public TournamentMatch playerMatch;

        /// <summary>Các trận AI đấu AI cùng vòng.</summary>
        public List<AIMatchResult> otherMatches = new List<AIMatchResult>();

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public TournamentStage() { }

        /// <summary>Khởi tạo một vòng đấu.</summary>
        /// <param name="stageNumber">Số thứ tự vòng (từ 0).</param>
        /// <param name="stageName">Tên vòng.</param>
        /// <param name="playerMatch">Trận của người chơi.</param>
        /// <param name="otherMatches">Các trận AI đấu AI cùng vòng.</param>
        public TournamentStage(int stageNumber, string stageName,
            TournamentMatch playerMatch, List<AIMatchResult> otherMatches)
        {
            this.stageNumber = stageNumber;
            this.stageName = stageName;
            this.playerMatch = playerMatch;
            this.otherMatches = otherMatches ?? new List<AIMatchResult>();
        }
    }
}
