using System;
using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>Hiệu ứng "pop": phóng to từ 0 khi hiện, thu về 0 khi ẩn (dùng <see cref="Tweener.Scale"/>).</summary>
    public class ScaleAnimatable : Animatable
    {
        [SerializeField] private Transform targetTransform;

        private Vector3 initialScale = Vector3.one;
        private bool initialized;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (initialized) return;

            if (targetTransform == null) targetTransform = transform;
            initialScale = targetTransform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
            initialized = true;
        }

        /// <inheritdoc/>
        public override void ResetAnimator()
        {
            base.ResetAnimator();
            Initialize();
            if (targetTransform != null) targetTransform.localScale = initialScale;
        }

        /// <inheritdoc/>
        public override void PlayIn(Action onComplete = null)
        {
            Initialize();
            if (targetTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            targetTransform.localScale = Vector3.zero;
            Run(ScaleRoutine(initialScale, onComplete), onComplete);
        }

        /// <inheritdoc/>
        public override void PlayOut(Action onComplete = null)
        {
            Initialize();
            if (targetTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            Run(ScaleRoutine(Vector3.zero, onComplete), onComplete);
        }

        private IEnumerator ScaleRoutine(Vector3 to, Action onComplete)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            yield return Tweener.Scale(targetTransform, to, duration);

            IsPlaying = false;
            onComplete?.Invoke();
        }
    }
}
