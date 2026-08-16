using System;
using System.Collections;
using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>
    /// Tween tối giản thay cho DOTween (tránh phụ thuộc Asset Store).
    /// Đủ dùng cho fade / scale / move / lerp Time.timeScale.
    /// </summary>
    public static class Tweener
    {
        /// <summary>Nội suy một giá trị float và gọi callback mỗi frame.</summary>
        public static IEnumerator Value(float from, float to, float duration, Action<float> onUpdate,
                                        Action onComplete = null, bool unscaled = false,
                                        Func<float, float> ease = null)
        {
            if (duration <= 0f)
            {
                onUpdate?.Invoke(to);
                onComplete?.Invoke();
                yield break;
            }

            if (ease == null) ease = EaseOutQuad;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / duration));
                onUpdate?.Invoke(Mathf.LerpUnclamped(from, to, t));
                yield return null;
            }

            onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        /// <summary>Di chuyển transform tới vị trí đích.</summary>
        public static IEnumerator Move(Transform target, Vector3 to, float duration,
                                       Action onComplete = null, bool local = false)
        {
            if (target == null) yield break;

            Vector3 from = local ? target.localPosition : target.position;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
                Vector3 p = Vector3.LerpUnclamped(from, to, t);
                if (local) target.localPosition = p;
                else target.position = p;
                yield return null;
            }

            if (target != null)
            {
                if (local) target.localPosition = to;
                else target.position = to;
            }
            onComplete?.Invoke();
        }

        /// <summary>Scale transform với ease-out-back (hiệu ứng "pop" cho UI).</summary>
        public static IEnumerator Scale(Transform target, Vector3 to, float duration, Action onComplete = null)
        {
            if (target == null) yield break;

            Vector3 from = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            if (target != null) target.localScale = to;
            onComplete?.Invoke();
        }

        public static float Linear(float t) { return t; }

        public static float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }

        public static float EaseInQuad(float t) { return t * t; }

        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
