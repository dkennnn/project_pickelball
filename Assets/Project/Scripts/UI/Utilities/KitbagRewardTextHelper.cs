using System.Collections.Generic;
using System.Text;

namespace Pickleball
{
    /// <summary>
    /// Sinh chuỗi mô tả nội dung một <see cref="Kitbag"/> để hiển thị trên thẻ bán túi
    /// và trong popup thông tin — ví dụ <c>"20-30 Tazos, 3-4 rewards"</c>.
    /// </summary>
    public static class KitbagRewardTextHelper
    {
        /// <summary>
        /// Chuỗi mô tả đầy đủ nội dung túi: khoảng tazo, số lượt bốc và các nhóm phần thưởng.
        /// </summary>
        /// <param name="kitbagData">Túi đồ cần mô tả; null trả về chuỗi rỗng.</param>
        public static string GetKitbagContentsText(Kitbag kitbagData)
        {
            if (kitbagData == null) return string.Empty;

            StringBuilder builder = new StringBuilder();

            string tazos = GetTazoRangeText(kitbagData);
            if (!string.IsNullOrEmpty(tazos)) Append(builder, tazos);

            string picks = GetPicksText(kitbagData);
            if (!string.IsNullOrEmpty(picks)) Append(builder, picks);

            return builder.ToString();
        }

        /// <summary>
        /// Chuỗi mô tả kèm thông tin cách mở túi — giữ tương đương chữ ký của bản gốc.
        /// </summary>
        /// <param name="kitbagData">Túi đồ cần mô tả; null trả về chuỗi rỗng.</param>
        /// <param name="isOpenedByAd">True khi túi đang được mở bằng quảng cáo.</param>
        /// <param name="totalAds">Tổng số quảng cáo cần xem để mở miễn phí.</param>
        /// <param name="costInCoins">Giá coin để mở ngay; 0 nghĩa là không bán bằng coin.</param>
        /// <param name="isKitbagInLocker">True khi túi đang nằm trong ô chờ mở của locker.</param>
        public static string GetKitbagContentsText(
            Kitbag kitbagData,
            bool isOpenedByAd,
            int totalAds = 1,
            int costInCoins = 0,
            bool isKitbagInLocker = false)
        {
            if (kitbagData == null) return string.Empty;

            StringBuilder builder = new StringBuilder(GetKitbagContentsText(kitbagData));

            string categories = GetRewardCategoriesText(kitbagData);
            if (!string.IsNullOrEmpty(categories)) Append(builder, categories);

            if (isOpenedByAd && totalAds > 0) Append(builder, "watch " + totalAds + (totalAds == 1 ? " ad" : " ads"));
            else if (costInCoins > 0) Append(builder, Utilities.FormatCount(costInCoins) + " coins");

            if (isKitbagInLocker) Append(builder, "in locker");

            return builder.ToString();
        }

        /// <summary>
        /// Khoảng tazo túi hứa hẹn, ví dụ <c>"20-30 Tazos"</c>.
        /// Trả về chuỗi rỗng khi túi không hứa tazo nào.
        /// </summary>
        /// <param name="kitbagData">Túi đồ cần mô tả; null trả về chuỗi rỗng.</param>
        public static string GetTazoRangeText(Kitbag kitbagData)
        {
            if (kitbagData == null) return string.Empty;

            int min = kitbagData.minTazosProvided;
            int max = kitbagData.maxTazosProvided;
            if (min > max) (min, max) = (max, min);
            if (max <= 0) return string.Empty;

            return FormatRange(min, max) + " Tazos";
        }

        /// <summary>
        /// Số lượt bốc của túi, ví dụ <c>"3-4 rewards"</c>.
        /// Trả về chuỗi rỗng khi túi không khai báo lượt bốc.
        /// </summary>
        /// <param name="kitbagData">Túi đồ cần mô tả; null trả về chuỗi rỗng.</param>
        public static string GetPicksText(Kitbag kitbagData)
        {
            if (kitbagData == null) return string.Empty;

            int min = kitbagData.minPicks;
            int max = kitbagData.maxPicks;
            if (min > max) (min, max) = (max, min);
            if (max <= 0) return string.Empty;

            return FormatRange(min, max) + (max == 1 ? " reward" : " rewards");
        }

        /// <summary>
        /// Liệt kê các nhóm phần thưởng có trong bảng thưởng của túi,
        /// ví dụ <c>"Tazos, Boosters, Cosmetics"</c>.
        /// </summary>
        /// <param name="kitbagData">Túi đồ cần mô tả; null trả về chuỗi rỗng.</param>
        public static string GetRewardCategoriesText(Kitbag kitbagData)
        {
            List<Reward> rewards = kitbagData != null ? kitbagData.rewards : null;
            if (rewards == null || rewards.Count == 0) return string.Empty;

            List<string> categories = new List<string>();

            for (int i = 0; i < rewards.Count; i++)
            {
                Reward reward = rewards[i];
                if (reward == null) continue;

                string category = GetRewardCategory(reward.rewardType);
                if (string.IsNullOrEmpty(category) || categories.Contains(category)) continue;

                categories.Add(category);
            }

            return string.Join(", ", categories);
        }

        /// <summary>Nhóm hiển thị của một loại phần thưởng.</summary>
        /// <param name="rewardType">Loại phần thưởng cần phân nhóm.</param>
        public static string GetRewardCategory(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Coins:
                case RewardType.Gems:
                    return "Currency";

                case RewardType.GripTazos:
                case RewardType.PaddleTazos:
                case RewardType.WorkoutTazos:
                case RewardType.CharacterTazos:
                    return "Tazos";

                case RewardType.StaminaBooster:
                case RewardType.SpeedBooster:
                case RewardType.SpinBooster:
                case RewardType.SwingBooster:
                case RewardType.PowerBooster:
                    return "Boosters";

                case RewardType.BallVisual:
                case RewardType.RacketVisual:
                case RewardType.StadiumVisual:
                    return "Cosmetics";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Tên ngắn của một loại phần thưởng, tách chữ hoa thành từ
        /// ("PaddleTazos" → "Paddle Tazos").
        /// </summary>
        /// <param name="rewardType">Loại phần thưởng cần đặt tên.</param>
        public static string GetRewardTypeShortName(RewardType rewardType)
        {
            string raw = rewardType.ToString();
            StringBuilder builder = new StringBuilder(raw.Length + 4);

            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i])) builder.Append(' ');
                builder.Append(raw[i]);
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private static string FormatRange(int min, int max)
        {
            if (min <= 0) min = max;
            return min == max ? min.ToString() : min + "-" + max;
        }

        private static void Append(StringBuilder builder, string part)
        {
            if (string.IsNullOrEmpty(part)) return;
            if (builder.Length > 0) builder.Append(", ");

            builder.Append(part);
        }
    }
}
