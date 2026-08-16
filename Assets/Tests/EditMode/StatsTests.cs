using NUnit.Framework;
using Pickleball;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm chứng <see cref="GameStatsData"/> ở mức POCO thuần — không cần scene, không cần
    /// MonoBehaviour, nên chạy được trong EditMode.
    ///
    /// <para>Điểm dễ sai nhất được nhắm tới ở đây là <c>shortestRallyTime</c>: nếu
    /// <see cref="StatsData.Reset"/> đưa nó về 0 thay vì <see cref="float.MaxValue"/> thì
    /// phép so sánh <c>time &lt; shortestRallyTime</c> sẽ không bao giờ đúng và chỉ số
    /// vĩnh viễn bằng 0.</para>
    /// </summary>
    public class StatsTests
    {
        private const string P1 = "P1";
        private const string P2 = "P2";

        private GameStatsData stats;

        [SetUp]
        public void SetUp()
        {
            stats = new GameStatsData();
            stats.AddPlayer(new[] { P1, P2 });
        }

        // ------------------------------------------------------------------
        // AddPlayer / GetPlayerStats
        // ------------------------------------------------------------------

        [Test]
        public void AddPlayer_TaoDungSoEntry()
        {
            Assert.AreEqual(2, stats.Stats.Count, "AddPlayer phải tạo đúng 2 bản ghi.");
            Assert.IsNotNull(stats.GetPlayerStats(P1));
            Assert.IsNotNull(stats.GetPlayerStats(P2));
        }

        [Test]
        public void AddPlayer_TrungID_KhongTaoBanGhiThua()
        {
            stats.AddPlayer(new[] { P1, P2 });
            Assert.AreEqual(2, stats.Stats.Count, "ID trùng không được tạo bản ghi mới.");
        }

        [Test]
        public void GetPlayerStats_TraCungInstanceKhiGoiLai()
        {
            PlayerStatsData first = stats.GetPlayerStats(P1);
            PlayerStatsData second = stats.GetPlayerStats(P1);

            Assert.AreSame(first, second, "GetPlayerStats phải trả về cùng một instance.");
            Assert.AreSame(first.StatsData, second.StatsData);
        }

        [Test]
        public void GetPlayerStats_IDMoi_TuTaoBanGhi()
        {
            PlayerStatsData created = stats.GetPlayerStats("P3");

            Assert.IsNotNull(created);
            Assert.AreEqual("P3", created.playerId);
            Assert.AreEqual(3, stats.Stats.Count);
        }

        // ------------------------------------------------------------------
        // SetFastestShot
        // ------------------------------------------------------------------

        [Test]
        public void SetFastestShot_ChiGhiKhiLonHon()
        {
            StatsData data = stats.GetPlayerStats(P1).StatsData;

            stats.SetFastestShot(P1, 10f);
            Assert.AreEqual(10f, data.fastestShot, 0.0001f);

            stats.SetFastestShot(P1, 6f);
            Assert.AreEqual(10f, data.fastestShot, 0.0001f, "Giá trị nhỏ hơn không được ghi đè.");

            stats.SetFastestShot(P1, 14.5f);
            Assert.AreEqual(14.5f, data.fastestShot, 0.0001f, "Giá trị lớn hơn phải ghi đè.");

            Assert.AreEqual(0f, stats.GetPlayerStats(P2).StatsData.fastestShot, 0.0001f,
                "Chỉ số của người chơi khác không được đụng tới.");
        }

        // ------------------------------------------------------------------
        // SetShortestRallyTime / SetLongestRallyTime
        // ------------------------------------------------------------------

        [Test]
        public void StatsData_MoiTao_ShortestRallyTimeLaMaxValue()
        {
            Assert.AreEqual(float.MaxValue, stats.GetPlayerStats(P1).StatsData.shortestRallyTime,
                "shortestRallyTime phải khởi tạo bằng float.MaxValue.");
        }

        [Test]
        public void SetShortestRallyTime_LanDauLuonGhiDuoc()
        {
            StatsData data = stats.GetPlayerStats(P1).StatsData;

            stats.SetShortestRallyTime(P1, 7.25f);

            Assert.AreEqual(7.25f, data.shortestRallyTime, 0.0001f,
                "Lần ghi đầu tiên phải thành công nhờ giá trị khởi tạo float.MaxValue.");
        }

        [Test]
        public void SetShortestRallyTime_ChiGhiKhiNhoHon()
        {
            StatsData data = stats.GetPlayerStats(P1).StatsData;

            stats.SetShortestRallyTime(P1, 5f);
            stats.SetShortestRallyTime(P1, 9f);
            Assert.AreEqual(5f, data.shortestRallyTime, 0.0001f, "Giá trị lớn hơn không được ghi đè.");

            stats.SetShortestRallyTime(P1, 2.5f);
            Assert.AreEqual(2.5f, data.shortestRallyTime, 0.0001f, "Giá trị nhỏ hơn phải ghi đè.");

            Assert.AreEqual(float.MaxValue, stats.GetPlayerStats(P2).StatsData.shortestRallyTime,
                "Chỉ người thắng pha bóng mới được ghi shortestRallyTime.");
        }

        [Test]
        public void SetShortestRallyTime_SauReset_LaiGhiDuocLanDau()
        {
            StatsData data = stats.GetPlayerStats(P1).StatsData;

            stats.SetShortestRallyTime(P1, 4f);
            stats.Reset();

            Assert.AreEqual(float.MaxValue, data.shortestRallyTime,
                "Reset() phải đưa shortestRallyTime về float.MaxValue.");

            stats.SetShortestRallyTime(P1, 12f);
            Assert.AreEqual(12f, data.shortestRallyTime, 0.0001f,
                "Sau Reset() lần ghi đầu tiên phải thành công trở lại.");
        }

        [Test]
        public void SetLongestRallyTime_GhiChoMoiNguoiChoi()
        {
            stats.SetLongestRallyTime(8f);

            Assert.AreEqual(8f, stats.GetPlayerStats(P1).StatsData.longestRallyTime, 0.0001f);
            Assert.AreEqual(8f, stats.GetPlayerStats(P2).StatsData.longestRallyTime, 0.0001f);

            stats.SetLongestRallyTime(3f);
            Assert.AreEqual(8f, stats.GetPlayerStats(P1).StatsData.longestRallyTime, 0.0001f,
                "Pha bóng ngắn hơn không được ghi đè kỷ lục.");

            stats.SetLongestRallyTime(11f);
            Assert.AreEqual(11f, stats.GetPlayerStats(P2).StatsData.longestRallyTime, 0.0001f);
        }

        // ------------------------------------------------------------------
        // SetMaxShotsInRally / ResetShotsInRally
        // ------------------------------------------------------------------

        [Test]
        public void MaxShotsInRally_QuaBaPhaBongLienTiep()
        {
            StatsData p1 = stats.GetPlayerStats(P1).StatsData;
            StatsData p2 = stats.GetPlayerStats(P2).StatsData;

            // Pha bóng 1: P1 đánh 3, P2 đánh 2.
            stats.IncrementShotsInRally(P1, 1);
            stats.IncrementShotsInRally(P1, 1);
            stats.IncrementShotsInRally(P1, 1);
            stats.IncrementShotsInRally(P2, 1);
            stats.IncrementShotsInRally(P2, 1);
            stats.SetMaxShotsInRally();
            stats.ResetShotsInRally();

            Assert.AreEqual(3, p1.maxShotsInRally);
            Assert.AreEqual(2, p2.maxShotsInRally);
            Assert.AreEqual(0, p1.shotsInRally, "ResetShotsInRally phải xoá bộ đếm tạm.");
            Assert.AreEqual(0, p2.shotsInRally);

            // Pha bóng 2: ngắn hơn -> kỷ lục không đổi.
            stats.IncrementShotsInRally(P1, 1);
            stats.IncrementShotsInRally(P2, 1);
            stats.SetMaxShotsInRally();
            stats.ResetShotsInRally();

            Assert.AreEqual(3, p1.maxShotsInRally, "Pha bóng ngắn hơn không được hạ kỷ lục.");
            Assert.AreEqual(2, p2.maxShotsInRally);

            // Pha bóng 3: P2 đánh 5 -> kỷ lục của P2 tăng, của P1 giữ nguyên.
            stats.IncrementShotsInRally(P2, 5);
            stats.SetMaxShotsInRally();
            stats.ResetShotsInRally();

            Assert.AreEqual(3, p1.maxShotsInRally);
            Assert.AreEqual(5, p2.maxShotsInRally);
            Assert.AreEqual(0, p2.shotsInRally);
        }

        // ------------------------------------------------------------------
        // Increment tổng quát + Reset
        // ------------------------------------------------------------------

        [Test]
        public void Increment_CongDungChiSoChoDungNguoiChoi()
        {
            stats.IncrementVolleys(P1, 2);
            stats.IncrementMisserves(P1, 1);
            stats.IncrementDrops(P1, 3);
            stats.IncrementOutOfBounds(P1, 4);
            stats.IncrementKitchen(P1, 5);
            stats.IncrementTotalShots(P1, 6);
            stats.IncrementOutrightWinners(P1, 7);

            StatsData p1 = stats.GetPlayerStats(P1).StatsData;
            Assert.AreEqual(2, p1.volleys);
            Assert.AreEqual(1, p1.misserves);
            Assert.AreEqual(3, p1.drops);
            Assert.AreEqual(4, p1.outOfBounds);
            Assert.AreEqual(5, p1.kitchen);
            Assert.AreEqual(6, p1.totalShots);
            Assert.AreEqual(7, p1.outrightWinners);

            StatsData p2 = stats.GetPlayerStats(P2).StatsData;
            Assert.AreEqual(0, p2.volleys);
            Assert.AreEqual(0, p2.totalShots);
        }

        [Test]
        public void Reset_DuaMoiChiSoVeMacDinh()
        {
            stats.IncrementVolleys(P1, 2);
            stats.IncrementMisserves(P1, 3);
            stats.IncrementDrops(P1, 4);
            stats.IncrementOutOfBounds(P1, 5);
            stats.IncrementKitchen(P1, 6);
            stats.IncrementTotalShots(P1, 7);
            stats.IncrementOutrightWinners(P1, 8);
            stats.IncrementShotsInRally(P1, 9);
            stats.SetMaxShotsInRally();
            stats.SetFastestShot(P1, 15f);
            stats.SetLongestRallyTime(20f);
            stats.SetShortestRallyTime(P1, 1.5f);
            stats.SetGameDuration(300f);

            stats.Reset();

            Assert.AreEqual(2, stats.Stats.Count, "Reset() phải giữ nguyên danh sách người chơi.");

            foreach (PlayerStatsData entry in stats.Stats)
            {
                StatsData data = entry.StatsData;
                Assert.AreEqual(0f, data.gameDuration, 0.0001f);
                Assert.AreEqual(0, data.maxShotsInRally);
                Assert.AreEqual(0, data.shotsInRally);
                Assert.AreEqual(0, data.volleys);
                Assert.AreEqual(0, data.misserves);
                Assert.AreEqual(0, data.drops);
                Assert.AreEqual(0, data.outOfBounds);
                Assert.AreEqual(0, data.kitchen);
                Assert.AreEqual(0f, data.fastestShot, 0.0001f);
                Assert.AreEqual(0f, data.longestRallyTime, 0.0001f);
                Assert.AreEqual(float.MaxValue, data.shortestRallyTime);
                Assert.AreEqual(0, data.totalShots);
                Assert.AreEqual(0, data.outrightWinners);
            }
        }

        [Test]
        public void SetGameDuration_GhiChoMoiNguoiChoi()
        {
            stats.SetGameDuration(123.5f);

            Assert.AreEqual(123.5f, stats.GetPlayerStats(P1).StatsData.gameDuration, 0.0001f);
            Assert.AreEqual(123.5f, stats.GetPlayerStats(P2).StatsData.gameDuration, 0.0001f);
        }

        [Test]
        public void API_VoiIDRong_KhongNemException()
        {
            Assert.IsNull(stats.GetPlayerStats(null));
            Assert.IsNull(stats.GetPlayerStats(string.Empty));

            Assert.DoesNotThrow(() =>
            {
                stats.IncrementVolleys(null, 1);
                stats.IncrementTotalShots(string.Empty, 1);
                stats.SetFastestShot(null, 10f);
                stats.SetShortestRallyTime(null, 1f);
                stats.AddPlayer(null);
            });

            Assert.AreEqual(2, stats.Stats.Count, "ID rỗng không được tạo bản ghi.");
        }
    }
}
