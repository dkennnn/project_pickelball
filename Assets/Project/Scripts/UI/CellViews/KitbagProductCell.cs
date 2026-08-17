using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ bán một túi đồ trong cửa hàng (prefab <c>KitBagCellView</c>): ảnh túi, tên,
    /// mô tả nội dung, giá coin/gem và khối tiến độ xem quảng cáo để mở miễn phí.
    /// <para>
    /// Bản dựng lại KHÔNG có SDK quảng cáo: nút "Watch Ad" chỉ cộng một lượt vào
    /// <see cref="Kitbag.watchedAds"/> và phát <see cref="OnWatchAdRequested"/> để tầng ngoài
    /// tự quyết định có phát quảng cáo thật hay không.
    /// </para>
    /// </summary>
    public class KitbagProductCell : MonoBehaviour
    {
        /// <summary>Asset túi đồ đang gắn vào thẻ.</summary>
        [SerializeField] private Kitbag kitbagData;

        /// <summary>Tên túi (node <c>Name</c>).</summary>
        [SerializeField] private TextMeshProUGUI kitbagNameText;

        /// <summary>Ảnh túi (node <c>BagIcon</c>).</summary>
        [SerializeField] private Image kitbagImage;

        /// <summary>Mô tả nội dung túi (node <c>BagIcon/BagItems</c>).</summary>
        [SerializeField] private TextMeshProUGUI bagItemsText;

        /// <summary>Số tazo hứa hẹn hiển thị cạnh ảnh (node <c>CoinsContent/CoinsText</c>).</summary>
        [SerializeField] private TextMeshProUGUI coinsText;

        /// <summary>Các ô ảnh khoe phần thưởng tiêu biểu.</summary>
        [SerializeField] private List<Image> rewardShowcaseImages = new List<Image>();

        /// <summary>Nút mua (node <c>BuyButton</c>).</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Khối giá bằng tiền trong game (node <c>BuyButton/Currency</c>).</summary>
        [SerializeField] private GameObject coinButtonContent;

        /// <summary>Khối giá bằng quảng cáo (node <c>BuyButton/Ads</c>).</summary>
        [SerializeField] private GameObject adsButtonContent;

        /// <summary>Ô chữ giá (node <c>BuyButton/Currency/PriceText</c>).</summary>
        [SerializeField] private TextMeshProUGUI kitbagPriceText;

        /// <summary>Khối quảng cáo (node <c>Section -Ads</c>).</summary>
        [Header("Ads config")]
        [SerializeField] private GameObject adsSection;

        /// <summary>Khối đếm số quảng cáo đã xem (node <c>Section -Ads/WatchAdButton/AdCounts</c>).</summary>
        [SerializeField] private GameObject adCountSection;

        /// <summary>Nút xem quảng cáo (node <c>Section -Ads/WatchAdButton</c>).</summary>
        [SerializeField] private Button watchAdButton;

        /// <summary>Ô chữ "đã xem / tổng" (node <c>Section -Ads/WatchAdButton/AdCounts/Text - AdCount</c>).</summary>
        [SerializeField] private TextMeshProUGUI adsCountText;

        /// <summary>Ô chữ đếm ngược tới lần làm mới lượt quảng cáo (node <c>Section -Ads/AdResetTimeTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI adsResetTimeText;

        /// <summary>Ô chữ tiến độ quảng cáo dạng phần trăm; layout gốc không có.</summary>
        [SerializeField] private TextMeshProUGUI adsProgressText;

        /// <summary>Nút xem chi tiết nội dung túi (node <c>InfoBtn</c>).</summary>
        [Header("Info Button")]
        [SerializeField] private Button infoButton;

        /// <summary>Phát khi người chơi bấm mua và đã trả tiền thành công.</summary>
        public event Action<Kitbag> OnPurchased;

        /// <summary>Phát khi người chơi bấm nút xem quảng cáo.</summary>
        public event Action<Kitbag> OnWatchAdRequested;

        /// <summary>Túi đồ đang gắn vào thẻ.</summary>
        public Kitbag BoundKitbag => kitbagData;

        private GameData gameData;
        private bool wired;

        /// <summary>Gắn một túi đồ vào thẻ và vẽ lại toàn bộ widget.</summary>
        /// <param name="kitbag">Túi đồ cần bán; null sẽ ẩn thẻ.</param>
        public void Bind(Kitbag kitbag)
        {
            SetData(kitbag, GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null);
        }

        /// <summary>Gắn túi đồ kèm nguồn dữ liệu người chơi để kiểm tra đủ tiền.</summary>
        /// <param name="kitbag">Túi đồ cần bán; null sẽ ẩn thẻ.</param>
        /// <param name="data">Dữ liệu người chơi; null sẽ tự lấy từ <see cref="GameBootstrap"/>.</param>
        public void SetData(Kitbag kitbag, GameData data)
        {
            Wire();

            kitbagData = kitbag;
            gameData = data ?? (GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null);

            if (kitbag == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Vẽ lại tên, giá, mô tả và khối quảng cáo theo trạng thái hiện tại.</summary>
        public void Refresh()
        {
            if (kitbagData == null) return;

            if (kitbagNameText != null)
            {
                kitbagNameText.text = string.IsNullOrEmpty(kitbagData.kitbagName)
                    ? kitbagData.kitbagType.ToString()
                    : kitbagData.kitbagName;
            }

            if (kitbagImage != null)
            {
                if (kitbagData.kitbagImage != null) kitbagImage.sprite = kitbagData.kitbagImage;
                kitbagImage.enabled = kitbagImage.sprite != null;
            }

            if (bagItemsText != null) bagItemsText.text = KitbagRewardTextHelper.GetKitbagContentsText(kitbagData);

            if (coinsText != null)
            {
                coinsText.text = KitbagRewardTextHelper.GetTazoRangeText(kitbagData);
            }

            RefreshPrice();
            RefreshShowcase();
            UpdateAdsProgress();
        }

        /// <summary>Cập nhật giá và trạng thái bấm được của nút mua.</summary>
        public void RefreshPrice()
        {
            if (kitbagData == null) return;

            bool payWithGems = kitbagData.costInCoins <= 0 && kitbagData.costInGems > 0;
            int price = payWithGems ? kitbagData.costInGems : kitbagData.costInCoins;

            if (kitbagPriceText != null) kitbagPriceText.text = price > 0 ? Utilities.FormatCount(price) : "FREE";

            if (coinButtonContent != null) coinButtonContent.SetActive(price > 0);
            if (adsButtonContent != null) adsButtonContent.SetActive(price <= 0);

            if (buyButton != null) buyButton.interactable = CanAfford(price, payWithGems);
        }

        /// <summary>Cập nhật khối tiến độ quảng cáo.</summary>
        /// <param name="isSetData">True khi gọi ngay sau <see cref="SetData"/> (bỏ qua hiệu ứng).</param>
        public void UpdateAdsProgress(bool isSetData = false)
        {
            if (kitbagData == null) return;

            bool hasAds = kitbagData.totalAds > 0;

            if (adsSection != null) adsSection.SetActive(hasAds);
            if (adCountSection != null) adCountSection.SetActive(hasAds);
            if (watchAdButton != null) watchAdButton.interactable = hasAds && kitbagData.isAdAvailable;

            int watched = Mathf.Clamp(kitbagData.watchedAds, 0, Mathf.Max(0, kitbagData.totalAds));

            if (adsCountText != null) adsCountText.text = watched + "/" + kitbagData.totalAds;

            if (adsProgressText != null)
            {
                float ratio = hasAds ? (float)watched / kitbagData.totalAds : 0f;
                adsProgressText.text = Mathf.RoundToInt(ratio * 100f) + "%";
            }

            if (adsResetTimeText != null) adsResetTimeText.gameObject.SetActive(false);
        }

        private void RefreshShowcase()
        {
            List<Sprite> sprites = kitbagData != null ? kitbagData.rewardShowcaseImages : null;
            int count = sprites != null ? sprites.Count : 0;

            for (int i = 0; i < rewardShowcaseImages.Count; i++)
            {
                Image image = rewardShowcaseImages[i];
                if (image == null) continue;

                bool used = i < count && sprites[i] != null;
                image.enabled = used;
                if (used) image.sprite = sprites[i];
            }
        }

        private bool CanAfford(int price, bool payWithGems)
        {
            if (price <= 0) return true;
            if (gameData == null) return false;

            return payWithGems ? gameData.totalGems >= price : gameData.totalCoins >= price;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAdBtnClick);
            if (infoButton != null) infoButton.onClick.AddListener(OnInfoButtonClicked);
        }

        /// <summary>Trả tiền mở túi rồi khoe phần thưởng vừa quay được.</summary>
        public void OnBuy()
        {
            if (kitbagData == null) return;
            if (gameData == null) gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            if (gameData == null) return;

            bool payWithGems = kitbagData.costInCoins <= 0 && kitbagData.costInGems > 0;
            int price = payWithGems ? kitbagData.costInGems : kitbagData.costInCoins;

            if (price > 0)
            {
                bool paid = payWithGems ? gameData.TrySpendGems(price) : gameData.TrySpendCoins(price);
                if (!paid)
                {
                    if (ToastHandler.HasInstance) ToastHandler.Instance.Show(payWithGems ? "Not enough gems" : "Not enough coins");
                    return;
                }
            }

            GrantRewards();
            RefreshPrice();

            OnPurchased?.Invoke(kitbagData);
        }

        /// <summary>Ghi nhận một lượt xem quảng cáo và mở túi khi đủ số lượt.</summary>
        public void OnWatchAdBtnClick()
        {
            if (kitbagData == null) return;

            OnWatchAdRequested?.Invoke(kitbagData);

            // Không có SDK quảng cáo: coi như lượt xem đã thành công.
            kitbagData.watchedAds = Mathf.Min(kitbagData.watchedAds + 1, Mathf.Max(1, kitbagData.totalAds));

            if (kitbagData.totalAds > 0 && kitbagData.watchedAds >= kitbagData.totalAds)
            {
                kitbagData.watchedAds = 0;
                GrantRewards();
            }

            UpdateAdsProgress();
        }

        private void OnInfoButtonClicked()
        {
            if (kitbagData == null) return;
            if (!StarterKit.UIKit.UIController.HasInstance) return;

            StarterKit.UIKit.UIController.Instance.Show(
                ScreenType.AlertPopup,
                new AlertData(
                    string.IsNullOrEmpty(kitbagData.kitbagName) ? kitbagData.kitbagType.ToString() : kitbagData.kitbagName,
                    KitbagRewardTextHelper.GetKitbagContentsText(kitbagData)));
        }

        private void GrantRewards()
        {
            if (!RewardManager.HasInstance) return;

            List<CollectibleReward> rewards = RewardManager.Instance.GenerateKitbagRewards(kitbagData.kitbagType);
            if (rewards == null || rewards.Count == 0) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] != null) rewards[i].GrantReward(gameData);
            }

            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.RewardUI, rewards);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuy);
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAdBtnClick);
            if (infoButton != null) infoButton.onClick.RemoveListener(OnInfoButtonClicked);

            OnPurchased = null;
            OnWatchAdRequested = null;
        }
    }
}
