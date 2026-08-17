using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn kết quả cuối trận. Tự hiện khi <see cref="MatchRewardHandler.OnMatchRewarded"/> phát,
    /// trình bày thắng/thua, coin và trophy nhận được, level mới, túi đồ nhận được và
    /// bảng thống kê lấy từ <see cref="StatsManager"/>.
    /// </summary>
    public class ResultUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.GameComplete;

        /// <summary>Biểu tượng VICTORY/DEFEAT.</summary>
        [SerializeField] private Image winnerIcon;

        /// <summary>Ảnh thay cho <see cref="winnerIcon"/> khi thua.</summary>
        [SerializeField] private Sprite defeatSprite;

        /// <summary>Ảnh dùng cho <see cref="winnerIcon"/> khi thắng.</summary>
        [SerializeField] private Sprite victorySprite;

        /// <summary>Nút "TAP TO CONTINUE" quay lại màn chính.</summary>
        [SerializeField] private Button continueButton;

        /// <summary>Bảng phần thưởng.</summary>
        [SerializeField] private GameObject rewardPanel;

        /// <summary>Số coin nhận được.</summary>
        [SerializeField] private TextMeshProUGUI coinRewardCountTxt;

        /// <summary>Số trophy thay đổi.</summary>
        [SerializeField] private TextMeshProUGUI trophyRewardCountTxt;

        /// <summary>Thông báo lên level mới.</summary>
        [SerializeField] private TextMeshProUGUI newLevelText;

        /// <summary>Ảnh túi đồ nhận được.</summary>
        [SerializeField] private Image kitbagIcon;

        /// <summary>Chữ mô tả túi đồ nhận được.</summary>
        [SerializeField] private TextMeshProUGUI kitbagLabel;

        /// <summary>Node cha bên người chơi (thẻ Winner/Loser).</summary>
        [SerializeField] private Transform player;

        /// <summary>Node cha bên đối thủ (thẻ Winner/Loser).</summary>
        [SerializeField] private Transform opponent;

        /// <summary>Vương miện đặt trên bên thắng.</summary>
        [SerializeField] private GameObject crown;

        /// <summary>Biểu tượng "VS" ở giữa.</summary>
        [SerializeField] private GameObject vsIcon;

        /// <summary>Nút nhân đôi coin (chưa dùng ở bản offline).</summary>
        [SerializeField] private Button get2XCoins;

        /// <summary>Nút hoàn coin (chưa dùng ở bản offline).</summary>
        [SerializeField] private Button recoverCoins;

        /// <summary>Node cha chứa bảng thống kê.</summary>
        [SerializeField] private Transform statsContainer;

        /// <summary>Prefab một dòng thống kê.</summary>
        [SerializeField] private StatItemCell statItemPrefab;

        /// <summary>Kết quả trao thưởng của trận vừa xong.</summary>
        public MatchRewardHandler.MatchRewardResult LastResult { get; private set; }

        private readonly List<StatItemCell> statCells = new List<StatItemCell>();

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        private void OnEnable()
        {
            MatchRewardHandler.OnMatchRewarded += HandleMatchRewarded;
        }

        private void OnDisable()
        {
            MatchRewardHandler.OnMatchRewarded -= HandleMatchRewarded;
        }

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);

            // Bản offline không có quảng cáo/IAP nên hai nút này luôn tắt.
            if (get2XCoins != null) get2XCoins.gameObject.SetActive(false);
            if (recoverCoins != null) recoverCoins.gameObject.SetActive(false);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            if (data is MatchRewardHandler.MatchRewardResult result) LastResult = result;

            RefreshResult();
            RefreshStats();
        }

        protected override void OnDestroy()
        {
            MatchRewardHandler.OnMatchRewarded -= HandleMatchRewarded;
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinue);
            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Cập nhật hiển thị
        // ------------------------------------------------------------------

        /// <summary>Vẽ lại phần thắng/thua và bảng phần thưởng theo <see cref="LastResult"/>.</summary>
        public void RefreshResult()
        {
            MatchRewardHandler.MatchRewardResult result = LastResult;
            if (result == null)
            {
                if (rewardPanel != null) rewardPanel.SetActive(false);
                return;
            }

            if (winnerIcon != null)
            {
                Sprite sprite = result.playerWon ? victorySprite : defeatSprite;
                if (sprite != null) winnerIcon.sprite = sprite;
                winnerIcon.enabled = winnerIcon.sprite != null;
            }

            if (rewardPanel != null) rewardPanel.SetActive(true);

            if (coinRewardCountTxt != null)
            {
                coinRewardCountTxt.text = (result.coinsDelta >= 0 ? "+" : "-")
                                          + Utilities.FormatCount(Mathf.Abs(result.coinsDelta));
            }

            if (trophyRewardCountTxt != null)
            {
                trophyRewardCountTxt.text = (result.trophyDelta >= 0 ? "+" : "-")
                                            + Utilities.FormatCount(Mathf.Abs(result.trophyDelta));
            }

            if (newLevelText != null)
            {
                newLevelText.gameObject.SetActive(result.leveledUp);
                if (result.leveledUp) newLevelText.text = "LEVEL " + result.newLevel;
            }

            bool gotKitbag = result.kitbagAwarded != KitbagType.None;

            if (kitbagIcon != null)
            {
                kitbagIcon.gameObject.SetActive(gotKitbag);
                if (gotKitbag)
                {
                    GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
                    Shop shop = gameData != null ? gameData.shopData : null;
                    Sprite sprite = shop != null ? shop.GetKitbagSprite(result.kitbagAwarded) : null;
                    if (sprite != null) kitbagIcon.sprite = sprite;
                }
            }

            if (kitbagLabel != null)
            {
                kitbagLabel.gameObject.SetActive(gotKitbag);
                if (gotKitbag)
                {
                    kitbagLabel.text = result.lockerFull
                        ? "LOCKER FULL"
                        : (result.kitbagStored ? "KITBAG ADDED" : result.kitbagAwarded.ToString());
                }
            }

            if (crown != null) crown.SetActive(result.playerWon);
            if (vsIcon != null) vsIcon.SetActive(true);
            if (player != null) player.gameObject.SetActive(true);
            if (opponent != null) opponent.gameObject.SetActive(true);
        }

        /// <summary>Dựng lại bảng thống kê trận từ <see cref="StatsManager"/>.</summary>
        public void RefreshStats()
        {
            if (statsContainer == null || statItemPrefab == null) return;

            List<KeyValuePair<string, string>> rows = BuildStatRows();

            while (statCells.Count < rows.Count)
            {
                statCells.Add(Instantiate(statItemPrefab, statsContainer));
            }

            for (int i = 0; i < statCells.Count; i++)
            {
                StatItemCell cell = statCells[i];
                if (cell == null) continue;

                bool used = i < rows.Count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(rows[i].Key, rows[i].Value);
            }
        }

        private List<KeyValuePair<string, string>> BuildStatRows()
        {
            List<KeyValuePair<string, string>> rows = new List<KeyValuePair<string, string>>();

            if (!StatsManager.HasInstance) return rows;

            GameStatsData stats = StatsManager.Instance.statsData;
            if (stats == null) return rows;

            string teamID = GameManager.HasInstance ? GameManager.Instance.player1TeamID : "P1";
            PlayerStatsData playerStats = stats.GetPlayerStats(teamID);
            if (playerStats == null || playerStats.StatsData == null) return rows;

            StatsData data = playerStats.StatsData;
            rows.Add(new KeyValuePair<string, string>("TOTAL SHOTS", data.totalShots.ToString()));
            rows.Add(new KeyValuePair<string, string>("VOLLEYS", data.volleys.ToString()));
            rows.Add(new KeyValuePair<string, string>("WINNERS", data.outrightWinners.ToString()));
            rows.Add(new KeyValuePair<string, string>("LONGEST RALLY", data.maxShotsInRally.ToString()));
            rows.Add(new KeyValuePair<string, string>("FASTEST SHOT", data.fastestShot.ToString("0.0")));
            rows.Add(new KeyValuePair<string, string>("MISSERVES", data.misserves.ToString()));
            rows.Add(new KeyValuePair<string, string>("OUT OF BOUNDS", data.outOfBounds.ToString()));
            rows.Add(new KeyValuePair<string, string>("KITCHEN FAULTS", data.kitchen.ToString()));
            return rows;
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void HandleMatchRewarded(MatchRewardHandler.MatchRewardResult result)
        {
            LastResult = result;

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.GameComplete, result);
            else Show(result);
        }

        private void HandleContinue()
        {
            if (!UIController.HasInstance)
            {
                Hide();
                return;
            }

            UIController.Instance.Show(ScreenType.MainMenu);
        }
    }
}
