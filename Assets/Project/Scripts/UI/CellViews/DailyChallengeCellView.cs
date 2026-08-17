using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô một nhiệm vụ hằng ngày: mô tả, thanh tiến độ, phần thưởng và nút nhận.
    /// </summary>
    public class DailyChallengeCellView : MonoBehaviour
    {
        /// <summary>Mô tả nhiệm vụ.</summary>
        [SerializeField] private TextMeshProUGUI description;

        /// <summary>Chuỗi tiến độ dạng "3/10".</summary>
        [SerializeField] private TextMeshProUGUI progressCount;

        /// <summary>Thanh tiến độ (fillAmount).</summary>
        [SerializeField] private Image currentProgress;

        /// <summary>Nền thanh tiến độ.</summary>
        [SerializeField] private Image progressBG;

        /// <summary>Ảnh phần thưởng.</summary>
        [SerializeField] private Image icon;

        /// <summary>Số lượng phần thưởng.</summary>
        [SerializeField] private TextMeshProUGUI rewardAmountText;

        /// <summary>Nút nhận thưởng khi nhiệm vụ đã hoàn thành.</summary>
        [SerializeField] private Button claimButton;

        /// <summary>Nút nhảy tới màn chơi để làm nhiệm vụ.</summary>
        [SerializeField] private Button goButton;

        /// <summary>Phát khi người chơi bấm nhận thưởng.</summary>
        public event Action<DailyChallenge> OnClaimClicked;

        /// <summary>Phát khi người chơi bấm nút "GO".</summary>
        public event Action<DailyChallenge> OnClicked;

        /// <summary>Nhiệm vụ đang gắn vào ô.</summary>
        public DailyChallenge BoundChallenge { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một nhiệm vụ vào ô.
        /// </summary>
        /// <param name="challenge">Nhiệm vụ cần hiển thị; null sẽ ẩn ô.</param>
        public void Bind(DailyChallenge challenge)
        {
            Wire();
            BoundChallenge = challenge;

            if (challenge == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (description != null) description.text = challenge.Description ?? string.Empty;

            float target = Mathf.Max(1f, challenge.TargetValue);
            float progress = Mathf.Clamp(challenge.Progress, 0f, target);

            if (progressCount != null) progressCount.text = Mathf.FloorToInt(progress) + "/" + Mathf.FloorToInt(target);
            if (currentProgress != null) currentProgress.fillAmount = Mathf.Clamp01(challenge.NormalizedProgress);
            if (progressBG != null) progressBG.enabled = true;

            CollectibleReward reward = challenge.Reward;
            if (rewardAmountText != null) rewardAmountText.text = reward != null ? Utilities.FormatCount(reward.Amount) : string.Empty;
            if (icon != null) icon.enabled = icon.sprite != null;

            bool claimable = challenge.IsCompleted && !challenge.RewardCollected;
            if (claimButton != null) claimButton.gameObject.SetActive(claimable);
            if (goButton != null) goButton.gameObject.SetActive(!challenge.IsCompleted);
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (claimButton != null) claimButton.onClick.AddListener(HandleClaim);
            if (goButton != null) goButton.onClick.AddListener(HandleGo);
        }

        private void HandleClaim()
        {
            if (BoundChallenge == null) return;
            OnClaimClicked?.Invoke(BoundChallenge);
        }

        private void HandleGo()
        {
            if (BoundChallenge == null) return;
            OnClicked?.Invoke(BoundChallenge);
        }

        private void OnDestroy()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(HandleClaim);
            if (goButton != null) goButton.onClick.RemoveListener(HandleGo);

            OnClaimClicked = null;
            OnClicked = null;
        }
    }
}
