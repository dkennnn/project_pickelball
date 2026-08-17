using System;
using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Rung (shake) kèm phóng to rồi trả về trạng thái gốc. Dùng cho ô phần thưởng League
    /// khi người chơi vừa đạt mốc, cho nút "nhận thưởng" cần gây chú ý.
    /// <para>
    /// Không tự chạy: phải gọi <see cref="Play"/> (hoặc <see cref="StartEffect"/> nếu cần callback).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ShakeAndScaleEffect : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Thời lượng rung, tính bằng giây.")]
        public float shakeDuration = 0.4f;

        [Tooltip("Biên độ rung, tính theo đơn vị local position.")]
        public float shakeStrength = 12f;

        [Tooltip("Số lần đổi hướng rung trong toàn bộ thời lượng.")]
        public int shakeVibrato = 12;

        [Header("Scale Settings")]
        [Tooltip("Tỉ lệ phóng to đỉnh so với scale gốc. 0.2 = to thêm 20%.")]
        public float scaleAmount = 0.2f;

        [Tooltip("Thời lượng một chiều của phần scale (phình ra, rồi thu về), tính bằng giây.")]
        public float scaleDuration = 0.2f;

        [Tooltip("Dùng unscaled time để hiệu ứng vẫn chạy khi game đang pause.")]
        public bool useUnscaledTime = true;

        [Tooltip("Tự chạy một lần ngay khi component được bật.")]
        public bool playOnEnable;

        private Vector3 originalScale = Vector3.one;
        private Vector3 originalLocalPosition;
        private bool captured;
        private Coroutine routine;

        private void Awake()
        {
            Capture();
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            StopEffect();
        }

        private void Capture()
        {
            if (captured) return;
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            originalLocalPosition = transform.localPosition;
            captured = true;
        }

        /// <summary>Chạy hiệu ứng một lần. Gọi được từ UnityEvent trên Inspector.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            StartEffect(null);
        }

        /// <summary>Chạy hiệu ứng một lần và gọi callback khi xong.</summary>
        /// <param name="onCompleteCallback">Callback chạy sau khi trả về trạng thái gốc; có thể null.</param>
        public void StartEffect(Action onCompleteCallback = null)
        {
            if (!isActiveAndEnabled)
            {
                onCompleteCallback?.Invoke();
                return;
            }

            Capture();
            StopEffect();
            routine = StartCoroutine(EffectRoutine(onCompleteCallback));
        }

        /// <summary>Dừng hiệu ứng và trả transform về scale/vị trí gốc.</summary>
        public void StopEffect()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (!captured) return;
            transform.localScale = originalScale;
            transform.localPosition = originalLocalPosition;
        }

        private IEnumerator EffectRoutine(Action onComplete)
        {
            Vector3 peak = originalScale * (1f + scaleAmount);
            float up = Mathf.Max(0.01f, scaleDuration);

            yield return StartCoroutine(Tweener.Value(0f, 1f, up,
                t => transform.localScale = Vector3.LerpUnclamped(originalScale, peak, t),
                null, useUnscaledTime, Tweener.EaseOutQuad));

            yield return StartCoroutine(ShakeRoutine());

            yield return StartCoroutine(Tweener.Value(0f, 1f, up,
                t => transform.localScale = Vector3.LerpUnclamped(peak, originalScale, t),
                null, useUnscaledTime, Tweener.EaseInQuad));

            transform.localScale = originalScale;
            transform.localPosition = originalLocalPosition;
            routine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ShakeRoutine()
        {
            float total = Mathf.Max(0f, shakeDuration);
            if (total <= 0f || shakeStrength <= 0f) yield break;

            int steps = Mathf.Max(1, shakeVibrato);
            float stepTime = total / steps;

            for (int i = 0; i < steps; i++)
            {
                // Biên độ giảm dần để cú rung "tắt" tự nhiên.
                float damping = 1f - (float)i / steps;
                Vector2 dir = UnityEngine.Random.insideUnitCircle * (shakeStrength * damping);
                transform.localPosition = originalLocalPosition + new Vector3(dir.x, dir.y, 0f);

                float t = 0f;
                while (t < stepTime)
                {
                    t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            transform.localPosition = originalLocalPosition;
        }
    }
}
