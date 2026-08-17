using TMPro;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Bật auto-size của TextMeshPro và giữ cỡ chữ trong khoảng cho phép, để chữ dài
    /// (tên người chơi, mô tả nhiệm vụ...) luôn vừa khung mà không tràn.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DynamicTextField : MonoBehaviour
    {
        /// <summary>Cỡ chữ nhỏ nhất được phép co xuống.</summary>
        [SerializeField] private float minFontSize = 12f;

        /// <summary>Cỡ chữ lớn nhất; để 0 thì lấy cỡ chữ hiện có của TMP.</summary>
        [SerializeField] private float maxFontSize = 0f;

        /// <summary>True thì ép chữ nằm trên đúng một dòng.</summary>
        [SerializeField] private bool singleLine;

        private TextMeshProUGUI label;

        private void Awake()
        {
            label = GetComponent<TextMeshProUGUI>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        /// <summary>Đặt nội dung và co lại cỡ chữ cho vừa khung.</summary>
        /// <param name="text">Nội dung cần hiển thị.</param>
        public void SetText(string text)
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (label == null) return;

            label.text = text ?? string.Empty;
            Apply();
        }

        /// <summary>Áp lại cấu hình auto-size lên TMP.</summary>
        public void Apply()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (label == null) return;

            float max = maxFontSize > 0f ? maxFontSize : Mathf.Max(minFontSize, label.fontSize);

            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Min(minFontSize, max);
            label.fontSizeMax = max;

            if (singleLine)
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
    }
}
