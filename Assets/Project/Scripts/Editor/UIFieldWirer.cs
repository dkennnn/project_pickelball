using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using StarterKit.UIKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Nối tự động các <c>[SerializeField]</c> của script màn hình vào node con tương ứng
    /// trong prefab UI.
    ///
    /// <para><b>Vì sao cần</b>: <c>UILayoutImporter</c> dựng đúng cây GameObject và gắn đúng
    /// component (Image, Button, TextMeshProUGUI...) nhưng KHÔNG hề gán tham chiếu vào các field
    /// của script màn hình. Hậu quả là mọi field đều null, ví dụ <c>MainMenuUI.play</c> null nên
    /// <c>Bind(play, HandlePlay)</c> lặng lẽ thoát ra và KHÔNG nút nào có handler — người chơi
    /// bấm nút không có gì xảy ra.</para>
    ///
    /// <para>Tool này chạy như một bước ĐỘC LẬP sau importer, không sửa gì trong
    /// <c>UILayoutImporter</c>. Chạy lại nhiều lần được: field nào đã có giá trị thì bỏ qua.</para>
    ///
    /// <para>Báo cáo ghi ra <c>ui_layout/field_wiring_report.md</c> (ngoài thư mục project).</para>
    /// </summary>
    public static class UIFieldWirer
    {
        // ------------------------------------------------------------------ Đường dẫn

        /// <summary>Thư mục chứa prefab UI (quét đệ quy cả Screens/ và Cells/).</summary>
        private const string PrefabFolder = "Assets/Project/Prefabs/UI";

        /// <summary>Tên file báo cáo ghi trong thư mục <c>ui_layout</c>.</summary>
        private const string ReportFileName = "field_wiring_report.md";

        /// <summary>Đường dẫn tuyệt đối dự phòng khi không suy được thư mục <c>ui_layout</c>.</summary>
        private const string FallbackReportPath = @"E:\pikeball\reverse-engineering\ui_layout\field_wiring_report.md";

        /// <summary>Tên assembly runtime của game; MonoBehaviour ngoài assembly này bị bỏ qua.</summary>
        private const string RuntimeAssemblyName = "Pickleball.Runtime";

        // ------------------------------------------------------------------ Điểm ưu tiên của luật khớp

        private const int ScoreExact = 1000;      // tên chuẩn hoá trùng khít
        private const int ScoreAncestor = 500;    // hậu tố token + tổ tiên phủ phần đầu
        private const int ScoreSuffixExact = 300; // bỏ hậu tố kiểu rồi trùng khít
        private const int ScoreSuffixAnc = 200;   // bỏ hậu tố kiểu rồi khớp theo tổ tiên
        private const int ScoreSynonymAnc = 150;  // đổi từ đồng nghĩa "container" rồi khớp tổ tiên
        private const int ScoreSynonymExact = 120;// đổi từ đồng nghĩa rồi trùng khít
        private const int ScoreSelf = 100;        // field trùng tên kiểu -> component trên chính node gốc

        // ------------------------------------------------------------------ Bảng từ

        /// <summary>
        /// Hậu tố tên field suy ra từ KIỂU của field. Ví dụ <c>playImage</c> kiểu <see cref="Image"/>:
        /// không có node tên <c>PlayImage</c> nhưng node <c>Play</c> CÓ component <see cref="Image"/>.
        /// </summary>
        private static readonly (Type Type, string[] Suffixes)[] TypeSuffixes =
        {
            (typeof(RawImage), new[] { "image", "img", "raw" }),
            (typeof(Image), new[] { "image", "img", "sprite" }),
            (typeof(TMP_Text), new[] { "text", "txt", "label" }),
            (typeof(Text), new[] { "text", "txt", "label" }),
            (typeof(Button), new[] { "button", "btn" }),
            (typeof(Slider), new[] { "slider", "bar" }),
            (typeof(Toggle), new[] { "toggle", "tgl" }),
            (typeof(ScrollRect), new[] { "scroll", "scrollrect" }),
            (typeof(Animator), new[] { "animator", "anim" }),
            (typeof(CanvasGroup), new[] { "group", "canvasgroup", "cg" }),
            (typeof(Canvas), new[] { "canvas" }),
            (typeof(RectTransform), new[] { "rect", "transform", "rt" }),
            (typeof(Transform), new[] { "transform", "tf" }),
            (typeof(GameObject), new[] { "obj", "object", "go", "node" })
        };

        /// <summary>
        /// Các từ chỉ "vật chứa" coi như đồng nghĩa. Dùng cho field kiểu Transform/GameObject:
        /// <c>ballsGrid</c> thực chất trỏ tới <c>BallsContent/Scroll View/Viewport/Content</c>.
        /// </summary>
        private static readonly string[] ContainerSynonyms =
        {
            "grid", "container", "parent", "content", "holder", "root", "list", "panel", "group"
        };

        private static readonly Regex DuplicateSuffixRegex = new Regex(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);
        private static readonly Regex TokenRegex = new Regex(@"[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z]+|[0-9]+", RegexOptions.Compiled);

        // ==================================================================
        // Entry point
        // ==================================================================

        /// <summary>
        /// Nối field cho TOÀN BỘ prefab trong <c>Assets/Project/Prefabs/UI</c> rồi ghi báo cáo.
        /// Gọi được qua menu hoặc <c>-executeMethod Pickleball.EditorTools.UIFieldWirer.WireAll</c>.
        /// </summary>
        [MenuItem("Pickleball/UI/Wire Screen Fields")]
        public static void WireAll()
        {
            Run(false);
        }

        /// <summary>
        /// Chạy thử: tính toán và ghi báo cáo y hệt <see cref="WireAll"/> nhưng KHÔNG ghi prefab.
        /// Dùng để soi trước kết quả khớp.
        /// </summary>
        [MenuItem("Pickleball/UI/Wire Screen Fields (Dry Run)")]
        public static void WireAllDryRun()
        {
            Run(true);
        }

        private static void Run(bool dryRun)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            List<string> paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            if (paths.Count == 0)
            {
                Debug.LogError($"[UIFieldWirer] Không tìm thấy prefab nào trong {PrefabFolder}.");
                return;
            }

            var results = new List<PrefabResult>(paths.Count);

            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    EditorUtility.DisplayProgressBar(
                        "UIFieldWirer",
                        Path.GetFileName(path),
                        (float)i / paths.Count);

                    PrefabResult result = WirePrefab(path, dryRun);
                    if (result != null) results.Add(result);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!dryRun) AssetDatabase.SaveAssets();

            WriteReport(results, dryRun);
            LogSummary(results, dryRun);
        }

        // ==================================================================
        // Xử lý một prefab
        // ==================================================================

        private static PrefabResult WirePrefab(string assetPath, bool dryRun)
        {
            var result = new PrefabResult
            {
                AssetPath = assetPath,
                PrefabName = Path.GetFileNameWithoutExtension(assetPath)
            };

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                return result;
            }

            try
            {
                List<MonoBehaviour> targets = CollectRootScripts(root);
                if (targets.Count == 0) return result;

                List<NodeInfo> nodes = CollectNodes(root);
                bool dirty = false;

                foreach (MonoBehaviour target in targets)
                {
                    dirty |= WireComponent(target, nodes, result, dryRun);
                }

                if (dirty && !dryRun)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    result.Saved = true;
                }
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                Debug.LogError($"[UIFieldWirer] {assetPath}: {e}");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }

            return result;
        }

        /// <summary>
        /// Lấy script cần nối trên node gốc: ưu tiên các lớp kế thừa <see cref="UIScreenBase"/>;
        /// nếu không có (prefab cell) thì lấy mọi MonoBehaviour thuộc assembly runtime của game.
        /// </summary>
        private static List<MonoBehaviour> CollectRootScripts(GameObject root)
        {
            MonoBehaviour[] all = root.GetComponents<MonoBehaviour>();

            var screens = all.Where(m => m is UIScreenBase).ToList();
            if (screens.Count > 0) return screens;

            return all
                .Where(m => m != null && IsRuntimeType(m.GetType()))
                .ToList();
        }

        private static bool IsRuntimeType(Type type)
        {
            return string.Equals(type.Assembly.GetName().Name, RuntimeAssemblyName, StringComparison.Ordinal);
        }

        /// <summary>Nối mọi field object-reference còn trống của một component. Trả về true nếu có thay đổi.</summary>
        private static bool WireComponent(MonoBehaviour target, List<NodeInfo> nodes, PrefabResult result, bool dryRun)
        {
            Type type = target.GetType();
            List<FieldInfo> fields = GetWirableFields(type);
            if (fields.Count == 0) return false;

            var so = new SerializedObject(target);

            // Bước 1: gom ứng viên cho từng field, đồng thời đếm mức "tranh chấp" của từng node
            // (bao nhiêu field cùng nhắm vào node đó) để làm tie-break.
            var pending = new List<PendingField>();
            var contention = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (FieldInfo field in fields)
            {
                SerializedProperty prop = so.FindProperty(field.Name);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                var entry = new PendingField
                {
                    Field = field,
                    Property = prop,
                    ScriptName = type.Name
                };

                if (prop.objectReferenceValue != null)
                {
                    entry.AlreadySet = true;
                    pending.Add(entry);
                    continue;
                }

                entry.Candidates = FindCandidates(field.Name, field.FieldType, nodes);
                foreach (Candidate c in entry.Candidates)
                {
                    contention.TryGetValue(c.Node.Path, out int n);
                    contention[c.Node.Path] = n + 1;
                }

                pending.Add(entry);
            }

            // Bước 2: chọn ứng viên tốt nhất và gán.
            bool dirty = false;
            foreach (PendingField entry in pending)
            {
                result.Total++;

                if (entry.AlreadySet)
                {
                    result.AlreadySet++;
                    result.Wired.Add(new WiredField
                    {
                        ScriptName = entry.ScriptName,
                        FieldName = entry.Field.Name,
                        TypeName = PrettyType(entry.Field.FieldType),
                        NodePath = AssetName(entry.Property.objectReferenceValue),
                        Rule = "có sẵn"
                    });
                    continue;
                }

                if (entry.Candidates == null || entry.Candidates.Count == 0)
                {
                    result.Missing.Add(new MissingField
                    {
                        ScriptName = entry.ScriptName,
                        FieldName = entry.Field.Name,
                        TypeName = PrettyType(entry.Field.FieldType),
                        Note = GuessMissingReason(entry.Field)
                    });
                    continue;
                }

                Candidate best = PickBest(entry.Candidates, contention);
                Object value = ResolveValue(best.Node, entry.Field.FieldType);
                if (value == null)
                {
                    // Không nên xảy ra (ứng viên đã lọc theo component) nhưng tuyệt đối không gán bừa null.
                    result.Missing.Add(new MissingField
                    {
                        ScriptName = entry.ScriptName,
                        FieldName = entry.Field.Name,
                        TypeName = PrettyType(entry.Field.FieldType),
                        Note = "node khớp tên nhưng thiếu component đúng kiểu"
                    });
                    continue;
                }

                if (!dryRun) entry.Property.objectReferenceValue = value;
                dirty = true;

                result.NewlyWired++;
                result.Wired.Add(new WiredField
                {
                    ScriptName = entry.ScriptName,
                    FieldName = entry.Field.Name,
                    TypeName = PrettyType(entry.Field.FieldType),
                    NodePath = best.Node.Path,
                    Rule = best.Rule + (entry.Candidates.Count > 1 ? $", {entry.Candidates.Count} ứng viên" : string.Empty)
                });
            }

            if (dirty && !dryRun) so.ApplyModifiedPropertiesWithoutUndo();
            return dirty;
        }

        /// <summary>Sắp ứng viên: điểm cao trước, rồi node ít bị tranh chấp, rồi node nông hơn.</summary>
        private static Candidate PickBest(List<Candidate> candidates, Dictionary<string, int> contention)
        {
            Candidate best = null;
            int bestContention = 0;

            foreach (Candidate c in candidates)
            {
                contention.TryGetValue(c.Node.Path, out int cont);

                if (best == null)
                {
                    best = c;
                    bestContention = cont;
                    continue;
                }

                if (c.Score != best.Score)
                {
                    if (c.Score > best.Score) { best = c; bestContention = cont; }
                    continue;
                }

                if (cont != bestContention)
                {
                    if (cont < bestContention) { best = c; bestContention = cont; }
                    continue;
                }

                if (c.Node.Depth != best.Node.Depth)
                {
                    if (c.Node.Depth < best.Node.Depth) { best = c; bestContention = cont; }
                    continue;
                }

                if (string.CompareOrdinal(c.Node.Path, best.Node.Path) < 0)
                {
                    best = c;
                    bestContention = cont;
                }
            }

            return best;
        }

        // ==================================================================
        // Thu thập field
        // ==================================================================

        /// <summary>
        /// Mọi field Unity serialize được (public hoặc có <see cref="SerializeField"/>) mà kiểu là
        /// <see cref="GameObject"/> hoặc kế thừa <see cref="Component"/>, tính cả field private
        /// khai báo ở lớp cha.
        /// </summary>
        private static List<FieldInfo> GetWirableFields(Type type)
        {
            var list = new List<FieldInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (Type cur = type; cur != null && cur != typeof(MonoBehaviour); cur = cur.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
                                                                 | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                foreach (FieldInfo f in cur.GetFields(flags))
                {
                    if (f.IsStatic || f.IsInitOnly) continue;
                    if (f.IsNotSerialized) continue;
                    if (Attribute.IsDefined(f, typeof(NonSerializedAttribute))) continue;
                    if (!f.IsPublic && !Attribute.IsDefined(f, typeof(SerializeField))) continue;
                    if (!IsObjectReferenceType(f.FieldType)) continue;
                    if (!seen.Add(f.Name)) continue;

                    list.Add(f);
                }
            }

            return list;
        }

        /// <summary>Chỉ nhận GameObject hoặc Component đơn lẻ; mảng/List/asset (Sprite, ScriptableObject...) bỏ qua.</summary>
        private static bool IsObjectReferenceType(Type t)
        {
            if (t.IsArray || t.IsGenericType) return false;
            return t == typeof(GameObject) || typeof(Component).IsAssignableFrom(t);
        }

        private static Object ResolveValue(NodeInfo node, Type fieldType)
        {
            if (fieldType == typeof(GameObject)) return node.Transform.gameObject;
            return node.Transform.GetComponent(fieldType);
        }

        private static bool NodeSupports(NodeInfo node, Type fieldType)
        {
            return ResolveValue(node, fieldType) != null;
        }

        // ==================================================================
        // Thuật toán khớp tên
        // ==================================================================

        /// <summary>
        /// Tìm mọi node ứng viên cho một field, theo thứ tự luật giảm dần độ tin cậy.
        /// Luật sau chỉ chạy khi luật trước không ra kết quả nào.
        /// </summary>
        private static List<Candidate> FindCandidates(string fieldName, Type fieldType, List<NodeInfo> nodes)
        {
            var res = new List<Candidate>();
            string[] tokens = Tokenize(fieldName);
            string norm = Normalize(fieldName);

            // Luật A — tên chuẩn hoá trùng khít: field `playTxt` ← node `PlayTxt`,
            // field `textLevel` ← node `Text  - Level   `.
            foreach (NodeInfo nd in nodes)
            {
                if (string.Equals(nd.Norm, norm, StringComparison.Ordinal) && NodeSupports(nd, fieldType))
                    res.Add(new Candidate(ScoreExact, nd, "trùng tên"));
            }
            if (res.Count > 0) return res;

            // Luật B — tách trùng tên bằng đường dẫn tổ tiên: node `ChangeIndicator` xuất hiện 3 lần,
            // field `lockerChangeIndicator` = [locker][change][indicator]; phần đuôi khớp tên node,
            // phần đầu (`locker`) phải nằm trong tên một tổ tiên (`LockerButton`).
            AddAncestorMatches(res, tokens, fieldType, nodes, ScoreAncestor, "tổ tiên");
            if (res.Count > 0) return res;

            // Luật C — bỏ hậu tố suy từ kiểu field: `playImage` (Image) → `play` → node `Play` có Image;
            // `avatarButton` (Button) → `avatar` → node `Avatar` có Button.
            foreach (string suffix in GetTypeSuffixes(fieldType))
            {
                if (tokens.Length <= 1 || !string.Equals(tokens[tokens.Length - 1], suffix, StringComparison.Ordinal))
                    continue;

                string[] shorter = tokens.Take(tokens.Length - 1).ToArray();
                string shortNorm = string.Concat(shorter);

                foreach (NodeInfo nd in nodes)
                {
                    if (string.Equals(nd.Norm, shortNorm, StringComparison.Ordinal) && NodeSupports(nd, fieldType))
                        res.Add(new Candidate(ScoreSuffixExact, nd, "bỏ hậu tố kiểu"));
                }

                if (res.Count == 0)
                    AddAncestorMatches(res, shorter, fieldType, nodes, ScoreSuffixAnc, "bỏ hậu tố kiểu + tổ tiên");

                break; // chỉ thử hậu tố khớp đầu tiên
            }
            if (res.Count > 0) return res;

            // Luật D — đổi từ chỉ vật chứa: `ballsGrid` (Transform) → `ballsContent`
            // → node `BallsContent/Scroll View/Viewport/Content`.
            if ((fieldType == typeof(GameObject) || typeof(Transform).IsAssignableFrom(fieldType))
                && tokens.Length > 1
                && ContainerSynonyms.Contains(tokens[tokens.Length - 1]))
            {
                foreach (string syn in ContainerSynonyms)
                {
                    string[] swapped = tokens.Take(tokens.Length - 1).Concat(new[] { syn }).ToArray();
                    AddAncestorMatches(res, swapped, fieldType, nodes, ScoreSynonymAnc, "từ đồng nghĩa + tổ tiên");

                    string swappedNorm = string.Concat(swapped);
                    foreach (NodeInfo nd in nodes)
                    {
                        if (string.Equals(nd.Norm, swappedNorm, StringComparison.Ordinal) && NodeSupports(nd, fieldType))
                            res.Add(new Candidate(ScoreSynonymExact, nd, "từ đồng nghĩa"));
                    }
                }
            }
            if (res.Count > 0) return res;

            // Luật E — field trùng tên kiểu (ví dụ `canvas` kiểu Canvas): lấy component trên chính
            // node gốc, hoặc node duy nhất có component đó.
            if (string.Equals(norm, Normalize(fieldType.Name), StringComparison.Ordinal))
            {
                NodeInfo root = nodes[0];
                if (NodeSupports(root, fieldType))
                {
                    res.Add(new Candidate(ScoreSelf, root, "component trên node gốc"));
                }
                else
                {
                    List<NodeInfo> owners = nodes.Where(n => NodeSupports(n, fieldType)).ToList();
                    if (owners.Count == 1) res.Add(new Candidate(ScoreSelf, owners[0], "node duy nhất có kiểu này"));
                }
            }

            return res;
        }

        /// <summary>
        /// Khớp "hậu tố token + tổ tiên": tên node bằng đúng phần token ĐUÔI của tên field, còn các
        /// token ĐẦU còn lại phải xuất hiện trong tên các tổ tiên (bỏ qua node gốc vì tên node gốc
        /// là tên màn hình, khớp bừa rất dễ). Phần đuôi càng dài thì điểm càng cao.
        /// </summary>
        private static void AddAncestorMatches(List<Candidate> res, string[] tokens, Type fieldType,
            List<NodeInfo> nodes, int baseScore, string rule)
        {
            if (tokens.Length < 2) return;

            foreach (NodeInfo nd in nodes)
            {
                if (nd.Norm.Length == 0 || !NodeSupports(nd, fieldType)) continue;

                for (int k = tokens.Length - 1; k >= 1; k--)
                {
                    string tail = string.Concat(tokens.Skip(k));
                    if (!string.Equals(tail, nd.Norm, StringComparison.Ordinal)) continue;

                    if (AncestorsCover(nd, tokens, k))
                        res.Add(new Candidate(baseScore + (tokens.Length - k), nd, rule));

                    break; // với mỗi node chỉ nhận phần đuôi dài nhất
                }
            }
        }

        /// <summary>Mọi token đầu <c>tokens[0..count)</c> đều phải nằm trong tên một tổ tiên (trừ node gốc).</summary>
        private static bool AncestorsCover(NodeInfo node, string[] tokens, int count)
        {
            for (int i = 0; i < count; i++)
            {
                bool found = false;
                for (int a = 1; a < node.AncestorNorms.Count; a++) // bỏ index 0 = node gốc
                {
                    if (node.AncestorNorms[a].Contains(tokens[i])) { found = true; break; }
                }

                if (!found) return false;
            }

            return true;
        }

        private static IEnumerable<string> GetTypeSuffixes(Type fieldType)
        {
            foreach ((Type type, string[] suffixes) in TypeSuffixes)
            {
                if (type.IsAssignableFrom(fieldType)) return suffixes;
            }

            return Array.Empty<string>();
        }

        // ==================================================================
        // Chuẩn hoá tên
        // ==================================================================

        /// <summary>Viết thường và bỏ mọi ký tự không phải chữ/số: <c>"Text  - Level   "</c> → <c>"textlevel"</c>.</summary>
        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        /// <summary>Bỏ hậu tố nhân bản của Unity: <c>"ChangeIndicator (1)"</c> → <c>"ChangeIndicator"</c>.</summary>
        private static string StripDuplicateSuffix(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : DuplicateSuffixRegex.Replace(s, string.Empty);
        }

        /// <summary>Cắt tên thành token theo camelCase và ký tự phân cách: <c>lockerChangeIndicator</c> → locker/change/indicator.</summary>
        private static string[] Tokenize(string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<string>();

            var res = new List<string>();
            foreach (string part in Regex.Split(s, "[^A-Za-z0-9]+"))
            {
                if (part.Length == 0) continue;
                foreach (Match m in TokenRegex.Matches(part)) res.Add(m.Value.ToLowerInvariant());
            }

            return res.ToArray();
        }

        // ==================================================================
        // Cây node
        // ==================================================================

        private static List<NodeInfo> CollectNodes(GameObject root)
        {
            var nodes = new List<NodeInfo>();
            Collect(root.transform, new List<string>(), nodes);
            return nodes;
        }

        private static void Collect(Transform t, List<string> ancestors, List<NodeInfo> nodes)
        {
            nodes.Add(new NodeInfo
            {
                Transform = t,
                Norm = Normalize(StripDuplicateSuffix(t.name)),
                AncestorNorms = ancestors.Select(Normalize).ToList(),
                Depth = ancestors.Count,
                Path = ancestors.Count == 0 ? t.name : string.Join("/", ancestors) + "/" + t.name
            });

            ancestors.Add(t.name);
            for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), ancestors, nodes);
            ancestors.RemoveAt(ancestors.Count - 1);
        }

        // ==================================================================
        // Báo cáo
        // ==================================================================

        private static void WriteReport(List<PrefabResult> results, bool dryRun)
        {
            int total = results.Sum(r => r.Total);
            int wired = results.Sum(r => r.NewlyWired + r.AlreadySet);
            float percent = total > 0 ? 100f * wired / total : 0f;

            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo nối field UI — `UIFieldWirer`");
            sb.AppendLine();
            sb.AppendLine($"Sinh lúc: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                          + (dryRun ? " — **CHẠY THỬ (không ghi prefab)**" : string.Empty));
            sb.AppendLine();
            sb.AppendLine("Tool tự nối `[SerializeField]` của script màn hình vào node con trong prefab, "
                          + "khớp theo tên đã chuẩn hoá (viết thường, bỏ ký tự không phải chữ/số), "
                          + "tách trùng tên bằng đường dẫn tổ tiên, và bỏ hậu tố suy từ kiểu field.");
            sb.AppendLine();

            // --- Bảng tổng quan ---
            sb.AppendLine("## Tổng quan theo prefab");
            sb.AppendLine();
            sb.AppendLine("| Prefab | Script | Đã nối / Tổng | Thiếu | Ghi |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (PrefabResult r in results.OrderBy(x => x.PrefabName, StringComparer.Ordinal))
            {
                if (r.Total == 0 && string.IsNullOrEmpty(r.Error)) continue;

                string scripts = string.Join(", ", r.Wired.Select(w => w.ScriptName)
                    .Concat(r.Missing.Select(m => m.ScriptName))
                    .Distinct(StringComparer.Ordinal));
                int ok = r.NewlyWired + r.AlreadySet;

                sb.AppendLine($"| `{r.PrefabName}` | {(scripts.Length > 0 ? scripts : "—")} "
                              + $"| {ok}/{r.Total} | {r.Missing.Count} "
                              + $"| {(string.IsNullOrEmpty(r.Error) ? (r.Saved ? "có" : "không") : "LỖI: " + r.Error)} |");
            }

            // --- Danh sách field không nối được ---
            var missing = results
                .SelectMany(x => x.Missing.Select(f => (Prefab: x.PrefabName, Field: f)))
                .OrderBy(x => x.Prefab, StringComparer.Ordinal)
                .ToList();

            sb.AppendLine();
            sb.AppendLine("## Field KHÔNG nối được — cần sửa tay");
            sb.AppendLine();

            if (missing.Count == 0)
            {
                sb.AppendLine("Không có. Toàn bộ field đều đã nối.");
            }
            else
            {
                sb.AppendLine($"Tổng cộng **{missing.Count}** field. Với mỗi dòng: hoặc tên node trong prefab "
                              + "lệch tên field (sửa tên node hoặc gán tay), hoặc field trỏ tới một asset prefab "
                              + "nằm ngoài cây (phải kéo thả prefab vào).");
                sb.AppendLine();
                sb.AppendLine("| Prefab | Script | Field | Kiểu | Nghi ngờ |");
                sb.AppendLine("|---|---|---|---|---|");

                foreach ((string prefabName, MissingField field) in missing)
                {
                    sb.AppendLine($"| `{prefabName}` | {field.ScriptName} | `{field.FieldName}` "
                                  + $"| `{field.TypeName}` | {field.Note} |");
                }
            }

            // --- Chi tiết từng prefab ---
            sb.AppendLine();
            sb.AppendLine("## Chi tiết từng prefab");

            foreach (PrefabResult r in results.OrderBy(x => x.PrefabName, StringComparer.Ordinal))
            {
                if (r.Total == 0) continue;

                int ok = r.NewlyWired + r.AlreadySet;
                sb.AppendLine();
                sb.AppendLine($"### {r.PrefabName} — {ok}/{r.Total}");
                sb.AppendLine();
                sb.AppendLine($"`{r.AssetPath}`");
                sb.AppendLine();

                foreach (WiredField w in r.Wired)
                {
                    sb.AppendLine($"- `{w.FieldName}` (`{w.TypeName}`) → `{w.NodePath}` — *{w.Rule}*");
                }

                foreach (MissingField m in r.Missing)
                {
                    sb.AppendLine($"- **THIẾU** `{m.FieldName}` (`{m.TypeName}`) — {m.Note}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Tổng kết");
            sb.AppendLine();
            sb.AppendLine($"**{wired}/{total} field đã nối ({percent:0.#}%)**");

            string reportPath = ResolveReportPath();
            try
            {
                string dir = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(reportPath, sb.ToString(), new UTF8Encoding(false));
                Debug.Log($"[UIFieldWirer] Đã ghi báo cáo: {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFieldWirer] Không ghi được báo cáo {reportPath}: {e.Message}");
            }
        }

        private static void LogSummary(List<PrefabResult> results, bool dryRun)
        {
            int total = results.Sum(r => r.Total);
            int wired = results.Sum(r => r.NewlyWired + r.AlreadySet);
            int newly = results.Sum(r => r.NewlyWired);
            int missing = results.Sum(r => r.Missing.Count);
            int saved = results.Count(r => r.Saved);
            float percent = total > 0 ? 100f * wired / total : 0f;

            Debug.Log($"[UIFieldWirer]{(dryRun ? " (CHẠY THỬ)" : string.Empty)} "
                      + $"{wired}/{total} field đã nối ({percent:0.#}%) — "
                      + $"mới nối {newly}, thiếu {missing}, ghi lại {saved} prefab.");
        }

        /// <summary>Suy ra <c>&lt;repo&gt;/ui_layout/field_wiring_report.md</c>; không thấy thì dùng đường dẫn tuyệt đối.</summary>
        private static string ResolveReportPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string repoRoot = projectRoot != null ? Directory.GetParent(projectRoot)?.FullName : null;

            if (repoRoot != null)
            {
                string candidate = Path.Combine(repoRoot, "ui_layout");
                if (Directory.Exists(candidate)) return Path.Combine(candidate, ReportFileName);
            }

            return FallbackReportPath;
        }

        // ==================================================================
        // Tiện ích nhỏ
        // ==================================================================

        private static string PrettyType(Type t)
        {
            return t == null ? "?" : t.Name;
        }

        private static string AssetName(Object o)
        {
            return o == null ? "—" : o.name;
        }

        private static string GuessMissingReason(FieldInfo field)
        {
            string lower = field.Name.ToLowerInvariant();

            if (lower.EndsWith("prefab"))
                return "tên field kết thúc bằng `Prefab` — nhiều khả năng trỏ tới asset prefab ngoài cây, phải gán tay";

            if (typeof(Component).IsAssignableFrom(field.FieldType) && field.FieldType.IsSubclassOf(typeof(MonoBehaviour)))
                return "không có node nào mang script này";

            return "không tìm được node khớp tên";
        }

        // ==================================================================
        // Kiểu dữ liệu nội bộ
        // ==================================================================

        private sealed class NodeInfo
        {
            public Transform Transform;
            public string Norm;
            public List<string> AncestorNorms;
            public int Depth;
            public string Path;
        }

        private sealed class Candidate
        {
            public readonly int Score;
            public readonly NodeInfo Node;
            public readonly string Rule;

            public Candidate(int score, NodeInfo node, string rule)
            {
                Score = score;
                Node = node;
                Rule = rule;
            }
        }

        private sealed class PendingField
        {
            public FieldInfo Field;
            public SerializedProperty Property;
            public string ScriptName;
            public bool AlreadySet;
            public List<Candidate> Candidates;
        }

        private sealed class WiredField
        {
            public string ScriptName;
            public string FieldName;
            public string TypeName;
            public string NodePath;
            public string Rule;
        }

        private sealed class MissingField
        {
            public string ScriptName;
            public string FieldName;
            public string TypeName;
            public string Note;
        }

        private sealed class PrefabResult
        {
            public string AssetPath;
            public string PrefabName;
            public int Total;
            public int NewlyWired;
            public int AlreadySet;
            public bool Saved;
            public string Error;
            public readonly List<WiredField> Wired = new List<WiredField>();
            public readonly List<MissingField> Missing = new List<MissingField>();
        }
    }
}
