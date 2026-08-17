using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Phát hiệu ứng UI ngắn (nổ coin, nổ gem, tia nâng cấp...) tại một điểm trên màn hình.
    /// Prefab chưa được gán thì hàm phát chỉ đơn giản không làm gì.
    /// </summary>
    public class UIVFXHandler : Singleton<UIVFXHandler>
    {
        /// <summary>Ánh xạ một loại hiệu ứng sang prefab tương ứng.</summary>
        [Serializable]
        public class VFXEntry
        {
            /// <summary>Loại hiệu ứng.</summary>
            public UIVFXType type;

            /// <summary>Prefab hiệu ứng; có thể để trống.</summary>
            public GameObject prefab;

            /// <summary>Thời gian sống trước khi tự huỷ, tính bằng giây.</summary>
            public float lifetime = 2f;
        }

        /// <summary>Bảng ánh xạ loại hiệu ứng sang prefab.</summary>
        [SerializeField] private List<VFXEntry> effects = new List<VFXEntry>();

        /// <summary>Node cha để sinh hiệu ứng; để trống thì dùng chính transform này.</summary>
        [SerializeField] private RectTransform container;

        /// <summary>
        /// Phát một hiệu ứng UI tại toạ độ màn hình.
        /// Không làm gì nếu loại hiệu ứng chưa được gán prefab.
        /// </summary>
        /// <param name="type">Loại hiệu ứng cần phát.</param>
        /// <param name="screenPosition">Toạ độ trên màn hình (pixel) để đặt hiệu ứng.</param>
        public void Play(UIVFXType type, Vector3 screenPosition)
        {
            VFXEntry entry = Find(type);
            if (entry == null || entry.prefab == null) return;

            Transform parent = container != null ? (Transform)container : transform;

            GameObject instance = Instantiate(entry.prefab, parent);
            RectTransform rect = instance.transform as RectTransform;

            if (rect != null && parent is RectTransform parentRect)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect, screenPosition, cam, out Vector2 local))
                {
                    rect.anchoredPosition = local;
                }
            }
            else
            {
                instance.transform.position = screenPosition;
            }

            float lifetime = entry.lifetime > 0f ? entry.lifetime : 2f;
            Destroy(instance, lifetime);
        }

        /// <summary>Phát hiệu ứng tại tâm của một RectTransform.</summary>
        /// <param name="type">Loại hiệu ứng cần phát.</param>
        /// <param name="target">Node UI làm mốc vị trí; bỏ qua nếu null.</param>
        public void PlayAt(UIVFXType type, RectTransform target)
        {
            if (target == null) return;
            Play(type, RectTransformUtility.WorldToScreenPoint(null, target.position));
        }

        private VFXEntry Find(UIVFXType type)
        {
            if (effects == null) return null;

            for (int i = 0; i < effects.Count; i++)
            {
                VFXEntry entry = effects[i];
                if (entry != null && entry.type == type) return entry;
            }
            return null;
        }
    }
}
