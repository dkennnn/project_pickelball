using System;

namespace Pickleball
{
    /// <summary>
    /// Một trận của người chơi trong giải đấu: đối thủ AI, độ khó và sân thi đấu.
    /// </summary>
    [Serializable]
    public class TournamentMatch
    {
        /// <summary>Số hiệu trận trong giải (1 = trận đầu tiên).</summary>
        public int matchId;

        /// <summary>Tên hiển thị của đối thủ AI.</summary>
        public string opponentName;

        /// <summary>Chỉ số ảnh đại diện của đối thủ trong <c>GameData.avatarSprites</c>.</summary>
        public int opponentAvatarIndex;

        /// <summary>Độ khó của AI trong trận này.</summary>
        public AIDifficulty difficulty;

        /// <summary>Số trophy hiển thị của đối thủ (chỉ để trưng bày trên UI).</summary>
        public int opponentTrophies;

        /// <summary>Tên sân đấu diễn ra trận; rỗng nghĩa là dùng sân theo level người chơi.</summary>
        public string stadiumName;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public TournamentMatch() { }

        /// <summary>Khởi tạo một trận đấu giải với đầy đủ thông tin đối thủ.</summary>
        /// <param name="matchId">Số hiệu trận.</param>
        /// <param name="opponentName">Tên đối thủ.</param>
        /// <param name="opponentAvatarIndex">Chỉ số ảnh đại diện của đối thủ.</param>
        /// <param name="difficulty">Độ khó AI.</param>
        /// <param name="opponentTrophies">Trophy hiển thị của đối thủ.</param>
        /// <param name="stadiumName">Tên sân thi đấu.</param>
        public TournamentMatch(int matchId, string opponentName, int opponentAvatarIndex,
            AIDifficulty difficulty, int opponentTrophies, string stadiumName)
        {
            this.matchId = matchId;
            this.opponentName = opponentName;
            this.opponentAvatarIndex = opponentAvatarIndex;
            this.difficulty = difficulty;
            this.opponentTrophies = opponentTrophies;
            this.stadiumName = stadiumName;
        }
    }
}
