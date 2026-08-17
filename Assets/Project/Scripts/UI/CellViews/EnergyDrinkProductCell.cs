using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Thẻ bán nước tăng lực trong cửa hàng: tên, ảnh, số đang có và giá coin.
    /// Mua qua <see cref="Shop.BuyEnergyDrink"/>.
    /// </summary>
    public class EnergyDrinkProductCell : MonoBehaviour
    {
        /// <summary>Tên sản phẩm.</summary>
        [SerializeField] private TextMeshProUGUI nameText;

        /// <summary>Ảnh chai nước tăng lực.</summary>
        [SerializeField] private Image iconImage;

        /// <summary>Ô chữ giá coin.</summary>
        [SerializeField] private TextMeshProUGUI priceText;

        /// <summary>Ô chữ số lượng đang có.</summary>
        [SerializeField] private TextMeshProUGUI ownedText;

        /// <summary>Nút mua.</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Tên hiển thị mặc định.</summary>
        [SerializeField] private string displayName = "ENERGY DRINK";

        /// <summary>Phát sau khi mua thành công; tham số là số chai đang có sau giao dịch.</summary>
        public event Action<int> OnPurchased;

        /// <summary>Số chai đang hiển thị.</summary>
        public int Owned { get; private set; }

        /// <summary>Giá coin đang hiển thị.</summary>
        public int Cost { get; private set; }

        private bool wired;

        /// <summary>Gắn số lượng đang có và giá vào thẻ.</summary>
        /// <param name="owned">Số chai người chơi đang có.</param>
        /// <param name="cost">Giá một chai, tính bằng coin.</param>
        public void Bind(int owned, int cost)
        {
            Wire();

            Owned = Mathf.Max(0, owned);
            Cost = Mathf.Max(0, cost);

            if (nameText != null) nameText.text = displayName;
            if (priceText != null) priceText.text = Utilities.FormatCount(Cost);
            if (ownedText != null) ownedText.text = "x" + Utilities.FormatCount(Owned);
            if (iconImage != null) iconImage.enabled = iconImage.sprite != null;

            RefreshInteractable();
        }

        /// <summary>Đọc lại số lượng và giá thẳng từ <see cref="Shop"/> của người chơi.</summary>
        /// <param name="data">Dữ liệu người chơi; null sẽ tự lấy từ <see cref="GameBootstrap"/>.</param>
        public void SetData(GameData data)
        {
            GameData gameData = data ?? (GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null);
            Shop shop = gameData != null ? gameData.shopData : null;

            if (shop == null)
            {
                Bind(0, 0);
                return;
            }

            Bind(shop.energyDrinks, shop.energyDrinkCost);
        }

        /// <summary>Mua một chai nước tăng lực bằng coin.</summary>
        public void OnBuy()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            if (shop == null) return;

            if (!shop.BuyEnergyDrink())
            {
                if (ToastHandler.HasInstance) ToastHandler.Instance.Show("Not enough coins");
                return;
            }

            Bind(shop.energyDrinks, shop.energyDrinkCost);
            OnPurchased?.Invoke(Owned);
        }

        private void RefreshInteractable()
        {
            if (buyButton == null) return;

            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            buyButton.interactable = gameData != null && gameData.totalCoins >= Cost;
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
