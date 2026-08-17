using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Pickleball.Tests
{
    /// <summary>
    /// Smoke test cho scene greybox <c>GreyboxMatch</c>: scene nạp được, mọi manager có mặt,
    /// máy trạng thái trận đấu chạy, không sinh exception và quả bóng nảy được trên mặt sân.
    ///
    /// <para>Scene phải được sinh trước bằng <c>Pickleball/Build Greybox Match Scene</c>
    /// (builder tự thêm scene vào Build Settings ở index 0).</para>
    /// </summary>
    public class MatchSmokeTests
    {
        private const string SceneName = "GreyboxMatch";

        /// <summary>Log Error/Exception/Assert bắt được trong cửa sổ quan sát của test.</summary>
        private readonly List<string> capturedErrors = new List<string>();

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            capturedErrors.Add(type + ": " + condition);
        }

        /// <summary>Nạp lại scene sạch trước mỗi test và chờ một frame cho Awake/Start chạy xong.</summary>
        [UnitySetUp]
        public IEnumerator SetUpScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Scene_Loads_And_AllManagers_Exist()
        {
            yield return null;

            Assert.IsTrue(SceneManager.GetActiveScene().name == SceneName, "Scene đang hoạt động không phải GreyboxMatch.");

            Assert.IsNotNull(Object.FindAnyObjectByType<Court>(), "Thiếu Court trong scene.");
            Assert.IsTrue(GameManager.HasInstance, "Thiếu GameManager.Instance.");
            Assert.IsTrue(ScoreManager.HasInstance, "Thiếu ScoreManager.Instance.");
            Assert.IsTrue(InputManager.HasInstance, "Thiếu InputManager.Instance.");
            Assert.IsTrue(PhysicsTrajectoryHandler.HasInstance, "Thiếu PhysicsTrajectoryHandler.Instance.");
            Assert.IsTrue(BallController.HasInstance, "Thiếu BallController.Instance.");

            Assert.IsNotNull(GameManager.Instance.court, "GameManager.court chưa được gán.");
            Assert.IsNotNull(GameManager.Instance.settings, "GameManager.settings chưa được gán.");
            Assert.IsNotNull(GameManager.Instance.gameMessages, "GameManager.gameMessages chưa được gán.");
            Assert.IsNotNull(BallController.Instance.simulationBallPrefab, "BallController.simulationBallPrefab chưa được gán.");

            // Hai tay vợt phải đăng ký được vào trận.
            Assert.IsNotNull(GameManager.Instance.GetParticipant("P1"), "Người chơi P1 chưa đăng ký participant.");
            Assert.IsNotNull(GameManager.Instance.GetParticipant("P2"), "AI P2 chưa đăng ký participant.");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Match_Advances_Past_PreServe()
        {
            Assert.IsTrue(GameManager.HasInstance, "Thiếu GameManager.Instance.");

            GameManager.Instance.StartMatch();
            Assert.AreEqual(GameState.PreServe, GameManager.Instance.currentState,
                "StartMatch phải đưa trận về PreServe.");

            float elapsed = 0f;
            while (elapsed < 5f && GameManager.Instance.currentState == GameState.PreServe)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreNotEqual(GameState.PreServe, GameManager.Instance.currentState,
                "Trận đấu kẹt ở PreServe quá 5 giây (PreServe -> Serving đáng lẽ mất 1 giây).");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator No_Exceptions_During_10_Seconds()
        {
            Assert.IsTrue(GameManager.HasInstance, "Thiếu GameManager.Instance.");

            // Khoá quyền giao cho AI: người chơi không có input trong test nên nếu P1 giao bóng thì
            // pha bóng chỉ dừng ở ServeTimeout và toàn bộ đường đánh bóng / dự đoán quỹ đạo
            // sẽ KHÔNG bao giờ được chạy. Khoá cho P2 để AI thật sự giao và mở một pha bóng.
            GameManager.Instance.LockServerToTeam("P2");
            GameManager.Instance.StartMatch();

            // KHÔNG dùng LogAssert.NoUnexpectedReceived(): hàm đó fail với MỌI log chưa được "expect",
            // kể cả warning lành tính của engine. Cụ thể, BallController.HoldBall/ResetBall
            // (file runtime, không được sửa) gán rb.linearVelocity KHI rb đã kinematic, khiến Unity 6
            // in "Setting linear velocity of a kinematic body is not supported." một cách không xác định
            // theo thời điểm -> test sẽ flaky. Ở đây ta bắt đúng thứ cần bắt: Error / Exception / Assert.
            capturedErrors.Clear();
            Application.logMessageReceived += OnLogMessageReceived;

            float elapsed = 0f;
            try
            {
                while (elapsed < 10f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            Assert.IsEmpty(capturedErrors,
                "Có log lỗi/exception trong 10 giây chạy trận: " + string.Join(" || ", capturedErrors));

            // AI phải thật sự đánh được ít nhất một quả trong 10 giây (serve delay tối đa 3 giây),
            // nghĩa là BallController + PhysicsTrajectoryHandler đã chạy trọn một cú đánh.
            Assert.IsTrue(BallController.HasInstance, "Thiếu BallController.Instance.");
            Assert.AreEqual(PlayerType.AI, BallController.Instance.lastHitby,
                "AI chưa thực hiện được cú giao nào trong 10 giây.");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Ball_Falls_And_Bounces()
        {
            Assert.IsTrue(BallController.HasInstance, "Thiếu BallController.Instance.");
            Assert.IsTrue(GameManager.HasInstance, "Thiếu GameManager.Instance.");

            // Cô lập quả bóng khỏi trận đấu.
            //
            // Test này TỪNG FLAKY: chỉ set GameOver rồi thả bóng ngay là không đủ. GameManager có
            // FSM tự chạy (PreServe 1s -> Serving, PointScored 1.5s -> PreServe) hẹn bằng
            // DelayedAction; một lượt chuyển đã hẹn từ trước vẫn có thể nổ sau đó, đẩy tay vợt vào
            // PrepareServe -> HoldBall. Bóng bị parent vào ballHoldPoint và đặt isKinematic = true
            // nên không bao giờ rơi, bounceCount đứng ở 0.
            //
            // Cách chữa: tắt hẳn các tay vợt trước, rồi CHỜ tới khi bóng thật sự tự do mới đo.
            foreach (BasePlayerController player in
                     Object.FindObjectsByType<BasePlayerController>(FindObjectsSortMode.None))
            {
                player.gameObject.SetActive(false);
            }

            GameManager.Instance.SetGameState(GameState.GameOver);
            yield return null;

            BallController ball = BallController.Instance;

            // Chờ bóng thoát khỏi tay người chơi (tối đa 2 giây) trước khi bắt đầu phép đo.
            float settle = 0f;
            while (settle < 2f && (ball.isBallHeld || ball.transform.parent != ball.initialParent))
            {
                ball.UnholdBall();
                settle += Time.unscaledDeltaTime;
                yield return null;
            }

            ball.ResetBall(new Vector3(0f, 2f, -3f));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, ball.bounceCount, "ResetBall phải đưa bounceCount về 0.");
            Assert.IsFalse(ball.isBallHeld, "Bóng vẫn đang bị giữ — phép đo lần nảy sẽ vô nghĩa.");
            Assert.IsFalse(ball.rb == null || ball.rb.isKinematic,
                "Rigidbody của bóng phải ở trạng thái động sau ResetBall.");

            float elapsed = 0f;
            while (elapsed < 5f && ball.bounceCount < 1)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            string diag =
                $"state={GameManager.Instance.currentState}, IsInPlay={ball.IsInPlay}, " +
                $"isBallHeld={ball.isBallHeld}, kinematic={(ball.rb != null ? ball.rb.isKinematic.ToString() : "no rb")}, " +
                $"parent={(ball.transform.parent != null ? ball.transform.parent.name : "none")}, " +
                $"pos={ball.transform.position}, timeScale={Time.timeScale}";

            Assert.GreaterOrEqual(ball.bounceCount, 1,
                "Bóng thả từ y = 2 phải chạm mặt sân (tag Ground) ít nhất một lần trong 5 giây. " + diag);
            Assert.Less(ball.transform.position.y, 2f, "Bóng không rơi — kiểm tra Rigidbody/gravity.");
        }
    }
}
