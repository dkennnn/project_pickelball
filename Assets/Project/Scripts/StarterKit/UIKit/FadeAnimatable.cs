using System;
using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>Hiệu ứng mờ dần/hiện dần bằng alpha của <see cref="CanvasGroup"/>.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeAnimatable : Animatable
    {
        [SerializeField] private CanvasGroup targetCanvasGroup;

        private float initialAlpha = 1f;
        private bool initialized;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (initialized) return;

            if (targetCanvasGroup == null) targetCanvasGroup = GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null) targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            initialAlpha = targetCanvasGroup.alpha <= 0f ? 1f : targetCanvasGroup.alpha;
            initialized = true;
        }

        /// <inheritdoc/>
        public override void ResetAnimator()
        {
            base.ResetAnimator();
            Initialize();
            if (targetCanvasGroup != null) targetCanvasGroup.alpha = initialAlpha;
        }

        /// <inheritdoc/>
        public override void PlayIn(Action onComplete = null)
        {
            Initialize();
            if (targetCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            targetCanvasGroup.alpha = 0f;
            Run(FadeRoutine(0f, initialAlpha, onComplete), onComplete);
        }

        /// <inheritdoc/>
        public override void PlayOut(Action onComplete = null)
        {
            Initialize();
            if (targetCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            Run(FadeRoutine(targetCanvasGroup.alpha, 0f, onComplete), onComplete);
        }

        private IEnumerator FadeRoutine(float from, float to, Action onComplete)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            yield return Tweener.Value(
                from,
                to,
                duration,
                value => { if (targetCanvasGroup != null) targetCanvasGroup.alpha = value; },
                null,
                true);

            IsPlaying = false;
            onComplete?.Invoke();
        }
    }
}
