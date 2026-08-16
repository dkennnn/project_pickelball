using System;

namespace Pickleball
{
    /// <summary>
    /// Ngưỡng tự chọn độ khó AI dựa trên thành tích người chơi.
    /// Chỉ dùng khi <see cref="AIDifficultyMode.Auto"/>; các mode khác ép cứng độ khó tương ứng.
    /// </summary>
    [Serializable]
    public class AIDifficultySettings
    {
        /// <summary>Số trận tối thiểu trước khi thoát khỏi bậc Newbie.</summary>
        public int friendlyGamesThreshold = 5;

        /// <summary>Tỉ lệ thắng tối thiểu để thoát khỏi bậc Newbie (0..1).</summary>
        public float friendlyWinRatioThreshold = 0.3f;

        /// <summary>Số trận tối thiểu trước khi thoát khỏi bậc Amateur.</summary>
        public int normalGamesThreshold = 15;

        /// <summary>Tỉ lệ thắng tối thiểu để thoát khỏi bậc Amateur (0..1).</summary>
        public float normalWinRatioThreshold = 0.45f;

        /// <summary>Tỉ lệ thắng tối thiểu để lên bậc Pro (0..1).</summary>
        public float hardWinRatioThreshold = 0.6f;

        /// <summary>Tỉ lệ thắng tối thiểu để lên bậc Master (0..1).</summary>
        public float expertWinRatioThreshold = 0.75f;

        /// <summary>
        /// Quyết định độ khó AI cho trận sắp tới.
        /// Mode khác Auto sẽ trả thẳng bậc tương ứng; Auto sẽ suy ra từ số trận và tỉ lệ thắng.
        /// </summary>
        /// <param name="aIDifficultyMode">Chế độ chọn độ khó.</param>
        /// <param name="gamesPlayed">Tổng số trận người chơi đã chơi.</param>
        /// <param name="winRatio">Tỉ lệ thắng của người chơi (0..1).</param>
        public AIDifficulty DetermineDifficultyLevel(AIDifficultyMode aIDifficultyMode, int gamesPlayed, float winRatio)
        {
            switch (aIDifficultyMode)
            {
                case AIDifficultyMode.Newbie: return AIDifficulty.Newbie;
                case AIDifficultyMode.Amateur: return AIDifficulty.Amateur;
                case AIDifficultyMode.Competitor: return AIDifficulty.Competitor;
                case AIDifficultyMode.Pro: return AIDifficulty.Pro;
                case AIDifficultyMode.Master: return AIDifficulty.Master;
            }

            if (gamesPlayed < friendlyGamesThreshold || winRatio < friendlyWinRatioThreshold)
                return AIDifficulty.Newbie;

            if (gamesPlayed < normalGamesThreshold || winRatio < normalWinRatioThreshold)
                return AIDifficulty.Amateur;

            if (winRatio < hardWinRatioThreshold) return AIDifficulty.Competitor;
            if (winRatio < expertWinRatioThreshold) return AIDifficulty.Pro;

            return AIDifficulty.Master;
        }
    }
}
