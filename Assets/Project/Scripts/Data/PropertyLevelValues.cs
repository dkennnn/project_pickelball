using System;

namespace Pickleball
{
    /// <summary>
    /// Giá trị của một chỉ số theo từng bậc nâng cấp của item.
    /// <para>
    /// Mảng <see cref="value"/> luôn dài bằng số bậc (5 bậc: level 0..4), index chính là
    /// <see cref="Item.currentLevel"/>. Giá trị nằm trên thang 0..100 (chỉ số shop), chưa nhân
    /// trọng số <see cref="Item.propertiesEffectiveness"/> và chưa quy đổi qua
    /// <see cref="PlayerProfileLimits"/>.
    /// </para>
    /// </summary>
    [Serializable]
    public class PropertyLevelValues
    {
        /// <summary>Loại chỉ số mà mảng giá trị này mô tả.</summary>
        public PropertyType propertyType;

        /// <summary>Giá trị chỉ số (thang 0..100) tại từng bậc nâng cấp, index = currentLevel.</summary>
        public float[] value;
    }
}
