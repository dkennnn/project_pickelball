using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Danh mục thông tin hiển thị của toàn bộ chỉ số.
    /// Thứ tự trong <see cref="properties"/> cũng là thứ tự UI vẽ các thanh chỉ số.
    /// </summary>
    [CreateAssetMenu(fileName = "PropertiesData", menuName = "ScriptableObjects/PropertiesData")]
    public class PropertiesData : ScriptableObject
    {
        /// <summary>Danh sách chỉ số kèm tên và biểu tượng, theo thứ tự hiển thị.</summary>
        public List<PropertyData> properties;

        /// <summary>Tra thông tin hiển thị của một chỉ số. Trả về <c>null</c> nếu chưa cấu hình.</summary>
        /// <param name="propertyType">Loại chỉ số cần tra cứu.</param>
        public PropertyData GetPropertyData(PropertyType propertyType)
        {
            if (properties == null) return null;

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyData data = properties[i];
                if (data != null && data.propertyType == propertyType) return data;
            }
            return null;
        }
    }
}
