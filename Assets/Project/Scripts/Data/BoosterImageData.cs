using System;
using UnityEngine;

namespace Pickleball.Data
{
    /// <summary>Ảnh biểu tượng UI của một loại booster.</summary>
    [Serializable]
    public class BoosterImageData
    {
        /// <summary>Loại booster.</summary>
        public BoosterType boosterType;

        /// <summary>Ảnh biểu tượng hiển thị trên UI.</summary>
        public Sprite image;
    }
}
