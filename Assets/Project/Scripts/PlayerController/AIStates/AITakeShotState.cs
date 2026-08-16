using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đánh bóng của AI.
    ///
    /// <para><b>Trình tự:</b></para>
    /// <list type="number">
    /// <item><description><see cref="Enter"/>: ghi nhớ tư thế vợt, phát animation giơ vợt và bắt đầu
    /// xoay vợt về điểm đón bóng (không có slow-motion — đó là đặc quyền của người chơi).</description></item>
    /// <item><description><see cref="Update"/>: vợt khớp hướng thì <see cref="TakeShot"/>; quá lâu không
    /// khớp thì bỏ pha đánh và về <see cref="PlayerState.Idle"/>.</description></item>
    /// <item><description><see cref="TakeShot"/>: hỏi chiến thuật điểm rơi + loại cú đánh, tính thời gian
    /// bay, ÁP SAI SỐ theo chỉ số rồi bắn bóng đúng khoảnh khắc vợt chạm bóng.</description></item>
    /// </list>
    ///
    /// <para><b>Cách AI yếu thua</b>: hai lớp sai số cộng dồn — <c>maxShotError</c> làm điểm rơi lệch
    /// ngẫu nhiên trong bán kính vài mét, còn <c>ProbablityToTakeOutShots</c> khiến AI CỐ TÌNH đẩy điểm
    /// rơi ra ngoài vạch biên (bóng out, mất điểm).</para>
    /// </summary>
    public class AITakeShotState : IPlayerState
    {
        private readonly PickleballAIController aiController;

        private Vector3 initialPaddlePosition;
        private bool isPaddleAligned;

        private bool isHitBallComplete;
        private float timer;
        private RacquetAnimator racquetAnimator;

        /// <summary>Thời gian dự phòng coi như vợt đã khớp khi chưa có <see cref="RacquetAnimator"/> (giây).</summary>
        private const float FallbackPaddleAlignTime = 0.12f;

        /// <summary>Thời gian tối đa ở trong pha đánh bóng trước khi bỏ cuộc (giây).</summary>
        private const float MaxShotWindow = 1.5f;

        /// <summary>Bán kính lệch điểm rơi (mét) khi <c>maxShotError</c> bằng 1.</summary>
        private const float MaxShotErrorDistance = 3f;

        /// <summary>
        /// Hệ số hạ nhiệt cho <c>ProbablityToTakeOutShots</c>.
        /// Chỉ số gốc lên tới 0.7 ở bậc thấp nhất; nếu áp nguyên thì 70% cú đánh ra ngoài và pha bóng
        /// không bao giờ kéo dài, nên xác suất thực tế được nhân với hệ số này.
        /// </summary>
        private const float OutShotProbabilityScale = 0.25f;

        /// <summary>Khoảng vượt vạch biên (mét) khi AI cố tình đánh ra ngoài.</summary>
        private const float MinOutOffset = 0.35f;
        private const float MaxOutOffset = 1.2f;

        /// <summary>Tạo trạng thái đánh bóng cho một AI.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public AITakeShotState(PickleballAIController controller)
        {
            aiController = controller;
        }

        /// <summary>Giơ vợt và bắt đầu xoay vợt về điểm đón bóng.</summary>
        public void Enter()
        {
            if (aiController == null) return;

            isPaddleAligned = false;
            isHitBallComplete = false;
            timer = 0f;

            initialPaddlePosition = aiController.paddleHolder != null
                ? aiController.paddleHolder.position
                : aiController.transform.position;

            aiController.AnimationPaddleAligned += OnPaddleAligned;
            aiController.AnimationShotHit += OnPaddleAligned;

            PlayPreShotAnimation();
            AlignPaddle();
        }

        /// <summary>Huỷ đăng ký animation event và trả vợt về tư thế gốc.</summary>
        public void Exit()
        {
            if (aiController == null) return;

            aiController.AnimationPaddleAligned -= OnPaddleAligned;
            aiController.AnimationShotHit -= OnPaddleAligned;

            if (racquetAnimator != null) racquetAnimator.ResetPose();
            aiController.ResetPaddlePose();
        }

        /// <summary>Vợt khớp hướng thì đánh; quá thời gian thì bỏ lỡ cú đánh.</summary>
        public void Update()
        {
            if (aiController == null || isHitBallComplete) return;

            timer += Time.deltaTime;

            if (!isPaddleAligned && racquetAnimator == null && timer >= FallbackPaddleAlignTime)
            {
                OnPaddleAligned();
            }

            if (isPaddleAligned)
            {
                TakeShot();
                return;
            }

            if (timer >= MaxShotWindow) aiController.ChangeState(PlayerState.Idle);
        }

        /// <summary>Vợt đã xoay khớp hướng đón bóng — mở khoá cho cú đánh.</summary>
        private void OnPaddleAligned()
        {
            isPaddleAligned = true;
        }

        /// <summary>Chọn và phát animation giơ vợt phù hợp với độ cao/bên của điểm đón bóng.</summary>
        private void PlayPreShotAnimation()
        {
            BasePlayerAnimationController animation = aiController.animationController;
            if (animation == null) return;

            Vector3 hitPoint = aiController.TargetBallHitPoint;
            float height = aiController.profile != null ? aiController.profile.height : 1.7f;
            ShotType shotType = aiController.isAttemptingVolley ? ShotType.Volley : ShotType.Groundstroke;

            animation.PlayPreShot(animation.DetermineShotAnimation(shotType,
                                                                   aiController.GetShotSideForPoint(hitPoint),
                                                                   hitPoint.y, height));
        }

        /// <summary>Bắt đầu xoay vợt về điểm đón bóng; không có <see cref="RacquetAnimator"/> thì dùng đồng hồ dự phòng.</summary>
        private void AlignPaddle()
        {
            if (racquetAnimator == null)
            {
                racquetAnimator = aiController.GetComponentInChildren<RacquetAnimator>(true);
            }

            if (racquetAnimator == null) return;

            racquetAnimator.AlignTo(aiController.TargetBallHitPoint, aiController.PaddleAlignSpeed, OnPaddleAligned);
        }

        // ------------------------------------------------------------------
        // Cú đánh
        // ------------------------------------------------------------------

        /// <summary>
        /// Toàn bộ quyết định của một cú đánh AI: điểm rơi (chiến thuật) → loại cú đánh → thời gian bay
        /// → sai số → xoáy → bắn bóng.
        /// </summary>
        private void TakeShot()
        {
            if (isHitBallComplete) return;
            isHitBallComplete = true;

            Vector3 aiPosition = aiController.transform.position;

            Vector3 destination = aiController.DetermineDestination();
            ShotType shotType = aiController.SelectShotType(aiPosition, destination);
            float shotTime = aiController.CalculateShotTime(destination, shotType);

            destination = ApplyShotError(destination);

            Vector3 velocity = aiController.CalculateInitialVelocity(destination, shotTime);

            float swingSpinIntensity = aiController.ShouldApplySlice()
                ? Mathf.Clamp01(aiController.baseShotEffect * Random.Range(0.5f, 1f))
                : 0f;

            Vector3 swingSpinDirection = swingSpinIntensity > 0f
                ? aiController.DetermineSwingSpinDirection(velocity)
                : Vector3.zero;

            float length = Vector2.Distance(new Vector2(aiPosition.x, aiPosition.z),
                                            new Vector2(destination.x, destination.z));

            float shotPower = aiController.profile != null ? aiController.profile.shotPower : 0.5f;

            aiController.HitBallAnimation(shotPower, () =>
            {
                HitBall(destination, shotTime, swingSpinIntensity, swingSpinDirection, length);
            });
        }

        /// <summary>
        /// Áp sai số của tay vợt lên điểm rơi.
        /// <list type="number">
        /// <item><description>Lệch ngẫu nhiên trong bán kính <c>maxShotError * </c><see cref="MaxShotErrorDistance"/> mét.</description></item>
        /// <item><description>Với xác suất <c>ProbablityToTakeOutShots</c> (đã nhân
        /// <see cref="OutShotProbabilityScale"/>), CỐ TÌNH đẩy điểm rơi ra ngoài vạch biên.</description></item>
        /// </list>
        /// </summary>
        /// <param name="destination">Điểm rơi lý tưởng do chiến thuật chọn.</param>
        private Vector3 ApplyShotError(Vector3 destination)
        {
            PlayerProfileProperties profile = aiController.profile;
            if (profile == null) return destination;

            float errorRadius = Mathf.Clamp01(profile.maxShotError) * MaxShotErrorDistance;
            if (errorRadius > 0f)
            {
                Vector2 offset = Random.insideUnitCircle * errorRadius;
                destination += new Vector3(offset.x, 0f, offset.y);
            }

            float outChance = Mathf.Clamp01(profile.ProbablityToTakeOutShots) * OutShotProbabilityScale;
            if (Random.value < outChance) destination = PushOutOfCourt(destination);

            destination.y = 0f;
            return destination;
        }

        /// <summary>Đẩy điểm rơi ra ngoài vạch biên (dài hoặc rộng) — đây là cách AI yếu tự thua điểm.</summary>
        private Vector3 PushOutOfCourt(Vector3 destination)
        {
            Court court = aiController.MatchCourt;

            float halfWidth = court != null ? court.HalfWidth : aiController.courtWidth * 0.5f;
            float halfLength = court != null ? court.HalfLength : aiController.courtDepth;

            float offset = Random.Range(MinOutOffset, MaxOutOffset);

            if (Random.value < 0.5f)
            {
                // Ra ngoài vạch cuối sân.
                float sign = destination.z >= 0f ? 1f : -1f;
                destination.z = sign * (halfLength + offset);
            }
            else
            {
                // Ra ngoài vạch biên dọc.
                float sign = destination.x >= 0f ? 1f : -1f;
                destination.x = sign * (halfWidth + offset);
            }

            return destination;
        }

        /// <summary>Bắn bóng đi và trở về trạng thái chờ.</summary>
        /// <param name="destination">Điểm rơi cuối cùng (đã tính sai số).</param>
        /// <param name="shotTime">Thời gian bay mong muốn (giây).</param>
        /// <param name="swingSpinIntensity">Cường độ xoáy/vung của cú đánh.</param>
        /// <param name="swingSpinDirection">Hướng bẻ cong trên mặt phẳng XZ.</param>
        /// <param name="length">Độ dài "cú vuốt ảo" — dùng nhiễu thời gian bay giống người chơi.</param>
        private void HitBall(Vector3 destination, float shotTime, float swingSpinIntensity,
                             Vector3 swingSpinDirection, float length)
        {
            if (aiController == null || !aiController.isActiveAndEnabled) return;

            PlayerProfileProperties profile = aiController.profile;

            float swingAbility = profile != null ? profile.swingAbility : 0f;
            float spinAbility = profile != null ? profile.spinAbility : 0f;

            aiController.HitBall(aiController.teamID, destination, shotTime, swingSpinIntensity, swingAbility,
                                 spinAbility, swingSpinDirection, length);

            aiController.ChangeState(PlayerState.Idle);
        }

        /// <summary>Vị trí vợt lúc bắt đầu pha đánh (dùng để so sánh biên độ vung).</summary>
        public Vector3 InitialPaddlePosition => initialPaddlePosition;

        /// <summary>True khi vợt đã xoay khớp hướng đón bóng.</summary>
        public bool IsPaddleAligned => isPaddleAligned;
    }
}
