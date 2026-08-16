using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>Nhóm profile AI khả dụng cho một bậc độ khó.</summary>
    [Serializable]
    public class AIProfileData
    {
        /// <summary>Bậc độ khó mà nhóm profile này phục vụ.</summary>
        public AIDifficulty difficulty;

        /// <summary>Các profile có thể bốc ngẫu nhiên cho bậc độ khó này.</summary>
        public List<PlayerProfile> profiles;
    }
}
