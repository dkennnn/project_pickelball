using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hồ sơ tiến trình của người chơi: danh tính, thành tích và level (league).
    /// Đây là phần dữ liệu được lưu/nạp giữa các phiên chơi, khác với
    /// <see cref="PlayerProfile"/> (chỉ số vật lý dùng trong trận).
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        /// <summary>Tên hiển thị của người chơi.</summary>
        public string playerName;

        /// <summary>Tổng số trận đã chơi.</summary>
        public int totalMatches;

        /// <summary>Tổng số trận thắng.</summary>
        public int totalWins;

        /// <summary>Tỉ lệ thắng (0..1), luôn được tính lại từ <see cref="totalWins"/>/<see cref="totalMatches"/>.</summary>
        public float winRate;

        /// <summary>Chỉ số ảnh đại diện trong <c>GameData.avatarSprites</c>.</summary>
        public int avatarIndex;

        /// <summary>Tổng trophy, đồng thời là kinh nghiệm để tính level.</summary>
        public int trophies;

        /// <summary>Chuỗi trận thắng liên tiếp hiện tại.</summary>
        public int consecutiveWins;

        /// <summary>Level (league) hiện tại, suy ra từ <see cref="trophies"/>.</summary>
        public int level;

        /// <summary>Đã dùng lượt đổi tên miễn phí hay chưa.</summary>
        public bool hasUsedFreeRename;

        /// <summary>
        /// Ghi nhận kết quả một trận: cập nhật số trận, số thắng, tỉ lệ thắng,
        /// chuỗi thắng và trophy. Trophy không bao giờ xuống dưới 0.
        /// </summary>
        /// <param name="won">Người chơi thắng trận này hay không.</param>
        /// <param name="trophyDelta">Số trophy cộng thêm (âm khi thua).</param>
        public void RecordMatch(bool won, int trophyDelta)
        {
            totalMatches++;

            if (won)
            {
                totalWins++;
                consecutiveWins++;
            }
            else
            {
                consecutiveWins = 0;
            }

            winRate = totalMatches > 0 ? (float)totalWins / totalMatches : 0f;
            trophies = Mathf.Max(0, trophies + trophyDelta);
        }

        /// <summary>
        /// Tính lại <see cref="level"/> từ <see cref="trophies"/> theo bảng mốc level.
        /// Không làm gì nếu bảng mốc chưa được gán.
        /// </summary>
        /// <param name="levels">Bảng mốc level của game.</param>
        public void RecalculateLevel(PlayerLevels levels)
        {
            if (levels == null) return;
            level = levels.GetLevelForExperience(trophies);
        }
    }
}
