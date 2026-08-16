using Pickleball.Network;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bộ thu thập thống kê trận đấu: lắng nghe sự kiện của <see cref="GameManager"/>,
    /// <see cref="ScoreManager"/> và <see cref="BallController"/> rồi quy đổi thành các chỉ số
    /// trong <see cref="GameStatsData"/>.
    ///
    /// <para><b>Quy đổi sự kiện → chỉ số</b>:</para>
    /// <list type="bullet">
    /// <item><description><c>OnBallHit</c> → <c>totalShots</c> + <c>shotsInRally</c>; nếu bóng chưa nảy
    /// lần nào kể từ cú đánh trước thì tính thêm một <c>volley</c>; đồng thời cập nhật <c>fastestShot</c>.</description></item>
    /// <item><description><c>OnBallCollision(Ground)</c> → tăng bộ đếm nảy của pha bóng.</description></item>
    /// <item><description>Vào <see cref="GameState.InPlay"/> → bắt đầu đo thời lượng pha bóng;
    /// rời InPlay → chốt <c>longestRallyTime</c>, <c>shortestRallyTime</c> (cho bên thắng pha bóng),
    /// <c>maxShotsInRally</c> rồi xoá bộ đếm.</description></item>
    /// <item><description><c>OnRuleViolated</c> → cộng chỉ số lỗi tương ứng cho BÊN PHẠM LỖI.</description></item>
    /// <item><description><c>OnScoreUpdated</c> → ghi nhận bên ghi điểm; nếu điểm đó tới mà không có
    /// lỗi luật nào thì tính một <c>outrightWinner</c>.</description></item>
    /// </list>
    ///
    /// <para><b>Về thứ tự sự kiện</b>: trong <c>GameManager.HandlePointAndServeChange</c>, thứ tự phát là
    /// <c>OnScoreUpdated</c> → <c>OnRuleViolated</c> → <c>OnGameStateChanged(PointScored)</c>.
    /// Nghĩa là tại thời điểm <c>OnScoreUpdated</c> chạy, cờ <see cref="lastPointHadViolation"/> CHƯA
    /// kịp được set. Vì vậy điểm ghi được chỉ ghi nhận là "đang chờ" và việc quyết định có phải
    /// outright winner hay không được chốt lại khi điểm đóng (state → PointScored/GameOver),
    /// lúc đó cả hai tín hiệu đều đã tới.</para>
    /// </summary>
    public class StatsManager : NetworkSingleton<StatsManager>
    {
        /// <summary>Bảng thống kê của trận đang diễn ra.</summary>
        public GameStatsData statsData = new GameStatsData();

        /// <summary>Thời gian đã trôi của pha bóng đang diễn ra (giây).</summary>
        private float currentRallyTime;

        /// <summary>Thời gian thi đấu tích luỹ (giây) — chỉ chạy khi state là Serving hoặc InPlay.</summary>
        private float currentGameTime;

        /// <summary>Số cú đánh đã ghi nhận trong pha bóng đang diễn ra (cả hai bên cộng lại).</summary>
        private int rallyShotCount;

        /// <summary>Số lần bóng chạm đất KỂ TỪ cú đánh gần nhất — dùng để phân biệt volley.</summary>
        private int rallyBounceCount;

        /// <summary>Trạng thái trận đấu gần nhất nhận được từ <see cref="GameManager"/>.</summary>
        private GameState currentGameState = GameState.PreServe;

        /// <summary>True khi đang trong <see cref="GameState.InPlay"/> và đồng hồ pha bóng đang chạy.</summary>
        private bool isRallyActive;

        /// <summary>True nếu điểm đang chờ chốt được ghi do đối thủ phạm lỗi.</summary>
        private bool lastPointHadViolation;

        /// <summary>TeamID vừa được cộng điểm và đang chờ chốt outright winner.</summary>
        private string pendingScoringTeamID;

        /// <summary>True khi có một điểm vừa được cộng nhưng chưa chốt xong.</summary>
        private bool hasPendingPoint;

        /// <summary>True khi đã subscribe (chống subscribe/unsubscribe lặp).</summary>
        private bool eventsSubscribed;

        /// <summary>Instance BallController đang được subscribe (event OnBallCollision là event instance).</summary>
        private BallController subscribedBall;

        /// <summary>Instance ScoreManager đang được subscribe (event OnScoreUpdated là event instance).</summary>
        private ScoreManager subscribedScore;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        /// <summary>
        /// Khởi tạo bảng thống kê cho hai đội của trận rồi đăng ký toàn bộ sự kiện gameplay.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (statsData == null) statsData = new GameStatsData();

            string team1 = GameManager.HasInstance ? GameManager.Instance.player1TeamID : "P1";
            string team2 = GameManager.HasInstance ? GameManager.Instance.player2TeamID : "P2";
            statsData.AddPlayer(new[] { team1, team2 });

            SubscribeToEvents();
        }

        /// <summary>Ngắt toàn bộ đăng ký sự kiện khi server dừng.</summary>
        public override void OnStopServer()
        {
            UnsubscribeFromEvents();
            base.OnStopServer();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            base.OnDestroy();
        }

        /// <summary>
        /// Đếm giờ trận đấu và giờ pha bóng. Đồng thời thử subscribe muộn các event dạng
        /// <i>instance</i> (<see cref="BallController"/>, <see cref="ScoreManager"/>) vì thứ tự
        /// <c>Start()</c> giữa các singleton không được đảm bảo.
        /// </summary>
        private void Update()
        {
            if (!IsServer) return;

            TrySubscribeInstanceEvents();

            float dt = Time.deltaTime;

            if (currentGameState == GameState.Serving || currentGameState == GameState.InPlay)
            {
                currentGameTime += dt;
            }

            if (isRallyActive) currentRallyTime += dt;
        }

        // ------------------------------------------------------------------
        // API công khai
        // ------------------------------------------------------------------

        /// <summary>Xoá toàn bộ chỉ số và bộ đếm tạm, giữ nguyên danh sách người chơi.</summary>
        public void ResetStats()
        {
            statsData?.Reset();

            currentRallyTime = 0f;
            currentGameTime = 0f;
            rallyShotCount = 0;
            rallyBounceCount = 0;
            isRallyActive = false;
            lastPointHadViolation = false;
            hasPendingPoint = false;
            pendingScoringTeamID = null;
        }

        /// <summary>
        /// Đăng ký toàn bộ sự kiện cần theo dõi. An toàn khi gọi nhiều lần.
        /// </summary>
        public void SubscribeToEvents()
        {
            if (eventsSubscribed) return;
            eventsSubscribed = true;

            GameManager.OnGameStateChanged += OnGameStateChanged;
            GameManager.OnRuleViolated += OnRuleViolated;
            BallController.OnBallHit += OnBallHit;

            TrySubscribeInstanceEvents();
        }

        /// <summary>
        /// Huỷ đăng ký toàn bộ sự kiện. An toàn khi gọi nhiều lần
        /// (được gọi cả từ <see cref="OnStopServer"/> lẫn <c>OnDestroy</c>).
        /// </summary>
        public void UnsubscribeFromEvents()
        {
            if (!eventsSubscribed) return;
            eventsSubscribed = false;

            GameManager.OnGameStateChanged -= OnGameStateChanged;
            GameManager.OnRuleViolated -= OnRuleViolated;
            BallController.OnBallHit -= OnBallHit;

            if (subscribedBall != null)
            {
                subscribedBall.OnBallCollision -= OnBallCollision;
                subscribedBall = null;
            }

            if (subscribedScore != null)
            {
                subscribedScore.OnScoreUpdated -= OnScoreUpdated;
                subscribedScore = null;
            }
        }

        /// <summary>Bản in nhiều dòng của bảng thống kê hiện tại.</summary>
        public override string ToString()
        {
            return statsData != null ? statsData.ToString() : "[StatsManager] statsData = null";
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        /// <summary>
        /// Theo dõi máy trạng thái trận đấu: mở/đóng pha bóng, chốt điểm và chốt thời lượng trận.
        /// </summary>
        /// <param name="newState">Trạng thái mới.</param>
        private void OnGameStateChanged(GameState newState)
        {
            GameState previous = currentGameState;
            currentGameState = newState;

            if (newState == GameState.InPlay && previous != GameState.InPlay)
            {
                BeginRally();
            }
            else if (previous == GameState.InPlay && newState != GameState.InPlay)
            {
                EndRally();
            }

            // Điểm đóng lại: lúc này OnScoreUpdated và OnRuleViolated (nếu có) đều đã tới.
            if (newState == GameState.PointScored || newState == GameState.GameOver)
            {
                ResolvePendingPoint();
            }

            // Pha bóng mới bắt đầu -> xoá cờ lỗi của điểm trước.
            if (newState == GameState.PreServe)
            {
                lastPointHadViolation = false;
                rallyBounceCount = 0;
                rallyShotCount = 0;
                currentRallyTime = 0f;
            }

            if (newState == GameState.GameOver)
            {
                statsData?.SetGameDuration(currentGameTime);
            }
        }

        /// <summary>
        /// Một cú đánh vừa được thực hiện: cộng tổng cú đánh, cú đánh trong pha bóng,
        /// xét volley và cập nhật kỷ lục tốc độ.
        /// </summary>
        /// <param name="teamID">TeamID của bên vừa đánh.</param>
        /// <param name="destination">Điểm rơi dự kiến (không dùng cho thống kê, giữ đúng chữ ký event).</param>
        /// <param name="shotTrajetoryInfo">Thông tin quỹ đạo (không dùng cho thống kê).</param>
        private void OnBallHit(string teamID, Vector3 destination, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            if (statsData == null || string.IsNullOrEmpty(teamID)) return;

            statsData.IncrementTotalShots(teamID, 1);
            statsData.IncrementShotsInRally(teamID, 1);

            // Volley = đánh bóng khi nó chưa nảy lần nào kể từ cú đánh trước.
            // Chỉ xét trong pha bóng sống — cú giao bóng luôn có rallyBounceCount == 0 nhưng không phải volley.
            if (rallyBounceCount == 0 && currentGameState == GameState.InPlay)
            {
                statsData.IncrementVolleys(teamID, 1);
            }

            if (BallController.HasInstance)
            {
                statsData.SetFastestShot(teamID, BallController.Instance.GetVelocity());
            }

            rallyShotCount++;
            rallyBounceCount = 0;
        }

        /// <summary>Bóng vừa va chạm: chỉ quan tâm chạm đất để đếm số lần nảy.</summary>
        /// <param name="type">Loại va chạm.</param>
        private void OnBallCollision(BallController.CollisionType type)
        {
            if (type == BallController.CollisionType.Ground) rallyBounceCount++;
        }

        /// <summary>Tỉ số vừa đổi: ghi nhận bên ghi điểm, chờ chốt outright winner.</summary>
        /// <param name="score">Điểm mới của đội vừa ghi.</param>
        /// <param name="teamID">TeamID của đội vừa ghi điểm.</param>
        private void OnScoreUpdated(int score, string teamID)
        {
            // ScoreManager.ResetScore() cũng phát sự kiện này với score = 0 — bỏ qua.
            if (score <= 0 || string.IsNullOrEmpty(teamID)) return;

            pendingScoringTeamID = teamID;
            hasPendingPoint = true;
        }

        /// <summary>Có lỗi luật: cộng chỉ số lỗi tương ứng cho bên phạm lỗi.</summary>
        /// <param name="type">Loại lỗi.</param>
        /// <param name="teamID">TeamID của bên phạm lỗi.</param>
        /// <param name="message">Thông báo hiển thị (không dùng cho thống kê).</param>
        private void OnRuleViolated(RuleType type, string teamID, string message)
        {
            lastPointHadViolation = true;

            if (statsData == null || string.IsNullOrEmpty(teamID)) return;

            switch (type)
            {
                case RuleType.ServeInNet:
                case RuleType.ServeNotInArea:
                case RuleType.ServeVolley:
                case RuleType.ServeTimeout:
                    statsData.IncrementMisserves(teamID, 1);
                    break;

                case RuleType.VolleyInsideKitchen:
                    statsData.IncrementKitchen(teamID, 1);
                    break;

                case RuleType.BounceOutOfCourt:
                    statsData.IncrementOutOfBounds(teamID, 1);
                    break;

                case RuleType.DoubleBounceOnSide:
                    // Bên đó để bóng nảy hai lần bên sân mình = không đỡ được.
                    statsData.IncrementDrops(teamID, 1);
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Mở một pha bóng: reset đồng hồ và bộ đếm nảy.</summary>
        private void BeginRally()
        {
            isRallyActive = true;
            currentRallyTime = 0f;
            rallyShotCount = 0;
            rallyBounceCount = 0;
            lastPointHadViolation = false;
        }

        /// <summary>
        /// Đóng một pha bóng: chốt thời lượng dài nhất (cho cả hai bên), thời lượng thắng nhanh nhất
        /// (cho bên thắng pha bóng), kỷ lục số cú đánh rồi xoá bộ đếm.
        /// </summary>
        private void EndRally()
        {
            isRallyActive = false;

            if (statsData != null && currentRallyTime > 0f)
            {
                statsData.SetLongestRallyTime(currentRallyTime);

                // Chỉ ghi khi điểm của pha bóng này thực sự vừa được cộng (tránh dùng lại ID cũ).
                if (hasPendingPoint && !string.IsNullOrEmpty(pendingScoringTeamID))
                {
                    statsData.SetShortestRallyTime(pendingScoringTeamID, currentRallyTime);
                }
            }

            statsData?.SetMaxShotsInRally();
            statsData?.ResetShotsInRally();

            currentRallyTime = 0f;
            rallyShotCount = 0;
            rallyBounceCount = 0;
        }

        /// <summary>
        /// Chốt điểm đang chờ: nếu điểm tới mà KHÔNG có lỗi luật nào thì đó là cú đánh ăn điểm
        /// trực tiếp của bên ghi điểm.
        /// </summary>
        private void ResolvePendingPoint()
        {
            if (!hasPendingPoint) return;
            hasPendingPoint = false;

            if (!lastPointHadViolation && statsData != null && !string.IsNullOrEmpty(pendingScoringTeamID))
            {
                statsData.IncrementOutrightWinners(pendingScoringTeamID, 1);
            }
        }

        /// <summary>
        /// Subscribe các event dạng instance khi singleton tương ứng đã sẵn sàng.
        /// Gọi lặp lại mỗi frame là an toàn vì có kiểm tra tham chiếu.
        /// </summary>
        private void TrySubscribeInstanceEvents()
        {
            if (!eventsSubscribed) return;

            if (subscribedBall == null && BallController.HasInstance)
            {
                subscribedBall = BallController.Instance;
                subscribedBall.OnBallCollision += OnBallCollision;
            }

            if (subscribedScore == null && ScoreManager.HasInstance)
            {
                subscribedScore = ScoreManager.Instance;
                subscribedScore.OnScoreUpdated += OnScoreUpdated;
            }
        }
    }
}
