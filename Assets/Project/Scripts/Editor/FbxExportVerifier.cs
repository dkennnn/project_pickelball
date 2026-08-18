using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Đọc lại các file FBX đã xuất và ĐẾM xem bên trong thật sự có gì.
    /// <para>
    /// Cần bước này vì "file tồn tại và dung lượng khác 0" không chứng minh được điều gì: một
    /// FBX chỉ có mesh mà thiếu animation trông y hệt một FBX đầy đủ nếu chỉ nhìn dung lượng.
    /// Đây đúng là trường hợp đã xảy ra — mọi file xuất ra đều nặng bằng nhau đến từng KB.
    /// </para>
    /// </summary>
    public static class FbxExportVerifier
    {
        private const string ExportFolder = "Assets/Project/ExportedFBX";

        [MenuItem("Pickleball/Export/Verify Exported FBX")]
        public static void Verify()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ExportFolder });
            if (guids.Length == 0)
            {
                Debug.LogError($"[FbxExportVerifier] Không có model nào trong {ExportFolder}.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine("file | clip | xương | mesh | đỉnh");
            report.AppendLine("--- | ---: | ---: | ---: | ---:");
            var clipNames = new StringBuilder();

            int withAnimation = 0;

            foreach (string guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] contents = AssetDatabase.LoadAllAssetsAtPath(path);

                List<AnimationClip> clips = contents.OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToList();

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SkinnedMeshRenderer[] skinned = root != null
                    ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    : new SkinnedMeshRenderer[0];

                int bones = skinned.Length > 0 ? skinned.Max(s => s.bones != null ? s.bones.Length : 0) : 0;
                int vertices = contents.OfType<Mesh>().Sum(m => m.vertexCount);

                if (clips.Count > 0) withAnimation++;

                report.AppendLine($"{Path.GetFileName(path)} | {clips.Count} | {bones} | " +
                                  $"{contents.OfType<Mesh>().Count()} | {vertices}");

                clipNames.AppendLine($"### {Path.GetFileName(path)}");
                clipNames.AppendLine(string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s)")));
                clipNames.AppendLine();
            }

            report.AppendLine();
            report.AppendLine("## Tên clip trong từng file");
            report.AppendLine();
            report.Append(clipNames);

            string outPath = Path.Combine(Path.GetFullPath(ExportFolder), "verify_report.md");
            File.WriteAllText(outPath, report.ToString());

            Debug.Log($"[FbxExportVerifier] {guids.Length} file, {withAnimation} file CÓ animation clip. " +
                      $"Chi tiết: {outPath}");
        }
    }
}
