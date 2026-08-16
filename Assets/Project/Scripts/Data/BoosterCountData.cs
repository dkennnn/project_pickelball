using System;

namespace Pickleball.Data
{
    /// <summary>Số lượng booster người chơi đang sở hữu của một loại.</summary>
    [Serializable]
    public class BoosterCountData
    {
        /// <summary>Loại booster.</summary>
        public BoosterType boosterType;

        /// <summary>Số lượng đang có trong kho.</summary>
        public int count;
    }
}
