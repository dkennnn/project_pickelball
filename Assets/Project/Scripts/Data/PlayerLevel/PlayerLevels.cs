using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bảng mốc level (league) của người chơi: ngưỡng kinh nghiệm và quà từng mốc.
    /// Trạng thái đã nhận quà được nạp/lưu qua <see cref="LoadCollectStatus"/> và <see cref="GetCollectStatuses"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerLevels", menuName = "ScriptableObjects/PlayerLevel/PlayerLevels")]
    public class PlayerLevels : ScriptableObject
    {
        /// <summary>Danh sách mốc level, sắp theo kinh nghiệm tăng dần.</summary>
        public List<PlayerLevelData> levels;

        /// <summary>
        /// Tìm level cao nhất mà người chơi đã đạt với số kinh nghiệm cho trước.
        /// Trả về 0 nếu chưa đạt mốc nào hoặc bảng rỗng.
        /// </summary>
        /// <param name="xp">Tổng kinh nghiệm (trophy) hiện có.</param>
        public int GetLevelForExperience(int xp)
        {
            if (levels == null) return 0;

            int result = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                PlayerLevelData data = levels[i];
                if (data == null) continue;
                if (xp >= data.experience && data.level > result) result = data.level;
            }
            return result;
        }

        /// <summary>Lấy dữ liệu của một mốc level. Trả về <c>null</c> nếu không tồn tại.</summary>
        /// <param name="level">Số hiệu level cần tra cứu.</param>
        public PlayerLevelData GetLevel(int level)
        {
            if (levels == null) return null;

            for (int i = 0; i < levels.Count; i++)
            {
                PlayerLevelData data = levels[i];
                if (data != null && data.level == level) return data;
            }
            return null;
        }

        /// <summary>
        /// Số trophy cần để lên level kế tiếp.
        /// Nếu đã ở mốc cuối, trả về ngưỡng của chính mốc cuối đó.
        /// </summary>
        /// <param name="currentLevel">Level hiện tại của người chơi.</param>
        public int GetNextLevelTrophies(int currentLevel)
        {
            PlayerLevelData next = GetLevel(currentLevel + 1);
            if (next != null) return next.experience;

            if (levels == null || levels.Count == 0) return 0;
            PlayerLevelData last = levels[levels.Count - 1];
            return last != null ? last.experience : 0;
        }

        /// <summary>Đánh dấu đã nhận quà của một level (không trả quà).</summary>
        /// <param name="level">Level cần đánh dấu.</param>
        public void MarkCollected(int level)
        {
            PlayerLevelData data = GetLevel(level);
            if (data != null) data.isRewardCollected = true;
        }

        /// <summary>
        /// Nhận quà của một level: đánh dấu đã nhận và trả về danh sách phần thưởng.
        /// Trả về danh sách rỗng nếu level không tồn tại hoặc đã nhận trước đó.
        /// Việc cộng quà vào tài khoản do tầng GameData xử lý.
        /// </summary>
        /// <param name="level">Level muốn nhận quà.</param>
        public List<DynamicReward> ClaimReward(int level)
        {
            List<DynamicReward> claimed = new List<DynamicReward>();

            PlayerLevelData data = GetLevel(level);
            if (data == null || data.isRewardCollected) return claimed;

            if (data.rewards != null)
            {
                for (int i = 0; i < data.rewards.Count; i++)
                {
                    DynamicReward reward = data.rewards[i];
                    if (reward != null) claimed.Add(reward);
                }
            }

            data.isRewardCollected = true;
            return claimed;
        }

        /// <summary>
        /// Nạp trạng thái đã nhận quà từ dữ liệu lưu trữ, theo thứ tự của <see cref="levels"/>.
        /// Phần tử thiếu sẽ được coi là chưa nhận.
        /// </summary>
        /// <param name="collectStatuses">Danh sách cờ đã nhận quà.</param>
        public void LoadCollectStatus(List<bool> collectStatuses)
        {
            if (levels == null) return;

            for (int i = 0; i < levels.Count; i++)
            {
                PlayerLevelData data = levels[i];
                if (data == null) continue;
                data.isRewardCollected = collectStatuses != null && i < collectStatuses.Count && collectStatuses[i];
            }
        }

        /// <summary>Xuất trạng thái đã nhận quà của mọi level, theo thứ tự của <see cref="levels"/>.</summary>
        public List<bool> GetCollectStatuses()
        {
            List<bool> statuses = new List<bool>();
            if (levels == null) return statuses;

            for (int i = 0; i < levels.Count; i++)
            {
                PlayerLevelData data = levels[i];
                statuses.Add(data != null && data.isRewardCollected);
            }
            return statuses;
        }
    }
}
