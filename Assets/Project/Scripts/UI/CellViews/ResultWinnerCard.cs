using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ người thắng ở màn kết quả (prefab <c>WinnerPanelCellView</c>): avatar, tên,
    /// điểm số và số cúp cộng thêm.
    /// </summary>
    public class ResultWinnerCard : MonoBehaviour
    {
        /// <summary>Ảnh đại diện (node <c>WinnerImage/PlayerIconPanel/PlayerIcon</c>).</summary>
        [SerializeField] private Image playerIcon;

        /// <summary>Tên người chơi (node <c>NamePanel/PlayerNamePanel</c>).</summary>
        [SerializeField] private TextMeshProUGUI playerNamePanel;

        /// <summary>Điểm số (node <c>ScorePanel/ScoreTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI scoreTxt;

        /// <summary>Chữ "WINNER" (node <c>WinnerImage/WinnerTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI winnerTxt;

        /// <summary>Vương miện (node <c>NamePanel/CrownImg</c>).</summary>
        [SerializeField] private GameObject crownImg;

        /// <summary>Ô chữ cúp cộng thêm; layout gốc không có, để trống thì bỏ qua.</summary>
        [SerializeField] private TextMeshProUGUI trophyText;

        /// <summary>Chữ mặc định trên băng rôn thắng.</summary>
        [SerializeField] private string winnerLabel = "WINNER";

        /// <summary>Tên đang hiển thị.</summary>
        public string PlayerName { get; private set; }

        /// <summary>Điểm đang hiển thị.</summary>
        public int Points { get; private set; }

        /// <summary>
        /// Nạp dữ liệu người thắng — giữ nguyên chữ ký của bản gốc.
        /// </summary>
        /// <param name="playerName">Tên người thắng.</param>
        /// <param name="iconSprite">Ảnh đại diện; null sẽ giữ ảnh sẵn có.</param>
        /// <param name="points">Điểm số cuối trận.</param>
        public void Setup(string playerName, Sprite iconSprite, int points)
        {
            Bind(playerName, iconSprite, points, 0);
        }

        /// <summary>
        /// Nạp dữ liệu người thắng kèm số cúp cộng thêm.
        /// </summary>
        /// <param name="playerName">Tên người thắng.</param>
        /// <param name="iconSprite">Ảnh đại diện; null sẽ giữ ảnh sẵn có.</param>
        /// <param name="points">Điểm số cuối trận.</param>
        /// <param name="trophyDelta">Số cúp cộng thêm; nhỏ hơn hoặc bằng 0 sẽ ẩn ô chữ cúp.</param>
        public void Bind(string playerName, Sprite iconSprite, int points, int trophyDelta)
        {
            PlayerName = playerName ?? string.Empty;
            Points = points;

            if (playerNamePanel != null) playerNamePanel.text = PlayerName;
            if (scoreTxt != null) scoreTxt.text = Utilities.FormatCount(points);
            if (winnerTxt != null) winnerTxt.text = winnerLabel;
            if (crownImg != null) crownImg.SetActive(true);

            if (playerIcon != null)
            {
                if (iconSprite != null) playerIcon.sprite = iconSprite;
                playerIcon.enabled = playerIcon.sprite != null;
            }

            if (trophyText != null)
            {
                bool show = trophyDelta > 0;
                trophyText.gameObject.SetActive(show);
                if (show) trophyText.text = "+" + Utilities.FormatCount(trophyDelta);
            }
        }
    }
}
