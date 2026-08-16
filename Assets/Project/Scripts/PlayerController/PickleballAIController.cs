using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tay vợt do MÁY điều khiển.
    /// <para>
    /// Dùng lại toàn bộ phần "thân thể" của <see cref="BasePlayerController"/> (di chuyển, thể lực,
    /// thuật toán đón bóng) và chỉ thay phần "bộ não": FSM riêng
    /// (<see cref="AIIdleState"/>, <see cref="AIServeState"/>, <see cref="AITrackBallState"/>,
    /// <see cref="AITakeShotState"/>) cộng một <see cref="IAIStrategy"/> cắm theo bậc độ khó.
    /// </para>
    /// <para><b>Luồng một cú đánh của AI:</b>
    /// <see cref="BallController.OnBallHit"/> → <see cref="AITrackBallState"/> chạy tới điểm đón bóng →
    /// <see cref="AITakeShotState"/> xoay vợt → <see cref="DetermineDestination"/> +
    /// <see cref="SelectShotType"/> + <see cref="CalculateShotTime"/> → áp sai số →
    /// <see cref="BasePlayerController.HitBall(string, Vector3, float, float, float, float, Vector3, float)"/>.
    /// </para>
    /// <para><b>AI yếu thua bằng cách nào:</b> chỉ số <c>maxShotError</c> làm lệch điểm rơi và
    /// <c>ProbablityToTakeOutShots</c> khiến AI CỐ TÌNH đánh ra ngoài sân (xem <see cref="AITakeShotState"/>).</para>
    /// </summary>
    public class PickleballAIController : BasePlayerController
    {
        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        /// <summary>Bậc độ khó hiện tại; quyết định <see cref="IAIStrategy"/> và bộ chỉ số được nạp.</summary>
        [Header("AI Properties")]
        public AIDifficulty difficulty;

        /// <summary>
        /// Bảng tra profile AI theo bậc độ khó, dùng cho <see cref="SetAILevel"/>.
        /// <para>Bản gốc lấy bảng này từ dữ liệu cấu hình toàn cục; ở phase hiện tại nó được gắn thẳng
        /// vào prefab AI để không phụ thuộc hệ thống chưa tồn tại.</para>
        /// </summary>
        public PlayerProfileConfigs playerProfileConfigs;

        /// <summary>Xác suất AI không nhích lần nào trước khi giao.</summary>
        [Header("AI Serve Movement Settings")]
        [Range(0f, 1f)]
        [Tooltip("Xác suất AI không nhích lần nào trước khi giao")]
        public float serveMovement0Probability = 0.35f;

        /// <summary>Xác suất AI nhích 1 lần trước khi giao.</summary>
        [Range(0f, 1f)]
        [Tooltip("Xác suất nhích 1 lần")]
        public float serveMovement1Probability = 0.45f;

        /// <summary>Xác suất AI nhích 2 lần trước khi giao (tự tính từ phần còn lại).</summary>
        [Range(0f, 1f)]
        [Tooltip("Xác suất nhích 2 lần (tự tính từ phần còn lại)")]
        public float serveMovement2Probability = 0.2f;

        /// <summary>Biên độ nhích ngang trong ô giao, tính theo % chiều rộng ô giao (min, max).</summary>
        [Tooltip("Biên độ nhích trong ô giao, theo % chiều rộng ô")]
        public Vector2 serveMovementRange = new Vector2(0.08f, 0.3f);

        /// <summary>Trễ tối thiểu trước khi AI bắt đầu animation giao bóng.</summary>
        [Header("AI Serve Timing Settings")]
        [Tooltip("Trễ tối thiểu trước khi AI bắt đầu animation giao bóng (gốc: 1.5)")]
        public float minServeDelay = 1.5f;

        /// <summary>Trễ tối đa trước khi AI bắt đầu animation giao bóng.</summary>
        [Tooltip("Trễ tối đa (gốc: 3)")]
        public float maxServeDelay = 3f;

        // ------------------------------------------------------------------
        // Cache hình học sân (nội bộ, các AI state đọc trực tiếp)
        // ------------------------------------------------------------------

        /// <summary>Bề ngang TOÀN SÂN theo trục X (mét).</summary>
        internal float courtWidth;

        /// <summary>Chiều sâu của MỘT NỬA sân theo trục Z (mét) — bằng <see cref="Court.HalfLength"/>.</summary>
        internal float courtDepth;

        /// <summary>Chiều sâu kitchen tính từ lưới (mét).</summary>
        internal float kitchenDepth;

        /// <summary>Khoảng cách tối thiểu từ lưới tới điểm ngắm của AI (mét).</summary>
        internal float gapFromNet;

        /// <summary>Chiều cao lưới (mét) — mốc so sánh độ cao bóng khi chọn loại cú đánh.</summary>
        internal float netHeight;

        /// <summary>Loại cú đánh vừa được chọn cho cú đánh hiện tại.</summary>
        internal ShotType currentShotType;

        /// <summary>Thời gian bay mong muốn của cú đánh hiện tại (giây).</summary>
        internal float currentShotTime;

        /// <summary>Cường độ xoáy/vung "nền" suy ra từ chỉ số, dùng làm mốc cho mỗi cú đánh.</summary>
        internal float baseShotEffect;

        /// <summary>Ô giao bóng mà trận đấu vừa chỉ định cho AI (ghi trong <see cref="PrepareServe"/>).</summary>
        internal ServeSide currentServeSide;

        private IAIStrategy aiStrategy;
        private Coroutine recenterRoutine;

        // ------------------------------------------------------------------
        // Hằng số
        // ------------------------------------------------------------------

        /// <summary>Kích thước sân mặc định khi scene chưa có <see cref="Court"/>.</summary>
        private const float DefaultCourtWidth = 6.096f;
        private const float DefaultCourtHalfLength = 6.7056f;
        private const float DefaultKitchenDepth = 2.1336f;

        /// <summary>
        /// Chiều cao lưới mặc định (mét). Hiện KHÔNG có nguồn dữ liệu nào trong project cung cấp
        /// giá trị này (<see cref="Court"/> và <see cref="GameSettings"/> đều không có), nên AI dùng
        /// chuẩn pickleball (34 inch ở giữa lưới).
        /// </summary>
        private const float DefaultNetHeight = 0.86f;

        /// <summary>Khoảng cách tối thiểu từ lưới tới điểm ngắm của AI (mét).</summary>
        private const float DefaultGapFromNet = 0.5f;

        /// <summary>Độ trễ (giây) trước khi AI chạy về vị trí trung tâm chiến thuật sau cú đánh.</summary>
        private const float RecenterDelay = 0.25f;

        /// <summary>Thời gian bay cơ bản (giây) của từng loại cú đánh, trước khi nội suy.</summary>
        private const float LobBaseTime = 1.6f;
        private const float GroundstrokeBaseTime = 1.1f;
        private const float VolleyBaseTime = 0.7f;
        private const float DropShotBaseTime = 0.9f;
        private const float DinkBaseTime = 0.8f;
        private const float ServeBaseTime = 1.2f;

        /// <summary>Cận dưới/cận trên tuyệt đối của thời gian bay để không sinh cú đánh phi lý.</summary>
        private const float MinShotTime = 0.35f;
        private const float MaxShotTime = 2.2f;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        /// <summary>Nạp FSM riêng của AI và tạo chiến thuật ứng với <see cref="difficulty"/>.</summary>
        protected override void InitializeStates()
        {
            stateDictionary[PlayerState.Idle] = new AIIdleState(this);
            stateDictionary[PlayerState.Serve] = new AIServeState(this);
            stateDictionary[PlayerState.TrackBall] = new AITrackBallState(this);
            stateDictionary[PlayerState.TakeShot] = new AITakeShotState(this);

            aiStrategy = CreateStrategy(difficulty);
        }

        protected override void Start()
        {
            base.Start();

            CacheCourtMetrics();
            RefreshBaseShotEffect();
        }

        protected override void OnDestroy()
        {
            DelayedAction.Cancel(recenterRoutine);
            recenterRoutine = null;

            base.OnDestroy();
        }

        private void OnValidate()
        {
            // Xác suất nhích 2 lần luôn là phần còn lại của hai xác suất kia.
            serveMovement0Probability = Mathf.Clamp01(serveMovement0Probability);
            serveMovement1Probability = Mathf.Clamp01(serveMovement1Probability);
            serveMovement2Probability = Mathf.Clamp01(1f - serveMovement0Probability - serveMovement1Probability);

            if (maxServeDelay < minServeDelay) maxServeDelay = minServeDelay;
        }

        /// <summary>Đọc kích thước sân một lần để các state không phải truy vấn <see cref="Court"/> mỗi frame.</summary>
        private void CacheCourtMetrics()
        {
            Court court = MatchCourt;

            courtWidth = court != null ? court.HalfWidth * 2f : DefaultCourtWidth;
            courtDepth = court != null ? court.HalfLength : DefaultCourtHalfLength;
            kitchenDepth = court != null ? court.kitchenDepth : DefaultKitchenDepth;
            gapFromNet = DefaultGapFromNet;
            netHeight = court != null ? court.netHeight : DefaultNetHeight;
        }

        /// <summary>Cường độ xoáy nền = trung bình cộng của khả năng vung và khả năng tạo xoáy.</summary>
        private void RefreshBaseShotEffect()
        {
            if (profile == null)
            {
                baseShotEffect = 0.5f;
                return;
            }

            baseShotEffect = Mathf.Clamp01((profile.swingAbility + profile.spinAbility) * 0.5f);
        }

        // ------------------------------------------------------------------
        // Độ khó
        // ------------------------------------------------------------------

        /// <summary>
        /// Đổi bậc độ khó của AI: thay <see cref="IAIStrategy"/> và nạp lại bộ chỉ số tương ứng
        /// từ <see cref="playerProfileConfigs"/>.
        /// </summary>
        /// <param name="AIdifficulty">Bậc độ khó mới.</param>
        public void SetAILevel(AIDifficulty AIdifficulty) // [Command]
        {
            difficulty = AIdifficulty;
            aiStrategy = CreateStrategy(AIdifficulty);

            ApplyDifficultyProfile(AIdifficulty);
        }

        /// <summary>
        /// Tự chọn bậc độ khó cho trận sắp tới dựa trên thành tích người chơi
        /// (uỷ quyền cho <see cref="AIDifficultySettings.DetermineDifficultyLevel"/>) rồi gọi
        /// <see cref="SetAILevel"/>.
        /// </summary>
        /// <param name="gamesPlayed">Tổng số trận người chơi đã chơi.</param>
        /// <param name="winRatio">Tỉ lệ thắng của người chơi (0..1).</param>
        /// <param name="mode">Chế độ chọn độ khó; <see cref="AIDifficultyMode.Auto"/> mới thực sự tự chọn.</param>
        /// <param name="settings">Bộ ngưỡng dùng cho chế độ Auto; <c>null</c> sẽ dùng ngưỡng mặc định.</param>
        public void DetermineAILevelBasedOnPlayerStats(int gamesPlayed, float winRatio, AIDifficultyMode mode,
                                                       AIDifficultySettings settings)
        {
            AIDifficultySettings thresholds = settings ?? new AIDifficultySettings();
            SetAILevel(thresholds.DetermineDifficultyLevel(mode, gamesPlayed, winRatio));
        }

        /// <summary>Bốc một <see cref="PlayerProfile"/> thuộc bậc độ khó và áp vào chỉ số runtime.</summary>
        private void ApplyDifficultyProfile(AIDifficulty AIdifficulty)
        {
            if (playerProfileConfigs == null) return;

            PlayerProfile picked = playerProfileConfigs.GetProfile(AIdifficulty);
            if (picked == null) return;

            HandSide handSide = profile != null ? profile.handSide : HandSide.Right;

            playerProfile = picked;
            profile = picked.ToData(handSide);

            stamina = profile.maxStamina;
            movementSpeed = profile.MovementSpeed;

            RefreshBaseShotEffect();
        }

        /// <summary>Tạo chiến thuật ứng với một bậc độ khó.</summary>
        private static IAIStrategy CreateStrategy(AIDifficulty AIdifficulty)
        {
            switch (AIdifficulty)
            {
                case AIDifficulty.Amateur: return new AmateurStrategy();
                case AIDifficulty.Competitor: return new CompetitorStrategy();
                case AIDifficulty.Pro: return new ProStrategy();
                case AIDifficulty.Master: return new MasterStrategy();

                // Tutorial dùng lối đánh an toàn nhất để người mới luôn đỡ được bóng.
                case AIDifficulty.Tutorial:
                case AIDifficulty.Newbie:
                default: return new NewbieStrategy();
            }
        }

        /// <summary>True nếu bậc độ khó hiện tại chơi theo lối tấn công (đứng cao, nhắm sát biên).</summary>
        internal bool IsAggressive => difficulty == AIDifficulty.Pro || difficulty == AIDifficulty.Master;

        // ------------------------------------------------------------------
        // Quyết định cú đánh
        // ------------------------------------------------------------------

        /// <summary>
        /// Hỏi chiến thuật xem cú đánh sắp tới nên rơi vào đâu (đã tính vị trí hiện tại của đối thủ).
        /// </summary>
        /// <returns>Điểm rơi mong muốn trong nửa sân đối phương (world space, <c>y = 0</c>).</returns>
        internal Vector3 DetermineDestination()
        {
            if (aiStrategy == null) aiStrategy = CreateStrategy(difficulty);
            if (courtWidth <= 0f || courtDepth <= 0f) CacheCourtMetrics();

            Vector3 destination = aiStrategy.CalculateShotDestination(GetOpponentPosition(), courtWidth, courtDepth,
                                                                     !IsPositiveCourt);
            destination.y = 0f;

            // Không bao giờ nhắm quá sát lưới: bóng sẽ chạm băng lưới.
            float sign = IsPositiveCourt ? -1f : 1f;
            if (Mathf.Abs(destination.z) < gapFromNet) destination.z = sign * gapFromNet;

            return destination;
        }

        /// <summary>Vị trí đối thủ; nếu chưa đăng ký participant thì lấy điểm đối xứng qua lưới làm ước lượng.</summary>
        private Vector3 GetOpponentPosition()
        {
            if (GameManager.HasInstance)
            {
                string opponentTeamID = GameManager.Instance.GetOpponentTeamID(teamID);
                IMatchParticipant opponent = GameManager.Instance.GetParticipant(opponentTeamID);
                if (opponent != null) return opponent.Position;
            }

            Vector3 mirrored = transform.position;
            mirrored.z = -mirrored.z;
            return mirrored;
        }

        /// <summary>
        /// Hỏi chiến thuật xem nên đánh kiểu gì và ghi lại vào <see cref="currentShotType"/>.
        /// </summary>
        /// <param name="aiPosition">Vị trí AI lúc đánh (world space).</param>
        /// <param name="destination">Điểm rơi đã chọn.</param>
        internal ShotType SelectShotType(Vector3 aiPosition, Vector3 destination)
        {
            if (aiStrategy == null) aiStrategy = CreateStrategy(difficulty);

            currentShotType = aiStrategy.SelectShotType(aiPosition, destination, ballController, netHeight, profile);
            return currentShotType;
        }

        /// <summary>
        /// Thời gian bay mong muốn của cú đánh: lấy mốc theo <paramref name="shotType"/>
        /// (Lob chậm nhất, Volley nhanh nhất) rồi nội suy theo khoảng cách tới điểm rơi và
        /// chỉ số <c>shotPower</c> (đánh càng mạnh, bóng tới càng nhanh).
        /// </summary>
        /// <param name="destination">Điểm rơi mong muốn.</param>
        /// <param name="shotType">Loại cú đánh.</param>
        /// <returns>Thời gian bay (giây), đã kẹp trong khoảng an toàn.</returns>
        internal float CalculateShotTime(Vector3 destination, ShotType shotType)
        {
            float baseTime;
            switch (shotType)
            {
                case ShotType.Lob: baseTime = LobBaseTime; break;
                case ShotType.Volley: baseTime = VolleyBaseTime; break;
                case ShotType.DropShot: baseTime = DropShotBaseTime; break;
                case ShotType.Dink: baseTime = DinkBaseTime; break;
                case ShotType.Serve: baseTime = ServeBaseTime; break;
                default: baseTime = GroundstrokeBaseTime; break;
            }

            Vector3 origin = transform.position;
            float distance = Vector2.Distance(new Vector2(origin.x, origin.z),
                                              new Vector2(destination.x, destination.z));

            float reference = Mathf.Max(1f, courtDepth * 2f);
            float distanceFactor = Mathf.Lerp(0.82f, 1.25f, Mathf.Clamp01(distance / reference));

            float power = profile != null ? Mathf.Clamp01(profile.shotPower) : 0.5f;
            float powerFactor = Mathf.Lerp(1.15f, 0.85f, power);

            currentShotTime = Mathf.Clamp(baseTime * distanceFactor * powerFactor, MinShotTime, MaxShotTime);
            return currentShotTime;
        }

        /// <summary>
        /// Vận tốc ban đầu để bóng bay từ vị trí hiện tại tới <paramref name="destination"/> đúng
        /// <paramref name="shotTime"/> giây (bài toán ném xiên).
        /// <para>Chỉ dùng để suy ra HƯỚNG xoáy; lực thật do
        /// <see cref="BallController.HitBall(string, Vector3, float, float, float, float, Vector3, float)"/> tính lại.</para>
        /// </summary>
        /// <param name="destination">Điểm rơi mong muốn.</param>
        /// <param name="shotTime">Thời gian bay mong muốn (giây).</param>
        internal Vector3 CalculateInitialVelocity(Vector3 destination, float shotTime)
        {
            Vector3 origin = ballController != null
                ? ballController.transform.position
                : transform.position + Vector3.up * (profile != null ? profile.height * 0.7f : 1.2f);

            return Utilities.SolveBallisticVelocity(origin, destination, Mathf.Max(MinShotTime, shotTime),
                                                    Physics.gravity.y);
        }

        /// <summary>True nếu cú đánh này nên được cắt xoáy (theo chỉ số <c>swingSpinProbablity</c>).</summary>
        internal bool ShouldApplySlice()
        {
            if (profile == null) return false;
            return Random.value < Mathf.Clamp01(profile.swingSpinProbablity);
        }

        /// <summary>
        /// Hướng bẻ cong của cú đánh trên mặt phẳng XZ: vuông góc với hướng bay, ưu tiên bẻ RA PHÍA BIÊN
        /// (để bóng chạy xa tầm với đối thủ). Chỉ số <c>spinAbility</c> càng thấp thì hướng càng ngẫu nhiên.
        /// </summary>
        /// <param name="velocity">Vận tốc ban đầu của cú đánh.</param>
        /// <returns>Vector đơn vị trên mặt phẳng XZ; <see cref="Vector3.zero"/> nếu không xác định được.</returns>
        internal Vector3 DetermineSwingSpinDirection(Vector3 velocity)
        {
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            if (flat.sqrMagnitude <= Mathf.Epsilon) return Vector3.zero;

            flat.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, flat);
            if (right.sqrMagnitude <= Mathf.Epsilon) return Vector3.zero;
            right.Normalize();

            // Bẻ về phía biên mà bóng đang hướng tới.
            Vector3 outward = Vector3.right * (flat.x >= 0f ? 1f : -1f);
            float side = Vector3.Dot(right, outward) >= 0f ? 1f : -1f;

            // AI xoáy kém thì đôi lúc bẻ nhầm hướng.
            float control = profile != null ? Mathf.Clamp01(profile.spinAbility) : 0.5f;
            if (Random.value > control * 0.5f + 0.5f) side = -side;

            return right * side;
        }

        // ------------------------------------------------------------------
        // Vị trí chiến thuật
        // ------------------------------------------------------------------

        /// <summary>
        /// Sau mỗi cú đánh, chạy về vị trí trung tâm chiến thuật của bậc độ khó:
        /// AI yếu lùi sâu cuối sân, AI mạnh lên áp sát vạch kitchen.
        /// </summary>
        private void CheckAndRecenter()
        {
            recenterRoutine = null;

            if (this == null || !isActiveAndEnabled) return;

            if (GameManager.HasInstance)
            {
                GameState state = GameManager.Instance.currentState;
                if (state == GameState.GameOver || state == GameState.PointScored) return;
            }

            SetRandomTargetAroundPoint(GetStrategicCenter());
        }

        /// <summary>Vị trí trung tâm chiến thuật trong nửa sân của AI, phụ thuộc bậc độ khó.</summary>
        private Vector3 GetStrategicCenter()
        {
            float sign = IsPositiveCourt ? 1f : -1f;
            float depth;

            switch (difficulty)
            {
                case AIDifficulty.Pro:
                case AIDifficulty.Master:
                    depth = kitchenDepth + 0.9f; // áp sát vạch kitchen để volley/dink
                    break;
                case AIDifficulty.Competitor:
                    depth = Mathf.Lerp(kitchenDepth, courtDepth, 0.5f);
                    break;
                default:
                    depth = courtDepth * 0.78f; // lùi sâu, chỉ đỡ bóng
                    break;
            }

            return new Vector3(0f, 0f, sign * depth);
        }

        // ------------------------------------------------------------------
        // Sự kiện trận đấu
        // ------------------------------------------------------------------

        /// <summary>
        /// Cú đánh của CHÍNH MÌNH thì chỉ hẹn giờ về vị trí trung tâm; cú đánh của đối thủ thì
        /// để lớp cha tính điểm đón bóng và chuyển sang <see cref="PlayerState.TrackBall"/>.
        /// </summary>
        protected override void OnBallHit(string teamID, Vector3 shotLocation, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            if (teamID == this.teamID)
            {
                DelayedAction.Cancel(recenterRoutine);
                recenterRoutine = DelayedAction.Run(RecenterDelay, CheckAndRecenter);
                return;
            }

            base.OnBallHit(teamID, shotLocation, shotTrajetoryInfo);
        }

        /// <summary>Ghi nhớ ô giao được chỉ định rồi để lớp cha đưa AI vào <see cref="PlayerState.Serve"/>.</summary>
        /// <param name="side">Ô giao (Left/Right) theo góc nhìn của AI.</param>
        public override void PrepareServe(ServeSide side)
        {
            currentServeSide = side;
            base.PrepareServe(side);
        }

        /// <summary>Quả giao đã rời vợt: tắt chỉ dẫn, về trạng thái chờ và chạy về vị trí chiến thuật.</summary>
        public override void OnServe()
        {
            base.OnServe();
            CheckAndRecenter();
        }
    }
}
