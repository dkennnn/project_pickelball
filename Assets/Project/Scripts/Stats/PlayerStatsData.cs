using System;

namespace Pickleball
{
    /// <summary>
    /// Gắn một bộ <see cref="StatsData"/> với định danh đội / người chơi sở hữu nó.
    /// </summary>
    [Serializable]
    public class PlayerStatsData
    {
        /// <summary>TeamID của người chơi (khớp <c>GameManager.player1TeamID</c> / <c>player2TeamID</c>).</summary>
        public string playerId;

        /// <summary>Bộ chỉ số của người chơi này. Không bao giờ null sau khi khởi tạo.</summary>
        public StatsData StatsData;

        /// <summary>Tạo bản ghi rỗng (dùng cho deserialize).</summary>
        public PlayerStatsData()
        {
            playerId = string.Empty;
            StatsData = new StatsData();
        }

        /// <summary>Tạo bản ghi cho một người chơi cụ thể.</summary>
        /// <param name="playerId">TeamID của người chơi.</param>
        public PlayerStatsData(string playerId)
        {
            this.playerId = playerId;
            StatsData = new StatsData();
        }
    }
}
