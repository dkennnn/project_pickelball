using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái giao bóng của AI — phần "diễn xuất" quan trọng nhất để AI trông giống người thật.
    ///
    /// <para><b>Trình tự:</b></para>
    /// <list type="number">
    /// <item><description><see cref="Enter"/>: cầm bóng, bật chỉ dẫn ô giao, rút số lần nhích chân bằng
    /// <see cref="DetermineMovementCount"/> (theo ba xác suất trên Inspector).</description></item>
    /// <item><description><see cref="PerformRandomServeMovement"/>: nhích ngang trong ô giao bằng
    /// <see cref="Tweener.Move"/>, lặp lại đủ số lần đã rút.</description></item>
    /// <item><description>Chờ ngẫu nhiên <c>minServeDelay..maxServeDelay</c> giây bằng
    /// <see cref="DelayedAction.Run"/> rồi <see cref="ServeBall"/>.</description></item>
    /// <item><description><see cref="OnThrowBall"/> (tung bóng) và <see cref="OnShotHit"/> (vợt chạm bóng)
    /// nhận animation event; nếu rig chưa có event thì đồng hồ dự phòng vẫn bảo đảm bóng được giao —
    /// đúng cơ chế của <see cref="ServeState"/> bên người chơi.</description></item>
    /// </list>
    /// </summary>
    public class AIServeState : IPlayerState
    {
        private readonly PickleballAIController aiController;

        private float currentShotTime;
        private int movementCount;
        private int currentMovementIndex;

        private bool hasThrownBall;
        private bool hasHitBall;
        private bool isServing;
        private Vector3 pendingDestination;

        private Coroutine movementRoutine;
        private Coroutine movementDelayRoutine;
        private Coroutine serveDelayRoutine;
        private Coroutine throwFallbackRoutine;

        /// <summary>Thời lượng một lần nhích chân (giây).</summary>
        private const float MovementDuration = 0.35f;

        /// <summary>Khoảng nghỉ giữa hai lần nhích chân (giây).</summary>
        private const float MovementPause = 0.2f;

        /// <summary>Độ trễ dự phòng từ lúc bắt đầu animation tới khung tung bóng (giây).</summary>
        private const float FallbackThrowDelay = 0.2f;

        /// <summary>Đệm (mét) để điểm giao không rơi sát vạch ô giao.</summary>
        private const float ServeBoxMargin = 0.35f;

        /// <summary>Tạo trạng thái giao bóng cho một AI.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public AIServeState(PickleballAIController controller)
        {
            aiController = controller;
        }

        /// <summary>Cầm bóng, rút số lần nhích chân rồi bắt đầu chuỗi chuẩn bị giao.</summary>
        public void Enter()
        {
            if (aiController == null) return;

            hasThrownBall = false;
            hasHitBall = false;
            isServing = false;
            currentMovementIndex = 0;
            pendingDestination = Vector3.zero;

            aiController.SetAttemptingVolley(false);
            aiController.HoldBall();
            aiController.ShowServeIndicator();

            if (aiController.animationController != null) aiController.animationController.PlayServe();

            aiController.AnimationThrowBall += OnThrowBall;
            aiController.AnimationShotHit += OnShotHit;

            movementCount = DetermineMovementCount();

            if (movementCount > 0) PerformRandomServeMovement();
            else ScheduleServe();
        }

        /// <summary>Huỷ đăng ký animation event, dừng mọi tween/hẹn giờ còn treo và tắt chỉ dẫn ô giao.</summary>
        public void Exit()
        {
            if (aiController == null) return;

            aiController.AnimationThrowBall -= OnThrowBall;
            aiController.AnimationShotHit -= OnShotHit;

            if (movementRoutine != null)
            {
                aiController.StopCoroutine(movementRoutine);
                movementRoutine = null;
            }

            DelayedAction.Cancel(movementDelayRoutine);
            DelayedAction.Cancel(serveDelayRoutine);
            DelayedAction.Cancel(throwFallbackRoutine);

            movementDelayRoutine = null;
            serveDelayRoutine = null;
            throwFallbackRoutine = null;

            aiController.HideServeIndicator();
        }

        /// <summary>Mọi thứ chạy theo tween và hẹn giờ nên không cần xử lý theo frame.</summary>
        public void Update()
        {
        }

        // ------------------------------------------------------------------
        // Nhích chân trước khi giao
        // ------------------------------------------------------------------

        /// <summary>
        /// Rút số lần AI nhích chân (0, 1 hoặc 2) theo ba xác suất trên Inspector.
        /// Ba xác suất được chuẩn hoá lại nên không cần tổng đúng bằng 1.
        /// </summary>
        private int DetermineMovementCount()
        {
            float p0 = Mathf.Max(0f, aiController.serveMovement0Probability);
            float p1 = Mathf.Max(0f, aiController.serveMovement1Probability);
            float p2 = Mathf.Max(0f, aiController.serveMovement2Probability);

            float total = p0 + p1 + p2;
            if (total <= 0f) return 0;

            float roll = Random.value * total;

            if (roll < p0) return 0;
            if (roll < p0 + p1) return 1;
            return 2;
        }

        /// <summary>
        /// Nhích ngang một lần trong phạm vi ô giao: biên độ lấy từ <c>serveMovementRange</c>
        /// (tính theo % chiều rộng ô giao), hướng ngẫu nhiên, có kẹp lại trong ô giao.
        /// </summary>
        private void PerformRandomServeMovement()
        {
            movementDelayRoutine = null;

            if (aiController == null || !aiController.isActiveAndEnabled)
            {
                ScheduleServe();
                return;
            }

            Court court = aiController.MatchCourt;
            Vector3 current = aiController.transform.position;

            float boxWidth = court != null
                ? court.GetServeBoxBounds(aiController.IsPositiveCourt, aiController.currentServeSide).Size.x
                : aiController.courtWidth * 0.5f;

            Vector2 range = aiController.serveMovementRange;
            float ratio = Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
            float offset = boxWidth * Mathf.Abs(ratio) * (Random.value < 0.5f ? -1f : 1f);

            float targetX = current.x + offset;

            if (court != null)
            {
                Vector2 clamp = court.GetServeHorizontalClamp(aiController.IsPositiveCourt,
                                                              aiController.currentServeSide);
                targetX = Mathf.Clamp(targetX, clamp.x + ServeBoxMargin, clamp.y - ServeBoxMargin);
            }

            Vector3 destination = new Vector3(targetX, 0f, current.z);

            // Đặt luôn làm đích di chuyển để ApplyMovement của lớp cha không kéo ngược AI về chỗ cũ;
            // GetTargetPosition trả về giá trị đã được kẹp trong vùng hợp lệ.
            aiController.SetTargetPosition(destination);
            destination = aiController.GetTargetPosition();

            if (movementRoutine != null) aiController.StopCoroutine(movementRoutine);

            movementRoutine = aiController.StartCoroutine(
                Tweener.Move(aiController.transform, destination, MovementDuration, OnServeMovementComplete));
        }

        /// <summary>Nhích xong một lần: còn lượt thì nhích tiếp, hết lượt thì hẹn giờ giao bóng.</summary>
        private void OnServeMovementComplete()
        {
            movementRoutine = null;
            currentMovementIndex++;

            if (currentMovementIndex < movementCount)
            {
                movementDelayRoutine = DelayedAction.Run(MovementPause, PerformRandomServeMovement);
                return;
            }

            ScheduleServe();
        }

        /// <summary>Hẹn giờ ngẫu nhiên trong khoảng <c>minServeDelay..maxServeDelay</c> rồi giao bóng.</summary>
        private void ScheduleServe()
        {
            float min = Mathf.Max(0f, aiController.minServeDelay);
            float max = Mathf.Max(min, aiController.maxServeDelay);

            DelayedAction.Cancel(serveDelayRoutine);
            serveDelayRoutine = DelayedAction.Run(Random.Range(min, max), ServeBall);
        }

        // ------------------------------------------------------------------
        // Cú giao
        // ------------------------------------------------------------------

        /// <summary>Chốt điểm rơi, thời gian bay rồi chạy animation vung vợt (kèm đồng hồ dự phòng).</summary>
        private void ServeBall()
        {
            serveDelayRoutine = null;

            if (aiController == null || !aiController.isActiveAndEnabled) return;
            if (isServing || hasHitBall) return;
            if (aiController.currentStateEnum != PlayerState.Serve) return;

            isServing = true;

            pendingDestination = DetermineDestination(aiController.currentServeSide);
            currentShotTime = aiController.CalculateShotTime(pendingDestination, ShotType.Serve);

            float shotPower = aiController.profile != null ? aiController.profile.shotPower : 0.5f;

            throwFallbackRoutine = DelayedAction.Run(FallbackThrowDelay, OnThrowBall);
            aiController.HitBallAnimation(shotPower, OnShotHit);
        }

        /// <summary>Animation event: bóng rời tay người giao.</summary>
        private void OnThrowBall()
        {
            throwFallbackRoutine = null;

            if (aiController == null || hasThrownBall || !isServing) return;

            hasThrownBall = true;
            aiController.UnholdBall();
        }

        /// <summary>Animation event: vợt chạm bóng — bắn quả giao đi.</summary>
        private void OnShotHit()
        {
            if (aiController == null || hasHitBall || !isServing) return;

            hasHitBall = true;

            OnThrowBall();

            float intensity = aiController.ShouldApplySlice()
                ? Mathf.Clamp01(aiController.baseShotEffect * Random.Range(0.5f, 1f))
                : 0f;

            Vector3 swingSpinDirection = intensity > 0f
                ? aiController.DetermineSwingSpinDirection(
                    aiController.CalculateInitialVelocity(pendingDestination, currentShotTime))
                : Vector3.zero;

            float length = Vector2.Distance(
                new Vector2(aiController.transform.position.x, aiController.transform.position.z),
                new Vector2(pendingDestination.x, pendingDestination.z));

            HitBall(pendingDestination, currentShotTime, intensity, swingSpinDirection, length);
        }

        /// <summary>Bắn quả giao và báo cho controller là cú giao đã xong.</summary>
        /// <param name="destination">Điểm rơi trong ô giao chéo sân.</param>
        /// <param name="shotTime">Thời gian bay mong muốn (giây).</param>
        /// <param name="swingSpinIntensity">Cường độ xoáy/vung của cú giao.</param>
        /// <param name="swingSpinDirection">Hướng bẻ cong trên mặt phẳng XZ.</param>
        /// <param name="length">Độ dài "cú vuốt ảo" — dùng nhiễu thời gian bay giống người chơi.</param>
        private void HitBall(Vector3 destination, float shotTime, float swingSpinIntensity,
                             Vector3 swingSpinDirection, float length)
        {
            PlayerProfileProperties profile = aiController.profile;

            float swingAbility = profile != null ? profile.swingAbility : 0f;
            float spinAbility = profile != null ? profile.spinAbility : 0f;

            aiController.HitBall(aiController.teamID, destination, shotTime, swingSpinIntensity, swingAbility,
                                 spinAbility, swingSpinDirection, length);

            aiController.OnServe();
        }

        /// <summary>
        /// Chọn điểm rơi trong Ô GIAO CHÉO SÂN: cùng <see cref="ServeSide"/> nhưng ở nửa sân đối diện
        /// (xem quy ước tại <see cref="Court.GetServeBoxBounds"/>). AI có lực đánh cao sẽ nhắm sâu hơn.
        /// </summary>
        /// <param name="serveSide">Ô giao mà AI đang đứng.</param>
        private Vector3 DetermineDestination(ServeSide serveSide)
        {
            Court court = aiController.MatchCourt;
            float receiverSign = aiController.IsPositiveCourt ? -1f : 1f;

            if (court == null)
            {
                return new Vector3(0f, 0f, receiverSign * aiController.courtDepth * 0.65f);
            }

            CourtBounds box = court.GetServeBoxBounds(!aiController.IsPositiveCourt, serveSide);

            float minX = box.MinX + ServeBoxMargin;
            float maxX = box.MaxX - ServeBoxMargin;
            if (minX > maxX) minX = maxX = box.center.x;

            float x = Random.Range(minX, maxX);

            // Lực càng mạnh, quả giao càng cắm sâu về phía baseline của bên nhận.
            float power = aiController.profile != null ? Mathf.Clamp01(aiController.profile.shotPower) : 0.5f;
            float nearZ = Mathf.Abs(box.MinZ) < Mathf.Abs(box.MaxZ) ? box.MinZ : box.MaxZ;
            float farZ = Mathf.Abs(box.MinZ) < Mathf.Abs(box.MaxZ) ? box.MaxZ : box.MinZ;

            nearZ += receiverSign * ServeBoxMargin;
            farZ -= receiverSign * ServeBoxMargin;

            float z = Mathf.Lerp(nearZ, farZ, Random.Range(power * 0.4f, Mathf.Max(power * 0.4f, power)));

            return new Vector3(x, 0f, z);
        }
    }
}
