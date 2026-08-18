using NUnit.Framework;
using UnityEngine;

namespace Pickleball.Tests
{
    /// <summary>
    /// Ô giao bóng (Left/Right) chỉ được suy ra từ MỘT nguồn sự thật duy nhất: chẵn/lẻ điểm số
    /// của người giao.
    /// <para>
    /// Trước đây <c>HandlePointAndServeChange</c> còn gọi thêm một hàm <c>SwitchSide()</c> đảo
    /// tay ô giao khi người giao giữ được quyền giao. Hàm đó là mã chết: <c>SetGameState</c>
    /// vào <see cref="GameState.PreServe"/> ngay sau đó ghi đè bằng quy tắc chẵn/lẻ. Hai bên
    /// tình cờ cho cùng kết quả nên không ai phát hiện — nhưng có hai nguồn sự thật cho cùng
    /// một giá trị là mầm bệnh. Bộ test này khoá lại quy tắc duy nhất đó.
    /// </para>
    /// </summary>
    public class ServeSideRotationTests
    {
        private GameObject host;
        private GameManager gameManager;
        private ScoreManager scoreManager;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("ServeSideRotationTestHost");
            scoreManager = host.AddComponent<ScoreManager>();
            gameManager = host.AddComponent<GameManager>();

            gameManager.player1TeamID = "P1";
            gameManager.player2TeamID = "P2";
            scoreManager.player1TeamID = "P1";
            scoreManager.player2TeamID = "P2";
            scoreManager.ResetScore();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        [Test]
        public void DiemChan_ThiGiaoOPhai()
        {
            Assert.AreEqual(0, scoreManager.GetScore("P1"), "Điểm khởi đầu phải là 0.");
            Assert.AreEqual(ServeSide.Right, gameManager.GetPlayerSideForServe("P1"));
        }

        [Test]
        public void DiemLe_ThiGiaoOTrai()
        {
            scoreManager.AddScore("P1");
            Assert.AreEqual(1, scoreManager.GetScore("P1"));
            Assert.AreEqual(ServeSide.Left, gameManager.GetPlayerSideForServe("P1"));
        }

        [Test]
        public void GhiThemDiem_ThiODoiBen()
        {
            // Người giao giữ quyền giao và ghi điểm liên tiếp: ô giao phải đổi bên MỖI điểm,
            // vì chẵn/lẻ điểm của chính họ đảo sau mỗi lần cộng.
            var expected = new[] { ServeSide.Right, ServeSide.Left, ServeSide.Right, ServeSide.Left };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], gameManager.GetPlayerSideForServe("P1"),
                    $"Sau {i} điểm, ô giao phải là {expected[i]}.");
                scoreManager.AddScore("P1");
            }
        }

        [Test]
        public void MoiDoiCoOGiaoRieng_TinhTheoDiemCuaChinhMinh()
        {
            scoreManager.AddScore("P1");   // P1 = 1 (lẻ), P2 = 0 (chẵn)

            Assert.AreEqual(ServeSide.Left, gameManager.GetPlayerSideForServe("P1"),
                "Ô giao phải tính theo điểm của CHÍNH đội đó.");
            Assert.AreEqual(ServeSide.Right, gameManager.GetPlayerSideForServe("P2"),
                "Điểm của đội kia không được ảnh hưởng tới ô giao của đội này.");
        }

        [Test]
        public void KhongCoHamDoiTayOGiao_TranhHaiNguonSuThat()
        {
            // Khoá lại bằng phản chiếu: nếu ai đó thêm lại một hàm đảo tay ô giao thì test này
            // đỏ ngay, kèm lời giải thích vì sao không nên có.
            System.Reflection.MethodInfo legacy = typeof(GameManager).GetMethod("SwitchSide");

            Assert.IsNull(legacy,
                "GameManager không được có hàm đảo tay ô giao. Ô giao chỉ suy ra từ chẵn/lẻ "
                + "điểm của người giao (GetPlayerSideForServe) — thêm nguồn thứ hai là tạo mầm bệnh.");
        }
    }
}
