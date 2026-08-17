using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn hồ sơ người chơi: avatar, tên, level, trophy, thống kê trận và bảng 7 chỉ số
    /// tổng hợp từ trang bị đang mặc.
    /// </summary>
    public class ProfileUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.Profile;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Ảnh đại diện.</summary>
        [SerializeField] private Image avatar;

        /// <summary>Tên người chơi.</summary>
        [SerializeField] private TextMeshProUGUI playerNameText;

        /// <summary>Level hiện tại.</summary>
        [SerializeField] private TextMeshProUGUI levelText;

        /// <summary>Số trophy.</summary>
        [SerializeField] private TextMeshProUGUI trophiesText;

        /// <summary>Tổng số trận đã chơi.</summary>
        [SerializeField] private TextMeshProUGUI totalMatchesText;

        /// <summary>Tổng số trận thắng.</summary>
        [SerializeField] private TextMeshProUGUI totalWinsText;

        /// <summary>Tỉ lệ thắng.</summary>
        [SerializeField] private TextMeshProUGUI winRateText;

        /// <summary>Chuỗi thắng hiện tại.</summary>
        [SerializeField] private TextMeshProUGUI winStreakText;

        /// <summary>Node cha chứa các thanh chỉ số.</summary>
        [SerializeField] private Transform propertyContainer;

        /// <summary>Prefab một thanh chỉ số.</summary>
        [SerializeField] private ProfilePropertyCellView propertyCellPrefab;

        private readonly List<ProfilePropertyCellView> propertyCells = new List<ProfilePropertyCellView>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            RefreshProfile();
            RefreshProperties();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            base.OnDestroy();
        }

        /// <summary>Vẽ lại phần thông tin và thống kê của hồ sơ.</summary>
        public void RefreshProfile()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;
            if (profile == null) return;

            if (playerNameText != null) playerNameText.text = profile.playerName ?? string.Empty;
            if (levelText != null) levelText.text = "LVL." + profile.level;
            if (trophiesText != null) trophiesText.text = Utilities.FormatCount(profile.trophies);
            if (totalMatchesText != null) totalMatchesText.text = profile.totalMatches.ToString();
            if (totalWinsText != null) totalWinsText.text = profile.totalWins.ToString();
            if (winRateText != null) winRateText.text = Mathf.RoundToInt(profile.winRate * 100f) + "%";
            if (winStreakText != null) winStreakText.text = profile.consecutiveWins.ToString();

            if (avatar != null && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(profile.avatarIndex, 0, gameData.avatarSprites.Count - 1);
                Sprite sprite = gameData.avatarSprites[index];
                if (sprite != null) avatar.sprite = sprite;
            }
        }

        /// <summary>Dựng lại bảng 7 chỉ số từ <see cref="PlayerLoadout.GetPropertyValues"/>.</summary>
        public void RefreshProperties()
        {
            if (propertyContainer == null || propertyCellPrefab == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerLoadout loadout = gameData != null ? gameData.playerLoadout : null;
            if (loadout == null) return;

            List<ProfileProperty> values = loadout.GetPropertyValues();
            if (values == null) values = new List<ProfileProperty>();

            PropertiesData propertiesData = gameData.propertiesData;

            while (propertyCells.Count < values.Count)
            {
                propertyCells.Add(Instantiate(propertyCellPrefab, propertyContainer));
            }

            for (int i = 0; i < propertyCells.Count; i++)
            {
                ProfilePropertyCellView cell = propertyCells[i];
                if (cell == null) continue;

                bool used = i < values.Count;
                cell.gameObject.SetActive(used);
                if (!used) continue;

                ProfileProperty property = values[i];
                PropertyData display = propertiesData != null
                    ? propertiesData.GetPropertyData(property.propertyType)
                    : null;
                cell.Bind(property, display);
            }
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
