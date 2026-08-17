using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Skin bóng: ngoài phần dùng chung của <see cref="ShopItem"/> còn có
    /// màu vệt bay riêng cho trail của bóng.
    /// </summary>
    [Serializable]
    public class BallShopItem : ShopItem
    {
        /// <summary>Màu vệt bay của bóng khi skin này được trang bị.</summary>
        public Color trailColor = Color.white;
    }
}
