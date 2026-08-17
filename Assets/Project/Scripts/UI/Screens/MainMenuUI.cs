using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn hình chính: nút Play theo chế độ đang chọn, thông tin hồ sơ (avatar, level, trophy),
    /// ví coin/gem và toàn bộ cửa ngõ đi các màn khác. Ba nút Locker / Challenge / DailyReward
    /// có chấm báo khi đang có thứ nhận được.
    /// </summary>
    public class MainMenuUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.MainMenu;

        // --- Nút hàng dưới ---

        /// <summary>Nút mở màn tuỳ biến bóng/vợt.</summary>
        [SerializeField] private Button customizeButton;

        /// <summary>Nút mở cửa hàng.</summary>
        [SerializeField] private Button shopButton;

        /// <summary>Nút mở locker.</summary>
        [SerializeField] private Button lockerButton;

        /// <summary>Nút mở phòng thay đồ.</summary>
        [SerializeField] private Button roomButton;

        /// <summary>Nút mở màn League.</summary>
        [SerializeField] private Button leagueButton;

        // --- Nút nổi ---

        /// <summary>Nút mở màn nhiệm vụ hằng ngày.</summary>
        [SerializeField] private Button dailyChallengeButton;

        /// <summary>Nút mở màn bộ sưu tập.</summary>
        [SerializeField] private Button collectionsButton;

        /// <summary>Nút mở màn chọn chế độ chơi.</summary>
        [SerializeField] private Button modeSelectionBtn;

        /// <summary>Nút mở màn quà hằng ngày.</summary>
        [SerializeField] private Button dailyRewardButton;

        /// <summary>Nút mở màn cài đặt.</summary>
        [SerializeField] private Button optionsButton;

        /// <summary>Nút bắt đầu tìm trận.</summary>
        [SerializeField] private Button play;

        /// <summary>Chữ trên nút Play (hiện tên chế độ đang chọn).</summary>
        [SerializeField] private TextMeshProUGUI playTxt;

        /// <summary>Ảnh nền của nút Play, đổi theo chế độ đang chọn.</summary>
        [SerializeField] private Image playImage;

        /// <summary>Đếm ngược tới lần nhận quà hằng ngày kế tiếp.</summary>
        [SerializeField] private TextMeshProUGUI dailyRewardTimerText;

        // --- Bảng thông tin phía trên ---

        /// <summary>Nút mở hồ sơ người chơi.</summary>
        [SerializeField] private Button avatarButton;

        /// <summary>Ảnh đại diện.</summary>
        [SerializeField] private Image avatar;

        /// <summary>Ô chữ hiển thị level.</summary>
        [SerializeField] private TextMeshProUGUI textLevel;

        /// <summary>Ô chữ hiển thị số trophy.</summary>
        [SerializeField] private TextMeshProUGUI textCurrentTrophies;

        /// <summary>Tên người chơi.</summary>
        [SerializeField] private TextMeshProUGUI profileName;

        /// <summary>Số coin.</summary>
        [SerializeField] private TextMeshProUGUI coinsText;

        /// <summary>Số gem.</summary>
        [SerializeField] private TextMeshProUGUI gemsText;

        // --- Chấm báo ---

        /// <summary>Chấm báo có túi đồ sẵn sàng mở trong locker.</summary>
        [SerializeField] private GameObject lockerChangeIndicator;

        /// <summary>Chấm báo có nhiệm vụ hằng ngày chờ nhận thưởng.</summary>
        [SerializeField] private GameObject challengeChangeIndicator;

        /// <summary>Chấm báo quà hằng ngày đã sẵn sàng.</summary>
        [SerializeField] private GameObject dailyRewardChangeIndicator;

        /// <summary>Chấm báo có vật phẩm nâng cấp được trong phòng thay đồ.</summary>
        [SerializeField] private GameObject roomUpgradeIcon;

        private GameData gameData;
        private float badgeTimer;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Bind(customizeButton, () => ShowScreen(ScreenType.CustomizationUI));
            Bind(shopButton, () => ShowScreen(ScreenType.IAPPurchase));
            Bind(lockerButton, () => ShowScreen(ScreenType.LockerUI));
            Bind(roomButton, () => ShowScreen(ScreenType.DressingRoomUI));
            Bind(leagueButton, () => ShowScreen(ScreenType.LeagueUI));
            Bind(dailyChallengeButton, () => ShowScreen(ScreenType.DailyChallengeUI));
            Bind(collectionsButton, () => ShowScreen(ScreenType.CollectionsUI));
            Bind(modeSelectionBtn, () => ShowScreen(ScreenType.ModeSelectionUI));
            Bind(dailyRewardButton, () => ShowScreen(ScreenType.DailyRewardUI));
            Bind(optionsButton, () => ShowScreen(ScreenType.Settings));
            Bind(avatarButton, () => ShowScreen(ScreenType.Profile));
            Bind(play, HandlePlay);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            if (gameData != null)
            {
                gameData.OnTotalCoinsUpdated += HandleCoinsUpdated;
                gameData.OnTotalGemsUpdated += HandleGemsUpdated;
            }

            RefreshProfile();
            RefreshCurrencies();
            RefreshPlayButton();
            RefreshBadges();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            if (gameData != null)
            {
                gameData.OnTotalCoinsUpdated -= HandleCoinsUpdated;
                gameData.OnTotalGemsUpdated -= HandleGemsUpdated;
            }
        }

        protected override void OnDestroy()
        {
            if (gameData != null)
            {
                gameData.OnTotalCoinsUpdated -= HandleCoinsUpdated;
                gameData.OnTotalGemsUpdated -= HandleGemsUpdated;
            }
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsVisible) return;

            badgeTimer -= Time.unscaledDeltaTime;
            if (badgeTimer > 0f) return;

            badgeTimer = 1f;
            RefreshBadges();
            RefreshDailyRewardTimer();
        }

        // ------------------------------------------------------------------
        // Cập nhật hiển thị
        // ------------------------------------------------------------------

        /// <summary>Vẽ lại avatar, tên, level và trophy từ hồ sơ người chơi.</summary>
        public void RefreshProfile()
        {
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;
            if (profile == null) return;

            if (profileName != null) profileName.text = profile.playerName ?? string.Empty;
            if (textLevel != null) textLevel.text = profile.level.ToString();
            if (textCurrentTrophies != null) textCurrentTrophies.text = Utilities.FormatCount(profile.trophies);

            if (avatar != null && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(profile.avatarIndex, 0, gameData.avatarSprites.Count - 1);
                Sprite sprite = gameData.avatarSprites[index];
                if (sprite != null) avatar.sprite = sprite;
            }
        }

        /// <summary>Vẽ lại số coin/gem.</summary>
        public void RefreshCurrencies()
        {
            if (gameData == null) return;

            if (coinsText != null) coinsText.text = Utilities.FormatCount(gameData.totalCoins);
            if (gemsText != null) gemsText.text = Utilities.FormatCount(gameData.totalGems);
        }

        /// <summary>Cập nhật nhãn và ảnh nút Play theo chế độ đang chọn.</summary>
        public void RefreshPlayButton()
        {
            GameSettings settings = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameSettings : null;
            if (settings == null) return;

            if (playTxt != null) playTxt.text = settings.selectedMode == MatchModeType.Normal
                ? "PLAY"
                : settings.selectedMode.ToString().ToUpperInvariant();

            Sprite sprite = settings.GetSelectedModeMainMenuSprite();
            if (playImage != null && sprite != null) playImage.sprite = sprite;
        }

        /// <summary>Bật/tắt các chấm báo trên nút Locker, Challenge, DailyReward và Room.</summary>
        public void RefreshBadges()
        {
            bool lockerReady = gameData != null && gameData.slotsData != null && gameData.slotsData.HasReadyToOpenBags();
            if (lockerChangeIndicator != null) lockerChangeIndicator.SetActive(lockerReady);

            bool challengeReady = DailyChallengeManager.HasInstance
                                  && DailyChallengeManager.Instance.IsAnyDailyChallengeClaimPending();
            if (challengeChangeIndicator != null) challengeChangeIndicator.SetActive(challengeReady);

            bool dailyReady = DailyRewardManager.HasInstance && DailyRewardManager.Instance.IsDailyRewardAvailable();
            if (dailyRewardChangeIndicator != null) dailyRewardChangeIndicator.SetActive(dailyReady);

            bool upgradeReady = gameData != null && gameData.shopData != null
                                && gameData.shopData.IsAnySelectedUpgradeAvailable();
            if (roomUpgradeIcon != null) roomUpgradeIcon.SetActive(upgradeReady);
        }

        private void RefreshDailyRewardTimer()
        {
            if (dailyRewardTimerText == null) return;

            if (!DailyRewardManager.HasInstance)
            {
                dailyRewardTimerText.text = string.Empty;
                return;
            }

            DailyRewardManager manager = DailyRewardManager.Instance;
            dailyRewardTimerText.text = manager.IsDailyRewardAvailable()
                ? "READY"
                : Utilities.FormatTimeSpan(manager.GetTimeUntilNextReward());
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void HandleCoinsUpdated(int total)
        {
            if (coinsText != null) coinsText.text = Utilities.FormatCount(total);
        }

        private void HandleGemsUpdated(int total)
        {
            if (gemsText != null) gemsText.text = Utilities.FormatCount(total);
        }

        private void HandlePlay()
        {
            ShowScreen(ScreenType.MatchMaking);
        }

        private static void ShowScreen(ScreenType screen)
        {
            if (UIController.HasInstance) UIController.Instance.Show(screen);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
