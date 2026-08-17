using StarterKit.UIKit;

namespace Pickleball
{
    /// <summary>
    /// Màn hình trống (bản gốc là "BlankUI"): chỉ có nền, dùng làm chỗ đệm khi cần
    /// che hoàn toàn giao diện mà không hiển thị gì.
    /// </summary>
    public class EmptyUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.Blank;

    }
}
