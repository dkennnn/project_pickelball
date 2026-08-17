using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn chọn chế độ chơi: liệt kê <see cref="GameSettings.matchModeConfigs"/>, khoá chế độ
    /// chưa đủ level và ghi lựa chọn về <see cref="GameSettings.SelectMode"/>.
    /// </summary>
    public class ModeSelectionUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.ModeSelectionUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Node cha chứa danh sách ô chế độ.</summary>
        [SerializeField] private Transform content;

        /// <summary>Prefab một ô chế độ.</summary>
        [SerializeField] private ModeSelectionCellView modeCellPrefab;

        /// <summary>Vùng cuộn của danh sách.</summary>
        [SerializeField] private ScrollRect scrollView;

        private readonly List<ModeSelectionCellView> cells = new List<ModeSelectionCellView>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (title != null) title.text = "MODES";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            RefreshModes();
            if (scrollView != null) scrollView.verticalNormalizedPosition = 1f;
        }

        /// <summary>Dựng lại danh sách chế độ theo cấu hình và level người chơi.</summary>
        public void RefreshModes()
        {
            GameSettings settings = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameSettings : null;
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            List<MatchModeConfig> configs = settings != null ? settings.matchModeConfigs : null;
            if (content == null || modeCellPrefab == null || configs == null) return;

            int playerLevel = gameData != null && gameData.playerProfileData != null
                ? gameData.playerProfileData.level
                : 0;

            while (cells.Count < configs.Count)
            {
                ModeSelectionCellView cell = Instantiate(modeCellPrefab, content);
                cell.OnClicked += HandleModeSelected;
                cell.OnInfoClicked += HandleModeInfo;
                cells.Add(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                ModeSelectionCellView cell = cells[i];
                if (cell == null) continue;

                cell.Bind(i < configs.Count ? configs[i] : null, playerLevel);
            }
        }

        private void HandleModeSelected(MatchModeType mode)
        {
            GameSettings settings = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameSettings : null;
            if (settings != null) settings.SelectMode(mode);

            HandleBack();
        }

        private void HandleModeInfo(MatchModeConfig config)
        {
            if (config == null) return;
            if (!ToastHandler.HasInstance) return;

            string message = string.IsNullOrEmpty(config.infoDescription) ? config.infoTitle : config.infoDescription;
            ToastHandler.Instance.Show(message);
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cells.Count; i++)
            {
                ModeSelectionCellView cell = cells[i];
                if (cell == null) continue;

                cell.OnClicked -= HandleModeSelected;
                cell.OnInfoClicked -= HandleModeInfo;
            }
            cells.Clear();

            base.OnDestroy();
        }
    }
}
