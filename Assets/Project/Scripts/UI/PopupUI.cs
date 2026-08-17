using StarterKit.UIKit;

namespace Pickleball
{
    /// <summary>
    /// Lớp cha của mọi popup: chồng lên màn hình đang mở thay vì thay thế nó.
    /// </summary>
    public abstract class PopupUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override bool IsPopup => true;
    }
}
