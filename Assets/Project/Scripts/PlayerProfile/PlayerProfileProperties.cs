using System;

namespace Pickleball
{
    /// <summary>
    /// Bản sao runtime (thuần dữ liệu, serialize được) của <see cref="PlayerProfile"/>.
    /// Dùng để truyền chỉ số nhân vật qua mạng và cho các controller sửa đổi
    /// mà không đụng vào asset gốc.
    /// </summary>
    [Serializable]
    public class PlayerProfileProperties
    {
        /// <summary>Tên hiển thị của tay vợt.</summary>
        public string playerName;

        /// <summary>Chiều cao nhân vật (mét).</summary>
        public float height;

        /// <summary>Tay thuận của tay vợt.</summary>
        public HandSide handSide;

        /// <summary>Tốc độ di chuyển (m/s).</summary>
        public float MovementSpeed;

        /// <summary>Độ nhanh nhẹn khi đổi hướng.</summary>
        public float Agility;

        /// <summary>Độ chính xác khi bám theo bóng (0..1).</summary>
        public float TrackingAccuracy;

        /// <summary>Tầm với của cú đánh thường (mét).</summary>
        public float shotRange;

        /// <summary>Tầm với của cú volley (mét).</summary>
        public float volleyRange;

        /// <summary>Tỉ lệ quyết định đánh bóng khi bóng trong tầm (0..1).</summary>
        public float shotTakingRate;

        /// <summary>Thể lực tối đa.</summary>
        public float maxStamina;

        /// <summary>Tốc độ hồi thể lực (đơn vị/giây).</summary>
        public float staminaRechargeRate;

        /// <summary>Tốc độ tiêu hao thể lực (đơn vị/giây).</summary>
        public float staminaDepletionRate;

        /// <summary>Xác suất đánh bóng ra ngoài sân (0..1).</summary>
        public float ProbablityToTakeOutShots;

        /// <summary>Sai số tối đa của điểm rơi cú đánh.</summary>
        public float maxShotError;

        /// <summary>Xác suất bám bóng sai (0..1).</summary>
        public float TrackingErrorProbablity;

        /// <summary>Lực đánh (0..1).</summary>
        public float shotPower;

        /// <summary>Khả năng tạo xoáy (0..1).</summary>
        public float spinAbility;

        /// <summary>Khả năng vung vợt (0..1).</summary>
        public float swingAbility;

        /// <summary>Xác suất cú vung tạo xoáy (0..1).</summary>
        public float swingSpinProbablity;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public PlayerProfileProperties() { }

        /// <summary>Khởi tạo bằng cách sao chép toàn bộ chỉ số từ một bản khác.</summary>
        /// <param name="source">Nguồn để sao chép; <c>null</c> sẽ tạo bản rỗng.</param>
        public PlayerProfileProperties(PlayerProfileProperties source)
        {
            if (source == null) return;

            playerName = source.playerName;
            height = source.height;
            handSide = source.handSide;
            MovementSpeed = source.MovementSpeed;
            Agility = source.Agility;
            TrackingAccuracy = source.TrackingAccuracy;
            shotRange = source.shotRange;
            volleyRange = source.volleyRange;
            shotTakingRate = source.shotTakingRate;
            maxStamina = source.maxStamina;
            staminaRechargeRate = source.staminaRechargeRate;
            staminaDepletionRate = source.staminaDepletionRate;
            ProbablityToTakeOutShots = source.ProbablityToTakeOutShots;
            maxShotError = source.maxShotError;
            TrackingErrorProbablity = source.TrackingErrorProbablity;
            shotPower = source.shotPower;
            spinAbility = source.spinAbility;
            swingAbility = source.swingAbility;
            swingSpinProbablity = source.swingSpinProbablity;
        }

        /// <summary>Tạo một bản sao độc lập của chỉ số hiện tại.</summary>
        public PlayerProfileProperties Clone()
        {
            return new PlayerProfileProperties(this);
        }
    }
}
