using System;
using System.Collections.Generic;
using System.Text;

namespace Pickleball
{
    /// <summary>
    /// Tập hợp thống kê của cả trận: mỗi người chơi một <see cref="PlayerStatsData"/>.
    ///
    /// <para>Đây là POCO thuần (không phụ thuộc Unity runtime) nên có thể test bằng EditMode test
    /// mà không cần scene. <see cref="StatsManager"/> chỉ làm nhiệm vụ dịch sự kiện gameplay
    /// thành các lời gọi API ở đây.</para>
    ///
    /// <para>Quy ước ghi chỉ số:</para>
    /// <list type="bullet">
    /// <item><description><c>Increment*</c> — cộng dồn cho MỘT người chơi.</description></item>
    /// <item><description><c>Set*</c> có tham số <c>playerId</c> — ghi kỷ lục cho MỘT người chơi
    /// (chỉ ghi khi tốt hơn giá trị đang có).</description></item>
    /// <item><description><c>SetLongestRallyTime</c>, <c>SetGameDuration</c>, <c>SetMaxShotsInRally</c>,
    /// <c>ResetShotsInRally</c> — áp cho MỌI người chơi, vì pha bóng và thời lượng trận là của cả hai bên.</description></item>
    /// </list>
    /// </summary>
    [Serializable]
    public class GameStatsData
    {
        /// <summary>Danh sách thống kê theo từng người chơi.</summary>
        public List<PlayerStatsData> Stats;

        /// <summary>Tạo bảng thống kê rỗng.</summary>
        public GameStatsData()
        {
            Stats = new List<PlayerStatsData>();
        }

        // ------------------------------------------------------------------
        // Increment — cộng dồn cho một người chơi
        // ------------------------------------------------------------------

        /// <summary>Cộng thêm <paramref name="count"/> cú volley cho người chơi.</summary>
        /// <param name="playerId">TeamID của người chơi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementVolleys(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.volleys += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> lỗi giao bóng cho người chơi.</summary>
        /// <param name="playerId">TeamID của người chơi phạm lỗi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementMisserves(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.misserves += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> lần để bóng nảy hai lần (không đỡ được).</summary>
        /// <param name="playerId">TeamID của người chơi để rơi bóng.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementDrops(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.drops += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> lần đánh bóng ra ngoài sân.</summary>
        /// <param name="playerId">TeamID của người chơi phạm lỗi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementOutOfBounds(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.outOfBounds += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> lỗi volley trong kitchen.</summary>
        /// <param name="playerId">TeamID của người chơi phạm lỗi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementKitchen(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.kitchen += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> vào tổng số cú đánh cả trận.</summary>
        /// <param name="playerId">TeamID của người chơi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementTotalShots(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.totalShots += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> điểm thắng trực tiếp (outright winner).</summary>
        /// <param name="playerId">TeamID của người chơi ghi điểm.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementOutrightWinners(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.outrightWinners += count;
        }

