using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một chấm đánh dấu lượt đánh thành công trong tutorial (prefab <c>TutorialBallCellView</c>).
    /// Chấm sáng lên khi lượt đó đã hoàn thành.
    /// </summary>
    public class TutorialSuccessBallCellView : MonoBehaviour
    {
        /// <summary>Lớp phủ dấu tick (node <c>SuccessTick</c>).</summary>
        [SerializeField] private CanvasGroup successTick;

        /// <summary>Thời gian mờ dần khi bật/tắt dấu tick, tính bằng giây.</summary>
        [SerializeField] private float fadeDuration = 0.2f;

        /// <summary>True khi lượt đánh này đã hoàn thành.</summary>
        public bool IsCompleted { get; private set; }

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (successTick == null) successTick = GetComponentInChildren<CanvasGroup>(true);
            ApplyAlpha(0f);
        }

        /// <summary>
        /// Đặt trạng thái hoàn thành của chấm.
        /// </summary>
        /// <param name="isCompleted">True khi lượt đánh đã thành công.</param>
        /// <param name="isFade">True để chuyển mượt; false để đổi tức thì.</param>
        public void SetCompleted(bool isCompleted, bool isFade = true)
        {
            IsCompleted = isCompleted;

            float target = isCompleted ? 1f : 0f;

            StopFade();

            if (!isFade || fadeDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyAlpha(target);
                return;
            }

            fadeRoutine = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            float from = successTick != null ? successTick.alpha : target;

            yield return Tweener.Value(from, target, fadeDuration, ApplyAlpha);

            ApplyAlpha(target);
            fadeRoutine = null;
        }

        private void ApplyAlpha(float alpha)
        {
            if (successTick == null) return;

            successTick.alpha = alpha;
            successTick.blocksRaycasts = alpha > 0.99f;
        }

        private void StopFade()
        {
            if (fadeRoutine == null) return;

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        private void OnDisable()
        {
            StopFade();
        }

        private void OnDestroy()
        {
            StopFade();
        }
    }
}
