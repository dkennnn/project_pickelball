using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Dựng prefab lớp phủ hướng dẫn <c>Assets/Project/Prefabs/UI/Screens/TutorialUI.prefab</c>.
    ///
    /// <para>Bản gốc KHÔNG trích được layout cho màn này (nó không có entry trong
    /// <see cref="ScreenType"/> nên <c>UILayoutImporter</c> bỏ qua), vì vậy prefab được dựng bằng
    /// tay ở đây với đúng bộ widget mà <see cref="TutorialUI"/> cần: vùng tối, khung sáng,
    /// mũi tên, bảng thông điệp và nút Skip.</para>
    ///
    /// <para><b>Idempotent</b>: chạy lại sẽ ghi đè prefab cũ và giữ nguyên GUID asset, nên mọi
    /// tham chiếu trong scene / <c>TutorialManager</c> vẫn còn nguyên.</para>
    ///
    /// <para>Sorting order 200 đặt lớp phủ NẰM TRÊN mọi màn hình thường (0) và popup (100) —
    /// xem <c>GreyboxSceneBuilder</c>.</para>
    /// </summary>
    public static class TutorialUIBuilder
    {
        // ------------------------------------------------------------------ Đường dẫn

        private const string ScreensFolder = "Assets/Project/Prefabs/UI/Screens";
        private const string PrefabPath = ScreensFolder + "/TutorialUI.prefab";

        // ------------------------------------------------------------------ Thông số canvas

        /// <summary>Độ phân giải tham chiếu dọc, trùng với mọi màn hình khác của project.</summary>
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        /// <summary>Lớp phủ hướng dẫn luôn nằm trên cùng.</summary>
        private const int TutorialSortingOrder = 200;

        // ------------------------------------------------------------------ Bảng màu

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color HighlightColor = new Color(1f, 0.92f, 0.23f, 0.35f);
        private static readonly Color PointerColor = new Color(1f, 0.92f, 0.23f, 1f);
        private static readonly Color MessagePanelColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        private static readonly Color SkipButtonColor = new Color(0.82f, 0.82f, 0.86f, 1f);

        // ------------------------------------------------------------------ Entry point

        /// <summary>Menu item dựng lại prefab TutorialUI.</summary>
        [MenuItem("Pickleball/UI/Build Tutorial UI Prefab")]
        public static void BuildTutorialUIPrefabMenu()
        {
            GameObject prefab = BuildTutorialUIPrefab();

            Debug.Log(prefab != null
                ? "[TutorialUIBuilder] Đã dựng " + PrefabPath
                : "[TutorialUIBuilder] KHÔNG dựng được " + PrefabPath);
        }

        /// <summary>
        /// Dựng (hoặc dựng lại) prefab TutorialUI và trả về asset vừa ghi.
        /// </summary>
        /// <returns>Prefab asset, hoặc <c>null</c> nếu ghi thất bại.</returns>
        public static GameObject BuildTutorialUIPrefab()
        {
            EnsureFolder(ScreensFolder);

            TMP_FontAsset font = FindDefaultFont();
            if (font == null)
            {
                Debug.LogWarning("[TutorialUIBuilder] Không tìm được TMP_Settings.defaultFontAsset — " +
                                 "chữ trong TutorialUI sẽ KHÔNG hiện. Chạy Window/TextMeshPro/Import TMP Essential Resources.");
            }

            GameObject root = BuildHierarchy(font);

            bool success;
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
            Object.DestroyImmediate(root);

            if (!success) Debug.LogWarning("[TutorialUIBuilder] SaveAsPrefabAsset thất bại: " + PrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        // ------------------------------------------------------------------ Cây node

        /// <summary>Dựng toàn bộ cây node tạm trong scene rồi trả về gốc để đem đi lưu prefab.</summary>
        /// <param name="font">Font TMP dùng cho mọi ô chữ; có thể null.</param>
        private static GameObject BuildHierarchy(TMP_FontAsset font)
        {
            // --- Gốc: Canvas riêng để lớp phủ độc lập với mọi màn hình khác ---------------
            GameObject rootObject = new GameObject("TutorialUI", typeof(RectTransform));
            RectTransform rootRect = (RectTransform)rootObject.transform;
            Stretch(rootRect);

            Canvas canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = TutorialSortingOrder;

            CanvasScaler scaler = rootObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            rootObject.AddComponent<GraphicRaycaster>();

            TutorialUI tutorialUI = rootObject.AddComponent<TutorialUI>();

            // --- Vùng tối phủ toàn màn ------------------------------------------------------
            RectTransform dimRect = CreateChild("DimOverlay", rootRect);
            Stretch(dimRect);
            Image dimOverlay = dimRect.gameObject.AddComponent<Image>();
            dimOverlay.color = DimColor;
            dimOverlay.raycastTarget = true;

            // --- Khung sáng bám mục tiêu ----------------------------------------------------
            RectTransform highlightFrame = CreateChild("HighlightFrame", rootRect);
            Center(highlightFrame, new Vector2(300f, 300f));
            Image highlightImage = highlightFrame.gameObject.AddComponent<Image>();
            highlightImage.color = HighlightColor;
            highlightImage.raycastTarget = false;

            // --- Mũi tên chỉ ----------------------------------------------------------------
            RectTransform pointerArrow = CreateChild("PointerArrow", rootRect);
            Center(pointerArrow, new Vector2(80f, 80f));
            Image pointerImage = pointerArrow.gameObject.AddComponent<Image>();
            pointerImage.color = PointerColor;
            pointerImage.raycastTarget = false;

            // --- Bảng thông điệp (neo đáy) --------------------------------------------------
            RectTransform messagePanel = CreateChild("MessagePanel", rootRect);
            messagePanel.anchorMin = new Vector2(0.5f, 0f);
            messagePanel.anchorMax = new Vector2(0.5f, 0f);
            messagePanel.pivot = new Vector2(0.5f, 0f);
            messagePanel.sizeDelta = new Vector2(900f, 260f);
            messagePanel.anchoredPosition = new Vector2(0f, 180f);
            Image messagePanelImage = messagePanel.gameObject.AddComponent<Image>();
            messagePanelImage.color = MessagePanelColor;
            messagePanelImage.raycastTarget = false;

            RectTransform messageTextRect = CreateChild("MessageText", messagePanel);
            Stretch(messageTextRect, 32f);
            TextMeshProUGUI messageText = CreateText(messageTextRect, font, string.Empty, Color.white);
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 24f;
            messageText.fontSizeMax = 48f;
            messageText.fontSize = 36f;
            messageText.alignment = TextAlignmentOptions.Center;

            // --- Nút Skip (neo phải trên) ---------------------------------------------------
            RectTransform skipRect = CreateChild("SkipButton", rootRect);
            skipRect.anchorMin = Vector2.one;
            skipRect.anchorMax = Vector2.one;
            skipRect.pivot = Vector2.one;
            skipRect.sizeDelta = new Vector2(200f, 80f);
            skipRect.anchoredPosition = new Vector2(-40f, -40f);

            Image skipImage = skipRect.gameObject.AddComponent<Image>();
            skipImage.color = SkipButtonColor;
            skipImage.raycastTarget = true;

            Button skipButton = skipRect.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipImage;

            RectTransform skipTextRect = CreateChild("SkipText", skipRect);
            Stretch(skipTextRect, 8f);
            TextMeshProUGUI skipText = CreateText(skipTextRect, font, "Skip", new Color(0.1f, 0.1f, 0.12f, 1f));
            skipText.enableAutoSizing = true;
            skipText.fontSizeMin = 20f;
            skipText.fontSizeMax = 40f;
            skipText.fontSize = 32f;
            skipText.alignment = TextAlignmentOptions.Center;

            // --- Trạng thái mặc định: chỉ nút Skip còn bật ---------------------------------
            dimRect.gameObject.SetActive(false);
            highlightFrame.gameObject.SetActive(false);
            pointerArrow.gameObject.SetActive(false);
            messagePanel.gameObject.SetActive(false);

            // --- Gán [SerializeField] private của TutorialUI --------------------------------
            BindSerializedFields(tutorialUI, canvas, dimOverlay, highlightFrame, pointerArrow,
                messagePanel.gameObject, messageText, skipButton);

            return rootObject;
        }

        /// <summary>
        /// Ghi toàn bộ tham chiếu private của <see cref="TutorialUI"/> (và trường
        /// <c>canvas</c> thừa kế từ <see cref="StarterKit.UIKit.UIScreenBase"/>) trong MỘT
        /// <see cref="SerializedObject"/> rồi Apply một lần — mở nhiều SerializedObject liên tiếp
        /// cho cùng một target khiến lần Apply sau ghi đè lần trước.
        /// </summary>
        private static void BindSerializedFields(TutorialUI tutorialUI, Canvas canvas, Image dimOverlay,
            RectTransform highlightFrame, RectTransform pointerArrow, GameObject messagePanel,
            TextMeshProUGUI messageText, Button skipButton)
        {
            SerializedObject serialized = new SerializedObject(tutorialUI);

            SetReference(serialized, "canvas", canvas);
            SetReference(serialized, "dimOverlay", dimOverlay);
            SetReference(serialized, "highlightFrame", highlightFrame);
            SetReference(serialized, "pointerArrow", pointerArrow);
            SetReference(serialized, "messagePanel", messagePanel);
            SetReference(serialized, "messageText", messageText);
            SetReference(serialized, "skipButton", skipButton);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Helper: node

        /// <summary>Tạo một node UI rỗng (chỉ RectTransform) làm con của <paramref name="parent"/>.</summary>
        private static RectTransform CreateChild(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>Kéo giãn node phủ kín node cha với khoảng đệm cho trước.</summary>
        /// <param name="rect">Node cần kéo giãn.</param>
        /// <param name="padding">Khoảng đệm bốn phía, tính bằng pixel.</param>
        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Neo node vào giữa màn hình với kích thước cố định.</summary>
        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Gắn một ô chữ TMP vào node cho trước.</summary>
        /// <param name="rect">Node đích.</param>
        /// <param name="font">Font TMP; bỏ qua nếu null.</param>
        /// <param name="content">Nội dung ban đầu.</param>
        /// <param name="color">Màu chữ.</param>
        private static TextMeshProUGUI CreateText(RectTransform rect, TMP_FontAsset font, string content, Color color)
        {
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;

            text.text = content;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        // ------------------------------------------------------------------ Helper: asset

        /// <summary>Font TMP mặc định của project; null khi chưa import TMP Essentials.</summary>
        private static TMP_FontAsset FindDefaultFont()
        {
            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Ghi một tham chiếu vào SerializedObject đang mở (chưa Apply).</summary>
        private static void SetReference(SerializedObject serialized, string propertyPath, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning("[TutorialUIBuilder] Không tìm thấy field '" + propertyPath + "' trên "
                                 + serialized.targetObject.GetType().Name + ".");
                return;
            }

            property.objectReferenceValue = value;
        }

        /// <summary>Tạo thư mục asset (đệ quy) nếu chưa có.</summary>
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
