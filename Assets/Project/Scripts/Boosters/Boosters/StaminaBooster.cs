using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Booster THỂ LỰC: khoá thể lực ở mức tối đa trong suốt thời gian hiệu lực, nên tay vợt
    /// không bao giờ bị tụt tốc độ vì mệt (xem <see cref="BasePlayerController.ApplyStaminaToSpeed"/>).
    /// Booster này KHÔNG sửa chỉ số nào trong profile nên không cần lưu giá trị gốc;
    /// khi kết thúc, thể lực lại tiêu hao/hồi bình thường theo <c>ManageStamina</c>.
    /// </summary>
    public class StaminaBooster : Booster
    {
        /// <summary>Thời gian hiệu lực mặc định của prefab (giây).</summary>
        [SerializeField] private float boosterDuration = 10f;

        /// <summary>VFX hào quang thể lực gắn trên nhân vật.</summary>
        public VFXPlayer staminaVFX;

        protected override void Awake()
        {
            base.Awake();
            Type = BoosterType.Stamina;
            if (activeDuration <= 0f) activeDuration = boosterDuration;
        }

        /// <inheritdoc/>
        public override void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            Type = BoosterType.Stamina;
            base.Initialize(player, duration > 0f ? duration : boosterDuration, onComplete);
        }

        /// <inheritdoc/>
        protected override void ApplyEffect()
        {
            if (staminaVFX != null) staminaVFX.Play();

            if (player == null || player.profile == null) return;
            player.stamina = player.profile.maxStamina;
        }

        /// <inheritdoc/>
        protected override void RemoveEffect()
        {
            if (staminaVFX != null) staminaVFX.Stop();
        }

        /// <inheritdoc/>
        protected override void UpdateBooster()
        {
            if (player == null || player.profile == null) return;

            // RechargeStamina tự clamp về [0, maxStamina] nên nạp thừa là an toàn:
            // thể lực luôn bị ghim ở mức tối đa suốt thời gian booster chạy.
            player.RechargeStamina(player.profile.maxStamina);
        }
    }
}
