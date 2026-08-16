using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Decal + particle đánh dấu điểm bóng vừa chạm đất, tự mờ dần rồi tự huỷ.
    /// <para>
    /// Prefab được <see cref="BallController"/> spawn tại mỗi lần nảy. Đây là object thuần
    /// trình diễn: nó KHÔNG ảnh hưởng gameplay và không được phép gọi ngược vào rule engine.
    /// </para>
    /// </summary>
    public class BallBounceVisual : MonoBehaviour
    {
        [Tooltip("Tổng thời gian sống (giây) trước khi tự huỷ.")]
        public float LifeTime = 1.2f;

        [Tooltip("Số giây cuối vòng đời dùng để mờ dần. Phải nhỏ hơn LifeTime.")]
        public float FadeTime = 0.5f;

        [Tooltip("Decal vệt nảy trên mặt sân. Có thể bỏ trống nếu chỉ dùng particle.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Particle bụi/nước bắn tại điểm nảy. Có thể bỏ trống.")]
        [SerializeField] private ParticleSystem bounceParticle;

        [Tooltip("Nhấc decal khỏi mặt sân vài mm để tránh z-fighting.")]
        [SerializeField] private float groundOffset = 0.01f;

        private Color initialColor = Color.white;
        private float elapsed;
        private bool isPlaying;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bounceParticle == null) bounceParticle = GetComponentInChildren<ParticleSystem>();
            if (spriteRenderer != null) initialColor = spriteRenderer.color;
        }

        private void Start()
        {
            // Nếu prefab được spawn mà không ai gọi Play() thì vẫn tự chạy tại chỗ.
            if (!isPlaying) Play(transform.position, transform.localScale.x);
        }

        /// <summary>
        /// Đặt decal tại điểm nảy và bắt đầu vòng đời mờ dần.
        /// </summary>
        /// <param name="position">Điểm chạm đất trong world space.</param>
        /// <param name="scale">Hệ số kích thước decal — thường tỉ lệ với tốc độ bóng lúc chạm đất.</param>
        public void Play(Vector3 position, float scale)
        {
            isPlaying = true;
            elapsed = 0f;

            transform.position = position + Vector3.up * groundOffset;
            // Decal nằm áp mặt sân (mặt phẳng XZ).
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            if (spriteRenderer != null) spriteRenderer.color = initialColor;
            if (bounceParticle != null) bounceParticle.Play(true);

            DelayedAction.Run(LifeTime, () =>
            {
                if (this != null) Destroy(gameObject);
            });
        }

        private void Update()
        {
            if (!isPlaying || spriteRenderer == null) return;

            elapsed += Time.deltaTime;

            float fadeStart = Mathf.Max(0f, LifeTime - FadeTime);
            if (elapsed < fadeStart || FadeTime <= 0f) return;

            float t = Mathf.Clamp01((elapsed - fadeStart) / FadeTime);
            Color c = initialColor;
            c.a = initialColor.a * (1f - t);
            spriteRenderer.color = c;
        }
    }
}
