using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Đánh dấu một <see cref="UnityEngine.UI.Image"/> đang thiếu sprite gốc và giữ lại TÊN sprite
    /// của bản build gốc, để sau này thay art hàng loạt bằng menu
    /// <c>Pickleball/UI/Bind Sprites From Folder</c>.
    /// </summary>
    /// <remarks>
    /// Quy trình: thả file ảnh đúng tên vào <c>Assets/Project/Textures/UI/</c> rồi bấm menu trên.
    /// SpriteBinder sẽ gán sprite vào Image cùng node và xoá component đánh dấu này.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SpritePlaceholder : MonoBehaviour
    {
        /// <summary>Tên sprite trong bản build gốc — dùng làm khoá để tìm file art thay thế.</summary>
        public string originalSpriteName;

        /// <summary>Màu gốc của Image, giữ lại để đối chiếu khi art mới được gán.</summary>
        public Color fallbackColor = Color.white;
    }
}
