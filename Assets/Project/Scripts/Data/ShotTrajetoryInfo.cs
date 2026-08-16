using System;

namespace Pickleball
{
    /// <summary>
    /// Toàn bộ thông tin quỹ đạo của MỘT cú đánh, tách theo lần nảy.
    /// <para>
    /// LƯU Ý: tên class giữ nguyên lỗi chính tả "Trajetory" của bản gốc để mọi chữ ký hàm
    /// (kể cả RPC) khớp 1-1 với bản decompile — KHÔNG sửa thành "Trajectory".
    /// </para>
    /// <para>
    /// Đây là mấu chốt của cả gameplay lẫn AI: người chơi và AI đều nhận được object này
    /// ngay khi bóng vừa rời vợt, nên biết trước bóng rơi ở đâu và lúc nào để chạy đón.
    /// </para>
    /// </summary>
    [Serializable]
    public class ShotTrajetoryInfo
    {
        /// <summary>Đoạn quỹ đạo từ lúc rời vợt cho tới lần chạm đất ĐẦU TIÊN.</summary>
        public TrajectoryData NormaltrajectoryData;

        /// <summary>Đoạn quỹ đạo từ lần nảy thứ nhất tới lần chạm đất THỨ HAI (dùng cho luật two-bounce và AI).</summary>
        public TrajectoryData SecondBounceTrajectory;

        /// <summary>Tạo thông tin rỗng với hai quỹ đạo rỗng (không bao giờ null).</summary>
        public ShotTrajetoryInfo()
        {
            NormaltrajectoryData = new TrajectoryData();
            SecondBounceTrajectory = new TrajectoryData();
        }

        /// <summary>Tạo đầy đủ thông tin quỹ đạo cho một cú đánh.</summary>
        /// <param name="normalTrajectoryData">Quỹ đạo tới lần nảy thứ nhất.</param>
        /// <param name="secondBounceTrajectory">Quỹ đạo giữa lần nảy thứ nhất và thứ hai.</param>
        public ShotTrajetoryInfo(TrajectoryData normalTrajectoryData, TrajectoryData secondBounceTrajectory)
        {
            NormaltrajectoryData = normalTrajectoryData ?? new TrajectoryData();
            SecondBounceTrajectory = secondBounceTrajectory ?? new TrajectoryData();
        }
    }
}
