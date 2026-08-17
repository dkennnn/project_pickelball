using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Kiểm tra nhanh các thiết lập project mà gameplay phụ thuộc: tag, layer, render pipeline,
    /// và các ScriptableObject bắt buộc. Chạy được cả trong Editor lẫn batchmode
    /// (<c>-executeMethod Pickleball.EditorTools.ProjectSetupValidator.Validate</c>).
    /// </summary>
    public static class ProjectSetupValidator
    {
        private static readonly string[] RequiredTags = { "Net", "Ground", "Ball", "Player" };
        private static readonly string[] RequiredLayers = { "Ground", "Ball", "Player", "CourtBounds" };
        private static readonly string[] RequiredAssets =
        {
            "Assets/Project/ScriptableObjects/Game/GameSettings.asset",
            "Assets/Project/ScriptableObjects/Game/GameMessages.asset",
            "Assets/Project/ScriptableObjects/Profiles/PlayerProfileLimits.asset",
            "Assets/Project/Scenes/GreyboxMatch.unity",
            "Assets/Project/Settings/PickleballUniversalRP.asset"
        };

        [MenuItem("Pickleball/Validate Project Setup")]
        public static void Validate()
        {
            var problems = new List<string>();
            var report = new StringBuilder("[ProjectSetupValidator]\n");

            // --- Tags ---
            string[] tags = InternalEditorUtility.tags;
            foreach (string tag in RequiredTags)
            {
                bool ok = tags.Contains(tag);
                report.AppendLine($"  tag  {tag,-12} : {(ok ? "OK" : "THIẾU")}");
                if (!ok) problems.Add($"Thiếu tag '{tag}'");
            }

            // --- Layers ---
            // LayerMask.NameToLayer trả -1 khi TagManager.asset không parse được,
            // nên đây cũng là phép thử tính hợp lệ của file YAML đó.
            foreach (string layer in RequiredLayers)
            {
                int index = LayerMask.NameToLayer(layer);
                report.AppendLine($"  layer {layer,-12} : {(index >= 0 ? "index " + index : "KHÔNG RESOLVE ĐƯỢC (-1)")}");
                if (index < 0) problems.Add($"Layer '{layer}' không resolve được — kiểm tra ProjectSettings/TagManager.asset");
            }

            // --- Render pipeline ---
            RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
            report.AppendLine($"  pipeline mặc định     : {(pipeline != null ? pipeline.name : "KHÔNG CÓ (Built-in) → material URP sẽ ra màu hồng")}");
            if (pipeline == null) problems.Add("GraphicsSettings chưa gán render pipeline — chạy 'Pickleball/Setup URP Pipeline'");

            int qualityMissing = 0;
            int original = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline == null) qualityMissing++;
            }
            QualitySettings.SetQualityLevel(original, false);
            report.AppendLine($"  quality level thiếu RP: {qualityMissing}/{QualitySettings.names.Length}");
            if (qualityMissing > 0) problems.Add($"{qualityMissing} quality level chưa gán render pipeline");

            // --- Asset bắt buộc ---
            foreach (string path in RequiredAssets)
            {
                bool ok = AssetDatabase.LoadAssetAtPath<Object>(path) != null;
                report.AppendLine($"  asset {System.IO.Path.GetFileName(path),-28} : {(ok ? "OK" : "THIẾU")}");
                if (!ok) problems.Add($"Thiếu asset '{path}'");
            }

            if (problems.Count == 0)
            {
                report.AppendLine("  ==> TẤT CẢ HỢP LỆ");
                Debug.Log(report.ToString());
            }
            else
            {
                report.AppendLine($"  ==> CÓ {problems.Count} VẤN ĐỀ:");
                foreach (string p in problems) report.AppendLine($"      - {p}");
                Debug.LogError(report.ToString());
            }
        }
    }
}
