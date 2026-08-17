using System.Collections.Generic;

namespace ProfanityFilter.Interfaces
{
    /// <summary>
    /// Bộ lọc từ ngữ thô tục dùng cho tên người chơi, tên phòng LAN và mọi chỗ người chơi nhập chữ.
    /// </summary>
    public interface IProfanityFilter
    {
        /// <summary>Danh sách từ được phép, luôn thắng bộ từ cấm.</summary>
        IAllowList AllowList { get; }

        /// <summary>Số từ cấm đang có.</summary>
        int Count { get; }

        /// <summary>Một từ đơn có phải từ cấm không (không phân biệt hoa thường).</summary>
        /// <param name="word">Từ cần kiểm tra.</param>
        bool IsProfanity(string word);

        /// <summary>Trong câu có chứa từ cấm nào không.</summary>
        /// <param name="term">Câu hoặc cụm từ cần kiểm tra.</param>
        bool ContainsProfanity(string term);

        /// <summary>Liệt kê mọi từ cấm tìm thấy trong câu (đã loại trùng).</summary>
        /// <param name="sentence">Câu cần quét.</param>
        IEnumerable<string> DetectAllProfanities(string sentence);

        /// <summary>Thay mọi từ cấm trong câu bằng ký tự che.</summary>
        /// <param name="sentence">Câu gốc.</param>
        /// <param name="censorChar">Ký tự dùng để che, mặc định là dấu sao.</param>
        /// <returns>Câu đã được che; trả về chính chuỗi gốc nếu null/rỗng.</returns>
        string CensorString(string sentence, char censorChar = '*');

        /// <summary>Thêm một từ vào bộ từ cấm.</summary>
        /// <param name="profanity">Từ hoặc cụm từ cần cấm.</param>
        void AddProfanity(string profanity);

        /// <summary>Gỡ một từ khỏi bộ từ cấm.</summary>
        /// <param name="profanity">Từ cần gỡ.</param>
        /// <returns>True nếu từ có trong bộ và đã được gỡ.</returns>
        bool RemoveProfanity(string profanity);

        /// <summary>Xoá sạch bộ từ cấm.</summary>
        void Clear();
    }
}
