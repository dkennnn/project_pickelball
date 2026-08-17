using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Các đường dẫn dùng chung cho bộ công cụ dựng UI từ layout JSON đã trích từ bản gốc.
    /// </summary>
    public static class UILayoutPaths
    {
        /// <summary>Khoá EditorPrefs lưu thư mục ui_layout do người dùng chọn tay.</summary>
        public const string LayoutRootPrefsKey = "Pickleball.UILayoutImporter.LayoutRoot";

        /// <summary>Thư mục chứa toàn bộ prefab UI được sinh ra.</summary>
        public const string UIPrefabRoot = "Assets/Project/Prefabs/UI";

        /// <summary>Thư mục prefab của các màn hình.</summary>
        public const string ScreenPrefabFolder = UIPrefabRoot + "/Screens";

        /// <summary>Thư mục prefab của các cell dùng trong list/scroll.</summary>
        public const string CellPrefabFolder = UIPrefabRoot + "/Cells";

        /// <summary>Thư mục art UI — nơi SpriteBinder đi tìm sprite theo tên.</summary>
        public const string SpriteFolder = "Assets/Project/Textures/UI";

        /// <summary>Tên file báo cáo xuất ra trong thư mục ui_layout.</summary>
        public const string ReportFileName = "import_report.md";

        /// <summary>
        /// Tìm thư mục <c>ui_layout</c> chứa <c>_index.json</c>.
        /// Thử lần lượt: EditorPrefs → các vị trí quy ước quanh project → hộp thoại chọn thư mục.
        /// </summary>
        /// <param name="askUser">Cho phép mở hộp thoại chọn thư mục khi không tự dò được.</param>
        /// <returns>Đường dẫn tuyệt đối, hoặc null nếu không tìm thấy.</returns>
        public static string FindLayoutRoot(bool askUser = true)
        {
            string saved = EditorPrefs.GetString(LayoutRootPrefsKey, string.Empty);
            if (IsLayoutRoot(saved)) return saved;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string[] candidates =
            {
                Combine(projectRoot, "..", "ui_layout"),
                Combine(projectRoot, "..", "..", "ui_layout"),
                Combine(projectRoot, "ui_layout"),
                Combine(projectRoot, "..", "reverse-engineering", "ui_layout"),
                @"E:\pikeball\reverse-engineering\ui_layout"
            };

            foreach (string candidate in candidates)
            {
                if (!IsLayoutRoot(candidate)) continue;
                EditorPrefs.SetString(LayoutRootPrefsKey, candidate);
                return candidate;
            }

            if (!askUser) return null;

            string picked = EditorUtility.OpenFolderPanel("Chọn thư mục ui_layout (chứa _index.json)", projectRoot, string.Empty);
            if (!IsLayoutRoot(picked)) return null;
            EditorPrefs.SetString(LayoutRootPrefsKey, picked);
            return picked;
        }

        /// <summary>Kiểm tra một thư mục có phải thư mục layout hợp lệ hay không.</summary>
        /// <param name="folder">Đường dẫn thư mục cần kiểm tra.</param>
        public static bool IsLayoutRoot(string folder)
        {
            return !string.IsNullOrEmpty(folder)
                   && Directory.Exists(folder)
                   && File.Exists(Path.Combine(folder, "_index.json"));
        }

        /// <summary>Tạo (đệ quy) thư mục asset nếu chưa tồn tại.</summary>
        /// <param name="assetFolder">Đường dẫn kiểu <c>Assets/...</c>.</param>
        public static void EnsureAssetFolder(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetFolder)) AssetDatabase.CreateFolder(parent, leaf);
        }

        private static string Combine(params string[] parts)
        {
            try { return Path.GetFullPath(Path.Combine(parts)); }
            catch { return string.Empty; }
        }
    }

    /// <summary>
    /// Tra <see cref="Type"/> theo tên class đọc từ JSON, hoàn toàn bằng reflection.
    /// Nhờ vậy importer không phụ thuộc compile-time vào tầng code UI mà agent khác đang viết:
    /// class nào đã có thì gắn được, class nào chưa có thì bỏ qua và ghi vào báo cáo.
    /// </summary>
    public static class UILayoutTypeResolver
    {
        /// <summary>Các namespace được thử khi tên đầy đủ trong JSON không tra được.</summary>
        public static readonly string[] FallbackNamespaces =
        {
            "Pickleball",
            "Pickleball.UI",
            "StarterKit.UIKit",
            "StarterKit.UIKit.UIEffects",
            "StarterKit.UI",
            "UnityEngine.UI",
            "TMPro",
            "UnityEngine"
        };

        private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();
        private static readonly HashSet<string> FuzzyResolved = new HashSet<string>();
        private static Dictionary<string, List<Type>> _componentsByShortName;

        /// <summary>
        /// Danh sách tên class đã phải dùng đến bước dò mờ (không phân biệt hoa thường).
        /// Dùng để cảnh báo trong báo cáo vì đây là phép đoán, không phải khớp chính xác.
        /// </summary>
        public static IEnumerable<string> FuzzyMatches => FuzzyResolved;

        /// <summary>Xoá cache tra type (dùng khi vừa có thêm class mới được compile).</summary>
        public static void ClearCache()
        {
            Cache.Clear();
            FuzzyResolved.Clear();
            _componentsByShortName = null;
        }

        /// <summary>
        /// Tra type component theo tên class trong JSON.
        /// Thứ tự: tên đầy đủ trong mọi assembly đã nạp → tên rút gọn trong các namespace quy ước →
        /// dò tên rút gọn (khớp chính xác rồi tới không phân biệt hoa thường) trong mọi assembly.
        /// </summary>
        /// <param name="className">Tên class đọc từ JSON, ví dụ <c>StarterKit.UI.MainMenuUI</c>.</param>
        /// <returns>Type kế thừa <see cref="Component"/>, hoặc null nếu không tra được.</returns>
        /// <summary>
        /// Bảng đổi tên: class trong bản gốc → class tương ứng của dự án này.
        /// Dùng khi bản gốc gắn thẳng một class mà ở đây là <c>abstract</c> (lớp cơ sở),
        /// hoặc khi ta cố ý đặt tên khác.
        /// </summary>
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Bản gốc gắn PopupUI (concrete) lên prefab "CommonPopupUI"; ở đây PopupUI là lớp
            // cơ sở abstract nên importer bỏ qua — trỏ sang class cụ thể tương đương.
            { "PopupUI", "CommonPopupUI" },
        };

        public static Type Resolve(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;
            if (Aliases.TryGetValue(className, out string alias)) className = alias;
            if (Cache.TryGetValue(className, out Type cached)) return cached;

            Type found = ResolveUncached(className);
            Cache[className] = found;
            return found;
        }

        private static Type ResolveUncached(string className)
        {
            // 1. Tên đầy đủ.
            Type direct = FindExact(className);
            if (direct != null) return direct;

            string shortName = ShortName(className);

            // 2. Tên rút gọn trong các namespace quy ước.
            foreach (string ns in FallbackNamespaces)
            {
                Type candidate = FindExact(ns + "." + shortName);
                if (candidate != null) return candidate;
            }

            // 3. Dò tên rút gọn trong mọi assembly.
            Dictionary<string, List<Type>> map = ComponentsByShortName();
            if (map.TryGetValue(shortName, out List<Type> exact) && exact.Count > 0)
                return Prefer(exact);

            // 4. Dò mờ: không phân biệt hoa thường (bắt các sai lệch kiểu Matchmaking / MatchMaking).
            foreach (KeyValuePair<string, List<Type>> pair in map)
            {
                if (!string.Equals(pair.Key, shortName, StringComparison.OrdinalIgnoreCase)) continue;
                if (pair.Value.Count == 0) continue;
                FuzzyResolved.Add(className + " → " + pair.Value[0].FullName);
                return Prefer(pair.Value);
            }

            return null;
        }

        private static Type FindExact(string fullName)
        {
            Type t = Type.GetType(fullName, false);
            if (IsUsable(t)) return t;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName, false);
                    if (IsUsable(t)) return t;
                }
                catch
                {
                    // Assembly động / không nạp được type — bỏ qua.
                }
            }

            return null;
        }

        private static bool IsUsable(Type t)
        {
            return t != null && !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(Component).IsAssignableFrom(t);
        }

        private static Type Prefer(List<Type> candidates)
        {
            // Ưu tiên code của project, rồi tới UnityEngine.UI / TMPro, cuối cùng là phần còn lại.
            return candidates
                .OrderByDescending(t => Score(t))
                .ThenBy(t => t.FullName, StringComparer.Ordinal)
                .First();
        }

        private static int Score(Type t)
        {
            string ns = t.Namespace ?? string.Empty;
            if (ns.StartsWith("Pickleball", StringComparison.Ordinal)) return 5;
            if (ns.StartsWith("StarterKit", StringComparison.Ordinal)) return 4;
            if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal)) return 3;
            if (ns.StartsWith("TMPro", StringComparison.Ordinal)) return 2;
            if (ns.StartsWith("UnityEngine", StringComparison.Ordinal)) return 1;
            if (ns.Length == 0) return 4; // Script không namespace của project.
            return 0;
        }

        private static Dictionary<string, List<Type>> ComponentsByShortName()
        {
            if (_componentsByShortName != null) return _componentsByShortName;

            _componentsByShortName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.GetName().Name;
                if (asmName.StartsWith("System", StringComparison.Ordinal)
                    || asmName.StartsWith("mscorlib", StringComparison.Ordinal)
                    || asmName.StartsWith("Mono.", StringComparison.Ordinal)
                    || asmName.StartsWith("netstandard", StringComparison.Ordinal))
                    continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (Type t in types)
                {
                    if (!IsUsable(t)) continue;
                    if (!_componentsByShortName.TryGetValue(t.Name, out List<Type> list))
                    {
                        list = new List<Type>();
                        _componentsByShortName[t.Name] = list;
                    }
                    list.Add(t);
                }
            }

            return _componentsByShortName;
        }

        /// <summary>Lấy phần tên sau dấu chấm cuối cùng của tên class đầy đủ.</summary>
        /// <param name="className">Tên class đầy đủ.</param>
        public static string ShortName(string className)
        {
            if (string.IsNullOrEmpty(className)) return string.Empty;
            int dot = className.LastIndexOf('.');
            return dot >= 0 && dot < className.Length - 1 ? className.Substring(dot + 1) : className;
        }
    }

    /// <summary>
    /// Bảng tra sprite theo tên trong thư mục art UI. Khớp không phân biệt hoa thường và bỏ qua đuôi file.
    /// </summary>
    public sealed class UISpriteIndex
    {
        private readonly Dictionary<string, Sprite> _byName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Số sprite tìm được trong thư mục art.</summary>
        public int Count => _byName.Count;

        /// <summary>
        /// Quét thư mục art và dựng bảng tra. Thư mục chưa tồn tại thì trả về bảng rỗng (không phải lỗi).
        /// </summary>
        /// <param name="assetFolder">Thư mục kiểu <c>Assets/...</c> chứa sprite.</param>
        public static UISpriteIndex Build(string assetFolder)
        {
            var index = new UISpriteIndex();
            if (!AssetDatabase.IsValidFolder(assetFolder)) return index;

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { assetFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Sprite sprite) index.Add(sprite.name, sprite);
                }

                // Sprite đơn: cho phép khớp cả theo tên file.
                var main = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (main != null) index.Add(Path.GetFileNameWithoutExtension(path), main);
            }

            return index;
        }

        private void Add(string key, Sprite sprite)
        {
            if (string.IsNullOrEmpty(key) || sprite == null) return;
            if (!_byName.ContainsKey(key)) _byName[key] = sprite;
        }

        /// <summary>Tìm sprite theo tên gốc trong bản build cũ.</summary>
        /// <param name="originalName">Tên sprite ghi trong JSON layout.</param>
        /// <param name="sprite">Sprite tìm được.</param>
        /// <returns>true nếu tìm thấy.</returns>
        public bool TryGet(string originalName, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(originalName)) return false;

            string name = originalName.Trim();
            if (_byName.TryGetValue(name, out sprite)) return true;

            string noExt = Path.GetFileNameWithoutExtension(name);
            if (!string.Equals(noExt, name, StringComparison.Ordinal) && _byName.TryGetValue(noExt, out sprite)) return true;

            return false;
        }
    }

    /// <summary>
    /// Số liệu thu thập trong một lần import, dùng để in tổng kết ra Console.
    /// </summary>
    public sealed class UILayoutImportStats
    {
        /// <summary>Số prefab đã ghi thành công.</summary>
        public int PrefabCount;

        /// <summary>Tổng số node đã dựng.</summary>
        public int NodeCount;

        /// <summary>Tổng số component đã gắn.</summary>
        public int ComponentCount;

        /// <summary>Số component cố ý bỏ qua (VFX, package bên thứ ba, localization).</summary>
        public int SkippedCount;

        /// <summary>Tên class không tra được type, kèm số lần gặp.</summary>
        public readonly Dictionary<string, int> UnresolvedClasses = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Tên sprite chưa có file art, kèm số lần dùng.</summary>
        public readonly Dictionary<string, int> MissingSprites = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Các node có component layout cần căn tay.</summary>
        public readonly List<string> LayoutNodes = new List<string>();

        /// <summary>Các sự cố không chặn quá trình import (AddComponent trả null, ngoại lệ lẻ...).</summary>
        public readonly List<string> Warnings = new List<string>();

        /// <summary>Ghi nhận một class không tra được type.</summary>
        /// <param name="className">Tên class trong JSON.</param>
        public void AddUnresolved(string className) => Bump(UnresolvedClasses, className);

        /// <summary>Ghi nhận một sprite còn thiếu file art.</summary>
        /// <param name="spriteName">Tên sprite gốc.</param>
        public void AddMissingSprite(string spriteName) => Bump(MissingSprites, spriteName);

        private static void Bump(Dictionary<string, int> map, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            map.TryGetValue(key, out int n);
            map[key] = n + 1;
        }

        /// <summary>Dựng chuỗi tổng kết ngắn để in ra Console sau khi import.</summary>
        public string BuildConsoleSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[UILayoutImporter] Đã sinh {PrefabCount} prefab / {NodeCount} node / {ComponentCount} component.");
            sb.AppendLine($"  - Component cố ý bỏ qua: {SkippedCount}");
            sb.AppendLine($"  - Class không tra được type: {UnresolvedClasses.Count} loại ({UnresolvedClasses.Values.Sum()} lần)");
            sb.AppendLine($"  - Sprite còn thiếu art: {MissingSprites.Count} tên ({MissingSprites.Values.Sum()} chỗ dùng)");
            sb.AppendLine($"  - Node có component layout cần căn tay: {LayoutNodes.Count}");
            if (UnresolvedClasses.Count > 0)
            {
                string list = string.Join(", ", UnresolvedClasses.OrderByDescending(p => p.Value).Take(12).Select(p => $"{p.Key} x{p.Value}"));
                sb.AppendLine("  - Thiếu nhiều nhất: " + list);
            }
            sb.Append("  → Chạy 'Pickleball/UI/Layout Report' để xuất báo cáo đầy đủ.");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Sinh prefab UI từ các file layout JSON đã trích từ bản build gốc.
    /// Chạy lại bao nhiêu lần cũng được: prefab cũ bị ghi đè tại đúng đường dẫn nên GUID được giữ nguyên.
    /// </summary>
    public static class UILayoutImporter
    {
        /// <summary>Cỡ chữ mặc định do importer chọn — bản gốc không lộ fontSize, cần căn tay sau.</summary>
        public const float DefaultFontSize = 36f;

        /// <summary>
        /// Các class cố ý bỏ qua: VFX hạt, package bên thứ ba (Coffee UIEffects, SimpleScrollSnap) và localization.
        /// </summary>
        public static readonly HashSet<string> SkippedClasses = new HashSet<string>(StringComparer.Ordinal)
        {
            "ParticleSystem",
            "UnityEngine.ParticleSystem",
            "ParticleSystemRenderer",
            "UnityEngine.ParticleSystemRenderer",
            "UIParticle",
            "Coffee.UIExtensions.UIParticle",
            "Coffee.UIExtensions.UIParticleAttractor",
            "ShinyEffectForUGUI",
            "Coffee.UIExtensions.ShinyEffectForUGUI",
            "UIEffect",
            "Coffee.UIEffects.UIEffect",
            "Coffee.UIEffects.UIEffectTweener",
            "UnityEngine.Localization.Components.LocalizeStringEvent",
            "LocalizeStringEvent",
            "DanielLochner.Assets.SimpleScrollSnap.SimpleScrollSnap",
            "DanielLochner.Assets.SimpleScrollSnap.Scale"
        };

        /// <summary>
        /// Các component layout được gắn với giá trị mặc định vì bản gốc không lộ tham số — phải căn tay sau.
        /// </summary>
        public static readonly HashSet<string> LayoutClasses = new HashSet<string>(StringComparer.Ordinal)
        {
            "UnityEngine.UI.HorizontalLayoutGroup",
            "UnityEngine.UI.VerticalLayoutGroup",
            "UnityEngine.UI.GridLayoutGroup",
            "UnityEngine.UI.ContentSizeFitter",
            "UnityEngine.UI.AspectRatioFitter",
            "UnityEngine.UI.LayoutElement",
            "UnityEngine.UI.ScrollRect",
            "UnityEngine.UI.Mask",
            "UnityEngine.UI.RectMask2D",
            "CanvasGroup",
            "UnityEngine.CanvasGroup"
        };

        private sealed class BuildContext
        {
            public UISpriteIndex Sprites;
            public UILayoutImportStats Stats;
            public TMP_FontAsset Font;
            public string Screen;
            public CanvasConfig Canvas;
        }

        private struct CanvasConfig
        {
            public Vector2 ReferenceResolution;
            public float Match;
            public float ReferencePixelsPerUnit;

            public static CanvasConfig Default => new CanvasConfig
            {
                ReferenceResolution = new Vector2(1080f, 1920f),
                Match = 0.5f,
                ReferencePixelsPerUnit = 100f
            };
        }

        /// <summary>
        /// Sinh toàn bộ prefab UI theo <c>_index.json</c>: màn hình vào <c>Prefabs/UI/Screens</c>,
        /// cell vào <c>Prefabs/UI/Cells</c>.
        /// </summary>
        [MenuItem("Pickleball/UI/Import All UI Layouts")]
        public static void ImportAllLayouts()
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogError("[UILayoutImporter] Không tìm thấy thư mục ui_layout (cần có _index.json).");
                return;
            }

            JObject index;
            try
            {
                index = JObject.Parse(File.ReadAllText(Path.Combine(layoutRoot, "_index.json")));
            }
            catch (Exception e)
            {
                Debug.LogError($"[UILayoutImporter] Không đọc được _index.json: {e.Message}");
                return;
            }

            if (!(index["screens"] is JArray entries) || entries.Count == 0)
            {
                Debug.LogError("[UILayoutImporter] _index.json không có mảng 'screens'.");
                return;
            }

            if (!EnsureTextMeshProResources()) return;

            UILayoutTypeResolver.ClearCache();
            CanvasConfig canvas = ReadCanvasConfig(index["canvasScaler"]);
            UILayoutPaths.EnsureAssetFolder(UILayoutPaths.ScreenPrefabFolder);
            UILayoutPaths.EnsureAssetFolder(UILayoutPaths.CellPrefabFolder);

            var stats = new UILayoutImportStats();
            var sprites = UISpriteIndex.Build(UILayoutPaths.SpriteFolder);
            TMP_FontAsset font = FindDefaultFont();

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i] as JObject;
                    if (entry == null) continue;

                    string screenName = CleanName((string)entry["screen"]);
                    string relative = (string)entry["file"];
                    if (string.IsNullOrEmpty(relative)) continue;

                    string jsonPath = Path.Combine(layoutRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(jsonPath))
                    {
                        stats.Warnings.Add($"Thiếu file layout: {relative}");
                        Debug.LogWarning($"[UILayoutImporter] Thiếu file layout: {jsonPath}");
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("Import UI Layouts", $"{screenName} ({i + 1}/{entries.Count})",
                        (i + 1) / (float)entries.Count);

                    ImportFile(jsonPath, screenName, canvas, sprites, font, stats);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(stats.BuildConsoleSummary());
        }

        /// <summary>
        /// Sinh prefab cho một file layout JSON do người dùng chọn trong hộp thoại.
        /// File có tiền tố <c>CELL_</c> được lưu vào thư mục Cells, còn lại vào Screens.
        /// </summary>
        [MenuItem("Pickleball/UI/Import Single Layout...")]
        public static void ImportSingleLayout()
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            string startFolder = layoutRoot != null ? Path.Combine(layoutRoot, "screens") : Application.dataPath;

            string jsonPath = EditorUtility.OpenFilePanel("Chọn file layout JSON", startFolder, "json");
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) return;

            if (string.Equals(Path.GetFileName(jsonPath), "_index.json", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[UILayoutImporter] Đây là file index — dùng 'Import All UI Layouts'.");
                return;
            }

            if (!EnsureTextMeshProResources()) return;

            UILayoutTypeResolver.ClearCache();
            CanvasConfig canvas = CanvasConfig.Default;
            if (layoutRoot != null)
            {
                try
                {
                    JObject index = JObject.Parse(File.ReadAllText(Path.Combine(layoutRoot, "_index.json")));
                    canvas = ReadCanvasConfig(index["canvasScaler"]);
                }
                catch
                {
                    // Không đọc được index thì dùng cấu hình mặc định 1080x1920.
                }
            }

            UILayoutPaths.EnsureAssetFolder(UILayoutPaths.ScreenPrefabFolder);
            UILayoutPaths.EnsureAssetFolder(UILayoutPaths.CellPrefabFolder);

            var stats = new UILayoutImportStats();
            string screenName = Path.GetFileNameWithoutExtension(jsonPath);
            try
            {
                ImportFile(jsonPath, screenName, canvas, UISpriteIndex.Build(UILayoutPaths.SpriteFolder), FindDefaultFont(), stats);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(stats.BuildConsoleSummary());
        }

        private static void ImportFile(string jsonPath, string screenName, CanvasConfig canvas,
            UISpriteIndex sprites, TMP_FontAsset font, UILayoutImportStats stats)
        {
            JObject rootNode;
            try
            {
                rootNode = JObject.Parse(File.ReadAllText(jsonPath));
            }
            catch (Exception e)
            {
                stats.Warnings.Add($"{screenName}: JSON hỏng — {e.Message}");
                Debug.LogError($"[UILayoutImporter] {screenName}: không parse được JSON — {e.Message}");
                return;
            }

            bool isCell = IsCellLayout(jsonPath);
            string prefabPath = ResolvePrefabPath(jsonPath, screenName);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            var ctx = new BuildContext { Sprites = sprites, Stats = stats, Font = font, Screen = prefabName, Canvas = canvas };
            GameObject root = null;
            try
            {
                root = BuildNode(rootNode, null, !isCell, ctx, string.Empty);
                if (root == null) return;

                if (!isCell) ConfigureScreenRoot(root, canvas, ctx);

                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out saved);
                if (saved) stats.PrefabCount++;
                else
                {
                    stats.Warnings.Add($"{prefabName}: ghi prefab thất bại ({prefabPath}).");
                    Debug.LogError($"[UILayoutImporter] Ghi prefab thất bại: {prefabPath}");
                }
            }
            catch (Exception e)
            {
                stats.Warnings.Add($"{prefabName}: lỗi khi dựng — {e.Message}");
                Debug.LogError($"[UILayoutImporter] {prefabName}: lỗi khi dựng — {e}");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildNode(JObject node, Transform parent, bool isPrefabRoot, BuildContext ctx, string parentPath)
        {
            string name = (string)node["name"];
            if (string.IsNullOrEmpty(name)) name = "GameObject";
            string path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;

            var go = new GameObject(name);
            ctx.Stats.NodeCount++;

            var rect = node["rect"] as JObject;
            bool wantsRectTransform = isPrefabRoot || rect?["anchorMin"] != null;
            RectTransform rectTransform = wantsRectTransform ? go.AddComponent<RectTransform>() : null;

            go.transform.SetParent(parent, false);
            ApplyTransform(go, rectTransform, rect, path, ctx);

            // Component của chính node này (trước khi dựng con, để Button/ScrollRect có sẵn khi fixup).
            if (node["components"] is JArray components)
            {
                foreach (JToken token in components)
                {
                    if (token is JObject component) AddComponentFromJson(go, component, path, isPrefabRoot, ctx);
                }
            }

            if (node["children"] is JArray children)
            {
                foreach (JToken token in children)
                {
                    if (token is JObject child) BuildNode(child, go.transform, false, ctx, path);
                }
            }

            WireReferences(go, path, ctx);

            // active = false phải đặt SAU khi con đã dựng xong, nếu không con sẽ không chạy khởi tạo.
            JToken activeToken = node["active"];
            bool active = activeToken == null || activeToken.Type != JTokenType.Boolean || (bool)activeToken;
            if (!active) go.SetActive(false);

            return go;
        }

        private static void ApplyTransform(GameObject go, RectTransform rect, JObject data, string path, BuildContext ctx)
        {
            if (data == null) return;

            if (rect != null && data["anchorMin"] != null)
            {
                // Thứ tự quan trọng: anchor + pivot trước, sizeDelta/anchoredPosition sau.
                rect.anchorMin = ReadVector2(data["anchorMin"], Vector2.zero);
                rect.anchorMax = ReadVector2(data["anchorMax"], Vector2.one);
                rect.pivot = ReadVector2(data["pivot"], new Vector2(0.5f, 0.5f));
                rect.sizeDelta = ReadVector2(data["sizeDelta"], Vector2.zero);
                rect.anchoredPosition = ReadVector2(data["anchoredPosition"], Vector2.zero);
            }

            if (data["localPosition"] != null && data["anchorMin"] == null)
                go.transform.localPosition = ReadVector3(data["localPosition"], Vector3.zero);

            if (data["localScale"] != null)
            {
                Vector3 scale = ReadVector3(data["localScale"], Vector3.one);
                // (0,0,0) KHÔNG phải lỗi trích xuất: Unity serialize RectTransform của Canvas
                // ScreenSpaceOverlay với localScale = 0 vì giá trị thật được Canvas tính lại lúc
                // runtime theo scale factor. Gặp ở 35 node (29 Canvas gốc + 6 con). Phải ép về 1,
                // nếu không prefab sẽ tàng hình.
                // LƯU Ý: chỉ ép khi scale ~ 0. Các giá trị (-1,1,1) x21 và (1,-1,1) x4 là sprite
                // LẬT GƯƠNG cố ý của bản gốc — ép chúng về 1 sẽ làm sai bố cục.
                if (scale.sqrMagnitude < 1e-8f)
                {
                    ctx.Stats.Warnings.Add($"{ctx.Screen}/{path}: localScale = (0,0,0) (Canvas overlay) → ép về (1,1,1).");
                }
                else
                {
                    go.transform.localScale = scale;
                }
            }
        }

        private static void AddComponentFromJson(GameObject go, JObject component, string path, bool isPrefabRoot, BuildContext ctx)
        {
            string className = (string)component["class"];
            if (string.IsNullOrEmpty(className)) return;

            if (SkippedClasses.Contains(className))
            {
                ctx.Stats.SkippedCount++;
                return;
            }

            switch (className)
            {
                case "UnityEngine.UI.Image":
                case "Image":
                    AddImage(go, component, path, ctx);
                    return;
                case "TMPro.TextMeshProUGUI":
                case "TextMeshProUGUI":
                    AddText(go, component, path, ctx);
                    return;
            }

            Type type = UILayoutTypeResolver.Resolve(className);
            if (type == null)
            {
                ctx.Stats.AddUnresolved(className);
                return;
            }

            Component added = GetOrAdd(go, type, path, ctx);
            if (added == null) return;

            ctx.Stats.ComponentCount++;
            if (LayoutClasses.Contains(className)) ctx.Stats.LayoutNodes.Add($"{ctx.Screen}/{path} → {UILayoutTypeResolver.ShortName(className)}");

            switch (added)
            {
                case RawImage rawImage:
                    rawImage.color = ReadColor(component["color"], Color.white);
                    break;
                case CanvasScaler scaler:
                    // Cấu hình chuẩn 1080x1920 áp cho mọi CanvasScaler, kể cả canvas lồng nhau.
                    ApplyCanvasScaler(scaler, ctx.Canvas);
                    break;
                case Canvas canvas when isPrefabRoot:
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
            }
        }

        private static void ApplyCanvasScaler(CanvasScaler scaler, CanvasConfig config)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = config.Match;
            scaler.referencePixelsPerUnit = config.ReferencePixelsPerUnit;
        }

        private static void AddImage(GameObject go, JObject component, string path, BuildContext ctx)
        {
            var image = GetOrAdd(go, typeof(Image), path, ctx) as Image;
            if (image == null) return;

            ctx.Stats.ComponentCount++;
            Color color = ReadColor(component["color"], Color.white);
            image.color = color;

            string spriteName = (string)component["sprite"];
            if (string.IsNullOrEmpty(spriteName)) return;

            if (ctx.Sprites.TryGet(spriteName, out Sprite sprite))
            {
                image.sprite = sprite;
                return;
            }

            // Chưa có art: để sprite = null và ghi tên gốc lại để SpriteBinder gán hàng loạt sau.
            image.sprite = null;
            var placeholder = go.GetComponent<SpritePlaceholder>();
            if (placeholder == null) placeholder = go.AddComponent<SpritePlaceholder>();
            if (placeholder != null)
            {
                placeholder.originalSpriteName = spriteName;
                placeholder.fallbackColor = color;
            }

            ctx.Stats.AddMissingSprite(spriteName);
        }

        private static void AddText(GameObject go, JObject component, string path, BuildContext ctx)
        {
            var text = GetOrAdd(go, typeof(TextMeshProUGUI), path, ctx) as TextMeshProUGUI;
            if (text == null) return;

            ctx.Stats.ComponentCount++;
            if (ctx.Font != null && text.font == null) text.font = ctx.Font;

            text.text = DecodeText((string)component["text"]);
            text.color = ReadColor(component["color"], Color.white);

            // Bản gốc không lộ fontSize/alignment — đây là giá trị mặc định do importer chọn, cần căn tay.
            text.fontSize = DefaultFontSize;
            text.alignment = TextAlignmentOptions.Center;
            SetWordWrapping(text, true);
        }

        /// <summary>
        /// Bật/tắt xuống dòng cho TMP. Dùng reflection vì Unity 6 đổi
        /// <c>enableWordWrapping</c> (đã deprecated) thành <c>textWrappingMode</c>.
        /// </summary>
        /// <param name="text">Component TMP cần chỉnh.</param>
        /// <param name="wrap">true = cho phép xuống dòng.</param>
        public static void SetWordWrapping(TMP_Text text, bool wrap)
        {
            if (text == null) return;

            PropertyInfo modeProperty = typeof(TMP_Text).GetProperty("textWrappingMode", BindingFlags.Public | BindingFlags.Instance);
            if (modeProperty != null && modeProperty.PropertyType.IsEnum)
            {
                try
                {
                    object value = Enum.Parse(modeProperty.PropertyType, wrap ? "Normal" : "NoWrap");
                    modeProperty.SetValue(text, value);
                    return;
                }
                catch
                {
                    // Enum đổi tên ở version khác — rơi xuống nhánh cũ bên dưới.
                }
            }

            PropertyInfo legacy = typeof(TMP_Text).GetProperty("enableWordWrapping", BindingFlags.Public | BindingFlags.Instance);
            if (legacy != null && legacy.PropertyType == typeof(bool)) legacy.SetValue(text, wrap);
        }

        private static void WireReferences(GameObject go, string path, BuildContext ctx)
        {
            var button = go.GetComponent<Button>();
            if (button != null && button.targetGraphic == null)
            {
                var graphic = go.GetComponent<Graphic>();
                if (graphic != null) button.targetGraphic = graphic;
            }

            var scrollRect = go.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                // Bản gốc không lộ tham số ScrollRect; nối tạm theo quy ước tên con để scroll không rỗng.
                if (scrollRect.content == null)
                {
                    RectTransform content = FindChildRect(go.transform, "Content");
                    if (content != null) scrollRect.content = content;
                }

                if (scrollRect.viewport == null)
                {
                    RectTransform viewport = FindChildRect(go.transform, "Viewport") ?? FindChildRect(go.transform, "Mask");
                    if (viewport != null) scrollRect.viewport = viewport;
                }

                if (scrollRect.content == null)
                    ctx.Stats.Warnings.Add($"{ctx.Screen}/{path}: ScrollRect chưa có Content — cần nối tay.");
            }
        }

        private static RectTransform FindChildRect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase)) return child as RectTransform;
            }

            return null;
        }

        private static void ConfigureScreenRoot(GameObject root, CanvasConfig config, BuildContext ctx)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.AddComponent<Canvas>();
            if (canvas != null) canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = root.AddComponent<CanvasScaler>();
            ApplyCanvasScaler(scaler, config);

            if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();

            // Node gốc của Canvas luôn full-screen; giá trị rect trong JSON ở đây là rác do Canvas ghi đè.
            var rect = root.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            EnsureScreenScript(root, ctx);
        }

        private static void EnsureScreenScript(GameObject root, BuildContext ctx)
        {
            // Script màn hình thường đã nằm trong "components" của node gốc và được gắn ở bước dựng.
            // Nếu chưa gắn được (class chưa tồn tại lúc đó), thử thêm theo tên màn hình.
            foreach (Component c in root.GetComponents<Component>())
            {
                if (c == null) continue;
                Type t = c.GetType();
                if (t == typeof(Canvas) || t == typeof(CanvasScaler) || t == typeof(GraphicRaycaster) || t == typeof(RectTransform)) continue;
                if (typeof(MonoBehaviour).IsAssignableFrom(t)) return; // Đã có script màn hình.
            }

            Type screenType = UILayoutTypeResolver.Resolve(ctx.Screen);
            if (screenType == null) return;
            if (GetOrAdd(root, screenType, ctx.Screen, ctx) != null) ctx.Stats.ComponentCount++;
        }

        private static Component GetOrAdd(GameObject go, Type type, string path, BuildContext ctx)
        {
            Component existing = go.GetComponent(type);
            if (existing != null) return existing;

            try
            {
                Component added = go.AddComponent(type);
                if (added == null)
                {
                    // Thường do [DisallowMultipleComponent] (ví dụ hai Graphic trên cùng node).
                    ctx.Stats.Warnings.Add($"{ctx.Screen}/{path}: không gắn được {type.FullName} (xung đột component).");
                }

                return added;
            }
            catch (Exception e)
            {
                ctx.Stats.Warnings.Add($"{ctx.Screen}/{path}: lỗi khi gắn {type.FullName} — {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra TMP Essential Resources đã được import chưa. Thiếu font mặc định thì mọi
        /// TextMeshProUGUI sinh ra sẽ có font null và text không hiện.
        /// </summary>
        /// <returns>true nếu được phép tiếp tục import.</returns>
        private static bool EnsureTextMeshProResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            int choice = EditorUtility.DisplayDialogComplex(
                "Thiếu TMP Essential Resources",
                "Project chưa import TMP Essential Resources.\n" +
                "Nếu import UI ngay bây giờ, toàn bộ TextMeshProUGUI sẽ có font = null và chữ sẽ không hiện.\n\n" +
                "Nên chọn 'Import TMP ngay', hoặc chạy Window > TextMeshPro > Import TMP Essential Resources rồi thử lại.",
                "Import TMP ngay", "Huỷ", "Vẫn tiếp tục");

            switch (choice)
            {
                case 0:
                    if (!TryImportTmpEssentials())
                    {
                        Debug.LogError("[UILayoutImporter] Không tự import được TMP Essentials — hãy dùng Window > TextMeshPro > Import TMP Essential Resources.");
                        return false;
                    }

                    AssetDatabase.Refresh();
                    return true;
                case 2:
                    Debug.LogWarning("[UILayoutImporter] Đang import UI khi chưa có TMP Essentials — chữ sẽ không có font.");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryImportTmpEssentials()
        {
            try
            {
                Type importer = UILayoutTypeResolverHelper.FindType("TMPro.TMP_PackageResourceImporter");
                MethodInfo method = importer?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static);
                if (method == null) return false;
                method.Invoke(null, new object[] { true, false, false });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UILayoutImporter] Import TMP Essentials thất bại: {e.Message}");
                return false;
            }
        }

        private static TMP_FontAsset FindDefaultFont()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return null;

            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch
            {
                return null;
            }
        }

        private static CanvasConfig ReadCanvasConfig(JToken token)
        {
            CanvasConfig config = CanvasConfig.Default;
            if (!(token is JObject obj)) return config;

            config.ReferenceResolution = ReadVector2(obj["referenceResolution"], config.ReferenceResolution);
            if (obj["match"] != null) config.Match = (float)obj["match"];
            if (obj["referencePixelsPerUnit"] != null) config.ReferencePixelsPerUnit = (float)obj["referencePixelsPerUnit"];
            return config;
        }

        /// <summary>File layout có phải cell prefab hay không (quy ước tiền tố <c>CELL_</c>).</summary>
        /// <param name="jsonPath">Đường dẫn file layout JSON.</param>
        public static bool IsCellLayout(string jsonPath)
        {
            return !string.IsNullOrEmpty(jsonPath)
                   && Path.GetFileName(jsonPath).StartsWith("CELL_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Suy ra đường dẫn prefab tương ứng với một file layout — dùng chung giữa importer và
        /// báo cáo để hai bên luôn nhìn vào cùng một chỗ.
        /// </summary>
        /// <param name="jsonPath">Đường dẫn file layout JSON.</param>
        /// <param name="screenName">Tên màn hình trong _index.json (đã bỏ hậu tố), có thể để trống.</param>
        /// <returns>Đường dẫn kiểu <c>Assets/Project/Prefabs/UI/...</c>.</returns>
        public static string ResolvePrefabPath(string jsonPath, string screenName)
        {
            bool isCell = IsCellLayout(jsonPath);
            string prefabName = SanitizeFileName(string.IsNullOrEmpty(screenName)
                ? Path.GetFileNameWithoutExtension(jsonPath)
                : screenName);
            if (prefabName.StartsWith("CELL_", StringComparison.OrdinalIgnoreCase))
                prefabName = prefabName.Substring(5);

            string folder = isCell ? UILayoutPaths.CellPrefabFolder : UILayoutPaths.ScreenPrefabFolder;
            return $"{folder}/{prefabName}.prefab";
        }

        /// <summary>Bỏ hậu tố "(cell prefab)" trong tên màn hình lấy từ _index.json.</summary>
        /// <param name="raw">Tên gốc trong index.</param>
        public static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            int paren = raw.IndexOf("(cell prefab)", StringComparison.OrdinalIgnoreCase);
            string cleaned = paren >= 0 ? raw.Substring(0, paren) : raw;
            return cleaned.Trim();
        }

        /// <summary>Loại các ký tự không hợp lệ trong tên file khỏi tên prefab.</summary>
        /// <param name="raw">Tên gốc.</param>
        public static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unnamed";
            var sb = new StringBuilder(raw.Length);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in raw)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            string result = sb.ToString().Trim();
            return result.Length == 0 ? "Unnamed" : result;
        }

        /// <summary>
        /// Chuyển các escape dạng văn bản còn sót trong JSON ("\\n", "\\t") thành ký tự thật.
        /// </summary>
        /// <param name="raw">Chuỗi text đọc từ JSON.</param>
        public static string DecodeText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return raw.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\t", "\t");
        }

        private static Vector2 ReadVector2(JToken token, Vector2 fallback)
        {
            if (!(token is JArray array) || array.Count < 2) return fallback;
            return new Vector2(ToFloat(array[0], fallback.x), ToFloat(array[1], fallback.y));
        }

        private static Vector3 ReadVector3(JToken token, Vector3 fallback)
        {
            if (!(token is JArray array) || array.Count < 3) return fallback;
            return new Vector3(ToFloat(array[0], fallback.x), ToFloat(array[1], fallback.y), ToFloat(array[2], fallback.z));
        }

        private static Color ReadColor(JToken token, Color fallback)
        {
            if (!(token is JArray array) || array.Count < 4) return fallback;
            return new Color(ToFloat(array[0], 1f), ToFloat(array[1], 1f), ToFloat(array[2], 1f), ToFloat(array[3], 1f));
        }

        private static float ToFloat(JToken token, float fallback)
        {
            try
            {
                float value = (float)token;
                return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }
    }

    /// <summary>
    /// Tiện ích tra type thô (không giới hạn ở <see cref="Component"/>) — dùng cho các class
    /// tiện ích của Unity như TMP_PackageResourceImporter.
    /// </summary>
    public static class UILayoutTypeResolverHelper
    {
        /// <summary>Tìm type theo tên đầy đủ trong mọi assembly đã nạp.</summary>
        /// <param name="fullName">Tên type đầy đủ.</param>
        public static Type FindType(string fullName)
        {
            Type t = Type.GetType(fullName, false);
            if (t != null) return t;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch
                {
                    // Bỏ qua assembly không truy vấn được.
                }
            }

            return null;
        }
    }
}
