using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một dòng trong bảng thưởng của <see cref="DynamicKitbag"/>.
    /// Khác với <see cref="Reward"/>, dòng này khai báo thẳng trong asset túi
    /// nên không cần tạo asset phần thưởng riêng cho từng loại.
    /// </summary>
    [System.Serializable]
    public class DynamicKitbagReward
    {
        /// <summary>Loại phần thưởng.</summary>
        public RewardType rewardType;

        /// <summary>Số lượng tối thiểu.</summary>
        public int minQuantity;

        /// <summary>Số lượng tối đa.</summary>
        public int maxQuantity;

        /// <summary>Trọng số bốc trúng, chuẩn hoá về [0,1].</summary>
        [Range(0f, 1f)]
        public float probability = 1f;

        /// <summary>Quay số lượng ngẫu nhiên trong [minQuantity, maxQuantity].</summary>
        public int GetRandomQuantity()
        {
            int min = Mathf.Min(minQuantity, maxQuantity);
            int max = Mathf.Max(minQuantity, maxQuantity);
            return Random.Range(min, max + 1);
        }
    }
}
