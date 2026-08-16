using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>Tự hạ chất lượng đồ hoạ trên máy yếu dựa theo RAM và số nhân CPU.</summary>
    public class DynamicGraphicsConfigurator : MonoBehaviour
    {
        [SerializeField] private int lowEndMemoryMB = 3072;
        [SerializeField] private int midEndMemoryMB = 6144;
        [SerializeField] private int lowEndCoreCount = 4;
        [SerializeField] private int lowEndTargetFrameRate = 30;

        private void Awake()
        {
            Apply();
        }

        /// <summary>Chọn quality level: 0 = low, 1 = medium, 2 = high.</summary>
        public void Apply()
        {
            int level = DetermineQualityLevel();
            int maxLevel = Mathf.Max(0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(Mathf.Min(level, maxLevel), true);

            if (level == 0) Application.targetFrameRate = lowEndTargetFrameRate;
        }

        private int DetermineQualityLevel()
        {
            int memory = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;

            if (memory <= lowEndMemoryMB || cores <= lowEndCoreCount) return 0;
            if (memory <= midEndMemoryMB) return 1;
            return 2;
        }
    }
}
