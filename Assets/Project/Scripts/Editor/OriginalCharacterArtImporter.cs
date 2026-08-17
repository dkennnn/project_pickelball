using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Chép art GỐC (đã được AssetRipper trích ra) vào <c>Assets/Project/ArtFromOriginal/</c>.
    ///
    /// <para><b>Nguyên tắc sống còn</b>: mọi file đều được chép KÈM <c>.meta</c>. Nhờ vậy GUID nội bộ
    /// giữ nguyên, nên các liên kết material → texture, controller → clip, prefab → mesh/avatar
    /// vẫn còn nguyên vẹn sau khi chép. Nếu quên <c>.meta</c> thì Unity sinh GUID mới và toàn bộ
    /// liên kết vỡ.</para>
    ///
    /// <para><b>Chỉ chép cái cần</b>: thư mục gốc có 262 MB texture + 155 MB mesh. Công cụ dò phụ thuộc
    /// đệ quy từ một danh sách prefab hạt giống (nhân vật, vợt, bag, tazo, marker) cộng với toàn bộ
    /// clip / controller / avatar, rồi chỉ chép đúng những asset nằm trong đồ thị phụ thuộc đó
    /// (~100 MB thay vì ~420 MB).</para>
    ///
    /// <para><b>Hai điểm phải vá sau khi chép</b>:</para>
    /// <list type="number">
    /// <item><description><b>Shader</b>: AssetRipper xuất ra shader "rỗng" (chỉ có khối Properties,
    /// không có code) cho URP Lit / URP Unlit / TMP. Chép chúng vào project sẽ làm mọi material
    /// thành màu hồng. Vì vậy công cụ KHÔNG chép thư mục Shader mà viết lại thẳng vào file
    /// <c>.mat</c> tham chiếu tới shader THẬT của project (xem <see cref="RemapShaders"/>).</description></item>
    /// <item><description><b>Animation Event</b>: clip gốc gọi <c>OnThrow</c> / <c>OnHit</c>, còn
    /// <see cref="PlayerAnimationEventReceiver"/> của ta khai báo <c>OnThrowBall</c> / <c>OnShotHit</c>.
    /// Vì không được sửa file runtime, công tác đổi tên được thực hiện trên BẢN SAO của clip
    /// (xem <see cref="RemapAnimationEvents"/>).</description></item>
    /// </list>
    ///
    /// <para><b>Idempotent</b>: file đã tồn tại ở đích thì bỏ qua, chạy lại bao nhiêu lần cũng an toàn.</para>
    /// </summary>
    public static class OriginalCharacterArtImporter
    {
        // ------------------------------------------------------------------ Đường dẫn

        /// <summary>Đường dẫn tuyệt đối dự phòng tới thư mục asset đã trích, khi suy từ project thất bại.</summary>
        private const string FallbackRippedRoot =
            @"E:\pikeball\reverse-engineering\ripped2\ExportedProject\Assets";

        /// <summary>Thư mục đích trong project (đường dẫn kiểu Unity).</summary>
        private const string DestRoot = "Assets/Project/ArtFromOriginal";

        private const string DestAnimations = DestRoot + "/Animations";
        private const string DestControllers = DestAnimations + "/Controllers";
        private const string DestAvatars = DestAnimations + "/Avatars";
        private const string DestModels = DestRoot + "/Models";
        private const string DestMeshes = DestModels + "/Meshes";
        private const string DestMaterials = DestModels + "/Materials";
        private const string DestTextures = DestModels + "/Textures";
        private const string DestPrefabs = DestRoot + "/Prefabs";
        private const string ReadmePath = DestRoot + "/README.txt";

        // ------------------------------------------------------------------ Cấu hình dò phụ thuộc

        /// <summary>
        /// Prefab hạt giống: nhân vật, vợt, bag, tazo và các marker sân. Mọi mesh/material/texture
        /// mà chúng dùng sẽ được kéo theo.
        /// </summary>
        private static readonly string[] SeedPrefabNames =
        {
            "PlayerV3_Character", "PlayerV4_Female_Character", "AIv3_Character",
            "Paddle 1", "Paddle 2", "Paddle 3", "Paddle 4", "Paddle 5",
            "Tazo", "BallBounceVisual", "Marker", "MarkerCircle", "ShotVisualizer",
            "Bag1", "Bag2", "Bag3", "Bag4"
        };

        /// <summary>Các thư mục nguồn được lập chỉ mục GUID (nơi có thể tra ra file từ một GUID).</summary>
        private static readonly string[] SourceFolders =
        {
            "GameObject", "Mesh", "Material", "Texture2D", "Shader", "Avatar",
            "AnimationClip", "AnimatorController", "AnimatorOverrideController", "Sprite", "PhysicsMaterial"
        };

        /// <summary>Thư mục nguồn được chép TRỌN VẸN (không cần dò phụ thuộc).</summary>
        private static readonly string[] WholeFolders = { "AnimationClip", "AnimatorController", "Avatar" };

        /// <summary>
        /// Thư mục KHÔNG mở ra để dò tiếp: mesh và texture không tham chiếu tới asset nào khác,
        /// quét nội dung nhị phân hàng chục MB của chúng chỉ tốn thời gian.
        /// </summary>
        private static readonly HashSet<string> LeafFolders = new HashSet<string> { "Mesh", "Texture2D" };

        /// <summary>
        /// Thư mục nguồn bị BỎ QUA hoàn toàn khi chép. <c>Shader</c> là shader "rỗng" do AssetRipper
        /// sinh ra — chép vào sẽ trùng tên với shader URP thật và làm hỏng toàn bộ material.
        /// </summary>
        private static readonly HashSet<string> SkipCopyFolders = new HashSet<string> { "Shader" };

        /// <summary>Ánh xạ thư mục nguồn → thư mục đích trong project.</summary>
        private static readonly Dictionary<string, string> FolderMap = new Dictionary<string, string>
        {
            { "AnimationClip", DestAnimations },
            { "AnimatorController", DestControllers },
            { "AnimatorOverrideController", DestControllers },
            { "Avatar", DestAvatars },
            { "Mesh", DestMeshes },
            { "Material", DestMaterials },
            { "PhysicsMaterial", DestMaterials },
            { "Texture2D", DestTextures },
            { "Sprite", DestTextures },
            { "GameObject", DestPrefabs }
        };

        // ------------------------------------------------------------------ Biểu thức chính quy

        private static readonly Regex GuidInFile = new Regex("guid: ([0-9a-f]{32})", RegexOptions.Compiled);
        private static readonly Regex GuidInMeta = new Regex(@"^guid:\s*([0-9a-f]{32})",
                                                             RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ShaderRefInMaterial =
            new Regex(@"m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*\d+\}",
                      RegexOptions.Compiled);

        // ------------------------------------------------------------------ Entry point

        /// <summary>Chép art gốc (nhân vật / vợt / animation) vào project. An toàn khi chạy lại.</summary>
        [MenuItem("Pickleball/Art/Import Original Character Art")]
        public static void ImportOriginalArt()
        {
            string rippedRoot = ResolveRippedRoot();
            if (rippedRoot == null)
            {
                Debug.LogError("[OriginalCharacterArtImporter] Không tìm thấy thư mục art gốc. " +
                               "Kỳ vọng ở '" + FallbackRippedRoot + "'.");
                return;
            }

            Debug.Log("[OriginalCharacterArtImporter] Nguồn: " + rippedRoot);

            foreach (string folder in new[]
                     {
                         DestRoot, DestAnimations, DestControllers, DestAvatars,
                         DestModels, DestMeshes, DestMaterials, DestTextures, DestPrefabs
                     })
            {
                EnsureFolder(folder);
            }

            Dictionary<string, string> guidToPath = BuildGuidIndex(rippedRoot);
            Debug.Log($"[OriginalCharacterArtImporter] Đã lập chỉ mục {guidToPath.Count} asset nguồn.");

            HashSet<string> needed = CollectDependencies(rippedRoot, guidToPath, out int missingSeeds);

            var stats = new SortedDictionary<string, CopyStats>(StringComparer.Ordinal);
            var copiedMaterials = new List<string>();
            var copiedClips = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string sourceFile in needed.OrderBy(p => p, StringComparer.Ordinal))
                {
                    string sourceFolder = Path.GetFileName(Path.GetDirectoryName(sourceFile));
                    if (sourceFolder == null || SkipCopyFolders.Contains(sourceFolder)) continue;
                    if (!FolderMap.TryGetValue(sourceFolder, out string destFolder)) continue;

                    string destFile = destFolder + "/" + Path.GetFileName(sourceFile);
                    CopyOutcome outcome = CopyWithMeta(sourceFile, destFile);

                    CopyStats entry = GetStats(stats, sourceFolder);
                    entry.DestFolder = destFolder;
                    entry.Register(outcome, outcome == CopyOutcome.Failed ? 0L : new FileInfo(sourceFile).Length);

                    if (outcome == CopyOutcome.Failed) continue;

                    if (destFile.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) copiedMaterials.Add(destFile);
                    if (destFile.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)) copiedClips.Add(destFile);
                }

                // Vá shader TRƯỚC khi Unity nhìn thấy material: nếu để Unity nạp material với shader
                // không tồn tại thì các property đã lưu có nguy cơ bị cắt bỏ khi lưu lại.
                int remappedShaders = RemapShaders(rippedRoot, guidToPath, copiedMaterials);
                int remappedEvents = RemapAnimationEvents(copiedClips);

                WriteReadme();

                Debug.Log($"[OriginalCharacterArtImporter] Đã ánh xạ shader cho {remappedShaders} material, " +
                          $"đổi tên animation event trong {remappedEvents} clip.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            ReportStats(stats, missingSeeds);
        }

        // ------------------------------------------------------------------ Dò phụ thuộc

        /// <summary>Đọc mọi <c>.meta</c> trong các thư mục nguồn để dựng bảng tra GUID → đường dẫn asset.</summary>
        private static Dictionary<string, string> BuildGuidIndex(string rippedRoot)
        {
            var index = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string folder in SourceFolders)
            {
                string absolute = Path.Combine(rippedRoot, folder);
                if (!Directory.Exists(absolute)) continue;

                foreach (string metaPath in Directory.EnumerateFiles(absolute, "*.meta", SearchOption.TopDirectoryOnly))
                {
                    string text;
                    try { text = File.ReadAllText(metaPath); }
                    catch (IOException) { continue; }

                    Match match = GuidInMeta.Match(text);
                    if (!match.Success) continue;

                    // Bỏ phần đuôi ".meta" để lấy asset tương ứng.
                    index[match.Groups[1].Value] = metaPath.Substring(0, metaPath.Length - 5);
                }
            }

            return index;
        }

        /// <summary>
        /// Duyệt đồ thị phụ thuộc theo chiều rộng từ các prefab hạt giống + toàn bộ clip/controller/avatar,
        /// trả về tập đường dẫn tuyệt đối của mọi asset cần chép.
        /// </summary>
        /// <param name="rippedRoot">Thư mục Assets của bản trích.</param>
        /// <param name="guidToPath">Bảng tra GUID → asset.</param>
        /// <param name="missingSeeds">Số prefab hạt giống không tìm thấy.</param>
        private static HashSet<string> CollectDependencies(string rippedRoot, Dictionary<string, string> guidToPath,
                                                           out int missingSeeds)
        {
            missingSeeds = 0;
            var stack = new Stack<string>();

            string prefabFolder = Path.Combine(rippedRoot, "GameObject");
            foreach (string seedName in SeedPrefabNames)
            {
                string path = Path.Combine(prefabFolder, seedName + ".prefab");
                if (File.Exists(path))
                {
                    stack.Push(path);
                }
                else
                {
                    missingSeeds++;
                    Debug.LogWarning("[OriginalCharacterArtImporter] Không có prefab hạt giống: " + seedName);
                }
            }

            foreach (string folder in WholeFolders)
            {
                string absolute = Path.Combine(rippedRoot, folder);
                if (!Directory.Exists(absolute)) continue;

                foreach (string file in Directory.EnumerateFiles(absolute, "*", SearchOption.TopDirectoryOnly))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    stack.Push(file);
                }
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (stack.Count > 0)
            {
                string current = stack.Pop();
                if (!visited.Add(current)) continue;

                string folderName = Path.GetFileName(Path.GetDirectoryName(current));
                if (folderName != null && LeafFolders.Contains(folderName)) continue;

                string content;
                try { content = File.ReadAllText(current); }
                catch (IOException) { continue; }

                foreach (Match match in GuidInFile.Matches(content))
                {
                    if (!guidToPath.TryGetValue(match.Groups[1].Value, out string target)) continue;
                    if (!visited.Contains(target)) stack.Push(target);
                }
            }

            return visited;
        }

        // ------------------------------------------------------------------ Chép file

        /// <summary>Kết quả của một lần chép.</summary>
        private enum CopyOutcome
        {
            /// <summary>Đã chép mới.</summary>
            Copied,

            /// <summary>Đích đã tồn tại — bỏ qua (idempotent).</summary>
            Skipped,

            /// <summary>Không chép được (lỗi IO hoặc GUID đụng độ với asset sẵn có).</summary>
            Failed
        }

        /// <summary>
        /// Chép một asset kèm <c>.meta</c>. Trước khi chép, kiểm tra GUID trong <c>.meta</c> có đụng
        /// một asset ĐANG CÓ trong project hay không — nếu đụng thì bỏ qua để tránh hỏng liên kết cũ.
        /// </summary>
        private static CopyOutcome CopyWithMeta(string sourceFile, string destAssetPath)
        {
            string destAbsolute = ToAbsolute(destAssetPath);
            if (File.Exists(destAbsolute)) return CopyOutcome.Skipped;

            string sourceMeta = sourceFile + ".meta";
            if (!File.Exists(sourceMeta))
            {
                Debug.LogWarning("[OriginalCharacterArtImporter] Thiếu .meta, bỏ qua: " + sourceFile);
                return CopyOutcome.Failed;
            }

            try
            {
                string metaText = File.ReadAllText(sourceMeta);
                Match guidMatch = GuidInMeta.Match(metaText);
                if (guidMatch.Success)
                {
                    string existing = AssetDatabase.GUIDToAssetPath(guidMatch.Groups[1].Value);
                    if (!string.IsNullOrEmpty(existing) && !existing.StartsWith(DestRoot, StringComparison.Ordinal))
                    {
                        Debug.LogWarning("[OriginalCharacterArtImporter] GUID đụng độ với asset sẵn có '" +
                                         existing + "' — bỏ qua " + Path.GetFileName(sourceFile));
                        return CopyOutcome.Failed;
                    }
                }

                File.Copy(sourceFile, destAbsolute, false);
                File.Copy(sourceMeta, destAbsolute + ".meta", true);
                return CopyOutcome.Copied;
            }
            catch (IOException exception)
            {
                Debug.LogWarning("[OriginalCharacterArtImporter] Lỗi chép " + sourceFile + ": " + exception.Message);
                return CopyOutcome.Failed;
            }
        }

        // ------------------------------------------------------------------ Vá shader

        /// <summary>
        /// Viết lại tham chiếu shader trong các file <c>.mat</c> đã chép sang shader THẬT của project.
        /// <para>
        /// AssetRipper xuất shader chỉ có khối <c>Properties</c> nên không dùng được. Hàm này đọc dòng
        /// đầu của file <c>.shader</c> gốc để lấy TÊN shader, tra shader cùng tên trong project rồi
        /// thay cặp <c>fileID/guid</c> ngay trong văn bản YAML — nhờ đó toàn bộ texture/màu đã lưu
        /// trong material được giữ nguyên.
        /// </para>
        /// <para>
        /// Riêng <c>Shader Graphs/ArnoldStandardSurface</c> không có trong project ta nên rơi về
        /// URP/Lit, kèm đổi tên property <c>_BASE_COLOR_MAP → _BaseMap</c>,
        /// <c>_NORMAL_MAP → _BumpMap</c>, <c>_BASE_COLOR → _BaseColor</c> để vẫn thấy đúng texture.
        /// </para>
        /// </summary>
        /// <returns>Số material đã được viết lại.</returns>
        private static int RemapShaders(string rippedRoot, Dictionary<string, string> guidToPath,
                                        List<string> materialAssetPaths)
        {
            if (materialAssetPaths.Count == 0) return 0;

            // GUID shader gốc -> tên shader (đọc từ dòng đầu file .shader đã trích).
            var guidToShaderName = new Dictionary<string, string>(StringComparer.Ordinal);
            string shaderFolder = Path.Combine(rippedRoot, "Shader");
            if (Directory.Exists(shaderFolder))
            {
                foreach (KeyValuePair<string, string> pair in guidToPath)
                {
                    if (!pair.Value.EndsWith(".shader", StringComparison.OrdinalIgnoreCase)) continue;

                    string name = ReadShaderName(pair.Value);
                    if (name != null) guidToShaderName[pair.Key] = name;
                }
            }

            var resolved = new Dictionary<string, string>(StringComparer.Ordinal); // guid gốc -> chuỗi thay thế
            int rewritten = 0;

            foreach (string materialAssetPath in materialAssetPaths)
            {
                string absolute = ToAbsolute(materialAssetPath);
                if (!File.Exists(absolute)) continue;

                string text;
                try { text = File.ReadAllText(absolute); }
                catch (IOException) { continue; }

                Match match = ShaderRefInMaterial.Match(text);
                if (!match.Success) continue;

                string originalGuid = match.Groups[1].Value;
                if (!guidToShaderName.TryGetValue(originalGuid, out string shaderName)) continue;

                bool isArnold = shaderName.StartsWith("Shader Graphs/", StringComparison.Ordinal);

                if (!resolved.TryGetValue(originalGuid, out string replacement))
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");

                    if (shader == null ||
                        !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shader, out string guid, out long localId))
                    {
                        Debug.LogWarning("[OriginalCharacterArtImporter] Không tra được shader thay thế cho '" +
                                         shaderName + "'.");
                        resolved[originalGuid] = null;
                        continue;
                    }

                    replacement = "m_Shader: {fileID: " + localId + ", guid: " + guid + ", type: 3}";
                    resolved[originalGuid] = replacement;
                }

                if (replacement == null) continue;

                text = ShaderRefInMaterial.Replace(text, replacement, 1);

                if (isArnold)
                {
                    text = text.Replace("      _BASE_COLOR_MAP:", "      _BaseMap:")
                               .Replace("      _NORMAL_MAP:", "      _BumpMap:")
                               .Replace("      _BASE_COLOR: {", "      _BaseColor: {");
                }

                try
                {
                    File.WriteAllText(absolute, text);
                    rewritten++;
                }
                catch (IOException exception)
                {
                    Debug.LogWarning("[OriginalCharacterArtImporter] Không ghi được " + materialAssetPath + ": " +
                                     exception.Message);
                }
            }

            return rewritten;
        }

        /// <summary>Lấy tên shader từ dòng <c>Shader "..." {</c> đầu file.</summary>
        private static string ReadShaderName(string shaderFilePath)
        {
            try
            {
                foreach (string line in File.ReadLines(shaderFilePath))
                {
                    int first = line.IndexOf('"');
                    if (first < 0) continue;

                    int last = line.IndexOf('"', first + 1);
                    if (last <= first) continue;

                    return line.Substring(first + 1, last - first - 1);
                }
            }
            catch (IOException)
            {
                // Không đọc được thì coi như không biết tên shader.
            }

            return null;
        }

        // ------------------------------------------------------------------ Vá animation event

        /// <summary>Tên event trong clip gốc → tên hàm mà <see cref="PlayerAnimationEventReceiver"/> khai báo.</summary>
        private static readonly Dictionary<string, string> AnimationEventRenames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "OnThrow", "OnThrowBall" },
                { "OnHit", "OnShotHit" }
            };

        /// <summary>
        /// Đổi tên animation event trong các clip ĐÃ CHÉP cho khớp với
        /// <see cref="PlayerAnimationEventReceiver"/>. Chỉ đụng vào bản sao trong
        /// <c>ArtFromOriginal</c>, không đụng file runtime và không đụng thư mục gốc.
        /// Số khung hình của event giữ nguyên nên timing y hệt bản gốc.
        /// </summary>
        /// <returns>Số clip đã được sửa.</returns>
        private static int RemapAnimationEvents(List<string> clipAssetPaths)
        {
            int changed = 0;

            foreach (string clipAssetPath in clipAssetPaths)
            {
                string absolute = ToAbsolute(clipAssetPath);
                if (!File.Exists(absolute)) continue;

                string text;
                try { text = File.ReadAllText(absolute); }
                catch (IOException) { continue; }

                string updated = text;
                foreach (KeyValuePair<string, string> rename in AnimationEventRenames)
                {
                    updated = updated.Replace("functionName: " + rename.Key + "\n",
                                              "functionName: " + rename.Value + "\n");
                    updated = updated.Replace("functionName: " + rename.Key + "\r\n",
                                              "functionName: " + rename.Value + "\r\n");
                }

                if (updated == text) continue;

                try
                {
                    File.WriteAllText(absolute, updated);
                    changed++;
                }
                catch (IOException exception)
                {
                    Debug.LogWarning("[OriginalCharacterArtImporter] Không ghi được clip " + clipAssetPath + ": " +
                                     exception.Message);
                }
            }

            return changed;
        }

        // ------------------------------------------------------------------ README

        /// <summary>Ghi cảnh báo bản quyền vào <c>ArtFromOriginal/README.txt</c>.</summary>
        private static void WriteReadme()
        {
            var text = new StringBuilder();
            text.AppendLine("========================================================================");
            text.AppendLine(" ART CỦA BẢN GỐC - CHỈ DÙNG ĐỂ DỰNG LẠI GAMEPLAY, PHẢI THAY TRƯỚC KHI PHÁT HÀNH");
            text.AppendLine("========================================================================");
            text.AppendLine();
            text.AppendLine("Toàn bộ mesh, texture, material, animation clip, animator controller, avatar");
            text.AppendLine("và prefab trong thư mục này được TRÍCH TỪ BẢN GỐC bằng AssetRipper.");
            text.AppendLine();
            text.AppendLine("KHÔNG được phát hành sản phẩm còn dùng bất kỳ file nào ở đây.");
            text.AppendLine("Chúng chỉ tồn tại để:");
            text.AppendLine("  1. dựng lại gameplay/animation cho ĐÚNG cảm giác bản gốc,");
            text.AppendLine("  2. làm chuẩn đối chiếu khi reskin bằng art do mình tự làm.");
            text.AppendLine();
            text.AppendLine("TRƯỚC KHI PHÁT HÀNH, bắt buộc:");
            text.AppendLine("  - thay toàn bộ Models/Meshes, Models/Materials, Models/Textures bằng art riêng;");
            text.AppendLine("  - thay Animations/*.anim bằng animation tự làm hoặc có bản quyền hợp lệ;");
            text.AppendLine("  - xoá sạch thư mục Assets/Project/ArtFromOriginal.");
            text.AppendLine();
            text.AppendLine("--- Ghi chú kỹ thuật ---");
            text.AppendLine();
            text.AppendLine("1) File .meta được chép kèm để GUID giữ nguyên, nhờ đó liên kết");
            text.AppendLine("   material -> texture, controller -> clip, prefab -> mesh/avatar còn nguyên vẹn.");
            text.AppendLine();
            text.AppendLine("2) Prefab gốc tham chiếu script của bản gốc (Yudiz.*, StarterKit.*) với GUID khác,");
            text.AppendLine("   nên các component đó hiện ra dạng 'Missing (Mono Script)'. Điều này BÌNH THƯỜNG.");
            text.AppendLine("   Dùng menu 'Pickleball/Art/Build Playable Character Prefabs' để sinh ra");
            text.AppendLine("   prefab sạch trong Assets/Project/Prefabs/Characters.");
            text.AppendLine();
            text.AppendLine("3) Thư mục Shader của bản trích KHÔNG được chép: AssetRipper chỉ xuất được khối");
            text.AppendLine("   Properties chứ không có code shader. Thay vào đó, tham chiếu shader trong");
            text.AppendLine("   từng file .mat đã được viết lại sang shader thật của project (URP Lit/Unlit).");
            text.AppendLine();
            text.AppendLine("4) Animation event trong clip đã được đổi tên cho khớp PlayerAnimationEventReceiver:");
            text.AppendLine("   OnThrow -> OnThrowBall, OnHit -> OnShotHit. Khung hình giữ nguyên.");
            text.AppendLine();

            try
            {
                File.WriteAllText(ToAbsolute(ReadmePath), text.ToString(), new UTF8Encoding(false));
            }
            catch (IOException exception)
            {
                Debug.LogWarning("[OriginalCharacterArtImporter] Không ghi được README: " + exception.Message);
            }
        }

        // ------------------------------------------------------------------ Thống kê

        /// <summary>Bộ đếm số file / dung lượng cho một nhóm asset.</summary>
        private sealed class CopyStats
        {
            /// <summary>Số file chép mới trong lần chạy này.</summary>
            public int Copied;

            /// <summary>Số file đã có sẵn ở đích nên bỏ qua.</summary>
            public int Skipped;

            /// <summary>Số file không chép được.</summary>
            public int Failed;

            /// <summary>Tổng dung lượng (byte) của các file nhóm này hiện có trong project.</summary>
            public long Bytes;

            /// <summary>Thư mục đích của nhóm, dùng khi in báo cáo.</summary>
            public string DestFolder;

            /// <summary>Ghi nhận kết quả một lần chép kèm dung lượng file.</summary>
            /// <param name="outcome">Kết quả chép.</param>
            /// <param name="bytes">Dung lượng file (0 khi thất bại).</param>
            public void Register(CopyOutcome outcome, long bytes)
            {
                switch (outcome)
                {
                    case CopyOutcome.Copied: Copied++; break;
                    case CopyOutcome.Skipped: Skipped++; break;
                    default: Failed++; break;
                }

                Bytes += bytes;
            }
        }

        private static CopyStats GetStats(SortedDictionary<string, CopyStats> stats, string key)
        {
            if (!stats.TryGetValue(key, out CopyStats entry))
            {
                entry = new CopyStats();
                stats[key] = entry;
            }

            return entry;
        }

        /// <summary>In bảng tổng kết số file và dung lượng thực tế nằm trong project theo từng nhóm.</summary>
        private static void ReportStats(SortedDictionary<string, CopyStats> stats, int missingSeeds)
        {
            var report = new StringBuilder();
            report.AppendLine("[OriginalCharacterArtImporter] KẾT QUẢ");
            report.AppendLine("  nhóm                          chép mới  bỏ qua  lỗi     dung lượng");

            long totalBytes = 0;
            int totalFiles = 0;

            foreach (KeyValuePair<string, CopyStats> pair in stats)
            {
                CopyStats value = pair.Value;
                report.AppendLine($"  {pair.Key,-28} {value.Copied,8} {value.Skipped,7} " +
                                  $"{value.Failed,5}   {value.Bytes / 1048576f,8:F2} MB -> {value.DestFolder}");

                totalFiles += value.Copied + value.Skipped;
                totalBytes += value.Bytes;
            }

            report.AppendLine($"  {"TỔNG (có trong project)",-28} {totalFiles,8} file            " +
                              $"{totalBytes / 1048576f,8:F2} MB");

            foreach (string destFolder in FolderMap.Values.Distinct())
            {
                report.AppendLine($"  thư mục {destFolder,-52} {MeasureFolder(destFolder) / 1048576f,8:F2} MB");
            }

            if (missingSeeds > 0) report.AppendLine($"  CẢNH BÁO: {missingSeeds} prefab hạt giống không tìm thấy.");

            report.AppendLine("  Shader của bản trích ĐÃ BỊ BỎ QUA (stub) — material đã trỏ sang shader thật của project.");

            Debug.Log(report.ToString());
        }

        /// <summary>Tổng dung lượng file (không tính <c>.meta</c>) của một thư mục đích.</summary>
        private static long MeasureFolder(string assetFolder)
        {
            string absolute = ToAbsolute(assetFolder);
            if (!Directory.Exists(absolute)) return 0;

            long total = 0;
            foreach (string file in Directory.EnumerateFiles(absolute, "*", SearchOption.TopDirectoryOnly))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                total += new FileInfo(file).Length;
            }

            return total;
        }

        // ------------------------------------------------------------------ Tiện ích

        /// <summary>
        /// Suy ra thư mục <c>ripped2/ExportedProject/Assets</c> từ vị trí project;
        /// không thấy thì dùng <see cref="FallbackRippedRoot"/>.
        /// </summary>
        private static string ResolveRippedRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot != null)
            {
                string parent = Directory.GetParent(projectRoot)?.FullName;
                if (parent != null)
                {
                    string candidate = Path.Combine(parent, "ripped2", "ExportedProject", "Assets");
                    if (Directory.Exists(candidate)) return candidate;
                }
            }

            return Directory.Exists(FallbackRippedRoot) ? FallbackRippedRoot : null;
        }

        /// <summary>Đổi đường dẫn kiểu <c>Assets/...</c> sang đường dẫn tuyệt đối trên đĩa.</summary>
        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        /// <summary>Tạo thư mục asset (kể cả các cấp cha còn thiếu).</summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            int lastSlash = folderPath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            string parent = folderPath.Substring(0, lastSlash);
            string leaf = folderPath.Substring(lastSlash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
