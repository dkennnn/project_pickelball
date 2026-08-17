using System;

namespace Pickleball
{
    /// <summary>
    /// Một phần thưởng đã được quay xong (đã chốt số lượng) đang chờ người chơi nhận.
    /// Khác với <see cref="Reward"/> là asset cấu hình, đây là dữ liệu runtime có thể serialize
    /// để lưu lại giữa các phiên chơi.
    /// </summary>
    [Serializable]
    public class CollectibleReward
    {
        /// <summary>Số lượng đã chốt của phần thưởng này.</summary>
        public int Amount;

        /// <summary>Loại phần thưởng sẽ được cộng vào tài khoản.</summary>
        public RewardType Type;

        /// <summary>True khi phần thưởng đã được cộng vào tài khoản, tránh nhận trùng.</summary>
        public bool isCollected;

        /// <summary>Khởi tạo phần thưởng chờ nhận với loại và số lượng cho trước.</summary>
        /// <param name="type">Loại phần thưởng.</param>
        /// <param name="amount">Số lượng đã quay được.</param>
        public CollectibleReward(RewardType type, int amount)
        {
            Type = type;
            Amount = amount;
            isCollected = false;
        }

        /// <summary>
        /// Cộng phần thưởng vào dữ liệu người chơi rồi đánh dấu đã nhận.
        /// Gọi lại lần hai sẽ không cộng thêm.
        /// </summary>
        /// <param name="gameData">Dữ liệu người chơi nhận thưởng.</param>
        public void GrantReward(GameData gameData)
        {
            if (isCollected || gameData == null) return;

            gameData.GrantReward(Type, Amount);
            isCollected = true;
        }
    }
}
