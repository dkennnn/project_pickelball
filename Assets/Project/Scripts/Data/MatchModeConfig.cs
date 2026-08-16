using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Cấu hình hiển thị và điều kiện mở khoá của một chế độ trận đấu.
    /// Mỗi <see cref="MatchModeType"/> có đúng một cấu hình trong <see cref="GameSettings.matchModeConfigs"/>.
    /// </summary>
    [Serializable]
    public class MatchModeConfig
    {
        /// <summary>Chế độ trận đấu mà cấu hình này mô tả.</summary>
        public MatchModeType modeType;

        /// <summary>Ảnh nút của chế độ này trên màn hình chính.</summary>
        public Sprite mainMenuButtonSprite;

        /// <summary>Ảnh của chế độ này trong popup chọn chế độ.</summary>
        public Sprite modeSelectionSprite;

        /// <summary>Tiêu đề hiển thị trong popup thông tin.</summary>
        public string infoTitle;

        /// <summary>Mô tả chi tiết hiển thị trong popup thông tin.</summary>
        [TextArea(3, 6)]
        public string infoDescription;

        /// <summary>Level người chơi tối thiểu để mở khoá chế độ (0 = mở sẵn).</summary>
        public int unlockLevel;

        /// <summary>Bật khi chế độ chưa phát hành (hiển thị nhãn "Coming Soon" và không cho chơi).</summary>
        public bool isComingSoon;
    }
}
