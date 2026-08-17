using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô một chế độ chơi trong màn chọn chế độ: ảnh chế độ, điều kiện mở khoá
    /// và cờ "sắp ra mắt".
    /// </summary>
    public class ModeSelectionCellView : MonoBehaviour
    {
        /// <summary>Nút bấm chọn chế độ; để trống thì lấy Button trên chính node.</summary>
        [SerializeField] private Button button;

        /// <summary>Ảnh minh hoạ chế độ.</summary>
        [SerializeField] private Image icon;

        /// <summary>Chữ báo cần level bao nhiêu mới mở.</summary>
        [SerializeField] private TextMeshProUGUI lockedTxt;

        /// <summary>Băng rôn "COMING SOON".</summary>
        [SerializeField] private GameObject comingSoonBanner;

        /// <summary>Nút xem mô tả chế độ.</summary>
        [SerializeField] private Button infoBtn;

        /// <summary>Phát khi chọn một chế độ đã mở khoá.</summary>
        public event Action<MatchModeType> OnClicked;

        /// <summary>Phát khi bấm nút thông tin.</summary>
        public event Action<MatchModeConfig> OnInfoClicked;

        /// <summary>Cấu hình chế độ đang gắn vào ô.</summary>
        public MatchModeConfig BoundConfig { get; private set; }

        /// <summary>True nếu chế độ này đang chọn được.</summary>
        public bool IsUnlocked { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một chế độ vào ô.
        /// </summary>
        /// <param name="config">Cấu hình chế độ; null sẽ ẩn ô.</param>
        /// <param name="playerLevel">Level hiện tại của người chơi, dùng để xét mở khoá.</param>
        public void Bind(MatchModeConfig config, int playerLevel)
        {
            Wire();
            BoundConfig = config;

            if (config == null)
            {
                gameObject.SetActive(false);
                IsUnlocked = false;
                return;
            }

            gameObject.SetActive(true);

            IsUnlocked = !config.isComingSoon && playerLevel >= config.unlockLevel;

            if (icon != null && config.modeSelectionSprite != null)
            {
                icon.sprite = config.modeSelectionSprite;
                icon.enabled = true;
            }

            if (comingSoonBanner != null) comingSoonBanner.SetActive(config.isComingSoon);

            if (lockedTxt != null)
            {
                bool showLock = !config.isComingSoon && playerLevel < config.unlockLevel;
                lockedTxt.gameObject.SetActive(showLock);
                if (showLock) lockedTxt.text = "Mode Unlocks At LVL " + config.unlockLevel;
            }

            if (button != null) button.interactable = IsUnlocked;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
            if (infoBtn != null) infoBtn.onClick.AddListener(HandleInfo);
        }

        private void HandleClick()
        {
            if (BoundConfig == null || !IsUnlocked) return;
            OnClicked?.Invoke(BoundConfig.modeType);
        }

        private void HandleInfo()
        {
            OnInfoClicked?.Invoke(BoundConfig);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            if (infoBtn != null) infoBtn.onClick.RemoveListener(HandleInfo);

            OnClicked = null;
            OnInfoClicked = null;
        }
    }
}
