using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Bật/tắt một object theo chu kỳ ngẫu nhiên trong khoảng
    /// [<see cref="minInterval"/>, <see cref="maxInterval"/>] — dùng cho đèn nháy, mắt nhân vật chớp,
    /// icon "mới" nhấp nháy.
    /// <para>
    /// Bỏ trống <see cref="targetObject"/> thì component bật/tắt <see cref="Graphic"/> hoặc
    /// <see cref="Renderer"/> ngay trên GameObject này — cố ý KHÔNG tắt chính GameObject này,
    /// vì tắt đi thì coroutine sẽ chết và không bao giờ bật lại được.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class RandomBlink : MonoBehaviour
    {
        [Tooltip("Object cần bật/tắt. Bỏ trống thì bật/tắt Graphic/Renderer trên GameObject này.")]
        public GameObject targetObject;

        [Tooltip("Khoảng nghỉ ngắn nhất giữa hai lần đổi trạng thái, tính bằng giây.")]
        public float minInterval = 0.4f;

        [Tooltip("Khoảng nghỉ dài nhất giữa hai lần đổi trạng thái, tính bằng giây.")]
        public float maxInterval = 2.5f;

        [Tooltip("Thời gian object ở trạng thái tắt mỗi lần chớp, tính bằng giây.")]
        public float blinkDuration = 0.12f;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool playOnEnable = true;

        private Coroutine blinkRoutine;
        private Graphic cachedGraphic;
        private Renderer cachedRenderer;

        private void Awake()
        {
            cachedGraphic = GetComponent<Graphic>();
            if (cachedGraphic == null) cachedRenderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            if (playOnEnable) StartBlinking();
        }

        private void OnDisable()
        {
            StopBlinking();
        }

        /// <summary>Bắt đầu chớp ngẫu nhiên.</summary>
        public void StartBlinking()
        {
            if (!isActiveAndEnabled) return;
            StopBlinking();
            blinkRoutine = StartCoroutine(BlinkRandomly());
        }

        /// <summary>Dừng chớp và để object ở trạng thái hiện.</summary>
        public void StopBlinking()
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
            }

            SetVisible(true);
        }

        private IEnumerator BlinkRandomly()
        {
            float lo = Mathf.Max(0f, Mathf.Min(minInterval, maxInterval));
            float hi = Mathf.Max(lo, Mathf.Max(minInterval, maxInterval));

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(lo, hi));

                SetVisible(false);
                yield return new WaitForSeconds(Mathf.Max(0.01f, blinkDuration));
                SetVisible(true);
            }
        }

        /// <summary>Đặt trạng thái hiện/ẩn lên đích đang dùng. No-op nếu không có đích nào.</summary>
        /// <param name="visible">True = hiện.</param>
        private void SetVisible(bool visible)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(visible);
                return;
            }

            if (cachedGraphic != null)
            {
                cachedGraphic.enabled = visible;
                return;
            }

            if (cachedRenderer != null) cachedRenderer.enabled = visible;
        }
    }
}
