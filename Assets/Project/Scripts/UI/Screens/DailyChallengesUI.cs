namespace Pickleball
{
    /// <summary>
    /// Bí danh của <see cref="DailyChallengeUI"/> theo đúng tên trong bản gốc
    /// (<c>StarterKit.UI.DailyChallengesUI</c> — số nhiều).
    /// <para>
    /// Layout <c>DailyChallengeUI.json</c> khai báo script tên số nhiều, còn class đã viết
    /// trước trong project mang tên số ít, và bộ tra type của importer chỉ khớp mờ theo
    /// hoa/thường chứ không xử lý khác biệt số ít/số nhiều. Lớp mỏng này tồn tại để importer
    /// gắn được component vào prefab mà không phải nhân đôi logic, cũng không phải đổi tên
    /// class đã có (nhiều nơi khác đang tham chiếu).
    /// </para>
    /// <para>
    /// Chỉ gắn MỘT trong hai component lên prefab: cả hai cùng đăng ký
    /// <see cref="ScreenType.DailyChallengeUI"/>.
    /// </para>
    /// </summary>
    public class DailyChallengesUI : DailyChallengeUI
    {
    }
}
