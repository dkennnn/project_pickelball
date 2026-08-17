using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ProfanityFilter.Interfaces;

// Lớp ProfanityFilter có property tên `AllowList`, che mất tên kiểu `AllowList` bên trong thân lớp.
// Bí danh này giúp gọi được constructor của lớp AllowList mà không phải viết `global::`.
using AllowListImpl = ProfanityFilter.AllowList;

namespace ProfanityFilter
{
    /// <summary>
    /// Bộ lọc từ ngữ thô tục cho tên người chơi / tên phòng LAN.
    /// <para>
    /// CẢNH BÁO: bộ từ cấm mặc định trong <see cref="DefaultProfanities"/> chỉ là **bộ mẫu tối thiểu**
    /// (~30 từ tiếng Anh phổ thông) đủ để hệ thống chạy và test. Trước khi phát hành phải thay bằng
    /// bộ đầy đủ, có phân loại theo mức độ và bổ sung tiếng Việt cùng các biến thể viết lách
    /// (leetspeak, chèn ký tự…). Nạp bộ đầy đủ qua constructor nhận danh sách hoặc
    /// <see cref="AddProfanity"/>.
    /// </para>
    /// </summary>
    public class ProfanityFilter : IProfanityFilter
    {
        /// <summary>
        /// Bộ từ cấm mẫu — tự soạn, KHÔNG lấy từ bản gốc. Chỉ đủ dùng cho dev/test.
        /// </summary>
        private static readonly string[] DefaultProfanities =
        {
            "arse", "arsehole", "ass", "asshole", "bastard", "bitch", "bollocks", "bugger",
            "bullshit", "cock", "crap", "cunt", "damn", "dick", "dickhead", "dipshit",
            "douche", "dumbass", "fuck", "fucker", "fucking", "goddamn", "jackass", "jerkoff",
            "motherfucker", "nigga", "nigger", "piss", "prick", "pussy", "retard", "shit",
            "slut", "twat", "wanker", "whore"
        };

        private readonly HashSet<string> profanities = new HashSet<string>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public IAllowList AllowList { get; }

        /// <inheritdoc/>
        public int Count => profanities.Count;

        /// <summary>Khởi tạo với bộ từ cấm mẫu mặc định.</summary>
        public ProfanityFilter() : this(DefaultProfanities)
        {
        }

        /// <summary>Khởi tạo với một bộ từ cấm tự cung cấp.</summary>
        /// <param name="profanityList">Danh sách từ cấm; null thì bộ lọc khởi tạo rỗng.</param>
        public ProfanityFilter(IEnumerable<string> profanityList)
        {
            AllowList = new AllowListImpl();

            if (profanityList == null) return;
            foreach (string word in profanityList) AddProfanity(word);
        }

        /// <inheritdoc/>
        public bool IsProfanity(string word)
        {
            string normalized = Normalize(word);
            if (normalized == null) return false;
            if (AllowList.Contains(normalized)) return false;
            return profanities.Contains(normalized);
        }

        /// <inheritdoc/>
        public bool ContainsProfanity(string term)
        {
            foreach (string _ in DetectAllProfanities(term)) return true;
            return false;
        }

        /// <inheritdoc/>
        public IEnumerable<string> DetectAllProfanities(string sentence)
        {
            var found = new List<string>();
            if (string.IsNullOrWhiteSpace(sentence)) return found;

            string lower = sentence.ToLowerInvariant();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1. Khớp theo từ nguyên vẹn — bắt trường hợp phổ biến nhất.
            foreach (string token in Tokenize(lower))
            {
                if (AllowList.Contains(token)) continue;
                if (!profanities.Contains(token)) continue;
                if (seen.Add(token)) found.Add(token);
            }

            // 2. Khớp cụm nhiều từ và các từ bị viết dính (ví dụ "youfuckhead").
            foreach (string profanity in profanities)
            {
                if (seen.Contains(profanity)) continue;
                if (AllowList.Contains(profanity)) continue;
                if (lower.IndexOf(profanity, StringComparison.Ordinal) < 0) continue;
                if (seen.Add(profanity)) found.Add(profanity);
            }

            return found;
        }

        /// <inheritdoc/>
        public string CensorString(string sentence, char censorChar = '*')
        {
            if (string.IsNullOrEmpty(sentence)) return sentence;

            string result = sentence;

            foreach (string profanity in DetectAllProfanities(sentence))
            {
                string replacement = new string(censorChar, profanity.Length);
                // \b không hoạt động với cụm chứa khoảng trắng ở hai đầu, nên chỉ dùng cho từ đơn.
                string pattern = profanity.IndexOf(' ') >= 0
                    ? Regex.Escape(profanity)
                    : @"\b" + Regex.Escape(profanity) + @"\b";

                string censored = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);

                // Từ bị dính liền chữ khác thì \b không khớp — hạ xuống thay chuỗi con.
                if (string.Equals(censored, result, StringComparison.Ordinal))
                    censored = Regex.Replace(result, Regex.Escape(profanity), replacement, RegexOptions.IgnoreCase);

                result = censored;
            }

            return result;
        }

        /// <inheritdoc/>
        public void AddProfanity(string profanity)
        {
            string normalized = Normalize(profanity);
            if (normalized == null) return;
            profanities.Add(normalized);
        }

        /// <summary>Thêm nhiều từ cấm cùng lúc.</summary>
        /// <param name="profanityList">Danh sách từ; null thì bỏ qua.</param>
        public void AddProfanity(IEnumerable<string> profanityList)
        {
            if (profanityList == null) return;
            foreach (string word in profanityList) AddProfanity(word);
        }

        /// <inheritdoc/>
        public bool RemoveProfanity(string profanity)
        {
            string normalized = Normalize(profanity);
            return normalized != null && profanities.Remove(normalized);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            profanities.Clear();
        }

        /// <summary>Chuẩn hoá về chữ thường, bỏ khoảng trắng thừa; trả null nếu chuỗi rỗng.</summary>
        /// <param name="value">Chuỗi gốc.</param>
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToLowerInvariant();
        }

        /// <summary>Tách câu thành các từ, coi mọi ký tự không phải chữ/số là dấu phân cách.</summary>
        /// <param name="lowerSentence">Câu đã hạ về chữ thường.</param>
        private static IEnumerable<string> Tokenize(string lowerSentence)
        {
            var buffer = new StringBuilder();

            for (int i = 0; i < lowerSentence.Length; i++)
            {
                char c = lowerSentence[i];
                if (char.IsLetterOrDigit(c))
                {
                    buffer.Append(c);
                    continue;
                }

                if (buffer.Length == 0) continue;
                yield return buffer.ToString();
                buffer.Clear();
            }

            if (buffer.Length > 0) yield return buffer.ToString();
        }
    }
}
