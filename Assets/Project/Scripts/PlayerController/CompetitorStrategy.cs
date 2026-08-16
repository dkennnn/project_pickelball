using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Chiến thuật bậc <see cref="AIDifficulty.Competitor"/>: đã biết chơi có ý đồ.
    /// <para><b>Nhắm</b>: luôn đánh sang NỬA SÂN NGANG ĐỐI DIỆN với người chơi (bắt đối thủ chạy ngang),
    /// độ sâu vừa phải tới sâu — nhưng chưa dám ép sát biên như Pro.</para>
    /// <para><b>Cú đánh</b>: <see cref="ShotType.Groundstroke"/> là nền, thêm
    /// <see cref="ShotType.Volley"/> khi chặn được bóng trước lúc nảy và <see cref="ShotType.Lob"/>
    /// khi bị đẩy sâu cuối sân.</para>
    /// </summary>
    public class CompetitorStrategy : IAIStrategy
    {
        /// <summary>Khoảng lệch ngang tối thiểu/tối đa so với tim sân, theo tỉ lệ nửa bề ngang.</summary>
        private const float MinLateralRatio = 0.35f;
        private const float MaxLateralRatio = 0.8f;

        /// <summary>Khoảng chiều sâu mục tiêu, theo tỉ lệ nửa sân.</summary>
        private const float MinDepthRatio = 0.55f;
        private const float MaxDepthRatio = 0.85f;

        /// <summary>Chiều sâu kitchen mặc định (mét).</summary>
        private const float KitchenDepth = 2.1336f;

        /// <summary>Xác suất chọn câu bổng khi đủ điều kiện.</summary>
        private const float LobChance = 0.35f;

        /// <summary>Đánh sang nửa sân xa người chơi nhất theo trục X.</summary>
        public Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                bool isPositiveCourt)
        {
            float halfWidth = Mathf.Max(0.5f, courtWidth) * 0.5f;
            float depth = Mathf.Max(1f, courtDepth);
            float sign = isPositiveCourt ? 1f : -1f;

            // Người chơi lệch phải thì đánh sang trái và ngược lại.
            float side = playerPosition.x >= 0f ? -1f : 1f;

            float x = side * halfWidth * Random.Range(MinLateralRatio, MaxLateralRatio);
            float z = sign * depth * Random.Range(MinDepthRatio, MaxDepthRatio);

            return new Vector3(x, 0f, z);
        }

        /// <summary>Ba vũ khí: đánh nền, chặn volley và câu bổng cứu thế trận.</summary>
        public ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                       float netHeight, PlayerProfileProperties profile)
        {
            float reference = netHeight > 0f ? netHeight : 0.86f;
            float aiDepth = Mathf.Abs(aiPosition.z);

            bool ballHasBounced = ballController == null || ballController.bounceCount > 0;
            float ballHeight = ballController != null ? ballController.transform.position.y : reference;

            // Chặn bóng trước khi nảy, miễn là không đứng trong kitchen (luật cấm volley).
            if (!ballHasBounced && aiDepth > KitchenDepth * 1.1f) return ShotType.Volley;

            // Bị đẩy sâu cuối sân với quả bóng thấp: câu bổng để có thời gian về vị trí.
            if (ballHeight < reference && aiDepth > KitchenDepth * 2f && Random.value < LobChance)
            {
                return ShotType.Lob;
            }

            return ShotType.Groundstroke;
        }
    }
}
