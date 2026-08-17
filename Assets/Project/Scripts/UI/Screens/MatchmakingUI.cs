using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn tìm trận. Bản offline không có ghép trận thật: màn chỉ chạy hoạt cảnh chờ
    /// trong vài giây, bốc một tên AI rồi mở màn <see cref="ScreenType.Gameplay"/> và
    /// gọi <see cref="GameManager.StartMatch"/>.
    /// </summary>
    public class MatchmakingUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.MatchMaking;

        /// <summary>Khối thông tin bên trái (người chơi).</summary>
        [SerializeField] private RectTransform leftContainer;

        /// <summary>Khối thông tin bên phải (đối thủ).</summary>
        [SerializeField] private RectTransform rightContainer;

        /// <summary>Tên người chơi.</summary>
        [SerializeField] private TextMeshProUGUI leftUserName;

        /// <summary>Số coin cược của người chơi.</summary>
        [SerializeField] private TextMeshProUGUI leftCoins;

        /// <summary>Ảnh đại diện người chơi.</summary>
        [SerializeField] private Image leftAvatar;

        /// <summary>Tên đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI rightUserName;

        /// <summary>Số coin cược của đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI rightCoins;

        /// <summary>Ảnh đại diện đối thủ.</summary>
        [SerializeField] private Image rightAvatar;

        /// <summary>Biểu tượng "VS".</summary>
        [SerializeField] private GameObject vsImg;

        /// <summary>Nút huỷ tìm trận.</summary>
        [SerializeField] private Button cancel;

        /// <summary>Thời gian giả lập tìm trận, tính bằng giây.</summary>
        [SerializeField] private float searchDuration = 2.5f;

        /// <summary>Tên đối thủ vừa bốc được.</summary>
        public string OpponentName { get; private set; }

        private float timer;
        private bool searching;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (cancel != null) cancel.onClick.AddListener(HandleCancel);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            searching = true;
            timer = Mathf.Max(0.1f, searchDuration);

            RefreshPlayer();
            RollOpponent();

            if (vsImg != null) vsImg.SetActive(true);
            if (leftContainer != null) leftContainer.gameObject.SetActive(true);
            if (rightContainer != null) rightContainer.gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            searching = false;
        }

        protected override void OnDestroy()
        {
            if (cancel != null) cancel.onClick.RemoveListener(HandleCancel);
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsVisible || !searching) return;

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;

            searching = false;
            StartMatch();
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleCancel();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Vẽ lại khối thông tin của người chơi.</summary>
        public void RefreshPlayer()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;
            GameSettings settings = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameSettings : null;

            if (leftUserName != null && profile != null) leftUserName.text = profile.playerName ?? "PLAYER";

            int bet = settings != null ? settings.gameBetCoins : 0;
            if (leftCoins != null) leftCoins.text = Utilities.FormatCount(bet);
            if (rightCoins != null) rightCoins.text = Utilities.FormatCount(bet);

            if (leftAvatar != null && gameData != null && profile != null
                && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(profile.avatarIndex, 0, gameData.avatarSprites.Count - 1);
                Sprite sprite = gameData.avatarSprites[index];
                if (sprite != null) leftAvatar.sprite = sprite;
            }
        }

        /// <summary>Bốc ngẫu nhiên một tên và avatar cho đối thủ AI.</summary>
        public void RollOpponent()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            OpponentName = "OPPONENT";
            AINamesData names = gameData != null ? gameData.namesData : null;
            if (names != null) OpponentName = names.GetRandomName();

            if (rightUserName != null) rightUserName.text = OpponentName;

            if (rightAvatar != null && gameData != null
                && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                Sprite sprite = gameData.avatarSprites[Random.Range(0, gameData.avatarSprites.Count)];
                if (sprite != null) rightAvatar.sprite = sprite;
            }
        }

        /// <summary>Bắt đầu trận với AI và chuyển sang HUD trong trận.</summary>
        public void StartMatch()
        {
            GameSettings settings = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameSettings : null;
            if (settings != null) settings.ApplySelectedModeSettings();

            if (ScoreManager.HasInstance) ScoreManager.Instance.ResetScore();
            if (StatsManager.HasInstance) StatsManager.Instance.ResetStats();

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.Gameplay);
            else Hide();

            if (GameManager.HasInstance) GameManager.Instance.StartMatch();
        }

        private void HandleCancel()
        {
            searching = false;

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
