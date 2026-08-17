using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Phát tiếng click khi bấm nút. Gắn cạnh một <see cref="Button"/>.
    /// Chưa có AudioManager nên component tự tìm/tạo <see cref="AudioSource"/> dùng chung;
    /// nếu thiếu clip thì im lặng bỏ qua chứ không ném lỗi.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonSFX : MonoBehaviour
    {
        /// <summary>Tiếng click; để trống thì component không phát gì.</summary>
        [SerializeField] private AudioClip clickClip;

        /// <summary>Âm lượng phát tiếng click.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        /// <summary>Nguồn phát dùng riêng; để trống thì dùng nguồn phát dùng chung của UI.</summary>
        [SerializeField] private AudioSource audioSource;

        private static AudioSource sharedSource;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayClick);
        }

        /// <summary>Phát tiếng click ngay (dùng được cho node không phải Button).</summary>
        public void PlayClick()
        {
            if (clickClip == null) return;

            AudioSource source = audioSource != null ? audioSource : GetSharedSource();
            if (source == null) return;

            source.PlayOneShot(clickClip, Mathf.Clamp01(volume));
        }

        /// <summary>Nguồn phát dùng chung cho toàn bộ UI; tạo lần đầu khi cần.</summary>
        private static AudioSource GetSharedSource()
        {
            if (sharedSource != null) return sharedSource;

            GameObject host = new GameObject("[UIButtonSFX]");
            DontDestroyOnLoad(host);

            sharedSource = host.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.spatialBlend = 0f;
            return sharedSource;
        }
    }
}
