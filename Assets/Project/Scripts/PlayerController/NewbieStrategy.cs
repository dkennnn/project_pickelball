using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Chiến thuật bậc <see cref="AIDifficulty.Newbie"/> (và Tutorial): người mới tập chơi.
    /// <para><b>Nhắm</b>: luôn về GIỮA sân đối phương, không bao giờ tìm góc.</para>
    /// <para><b>Cú đánh</b>: duy nhất <see cref="ShotType.Groundstroke"/> — không volley, không bỏ nhỏ,
    /// không câu bổng.</para>
    /// </summary>
    public class NewbieStrategy : IAIStrategy
    {
        /// <summary>Biên độ lệch ngang so với tim sân, tính theo tỉ lệ nửa bề ngang sân.</summary>
        private const float CenterJitterRatio = 0.12f;

        /// <summary>Khoảng chiều sâu (tỉ lệ so với nửa sân) mà bóng được nhắm tới.</summary>
        private const float MinDepthRatio = 0.5f;
        private const float MaxDepthRatio = 0.65f;

        /// <summary>Luôn nhắm về giữa nửa sân đối phương với dao động rất nhỏ.</summary>
        public Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                bool isPositiveCourt)
        {
            float halfWidth = Mathf.Max(0.5f, courtWidth) * 0.5f;
            float sign = isPositiveCourt ? 1f : -1f;

            float x = Random.Range(-halfWidth * CenterJitterRatio, halfWidth * CenterJitterRatio);
            float z = sign * Mathf.Max(1f, courtDepth) * Random.Range(MinDepthRatio, MaxDepthRatio);

            return new Vector3(x, 0f, z);
        }

        /// <summary>Người mới chỉ biết một kiểu đánh duy nhất.</summary>
        public ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                       float netHeight, PlayerProfileProperties profile)
        {
            return ShotType.Groundstroke;
        }
    }
}
