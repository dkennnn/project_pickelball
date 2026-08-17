using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Đối chiếu layout JSON gốc với các prefab đã sinh và xuất báo cáo Markdown
    /// vào <c>ui_layout/import_report.md</c>.
    /// </summary>
    /// <remarks>
    /// Báo cáo trả lời 4 câu hỏi: (1) prefab nào dựng thiếu node, (2) class nào chưa tra được type
    /// (tức tầng code UI còn thiếu), (3) sprite nào chưa có art, (4) node nào có component layout
    /// phải căn tay vì bản gốc không lộ tham số.
    /// </remarks>
    public static class UILayoutReport
    {
        private sealed class ScreenRow
        {
            public string Screen;
            public string JsonFile;
            public string PrefabPath;
            public int JsonNodes;
            public int BuiltNodes;
            public int SkippedComponents;
            public int UnresolvedComponents;
            public bool PrefabExists;
        }

        /// <summary>
        /// Xuất báo cáo import ra <c>ui_layout/import_report.md</c> và in tóm tắt ra Console.
        /// Chạy được cả khi chưa import (cột "node đã dựng" sẽ là 0).
        /// </summary>
        [MenuItem("Pickleball/UI/Layout Report")]
        public static void GenerateReport()
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogError("[UILayoutReport] Không tìm thấy thư mục ui_layout (cần có _index.json).");
                return;
            }

            JObject index;
            try
            {
                index = JObject.Parse(File.ReadAllText(Path.Combine(layoutRoot, "_index.json")));
            }
            catch (Exception e)
            {
                Debug.LogError($"[UILayoutReport] Không đọc được _index.json: {e.Message}");
                return;
            }

            if (!(index["screens"] is JArray entries))
            {
                Debug.LogError("[UILayoutReport] _index.json không có mảng 'screens'.");
                return;
            }

            UILayoutTypeResolver.ClearCache();
            UISpriteIndex sprites = UISpriteIndex.Build(UILayoutPaths.SpriteFolder);

            var rows = new List<ScreenRow>();
            var unresolved = new Dictionary<string, int>(StringComparer.Ordinal);
            var unresolvedScreens = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            var skippedClasses = new Dictionary<string, int>(StringComparer.Ordinal);
            var missingSprites = new Dictionary<string, int>(StringComparer.Ordinal);
            var layoutNodes = new List<string>();

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i] as JObject;
                    string relative = (string)entry?["file"];
                    if (string.IsNullOrEmpty(relative)) continue;

                    string screenName = UILayoutImporter.CleanName((string)entry["screen"]);
                    string jsonPath = Path.Combine(layoutRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(jsonPath)) continue;

                    EditorUtility.DisplayProgressBar("Layout Report", screenName, (i + 1) / (float)entries.Count);

                    JObject node;
                    try
                    {
                        node = JObject.Parse(File.ReadAllText(jsonPath));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[UILayoutReport] Bỏ qua {relative}: {e.Message}");
                        continue;
                    }

                    string prefabPath = UILayoutImporter.ResolvePrefabPath(jsonPath, screenName);
                    var row = new ScreenRow
                    {
                        Screen = string.IsNullOrEmpty(screenName) ? Path.GetFileNameWithoutExtension(jsonPath) : screenName,
                        JsonFile = relative,
                        PrefabPath = prefabPath
                    };

                    Walk(node, row.Screen, string.Empty, row, sprites, unresolved, unresolvedScreens, skippedClasses,
                        missingSprites, layoutNodes);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    row.PrefabExists = prefab != null;
                    row.BuiltNodes = prefab != null ? prefab.GetComponentsInChildren<Transform>(true).Length : 0;
                    rows.Add(row);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string reportPath = Path.Combine(layoutRoot, UILayoutPaths.ReportFileName);
            string markdown = BuildMarkdown(rows, unresolved, unresolvedScreens, skippedClasses, missingSprites, layoutNodes, sprites);

            try
            {
                File.WriteAllText(reportPath, markdown, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogError($"[UILayoutReport] Không ghi được báo cáo: {e.Message}");
                return;
            }

            int mismatched = rows.Count(r => r.BuiltNodes != r.JsonNodes);
            Debug.Log($"[UILayoutReport] Đã ghi {reportPath}\n" +
                      $"  - {rows.Count} layout, {rows.Sum(r => r.JsonNodes)} node trong JSON, {rows.Sum(r => r.BuiltNodes)} node đã dựng ({mismatched} layout lệch số node)\n" +
                      $"  - {unresolved.Count} class chưa tra được type, {missingSprites.Count} sprite chưa có art, {layoutNodes.Count} node layout cần căn tay");
        }

        private static void Walk(JObject node, string screen, string parentPath, ScreenRow row, UISpriteIndex sprites,
            Dictionary<string, int> unresolved, Dictionary<string, SortedSet<string>> unresolvedScreens,
            Dictionary<string, int> skippedClasses, Dictionary<string, int> missingSprites, List<string> layoutNodes)
        {
            string name = (string)node["name"] ?? "GameObject";
            string path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;
            row.JsonNodes++;

            if (node["components"] is JArray components)
            {
                foreach (JToken token in components)
                {
                    string className = (string)(token as JObject)?["class"];
                    if (string.IsNullOrEmpty(className)) continue;

                    if (UILayoutImporter.SkippedClasses.Contains(className))
                    {
                        row.SkippedComponents++;
                        Bump(skippedClasses, className);
                        continue;
                    }

                    if (UILayoutImporter.LayoutClasses.Contains(className))
                        layoutNodes.Add($"`{screen}` — `{path}` → {UILayoutTypeResolver.ShortName(className)}");

                    bool isImage = className.EndsWith("Image", StringComparison.Ordinal);
                    if (isImage)
                    {
                        string spriteName = (string)(token as JObject)?["sprite"];
                        if (!string.IsNullOrEmpty(spriteName) && !sprites.TryGet(spriteName, out _))
                            Bump(missingSprites, spriteName);
                    }

                    if (UILayoutTypeResolver.Resolve(className) == null)
                    {
                        row.UnresolvedComponents++;
                        Bump(unresolved, className);
                        if (!unresolvedScreens.TryGetValue(className, out SortedSet<string> set))
                        {
                            set = new SortedSet<string>(StringComparer.Ordinal);
                            unresolvedScreens[className] = set;
                        }

                        set.Add(screen);
                    }
                }
            }

            if (node["children"] is JArray children)
            {
                foreach (JToken token in children)
                {
                    if (token is JObject child)
                        Walk(child, screen, path, row, sprites, unresolved, unresolvedScreens, skippedClasses, missingSprites, layoutNodes);
                }
            }
        }

        private static string BuildMarkdown(List<ScreenRow> rows, Dictionary<string, int> unresolved,
            Dictionary<string, SortedSet<string>> unresolvedScreens, Dictionary<string, int> skippedClasses,
            Dictionary<string, int> missingSprites, List<string> layoutNodes, UISpriteIndex sprites)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo import UI layout");
            sb.AppendLine();
            sb.AppendLine($"Sinh tự động bởi `Pickleball/UI/Layout Report` lúc {DateTime.Now:yyyy-MM-dd HH:mm}.");
            sb.AppendLine();
            sb.AppendLine("| Chỉ số | Giá trị |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Layout trong index | {rows.Count} |");
            sb.AppendLine($"| Prefab đã có trên đĩa | {rows.Count(r => r.PrefabExists)} |");
            sb.AppendLine($"| Node trong JSON | {rows.Sum(r => r.JsonNodes)} |");
            sb.AppendLine($"| Node đã dựng | {rows.Sum(r => r.BuiltNodes)} |");
            sb.AppendLine($"| Component cố ý bỏ qua | {skippedClasses.Values.Sum()} |");
            sb.AppendLine($"| Component chưa tra được type | {unresolved.Values.Sum()} ({unresolved.Count} class) |");
            sb.AppendLine($"| Sprite trong `Textures/UI` | {sprites.Count} |");
            sb.AppendLine($"| Sprite còn thiếu art | {missingSprites.Count} tên / {missingSprites.Values.Sum()} chỗ dùng |");
            string tmpStatus = Resources.Load<TMP_Settings>("TMP Settings") != null
                ? "đã import"
                : "**CHƯA import** — chữ sẽ không có font";
            sb.AppendLine($"| TMP Essential Resources | {tmpStatus} |");
            sb.AppendLine();

            sb.AppendLine("## 1. Từng màn hình");
            sb.AppendLine();
            sb.AppendLine("`node đã dựng` đếm số Transform trong prefab thật trên đĩa. Lệch so với JSON nghĩa là");
            sb.AppendLine("có node dựng hỏng hoặc prefab đang cũ hơn layout — chạy lại `Import All UI Layouts`.");
            sb.AppendLine();
            sb.AppendLine("| Màn hình | Node JSON | Node đã dựng | Component bỏ qua | Class thiếu | Prefab |");
            sb.AppendLine("|---|---:|---:|---:|---:|---|");
            foreach (ScreenRow row in rows.OrderByDescending(r => r.JsonNodes))
            {
                string status = !row.PrefabExists
                    ? "chưa có"
                    : row.BuiltNodes == row.JsonNodes ? "khớp" : $"lệch {row.BuiltNodes - row.JsonNodes:+#;-#;0}";
                sb.AppendLine($"| {row.Screen} | {row.JsonNodes} | {row.BuiltNodes} | {row.SkippedComponents} | {row.UnresolvedComponents} | {status} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 2. Class chưa tra được type");
            sb.AppendLine();
            if (unresolved.Count == 0)
            {
                sb.AppendLine("Không còn class nào thiếu — mọi component trong layout đều gắn được.");
            }
            else
            {
                sb.AppendLine("Importer tra type bằng reflection theo tên; các class dưới đây chưa tồn tại trong project");
                sb.AppendLine("nên node vẫn được dựng nhưng **thiếu component**. Viết class xong thì chạy lại importer là gắn được.");
                sb.AppendLine();
                sb.AppendLine("| Class trong bản gốc | Số lần dùng | Xuất hiện ở |");
                sb.AppendLine("|---|---:|---|");
                foreach (KeyValuePair<string, int> pair in unresolved.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
                {
                    unresolvedScreens.TryGetValue(pair.Key, out SortedSet<string> screens);
                    string where = screens == null ? string.Empty : string.Join(", ", screens.Take(6));
                    if (screens != null && screens.Count > 6) where += $", … (+{screens.Count - 6})";
                    sb.AppendLine($"| `{pair.Key}` | {pair.Value} | {where} |");
                }
            }

            IEnumerable<string> fuzzy = UILayoutTypeResolver.FuzzyMatches.ToArray();
            if (fuzzy.Any())
            {
                sb.AppendLine();
                sb.AppendLine("### Khớp gần đúng (kiểm tra lại)");
                sb.AppendLine();
                sb.AppendLine("Các class dưới đây chỉ khớp khi bỏ qua hoa/thường — importer đã gắn theo phỏng đoán:");
                sb.AppendLine();
                foreach (string item in fuzzy.OrderBy(s => s, StringComparer.Ordinal)) sb.AppendLine($"- `{item}`");
            }

            sb.AppendLine();
            sb.AppendLine("## 3. Component cố ý bỏ qua");
            sb.AppendLine();
            sb.AppendLine("VFX hạt, package bên thứ ba và localization — dựng lại không có ý nghĩa ở bước này.");
            sb.AppendLine();
            sb.AppendLine("| Class | Số lần |");
            sb.AppendLine("|---|---:|");
            foreach (KeyValuePair<string, int> pair in skippedClasses.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"| `{pair.Key}` | {pair.Value} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 4. Sprite còn thiếu art");
            sb.AppendLine();
            if (missingSprites.Count == 0)
            {
                sb.AppendLine($"Không thiếu sprite nào trong `{UILayoutPaths.SpriteFolder}`.");
            }
            else
            {
                sb.AppendLine($"Thả file ảnh **đúng tên** vào `{UILayoutPaths.SpriteFolder}/` (Texture Type = Sprite) rồi chạy");
                sb.AppendLine("`Pickleball/UI/Bind Sprites From Folder`. Mỗi Image thiếu art đang mang component");
                sb.AppendLine("`SpritePlaceholder` giữ tên gốc, nên gán hàng loạt được bất cứ lúc nào.");
                sb.AppendLine();
                sb.AppendLine("| Tên sprite gốc | Số chỗ dùng |");
                sb.AppendLine("|---|---:|");
                foreach (KeyValuePair<string, int> pair in missingSprites.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine($"| `{pair.Key}` | {pair.Value} |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## 5. Node có component layout — phải căn tay");
            sb.AppendLine();
            sb.AppendLine("Bundle gốc không lộ tham số của LayoutGroup / ScrollRect / Mask / ContentSizeFitter…");
            sb.AppendLine("nên các component này được gắn với **giá trị mặc định của Unity**. Padding, spacing, cellSize,");
            sb.AppendLine("hướng cuộn, elasticity… đều phải chỉnh tay theo ảnh chụp bản gốc.");
            sb.AppendLine();
            if (layoutNodes.Count == 0)
            {
                sb.AppendLine("Không có node nào dùng component layout.");
            }
            else
            {
                foreach (string item in layoutNodes) sb.AppendLine($"- {item}");
            }

            sb.AppendLine();
            sb.AppendLine("## 6. Giá trị mặc định do importer chọn");
            sb.AppendLine();
            sb.AppendLine($"- **TextMeshProUGUI**: `fontSize = {UILayoutImporter.DefaultFontSize}`, `alignment = Center`, cho phép xuống dòng,");
            sb.AppendLine("  font = font mặc định của TMP. Bản gốc **không lộ** fontSize/alignment/font asset → phải căn tay toàn bộ.");
            sb.AppendLine("- **localScale `(0,0,0)`** trong JSON (lỗi trích xuất, gặp ở mọi node gốc màn hình) được ép về `(1,1,1)`.");
            sb.AppendLine("- **Node gốc màn hình**: rect bị ép full-screen vì Canvas ghi đè RectTransform.");
            sb.AppendLine("- **Button.targetGraphic** được nối vào Graphic cùng node nếu có.");
            sb.AppendLine("- **ScrollRect**: `content`/`viewport` được nối theo quy ước tên con (`Content`, `Viewport`/`Mask`) nếu tìm thấy.");
            sb.AppendLine("- **Image thiếu sprite**: `sprite = null` + component `SpritePlaceholder` giữ tên gốc.");
            sb.AppendLine("- **`active: false`** được đặt sau khi dựng xong toàn bộ con của node đó.");
            return sb.ToString();
        }

        private static void Bump(Dictionary<string, int> map, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            map.TryGetValue(key, out int n);
            map[key] = n + 1;
        }
    }
}
