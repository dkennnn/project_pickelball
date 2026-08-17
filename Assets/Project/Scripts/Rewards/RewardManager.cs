using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Điểm vào duy nhất để quay thưởng trong game.
    /// Nhận cấu hình từ <see cref="rewardsCollection"/> rồi trả về danh sách
    /// <see cref="CollectibleReward"/> đã chốt số lượng, sẵn sàng cho UI hiển thị
    /// và cho <c>GameData</c> cộng vào tài khoản.
    /// </summary>
    public class RewardManager : Singleton<RewardManager>
    {
        /// <summary>Tập hợp bảng thưởng dùng cho toàn bộ game.</summary>
        public RewardsCollection rewardsCollection;

        /// <summary>Quay quà đăng nhập hằng ngày.</summary>
        /// <param name="picks">Số lượt bốc.</param>
        public List<CollectibleReward> GenerateDailyRewards(int picks)
        {
            if (rewardsCollection == null) return new List<CollectibleReward>();
            return ToCollectibles(rewardsCollection.GetRandomDailyRewards(picks));
        }

        /// <summary>Quay quà nhiệm vụ hằng ngày.</summary>
        /// <param name="picks">Số lượt bốc.</param>
        public List<CollectibleReward> GenerateDailyChallengeRewards(int picks)
        {
            if (rewardsCollection == null) return new List<CollectibleReward>();
            return ToCollectibles(rewardsCollection.GetRandomDailyChallengeRewards(picks));
        }

        /// <summary>Quay phần thưởng của túi đồ theo bậc.</summary>
        /// <param name="kitbagType">Bậc túi cần mở.</param>
        public List<CollectibleReward> GenerateKitbagRewards(KitbagType kitbagType)
        {
            if (rewardsCollection == null) return new List<CollectibleReward>();

            Kitbag kitbag = rewardsCollection.GetKitbag(kitbagType);
            if (kitbag == null)
            {
                Debug.LogWarning("[RewardManager] Không tìm thấy Kitbag cho bậc " + kitbagType);
                return new List<CollectibleReward>();
            }

            return ToCollectibles(rewardsCollection.GetRandomKitbagRewards(kitbag));
        }

        /// <summary>Quay phần thưởng của túi đồ khai báo động (ví dụ túi tutorial).</summary>
        /// <param name="kitbag">Asset túi động.</param>
        public List<CollectibleReward> GenerateKitbagRewards(DynamicKitbag kitbag)
        {
            if (kitbag == null) return new List<CollectibleReward>();
            return kitbag.Roll();
        }

        /// <summary>Chuyển danh sách asset phần thưởng thành phần thưởng đã chốt số lượng.</summary>
        private static List<CollectibleReward> ToCollectibles(List<Reward> rewards)
        {
            List<CollectibleReward> result = new List<CollectibleReward>();
            if (rewards == null) return result;

            for (int i = 0; i < rewards.Count; i++)
            {
                Reward reward = rewards[i];
                if (reward == null) continue;

                result.Add(new CollectibleReward(reward.rewardType, reward.GetReward()));
            }
            return result;
        }

        /// <summary>Kiểm tra nhanh bảng thưởng hằng ngày trong Inspector.</summary>
        [ContextMenu("Test Daily Rewards")]
        private void TestDailyRewards()
        {
            LogRewards("Daily", GenerateDailyRewards(3));
        }

        /// <summary>Kiểm tra nhanh bảng thưởng nhiệm vụ hằng ngày trong Inspector.</summary>
        [ContextMenu("Test Daily Challenge Rewards")]
        private void TestDailyChallengeRewards()
        {
            LogRewards("DailyChallenge", GenerateDailyChallengeRewards(3));
        }

        /// <summary>Kiểm tra nhanh bảng thưởng túi đồ bậc 1 trong Inspector.</summary>
        [ContextMenu("Test Kitbag Rewards")]
        private void TestKitbagRewards()
        {
            LogRewards("Kitbag Level1", GenerateKitbagRewards(KitbagType.Level1));
        }

        private static void LogRewards(string label, List<CollectibleReward> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                Debug.Log("[RewardManager] " + label + " -> " + rewards[i].Type + " x" + rewards[i].Amount);
            }
        }
    }
}
