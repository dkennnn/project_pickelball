using System;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Mảnh nối vòng lặp chơi: lắng nghe <see cref="GameManager.OnMatchEnded"/> rồi quy đổi kết quả
    /// trận đấu thành phần thưởng thật — coin, trophy, level và túi đồ bỏ vào locker.
    /// <para>
    /// Sau khi cộng/trừ xong, handler phát <see cref="OnMatchRewarded"/> kèm
    /// <see cref="MatchRewardResult"/> để màn hình kết quả hiển thị đúng những gì vừa xảy ra.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Dùng <see cref="IndestructibleSingleton{T}"/> chứ KHÔNG phải Singleton theo scene:
    /// <c>GameManager.OnMatchEnded</c> bắn TRỄ 5 giây sau GameOver (ResultScreenShowingTimeDelay).
    /// Nếu handler chết theo scene mà luồng chuyển về menu trước mốc đó thì người chơi mất trắng
    /// phần thưởng của trận vừa thắng.
    /// </remarks>
    public class MatchRewardHandler : IndestructibleSingleton<MatchRewardHandler>
    {
        /// <summary>Hub dữ liệu: ví tiền, hồ sơ người chơi, bảng level và locker.</summary>
        public GameData gameData;

        /// <summary>Cấu hình trận đấu: tiền cược và tiền thưởng thắng trận.</summary>
        public GameSettings settings;

        /// <summary>Trophy cộng thêm khi thắng.</summary>
        [Header("Trophy")]
        public int trophyOnWin = 30;

        /// <summary>Trophy bị trừ khi thua (giá trị âm).</summary>
        public int trophyOnLoss = -20;

        /// <summary>Sàn trophy — người chơi không bao giờ tụt xuống dưới mốc này.</summary>
        public int minTrophies = 0;

        /// <summary>Phát sau khi phần thưởng cuối trận đã được cộng/trừ xong.</summary>
        public static event Action<MatchRewardResult> OnMatchRewarded;

        /// <summary>Số trận thắng liên tiếp tối thiểu để được túi bậc 2.</summary>
        private const int WinsForMidKitbag = 3;

        /// <summary>Tổng hợp mọi thay đổi mà một trận đấu gây ra, dùng cho màn hình kết quả.</summary>
        [Serializable]
        public class MatchRewardResult
        {
            /// <summary>True nếu người chơi thắng trận.</summary>
            public bool playerWon;

            /// <summary>Số coin thực tế đã cộng (dương) hoặc bị trừ (âm) sau khi kẹp về quỹ 0.</summary>
            public int coinsDelta;

            /// <summary>Số trophy thực tế đã cộng/trừ sau khi kẹp về <see cref="minTrophies"/>.</summary>
            public int trophyDelta;

            /// <summary>Tổng trophy sau trận.</summary>
            public int newTrophies;

            /// <summary>Level sau trận.</summary>
            public int newLevel;

            /// <summary>True nếu trận này làm người chơi lên cấp.</summary>
            public bool leveledUp;

            /// <summary>Bậc túi đồ được trao; <see cref="KitbagType.None"/> nếu không có.</summary>
            public KitbagType kitbagAwarded = KitbagType.None;

            /// <summary>True nếu túi đã được cất vào locker thành công.</summary>
            public bool kitbagStored;

            /// <summary>True nếu locker đã đầy nên túi bị bỏ qua.</summary>
            public bool lockerFull;
        }

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            GameManager.OnMatchEnded += HandleMatchEnded;
        }

        private void OnDisable()
        {
            GameManager.OnMatchEnded -= HandleMatchEnded;
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            GameManager.OnMatchEnded -= HandleMatchEnded;
            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Xử lý kết thúc trận
        // ------------------------------------------------------------------

        /// <summary>
        /// Quy đổi teamID đội thắng thành "người chơi thắng hay thua" rồi trao thưởng.
        /// Người chơi cục bộ luôn là <c>GameManager.player1TeamID</c> ở bản offline
        /// (đúng quy ước của <c>StatsManager</c> và <c>GreyboxSceneBuilder</c>).
        /// </summary>
        /// <param name="winnerTeamID">TeamID của đội thắng do <see cref="GameManager"/> gửi tới.</param>
        private void HandleMatchEnded(string winnerTeamID)
        {
            if (string.IsNullOrEmpty(winnerTeamID))
            {
                Debug.LogWarning("[MatchRewardHandler] Trận kết thúc nhưng không có teamID đội thắng — bỏ qua trao thưởng.");
                return;
            }

            string playerTeamID = GetLocalPlayerTeamID();
            if (string.IsNullOrEmpty(playerTeamID))
            {
                Debug.LogWarning("[MatchRewardHandler] Không xác định được team của người chơi — bỏ qua trao thưởng.");
                return;
            }

            string opponentTeamID = GameManager.HasInstance ? GameManager.Instance.player2TeamID : null;
            if (winnerTeamID != playerTeamID && !string.IsNullOrEmpty(opponentTeamID) && winnerTeamID != opponentTeamID)
            {
                Debug.LogWarning($"[MatchRewardHandler] TeamID đội thắng '{winnerTeamID}' không khớp đội nào trong trận — bỏ qua trao thưởng.");
                return;
            }

            AwardMatch(winnerTeamID == playerTeamID);
        }

        /// <summary>
        /// Trao thưởng cuối trận: coin → trophy → level → túi đồ, rồi phát
        /// <see cref="OnMatchRewarded"/>. Trả về bảng tổng hợp kết quả.
        /// </summary>
        /// <param name="playerWon">Người chơi thắng trận này hay không.</param>
        public MatchRewardResult AwardMatch(bool playerWon)
        {
            MatchRewardResult result = new MatchRewardResult();
            result.playerWon = playerWon;

            if (gameData == null)
            {
                Debug.LogWarning("[MatchRewardHandler] Chưa gán GameData — không trao thưởng được.");
                return result;
            }

            ApplyCoins(playerWon, result);
            ApplyTrophiesAndLevel(playerWon, result);
            ApplyKitbag(playerWon, result);

            Action<MatchRewardResult> handler = OnMatchRewarded;
            if (handler != null) handler.Invoke(result);

            return result;
        }

        /// <summary>
        /// Thắng thì cộng <c>GameSettings.GetCareerWinRewardCoins()</c>, thua thì trừ
        /// <c>GameSettings.gameBetCoins</c> nhưng không cho quỹ xuống dưới 0.
        /// </summary>
        private void ApplyCoins(bool playerWon, MatchRewardResult result)
        {
            if (settings == null)
            {
                Debug.LogWarning("[MatchRewardHandler] Chưa gán GameSettings — bỏ qua phần thưởng coin.");
                return;
            }

            if (playerWon)
            {
                int reward = Mathf.Max(0, settings.GetCareerWinRewardCoins());
                gameData.IncreaseCoins(reward);
                result.coinsDelta = reward;
                return;
            }

            // Thua mất tiền cược, nhưng chỉ trừ tối đa bằng số coin đang có.
            int penalty = Mathf.Clamp(settings.gameBetCoins, 0, Mathf.Max(0, gameData.totalCoins));
            gameData.ReduceCoins(penalty);
            result.coinsDelta = -penalty;
        }

        /// <summary>
        /// Ghi nhận trận vào hồ sơ (số trận, chuỗi thắng, trophy) rồi tính lại level và
        /// phát hiện lên cấp bằng cách so level trước/sau.
        /// </summary>
        private void ApplyTrophiesAndLevel(bool playerWon, MatchRewardResult result)
        {
            PlayerProfileData profile = gameData.playerProfileData;
            if (profile == null)
            {
                Debug.LogWarning("[MatchRewardHandler] GameData chưa có PlayerProfileData — bỏ qua trophy/level.");
                return;
            }

            int levelBefore = profile.level;

            int trophyDelta = playerWon ? trophyOnWin : trophyOnLoss;
            int floor = Mathf.Max(0, minTrophies);
            if (profile.trophies + trophyDelta < floor) trophyDelta = floor - profile.trophies;

            profile.RecordMatch(playerWon, trophyDelta);
            profile.RecalculateLevel(gameData.playerLevels);

            result.trophyDelta = trophyDelta;
            result.newTrophies = profile.trophies;
            result.newLevel = profile.level;
            result.leveledUp = profile.level > levelBefore;
        }

        /// <summary>
        /// Chỉ khi thắng: chọn bậc túi theo chuỗi thắng liên tiếp rồi cất vào locker.
        /// Locker đầy thì vẫn tính là thắng, chỉ không nhận được túi.
        /// </summary>
        private void ApplyKitbag(bool playerWon, MatchRewardResult result)
        {
            if (!playerWon) return;

            if (gameData.slotsData == null)
            {
                Debug.LogWarning("[MatchRewardHandler] GameData chưa có SlotsData — bỏ qua phần thưởng túi đồ.");
                return;
            }

            int consecutiveWins = gameData.playerProfileData != null ? gameData.playerProfileData.consecutiveWins : 1;
            KitbagType type = GetKitbagTypeForStreak(consecutiveWins);

            result.kitbagAwarded = type;
            result.kitbagStored = gameData.slotsData.FillSlot(type);
            result.lockerFull = !result.kitbagStored;
        }

        /// <summary>
        /// Bậc túi tương ứng chuỗi thắng: từ <see cref="SlotsData.winsForHigherKitbag"/> (5) trở lên
        /// là <see cref="KitbagType.Level3"/>, từ 3 trở lên là <see cref="KitbagType.Level2"/>,
        /// còn lại <see cref="KitbagType.Level1"/>.
        /// </summary>
        /// <param name="consecutiveWins">Chuỗi trận thắng liên tiếp sau trận vừa rồi.</param>
        public static KitbagType GetKitbagTypeForStreak(int consecutiveWins)
        {
            if (consecutiveWins >= SlotsData.winsForHigherKitbag) return KitbagType.Level3;
            if (consecutiveWins >= WinsForMidKitbag) return KitbagType.Level2;
            return KitbagType.Level1;
        }

        /// <summary>TeamID của người chơi cục bộ; rỗng nếu chưa có <see cref="GameManager"/> trong scene.</summary>
        private static string GetLocalPlayerTeamID()
        {
            return GameManager.HasInstance ? GameManager.Instance.player1TeamID : string.Empty;
        }

        // ------------------------------------------------------------------
        // Tiện ích test
        // ------------------------------------------------------------------

        /// <summary>Trao thưởng như thật mà không cần đánh trận — dùng để kiểm thử vòng lặp.</summary>
        /// <param name="playerWon">Giả lập người chơi thắng hay thua.</param>
        public MatchRewardResult DebugAwardMatch(bool playerWon)
        {
            MatchRewardResult result = AwardMatch(playerWon);

            Debug.Log($"[MatchRewardHandler] Debug {(playerWon ? "THẮNG" : "THUA")}: " +
                      $"coin {result.coinsDelta:+#;-#;0}, trophy {result.trophyDelta:+#;-#;0} " +
                      $"(tổng {result.newTrophies}), level {result.newLevel}" +
                      (result.leveledUp ? " (LÊN CẤP)" : string.Empty) +
                      $", kitbag {result.kitbagAwarded}" +
                      (result.lockerFull ? " (locker đầy)" : string.Empty));

            return result;
        }

        /// <summary>Giả lập một trận thắng ngay trong Inspector.</summary>
        [ContextMenu("Debug Award Win")]
        private void DebugAwardWin()
        {
            DebugAwardMatch(true);
        }

        /// <summary>Giả lập một trận thua ngay trong Inspector.</summary>
        [ContextMenu("Debug Award Loss")]
        private void DebugAwardLoss()
        {
            DebugAwardMatch(false);
        }
    }
}
