using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bọc một <see cref="ParticleSystem"/> (và/hoặc một GameObject hiệu ứng) sau một API bật/tắt gọn.
    /// Mọi hàm đều NO-OP an toàn khi chưa gán reference, nên booster có thể gọi vô điều kiện.
    /// </summary>
    public class VFXPlayer : MonoBehaviour
    {
        /// <summary>Hệ hạt của hiệu ứng; có thể để trống nếu chỉ dùng <see cref="vfxRoot"/>.</summary>
        public ParticleSystem particle;

        /// <summary>GameObject sẽ được bật/tắt kèm theo; để trống nếu chỉ dùng <see cref="particle"/>.</summary>
        [SerializeField] private GameObject vfxRoot;

        /// <summary>Bật hiệu ứng ngay khi component được kích hoạt.</summary>
        [SerializeField] private bool playOnEnable;

        private bool isPlayingFallback;
        private Coroutine oneShotRoutine;

        /// <summary>True khi hiệu ứng đang chạy.</summary>
        public bool IsPlaying => particle != null ? particle.isPlaying : isPlayingFallback;

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            if (oneShotRoutine != null)
            {
                DelayedAction.Cancel(oneShotRoutine);
                oneShotRoutine = null;
            }
            isPlayingFallback = false;
        }

        /// <summary>Bật hiệu ứng. Không làm gì nếu chưa gán reference nào.</summary>
        public void Play()
        {
            if (vfxRoot != null) vfxRoot.SetActive(true);

            if (particle != null)
            {
                if (!particle.gameObject.activeSelf) particle.gameObject.SetActive(true);
                particle.Play(true);
            }

            isPlayingFallback = true;
        }

        /// <summary>Tắt hiệu ứng (dừng phát và xoá hạt còn lại). Không làm gì nếu chưa gán reference nào.</summary>
        public void Stop()
        {
            if (particle != null) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (vfxRoot != null) vfxRoot.SetActive(false);

            isPlayingFallback = false;
        }

        /// <summary>
        /// Bật hiệu ứng rồi tự tắt sau <paramref name="duration"/> giây.
        /// Gọi lại trước khi hết giờ sẽ huỷ hẹn giờ cũ và hẹn lại từ đầu.
        /// </summary>
        /// <param name="duration">Thời gian phát (giây); &lt;= 0 thì phát luôn và không tự tắt.</param>
        public void PlayOneShot(float duration)
        {
            if (oneShotRoutine != null)
            {
                DelayedAction.Cancel(oneShotRoutine);
                oneShotRoutine = null;
            }

            Play();

            if (duration <= 0f) return;

            oneShotRoutine = DelayedAction.Run(duration, () =>
            {
                if (this == null) return;
                oneShotRoutine = null;
                Stop();
            });
        }
    }
}
