using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// ScriptableObject chứa toàn bộ luật/tham số chung của trận đấu.
    /// Nhóm field <c>normal*</c> và <c>quick*</c> là dữ liệu tĩnh cấu hình sẵn;
    /// nhóm <c>maxScore</c>/<c>winMargin</c>/<c>gameBetCoins</c> là giá trị đang áp dụng lúc runtime,
    /// được ghi đè mỗi khi gọi <see cref="ApplySelectedModeSettings"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameSettings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        /// <summary>Điểm cần đạt để thắng ván ở chế độ Normal.</summary>
        [Header("Normal Match Settings")]
        public int normalMaxScore;

        /// <summary>Chênh lệch điểm tối thiểu để thắng ở chế độ Normal (luật deuce).</summary>
        public int normalWinMargin;

        /// <summary>Tiền cược mỗi trận ở chế độ Normal.</summary>
        public int normalGameBetCoins;

        /// <summary>Điểm cần đạt để thắng ván ở chế độ Quick Match.</summary>
        [Header("Quick Match Settings")]
        public int quickMaxScore;

        /// <summary>Chênh lệch điểm tối thiểu để thắng ở chế độ Quick Match.</summary>
        public int quickWinMargin;

        /// <summary>Tiền cược mỗi trận ở chế độ Quick Match.</summary>
        public int quickGameBetCoins;

        /// <summary>Bán kính vật lý của quả bóng (mét).</summary>
        [Header("Common Settings")]
        public float ballRadius;

        /// <summary>Thời gian tối đa được phép giữ bóng trước khi giao (giây).</summary>
        public float serveTimeout;

        /// <summary>Hệ số nhân khi booster năng lượng đang hoạt động.</summary>
        public float EnergyBoostMultiplier;

        /// <summary>Số cú đánh mà một booster còn hiệu lực.</summary>
        public int BoosterMaxShots;

        /// <summary>Chế độ trận đấu người chơi đang chọn.</summary>
        [Header("Match Mode Selection")]
        public MatchModeType selectedMode;

        /// <summary>Danh sách cấu hình của tất cả chế độ trận đấu.</summary>
        public List<MatchModeConfig> matchModeConfigs;

        /// <summary>Điểm thắng đang áp dụng cho trận hiện tại (chỉ runtime).</summary>
        [Header("Current Active Settings (Runtime Only)")]
        [Space(10f)]
        public int maxScore;

        /// <summary>Chênh lệch điểm thắng đang áp dụng cho trận hiện tại (chỉ runtime).</summary>
        public int winMargin;

        /// <summary>Tiền cược đang áp dụng cho trận hiện tại (chỉ runtime).</summary>
        public int gameBetCoins;

        /// <summary>Tên scene sân đấu do người chơi/host chỉ định; rỗng nếu chọn sân theo level (chỉ runtime).</summary>
        public string customStadiumSceneName;

        /// <summary>Lấy cấu hình của một chế độ trận đấu. Trả về <c>null</c> nếu chưa khai báo.</summary>
        /// <param name="mode">Chế độ cần tra cứu.</param>
        public MatchModeConfig GetModeConfig(MatchModeType mode)
        {
            if (matchModeConfigs == null) return null;

            for (int i = 0; i < matchModeConfigs.Count; i++)
            {
                MatchModeConfig config = matchModeConfigs[i];
                if (config != null && config.modeType == mode) return config;
            }
            return null;
        }

        /// <summary>Lấy ảnh nút màn hình chính của chế độ đang chọn. Trả về <c>null</c> nếu chưa gán.</summary>
        public Sprite GetSelectedModeMainMenuSprite()
        {
            MatchModeConfig config = GetModeConfig(selectedMode);
            return config != null ? config.mainMenuButtonSprite : null;
        }

        /// <summary>Đổi chế độ đang chọn và áp dụng ngay tham số tương ứng.</summary>
        /// <param name="mode">Chế độ muốn chuyển sang.</param>
        public void SelectMode(MatchModeType mode)
        {
            selectedMode = mode;
            ApplySelectedModeSettings();
        }

        /// <summary>
        /// Ghi tham số của <see cref="selectedMode"/> vào nhóm field runtime.
        /// LAN và Target dùng chung tham số của chế độ Normal.
        /// </summary>
        public void ApplySelectedModeSettings()
        {
            switch (selectedMode)
            {
                case MatchModeType.QuickMatch:
                    maxScore = quickMaxScore;
                    winMargin = quickWinMargin;
                    gameBetCoins = quickGameBetCoins;
                    break;

                case MatchModeType.Normal:
                case MatchModeType.LAN:
                case MatchModeType.Target:
                default:
                    maxScore = normalMaxScore;
                    winMargin = normalWinMargin;
                    gameBetCoins = normalGameBetCoins;
                    break;
            }
        }

        /// <summary>
        /// Kiểm tra một chế độ đã mở khoá cho level người chơi hay chưa.
        /// Chế độ chưa phát hành (<c>isComingSoon</c>) luôn bị coi là đang khoá.
        /// </summary>
        /// <param name="mode">Chế độ cần kiểm tra.</param>
        /// <param name="playerLevel">Level hiện tại của người chơi.</param>
        public bool IsModeUnlocked(MatchModeType mode, int playerLevel)
        {
            MatchModeConfig config = GetModeConfig(mode);
            if (config == null) return false;
            if (config.isComingSoon) return false;
            return playerLevel >= config.unlockLevel;
        }

        /// <summary>Chuyển nhanh giữa Quick Match và Normal rồi áp dụng tham số.</summary>
        /// <param name="isQuickMatch">True để bật Quick Match, false để quay về Normal.</param>
        public void SetQuickMatch(bool isQuickMatch = false)
        {
            selectedMode = isQuickMatch ? MatchModeType.QuickMatch : MatchModeType.Normal;
            ApplySelectedModeSettings();
        }

        /// <summary>Tiền thưởng khi thắng trận career: gấp đôi tiền cược đang áp dụng.</summary>
        public int GetCareerWinRewardCoins()
        {
            return gameBetCoins * 2;
        }

        /// <summary>Đưa asset về trạng thái mặc định (Normal) và xoá sân đấu tuỳ chọn.</summary>
        public void ResetToNormalSettings()
        {
            selectedMode = MatchModeType.Normal;
            customStadiumSceneName = string.Empty;
            ApplySelectedModeSettings();
        }

        private void OnEnable()
        {
            ResetToNormalSettings();
        }
    }
}
