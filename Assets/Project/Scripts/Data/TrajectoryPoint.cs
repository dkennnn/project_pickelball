using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một mẫu (sample) trên quỹ đạo bay của bóng: vị trí world space kèm mốc thời gian
    /// tính từ lúc bóng rời vợt.
    /// <para>
    /// Đây là đơn vị dữ liệu thô do <see cref="PhysicsTrajectoryHandler"/> sinh ra trong
    /// physics scene phụ. Trước khi gửi đi (RPC / lưu vào <see cref="ShotTrajetoryInfo"/>)
    /// nó được nén lại thành <see cref="TrajectoryData"/> (hai mảng song song) cho gọn.
    /// </para>
    /// </summary>
    [Serializable]
    public class TrajectoryPoint
    {
        /// <summary>Vị trí tâm bóng trong world space tại thời điểm <see cref="time"/>.</summary>
        public Vector3 position;

        /// <summary>Thời gian (giây) tính từ khoảnh khắc bóng rời vợt.</summary>
        public float time;

        /// <summary>Tạo một mẫu quỹ đạo.</summary>
        /// <param name="position">Vị trí world space của tâm bóng.</param>
        /// <param name="time">Mốc thời gian (giây) kể từ lúc đánh bóng.</param>
        public TrajectoryPoint(Vector3 position, float time)
        {
            this.position = position;
            this.time = time;
        }
    }
}
