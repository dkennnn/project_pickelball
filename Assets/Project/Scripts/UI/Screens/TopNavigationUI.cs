using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thanh thông tin cố định phía trên (avatar + level + trophy bên trái, ví coin/gem bên phải).
    /// Là popup vì luôn nằm chồng lên màn hình đang mở.
    /// </summary>
    public class TopNavigationUI : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.TopNavigation;

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

        /// <summary>Nút "+" mở cửa hàng coin.</summary>
        [SerializeField] private Button addCoins;

        /// <summary>Nút "+" mở cửa hàng gem.</summary>
        [SerializeField] private Button addGems;

        /// <summary>Bộ chạy số coin (tuỳ chọn).</summary>
        [SerializeField] private TextCounterUtility coinsCounter;

        /// <summary>Bộ chạy số gem (tuỳ chọn).</summary>
        [SerializeField] private TextCounterUtility gemsCounter;

        private GameData gameData;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (avatarButton != null) avatarButton.onClick.AddListener(HandleAvatar);
            if (addCoins != null) addCoins.onClick.AddListener(HandleShop);
            if (addGems != null) addGems.onClick.AddListener(HandleShop);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (gameData != null)
            {
                gameData.OnTotalCoinsUpdated += HandleCoins;
                gameData.OnTotalGemsUpdated += HandleGems;
            }

            Refresh();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();
            base.OnDestroy();
        }

        /// <summary>Vẽ lại toàn bộ thanh thông tin từ dữ liệu hiện tại.</summary>
        public void Refresh()
        {
            if (gameData == null) return;

            if (coinsCounter != null) coinsCounter.SetImmediate(gameData.totalCoins);
            else if (coinsText != null) coinsText.text = Utilities.FormatCount(gameData.totalCoins);

            if (gemsCounter != null) gemsCounter.SetImmediate(gameData.totalGems);
            else if (gemsText != null) gemsText.text = Utilities.FormatCount(gameData.totalGems);

            PlayerProfileData profile = gameData.playerProfileData;
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

        private void HandleCoins(int total)
        {
            if (coinsCounter != null) coinsCounter.CountTo(total);
            else if (coinsText != null) coinsText.text = Utilities.FormatCount(total);
        }

        private void HandleGems(int total)
        {
            if (gemsCounter != null) gemsCounter.CountTo(total);
            else if (gemsText != null) gemsText.text = Utilities.FormatCount(total);
        }

        private void HandleAvatar()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.Profile);
        }

        private void HandleShop()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.IAPPurchase);
        }

        private void Unsubscribe()
        {
            if (gameData == null) return;

            gameData.OnTotalCoinsUpdated -= HandleCoins;
            gameData.OnTotalGemsUpdated -= HandleGems;
        }
    }
}
