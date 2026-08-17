using System;

namespace Pickleball
{
    /// <summary>
    /// Khoảng giá trị mục tiêu cho một loại nhiệm vụ hằng ngày.
    /// Khi sinh nhiệm vụ, <c>DailyChallengeManager</c> bốc ngẫu nhiên một số nguyên
    /// trong đoạn [<see cref="MinValue"/>, <see cref="MaxValue"/>] làm mục tiêu.
    /// </summary>
    [Serializable]
    public class ChallengeRange
    {
        /// <summary>Loại nhiệm vụ mà khoảng này áp dụng.</summary>
        public ChallengeType Type;

        /// <summary>Giá trị mục tiêu nhỏ nhất (bao gồm).</summary>
        public int MinValue;

        /// <summary>Giá trị mục tiêu lớn nhất (bao gồm).</summary>
        public int MaxValue;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public ChallengeRange() { }

        /// <summary>Khởi tạo khoảng mục tiêu cho một loại nhiệm vụ.</summary>
        /// <param name="type">Loại nhiệm vụ.</param>
        /// <param name="minValue">Mục tiêu nhỏ nhất.</param>
        /// <param name="maxValue">Mục tiêu lớn nhất.</param>
        public ChallengeRange(ChallengeType type, int minValue, int maxValue)
        {
            Type = type;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
