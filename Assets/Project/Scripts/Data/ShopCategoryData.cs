using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Một tab trong shop: nhóm vật phẩm cùng loại kèm vật phẩm đang được trang bị.</summary>
    [Serializable]
    public class ShopCategoryData
    {
        /// <summary>Loại vật phẩm của tab này.</summary>
        public ShopItemType categoryType;

        /// <summary>Tên hiển thị của tab.</summary>
        public string categoryName;

        /// <summary>Biểu tượng của tab.</summary>
        public Sprite categoryIcon;

        /// <summary>Toàn bộ vật phẩm thuộc tab, theo thứ tự hiển thị.</summary>
        public List<Item> items;

        /// <summary>Vật phẩm đang được trang bị của tab này.</summary>
        public Item selectedItem;
    }
}
