using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Lớp cơ sở cho mọi phần thưởng dạng asset ScriptableObject.
    /// Mỗi phần thưởng biết mình thuộc loại nào và có trọng số bốc trúng bao nhiêu.
    /// </summary>
    public abstract class Reward : ScriptableObject
    {
        /// <summary>Loại phần thưởng sẽ được cộng cho người chơi.</summary>
        public RewardType rewardType;

        /// <summary>
        /// Trọng số bốc trúng trong bảng thưởng, chuẩn hoá về [0,1].
        /// Đây là trọng số tương đối chứ không phải xác suất tuyệt đối:
        /// bảng thưởng luôn tính tổng trọng số rồi mới quay.
        /// </summary>
        [Range(0f, 1f)]
        public float probability = 1f;

        /// <summary>Số lượng thực tế nhận được khi phần thưởng này được bốc trúng.</summary>
        public virtual int GetReward()
        {
            return 0;
        }
    }
}
