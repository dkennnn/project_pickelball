using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trung tâm phát âm thanh: ba kênh riêng (nhạc nền / SFX gameplay / SFX giao diện),
    /// tra clip theo id trong <see cref="AudioData"/>, âm lượng lưu vào <see cref="PlayerPrefs"/>.
    /// <para>
    /// Mọi hàm đều an toàn khi <see cref="audioData"/> null hoặc id không tồn tại — im lặng bỏ qua
    /// chứ không ném lỗi, để giai đoạn chưa có asset audio vẫn chạy được.
    /// </para>
    /// <para>
    /// Ghi chú: <c>UI/UIButtonSFX.cs</c> hiện tự dựng AudioSource riêng. Khi audio asset sẵn sàng
    /// nên đổi nó gọi <see cref="PlayUI"/> để dùng chung một kênh và một chỉnh âm lượng.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : IndestructibleSingleton<AudioManager>
    {
        /// <summary>Khoá PlayerPrefs cho âm lượng nhạc nền.</summary>
        public const string MusicVolumeKey = "Pickleball.MusicVolume";

        /// <summary>Khoá PlayerPrefs cho âm lượng hiệu ứng (SFX + UI).</summary>
        public const string SfxVolumeKey = "Pickleball.SfxVolume";

        [Tooltip("Bảng tra âm thanh. Bỏ trống thì mọi lệnh phát đều bị bỏ qua trong im lặng.")]
        [SerializeField] private AudioData audioData;

        [Header("Sources (bỏ trống sẽ tự tạo)")]
        [Tooltip("Kênh nhạc nền, luôn loop.")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Kênh hiệu ứng gameplay.")]
        [SerializeField] private AudioSource sfxSource;

        [Tooltip("Kênh hiệu ứng giao diện (bấm nút, mở popup).")]
        [SerializeField] private AudioSource uiSource;

        [Header("Defaults")]
        [Tooltip("Âm lượng nhạc nền mặc định khi chưa có giá trị lưu.")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultMusicVolume = 0.6f;

        [Tooltip("Âm lượng hiệu ứng mặc định khi chưa có giá trị lưu.")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultSfxVolume = 1f;

        /// <summary>Âm lượng nhạc nền hiện tại, trong [0,1].</summary>
        public float MusicVolume { get; private set; } = 1f;

        /// <summary>Âm lượng hiệu ứng hiện tại, trong [0,1].</summary>
        public float SfxVolume { get; private set; } = 1f;

        /// <summary>Id nhạc nền đang phát; rỗng nếu không phát gì.</summary>
        public string CurrentMusicId { get; private set; } = string.Empty;

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();

            EnsureSources();
            LoadVolumes();
        }

        /// <summary>Gán bảng âm thanh lúc runtime (dùng khi nạp từ Addressables/Resources).</summary>
        /// <param name="data">Bảng âm thanh mới; null = tắt toàn bộ phát.</param>
        public void SetAudioData(AudioData data)
        {
            audioData = data;
        }

        private void EnsureSources()
        {
            musicSource = EnsureSource(musicSource, "MusicSource", true);
            sfxSource = EnsureSource(sfxSource, "SfxSource", false);
            uiSource = EnsureSource(uiSource, "UiSource", false);
        }

        private AudioSource EnsureSource(AudioSource existing, string childName, bool loop)
        {
            AudioSource source = existing;
            if (source == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                source = go.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D: âm lượng không phụ thuộc vị trí.
            return source;
        }

        private void LoadVolumes()
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume));
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = MusicVolume;
            if (sfxSource != null) sfxSource.volume = SfxVolume;
            if (uiSource != null) uiSource.volume = SfxVolume;
        }

        /// <summary>Phát một hiệu ứng gameplay theo id. No-op nếu id không tra được.</summary>
        /// <param name="id">Định danh clip trong <see cref="AudioData"/>.</param>
        public void PlaySFX(string id)
        {
            PlayOneShot(sfxSource, id, SfxVolume);
        }

        /// <summary>Phát một hiệu ứng giao diện theo id. No-op nếu id không tra được.</summary>
        /// <param name="id">Định danh clip trong <see cref="AudioData"/>.</param>
        public void PlayUI(string id)
        {
            PlayOneShot(uiSource, id, SfxVolume);
        }

        private void PlayOneShot(AudioSource source, string id, float channelVolume)
        {
            if (source == null || audioData == null) return;

            AudioClipData data = audioData.Get(id);
            if (data == null || data.clip == null) return;
            if (channelVolume <= 0f) return;

            source.PlayOneShot(data.clip, Mathf.Clamp01(data.volume) * Mathf.Clamp01(channelVolume));
        }

        /// <summary>
        /// Phát nhạc nền theo id. Gọi lại cùng id trong lúc đang phát thì không làm gì
        /// (tránh nhạc bị nhảy về đầu mỗi lần đổi màn).
        /// </summary>
        /// <param name="id">Định danh clip trong <see cref="AudioData"/>.</param>
        public void PlayMusic(string id)
        {
            if (musicSource == null || audioData == null) return;

            AudioClipData data = audioData.Get(id);
            if (data == null || data.clip == null) return;

            if (musicSource.isPlaying && CurrentMusicId == id) return;

            CurrentMusicId = id;
            musicSource.clip = data.clip;
            musicSource.loop = true; // Nhạc nền luôn loop bất kể cờ trong AudioClipData.
            musicSource.volume = MusicVolume * Mathf.Clamp01(data.volume);
            musicSource.Play();
        }

        /// <summary>Dừng nhạc nền.</summary>
        public void StopMusic()
        {
            CurrentMusicId = string.Empty;
            if (musicSource == null) return;
            musicSource.Stop();
            musicSource.clip = null;
        }

        /// <summary>Đặt âm lượng nhạc nền và lưu vào PlayerPrefs.</summary>
        /// <param name="value">Giá trị trong [0,1]; ngoài khoảng sẽ bị clamp.</param>
        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        /// <summary>Đặt âm lượng hiệu ứng (cả SFX lẫn UI) và lưu vào PlayerPrefs.</summary>
        /// <param name="value">Giá trị trong [0,1]; ngoài khoảng sẽ bị clamp.</param>
        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        /// <summary>Bật/tắt nhạc nền nhanh (giữ nguyên giá trị âm lượng đã lưu).</summary>
        /// <param name="isActive">False = tắt tiếng nhạc nền.</param>
        public void SetMusicActive(bool isActive)
        {
            if (musicSource == null) return;
            musicSource.mute = !isActive;
        }

        /// <summary>Bật/tắt hiệu ứng nhanh (giữ nguyên giá trị âm lượng đã lưu).</summary>
        /// <param name="isActive">False = tắt tiếng hiệu ứng.</param>
        public void SetSfxActive(bool isActive)
        {
            if (sfxSource != null) sfxSource.mute = !isActive;
            if (uiSource != null) uiSource.mute = !isActive;
        }
    }
}
