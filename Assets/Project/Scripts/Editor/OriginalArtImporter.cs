using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Thông tin một sprite UI trích từ bản gốc (đọc từ <c>ui_layout/sprite_metadata.json</c>).
    /// Dùng để đặt lại import settings cho file PNG tương ứng.
    /// </summary>
    public sealed class OriginalSpriteMeta
    {
        /// <summary>Tên sprite trong bản gốc (phân biệt hoa thường).</summary>
        public string Name;

        /// <summary>Chiều rộng của sprite rect trong bản gốc, tính bằng pixel.</summary>
        public int Width;

        /// <summary>Chiều cao của sprite rect trong bản gốc, tính bằng pixel.</summary>
        public int Height;

        /// <summary>Viền 9-slice theo thứ tự Unity: (left, bottom, right, top), tính bằng pixel.</summary>
        public Vector4 Border;

        /// <summary>Pivot chuẩn hoá trong khoảng [0..1].</summary>
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);

        /// <summary>Số pixel trên một unit của sprite gốc.</summary>
        public float PixelsPerUnit = 100f;

        /// <summary>Sprite này có dùng 9-slice hay không.</summary>
        public bool HasBorder => Border.sqrMagnitude > 0.0001f;
    }

    /// <summary>
    /// Chép art UI của bản gốc (đã trích bằng AssetRipper) vào project và đặt import settings
    /// đúng theo metadata gốc: 9-slice border, pivot, pixels-per-unit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Art được đặt trong <c>Assets/Project/ArtFromOriginal/UI/</c> — tên thư mục cố ý nói rõ
    /// đây là tài sản của bản gốc, <b>phải thay trước khi phát hành</b>.
    /// </para>
    /// <para>
    /// Điểm khó: các file PNG tham chiếu đã bị cắt sát theo vùng alpha (tight rect) nên nhỏ hơn
    /// sprite rect ghi trong metadata. Border 9-slice trong metadata tính theo rect gốc, vì vậy
    /// importer sẽ co border theo tỉ lệ kích thước thật của PNG rồi kẹp lại cho vừa ảnh.
    /// Xem <see cref="AdaptBorder"/>.
    /// </para>
    /// </remarks>
    public static class OriginalArtImporter
    {
        /// <summary>Thư mục chứa art UI trích từ bản gốc — cần thay trước khi phát hành.</summary>
        public const string UIArtFolder = "Assets/Project/ArtFromOriginal/UI";

        /// <summary>Thư mục chứa font trích từ bản gốc.</summary>
        public const string FontArtFolder = "Assets/Project/ArtFromOriginal/Fonts";

        /// <summary>Thư mục gốc của toàn bộ art lấy từ bản gốc.</summary>
        public const string ArtRootFolder = "Assets/Project/ArtFromOriginal";

        /// <summary>Tên thư mục nguồn (nằm cạnh <c>ui_layout/sprite_metadata.json</c>) chứa 202 PNG đã cắt.</summary>
        public const string SourceFolderName = "original_sprites_reference";

        /// <summary>Tên file metadata sprite trong thư mục <c>ui_layout</c>.</summary>
        public const string MetadataFileName = "sprite_metadata.json";

        /// <summary>
        /// Chép toàn bộ PNG art UI của bản gốc vào <see cref="UIArtFolder"/> và áp import settings
        /// theo <c>sprite_metadata.json</c>. Chạy lại nhiều lần vô hại: file đã có thì không chép lại
        /// nhưng import settings vẫn được áp lại.
        /// </summary>
        [MenuItem("Pickleball/Art/Import Original UI Art")]
        public static void ImportOriginalUIArt()
        {
            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogError("[OriginalArtImporter] Không tìm thấy thư mục ui_layout (cần có _index.json).");
                return;
            }

            string sourceFolder = Path.Combine(layoutRoot, SourceFolderName);
            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogError($"[OriginalArtImporter] Không thấy thư mục nguồn: {sourceFolder}");
                return;
            }

            string metadataPath = Path.Combine(layoutRoot, MetadataFileName);
            Dictionary<string, OriginalSpriteMeta> metadata = LoadMetadata(metadataPath, out string metaError);
            if (metadata == null)
            {
                Debug.LogError($"[OriginalArtImporter] Không đọc được {metadataPath}: {metaError}");
                return;
            }

            UILayoutPaths.EnsureAssetFolder(UIArtFolder);
            WriteReadme();

            string[] sources = Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(sources, StringComparer.OrdinalIgnoreCase);

            var report = new ImportReport { MetadataCount = metadata.Count, SourceCount = sources.Length };
            var pending = new List<(string assetPath, OriginalSpriteMeta meta, Vector2Int pixelSize)>(sources.Length);

            // --- Bước 1: chép file (không chép .meta — để Unity tự sinh GUID mới cho project này).
            try
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    string source = sources[i];
                    string fileName = Path.GetFileName(source);
                    EditorUtility.DisplayProgressBar("Import Original UI Art", $"Chép {fileName} ({i + 1}/{sources.Length})",
                        (i + 1) / (float)sources.Length * 0.5f);

                    string assetPath = UIArtFolder + "/" + fileName;
                    string absolute = ToAbsolute(assetPath);

                    try
                    {
                        if (File.Exists(absolute)) report.Skipped++;
                        else
                        {
                            File.Copy(source, absolute, false);
                            report.Copied++;
                        }
                    }
                    catch (Exception e)
                    {
                        report.Errors.Add($"Chép {fileName} thất bại: {e.Message}");
                        continue;
                    }

                    if (!TryReadPngSize(absolute, out Vector2Int pixelSize))
                    {
                        report.Errors.Add($"{fileName}: không đọc được kích thước PNG (file hỏng?).");
                        continue;
                    }

                    OriginalSpriteMeta meta = ResolveMeta(metadata, Path.GetFileNameWithoutExtension(fileName), pixelSize, report);
                    if (meta == null)
                    {
                        report.NoMetadata.Add(fileName);
                        continue;
                    }

                    pending.Add((assetPath, meta, pixelSize));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // File vừa chép bằng System.IO nên AssetDatabase chưa biết — phải Refresh trước khi lấy importer.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // --- Bước 2: áp import settings theo metadata.
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < pending.Count; i++)
                {
                    (string assetPath, OriginalSpriteMeta meta, Vector2Int pixelSize) = pending[i];
                    EditorUtility.DisplayProgressBar("Import Original UI Art",
                        $"Import settings {Path.GetFileName(assetPath)} ({i + 1}/{pending.Count})",
                        0.5f + (i + 1) / (float)pending.Count * 0.5f);

                    ApplyImportSettings(assetPath, meta, pixelSize, report);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            Debug.Log(report.Build());
        }

        /// <summary>
        /// Đọc <c>sprite_metadata.json</c> thành bảng tra theo tên sprite (phân biệt hoa thường).
        /// </summary>
        /// <param name="metadataPath">Đường dẫn tuyệt đối tới file metadata.</param>
        /// <param name="error">Thông báo lỗi khi trả về null.</param>
        /// <returns>Bảng tra, hoặc null nếu không đọc được.</returns>
        public static Dictionary<string, OriginalSpriteMeta> LoadMetadata(string metadataPath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(metadataPath) || !File.Exists(metadataPath))
            {
                error = "file không tồn tại";
                return null;
            }

            JObject root;
            try
            {
                root = JObject.Parse(File.ReadAllText(metadataPath));
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }

            if (!(root["sprites"] is JObject sprites))
            {
                error = "không có object 'sprites'";
                return null;
            }

            var map = new Dictionary<string, OriginalSpriteMeta>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JToken> pair in sprites)
            {
                if (!(pair.Value is JObject obj)) continue;

                var entry = new OriginalSpriteMeta
                {
                    Name = (string)obj["name"] ?? pair.Key,
                    Width = ReadInt(obj["width"], 0),
                    Height = ReadInt(obj["height"], 0),
                    Border = ReadVector4(obj["border"]),
                    Pivot = ReadVector2(obj["pivot"], new Vector2(0.5f, 0.5f)),
                    PixelsPerUnit = ReadFloat(obj["pixelsPerUnit"], 100f)
                };

                if (entry.PixelsPerUnit <= 0f) entry.PixelsPerUnit = 100f;
                map[pair.Key] = entry;
            }

            return map;
        }

        /// <summary>
        /// Tra metadata cho một file PNG. Khớp chính xác trước; nếu nhiều entry chỉ khác nhau ở
        /// hoa/thường (hệ thống file Windows đã gộp chúng thành một file) thì chọn entry có kích
        /// thước gần với ảnh thật nhất — đây là chỗ dễ gán nhầm border nhất.
        /// </summary>
        /// <param name="metadata">Bảng metadata đã đọc.</param>
        /// <param name="fileStem">Tên file PNG không kèm đuôi.</param>
        /// <param name="pixelSize">Kích thước thật của PNG.</param>
        /// <param name="report">Báo cáo để ghi lại các lần phải phân giải nhập nhằng.</param>
        /// <returns>Entry phù hợp nhất, hoặc null nếu không có.</returns>
        public static OriginalSpriteMeta ResolveMeta(Dictionary<string, OriginalSpriteMeta> metadata,
            string fileStem, Vector2Int pixelSize, ImportReport report)
        {
            if (metadata == null || string.IsNullOrEmpty(fileStem)) return null;

            List<OriginalSpriteMeta> candidates = metadata
                .Where(p => string.Equals(p.Key, fileStem, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Value)
                .ToList();

            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            // Nhiều entry trùng tên khi bỏ qua hoa/thường: chọn theo sai lệch kích thước nhỏ nhất.
            OriginalSpriteMeta best = candidates
                .OrderBy(c => Math.Abs(c.Width - pixelSize.x) + Math.Abs(c.Height - pixelSize.y))
                .First();

            report?.Ambiguous.Add($"{fileStem} ({pixelSize.x}x{pixelSize.y}): {candidates.Count} entry trùng tên " +
                                  $"[{string.Join(", ", candidates.Select(c => $"{c.Name} {c.Width}x{c.Height}"))}] → chọn " +
                                  $"{best.Name} {best.Width}x{best.Height}");
            return best;
        }

        /// <summary>
        /// Đưa border 9-slice của sprite rect gốc về đúng kích thước ảnh PNG thật (ảnh đã bị cắt
        /// sát alpha nên thường nhỏ hơn). Co theo tỉ lệ rồi kẹp để tổng hai cạnh đối luôn nhỏ hơn
        /// kích thước ảnh — border tràn ảnh sẽ làm Unity dựng sprite sai và nút bấm vỡ hình.
        /// </summary>
        /// <param name="border">Border gốc (left, bottom, right, top) theo pixel.</param>
        /// <param name="metaSize">Kích thước sprite rect trong metadata.</param>
        /// <param name="pixelSize">Kích thước thật của file PNG.</param>
        /// <param name="adjusted">true nếu giá trị đã bị đổi so với gốc.</param>
        /// <returns>Border đã hợp lệ với ảnh thật.</returns>
        public static Vector4 AdaptBorder(Vector4 border, Vector2Int metaSize, Vector2Int pixelSize, out bool adjusted)
        {
            adjusted = false;
            if (border.sqrMagnitude <= 0.0001f) return Vector4.zero;
            if (pixelSize.x <= 0 || pixelSize.y <= 0) return Vector4.zero;

            Vector4 result = border;

            // Co theo tỉ lệ khi PNG đã bị cắt nhỏ hơn sprite rect gốc.
            if (metaSize.x > 0 && metaSize.y > 0 && (metaSize.x != pixelSize.x || metaSize.y != pixelSize.y))
            {
                float sx = pixelSize.x / (float)metaSize.x;
                float sy = pixelSize.y / (float)metaSize.y;
                result = new Vector4(
                    Mathf.Round(result.x * sx),
                    Mathf.Round(result.y * sy),
                    Mathf.Round(result.z * sx),
                    Mathf.Round(result.w * sy));
                adjusted = true;
            }

            // Kẹp cho vừa ảnh: chừa ít nhất 1px cho phần giữa co giãn.
            ClampPair(ref result.x, ref result.z, pixelSize.x, ref adjusted);
            ClampPair(ref result.y, ref result.w, pixelSize.y, ref adjusted);

            return result;
        }

        private static void ClampPair(ref float low, ref float high, int size, ref bool adjusted)
        {
            low = Mathf.Max(0f, low);
            high = Mathf.Max(0f, high);

            float limit = size - 1;
            if (limit <= 0f)
            {
                if (low > 0f || high > 0f) adjusted = true;
                low = 0f;
                high = 0f;
                return;
            }

            float sum = low + high;
            if (sum <= limit) return;

            float scale = limit / sum;
            low = Mathf.Floor(low * scale);
            high = Mathf.Floor(high * scale);
            adjusted = true;
        }

        private static void ApplyImportSettings(string assetPath, OriginalSpriteMeta meta, Vector2Int pixelSize, ImportReport report)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                report.Errors.Add($"{Path.GetFileName(assetPath)}: không lấy được TextureImporter.");
                return;
            }

            Vector4 border = AdaptBorder(meta.Border, new Vector2Int(meta.Width, meta.Height), pixelSize, out bool borderAdjusted);
            bool wantsCustomPivot = Vector2.Distance(meta.Pivot, new Vector2(0.5f, 0.5f)) > 0.0001f;
            int alignment = (int)(wantsCustomPivot ? SpriteAlignment.Custom : SpriteAlignment.Center);
            int maxSize = ClampMaxTextureSize(Mathf.Max(pixelSize.x, pixelSize.y));

            Vector2 pivot = wantsCustomPivot ? meta.Pivot : new Vector2(0.5f, 0.5f);

            // spritePivot / spriteAlignment / spriteBorder chỉ ghi được qua TextureImporterSettings
            // (TextureImporter.spritePivot là property chỉ đọc). Các thành viên của
            // TextureImporterSettings là property nên phải gán trực tiếp, không dùng được ref.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool changed =
                settings.textureType != TextureImporterType.Sprite
                || settings.spriteMode != (int)SpriteImportMode.Single
                || settings.spriteAlignment != alignment
                || (settings.spritePivot - pivot).sqrMagnitude > 1e-8f
                || (settings.spriteBorder - border).sqrMagnitude > 1e-6f
                || !Mathf.Approximately(settings.spritePixelsPerUnit, meta.PixelsPerUnit)
                || settings.mipmapEnabled
                || !settings.alphaIsTransparency
                || settings.filterMode != FilterMode.Bilinear
                || settings.readable
                || settings.wrapMode != TextureWrapMode.Clamp;

            if (changed)
            {
                settings.textureType = TextureImporterType.Sprite;
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteAlignment = alignment;
                settings.spritePivot = pivot;
                settings.spriteBorder = border;
                settings.spritePixelsPerUnit = meta.PixelsPerUnit;
                settings.mipmapEnabled = false;
                settings.alphaIsTransparency = true;
                settings.filterMode = FilterMode.Bilinear;
                settings.readable = false;
                settings.wrapMode = TextureWrapMode.Clamp;
                importer.SetTextureSettings(settings);
            }

            // Các thuộc tính không nằm trong TextureImporterSettings.
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, meta.PixelsPerUnit))
            {
                importer.spritePixelsPerUnit = meta.PixelsPerUnit;
                changed = true;
            }

            // Nén chuẩn thay vì Uncompressed.
            //
            // Uncompressed cho chất lượng khớp bản gốc tuyệt đối nhưng 29 MB PNG sẽ nở thành
            // ~196 MB RGBA32 trong build — không chấp nhận được với game mobile. Nén chuẩn
            // (ASTC trên Android) giữ chất lượng gần như y hệt ở kích thước bằng một phần nhỏ.
            // Muốn đối chiếu pixel-perfect với bản gốc thì đổi tạm sang Uncompressed rồi đổi lại.
            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            if (importer.crunchedCompression)
            {
                // Crunch làm mờ viền sprite UI và tốn thời gian build; UI không cần nó.
                importer.crunchedCompression = false;
                changed = true;
            }

            // Android dùng ASTC 6x6 — cân bằng tốt giữa chất lượng và dung lượng cho UI.
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6)
            {
                android.overridden = true;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.maxTextureSize = maxSize;
                android.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }

            report.Configured++;
            if (border.sqrMagnitude > 0.0001f) report.WithBorder++;
            if (wantsCustomPivot) report.WithCustomPivot++;
            if (borderAdjusted)
            {
                report.BorderAdjusted.Add($"{Path.GetFileNameWithoutExtension(assetPath)}: " +
                                          $"rect gốc {meta.Width}x{meta.Height} → ảnh {pixelSize.x}x{pixelSize.y}, " +
                                          $"border {Fmt(meta.Border)} → {Fmt(border)}");
            }

            if (!changed)
            {
                report.AlreadyCorrect++;
                return;
            }

            try
            {
                importer.SaveAndReimport();
                report.Reimported++;
            }
            catch (Exception e)
            {
                report.Errors.Add($"{Path.GetFileName(assetPath)}: SaveAndReimport lỗi — {e.Message}");
            }
        }

        /// <summary>Làm tròn lên luỹ thừa 2 và kẹp vào dải maxTextureSize Unity chấp nhận.</summary>
        /// <param name="longestSide">Cạnh dài nhất của ảnh, tính bằng pixel.</param>
        public static int ClampMaxTextureSize(int longestSide)
        {
            int size = Mathf.NextPowerOfTwo(Mathf.Max(1, longestSide));
            return Mathf.Clamp(size, 32, 8192);
        }

        /// <summary>
        /// Đọc chiều rộng / chiều cao từ chunk IHDR của file PNG mà không cần nạp texture.
        /// </summary>
        /// <param name="absolutePath">Đường dẫn tuyệt đối tới file PNG.</param>
        /// <param name="size">Kích thước đọc được.</param>
        /// <returns>true nếu đọc thành công.</returns>
        public static bool TryReadPngSize(string absolutePath, out Vector2Int size)
        {
            size = Vector2Int.zero;
            try
            {
                using (var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, 24) < 24) return false;
                    if (header[0] != 0x89 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G') return false;

                    int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                    int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                    if (width <= 0 || height <= 0) return false;

                    size = new Vector2Int(width, height);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void WriteReadme()
        {
            string path = ToAbsolute(ArtRootFolder + "/README.txt");
            const string content =
                "ART CỦA BẢN GỐC — PHẢI THAY TRƯỚC KHI PHÁT HÀNH\r\n" +
                "================================================\r\n" +
                "\r\n" +
                "Toàn bộ ảnh và font trong thư mục này được trích ra từ bản build gốc bằng AssetRipper.\r\n" +
                "Chúng CHỈ dùng để dựng lại đúng bố cục / tỉ lệ / cảm giác UI của bản gốc trong lúc phát\r\n" +
                "triển. Đây KHÔNG phải tài sản của dự án này.\r\n" +
                "\r\n" +
                "TRƯỚC KHI PHÁT HÀNH BẮT BUỘC PHẢI:\r\n" +
                "  1. Vẽ lại (reskin) toàn bộ art thay cho các file ở đây.\r\n" +
                "  2. Thả art mới vào  Assets/Project/Textures/UI/  với ĐÚNG TÊN FILE như ở đây.\r\n" +
                "     SpriteBinder ưu tiên Textures/UI/ nên art mới sẽ tự động đè lên art gốc.\r\n" +
                "  3. Chạy menu  Pickleball/UI/Bind Sprites From Folder  để gán lại vào prefab.\r\n" +
                "  4. Xoá hẳn thư mục Assets/Project/ArtFromOriginal/ khỏi project.\r\n" +
                "\r\n" +
                "GHI CHÚ KỸ THUẬT\r\n" +
                "----------------\r\n" +
                "- Import settings (9-slice border, pivot, pixels-per-unit) được đặt tự động theo\r\n" +
                "  ui_layout/sprite_metadata.json qua menu  Pickleball/Art/Import Original UI Art.\r\n" +
                "- Các file PNG ở đây đã bị cắt sát vùng alpha nên nhỏ hơn sprite rect của bản gốc;\r\n" +
                "  border 9-slice vì vậy được co theo tỉ lệ. Khi vẽ art mới, hãy tự đặt lại border\r\n" +
                "  trong Sprite Editor cho khớp thiết kế mới.\r\n" +
                "- Ảnh dùng NÉN CHUẨN, tắt mipmap, tắt crunch; Android override sang ASTC 6x6.\r\n" +
                "  (Uncompressed cho chất lượng khớp tuyệt đối nhưng ~29 MB PNG sẽ nở thành ~196 MB\r\n" +
                "  RGBA32 trong build — không dùng được cho mobile.)\r\n" +
                "- Muốn đối chiếu pixel-perfect với bản gốc thì đổi tạm sang Uncompressed rồi đổi lại.\r\n" +
                "- Nên gộp vào Sprite Atlas trước khi phát hành để giảm draw call.\r\n";

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if (!File.Exists(path) || File.ReadAllText(path) != content)
                    File.WriteAllText(path, content, new UTF8Encoding(true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OriginalArtImporter] Không ghi được README.txt: {e.Message}");
            }
        }

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Fmt(Vector4 v) =>
            $"({v.x:0},{v.y:0},{v.z:0},{v.w:0})";

        private static int ReadInt(JToken token, int fallback)
        {
            try { return token == null ? fallback : (int)token; }
            catch { return fallback; }
        }

        private static float ReadFloat(JToken token, float fallback)
        {
            try
            {
                if (token == null) return fallback;
                float value = (float)token;
                return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
            }
            catch { return fallback; }
        }

        private static Vector2 ReadVector2(JToken token, Vector2 fallback)
        {
            if (!(token is JArray array) || array.Count < 2) return fallback;
            return new Vector2(ReadFloat(array[0], fallback.x), ReadFloat(array[1], fallback.y));
        }

        private static Vector4 ReadVector4(JToken token)
        {
            if (!(token is JArray array) || array.Count < 4) return Vector4.zero;
            return new Vector4(ReadFloat(array[0], 0f), ReadFloat(array[1], 0f),
                ReadFloat(array[2], 0f), ReadFloat(array[3], 0f));
        }

        /// <summary>
        /// Số liệu của một lần chạy <see cref="ImportOriginalUIArt"/>, dùng để in tổng kết ra Console.
        /// </summary>
        public sealed class ImportReport
        {
            /// <summary>Số entry đọc được trong sprite_metadata.json.</summary>
            public int MetadataCount;

            /// <summary>Số file PNG tìm thấy trong thư mục nguồn.</summary>
            public int SourceCount;

            /// <summary>Số file đã chép mới vào project.</summary>
            public int Copied;

            /// <summary>Số file đã có sẵn nên không chép lại.</summary>
            public int Skipped;

            /// <summary>Số file đã áp import settings.</summary>
            public int Configured;

            /// <summary>Số file thực sự phải reimport (có thay đổi settings).</summary>
            public int Reimported;

            /// <summary>Số file đã đúng settings từ trước.</summary>
            public int AlreadyCorrect;

            /// <summary>Số sprite được đặt 9-slice border khác 0.</summary>
            public int WithBorder;

            /// <summary>Số sprite dùng pivot tuỳ chỉnh (khác 0.5, 0.5).</summary>
            public int WithCustomPivot;

            /// <summary>Các sprite phải co/kẹp border vì PNG đã bị cắt nhỏ hơn rect gốc.</summary>
            public readonly List<string> BorderAdjusted = new List<string>();

            /// <summary>Các tên file trùng nhau khi bỏ qua hoa/thường, đã phải đoán theo kích thước.</summary>
            public readonly List<string> Ambiguous = new List<string>();

            /// <summary>Các file PNG không tìm được entry metadata tương ứng.</summary>
            public readonly List<string> NoMetadata = new List<string>();

            /// <summary>Các lỗi gặp phải trong quá trình chép / import.</summary>
            public readonly List<string> Errors = new List<string>();

            /// <summary>Dựng chuỗi tổng kết in ra Console.</summary>
            public string Build()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[OriginalArtImporter] Xong. Nguồn {SourceCount} PNG / metadata {MetadataCount} sprite.");
                sb.AppendLine($"  - Chép mới: {Copied}   | đã có sẵn (bỏ qua chép): {Skipped}");
                sb.AppendLine($"  - Áp import settings: {Configured}  (reimport {Reimported}, đã đúng sẵn {AlreadyCorrect})");
                sb.AppendLine($"  - Đặt 9-slice border khác 0: {WithBorder}  | pivot tuỳ chỉnh: {WithCustomPivot}");

                if (BorderAdjusted.Count > 0)
                {
                    sb.AppendLine($"  - Border phải co lại cho vừa ảnh đã cắt: {BorderAdjusted.Count}");
                    foreach (string line in BorderAdjusted.Take(20)) sb.AppendLine("      " + line);
                    if (BorderAdjusted.Count > 20) sb.AppendLine($"      ... và {BorderAdjusted.Count - 20} sprite nữa.");
                }

                if (Ambiguous.Count > 0)
                {
                    sb.AppendLine($"  - CẢNH BÁO tên trùng khi bỏ qua hoa/thường (Windows gộp file): {Ambiguous.Count}");
                    foreach (string line in Ambiguous) sb.AppendLine("      " + line);
                }

                if (NoMetadata.Count > 0)
                {
                    sb.AppendLine($"  - Không có metadata (dùng mặc định của Unity): {NoMetadata.Count}");
                    foreach (string line in NoMetadata.Take(20)) sb.AppendLine("      " + line);
                }

                if (Errors.Count > 0)
                {
                    sb.AppendLine($"  - LỖI: {Errors.Count}");
                    foreach (string line in Errors.Take(20)) sb.AppendLine("      " + line);
                }

                sb.Append($"  → Tiếp theo chạy 'Pickleball/UI/Bind Sprites From Folder' để gán vào prefab. " +
                          $"Art nằm ở {UIArtFolder}/ (xem README.txt — phải thay trước khi phát hành).");
                return sb.ToString();
            }
        }
    }
}
