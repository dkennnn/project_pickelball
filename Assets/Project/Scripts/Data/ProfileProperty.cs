using System;

namespace Pickleball
{
    /// <summary>
    /// Một dòng chỉ số dùng cho UI: giá trị hiện tại, giá trị sau khi nâng cấp một bậc và giá trị trần.
    /// <para>
    /// UI vẽ thanh chỉ số theo tỉ lệ <see cref="currentValue"/>/<see cref="maxValue"/> và tô phần
    /// tăng thêm từ <see cref="currentValue"/> tới <see cref="nextLevelValue"/> khi xem trước nâng cấp.
    /// </para>
    /// </summary>
    [Serializable]
    public class ProfileProperty
    {
        /// <summary>Loại chỉ số.</summary>
        public PropertyType propertyType;

        /// <summary>Giá trị hiện tại (thang 0..100).</summary>
        public float currentValue;

        /// <summary>Giá trị sau khi nâng thêm một bậc; bằng <see cref="currentValue"/> nếu đã tối đa.</summary>
        public float nextLevelValue;

        /// <summary>Giá trị tại bậc cao nhất, dùng làm mốc 100% của thanh chỉ số.</summary>
        public float maxValue;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public ProfileProperty() { }

        /// <summary>Khởi tạo đầy đủ một dòng chỉ số.</summary>
        /// <param name="propertyType">Loại chỉ số.</param>
        /// <param name="currentValue">Giá trị hiện tại.</param>
        /// <param name="nextLevelValue">Giá trị ở bậc kế tiếp.</param>
        /// <param name="maxValue">Giá trị ở bậc cao nhất.</param>
        public ProfileProperty(PropertyType propertyType, float currentValue, float nextLevelValue, float maxValue)
        {
            this.propertyType = propertyType;
            this.currentValue = currentValue;
            this.nextLevelValue = nextLevelValue;
            this.maxValue = maxValue;
        }
    }
}
