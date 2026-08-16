using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Booster LỰC ĐÁNH: nâng <c>profile.shotPower</c> lên <see cref="boostedPower"/> trong thời gian hiệu lực.
    /// Giá trị gốc được lưu lúc kích hoạt và trả lại nguyên vẹn khi booster kết thúc.
    /// </summary>
    public class PowerBooster : Booster
    {
        /// <summary>Lực đánh (0..1) khi booster đang chạy.</summary>
        [SerializeField] private float boostedPower = 1f;

        /// <summary>Thời gian hiệu lực mặc định của prefab (giây).</summary>
        [SerializeField] private float boosterDuration = 10f;

        /// <summary>VFX gắn với quả bóng khi cú đánh được tăng lực.</summary>
        public VFXPlayer ballVFX;

        private float originalPower;
        private bool effectApplied;

        protected override void Awake()
        {
            base.Awake();
            Type = BoosterType.Power;
            if (activeDuration <= 0f) activeDuration = boosterDuration;
        }

        /// <inheritdoc/>
        public override void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            Type = BoosterType.Power;
            base.Initialize(player, duration > 0f ? duration : boosterDuration, onComplete);
        }

        /// <inheritdoc/>
        protected override void ApplyEffect()
        {
            if (player == null || player.profile == null) return;

            originalPower = player.profile.shotPower;
            player.profile.shotPower = Mathf.Clamp01(boostedPower);
            effectApplied = true;
        }

        /// <inheritdoc/>
        protected override void RemoveEffect()
        {
            if (!effectApplied) return;
            effectApplied = false;

            if (player == null || player.profile == null) return;
            player.profile.shotPower = originalPower;
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
    }
}