        /// <summary>Cộng thêm <paramref name="count"/> cú đánh vào bộ đếm của pha bóng đang diễn ra.</summary>
        /// <param name="playerId">TeamID của người chơi.</param>
        /// <param name="count">Số lượng cộng thêm.</param>
        public void IncrementShotsInRally(string playerId, int count)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            stats.StatsData.shotsInRally += count;
        }

        // ------------------------------------------------------------------
        // Set — ghi kỷ lục
        // ------------------------------------------------------------------

        /// <summary>
        /// Ghi tốc độ cú đánh nhanh nhất. CHỈ ghi khi <paramref name="speed"/> lớn hơn kỷ lục hiện tại.
        /// </summary>
        /// <param name="playerId">TeamID của người chơi.</param>
        /// <param name="speed">Tốc độ bóng ngay sau cú đánh (m/s).</param>
        public void SetFastestShot(string playerId, float speed)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            if (speed > stats.StatsData.fastestShot) stats.StatsData.fastestShot = speed;
        }

        /// <summary>
        /// Ghi pha bóng dài nhất cho MỌI người chơi — một pha bóng là của cả hai bên nên
        /// cả hai đều nhận cùng con số. Chỉ ghi khi dài hơn kỷ lục hiện tại.
        /// </summary>
        /// <param name="time">Thời lượng pha bóng vừa kết thúc (giây).</param>
        public void SetLongestRallyTime(float time)
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                StatsData data = Stats[i]?.StatsData;
                if (data == null) continue;
                if (time > data.longestRallyTime) data.longestRallyTime = time;
            }
        }

        /// <summary>
        /// Ghi pha bóng thắng nhanh nhất cho người THẮNG pha bóng.
        /// CHỈ ghi khi <paramref name="time"/> nhỏ hơn kỷ lục hiện tại — nhờ
        /// <see cref="StatsData.shortestRallyTime"/> khởi tạo bằng <see cref="float.MaxValue"/>
        /// nên lần ghi đầu tiên luôn thành công.
        /// </summary>
        /// <param name="playerId">TeamID của người thắng pha bóng.</param>
        /// <param name="time">Thời lượng pha bóng (giây).</param>
        public void SetShortestRallyTime(string playerId, float time)
        {
            PlayerStatsData stats = GetPlayerStats(playerId);
            if (stats == null) return;
            if (time < stats.StatsData.shortestRallyTime) stats.StatsData.shortestRallyTime = time;
        }

        /// <summary>Ghi tổng thời lượng trận đấu cho MỌI người chơi.</summary>
        /// <param name="duration">Thời lượng trận (giây).</param>
        public void SetGameDuration(float duration)
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                StatsData data = Stats[i]?.StatsData;
                if (data == null) continue;
                data.gameDuration = duration;
            }
        }

        /// <summary>
        /// Chốt kỷ lục số cú đánh trong một pha bóng cho MỌI người chơi:
        /// <c>maxShotsInRally = Max(maxShotsInRally, shotsInRally)</c>.
        /// Gọi ngay trước <see cref="ResetShotsInRally"/> khi pha bóng kết thúc.
        /// </summary>
        public void SetMaxShotsInRally()
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                StatsData data = Stats[i]?.StatsData;
                if (data == null) continue;
                if (data.shotsInRally > data.maxShotsInRally) data.maxShotsInRally = data.shotsInRally;
            }
        }

        /// <summary>Xoá bộ đếm cú đánh của pha bóng cho MỌI người chơi (chuẩn bị pha bóng mới).</summary>
        public void ResetShotsInRally()
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                StatsData data = Stats[i]?.StatsData;
                if (data == null) continue;
                data.shotsInRally = 0;
            }
        }

        // ------------------------------------------------------------------
        // Quản lý danh sách người chơi
        // ------------------------------------------------------------------

        /// <summary>
        /// Đăng ký danh sách người chơi tham gia trận. ID trùng hoặc rỗng được bỏ qua,
        /// nên gọi nhiều lần cũng không tạo bản ghi thừa.
        /// </summary>
        /// <param name="playerIds">Mảng TeamID cần thêm.</param>
        public void AddPlayer(string[] playerIds)
        {
            if (playerIds == null) return;

            for (int i = 0; i < playerIds.Length; i++)
            {
                // GetPlayerStats tự tạo bản ghi nếu chưa có và bỏ qua id rỗng.
                GetPlayerStats(playerIds[i]);
            }
        }

        /// <summary>
        /// Lấy bản ghi thống kê của một người chơi; tự tạo mới nếu chưa tồn tại.
        /// Trả về <c>null</c> khi <paramref name="playerId"/> null/rỗng.
        /// </summary>
        /// <param name="playerId">TeamID cần tra cứu.</param>
        public PlayerStatsData GetPlayerStats(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            if (Stats == null) Stats = new List<PlayerStatsData>();

            for (int i = 0; i < Stats.Count; i++)
            {
                PlayerStatsData entry = Stats[i];
                if (entry != null && entry.playerId == playerId) return entry;
            }

            PlayerStatsData created = new PlayerStatsData(playerId);
            Stats.Add(created);
            return created;
        }

        /// <summary>
        /// Đưa mọi chỉ số của mọi người chơi về mặc định nhưng GIỮ nguyên danh sách người chơi
        /// (và giữ nguyên instance <see cref="PlayerStatsData"/> để tham chiếu bên ngoài không hỏng).
        /// </summary>
        public void Reset()
        {
            if (Stats == null)
            {
                Stats = new List<PlayerStatsData>();
                return;
            }

            for (int i = 0; i < Stats.Count; i++)
            {
                Stats[i]?.StatsData?.Reset();
            }
        }

        /// <summary>Bản in nhiều dòng của toàn bộ bảng thống kê, dùng cho Console log.</summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Game Stats ===");

            if (Stats == null || Stats.Count == 0)
            {
                sb.AppendLine("(chưa có người chơi nào)");
                return sb.ToString();
            }

            for (int i = 0; i < Stats.Count; i++)
            {
                PlayerStatsData entry = Stats[i];
                if (entry == null) continue;

                sb.AppendLine($"[{entry.playerId}]");
                sb.Append(entry.StatsData);
            }

            return sb.ToString();
        }
    }
}
