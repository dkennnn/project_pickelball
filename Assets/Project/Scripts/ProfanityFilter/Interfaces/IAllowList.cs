using System.Collections.Generic;

namespace ProfanityFilter.Interfaces
{
    /// <summary>
    /// Danh sách từ được phép — những từ tuy khớp bộ lọc nhưng vẫn cho qua
    /// (tên riêng, tên đội, từ vô hại trùng chuỗi con với từ cấm).
    /// </summary>
    public interface IAllowList
    {
        /// <summary>Số từ đang có trong danh sách.</summary>
        int Count { get; }

        /// <summary>Bản chụp chỉ-đọc của toàn bộ danh sách (đã chuẩn hoá về chữ thường).</summary>
        IReadOnlyList<string> ToList { get; }

        /// <summary>Thêm một từ vào danh sách được phép. Chuỗi rỗng/null bị bỏ qua.</summary>
        /// <param name="wordToAllowlist">Từ cần cho qua.</param>
        void Add(string wordToAllowlist);

        /// <summary>Từ này có nằm trong danh sách được phép không (không phân biệt hoa thường).</summary>
        /// <param name="wordToCheck">Từ cần kiểm tra.</param>
        bool Contains(string wordToCheck);

        /// <summary>Gỡ một từ khỏi danh sách.</summary>
        /// <param name="wordToRemove">Từ cần gỡ.</param>
        /// <returns>True nếu từ có trong danh sách và đã được gỡ.</returns>
        bool Remove(string wordToRemove);

        /// <summary>Xoá sạch danh sách.</summary>
        void Clear();
    }
}
