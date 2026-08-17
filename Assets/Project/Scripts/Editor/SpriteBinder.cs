using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Gán art thật vào các prefab UI đã sinh: quét mọi <see cref="SpritePlaceholder"/> trong
    /// <c>Assets/Project/Prefabs/UI/</c>, tìm sprite cùng tên trong <c>Assets/Project/Textures/UI/</c>,
    /// gán vào <see cref="Image"/> cùng node rồi xoá component đánh dấu.
    /// </summary>
    /// <remarks>
    /// Quy trình thay art: thả file ảnh đúng tên sprite gốc vào <c>Textures/UI/</c> (không phân biệt
    /// hoa thường, không cần đuôi file trùng) rồi bấm menu <c>Pickleball/UI/Bind Sprites From Folder</c>.
    /// Chạy lại bao nhiêu lần cũng được — chỗ nào đã gán rồi thì không còn placeholder nên bị bỏ qua.
    /// </remarks>
    public static class SpriteBinder
    {
        /// <summary>
        /// Quét toàn bộ prefab UI và gán sprite theo tên. In ra Console số đã gán, số còn thiếu
        /// và danh sách tên sprite chưa có file art.
        /// </summary>
        [MenuItem("Pickleball/UI/Bind Sprites From Folder")]
        public static void BindSprites()
        {
            if (!AssetDatabase.IsValidFolder(UILayoutPaths.UIPrefabRoot))
            {
                Debug.LogWarning($"[SpriteBinder] Chưa có thư mục {UILayoutPaths.UIPrefabRoot} — chạy 'Pickleball/UI/Import All UI Layouts' trước.");
                return;
            }

            UISpriteIndex sprites = UISpriteIndex.Build(UILayoutPaths.SpriteFolder);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[SpriteBinder] Không tìm thấy sprite nào trong {UILayoutPaths.SpriteFolder} " +
                                 "(thư mục chưa tồn tại, hoặc ảnh chưa đặt Texture Type = Sprite (2D and UI)).");
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UILayoutPaths.UIPrefabRoot });
            int bound = 0;
            int missing = 0;
            int touchedPrefabs = 0;
            var missingNames = new Dictionary<string, int>(StringComparer.Ordinal);
            var brokenNodes = new List<string>();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Bind Sprites", path, (i + 1) / (float)guids.Length);

                    GameObject contents = PrefabUtility.LoadPrefabContents(path);
                    if (contents == null) continue;

                    bool dirty = false;
                    try
                    {
                        foreach (SpritePlaceholder placeholder in contents.GetComponentsInChildren<SpritePlaceholder>(true))
                        {
                            if (placeholder == null) continue;
                            string wanted = placeholder.originalSpriteName;

                            if (!sprites.TryGet(wanted, out Sprite sprite))
                            {
                                missing++;
                                if (!string.IsNullOrEmpty(wanted))
                                {
                                    missingNames.TryGetValue(wanted, out int n);
                                    missingNames[wanted] = n + 1;
                                }

                                continue;
                            }

                            var image = placeholder.GetComponent<Image>();
                            if (image == null)
                            {
                                brokenNodes.Add($"{System.IO.Path.GetFileNameWithoutExtension(path)}/{GetPath(placeholder.transform, contents.transform)}");
                                continue;
                            }

                            image.sprite = sprite;
                            Object.DestroyImmediate(placeholder);
                            bound++;
                            dirty = true;
                        }

                        if (dirty)
                        {
                            PrefabUtility.SaveAsPrefabAsset(contents, path);
                            touchedPrefabs++;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[SpriteBinder] Đã gán {bound} sprite vào {touchedPrefabs}/{guids.Length} prefab. Còn thiếu {missing} chỗ ({missingNames.Count} tên sprite).");
            if (brokenNodes.Count > 0)
                sb.AppendLine($"  - {brokenNodes.Count} node có SpritePlaceholder nhưng không có Image: {string.Join(", ", brokenNodes.Take(10))}");

            if (missingNames.Count > 0)
            {
                sb.AppendLine("  - Sprite còn thiếu (sắp theo số lần dùng giảm dần):");
                foreach (KeyValuePair<string, int> pair in missingNames.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine($"      {pair.Value,4}x  {pair.Key}");
                }

                sb.AppendLine($"  → Thả file ảnh đúng tên vào {UILayoutPaths.SpriteFolder}/ rồi chạy lại menu này.");
            }
            else if (bound > 0)
            {
                sb.AppendLine("  → Không còn sprite nào thiếu art.");
            }

            Debug.Log(sb.ToString());
        }

        private static string GetPath(Transform target, Transform root)
        {
            if (target == null) return string.Empty;
            var parts = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
