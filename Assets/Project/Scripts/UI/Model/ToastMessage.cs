using System;
using System.Collections;
using StarterKit.Utilities;
using TMPro;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một dòng thông báo nhanh (toast). Gắn trên prefab ToastMessage.
    /// <see cref="ToastHandler"/> điều khiển vòng đời của nó.
    /// </summary>
    [DisallowMultipleComponent]
    public class ToastMessage : MonoBehaviour
    {
        /// <summary>Ô chữ hiển thị nội dung.</summary>
        [SerializeField] private TextMeshProUGUI toastMessage;

        /// <summary>Nhóm canvas để làm mờ khi hiện/ẩn.</summary>
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>Thời gian hiệu ứng hiện/ẩn, tính bằng giây.</summary>
        [SerializeField] private float fadeDuration = 0.2f;

        /// <summary>Phát khi toast đã ẩn xong; tham số là chính toast này.</summary>
        public event Action<ToastMessage> OnFinished;

        private Coroutine routine;

        private void Awake()
        {
            if (toastMessage == null) toastMessage = GetComponentInChildren<TextMeshProUGUI>(true);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Hiện toast với nội dung cho trước rồi tự ẩn sau <paramref name="duration"/> giây.
        /// </summary>
        /// <param name="message">Nội dung hiển thị.</param>
        /// <param name="duration">Thời gian giữ trên màn hình, tính bằng giây.</param>
        public void Show(string message, float duration = 2f)
        {
            if (toastMessage != null) toastMessage.text = message ?? string.Empty;

            gameObject.SetActive(true);

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine(duration));
        }

        /// <summary>Ẩn ngay lập tức và phát <see cref="OnFinished"/>.</summary>
        public void HideImmediate()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            OnFinished?.Invoke(this);
        }

        private IEnumerator ShowRoutine(float duration)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                yield return Tweener.Value(0f, 1f, fadeDuration, v => { if (canvasGroup != null) canvasGroup.alpha = v; }, null, true);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));

            if (canvasGroup != null)
            {
                yield return Tweener.Value(1f, 0f, fadeDuration, v => { if (canvasGroup != null) canvasGroup.alpha = v; }, null, true);
            }

            routine = null;
            gameObject.SetActive(false);
            OnFinished?.Invoke(this);
        }

        private void OnDestroy()
        {
            OnFinished = null;
        }
    }
}
