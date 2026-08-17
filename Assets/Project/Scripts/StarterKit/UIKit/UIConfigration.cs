using System;
using System.Collections.Generic;
using Pickleball;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Bảng khai báo tính chất của từng màn hình (popup hay không, có cho Back hay không).
    /// Dùng để đối chiếu với code lúc <see cref="UIController"/> đăng ký màn hình.
    /// </summary>
    [CreateAssetMenu(fileName = "UIConfigration", menuName = "ScriptableObjects/UI/UIConfigration")]
    public class UIConfigration : ScriptableObject
    {
        /// <summary>Khai báo tính chất của một màn hình.</summary>
        [Serializable]
        public class ScreenEntry
        {
            /// <summary>Định danh màn hình.</summary>
            public ScreenType screenType;

            /// <summary>True nếu màn hình chồng lên màn hình dưới thay vì thay thế.</summary>
            public bool isPopup;

            /// <summary>True nếu cho phép đóng bằng phím Back/Escape.</summary>
            public bool canGoBack = true;
        }

        /// <summary>Danh sách khai báo cho toàn bộ màn hình.</summary>
        public List<ScreenEntry> screens = new List<ScreenEntry>();

        /// <summary>Tra khai báo của một màn hình; trả về null nếu chưa khai báo.</summary>
        /// <param name="screenType">Định danh màn hình cần tra.</param>
        public ScreenEntry Get(ScreenType screenType)
        {
            if (screens == null) return null;

            for (int i = 0; i < screens.Count; i++)
            {
                ScreenEntry entry = screens[i];
                if (entry != null && entry.screenType == screenType) return entry;
            }
            return null;
        }
    }
}
