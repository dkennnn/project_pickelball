using System;
using System.Collections;
using System.Collections.Generic;
using Pickleball.Network;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quả bóng: vật lý thật (Rigidbody) + toàn bộ thuật toán biến một cú vuốt thành lực đánh,
    /// + cầu nối tới hệ dự đoán quỹ đạo <see cref="PhysicsTrajectoryHandler"/>.
    ///
    /// <para><b>Nguyên tắc cốt lõi</b>: mọi tham số của cú đánh (vận tốc, xoáy, lực bên, thời lượng
    /// swing) được chốt xong TRƯỚC khi bóng bay, rồi đúng bộ tham số ấy được đưa vào physics scene
    /// phụ để mô phỏng. Nhờ vậy quỹ đạo dự đoán trùng khít quỹ đạo thật, và cả người chơi lẫn AI
    /// đều biết trước bóng rơi ở đâu, lúc nào.</para>
    ///
    /// <para><b>Hệ toạ độ</b> (theo <see cref="Court"/>): sân trên mặt phẳng XZ, mặt sân <c>y = 0</c>,
    /// lưới tại <c>z = 0</c>, <c>z &gt; 0</c> là positive court.</para>
    ///
    /// <para><b>Unity 6</b>: dùng <c>rb.linearVelocity</c>; <c>rb.velocity</c> đã bị gỡ bỏ.</para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : NetworkSingleton<BallController>
    {
        #region Nested types

        /// <summary>Phân loại va chạm mà bóng quan tâm.</summary>
        public enum CollisionType
        {
            /// <summary>Chạm mặt sân.</summary>
            Ground = 0,

            /// <summary>Chạm lưới.</summary>
            Net = 1
        }

        /// <summary>Chữ ký callback cho sự kiện va chạm của bóng.</summary>
        /// <param name="collisionType">Loại va chạm vừa xảy ra.</param>
        public delegate void BallCollisionHandler(CollisionType collisionType);

        #endregion

        #region Serialized fields

        /// <summary>Ai vừa chạm bóng lần cuối (người chơi hay AI).</summary>
        public PlayerType lastHitby;

        /// <summary>Số lần bóng chạm đất kể từ cú đánh gần nhất. Reset về 0 mỗi lần đánh.</summary>
        public int bounceCount;

        /// <summary>True khi bóng đang trong pha bóng sống (đã rời vợt, chưa kết thúc điểm).</summary>
        public bool IsInPlay;

        /// <summary>True khi bóng đang bị giữ trên tay/vợt người chơi (kinematic, bám theo parent).</summary>
        public bool isBallHeld;

        /// <summary>Rigidbody của bóng.</summary>
        public Rigidbody rb;

        /// <summary>Parent gốc trong hierarchy; bóng được trả về đây khi thả ra.</summary>
        public Transform initialParent;

        /// <summary>Layer mask của mặt sân — dùng để nhận diện chạm đất bổ trợ cho tag.</summary>
        public LayerMask groundLayermask;

        [Header("Shot Visualization")]
        /// <summary>Prefab vẽ đường bay dự đoán.</summary>
        public ShotVisualizer ShotVisualizerPrefab;

        /// <summary>Prefab decal đánh dấu điểm nảy.</summary>
        public BallBounceVisual BallBounceVisualPrefab;

        /// <summary>Prefab marker đặt tại điểm rơi dự đoán (nảy 1 và nảy 2).</summary>
        public GameObject markerPrefab;

        /// <summary>
        /// Prefab bóng mô phỏng dùng trong physics scene phụ.
        /// PHẢI trùng thông số vật lý với quả bóng thật (mass, drag, collider, PhysicMaterial,
        /// <see cref="EnhancedBounce.bounceFactor"/>).
        /// </summary>
        public SimulationBall simulationBallPrefab;

        /// <summary>Renderer của bóng — dùng khi đổi skin bóng từ Shop.</summary>
        public Renderer ballRenderer;

        /// <summary>Vệt đuôi của bóng — dùng khi đổi gradient theo skin.</summary>
        public TrailRenderer trailRenderer;

        /// <summary>
        /// Khoảng thời gian tối thiểu (giây) giữa hai lần ghi nhận chạm đất.
        /// Chống việc một cú nảy sinh nhiều <c>OnCollisionEnter</c> liên tiếp làm
        /// <see cref="bounceCount"/> nhảy vọt và gây thổi phạt oan.
        /// </summary>
        public float intervalSeconds = 0.1f;

        [Header("Tuning - Force magnitude (m/s)")]
        [Tooltip("Cận dưới tốc độ bóng cho cú đánh XA (cuối sân). Chặn dưới để bóng không lết qua lưới.")]
        [SerializeField] private float minForceMagnitude = 4f;

        [Tooltip("Cận trên tốc độ bóng cho cú đánh XA. Chặn trên để lỗi input không tạo cú đánh phi lý.")]
        [SerializeField] private float maxForceMagnitude = 16f;

        [Tooltip("Cận dưới tốc độ bóng cho cú đánh GẦN lưới (dink) — thấp hơn nhiều so với cú xa.")]
        [SerializeField] private float minCloseForceMagnitude = 2.5f;

        [Tooltip("Cận trên tốc độ bóng cho cú đánh GẦN lưới.")]
        [SerializeField] private float maxCloseForceMagnitude = 9f;

        [Header("Tuning - Upward power (m/s cộng thêm vào trục Y)")]
        [Tooltip("Lực nâng thêm tối thiểu cho cú XA, cộng vào vận tốc Y sau khi giải ném xiên. " +
                 "Có nó bóng mới có vòng cung 'đẹp' thay vì bay thẳng căng.")]
        [SerializeField] private float minUpwardPower = 0.5f;

        [Tooltip("Lực nâng thêm tối đa cho cú XA.")]
        [SerializeField] private float maxUpwardPower = 2f;

        [Tooltip("Lực nâng thêm tối thiểu cho cú GẦN lưới — phải nhỏ, dink bay bổng là mất điểm.")]
        [SerializeField] private float minCloseUpwardPower = 0.3f;

        [Tooltip("Lực nâng thêm tối đa cho cú GẦN lưới.")]
        [SerializeField] private float maxCloseUpwardPower = 1.2f;

        [Header("Tuning - Flight time cú đánh XA (giây)")]
        [Tooltip("Thời gian bay ngắn nhất có thể của cú xa (vuốt chậm + swipe ngắn).")]
        [SerializeField] private float minFarFlightTimeLow = 0.8f;

        [Tooltip("Thời gian bay ngắn nhất của cú xa khi swipe dài.")]
        [SerializeField] private float minFarFlightTimeHigh = 1f;

        [Tooltip("Thời gian bay dài nhất của cú xa khi swipe ngắn.")]
        [SerializeField] private float maxFarFlightTimeLow = 1.2f;

        [Tooltip("Thời gian bay dài nhất có thể của cú xa (vuốt nhanh + swipe dài).")]
        [SerializeField] private float maxFarFlightTimeHigh = 1.4f;

        [Header("Tuning - Flight time cú đánh GẦN (giây)")]
        [Tooltip("Thời gian bay ngắn nhất có thể của cú gần lưới.")]
        [SerializeField] private float minCloseFlightTimeLow = 0.5f;

        [Tooltip("Thời gian bay ngắn nhất của cú gần khi swipe dài.")]
        [SerializeField] private float minCloseFlightTimeHigh = 0.62f;

        [Tooltip("Thời gian bay dài nhất của cú gần khi swipe ngắn.")]
        [SerializeField] private float maxCloseFlightTimeLow = 0.76f;

        [Tooltip("Thời gian bay dài nhất có thể của cú gần lưới.")]
        [SerializeField] private float maxCloseFlightTimeHigh = 0.9f;

        [Header("Tuning - Spin & Swing")]
        [Tooltip("Mô-men xoáy tối thiểu khi vuốt gần như thẳng.")]
        [SerializeField] private float minTorqueMagnitude = 2f;

        [Tooltip("Mô-men xoáy tối đa khi vuốt cong hết cỡ với chỉ số spin tối đa.")]
        [SerializeField] private float maxTorqueMagnitude = 12f;

        [Tooltip("Lực bên (Newton) tối thiểu cộng mỗi FixedUpdate trong lúc swing.")]
        [SerializeField] private float minSwingMagnitude = 0.5f;

        [Tooltip("Lực bên (Newton) tối đa cộng mỗi FixedUpdate trong lúc swing. " +
                 "Lớn quá thì bóng bẻ cua phi vật lý.")]
        [SerializeField] private float maxSwingMagnitude = 3f;

        [Tooltip("Số giây đầu tiên của đường bay còn chịu lực bên. Ngắn hơn thời gian bay để " +
                 "bóng vẫn kịp ổn định trước khi chạm đất.")]
        [SerializeField] private float swingDuration = 0.4f;

        [Tooltip("Tốc độ bóng mà dưới đó cú slice bẻ hướng mạnh nhất (bóng chậm, xoáy kịp ăn).")]
        [SerializeField] private float minSliceBallSpeed = 6f;

        [Tooltip("Tốc độ bóng mà trên đó cú slice gần như không bẻ được hướng nữa.")]
        [SerializeField] private float maxSliceBallSpeed = 14f;

        [Tooltip("Độ lệch tối đa (mét) của điểm rơi thực tế so với điểm ngắm, do swing + spin.")]
        [SerializeField] private float maxDeviationDistance = 1.5f;

        [Tooltip("Biên độ điều chỉnh tốc độ bóng theo tốc độ vuốt, tính theo tỉ lệ (0.15 = ±15%).")]
        [SerializeField] private float speedFactorPercentage = 0.15f;

        [Tooltip("Biên độ điều chỉnh thời gian bay theo độ dài vuốt, tính theo tỉ lệ (0.2 = ±20%).")]
        [SerializeField] private float lengthFactorPercentage = 0.2f;

        [Header("Tuning - Kết thúc pha bóng")]
        [Tooltip("Dưới tốc độ này coi như bóng đã đứng yên.")]
        [SerializeField] private float idleSpeedThreshold = 0.35f;

        [Tooltip("Bóng đứng yên liên tục bấy nhiêu giây trong lúc IsInPlay thì tự kết thúc pha bóng.")]
        [SerializeField] private float idleTimeout = 1.5f;

        #endregion

        #region Constants

        private const float MinUpwardForceMultiplier = 0.3f;
        private const float MaxUpwardForceMultiplier = 0.7f;
        private const float MinCloseUpwardForceMultiplier = 0.4f;
        private const float MaxCloseUpwardForceMultiplier = 0.8f;
        private const float MinSwipeSpeed = 3f;
        private const float MaxSwipeSpeed = 7f;
        private const float forceScale = 1f;

        /// <summary>Thời gian bay tối thiểu chấp nhận được, chặn chia cho 0 trong bài toán ném xiên.</summary>
        private const float MinFlightTime = 0.15f;

        /// <summary>Chiều dài mặc định của sân (mét) khi chưa có <see cref="Court"/> để tham chiếu.</summary>
        private const float DefaultCourtLength = 13.4112f;

        #endregion

        #region Events

        /// <summary>Bóng vừa va chạm mặt sân hoặc lưới (sự kiện cục bộ của instance này).</summary>
        public event BallCollisionHandler OnBallCollision;

        /// <summary>
        /// Bóng vừa được đánh đi: (teamID người đánh, điểm rơi đã tính cả độ lệch, thông tin quỹ đạo).
        /// AI và UI lắng nghe sự kiện này để biết trước bóng sẽ đi đâu.
        /// </summary>
        public static event Action<string, Vector3, ShotTrajetoryInfo> OnBallHit;

        /// <summary>
        /// Hướng bóng theo trục ngang vừa đổi: (teamID, hướng cũ, hướng mới).
        /// Dùng cho animation quay người và cho AI chọn bên đón bóng.
        /// </summary>
        public static event Action<string, BallDirection, BallDirection> OnBallDirectionChange;

        #endregion

        #region Properties

        /// <summary>Mô-men xoáy tối đa mà một cú đánh có thể tạo ra (các booster spin đọc giá trị này).</summary>
        public float MaxTorqueMagnitude => maxTorqueMagnitude;

        #endregion

        #region Private state

        private Transform spawnedMarker;
        private Transform spawnedMarker2;
        private ShotVisualizer spawnedShotVisualizer;
        private Coroutine swingCoroutine;

        private BallDirection currentBallDirection = BallDirection.Right;
        private bool hasBallDirection;

        private Vector3 lastValidPosition;
        private float collisionDetectionTimer;
        private float ballIdleTimer;

        #endregion

        #region Unity lifecycle

        protected override void OnAwake()
        {
            base.OnAwake();

            if (rb == null) rb = GetComponent<Rigidbody>();
            if (ballRenderer == null) ballRenderer = GetComponentInChildren<Renderer>();
            if (trailRenderer == null) trailRenderer = GetComponentInChildren<TrailRenderer>();
            if (initialParent == null) initialParent = transform.parent;

            lastValidPosition = transform.position;
        }

        protected override void Start()
        {
            base.Start();

            if (ShotVisualizerPrefab != null && spawnedShotVisualizer == null)
            {
                spawnedShotVisualizer = Instantiate(ShotVisualizerPrefab);
                spawnedShotVisualizer.Hide();
            }

            if (markerPrefab != null)
            {
                if (spawnedMarker == null) spawnedMarker = Instantiate(markerPrefab).transform;
                if (spawnedMarker2 == null) spawnedMarker2 = Instantiate(markerPrefab).transform;
                spawnedMarker.gameObject.SetActive(false);
                spawnedMarker2.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (rb == null) return;

            // --- Chống NaN: một lần Rigidbody dính NaN là toàn bộ physics scene hỏng vĩnh viễn ---
            if (Utilities.IsInvalid(transform.position) || Utilities.IsInvalid(rb.linearVelocity)
                || Utilities.IsInvalid(rb.angularVelocity))
            {
                Debug.LogWarning("[BallController] Phát hiện vị trí/vận tốc NaN — reset bóng về vị trí hợp lệ gần nhất.");
                ResetBall(lastValidPosition);
                return;
            }

            lastValidPosition = transform.position;

            // --- Tự kết thúc pha bóng khi bóng nằm chết trên sân ---
            if (!IsInPlay || isBallHeld || rb.isKinematic)
            {
                ballIdleTimer = 0f;
                return;
            }

            if (rb.linearVelocity.sqrMagnitude <= idleSpeedThreshold * idleSpeedThreshold)
            {
                ballIdleTimer += Time.deltaTime;
                if (ballIdleTimer >= idleTimeout)
                {
                    ballIdleTimer = 0f;
                    IsInPlay = false;
                    HideShotVisuals();
                }
            }
            else
            {
                ballIdleTimer = 0f;
            }
        }

        #endregion

        #region Hold / Unhold

        /// <summary>
        /// Gắn bóng vào tay/vợt: bóng thành kinematic và bám cứng theo <paramref name="holdParent"/>.
        /// </summary>
        /// <param name="holdParent">Transform của điểm giữ bóng (thường là xương tay hoặc đầu vợt).</param>
        public void HoldBall(Transform holdParent)
        {
            if (holdParent == null) return;

            isBallHeld = true;
            IsInPlay = false;
            ballIdleTimer = 0f;

            StopSwing();

            if (rb != null)
            {
                // Unity 6 cảnh báo nếu gán vận tốc cho rigidbody đang kinematic,
                // nên phải zero TRƯỚC khi bật kinematic và bỏ qua nếu đã kinematic sẵn.
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            transform.SetParent(holdParent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            HideShotVisuals();
        }

        /// <summary>Thả bóng khỏi tay: trả về parent gốc và bật lại vật lý.</summary>
        public void UnholdBall()
        {
            isBallHeld = false;

            transform.SetParent(initialParent, true);

            if (rb == null) return;

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        #endregion

        #region Hitting - player

        /// <summary>
        /// Cú đánh của NGƯỜI CHƠI, chuyển tiếp sang <see cref="HitBallServer"/>.
        /// Giữ lại để khớp chữ ký bản gốc (bản gốc là <c>[Command]</c> gửi lên server).
        /// </summary>
        /// <param name="teamID">TeamID của người đánh.</param>
        /// <param name="swipeData">Dữ liệu cú vuốt đã phân tích.</param>
        /// <param name="destination">Điểm ngắm trên sân.</param>
        /// <param name="playerDepth">Toạ độ Z của người đánh (khoảng cách có dấu tới lưới).</param>
        /// <param name="swingAbility">Chỉ số swing của nhân vật, chuẩn hoá [0..1].</param>
        /// <param name="spinAbility">Chỉ số spin của nhân vật, chuẩn hoá [0..1].</param>
        /// <param name="shotPowerFactor">Hệ số nhân lực từ booster/thể lực (1 = bình thường).</param>
        public void HitBall(string teamID, SwipeData swipeData, Vector3 destination, float playerDepth,
            float swingAbility, float spinAbility, float shotPowerFactor)
        {
            HitBallServer(teamID, swipeData, destination, playerDepth, swingAbility, spinAbility, shotPowerFactor);
        }

        /// <summary>
        /// Cú đánh của NGƯỜI CHƠI — biến cú vuốt thành vận tốc, xoáy và lực bên, đồng thời
        /// mô phỏng trước toàn bộ quỹ đạo.
        ///
        /// <para><b>Thuật toán</b>:</para>
        /// <list type="number">
        /// <item><description>Chuẩn hoá tốc độ vuốt về [0..1] giữa <c>MinSwipeSpeed</c> và <c>MaxSwipeSpeed</c>.</description></item>
        /// <item><description>Phân loại cú đánh gần/xa theo <c>|playerDepth| &lt; kitchenDepth * 1.5</c>
        /// rồi chọn bộ hằng tương ứng.</description></item>
        /// <item><description>Nội suy thời gian bay theo tốc độ vuốt, rồi nhiễu theo độ dài vuốt.</description></item>
        /// <item><description>Nội suy hệ số nâng, giải bài toán ném xiên tới điểm rơi ĐÃ LỆCH vì xoáy.</description></item>
        /// <item><description>Nhân hệ số lực (booster), kẹp tốc độ trong khoảng an toàn.</description></item>
        /// <item><description>Mô phỏng trước quỹ đạo → <see cref="ShotTrajetoryInfo"/>.</description></item>
        /// <item><description>Áp vận tốc/xoáy thật, chạy coroutine lực bên, phát sự kiện.</description></item>
        /// </list>
        /// </summary>
        /// <param name="teamID">TeamID của người đánh.</param>
        /// <param name="swipeData">Dữ liệu cú vuốt đã phân tích (không được null).</param>
        /// <param name="destination">Điểm ngắm trên sân (world space, y ≈ 0).</param>
        /// <param name="playerDepth">Toạ độ Z của người đánh — dùng để phân biệt cú gần lưới với cú cuối sân.</param>
        /// <param name="swingAbility">Chỉ số swing của nhân vật [0..1].</param>
        /// <param name="spinAbility">Chỉ số spin của nhân vật [0..1].</param>
        /// <param name="shotPowerFactor">Hệ số nhân lực từ booster/thể lực.</param>
        public void HitBallServer(string teamID, SwipeData swipeData, Vector3 destination, float playerDepth,
            float swingAbility, float spinAbility, float shotPowerFactor) // [Server]
        {
            if (!IsServer || rb == null) return;
            if (swipeData == null)
            {
                Debug.LogWarning("[BallController] HitBallServer nhận swipeData null — bỏ qua cú đánh.");
                return;
            }

            Court court = GameManager.HasInstance ? GameManager.Instance.court : null;

            // 1. Tốc độ vuốt chuẩn hoá.
            float normalizedSpeed = Mathf.Clamp01(Mathf.InverseLerp(MinSwipeSpeed, MaxSwipeSpeed, swipeData.Velocity));

            // 2. Cú đánh sát lưới (dink) hay cú đánh xa?
            float kitchenDepth = court != null ? court.kitchenDepth : 2.1336f;
            bool isCloseShot = Mathf.Abs(playerDepth) < kitchenDepth * 1.5f;

            // 3. Thời gian bay: nội suy theo tốc độ vuốt rồi nhiễu theo độ dài đường vuốt.
            float flightTimeLow = isCloseShot ? minCloseFlightTimeLow : minFarFlightTimeLow;
            float flightTimeHigh = isCloseShot ? maxCloseFlightTimeHigh : maxFarFlightTimeHigh;
            float flightTime = Mathf.Lerp(flightTimeLow, flightTimeHigh, normalizedSpeed);
            flightTime = ApplyLengthNoise(flightTime, swipeData.length, court);

            // 4. Hệ số nâng theo tốc độ vuốt.
            float upwardMultiplier = isCloseShot
                ? Mathf.Lerp(MinCloseUpwardForceMultiplier, MaxCloseUpwardForceMultiplier, normalizedSpeed)
                : Mathf.Lerp(MinUpwardForceMultiplier, MaxUpwardForceMultiplier, normalizedSpeed);

            float swingIntensity = Mathf.Clamp01(swingAbility * swipeData.CurveIntensity);
            float spinIntensity = Mathf.Clamp01(spinAbility * swipeData.CurveIntensity);

            // 5. Điểm rơi thực tế lệch khỏi điểm ngắm vì xoáy.
            Vector3 deviatedDestination =
                CalculateDeviatedDestination(destination, swipeData.CurveDirection, swingIntensity, spinIntensity);

            // 6. Giải ném xiên rồi áp hệ số lực.
            Vector3 velocity = Utilities.SolveBallisticVelocity(
                transform.position, deviatedDestination, flightTime, Physics.gravity.y);
            velocity *= Mathf.Max(0.01f, shotPowerFactor) * forceScale;
            velocity *= 1f + (normalizedSpeed - 0.5f) * 2f * speedFactorPercentage;
            velocity.y += Mathf.Lerp(
                isCloseShot ? minCloseUpwardPower : minUpwardPower,
                isCloseShot ? maxCloseUpwardPower : maxUpwardPower,
                upwardMultiplier);
            velocity = ClampShotSpeed(velocity, isCloseShot);

            // 7. Mô-men xoáy.
            float torqueMagnitude = Mathf.Lerp(minTorqueMagnitude, maxTorqueMagnitude,
                Mathf.Clamp01(spinAbility * swipeData.CurveIntensity));
            Vector3 torque = CalculateTorque(velocity, swipeData.CurveDirection, torqueMagnitude);

            // Lực bên: cùng một cặp (hướng, độ lớn) được dùng cho CẢ mô phỏng lẫn coroutine thật.
            Vector3 swingDirection = FlattenAndNormalize(swipeData.CurveDirection);
            float swingMagnitude = Mathf.Lerp(minSwingMagnitude, maxSwingMagnitude, swingIntensity);
            float effectiveSwingDuration = Mathf.Min(swingDuration, flightTime);

            ApplyShot(teamID, deviatedDestination, velocity, torque, swingDirection, swingMagnitude,
                effectiveSwingDuration, spinIntensity, PlayerType.Player);
        }

        #endregion

        #region Hitting - AI

        /// <summary>
        /// Cú đánh của AI. AI đã tự chọn sẵn thời gian bay mong muốn (<paramref name="shotTime"/>)
        /// nên không cần suy ra từ cú vuốt — chỉ cần giải bài toán ném xiên.
        /// </summary>
        /// <param name="teamID">TeamID của AI.</param>
        /// <param name="destination">Điểm ngắm trên sân (world space).</param>
        /// <param name="shotTime">Thời gian bay mong muốn tới điểm ngắm (giây).</param>
        /// <param name="swingSpinIntensity">Cường độ xoáy AI muốn tạo [0..1].</param>
        /// <param name="swingAbility">Chỉ số swing của AI [0..1].</param>
        /// <param name="spinAbility">Chỉ số spin của AI [0..1].</param>
        /// <param name="swingspinDirection">Hướng bẻ cong mong muốn (mặt phẳng XZ).</param>
        /// <param name="length">Độ dài "cú vuốt ảo" của AI — dùng nhiễu thời gian bay giống người chơi.</param>
        public void HitBall(string teamID, Vector3 destination, float shotTime, float swingSpinIntensity,
            float swingAbility, float spinAbility, Vector3 swingspinDirection, float length)
        {
            if (!IsServer || rb == null) return;

            Court court = GameManager.HasInstance ? GameManager.Instance.court : null;

            float kitchenDepth = court != null ? court.kitchenDepth : 2.1336f;
            bool isCloseShot = Mathf.Abs(transform.position.z) < kitchenDepth * 1.5f;

            float flightTime = ApplyLengthNoise(shotTime, length, court);

            float swingIntensity = Mathf.Clamp01(swingAbility * swingSpinIntensity);
            float spinIntensity = Mathf.Clamp01(spinAbility * swingSpinIntensity);

            Vector3 deviatedDestination =
                CalculateDeviatedDestination(destination, swingspinDirection, swingIntensity, spinIntensity);

            Vector3 velocity = Utilities.SolveBallisticVelocity(
                transform.position, deviatedDestination, flightTime, Physics.gravity.y);
            velocity *= forceScale;
            velocity = ClampShotSpeed(velocity, isCloseShot);

            float torqueMagnitude = Mathf.Lerp(minTorqueMagnitude, maxTorqueMagnitude,
                Mathf.Clamp01(spinAbility * swingSpinIntensity));
            Vector3 torque = CalculateTorque(velocity, swingspinDirection, torqueMagnitude);

            Vector3 swingDirection = FlattenAndNormalize(swingspinDirection);
            float swingMagnitude = Mathf.Lerp(minSwingMagnitude, maxSwingMagnitude, swingIntensity);
            float effectiveSwingDuration = Mathf.Min(swingDuration, flightTime);

            ApplyShot(teamID, deviatedDestination, velocity, torque, swingDirection, swingMagnitude,
                effectiveSwingDuration, spinIntensity, PlayerType.AI);
        }

        #endregion

        #region Shot core

        /// <summary>
        /// Bước cuối dùng chung cho cả cú đánh người chơi lẫn AI: mô phỏng trước quỹ đạo,
        /// áp vận tốc/xoáy thật, chạy lực bên, phát sự kiện và báo cho rule engine.
        /// </summary>
        private void ApplyShot(string teamID, Vector3 destination, Vector3 velocity, Vector3 torque,
            Vector3 swingDirection, float swingMagnitude, float effectiveSwingDuration, float spinIntensity,
            PlayerType hitter)
        {
            if (Utilities.IsInvalid(velocity))
            {
                Debug.LogWarning("[BallController] Vận tốc tính ra không hợp lệ — bỏ qua cú đánh.");
                return;
            }

            // Bóng phải rời tay trước khi bay.
            if (isBallHeld) UnholdBall();

            ShotTrajetoryInfo shotInfo = SimulateShot(velocity, torque, swingDirection, swingMagnitude,
                effectiveSwingDuration, spinIntensity);

            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
            rb.AddTorque(torque);

            StopSwing();
            // curveIntensity = swingMagnitude * spinIntensity — PHẢI khớp với công thức
            // swingFactor * swingSpinIntensity mà PhysicsTrajectoryHandler dùng, nếu không dự đoán sẽ lệch.
            if (swingDirection.sqrMagnitude > Mathf.Epsilon && spinIntensity > 0f && effectiveSwingDuration > 0f)
            {
                swingCoroutine = StartCoroutine(
                    ApplySwingEffect(swingDirection, swingMagnitude * spinIntensity, effectiveSwingDuration));
            }

            lastHitby = hitter;
            IsInPlay = true;
            bounceCount = 0;
            ballIdleTimer = 0f;
            collisionDetectionTimer = 0f;

            ShowShotVisuals(shotInfo);

            OnBallHit?.Invoke(teamID, destination, shotInfo);

            if (GameManager.HasInstance) GameManager.Instance.OnBallHit(teamID);
        }

        /// <summary>
        /// Mô phỏng trước quỹ đạo trong physics scene phụ và gói thành <see cref="ShotTrajetoryInfo"/>.
        /// Trả về info rỗng (không null) nếu hệ dự đoán chưa sẵn sàng.
        /// </summary>
        private ShotTrajetoryInfo SimulateShot(Vector3 velocity, Vector3 torque, Vector3 swingDirection,
            float swingMagnitude, float effectiveSwingDuration, float spinIntensity)
        {
            if (simulationBallPrefab == null
                || !PhysicsTrajectoryHandler.HasInstance
                || !PhysicsTrajectoryHandler.Instance.IsReady)
            {
                return new ShotTrajetoryInfo();
            }

            PhysicsTrajectoryHandler.Instance.SimulateTrajectory(
                out List<TrajectoryPoint> firstBouncePoints,
                out List<TrajectoryPoint> secondBouncePoints,
                simulationBallPrefab,
                transform.position,
                transform.rotation,
                velocity,
                Vector3.zero,
                torque,
                swingDirection,
                swingMagnitude,
                effectiveSwingDuration,
                spinIntensity);

            return new ShotTrajetoryInfo(
                new TrajectoryData(firstBouncePoints),
                new TrajectoryData(secondBouncePoints));
        }

        /// <summary>
        /// Cộng lực bên mỗi bước vật lý để bóng bẻ cong trong không khí (hiệu ứng Magnus giả lập).
        /// </summary>
        /// <param name="curveDirection">Hướng lực bên (đã chuẩn hoá, mặt phẳng XZ).</param>
        /// <param name="curveIntensity">Độ lớn lực bên (Newton) cộng mỗi FixedUpdate.</param>
        /// <param name="swingDuration">Tổng thời gian (giây) còn cộng lực.</param>
        private IEnumerator ApplySwingEffect(Vector3 curveDirection, float curveIntensity, float swingDuration)
        {
            float elapsedTime = 0f;
            Vector3 swingForce = curveDirection * curveIntensity;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            while (elapsedTime < swingDuration)
            {
                if (rb == null || rb.isKinematic) yield break;

                rb.AddForce(swingForce, ForceMode.Force);
                elapsedTime += Time.fixedDeltaTime;
                yield return wait;
            }

            swingCoroutine = null;
        }

        private void StopSwing()
        {
            if (swingCoroutine == null) return;

            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        /// <summary>
        /// Độ lệch (mét) mà cú slice tạo ra. Xoáy chỉ "ăn" khi bóng đủ chậm: bóng càng nhanh,
        /// thời gian chịu lực bên trên mỗi mét đường bay càng ngắn nên bẻ được càng ít.
        /// </summary>
        /// <param name="swingAmount">Cường độ swing đã chuẩn hoá [0..1].</param>
        /// <param name="spinAmount">Cường độ spin đã chuẩn hoá [0..1].</param>
        private float CalculateDeflectionAfterSlice(float swingAmount, float spinAmount)
        {
            float slice = Mathf.Clamp01(swingAmount) * Mathf.Clamp01(spinAmount);
            if (slice <= 0f) return 0f;

            float speedDamp = 1f - Mathf.Clamp01(
                Mathf.InverseLerp(minSliceBallSpeed, maxSliceBallSpeed, GetVelocity())) * 0.5f;

            return maxDeviationDistance * slice * speedDamp;
        }

        /// <summary>
        /// Dịch điểm ngắm sang bên theo hướng xoáy, tối đa <see cref="maxDeviationDistance"/> mét,
        /// rồi kẹp lại trong biên sân để cú đánh không tự sát vì xoáy.
        /// </summary>
        /// <param name="originalDestination">Điểm ngắm gốc do người chơi/AI chọn.</param>
        /// <param name="curveDirection">Hướng bẻ cong (mặt phẳng XZ); zero = không lệch.</param>
        /// <param name="swingIntensity">Cường độ swing đã chuẩn hoá [0..1].</param>
        /// <param name="spinIntensity">Cường độ spin đã chuẩn hoá [0..1].</param>
        private Vector3 CalculateDeviatedDestination(Vector3 originalDestination, Vector3 curveDirection,
            float swingIntensity, float spinIntensity)
        {
            Vector3 direction = FlattenAndNormalize(curveDirection);
            if (direction.sqrMagnitude <= Mathf.Epsilon) return originalDestination;

            float deflection = Mathf.Min(
                CalculateDeflectionAfterSlice(swingIntensity, spinIntensity), maxDeviationDistance);
            if (deflection <= 0f) return originalDestination;

            Vector3 deviated = originalDestination + direction * deflection;

            Court court = GameManager.HasInstance ? GameManager.Instance.court : null;
            if (court != null)
            {
                deviated = Utilities.ClampToBounds(deviated, Vector3.zero,
                    new Vector2(court.HalfWidth, court.HalfLength));
            }

            deviated.y = originalDestination.y;
            return deviated;
        }

        /// <summary>
        /// Trục xoáy: luôn có thành phần topspin (vuông góc hướng bay, nằm ngang) cộng thêm
        /// sidespin quanh trục đứng theo chiều vuốt cong.
        /// </summary>
        private Vector3 CalculateTorque(Vector3 velocity, Vector3 curveDirection, float torqueMagnitude)
        {
            Vector3 flatVelocity = FlattenAndNormalize(velocity);
            if (flatVelocity.sqrMagnitude <= Mathf.Epsilon) return Vector3.zero;

            Vector3 topSpinAxis = Vector3.Cross(Vector3.up, flatVelocity);

            Vector3 flatCurve = FlattenAndNormalize(curveDirection);
            float side = flatCurve.sqrMagnitude <= Mathf.Epsilon
                ? 0f
                : Mathf.Sign(Vector3.Dot(Vector3.Cross(flatVelocity, flatCurve), Vector3.up));

            Vector3 axis = topSpinAxis + Vector3.up * side;
            if (axis.sqrMagnitude <= Mathf.Epsilon) return Vector3.zero;

            return axis.normalized * torqueMagnitude;
        }

        /// <summary>
        /// Nhiễu thời gian bay theo độ dài cú vuốt: vuốt dài hơn nửa chiều dài sân thì kéo dài
        /// thời gian bay, vuốt ngắn thì rút lại — biên độ đúng bằng <see cref="lengthFactorPercentage"/>.
        /// </summary>
        private float ApplyLengthNoise(float flightTime, float swipeLength, Court court)
        {
            float courtLength = court != null ? court.HalfLength * 2f : DefaultCourtLength;
            if (courtLength <= Mathf.Epsilon) courtLength = DefaultCourtLength;

            float normalizedLength = Mathf.Clamp01(swipeLength / courtLength);
            float adjusted = flightTime * (1f + (normalizedLength - 0.5f) * 2f * lengthFactorPercentage);

            return Mathf.Max(MinFlightTime, adjusted);
        }

        /// <summary>Kẹp tốc độ bóng vào khoảng an toàn của loại cú đánh, giữ nguyên hướng bay.</summary>
        private Vector3 ClampShotSpeed(Vector3 velocity, bool isCloseShot)
        {
            float minMagnitude = isCloseShot ? minCloseForceMagnitude : minForceMagnitude;
            float maxMagnitude = isCloseShot ? maxCloseForceMagnitude : maxForceMagnitude;

            float speed = velocity.magnitude;
            if (speed <= Mathf.Epsilon) return velocity;

            float clamped = Mathf.Clamp(speed, minMagnitude, maxMagnitude);
            return velocity * (clamped / speed);
        }

        private static Vector3 FlattenAndNormalize(Vector3 v)
        {
            Vector3 flat = new Vector3(v.x, 0f, v.z);
            return flat.sqrMagnitude <= Mathf.Epsilon ? Vector3.zero : flat.normalized;
        }

        #endregion

        #region Direction

        /// <summary>
        /// Xác định bóng đang đi sang trái hay phải và phát <see cref="OnBallDirectionChange"/>
        /// khi hướng đổi. Animation quay người và AI dựa vào sự kiện này.
        /// </summary>
        /// <param name="teamID">TeamID của người vừa đánh.</param>
        /// <param name="swipeData">Cú vuốt tương ứng (có thể null khi gọi từ AI).</param>
        /// <param name="direction">Vector hướng bóng trong world space.</param>
        public void DetermineBallDirection(string teamID, SwipeData swipeData, Vector3 direction)
        {
            Vector3 reference = direction.sqrMagnitude > Mathf.Epsilon
                ? direction
                : (swipeData != null ? swipeData.Direction : Vector3.zero);

            if (reference.sqrMagnitude <= Mathf.Epsilon) return;

            BallDirection newDirection = reference.x >= 0f ? BallDirection.Right : BallDirection.Left;

            if (hasBallDirection && newDirection == currentBallDirection) return;

            BallDirection previous = hasBallDirection ? currentBallDirection : newDirection;
            currentBallDirection = newDirection;
            hasBallDirection = true;

            OnBallDirectionChange?.Invoke(teamID, previous, newDirection);
        }

        #endregion

        #region Collision

        // [ServerCallback] — Phase 2 chỉ server chạy khối này rồi phát RpcOnBallCollision về client.
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;

            GameObject other = collision.gameObject;
            Vector3 contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            bool isGround = other.CompareTag(StringConstants.GroundTag)
                            || (groundLayermask.value & (1 << other.layer)) != 0;

            if (isGround)
            {
                // Chống đếm trùng khi một cú nảy sinh nhiều contact event liên tiếp.
                if (Time.time - collisionDetectionTimer < intervalSeconds) return;
                collisionDetectionTimer = Time.time;

                bounceCount++;
                OnBallCollision?.Invoke(CollisionType.Ground);

                SpawnBounceVisual(contactPoint);

                if (GameManager.HasInstance)
                {
                    GameManager.Instance.OnBallBounced(contactPoint, contactPoint.z > 0f);
                }

                return;
            }

            if (!other.CompareTag(StringConstants.NetTag)) return;

            OnBallCollision?.Invoke(CollisionType.Net);

            // PickleNet cũng báo cho GameManager; GameManager tự bỏ qua lời gọi thứ hai
            // vì trạng thái đã chuyển sang PointScored.
            if (GameManager.HasInstance) GameManager.Instance.OnBallHitNet();
        }

        private void SpawnBounceVisual(Vector3 contactPoint)
        {
            if (BallBounceVisualPrefab == null) return;

            BallBounceVisual visual = Instantiate(BallBounceVisualPrefab);
            // Cú nảy càng mạnh, vệt càng to.
            float scale = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(GetVelocity() / Mathf.Max(1f, maxForceMagnitude)));
            visual.Play(contactPoint, scale);
        }

        #endregion

        #region Visuals & state

        private void ShowShotVisuals(ShotTrajetoryInfo shotInfo)
        {
            if (shotInfo == null) return;

            if (spawnedShotVisualizer != null) spawnedShotVisualizer.Show(shotInfo.NormaltrajectoryData);

            PlaceMarker(spawnedMarker, shotInfo.NormaltrajectoryData);
            PlaceMarker(spawnedMarker2, shotInfo.SecondBounceTrajectory);
        }

        private static void PlaceMarker(Transform marker, TrajectoryData data)
        {
            if (marker == null) return;

            if (data == null || data.Count == 0)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            marker.position = data.Points[data.Count - 1];
            marker.gameObject.SetActive(true);
        }

        private void HideShotVisuals()
        {
            if (spawnedShotVisualizer != null) spawnedShotVisualizer.Hide();
            if (spawnedMarker != null) spawnedMarker.gameObject.SetActive(false);
            if (spawnedMarker2 != null) spawnedMarker2.gameObject.SetActive(false);
        }

        /// <summary>Tốc độ hiện tại của bóng (m/s); 0 nếu chưa có Rigidbody.</summary>
        internal float GetVelocity()
        {
            if (rb == null) return 0f;

            Vector3 v = rb.linearVelocity;
            return Utilities.IsInvalid(v) ? 0f : v.magnitude;
        }

        /// <summary>
        /// Đưa bóng về một vị trí sạch: dừng hẳn vật lý, xoá xoáy, xoá trail và reset trạng thái pha bóng.
        /// </summary>
        /// <param name="position">Vị trí đặt lại bóng (world space).</param>
        public void ResetBall(Vector3 position)
        {
            StopSwing();

            isBallHeld = false;
            IsInPlay = false;
            bounceCount = 0;
            ballIdleTimer = 0f;
            collisionDetectionTimer = 0f;
            hasBallDirection = false;

            transform.SetParent(initialParent, true);

            if (Utilities.IsInvalid(position)) position = Vector3.zero;

            if (rb != null)
            {
                // Zero vận tốc TRƯỚC khi bật kinematic — thứ tự ngược lại sẽ sinh warning
                // "Setting linear velocity of a kinematic body is not supported" trên Unity 6.
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            transform.position = position;
            transform.rotation = Quaternion.identity;
            lastValidPosition = position;

            if (rb != null) rb.isKinematic = false;
            if (trailRenderer != null) trailRenderer.Clear();

            HideShotVisuals();
        }

        /// <summary>Đổi skin bóng theo item đang trang bị.</summary>
        /// <param name="ballIndex">Chỉ số item bóng trong shop data.</param>
        public void ApplyEquipedBall(int ballIndex)
        {
            // TODO P10: lấy material/màu từ Shop
        }

        /// <summary>Đổi gradient của vệt đuôi bóng theo item đang trang bị.</summary>
        /// <param name="ballIndex">Chỉ số item bóng trong shop data.</param>
        public void ApplyTrailGradient(int ballIndex)
        {
            // TODO P10: lấy material/màu từ Shop
        }

        #endregion
    }
}
