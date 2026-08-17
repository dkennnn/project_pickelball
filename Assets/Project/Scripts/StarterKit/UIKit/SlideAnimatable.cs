using System;
using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>Hướng mà node trượt vào màn hình.</summary>
    public enum SlideFrom
    {
        /// <summary>Trượt vào từ mép trái.</summary>
        Left = 0,

        /// <summary>Trượt vào từ mép phải.</summary>
        Right = 1,

        /// <summary>Trượt vào từ mép trên.</summary>
        Top = 2,

        /// <summary>Trượt vào từ mép dưới.</summary>
        Bottom = 3
    }

    /// <summary>
    /// Hiệu ứng trượt: node bay vào từ một trong bốn mép màn hình khi hiện,
    /// và bay ngược ra đúng mép đó khi ẩn.
    /// </summary>
    public class SlideAnimatable : Animatable
    {
        /// <summary>Mép màn hình mà node trượt vào.</summary>
        public SlideFrom direction = SlideFrom.Bottom;

        /// <summary>Nhân thêm vào quãng đường trượt (1 = đúng một màn hình).</summary>
        public float distanceMultiplier = 1f;

        [SerializeField] private RectTransform targetTransform;

        private Vector2 initialPosition;
        private bool initialized;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (initialized) return;

            if (targetTransform == null) targetTransform = transform as RectTransform;
            if (targetTransform != null) initialPosition = targetTransform.anchoredPosition;
            initialized = true;
        }

        /// <inheritdoc/>
        public override void ResetAnimator()
        {
            base.ResetAnimator();
            Initialize();
            if (targetTransform != null) targetTransform.anchoredPosition = initialPosition;
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

            Vector2 from = initialPosition + GetOffset();
            targetTransform.anchoredPosition = from;
            Run(SlideRoutine(from, initialPosition, onComplete), onComplete);
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

            Vector2 to = initialPosition + GetOffset();
            Run(SlideRoutine(targetTransform.anchoredPosition, to, onComplete), onComplete);
        }

        /// <summary>Độ lệch từ vị trí gốc ra ngoài màn hình theo <see cref="direction"/>.</summary>
        private Vector2 GetOffset()
        {
            float width = Screen.width;
            float height = Screen.height;

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null)
            {
                width = canvasRect.rect.width;
                height = canvasRect.rect.height;
            }

            float m = Mathf.Max(0.01f, distanceMultiplier);
            switch (direction)
            {
                case SlideFrom.Left: return new Vector2(-width * m, 0f);
                case SlideFrom.Right: return new Vector2(width * m, 0f);
                case SlideFrom.Top: return new Vector2(0f, height * m);
                default: return new Vector2(0f, -height * m);
            }
        }

        private IEnumerator SlideRoutine(Vector2 from, Vector2 to, Action onComplete)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            yield return Tweener.Value(
                0f,
                1f,
                duration,
                t =>
                {
                    if (targetTransform != null) targetTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                },
                null,
                true);

            IsPlaying = false;
            onComplete?.Invoke();
        }
    }
}
