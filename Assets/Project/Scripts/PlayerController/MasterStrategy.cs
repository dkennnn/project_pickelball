using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Chiến thuật bậc <see cref="AIDifficulty.Master"/>: mạnh nhất game.
    /// <para><b>Nhắm</b>: đọc vị trí người chơi rồi quét một lưới 12 ứng viên phủ khắp nửa sân đối phương
    /// (góc sâu, góc ngắn sau vạch kitchen, hai biên giữa sân, hai điểm sát vạch giữa) và chọn
    /// <b>"điểm chết"</b> — điểm mà đối thủ tốn NHIỀU THỜI GIAN DI CHUYỂN nhất, tính theo mô hình
    /// chạy ngang nhanh hơn chạy tiến/lùi của <see cref="AIHelper.EstimateTravelTime"/>.</para>
    /// <para><b>Cú đánh</b>: như <see cref="ProStrategy"/> (đủ 6 loại, ưu tiên dink trên lưới) nhưng
    /// TỐI ĐA HOÁ XOÁY: khi chỉ số <c>spinAbility</c> cao, những quả groundstroke trung tính được
    /// đổi thành <see cref="ShotType.DropShot"/>/<see cref="ShotType.Dink"/> — các cú đánh chậm mà
    /// hiệu ứng xoáy của <see cref="BallController"/> ăn mạnh nhất (xoáy chỉ bẻ được bóng chậm).</para>
    /// </summary>
    public class MasterStrategy : IAIStrategy
    {
        /// <summary>Chiều sâu kitchen mặc định (mét).</summary>
        private const float KitchenDepth = 2.1336f;

        /// <summary>Khoảng cách an toàn tới vạch biên (mét) — Master dám đánh sát hơn Pro.</summary>
        private const float EdgePadding = 0.35f;

        /// <summary>Khoảng lùi tối thiểu so với baseline (mét).</summary>
        private const float BaselinePadding = 0.5f;

        /// <summary>Hệ số nới vùng "sát lưới".</summary>
        private const float NetZoneFactor = 1.3f;

        /// <summary>Ngưỡng <c>spinAbility</c> để bắt đầu ưu tiên các cú đánh ăn xoáy.</summary>
        private const float SpinPreferenceThreshold = 0.55f;

        /// <summary>Quét lưới ứng viên và chọn "điểm chết" của đối thủ.</summary>
        public Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                bool isPositiveCourt)
        {
            float halfWidth = Mathf.Max(0.5f, courtWidth) * 0.5f;
            float depth = Mathf.Max(1f, courtDepth);
            float sign = isPositiveCourt ? 1f : -1f;

            // Đối thủ đứng ngoài sân thì quy chiếu về điểm hợp lệ gần họ nhất.
            Vector3 reference = playerPosition;
            if (AIHelper.IsPlayerOutsideCourt(playerPosition, courtWidth, depth))
            {
                reference = AIHelper.GetNearestInsideCourtPosition(playerPosition, courtWidth, depth, isPositiveCourt,
                                                                   KitchenDepth);
            }

            float edgeX = Mathf.Max(0.2f, halfWidth - EdgePadding);
            float midX = edgeX * 0.5f;
            float deepZ = Mathf.Max(KitchenDepth + 0.4f, depth - BaselinePadding);
            float shortZ = Mathf.Min(deepZ, KitchenDepth + 0.3f);
            float midZ = Mathf.Lerp(shortZ, deepZ, 0.55f);

            Vector3 best = new Vector3(0f, 0f, sign * deepZ);
            float bestTime = float.MinValue;

            // 12 ứng viên: 3 mức sâu × (2 biên + 2 điểm trong).
            float[] xs = { -edgeX, -midX, midX, edgeX };
            float[] zs = { shortZ, midZ, deepZ };

            for (int zi = 0; zi < zs.Length; zi++)
            {
                for (int xi = 0; xi < xs.Length; xi++)
                {
                    Vector3 candidate = new Vector3(xs[xi], 0f, sign * zs[zi]);
                    float travelTime = AIHelper.EstimateTravelTime(reference, candidate);

                    if (travelTime <= bestTime) continue;

                    bestTime = travelTime;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>Như Pro nhưng thiên về những cú đánh mà xoáy phát huy tối đa.</summary>
        public ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                       float netHeight, PlayerProfileProperties profile)
        {
            bool aiAtNet = Mathf.Abs(aiPosition.z) <= KitchenDepth * NetZoneFactor;
            bool destinationAtNet = Mathf.Abs(destination.z) <= KitchenDepth * NetZoneFactor;
            bool ballHasBounced = ballController == null || ballController.bounceCount > 0;

            if (aiAtNet && destinationAtNet && ballHasBounced) return ShotType.Dink;

            ShotType shotType = AIHelper.SelectBestShot(aiPosition, destination, ballController, netHeight, profile,
                                                        true, KitchenDepth);

            // Tối đa hoá xoáy: đổi cú đánh nền trung tính thành cú chậm hơn để xoáy kịp bẻ bóng.
            float spin = profile != null ? Mathf.Clamp01(profile.spinAbility) : 0f;
            if (shotType == ShotType.Groundstroke && spin >= SpinPreferenceThreshold && Random.value < spin)
            {
                return destinationAtNet ? ShotType.Dink : ShotType.DropShot;
            }

            return shotType;
        }
    }
}
