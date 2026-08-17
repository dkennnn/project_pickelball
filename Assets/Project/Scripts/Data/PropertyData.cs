using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Thông tin hiển thị của một chỉ số: tên và biểu tượng dùng cho UI.</summary>
    [Serializable]
    public class PropertyData
    {
        /// <summary>Loại chỉ số.</summary>
        public PropertyType propertyType;

        /// <summary>Tên hiển thị trên UI (ví dụ "AGILITY").</summary>
        public string propertyName;

        /// <summary>Biểu tượng hiển thị cạnh thanh chỉ số.</summary>
        public Sprite propertyIcon;
    }
}
