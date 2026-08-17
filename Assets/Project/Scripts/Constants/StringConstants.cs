namespace Pickleball
{
    /// <summary>Tag, layer và khoá remote-config dùng chung toàn game.</summary>
    public static class StringConstants
    {
        // --- Tags / Layers ---
        public const string NetTag = "Net";
        public const string GroundTag = "Ground";
        public const string BallTag = "Ball";
        public const string PlayerTag = "Player";
        public const string GroundLayer = "Ground";
        public const string BallLayer = "Ball";
        public const string PlayerLayer = "Player";

        // --- Remote config keys ---
        public const string AdRepeatCount = "AdRepeatCount";
        public const string MatchmakingGiveUpTime = "MatchmakingGiveUpTime";
        public const string MainMenuAdInterval = "MainMenuAdInterval";
        public const string StadiumIndices = "StadiumIndices";
        public const string InAppReviewDefaultCount = "InAppReviewDefaultCount";
        public const string Tournament = "Tournament";
        public const string EnableEventTheme = "enableEventTheme";
        public const string AppReleaseForceUpdateVersionAndroid = "AppReleaseForceUpdateVersionAndroid";
        public const string AppReleaseForceUpdateVersionIOS = "AppReleaseForceUpdateVersionIOS";
        public const string AppReleaseLatestVersionAndroid = "AppReleaseLatestVersionAndroid";
        public const string AppReleaseLatestVersionIOS = "AppReleaseLatestVersionIOS";
        public const string BannerAdProvider = "BannerAdProvider";
        public const string InterstitialAdProvider = "InterstitialAdProvider";
        public const string RewardedAdProvider = "RewardedAdProvider";
        public const string FallbackAdProvider = "FallbackAdProvider";

        // --- Matchmaking states ---
        public const string MatchmakingStarted = "MatchmakingStarted";
        public const string MatchmakingCanceled = "MatchmakingCanceled";
        public const string MatchmakingError = "MatchmakingError";
        public const string MatchmakingMatchFound = "MatchmakingMatchFound";
        public const string MatchmakingMatchNotFound = "MatchmakingMatchNotFound";
        public const string MatchmakingMatchFoundWithAI = "MatchmakingMatchFoundWithAI";

        // --- Animator parameters ---
        // Tên parameter LẤY TỪ ANIMATOR CONTROLLER GỐC (PlayerAnimationControllerv3 và
        // FemaleAnimationControllerv3 dùng bộ parameter giống hệt nhau). Sáu hằng số cũ
        // (Speed/Shot/PreShot/Serve/Victory/ShotTypeIndex) là tên tôi tự đặt và ĐỀU SAI —
        // nhân vật sẽ đứng im ở Idle vì mọi lệnh SetTrigger/SetFloat rơi vào hư không.

        /// <summary>Vận tốc ngang, dùng cho blend tree 2D di chuyển.</summary>
        public const string AnimXSpeed = "XSpeed";

        /// <summary>Vận tốc dọc, dùng cho blend tree 2D di chuyển.</summary>
        public const string AnimZSpeed = "ZSpeed";

        /// <summary>Int chọn kiểu đánh — giá trị khớp chính xác enum <c>ShotAnimationType</c>.</summary>
        public const string AnimShotTypeIndex = "HitType";

        /// <summary>Trigger vào tư thế chuẩn bị đánh.</summary>
        public const string AnimPreShotTrigger = "PreHit";

        /// <summary>Trigger đánh bóng.</summary>
        public const string AnimShotTrigger = "Hit";

        /// <summary>Trigger đánh hụt.</summary>
        public const string AnimMissTrigger = "Miss";

        /// <summary>Trigger đưa animator về trạng thái nghỉ.</summary>
        public const string AnimResetTrigger = "Reset";

        /// <summary>Bool chọn bên phải/trái — quyết định state giao bóng và hướng đánh.</summary>
        public const string AnimIsRightSide = "IsRightSide";

        /// <summary>Bool bật/tắt lớp animation khi đang trong trận.</summary>
        public const string AnimIsGameplayActive = "IsGameplayActive";

        /// <summary>Trigger ăn mừng thắng trận.</summary>
        public const string AnimWinTrigger = "Win";

        /// <summary>Trigger thua trận.</summary>
        public const string AnimLoseTrigger = "Lose";

        /// <summary>Trigger thắng một pha bóng.</summary>
        public const string AnimRallyWinTrigger = "RallyWin";

        /// <summary>Trigger thua một pha bóng.</summary>
        public const string AnimRallyLoseTrigger = "RallyLose";

        /// <summary>Bool lật animation cho tay thuận trái.</summary>
        public const string AnimMirrorHandSide = "MirrorHandSide";
    }
}
