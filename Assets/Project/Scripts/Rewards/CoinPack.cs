using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Gói coin bán trong shop, thanh toán bằng gem (tiền tệ cứng trong game).
    /// Gói giá 0 gem là gói thưởng miễn phí/xem quảng cáo.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinPack", menuName = "ScriptableObjects/CoinPack", order = 1)]
    public class CoinPack : ScriptableObject
    {
        /// <summary>Tên hiển thị của gói.</summary>
        public string packName;

        /// <summary>Ảnh biểu tượng hiển thị trên thẻ gói trong shop.</summary>
        public Sprite ItemSprite;

        /// <summary>Giá của gói tính bằng gem.</summary>
        public int costInGems;

        /// <summary>Số coin nhận được khi mua gói.</summary>
        public int coinsProvided;
    }
}
