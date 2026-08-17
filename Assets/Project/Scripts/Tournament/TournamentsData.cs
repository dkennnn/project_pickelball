using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Asset điều phối giải đấu: giữ định nghĩa giải, tiến độ hiện tại của người chơi
    /// và trận đang chờ thi đấu.
    /// <para>
    /// Luồng chuẩn: <see cref="CanEnter"/> → <see cref="TryEnter"/> → vào trận với
    /// <see cref="GetCurrentMatch"/> → <see cref="ReportMatchResult"/> sau mỗi trận.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "TournamentsData", menuName = "ScriptableObjects/TournamentsData")]
    public class TournamentsData : ScriptableObject
    {
        /// <summary>Dữ liệu người chơi: nguồn level, ví tiền và nơi cộng phần thưởng.</summary>
        public GameData gameData;

        /// <summary>Định nghĩa giải đấu hiện hành.</summary>
        public Tournament tournament;

        /// <summary>Tiến độ của người chơi trong giải.</summary>
        public PlayerTournamentProgress currentProgress = new PlayerTournamentProgress();

        /// <summary>Trận đang chờ thi đấu; <c>null</c> khi không có giải đang chạy.</summary>
        public TournamentMatch currentMatch;

        /// <summary>Phát mỗi khi tiến độ giải đổi (vào giải, thắng/thua một vòng, reset).</summary>
        public static event Action OnTournamentProgressChanged;

        // ------------------------------------------------------------------
        // Điều kiện tham gia
        // ------------------------------------------------------------------

        /// <summary>
        /// Kiểm tra người chơi có đủ điều kiện vào giải hay không.
        /// </summary>
        /// <param name="playerLevel">Level hiện tại của người chơi.</param>
        /// <param name="reason">Lý do bị từ chối; rỗng khi hợp lệ.</param>
        /// <returns>True nếu được phép vào giải.</returns>
        public bool CanEnter(int playerLevel, out string reason)
        {
            if (tournament == null)
            {
                reason = "Tournament is not configured.";
                return false;
            }

            if (!tournament.IsOpenNow())
            {
                reason = "Tournament is not open right now.";
                return false;
            }

            if (tournament.StageCount == 0)
            {
                reason = "Tournament has no stages.";
                return false;
            }

            if (playerLevel < tournament.requiredLevel)
            {
                reason = "Reach level " + tournament.requiredLevel + " to enter.";
                return false;
            }

            if (IsInProgress())
            {
                reason = "You are already in this tournament.";
                return false;
            }

            if (gameData == null)
            {
                reason = "Player data is missing.";
                return false;
            }

            bool enoughCurrency = tournament.entryCostType == CurrencyType.Gems
                ? gameData.totalGems >= tournament.entryCost
                : gameData.totalCoins >= tournament.entryCost;

            if (!enoughCurrency)
            {
                reason = "Not enough " + (tournament.entryCostType == CurrencyType.Gems ? "gems" : "coins") + ".";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Trả phí tham gia rồi mở giải từ vòng đầu tiên.
        /// Không trừ tiền nếu điều kiện tham gia chưa thoả.
        /// </summary>
        /// <returns>True nếu đã vào giải thành công.</returns>
        public bool TryEnter()
        {
            int playerLevel = gameData != null && gameData.playerProfileData != null
                ? gameData.playerProfileData.level
                : 0;

            if (!CanEnter(playerLevel, out string reason))
            {
                Debug.Log("[TournamentsData] Không thể vào giải: " + reason);
                return false;
            }

            bool paid = tournament.entryCostType == CurrencyType.Gems
                ? gameData.TrySpendGems(tournament.entryCost)
                : gameData.TrySpendCoins(tournament.entryCost);

            if (!paid) return false;

            if (currentProgress == null) currentProgress = new PlayerTournamentProgress();
            currentProgress.Reset(tournament.id);
            currentMatch = GetCurrentMatch();

            OnTournamentProgressChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------
        // Truy vấn tiến độ
        // ------------------------------------------------------------------

        /// <summary>
        /// Vòng đấu người chơi đang tham gia; <c>null</c> nếu chưa vào giải,
        /// đã bị loại hoặc đã vô địch.
        /// </summary>
        public TournamentStage GetCurrentStage()
        {
            if (!IsInProgress()) return null;
            return tournament.GetStage(currentProgress.currentStage);
        }

        /// <summary>Trận của người chơi ở vòng hiện tại; <c>null</c> nếu không có.</summary>
        public TournamentMatch GetCurrentMatch()
        {
            TournamentStage stage = GetCurrentStage();
            return stage?.playerMatch;
        }

        /// <summary>True khi người chơi đang trong một giải chưa kết thúc.</summary>
        public bool IsInProgress()
        {
            if (tournament == null || currentProgress == null) return false;
            if (string.IsNullOrEmpty(currentProgress.tournamentId)) return false;
            if (currentProgress.tournamentId != tournament.id) return false;

            return !currentProgress.isEliminated && !currentProgress.isCompleted;
        }

        // ------------------------------------------------------------------
        // Ghi nhận kết quả
        // ------------------------------------------------------------------

        /// <summary>
        /// Ghi nhận kết quả trận vừa đấu.
        /// Thắng thì lên vòng sau, hoặc vô địch (đánh dấu <c>isCompleted</c> và cấp
        /// toàn bộ <c>tournament.rewards</c>) nếu đó là vòng cuối. Thua thì bị loại.
        /// </summary>
        /// <param name="playerWon">Người chơi có thắng trận vừa rồi hay không.</param>
        public void ReportMatchResult(bool playerWon)
        {
            if (!IsInProgress()) return;

            if (!playerWon)
            {
                currentProgress.isEliminated = true;
                currentMatch = null;
                OnTournamentProgressChanged?.Invoke();
                return;
            }

            int nextStage = currentProgress.currentStage + 1;

            if (nextStage >= tournament.StageCount)
            {
                currentProgress.isCompleted = true;
                currentMatch = null;
                GrantTournamentRewards();
            }
            else
            {
                currentProgress.currentStage = nextStage;
                currentMatch = GetCurrentMatch();
            }

            OnTournamentProgressChanged?.Invoke();
        }

        /// <summary>Xoá sạch tiến độ giải, đưa người chơi về trạng thái chưa tham gia.</summary>
        public void ResetProgress()
        {
            if (currentProgress == null) currentProgress = new PlayerTournamentProgress();
            currentProgress.Reset(string.Empty);
            currentMatch = null;

            OnTournamentProgressChanged?.Invoke();
        }

        /// <summary>
        /// Áp tiến độ đã lưu từ hệ save vào asset và đồng bộ lại trận hiện tại.
        /// </summary>
        /// <param name="progress">Tiến độ đã lưu; <c>null</c> tương đương xoá tiến độ.</param>
        public void ApplyProgress(PlayerTournamentProgress progress)
        {
            currentProgress = progress ?? new PlayerTournamentProgress();
            currentMatch = GetCurrentMatch();

            OnTournamentProgressChanged?.Invoke();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Cộng toàn bộ phần thưởng vô địch vào tài khoản người chơi.</summary>
        private void GrantTournamentRewards()
        {
            if (gameData == null || tournament == null) return;

            List<DynamicReward> rewards = tournament.rewards;
            if (rewards == null) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                DynamicReward reward = rewards[i];
                if (reward == null) continue;
                gameData.GrantReward(reward.rewardType, reward.value);
            }
        }
    }
}
