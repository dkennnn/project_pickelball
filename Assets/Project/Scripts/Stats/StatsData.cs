using System;
using System.Text;

namespace Pickleball
{
    /// <summary>
    /// Bộ chỉ số thống kê thô của MỘT người chơi trong một trận đấu.
    ///
    /// <para><b>Lưu ý quan trọng</b>: <see cref="shortestRallyTime"/> được khởi tạo bằng
    /// <see cref="float.MaxValue"/> chứ không phải 0. Nếu để 0 thì mọi phép so sánh
    /// <c>time &lt; shortestRallyTime</c> sẽ không bao giờ đúng và chỉ số này vĩnh viễn bằng 0.
    /// Vì vậy <see cref="Reset"/> và constructor đều phải đưa nó về <see cref="float.MaxValue"/>.</para>
    ///
    /// <para>Thứ tự field được giữ NGUYÊN theo bản gốc (Il2Cpp dump) để dữ liệu serialize cũ
    /// vẫn đọc được và để so khớp khi đối chiếu ngược.</para>
    /// </summary>
    [Serializable]
    public class StatsData
    {
        /// <summary>Tổng thời gian thi đấu (giây), chỉ tính lúc bóng sống hoặc đang chờ giao.</summary>
        public float gameDuration;

        /// <summary>Số cú đánh nhiều nhất mà người chơi này thực hiện trong MỘT pha bóng.</summary>
        public int maxShotsInRally;

        /// <summary>Số cú đánh trong pha bóng đang diễn ra (bộ đếm tạm, reset sau mỗi pha).</summary>
        public int shotsInRally;

        /// <summary>Số cú volley (đánh bóng khi chưa để nó nảy).</summary>
        public int volleys;

        /// <summary>Số lỗi giao bóng (vào lưới, ngoài ô giao, volley khi giao, hết giờ giao).</summary>
        public int misserves;

        /// <summary>Số lần để bóng nảy hai lần bên phần sân mình (không đỡ được).</summary>
        public int drops;

        /// <summary>Số lần đánh bóng ra ngoài sân / vào lưới trong pha bóng.</summary>
        public int outOfBounds;

        /// <summary>Số lỗi volley trong khu kitchen.</summary>
        public int kitchen;

        /// <summary>Tốc độ cú đánh nhanh nhất (m/s).</summary>
        public float fastestShot;

        /// <summary>Pha bóng dài nhất (giây).</summary>
        public float longestRallyTime;

        /// <summary>
        /// Pha bóng thắng nhanh nhất (giây). Bằng <see cref="float.MaxValue"/> khi chưa có dữ liệu.
        /// </summary>
        public float shortestRallyTime;

        /// <summary>Tổng số cú đánh trong cả trận.</summary>
        public int totalShots;

        /// <summary>Số điểm thắng trực tiếp bằng cú đánh ăn điểm (đối thủ không phạm lỗi).</summary>
        public int outrightWinners;

        /// <summary>Tạo bộ chỉ số rỗng — đã ở trạng thái sau <see cref="Reset"/>.</summary>
        public StatsData()
        {
            Reset();
        }

        /// <summary>
        /// Đưa mọi chỉ số về mặc định.
        /// <see cref="shortestRallyTime"/> về <see cref="float.MaxValue"/> để phép so sánh min
        /// hoạt động ngay ở lần ghi đầu tiên; mọi chỉ số còn lại về 0.
        /// </summary>
        public void Reset()
        {
            gameDuration = 0f;
            maxShotsInRally = 0;
            shotsInRally = 0;
            volleys = 0;
            misserves = 0;
            drops = 0;
            outOfBounds = 0;
            kitchen = 0;
            fastestShot = 0f;
            longestRallyTime = 0f;
            shortestRallyTime = float.MaxValue;
            totalShots = 0;
            outrightWinners = 0;
        }

        /// <summary>Bản in nhiều dòng, dễ đọc trong Console log.</summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"  gameDuration      : {gameDuration:F2}s");
            sb.AppendLine($"  totalShots        : {totalShots}");
            sb.AppendLine($"  shotsInRally      : {shotsInRally}");
            sb.AppendLine($"  maxShotsInRally   : {maxShotsInRally}");
            sb.AppendLine($"  volleys           : {volleys}");
            sb.AppendLine($"  misserves         : {misserves}");
            sb.AppendLine($"  drops             : {drops}");
            sb.AppendLine($"  outOfBounds       : {outOfBounds}");
            sb.AppendLine($"  kitchen           : {kitchen}");
            sb.AppendLine($"  outrightWinners   : {outrightWinners}");
            sb.AppendLine($"  fastestShot       : {fastestShot:F2} m/s");
            sb.AppendLine($"  longestRallyTime  : {longestRallyTime:F2}s");
            sb.AppendLine(shortestRallyTime >= float.MaxValue
                ? "  shortestRallyTime : (chưa có)"
                : $"  shortestRallyTime : {shortestRallyTime:F2}s");
            return sb.ToString();
        }
    }
}
