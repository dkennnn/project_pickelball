using System;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Lớp cha của mọi hiệu ứng vào/ra cho một node UI.
    /// <see cref="UIScreenBase"/> gom toàn bộ <see cref="Animatable"/> con và gọi
    /// <see cref="PlayIn"/> khi hiện, <see cref="PlayOut"/> khi ẩn.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Animatable : MonoBehaviour
    {
        /// <summary>Thời lượng hiệu ứng, tính bằng giây.</summary>
        public float duration = 0.25f;

        /// <summary>Trễ trước khi hiệu ứng bắt đầu, tính bằng giây.</summary>
        public float delay = 0f;

        /// <summary>True khi một hiệu ứng đang chạy.</summary>
        public bool IsPlaying { get; protected set; }

        private Coroutine running;

        /// <summary>Ghi lại trạng thái gốc của node để hiệu ứng biết đích đến. Gọi một lần.</summary>
        public virtual void Initialize() { }

        /// <summary>Trả node về đúng trạng thái gốc, huỷ hiệu ứng đang chạy.</summary>
        public virtual void ResetAnimator()
        {
            StopRunning();
            IsPlaying = false;
        }

        /// <summary>Chạy hiệu ứng xuất hiện; <paramref name="onComplete"/> được gọi khi xong.</summary>
        /// <param name="onComplete">Callback sau khi hiệu ứng kết thúc; có thể null.</param>
        public abstract void PlayIn(Action onComplete = null);

        /// <summary>Chạy hiệu ứng biến mất; <paramref name="onComplete"/> được gọi khi xong.</summary>
        /// <param name="onComplete">Callback sau khi hiệu ứng kết thúc; có thể null.</param>
        public abstract void PlayOut(Action onComplete = null);

        /// <summary>Chạy một coroutine hiệu ứng, tự huỷ coroutine cũ nếu còn.</summary>
        /// <param name="routine">Coroutine hiệu ứng cần chạy.</param>
        /// <param name="onComplete">Callback gọi ngay nếu object đã tắt.</param>
        protected void Run(System.Collections.IEnumerator routine, Action onComplete)
        {
            StopRunning();

            if (!isActiveAndEnabled || routine == null)
            {
                IsPlaying = false;
                onComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            running = StartCoroutine(routine);
        }

        /// <summary>Dừng coroutine hiệu ứng đang chạy (nếu có).</summary>
        protected void StopRunning()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }
        }

        protected virtual void OnDisable()
        {
            StopRunning();
            IsPlaying = false;
        }
    }
}
