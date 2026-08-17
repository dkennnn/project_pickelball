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
    /// Tiện ích dùng chung cho việc tra art UI và áp 9-slice — chia sẻ giữa
    /// <see cref="SpriteBinder"/> và <see cref="UILayoutImporter"/> để hai bên luôn nhìn vào
    /// cùng một bộ thư mục theo cùng một thứ tự ưu tiên.
    /// </summary>
    public static class UIArtLookup
    {
        /// <summary>
        /// Các thư mục chứa sprite UI, xếp theo ĐỘ ƯU TIÊN GIẢM DẦN.
        /// <c>Textures/UI/</c> đứng trước vì đó là chỗ người dùng thả art thay thế — art mới
        /// luôn đè lên art trích từ bản gốc mà không cần xoá gì.
        /// </summary>
        public static readonly string[] SearchFolders =
        {
            UILayoutPaths.SpriteFolder,          // Assets/Project/Textures/UI  (art thay thế — ưu tiên cao)
            OriginalArtImporter.UIArtFolder      // Assets/Project/ArtFromOriginal/UI (art bản gốc)
        };

        /// <summary>
        /// Sprite này có 9-slice hay không (một trong bốn cạnh border khác 0).
        /// </summary>
        /// <param name="sprite">Sprite cần kiểm tra.</param>
        public static bool HasNineSliceBorder(Sprite sprite)
        {
            if (sprite == null) return false;
            Vector4 border = sprite.border;
            return border.sqrMagnitude > 0.0001f;
        }

        /// <summary>
        /// Đặt <see cref="Image.type"/> cho khớp với sprite: có border 9-slice thì dùng
        /// <see cref="Image.Type.Sliced"/> (nút bấm co giãn giữ nguyên góc bo), không thì
        /// <see cref="Image.Type.Simple"/>. Đây là bước quyết định UI trông đúng bản gốc.
        /// </summary>
        /// <param name="image">Component Image vừa được gán sprite.</param>
        /// <param name="sprite">Sprite đã gán.</param>
        /// <returns>true nếu sprite có 9-slice và Image đã chuyển sang Sliced.</returns>
        public static bool ApplyNineSlice(Image image, Sprite sprite)
        {
            if (image == null) return false;

            if (HasNineSliceBorder(sprite))
            {
                image.type = Image.Type.Sliced;
                // Border trong metadata tính theo pixel gốc — nhân 1 để Unity không tự co lại
                // theo referencePixelsPerUnit của Canvas.
                image.pixelsPerUnitMultiplier = 1f;
                return true;
            }

            // Chỉ hạ về Simple khi đang ở Sliced/Tiled — không đụng vào Filled/sliced cố ý của người dùng.
            if (image.type == Image.Type.Sliced || image.type == Image.Type.Tiled)
                image.type = Image.Type.Simple;

            return false;
        }

        /// <summary>
        /// Dựng bảng tra sprite quét toàn bộ <see cref="SearchFolders"/> theo đúng thứ tự ưu tiên.
        /// </summary>
        public static UISpriteIndex BuildIndex()
        {
            return UISpriteIndex.Build(SearchFolders);
        }

        /// <summary>Mô tả ngắn các thư mục art đang được quét, dùng trong log.</summary>
        public static string DescribeFolders()
        {
            return string.Join(" → ", SearchFolders.Select(f =>
                AssetDatabase.IsValidFolder(f) ? f : f + " (chưa có)"));
        }
    }

    /// <summary>
    /// Gán art thật vào các prefab UI đã sinh: quét mọi <see cref="SpritePlaceholder"/> trong
    /// <c>Assets/Project/Prefabs/UI/</c>, tìm sprite cùng tên trong <c>Assets/Project/Textures/UI/</c>
    /// rồi tới <c>Assets/Project/ArtFromOriginal/UI/</c>, gán vào <see cref="Image"/> cùng node,
    /// đặt 9-slice nếu sprite có border, rồi xoá component đánh dấu.
    /// </summary>
    /// <remarks>
    /// Quy trình thay art: thả file ảnh đúng tên sprite gốc vào <c>Textures/UI/</c> (không phân biệt
    /// hoa thường, không cần đuôi file trùng) rồi bấm menu <c>Pickleball/UI/Bind Sprites From Folder</c>.
    /// Art trong <c>Textures/UI/</c> luôn thắng art trích từ bản gốc.
    /// Chạy lại bao nhiêu lần cũng được — chỗ nào đã gán rồi thì không còn placeholder nên bị bỏ qua.
    /// </remarks>
    public static class SpriteBinder
    {
        /// <summary>
        /// Quét toàn bộ prefab UI và gán sprite theo tên. In ra Console số đã gán, số đặt 9-slice,
        /// số còn thiếu và danh sách tên sprite chưa có file art.
        /// </summary>
        [MenuItem("Pickleball/UI/Bind Sprites From Folder")]
        public static void BindSprites()
        {
            if (!AssetDatabase.IsValidFolder(UILayoutPaths.UIPrefabRoot))
            {
                Debug.LogWarning($"[SpriteBinder] Chưa có thư mục {UILayoutPaths.UIPrefabRoot} — chạy 'Pickleball/UI/Import All UI Layouts' trước.");
                return;
            }

            UISpriteIndex sprites = UIArtLookup.BuildIndex();
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[SpriteBinder] Không tìm thấy sprite nào trong {UIArtLookup.DescribeFolders()} " +
                                 "(thư mục chưa tồn tại, hoặc ảnh chưa đặt Texture Type = Sprite (2D and UI)). " +
                                 "Chạy 'Pickleball/Art/Import Original UI Art' để nạp art bản gốc.");
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UILayoutPaths.UIPrefabRoot });
            int bound = 0;
            int sliced = 0;
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
                            if (UIArtLookup.ApplyNineSlice(image, sprite)) sliced++;

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
            sb.AppendLine($"  - Nguồn art (ưu tiên trái → phải): {UIArtLookup.DescribeFolders()} — tổng {sprites.Count} tên tra được.");
            sb.AppendLine($"  - Đặt Image.type = Sliced (9-slice) cho {sliced}/{bound} chỗ, còn lại Simple.");
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
