using System;
using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>Bảng tra profile AI theo bậc độ khó.</summary>
    [Serializable]
    public class PlayerProfileConfigs
    {
        /// <summary>Danh sách nhóm profile theo từng bậc độ khó.</summary>
        public List<AIProfileData> aiProfileDataList;

        /// <summary>
        /// Bốc ngẫu nhiên một profile thuộc bậc độ khó yêu cầu.
        /// Nếu bậc đó chưa cấu hình, lấy profile đầu tiên tìm được; trả về <c>null</c> nếu không có gì.
        /// </summary>
        /// <param name="difficulty">Bậc độ khó cần lấy profile.</param>
        public PlayerProfile GetProfile(AIDifficulty difficulty)
        {
            if (aiProfileDataList == null || aiProfileDataList.Count == 0) return null;

            for (int i = 0; i < aiProfileDataList.Count; i++)
            {
                AIProfileData data = aiProfileDataList[i];
                if (data == null || data.difficulty != difficulty) continue;

                PlayerProfile picked = PickRandom(data.profiles);
                if (picked != null) return picked;
            }

            // Fallback: profile đầu tiên còn dùng được trong toàn bảng.
            for (int i = 0; i < aiProfileDataList.Count; i++)
            {
                AIProfileData data = aiProfileDataList[i];
                if (data == null || data.profiles == null) continue;

                for (int j = 0; j < data.profiles.Count; j++)
                {
                    if (data.profiles[j] != null) return data.profiles[j];
                }
            }

            return null;
        }

        private static PlayerProfile PickRandom(List<PlayerProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0) return null;

            PlayerProfile candidate = profiles[UnityEngine.Random.Range(0, profiles.Count)];
            if (candidate != null) return candidate;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] != null) return profiles[i];
            }
            return null;
        }
    }
}
