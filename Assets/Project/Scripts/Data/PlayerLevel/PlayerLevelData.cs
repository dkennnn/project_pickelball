using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>Một mốc level: ngưỡng kinh nghiệm (trophy) cần đạt và gói quà đi kèm.</summary>
    [Serializable]
    public class PlayerLevelData
    {
        /// <summary>Số hiệu level.</summary>
        public int level;

        /// <summary>Ngưỡng kinh nghiệm (trophy) tối thiểu để đạt level này.</summary>
        public int experience;

        /// <summary>Danh sách phần thưởng nhận được khi đạt level.</summary>
        public List<DynamicReward> rewards;

        /// <summary>Đã nhận quà của level này hay chưa.</summary>
        public bool isRewardCollected;
    }
}
