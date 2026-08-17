using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn locker: 8 ô chứa túi đồ, đếm ngược tới lúc mở được, trả gem để bỏ qua đồng hồ
    /// hoặc mở khoá ô. Mở túi sẽ sinh phần thưởng qua
    /// <see cref="RewardManager.GenerateKitbagRewards(KitbagType)"/>, cộng vào tài khoản
    /// rồi mở màn <see cref="ScreenType.RewardUI"/>.
    /// </summary>
    public class LockerUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.LockerUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Node cha chứa lưới ô locker.</summary>
        [SerializeField] private Transform content;

        /// <summary>Prefab một ô locker.</summary>
        [SerializeField] private LockerSlotCellView slotCellPrefab;

        private readonly List<LockerSlotCellView> cells = new List<LockerSlotCellView>();

        private SlotsData slotsData;
        private float tickTimer;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (title != null) title.text = "LOCKER";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            slotsData = gameData != null ? gameData.slotsData : null;

            if (slotsData != null) slotsData.OnSlotsChanged += RefreshSlots;

            RefreshSlots();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            if (slotsData != null) slotsData.OnSlotsChanged -= RefreshSlots;
        }

        protected override void OnDestroy()
        {
            if (slotsData != null) slotsData.OnSlotsChanged -= RefreshSlots;
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);

            for (int i = 0; i < cells.Count; i++)
            {
                LockerSlotCellView cell = cells[i];
                if (cell == null) continue;

                cell.OnOpenClicked -= HandleOpenSlot;
                cell.OnSkipTimerClicked -= HandleSkipTimer;
                cell.OnUnlockSlotClicked -= HandleUnlockSlot;
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
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].gameObject.activeSelf) cells[i].RefreshTimer();
            }
        }

        /// <summary>Dựng/vẽ lại toàn bộ ô locker.</summary>
        public void RefreshSlots()
        {
            if (content == null || slotCellPrefab == null || slotsData == null) return;

            int count = slotsData.slots != null ? slotsData.slots.Length : slotsData.maxSlots;

            while (cells.Count < count)
            {
                LockerSlotCellView cell = Instantiate(slotCellPrefab, content);
                cell.OnOpenClicked += HandleOpenSlot;
                cell.OnSkipTimerClicked += HandleSkipTimer;
                cell.OnUnlockSlotClicked += HandleUnlockSlot;
                cells.Add(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                LockerSlotCellView cell = cells[i];
                if (cell == null) continue;

                bool used = i < count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(slotsData, i);
            }
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void HandleOpenSlot(int index)
        {
            if (slotsData == null) return;

            KitbagType opened = slotsData.OpenSlot(index);
            if (opened == KitbagType.None) return;

            List<CollectibleReward> rewards = null;
            if (RewardManager.HasInstance) rewards = RewardManager.Instance.GenerateKitbagRewards(opened);

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (rewards != null && gameData != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i] != null) rewards[i].GrantReward(gameData);
                }
            }

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.RewardUI, rewards);
            RefreshSlots();
        }

        private void HandleSkipTimer(int index)
        {
            if (slotsData == null) return;

            if (!slotsData.SkipUnlockTimerWithGems(index))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough gems");
                return;
            }

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
            RefreshSlots();
        }

        private void HandleUnlockSlot(int index)
        {
            if (slotsData == null) return;

            if (!slotsData.UnlockSlotWithGems(index))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough gems");
                return;
            }

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
            RefreshSlots();
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
