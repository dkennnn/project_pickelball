using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Booster XOÁY: nâng <c>profile.spinAbility</c> lên <see cref="boostedSpinAbility"/> và
    /// còn ĐẢO CHIỀU XOÁY của quả bóng mỗi khi hướng bóng đổi trong pha đánh của chủ sở hữu
    /// (<see cref="BallController.OnBallDirectionChange"/>) — bóng "bẻ ngược" khiến đối thủ khó đọc.
    /// </summary>
    public class SpinBooster : Booster
    {
        /// <summary>Khả năng tạo xoáy (0..1) khi booster đang chạy.</summary>
        [SerializeField] private float boostedSpinAbility = 1f;

        /// <summary>Thời gian hiệu lực mặc định của prefab (giây).</summary>
        [SerializeField] private float boosterDuration = 10f;

        /// <summary>VFX gắn với quả bóng khi cú đánh được tăng xoáy.</summary>
        public VFXPlayer ballVFX;

        private float originalSpinAbility;
        private bool effectApplied;
        private bool directionSubscribed;

        protected override void Awake()
        {
            base.Awake();
            Type = BoosterType.Spin;
            if (activeDuration <= 0f) activeDuration = boosterDuration;
        }

        private void OnEnable()
        {
            // Chỉ đăng ký lại nếu booster đang thực sự chạy (trường hợp GameObject bị tắt/bật lại).
            if (isActive) SubscribeDirection();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            UnsubscribeDirection();
            base.OnDisable();
        }

        /// <inheritdoc/>
        public override void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            Type = BoosterType.Spin;
            base.Initialize(player, duration > 0f ? duration : boosterDuration, onComplete);
        }

        /// <inheritdoc/>
        protected override void ApplyEffect()
        {
            if (player == null || player.profile == null) return;

            originalSpinAbility = player.profile.spinAbility;
            player.profile.spinAbility = Mathf.Clamp01(boostedSpinAbility);
            effectApplied = true;

            SubscribeDirection();
        }

        /// <inheritdoc/>
        protected override void RemoveEffect()
        {
            UnsubscribeDirection();

            if (!effectApplied) return;
            effectApplied = false;

            if (player == null || player.profile == null) return;
            player.profile.spinAbility = originalSpinAbility;
        }

        /// <inheritdoc/>
        protected override void ApplyBallEffect()
        {
            if (ballVFX != null) ballVFX.Play();
        }

        /// <inheritdoc/>
        protected override void RemoveBallEffect()
        {
            if (ballVFX != null) ballVFX.Stop();
        }

        private void SubscribeDirection()
        {
            if (directionSubscribed) return;
            directionSubscribed = true;
            BallController.OnBallDirectionChange += ChangeSpinDirection;
        }

        private void UnsubscribeDirection()
        {
            if (!directionSubscribed) return;
            directionSubscribed = false;
            BallController.OnBallDirectionChange -= ChangeSpinDirection;
        }

        /// <summary>
        /// Đảo chiều xoáy của quả bóng khi hướng bóng đổi trong cú đánh của chủ sở hữu booster.
        /// Độ lớn xoáy được khuếch đại theo <see cref="boostedSpinAbility"/> và bị chặn trên bởi
        /// <see cref="BallController.MaxTorqueMagnitude"/> để không phá vật lý.
        /// </summary>
        /// <param name="id">TeamID của đội vừa đánh bóng.</param>
        /// <param name="swingDirection">Hướng bóng trước đó.</param>
        /// <param name="spinDirection">Hướng bóng mới.</param>
        private void ChangeSpinDirection(string id, BallDirection swingDirection, BallDirection spinDirection)
        {
            if (!isActive || player == null) return;
            if (id != player.teamID) return;
            if (!BallController.HasInstance) return;

            BallController ball = BallController.Instance;
            if (ball == null || ball.rb == null) return;

            Vector3 angular = ball.rb.angularVelocity;
            if (angular.sqrMagnitude <= Mathf.Epsilon) return;

            Vector3 reversed = -angular * (1f + Mathf.Clamp01(boostedSpinAbility));

            float maxMagnitude = ball.MaxTorqueMagnitude;
            if (maxMagnitude > 0f && reversed.magnitude > maxMagnitude)
            {
                reversed = reversed.normalized * maxMagnitude;
            }

            ball.rb.angularVelocity = reversed;
        }
    }
}
