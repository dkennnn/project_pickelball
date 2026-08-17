using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hiệu ứng "pop": phóng to rồi thu về scale gốc một cách nhịp nhàng.
    /// <para>
    /// Dùng cho nút mua, thẻ vật phẩm, icon quà… Bản gốc chạy bằng DOTween Sequence;
    /// ở đây thay bằng <see cref="Tweener"/> nội bộ để không phụ thuộc asset ngoài.
    /// </para>
    /// <para>Component hoàn toàn độc lập: không cần bất kỳ reference nào để chạy.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class PopEffect : MonoBehaviour
    {
        [Header("Pop Settings")]
        [Tooltip("Tỉ lệ phóng to thêm so với scale gốc. 0.15 nghĩa là to thêm 15%.")]
        public float scaleAmount = 0.15f;

        [Tooltip("Thời lượng một nửa chu kỳ (phình ra, hoặc thu về), tính bằng giây.")]
        public float duration = 0.35f;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool playOnEnable = true;

        [Tooltip("Lặp vô hạn. Tắt thì chỉ pop đúng một lần rồi trả về scale gốc.")]
        public bool loop = true;

        [Tooltip("Thời gian nghỉ giữa hai lần pop khi bật loop, tính bằng giây.")]
        public float delayBetweenLoops = 0f;

        [Tooltip("Dùng unscaled time để hiệu ứng vẫn chạy khi game đang pause.")]
        public bool useUnscaledTime = true;

        /// <summary>Scale gốc được chụp lại ở lần Awake đầu tiên.</summary>
        private Vector3 originalScale = Vector3.one;

        private bool scaleCaptured;
        private Coroutine popRoutine;

        private void Awake()
        {
            CaptureOriginalScale();
        }

        private void OnEnable()
        {
            if (playOnEnable) StartPopLoop();
        }

        private void OnDisable()
        {
            StopPop();
        }

        /// <summary>Chụp scale gốc đúng một lần để các lần bật/tắt sau không bị trôi scale.</summary>
        private void CaptureOriginalScale()
        {
            if (scaleCaptured) return;
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            scaleCaptured = true;
        }

        /// <summary>Bắt đầu (hoặc khởi động lại) vòng pop.</summary>
        [ContextMenu("Start Pop")]
        public void StartPopLoop()
        {
            if (!isActiveAndEnabled) return;

            CaptureOriginalScale();
            StopPop();
            popRoutine = StartCoroutine(PopRoutine());
        }

        /// <summary>Dừng hiệu ứng và trả transform về scale gốc.</summary>
        [ContextMenu("Stop Pop")]
        public void StopPop()
        {
            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
                popRoutine = null;
            }

            if (scaleCaptured) transform.localScale = originalScale;
        }

        /// <summary>Bật/tắt hiệu ứng bằng một lời gọi duy nhất (tiện cho UnityEvent trên Inspector).</summary>
        /// <param name="enable">True = chạy, false = dừng.</param>
        public void EnablePop(bool enable)
        {
            if (enable) StartPopLoop();
            else StopPop();
        }

        private IEnumerator PopRoutine()
        {
            float half = Mathf.Max(0.01f, duration);
            Vector3 peak = originalScale * (1f + scaleAmount);

            do
            {
                yield return StartCoroutine(ScaleTo(originalScale, peak, half));
                yield return StartCoroutine(ScaleTo(peak, originalScale, half));

                if (loop && delayBetweenLoops > 0f)
                {
                    if (useUnscaledTime) yield return new WaitForSecondsRealtime(delayBetweenLoops);
                    else yield return new WaitForSeconds(delayBetweenLoops);
                }
            }
            while (loop);

            popRoutine = null;
        }

        private IEnumerator ScaleTo(Vector3 from, Vector3 to, float time)
        {
            return Tweener.Value(0f, 1f, time,
                t => transform.localScale = Vector3.LerpUnclamped(from, to, t),
                null, useUnscaledTime, Tweener.EaseInOutQuad);
        }
    }
}
