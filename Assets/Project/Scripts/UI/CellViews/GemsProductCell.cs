using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ bán một gói gem (prefab <c>GemsPackCellViewv2</c>). Gói gem là hàng IAP thật —
    /// ngoài phạm vi bản dựng lại — nên nút mua chỉ phát <see cref="OnPurchaseRequested"/>
    /// cho tầng ngoài xử lý, không tự cộng gem.
    /// </summary>
    public class GemsProductCell : MonoBehaviour
    {
        /// <summary>Gói gem đang gắn vào thẻ.</summary>
        [SerializeField] private GemPack gemPack;

        /// <summary>Số gem của gói (node <c>GemsText</c>).</summary>
        [SerializeField] private TextMeshProUGUI gemsCountText;

        /// <summary>Ảnh gói gem (node <c>GemsImage</c>).</summary>
        [SerializeField] private Image gemsImage;

        /// <summary>Nút mua (node <c>BuyButton</c>).</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Khối giá tiền thật (node <c>BuyButton/Currency</c>).</summary>
        [SerializeField] private GameObject currencyButtonContent;

        /// <summary>Khối mở bằng quảng cáo (node <c>BuyButton/Ads</c>).</summary>
        [SerializeField] private GameObject adsButtonContent;

        /// <summary>Ô chữ giá (node <c>BuyButton/Currency/PriceText</c>).</summary>
        [SerializeField] private TextMeshProUGUI gemsPriceText;

        /// <summary>Ô chữ số lượt quảng cáo (node <c>BuyButton/Ads/AdsPriceText</c>).</summary>
        [SerializeField] private TextMeshProUGUI adsCountText;

        /// <summary>Chuỗi giá quy ước cho gói mở bằng quảng cáo thay vì tiền thật.</summary>
        [SerializeField] private string adsPriceKeyword = "ADS";

        /// <summary>Phát khi người chơi bấm mua; tầng ngoài tự nối IAP nếu có.</summary>
        public event Action<GemPack> OnPurchaseRequested;

        /// <summary>Gói gem đang gắn vào thẻ.</summary>
        public GemPack BoundPack => gemPack;

        private bool wired;

        /// <summary>Gắn một gói gem vào thẻ.</summary>
        /// <param name="pack">Gói gem cần bán; null sẽ ẩn thẻ.</param>
        public void Bind(GemPack pack)
        {
            SetData(pack);
        }

        /// <summary>Gắn một gói gem vào thẻ và vẽ lại widget.</summary>
        /// <param name="pack">Gói gem cần bán; null sẽ ẩn thẻ.</param>
        public void SetData(GemPack pack)
        {
            Wire();

            gemPack = pack;

            if (pack == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Vẽ lại số gem và giá hiển thị.</summary>
        public void Refresh()
        {
            if (gemPack == null) return;

            if (gemsCountText != null) gemsCountText.text = Utilities.FormatCount(gemPack.gemsProvided);

            if (gemsImage != null)
            {
                if (gemPack.ItemSprite != null) gemsImage.sprite = gemPack.ItemSprite;
                gemsImage.enabled = gemsImage.sprite != null;
            }

            bool isAdPack = string.IsNullOrEmpty(gemPack.costInUSD)
                || string.Equals(gemPack.costInUSD, adsPriceKeyword, StringComparison.OrdinalIgnoreCase);

            if (gemsPriceText != null) gemsPriceText.text = isAdPack ? adsPriceKeyword : gemPack.costInUSD;
            if (adsCountText != null) adsCountText.text = adsPriceKeyword;

            if (currencyButtonContent != null) currencyButtonContent.SetActive(!isAdPack);
            if (adsButtonContent != null) adsButtonContent.SetActive(isAdPack);
        }

        /// <summary>Yêu cầu mua gói gem. Bản offline chỉ phát sự kiện và báo toast.</summary>
        public void OnBuy()
        {
            if (gemPack == null) return;

            Action<GemPack> handler = OnPurchaseRequested;
            if (handler != null)
            {
                try { handler.Invoke(gemPack); }
                catch (Exception e) { Debug.LogException(e, this); }
                return;
            }

            if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Purchases are not available in this build");
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
            OnPurchaseRequested = null;
        }
    }
}
