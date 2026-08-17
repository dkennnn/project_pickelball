namespace Pickleball
{
    /// <summary>Danh mục 33 màn hình/popup do UIController quản lý.</summary>
    public enum ScreenType
    {
        None = 0, LoadingScreen = 1, TopNavigation = 2, MainMenu = 3, Settings = 4, Shop = 5,
        Profile = 6, Gameplay = 7, MatchMaking = 8, GameComplete = 9, IAPPurchase = 10, Blank = 11,
        DressingRoomUI = 12, CategoryDisplayUI = 13, ShopItemDetailUI = 14, DailyChallengeUI = 15,
        DailyRewardUI = 16, RewardUI = 17, LeagueUI = 18, AlertPopup = 19, NoInternetPopup = 20,
        ConfirmationPopup = 21, RewardPopup = 22, CustomizationUI = 23, LockerUI = 24,
        CollectionsUI = 25, TournamentUI = 26, TournamentEntryPopup = 27, LANCreationUI = 28,
        LANLobbyUI = 29, LANSelectionUI = 30, LoadingPopup = 31, ModeSelectionUI = 32,

        /// <summary>
        /// Bảng điểm bật lên giữa các pha bóng. Bản gốc có node "PointScoredUI" với script
        /// <c>ScoreboardPointsUI</c> nhưng KHÔNG có entry tương ứng trong enum.
        /// Thêm ở cuối để không đổi giá trị các mục cũ (save data phụ thuộc vào số).
        /// </summary>
        ScoreboardPoints = 33
    }
}
