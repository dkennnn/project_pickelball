using System.Collections;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn khoe phần thưởng: chạm để mở, rồi lần lượt hiện các phần thưởng vừa nhận.
    /// Nhận qua tham số <c>data</c> một <c>List&lt;CollectibleReward&gt;</c> hoặc
    /// <c>List&lt;DynamicReward&gt;</c>. Phần thưởng đã được cộng vào tài khoản từ trước,
    /// màn này chỉ trình bày.
    /// </summary>
    public class RewardUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.RewardUI;

        /// <summary>Chữ "TAP TO OPEN".</summary>
        [SerializeField] private TextMeshProUGUI tapToOpen;

        /// <summary>Nút phủ toàn màn để chạm mở.</summary>
        [SerializeField] private Button tapButton;

        /// <summary>Node cha chứa danh sách phần thưởng.</summary>
        [SerializeField] private Transform rewardsContainer;

        /// <summary>Prefab một dòng phần thưởng.</summary>
        [SerializeField] private StatItemCell rewardCellPrefab;

        /// <summary>Khoảng cách thời gian giữa hai phần thưởng hiện ra, tính bằng giây.</summary>
        [SerializeField] private float revealInterval = 0.15f;

        /// <summary>True khi người chơi đã chạm mở và các phần thưởng đã hiện hết.</summary>
        public bool IsOpened { get; private set; }

        private readonly List<StatItemCell> cells = new List<StatItemCell>();
        private readonly List<string> labels = new List<string>();
        private readonly List<string> values = new List<string>();

        private Coroutine revealRoutine;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (tapButton != null) tapButton.onClick.AddListener(HandleTap);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            BuildRows(data);

            IsOpened = false;
            if (tapToOpen != null)
            {
                tapToOpen.gameObject.SetActive(true);
                tapToOpen.text = "TAP TO OPEN";
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null) cells[i].gameObject.SetActive(false);
            }
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            StopReveal();
        }

        protected override void OnDestroy()
        {
            StopReveal();
            if (tapButton != null) tapButton.onClick.RemoveListener(HandleTap);
            cells.Clear();

            base.OnDestroy();
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleTap();
        }

        /// <summary>Chuyển dữ liệu phần thưởng thành các cặp nhãn — giá trị để hiển thị.</summary>
        /// <param name="data">Danh sách phần thưởng nhận từ màn gọi.</param>
        private void BuildRows(object data)
        {
            labels.Clear();
            values.Clear();

            if (data is List<CollectibleReward> collectibles)
            {
                for (int i = 0; i < collectibles.Count; i++)
                {
                    CollectibleReward reward = collectibles[i];
                    if (reward == null) continue;

                    labels.Add(StatItemCell.FormatRewardName(reward.Type));
                    values.Add("x" + Utilities.FormatCount(reward.Amount));
                }
            }
            else if (data is List<DynamicReward> dynamics)
            {
                for (int i = 0; i < dynamics.Count; i++)
                {
                    DynamicReward reward = dynamics[i];
                    if (reward == null) continue;

                    labels.Add(StatItemCell.FormatRewardName(reward.rewardType));
                    values.Add("x" + Utilities.FormatCount(reward.value));
                }
            }

            if (rewardsContainer == null || rewardCellPrefab == null) return;

            while (cells.Count < labels.Count) cells.Add(Instantiate(rewardCellPrefab, rewardsContainer));
        }

        private void HandleTap()
        {
            if (!IsOpened)
            {
                Open();
                return;
            }

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();
        }

        /// <summary>Bắt đầu hiện lần lượt các phần thưởng.</summary>
        public void Open()
        {
            if (tapToOpen != null) tapToOpen.gameObject.SetActive(false);

            StopReveal();
            if (isActiveAndEnabled) revealRoutine = StartCoroutine(RevealRoutine());
            else RevealAll();
        }

        private IEnumerator RevealRoutine()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                StatItemCell cell = cells[i];
                if (cell == null) continue;

                bool used = i < labels.Count;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                cell.Bind(labels[i], values[i]);
                if (revealInterval > 0f) yield return new WaitForSecondsRealtime(revealInterval);
            }

            revealRoutine = null;
            IsOpened = true;

            if (tapToOpen != null)
            {
                tapToOpen.gameObject.SetActive(true);
                tapToOpen.text = "TAP TO CONTINUE";
            }
        }

        private void RevealAll()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                StatItemCell cell = cells[i];
                if (cell == null) continue;

                bool used = i < labels.Count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(labels[i], values[i]);
            }
            IsOpened = true;
        }

        private void StopReveal()
        {
            if (revealRoutine == null) return;

            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
    }
}
