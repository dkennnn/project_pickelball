using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sửa hàng loạt các lỗi hiển thị **máy móc** trên prefab UI: TMP thiếu font, Canvas thiếu
    /// GraphicRaycaster, node có Graphic nhưng RectTransform kích thước 0.
    /// </summary>
    /// <remarks>
    /// Nguyên tắc: chỉ sửa những thứ chắc chắn là LỖI DỰNG, không sửa những thứ có thể là Ý ĐỒ
    /// của bản gốc. Cụ thể, công cụ KHÔNG động vào <c>color.a = 0</c> và KHÔNG bật node
    /// <c>activeSelf = false</c> — hai thứ đó chỉ được liệt kê ra báo cáo.
    /// <para>
    /// Công cụ GHI ĐÈ prefab trên đĩa và không có Undo (Undo chỉ áp dụng cho object trong scene).
    /// Nên commit/backup trước khi chạy, rồi chạy <c>Pickleball/UI/Check Prefab Visibility</c>
    /// trước và sau để so số liệu.
    /// </para>
    /// </remarks>
    public static class UIVisibilityFixer
    {
        // ─── Cờ bật/tắt từng loại sửa ────────────────────────────────────────────────────────
        // Tắt cờ nào thì loại sửa đó chỉ được đếm/ghi báo cáo chứ không ghi vào prefab.

        /// <summary>Gán font mặc định cho TextMeshProUGUI đang có <c>font == null</c>.</summary>
        private const bool FixMissingTmpFont = true;

        /// <summary>Thêm <see cref="GraphicRaycaster"/> cho Canvas còn thiếu.</summary>
        private const bool FixMissingGraphicRaycaster = true;

        /// <summary>Đặt lại kích thước cho node có Image/TMP nhưng RectTransform rỗng. ĐÂY LÀ PHỎNG ĐOÁN.</summary>
        private const bool FixZeroSizeGraphicNodes = true;

        /// <summary>Liệt kê Image có alpha ≈ 0 (không bao giờ tự sửa — bản gốc cố ý để vậy).</summary>
        private const bool ReportAlphaZeroImages = true;

        /// <summary>Liệt kê node đang tắt (không bao giờ tự bật — layout gốc cố ý tắt).</summary>
        private const bool ReportInactiveNodes = true;

        // ─────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Kích thước gán cho node rỗng có Graphic. Là con số bịa ra, chỉ để nhìn thấy node.</summary>
        public static readonly Vector2 ZeroSizeFallback = new Vector2(100f, 100f);

        /// <summary>Tên file báo cáo ghi trong thư mục ui_layout.</summary>
        public const string ReportFileName = "visibility_fix_report.md";

        /// <summary>Tên font dự phòng khi TMP_Settings chưa có font mặc định.</summary>
        private const string FallbackFontName = "LiberationSans SDF";

        private sealed class FixResult
        {
            public string PrefabName;
            public string PrefabPath;
            public int FontsFixed;
            public int RaycastersAdded;
            public int SizesFixed;
            public int AlphaZeroFound;
            public int InactiveFound;
            public bool Saved;
            public string Error;
            public readonly List<string> SizeLog = new List<string>();
            public readonly List<string> FontLog = new List<string>();
            public readonly List<string> RaycasterLog = new List<string>();
            public readonly List<string> AlphaZeroLog = new List<string>();
            public readonly List<string> InactiveLog = new List<string>();

            public int TotalFixes => FontsFixed + RaycastersAdded + SizesFixed;
        }

        /// <summary>
        /// Duyệt mọi prefab trong <c>Assets/Project/Prefabs/UI/</c>, áp các bản sửa an toàn
        /// theo cờ ở đầu file, ghi đè prefab và xuất <c>ui_layout/visibility_fix_report.md</c>.
        /// </summary>
        [MenuItem("Pickleball/UI/Fix Common Visibility Issues")]
        public static void FixAllPrefabs()
        {
            List<string> paths = UIPreviewChecker.CollectPrefabPaths();
            if (paths.Count == 0)
            {
                Debug.LogError($"[UIVisibilityFixer] Không tìm thấy prefab nào trong {UILayoutPaths.UIPrefabRoot}.");
                return;
            }

            TMP_FontAsset font = ResolveDefaultFont();
            if (FixMissingTmpFont && font == null)
            {
                Debug.LogWarning("[UIVisibilityFixer] Không tìm được font TMP mặc định (TMP_Settings.defaultFontAsset = null " +
                                 $"và không thấy '{FallbackFontName}'). Chạy Window > TextMeshPro > Import TMP Essential Resources " +
                                 "rồi thử lại — nếu không, phần chữ vẫn sẽ không hiện.");
            }

            // Batchmode không có ai bấm nút: DisplayDialog trả về false nên fixer sẽ im lặng
            // không làm gì và ta tưởng "không có gì để sửa". Bỏ qua hộp thoại khi chạy tự động.
            bool proceed = Application.isBatchMode || EditorUtility.DisplayDialog(
                "Sửa lỗi hiển thị UI",
                $"Sẽ GHI ĐÈ tối đa {paths.Count} prefab trong {UILayoutPaths.UIPrefabRoot}.\n\n" +
                "Các bản sửa được bật:\n" +
                $"  • TMP thiếu font → {(FixMissingTmpFont ? (font != null ? "gán '" + font.name + "'" : "BỎ QUA (không có font)") : "tắt")}\n" +
                $"  • Canvas thiếu GraphicRaycaster → {(FixMissingGraphicRaycaster ? "thêm" : "tắt")}\n" +
                $"  • Node có Graphic nhưng rect rỗng → {(FixZeroSizeGraphicNodes ? $"đặt sizeDelta = ({ZeroSizeFallback.x:0}, {ZeroSizeFallback.y:0})" : "tắt")}\n\n" +
                "KHÔNG sửa: alpha = 0 và node đang tắt (bản gốc cố ý).\n\n" +
                "Thao tác này KHÔNG có Undo. Nên backup/commit trước.",
                "Sửa ngay", "Huỷ");
            if (!proceed) return;

            var results = new List<FixResult>(paths.Count);
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Fix Common Visibility Issues",
                        Path.GetFileNameWithoutExtension(paths[i]) + $" ({i + 1}/{paths.Count})",
                        (i + 1) / (float)paths.Count);
                    results.Add(FixPrefab(paths[i], font));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(BuildConsoleSummary(results, font));
            WriteReport(results, font);
        }

        private static FixResult FixPrefab(string prefabPath, TMP_FontAsset font)
        {
            var result = new FixResult
            {
                PrefabPath = prefabPath,
                PrefabName = Path.GetFileNameWithoutExtension(prefabPath)
            };

            GameObject root;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            catch (Exception e)
            {
                result.Error = "không nạp được prefab — " + e.Message;
                Debug.LogError($"[UIVisibilityFixer] {result.PrefabName}: {result.Error}");
                return result;
            }

            if (root == null)
            {
                result.Error = "LoadPrefabContents trả về null";
                return result;
            }

            try
            {
                Transform rootTransform = root.transform;

                if (FixMissingTmpFont && font != null)
                {
                    foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (text == null || text.font != null) continue;
                        text.font = font;
                        result.FontsFixed++;
                        result.FontLog.Add(UIPreviewChecker.GetNodePath(text.transform, rootTransform));
                    }
                }

                if (FixMissingGraphicRaycaster)
                {
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    {
                        if (canvas == null || canvas.GetComponent<GraphicRaycaster>() != null) continue;
                        if (canvas.gameObject.AddComponent<GraphicRaycaster>() == null) continue;
                        result.RaycastersAdded++;
                        result.RaycasterLog.Add(UIPreviewChecker.GetNodePath(canvas.transform, rootTransform));
                    }
                }

                if (FixZeroSizeGraphicNodes) FixZeroSizeNodes(root, rootTransform, result);

                if (ReportAlphaZeroImages)
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image == null || image.color.a > UIPreviewChecker.AlphaEpsilon) continue;
                        result.AlphaZeroFound++;
                        result.AlphaZeroLog.Add(UIPreviewChecker.GetNodePath(image.transform, rootTransform));
                    }
                }

                if (ReportInactiveNodes)
                {
                    foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (node == null || node.gameObject.activeSelf) continue;
                        result.InactiveFound++;
                        result.InactiveLog.Add(UIPreviewChecker.GetNodePath(node, rootTransform));
                    }
                }

                if (result.TotalFixes > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
                    result.Saved = saved;
                    if (!saved)
                    {
                        result.Error = "ghi prefab thất bại";
                        Debug.LogError($"[UIVisibilityFixer] Ghi prefab thất bại: {prefabPath}");
                    }
                }
            }
            catch (Exception e)
            {
                result.Error = "lỗi khi sửa — " + e.Message;
                Debug.LogError($"[UIVisibilityFixer] {result.PrefabName}: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return result;
        }

        /// <summary>
        /// Đặt lại kích thước cho node vừa có Image/TMP vừa có RectTransform rỗng
        /// (<c>sizeDelta ≈ 0</c> và <c>anchorMin ≈ anchorMax</c>).
        /// </summary>
        /// <remarks>
        /// Đây là PHỎNG ĐOÁN, không phải dữ liệu gốc — bản gốc không lộ kích thước thật của các node này
        /// (chúng được script hoặc layout của bản gốc set lúc runtime). Vì vậy mỗi lần sửa đều được ghi log.
        /// Cố ý bỏ qua: node có Canvas (Canvas tự cấp kích thước) và node đang bị LayoutGroup /
        /// ContentSizeFitter / AspectRatioFitter điều khiển (để 0 ở đó là bình thường, sửa vào chỉ gây nhiễu).
        /// </remarks>
        private static void FixZeroSizeNodes(GameObject root, Transform rootTransform, FixResult result)
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == null || !UIPreviewChecker.IsZeroSize(rect)) continue;
                if (rect.GetComponent<Graphic>() == null) continue;
                if (rect.GetComponent<Canvas>() != null) continue;
                if (UIPreviewChecker.IsSizeDriven(rect)) continue;

                // Ưu tiên kích thước GỐC của sprite thay vì con số bịa.
                //
                // Phần lớn node rỗng này là Image đã được gắn sprite art gốc; kích thước thật của
                // sprite là dữ liệu CÓ CĂN CỨ, sát bản gốc hơn nhiều so với một hằng số tuỳ ý.
                // Chỉ khi không có sprite mới rơi về ZeroSizeFallback.
                Vector2 size = ZeroSizeFallback;
                string source = "phỏng đoán";

                Image image = rect.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    Rect spriteRect = image.sprite.rect;
                    if (spriteRect.width > 1f && spriteRect.height > 1f)
                    {
                        size = new Vector2(spriteRect.width, spriteRect.height);
                        source = "kích thước gốc của sprite " + image.sprite.name;
                    }
                }

                rect.sizeDelta = size;
                result.SizesFixed++;

                string path = UIPreviewChecker.GetNodePath(rect, rootTransform);
                result.SizeLog.Add($"{path} → ({size.x:0}, {size.y:0}) [{source}]");
                Debug.Log($"[UIVisibilityFixer] {result.PrefabName} → {path}: " +
                          $"sizeDelta (0,0) → ({size.x:0},{size.y:0}) [{source}].");
            }
        }

        /// <summary>
        /// Tìm font mặc định để gán cho TMP thiếu font: ưu tiên
        /// <see cref="TMP_Settings.defaultFontAsset"/>, sau đó tìm <c>LiberationSans SDF</c> trong project.
        /// </summary>
        /// <returns>Font tìm được, hoặc null nếu project chưa có font TMP nào phù hợp.</returns>
        public static TMP_FontAsset ResolveDefaultFont()
        {
            try
            {
                if (Resources.Load<TMP_Settings>("TMP Settings") != null && TMP_Settings.defaultFontAsset != null)
                    return TMP_Settings.defaultFontAsset;
            }
            catch
            {
                // TMP Essentials chưa import — rơi xuống nhánh tìm tay bên dưới.
            }

            TMP_FontAsset partial = null;
            foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null) continue;

                if (string.Equals(asset.name, FallbackFontName, StringComparison.OrdinalIgnoreCase)) return asset;
                if (partial == null && asset.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0)
                    partial = asset;
            }

            return partial;
        }

        private static string BuildConsoleSummary(List<FixResult> results, TMP_FontAsset font)
        {
            int touched = results.Count(r => r.Saved);
            var sb = new StringBuilder();
            sb.AppendLine($"[UIVisibilityFixer] Đã quét {results.Count} prefab, ghi lại {touched} prefab.");
            sb.AppendLine($"  - TMP thiếu font đã gán '{(font != null ? font.name : "—")}': {results.Sum(r => r.FontsFixed)} chỗ / {results.Count(r => r.FontsFixed > 0)} prefab");
            sb.AppendLine($"  - GraphicRaycaster đã thêm: {results.Sum(r => r.RaycastersAdded)} chỗ / {results.Count(r => r.RaycastersAdded > 0)} prefab");
            sb.AppendLine($"  - Node rỗng đã đặt kích thước (PHỎNG ĐOÁN): {results.Sum(r => r.SizesFixed)} chỗ / {results.Count(r => r.SizesFixed > 0)} prefab");
            sb.AppendLine($"  - Image alpha ≈ 0 (chỉ liệt kê, KHÔNG sửa): {results.Sum(r => r.AlphaZeroFound)} chỗ");
            sb.AppendLine($"  - Node đang tắt (chỉ liệt kê, KHÔNG bật): {results.Sum(r => r.InactiveFound)} chỗ");

            List<FixResult> failed = results.Where(r => r.Error != null).ToList();
            if (failed.Count > 0)
            {
                sb.AppendLine($"  - LỖI ở {failed.Count} prefab:");
                foreach (FixResult r in failed.Take(10)) sb.AppendLine($"      {r.PrefabName}: {r.Error}");
            }

            sb.Append("  → Chạy 'Pickleball/UI/Check Prefab Visibility' để đo lại VisibleGraphicCount.");
            return sb.ToString();
        }

        private static void WriteReport(List<FixResult> results, TMP_FontAsset font)
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot(false);
            if (layoutRoot == null)
            {
                Debug.LogWarning("[UIVisibilityFixer] Không tìm thấy thư mục ui_layout — bỏ qua bước ghi báo cáo.");
                return;
            }

            string reportPath = Path.Combine(layoutRoot, ReportFileName);
            try
            {
                File.WriteAllText(reportPath, BuildMarkdown(results, font), new UTF8Encoding(false));
                Debug.Log($"[UIVisibilityFixer] Đã ghi {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIVisibilityFixer] Không ghi được báo cáo: {e.Message}");
            }
        }

        private static string BuildMarkdown(List<FixResult> results, TMP_FontAsset font)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo sửa lỗi hiển thị UI");
            sb.AppendLine();
            sb.AppendLine($"Sinh tự động bởi `Pickleball/UI/Fix Common Visibility Issues` lúc {DateTime.Now:yyyy-MM-dd HH:mm}.");
            sb.AppendLine();
            sb.AppendLine("| Loại | Trạng thái cờ | Số chỗ | Số prefab |");
            sb.AppendLine("|---|---|---:|---:|");
            sb.AppendLine($"| TMP thiếu font → gán `{(font != null ? font.name : "KHÔNG TÌM ĐƯỢC FONT")}` | {Flag(FixMissingTmpFont)} | {results.Sum(r => r.FontsFixed)} | {results.Count(r => r.FontsFixed > 0)} |");
            sb.AppendLine($"| Canvas thiếu GraphicRaycaster → thêm | {Flag(FixMissingGraphicRaycaster)} | {results.Sum(r => r.RaycastersAdded)} | {results.Count(r => r.RaycastersAdded > 0)} |");
            sb.AppendLine($"| Node rỗng có Graphic → `sizeDelta = ({ZeroSizeFallback.x:0}, {ZeroSizeFallback.y:0})` | {Flag(FixZeroSizeGraphicNodes)} | {results.Sum(r => r.SizesFixed)} | {results.Count(r => r.SizesFixed > 0)} |");
            sb.AppendLine($"| Image alpha ≈ 0 | **chỉ liệt kê** | {results.Sum(r => r.AlphaZeroFound)} | {results.Count(r => r.AlphaZeroFound > 0)} |");
            sb.AppendLine($"| Node `activeSelf = false` | **chỉ liệt kê** | {results.Sum(r => r.InactiveFound)} | {results.Count(r => r.InactiveFound > 0)} |");
            sb.AppendLine($"| Prefab đã ghi lại | | {results.Count(r => r.Saved)} | {results.Count} đã quét |");
            sb.AppendLine();

            sb.AppendLine("## 1. Những thứ CỐ Ý không tự sửa");
            sb.AppendLine();
            sb.AppendLine("- **`color.a = 0`**: bản gốc dùng Image trong suốt làm vùng bấm (raycast target) và làm");
            sb.AppendLine("  lớp nền cho hiệu ứng bật lên lúc runtime. Bật alpha lên sẽ sinh ra mảng màu không có");
            sb.AppendLine("  trong bản gốc — nên chỉ liệt kê ở mục 5.");
            sb.AppendLine("- **`activeSelf = false`**: layout gốc cố ý tắt (popup, trạng thái khác, phần thưởng ẩn…).");
            sb.AppendLine("  Bật hết lên thì màn hình chồng chéo sai hoàn toàn — chỉ liệt kê ở mục 6.");
            sb.AppendLine("- **Node rỗng do LayoutGroup / ContentSizeFitter / AspectRatioFitter điều khiển**:");
            sb.AppendLine("  `sizeDelta = 0` ở đó là đúng, kích thước được tính lúc runtime.");
            sb.AppendLine("- **Image thiếu sprite**: thuộc phạm vi `Pickleball/UI/Bind Sprites From Folder`, không phải công cụ này.");
            sb.AppendLine();

            sb.AppendLine("## 2. Node đã bị đặt lại kích thước — PHỎNG ĐOÁN, cần soát tay");
            sb.AppendLine();
            sb.AppendLine($"Bản gốc không lộ kích thước thật của các node này. Giá trị `({ZeroSizeFallback.x:0}, {ZeroSizeFallback.y:0})`");
            sb.AppendLine("chỉ để node hiện ra thay vì tàng hình — **phải đối chiếu `ui_layout/original_sprites_reference/`");
            sb.AppendLine("và ảnh chụp bản gốc rồi chỉnh lại tay**.");
            AppendLogSection(sb, results, r => r.SizeLog);

            sb.AppendLine();
            sb.AppendLine("## 3. TMP đã gán font");
            AppendLogSection(sb, results, r => r.FontLog);

            sb.AppendLine();
            sb.AppendLine("## 4. Canvas đã thêm GraphicRaycaster");
            AppendLogSection(sb, results, r => r.RaycasterLog);

            sb.AppendLine();
            sb.AppendLine("## 5. Image alpha ≈ 0 — KHÔNG sửa, chỉ liệt kê");
            AppendLogSection(sb, results, r => r.AlphaZeroLog);

            sb.AppendLine();
            sb.AppendLine("## 6. Node đang tắt — KHÔNG bật, chỉ liệt kê");
            AppendLogSection(sb, results, r => r.InactiveLog);

            List<FixResult> failed = results.Where(r => r.Error != null).ToList();
            if (failed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## 7. Prefab lỗi");
                sb.AppendLine();
                foreach (FixResult r in failed) sb.AppendLine($"- `{r.PrefabName}` — {r.Error} (`{r.PrefabPath}`)");
            }

            return sb.ToString();
        }

        private static void AppendLogSection(StringBuilder sb, List<FixResult> results, Func<FixResult, List<string>> selector)
        {
            sb.AppendLine();
            int total = results.Sum(r => selector(r).Count);
            if (total == 0)
            {
                sb.AppendLine("Không có mục nào.");
                return;
            }

            sb.AppendLine($"Tổng: **{total}**.");
            sb.AppendLine();
            foreach (FixResult r in results.OrderBy(x => x.PrefabName, StringComparer.OrdinalIgnoreCase))
            {
                List<string> items = selector(r);
                if (items.Count == 0) continue;

                sb.AppendLine($"### `{r.PrefabName}` ({items.Count})");
                sb.AppendLine();
                foreach (string item in items) sb.AppendLine($"- `{item}`");
                sb.AppendLine();
            }
        }

        private static string Flag(bool value) => value ? "bật" : "TẮT";
    }
}
