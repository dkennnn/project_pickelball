using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Popup khoe quà: hiện lần lượt từng phần thưởng trong danh sách, chạm/bấm Claim để
    /// sang phần thưởng kế tiếp; hết danh sách thì cộng những phần chưa nhận rồi đóng.
    /// <para>
    /// Nhận <c>List&lt;CollectibleReward&gt;</c> (hoặc <c>List&lt;DynamicReward&gt;</c>,
    /// hoặc một <see cref="CollectibleReward"/> đơn lẻ) qua tham số <c>data</c>.
    /// </para>
    /// </summary>
    public class RewardPopup : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.RewardPopup;

        /// <summary>Ô chữ mô tả phần thưởng đang hiện (node <c>Parent/TitleText</c>).</summary>
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>Ảnh phần thưởng đang hiện (node <c>Parent/RewardImage</c>).</summary>
        [SerializeField] private Image rewardImage;

        /// <summary>Vầng sáng sau ảnh phần thưởng (node <c>Parent/Glow</c>).</summary>
        [SerializeField] private GameObject glow;

        /// <summary>Nút phủ toàn màn kiêm nút Claim (node <c>Parent/TapButton</c>).</summary>
        [SerializeField] private Button tapButton;

        /// <summary>
        /// Ảnh theo từng <see cref="RewardType"/>, xếp đúng thứ tự giá trị enum.
        /// Project chưa có bảng tra ảnh theo loại phần thưởng nên phải gán tay trên prefab;
        /// để trống thì popup chỉ hiện chữ.
        /// </summary>
        [SerializeField] private List<Sprite> rewardSpritesByType = new List<Sprite>();

        /// <summary>Phát sau khi người chơi nhận xong toàn bộ phần thưởng.</summary>
        public event Action OnAllClaimed;

        /// <summary>Danh sách phần thưởng đang trình bày.</summary>
        public IReadOnlyList<CollectibleReward> Rewards => rewards;

        /// <summary>Chỉ số phần thưởng đang hiện; bằng số phần thưởng khi đã xem hết.</summary>
        public int Index { get; private set; }

        private readonly List<CollectibleReward> rewards = new List<CollectibleReward>();
        private bool wired;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Wire();
        }

        /// <inheritdoc/>
        /// <param name="data">Danh sách phần thưởng cần khoe; null sẽ đóng popup ngay khi chạm.</param>
        public override void OnShow(object data)
        {
            Wire();
            SetupPopup(data);
        }

        /// <summary>Nạp danh sách phần thưởng và hiện phần thưởng đầu tiên.</summary>
        /// <param name="data">
        /// <c>List&lt;CollectibleReward&gt;</c>, <c>List&lt;DynamicReward&gt;</c>,
        /// <see cref="CollectibleReward"/> hoặc <see cref="DynamicReward"/>.
        /// </param>
        public void SetupPopup(object data)
        {
            rewards.Clear();
            Index = 0;

            switch (data)
            {
                case List<CollectibleReward> collectibles:
                    for (int i = 0; i < collectibles.Count; i++)
                    {
                        if (collectibles[i] != null) rewards.Add(collectibles[i]);
                    }
                    break;

                case List<DynamicReward> dynamics:
                    for (int i = 0; i < dynamics.Count; i++)
                    {
                        DynamicReward reward = dynamics[i];
                        if (reward != null) rewards.Add(new CollectibleReward(reward.rewardType, reward.value));
                    }
                    break;

                case CollectibleReward single:
                    rewards.Add(single);
                    break;

                case DynamicReward singleDynamic:
                    rewards.Add(new CollectibleReward(singleDynamic.rewardType, singleDynamic.value));
                    break;
            }

            RefreshCurrent();
        }

        /// <summary>Nạp danh sách phần thưởng đã chốt số lượng.</summary>
        /// <param name="list">Danh sách phần thưởng; null tương đương danh sách rỗng.</param>
        public void SetupPopup(List<CollectibleReward> list)
        {
            SetupPopup((object)list);
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleTap();
        }

        protected override void OnDestroy()
        {
            if (tapButton != null) tapButton.onClick.RemoveListener(HandleTap);
            OnAllClaimed = null;
            rewards.Clear();

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Sang phần thưởng kế tiếp, hoặc nhận hết và đóng popup nếu đã xem xong.</summary>
        public void Claim()
        {
            HandleTap();
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (tapButton != null) tapButton.onClick.AddListener(HandleTap);
        }

        private void HandleTap()
        {
            if (Index < rewards.Count - 1)
            {
                Index++;
                RefreshCurrent();
                return;
            }

            GrantAll();

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();

            Action handler = OnAllClaimed;
            if (handler == null) return;

            try { handler.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void RefreshCurrent()
        {
            bool has = Index >= 0 && Index < rewards.Count;
            CollectibleReward reward = has ? rewards[Index] : null;

            if (titleText != null)
            {
                titleText.text = reward != null
                    ? StatItemCell.FormatRewardName(reward.Type) + " x" + Utilities.FormatCount(reward.Amount)
                    : "NO REWARD";
            }

            Sprite sprite = GetSprite(reward);
            if (rewardImage != null)
            {
                if (sprite != null) rewardImage.sprite = sprite;
                rewardImage.enabled = rewardImage.sprite != null;
            }

            if (glow != null) glow.SetActive(reward != null);
        }

        private Sprite GetSprite(CollectibleReward reward)
        {
            if (reward == null || rewardSpritesByType == null) return null;

            int index = (int)reward.Type;
            if (index < 0 || index >= rewardSpritesByType.Count) return null;

            return rewardSpritesByType[index];
        }

        private void GrantAll()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (gameData == null) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                CollectibleReward reward = rewards[i];
                if (reward != null) reward.GrantReward(gameData);
            }
        }
    }
}
