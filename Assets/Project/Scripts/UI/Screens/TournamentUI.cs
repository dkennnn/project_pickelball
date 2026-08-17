using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn giải đấu: sơ đồ ba vòng của <see cref="TournamentsData"/>, điều kiện tham gia
    /// và nút vào giải (<see cref="TournamentsData.TryEnter"/>).
    /// </summary>
    public class TournamentUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.TournamentUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tên giải.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Mô tả giải.</summary>
        [SerializeField] private TextMeshProUGUI description;

        /// <summary>Phí tham gia.</summary>
        [SerializeField] private TextMeshProUGUI entryCostText;

        /// <summary>Nút vào giải.</summary>
        [SerializeField] private Button enterButton;

        /// <summary>Chữ trên nút vào giải.</summary>
        [SerializeField] private TextMeshProUGUI enterButtonText;

        /// <summary>Nút chơi trận kế tiếp khi đang trong giải.</summary>
        [SerializeField] private Button playMatchButton;

        /// <summary>Node cha chứa sơ đồ các vòng.</summary>
        [SerializeField] private Transform stagesContainer;

        /// <summary>Prefab một dòng vòng đấu.</summary>
        [SerializeField] private StatItemCell stageCellPrefab;

        /// <summary>Ô chữ báo lý do chưa vào giải được.</summary>
        [SerializeField] private TextMeshProUGUI reasonText;

        private readonly List<StatItemCell> stageCells = new List<StatItemCell>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (enterButton != null) enterButton.onClick.AddListener(HandleEnter);
            if (playMatchButton != null) playMatchButton.onClick.AddListener(HandlePlayMatch);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            TournamentsData.OnTournamentProgressChanged += Refresh;
            Refresh();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            TournamentsData.OnTournamentProgressChanged -= Refresh;
        }

        protected override void OnDestroy()
        {
            TournamentsData.OnTournamentProgressChanged -= Refresh;

            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (enterButton != null) enterButton.onClick.RemoveListener(HandleEnter);
            if (playMatchButton != null) playMatchButton.onClick.RemoveListener(HandlePlayMatch);

            stageCells.Clear();
            base.OnDestroy();
        }

        /// <summary>Vẽ lại thông tin giải, sơ đồ vòng đấu và trạng thái nút.</summary>
        public void Refresh()
        {
            TournamentsData data = ResolveTournaments();
            Tournament tournament = data != null ? data.tournament : null;

            if (tournament == null)
            {
                if (title != null) title.text = "TOURNAMENT";
                if (enterButton != null) enterButton.interactable = false;
                if (playMatchButton != null) playMatchButton.gameObject.SetActive(false);
                if (reasonText != null) reasonText.text = "Tournament is not configured.";
                return;
            }

            if (title != null) title.text = (tournament.name ?? "TOURNAMENT").ToUpperInvariant();
            if (description != null) description.text = tournament.description ?? string.Empty;

            if (entryCostText != null)
            {
                entryCostText.text = Utilities.FormatCount(tournament.entryCost)
                                     + (tournament.entryCostType == CurrencyType.Gems ? " gems" : " coins");
            }

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            int playerLevel = gameData != null && gameData.playerProfileData != null
                ? gameData.playerProfileData.level
                : 0;

            bool inProgress = data.IsInProgress();
            bool canEnter = data.CanEnter(playerLevel, out string reason);

            if (reasonText != null) reasonText.text = canEnter ? string.Empty : reason;

            if (enterButton != null)
            {
                enterButton.gameObject.SetActive(!inProgress);
                enterButton.interactable = canEnter;
            }

            if (enterButtonText != null) enterButtonText.text = "ENTER";
            if (playMatchButton != null) playMatchButton.gameObject.SetActive(inProgress);

            RefreshStages(data, tournament);
        }

        private void RefreshStages(TournamentsData data, Tournament tournament)
        {
            if (stagesContainer == null || stageCellPrefab == null) return;

            List<TournamentStage> stages = tournament.stages;
            int count = stages != null ? stages.Count : 0;
            int currentStage = data.currentProgress != null ? data.currentProgress.currentStage : -1;
            bool inProgress = data.IsInProgress();

            while (stageCells.Count < count) stageCells.Add(Instantiate(stageCellPrefab, stagesContainer));

            for (int i = 0; i < stageCells.Count; i++)
            {
                StatItemCell cell = stageCells[i];
                if (cell == null) continue;

                bool used = i < count && stages[i] != null;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                TournamentStage stage = stages[i];
                string label = string.IsNullOrEmpty(stage.stageName) ? "STAGE " + (i + 1) : stage.stageName;

                string state;
                if (!inProgress) state = "LOCKED";
                else if (i < currentStage) state = "WON";
                else if (i == currentStage) state = stage.playerMatch != null && !string.IsNullOrEmpty(stage.playerMatch.opponentName)
                    ? "VS " + stage.playerMatch.opponentName
                    : "CURRENT";
                else state = "UPCOMING";

                cell.Bind(label, state);
            }
        }

        private void HandleEnter()
        {
            TournamentsData data = ResolveTournaments();
            if (data == null) return;

            if (!data.TryEnter())
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Cannot enter the tournament");
                Refresh();
                return;
            }

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
            Refresh();
        }

        private void HandlePlayMatch()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (gameData != null) gameData.gameMode = GameMode.Tournament;

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MatchMaking);
        }

        private static TournamentsData ResolveTournaments()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            return gameData != null ? gameData.tournamentsData : null;
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
