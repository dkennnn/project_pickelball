using System;

namespace Pickleball
{
    /// <summary>Số tazo (mảnh nâng cấp) đang có của một loại vật phẩm.</summary>
    [Serializable]
    public class TazoCountData
    {
        /// <summary>Loại vật phẩm mà kho tazo này phục vụ.</summary>
        public ShopItemType ShopItemType;

        /// <summary>Số tazo đang sở hữu.</summary>
        public int count;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public TazoCountData() { }

        /// <summary>Khởi tạo kho tazo cho một loại vật phẩm.</summary>
        /// <param name="shopItemType">Loại vật phẩm.</param>
        /// <param name="count">Số tazo ban đầu.</param>
        public TazoCountData(ShopItemType shopItemType, int count)
        {
            ShopItemType = shopItemType;
            this.count = count;
        }
    }
}
