using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Popup xác nhận vào giải đấu: hiện phí tham gia (kèm icon coin/gem) và phần thưởng
    /// vô địch lấy từ <see cref="TournamentsData.tournament"/>.
    /// Nút Enter gọi <see cref="TournamentsData.TryEnter"/>, nút Cancel chỉ đóng popup.
    /// </summary>
    public class TournamentEntryPopup : PopupUI
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.TournamentEntryPopup;

        /// <summary>Tiêu đề popup.</summary>
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>Nút xác nhận vào giải.</summary>
        [SerializeField] private Button okButton;

        /// <summary>Nút huỷ.</summary>
        [SerializeField] private Button cancelButton;

        /// <summary>Ô chữ phí tham gia.</summary>
        [SerializeField] private TextMeshProUGUI entryFeeText;

        /// <summary>Biểu tượng loại tiền dùng để trả phí.</summary>
        [SerializeField] private Image currencyTypeIcon;

        /// <summary>Ảnh đồng coin.</summary>
        [SerializeField] private Sprite coinsIcon;

        /// <summary>Ảnh viên gem.</summary>
        [SerializeField] private Sprite gemsIcon;

        /// <summary>Node cha danh sách phần thưởng vô địch.</summary>
        [SerializeField] private Transform rewardsContainer;

        /// <summary>Prefab một dòng phần thưởng.</summary>
        [SerializeField] private StatItemCell rewardCellPrefab;

        /// <summary>Ô chữ báo lý do chưa vào giải được.</summary>
        [SerializeField] private TextMeshProUGUI reasonText;

        /// <summary>Gọi lại sau khi vào giải thành công; có thể null.</summary>
        public Action okButtonAction;

        /// <summary>Gọi lại khi người chơi huỷ; có thể null.</summary>
        public Action cancelButtonAction;

        private readonly List<StatItemCell> rewardCells = new List<StatItemCell>();
        private TournamentsData tournamentsData;
        private bool wired;

        /// <inheritdoc/>
        public override void OnInit()
        {
            Wire();
        }

        /// <inheritdoc/>
        /// <param name="data"><see cref="TournamentsData"/> tuỳ chọn; null sẽ lấy từ <see cref="GameBootstrap"/>.</param>
        public override void OnShow(object data)
        {
            Wire();

            tournamentsData = data as TournamentsData ?? ResolveTournamentsData();
            Refresh();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            okButtonAction = null;
            cancelButtonAction = null;
        }

        /// <summary>Nạp callback cho hai nút và vẽ lại nội dung.</summary>
        /// <param name="data">Nguồn dữ liệu giải đấu; null sẽ lấy từ <see cref="GameBootstrap"/>.</param>
        /// <param name="onEntered">Gọi lại sau khi vào giải thành công.</param>
        /// <param name="onCancel">Gọi lại khi người chơi huỷ.</param>
        public void SetupPopup(TournamentsData data, Action onEntered, Action onCancel = null)
        {
            Wire();

            tournamentsData = data ?? ResolveTournamentsData();
            okButtonAction = onEntered;
            cancelButtonAction = onCancel;

            Refresh();
        }

        /// <summary>Vẽ lại phí tham gia, phần thưởng và trạng thái nút Enter.</summary>
        public void Refresh()
        {
            Tournament tournament = tournamentsData != null ? tournamentsData.tournament : null;

            if (titleText != null)
            {
                titleText.text = tournament != null && !string.IsNullOrEmpty(tournament.name)
                    ? tournament.name
                    : "TOURNAMENT";
            }

            int fee = tournament != null ? tournament.entryCost : 0;
            CurrencyType currency = tournament != null ? tournament.entryCostType : CurrencyType.Coins;

            if (entryFeeText != null) entryFeeText.text = Utilities.FormatCount(fee);

            if (currencyTypeIcon != null)
            {
                Sprite sprite = currency == CurrencyType.Gems ? gemsIcon : coinsIcon;
                if (sprite != null) currencyTypeIcon.sprite = sprite;
                currencyTypeIcon.enabled = currencyTypeIcon.sprite != null;
            }

            RefreshRewards(tournament != null ? tournament.rewards : null);
            RefreshEligibility();
        }

        /// <inheritdoc/>
        public override void OnBackPressed()
        {
            HandleCancel();
        }

        protected override void OnDestroy()
        {
            if (okButton != null) okButton.onClick.RemoveListener(HandleEnter);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);

            okButtonAction = null;
            cancelButtonAction = null;
            rewardCells.Clear();

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void RefreshRewards(List<DynamicReward> list)
        {
            if (rewardsContainer == null || rewardCellPrefab == null) return;

            int count = list != null ? list.Count : 0;
            while (rewardCells.Count < count) rewardCells.Add(Instantiate(rewardCellPrefab, rewardsContainer));

            for (int i = 0; i < rewardCells.Count; i++)
            {
                StatItemCell cell = rewardCells[i];
                if (cell == null) continue;

                cell.Bind(i < count ? list[i] : null);
            }
        }

        private void RefreshEligibility()
        {
            if (tournamentsData == null)
            {
                if (reasonText != null) reasonText.text = "Tournament data is missing.";
                if (okButton != null) okButton.interactable = false;
                return;
            }

            GameData gameData = tournamentsData.gameData;
            int level = gameData != null && gameData.playerProfileData != null ? gameData.playerProfileData.level : 0;

            bool canEnter = tournamentsData.CanEnter(level, out string reason);

            if (okButton != null) okButton.interactable = canEnter;
            if (reasonText != null)
            {
                reasonText.text = reason ?? string.Empty;
                reasonText.gameObject.SetActive(!canEnter && !string.IsNullOrEmpty(reason));
            }
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (okButton != null) okButton.onClick.AddListener(HandleEnter);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);
        }

        private void HandleEnter()
        {
            if (tournamentsData == null) tournamentsData = ResolveTournamentsData();
            if (tournamentsData == null) return;

            if (!tournamentsData.TryEnter())
            {
                RefreshEligibility();
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Cannot enter the tournament");
                return;
            }

            Action callback = okButtonAction;
            Close();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void HandleCancel()
        {
            Action callback = cancelButtonAction;
            Close();

            if (callback == null) return;

            try { callback.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void Close()
        {
            okButtonAction = null;
            cancelButtonAction = null;

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();
        }

        private static TournamentsData ResolveTournamentsData()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            return gameData != null ? gameData.tournamentsData : null;
        }
    }
}
