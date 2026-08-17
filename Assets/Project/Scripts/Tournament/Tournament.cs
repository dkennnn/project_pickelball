using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pickleball
{
    /// <summary>
    /// Định nghĩa một giải đấu: điều kiện tham gia, các vòng đấu, phần thưởng vô địch
    /// và khung thời gian mở giải.
    /// </summary>
    [Serializable]
    public class Tournament
    {
        /// <summary>Định danh giải đấu (ví dụ "tournament_001").</summary>
        public string id;

        /// <summary>Tên hiển thị của giải.</summary>
        public string name;

        /// <summary>Mô tả ngắn hiển thị trên UI.</summary>
        public string description;

        /// <summary>Level tối thiểu người chơi phải đạt để được vào giải.</summary>
        public int requiredLevel;

        /// <summary>Phí tham gia giải.</summary>
        public int entryCost;

        /// <summary>Loại tiền dùng để trả phí tham gia.</summary>
        public CurrencyType entryCostType;

        /// <summary>Các vòng đấu, sắp theo <c>stageNumber</c> tăng dần.</summary>
        public List<TournamentStage> stages = new List<TournamentStage>();

        /// <summary>Phần thưởng nhận được khi vô địch giải.</summary>
        public List<DynamicReward> rewards = new List<DynamicReward>();

        /// <summary>Thời điểm mở giải, dạng chuỗi ISO 8601 UTC.</summary>
        public string startDateString;

        /// <summary>Thời điểm đóng giải, dạng chuỗi ISO 8601 UTC.</summary>
        public string endDateString;

        /// <summary>Công tắc bật/tắt giải, độc lập với khung thời gian.</summary>
        public bool isActive;

        /// <summary>
        /// True khi giải đang mở: <see cref="isActive"/> bật và thời điểm hiện tại (UTC)
        /// nằm trong khung <see cref="startDateString"/>..<see cref="endDateString"/>.
        /// Mốc thời gian để trống hoặc sai định dạng được coi là không giới hạn.
        /// </summary>
        public bool IsOpenNow()
        {
            if (!isActive) return false;

            // TODO: đổi sang server time khi có backend.
            DateTime now = DateTime.UtcNow;

            if (TryParseIso(startDateString, out DateTime start) && now < start) return false;
            if (TryParseIso(endDateString, out DateTime end) && now > end) return false;

            return true;
        }

        /// <summary>Số vòng đấu của giải.</summary>
        public int StageCount => stages != null ? stages.Count : 0;

        /// <summary>
        /// Lấy vòng đấu theo chỉ số. Trả về <c>null</c> nếu chỉ số nằm ngoài danh sách.
        /// </summary>
        /// <param name="stageIndex">Chỉ số vòng, bắt đầu từ 0.</param>
        public TournamentStage GetStage(int stageIndex)
        {
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Count) return null;
            return stages[stageIndex];
        }

        /// <summary>Phân tích chuỗi ISO 8601 thành thời điểm UTC.</summary>
        private static bool TryParseIso(string value, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(value)) return false;

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return false;
            }

            utc = parsed.ToUniversalTime();
            return true;
        }
    }
}
