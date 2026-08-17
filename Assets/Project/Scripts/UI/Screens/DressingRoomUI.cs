using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Phòng thay đồ: 4 nhóm vật phẩm (Character / Grip / Paddle / Workout), bảng 7 chỉ số
    /// tổng hợp từ trang bị đang mặc, thống kê ngắn và ô đổi tên.
    /// Bấm vào một nhóm sẽ mở <see cref="ScreenType.CategoryDisplayUI"/>.
    /// </summary>
    public class DressingRoomUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.DressingRoomUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề màn.</summary>
        [SerializeField] private TextMeshProUGUI title;

        // --- Thống kê ---

        /// <summary>Chuỗi thắng hiện tại.</summary>
        [SerializeField] private TextMeshProUGUI winStreakValue;

        /// <summary>Tỉ lệ thắng.</summary>
        [SerializeField] private TextMeshProUGUI winRate;

        /// <summary>Tổng số trận.</summary>
        [SerializeField] private TextMeshProUGUI matchesCount;

        // --- Tên người chơi ---

        /// <summary>Ô nhập tên người chơi.</summary>
        [SerializeField] private TMP_InputField playernameInputField;

        /// <summary>Ô chữ hiển thị tên (khi không ở chế độ sửa).</summary>
        [SerializeField] private TextMeshProUGUI usernameText;

        /// <summary>Nút bật chế độ sửa tên.</summary>
        [SerializeField] private Button editBtn;

        // --- Chỉ số ---

        /// <summary>Node cha chứa các thanh chỉ số.</summary>
        [SerializeField] private Transform propertyContainer;

        /// <summary>Prefab một thanh chỉ số.</summary>
        [SerializeField] private ProfilePropertyCellView propertyCellPrefab;

        // --- Nhóm vật phẩm ---

        /// <summary>Node cha chứa các ô nhóm vật phẩm.</summary>
        [SerializeField] private Transform categories;

        /// <summary>Prefab một ô nhóm vật phẩm.</summary>
        [SerializeField] private ShopCategoryCellView categoryCellPrefab;

        // --- Nút thao tác nhanh trên vật phẩm đang chọn ---

        /// <summary>Nút nâng cấp vật phẩm đang chọn bằng tazo.</summary>
        [SerializeField] private Button upgradeTazosButton;

        /// <summary>Nút nâng cấp vật phẩm đang chọn bằng gem.</summary>
        [SerializeField] private Button upgradeGemsButton;

        /// <summary>Nút mua vật phẩm đang chọn bằng coin.</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Ảnh nhân vật nam trong nền.</summary>
        [SerializeField] private GameObject malePlayer;

        /// <summary>Ảnh nhân vật nữ trong nền.</summary>
        [SerializeField] private GameObject femalePlayer;

        /// <summary>Nhóm vật phẩm đang được chọn để thao tác nhanh.</summary>
        public ShopCategoryData SelectedCategory { get; private set; }

        private readonly List<ProfilePropertyCellView> propertyCells = new List<ProfilePropertyCellView>();
        private readonly List<ShopCategoryCellView> categoryCells = new List<ShopCategoryCellView>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (editBtn != null) editBtn.onClick.AddListener(HandleEditName);
            if (playernameInputField != null) playernameInputField.onEndEdit.AddListener(HandleNameSubmitted);

            if (upgradeTazosButton != null) upgradeTazosButton.onClick.AddListener(HandleUpgradeTazos);
            if (upgradeGemsButton != null) upgradeGemsButton.onClick.AddListener(HandleUpgradeGems);
            if (buyButton != null) buyButton.onClick.AddListener(HandleBuy);

            if (title != null) title.text = "ROOM";
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            PlayerLoadout.OnPlayerLoadoutUpdated += RefreshProperties;
            Shop.OnShopChanged += RefreshAll;

            RefreshAll();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            PlayerLoadout.OnPlayerLoadoutUpdated -= RefreshProperties;
            Shop.OnShopChanged -= RefreshAll;
        }

        protected override void OnDestroy()
        {
            PlayerLoadout.OnPlayerLoadoutUpdated -= RefreshProperties;
            Shop.OnShopChanged -= RefreshAll;

            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (editBtn != null) editBtn.onClick.RemoveListener(HandleEditName);
            if (playernameInputField != null) playernameInputField.onEndEdit.RemoveListener(HandleNameSubmitted);
            if (upgradeTazosButton != null) upgradeTazosButton.onClick.RemoveListener(HandleUpgradeTazos);
            if (upgradeGemsButton != null) upgradeGemsButton.onClick.RemoveListener(HandleUpgradeGems);
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuy);

            for (int i = 0; i < categoryCells.Count; i++)
            {
                if (categoryCells[i] != null) categoryCells[i].OnClicked -= HandleCategoryClicked;
            }
            categoryCells.Clear();

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Cập nhật hiển thị
        // ------------------------------------------------------------------

        /// <summary>Vẽ lại toàn bộ màn.</summary>
        public void RefreshAll()
        {
            RefreshStats();
            RefreshProperties();
            RefreshCategories();
            RefreshQuickActions();
        }

        /// <summary>Vẽ lại khối thống kê ngắn và tên người chơi.</summary>
        public void RefreshStats()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;
            if (profile == null) return;

            if (winStreakValue != null) winStreakValue.text = profile.consecutiveWins.ToString();
            if (winRate != null) winRate.text = Mathf.RoundToInt(profile.winRate * 100f) + "%";
            if (matchesCount != null) matchesCount.text = profile.totalMatches.ToString();

            if (usernameText != null) usernameText.text = profile.playerName ?? string.Empty;
            if (playernameInputField != null) playernameInputField.SetTextWithoutNotify(profile.playerName ?? string.Empty);

            if (malePlayer != null) malePlayer.SetActive(true);
            if (femalePlayer != null) femalePlayer.SetActive(false);
        }

        /// <summary>Dựng lại bảng 7 chỉ số từ trang bị đang mặc.</summary>
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

        /// <summary>Dựng lại 4 ô nhóm vật phẩm.</summary>
        public void RefreshCategories()
        {
            if (categories == null || categoryCellPrefab == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            List<ShopCategoryData> list = shop != null ? shop.shopCategories : null;
            if (list == null) return;

            while (categoryCells.Count < list.Count)
            {
                ShopCategoryCellView cell = Instantiate(categoryCellPrefab, categories);
                cell.OnClicked += HandleCategoryClicked;
                categoryCells.Add(cell);
            }

            for (int i = 0; i < categoryCells.Count; i++)
            {
                ShopCategoryCellView cell = categoryCells[i];
                if (cell == null) continue;

                bool used = i < list.Count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(list[i]);
            }

            if (SelectedCategory == null && list.Count > 0) SelectedCategory = list[0];
        }

        /// <summary>Bật/tắt các nút thao tác nhanh theo vật phẩm đang chọn.</summary>
        public void RefreshQuickActions()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Item item = SelectedCategory != null ? SelectedCategory.selectedItem : null;

            bool owned = item != null && item.isPurchased;

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(item != null && !owned);
                buyButton.interactable = item != null && gameData != null && item.CanPurchase(gameData);
            }

            if (upgradeTazosButton != null)
            {
                upgradeTazosButton.gameObject.SetActive(owned);
                upgradeTazosButton.interactable = owned && gameData != null && item.CanUpgradeWithTazos(gameData);
            }

            if (upgradeGemsButton != null)
            {
                upgradeGemsButton.gameObject.SetActive(owned);
                upgradeGemsButton.interactable = owned && gameData != null && item.CanUpgradeWithGems(gameData);
            }
        }

        // ------------------------------------------------------------------
        // Sự kiện
        // ------------------------------------------------------------------

        private void HandleCategoryClicked(ShopCategoryData category)
        {
            SelectedCategory = category;
            RefreshQuickActions();

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.CategoryDisplayUI, category);
        }

        private void HandleUpgradeTazos()
        {
            Shop shop = ResolveShop();
            Item item = SelectedCategory != null ? SelectedCategory.selectedItem : null;
            if (shop == null || item == null) return;

            shop.UpgradeItemWithTazos(item);
            RefreshAll();
        }

        private void HandleUpgradeGems()
        {
            Shop shop = ResolveShop();
            Item item = SelectedCategory != null ? SelectedCategory.selectedItem : null;
            if (shop == null || item == null) return;

            shop.UpgradeItemWithGems(item);
            RefreshAll();
        }

        private void HandleBuy()
        {
            Shop shop = ResolveShop();
            Item item = SelectedCategory != null ? SelectedCategory.selectedItem : null;
            if (shop == null || item == null) return;

            if (!shop.PurchaseItem(item))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough coins");
                return;
            }

            RefreshAll();
        }

        private void HandleEditName()
        {
            if (playernameInputField == null) return;

            playernameInputField.gameObject.SetActive(true);
            playernameInputField.Select();
            playernameInputField.ActivateInputField();
        }

        private void HandleNameSubmitted(string value)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;
            if (profile == null) return;

            string trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            profile.playerName = trimmed;
            if (usernameText != null) usernameText.text = trimmed;

            if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
        }

        private static Shop ResolveShop()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            return gameData != null ? gameData.shopData : null;
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
