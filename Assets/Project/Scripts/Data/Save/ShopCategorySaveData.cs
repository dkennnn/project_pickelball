using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đã lưu của một tab shop: bậc nâng cấp và trạng thái sở hữu của từng vật phẩm,
    /// kèm vật phẩm đang trang bị.
    /// <para>
    /// Vật phẩm được khớp theo <see cref="ItemSaveData.itemName"/> chứ KHÔNG theo chỉ số, vì
    /// thứ tự asset trong <c>Shop.shopCategories</c> có thể đổi mỗi lần chạy lại generator.
    /// </para>
    /// </summary>
    [Serializable]
    public class ShopCategorySaveData
    {
        /// <summary>Loại vật phẩm của tab này (khoá để khớp với <c>ShopCategoryData.categoryType</c>).</summary>
        public ShopItemType categoryType;

        /// <summary>Trạng thái từng vật phẩm trong tab.</summary>
        public List<ItemSaveData> items;

        /// <summary>Tên vật phẩm đang trang bị; rỗng nghĩa là chưa chọn.</summary>
        public string selectedItemName;
    }

    /// <summary>Trạng thái đã lưu của một vật phẩm shop, khớp theo tên.</summary>
    [Serializable]
    public class ItemSaveData
    {
        /// <summary>Tên vật phẩm — khoá khớp với <c>Item.itemName</c>.</summary>
        public string itemName;

        /// <summary>Bậc nâng cấp đã đạt.</summary>
        public int currentLevel;

        /// <summary>Người chơi đã sở hữu vật phẩm này hay chưa.</summary>
        public bool isPurchased;

        /// <summary>Khởi tạo rỗng phục vụ serialize.</summary>
        public ItemSaveData() { }

        /// <summary>Khởi tạo trạng thái lưu của một vật phẩm.</summary>
        /// <param name="itemName">Tên vật phẩm.</param>
        /// <param name="currentLevel">Bậc nâng cấp.</param>
        /// <param name="isPurchased">Đã sở hữu hay chưa.</param>
        public ItemSaveData(string itemName, int currentLevel, bool isPurchased)
        {
            this.itemName = itemName;
            this.currentLevel = currentLevel;
            this.isPurchased = isPurchased;
        }
    }
}
