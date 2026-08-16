using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>Khoá framerate mục tiêu và tắt vsync trên mobile.</summary>
    public class FPSManager : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool disableVSync = true;
        [SerializeField] private bool neverSleep = true;

        private void Awake()
        {
            if (disableVSync) QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            if (neverSleep) Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>Đổi framerate mục tiêu lúc runtime (ví dụ hạ xuống 30 khi ở menu).</summary>
        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = fps;
        }
    }
}
