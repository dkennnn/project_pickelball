using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Danh mục toàn bộ sân đấu, sắp theo bậc level tăng dần.</summary>
    [CreateAssetMenu(fileName = "StadiumsData", menuName = "ScriptableObjects/StadiumsData")]
    public class StadiumsData : ScriptableObject
    {
        /// <summary>Danh sách sân đấu đã cấu hình.</summary>
        public List<StadiumData> stadiums;

        /// <summary>
        /// Lấy sân đấu ứng với level người chơi.
        /// Level vượt quá mốc cao nhất sẽ nhận sân cuối cùng; trả về <c>null</c> nếu danh sách rỗng.
        /// </summary>
        /// <param name="level">Level hiện tại của người chơi.</param>
        public StadiumData GetStadiumForLevel(int level)
        {
            if (stadiums == null || stadiums.Count == 0) return null;

            for (int i = 0; i < stadiums.Count; i++)
            {
                StadiumData stadium = stadiums[i];
                if (stadium == null) continue;
                if (level >= stadium.minLevel && level <= stadium.maxLevel) return stadium;
            }

            // Level nằm ngoài mọi khoảng: dưới mốc thấp nhất lấy sân đầu, trên mốc cao nhất lấy sân cuối.
            StadiumData first = stadiums[0];
            return (first != null && level < first.minLevel) ? first : stadiums[stadiums.Count - 1];
        }

        /// <summary>Tìm sân đấu theo tên scene. Trả về <c>null</c> nếu không có sân nào khớp.</summary>
        /// <param name="sceneName">Tên scene Unity cần tra cứu.</param>
        public StadiumData GetStadiumByScene(string sceneName)
        {
            if (stadiums == null || string.IsNullOrEmpty(sceneName)) return null;

            for (int i = 0; i < stadiums.Count; i++)
            {
                StadiumData stadium = stadiums[i];
                if (stadium != null && stadium.stadiumSceneName == sceneName) return stadium;
            }
            return null;
        }
    }
}
