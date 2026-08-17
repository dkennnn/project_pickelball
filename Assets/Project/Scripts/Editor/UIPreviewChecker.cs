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
    /// Số liệu hiển thị đo được trên MỘT prefab UI.
    /// </summary>
    /// <remarks>
    /// Mọi con số ở đây là đo tĩnh trong Editor: prefab được nạp bằng
    /// <see cref="PrefabUtility.LoadPrefabContents"/> vào một scene ẩn, KHÔNG chạy script runtime.
    /// Vì vậy các script bật/tắt widget lúc chơi không ảnh hưởng kết quả — đúng ý đồ: ta muốn biết
    /// prefab "tự nó" hiện được bao nhiêu thứ.
    /// </remarks>
    public sealed class UIPrefabVisibility
    {
        /// <summary>Đường dẫn asset của prefab, kiểu <c>Assets/...</c>.</summary>
        public string PrefabPath;

        /// <summary>Tên prefab (không đuôi file).</summary>
        public string PrefabName;

        /// <summary>Prefab nằm trong thư mục Screens (true) hay Cells/khác (false).</summary>
        public bool IsScreen;

        /// <summary>Node gốc có đang bật hay không — tắt thì cả prefab vô hình.</summary>
        public bool RootActive = true;

        /// <summary>Tổng số Transform trong prefab (kể cả node đang tắt).</summary>
        public int TotalNodes;

        /// <summary>Số node có <c>activeSelf == false</c>.</summary>
        public int InactiveNodes;

        /// <summary>Tổng số component <see cref="Image"/>.</summary>
        public int ImageCount;

        /// <summary>Số <see cref="Image"/> đã có sprite.</summary>
        public int ImageWithSprite;

        /// <summary>Số <see cref="Image"/> chưa có sprite (thường còn <c>SpritePlaceholder</c>).</summary>
        public int ImageNoSprite;

        /// <summary>Số <see cref="Image"/> có <c>color.a &lt; 0.01</c> — trong suốt hoàn toàn.</summary>
        public int ImageAlphaZero;

        /// <summary>Số <see cref="Image"/> vừa không có sprite vừa đục (alpha ≈ 1) — nguy cơ ô trắng che màn.</summary>
        public int ImageWhiteBox;

        /// <summary>Tổng số component <see cref="TextMeshProUGUI"/>.</summary>
        public int TextCount;

        /// <summary>Số TMP có <c>font == null</c> — không render được ký tự nào.</summary>
        public int TextNoFont;

        /// <summary>Số TMP có chuỗi rỗng.</summary>
        public int TextEmpty;

        /// <summary>Số node có RectTransform kích thước 0 (theo giá trị đã serialize).</summary>
        public int ZeroSizeNodes;

        /// <summary>Số node kích thước 0 nhưng được LayoutGroup/Fitter điều khiển — bình thường, không phải lỗi.</summary>
        public int ZeroSizeDriven;

        /// <summary>Số component <see cref="Canvas"/> trong prefab.</summary>
        public int CanvasCount;

        /// <summary>Số <see cref="Canvas"/> thiếu <see cref="GraphicRaycaster"/> — không bấm được.</summary>
        public int CanvasWithoutRaycaster;

        /// <summary>
        /// Số <see cref="Graphic"/> thực sự nhìn thấy được — xem
        /// <see cref="UIPreviewChecker.CountVisibleGraphics"/> để biết định nghĩa chính xác.
        /// Đây là chỉ số quan trọng nhất của báo cáo.
        /// </summary>
        public int VisibleGraphicCount;

        /// <summary>Đường dẫn node kích thước 0 (không tính node do layout điều khiển).</summary>
        public readonly List<string> ZeroSizePaths = new List<string>();

        /// <summary>Đường dẫn node có TMP thiếu font.</summary>
        public readonly List<string> MissingFontPaths = new List<string>();

        /// <summary>Đường dẫn node có Image alpha ≈ 0.</summary>
        public readonly List<string> AlphaZeroPaths = new List<string>();

        /// <summary>Đường dẫn node đang tắt.</summary>
        public readonly List<string> InactivePaths = new List<string>();

        /// <summary>Đường dẫn node có Canvas nhưng thiếu GraphicRaycaster.</summary>
        public readonly List<string> MissingRaycasterPaths = new List<string>();

        /// <summary>Thông báo lỗi nếu không phân tích được prefab; null nếu bình thường.</summary>
        public string Error;
    }

    /// <summary>
    /// Đo tình trạng HIỂN THỊ của toàn bộ prefab UI trong <c>Assets/Project/Prefabs/UI/</c>
    /// và xuất báo cáo <c>ui_layout/visibility_report.md</c>.
    /// </summary>
    /// <remarks>
    /// Prefab sinh bằng code hỏng rất im lặng: không exception, không log, chỉ là màn hình trống.
    /// Công cụ này biến "trông có vẻ trống" thành con số đếm được, để so trước/sau mỗi lần sửa.
    /// Công cụ CHỈ ĐỌC — không ghi gì vào prefab. Muốn sửa thì dùng
    /// <c>Pickleball/UI/Fix Common Visibility Issues</c>.
    /// </remarks>
    public static class UIPreviewChecker
    {
        /// <summary>Ngưỡng coi alpha là 0 (trong suốt hoàn toàn).</summary>
        public const float AlphaEpsilon = 0.01f;

        /// <summary>Ngưỡng coi sizeDelta / khoảng cách anchor là 0.</summary>
        public const float SizeEpsilon = 0.01f;

        /// <summary>Bề rộng/cao tối thiểu (pixel) để một Graphic được tính là nhìn thấy được.</summary>
        public const float MinVisibleSize = 1f;

        /// <summary>Tên file báo cáo ghi trong thư mục ui_layout.</summary>
        public const string ReportFileName = "visibility_report.md";

        /// <summary>Số dòng tối đa in ra Console cho mỗi mục danh sách (báo cáo .md thì đầy đủ).</summary>
        private const int ConsoleListLimit = 12;

        /// <summary>
        /// Kích thước canvas giả định khi đo. Prefab nằm trên đĩa không có Canvas thật để cấp
        /// kích thước cho node gốc, nên node gốc kích thước 0 sẽ kéo theo mọi con stretch = 0.
        /// Ép node gốc về 1080x1920 (đúng referenceResolution của importer) trước khi đo.
        /// </summary>
        public static readonly Vector2 MeasureCanvasSize = new Vector2(1080f, 1920f);

        /// <summary>
        /// Quét mọi prefab UI, in bảng tổng hợp ra Console và ghi
        /// <c>ui_layout/visibility_report.md</c>.
        /// </summary>
        [MenuItem("Pickleball/UI/Check Prefab Visibility")]
        public static void CheckAllPrefabs()
        {
            List<string> paths = CollectPrefabPaths();
            if (paths.Count == 0)
            {
                Debug.LogError($"[UIPreviewChecker] Không tìm thấy prefab nào trong {UILayoutPaths.UIPrefabRoot}.");
                return;
            }

            var results = new List<UIPrefabVisibility>(paths.Count);
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Check Prefab Visibility",
                        Path.GetFileNameWithoutExtension(paths[i]) + $" ({i + 1}/{paths.Count})",
                        (i + 1) / (float)paths.Count);
                    results.Add(Analyze(paths[i]));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            results.Sort(CompareByVisibility);

            Debug.Log(BuildConsoleTable(results));
            WriteReport(results);
        }

        /// <summary>
        /// Liệt kê đường dẫn mọi prefab trong <see cref="UILayoutPaths.UIPrefabRoot"/> (cả Screens lẫn Cells).
        /// </summary>
        /// <returns>Danh sách đường dẫn asset, sắp theo thứ tự chữ cái.</returns>
        public static List<string> CollectPrefabPaths()
        {
            var paths = new List<string>();
            if (!AssetDatabase.IsValidFolder(UILayoutPaths.UIPrefabRoot)) return paths;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { UILayoutPaths.UIPrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path)) paths.Add(path);
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        /// <summary>
        /// Phân tích một prefab: nạp vào scene ẩn, đếm số liệu rồi huỷ. Không ghi gì lên đĩa.
        /// </summary>
        /// <param name="prefabPath">Đường dẫn asset kiểu <c>Assets/...</c>.</param>
        /// <returns>Số liệu hiển thị; trường <c>Error</c> khác null nếu nạp thất bại.</returns>
        public static UIPrefabVisibility Analyze(string prefabPath)
        {
            var stats = new UIPrefabVisibility
            {
                PrefabPath = prefabPath,
                PrefabName = Path.GetFileNameWithoutExtension(prefabPath),
                IsScreen = prefabPath.StartsWith(UILayoutPaths.ScreenPrefabFolder, StringComparison.OrdinalIgnoreCase)
            };

            GameObject root;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            catch (Exception e)
            {
                stats.Error = "không nạp được prefab — " + e.Message;
                return stats;
            }

            if (root == null)
            {
                stats.Error = "LoadPrefabContents trả về null";
                return stats;
            }

            try
            {
                stats.RootActive = root.activeSelf;

                // Thứ tự quan trọng: đếm cấu trúc TRƯỚC khi ép layout, vì LayoutGroup sẽ ghi đè
                // sizeDelta trong bộ nhớ và làm mất dấu các node kích thước 0 gốc.
                CollectStructure(root, stats);
                PrepareForMeasure(root);
                stats.VisibleGraphicCount = CountVisibleGraphics(root);
            }
            catch (Exception e)
            {
                stats.Error = "lỗi khi phân tích — " + e.Message;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return stats;
        }

        /// <summary>
        /// Đếm số <see cref="Graphic"/> mà người chơi thực sự nhìn thấy được.
        /// Một Graphic được tính khi thoả TẤT CẢ các điều kiện:
        /// <list type="number">
        /// <item><description><c>graphic.enabled == true</c>;</description></item>
        /// <item><description>chính node và MỌI node cha (tới tận node gốc prefab) đều <c>activeSelf == true</c>;</description></item>
        /// <item><description><c>graphic.color.a &gt; 0.01</c>;</description></item>
        /// <item><description><c>rectTransform.rect.width &gt; 1</c> VÀ <c>rect.height &gt; 1</c>.</description></item>
        /// </list>
        /// </summary>
        /// <param name="root">Node gốc của prefab đã nạp (nên gọi <see cref="PrepareForMeasure"/> trước).</param>
        /// <returns>Số Graphic nhìn thấy được.</returns>
        /// <remarks>
        /// Cố ý KHÔNG xét: font của TMP (TMP thiếu font vẫn được tính là "có ô chữ"),
        /// sprite của Image (Image trắng không sprite vẫn nhìn thấy — thậm chí là lỗi che màn),
        /// <see cref="CanvasGroup"/> alpha, Mask cắt ngoài khung, và thứ tự vẽ đè lên nhau.
        /// Vì vậy con số này là CẬN TRÊN của "người chơi nhìn thấy bao nhiêu thứ": bằng 0 thì
        /// chắc chắn hỏng, khác 0 thì chưa chắc đã đẹp.
        /// </remarks>
        public static int CountVisibleGraphics(GameObject root)
        {
            if (root == null) return 0;

            int visible = 0;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null || !graphic.enabled) continue;
                if (!IsChainActive(graphic.transform, root.transform)) continue;
                if (graphic.color.a <= AlphaEpsilon) continue;

                RectTransform rect = graphic.rectTransform;
                if (rect == null) continue;

                Rect r = rect.rect;
                if (r.width <= MinVisibleSize || r.height <= MinVisibleSize) continue;

                visible++;
            }

            return visible;
        }

        /// <summary>
        /// Chuẩn bị prefab đã nạp để đo kích thước: cấp cho node gốc một khung 1080x1920 nếu nó
        /// đang rỗng, ép localScale 0 về 1, rồi chạy layout một lần để LayoutGroup/Fitter tính ra
        /// kích thước thật của con.
        /// </summary>
        /// <param name="root">Node gốc prefab đã nạp bằng LoadPrefabContents.</param>
        /// <remarks>Chỉ đổi dữ liệu trong bộ nhớ; người gọi tuyệt đối không được lưu lại prefab sau bước này.</remarks>
        public static void PrepareForMeasure(GameObject root)
        {
            if (root == null) return;

            if (root.transform.localScale.sqrMagnitude < 1e-8f) root.transform.localScale = Vector3.one;

            var rect = root.transform as RectTransform;
            if (rect == null) return;

            if (rect.rect.width < MinVisibleSize || rect.rect.height < MinVisibleSize)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = MeasureCanvasSize;
            }

            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            catch (Exception e)
            {
                // Layout lỗi (ScrollRect thiếu content, Mask hỏng...) không được làm hỏng cả lượt quét.
                Debug.LogWarning($"[UIPreviewChecker] {root.name}: không dựng được layout để đo — {e.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra một RectTransform có bị coi là "kích thước 0" theo giá trị đã serialize hay không:
        /// <c>sizeDelta ≈ (0,0)</c> VÀ <c>anchorMin ≈ anchorMax</c> (không stretch nên không mượn được
        /// kích thước từ cha).
        /// </summary>
        /// <param name="rect">RectTransform cần kiểm tra.</param>
        public static bool IsZeroSize(RectTransform rect)
        {
            if (rect == null) return false;
            if (Mathf.Abs(rect.sizeDelta.x) > SizeEpsilon || Mathf.Abs(rect.sizeDelta.y) > SizeEpsilon) return false;
            return Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) <= SizeEpsilon
                   && Mathf.Abs(rect.anchorMax.y - rect.anchorMin.y) <= SizeEpsilon;
        }

        /// <summary>
        /// Kích thước của node này có đang do component khác điều khiển hay không
        /// (cha là LayoutGroup, hoặc chính nó có ContentSizeFitter/AspectRatioFitter).
        /// Node như vậy để sizeDelta = 0 là BÌNH THƯỜNG — không được coi là lỗi và không được sửa tay.
        /// </summary>
        /// <param name="node">Node cần kiểm tra.</param>
        public static bool IsSizeDriven(Transform node)
        {
            if (node == null) return false;
            if (node.GetComponent<ContentSizeFitter>() != null) return true;
            if (node.GetComponent<AspectRatioFitter>() != null) return true;

            Transform parent = node.parent;
            return parent != null && parent.GetComponent<LayoutGroup>() != null;
        }

        /// <summary>Đường dẫn node bên trong prefab, kiểu <c>Root/Parent/Node</c>.</summary>
        /// <param name="node">Node cần lấy đường dẫn.</param>
        /// <param name="root">Node gốc prefab.</param>
        public static string GetNodePath(Transform node, Transform root)
        {
            if (node == null) return string.Empty;

            var parts = new List<string>();
            Transform current = node;
            while (current != null)
            {
                parts.Add(current.name);
                if (current == root) break;
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool IsChainActive(Transform node, Transform root)
        {
            Transform current = node;
            while (current != null)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
                current = current.parent;
            }

            return true;
        }

        private static void CollectStructure(GameObject root, UIPrefabVisibility stats)
        {
            Transform rootTransform = root.transform;

            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node == null) continue;
                stats.TotalNodes++;

                if (!node.gameObject.activeSelf)
                {
                    stats.InactiveNodes++;
                    stats.InactivePaths.Add(GetNodePath(node, rootTransform));
                }

                if (node is RectTransform rect && IsZeroSize(rect))
                {
                    if (IsSizeDriven(node))
                    {
                        stats.ZeroSizeDriven++;
                    }
                    else
                    {
                        stats.ZeroSizeNodes++;
                        bool hasGraphic = node.GetComponent<Graphic>() != null;
                        stats.ZeroSizePaths.Add(GetNodePath(node, rootTransform) + (hasGraphic ? " (có Graphic)" : string.Empty));
                    }
                }
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image == null) continue;
                stats.ImageCount++;

                if (image.sprite != null) stats.ImageWithSprite++;
                else stats.ImageNoSprite++;

                if (image.color.a <= AlphaEpsilon)
                {
                    stats.ImageAlphaZero++;
                    stats.AlphaZeroPaths.Add(GetNodePath(image.transform, rootTransform));
                }
                else if (image.sprite == null && image.color.a > 0.9f)
                {
                    stats.ImageWhiteBox++;
                }
            }

            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null) continue;
                stats.TextCount++;

                if (text.font == null)
                {
                    stats.TextNoFont++;
                    stats.MissingFontPaths.Add(GetNodePath(text.transform, rootTransform));
                }

                if (string.IsNullOrEmpty(text.text)) stats.TextEmpty++;
            }

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas == null) continue;
                stats.CanvasCount++;

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    stats.CanvasWithoutRaycaster++;
                    stats.MissingRaycasterPaths.Add(GetNodePath(canvas.transform, rootTransform));
                }
            }
        }

        private static int CompareByVisibility(UIPrefabVisibility a, UIPrefabVisibility b)
        {
            int byVisible = a.VisibleGraphicCount.CompareTo(b.VisibleGraphicCount);
            if (byVisible != 0) return byVisible;
            return string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildConsoleTable(List<UIPrefabVisibility> results)
        {
            var sb = new StringBuilder();
            int broken = results.Count(r => r.VisibleGraphicCount == 0);
            sb.AppendLine($"[UIPreviewChecker] {results.Count} prefab — {results.Sum(r => r.VisibleGraphicCount)} Graphic nhìn thấy được, {broken} prefab KHÔNG hiện gì.");
            sb.AppendLine("Sắp theo VisibleGraphic tăng dần (tệ nhất lên đầu).");
            sb.AppendLine();
            sb.AppendLine(string.Format("{0,-26} {1,7} {2,6} {3,5} {4,5} {5,6} {6,5} {7,5} {8,7} {9,6} {10,6}",
                "Prefab", "Visible", "Node", "Tắt", "Img", "NoSpr", "A=0", "TMP", "NoFont", "Size0", "NoRC"));
            sb.AppendLine(new string('-', 100));

            foreach (UIPrefabVisibility r in results)
            {
                if (r.Error != null)
                {
                    sb.AppendLine($"{Trim(r.PrefabName, 26),-26}   LỖI: {r.Error}");
                    continue;
                }

                sb.AppendLine(string.Format("{0,-26} {1,7} {2,6} {3,5} {4,5} {5,6} {6,5} {7,5} {8,7} {9,6} {10,6}",
                    Trim(r.PrefabName, 26), r.VisibleGraphicCount, r.TotalNodes, r.InactiveNodes,
                    r.ImageCount, r.ImageNoSprite, r.ImageAlphaZero, r.TextCount, r.TextNoFont,
                    r.ZeroSizeNodes, r.CanvasWithoutRaycaster));
            }

            AppendConsoleList(sb, "Prefab không hiện gì", results.Where(r => r.Error == null && r.VisibleGraphicCount == 0).Select(r => r.PrefabName));
            AppendConsoleList(sb, "TMP thiếu font", results.SelectMany(r => r.MissingFontPaths.Select(p => r.PrefabName + " → " + p)));
            AppendConsoleList(sb, "Node kích thước 0", results.SelectMany(r => r.ZeroSizePaths.Select(p => r.PrefabName + " → " + p)));

            sb.AppendLine();
            sb.Append("→ Báo cáo đầy đủ: ui_layout/" + ReportFileName);
            return sb.ToString();
        }

        private static void AppendConsoleList(StringBuilder sb, string title, IEnumerable<string> items)
        {
            List<string> list = items.ToList();
            if (list.Count == 0) return;

            sb.AppendLine();
            sb.AppendLine($"{title} ({list.Count}):");
            foreach (string item in list.Take(ConsoleListLimit)) sb.AppendLine("  - " + item);
            if (list.Count > ConsoleListLimit) sb.AppendLine($"  … còn {list.Count - ConsoleListLimit} mục, xem file báo cáo.");
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max - 1) + "…";
        }

        private static void WriteReport(List<UIPrefabVisibility> results)
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogWarning("[UIPreviewChecker] Không tìm thấy thư mục ui_layout — chỉ in ra Console, không ghi báo cáo.");
                return;
            }

            string reportPath = Path.Combine(layoutRoot, ReportFileName);
            try
            {
                File.WriteAllText(reportPath, BuildMarkdown(results), new UTF8Encoding(false));
                Debug.Log($"[UIPreviewChecker] Đã ghi {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIPreviewChecker] Không ghi được báo cáo: {e.Message}");
            }
        }

        private static string BuildMarkdown(List<UIPrefabVisibility> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo hiển thị prefab UI");
            sb.AppendLine();
            sb.AppendLine($"Sinh tự động bởi `Pickleball/UI/Check Prefab Visibility` lúc {DateTime.Now:yyyy-MM-dd HH:mm}.");
            sb.AppendLine();
            sb.AppendLine("| Chỉ số | Giá trị |");
            sb.AppendLine("|---|---:|");
            sb.AppendLine($"| Prefab đã quét | {results.Count} |");
            sb.AppendLine($"| Prefab không hiện gì (`VisibleGraphicCount == 0`) | {results.Count(r => r.Error == null && r.VisibleGraphicCount == 0)} |");
            sb.AppendLine($"| Tổng Graphic nhìn thấy được | {results.Sum(r => r.VisibleGraphicCount)} |");
            sb.AppendLine($"| Tổng node | {results.Sum(r => r.TotalNodes)} |");
            sb.AppendLine($"| Node đang tắt | {results.Sum(r => r.InactiveNodes)} |");
            sb.AppendLine($"| Image / có sprite / thiếu sprite | {results.Sum(r => r.ImageCount)} / {results.Sum(r => r.ImageWithSprite)} / {results.Sum(r => r.ImageNoSprite)} |");
            sb.AppendLine($"| Image alpha ≈ 0 | {results.Sum(r => r.ImageAlphaZero)} |");
            sb.AppendLine($"| Image trắng đục không sprite (nguy cơ che màn) | {results.Sum(r => r.ImageWhiteBox)} |");
            sb.AppendLine($"| TMP / thiếu font / chuỗi rỗng | {results.Sum(r => r.TextCount)} / {results.Sum(r => r.TextNoFont)} / {results.Sum(r => r.TextEmpty)} |");
            sb.AppendLine($"| Node kích thước 0 (không do layout điều khiển) | {results.Sum(r => r.ZeroSizeNodes)} |");
            sb.AppendLine($"| Node kích thước 0 do LayoutGroup/Fitter điều khiển (bình thường) | {results.Sum(r => r.ZeroSizeDriven)} |");
            sb.AppendLine($"| Canvas thiếu GraphicRaycaster | {results.Sum(r => r.CanvasWithoutRaycaster)} |");
            sb.AppendLine();

            sb.AppendLine("## 0. `VisibleGraphicCount` nghĩa là gì");
            sb.AppendLine();
            sb.AppendLine("Số `Graphic` (Image / RawImage / TextMeshProUGUI…) thoả **tất cả**:");
            sb.AppendLine();
            sb.AppendLine("1. `graphic.enabled == true`;");
            sb.AppendLine("2. node đó và **mọi node cha** tới tận node gốc prefab đều `activeSelf == true`;");
            sb.AppendLine("3. `color.a > 0.01`;");
            sb.AppendLine("4. `rectTransform.rect.width > 1` **và** `rect.height > 1`.");
            sb.AppendLine();
            sb.AppendLine("Cách đo: prefab được nạp vào scene ẩn bằng `LoadPrefabContents`, node gốc được cấp khung");
            sb.AppendLine($"`{MeasureCanvasSize.x:0}x{MeasureCanvasSize.y:0}` (thay cho Canvas thật, vì prefab trên đĩa không có),");
            sb.AppendLine("rồi chạy `LayoutRebuilder.ForceRebuildLayoutImmediate` một lần để LayoutGroup/Fitter tính kích thước con.");
            sb.AppendLine("Không có script runtime nào chạy.");
            sb.AppendLine();
            sb.AppendLine("**Cố ý không xét**: font của TMP (chữ thiếu font vẫn tính là một ô chữ), sprite của Image");
            sb.AppendLine("(ô trắng không sprite vẫn \"nhìn thấy\" — thậm chí là lỗi che màn), `CanvasGroup.alpha`,");
            sb.AppendLine("Mask cắt ngoài khung, và việc các Graphic vẽ đè lên nhau.");
            sb.AppendLine("Nên con số này là **cận trên**: bằng 0 thì chắc chắn hỏng, khác 0 thì chưa chắc đã đúng bản gốc.");
            sb.AppendLine();

            sb.AppendLine("## 1. Bảng tổng hợp (tệ nhất lên đầu)");
            sb.AppendLine();
            sb.AppendLine("| Prefab | Loại | Visible | Node | Tắt | Image | Thiếu sprite | Alpha 0 | TMP | Thiếu font | Size 0 | Canvas thiếu RC |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (UIPrefabVisibility r in results)
            {
                string kind = r.IsScreen ? "screen" : "cell";
                if (r.Error != null)
                {
                    sb.AppendLine($"| `{r.PrefabName}` | {kind} | LỖI: {r.Error} | | | | | | | | | |");
                    continue;
                }

                string name = r.VisibleGraphicCount == 0 ? $"**`{r.PrefabName}`**" : $"`{r.PrefabName}`";
                sb.AppendLine($"| {name} | {kind} | {r.VisibleGraphicCount} | {r.TotalNodes} | {r.InactiveNodes} | {r.ImageCount} | {r.ImageNoSprite} | {r.ImageAlphaZero} | {r.TextCount} | {r.TextNoFont} | {r.ZeroSizeNodes} | {r.CanvasWithoutRaycaster} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 2. Prefab không hiện gì — chắc chắn hỏng");
            sb.AppendLine();
            List<UIPrefabVisibility> broken = results.Where(r => r.Error == null && r.VisibleGraphicCount == 0).ToList();
            if (broken.Count == 0)
            {
                sb.AppendLine("Không có prefab nào rỗng hoàn toàn.");
            }
            else
            {
                sb.AppendLine("Không một `Graphic` nào thoả điều kiện nhìn thấy được. Nguyên nhân thường gặp:");
                sb.AppendLine("node gốc bị tắt, mọi node kích thước 0, hoặc prefab chỉ có node rỗng.");
                sb.AppendLine();
                sb.AppendLine("| Prefab | Node | Node gốc bật? | Image | TMP | Size 0 | Đường dẫn |");
                sb.AppendLine("|---|---:|---|---:|---:|---:|---|");
                foreach (UIPrefabVisibility r in broken)
                {
                    sb.AppendLine($"| `{r.PrefabName}` | {r.TotalNodes} | {(r.RootActive ? "có" : "**KHÔNG**")} | {r.ImageCount} | {r.TextCount} | {r.ZeroSizeNodes} | `{r.PrefabPath}` |");
                }
            }

            AppendPathSection(sb, results, "## 3. Node kích thước 0",
                "`sizeDelta ≈ (0,0)` **và** `anchorMin ≈ anchorMax` → rect rỗng, Graphic trên node không vẽ ra pixel nào.\n" +
                "Node do LayoutGroup/ContentSizeFitter/AspectRatioFitter điều khiển đã bị loại khỏi danh sách\n" +
                "vì để 0 ở đó là bình thường.\n" +
                "`Pickleball/UI/Fix Common Visibility Issues` chỉ đặt lại kích thước cho node **có Image/TMP**.",
                r => r.ZeroSizePaths);

            AppendPathSection(sb, results, "## 4. TMP thiếu font",
                "`font == null` thì TextMeshProUGUI **không render ký tự nào** — đây là nguyên nhân số một\n" +
                "khiến màn hình trông trống trơn. Sửa hàng loạt bằng `Pickleball/UI/Fix Common Visibility Issues`.",
                r => r.MissingFontPaths);

            AppendPathSection(sb, results, "## 5. Image alpha ≈ 0 — CỐ Ý không tự sửa",
                "Bản gốc để `color.a = 0` cho các nút trong suốt chỉ cần vùng bấm (raycast target).\n" +
                "Tự ý bật alpha lên sẽ sinh ra các mảng màu không có trong bản gốc, nên công cụ chỉ liệt kê.\n" +
                "Đối chiếu với ảnh trong `ui_layout/original_sprites_reference/` rồi sửa tay từng chỗ.",
                r => r.AlphaZeroPaths);

            AppendPathSection(sb, results, "## 6. Canvas thiếu GraphicRaycaster",
                "Canvas không có `GraphicRaycaster` thì vẫn vẽ nhưng **không bấm được**.",
                r => r.MissingRaycasterPaths);

            AppendPathSection(sb, results, "## 7. Node đang tắt (`activeSelf = false`) — CỐ Ý không tự bật",
                "Layout gốc cố ý tắt các node này (popup, trạng thái khác, phần thưởng ẩn…).\n" +
                "Bật hết lên sẽ làm màn hình chồng chéo sai hoàn toàn, nên công cụ chỉ liệt kê để đối chiếu.",
                r => r.InactivePaths);

            return sb.ToString();
        }

        private static void AppendPathSection(StringBuilder sb, List<UIPrefabVisibility> results, string title,
            string description, Func<UIPrefabVisibility, List<string>> selector)
        {
            sb.AppendLine();
            sb.AppendLine(title);
            sb.AppendLine();
            sb.AppendLine(description);
            sb.AppendLine();

            int total = results.Sum(r => selector(r).Count);
            if (total == 0)
            {
                sb.AppendLine("Không có mục nào.");
                return;
            }

            sb.AppendLine($"Tổng: **{total}**.");
            sb.AppendLine();
            foreach (UIPrefabVisibility r in results.OrderBy(r => r.PrefabName, StringComparer.OrdinalIgnoreCase))
            {
                List<string> items = selector(r);
                if (items.Count == 0) continue;

                sb.AppendLine($"### `{r.PrefabName}` ({items.Count})");
                sb.AppendLine();
                foreach (string item in items) sb.AppendLine($"- `{item}`");
                sb.AppendLine();
            }
        }
    }
}
