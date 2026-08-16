using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Asset chỉ số gốc của một tay vợt (người chơi hoặc AI).
    /// Không sửa trực tiếp asset lúc chơi — hãy dùng <see cref="ToData"/> để lấy bản runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerProfile", menuName = "Player Profile", order = 0)]
    public class PlayerProfile : ScriptableObject
    {
        /// <summary>Tên hiển thị của tay vợt.</summary>
        [Header("Player Properties")]
        public string playerName;

        /// <summary>Chiều cao nhân vật (mét).</summary>
        public float height;

        /// <summary>Tốc độ di chuyển (m/s).</summary>
        [Header("Locomotion Properties")]
        public float MovementSpeed;

        /// <summary>Độ nhanh nhẹn khi đổi hướng.</summary>
        public float Agility;

        /// <summary>Độ chính xác khi bám theo bóng (0..1).</summary>
        public float TrackingAccuracy;

        /// <summary>Tầm với của cú đánh thường (mét).</summary>
        [Header("Shot Properties")]
        public float shotRange;

        /// <summary>Tầm với của cú volley (mét).</summary>
        public float volleyRange;

        /// <summary>Tỉ lệ quyết định đánh bóng khi bóng trong tầm.</summary>
        [Header("Shot Decision Properties")]
        [Range(0f, 1f)]
        public float shotTakingRate;

        /// <summary>Thể lực tối đa.</summary>
        [Header("Stamina Properties")]
        public float maxStamina;

        /// <summary>Tốc độ hồi thể lực (đơn vị/giây).</summary>
        public float staminaRechargeRate;

        /// <summary>Tốc độ tiêu hao thể lực (đơn vị/giây).</summary>
        public float staminaDepletionRate;

        /// <summary>Xác suất đánh bóng ra ngoài sân (0..1).</summary>
        [Header("Error Properties")]
        public float ProbablityToTakeOutShots;

        /// <summary>Sai số tối đa của điểm rơi cú đánh.</summary>
        public float maxShotError;

        /// <summary>Xác suất bám bóng sai (0..1).</summary>
        public float TrackingErrorProbablity;

        /// <summary>Lực đánh (0..1).</summary>
        [Header("Stoke Properties")]
        public float shotPower;

        /// <summary>Khả năng tạo xoáy (0..1).</summary>
        public float spinAbility;

        /// <summary>Khả năng vung vợt (0..1).</summary>
        public float swingAbility;

        /// <summary>Xác suất cú vung tạo xoáy (0..1).</summary>
        public float swingSpinProbablity;

        /// <summary>
        /// Tạo bản dữ liệu runtime từ asset này, gắn thêm tay thuận.
        /// Sửa đổi trên kết quả trả về không ảnh hưởng tới asset gốc.
        /// </summary>
        /// <param name="handSide">Tay thuận của tay vợt.</param>
        public PlayerProfileProperties ToData(HandSide handSide)
        {
            return new PlayerProfileProperties
            {
                playerName = playerName,
                height = height,
                handSide = handSide,
                MovementSpeed = MovementSpeed,
                Agility = Agility,
                TrackingAccuracy = TrackingAccuracy,
                shotRange = shotRange,
                volleyRange = volleyRange,
                shotTakingRate = shotTakingRate,
                maxStamina = maxStamina,
                staminaRechargeRate = staminaRechargeRate,
                staminaDepletionRate = staminaDepletionRate,
                ProbablityToTakeOutShots = ProbablityToTakeOutShots,
                maxShotError = maxShotError,
                TrackingErrorProbablity = TrackingErrorProbablity,
                shotPower = shotPower,
                spinAbility = spinAbility,
                swingAbility = swingAbility,
                swingSpinProbablity = swingSpinProbablity
            };
        }
    }
}
