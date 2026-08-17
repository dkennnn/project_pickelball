using System;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đã lưu của một bậc túi đồ: bộ đếm quảng cáo và các cờ hiển thị.
    /// Khớp với asset <see cref="Kitbag"/> theo <see cref="kitbagType"/>.
    /// </summary>
    [Serializable]
    public class KitbagSaveData
    {
        /// <summary>Bậc túi mà bản ghi này mô tả.</summary>
        public KitbagType kitbagType = KitbagType.None;

        /// <summary>Tổng số quảng cáo cần xem để mở miễn phí.</summary>
        public int totalAds;

        /// <summary>Số quảng cáo đã xem cho túi này.</summary>
        public int watchedAds;

        /// <summary>True khi túi đang cho phép mở bằng quảng cáo.</summary>
        public bool isAdAvailable;

        /// <summary>True khi phần thưởng của lần mở gần nhất đã được nhận.</summary>
        public bool isRewardCollected;

        /// <summary>True khi cần chèn quảng cáo xen kẽ sau lúc mở túi.</summary>
        public bool shouldShowInterstitial;

        /// <summary>Khởi tạo rỗng phục vụ serialize.</summary>
        public KitbagSaveData() { }

        /// <summary>Khởi tạo đầy đủ trạng thái lưu của một bậc túi.</summary>
        /// <param name="kitbagType">Bậc túi.</param>
        /// <param name="totalAds">Tổng số quảng cáo cần xem.</param>
        /// <param name="watchedAds">Số quảng cáo đã xem.</param>
        /// <param name="isAdAvailable">Túi đang mở được bằng quảng cáo hay không.</param>
        /// <param name="isRewardCollected">Phần thưởng đã được nhận hay chưa.</param>
        /// <param name="shouldShowInterstitial">Có cần chèn quảng cáo xen kẽ hay không.</param>
        public KitbagSaveData(KitbagType kitbagType, int totalAds, int watchedAds,
            bool isAdAvailable, bool isRewardCollected, bool shouldShowInterstitial)
        {
            this.kitbagType = kitbagType;
            this.totalAds = totalAds;
            this.watchedAds = watchedAds;
            this.isAdAvailable = isAdAvailable;
            this.isRewardCollected = isRewardCollected;
            this.shouldShowInterstitial = shouldShowInterstitial;
        }
    }
}
