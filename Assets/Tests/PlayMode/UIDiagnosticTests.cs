using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using StarterKit.UIKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Pickleball.Tests
{
    /// <summary>
    /// Chẩn đoán trạng thái UI lúc chạy: canvas nào đang bật, thứ tự vẽ ra sao, và graphic nào
    /// đang phủ kín màn hình.
    /// <para>
    /// Test này LUÔN PASS — nó chỉ in báo cáo ra log. Dùng khi "bấm Play không thấy gì" hoặc
    /// "có thứ gì đó che hết màn hình": đọc log để biết chính xác thủ phạm thay vì đoán.
    /// </para>
    /// </summary>
    public class UIDiagnosticTests
    {
        private const string SceneName = "GreyboxMatch";

        [UnityTest]
        public IEnumerator Diagnose_UI_State()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            // Chờ UIController mở màn khởi đầu xong.
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            var report = new StringBuilder();
            report.AppendLine("=== CHẨN ĐOÁN UI ===");

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            report.AppendLine($"Tổng Canvas trong scene: {canvases.Length}");

            var enabled = new List<Canvas>();
            foreach (Canvas c in canvases)
            {
                if (c.enabled && c.gameObject.activeInHierarchy) enabled.Add(c);
            }

            enabled.Sort((a, b) => b.sortingOrder.CompareTo(a.sortingOrder));
            report.AppendLine($"Canvas ĐANG BẬT: {enabled.Count}  (sắp theo sortingOrder giảm dần — trên cùng vẽ sau, che các cái dưới)");

            foreach (Canvas c in enabled)
            {
                UIScreenBase screen = c.GetComponent<UIScreenBase>();
                string kind = screen == null ? "KHÔNG phải UIScreenBase" : $"{screen.screenType}, IsPopup={screen.IsPopup}";
                report.AppendLine($"  [order {c.sortingOrder,4}] {c.name}  ({kind})");

                // Tìm graphic phủ gần kín màn hình — thủ phạm điển hình của "che hết".
                foreach (Graphic g in c.GetComponentsInChildren<Graphic>(false))
                {
                    if (!g.enabled || g.color.a < 0.01f) continue;

                    Rect r = g.rectTransform.rect;
                    float coverage = (r.width * r.height) / (Screen.width * (float)Screen.height);
                    if (coverage < 0.6f) continue;

                    string sprite = g is Image img && img.sprite != null ? img.sprite.name : "(không sprite)";
                    report.AppendLine($"        PHỦ {coverage * 100f:0}% màn hình: {g.name} [{g.GetType().Name}] " +
                                      $"sprite={sprite} alpha={g.color.a:0.00} size={r.width:0}x{r.height:0}");
                }
            }

            report.AppendLine($"Screen.width x height = {Screen.width} x {Screen.height}");
            if (UIController.HasInstance)
            {
                report.AppendLine($"UIController.CurrentScreen = {UIController.Instance.CurrentScreen}");
            }
            else
            {
                report.AppendLine("UIController KHÔNG có Instance!");
            }

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            report.AppendLine($"Camera: {cameras.Length}");
            foreach (Camera cam in cameras)
            {
                report.AppendLine($"  {cam.name} enabled={cam.enabled} depth={cam.depth} " +
                                  $"clear={cam.clearFlags} culling=0x{cam.cullingMask:X}");
            }

            Debug.Log(report.ToString());
            Assert.Pass("Xem log để biết chi tiết.");
        }
    }
}
