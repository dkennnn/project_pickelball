using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một nhiệm vụ hằng ngày đang hoạt động: mục tiêu, tiến độ, trạng thái hoàn thành
    /// và phần thưởng đã chốt số lượng chờ người chơi nhận.
    /// Đây là dữ liệu runtime có thể serialize để lưu giữa các phiên chơi.
    /// </summary>
    [Serializable]
    public class DailyChallenge
    {
        /// <summary>Định danh duy nhất của nhiệm vụ trong ngày (dùng để khớp khi nạp dữ liệu lưu).</summary>
        public string Id;

        /// <summary>Mô tả hiển thị cho người chơi, sinh từ <c>DailyChallengeManager.GenerateDescription</c>.</summary>
        public string Description;

        /// <summary>Loại nhiệm vụ, quyết định nguồn sự kiện nào sẽ đẩy tiến độ.</summary>
        public ChallengeType Type;

        /// <summary>Giá trị cần đạt để hoàn thành nhiệm vụ.</summary>
        public float TargetValue;

        /// <summary>Tiến độ hiện tại (luôn nằm trong đoạn [0, <see cref="TargetValue"/>]).</summary>
        public float Progress;

        /// <summary>True khi tiến độ đã chạm mục tiêu.</summary>
        public bool IsCompleted;

        /// <summary>True khi người chơi đã nhận phần thưởng của nhiệm vụ này.</summary>
        public bool RewardCollected;

        /// <summary>Phần thưởng đã chốt số lượng, cấp cho người chơi khi nhận.</summary>
        public CollectibleReward Reward;

        /// <summary>Tiến độ chuẩn hoá về đoạn [0,1] để UI vẽ thanh progress.</summary>
        public float NormalizedProgress
        {
            get
            {
                if (TargetValue <= 0f) return IsCompleted ? 1f : 0f;
                return Mathf.Clamp01(Progress / TargetValue);
            }
        }

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public DailyChallenge() { }

        /// <summary>Khởi tạo một nhiệm vụ mới đã có đủ mục tiêu, mô tả và phần thưởng.</summary>
        /// <param name="id">Định danh nhiệm vụ.</param>
        /// <param name="type">Loại nhiệm vụ.</param>
        /// <param name="targetValue">Giá trị cần đạt.</param>
        /// <param name="description">Mô tả hiển thị.</param>
        /// <param name="reward">Phần thưởng khi hoàn thành.</param>
        public DailyChallenge(string id, ChallengeType type, float targetValue,
            string description, CollectibleReward reward)
        {
            Id = id;
            Type = type;
            TargetValue = targetValue;
            Description = description;
            Reward = reward;
            Progress = 0f;
            IsCompleted = false;
            RewardCollected = false;
        }
    }
}
