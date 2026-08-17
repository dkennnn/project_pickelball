using System;
using System.Collections.Generic;
using ProfanityFilter.Interfaces;

namespace ProfanityFilter
{
    /// <summary>
    /// Danh sách từ được phép mặc định: lưu trong <see cref="HashSet{T}"/> chữ thường,
    /// tra O(1), không phân biệt hoa thường.
    /// </summary>
    public class AllowList : IAllowList
    {
        private readonly HashSet<string> allowList = new HashSet<string>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public int Count => allowList.Count;

        /// <inheritdoc/>
        public IReadOnlyList<string> ToList
        {
            get
            {
                var list = new List<string>(allowList);
                list.Sort(StringComparer.Ordinal);
                return list;
            }
        }

        /// <inheritdoc/>
        public void Add(string wordToAllowlist)
        {
            string normalized = Normalize(wordToAllowlist);
            if (normalized == null) return;
            allowList.Add(normalized);
        }

        /// <inheritdoc/>
        public bool Contains(string wordToCheck)
        {
            string normalized = Normalize(wordToCheck);
            return normalized != null && allowList.Contains(normalized);
        }

        /// <inheritdoc/>
        public bool Remove(string wordToRemove)
        {
            string normalized = Normalize(wordToRemove);
            return normalized != null && allowList.Remove(normalized);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            allowList.Clear();
        }

        /// <summary>Chuẩn hoá về chữ thường, bỏ khoảng trắng thừa; trả null nếu chuỗi rỗng.</summary>
        /// <param name="value">Chuỗi gốc.</param>
        internal static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToLowerInvariant();
        }
    }
}
