using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bộ hàm tĩnh dùng chung cho mọi <see cref="IAIStrategy"/>: chọn điểm rơi, kẹp điểm rơi về trong sân
    /// và chọn loại cú đánh theo tình huống.
    ///
    /// <para><b>Quy ước tham số</b> (thống nhất trong toàn bộ hệ AI):</para>
    /// <list type="bullet">
    /// <item><description><c>courtWidth</c> = bề ngang TOÀN SÂN theo trục X (mặc định 6.096).</description></item>
    /// <item><description><c>courtDepth</c> = chiều sâu của MỘT NỬA sân theo trục Z (mặc định 6.7056),
    /// tức <see cref="Court.HalfLength"/>.</description></item>
    /// <item><description><c>isPositiveCourt</c> = nửa sân ĐÍCH (nơi bóng phải rơi), không phải nửa sân của AI.</description></item>
    /// </list>
    ///
    /// <para><b>Mô hình "thời gian di chuyển"</b>: đối thủ chạy ngang nhanh hơn chạy tiến/lùi, nên chi phí
    /// theo trục Z được nhân thêm <c>DepthCostFactor</c>. Cú đánh aggressive luôn chọn điểm khiến chi phí này
    /// LỚN NHẤT — đó chính là "điểm chết" của đối thủ.</para>
    /// </summary>
    public static class AIHelper
    {
        /// <summary>Khoảng cách an toàn (mét) từ điểm ngắm tới vạch biên khi đánh aggressive.</summary>
        private const float AggressiveEdgePadding = 0.4f;

        /// <summary>Khoảng lùi tối thiểu (mét) so với baseline để cú đánh không tự ra ngoài.</summary>
        private const float BaselinePadding = 0.6f;

        /// <summary>Tỉ lệ chiều sâu kitchen so với nửa sân (2.1336 / 6.7056 ≈ 0.318).</summary>
        private const float KitchenDepthRatio = 0.32f;

        /// <summary>Tốc độ chạy giả định của đối thủ (m/s) khi ước lượng thời gian di chuyển.</summary>
        private const float AssumedOpponentSpeed = 4f;

        /// <summary>Hệ số phạt khi đối thủ phải chạy theo trục Z (tiến/lùi chậm hơn chạy ngang).</summary>
        private const float DepthCostFactor = 1.15f;

        /// <summary>Nửa bề rộng vùng được coi là "giữa sân" (mét).</summary>
        private const float CenterHalfWidth = 1.2f;

        // ------------------------------------------------------------------
        // Chọn điểm rơi
        // ------------------------------------------------------------------

        /// <summary>
        /// Chọn điểm rơi trong nửa sân đối phương.
        /// <para>
        /// <paramref name="isAggressive"/> = <c>false</c>: nhắm về giữa sân đối phương (an toàn, bóng dễ vào).
        /// <br/>
        /// <paramref name="isAggressive"/> = <c>true</c>: duyệt các điểm sát biên (cách vạch
        /// <see cref="AggressiveEdgePadding"/> mét) và chọn điểm khiến đối thủ mất NHIỀU THỜI GIAN chạy tới nhất.
        /// </para>
        /// </summary>
        /// <param name="playerPosition">Vị trí đối thủ (world space).</param>
        /// <param name="courtWidth">Bề ngang toàn sân theo trục X (mét).</param>
        /// <param name="courtDepth">Chiều sâu một nửa sân theo trục Z (mét).</param>
        /// <param name="isPositiveCourt">Nửa sân đích (true = <c>z &gt; 0</c>).</param>
        /// <param name="isAggressive">True để nhắm sát biên và xa đối thủ nhất.</param>
        /// <returns>Điểm rơi mong muốn (world space, <c>y = 0</c>).</returns>
        public static Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                                       bool isPositiveCourt, bool isAggressive)
        {
            courtWidth = Mathf.Max(1f, courtWidth);
            courtDepth = Mathf.Max(1f, courtDepth);

            float halfWidth = courtWidth * 0.5f;
            float sign = isPositiveCourt ? 1f : -1f;
            float kitchenDepth = courtDepth * KitchenDepthRatio;

            // Đối thủ đang đứng ngoài sân thì quy chiếu về điểm trong sân gần họ nhất,
            // nếu không phép đo "xa đối thủ nhất" sẽ luôn chọn đúng một góc.
            Vector3 reference = playerPosition;
            if (IsPlayerOutsideCourt(playerPosition, courtWidth, courtDepth))
            {
                reference = GetNearestInsideCourtPosition(playerPosition, courtWidth, courtDepth, isPositiveCourt,
                                                          kitchenDepth);
            }

            if (!isAggressive)
            {
                // An toàn: giữa sân đối phương, hơi sâu để bóng có chỗ rơi.
                float safeX = Random.Range(-halfWidth * 0.15f, halfWidth * 0.15f);
                float safeZ = sign * courtDepth * Random.Range(0.5f, 0.68f);
                return new Vector3(safeX, 0f, safeZ);
            }

            float edgeX = Mathf.Max(0.2f, halfWidth - AggressiveEdgePadding);
            float deepZ = Mathf.Max(kitchenDepth + 0.3f, courtDepth - BaselinePadding);
            float shortZ = kitchenDepth + 0.35f;
            float midZ = Mathf.Lerp(shortZ, deepZ, 0.55f);

            // 6 ứng viên: 2 góc sâu, 2 góc ngắn (ngay sau vạch kitchen), 2 điểm rộng giữa sân.
            Vector3 best = Vector3.zero;
            float bestCost = float.MinValue;

            EvaluateCandidate(new Vector3(-edgeX, 0f, sign * deepZ), reference, ref best, ref bestCost);
            EvaluateCandidate(new Vector3(edgeX, 0f, sign * deepZ), reference, ref best, ref bestCost);
            EvaluateCandidate(new Vector3(-edgeX, 0f, sign * shortZ), reference, ref best, ref bestCost);
            EvaluateCandidate(new Vector3(edgeX, 0f, sign * shortZ), reference, ref best, ref bestCost);
            EvaluateCandidate(new Vector3(-edgeX, 0f, sign * midZ), reference, ref best, ref bestCost);
            EvaluateCandidate(new Vector3(edgeX, 0f, sign * midZ), reference, ref best, ref bestCost);

            return best;
        }

        /// <summary>
        /// Ước lượng thời gian (giây) đối thủ cần để chạy từ <paramref name="from"/> tới <paramref name="to"/>,
        /// có tính việc chạy tiến/lùi chậm hơn chạy ngang.
        /// </summary>
        /// <param name="from">Vị trí hiện tại của đối thủ.</param>
        /// <param name="to">Điểm cần chạy tới.</param>
        internal static float EstimateTravelTime(Vector3 from, Vector3 to)
        {
            float dx = Mathf.Abs(to.x - from.x);
            float dz = Mathf.Abs(to.z - from.z) * DepthCostFactor;
            return Mathf.Sqrt(dx * dx + dz * dz) / AssumedOpponentSpeed;
        }

        private static void EvaluateCandidate(Vector3 candidate, Vector3 reference, ref Vector3 best,
                                              ref float bestCost)
        {
            float cost = EstimateTravelTime(reference, candidate);
            if (cost <= bestCost) return;

            bestCost = cost;
            best = candidate;
        }

        /// <summary>
        /// True nếu đối thủ đang đứng ngoài biên sân (xét trên mặt phẳng XZ).
        /// </summary>
        /// <param name="playerPosition">Vị trí đối thủ.</param>
        /// <param name="courtWidth">Bề ngang toàn sân theo trục X.</param>
        /// <param name="courtDepth">Chiều sâu một nửa sân theo trục Z.</param>
        public static bool IsPlayerOutsideCourt(Vector3 playerPosition, float courtWidth, float courtDepth)
        {
            return Mathf.Abs(playerPosition.x) > courtWidth * 0.5f || Mathf.Abs(playerPosition.z) > courtDepth;
        }

        /// <summary>
        /// Kẹp một vị trí về điểm hợp lệ gần nhất bên trong một nửa sân: nằm trong biên ngang,
        /// nằm trong nửa sân được chỉ định và không lấn vào kitchen.
        /// </summary>
        /// <param name="playerPosition">Vị trí cần kẹp.</param>
        /// <param name="paddedCourtWidth">Bề ngang toàn sân đã trừ đệm biên (mét).</param>
        /// <param name="paddedCourtDepth">Chiều sâu nửa sân đã trừ đệm baseline (mét).</param>
        /// <param name="isPositiveCourt">Nửa sân đích (true = <c>z &gt; 0</c>).</param>
        /// <param name="kitchenPadding">Khoảng cách tối thiểu tới lưới (mét).</param>
        public static Vector3 GetNearestInsideCourtPosition(Vector3 playerPosition, float paddedCourtWidth,
                                                            float paddedCourtDepth, bool isPositiveCourt,
                                                            float kitchenPadding)
        {
            float halfWidth = Mathf.Max(0.1f, paddedCourtWidth * 0.5f);
            float maxDepth = Mathf.Max(kitchenPadding + 0.1f, paddedCourtDepth);
            float sign = isPositiveCourt ? 1f : -1f;

            float x = Mathf.Clamp(playerPosition.x, -halfWidth, halfWidth);
            float depth = Mathf.Clamp(Mathf.Abs(playerPosition.z), kitchenPadding, maxDepth);

            return new Vector3(x, 0f, sign * depth);
        }

        // ------------------------------------------------------------------
        // Chọn loại cú đánh
        // ------------------------------------------------------------------

        /// <summary>
        /// Chọn loại cú đánh hợp lý nhất từ tình huống hiện tại.
        /// <para><b>Thứ tự ưu tiên:</b></para>
        /// <list type="number">
        /// <item><description>Bóng chưa nảy và AI đứng ngoài kitchen → <see cref="ShotType.Volley"/>.</description></item>
        /// <item><description>Cả AI lẫn điểm rơi đều sát lưới → <see cref="ShotType.Dink"/> (bóng thấp)
        /// hoặc <see cref="ShotType.DropShot"/>.</description></item>
        /// <item><description>Điểm rơi ngắn (ngay sau vạch kitchen) → <see cref="ShotType.DropShot"/>.</description></item>
        /// <item><description>Bóng thấp mà AI bị đẩy sâu cuối sân → <see cref="ShotType.Lob"/> để lấy lại thế trận.</description></item>
        /// <item><description>Còn lại → <see cref="ShotType.Groundstroke"/>.</description></item>
        /// </list>
        /// </summary>
        /// <param name="aiPosition">Vị trí AI (world space).</param>
        /// <param name="destination">Điểm rơi đã chọn.</param>
        /// <param name="ballController">Quả bóng (có thể <c>null</c>).</param>
        /// <param name="netHeight">Chiều cao lưới (mét).</param>
        /// <param name="profile">Chỉ số runtime của AI (có thể <c>null</c>).</param>
        /// <param name="isAggressive">True nếu AI đang chơi tấn công (ưu tiên cú đánh dứt điểm).</param>
        /// <param name="kitchenDepth">Chiều sâu kitchen tính từ lưới (mét).</param>
        public static ShotType SelectBestShot(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                              float netHeight, PlayerProfileProperties profile,
                                              bool isAggressive = false, float kitchenDepth = 2f)
        {
            kitchenDepth = Mathf.Max(0.5f, kitchenDepth);
            netHeight = netHeight > 0f ? netHeight : 0.86f;

            float aiDepth = Mathf.Abs(aiPosition.z);
            float destinationDepth = Mathf.Abs(destination.z);

            bool aiAtNet = aiDepth <= kitchenDepth * 1.35f;
            bool destinationShort = destinationDepth <= kitchenDepth * 1.35f;

            float ballHeight = ballController != null ? ballController.transform.position.y : netHeight;
            bool ballHasBounced = ballController == null || ballController.bounceCount > 0;
            bool ballAboveNet = ballHeight > netHeight * 1.15f;
            bool ballBelowNet = ballHeight < netHeight * 0.9f;

            float power = profile != null ? Mathf.Clamp01(profile.shotPower) : 0.5f;

            // 1. Chặn bóng trước khi nảy — chỉ hợp lệ khi đứng ngoài kitchen.
            if (!ballHasBounced && aiDepth > kitchenDepth) return ShotType.Volley;

            // 2. Đôi bên cùng ở trên lưới: đấu dink.
            if (aiAtNet && destinationShort) return ballBelowNet ? ShotType.Dink : ShotType.DropShot;

            // 3. Nhắm ngắn từ xa: bỏ nhỏ.
            if (destinationShort) return ShotType.DropShot;

            // 4. Bị ép sâu với quả bóng thấp: câu bổng lấy lại vị trí.
            if (ballBelowNet && aiDepth > kitchenDepth * 2f && !(isAggressive && power >= 0.6f))
            {
                return ShotType.Lob;
            }

            // 5. Bóng cao trên lưới + lực tốt: đè bóng thành cú đánh nhanh.
            if (isAggressive && ballAboveNet && power >= 0.5f) return ShotType.Groundstroke;

            return ShotType.Groundstroke;
        }

        /// <summary>True nếu vị trí nằm trong dải giữa sân theo trục X (±<see cref="CenterHalfWidth"/> mét).</summary>
        /// <param name="position">Vị trí cần xét (world space).</param>
        public static bool IsInCenter(Vector3 position)
        {
            return Mathf.Abs(position.x) <= CenterHalfWidth;
        }
    }
}
