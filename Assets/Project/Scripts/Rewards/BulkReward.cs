using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Phần thưởng theo lô: khi bốc trúng sẽ trả về một số lượng ngẫu nhiên
    /// trong khoảng [minQuantity, maxQuantity] (bao gồm cả hai đầu).
    /// </summary>
    [CreateAssetMenu(fileName = "BulkReward", menuName = "ScriptableObjects/Rewards/BulkReward", order = 1)]
    public class BulkReward : Reward
    {
        /// <summary>Số lượng tối thiểu nhận được.</summary>
        public int minQuantity;

        /// <summary>Số lượng tối đa nhận được.</summary>
        public int maxQuantity;

        /// <summary>Quay số lượng ngẫu nhiên trong [minQuantity, maxQuantity].</summary>
        public override int GetReward()
        {
            int min = Mathf.Min(minQuantity, maxQuantity);
            int max = Mathf.Max(minQuantity, maxQuantity);
            return Random.Range(min, max + 1);
        }
    }
}
