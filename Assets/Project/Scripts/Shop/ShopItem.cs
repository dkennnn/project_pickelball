using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một món đồ ngoại hình bán trong shop (skin bóng, skin vợt, sân...).
    /// Chỉ mang dữ liệu hiển thị và trạng thái sở hữu, không ảnh hưởng chỉ số gameplay.
    /// </summary>
    [Serializable]
    public class ShopItem
    {
        /// <summary>Ảnh biểu tượng dùng cho UI Image.</summary>
        public Sprite icon;

        /// <summary>Ảnh dạng Texture dùng cho RawImage hoặc preview 3D.</summary>
        public Texture iconTexture;

        /// <summary>Material áp lên model khi món đồ được trang bị.</summary>
        public Material material;

        /// <summary>Giá mua bằng coin.</summary>
        public int cost;

        /// <summary>True khi người chơi đã sở hữu món đồ này.</summary>
        public bool isPurchased;
    }
}
