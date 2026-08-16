using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Chiến thuật bậc <see cref="AIDifficulty.Pro"/>: chơi tấn công thực thụ.
    /// <para><b>Nhắm</b>: uỷ quyền cho <see cref="AIHelper.CalculateShotDestination"/> ở chế độ
    /// <c>isAggressive = true</c> — nhắm sát biên (cách vạch ~0.4m) và chọn điểm mà đối thủ mất nhiều
    /// thời gian chạy tới nhất.</para>
    /// <para><b>Cú đánh</b>: dùng đủ bộ 6 loại qua <see cref="AIHelper.SelectBestShot"/>, và ƯU TIÊN
    /// <see cref="ShotType.Dink"/> khi cả AI lẫn điểm rơi đều nằm sát vạch kitchen (đấu dink trên lưới).</para>
    /// </summary>
    public class ProStrategy : IAIStrategy
    {
        /// <summary>Chiều sâu kitchen mặc định (mét).</summary>
        private const float KitchenDepth = 2.1336f;

        /// <summary>Hệ số nới vùng "sát lưới" khi xét điều kiện đấu dink.</summary>
        private const float NetZoneFactor = 1.3f;

        /// <summary>Nhắm sát biên, xa đối thủ nhất theo thời gian di chuyển.</summary>
        public Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                bool isPositiveCourt)
        {
            return AIHelper.CalculateShotDestination(playerPosition, courtWidth, courtDepth, isPositiveCourt, true);
        }

        /// <summary>Ưu tiên đấu dink khi đôi bên cùng trên lưới, còn lại theo <see cref="AIHelper.SelectBestShot"/>.</summary>
        public ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                       float netHeight, PlayerProfileProperties profile)
        {
            bool aiAtNet = Mathf.Abs(aiPosition.z) <= KitchenDepth * NetZoneFactor;
            bool destinationAtNet = Mathf.Abs(destination.z) <= KitchenDepth * NetZoneFactor;
            bool ballHasBounced = ballController == null || ballController.bounceCount > 0;

            // Đấu dink chỉ hợp lệ khi bóng đã nảy — chưa nảy mà đứng trong kitchen là phạm luật volley.
            if (aiAtNet && destinationAtNet && ballHasBounced) return ShotType.Dink;

            return AIHelper.SelectBestShot(aiPosition, destination, ballController, netHeight, profile, true,
                                           KitchenDepth);
        }
    }
}
