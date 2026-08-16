using System;

namespace Pickleball
{
    /// <summary>
    /// Một phần thưởng đơn lẻ: loại phần thưởng kèm số lượng.
    /// Việc cộng thưởng vào tài khoản do tầng GameData xử lý ở bước sau.
    /// </summary>
    [Serializable]
    public class DynamicReward
    {
        /// <summary>Loại phần thưởng (tiền, gem, tazo, booster, vật phẩm hình ảnh...).</summary>
        public RewardType rewardType;

        /// <summary>Số lượng phần thưởng.</summary>
        public int value;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public DynamicReward() { }

        /// <summary>Khởi tạo phần thưởng với loại và số lượng cho trước.</summary>
        /// <param name="rewardType">Loại phần thưởng.</param>
        /// <param name="value">Số lượng.</param>
        public DynamicReward(RewardType rewardType, int value)
        {
            this.rewardType = rewardType;
            this.value = value;
        }
    }
}
