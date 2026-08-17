using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>
    /// Một bậc trình độ của người chơi kèm toàn bộ khoảng mục tiêu nhiệm vụ hằng ngày của bậc đó.
    /// Người chơi càng lên bậc cao thì mục tiêu nhiệm vụ càng nặng.
    /// </summary>
    [Serializable]
    public class ExperienceLevel
    {
        /// <summary>Bậc trình độ (dùng chung thang với độ khó AI).</summary>
        public AIDifficulty LevelName;

        /// <summary>Danh sách khoảng mục tiêu theo từng loại nhiệm vụ ở bậc này.</summary>
        public List<ChallengeRange> ranges = new List<ChallengeRange>();

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public ExperienceLevel() { }

        /// <summary>Khởi tạo một bậc trình độ với bảng khoảng mục tiêu cho trước.</summary>
        /// <param name="levelName">Bậc trình độ.</param>
        /// <param name="ranges">Bảng khoảng mục tiêu theo loại nhiệm vụ.</param>
        public ExperienceLevel(AIDifficulty levelName, List<ChallengeRange> ranges)
        {
            LevelName = levelName;
            this.ranges = ranges ?? new List<ChallengeRange>();
        }

        /// <summary>
        /// Tìm khoảng mục tiêu của một loại nhiệm vụ trong bậc này.
        /// Trả về <c>null</c> nếu bậc này không hỗ trợ loại nhiệm vụ đó.
        /// </summary>
        /// <param name="type">Loại nhiệm vụ cần tra cứu.</param>
        public ChallengeRange GetRange(ChallengeType type)
        {
            if (ranges == null) return null;

            for (int i = 0; i < ranges.Count; i++)
            {
                ChallengeRange range = ranges[i];
                if (range != null && range.Type == type) return range;
            }
            return null;
        }
    }
}
