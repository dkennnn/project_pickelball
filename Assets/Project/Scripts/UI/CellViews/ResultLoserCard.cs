using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ người thua ở màn kết quả (prefab <c>LoserPanelCellView</c>): avatar, tên,
    /// điểm số và số cúp bị trừ.
    /// </summary>
    public class ResultLoserCard : MonoBehaviour
    {
        /// <summary>Ảnh đại diện (node <c>PlayerIconPanel/PlayerIcon</c>).</summary>
        [SerializeField] private Image playerIcon;

        /// <summary>Tên người chơi (node <c>NamePanel/PlayerNamePanel</c>).</summary>
        [SerializeField] private TextMeshProUGUI playerNamePanel;

        /// <summary>Điểm số (node <c>ScorePanel/ScoreTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI scoreTxt;

        /// <summary>Chữ "DEFEAT" (node <c>DefeatIcon/DefeatTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI defeatTxt;

        /// <summary>Ô chữ cúp bị trừ; layout gốc không có, để trống thì bỏ qua.</summary>
        [SerializeField] private TextMeshProUGUI trophyText;

        /// <summary>Chữ mặc định trên băng rôn thua.</summary>
        [SerializeField] private string defeatLabel = "DEFEAT";

        /// <summary>Tên đang hiển thị.</summary>
        public string PlayerName { get; private set; }

        /// <summary>Điểm đang hiển thị.</summary>
        public int Points { get; private set; }

        /// <summary>
        /// Nạp dữ liệu người thua — giữ nguyên chữ ký của bản gốc.
        /// </summary>
        /// <param name="playerName">Tên người thua.</param>
        /// <param name="iconSprite">Ảnh đại diện; null sẽ giữ ảnh sẵn có.</param>
        /// <param name="points">Điểm số cuối trận.</param>
        public void Setup(string playerName, Sprite iconSprite, int points)
        {
            Bind(playerName, iconSprite, points, 0);
        }

        /// <summary>
        /// Nạp dữ liệu người thua kèm số cúp bị trừ.
        /// </summary>
        /// <param name="playerName">Tên người thua.</param>
        /// <param name="iconSprite">Ảnh đại diện; null sẽ giữ ảnh sẵn có.</param>
        /// <param name="points">Điểm số cuối trận.</param>
        /// <param name="trophyDelta">Số cúp bị trừ (giá trị dương); 0 sẽ ẩn ô chữ cúp.</param>
        public void Bind(string playerName, Sprite iconSprite, int points, int trophyDelta)
        {
            PlayerName = playerName ?? string.Empty;
            Points = points;

            if (playerNamePanel != null) playerNamePanel.text = PlayerName;
            if (scoreTxt != null) scoreTxt.text = Utilities.FormatCount(points);
            if (defeatTxt != null) defeatTxt.text = defeatLabel;

            if (playerIcon != null)
            {
                if (iconSprite != null) playerIcon.sprite = iconSprite;
                playerIcon.enabled = playerIcon.sprite != null;
            }

            if (trophyText != null)
            {
                int amount = Mathf.Abs(trophyDelta);
                bool show = amount > 0;

                trophyText.gameObject.SetActive(show);
                if (show) trophyText.text = "-" + Utilities.FormatCount(amount);
            }
        }
    }
}
