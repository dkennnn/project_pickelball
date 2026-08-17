using System;
using System.Collections;
using Pickleball;
using StarterKit.Utilities;
using TMPro;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Chạy số dần dần trên một TextMeshPro (coin, gem, trophy...) thay vì nhảy phắt sang giá trị mới.
    /// Kết quả được định dạng bằng <see cref="Pickleball.Utilities.FormatCount"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class TextCounterUtility : MonoBehaviour
    {
        /// <summary>Ô chữ hiển thị; tự lấy trên chính GameObject nếu để trống.</summary>
        [SerializeField] private TextMeshProUGUI label;

        /// <summary>True thì rút gọn số lớn thành 1.2K / 3.4M; false thì in đủ chữ số.</summary>
        [SerializeField] private bool useShortFormat = true;

        /// <summary>Giá trị đang hiển thị sau lần chạy gần nhất.</summary>
        public long CurrentValue { get; private set; }

        /// <summary>Phát khi số đã chạy tới đích.</summary>
        public event Action OnCountCompleted;

        private Coroutine routine;

        private void Awake()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>Đặt giá trị ngay lập tức, không chạy số.</summary>
        /// <param name="value">Giá trị cần hiển thị.</param>
        public void SetImmediate(long value)
        {
            StopRunning();
            CurrentValue = value;
            Render(value);
        }

        /// <summary>
        /// Chạy số từ giá trị đang hiển thị tới <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Giá trị đích.</param>
        /// <param name="duration">Thời lượng chạy số, tính bằng giây.</param>
        public void CountTo(long target, float duration = 0.5f)
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (label == null) return;

            if (duration <= 0f || !isActiveAndEnabled)
            {
                SetImmediate(target);
                OnCountCompleted?.Invoke();
                return;
            }

            StopRunning();
            routine = StartCoroutine(CountRoutine(CurrentValue, target, duration));
        }

        private IEnumerator CountRoutine(long from, long to, float duration)
        {
            yield return Tweener.Value(
                from,
                to,
                duration,
                value => Render((long)Math.Round(value)),
                null,
                true,
                Tweener.EaseOutQuad);

            CurrentValue = to;
            Render(to);
            routine = null;
            OnCountCompleted?.Invoke();
        }

        private void Render(long value)
        {
            if (label == null) return;
            label.text = useShortFormat ? Pickleball.Utilities.FormatCount(value) : value.ToString();
        }

        private void StopRunning()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private void OnDisable()
        {
            StopRunning();
        }
    }
}
