using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ bán một gói coin (prefab <c>CoinPackCellView</c>). Gói coin thanh toán bằng gem
    /// qua <see cref="Shop.BuyCoinPack"/>; gói giá 0 gem là gói mở bằng quảng cáo.
    /// </summary>
    public class CoinProductCell : MonoBehaviour
    {
        /// <summary>Gói coin đang gắn vào thẻ.</summary>
        [SerializeField] private CoinPack coinPack;

        /// <summary>Số coin của gói (node <c>CoinsText</c>).</summary>
        [SerializeField] private TextMeshProUGUI coinCountText;

        /// <summary>Ảnh gói coin (node <c>CoinImage</c>).</summary>
        [SerializeField] private Image coinImage;

        /// <summary>Nút mua (node <c>BuyButton</c>).</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Khối giá bằng gem (node <c>BuyButton/Currency</c>).</summary>
        [SerializeField] private GameObject currencyButtonContent;

        /// <summary>Khối mở bằng quảng cáo (node <c>BuyButton/Ads</c>).</summary>
        [SerializeField] private GameObject adsButtonContent;

        /// <summary>Ô chữ giá gem (node <c>BuyButton/Currency/PriceText</c>).</summary>
        [SerializeField] private TextMeshProUGUI coinPriceText;

        /// <summary>Ô chữ số lượt quảng cáo (node <c>BuyButton/Ads/AdsPriceText</c>).</summary>
        [SerializeField] private TextMeshProUGUI adsCountText;

        /// <summary>Phát sau khi mua thành công.</summary>
        public event Action<CoinPack> OnPurchased;

        /// <summary>Gói coin đang gắn vào thẻ.</summary>
        public CoinPack BoundPack => coinPack;

        private GameData gameData;
        private bool wired;

        /// <summary>Gắn một gói coin vào thẻ.</summary>
        /// <param name="pack">Gói coin cần bán; null sẽ ẩn thẻ.</param>
        public void Bind(CoinPack pack)
        {
            SetData(pack, GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null);
        }

        /// <summary>Gắn gói coin kèm nguồn dữ liệu người chơi.</summary>
        /// <param name="pack">Gói coin cần bán; null sẽ ẩn thẻ.</param>
        /// <param name="data">Dữ liệu người chơi; null sẽ tự lấy từ <see cref="GameBootstrap"/>.</param>
        public void SetData(CoinPack pack, GameData data)
        {
            Wire();

            coinPack = pack;
            gameData = data ?? (GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null);

            if (pack == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Vẽ lại số coin, giá và trạng thái bấm được của nút mua.</summary>
        public void Refresh()
        {
            if (coinPack == null) return;

            if (coinCountText != null) coinCountText.text = Utilities.FormatCount(coinPack.coinsProvided);

            if (coinImage != null)
            {
                if (coinPack.ItemSprite != null) coinImage.sprite = coinPack.ItemSprite;
                coinImage.enabled = coinImage.sprite != null;
            }

            bool paid = coinPack.costInGems > 0;

            if (coinPriceText != null) coinPriceText.text = Utilities.FormatCount(coinPack.costInGems);
            if (adsCountText != null) adsCountText.text = "FREE";

            if (currencyButtonContent != null) currencyButtonContent.SetActive(paid);
            if (adsButtonContent != null) adsButtonContent.SetActive(!paid);

            if (buyButton != null)
            {
                buyButton.interactable = !paid || (gameData != null && gameData.totalGems >= coinPack.costInGems);
            }
        }

        /// <summary>Mua gói coin bằng gem.</summary>
        public void OnBuy()
        {
            if (coinPack == null) return;
            if (gameData == null) gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;

            Shop shop = gameData != null ? gameData.shopData : null;
            if (shop == null) return;

            if (!shop.BuyCoinPack(coinPack))
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough gems");
                return;
            }

            Refresh();
            OnPurchased?.Invoke(coinPack);
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuy);
            OnPurchased = null;
        }
    }
}
