using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Chiến thuật bậc <see cref="AIDifficulty.Amateur"/>: biết đánh nhưng chưa kiểm soát được bóng.
    /// <para><b>Nhắm</b>: vẫn về giữa sân đối phương nhưng kèm SAI SỐ NGẪU NHIÊN LỚN
    /// (tới ~35% bề ngang và ~25% chiều sâu nửa sân), nên bóng rơi lung tung khắp sân.</para>
    /// <para><b>Cú đánh</b>: chủ yếu <see cref="ShotType.Groundstroke"/>, thỉnh thoảng thử
    /// <see cref="ShotType.DropShot"/> khi bóng đủ thấp.</para>
    /// </summary>
    public class AmateurStrategy : IAIStrategy
    {
        /// <summary>Biên độ sai số ngang, theo tỉ lệ nửa bề ngang sân.</summary>
        private const float LateralErrorRatio = 0.7f;

        /// <summary>Biên độ sai số theo chiều sâu, theo tỉ lệ nửa sân.</summary>
        private const float DepthErrorRatio = 0.25f;

        /// <summary>Chiều sâu mục tiêu cơ bản, theo tỉ lệ nửa sân.</summary>
        private const float BaseDepthRatio = 0.6f;

        /// <summary>Xác suất thử một cú bỏ nhỏ.</summary>
        private const float DropShotChance = 0.18f;

        /// <summary>Chiều sâu kitchen mặc định (mét) dùng để phân loại "gần lưới".</summary>
        private const float KitchenDepth = 2.1336f;

        /// <summary>Giữa sân đối phương cộng sai số ngẫu nhiên lớn.</summary>
        public Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                bool isPositiveCourt)
        {
            float halfWidth = Mathf.Max(0.5f, courtWidth) * 0.5f;
            float depth = Mathf.Max(1f, courtDepth);
            float sign = isPositiveCourt ? 1f : -1f;

            float x = Random.Range(-halfWidth * LateralErrorRatio, halfWidth * LateralErrorRatio);
            float z = sign * Mathf.Clamp(depth * (BaseDepthRatio + Random.Range(-DepthErrorRatio, DepthErrorRatio)),
                                         KitchenDepth * 0.6f, depth);

            return new Vector3(x, 0f, z);
        }

        /// <summary>Groundstroke là chính; đôi khi bỏ nhỏ nếu bóng đang thấp hơn lưới.</summary>
        public ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                       float netHeight, PlayerProfileProperties profile)
        {
            float reference = netHeight > 0f ? netHeight : 0.86f;
            float ballHeight = ballController != null ? ballController.transform.position.y : reference;

            bool ballIsLow = ballHeight < reference;
            bool destinationShort = Mathf.Abs(destination.z) <= KitchenDepth * 1.5f;

            if ((ballIsLow || destinationShort) && Random.value < DropShotChance) return ShotType.DropShot;

            return ShotType.Groundstroke;
        }
    }
}
