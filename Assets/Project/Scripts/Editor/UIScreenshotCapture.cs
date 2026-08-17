using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Chụp ảnh từng prefab màn hình trong <c>Prefabs/UI/Screens/</c> ra file PNG 1080x1920
    /// trong <c>ui_layout/previews/</c> để so trực quan với bản gốc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Menu này CẦN chạy trong Unity Editor CÓ ĐỒ HOẠ.</b> Chạy batchmode với <c>-nographics</c>
    /// thì không có thiết bị đồ hoạ để render: công cụ phát hiện
    /// <c>SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null</c>, báo lỗi rõ ràng rồi thoát
    /// (không crash, không ghi file rỗng). Batchmode CÓ đồ hoạ (không truyền <c>-nographics</c>) thì chạy được.
    /// </para>
    /// <para>
    /// Cách làm: mở một scene rỗng tạm (không bao giờ lưu xuống đĩa), dựng một Camera orthographic
    /// render vào <see cref="RenderTexture"/> 1080x1920, instantiate prefab rồi ép mọi Canvas sang
    /// <see cref="RenderMode.ScreenSpaceCamera"/> trỏ vào camera đó, render một frame, đọc pixel ra PNG.
    /// Xong thì huỷ instance và khôi phục lại đúng scene người dùng đang mở.
    /// </para>
    /// <para>
    /// Ảnh chụp là trạng thái TĨNH của prefab — không script runtime nào chạy, nên những widget mà
    /// bản gốc bật lên lúc chơi sẽ không xuất hiện. Dùng ảnh này để bắt lỗi bố cục/vô hình,
    /// không phải để nghiệm thu pixel-perfect.
    /// </para>
    /// </remarks>
    public static class UIScreenshotCapture
    {
        /// <summary>Bề rộng ảnh chụp, khớp referenceResolution của CanvasScaler.</summary>
        public const int CaptureWidth = 1080;

        /// <summary>Chiều cao ảnh chụp, khớp referenceResolution của CanvasScaler.</summary>
        public const int CaptureHeight = 1920;

        /// <summary>Tên thư mục ảnh trong ui_layout.</summary>
        public const string PreviewFolderName = "previews";

        /// <summary>Khoảng cách từ camera tới mặt phẳng canvas (đơn vị world).</summary>
        private const float CanvasPlaneDistance = 10f;

        /// <summary>Màu nền của ảnh — xám đậm để thấy được UI trắng lẫn UI tối.</summary>
        private static readonly Color BackgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);

        /// <summary>Tên GameObject camera tạm; đặt tên riêng để dọn cho sạch.</summary>
        private const string CameraName = "__UIPreviewCamera";

        /// <summary>
        /// Chụp toàn bộ prefab màn hình ra <c>ui_layout/previews/&lt;Tên&gt;.png</c>.
        /// Cần Editor có đồ hoạ; thiếu thiết bị đồ hoạ thì báo lỗi và thoát an toàn.
        /// </summary>
        [MenuItem("Pickleball/UI/Capture Screen Previews")]
        public static void CaptureScreenPreviews()
        {
            if (!EnsureGraphicsDevice()) return;

            string layoutRoot = UILayoutPaths.FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogError("[UIScreenshotCapture] Không tìm thấy thư mục ui_layout (cần có _index.json).");
                return;
            }

            string outputFolder = Path.Combine(layoutRoot, PreviewFolderName);
            try
            {
                Directory.CreateDirectory(outputFolder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIScreenshotCapture] Không tạo được thư mục ảnh {outputFolder}: {e.Message}");
                return;
            }

            List<string> prefabPaths = CollectScreenPrefabPaths();
            if (prefabPaths.Count == 0)
            {
                Debug.LogError($"[UIScreenshotCapture] Không có prefab nào trong {UILayoutPaths.ScreenPrefabFolder}.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[UIScreenshotCapture] Người dùng huỷ — không chụp.");
                return;
            }

            // Nhớ lại các scene đang mở để trả về nguyên trạng sau khi chụp xong.
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            var captured = new List<string>();
            var failed = new List<string>();
            RenderTexture renderTexture = null;
            GameObject cameraObject = null;

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "__UIPreviewRT",
                    antiAliasing = 1
                };
                renderTexture.Create();

                cameraObject = new GameObject(CameraName);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BackgroundColor;
                camera.cullingMask = ~0;
                camera.targetTexture = renderTexture;
                cameraObject.transform.position = new Vector3(0f, 0f, -100f);

                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string prefabPath = prefabPaths[i];
                    string screenName = Path.GetFileNameWithoutExtension(prefabPath);

                    EditorUtility.DisplayProgressBar("Capture Screen Previews",
                        $"{screenName} ({i + 1}/{prefabPaths.Count})", (i + 1) / (float)prefabPaths.Count);

                    string pngPath = Path.Combine(outputFolder, screenName + ".png");
                    if (CaptureOne(prefabPath, camera, renderTexture, pngPath)) captured.Add(pngPath);
                    else failed.Add(screenName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIScreenshotCapture] Dừng giữa chừng vì lỗi: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Cleanup(cameraObject, renderTexture);
                RestoreScenes(previousSetup);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[UIScreenshotCapture] Đã chụp {captured.Count}/{prefabPaths.Count} màn hình ({CaptureWidth}x{CaptureHeight}).");
            sb.AppendLine($"  Thư mục: {outputFolder}");
            sb.AppendLine($"  So sánh với: {Path.Combine(layoutRoot, "original_sprites_reference")}");
            if (failed.Count > 0) sb.AppendLine("  Thất bại: " + string.Join(", ", failed));
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Kiểm tra Editor có thiết bị đồ hoạ để render hay không.
        /// </summary>
        /// <returns>true nếu render được; false thì đã log lỗi giải thích và người gọi phải thoát.</returns>
        public static bool EnsureGraphicsDevice()
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) return true;

            Debug.LogError(
                "[UIScreenshotCapture] KHÔNG render được: SystemInfo.graphicsDeviceType == Null.\n" +
                "Unity đang chạy không có thiết bị đồ hoạ (thường do batchmode với cờ -nographics).\n" +
                "Menu 'Pickleball/UI/Capture Screen Previews' phải chạy trong Editor có đồ hoạ,\n" +
                "hoặc batchmode BỎ cờ -nographics. Đã thoát an toàn, không ghi file nào.");
            return false;
        }

        /// <summary>Liệt kê prefab màn hình trong <see cref="UILayoutPaths.ScreenPrefabFolder"/>.</summary>
        /// <returns>Danh sách đường dẫn asset, sắp theo thứ tự chữ cái.</returns>
        public static List<string> CollectScreenPrefabPaths()
        {
            var paths = new List<string>();
            if (!AssetDatabase.IsValidFolder(UILayoutPaths.ScreenPrefabFolder)) return paths;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { UILayoutPaths.ScreenPrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path)) paths.Add(path);
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        private static bool CaptureOne(string prefabPath, Camera camera, RenderTexture renderTexture, string pngPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIScreenshotCapture] Không nạp được prefab: {prefabPath}");
                return false;
            }

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) instance = Object.Instantiate(prefab);
                if (instance == null)
                {
                    Debug.LogWarning($"[UIScreenshotCapture] Không instantiate được: {prefabPath}");
                    return false;
                }

                PrepareInstance(instance, camera, prefab.name);
                RenderFrame(camera, renderTexture);
                byte[] png = ReadBack(renderTexture);
                if (png == null) return false;

                File.WriteAllBytes(pngPath, png);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIScreenshotCapture] {Path.GetFileNameWithoutExtension(prefabPath)}: lỗi khi chụp — {e.Message}");
                return false;
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        private static void PrepareInstance(GameObject instance, Camera camera, string prefabName)
        {
            instance.transform.position = Vector3.zero;
            if (instance.transform.localScale.sqrMagnitude < 1e-8f) instance.transform.localScale = Vector3.one;

            if (!instance.activeSelf)
            {
                // Node gốc tắt thì ảnh sẽ trống trơn — bật tạm trên bản instance (prefab trên đĩa không đổi).
                instance.SetActive(true);
                Debug.LogWarning($"[UIScreenshotCapture] {prefabName}: node gốc đang activeSelf = false, đã bật tạm để chụp.");
            }

            foreach (Canvas canvas in instance.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas == null) continue;
                canvas.enabled = true;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = CanvasPlaneDistance;
            }

            foreach (CanvasScaler scaler in instance.GetComponentsInChildren<CanvasScaler>(true))
            {
                if (scaler == null) continue;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(CaptureWidth, CaptureHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Canvas.ForceUpdateCanvases();

            if (instance.transform is RectTransform rect)
            {
                try
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UIScreenshotCapture] {prefabName}: dựng layout lỗi — {e.Message}");
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void RenderFrame(Camera camera, RenderTexture renderTexture)
        {
            camera.targetTexture = renderTexture;

            // Trong SRP (URP), Camera.Render() không được hỗ trợ chính thức — ưu tiên RenderRequest.
            if (RenderPipelineManager.currentPipeline != null && TrySubmitRenderRequest(camera, renderTexture)) return;

            camera.Render();
        }

        /// <summary>
        /// Thử render bằng API render request của URP (<c>UniversalRenderPipeline.SingleCameraRequest</c>)
        /// qua reflection, để không phải tham chiếu cứng vào package URP.
        /// </summary>
        /// <param name="camera">Camera dùng để render.</param>
        /// <param name="target">RenderTexture đích.</param>
        /// <returns>true nếu đã render xong bằng render request.</returns>
        private static bool TrySubmitRenderRequest(Camera camera, RenderTexture target)
        {
            try
            {
                Type requestType = UILayoutTypeResolverHelper.FindType(
                    "UnityEngine.Rendering.Universal.UniversalRenderPipeline+SingleCameraRequest");
                if (requestType == null) return false;

                object request = Activator.CreateInstance(requestType);
                if (request == null) return false;

                FieldInfo destinationField = requestType.GetField("destination", BindingFlags.Public | BindingFlags.Instance);
                if (destinationField != null)
                {
                    destinationField.SetValue(request, target);
                }
                else
                {
                    PropertyInfo destinationProperty = requestType.GetProperty("destination", BindingFlags.Public | BindingFlags.Instance);
                    if (destinationProperty == null || !destinationProperty.CanWrite) return false;
                    destinationProperty.SetValue(request, target);
                }

                MethodInfo supported = typeof(Camera).GetMethod("RenderRequestIsSupported")?.MakeGenericMethod(requestType);
                if (supported != null && !(bool)supported.Invoke(camera, new[] { request })) return false;

                MethodInfo submit = typeof(Camera).GetMethod("SubmitRenderRequest")?.MakeGenericMethod(requestType);
                if (submit == null) return false;

                submit.Invoke(camera, new[] { request });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UIScreenshotCapture] RenderRequest của URP không dùng được ({e.Message}) — quay về Camera.Render().");
                return false;
            }
        }

        private static byte[] ReadBack(RenderTexture renderTexture)
        {
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                RenderTexture.active = renderTexture;
                texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply(false);
                return texture.EncodeToPNG();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIScreenshotCapture] Đọc pixel thất bại: {e.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        private static void Cleanup(GameObject cameraObject, RenderTexture renderTexture)
        {
            try
            {
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);

                // Dọn nốt camera tạm còn sót nếu lần chạy trước bị ngắt giữa chừng.
                foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (camera != null && camera.gameObject.name == CameraName) Object.DestroyImmediate(camera.gameObject);
                }

                if (renderTexture != null)
                {
                    if (RenderTexture.active == renderTexture) RenderTexture.active = null;
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UIScreenshotCapture] Dọn dẹp gặp lỗi: {e.Message}");
            }
        }

        private static void RestoreScenes(SceneSetup[] previousSetup)
        {
            try
            {
                if (previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                    return;
                }

                // Trước khi chạy chỉ có scene chưa lưu / không có scene nào — để lại một scene rỗng.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UIScreenshotCapture] Không khôi phục được scene đang mở trước đó: {e.Message}");
            }
        }
    }
}
