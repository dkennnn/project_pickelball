using System;
using System.Collections.Generic;
using System.Globalization;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quản lý quà đăng nhập hằng ngày: mỗi ngày (theo lịch UTC) người chơi được bốc
    /// <see cref="picksPerDay"/> phần thưởng từ bảng quà hằng ngày.
    /// <para>
    /// Mốc nhận quà gần nhất được lưu nội bộ dạng chuỗi ISO round-trip ("o") để hệ save
    /// đọc/ghi qua <see cref="GetLastClaimTimeString"/> và <see cref="SetLastClaimTimeString"/>.
    /// Việc so sánh luôn theo NGÀY UTC: qua 00:00 là có lượt mới.
    /// </para>
    /// </summary>
    public class DailyRewardManager : Singleton<DailyRewardManager>
    {
        /// <summary>Dữ liệu người chơi nhận quà.</summary>
        public GameData gameData;

        /// <summary>Bảng quà dự phòng khi scene chưa có <see cref="RewardManager"/>.</summary>
        public RewardsCollection rewardsCollection;

        /// <summary>Số lượt bốc quà mỗi ngày.</summary>
        public int picksPerDay = 3;

        /// <summary>Phát khi người chơi vừa nhận xong quà hằng ngày, tham số là danh sách quà đã cấp.</summary>
        public static event Action<List<CollectibleReward>> OnDailyRewardReady;

        /// <summary>Mốc nhận quà gần nhất dạng ISO round-trip; rỗng nghĩa là chưa từng nhận.</summary>
        private string lastClaimTimeString = string.Empty;

        /// <summary>Định dạng ISO round-trip dùng cho mốc thời gian lưu trữ.</summary>
        private const string IsoFormat = "o";

        /// <summary>
        /// True nếu người chơi chưa nhận quà trong ngày UTC hiện tại.
        /// </summary>
        public bool IsDailyRewardAvailable()
        {
            // TODO: đổi sang server time khi có backend.
            if (!TryGetLastClaimTime(out DateTime lastClaim)) return true;
            return DateTime.UtcNow.Date > lastClaim.Date;
        }

        /// <summary>
        /// Nhận quà hằng ngày: quay <see cref="picksPerDay"/> phần thưởng, cộng thẳng vào
        /// <see cref="gameData"/>, ghi lại mốc thời gian và phát <see cref="OnDailyRewardReady"/>.
        /// Trả về danh sách rỗng nếu hôm nay đã nhận rồi.
        /// </summary>
        public List<CollectibleReward> ClaimDailyReward()
        {
            if (!IsDailyRewardAvailable()) return new List<CollectibleReward>();

            List<CollectibleReward> rewards = RollRewards();

            for (int i = 0; i < rewards.Count; i++)
            {
                rewards[i]?.GrantReward(gameData);
            }

            // TODO: đổi sang server time khi có backend.
            lastClaimTimeString = DateTime.UtcNow.ToString(IsoFormat, CultureInfo.InvariantCulture);

            OnDailyRewardReady?.Invoke(rewards);
            return rewards;
        }

        /// <summary>
        /// Thời gian còn lại tới lượt quà kế tiếp (mốc 00:00 UTC).
        /// Trả về <see cref="TimeSpan.Zero"/> khi quà đang sẵn sàng.
        /// </summary>
        public TimeSpan GetTimeUntilNextReward()
        {
            if (IsDailyRewardAvailable()) return TimeSpan.Zero;

            DateTime now = DateTime.UtcNow;
            TimeSpan remaining = now.Date.AddDays(1) - now;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>Mốc nhận quà gần nhất dạng chuỗi ISO round-trip, để hệ save ghi xuống đĩa.</summary>
        public string GetLastClaimTimeString()
        {
            return lastClaimTimeString ?? string.Empty;
        }

        /// <summary>Nạp mốc nhận quà gần nhất từ chuỗi ISO round-trip do hệ save cung cấp.</summary>
        /// <param name="value">Chuỗi ISO; rỗng hoặc sai định dạng sẽ coi như chưa từng nhận.</param>
        public void SetLastClaimTimeString(string value)
        {
            lastClaimTimeString = TryParseIso(value, out DateTime _) ? value : string.Empty;
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>
        /// Quay danh sách quà: ưu tiên <see cref="RewardManager"/>, nếu chưa có thì
        /// quay trực tiếp từ <see cref="rewardsCollection"/>.
        /// </summary>
        private List<CollectibleReward> RollRewards()
        {
            int picks = Mathf.Max(1, picksPerDay);

            if (RewardManager.HasInstance)
            {
                List<CollectibleReward> rolled = RewardManager.Instance.GenerateDailyRewards(picks);
                if (rolled != null && rolled.Count > 0) return rolled;
            }

            List<CollectibleReward> fallback = new List<CollectibleReward>();
            if (rewardsCollection == null)
            {
                Debug.LogWarning("[DailyRewardManager] Chưa có RewardManager lẫn RewardsCollection, không quay được quà.");
                return fallback;
            }

            List<Reward> source = rewardsCollection.GetRandomDailyRewards(picks);
            if (source == null) return fallback;

            for (int i = 0; i < source.Count; i++)
            {
                Reward reward = source[i];
                if (reward == null) continue;
                fallback.Add(new CollectibleReward(reward.rewardType, reward.GetReward()));
            }
            return fallback;
        }

        /// <summary>Đọc mốc nhận quà gần nhất về giờ UTC; false nếu chưa từng nhận.</summary>
        private bool TryGetLastClaimTime(out DateTime lastClaim)
        {
            return TryParseIso(lastClaimTimeString, out lastClaim);
        }

        /// <summary>Phân tích chuỗi ISO round-trip thành thời điểm UTC.</summary>
        private static bool TryParseIso(string value, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(value)) return false;

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return false;
            }

            utc = parsed.ToUniversalTime();
            return true;
        }
    }
}
