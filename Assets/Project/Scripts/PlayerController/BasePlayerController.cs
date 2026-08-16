using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Pickleball
{
    /// <summary>
    /// Lớp cha chung của mọi tay vợt (người chơi và AI): chỉ số, di chuyển, thể lực,
    /// FSM <see cref="PlayerState"/>, logic đón bóng theo quỹ đạo dự đoán và đánh bóng.
    ///
    /// <para><b>Kiến trúc</b></para>
    /// <list type="bullet">
    /// <item><description>FSM tự quản bằng <see cref="stateDictionary"/>; lớp con cài
    /// <see cref="InitializeStates"/> để nạp các <see cref="IPlayerState"/> của mình.</description></item>
    /// <item><description>Giao tiếp với <see cref="GameManager"/> qua <see cref="IMatchParticipant"/>,
    /// giao tiếp với quả bóng qua <see cref="BallController"/>.</description></item>
    /// <item><description>Mấu chốt gameplay: khi đối thủ đánh bóng, <see cref="BallController.OnBallHit"/>
    /// mang theo <see cref="ShotTrajetoryInfo"/> (quỹ đạo đã mô phỏng trước), controller dùng
    /// <see cref="GetReachableTrajectoryPoint(float, float, float, float, bool, TrajectoryData, out float)"/>
    /// để chọn điểm đón bóng và chạy tới đó.</description></item>
    /// </list>
    ///
    /// <para>
    /// Phase 1 chạy offline; những chỗ bản gốc mang attribute Mirror được đánh dấu bằng comment
    /// <c>// [Server]</c> / <c>// [ClientRpc]</c> để Phase 2 chỉ việc gắn attribute.
    /// </para>
    /// </summary>
    public abstract class BasePlayerController : MonoBehaviour, IMatchParticipant
    {
        /// <summary>UnityEvent hai tham số (trạng thái mới, payload) — cần lớp con cụ thể vì UnityEvent&lt;T0,T1&gt; là abstract.</summary>
        [Serializable]
        private sealed class StateChangeEvent : UnityEvent<PlayerState, object> { }

        /// <summary>UnityEvent một tham số Vector3 — cần lớp con cụ thể vì UnityEvent&lt;T0&gt; là abstract.</summary>
        [Serializable]
        private sealed class Vector3Event : UnityEvent<Vector3> { }

        // ------------------------------------------------------------------
        // Sự kiện phát ra ngoài (UI, VFX, camera)
        // ------------------------------------------------------------------

        /// <summary>Bắn ra mỗi khi FSM đổi trạng thái: (trạng thái mới, payload tuỳ trạng thái).</summary>
        public UnityEvent<PlayerState, object> onStateChange;

        /// <summary>Bắn ra khi vị trí đích di chuyển được cập nhật.</summary>
        public UnityEvent<Vector3> onTargetPositionUpdated;

        /// <summary>Bắn ra khi điểm đứng đánh bóng (nơi avatar cần quay mặt tới) được cập nhật.</summary>
        public UnityEvent<Vector3> onTargetAvatarPositionUpdated;

        /// <summary>Prefab marker debug cho điểm đích/điểm đón bóng.</summary>
        [Header("Visualization")]
        public GameObject markerPrefab;

        // ------------------------------------------------------------------
        // Chỉ số
        // ------------------------------------------------------------------

        /// <summary>Định danh đội. Phase 2 sẽ thành [SyncVar].</summary>
        [Header("Player Properties")]
        public string teamID;

        /// <summary>Asset chỉ số gốc; được sao chép sang <see cref="profile"/> lúc khởi tạo.</summary>
        public PlayerProfile playerProfile;

        /// <summary>Dải min/max để quy đổi chỉ số shop 0..100 sang giá trị thật.</summary>
        public PlayerProfileLimits profileLimits;

        /// <summary>Chỉ số runtime đang áp dụng. Phase 2 sẽ thành [SyncVar].</summary>
        public PlayerProfileProperties profile;

        /// <summary>Transform của model nhân vật (con của controller).</summary>
        public Transform avatar;

        /// <summary>Component quản lý phần nhìn của nhân vật (mesh, tay thuận, điểm cầm).</summary>
        public PlayerAvatar playerAvatar;

        /// <summary>Điểm gắn quả bóng khi đang giữ bóng chuẩn bị giao.</summary>
        public Transform ballHoldPoint;

        /// <summary>Object chỉ dẫn ô giao bóng, chỉ bật trong pha chuẩn bị giao.</summary>
        public Transform serveIndicator;

        /// <summary>Nguồn phát âm thanh cú đánh.</summary>
        public AudioSource audioSource;

        // ------------------------------------------------------------------
        // Runtime
        // ------------------------------------------------------------------

        /// <summary>Bộ điều khiển animation của nhân vật; có thể null khi chưa gắn rig.</summary>
        [Header("Runtime Properties")]
        public BasePlayerAnimationController animationController;

        /// <summary>Tốc độ di chuyển hiện tại (đã trừ hao theo thể lực).</summary>
        public float movementSpeed;

        /// <summary>Tốc độ xoay vợt về điểm đón bóng (độ/giây).</summary>
        public float PaddleAlignSpeed;

        /// <summary>Tốc độ xoay vợt đã chuẩn hoá 0..1 (dùng cho animation blend).</summary>
        public float PaddleAlignNormalizedSpeed;

        /// <summary>Cận dưới của <see cref="PaddleAlignSpeed"/>, suy ra từ chỉ số Agility.</summary>
        [HideInInspector] public float minPaddleAlignSpeed = 180f;

        /// <summary>Cận trên của <see cref="PaddleAlignSpeed"/>, suy ra từ chỉ số Agility.</summary>
        [HideInInspector] public float maxPaddleAlignSpeed = 720f;

        /// <summary>Thể lực hiện tại. Phase 2 sẽ thành [SyncVar].</summary>
        public float stamina;

        /// <summary>Trạng thái FSM hiện tại. Phase 2 sẽ thành [SyncVar].</summary>
        public PlayerState currentStateEnum;

        /// <summary>Trạng thái FSM ngay trước đó. Phase 2 sẽ thành [SyncVar].</summary>
        public PlayerState previousStateEnum;

        /// <summary>Quả bóng của trận; tự tìm nếu để trống.</summary>
        public BallController ballController;

        /// <summary>Số cú đánh đã thực hiện trong pha bóng hiện tại.</summary>
        public int shotCount;

        /// <summary>True khi đang chủ động đón volley (đánh trước khi bóng nảy).</summary>
        public bool isAttemptingVolley;

        /// <summary>Nút gắn vợt trên tay nhân vật.</summary>
        [Header("Equipment")]
        public Transform paddleHolder;

        /// <summary>Renderer của mặt vợt (đổi material khi đổi skin).</summary>
        public Renderer paddleRenderer;

        /// <summary>VFX hào quang khi booster năng lượng đang chạy.</summary>
        [Header("VFX")]
        public GameObject EnergyVFX;

        /// <summary>Bảng tra trạng thái FSM; lớp con nạp qua <see cref="InitializeStates"/>.</summary>
        protected Dictionary<PlayerState, IPlayerState> stateDictionary;

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private Vector3 targetPosition;
        private bool isPositiveCourt;
        private Vector3 targetBallHitPoint;
        private float targetBallHitTime;
        private Vector3 targetHitStandPoint;
        private bool isEnergyBoostApplied;
        private Vector3 paddleOriginalPosition;
        private Quaternion paddleOriginalRotation;
        private bool paddlePoseCached;
        private Court cachedCourt;
        private GameState lastHandledGameState = (GameState)(-1);
        private bool isSubscribed;

        /// <summary>Người chơi được phép đứng lấn ra ngoài biên sân một khoảng này (mét).</summary>
        protected const float BoundsBleeding = 2f;

        /// <summary>Khoảng cách tối thiểu tới lưới mà người chơi được phép tiến lại.</summary>
        protected const float ValidZDistanceFromNet = 1f;

        /// <summary>Đệm chống kẹt ở mép vùng di chuyển.</summary>
        protected const float EdgeBugOffset = 1f;

        /// <summary>Ngưỡng độ cao coi như bóng đang chạm đất (dùng để dò lần nảy đầu tiên).</summary>
        private const float BounceHeightThreshold = 0.15f;

        /// <summary>Sai lệch góc tối đa (độ) khi chỉ số <c>maxShotError</c> bằng 1.</summary>
        private const float MaxTrackingErrorAngle = 30f;

        /// <summary>Khoảng cách tối thiểu tới đích để coi là "đang di chuyển".</summary>
        private const float MovingThreshold = 0.05f;

        /// <summary>Khoảng lùi của điểm đứng so với điểm đón bóng (mét).</summary>
        private const float StandOffsetFromHitPoint = 0.35f;

        /// <summary>Bán kính ngẫu nhiên khi chọn vị trí chờ quanh một điểm (mét).</summary>
        private const float RandomTargetRadius = 1.2f;

        /// <summary>Độ trễ (giây) của cú đánh khi shotPower = 0.</summary>
        private const float MaxShotAnimationDelay = 0.35f;

        /// <summary>Độ trễ (giây) của cú đánh khi shotPower = 1.</summary>
        private const float MinShotAnimationDelay = 0.12f;

        // ------------------------------------------------------------------
        // Sự kiện animation (do PlayerAnimationEventReceiver bắn lên)
        // ------------------------------------------------------------------

        /// <summary>Animation vừa tới khung "tung bóng" của cú giao.</summary>
        public event Action AnimationThrowBall;

        /// <summary>Animation vừa tới khung "vợt chạm bóng".</summary>
        public event Action AnimationShotHit;

        /// <summary>Animation/vợt vừa xoay khớp xong về điểm đón bóng.</summary>
        public event Action AnimationPaddleAligned;

        // ------------------------------------------------------------------
        // Property
        // ------------------------------------------------------------------

        /// <summary>Điểm trên quỹ đạo mà tay vợt định đón bóng (world space).</summary>
        public Vector3 TargetBallHitPoint => targetBallHitPoint;

        /// <summary>Thời điểm tuyệt đối (<see cref="Time.time"/>) bóng tới <see cref="TargetBallHitPoint"/>.</summary>
        public float TargetBallHitTime => targetBallHitTime;

        /// <summary>Điểm mà tay vợt cần đứng để đánh được <see cref="TargetBallHitPoint"/>.</summary>
        public Vector3 TargetHitStandPoint => targetHitStandPoint;

        /// <summary>True nếu tay vợt đang ở nửa sân dương (z &gt; 0).</summary>
        public bool IsPositiveCourt => isPositiveCourt;

        /// <summary>Định danh đội (cài đặt <see cref="IMatchParticipant"/>).</summary>
        public string TeamID => teamID;

        /// <summary>Vị trí world hiện tại (cài đặt <see cref="IMatchParticipant"/>).</summary>
        public Vector3 Position => transform.position;

        /// <summary>Sân đấu đang dùng; ưu tiên lấy từ <see cref="GameManager"/>, nếu không có thì tự tìm trong scene.</summary>
        public Court MatchCourt
        {
            get
            {
                if (cachedCourt != null) return cachedCourt;
                if (GameManager.HasInstance && GameManager.Instance.court != null) cachedCourt = GameManager.Instance.court;
                if (cachedCourt == null) cachedCourt = FindAnyObjectByType<Court>();
                return cachedCourt;
            }
        }

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        protected virtual void Awake()
        {
            if (onStateChange == null) onStateChange = new StateChangeEvent();
            if (onTargetPositionUpdated == null) onTargetPositionUpdated = new Vector3Event();
            if (onTargetAvatarPositionUpdated == null) onTargetAvatarPositionUpdated = new Vector3Event();

            stateDictionary = new Dictionary<PlayerState, IPlayerState>();

            CachePaddlePose();
            InitializeProfile();

            isPositiveCourt = transform.position.z >= 0f;
            targetPosition = transform.position;
            currentStateEnum = PlayerState.None;
            previousStateEnum = PlayerState.None;

            InitializeStates();
        }

        protected virtual void Start()
        {
            if (ballController == null && BallController.HasInstance) ballController = BallController.Instance;

            if (GameManager.HasInstance) GameManager.Instance.RegisterParticipant(this);

            Subscribe();
            ChangeState(PlayerState.Idle);
        }

        protected virtual void OnDestroy()
        {
            Unsubscribe();
        }

        protected virtual void Update()
        {
            ApplyMovement();
            ManageStamina();
            ApplyStaminaToSpeed();

            if (stateDictionary != null && stateDictionary.TryGetValue(currentStateEnum, out IPlayerState state))
            {
                state?.Update();
            }
        }

        /// <summary>Đăng ký toàn bộ event ngoài. Có bảo vệ chống đăng ký hai lần.</summary>
        private void Subscribe()
        {
            if (isSubscribed) return;
            isSubscribed = true;

            GameManager.OnGameStateChanged += OnGameStateChanged;
            GameManager.OnRuleViolated += OnRuleViolated;
            BallController.OnBallHit += OnBallHit;
        }

        /// <summary>Huỷ đăng ký TẤT CẢ event ngoài — bắt buộc để không rò rỉ khi vào/ra trận nhiều lần.</summary>
        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            isSubscribed = false;

            GameManager.OnGameStateChanged -= OnGameStateChanged;
            GameManager.OnRuleViolated -= OnRuleViolated;
            BallController.OnBallHit -= OnBallHit;
        }

        /// <summary>Lớp con nạp các <see cref="IPlayerState"/> của mình vào <see cref="stateDictionary"/>.</summary>
        protected abstract void InitializeStates();

        /// <summary>Sao chép chỉ số từ asset sang bản runtime và khởi tạo các giá trị dẫn xuất.</summary>
        private void InitializeProfile()
        {
            if (profile == null && playerProfile != null) profile = playerProfile.ToData(HandSide.Right);
            if (profile == null) profile = new PlayerProfileProperties { maxStamina = 100f, MovementSpeed = 3.5f, height = 1.7f };

            stamina = profile.maxStamina;
            movementSpeed = profile.MovementSpeed;
            PaddleAlignSpeed = maxPaddleAlignSpeed;
            PaddleAlignNormalizedSpeed = 1f;
        }

        /// <summary>Ghi nhớ tư thế gốc của vợt để <see cref="ResetPaddlePose"/> khôi phục sau mỗi cú đánh.</summary>
        private void CachePaddlePose()
        {
            if (paddleHolder == null || paddlePoseCached) return;
            paddleOriginalPosition = paddleHolder.localPosition;
            paddleOriginalRotation = paddleHolder.localRotation;
            paddlePoseCached = true;
        }

        // ------------------------------------------------------------------
        // FSM
        // ------------------------------------------------------------------

        /// <summary>
        /// Chuyển FSM sang trạng thái mới: gọi <see cref="IPlayerState.Exit"/> của trạng thái cũ,
        /// <see cref="IPlayerState.Enter"/> của trạng thái mới rồi bắn <see cref="onStateChange"/>.
        /// <para>Cho phép vào lại chính trạng thái đang chạy (Exit rồi Enter) để reset pha đánh.</para>
        /// </summary>
        /// <param name="newState">Trạng thái muốn chuyển tới.</param>
        public void ChangeState(PlayerState newState)
        {
            if (stateDictionary == null) return;

            if (stateDictionary.TryGetValue(currentStateEnum, out IPlayerState oldState)) oldState?.Exit();

            previousStateEnum = currentStateEnum;
            currentStateEnum = newState;

            if (stateDictionary.TryGetValue(newState, out IPlayerState nextState)) nextState?.Enter();

            onStateChange?.Invoke(newState, null);
        }

        /// <summary>Trạng thái FSM đang chạy (null nếu chưa đăng ký).</summary>
        public IPlayerState GetCurrentState()
        {
            if (stateDictionary == null) return null;
            return stateDictionary.TryGetValue(currentStateEnum, out IPlayerState state) ? state : null;
        }

        // ------------------------------------------------------------------
        // Di chuyển
        // ------------------------------------------------------------------

        /// <summary>
        /// Tiến về <see cref="GetTargetPosition"/> với <see cref="movementSpeed"/>, sau đó ép vị trí
        /// nằm trong vùng di chuyển hợp lệ (nửa sân của mình nới thêm <see cref="BoundsBleeding"/>
        /// và không lại gần lưới hơn <see cref="ValidZDistanceFromNet"/>).
        /// </summary>
        public void ApplyMovement()
        {
            Vector3 next = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
            transform.position = ClampToMovementArea(next);
        }

        /// <summary>Đặt vị trí đích di chuyển (tự clamp vào vùng hợp lệ) và bắn event cập nhật.</summary>
        /// <param name="position">Vị trí đích mong muốn (world space).</param>
        public void SetTargetPosition(Vector3 position)
        {
            if (Utilities.IsInvalid(position)) return;

            targetPosition = ClampToMovementArea(position);
            onTargetPositionUpdated?.Invoke(targetPosition);
        }

        /// <summary>Vị trí đích di chuyển hiện tại.</summary>
        public Vector3 GetTargetPosition()
        {
            return targetPosition;
        }

        /// <summary>
        /// Chọn ngẫu nhiên một điểm chờ quanh tâm nửa sân của mình (vị trí "về vị trí" sau mỗi pha bóng)
        /// và đặt luôn làm đích di chuyển.
        /// </summary>
        /// <returns>Điểm vừa chọn.</returns>
        public Vector3 SetRandomTargetAroundCenter() // [Server]
        {
            CourtBounds bounds = GetPlayingSideCourtBounds();
            if (bounds == null)
            {
                SetTargetPosition(transform.position);
                return targetPosition;
            }

            float x = UnityEngine.Random.Range(-bounds.extends.x * 0.5f, bounds.extends.x * 0.5f);
            float z = bounds.center.z + UnityEngine.Random.Range(-bounds.extends.y * 0.3f, bounds.extends.y * 0.3f);

            SetTargetPosition(new Vector3(x, 0f, z));
            return targetPosition;
        }

        /// <summary>Chọn ngẫu nhiên một điểm quanh <paramref name="destination"/> trong bán kính nhỏ rồi đặt làm đích.</summary>
        /// <param name="destination">Tâm vùng ngẫu nhiên (world space).</param>
        public void SetRandomTargetAroundPoint(Vector3 destination)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * RandomTargetRadius;
            SetTargetPosition(new Vector3(destination.x + offset.x, 0f, destination.z + offset.y));
        }

        /// <summary>
        /// Ép một vị trí về vùng di chuyển hợp lệ: nửa sân của mình nới thêm <see cref="BoundsBleeding"/>,
        /// và luôn cách lưới ít nhất <see cref="ValidZDistanceFromNet"/>.
        /// </summary>
        private Vector3 ClampToMovementArea(Vector3 position)
        {
            position.y = 0f;

            CourtBounds bounds = GetPlayingSideCourtBounds();
            if (bounds != null)
            {
                Vector2 extents = bounds.extends + new Vector2(BoundsBleeding, BoundsBleeding);
                position = Utilities.ClampToBounds(position, bounds.center, extents);
            }

            // Không được lấn qua (hoặc dí sát) lưới ở z = 0.
            if (isPositiveCourt) position.z = Mathf.Max(position.z, ValidZDistanceFromNet);
            else position.z = Mathf.Min(position.z, -ValidZDistanceFromNet);

            return position;
        }

        // ------------------------------------------------------------------
        // Thể lực
        // ------------------------------------------------------------------

        /// <summary>
        /// Cập nhật thể lực mỗi frame: đang di chuyển thì tiêu hao <c>staminaDepletionRate</c>/giây,
        /// đứng yên thì hồi <c>staminaRechargeRate</c>/giây; luôn clamp trong [0, maxStamina].
        /// </summary>
        public void ManageStamina()
        {
            if (profile == null) return;

            bool isMoving = (transform.position - targetPosition).sqrMagnitude > MovingThreshold * MovingThreshold;

            if (isMoving) stamina -= profile.staminaDepletionRate * Time.deltaTime;
            else stamina += profile.staminaRechargeRate * Time.deltaTime;

            stamina = Mathf.Clamp(stamina, 0f, profile.maxStamina);
        }

        /// <summary>
        /// Quy đổi thể lực thành tốc độ: kiệt sức chỉ còn 50% tốc độ gốc, đầy thể lực đạt 100%.
        /// Đồng thời cập nhật tốc độ xoay vợt theo cùng tỉ lệ.
        /// </summary>
        public void ApplyStaminaToSpeed()
        {
            if (profile == null || profile.maxStamina <= 0f) return;

            float normalized = Mathf.Clamp01(stamina / profile.maxStamina);

            movementSpeed = Mathf.Lerp(profile.MovementSpeed * 0.5f, profile.MovementSpeed, normalized);
            PaddleAlignNormalizedSpeed = normalized;
            PaddleAlignSpeed = Mathf.Lerp(minPaddleAlignSpeed, maxPaddleAlignSpeed, normalized);
        }

        /// <summary>Trừ thẳng một lượng thể lực (ví dụ sau một cú đánh mạnh).</summary>
        /// <param name="depletionAmount">Lượng thể lực bị trừ.</param>
        public void DepleteStamina(float depletionAmount)
        {
            if (profile == null) return;
            stamina = Mathf.Clamp(stamina - depletionAmount, 0f, profile.maxStamina);
        }

        /// <summary>Cộng thẳng một lượng thể lực (ví dụ booster stamina).</summary>
        /// <param name="rechargeAmount">Lượng thể lực được hồi.</param>
        public void RechargeStamina(float rechargeAmount)
        {
            if (profile == null) return;
            stamina = Mathf.Clamp(stamina + rechargeAmount, 0f, profile.maxStamina);
        }

        /// <summary>
        /// Áp dụng booster năng lượng: nhân <c>maxStamina</c> và <c>staminaRechargeRate</c> với
        /// <c>GameSettings.EnergyBoostMultiplier</c>, hồi đầy thể lực và bật <see cref="EnergyVFX"/>.
        /// Chỉ có tác dụng một lần mỗi trận.
        /// </summary>
        [ContextMenu("Apply Energy Boost")]
        public void ApplyEnergyBoost()
        {
            if (profile == null || isEnergyBoostApplied) return;

            GameSettings settings = GameManager.HasInstance ? GameManager.Instance.settings : null;
            float multiplier = settings != null && settings.EnergyBoostMultiplier > 0f ? settings.EnergyBoostMultiplier : 1f;

            profile.maxStamina *= multiplier;
            profile.staminaRechargeRate *= multiplier;
            stamina = profile.maxStamina;

            isEnergyBoostApplied = true;
            if (EnergyVFX != null) EnergyVFX.SetActive(true);
        }

        // ------------------------------------------------------------------
        // Đón bóng
        // ------------------------------------------------------------------

        /// <summary>
        /// Tìm điểm đón bóng tối ưu trên quỹ đạo vừa được mô phỏng và chạy tới đó.
        /// <para>
        /// Thứ tự thử: (1) quỹ đạo chính với tầm với volley — dùng khi đang chủ động lên volley;
        /// (2) quỹ đạo chính với tầm với thường; (3) quỹ đạo sau lần nảy thứ hai.
        /// Không với tới điểm nào thì lùi về giữa nửa sân của mình.
        /// </para>
        /// </summary>
        /// <param name="shotLocation">Nơi đối thủ vừa đánh bóng (world space).</param>
        /// <param name="shotTrajetoryInfo">Quỹ đạo dự đoán của cú đánh đó.</param>
        public void FindOptimalTargetPositionForTrackingBall(Vector3 shotLocation, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            targetBallHitPoint = Vector3.zero;
            targetBallHitTime = -1f;

            if (shotTrajetoryInfo == null)
            {
                SetRandomTargetAroundCenter();
                return;
            }

            float speed = Mathf.Max(0.01f, movementSpeed);
            float kitchenDepth = MatchCourt != null ? MatchCourt.kitchenDepth : 2.1336f;
            float height = profile != null ? profile.height : 1.7f;
            float volleyRange = profile != null ? profile.volleyRange : 2f;

            float travelTime;
            Vector3 point = Vector3.zero;

            if (isAttemptingVolley)
            {
                point = GetReachableTrajectoryPoint(speed, kitchenDepth, height, volleyRange, isPositiveCourt,
                                                    shotTrajetoryInfo.NormaltrajectoryData, out travelTime);
            }
            else
            {
                point = GetReachableTrajectoryPoint(speed, kitchenDepth, height, isPositiveCourt,
                                                    shotTrajetoryInfo.NormaltrajectoryData, out travelTime);
            }

            if (travelTime < 0f)
            {
                point = GetReachableTrajectoryPoint(speed, kitchenDepth, height, isPositiveCourt,
                                                    shotTrajetoryInfo.SecondBounceTrajectory, out travelTime);
            }

            if (travelTime < 0f)
            {
                // Không với tới: về giữa sân chờ pha sau.
                SetRandomTargetAroundCenter();
                return;
            }

            targetBallHitPoint = point;
            targetBallHitTime = Time.time + travelTime;

            // Đứng lùi lại một chút so với điểm chạm bóng để vung được vợt.
            float sign = isPositiveCourt ? 1f : -1f;
            Vector3 stand = new Vector3(point.x, 0f, point.z + sign * StandOffsetFromHitPoint);

            float errorAngle = profile != null ? profile.maxShotError * MaxTrackingErrorAngle : 0f;
            stand = AddErrorToDestination(stand, errorAngle);

            targetHitStandPoint = ClampToMovementArea(stand);

            SetTargetPosition(targetHitStandPoint);
            onTargetAvatarPositionUpdated?.Invoke(targetBallHitPoint);
        }

        /// <summary>
        /// Chọn điểm trên quỹ đạo mà tay vợt kịp chạy tới và với tới, dùng TẦM VỚI VOLLEY.
        /// </summary>
        /// <param name="playerSpeed">Tốc độ chạy hiện tại (m/s).</param>
        /// <param name="kitchenDepth">Chiều sâu vùng cấm volley tính từ lưới.</param>
        /// <param name="playerHeight">Chiều cao nhân vật (mét).</param>
        /// <param name="volleyRange">Tầm với khi volley (mét) — cộng vào chiều cao để ra trần với tới.</param>
        /// <param name="isPositiveCourt">Nửa sân của tay vợt.</param>
        /// <param name="trajetoryData">Quỹ đạo cần duyệt.</param>
        /// <param name="travelTime">Thời gian (giây) kể từ bây giờ tới lúc bóng ở điểm trả về; <c>-1</c> nếu không với tới.</param>
        /// <returns>Điểm đón bóng, hoặc <see cref="Vector3.zero"/> nếu không với tới.</returns>
        public Vector3 GetReachableTrajectoryPoint(float playerSpeed, float kitchenDepth, float playerHeight,
                                                   float volleyRange, bool isPositiveCourt,
                                                   TrajectoryData trajetoryData, out float travelTime)
        {
            return FindReachablePoint(playerSpeed, kitchenDepth, playerHeight, volleyRange, isPositiveCourt,
                                      trajetoryData, out travelTime);
        }

        /// <summary>
        /// Chọn điểm trên quỹ đạo mà tay vợt kịp chạy tới và với tới, dùng TẦM VỚI THƯỜNG
        /// (<c>profile.shotRange</c>).
        /// </summary>
        /// <param name="playerSpeed">Tốc độ chạy hiện tại (m/s).</param>
        /// <param name="kitchenDepth">Chiều sâu vùng cấm volley tính từ lưới.</param>
        /// <param name="playerHeight">Chiều cao nhân vật (mét).</param>
        /// <param name="isPositiveCourt">Nửa sân của tay vợt.</param>
        /// <param name="trajetoryData">Quỹ đạo cần duyệt.</param>
        /// <param name="travelTime">Thời gian (giây) kể từ bây giờ tới lúc bóng ở điểm trả về; <c>-1</c> nếu không với tới.</param>
        /// <returns>Điểm đón bóng, hoặc <see cref="Vector3.zero"/> nếu không với tới.</returns>
        public Vector3 GetReachableTrajectoryPoint(float playerSpeed, float kitchenDepth, float playerHeight,
                                                   bool isPositiveCourt, TrajectoryData trajetoryData,
                                                   out float travelTime)
        {
            float reach = profile != null ? profile.shotRange : 1f;
            return FindReachablePoint(playerSpeed, kitchenDepth, playerHeight, reach, isPositiveCourt,
                                      trajetoryData, out travelTime);
        }

        /// <summary>
        /// Lõi của thuật toán đón bóng.
        /// <para><b>Điều kiện một điểm được coi là ĐẾN ĐƯỢC:</b></para>
        /// <list type="number">
        /// <item><description><c>Vector3.Distance(vị trí hiện tại, point) / playerSpeed &lt;= timeStamps[i]</c>
        /// — kịp chạy tới trước khi bóng tới đó;</description></item>
        /// <item><description><c>point.y &lt;= playerHeight + reach</c> — với tới được;</description></item>
        /// <item><description>điểm nằm trong nửa sân của mình (nới <see cref="BoundsBleeding"/>);</description></item>
        /// <item><description>nếu đây là điểm TRƯỚC lần nảy đầu tiên (tức là một cú volley) thì
        /// chỗ đứng không được nằm trong kitchen — luật cấm volley trong kitchen.</description></item>
        /// </list>
        /// <para>
        /// <b>Ưu tiên:</b> <see cref="isAttemptingVolley"/> = true → lấy điểm SỚM NHẤT (chặn bóng trước khi nảy);
        /// ngược lại → ưu tiên điểm đầu tiên SAU lần nảy đầu (đánh nảy chuẩn), nếu không có thì dùng
        /// điểm sớm nhất làm phương án dự phòng.
        /// </para>
        /// </summary>
        private Vector3 FindReachablePoint(float playerSpeed, float kitchenDepth, float playerHeight, float reach,
                                           bool isPositiveCourt, TrajectoryData trajetoryData, out float travelTime)
        {
            travelTime = -1f;

            if (trajetoryData == null || !trajetoryData.IsValid) return Vector3.zero;
            if (playerSpeed <= 0f) return Vector3.zero;

            List<Vector3> points = trajetoryData.Points;
            List<float> times = trajetoryData.timeStamps;
            int count = Mathf.Min(points.Count, times.Count);
            if (count == 0) return Vector3.zero;

            int firstBounceIndex = GetFirstBounceIndex(trajetoryData);
            float reachableHeight = playerHeight + reach;
            Vector3 origin = transform.position;

            int earliestIndex = -1;
            int afterBounceIndex = -1;

            for (int i = 0; i < count; i++)
            {
                Vector3 point = points[i];
                float time = times[i];

                if (time <= 0f) continue;
                if (Utilities.IsInvalid(point)) continue;
                if (point.y > reachableHeight) continue;
                if (!IsInPlayingSideCourtBounds(point)) continue;

                // Kịp chạy tới?
                float distance = Vector3.Distance(origin, new Vector3(point.x, origin.y, point.z));
                if (distance / playerSpeed > time) continue;

                // Volley (chưa nảy) mà chỗ đứng trong kitchen là phạm luật -> bỏ qua.
                bool isBeforeFirstBounce = firstBounceIndex < 0 || i < firstBounceIndex;
                if (isBeforeFirstBounce && Mathf.Abs(point.z) < kitchenDepth) continue;

                if (earliestIndex < 0) earliestIndex = i;

                if (!isBeforeFirstBounce && afterBounceIndex < 0)
                {
                    afterBounceIndex = i;
                    if (!isAttemptingVolley) break;
                }

                if (isAttemptingVolley) break;
            }

            int chosen = isAttemptingVolley
                ? earliestIndex
                : (afterBounceIndex >= 0 ? afterBounceIndex : earliestIndex);

            if (chosen < 0) return Vector3.zero;

            travelTime = times[chosen];
            return points[chosen];
        }

        /// <summary>
        /// Chỉ số của điểm đầu tiên coi như bóng đã chạm đất lần một
        /// (đã từng bay cao hơn <see cref="BounceHeightThreshold"/> rồi tụt xuống dưới ngưỡng đó).
        /// Trả về <c>-1</c> nếu quỹ đạo không có lần nảy nào.
        /// </summary>
        private static int GetFirstBounceIndex(TrajectoryData trajetoryData)
        {
            if (trajetoryData == null || !trajetoryData.IsValid) return -1;

            List<Vector3> points = trajetoryData.Points;
            bool wasAbove = false;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].y > BounceHeightThreshold)
                {
                    wasAbove = true;
                    continue;
                }
                if (wasAbove) return i;
            }
            return -1;
        }

        /// <summary>
        /// Điểm cao nhất của một quỹ đạo (dùng để chọn animation lob / smash).
        /// </summary>
        /// <param name="trajetoryData">Quỹ đạo cần xét.</param>
        /// <param name="highestPointTime">Thời điểm (giây, tương đối từ lúc đánh) bóng ở đỉnh; <c>-1</c> nếu quỹ đạo rỗng.</param>
        public Vector3 GetHighestPoint(TrajectoryData trajetoryData, out float highestPointTime)
        {
            highestPointTime = -1f;
            if (trajetoryData == null || !trajetoryData.IsValid) return Vector3.zero;

            List<Vector3> points = trajetoryData.Points;
            List<float> times = trajetoryData.timeStamps;
            int count = Mathf.Min(points.Count, times.Count);
            if (count == 0) return Vector3.zero;

            int best = 0;
            for (int i = 1; i < count; i++)
            {
                if (points[i].y > points[best].y) best = i;
            }

            highestPointTime = times[best];
            return points[best];
        }

        /// <summary>
        /// Thêm sai số bám bóng: với xác suất <c>profile.TrackingErrorProbablity</c>, xoay điểm đích
        /// quanh trục Y (tâm là vị trí tay vợt) một góc ngẫu nhiên trong ±<paramref name="degreeOfError"/>.
        /// Người chơi/AI chỉ số kém sẽ chạy lệch chỗ đón bóng.
        /// </summary>
        /// <param name="targetPosition">Điểm đích chuẩn.</param>
        /// <param name="degreeOfError">Sai lệch góc tối đa tính bằng độ.</param>
        public Vector3 AddErrorToDestination(Vector3 targetPosition, float degreeOfError)
        {
            if (profile == null || degreeOfError <= 0f) return targetPosition;
            if (UnityEngine.Random.value > profile.TrackingErrorProbablity) return targetPosition;

            Vector3 origin = transform.position;
            Vector3 offset = targetPosition - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.000001f) return targetPosition;

            float angle = UnityEngine.Random.Range(-degreeOfError, degreeOfError);
            Vector3 rotated = Quaternion.Euler(0f, angle, 0f) * offset;

            return new Vector3(origin.x + rotated.x, 0f, origin.z + rotated.z);
        }

        // ------------------------------------------------------------------
        // Giao bóng
        // ------------------------------------------------------------------

        /// <summary>
        /// Chuẩn bị giao bóng: đứng vào ô giao được chỉ định, bật chỉ dẫn và vào
        /// <see cref="PlayerState.Serve"/> (cài đặt <see cref="IMatchParticipant"/>).
        /// </summary>
        /// <param name="side">Ô giao (Left/Right) theo góc nhìn của người giao.</param>
        public virtual void PrepareServe(ServeSide side)
        {
            shotCount = 0;
            isAttemptingVolley = false;

            if (MatchCourt != null)
            {
                Vector3 servePosition = MatchCourt.GetServePosition(isPositiveCourt, side);
                transform.position = ClampToMovementArea(servePosition);
            }

            SetTargetPosition(transform.position);
            ShowServeIndicator();
            ChangeState(PlayerState.Serve);
        }

        /// <summary>Được gọi ngay sau khi quả giao rời vợt: tắt chỉ dẫn và trở về trạng thái chờ.</summary>
        public virtual void OnServe()
        {
            HideServeIndicator();
            ChangeState(PlayerState.Idle);
        }

        /// <summary>Đưa tay vợt về tư thế/vị trí sẵn sàng cho pha bóng mới.</summary>
        public virtual void ReadyForRally()
        {
            shotCount = 0;
            isAttemptingVolley = false;
            HideServeIndicator();
            ResetPaddlePose();
            SetRandomTargetAroundCenter();
            ChangeState(PlayerState.Idle);
        }

        /// <summary>Bật object chỉ dẫn ô giao bóng.</summary>
        public void ShowServeIndicator()
        {
            if (serveIndicator != null) serveIndicator.gameObject.SetActive(true);
        }

        /// <summary>Tắt object chỉ dẫn ô giao bóng.</summary>
        public void HideServeIndicator()
        {
            if (serveIndicator != null) serveIndicator.gameObject.SetActive(false);
        }

        /// <summary>Gắn quả bóng vào tay cầm để chuẩn bị giao.</summary>
        public void HoldBall() // [Server]
        {
            if (ballController == null || ballHoldPoint == null) return;
            ballController.HoldBall(ballHoldPoint);
        }

        /// <summary>Thả quả bóng khỏi tay cầm (khung animation tung bóng).</summary>
        public void UnholdBall() // [Server]
        {
            if (ballController == null) return;
            ballController.UnholdBall();
        }

        // ------------------------------------------------------------------
        // Đánh bóng
        // ------------------------------------------------------------------

        /// <summary>
        /// Đánh bóng theo cú vuốt của NGƯỜI CHƠI — uỷ quyền toàn bộ tính toán lực/xoáy cho
        /// <see cref="BallController.HitBallServer"/>.
        /// </summary>
        /// <param name="teamID">Đội thực hiện cú đánh.</param>
        /// <param name="swipeData">Dữ liệu cú vuốt đã phân tích.</param>
        /// <param name="playerDepth">Độ sâu chuẩn hoá của tay vợt so với lưới (0 = sát lưới, 1 = cuối sân).</param>
        /// <param name="swingAbility">Khả năng vung vợt (0..1).</param>
        /// <param name="spinAbility">Khả năng tạo xoáy (0..1).</param>
        /// <param name="shotPowerFactor">Hệ số lực đánh (0..1), đã tính cả booster.</param>
        public void HitBall(string teamID, SwipeData swipeData, float playerDepth, float swingAbility,
                            float spinAbility, float shotPowerFactor) // [Server]
        {
            if (ballController == null || swipeData == null) return;

            Vector3 destination = ClampDestinationToOpponentCourt(swipeData.DestinationPoint);

            ballController.HitBallServer(teamID, swipeData, destination, playerDepth, swingAbility, spinAbility,
                                         shotPowerFactor);

            OnBallHitPerformed();
        }

        /// <summary>
        /// Đánh bóng theo tính toán của AI — uỷ quyền cho <see cref="BallController.HitBall"/>
        /// (giải bài toán ném xiên theo thời gian bay mong muốn).
        /// </summary>
        /// <param name="teamID">Đội thực hiện cú đánh.</param>
        /// <param name="destination">Điểm rơi mong muốn (world space).</param>
        /// <param name="shotTime">Thời gian bay mong muốn (giây).</param>
        /// <param name="swingSpinIntensity">Cường độ xoáy/vung của cú đánh.</param>
        /// <param name="swingAbility">Khả năng vung vợt (0..1).</param>
        /// <param name="spinAbility">Khả năng tạo xoáy (0..1).</param>
        /// <param name="swingSpinDirection">Hướng xoáy trên mặt phẳng XZ.</param>
        /// <param name="length">Độ dài cú vung quy đổi.</param>
        public void HitBall(string teamID, Vector3 destination, float shotTime, float swingSpinIntensity,
                            float swingAbility, float spinAbility, Vector3 swingSpinDirection, float length) // [Server]
        {
            if (ballController == null) return;

            ballController.HitBall(teamID, destination, shotTime, swingSpinIntensity, swingAbility, spinAbility,
                                   swingSpinDirection, length);

            OnBallHitPerformed();
        }

        /// <summary>Phần dùng chung sau mỗi cú đánh: đếm cú đánh, phát âm thanh, trả vợt về tư thế gốc.</summary>
        private void OnBallHitPerformed()
        {
            shotCount++;
            isAttemptingVolley = false;

            if (audioSource != null && audioSource.clip != null) audioSource.Play(); // [ClientRpc] RpcPlayBallHitAudio

            ResetPaddlePose();

            // Luật (đúng lượt, volley/kitchen) do BallController báo cho GameManager qua
            // GameManager.Instance.OnBallHit(teamID) — KHÔNG gọi ở đây để tránh tính lỗi hai lần.
        }

        /// <summary>
        /// Ép điểm rơi nằm trong nửa sân đối phương. Người chơi vuốt hụt vẫn ra một điểm hợp lệ để bắn bóng,
        /// việc bóng có ra ngoài hay không do vật lý quyết định.
        /// </summary>
        private Vector3 ClampDestinationToOpponentCourt(Vector3 destination)
        {
            Court court = MatchCourt;
            if (court == null) return destination;

            CourtBounds opponent = court.GetHalfCourtBounds(!isPositiveCourt);
            Vector3 clamped = Utilities.ClampToBounds(destination, opponent.center, opponent.extends);
            clamped.y = 0f;
            return clamped;
        }

        /// <summary>
        /// Kích hoạt animation đánh bóng và hẹn giờ gọi <paramref name="callback"/> đúng khoảnh khắc
        /// vợt chạm bóng. Đóng vai trò PHƯƠNG ÁN DỰ PHÒNG khi chưa có rig/animation event:
        /// state gọi hàm này và tự chống gọi hai lần bằng cờ riêng.
        /// </summary>
        /// <param name="shotPower">Lực đánh 0..1 — càng mạnh thì vung càng nhanh, độ trễ càng ngắn.</param>
        /// <param name="callback">Hành động chạy tại khoảnh khắc chạm bóng.</param>
        public void HitBallAnimation(float shotPower, Action callback)
        {
            float delay = Mathf.Lerp(MaxShotAnimationDelay, MinShotAnimationDelay, Mathf.Clamp01(shotPower));

            if (animationController != null)
            {
                ShotAnimationType animationType = animationController.DetermineShotAnimation(
                    ShotType.Groundstroke, GetShotSideForPoint(targetBallHitPoint), targetBallHitPoint.y,
                    profile != null ? profile.height : 1.7f);
                animationController.PlayShot(animationType);
            }

            if (callback != null) DelayedAction.RunUnscaled(delay, callback);
        }

        /// <summary>Bóng đang ở bên phải hay bên trái thân người (theo hướng nhìn về lưới).</summary>
        /// <param name="point">Điểm cần xét (world space).</param>
        public ShotSide GetShotSideForPoint(Vector3 point)
        {
            Vector3 local = point - transform.position;
            // Người ở nửa sân dương nhìn về -Z nên trục "phải" của họ là -X.
            float lateral = isPositiveCourt ? -local.x : local.x;

            if (Mathf.Abs(lateral) < 0.2f) return ShotSide.Middle;
            return lateral > 0f ? ShotSide.Right : ShotSide.Left;
        }

        /// <summary>Trả vợt về đúng tư thế gốc đã ghi nhớ lúc <see cref="Awake"/>.</summary>
        public void ResetPaddlePose() // [Server]
        {
            if (paddleHolder == null || !paddlePoseCached) return;
            paddleHolder.localPosition = paddleOriginalPosition;
            paddleHolder.localRotation = paddleOriginalRotation;
        }

        /// <summary>
        /// True nếu được phép lên volley lúc này: đang trong pha bóng và tay vợt KHÔNG đứng trong kitchen.
        /// (Luật two-bounce do <see cref="GameManager"/> kiểm tra lại khi bóng thực sự được đánh.)
        /// </summary>
        public bool CanAttemptVolley()
        {
            if (!GameManager.HasInstance) return false;
            if (GameManager.Instance.currentState != GameState.InPlay) return false;

            Court court = MatchCourt;
            if (court != null && court.IsInKitchen(transform.position)) return false;

            return true;
        }

        /// <summary>Bật/tắt ý định lên volley cho pha bóng hiện tại.</summary>
        /// <param name="value">True để chủ động chặn bóng trước khi nảy.</param>
        public void SetAttemptingVolley(bool value)
        {
            isAttemptingVolley = value;
        }

        // ------------------------------------------------------------------
        // Trang bị
        // ------------------------------------------------------------------

        /// <summary>
        /// Áp dụng skin vợt đang chọn. Phase hiện tại mới chỉ đổi material của
        /// <see cref="paddleRenderer"/> nếu shop đã cung cấp; phần prefab/grip chờ hệ thống Shop.
        /// </summary>
        /// <param name="paddleIndex">Chỉ số mẫu vợt.</param>
        /// <param name="gripIndex">Chỉ số cán vợt.</param>
        /// <param name="selectPaddleTextureIndex">Chỉ số hoạ tiết mặt vợt.</param>
        /// <param name="handSide">Tay thuận, quyết định nút gắn vợt trái/phải.</param>
        public void SetPaddleVisual(int paddleIndex, int gripIndex, int selectPaddleTextureIndex,
                                    HandSide handSide = HandSide.Right)
        {
            if (playerAvatar != null)
            {
                playerAvatar.SetHandSide(handSide);
                Transform holder = playerAvatar.GetPaddleHolder(handSide);
                if (holder != null) paddleHolder = holder;

                Transform hold = playerAvatar.GetBallHoldPoint(handSide);
                if (hold != null) ballHoldPoint = hold;

                paddlePoseCached = false;
                CachePaddlePose();
            }

            if (profile != null) profile.handSide = handSide;

            // TODO P10: lấy prefab vợt / material grip / texture mặt vợt từ Shop theo các index này.
            if (paddleRenderer != null && paddleRenderer.sharedMaterial != null)
            {
                paddleRenderer.material = paddleRenderer.sharedMaterial;
            }
        }

        // ------------------------------------------------------------------
        // Truy vấn sân
        // ------------------------------------------------------------------

        /// <summary>Nửa sân mà tay vợt này thi đấu; <c>null</c> nếu scene chưa có <see cref="Court"/>.</summary>
        public CourtBounds GetPlayingSideCourtBounds()
        {
            Court court = MatchCourt;
            return court != null ? court.GetHalfCourtBounds(isPositiveCourt) : null;
        }

        /// <summary>True nếu một vị trí nằm trong nửa sân của tay vợt (đã nới <see cref="BoundsBleeding"/>).</summary>
        /// <param name="position">Vị trí cần kiểm tra (world space).</param>
        public bool IsInPlayingSideCourtBounds(Vector3 position)
        {
            CourtBounds bounds = GetPlayingSideCourtBounds();
            if (bounds == null) return true;

            return Mathf.Abs(position.x - bounds.center.x) <= bounds.extends.x + BoundsBleeding
                && Mathf.Abs(position.z - bounds.center.z) <= bounds.extends.y + BoundsBleeding;
        }

        /// <summary>True nếu quả bóng đang ở nửa sân của tay vợt này.</summary>
        public bool IsBallInMyCourt()
        {
            if (ballController == null) return false;
            float z = ballController.transform.position.z;
            return isPositiveCourt ? z >= 0f : z <= 0f;
        }

        /// <summary>Đặt nửa sân thi đấu của tay vợt (gọi lúc dựng trận).</summary>
        /// <param name="positiveCourt">True nếu đứng ở nửa sân z &gt; 0.</param>
        public void SetCourtSide(bool positiveCourt)
        {
            isPositiveCourt = positiveCourt;
        }

        // ------------------------------------------------------------------
        // Sự kiện trận đấu
        // ------------------------------------------------------------------

        /// <summary>
        /// Trạng thái trận đấu đổi (cài đặt <see cref="IMatchParticipant"/>).
        /// <para>
        /// <see cref="GameManager"/> vừa bắn event tĩnh vừa gọi hàm này nên có chống xử lý trùng
        /// bằng <c>lastHandledGameState</c>.
        /// </para>
        /// </summary>
        /// <param name="state">Trạng thái mới của trận.</param>
        public void OnMatchStateChanged(GameState state)
        {
            ApplyGameState(state);
        }

        private void OnGameStateChanged(GameState state)
        {
            ApplyGameState(state);
        }

        /// <summary>Phản ứng của tay vợt với từng trạng thái trận đấu.</summary>
        protected virtual void ApplyGameState(GameState state)
        {
            if (lastHandledGameState == state) return;
            lastHandledGameState = state;

            switch (state)
            {
                case GameState.PreServe:
                    // Người giao được GameManager gọi riêng PrepareServe; người nhận về vị trí chờ.
                    if (GameManager.HasInstance && GameManager.Instance.GetCurrentServerTeamID() != teamID)
                    {
                        ReadyForRally();
                    }
                    break;

                case GameState.PointScored:
                case GameState.GameOver:
                    isAttemptingVolley = false;
                    ResetPaddlePose();
                    ChangeState(PlayerState.Idle);
                    if (state == GameState.GameOver && animationController != null) animationController.PlayVictory();
                    break;
            }
        }

        /// <summary>Có lỗi luật xảy ra — mặc định chỉ dọn trạng thái đánh bóng dở dang.</summary>
        /// <param name="ruleViolated">Loại lỗi.</param>
        /// <param name="teamFault">Đội phạm lỗi.</param>
        /// <param name="message">Thông báo hiển thị.</param>
        protected virtual void OnRuleViolated(RuleType ruleViolated, string teamFault, string message)
        {
            isAttemptingVolley = false;
        }

        /// <summary>
        /// Đối thủ vừa đánh bóng: tính điểm đón bóng từ quỹ đạo dự đoán rồi vào
        /// <see cref="PlayerState.TrackBall"/>. Cú đánh của chính mình thì bỏ qua.
        /// </summary>
        /// <param name="teamID">Đội vừa đánh bóng.</param>
        /// <param name="shotLocation">Vị trí đánh bóng (world space).</param>
        /// <param name="shotTrajetoryInfo">Quỹ đạo dự đoán của cú đánh.</param>
        protected virtual void OnBallHit(string teamID, Vector3 shotLocation, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            if (teamID == this.teamID) return;

            SetAttemptingVolley(CanAttemptVolley());
            FindOptimalTargetPositionForTrackingBall(shotLocation, shotTrajetoryInfo);
            ChangeState(PlayerState.TrackBall);
        }

        // ------------------------------------------------------------------
        // Cầu nối animation event
        // ------------------------------------------------------------------

        /// <summary>Được <see cref="PlayerAnimationEventReceiver"/> gọi ở khung tung bóng.</summary>
        public void RaiseAnimationThrowBall()
        {
            AnimationThrowBall?.Invoke();
        }

        /// <summary>Được <see cref="PlayerAnimationEventReceiver"/> gọi ở khung vợt chạm bóng.</summary>
        public void RaiseAnimationShotHit()
        {
            AnimationShotHit?.Invoke();
        }

        /// <summary>Được <see cref="PlayerAnimationEventReceiver"/> hoặc <see cref="RacquetAnimator"/> gọi khi vợt đã khớp hướng.</summary>
        public void RaiseAnimationPaddleAligned()
        {
            AnimationPaddleAligned?.Invoke();
        }
    }
}
