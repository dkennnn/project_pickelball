using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Gói gem bán bằng tiền thật (IAP). Bản chơi thử chưa nối IAP nên các trường
    /// <see cref="costInUSD"/> và <see cref="iapID"/> chỉ dùng để hiển thị/đối chiếu.
    /// </summary>
    [CreateAssetMenu(fileName = "GemPack", menuName = "ScriptableObjects/GemPack", order = 1)]
    public class GemPack : ScriptableObject
    {
        /// <summary>Tên hiển thị của gói.</summary>
        public string packName;

        /// <summary>Ảnh biểu tượng hiển thị trên thẻ gói trong shop.</summary>
        public Sprite ItemSprite;

        /// <summary>Giá hiển thị dạng chuỗi. Gói mở bằng quảng cáo dùng chuỗi "ADS".</summary>
        public string costInUSD;

        /// <summary>Số gem nhận được khi mua gói.</summary>
        public int gemsProvided;

        /// <summary>Mã sản phẩm IAP tương ứng trên store.</summary>
        public string iapID;
    }
}
