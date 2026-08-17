using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn cài đặt: âm lượng nhạc/hiệu ứng, chọn tay thuận, số hiệu phiên bản.
    /// Âm lượng lưu bằng <see cref="PlayerPrefs"/>; tay thuận ghi thẳng vào
    /// <see cref="GameData.handSide"/>.
    /// </summary>
    public class SettingsUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.Settings;

        /// <summary>Khoá PlayerPrefs của âm lượng nhạc nền.</summary>
        public const string SoundVolumeKey = "settings.sound";

        /// <summary>Khoá PlayerPrefs của âm lượng hiệu ứng.</summary>
        public const string SfxVolumeKey = "settings.sfx";

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        /// <summary>Thanh chỉnh âm lượng hiệu ứng.</summary>
        [SerializeField] private Slider sfxSlider;

        /// <summary>Thanh chỉnh âm lượng nhạc nền.</summary>
        [SerializeField] private Slider soundSlider;

        /// <summary>Nút đổi tay thuận.</summary>
        [SerializeField] private Button handSelectionBtn;

        /// <summary>Ảnh minh hoạ tay thuận đang chọn.</summary>
        [SerializeField] private Image handSelectionImage;

        /// <summary>Ảnh cho tay thuận phải.</summary>
        [SerializeField] private Sprite rightHandSprite;

        /// <summary>Ảnh cho tay thuận trái.</summary>
        [SerializeField] private Sprite leftHandSprite;

        /// <summary>Số hiệu phiên bản ứng dụng.</summary>
        [SerializeField] private TextMeshProUGUI versionNumber;

        /// <summary>Nút khôi phục giao dịch (tắt ở bản offline).</summary>
        [SerializeField] private Button restoreBtn;

        /// <summary>Nút xem chính sách bảo mật (tắt ở bản offline).</summary>
        [SerializeField] private Button policyBtn;

        /// <summary>Ô chọn ngôn ngữ (tắt ở bản offline).</summary>
        [SerializeField] private TMP_Dropdown languageDropdown;

        private GameData gameData;

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (handSelectionBtn != null) handSelectionBtn.onClick.AddListener(HandleToggleHand);

            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(HandleSoundChanged);

            // Không làm IAP / localization ở bản này.
            if (restoreBtn != null) restoreBtn.gameObject.SetActive(false);
            if (policyBtn != null) policyBtn.gameObject.SetActive(false);
            if (languageDropdown != null) languageDropdown.gameObject.SetActive(false);

            if (title != null) title.text = "OPTIONS";
            if (versionNumber != null) versionNumber.text = "v " + Application.version;
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            if (soundSlider != null) soundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SoundVolumeKey, 1f));

            ApplyVolume();
            RefreshHand();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (handSelectionBtn != null) handSelectionBtn.onClick.RemoveListener(HandleToggleHand);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(HandleSoundChanged);

            base.OnDestroy();
        }

        /// <summary>Cập nhật ảnh tay thuận theo lựa chọn hiện tại.</summary>
        public void RefreshHand()
        {
            if (handSelectionImage == null || gameData == null) return;

            Sprite sprite = gameData.handSide == HandSide.Left ? leftHandSprite : rightHandSprite;
            if (sprite != null) handSelectionImage.sprite = sprite;
        }

        private void HandleToggleHand()
        {
            if (gameData == null) return;

            gameData.handSide = gameData.handSide == HandSide.Left ? HandSide.Right : HandSide.Left;
            RefreshHand();

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
        }

        private void HandleSfxChanged(float value)
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            ApplyVolume();
        }

        private void HandleSoundChanged(float value)
        {
            PlayerPrefs.SetFloat(SoundVolumeKey, Mathf.Clamp01(value));
            ApplyVolume();
        }

        /// <summary>
        /// Áp âm lượng lên <see cref="AudioListener.volume"/>.
        /// Chưa có AudioManager tách kênh nhạc/hiệu ứng nên tạm lấy giá trị lớn hơn.
        /// </summary>
        private void ApplyVolume()
        {
            float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            float sound = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
            AudioListener.volume = Mathf.Clamp01(Mathf.Max(sfx, sound));
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
