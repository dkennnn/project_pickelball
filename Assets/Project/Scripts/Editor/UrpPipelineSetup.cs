using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Tạo URP Asset + Universal Renderer Data rồi gán vào Graphics Settings và MỌI quality level.
    /// <para>
    /// Project được tạo bằng <c>-createProject</c> (template Built-in) rồi mới thêm package URP,
    /// nên Unity chỉ sinh <c>UniversalRenderPipelineGlobalSettings.asset</c> chứ KHÔNG tạo
    /// URP Asset và cũng không gán vào Graphics Settings. Hậu quả: pipeline đang chạy vẫn là
    /// Built-in, mọi material dùng shader "Universal Render Pipeline/Lit" render ra màu hồng.
    /// </para>
    /// Script này idempotent: chạy lại sẽ dùng lại asset cũ nếu đã tồn tại.
    /// </summary>
    public static class UrpPipelineSetup
    {
        private const string SettingsFolder = "Assets/Project/Settings";
        private const string RendererDataPath = SettingsFolder + "/PickleballUniversalRenderer.asset";
        private const string PipelineAssetPath = SettingsFolder + "/PickleballUniversalRP.asset";

        [MenuItem("Pickleball/Setup URP Pipeline")]
        public static void Setup()
        {
            EnsureFolder(SettingsFolder);

            UniversalRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
                Debug.Log($"[UrpPipelineSetup] Đã tạo Renderer Data: {RendererDataPath}");
            }

            UniversalRenderPipelineAsset pipelineAsset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
                Debug.Log($"[UrpPipelineSetup] Đã tạo URP Asset: {PipelineAssetPath}");
            }

            // Cấu hình hợp lý cho mobile portrait.
            pipelineAsset.msaaSampleCount = 4;
            pipelineAsset.supportsHDR = false;
            pipelineAsset.shadowDistance = 40f;

            // 1) Pipeline mặc định của project.
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            // 2) Gán cho MỌI quality level — nếu bỏ sót một level thì đổi Quality sẽ lại ra màu hồng.
            int originalLevel = QualitySettings.GetQualityLevel();
            int levelCount = QualitySettings.names.Length;
            for (int i = 0; i < levelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }
            QualitySettings.SetQualityLevel(originalLevel, false);

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipelineAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UrpPipelineSetup] Đã gán URP cho Graphics Settings và {levelCount} quality level. " +
                      "Material URP/Lit sẽ hết màu hồng sau khi Unity nạp lại pipeline.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
