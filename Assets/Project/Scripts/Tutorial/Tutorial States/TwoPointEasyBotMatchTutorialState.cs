using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 9 — trận tập 2 điểm với bot bậc <see cref="AIDifficulty.Tutorial"/>.
    /// <para>
    /// Bước tự hạ <see cref="GameSettings.maxScore"/> xuống <see cref="TargetScore"/>, khoá quyền
    /// giao bóng cho bot bằng <see cref="GameManager.LockServerToTeam"/> để pha đầu luôn do bot
    /// phát, rồi theo dõi <see cref="ScoreManager"/> / <see cref="GameManager.OnMatchEnded"/>.
    /// Mọi giá trị bị sửa đều được khôi phục ở <see cref="OnExit"/>.
    /// </para>
    /// </summary>
    public class TwoPointEasyBotMatchTutorialState : BaseTutorialState
    {
        /// <summary>Số điểm cần đạt để kết thúc trận tập.</summary>
        public const int TargetScore = 2;

        private const string Message = "Play a quick 2-point match against an easy bot.";

        private const float MatchStartDelay = OneSecond;

        private ScoreManager subscribedScore;
        private GameSettings settings;

        private int previousMaxScore;
        private int previousWinMargin;
        private bool settingsOverridden;

        private bool serverLocked;
        private bool matchStarted;
        private bool subscribedToMatchEnd;
        private bool aiDifficultyOverridden;
        private AIDifficulty previousDifficulty;
        private PickleballAIController ai;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public TwoPointEasyBotMatchTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.TwoPointEasyBotMatch;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            matchStarted = false;

            ShowMessage(Message);
            ApplyTutorialAI();
            ApplyMatchSettings();
            LockServerToBot();
            Subscribe();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!matchStarted && ElapsedTime >= MatchStartDelay)
            {
                matchStarted = true;
                StartMatch();
            }

            if (!matchStarted || IsCompleted) return;

            // Dự phòng khi OnMatchEnded không bắn (thiếu RuleEngine): tự chốt theo tỉ số.
            if (ScoreManager.HasInstance)
            {
                ScoreManager score = ScoreManager.Instance;
                if (score.player1Score >= TargetScore || score.player2Score >= TargetScore)
                {
                    Complete();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            Unsubscribe();
            UnlockServer();
            RestoreMatchSettings();
            RestoreAI();
        }

        // ------------------------------------------------------------------
        // Thiết lập trận
        // ------------------------------------------------------------------

        /// <summary>Hạ bot xuống bậc <see cref="AIDifficulty.Tutorial"/>, nhớ bậc cũ để trả lại.</summary>
        private void ApplyTutorialAI()
        {
            ai = context != null ? context.coachPlayer as PickleballAIController : null;
            if (ai == null) return;

            previousDifficulty = ai.difficulty;
            aiDifficultyOverridden = true;
            ai.SetAILevel(AIDifficulty.Tutorial);
        }

        private void RestoreAI()
        {
            if (!aiDifficultyOverridden || ai == null) return;

            ai.SetAILevel(previousDifficulty);
            aiDifficultyOverridden = false;
            ai = null;
        }

        /// <summary>Đặt luật thắng 2 điểm và bật cờ tutorial trên <see cref="GameManager"/>.</summary>
        private void ApplyMatchSettings()
        {
            GameManager gm = GameManagerRef;
            if (gm == null) return;

            gm.isTutorial = true;

            settings = gm.settings;
            if (settings == null) return;

            previousMaxScore = settings.maxScore;
            previousWinMargin = settings.winMargin;
            settingsOverridden = true;

            settings.maxScore = TargetScore;
            settings.winMargin = 1;
        }

        private void RestoreMatchSettings()
        {
            if (settingsOverridden && settings != null)
            {
                settings.maxScore = previousMaxScore;
                settings.winMargin = previousWinMargin;
            }

            settingsOverridden = false;
            settings = null;

            GameManager gm = GameManagerRef;
            if (gm != null) gm.isTutorial = false;
        }

        /// <summary>Khoá quyền giao bóng cho bot để pha đầu tiên do bot phát.</summary>
        private void LockServerToBot()
        {
            GameManager gm = GameManagerRef;
            if (gm == null) return;

            gm.LockServerToTeam(CoachTeamID);
            serverLocked = true;
        }

        private void UnlockServer()
        {
            if (!serverLocked) return;
            serverLocked = false;

            GameManager gm = GameManagerRef;
            if (gm != null) gm.UnlockServer();
        }

        /// <summary>
        /// Bắt đầu trận. <see cref="GameManager.StartMatch"/> tự gọi
        /// <see cref="GameSettings.ApplySelectedModeSettings"/> nên phải ép lại luật 2 điểm sau đó.
        /// </summary>
        private void StartMatch()
        {
            GameManager gm = GameManagerRef;
            if (gm == null)
            {
                Debug.LogWarning("[TwoPointEasyBotMatchTutorialState] Không có GameManager, bỏ qua trận tập.");
                Complete();
                return;
            }

            gm.StartMatch();

            if (settingsOverridden && settings != null)
            {
                settings.maxScore = TargetScore;
                settings.winMargin = 1;
            }
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void Subscribe()
        {
            GameManager.OnMatchEnded += HandleMatchEnded;
            subscribedToMatchEnd = true;

            if (!ScoreManager.HasInstance) return;

            subscribedScore = ScoreManager.Instance;
            subscribedScore.OnScoreUpdated += HandleScoreUpdated;
        }

        private void Unsubscribe()
        {
            if (subscribedToMatchEnd)
            {
                GameManager.OnMatchEnded -= HandleMatchEnded;
                subscribedToMatchEnd = false;
            }

            if (subscribedScore != null) subscribedScore.OnScoreUpdated -= HandleScoreUpdated;
            subscribedScore = null;
        }

        /// <summary>Trận kết thúc: bước hoàn thành bất kể ai thắng.</summary>
        /// <param name="winnerTeamID">Đội thắng.</param>
        private void HandleMatchEnded(string winnerTeamID)
        {
            ShowMessage(winnerTeamID == PlayerTeamID
                ? "You won your first match!"
                : "Good effort — you're ready for the real thing.");

            Complete();
        }

        /// <summary>Cập nhật thông điệp theo tỉ số hiện tại.</summary>
        /// <param name="score">Điểm mới của đội vừa ghi.</param>
        /// <param name="teamID">Đội vừa ghi điểm.</param>
        private void HandleScoreUpdated(int score, string teamID)
        {
            if (IsCompleted) return;

            ShowMessage(string.Format("{0} {1} - first to {2}.",
                teamID == PlayerTeamID ? "You:" : "Bot:", score, TargetScore));
        }
    }
}
