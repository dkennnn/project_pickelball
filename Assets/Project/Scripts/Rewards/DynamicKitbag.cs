using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Túi đồ khai báo bảng thưởng ngay trong asset, không cần tạo asset
    /// <see cref="Reward"/> riêng cho từng dòng. Dùng cho các túi kịch bản như
    /// túi thưởng tutorial.
    /// </summary>
    [CreateAssetMenu(fileName = "DynamicKitbag", menuName = "ScriptableObjects/DynamicKitbag", order = 1)]
    public class DynamicKitbag : ScriptableObject
    {
        /// <summary>Bậc của túi.</summary>
        public KitbagType kitbagType;

        /// <summary>Tên hiển thị của túi.</summary>
        public string kitbagName;

        /// <summary>Số lượt bốc tối thiểu.</summary>
        public int minPicks;

        /// <summary>Số lượt bốc tối đa.</summary>
        public int maxPicks;

        /// <summary>Bảng thưởng khai báo trực tiếp trong asset.</summary>
        public List<DynamicKitbagReward> rewards;

        /// <summary>
        /// Phần thưởng đặc biệt gắn kèm túi. Mỗi phần thưởng ở đây được quay riêng
        /// một lần theo <see cref="Reward.probability"/> và nhiều nhất chỉ rơi một lần,
        /// nằm ngoài số lượt bốc của <see cref="rewards"/>.
        /// </summary>
        public List<Reward> uniqueRewards;

        /// <summary>
        /// Quay toàn bộ phần thưởng cho một lần mở túi và trả về danh sách chờ nhận.
        /// </summary>
        public List<CollectibleReward> Roll()
        {
            List<CollectibleReward> result = new List<CollectibleReward>();

            if (rewards != null && rewards.Count > 0)
            {
                int min = Mathf.Max(0, Mathf.Min(minPicks, maxPicks));
                int max = Mathf.Max(minPicks, maxPicks);
                int picks = Random.Range(min, max + 1);

                List<DynamicKitbagReward> pool = new List<DynamicKitbagReward>();
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i] != null) pool.Add(rewards[i]);
                }

                for (int i = 0; i < picks && pool.Count > 0; i++)
                {
                    DynamicKitbagReward picked = Utilities.WeightedRandom(pool, r => r.probability);
                    if (picked == null) break;

                    result.Add(new CollectibleReward(picked.rewardType, picked.GetRandomQuantity()));
                    pool.Remove(picked);

                    // Hết dòng riêng biệt thì nạp lại bảng cho các lượt còn dư.
                    if (pool.Count == 0 && i + 1 < picks)
                    {
                        for (int j = 0; j < rewards.Count; j++)
                        {
                            if (rewards[j] != null) pool.Add(rewards[j]);
                        }
                    }
                }
            }

            if (uniqueRewards != null)
            {
                for (int i = 0; i < uniqueRewards.Count; i++)
                {
                    Reward unique = uniqueRewards[i];
                    if (unique == null) continue;
                    if (Random.value > unique.probability) continue;

                    result.Add(new CollectibleReward(unique.rewardType, unique.GetReward()));
                }
            }

            return result;
        }
    }
}
