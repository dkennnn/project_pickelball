using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Làm xám cả một cụm UI khi nó bị khoá/không dùng được: đổi màu mọi
    /// <see cref="Graphic"/> con sang thang xám và nhớ màu gốc để khôi phục.
    /// Node có <see cref="IgnoreGreyscaleObject"/> (kể cả ở cha) sẽ được bỏ qua.
    /// </summary>
    [DisallowMultipleComponent]
    public class GreyscaleUIHierarchy : MonoBehaviour
    {
        /// <summary>Mức pha trộn về xám: 0 = giữ nguyên màu, 1 = xám hoàn toàn.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float greyAmount = 1f;

        /// <summary>Hệ số nhân độ sáng khi bị xám (nhỏ hơn 1 sẽ tối đi).</summary>
        [Range(0f, 1f)]
        [SerializeField] private float brightness = 0.75f;

        /// <summary>True khi cụm đang bị làm xám.</summary>
        public bool IsGreyscale { get; private set; }

        private readonly List<Graphic> targets = new List<Graphic>();
        private readonly List<Color> originalColors = new List<Color>();
        private bool cached;

        private void OnEnable()
        {
            if (IsGreyscale) Apply(true);
        }

        /// <summary>
        /// Bật/tắt hiệu ứng xám cho cả cụm.
        /// </summary>
        /// <param name="greyscale">True để làm xám, false để trả về màu gốc.</param>
        public void SetGreyscale(bool greyscale)
        {
            IsGreyscale = greyscale;
            Apply(greyscale);
        }

        /// <summary>Đọc lại danh sách Graphic con và màu gốc (gọi sau khi thay đổi cây con).</summary>
        public void RefreshTargets()
        {
            cached = false;
            CacheTargets();
            Apply(IsGreyscale);
        }

        private void Apply(bool greyscale)
        {
            CacheTargets();

            for (int i = 0; i < targets.Count; i++)
            {
                Graphic graphic = targets[i];
                if (graphic == null) continue;

                Color original = originalColors[i];
                if (!greyscale)
                {
                    graphic.color = original;
                    continue;
                }

                float luminance = original.r * 0.299f + original.g * 0.587f + original.b * 0.114f;
                luminance *= brightness;

                Color grey = new Color(luminance, luminance, luminance, original.a);
                graphic.color = Color.Lerp(original, grey, Mathf.Clamp01(greyAmount));
            }
        }

        private void CacheTargets()
        {
            if (cached) return;
            cached = true;

            targets.Clear();
            originalColors.Clear();

            List<Graphic> found = new List<Graphic>();
            GetComponentsInChildren(true, found);

            for (int i = 0; i < found.Count; i++)
            {
                Graphic graphic = found[i];
                if (graphic == null) continue;
                if (IsIgnored(graphic.transform)) continue;

                targets.Add(graphic);
                originalColors.Add(graphic.color);
            }
        }

        /// <summary>True nếu node hoặc một cha của nó (tới tận cụm này) mang <see cref="IgnoreGreyscaleObject"/>.</summary>
        private bool IsIgnored(Transform node)
        {
            Transform current = node;
            while (current != null)
            {
                if (current.GetComponent<IgnoreGreyscaleObject>() != null) return true;
                if (current == transform) return false;
                current = current.parent;
            }
            return false;
        }
    }
}
