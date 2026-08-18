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
    /// Ghi lại diễn biến một trận: đổi trạng thái, lỗi luật, ai giao bóng, bóng có bị giữ không.
    /// Test LUÔN PASS — dùng để đọc log khi "vào trận mà không chơi được".
    /// </summary>
    public class MatchFlowDiagnosticTests
    {
        private const string SceneName = "GreyboxMatch";
        private readonly List<string> timeline = new List<string>();
        private float t0;

        private void Log(string s) => timeline.Add($"[{Time.time - t0,6:0.00}s] {s}");

        [UnityTest]
        public IEnumerator Diagnose_Match_Flow()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            t0 = Time.time;

            GameManager.OnGameStateChanged += s => Log($"STATE -> {s}");
            GameManager.OnRuleViolated += (rule, team, msg) => Log($"LOI LUAT: {rule} | pham loi: {team}");
            GameManager.OnMatchEnded += w => Log($"KET THUC, thang: {w}");
            BallController.OnBallHit += (team, dest, info) => Log($"DANH BONG boi {team}");

            if (!GameManager.HasInstance) { Debug.Log("KHONG co GameManager"); Assert.Pass(); yield break; }
            var gm = GameManager.Instance;

            Log($"truoc StartMatch: state={gm.currentState} P1={gm.player1TeamID} P2={gm.player2TeamID}");
            Log($"settings maxScore={gm.settings?.maxScore} winMargin={gm.settings?.winMargin} serveTimeout={gm.settings?.serveTimeout}");
            Log($"InputEnabled={(InputManager.HasInstance ? InputManager.Instance.InputEnabled.ToString() : "no instance")}");

            gm.StartMatch();

            float elapsed = 0f; string lastServer = null;
            while (elapsed < 16f)
            {
                string server = gm.GetCurrentServerTeamID();
                if (server != lastServer) { lastServer = server; Log($"NGUOI GIAO -> '{server}' serveSide={gm.currentServeSide}"); }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            var sb = new StringBuilder("=== DIEN BIEN TRAN DAU ===\n");
            foreach (string l in timeline) sb.AppendLine(l);
            if (ScoreManager.HasInstance)
                sb.AppendLine($"Diem: P1={ScoreManager.Instance.GetScore(gm.player1TeamID)} P2={ScoreManager.Instance.GetScore(gm.player2TeamID)}");
            if (BallController.HasInstance)
            {
                var b = BallController.Instance;
                sb.AppendLine($"Bong: InPlay={b.IsInPlay} held={b.isBallHeld} bounce={b.bounceCount} lastHit={b.lastHitby} pos={b.transform.position}");
            }
            foreach (BasePlayerController p in Object.FindObjectsByType<BasePlayerController>(FindObjectsSortMode.None))
                sb.AppendLine($"Player {p.teamID}: state={p.currentStateEnum} pos={p.transform.position} positiveCourt={p.IsPositiveCourt}");
            Debug.Log(sb.ToString());
            Assert.Pass();
        }
    }
}
