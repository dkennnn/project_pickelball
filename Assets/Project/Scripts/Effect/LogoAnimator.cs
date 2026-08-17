using System.Collections;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Animation logo cho màn loading/splash: logo phình ra thu vào theo nhịp, đồng thời fade
    /// nhẹ; các ngôi sao (nếu có) lần lượt "pop" ra theo thứ tự.
    /// <para>Mọi reference đều tuỳ chọn — thiếu hết thì component chỉ tác động lên chính transform này.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class LogoAnimator : MonoBehaviour
    {
        [Header("Logo")]
        [Tooltip("Transform của logo. Bỏ trống thì dùng transform của GameObject này.")]
        [SerializeField] private Transform logoTransform;

        [Tooltip("CanvasGroup để fade logo. Bỏ trống thì bỏ qua phần fade.")]
        [SerializeField] private CanvasGroup logoCanvasGroup;

        [Tooltip("Tỉ lệ phóng to đỉnh so với scale gốc. 0.08 = to thêm 8%.")]
        [SerializeField] private float scaleAmount = 0.08f;

        [Tooltip("Thời lượng một nửa nhịp (phình ra hoặc thu về), tính bằng giây.")]
        [SerializeField] private float pulseDuration = 0.6f;

        [Tooltip("Alpha thấp nhất trong nhịp fade. Đặt bằng 1 để tắt fade.")]
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.75f;

        [Header("Stars (tuỳ chọn)")]
        [Tooltip("Các ngôi sao pop lần lượt khi bắt đầu animation.")]
        [SerializeField] private List<GameObject> starImages = new List<GameObject>();

        [Tooltip("Khoảng nghỉ giữa hai ngôi sao, tính bằng giây.")]
        [SerializeField] private float delayBetweenStars = 0.12f;

        [Tooltip("Thời lượng pop của mỗi ngôi sao, tính bằng giây.")]
        [SerializeField] private float starScaleDuration = 0.25f;

        [Header("Behaviour")]
        [Tooltip("Tự chạy ngay khi component được bật.")]
        [SerializeField] private bool playOnEnable = true;

        private Vector3 originalScale = Vector3.one;
        private float originalAlpha = 1f;
        private bool captured;
        private Coroutine pulseRoutine;
        private Coroutine starsRoutine;

        private void Awake()
        {
            if (logoTransform == null) logoTransform = transform;
            if (logoCanvasGroup == null) logoCanvasGroup = GetComponent<CanvasGroup>();
            Capture();
        }

        private void OnEnable()
        {
            if (playOnEnable) PlayAnimation();
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void Capture()
        {
            if (captured) return;
            if (logoTransform == null) logoTransform = transform;

            originalScale = logoTransform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            if (logoCanvasGroup != null) originalAlpha = logoCanvasGroup.alpha;
            captured = true;
        }

        /// <summary>Chạy animation logo từ đầu.</summary>
        [ContextMenu("Play")]
        public void PlayAnimation()
        {
            if (!isActiveAndEnabled) return;

            Capture();
            StopAnimation();

            pulseRoutine = StartCoroutine(PulseRoutine());
            if (starImages != null && starImages.Count > 0) starsRoutine = StartCoroutine(PopStarsRoutine());
        }

        /// <summary>Dừng animation và trả logo về scale/alpha gốc.</summary>
        public void StopAnimation()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (starsRoutine != null)
            {
                StopCoroutine(starsRoutine);
                starsRoutine = null;
            }

            if (!captured) return;
            if (logoTransform != null) logoTransform.localScale = originalScale;
            if (logoCanvasGroup != null) logoCanvasGroup.alpha = originalAlpha;
        }

        private IEnumerator PulseRoutine()
        {
            float half = Mathf.Max(0.05f, pulseDuration);
            Vector3 peak = originalScale * (1f + scaleAmount);

            while (true)
            {
                yield return StartCoroutine(Blend(originalScale, peak, originalAlpha, minAlpha, half));
                yield return StartCoroutine(Blend(peak, originalScale, minAlpha, originalAlpha, half));
            }
        }

        private IEnumerator Blend(Vector3 fromScale, Vector3 toScale, float fromAlpha, float toAlpha, float time)
        {
            return Tweener.Value(0f, 1f, time, t =>
            {
                if (logoTransform != null) logoTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                if (logoCanvasGroup != null) logoCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            }, null, true, Tweener.EaseInOutQuad);
        }

        private IEnumerator PopStarsRoutine()
        {
            // Ẩn hết trước, rồi bung lần lượt.
            for (int i = 0; i < starImages.Count; i++)
            {
                GameObject star = starImages[i];
                if (star == null) continue;
                star.transform.localScale = Vector3.zero;
                star.SetActive(true);
            }

            for (int i = 0; i < starImages.Count; i++)
            {
                GameObject star = starImages[i];
                if (star == null) continue;

                yield return StartCoroutine(Tweener.Scale(star.transform, Vector3.one,
                                                          Mathf.Max(0.05f, starScaleDuration)));

                if (delayBetweenStars > 0f) yield return new WaitForSecondsRealtime(delayBetweenStars);
            }

            starsRoutine = null;
        }
    }
}
