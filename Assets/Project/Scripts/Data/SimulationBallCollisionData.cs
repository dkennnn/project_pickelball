using System;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Ghi nhận một va chạm xảy ra trong lúc mô phỏng quỹ đạo ở physics scene phụ.
    /// <para>
    /// Nhờ dữ liệu này mà <see cref="BallController"/> và AI biết trước bóng sẽ nảy ở đâu,
    /// vào lúc nào, và có chạm lưới hay không — trước cả khi quả bóng thật bay.
    /// </para>
    /// </summary>
    [Serializable]
    public class SimulationBallCollisionData
    {
        /// <summary>Điểm tiếp xúc trong world space.</summary>
        public Vector3 point;

        /// <summary>Thời gian (giây) kể từ lúc đánh bóng cho tới khi xảy ra va chạm này.</summary>
        public float time;

        /// <summary>True nếu va chạm với vật mang tag <see cref="StringConstants.GroundTag"/>.</summary>
        public bool isGround;

        /// <summary>True nếu va chạm với vật mang tag <see cref="StringConstants.NetTag"/>.</summary>
        public bool isNet;

        /// <summary>Tạo bản ghi rỗng (dùng cho serialization).</summary>
        public SimulationBallCollisionData() { }

        /// <summary>Tạo đầy đủ một bản ghi va chạm mô phỏng.</summary>
        /// <param name="point">Điểm tiếp xúc world space.</param>
        /// <param name="time">Mốc thời gian (giây) kể từ lúc đánh bóng.</param>
        /// <param name="isGround">Va chạm với mặt sân?</param>
        /// <param name="isNet">Va chạm với lưới?</param>
        public SimulationBallCollisionData(Vector3 point, float time, bool isGround, bool isNet)
        {
            this.point = point;
            this.time = time;
            this.isGround = isGround;
            this.isNet = isNet;
        }
    }
}
