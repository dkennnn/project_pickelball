using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn League: liệt kê các mốc level trong <see cref="PlayerLevels"/> kèm trạng thái
    /// đã nhận / nhận được / còn khoá. Bấm nhận sẽ gọi <see cref="PlayerLevels.ClaimReward"/>,
    /// cộng phần thưởng vào tài khoản rồi mở màn <see cref="ScreenType.RewardUI"/>.
    /// </summary>
    public class LeagueUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.LeagueUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Node cha chứa danh sách mốc level.</summary>
        [SerializeField] private Transform content;

        /// <summary>Prefab một ô mốc level.</summary>
        [SerializeField] private LeagueLevelCellView leagueCellPrefab;

        /// <summary>Số mốc hiển thị tối đa; 0 nghĩa là hiện hết.</summary>
        [SerializeField] private int maxLevelsToShow = 10;

        /// <summary>Vùng cuộn của danh sách.</summary>
        [SerializeField] private ScrollRect scrollView;

        private readonly List<LeagueLevelCellView> cells = new List<LeagueLevelCellView>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (title != null) title.text = "LEAGUE";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            RefreshLevels();
            if (scrollView != null) scrollView.verticalNormalizedPosition = 0f;
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null) cells[i].OnClaimClicked -= HandleClaim;
            }
            cells.Clear();

            base.OnDestroy();
        }

        /// <summary>Dựng lại danh sách mốc level theo tiến độ hiện tại.</summary>
        public void RefreshLevels()
        {
            if (content == null || leagueCellPrefab == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerLevels levels = gameData != null ? gameData.playerLevels : null;
            List<PlayerLevelData> list = levels != null ? levels.levels : null;
            if (list == null) return;

            int count = maxLevelsToShow > 0 ? Mathf.Min(maxLevelsToShow, list.Count) : list.Count;
            int playerLevel = gameData.playerProfileData != null ? gameData.playerProfileData.level : 0;

            while (cells.Count < count)
            {
                LeagueLevelCellView cell = Instantiate(leagueCellPrefab, content);
                cell.OnClaimClicked += HandleClaim;
                cells.Add(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                LeagueLevelCellView cell = cells[i];
                if (cell == null) continue;

                bool used = i < count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(list[i], playerLevel);
            }
        }

        private void HandleClaim(int level)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerLevels levels = gameData != null ? gameData.playerLevels : null;
            if (levels == null) return;

            List<DynamicReward> rewards = levels.ClaimReward(level);
            if (rewards == null || rewards.Count == 0)
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Nothing to claim");
                RefreshLevels();
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                DynamicReward reward = rewards[i];
                if (reward != null) gameData.GrantReward(reward.rewardType, reward.value);
            }

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.RewardUI, rewards);
            RefreshLevels();
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
