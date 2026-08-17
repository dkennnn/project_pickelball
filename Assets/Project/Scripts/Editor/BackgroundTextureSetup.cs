using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Đặt import settings cho các texture nền dùng bởi <see cref="UnityEngine.UI.RawImage"/>.
    /// <para>
    /// Khác với sprite UI: nền parallax được cuộn bằng <c>UVRectScroller</c>/<c>ParallaxUVScroller</c>
    /// nên bắt buộc <c>wrapMode = Repeat</c>. Để mặc định Clamp thì viền texture bị kéo dài thành
    /// vệt màu khi cuộn. Chúng cũng phải là Texture thường, KHÔNG phải Sprite.
    /// </para>
    /// </summary>
    public static class BackgroundTextureSetup
    {
        /// <summary>Tên các texture nền, lấy từ layout gốc (field <c>texture</c> của RawImage).</summary>
        public static readonly string[] BackgroundTextureNames =
        {
            "Background Clouds",   // MainMenuUI/Background
            "BG-Pattern"           // 7 màn khác: */Background/Parallax
        };

        private static readonly string[] SearchFolders =
        {
            "Assets/Project/Textures/UI",
            "Assets/Project/ArtFromOriginal/UI"
        };

        [MenuItem("Pickleball/Art/Setup Background Textures")]
        public static void Setup()
        {
            var applied = new List<string>();
            var missing = new List<string>(BackgroundTextureNames);

            foreach (string folder in SearchFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string name = Path.GetFileNameWithoutExtension(path);

                    bool wanted = false;
                    foreach (string target in BackgroundTextureNames)
                    {
                        if (!string.Equals(name, target, System.StringComparison.OrdinalIgnoreCase)) continue;
                        wanted = true;
                        missing.Remove(target);
                        break;
                    }
                    if (!wanted) continue;

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Default)
                    {
                        importer.textureType = TextureImporterType.Default;
                        changed = true;
                    }
                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        applied.Add(name);
                    }
                }
            }

            Debug.Log($"[BackgroundTextureSetup] Đã đặt Repeat cho {applied.Count} texture nền" +
                      (applied.Count > 0 ? ": " + string.Join(", ", applied) : "") +
                      (missing.Count > 0
                          ? $". THIẾU {missing.Count}: {string.Join(", ", missing)} — chạy " +
                            "Pickleball/Art/Import Original UI Art trước."
                          : "."));
        }
    }
}
