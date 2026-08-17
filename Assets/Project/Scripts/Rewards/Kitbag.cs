using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Túi đồ (loot box) của game: mô tả giá mở, bảng thưởng và số lượt bốc.
    /// Mỗi lần mở sẽ bốc từ <see cref="rewards"/> theo trọng số <see cref="Reward.probability"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Kitbag", menuName = "ScriptableObjects/Kitbag", order = 1)]
    public class Kitbag : ScriptableObject
    {
        /// <summary>Bậc của túi, dùng để tra cứu thời gian mở khoá trong locker.</summary>
        public KitbagType kitbagType;

        /// <summary>Tên hiển thị của túi.</summary>
        public string kitbagName;

        /// <summary>Ảnh của túi trên UI locker/shop.</summary>
        public Sprite kitbagImage;

        /// <summary>Giá mua ngay bằng coin. 0 nghĩa là không bán bằng coin.</summary>
        public int costInCoins;

        /// <summary>Giá mua ngay bằng gem. 0 nghĩa là không bán bằng gem.</summary>
        public int costInGems;

        /// <summary>Số tazo tối thiểu túi hứa hẹn (dùng để hiển thị "20-30 tazos").</summary>
        public int minTazosProvided;

        /// <summary>Số tazo tối đa túi hứa hẹn.</summary>
        public int maxTazosProvided;

        /// <summary>Số lượt bốc tối thiểu mỗi lần mở túi.</summary>
        public int minPicks;

        /// <summary>Số lượt bốc tối đa mỗi lần mở túi.</summary>
        public int maxPicks;

        /// <summary>Bảng thưởng của túi.</summary>
        public List<Reward> rewards;

        /// <summary>Tổng số quảng cáo cần xem để mở miễn phí.</summary>
        [Tooltip("Tổng số quảng cáo cần xem để mở miễn phí")]
        public int totalAds;

        /// <summary>Số quảng cáo người chơi đã xem cho túi này.</summary>
        public int watchedAds;

        /// <summary>True khi túi đang cho phép mở bằng quảng cáo.</summary>
        public bool isAdAvailable;

        /// <summary>True khi phần thưởng của lần mở gần nhất đã được nhận.</summary>
        public bool isRewardCollected;

        /// <summary>True khi cần chèn quảng cáo xen kẽ sau lúc mở túi.</summary>
        public bool shouldShowInterstitial;

        /// <summary>Ảnh minh hoạ các phần thưởng tiêu biểu hiển thị trên thẻ túi.</summary>
        public List<Sprite> rewardShowcaseImages;

        /// <summary>
        /// Bốc phần thưởng cho một lần mở túi. Số lượt bốc nằm trong [minPicks, maxPicks],
        /// mỗi lượt chọn theo trọng số <see cref="Reward.probability"/> và không lấy trùng
        /// một asset phần thưởng cho tới khi hết lựa chọn.
        /// </summary>
        public List<Reward> GetRandomKitbagRewards()
        {
            List<Reward> result = new List<Reward>();
            if (rewards == null || rewards.Count == 0) return result;

            int min = Mathf.Max(0, Mathf.Min(minPicks, maxPicks));
            int max = Mathf.Max(minPicks, maxPicks);
            int picks = Random.Range(min, max + 1);
            if (picks <= 0) return result;

            // Bốc không hoàn lại để một lần mở không ra nhiều thẻ trùng nhau.
            List<Reward> pool = new List<Reward>();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] != null) pool.Add(rewards[i]);
            }
            if (pool.Count == 0) return result;

            for (int i = 0; i < picks; i++)
            {
                // Hết lựa chọn riêng biệt thì nạp lại bảng thưởng cho các lượt còn dư.
                if (pool.Count == 0)
                {
                    for (int j = 0; j < rewards.Count; j++)
                    {
                        if (rewards[j] != null) pool.Add(rewards[j]);
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
