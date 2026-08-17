using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Cuộn UV của một hoặc nhiều lớp ảnh để tạo cảm giác chiều sâu (parallax) cho background.
    /// <para>
    /// Với <see cref="Renderer"/> thì cuộn <c>material.mainTextureOffset</c>.
    /// Với <see cref="RawImage"/> UGUI, offset texture nằm ở <c>uvRect.position</c> — component
    /// cuộn trường này để không phải nhân bản material của Canvas.
    /// </para>
    /// <para>
    /// Bỏ trống <see cref="layers"/> thì component tự dùng RawImage/Renderer ngay trên GameObject này.
    /// Không tìm thấy gì thì im lặng không làm gì.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ParallaxUVScroller : MonoBehaviour
    {
        /// <summary>Một lớp parallax: ảnh cần cuộn kèm hệ số nhân tốc độ riêng.</summary>
        [Serializable]
        public class ParallaxLayer
        {
            [Tooltip("Ảnh UGUI của lớp này. Ưu tiên hơn targetRenderer nếu cả hai cùng được gán.")]
            public RawImage rawImage;

            [Tooltip("Renderer 3D của lớp này (dùng khi lớp nằm ngoài Canvas).")]
            public Renderer targetRenderer;

            [Tooltip("Hệ số nhân tốc độ. Lớp càng xa nên để hệ số càng nhỏ.")]
            public Vector2 speedMultiplier = Vector2.one;

            [NonSerialized] internal Vector2 accumulated;
        }

        [Tooltip("Tốc độ cuộn gốc, đơn vị UV mỗi giây.")]
        public Vector2 speed = new Vector2(0.03f, 0f);

        [Tooltip("Các lớp parallax. Bỏ trống thì dùng RawImage/Renderer ngay trên GameObject này.")]
        public List<ParallaxLayer> layers = new List<ParallaxLayer>();

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool playOnEnable = true;

        [Tooltip("Dùng unscaled time để nền vẫn trôi khi game đang pause.")]
        public bool useUnscaledTime = true;

        private bool isScrolling;
        private readonly List<ParallaxLayer> activeLayers = new List<ParallaxLayer>();

        private void Awake()
        {
            BuildActiveLayers();
        }

        private void OnEnable()
        {
            if (playOnEnable) StartScrolling();
        }

        private void OnDisable()
        {
            isScrolling = false;
        }

        private void BuildActiveLayers()
        {
            activeLayers.Clear();

            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    ParallaxLayer layer = layers[i];
                    if (layer == null) continue;
                    if (layer.rawImage == null && layer.targetRenderer == null) continue;
                    layer.accumulated = Vector2.zero;
                    activeLayers.Add(layer);
                }
            }

            if (activeLayers.Count > 0) return;

            // Không cấu hình lớp nào — tự bắt ảnh trên chính GameObject này.
            RawImage selfRaw = GetComponent<RawImage>();
            Renderer selfRenderer = selfRaw == null ? GetComponent<Renderer>() : null;
            if (selfRaw == null && selfRenderer == null) return;

            activeLayers.Add(new ParallaxLayer
            {
                rawImage = selfRaw,
                targetRenderer = selfRenderer,
                speedMultiplier = Vector2.one
            });
        }

        /// <summary>Bắt đầu cuộn.</summary>
        public void StartScrolling()
        {
            if (activeLayers.Count == 0) BuildActiveLayers();
            isScrolling = activeLayers.Count > 0;
        }

        /// <summary>Dừng cuộn (giữ nguyên offset hiện tại).</summary>
        public void StopScrolling()
        {
            isScrolling = false;
        }

        /// <summary>Đưa toàn bộ lớp về offset 0.</summary>
        public void ResetScroll()
        {
            for (int i = 0; i < activeLayers.Count; i++)
            {
                ParallaxLayer layer = activeLayers[i];
                layer.accumulated = Vector2.zero;
                ApplyOffset(layer, Vector2.zero);
            }
        }

        private void Update()
        {
            if (!isScrolling) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            for (int i = 0; i < activeLayers.Count; i++)
            {
                ParallaxLayer layer = activeLayers[i];
                if (layer == null) continue;

                Vector2 delta = new Vector2(speed.x * layer.speedMultiplier.x,
                                            speed.y * layer.speedMultiplier.y) * dt;

                layer.accumulated += delta;
                // Giữ offset trong [0,1) để không mất độ chính xác float sau thời gian dài.
                layer.accumulated = new Vector2(Mathf.Repeat(layer.accumulated.x, 1f),
                                                Mathf.Repeat(layer.accumulated.y, 1f));

                ApplyOffset(layer, layer.accumulated);
            }
        }

        private static void ApplyOffset(ParallaxLayer layer, Vector2 offset)
        {
            if (layer.rawImage != null)
            {
                Rect r = layer.rawImage.uvRect;
                r.x = offset.x;
                r.y = offset.y;
                layer.rawImage.uvRect = r;
                return;
            }

            if (layer.targetRenderer == null) return;

            // material (không phải sharedMaterial) để chỉ ảnh hưởng instance của renderer này.
            Material mat = Application.isPlaying ? layer.targetRenderer.material : layer.targetRenderer.sharedMaterial;
            if (mat == null) return;
            mat.mainTextureOffset = offset;
        }
    }
}
