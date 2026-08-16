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
        public const string AnimSpeed = "Speed";
        public const string AnimShotTrigger = "Shot";
        public const string AnimPreShotTrigger = "PreShot";
        public const string AnimServeTrigger = "Serve";
        public const string AnimVictoryTrigger = "Victory";
        public const string AnimShotTypeIndex = "ShotTypeIndex";
    }
}
