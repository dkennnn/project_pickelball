using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Booster TỐC ĐỘ: nâng cả <c>profile.MovementSpeed</c> (giá trị nền mà
    /// <see cref="BasePlayerController.ApplyStaminaToSpeed"/> dùng để tính lại mỗi frame)
    /// lẫn <c>movementSpeed</c> hiện tại lên <see cref="boostedSpeed"/>.
    /// Cả hai giá trị gốc đều được lưu lại và trả về khi booster kết thúc.
    /// </summary>
    public class SpeedBooster : Booster
    {
        /// <summary>Tốc độ di chuyển (m/s) khi booster đang chạy.</summary>
        [SerializeField] private float boostedSpeed = 6f;

        /// <summary>Thời gian hiệu lực mặc định của prefab (giây).</summary>
        [SerializeField] private float boosterDuration = 10f;

        /// <summary>VFX vệt tốc độ gắn trên nhân vật.</summary>
        public VFXPlayer speedVFX;

        private float originalSpeed;
        private float profileMovementSpeed;
        private bool effectApplied;

        protected override void Awake()
        {
            base.Awake();
            Type = BoosterType.Speed;
            if (activeDuration <= 0f) activeDuration = boosterDuration;
        }

        /// <inheritdoc/>
        public override void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            Type = BoosterType.Speed;
            base.Initialize(player, duration > 0f ? duration : boosterDuration, onComplete);
        }

        /// <inheritdoc/>
        protected override void ApplyEffect()
        {
            if (player == null) return;

            originalSpeed = player.movementSpeed;
            player.movementSpeed = boostedSpeed;

            if (player.profile != null)
            {
                profileMovementSpeed = player.profile.MovementSpeed;
                player.profile.MovementSpeed = boostedSpeed;
            }

            effectApplied = true;

            if (speedVFX != null) speedVFX.Play();
        }

        /// <inheritdoc/>
        protected override void RemoveEffect()
        {
            if (!effectApplied) return;
            effectApplied = false;

            if (speedVFX != null) speedVFX.Stop();

            if (player == null) return;

            if (player.profile != null) player.profile.MovementSpeed = profileMovementSpeed;
            player.movementSpeed = originalSpeed;
        }

        /// <inheritdoc/>
        protected override void UpdateBooster()
        {
            // ApplyStaminaToSpeed() chạy mỗi frame và có thể hạ movementSpeed theo thể lực;
            // ta chỉ bảo đảm giá trị NỀN không bị hệ thống khác ghi đè xuống dưới mức boost.
            if (!effectApplied || player == null || player.profile == null) return;

            if (player.profile.MovementSpeed < boostedSpeed) player.profile.MovementSpeed = boostedSpeed;
        }
    }
}
