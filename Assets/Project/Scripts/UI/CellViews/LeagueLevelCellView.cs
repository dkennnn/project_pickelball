using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô một mốc level trong màn League: hiển thị số level, các phần thưởng của mốc
    /// và trạng thái đã nhận / nhận được / còn khoá.
    /// </summary>
    public class LeagueLevelCellView : MonoBehaviour
    {
        /// <summary>Trạng thái hiển thị của một mốc level.</summary>
        public enum LeagueLevelState
        {
            /// <summary>Người chơi chưa đạt level này.</summary>
            Locked = 0,

            /// <summary>Đã đạt level và chưa nhận thưởng.</summary>
            Claimable = 1,

            /// <summary>Đã nhận thưởng.</summary>
            Claimed = 2
        }

        /// <summary>Ô chữ hiển thị số level.</summary>
        [SerializeField] private TextMeshProUGUI levelText;

        /// <summary>Node hiển thị khi mốc còn khoá.</summary>
        [SerializeField] private GameObject lockedContent;

        /// <summary>Node hiển thị khi mốc đã nhận thưởng.</summary>
        [SerializeField] private GameObject collectedContent;

        /// <summary>Chữ "CLAIMED".</summary>
        [SerializeField] private TextMeshProUGUI collectedText;

        /// <summary>Node cha chứa các ô phần thưởng.</summary>
        [SerializeField] private Transform rewards;

        /// <summary>Các ô ảnh phần thưởng đã dựng sẵn trong prefab.</summary>
        [SerializeField] private List<Image> rewardImages = new List<Image>();

        /// <summary>Các ô chữ số lượng phần thưởng đã dựng sẵn trong prefab.</summary>
        [SerializeField] private List<TextMeshProUGUI> rewardAmounts = new List<TextMeshProUGUI>();

        /// <summary>Nút nhận thưởng.</summary>
        [SerializeField] private Button claimButton;

        /// <summary>Hiệu ứng làm xám khi mốc còn khoá.</summary>
        [SerializeField] private GreyscaleUIHierarchy greyscale;

        /// <summary>Phát khi bấm nhận thưởng; tham số là số level của mốc.</summary>
        public event Action<int> OnClaimClicked;

        /// <summary>Phát khi bấm vào ô; tham số là số level của mốc.</summary>
        public event Action<int> OnClicked;

        /// <summary>Mốc level đang gắn vào ô.</summary>
        public PlayerLevelData BoundLevel { get; private set; }

        /// <summary>Trạng thái hiển thị hiện tại của ô.</summary>
        public LeagueLevelState State { get; private set; } = LeagueLevelState.Locked;

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một mốc level vào ô và tự suy ra trạng thái từ level người chơi.
        /// </summary>
        /// <param name="levelData">Mốc level cần hiển thị; null sẽ ẩn ô.</param>
        /// <param name="playerLevel">Level hiện tại của người chơi.</param>
        public void Bind(PlayerLevelData levelData, int playerLevel)
        {
            Wire();
            BoundLevel = levelData;

            if (levelData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (levelData.isRewardCollected) State = LeagueLevelState.Claimed;
            else if (playerLevel >= levelData.level) State = LeagueLevelState.Claimable;
            else State = LeagueLevelState.Locked;

            if (levelText != null) levelText.text = "LVL." + levelData.level;

            if (lockedContent != null) lockedContent.SetActive(State == LeagueLevelState.Locked);
            if (collectedContent != null) collectedContent.SetActive(State == LeagueLevelState.Claimed);
            if (collectedText != null) collectedText.text = "CLAIMED";
            if (claimButton != null) claimButton.gameObject.SetActive(State == LeagueLevelState.Claimable);
            if (greyscale != null) greyscale.SetGreyscale(State == LeagueLevelState.Locked);

            RefreshRewards(levelData.rewards);
        }

        private void RefreshRewards(List<DynamicReward> list)
        {
            int count = list != null ? list.Count : 0;

            for (int i = 0; i < rewardImages.Count; i++)
            {
                Image image = rewardImages[i];
                if (image == null) continue;

                bool used = i < count;
                if (image.transform.parent != null) image.transform.parent.gameObject.SetActive(used);
                image.enabled = used && image.sprite != null;
            }

            for (int i = 0; i < rewardAmounts.Count; i++)
            {
                TextMeshProUGUI label = rewardAmounts[i];
                if (label == null) continue;

                bool used = i < count;
                label.gameObject.SetActive(used);
                if (used && list[i] != null) label.text = Utilities.FormatCount(list[i].value);
            }

            if (rewards != null) rewards.gameObject.SetActive(count > 0);
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (claimButton != null) claimButton.onClick.AddListener(HandleClaim);
        }

        private void HandleClaim()
        {
            if (BoundLevel == null) return;
            OnClaimClicked?.Invoke(BoundLevel.level);
            OnClicked?.Invoke(BoundLevel.level);
        }

        private void OnDestroy()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(HandleClaim);
            OnClaimClicked = null;
            OnClicked = null;
        }
    }
}
