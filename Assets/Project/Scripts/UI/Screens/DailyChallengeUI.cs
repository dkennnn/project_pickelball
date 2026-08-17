using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn nhiệm vụ hằng ngày: liệt kê bộ nhiệm vụ của ngày, thanh tiến độ và nút nhận thưởng.
    /// Nghe <see cref="DailyChallengeManager.OnChallengesUpdated"/> để vẽ lại.
    /// </summary>
    public class DailyChallengeUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.DailyChallengeUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Băng rôn giới thiệu.</summary>
        [SerializeField] private GameObject banner;

        /// <summary>Đếm ngược tới lúc đổi bộ nhiệm vụ mới.</summary>
        [SerializeField] private TextMeshProUGUI resetTimerText;

        /// <summary>Node cha chứa danh sách nhiệm vụ.</summary>
        [SerializeField] private Transform challenges;

        /// <summary>Prefab một ô nhiệm vụ.</summary>
        [SerializeField] private DailyChallengeCellView challengeCellPrefab;

        private readonly List<DailyChallengeCellView> cells = new List<DailyChallengeCellView>();
        private float tickTimer;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (title != null) title.text = "CHALLENGES";
            if (banner != null) banner.SetActive(true);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            DailyChallengeManager.OnChallengesUpdated += RefreshChallenges;

            if (DailyChallengeManager.HasInstance) DailyChallengeManager.Instance.CheckAndResetDailyChallenges();

            RefreshChallenges();
            RefreshTimer();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            DailyChallengeManager.OnChallengesUpdated -= RefreshChallenges;
        }

        protected override void OnDestroy()
        {
            DailyChallengeManager.OnChallengesUpdated -= RefreshChallenges;
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null) cells[i].OnClaimClicked -= HandleClaim;
            }
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

        /// <summary>Dựng lại danh sách nhiệm vụ.</summary>
        public void RefreshChallenges()
        {
            if (challenges == null || challengeCellPrefab == null) return;
            if (!DailyChallengeManager.HasInstance) return;

            List<DailyChallenge> list = DailyChallengeManager.Instance.GetDailyChallenges();
            int count = list != null ? list.Count : 0;

            while (cells.Count < count)
            {
                DailyChallengeCellView cell = Instantiate(challengeCellPrefab, challenges);
                cell.OnClaimClicked += HandleClaim;
                cells.Add(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                DailyChallengeCellView cell = cells[i];
                if (cell == null) continue;

                cell.Bind(i < count ? list[i] : null);
            }
        }

        /// <summary>Cập nhật đếm ngược tới lần đổi nhiệm vụ kế tiếp.</summary>
        public void RefreshTimer()
        {
            if (resetTimerText == null || !DailyChallengeManager.HasInstance) return;
            resetTimerText.text = Utilities.FormatTimeSpan(DailyChallengeManager.Instance.GetTimeUntilReset());
        }

        private void HandleClaim(DailyChallenge challenge)
        {
            if (!DailyChallengeManager.HasInstance || challenge == null) return;

            DailyChallengeManager.Instance.CollectReward(challenge);
            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();

            if (challenge.Reward != null && UIController.HasInstance)
            {
                UIController.Instance.Show(ScreenType.RewardUI, new List<CollectibleReward> { challenge.Reward });
            }

            RefreshChallenges();
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
