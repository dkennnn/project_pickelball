using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Dải giá trị hợp lệ (min/max) của từng chỉ số trong <see cref="PlayerProfile"/>.
    /// Dùng để quy đổi chỉ số shop thang 0..100 sang giá trị vật lý thật sự của nhân vật.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerProfileLimits", menuName = "Player Profile Limits", order = 1)]
    public class PlayerProfileLimits : ScriptableObject
    {
        /// <summary>Chiều cao nhân vật nhỏ nhất (mét).</summary>
        [Header("Player Size Limits")]
        public float minHeight;
        /// <summary>Chiều cao nhân vật lớn nhất (mét).</summary>
        public float maxHeight;

        /// <summary>Tốc độ di chuyển nhỏ nhất (m/s).</summary>
        [Header("Locomotion Limits")]
        public float minMovementSpeed;
        /// <summary>Tốc độ di chuyển lớn nhất (m/s).</summary>
        public float maxMovementSpeed;
        /// <summary>Độ nhanh nhẹn nhỏ nhất.</summary>
        public float minAgility;
        /// <summary>Độ nhanh nhẹn lớn nhất.</summary>
        public float maxAgility;
        /// <summary>Độ chính xác bám bóng nhỏ nhất (0..1).</summary>
        public float minTrackingAccuracy;
        /// <summary>Độ chính xác bám bóng lớn nhất (0..1).</summary>
        public float maxTrackingAccuracy;

        /// <summary>Tầm với cú đánh nhỏ nhất (mét).</summary>
        [Header("Shot Limits")]
        public float minShotRange;
        /// <summary>Tầm với cú đánh lớn nhất (mét).</summary>
        public float maxShotRange;
        /// <summary>Tầm volley nhỏ nhất (mét).</summary>
        public float minVolleyRange;
        /// <summary>Tầm volley lớn nhất (mét).</summary>
        public float maxVolleyRange;

        /// <summary>Tỉ lệ quyết định đánh bóng nhỏ nhất (0..1).</summary>
        [Header("Shot Taking Rate")]
        public float minShotTakingRate;
        /// <summary>Tỉ lệ quyết định đánh bóng lớn nhất (0..1).</summary>
        public float maxShotTakingRate;

        /// <summary>Thể lực tối đa nhỏ nhất.</summary>
        [Header("Stamina Limits")]
        public float minStamina;
        /// <summary>Thể lực tối đa lớn nhất.</summary>
        public float maxStamina;
        /// <summary>Tốc độ hồi thể lực nhỏ nhất (đơn vị/giây).</summary>
        public float minStaminaRechargeRate;
        /// <summary>Tốc độ hồi thể lực lớn nhất (đơn vị/giây).</summary>
        public float maxStaminaRechargeRate;
        /// <summary>Tốc độ tiêu hao thể lực nhỏ nhất (đơn vị/giây).</summary>
        public float minStaminaDepletionRate;
        /// <summary>Tốc độ tiêu hao thể lực lớn nhất (đơn vị/giây).</summary>
        public float maxStaminaDepletionRate;

        /// <summary>Xác suất đánh bóng ra ngoài nhỏ nhất (0..1).</summary>
        [Header("Error Limits")]
        public float minProbablityToTakeOutShots;
        /// <summary>Xác suất đánh bóng ra ngoài lớn nhất (0..1).</summary>
        public float maxProbablityToTakeOutShots;
        /// <summary>Sai số cú đánh tối đa nhỏ nhất.</summary>
        public float minMaxShotError;
        /// <summary>Sai số cú đánh tối đa lớn nhất.</summary>
        public float maxMaxShotError;
        /// <summary>Xác suất bám bóng sai nhỏ nhất (0..1).</summary>
        public float minTrackingErrorProbability;
        /// <summary>Xác suất bám bóng sai lớn nhất (0..1).</summary>
        public float maxTrackingErrorProbability;

        /// <summary>Lực đánh nhỏ nhất (0..1).</summary>
        [Header("Stroke Limits")]
        public float minShotPower;
        /// <summary>Lực đánh lớn nhất (0..1).</summary>
        public float maxShotPower;
        /// <summary>Khả năng tạo xoáy nhỏ nhất (0..1).</summary>
        public float minSpinAbility;
        /// <summary>Khả năng tạo xoáy lớn nhất (0..1).</summary>
        public float maxSpinAbility;
        /// <summary>Khả năng vung vợt nhỏ nhất (0..1).</summary>
        public float minSwingAbility;
        /// <summary>Khả năng vung vợt lớn nhất (0..1).</summary>
        public float maxSwingAbility;
        /// <summary>Xác suất vung tạo xoáy nhỏ nhất (0..1).</summary>
        public float minSwingSpinProbability;
        /// <summary>Xác suất vung tạo xoáy lớn nhất (0..1).</summary>
        public float maxSwingSpinProbability;

        /// <summary>
        /// Quy đổi một chỉ số shop thang 0..100 sang giá trị thật theo cặp min/max tương ứng.
        /// <see cref="PropertyType.None"/> hoặc giá trị lạ sẽ được trả về nguyên vẹn.
        /// </summary>
        /// <param name="type">Loại chỉ số cần quy đổi.</param>
        /// <param name="stat0To100">Giá trị chỉ số trên thang 0..100.</param>
        public float Denormalize(PropertyType type, float stat0To100)
        {
            switch (type)
            {
                case PropertyType.Agility:
                    return Utilities.StatToValue(stat0To100, minAgility, maxAgility);
                case PropertyType.Accuracy:
                    return Utilities.StatToValue(stat0To100, minTrackingAccuracy, maxTrackingAccuracy);
                case PropertyType.Power:
                    return Utilities.StatToValue(stat0To100, minShotPower, maxShotPower);
                case PropertyType.Swing:
                    return Utilities.StatToValue(stat0To100, minSwingAbility, maxSwingAbility);
                case PropertyType.Spin:
                    return Utilities.StatToValue(stat0To100, minSpinAbility, maxSpinAbility);
                case PropertyType.Speed:
                    return Utilities.StatToValue(stat0To100, minMovementSpeed, maxMovementSpeed);
                case PropertyType.Stamina:
                    return Utilities.StatToValue(stat0To100, minStamina, maxStamina);
                default:
                    return stat0To100;
            }
        }
    }
}
