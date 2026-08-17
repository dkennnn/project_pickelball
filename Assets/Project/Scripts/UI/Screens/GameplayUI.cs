using System.Collections.Generic;
using Pickleball.Data;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// HUD trong trận: điểm hai bên, thanh thể lực, hàng nút booster, đồng hồ giao bóng
    /// và thông báo lỗi luật.
    /// </summary>
    public class GameplayUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.Gameplay;

        // --- Đồng hồ và thông báo ---

        /// <summary>Đồng hồ đếm ngược thời gian phải giao bóng.</summary>
        [SerializeField] private TextMeshProUGUI serviceTimer;

        /// <summary>Node bảng thông báo lỗi luật.</summary>
        [SerializeField] private GameObject faultMessage;

        /// <summary>Nội dung thông báo lỗi luật.</summary>
        [SerializeField] private TextMeshProUGUI message;

        /// <summary>Thời gian giữ thông báo lỗi trên màn hình, tính bằng giây.</summary>
        [SerializeField] private float faultMessageDuration = 1.5f;

        // --- Bên người chơi ---

        /// <summary>Tên người chơi.</summary>
        [SerializeField] private TextMeshProUGUI leftUserNameText;

        /// <summary>Điểm của người chơi.</summary>
        [SerializeField] private TextMeshProUGUI leftScoreText;

        /// <summary>Thanh thể lực của người chơi (fillAmount).</summary>
        [SerializeField] private Image leftStaminaFill;

        /// <summary>Dấu báo người chơi đang cầm giao.</summary>
        [SerializeField] private GameObject leftServeIndicator;

        /// <summary>Ảnh đại diện người chơi.</summary>
        [SerializeField] private Image leftAvatar;

        // --- Bên đối thủ ---

        /// <summary>Tên đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI rightUserNameText;

        /// <summary>Điểm của đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI rightScoreText;

        /// <summary>Thanh thể lực của đối thủ (fillAmount).</summary>
        [SerializeField] private Image rightStaminaFill;

        /// <summary>Dấu báo đối thủ đang cầm giao.</summary>
        [SerializeField] private GameObject rightServeIndicator;

        /// <summary>Ảnh đại diện đối thủ.</summary>
        [SerializeField] private Image rightAvatar;

        // --- Booster ---

        /// <summary>Node cha chứa hàng nút booster.</summary>
        [SerializeField] private Transform boosters;

        /// <summary>Prefab một nút booster.</summary>
        [SerializeField] private BoosterButtonCell boosterButtonPrefab;

        private readonly List<BoosterButtonCell> boosterCells = new List<BoosterButtonCell>();

        private ScoreManager subscribedScoreManager;
        private float faultTimer;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            GameManager.OnServiceTimerUpdated += HandleServiceTimer;
            GameManager.OnRuleViolated += HandleRuleViolated;
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            BoosterManager.OnBoostersUpdated += RefreshBoosters;

            TrySubscribeScore();

            if (faultMessage != null) faultMessage.SetActive(false);
            if (serviceTimer != null) serviceTimer.text = string.Empty;

            RefreshNames();
            RefreshScores();
            RefreshBoosters();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();

            for (int i = 0; i < boosterCells.Count; i++)
            {
                if (boosterCells[i] != null) boosterCells[i].OnClicked -= HandleBoosterClicked;
            }
            boosterCells.Clear();

            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsVisible) return;

            TrySubscribeScore();
            RefreshStamina();
            RefreshBoosterProgress();

            if (faultTimer > 0f)
            {
                faultTimer -= Time.unscaledDeltaTime;
                if (faultTimer <= 0f && faultMessage != null) faultMessage.SetActive(false);
            }
        }

        // ------------------------------------------------------------------
        // Cập nhật hiển thị
        // ------------------------------------------------------------------

        /// <summary>Vẽ lại tên hai bên từ hồ sơ người chơi và tay vợt AI.</summary>
        public void RefreshNames()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;

            if (leftUserNameText != null && profile != null) leftUserNameText.text = profile.playerName ?? "PLAYER";

            if (leftAvatar != null && gameData != null && profile != null
                && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(profile.avatarIndex, 0, gameData.avatarSprites.Count - 1);
                Sprite sprite = gameData.avatarSprites[index];
                if (sprite != null) leftAvatar.sprite = sprite;
            }

            if (rightUserNameText != null && string.IsNullOrEmpty(rightUserNameText.text))
            {
                rightUserNameText.text = "OPPONENT";
            }

            if (rightAvatar != null) rightAvatar.enabled = rightAvatar.sprite != null;
        }

        /// <summary>Đọc lại điểm hiện tại của hai bên.</summary>
        public void RefreshScores()
        {
            if (!ScoreManager.HasInstance) return;

            ScoreManager manager = ScoreManager.Instance;
            if (leftScoreText != null) leftScoreText.text = manager.player1Score.ToString();
            if (rightScoreText != null) rightScoreText.text = manager.player2Score.ToString();
        }

        /// <summary>Dựng lại hàng nút booster theo kho booster của người chơi.</summary>
        public void RefreshBoosters()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            BoostersData boostersData = gameData != null ? gameData.boostersData : null;

            if (boosters == null || boosterButtonPrefab == null || boostersData == null) return;

            List<BoosterCountData> list = boostersData.boosters;
            int count = list != null ? list.Count : 0;

            while (boosterCells.Count < count)
            {
                BoosterButtonCell cell = Instantiate(boosterButtonPrefab, boosters);
                cell.OnClicked += HandleBoosterClicked;
                boosterCells.Add(cell);
            }

            for (int i = 0; i < boosterCells.Count; i++)
            {
                BoosterButtonCell cell = boosterCells[i];
                if (cell == null) continue;

                bool used = i < count && list[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                BoosterCountData entry = list[i];
                cell.Bind(entry.boosterType, entry.count, boostersData.GetImage(entry.boosterType));
            }
        }

        private void RefreshBoosterProgress()
        {
            if (!BoosterManager.HasInstance) return;

            string teamID = GetPlayerTeamID();
            if (string.IsNullOrEmpty(teamID)) return;

            for (int i = 0; i < boosterCells.Count; i++)
            {
                BoosterButtonCell cell = boosterCells[i];
                if (cell == null || !cell.gameObject.activeSelf) continue;

                cell.SetProgress(BoosterManager.Instance.GetBoosterRemainingTimePercentage(teamID, cell.Type));
            }
        }

        private void RefreshStamina()
        {
            if (!GameManager.HasInstance) return;

            GameManager manager = GameManager.Instance;
            ApplyStamina(leftStaminaFill, manager.GetParticipant(manager.player1TeamID) as BasePlayerController);
            ApplyStamina(rightStaminaFill, manager.GetParticipant(manager.player2TeamID) as BasePlayerController);

            if (leftServeIndicator != null || rightServeIndicator != null)
            {
                string server = manager.GetCurrentServerTeamID();
                if (leftServeIndicator != null) leftServeIndicator.SetActive(server == manager.player1TeamID);
                if (rightServeIndicator != null) rightServeIndicator.SetActive(server == manager.player2TeamID);
            }
        }

        private static void ApplyStamina(Image fill, BasePlayerController player)
        {
            if (fill == null) return;

            if (player == null || player.playerProfile == null || player.playerProfile.maxStamina <= 0f)
            {
                fill.fillAmount = 1f;
                return;
            }

            fill.fillAmount = Mathf.Clamp01(player.stamina / player.playerProfile.maxStamina);
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void HandleServiceTimer(float remaining)
        {
            if (serviceTimer == null) return;
            serviceTimer.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        }

        private void HandleRuleViolated(RuleType rule, string faultTeamID, string text)
        {
            if (message != null) message.text = string.IsNullOrEmpty(text) ? rule.ToString() : text;
            if (faultMessage != null) faultMessage.SetActive(true);

            faultTimer = Mathf.Max(0.1f, faultMessageDuration);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state != GameState.PreServe && state != GameState.Serving && serviceTimer != null)
            {
                serviceTimer.text = string.Empty;
            }
        }

        private void HandleScoreUpdated(int score, string teamID)
        {
            if (!GameManager.HasInstance)
            {
                RefreshScores();
                return;
            }

            GameManager manager = GameManager.Instance;
            if (teamID == manager.player1TeamID && leftScoreText != null) leftScoreText.text = score.ToString();
            else if (teamID == manager.player2TeamID && rightScoreText != null) rightScoreText.text = score.ToString();
        }

        private void HandleBoosterClicked(BoosterType type)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            BoostersData boostersData = gameData != null ? gameData.boostersData : null;

            if (boostersData == null || !BoosterManager.HasInstance) return;

            string teamID = GetPlayerTeamID();
            if (string.IsNullOrEmpty(teamID)) return;

            if (!boostersData.TryConsume(type))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("No booster left");
                return;
            }

            BoosterManager.Instance.AssignBooster(teamID, type);
            RefreshBoosters();
        }

        private static string GetPlayerTeamID()
        {
            return GameManager.HasInstance ? GameManager.Instance.player1TeamID : null;
        }

        private void TrySubscribeScore()
        {
            if (subscribedScoreManager != null) return;
            if (!ScoreManager.HasInstance) return;

            subscribedScoreManager = ScoreManager.Instance;
            subscribedScoreManager.OnScoreUpdated += HandleScoreUpdated;
            RefreshScores();
        }

        private void Unsubscribe()
        {
            GameManager.OnServiceTimerUpdated -= HandleServiceTimer;
            GameManager.OnRuleViolated -= HandleRuleViolated;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            BoosterManager.OnBoostersUpdated -= RefreshBoosters;

            if (subscribedScoreManager != null)
            {
                subscribedScoreManager.OnScoreUpdated -= HandleScoreUpdated;
                subscribedScoreManager = null;
            }
        }
    }
}
