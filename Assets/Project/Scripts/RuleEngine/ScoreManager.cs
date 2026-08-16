using System;
using Pickleball.Network;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quản lý tỉ số của trận đấu và các điều kiện kết thúc (win margin, deuce, advantage, match point).
    /// <para>
    /// LUẬT TÍNH ĐIỂM:
    /// <br/>• Thắng khi <c>score &gt;= settings.maxScore</c> VÀ <c>|score - other| &gt;= settings.winMargin</c>.
    /// <br/>• Nếu <c>winMargin == 0</c> (Quick Match) thì chạm <c>maxScore</c> là thắng ngay, không cần cách biệt.
    /// <br/>• Deuce xảy ra khi <c>winMargin &gt; 0</c> và cả hai bên đều đạt <c>maxScore - 1</c>.
    /// </para>
    /// <para>
    /// Phase 2 (Mirror): <see cref="player1Score"/>/<see cref="player2Score"/> sẽ trở thành <c>[SyncVar]</c>
    /// và <see cref="AddScore"/> sẽ được đánh dấu <c>[Server]</c> kèm một <c>[ClientRpc] RpcOnScoreUpdated</c>.
    /// </para>
    /// </summary>
    public class ScoreManager : NetworkSingleton<ScoreManager>
    {
        /// <summary>Điểm của đội 1. Phase 2 sẽ thành [SyncVar].</summary>
        public int player1Score;

        /// <summary>Điểm của đội 2. Phase 2 sẽ thành [SyncVar].</summary>
        public int player2Score;

        [SerializeField] private GameSettings settings;
        [SerializeField] private GameMessages gameMessages;

        /// <summary>Bắn ra mỗi khi tỉ số đổi: (điểm mới, teamID vừa ghi điểm).</summary>
        public event Action<int, string> OnScoreUpdated;

        /// <summary>TeamID của đội 1 — do <see cref="GameManager"/> gán khi khởi tạo trận.</summary>
        public string player1TeamID = "P1";

        /// <summary>TeamID của đội 2 — do <see cref="GameManager"/> gán khi khởi tạo trận.</summary>
        public string player2TeamID = "P2";

        /// <summary>Cấu hình đang dùng để chấm điểm (maxScore / winMargin).</summary>
        public GameSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        /// <summary>Kho thông báo dùng cho deuce / match point / advantage.</summary>
        public GameMessages Messages
        {
            get => gameMessages;
            set => gameMessages = value;
        }

        private int MaxScore => settings != null ? settings.maxScore : 6;
        private int WinMargin => settings != null ? Mathf.Max(0, settings.winMargin) : 2;

        // ------------------------------------------------------------------
        // Ghi điểm
        // ------------------------------------------------------------------

        /// <summary>
        /// Cộng một điểm cho đội <paramref name="teamID"/> và phát <see cref="OnScoreUpdated"/>.
        /// </summary>
        /// <param name="teamID">TeamID của đội vừa thắng pha bóng.</param>
        public void AddScore(string teamID) // [Server]
        {
            if (string.IsNullOrEmpty(teamID)) return;

            if (teamID == player1TeamID) player1Score++;
            else if (teamID == player2TeamID) player2Score++;
            else return;

            int newScore = GetScore(teamID);
            OnScoreUpdated?.Invoke(newScore, teamID); // [ClientRpc] RpcOnScoreUpdated ở Phase 2
        }

        /// <summary>Lấy điểm hiện tại của một đội; trả 0 nếu teamID không hợp lệ.</summary>
        public int GetScore(string teamID)
        {
            if (string.IsNullOrEmpty(teamID)) return 0;
            if (teamID == player1TeamID) return player1Score;
            if (teamID == player2TeamID) return player2Score;
            return 0;
        }

        /// <summary>Đưa tỉ số về 0-0 và phát <see cref="OnScoreUpdated"/> cho cả hai đội.</summary>
        public void ResetScore()
        {
            player1Score = 0;
            player2Score = 0;
            OnScoreUpdated?.Invoke(0, player1TeamID);
            OnScoreUpdated?.Invoke(0, player2TeamID);
        }

        // ------------------------------------------------------------------
        // Điều kiện kết thúc
        // ------------------------------------------------------------------

        /// <summary>True nếu một trong hai đội đã đủ điều kiện thắng trận.</summary>
        public bool CheckForGameComplete()
        {
            return IsWinningScore(player1Score, player2Score) || IsWinningScore(player2Score, player1Score);
        }

        /// <summary>
        /// TeamID của đội thắng, hoặc <c>string.Empty</c> nếu trận chưa kết thúc.
        /// </summary>
        public string GetWinner()
        {
            if (IsWinningScore(player1Score, player2Score)) return player1TeamID;
            if (IsWinningScore(player2Score, player1Score)) return player2TeamID;
            return string.Empty;
        }

        /// <summary>
        /// True khi đang ở thế deuce: cần cách biệt điểm (<c>winMargin &gt; 0</c>)
        /// và cả hai đội đều đã đạt <c>maxScore - 1</c>.
        /// </summary>
        public bool IsDeuce()
        {
            if (WinMargin <= 0) return false;
            int threshold = MaxScore - 1;
            return player1Score >= threshold && player2Score >= threshold;
        }

        /// <summary>
        /// True nếu đội <paramref name="teamID"/> chỉ cần thắng thêm một điểm nữa là thắng trận.
        /// </summary>
        public bool IsMatchPoint(string teamID)
        {
            if (string.IsNullOrEmpty(teamID)) return false;
            if (CheckForGameComplete()) return false;

            int mine = GetScore(teamID);
            int theirs = GetScore(GetOtherTeamID(teamID));
            return IsWinningScore(mine + 1, theirs);
        }

        /// <summary>
        /// True nếu đội <paramref name="teamID"/> đang có lợi thế trong deuce
        /// (đang ở thế deuce và dẫn trước đúng 1 điểm).
        /// </summary>
        /// <param name="teamID">Đội cần kiểm tra.</param>
        public bool IsAdvantage(string teamID)
        {
            if (string.IsNullOrEmpty(teamID)) return false;
            if (!IsDeuce()) return false;

            int mine = GetScore(teamID);
            int theirs = GetScore(GetOtherTeamID(teamID));
            return mine - theirs == 1;
        }

        /// <summary>
        /// Gộp ba trạng thái đặc biệt (advantage → deuce → match point) thành một lần kiểm tra
        /// và trả về thông báo tương ứng cho UI.
        /// </summary>
        /// <param name="teamID">Đội đang được xét (thường là người chơi cục bộ).</param>
        /// <param name="message">Thông báo lấy từ <see cref="GameMessages"/>, rỗng nếu không có gì đặc biệt.</param>
        /// <returns>True nếu có ít nhất một trạng thái đặc biệt đang diễn ra.</returns>
        public bool IsAnyDeuceCondition(string teamID, out string message)
        {
            if (IsAdvantage(teamID))
            {
                message = gameMessages != null ? gameMessages.advantageKey : "Advantage!";
                return true;
            }

            if (IsDeuce())
            {
                message = gameMessages != null ? gameMessages.deuceKey : "Deuce!";
                return true;
            }

            if (IsMatchPoint(teamID))
            {
                message = gameMessages != null ? gameMessages.matchPointKey : "Match Point!";
                return true;
            }

            message = string.Empty;
            return false;
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        /// <summary>TeamID của đối thủ của <paramref name="teamID"/>.</summary>
        public string GetOtherTeamID(string teamID)
        {
            if (teamID == player1TeamID) return player2TeamID;
            if (teamID == player2TeamID) return player1TeamID;
            return string.Empty;
        }

        /// <summary>Kiểm tra cặp điểm (mine, theirs) có phải là điểm thắng trận hay không.</summary>
        private bool IsWinningScore(int mine, int theirs)
        {
            int max = MaxScore;
            int margin = WinMargin;

            if (mine < max) return false;
            if (margin <= 0) return true; // Quick Match: chạm maxScore là thắng ngay.
            return Mathf.Abs(mine - theirs) >= margin;
        }
    }
}
