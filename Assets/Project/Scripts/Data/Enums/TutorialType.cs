namespace Pickleball
{
    /// <summary>Giá trị số giữ nguyên như bản gốc để save data cũ vẫn đọc được.</summary>
    public enum TutorialType
    {
        None = 0,
        MainMenuForcedPlay = 2,
        WelcomeMessage = 3,
        CourtSidesAndKitchenInfo = 4,
        ReceiveInfo = 5,
        CrossServeInfo = 6,
        StartTutorialMatch = 7,
        BasicHit = 10,
        TargetedHit = 11,
        KitchenHit = 12,
        TwoPointEasyBotMatch = 13,
        FirstMatch = 14,
        TutorialKitbagReward = 15,
        ForcedGripUpgrade = 16,
        SkillsTutorial = 20,
        TutorialCompleted = 999
    }
}
