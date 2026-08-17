using System;

namespace Pickleball
{
    /// <summary>
    /// Ảnh chụp phẳng của <see cref="PlayerProfileData"/> để ghi xuống đĩa.
    /// Tách riêng khỏi lớp runtime để định dạng file lưu không bị kéo theo mỗi lần
    /// <see cref="PlayerProfileData"/> thêm hàm hoặc tham chiếu mới.
    /// </summary>
    [Serializable]
    public class SavedPlayerData
    {
        /// <summary>Tên hiển thị của người chơi.</summary>
        public string playerName;

        /// <summary>Tổng số trận đã chơi.</summary>
        public int totalMatches;

        /// <summary>Tổng số trận thắng.</summary>
        public int totalWins;

        /// <summary>Tỉ lệ thắng (0..1).</summary>
        public float winRate;

        /// <summary>Chỉ số ảnh đại diện trong <c>GameData.avatarSprites</c>.</summary>
        public int avatarIndex;

        /// <summary>Tổng trophy, đồng thời là kinh nghiệm để tính level.</summary>
        public int trophies;

        /// <summary>Chuỗi trận thắng liên tiếp hiện tại.</summary>
        public int consecutiveWins;

        /// <summary>Level (league) hiện tại.</summary>
        public int level;

        /// <summary>Đã dùng lượt đổi tên miễn phí hay chưa.</summary>
        public bool hasUsedFreeRename;

        /// <summary>Khởi tạo rỗng phục vụ serialize.</summary>
        public SavedPlayerData() { }

        /// <summary>Khởi tạo đầy đủ mọi trường của hồ sơ đã lưu.</summary>
        /// <param name="playerName">Tên hiển thị.</param>
        /// <param name="totalMatches">Tổng số trận đã chơi.</param>
        /// <param name="totalWins">Tổng số trận thắng.</param>
        /// <param name="winRate">Tỉ lệ thắng (0..1).</param>
        /// <param name="avatarIndex">Chỉ số ảnh đại diện.</param>
        /// <param name="trophies">Tổng trophy.</param>
        /// <param name="consecutiveWins">Chuỗi thắng liên tiếp.</param>
        /// <param name="level">Level hiện tại.</param>
        /// <param name="hasUsedFreeRename">Đã dùng lượt đổi tên miễn phí hay chưa.</param>
        public SavedPlayerData(string playerName, int totalMatches, int totalWins, float winRate,
            int avatarIndex, int trophies, int consecutiveWins, int level, bool hasUsedFreeRename)
        {
            this.playerName = playerName;
            this.totalMatches = totalMatches;
            this.totalWins = totalWins;
            this.winRate = winRate;
            this.avatarIndex = avatarIndex;
            this.trophies = trophies;
            this.consecutiveWins = consecutiveWins;
            this.level = level;
            this.hasUsedFreeRename = hasUsedFreeRename;
        }

        /// <summary>
        /// Dựng lại đối tượng runtime <see cref="PlayerProfileData"/> từ dữ liệu đã lưu.
        /// Không bao giờ trả về <c>null</c>.
        /// </summary>
        public PlayerProfileData ToPlayerProfileData()
        {
            return new PlayerProfileData
            {
                playerName = playerName,
                totalMatches = totalMatches,
                totalWins = totalWins,
                winRate = winRate,
                avatarIndex = avatarIndex,
                trophies = trophies,
                consecutiveWins = consecutiveWins,
                level = level,
                hasUsedFreeRename = hasUsedFreeRename
            };
        }

        /// <summary>
        /// Chụp lại hồ sơ runtime thành dữ liệu lưu trữ.
        /// Trả về một bản rỗng (chứ không phải <c>null</c>) nếu <paramref name="profile"/> rỗng.
        /// </summary>
        /// <param name="profile">Hồ sơ người chơi đang chạy.</param>
        public static SavedPlayerData From(PlayerProfileData profile)
        {
            if (profile == null) return new SavedPlayerData();

            return new SavedPlayerData(
                profile.playerName,
                profile.totalMatches,
                profile.totalWins,
                profile.winRate,
                profile.avatarIndex,
                profile.trophies,
                profile.consecutiveWins,
                profile.level,
                profile.hasUsedFreeRename);
        }
    }
}
