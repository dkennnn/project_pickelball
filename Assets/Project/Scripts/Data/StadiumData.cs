using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Mô tả một sân đấu và khoảng level người chơi được sử dụng nó.</summary>
    [Serializable]
    public class StadiumData
    {
        /// <summary>Tên hiển thị của sân đấu.</summary>
        public string stadiumName;

        /// <summary>Tên scene Unity chứa sân đấu này.</summary>
        public string stadiumSceneName;

        /// <summary>Level người chơi thấp nhất được vào sân.</summary>
        public int minLevel;

        /// <summary>Level người chơi cao nhất còn dùng sân này.</summary>
        public int maxLevel;

        /// <summary>Ảnh xem trước của sân đấu trên UI.</summary>
        public Sprite stadiumSprite;
    }
}
