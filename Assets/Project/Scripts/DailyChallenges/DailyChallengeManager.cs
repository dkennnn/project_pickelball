using System;
using System.Collections.Generic;
using CommanTickManager;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quản lý bộ nhiệm vụ hằng ngày: sinh nhiệm vụ theo bậc trình độ người chơi,
    /// tự cập nhật tiến độ từ các sự kiện gameplay có sẵn, phát thưởng và reset mỗi ngày.
    ///
    /// <para><b>Quy đổi sự kiện → tiến độ</b>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="GameManager.OnMatchEnded"/> → <c>PlayMatches</c> +1,
    /// <c>WinMatches</c> +1 nếu người chơi thắng, <c>WinWithoutLosingPoint</c> +1 nếu thắng mà đối thủ chưa ghi điểm.</description></item>
    /// <item><description><see cref="GameManager.OnGameStateChanged"/> → mở/đóng đồng hồ pha bóng và đồng hồ trận,
    /// chốt <c>RallySeconds</c> (pha bóng dài nhất) và <c>PlayMinutesInGame</c> (trận dài nhất).</description></item>
    /// <item><description><c>BallController.OnBallHit</c> + <c>BallController.OnBallCollision</c> →
    /// <c>HitVolleys</c> (cú đánh khi bóng chưa nảy kể từ cú đánh trước, trong pha bóng sống).</description></item>
    /// <item><description><c>ScoreManager.OnScoreUpdated</c> → <c>ScorePoints</c> +1 mỗi điểm người chơi ghi được.</description></item>
    /// <item><description><see cref="Tick"/> → <c>PlayMinutes</c> cộng dồn thời gian thi đấu thực tế.</description></item>
    /// </list>
    ///
    /// <para>Các loại <c>UpgradeItem</c>, <c>OpenKitbag</c>, <c>UseBooster</c> chưa có nguồn sự kiện tự động;
    /// hệ Shop/Locker/Booster gọi thẳng <see cref="UpdateChallengeProgress"/> khi được bổ sung.</para>
    /// </summary>
    public class DailyChallengeManager : Singleton<DailyChallengeManager>, ITick
    {
        /// <summary>Dữ liệu người chơi: nguồn bảng <c>dailyChallengeLevels</c> và nơi cộng phần thưởng.</summary>
        public GameData gameData;

        /// <summary>Bậc trình độ đang dùng để sinh mục tiêu nhiệm vụ.</summary>
        public AIDifficulty CurrentExperienceLevel;

        /// <summary>Bộ nhiệm vụ của ngày hôm nay.</summary>
        public List<DailyChallenge> dailyChallenges = new List<DailyChallenge>();

        /// <summary>Số nhiệm vụ phát ra mỗi ngày.</summary>
        private readonly int maxChallengesCount = 3;

        /// <summary>Chu kỳ kiểm tra mốc reset (giây).</summary>
        private float timerUpdateInterval = 1f;

        /// <summary>Thời gian đã trôi kể từ lần kiểm tra mốc reset gần nhất.</summary>
        private float timeSinceLastUpdate;

        /// <summary>Mốc reset gần nhất (giờ UTC).</summary>
        private DateTime lastResetTime;

        /// <summary>Phát mỗi khi danh sách nhiệm vụ hoặc tiến độ thay đổi (UI lắng nghe để vẽ lại).</summary>
        public static event Action OnChallengesUpdated;

        // ------------------------------------------------------------------
        // Trạng thái nội bộ của bộ theo dõi sự kiện
        // ------------------------------------------------------------------

        /// <summary>True khi đã subscribe các event static (chống subscribe lặp).</summary>
        private bool eventsSubscribed;

        /// <summary>Instance ScoreManager đang được subscribe (OnScoreUpdated là event instance).</summary>
        private ScoreManager subscribedScore;

        /// <summary>Instance BallController đang được subscribe (OnBallCollision là event instance).</summary>
        private BallController subscribedBall;

        /// <summary>Trạng thái trận gần nhất nhận được từ <see cref="GameManager"/>.</summary>
        private GameState currentGameState = GameState.PreServe;

        /// <summary>Thời gian pha bóng đang diễn ra (giây).</summary>
        private float currentRallySeconds;

        /// <summary>Thời gian thi đấu của trận đang diễn ra (giây).</summary>
        private float currentGameSeconds;

        /// <summary>Thời gian thi đấu đã tích luỹ nhưng chưa đẩy vào nhiệm vụ <c>PlayMinutes</c>.</summary>
        private float pendingPlaySeconds;

        /// <summary>Số lần bóng chạm đất kể từ cú đánh gần nhất — dùng để phân biệt volley.</summary>
        private int bouncesSinceLastHit;

        /// <summary>True khi đồng hồ pha bóng đang chạy.</summary>
        private bool isRallyActive;

        /// <summary>Định dạng ISO round-trip dùng cho mốc thời gian lưu trữ.</summary>
        private const string IsoFormat = "o";

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();

            if (dailyChallenges == null) dailyChallenges = new List<DailyChallenge>();

            RefreshExperienceLevel();
            RegisterTick();
            SubscribeToEvents();
            CheckAndResetDailyChallenges();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            UnregisterTick();
            base.OnDestroy();
        }

        /// <summary>Đăng ký nhận Tick từ <see cref="ProcessingUpdate"/>.</summary>
        public void RegisterTick()
        {
            ProcessingUpdate.Add(this);
        }

        /// <summary>Huỷ đăng ký nhận Tick.</summary>
        public void UnregisterTick()
        {
            ProcessingUpdate.Remove(this);
        }

        /// <summary>
        /// Chạy mỗi frame: cộng dồn thời gian thi đấu / thời gian pha bóng, thử subscribe muộn
        /// các event dạng instance và kiểm tra mốc reset mỗi <see cref="timerUpdateInterval"/> giây.
        /// </summary>
        public void Tick()
        {
            float dt = Time.deltaTime;

            TrySubscribeInstanceEvents();
            AccumulatePlayTime(dt);

            timeSinceLastUpdate += dt;
            if (timeSinceLastUpdate < timerUpdateInterval) return;

            timeSinceLastUpdate = 0f;
            FlushPlayTime();
            CheckAndResetDailyChallenges();
        }

        // ------------------------------------------------------------------
        // Sinh nhiệm vụ
        // ------------------------------------------------------------------

        /// <summary>
        /// Sinh lại bộ nhiệm vụ của ngày: bốc tối đa <c>maxChallengesCount</c> loại nhiệm vụ
        /// khác nhau từ bảng khoảng mục tiêu của bậc trình độ hiện tại.
        /// Ghi đè hoàn toàn danh sách cũ và phát <see cref="OnChallengesUpdated"/>.
        /// </summary>
        /// <returns>Danh sách nhiệm vụ vừa sinh.</returns>
        public List<DailyChallenge> AssignDailyChallenges()
        {
            RefreshExperienceLevel();

            List<ChallengeType> pool = GetAvailableChallengeTypes();
            List<DailyChallenge> assigned = new List<DailyChallenge>();

            int count = Mathf.Min(maxChallengesCount, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                ChallengeType type = pool[pick];
                pool.RemoveAt(pick);

                int target = GenerateTargetValue(type);
                string id = type + "_" + DateTime.UtcNow.ToString("yyyyMMdd") + "_" + i;

                assigned.Add(new DailyChallenge(id, type, target,
                    GenerateDescription(type, target), GenerateRandomReward()));
            }

            dailyChallenges = assigned;
            OnChallengesUpdated?.Invoke();
            return dailyChallenges;
        }

        /// <summary>
        /// Bốc một giá trị mục tiêu trong khoảng cấu hình của bậc trình độ hiện tại.
        /// Trả về 1 nếu chưa có bảng cấu hình cho loại nhiệm vụ đó.
        /// </summary>
        /// <param name="type">Loại nhiệm vụ.</param>
        public int GenerateTargetValue(ChallengeType type)
        {
            ChallengeRange range = GetRange(type);
            if (range == null) return 1;

            int min = Mathf.Max(1, range.MinValue);
            int max = Mathf.Max(min, range.MaxValue);
            return UnityEngine.Random.Range(min, max + 1);
        }

        /// <summary>
        /// Sinh mô tả hiển thị cho một nhiệm vụ, giữ nguyên văn phong các chuỗi mẫu của bản gốc.
        /// </summary>
        /// <param name="type">Loại nhiệm vụ.</param>
        /// <param name="targetValue">Giá trị mục tiêu đã bốc.</param>
        public string GenerateDescription(ChallengeType type, float targetValue)
        {
            int value = Mathf.Max(1, Mathf.RoundToInt(targetValue));
            string plural = value > 1 ? "s" : string.Empty;

            switch (type)
            {
                case ChallengeType.PlayMatches:
                    // Mục tiêu 1 trận dùng đúng câu chào của bản gốc.
                    return value <= 1 ? "Play a game first" : string.Format("Play {0} matches", value);

                case ChallengeType.WinMatches:
                    return string.Format("Win {0} matches", value);

                case ChallengeType.ScorePoints:
                    return string.Format("Score {0} points", value);

                case ChallengeType.PlayMinutes:
                    return string.Format("Play for {0} Minutes", value);

                case ChallengeType.PlayMinutesInGame:
                    return string.Format("Play for {0} Minute{1} in a game", value, plural);

                case ChallengeType.RallySeconds:
                    return string.Format("Play for {0} seconds in a Rally", value);

                case ChallengeType.HitVolleys:
                    return string.Format("Hit {0} volleys", value);

                case ChallengeType.WinWithoutLosingPoint:
                    return string.Format("Win {0} matches without losing a point", value);

                case ChallengeType.UpgradeItem:
                    return string.Format("Upgrade {0} items", value);

                case ChallengeType.OpenKitbag:
                    return string.Format("Open {0} kitbags", value);

                case ChallengeType.UseBooster:
                    return string.Format("Use {0} boosters", value);

                default:
                    return string.Format("Complete {0}", value);
            }
        }

        /// <summary>
        /// Quay một phần thưởng từ bảng thưởng nhiệm vụ hằng ngày.
        /// Trả về phần thưởng coin mặc định nếu chưa có <see cref="RewardManager"/> trong scene.
        /// </summary>
        public CollectibleReward GenerateRandomReward()
        {
            if (RewardManager.HasInstance)
            {
                List<CollectibleReward> rolled = RewardManager.Instance.GenerateDailyChallengeRewards(1);
                if (rolled != null && rolled.Count > 0 && rolled[0] != null) return rolled[0];
            }

            return new CollectibleReward(RewardType.Coins, 100);
        }

        // ------------------------------------------------------------------
        // Tiến độ
        // ------------------------------------------------------------------

        /// <summary>
        /// Cộng dồn tiến độ cho mọi nhiệm vụ đang mở thuộc loại <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Loại nhiệm vụ nhận tiến độ.</param>
        /// <param name="value">Lượng tiến độ cộng thêm; bỏ qua nếu &lt;= 0.</param>
        public void UpdateChallengeProgress(ChallengeType type, float value)
        {
            if (value <= 0f || dailyChallenges == null) return;

            bool changed = false;
            for (int i = 0; i < dailyChallenges.Count; i++)
            {
                DailyChallenge challenge = dailyChallenges[i];
                if (challenge == null || challenge.Type != type || challenge.IsCompleted) continue;

                ApplyProgress(challenge, challenge.Progress + value);
                changed = true;
            }

            if (changed) OnChallengesUpdated?.Invoke();
        }

        /// <summary>
        /// Gán tuyệt đối tiến độ cho mọi nhiệm vụ đang mở thuộc loại <paramref name="type"/>.
        /// Dùng cho các nhiệm vụ đo "kỷ lục" (pha bóng dài nhất, trận dài nhất) chứ không cộng dồn.
        /// </summary>
        /// <param name="type">Loại nhiệm vụ nhận tiến độ.</param>
        /// <param name="value">Giá trị tiến độ mới.</param>
        public void SetChallengeProgress(ChallengeType type, float value)
        {
            if (dailyChallenges == null) return;

            bool changed = false;
            for (int i = 0; i < dailyChallenges.Count; i++)
            {
                DailyChallenge challenge = dailyChallenges[i];
                if (challenge == null || challenge.Type != type || challenge.IsCompleted) continue;

                ApplyProgress(challenge, value);
                changed = true;
            }

            if (changed) OnChallengesUpdated?.Invoke();
        }

        /// <summary>
        /// Nhận phần thưởng của một nhiệm vụ đã hoàn thành. Gọi lại lần hai không cộng thêm.
        /// </summary>
        /// <param name="challenge">Nhiệm vụ muốn nhận thưởng.</param>
        public void CollectReward(DailyChallenge challenge)
        {
            if (challenge == null || !challenge.IsCompleted || challenge.RewardCollected) return;

            challenge.Reward?.GrantReward(gameData);
            challenge.RewardCollected = true;

            OnChallengesUpdated?.Invoke();
        }

        /// <summary>True nếu còn ít nhất một nhiệm vụ đã hoàn thành mà chưa nhận thưởng.</summary>
        public bool IsAnyDailyChallengeClaimPending()
        {
            if (dailyChallenges == null) return false;

            for (int i = 0; i < dailyChallenges.Count; i++)
            {
                DailyChallenge challenge = dailyChallenges[i];
                if (challenge != null && challenge.IsCompleted && !challenge.RewardCollected) return true;
            }
            return false;
        }

        /// <summary>Danh sách nhiệm vụ hiện tại (không sao chép).</summary>
        public List<DailyChallenge> GetDailyChallenges()
        {
            return dailyChallenges;
        }

        // ------------------------------------------------------------------
        // Reset theo ngày
        // ------------------------------------------------------------------

        /// <summary>
        /// Kiểm tra mốc 00:00 UTC: nếu đã sang ngày mới (hoặc chưa từng phát nhiệm vụ)
        /// thì sinh lại bộ nhiệm vụ và ghi lại mốc reset.
        /// </summary>
        public void CheckAndResetDailyChallenges()
        {
            // TODO: đổi sang server time khi có backend.
            DateTime now = DateTime.UtcNow;

            bool neverAssigned = lastResetTime == default || dailyChallenges == null || dailyChallenges.Count == 0;
            if (!neverAssigned && now.Date <= lastResetTime.Date) return;

            lastResetTime = now;
            AssignDailyChallenges();
        }

        /// <summary>
        /// Nạp bộ nhiệm vụ từ dữ liệu lưu trữ. Danh sách rỗng/null sẽ khiến hệ thống
        /// sinh lại nhiệm vụ mới ở lần kiểm tra kế tiếp.
        /// </summary>
        /// <param name="dailyChallenges">Bộ nhiệm vụ đã lưu.</param>
        public void LoadDailyChallenges(List<DailyChallenge> dailyChallenges)
        {
            this.dailyChallenges = dailyChallenges ?? new List<DailyChallenge>();

            // Đồng bộ lại cờ hoàn thành phòng khi dữ liệu lưu bị lệch.
            for (int i = 0; i < this.dailyChallenges.Count; i++)
            {
                DailyChallenge challenge = this.dailyChallenges[i];
                if (challenge == null) continue;
                if (challenge.TargetValue > 0f && challenge.Progress >= challenge.TargetValue) challenge.IsCompleted = true;
            }

            OnChallengesUpdated?.Invoke();
        }

        /// <summary>Thời gian còn lại tới mốc reset 00:00 UTC kế tiếp.</summary>
        public TimeSpan GetTimeUntilReset()
        {
            // TODO: đổi sang server time khi có backend.
            DateTime now = DateTime.UtcNow;
            DateTime next = now.Date.AddDays(1);
            TimeSpan remaining = next - now;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>Mốc reset gần nhất dạng chuỗi ISO round-trip, để hệ save ghi xuống đĩa.</summary>
        public string GetLastResetTimeString()
        {
            return lastResetTime == default ? string.Empty : lastResetTime.ToString(IsoFormat);
        }

        /// <summary>Nạp mốc reset gần nhất từ chuỗi ISO round-trip do hệ save cung cấp.</summary>
        /// <param name="value">Chuỗi ISO; rỗng hoặc sai định dạng sẽ coi như chưa từng reset.</param>
        public void SetLastResetTimeString(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                lastResetTime = default;
                return;
            }

            lastResetTime = parsed.ToUniversalTime();
        }

        // ------------------------------------------------------------------
        // Đăng ký sự kiện gameplay
        // ------------------------------------------------------------------

        /// <summary>Đăng ký toàn bộ nguồn sự kiện đẩy tiến độ. An toàn khi gọi nhiều lần.</summary>
        public void SubscribeToEvents()
        {
            if (eventsSubscribed) return;
            eventsSubscribed = true;

            GameManager.OnMatchEnded += HandleMatchEnded;
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            BallController.OnBallHit += HandleBallHit;

            TrySubscribeInstanceEvents();
        }

        /// <summary>Huỷ đăng ký toàn bộ nguồn sự kiện. An toàn khi gọi nhiều lần.</summary>
        public void UnsubscribeFromEvents()
        {
            if (!eventsSubscribed) return;
            eventsSubscribed = false;

            GameManager.OnMatchEnded -= HandleMatchEnded;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            BallController.OnBallHit -= HandleBallHit;

            if (subscribedScore != null)
            {
                subscribedScore.OnScoreUpdated -= HandleScoreUpdated;
                subscribedScore = null;
            }

            if (subscribedBall != null)
            {
                subscribedBall.OnBallCollision -= HandleBallCollision;
                subscribedBall = null;
            }
        }

        /// <summary>
        /// Subscribe các event dạng instance khi singleton tương ứng đã sẵn sàng.
        /// Gọi lặp mỗi frame là an toàn nhờ kiểm tra tham chiếu.
        /// </summary>
        private void TrySubscribeInstanceEvents()
        {
            if (!eventsSubscribed) return;

            if (subscribedScore == null && ScoreManager.HasInstance)
            {
                subscribedScore = ScoreManager.Instance;
                subscribedScore.OnScoreUpdated += HandleScoreUpdated;
            }

            if (subscribedBall == null && BallController.HasInstance)
            {
                subscribedBall = BallController.Instance;
                subscribedBall.OnBallCollision += HandleBallCollision;
            }
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        /// <summary>
        /// Trận đấu kết thúc: ghi nhận số trận đã chơi, số trận thắng và trận thắng trắng.
        /// </summary>
        /// <param name="winnerTeamID">TeamID của đội thắng.</param>
        private void HandleMatchEnded(string winnerTeamID)
        {
            string localTeam = GetLocalTeamID();
            bool playerWon = !string.IsNullOrEmpty(winnerTeamID) && winnerTeamID == localTeam;

            UpdateChallengeProgress(ChallengeType.PlayMatches, 1f);

            if (playerWon)
            {
                UpdateChallengeProgress(ChallengeType.WinMatches, 1f);

                if (ScoreManager.HasInstance)
                {
                    string opponent = ScoreManager.Instance.GetOtherTeamID(localTeam);
                    if (ScoreManager.Instance.GetScore(opponent) == 0)
                    {
                        UpdateChallengeProgress(ChallengeType.WinWithoutLosingPoint, 1f);
                    }
                }
            }

            currentGameSeconds = 0f;
            currentRallySeconds = 0f;
            isRallyActive = false;
        }

        /// <summary>
        /// Máy trạng thái trận đấu: mở/đóng đồng hồ pha bóng và chốt các nhiệm vụ kỷ lục.
        /// </summary>
        /// <param name="newState">Trạng thái mới.</param>
        private void HandleGameStateChanged(GameState newState)
        {
            GameState previous = currentGameState;
            currentGameState = newState;

            if (newState == GameState.InPlay && previous != GameState.InPlay)
            {
                isRallyActive = true;
                currentRallySeconds = 0f;
                bouncesSinceLastHit = 0;
            }
            else if (previous == GameState.InPlay && newState != GameState.InPlay)
            {
                isRallyActive = false;
                SetChallengeProgressIfBetter(ChallengeType.RallySeconds, currentRallySeconds);
                currentRallySeconds = 0f;
            }

            if (newState == GameState.GameOver)
            {
                SetChallengeProgressIfBetter(ChallengeType.PlayMinutesInGame, currentGameSeconds / 60f);
            }
        }

        /// <summary>
        /// Một cú đánh vừa xảy ra: tính volley khi bóng chưa nảy lần nào kể từ cú đánh trước
        /// và pha bóng đang sống.
        /// </summary>
        /// <param name="teamID">TeamID của bên vừa đánh.</param>
        /// <param name="destination">Điểm rơi dự kiến (không dùng).</param>
        /// <param name="shotTrajetoryInfo">Thông tin quỹ đạo (không dùng).</param>
        private void HandleBallHit(string teamID, Vector3 destination, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            if (!string.IsNullOrEmpty(teamID) && teamID == GetLocalTeamID()
                && bouncesSinceLastHit == 0 && currentGameState == GameState.InPlay)
            {
                UpdateChallengeProgress(ChallengeType.HitVolleys, 1f);
            }

            bouncesSinceLastHit = 0;
        }

        /// <summary>Bóng va chạm: chỉ đếm lần chạm đất để phân biệt volley.</summary>
        /// <param name="collisionType">Loại va chạm.</param>
        private void HandleBallCollision(BallController.CollisionType collisionType)
        {
            if (collisionType == BallController.CollisionType.Ground) bouncesSinceLastHit++;
        }

        /// <summary>Tỉ số vừa đổi: cộng một điểm cho nhiệm vụ ghi điểm nếu là điểm của người chơi.</summary>
        /// <param name="score">Điểm mới của đội vừa ghi.</param>
        /// <param name="teamID">TeamID của đội vừa ghi điểm.</param>
        private void HandleScoreUpdated(int score, string teamID)
        {
            // ScoreManager.ResetScore() cũng phát sự kiện này với score = 0 — bỏ qua.
            if (score <= 0 || string.IsNullOrEmpty(teamID)) return;
            if (teamID != GetLocalTeamID()) return;

            UpdateChallengeProgress(ChallengeType.ScorePoints, 1f);
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Cộng dồn thời gian thi đấu thực tế vào bộ đệm và đồng hồ pha bóng.</summary>
        /// <param name="deltaTime">Thời gian frame vừa trôi qua.</param>
        private void AccumulatePlayTime(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (currentGameState != GameState.Serving && currentGameState != GameState.InPlay) return;

            currentGameSeconds += deltaTime;
            pendingPlaySeconds += deltaTime;

            if (isRallyActive) currentRallySeconds += deltaTime;
        }

        /// <summary>
        /// Đẩy thời gian thi đấu đã tích luỹ vào nhiệm vụ <c>PlayMinutes</c>.
        /// Chỉ gọi mỗi <see cref="timerUpdateInterval"/> giây để UI không phải vẽ lại từng frame.
        /// </summary>
        private void FlushPlayTime()
        {
            if (pendingPlaySeconds <= 0f) return;

            float minutes = pendingPlaySeconds / 60f;
            pendingPlaySeconds = 0f;
            UpdateChallengeProgress(ChallengeType.PlayMinutes, minutes);
        }

        /// <summary>Gán tiến độ tuyệt đối nhưng chỉ khi giá trị mới tốt hơn tiến độ hiện có.</summary>
        /// <param name="type">Loại nhiệm vụ.</param>
        /// <param name="value">Giá trị kỷ lục mới.</param>
        private void SetChallengeProgressIfBetter(ChallengeType type, float value)
        {
            if (value <= 0f || dailyChallenges == null) return;

            for (int i = 0; i < dailyChallenges.Count; i++)
            {
                DailyChallenge challenge = dailyChallenges[i];
                if (challenge == null || challenge.Type != type || challenge.IsCompleted) continue;
                if (value <= challenge.Progress) continue;

                SetChallengeProgress(type, value);
                return;
            }
        }

        /// <summary>Áp tiến độ mới cho một nhiệm vụ, kẹp trong [0, TargetValue] và cập nhật cờ hoàn thành.</summary>
        private static void ApplyProgress(DailyChallenge challenge, float newProgress)
        {
            float target = challenge.TargetValue;
            challenge.Progress = target > 0f ? Mathf.Clamp(newProgress, 0f, target) : Mathf.Max(0f, newProgress);

            if (target > 0f && challenge.Progress >= target) challenge.IsCompleted = true;
        }

        /// <summary>TeamID của người chơi cục bộ (Phase 1 luôn là player 1).</summary>
        private static string GetLocalTeamID()
        {
            return GameManager.HasInstance ? GameManager.Instance.player1TeamID : "P1";
        }

        /// <summary>
        /// Cập nhật <see cref="CurrentExperienceLevel"/> từ level người chơi.
        /// Ngưỡng bám theo bậc sân đấu: 0-2 Newbie, 3-5 Amateur, 6-8 Competitor, 9-11 Pro, 12+ Master.
        /// </summary>
        private void RefreshExperienceLevel()
        {
            if (gameData == null || gameData.playerProfileData == null) return;

            int level = gameData.playerProfileData.level;

            if (level >= 12) CurrentExperienceLevel = AIDifficulty.Master;
            else if (level >= 9) CurrentExperienceLevel = AIDifficulty.Pro;
            else if (level >= 6) CurrentExperienceLevel = AIDifficulty.Competitor;
            else if (level >= 3) CurrentExperienceLevel = AIDifficulty.Amateur;
            else CurrentExperienceLevel = AIDifficulty.Newbie;
        }

        /// <summary>Khoảng mục tiêu của một loại nhiệm vụ ở bậc trình độ hiện tại; null nếu chưa cấu hình.</summary>
        private ChallengeRange GetRange(ChallengeType type)
        {
            ExperienceLevel level = GetCurrentExperienceLevelData();
            return level?.GetRange(type);
        }

        /// <summary>
        /// Bảng khoảng mục tiêu của bậc trình độ hiện tại.
        /// Nếu bậc đó chưa được cấu hình thì lấy tạm bậc đầu tiên có trong bảng.
        /// </summary>
        private ExperienceLevel GetCurrentExperienceLevelData()
        {
            if (gameData == null || gameData.dailyChallengeLevels == null) return null;

            ExperienceLevel fallback = null;
            for (int i = 0; i < gameData.dailyChallengeLevels.Count; i++)
            {
                ExperienceLevel level = gameData.dailyChallengeLevels[i];
                if (level == null) continue;
                if (level.LevelName == CurrentExperienceLevel) return level;
                if (fallback == null) fallback = level;
            }
            return fallback;
        }

        /// <summary>Danh sách loại nhiệm vụ có cấu hình khoảng mục tiêu ở bậc trình độ hiện tại.</summary>
        private List<ChallengeType> GetAvailableChallengeTypes()
        {
            List<ChallengeType> types = new List<ChallengeType>();

            ExperienceLevel level = GetCurrentExperienceLevelData();
            if (level == null || level.ranges == null)
            {
                // Chưa có bảng cấu hình: dùng bộ nhiệm vụ tối thiểu để vòng lặp game vẫn chạy được.
                types.Add(ChallengeType.PlayMatches);
                types.Add(ChallengeType.WinMatches);
                types.Add(ChallengeType.ScorePoints);
                return types;
            }

            for (int i = 0; i < level.ranges.Count; i++)
            {
                ChallengeRange range = level.ranges[i];
                if (range == null || range.Type == ChallengeType.None) continue;
                if (!types.Contains(range.Type)) types.Add(range.Type);
            }
            return types;
        }
    }
}
