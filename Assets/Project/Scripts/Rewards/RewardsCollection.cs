using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tập hợp toàn bộ bảng thưởng của game: quà đăng nhập hằng ngày,
    /// quà nhiệm vụ hằng ngày và danh sách túi đồ.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardsCollection", menuName = "ScriptableObjects/RewardsCollection", order = 1)]
    public class RewardsCollection : ScriptableObject
    {
        /// <summary>Bảng thưởng của quà đăng nhập hằng ngày.</summary>
        public List<Reward> dailyRewards;

        /// <summary>Bảng thưởng của nhiệm vụ hằng ngày.</summary>
        public List<Reward> dailyChallengeRewards;

        /// <summary>Danh sách túi đồ có trong game.</summary>
        public List<Kitbag> kitbags;

        /// <summary>Bốc <paramref name="picks"/> phần thưởng đăng nhập hằng ngày.</summary>
        /// <param name="picks">Số lượt bốc.</param>
        public List<Reward> GetRandomDailyRewards(int picks)
        {
            return GetRandomRewards(dailyRewards, picks);
        }

        /// <summary>Bốc <paramref name="picks"/> phần thưởng nhiệm vụ hằng ngày.</summary>
        /// <param name="picks">Số lượt bốc.</param>
        public List<Reward> GetRandomDailyChallengeRewards(int picks)
        {
            return GetRandomRewards(dailyChallengeRewards, picks);
        }

        /// <summary>Bốc phần thưởng cho một lần mở túi đồ.</summary>
        /// <param name="kitbag">Túi đồ cần mở.</param>
        public List<Reward> GetRandomKitbagRewards(Kitbag kitbag)
        {
            if (kitbag == null) return new List<Reward>();
            return kitbag.GetRandomKitbagRewards();
        }

        /// <summary>Tìm túi đồ theo bậc. Trả về <c>null</c> nếu chưa khai báo bậc đó.</summary>
        /// <param name="kitbagType">Bậc túi cần tìm.</param>
        public Kitbag GetKitbag(KitbagType kitbagType)
        {
            if (kitbags == null) return null;

            for (int i = 0; i < kitbags.Count; i++)
            {
                Kitbag kitbag = kitbags[i];
                if (kitbag != null && kitbag.kitbagType == kitbagType) return kitbag;
            }
            return null;
        }

        /// <summary>
        /// Bốc không hoàn lại theo trọng số <see cref="Reward.probability"/>.
        /// Nếu số lượt bốc lớn hơn số phần thưởng khác nhau thì bảng được nạp lại.
        /// </summary>
        private List<Reward> GetRandomRewards(List<Reward> source, int picks)
        {
            List<Reward> result = new List<Reward>();
            if (source == null || source.Count == 0 || picks <= 0) return result;

            List<Reward> pool = new List<Reward>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) pool.Add(source[i]);
            }
            if (pool.Count == 0) return result;

            for (int i = 0; i < picks; i++)
            {
                if (pool.Count == 0)
                {
                    for (int j = 0; j < source.Count; j++)
                    {
                        if (source[j] != null) pool.Add(source[j]);
                    }
                }

                Reward picked = Utilities.WeightedRandom(pool, r => r.probability);
                if (picked == null) break;

                result.Add(picked);
                pool.Remove(picked);
            }

            return result;
        }
    }
}
