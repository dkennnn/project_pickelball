using System;
using System.Collections.Generic;
using Pickleball.Data;

namespace Pickleball
{
    /// <summary>
    /// Toàn bộ tiến trình của người chơi được ghi xuống đĩa trong một file duy nhất.
    /// <para>
    /// Đây là ảnh chụp PHẲNG: không giữ tham chiếu tới bất kỳ ScriptableObject nào, mọi liên kết
    /// tới asset đều được khớp lại lúc nạp bằng khoá bền vững (tên vật phẩm, loại tab, bậc túi).
    /// Nhờ vậy file lưu vẫn đọc được sau khi thứ tự asset trong project thay đổi.
    /// </para>
    /// <para>
    /// Ngoài phạm vi (cố tình KHÔNG lưu): dữ liệu backend/PlayFab, trạng thái quảng cáo toàn cục,
    /// gói Remove Ads / IAP và cờ in-app review của bản gốc.
    /// </para>
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Phiên bản định dạng file lưu, dùng để migrate về sau.</summary>
        public int version = 1;

        /// <summary>True khi người chơi đã đi hết hướng dẫn.</summary>
        public bool isTutorialCompleted;

        /// <summary>Các bước hướng dẫn đã hoàn thành.</summary>
        public List<TutorialType> completedTutorialSteps;

        /// <summary>Số coin đang có.</summary>
        public int coins;

        /// <summary>Số gem đang có.</summary>
        public int gems;

        /// <summary>Trạng thái sở hữu từng skin bóng, theo thứ tự <c>Shop.ballsList</c>.</summary>
        public List<bool> ballPurchasedStatuses;

        /// <summary>Trạng thái sở hữu từng skin vợt, theo thứ tự <c>Shop.racketsList</c>.</summary>
        public List<bool> racketPurchasedStatuses;

        /// <summary>Chỉ số skin bóng đang trang bị.</summary>
        public int equipedBallIndex;

        /// <summary>Chỉ số skin vợt đang trang bị.</summary>
        public int equipedRacketIndex;

        /// <summary>Hồ sơ tiến trình của người chơi.</summary>
        public SavedPlayerData savedProfileData;

        /// <summary>Số nước tăng lực đang có.</summary>
        public int energyDrinks;

        /// <summary>Trạng thái các tab vật phẩm nâng cấp được.</summary>
        public List<ShopCategorySaveData> shopCategories;

        /// <summary>Kho tazo theo từng loại vật phẩm.</summary>
        public List<TazoCountData> tazoCounts;

        /// <summary>Kho booster theo từng loại.</summary>
        public List<BoosterCountData> boosters;

        /// <summary>Bộ nhiệm vụ hằng ngày kèm tiến độ.</summary>
        public List<DailyChallenge> dailyChallenges;

        /// <summary>Trạng thái đã nhận quà của từng mốc level, theo thứ tự <c>PlayerLevels.levels</c>.</summary>
        public List<bool> playerLevelsCollectStatuses;

        /// <summary>Trạng thái bộ đếm quảng cáo của từng bậc túi đồ.</summary>
        public List<KitbagSaveData> kitbagsData;

        /// <summary>Trạng thái từng ô locker.</summary>
        public List<SlotSaveData> slotDataArray;

        /// <summary>Mốc nhận quà hằng ngày gần nhất (ISO round-trip).</summary>
        public string lastDailyRewardTime;

        /// <summary>Mốc reset nhiệm vụ hằng ngày gần nhất (ISO round-trip).</summary>
        public string lastDailyChallengeResetTime;

        /// <summary>Tiến độ giải đấu hiện tại.</summary>
        public PlayerTournamentProgress tournamentProgress;

        /// <summary>Tay thuận người chơi đã chọn.</summary>
        public HandSide handSide;
    }
}
