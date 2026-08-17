using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Đặt project và Game view về màn hình DỌC 1080×1920 giống bản gốc.
    /// <para>
    /// Toàn bộ layout UI được trích từ bản gốc dựa trên CanvasScaler reference 1080×1920
    /// (portrait). Chạy ở tỉ lệ ngang thì mọi màn hình bị kéo méo và tràn khỏi khung —
    /// đây là lý do "bấm Play thấy ảnh nền phủ kín mà không thấy nút nào".
    /// </para>
    /// </summary>
    public static class PortraitSetup
    {
        private const int ReferenceWidth = 1080;
        private const int ReferenceHeight = 1920;
        private const string GameViewSizeName = "Pickleball Portrait 1080x1920";

        [MenuItem("Pickleball/Setup Portrait Screen")]
        public static void Setup()
        {
            ApplyPlayerSettings();
            bool added = TryAddGameViewSize();

            string message =
                $"Player Settings đã đặt về màn hình dọc {ReferenceWidth}x{ReferenceHeight}.\n\n" +
                (added
                    ? $"Đã thêm cỡ Game view '{GameViewSizeName}'.\nHãy mở tab Game và chọn cỡ đó trong danh sách aspect."
                    : "KHÔNG tự thêm được cỡ Game view (API nội bộ của Unity đã đổi).\n" +
                      $"Hãy tự thêm tay: tab Game → dropdown aspect → dấu + → Fixed Resolution {ReferenceWidth} x {ReferenceHeight}.");

            Debug.Log("[PortraitSetup] " + message.Replace("\n\n", " | ").Replace("\n", " "));
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Màn hình dọc", message, "OK");
        }

        /// <summary>Khoá hướng màn hình về Portrait cho mọi build.</summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Tắt xoay sang ngang; chỉ cho phép portrait và portrait-upside-down.
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            // Cỡ cửa sổ mặc định cho build Standalone (tiện chạy thử trên PC).
            PlayerSettings.defaultScreenWidth = ReferenceWidth / 2;
            PlayerSettings.defaultScreenHeight = ReferenceHeight / 2;
            PlayerSettings.resizableWindow = true;

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Thêm cỡ Game view cố định 1080×1920 bằng reflection vào API nội bộ
        /// <c>UnityEditor.GameViewSizes</c> (không có API công khai cho việc này).
        /// </summary>
        /// <returns>true nếu thêm được hoặc đã tồn tại sẵn.</returns>
        private static bool TryAddGameViewSize()
        {
            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                if (sizesType == null || sizeType == null || sizeTypeEnum == null) return false;

                Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizesInstance = singletonType.GetProperty("instance",
                    BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (sizesInstance == null) return false;

                object group = sizesType.GetProperty("currentGroup",
                    BindingFlags.Public | BindingFlags.Instance)?.GetValue(sizesInstance);
                if (group == null) return false;

                Type groupType = group.GetType();

                // Đã có rồi thì thôi.
                var totalCount = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                MethodInfo getSize = groupType.GetMethod("GetGameViewSize");
                for (int i = 0; i < totalCount; i++)
                {
                    object existing = getSize.Invoke(group, new object[] { i });
                    string baseText = existing.GetType()
                        .GetProperty("baseText")?.GetValue(existing) as string;
                    if (baseText == GameViewSizeName) return true;
                }

                object fixedResolution = Enum.Parse(sizeTypeEnum, "FixedResolution");
                ConstructorInfo ctor = sizeType.GetConstructor(new[]
                {
                    sizeTypeEnum, typeof(int), typeof(int), typeof(string)
                });
                if (ctor == null) return false;

                object newSize = ctor.Invoke(new[]
                {
                    fixedResolution, (object)ReferenceWidth, ReferenceHeight, GameViewSizeName
                });

                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                sizesType.GetMethod("SaveToHDD")?.Invoke(sizesInstance, null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PortraitSetup] Không thêm được cỡ Game view: " + e.Message);
                return false;
            }
        }
    }
}
