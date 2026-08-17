using System;
using System.Collections;
using StarterKit.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Fade một object vào/ra. Tự chọn kênh alpha theo thứ tự ưu tiên:
    /// <see cref="CanvasGroup"/> → <see cref="Graphic"/> → <see cref="SpriteRenderer"/>.
    /// <para>Thiếu cả ba thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleObjectFadeAnimator : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("Thời lượng fade vào, tính bằng giây.")]
        [SerializeField] private float fadeInDuration = 0.25f;

        [Tooltip("Thời lượng fade ra, tính bằng giây.")]
        [SerializeField] private float fadeOutDuration = 0.25f;

        [Tooltip("Dùng unscaled time để fade vẫn chạy khi game đang pause.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("References (tuỳ chọn)")]
        [Tooltip("CanvasGroup dùng để fade. Bỏ trống thì tự tìm trên GameObject này.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Graphic (Image/Text) dùng để fade khi không có CanvasGroup.")]
        [SerializeField] private Graphic graphic;

        [Tooltip("SpriteRenderer dùng để fade khi không có CanvasGroup lẫn Graphic.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Behaviour")]
        [Tooltip("Đặt alpha về 0 ngay khi component được bật.")]
        [SerializeField] private bool hiddenOnEnable;

        [Tooltip("Tự fade vào ngay khi component được bật.")]
        [SerializeField] private bool fadeInOnEnable;

        private Coroutine fadeRoutine;

        /// <summary>True khi đang chạy một lượt fade.</summary>
        public bool IsFading => fadeRoutine != null;

        private void Awake()
        {
            ResolveTargets();
        }

        private void OnEnable()
        {
            if (hiddenOnEnable) SetAlpha(0f);
            if (fadeInOnEnable) FadeIn();
        }

        private void OnDisable()
        {
            StopFade();
        }

        private void ResolveTargets()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (graphic == null) graphic = GetComponent<Graphic>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Có kênh alpha nào để fade không.</summary>
        private bool HasTarget => canvasGroup != null || graphic != null || spriteRenderer != null;

        /// <summary>Fade từ alpha hiện tại lên 1.</summary>
        public void FadeIn()
        {
            FadeIn(null);
        }

        /// <summary>Fade lên 1 và gọi callback khi xong.</summary>
        /// <param name="onComplete">Callback sau khi fade xong; có thể null.</param>
        public void FadeIn(Action onComplete)
        {
            StartFade(1f, fadeInDuration, onComplete);
        }

        /// <summary>Fade từ alpha hiện tại xuống 0.</summary>
        public void FadeOut()
        {
            FadeOut(null);
        }

        /// <summary>Fade xuống 0 và gọi callback khi xong.</summary>
        /// <param name="onComplete">Callback sau khi fade xong; có thể null.</param>
        public void FadeOut(Action onComplete)
        {
            StartFade(0f, fadeOutDuration, onComplete);
        }

        /// <summary>Hiện ngay lập tức, không tween.</summary>
        public void FadeInInstant()
        {
            StopFade();
            SetAlpha(1f);
        }

        /// <summary>Ẩn ngay lập tức, không tween.</summary>
        public void FadeOutInstant()
        {
            StopFade();
            SetAlpha(0f);
        }

        /// <summary>Dừng lượt fade đang chạy (giữ nguyên alpha hiện tại).</summary>
        public void StopFade()
        {
            if (fadeRoutine == null) return;
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        private void StartFade(float targetAlpha, float duration, Action onComplete)
        {
            ResolveTargets();

            if (!HasTarget)
            {
                onComplete?.Invoke();
                return;
            }

            if (!isActiveAndEnabled)
            {
                // Không chạy coroutine được khi object đang tắt — nhảy thẳng tới đích.
                SetAlpha(targetAlpha);
                onComplete?.Invoke();
                return;
            }

            StopFade();
            fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            float from = GetCurrentAlpha();

            yield return StartCoroutine(Tweener.Value(from, targetAlpha, Mathf.Max(0f, duration),
                SetAlpha, null, useUnscaledTime, Tweener.EaseInOutQuad));

            SetAlpha(targetAlpha);
            fadeRoutine = null;
            onComplete?.Invoke();
        }

        /// <summary>Alpha hiện tại của kênh đang dùng; 1 nếu không có kênh nào.</summary>
        private float GetCurrentAlpha()
        {
            if (canvasGroup != null) return canvasGroup.alpha;
            if (graphic != null) return graphic.color.a;
            if (spriteRenderer != null) return spriteRenderer.color.a;
            return 1f;
        }

        /// <summary>Đặt alpha lên mọi kênh khả dụng.</summary>
        /// <param name="alpha">Giá trị alpha trong [0,1].</param>
        public void SetAlpha(float alpha)
        {
            float a = Mathf.Clamp01(alpha);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = a;
                return;
            }

            if (graphic != null)
            {
                Color c = graphic.color;
                c.a = a;
                graphic.color = c;
                return;
            }

            if (spriteRenderer == null) return;
            Color sc = spriteRenderer.color;
            sc.a = a;
            spriteRenderer.color = sc;
        }
    }
}
