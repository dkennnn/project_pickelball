using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Nhân vật người chơi. Ngoài 7 chỉ số chung, nhân vật còn quyết định các thông số
    /// hình thể/thể lực không lấy từ trang bị: tầm volley theo bậc, tầm đánh, chiều cao,
    /// tốc độ hồi và tiêu hao thể lực.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "ScriptableObjects/Shop/Character")]
    public class Character : Item
    {
        /// <summary>Tầm volley (mét) tại từng bậc nâng cấp, index = <see cref="Item.currentLevel"/>.</summary>
        public float[] volleyLevels;

        /// <summary>Tầm với của cú đánh thường (mét), không đổi theo bậc.</summary>
        public float shotRange;

        /// <summary>Chiều cao nhân vật (mét).</summary>
        public float height;

        /// <summary>Tốc độ hồi thể lực (đơn vị/giây).</summary>
        public float staminaRechargeRate;

        /// <summary>Tốc độ tiêu hao thể lực (đơn vị/giây).</summary>
        public float staminaDepletionRate;

        /// <summary>Prefab điều khiển nhân vật sẽ được sinh ra trong trận.</summary>
        public BasePlayerController playerPrefab;

        /// <summary>Tầm đánh hiện tại (mét).</summary>
        public float GetCurrentShotRange()
        {
            return shotRange;
        }

        /// <summary>Tầm volley ứng với bậc nâng cấp hiện tại (mét). Trả về 0 nếu chưa cấu hình.</summary>
        public float GetCurrentVolley()
        {
            if (volleyLevels == null || volleyLevels.Length == 0) return 0f;
            return volleyLevels[Mathf.Clamp(currentLevel, 0, volleyLevels.Length - 1)];
        }

        /// <summary>Chiều cao hiện tại (mét).</summary>
        public float GetCurrentHeight()
        {
            return height;
        }

        /// <summary>Tốc độ hồi thể lực hiện tại (đơn vị/giây).</summary>
        public float GetCurrentStaminaRechargeRate()
        {
            return staminaRechargeRate;
        }

        /// <summary>Tốc độ tiêu hao thể lực hiện tại (đơn vị/giây).</summary>
        public float GetCurrentStaminaDepletionRate()
        {
            return staminaDepletionRate;
        }
    }
}
