using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Chép font gốc (đã trích bằng AssetRipper) vào <c>Assets/Project/ArtFromOriginal/Fonts/</c>
    /// và dựng TMP FontAsset tương ứng để chữ trong UI nhìn giống bản gốc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chỉ file font THẬT (<c>.ttf</c> / <c>.otf</c>) mới dùng lại được. Các file
    /// <c>*.asset</c> kiểu "… SDF" trong bản trích là TMP FontAsset đã serialize sẵn: chúng trỏ
    /// tới atlas texture, material và MonoScript bằng GUID của PROJECT GỐC nên nhập thẳng vào đây
    /// sẽ chỉ ra một asset hỏng. Công cụ này phát hiện, BÁO CÁO rồi bỏ qua an toàn — đúng cách là
    /// dựng lại FontAsset từ file .ttf.
    /// </para>
    /// <para>
    /// Font ở đây là tài sản của bản gốc và có giấy phép riêng —
    /// <b>phải kiểm tra bản quyền / thay font trước khi phát hành</b>.
    /// </para>
    /// </remarks>
    public static class FontImporter
    {
        /// <summary>Khoá EditorPrefs lưu thư mục ExportedProject của AssetRipper do người dùng chọn tay.</summary>
        public const string RippedRootPrefsKey = "Pickleball.FontImporter.RippedRoot";

        /// <summary>Các đuôi file font thật có thể import trực tiếp.</summary>
        public static readonly string[] FontExtensions = { ".ttf", ".otf" };

        /// <summary>
        /// Chép font gốc vào project, đặt import settings và dựng TMP FontAsset cho từng font.
        /// Chạy lại nhiều lần vô hại: file/asset đã có thì bỏ qua.
        /// </summary>
        [MenuItem("Pickleball/Art/Import Original Fonts")]
        public static void ImportOriginalFonts()
        {
            string rippedAssets = FindRippedAssetsRoot();
            if (rippedAssets == null)
            {
                Debug.LogError("[FontImporter] Không tìm thấy thư mục ExportedProject/Assets của AssetRipper (cần có thư mục con 'Font').");
                return;
            }

            string sourceFolder = Path.Combine(rippedAssets, "Font");
            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogError($"[FontImporter] Không thấy thư mục font: {sourceFolder}");
                return;
            }

            UILayoutPaths.EnsureAssetFolder(OriginalArtImporter.FontArtFolder);

            var sb = new StringBuilder();
            var copiedAssetPaths = new List<string>();
            int copied = 0;
            int skipped = 0;
            var errors = new List<string>();

            // --- Bước 1: chép file font thật (.ttf/.otf). KHÔNG chép .meta — Unity tự sinh GUID mới.
            string[] sources = Directory.GetFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(p => FontExtensions.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string source in sources)
            {
                string fileName = Path.GetFileName(source);
                string assetPath = OriginalArtImporter.FontArtFolder + "/" + fileName;
                string absolute = ToAbsolute(assetPath);

                try
                {
                    if (File.Exists(absolute)) skipped++;
                    else
                    {
                        File.Copy(source, absolute, false);
                        copied++;
                    }

                    copiedAssetPaths.Add(assetPath);
                }
                catch (Exception e)
                {
                    errors.Add($"Chép {fileName} thất bại: {e.Message}");
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // --- Bước 2: các TMP FontAsset dạng .asset trong bản trích — chỉ báo cáo, không nhập.
            List<string> strandedFontAssets = FindStrandedTmpFontAssets(rippedAssets);

            // --- Bước 3: dựng TMP FontAsset từ từng .ttf.
            var created = new List<string>();
            var reused = new List<string>();
            foreach (string fontAssetPath in copiedAssetPaths)
            {
                string result = EnsureTmpFontAsset(fontAssetPath, out bool wasCreated, out string error);
                if (error != null) errors.Add(error);
                else if (result == null) continue;
                else if (wasCreated) created.Add(result);
                else reused.Add(result);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // --- Báo cáo.
            sb.AppendLine($"[FontImporter] Nguồn: {sourceFolder}");
            sb.AppendLine($"  - Font thật (.ttf/.otf): chép mới {copied}, đã có sẵn {skipped} → {OriginalArtImporter.FontArtFolder}/");
            sb.AppendLine($"  - TMP FontAsset dựng mới: {created.Count}, đã có sẵn: {reused.Count}");
            foreach (string line in created) sb.AppendLine($"      + {line}");

            if (strandedFontAssets.Count > 0)
            {
                sb.AppendLine($"  - BỎ QUA {strandedFontAssets.Count} TMP FontAsset dạng '.asset' trong bản trích:");
                foreach (string line in strandedFontAssets.Take(20)) sb.AppendLine($"      ! {Path.GetFileName(line)}");
                if (strandedFontAssets.Count > 20) sb.AppendLine($"      ! ... và {strandedFontAssets.Count - 20} file nữa.");
                sb.AppendLine("    LÝ DO: các file này trỏ tới atlas texture / material / MonoScript bằng GUID của");
                sb.AppendLine("    project gốc. Nhập thẳng vào đây sẽ ra asset hỏng (font null, atlas trắng).");
                sb.AppendLine("    ĐÚNG CÁCH: dựng lại FontAsset từ .ttf — công cụ này đã làm ở bước trên.");

                List<string> missingSource = strandedFontAssets
                    .Select(p => StripSdfSuffix(Path.GetFileNameWithoutExtension(p)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(n => !sources.Any(s => string.Equals(Path.GetFileNameWithoutExtension(s), n, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (missingSource.Count > 0)
                    sb.AppendLine($"    LƯU Ý: không có file .ttf nguồn cho: {string.Join(", ", missingSource)} — phải tìm/mua font này riêng.");
            }

            if (errors.Count > 0)
            {
                sb.AppendLine($"  - LỖI: {errors.Count}");
                foreach (string line in errors.Take(20)) sb.AppendLine($"      {line}");
            }

            if (created.Count > 0 || reused.Count > 0)
            {
                sb.AppendLine("  → GẮN FONT MẶC ĐỊNH: Edit > Project Settings > TextMeshPro > Settings,");
                sb.AppendLine($"    kéo một FontAsset trong {OriginalArtImporter.FontArtFolder}/ vào ô 'Default Font Asset',");
                sb.AppendLine("    rồi chạy lại 'Pickleball/UI/Import All UI Layouts' để mọi TextMeshProUGUI dùng font đó.");
                sb.AppendLine("    (Bản gốc dùng nhiều font khác nhau — Gobold/FreightSans/TiltWarp — nên sau đó vẫn cần");
                sb.AppendLine("     gán tay font riêng cho một số màn hình.)");
            }

            sb.Append("  → NHẮC: font trích từ bản gốc có giấy phép riêng, phải kiểm tra bản quyền hoặc thay trước khi phát hành.");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Dựng TMP FontAsset cạnh file font nếu chưa có.
        /// </summary>
        /// <param name="fontAssetPath">Đường dẫn kiểu <c>Assets/...</c> tới file .ttf/.otf.</param>
        /// <param name="wasCreated">true nếu asset vừa được tạo mới.</param>
        /// <param name="error">Thông báo lỗi, null nếu không lỗi.</param>
        /// <returns>Đường dẫn TMP FontAsset, hoặc null nếu không dựng được.</returns>
        public static string EnsureTmpFontAsset(string fontAssetPath, out bool wasCreated, out string error)
        {
            wasCreated = false;
            error = null;

            string targetPath = Path.ChangeExtension(fontAssetPath, null) + " SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetPath) != null) return targetPath;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontAssetPath);
            if (font == null)
            {
                error = $"{Path.GetFileName(fontAssetPath)}: Unity chưa import được thành Font.";
                return null;
            }

            try
            {
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
                if (fontAsset == null)
                {
                    error = $"{Path.GetFileName(fontAssetPath)}: TMP_FontAsset.CreateFontAsset trả về null.";
                    return null;
                }

                fontAsset.name = Path.GetFileNameWithoutExtension(targetPath);
                AssetDatabase.CreateAsset(fontAsset, targetPath);

                // Atlas texture và material phải nằm trong cùng asset, nếu không sẽ mất khi reload.
                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        Texture2D atlas = fontAsset.atlasTextures[i];
                        if (atlas == null) continue;
                        atlas.name = fontAsset.name + " Atlas" + (i > 0 ? " " + i : string.Empty);
                        AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                    }
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = fontAsset.name + " Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                EditorUtility.SetDirty(fontAsset);
                wasCreated = true;
                return targetPath;
            }
            catch (Exception e)
            {
                error = $"{Path.GetFileName(fontAssetPath)}: dựng TMP FontAsset lỗi — {e.Message}";
                return null;
            }
        }

        /// <summary>
        /// Tìm các TMP FontAsset dạng <c>.asset</c> trong bản trích — loại KHÔNG dùng lại được.
        /// </summary>
        /// <param name="rippedAssetsRoot">Thư mục <c>ExportedProject/Assets</c> của AssetRipper.</param>
        /// <returns>Danh sách đường dẫn tuyệt đối.</returns>
        public static List<string> FindStrandedTmpFontAssets(string rippedAssetsRoot)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(rippedAssetsRoot) || !Directory.Exists(rippedAssetsRoot)) return result;

            string[] searchFolders =
            {
                Path.Combine(rippedAssetsRoot, "Font"),
                Path.Combine(rippedAssetsRoot, "MonoBehaviour"),
                Path.Combine(rippedAssetsRoot, "Resources")
            };

            foreach (string folder in searchFolders)
            {
                if (!Directory.Exists(folder)) continue;
                try
                {
                    result.AddRange(Directory
                        .GetFiles(folder, "*.asset", SearchOption.AllDirectories)
                        .Where(p => Path.GetFileNameWithoutExtension(p)
                            .IndexOf("SDF", StringComparison.OrdinalIgnoreCase) >= 0));
                }
                catch
                {
                    // Thư mục không đọc được — bỏ qua.
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Tìm thư mục <c>ExportedProject/Assets</c> của AssetRipper.
        /// Thử EditorPrefs → các vị trí quy ước quanh project → hộp thoại chọn thư mục.
        /// </summary>
        /// <returns>Đường dẫn tuyệt đối, hoặc null nếu không tìm thấy.</returns>
        public static string FindRippedAssetsRoot()
        {
            string saved = EditorPrefs.GetString(RippedRootPrefsKey, string.Empty);
            if (IsRippedAssetsRoot(saved)) return saved;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            const string tail = "ripped2/ExportedProject/Assets";
            string[] candidates =
            {
                Combine(projectRoot, "..", tail),
                Combine(projectRoot, "..", "..", tail),
                Combine(projectRoot, tail),
                Combine(projectRoot, "..", "reverse-engineering", tail),
                @"E:\pikeball\reverse-engineering\ripped2\ExportedProject\Assets"
            };

            foreach (string candidate in candidates)
            {
                if (!IsRippedAssetsRoot(candidate)) continue;
                EditorPrefs.SetString(RippedRootPrefsKey, candidate);
                return candidate;
            }

            string picked = EditorUtility.OpenFolderPanel(
                "Chọn thư mục ExportedProject/Assets của AssetRipper (chứa thư mục con 'Font')",
                projectRoot, string.Empty);
            if (!IsRippedAssetsRoot(picked)) return null;
            EditorPrefs.SetString(RippedRootPrefsKey, picked);
            return picked;
        }

        /// <summary>Kiểm tra một thư mục có phải <c>ExportedProject/Assets</c> hợp lệ hay không.</summary>
        /// <param name="folder">Đường dẫn thư mục cần kiểm tra.</param>
        public static bool IsRippedAssetsRoot(string folder)
        {
            return !string.IsNullOrEmpty(folder)
                   && Directory.Exists(folder)
                   && Directory.Exists(Path.Combine(folder, "Font"));
        }

        private static string StripSdfSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            int index = name.IndexOf(" SDF", StringComparison.OrdinalIgnoreCase);
            return index > 0 ? name.Substring(0, index) : name;
        }

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Combine(params string[] parts)
        {
            try { return Path.GetFullPath(Path.Combine(parts)); }
            catch { return string.Empty; }
        }
    }
}
