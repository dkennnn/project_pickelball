using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Booster VUNG VỢT: nâng <c>profile.swingAbility</c> lên <see cref="boostedSwingAbility"/>,
    /// giúp cú đánh bẻ cong mạnh hơn. Giá trị gốc được trả lại khi booster kết thúc.
    /// </summary>
    public class SwingBooster : Booster
    {
        /// <summary>Khả năng vung vợt (0..1) khi booster đang chạy.</summary>
        [SerializeField] private float boostedSwingAbility = 1f;

        /// <summary>Thời gian hiệu lực mặc định của prefab (giây).</summary>
        [SerializeField] private float boosterDuration = 10f;

        /// <summary>VFX gắn với quả bóng khi cú đánh được tăng độ vung.</summary>
        public VFXPlayer ballVFX;

        private float originalSwingAbility;
        private bool effectApplied;

        protected override void Awake()
        {
            base.Awake();
            Type = BoosterType.Swing;
            if (activeDuration <= 0f) activeDuration = boosterDuration;
        }

        /// <inheritdoc/>
        public override void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            Type = BoosterType.Swing;
            base.Initialize(player, duration > 0f ? duration : boosterDuration, onComplete);
        }

        /// <inheritdoc/>
        protected override void ApplyEffect()
        {
            if (player == null || player.profile == null) return;

            originalSwingAbility = player.profile.swingAbility;
            player.profile.swingAbility = Mathf.Clamp01(boostedSwingAbility);
            effectApplied = true;
        }

        /// <inheritdoc/>
        protected override void RemoveEffect()
        {
            if (!effectApplied) return;
            effectApplied = false;

            if (player == null || player.profile == null) return;
            player.profile.swingAbility = originalSwingAbility;
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
