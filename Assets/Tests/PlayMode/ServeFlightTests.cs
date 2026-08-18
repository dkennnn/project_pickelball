using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm chứng cú giao của NGƯỜI CHƠI thật sự bay sang ô giao chéo sân.
    /// <para>
    /// Đây là bài kiểm tra cho hai lỗi sai đơn vị từng làm mọi cú giao của người chơi phạm luật:
    /// <c>playerDepth</c> phải là toạ độ Z world (mét) chứ không phải giá trị chuẩn hoá 0..1, và
    /// <c>shotPowerFactor</c> phải là hệ số quanh 1.0 chứ không phải chỉ số <c>shotPower</c> thô.
    /// Sai một trong hai thì bóng chạm đất ngay quanh lưới. Đường đánh của AI KHÔNG đi qua chỗ
    /// này, nên chỉ test bằng cú giao của người chơi mới lộ ra lỗi.
    /// </para>
    /// </summary>
    public class ServeFlightTests
    {
        private const string SceneName = "GreyboxMatch";

        private readonly List<string> violations = new List<string>();

        [UnityTest]
        public IEnumerator Cu_Giao_Cua_Nguoi_Choi_Bay_Qua_Luoi_Va_Khong_Pham_Luat()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Assert.IsTrue(GameManager.HasInstance, "Scene phải có GameManager.");
            Assert.IsTrue(InputManager.HasInstance, "Scene phải có InputManager.");
            Assert.IsTrue(BallController.HasInstance, "Scene phải có BallController.");

            var gm = GameManager.Instance;
            GameManager.OnRuleViolated += (rule, team, msg) => violations.Add($"{rule} ({team})");

            gm.StartMatch();

            // Chờ tới lúc đội của người chơi thật sự được quyền giao và đang cầm bóng.
            PickleballPlayerController human = Object.FindAnyObjectByType<PickleballPlayerController>();
            Assert.IsNotNull(human, "Scene phải có PickleballPlayerController (người chơi).");

            float wait = 0f;
            while (wait < 8f && !(gm.currentState == GameState.Serving
                                  && gm.GetCurrentServerTeamID() == human.teamID
                                  && BallController.Instance.isBallHeld))
            {
                wait += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(GameState.Serving, gm.currentState,
                $"Sau {wait:0.0}s vẫn chưa tới lượt giao của người chơi. Lỗi đã ghi: {Dump()}");

            Vector3 serverPos = human.transform.position;
            float startZ = serverPos.z;
            violations.Clear();

            // Vuốt thẳng về phía sân đối phương, tốc độ giữa MinSwipeSpeed(3) và MaxSwipeSpeed(7).
            float towardOpponent = -Mathf.Sign(startZ);
            var swipe = new SwipeData
            {
                SwipePhase = SwipePhase.End,
                Direction = new Vector3(0f, 0f, towardOpponent),
                Velocity = 5f,
                length = 2f,
                StartPoint = serverPos,
                EndPoint = serverPos + new Vector3(0f, 0f, towardOpponent * 2f),
                SwipePath = new List<Vector3>()
            };
            InputManager.Instance.onSwipe?.Invoke(swipe);

            // Theo dõi quỹ đạo: bóng có rời tay, có qua lưới, rơi ở đâu.
            BallController ball = BallController.Instance;
            float peakY = 0f, crossedZ = float.NaN;
            bool everInPlay = false;
            float elapsed = 0f;

            // Điểm CHẠM ĐẤT ĐẦU TIÊN mới là thứ quyết định cú giao hợp lệ hay không.
            // Quãng đường xa nhất vô nghĩa: sau khi nảy hợp lệ bóng vẫn lăn tiếp rất xa.
            int startBounces = ball.bounceCount;
            Vector3 firstBounce = Vector3.positiveInfinity;

            while (elapsed < 6f)
            {
                Vector3 p = ball.transform.position;
                if (ball.IsInPlay) everInPlay = true;
                peakY = Mathf.Max(peakY, p.y);
                if (float.IsInfinity(firstBounce.x) && ball.bounceCount > startBounces) firstBounce = p;
                if (float.IsNaN(crossedZ) && Mathf.Sign(p.z) != Mathf.Sign(startZ) && Mathf.Abs(p.z) > 0.2f)
                {
                    crossedZ = p.z;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 final = ball.transform.position;
            var report = new StringBuilder("=== QUY DAO CU GIAO ===\n");
            report.AppendLine($"Nguoi giao {human.teamID} tai z={startZ:0.00}, vuot huong z={towardOpponent:+0;-0}");
            report.AppendLine($"Bong: dinh y={peakY:0.00}  qua luoi tai z={crossedZ:0.00}");
            report.AppendLine($"Cham dat lan dau tai {firstBounce}");
            CourtBounds box = gm.court.GetServeBoxBounds(!human.IsPositiveCourt, gm.currentServeSide);
            report.AppendLine($"O giao chéo can toi: tam={box.center} nua-kich-thuoc={box.extends}");
            report.AppendLine($"  => z hop le [{box.center.z - box.extends.y:0.00} .. {box.center.z + box.extends.y:0.00}]"
                              + $", x hop le [{box.center.x - box.extends.x:0.00} .. {box.center.x + box.extends.x:0.00}]");
            report.AppendLine($"Vi tri cuoi={final}  IsInPlay tung bat={everInPlay}");
            report.AppendLine($"Loi luat sau khi giao: {Dump()}");
            report.AppendLine($"Diem: P1={ScoreManager.Instance.GetScore(gm.player1TeamID)} P2={ScoreManager.Instance.GetScore(gm.player2TeamID)}");
            Debug.Log(report.ToString());

            Assert.IsTrue(everInPlay, $"Bóng chưa bao giờ vào cuộc — cú giao không bắn đi được. {report}");
            Assert.IsFalse(float.IsNaN(crossedZ),
                $"Bóng KHÔNG qua được lưới — đây chính là triệu chứng của lỗi sai đơn vị lực/độ sâu. {report}");
            Assert.IsFalse(violations.Contains("ServeInNet"), $"Cú giao rúc lưới. {report}");
            Assert.IsFalse(violations.Contains("ServeNotInArea"), $"Cú giao rơi sai ô giao. {report}");

            Assert.IsFalse(float.IsInfinity(firstBounce.x), $"Bóng không chạm đất lần nào trong 6s. {report}");
            Assert.That(firstBounce.z, Is.InRange(box.center.z - box.extends.y, box.center.z + box.extends.y),
                $"Cú giao chạm đất NGOÀI ô giao theo chiều dọc sân. {report}");
            Assert.That(firstBounce.x, Is.InRange(box.center.x - box.extends.x, box.center.x + box.extends.x),
                $"Cú giao chạm đất NGOÀI ô giao theo chiều ngang sân. {report}");
        }

        private string Dump() => violations.Count == 0 ? "(khong co)" : string.Join(", ", violations);
    }
}
