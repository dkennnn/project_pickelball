using System;

namespace Pickleball
{
    /// <summary>
    /// Kết quả một trận giữa hai AI trong cùng nhánh bracket (người chơi không tham gia).
    /// Chỉ phục vụ hiển thị sơ đồ giải đấu.
    /// </summary>
    [Serializable]
    public class AIMatchResult
    {
        /// <summary>True khi UI cần vẽ người thắng ở ô dưới thay vì ô trên.</summary>
        public bool swapWinnerPosition;

        /// <summary>Tên AI thắng trận.</summary>
        public string winnerName;

        /// <summary>Chỉ số ảnh đại diện của AI thắng.</summary>
        public int winnerAvatarIndex;

        /// <summary>Tên AI thua trận.</summary>
        public string loserName;

        /// <summary>Chỉ số ảnh đại diện của AI thua.</summary>
        public int loserAvatarIndex;

        /// <summary>Khởi tạo rỗng phục vụ serialize của Unity.</summary>
        public AIMatchResult() { }

        /// <summary>Khởi tạo một kết quả AI đấu AI.</summary>
        /// <param name="winnerName">Tên AI thắng.</param>
        /// <param name="winnerAvatarIndex">Ảnh đại diện AI thắng.</param>
        /// <param name="loserName">Tên AI thua.</param>
        /// <param name="loserAvatarIndex">Ảnh đại diện AI thua.</param>
        /// <param name="swapWinnerPosition">Đảo vị trí hiển thị người thắng.</param>
        public AIMatchResult(string winnerName, int winnerAvatarIndex,
            string loserName, int loserAvatarIndex, bool swapWinnerPosition = false)
        {
            this.winnerName = winnerName;
            this.winnerAvatarIndex = winnerAvatarIndex;
            this.loserName = loserName;
            this.loserAvatarIndex = loserAvatarIndex;
            this.swapWinnerPosition = swapWinnerPosition;
        }
    }
}
