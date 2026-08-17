using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn quà hằng ngày: nút nhận khi tới lượt, đếm ngược khi chưa tới,
    /// và danh sách quà vừa nhận được.
    /// </summary>
    public class DailyRewardUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.DailyRewardUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Nút nhận quà.</summary>
        [SerializeField] private Button claimButton;

        /// <summary>Chữ trên nút nhận quà.</summary>
        [SerializeField] private TextMeshProUGUI claimText;

        /// <summary>Đếm ngược tới lượt quà kế tiếp.</summary>
        [SerializeField] private TextMeshProUGUI timerText;

        /// <summary>Node cha chứa danh sách quà.</summary>
        [SerializeField] private Transform rewardsContainer;

        /// <summary>Prefab một dòng quà.</summary>
        [SerializeField] private StatItemCell rewardCellPrefab;

        /// <summary>Danh sách quà vừa nhận trong lần bấm gần nhất.</summary>
        public List<CollectibleReward> LastRewards { get; private set; }

        private readonly List<StatItemCell> cells = new List<StatItemCell>();
        private float tickTimer;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (claimButton != null) claimButton.onClick.AddListener(HandleClaim);
            if (title != null) title.text = "DAILY REWARD";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            LastRewards = data as List<CollectibleReward>;
            Refresh();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (claimButton != null) claimButton.onClick.RemoveListener(HandleClaim);
            cells.Clear();

            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsVisible) return;

            tickTimer -= Time.unscaledDeltaTime;
            if (tickTimer > 0f) return;

            tickTimer = 1f;
            RefreshTimer();
        }

        /// <summary>Vẽ lại nút nhận, đếm ngược và danh sách quà.</summary>
        public void Refresh()
        {
            bool available = DailyRewardManager.HasInstance && DailyRewardManager.Instance.IsDailyRewardAvailable();

            if (claimButton != null) claimButton.interactable = available;
            if (claimText != null) claimText.text = available ? "CLAIM" : "COME BACK LATER";

            RefreshTimer();
            RefreshRewards();
        }

        /// <summary>Cập nhật đếm ngược tới lượt quà kế tiếp.</summary>
        public void RefreshTimer()
        {
            if (timerText == null) return;

            if (!DailyRewardManager.HasInstance)
            {
                timerText.text = string.Empty;
                return;
            }

            DailyRewardManager manager = DailyRewardManager.Instance;
            timerText.text = manager.IsDailyRewardAvailable()
                ? "READY"
                : Utilities.FormatTimeSpan(manager.GetTimeUntilNextReward());
        }

        /// <summary>Dựng lại danh sách quà vừa nhận.</summary>
        public void RefreshRewards()
        {
            if (rewardsContainer == null || rewardCellPrefab == null) return;

            int count = LastRewards != null ? LastRewards.Count : 0;

            while (cells.Count < count) cells.Add(Instantiate(rewardCellPrefab, rewardsContainer));

            for (int i = 0; i < cells.Count; i++)
            {
                StatItemCell cell = cells[i];
                if (cell == null) continue;

                if (i < count) cell.Bind(LastRewards[i]);
                else cell.gameObject.SetActive(false);
            }
        }

        private void HandleClaim()
        {
            if (!DailyRewardManager.HasInstance) return;

            List<CollectibleReward> rewards = DailyRewardManager.Instance.ClaimDailyReward();
            if (rewards == null || rewards.Count == 0)
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Already claimed today");
                Refresh();
                return;
            }

            LastRewards = rewards;
            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();

            Refresh();

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.RewardUI, rewards);
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
