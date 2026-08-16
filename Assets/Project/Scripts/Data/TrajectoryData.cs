using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quỹ đạo bay của bóng được nén thành hai mảng song song: <see cref="Points"/> (vị trí)
    /// và <see cref="timeStamps"/> (mốc thời gian tương ứng).
    /// <para>
    /// Dạng "hai list song song" được giữ nguyên từ bản gốc vì nó serialize/gửi RPC rẻ hơn
    /// nhiều so với list object (<c>Vector3[]</c> + <c>float[]</c> là kiểu Mirror hỗ trợ sẵn).
    /// Phần tử thứ <c>i</c> của hai list LUÔN phải ứng với nhau — dùng <see cref="IsValid"/>
    /// để kiểm tra trước khi đọc.
    /// </para>
    /// </summary>
    [Serializable]
    public class TrajectoryData
    {
        /// <summary>Danh sách vị trí world space của tâm bóng theo thứ tự thời gian tăng dần.</summary>
        public List<Vector3> Points;

        /// <summary>Mốc thời gian (giây, kể từ lúc đánh bóng) ứng với từng phần tử của <see cref="Points"/>.</summary>
        public List<float> timeStamps;

        /// <summary>Tạo quỹ đạo rỗng.</summary>
        public TrajectoryData()
        {
            Points = new List<Vector3>();
            timeStamps = new List<float>();
        }

        /// <summary>Tạo quỹ đạo từ hai list đã có sẵn (không copy — dùng trực tiếp tham chiếu).</summary>
        /// <param name="points">Danh sách vị trí.</param>
        /// <param name="timeStamps">Danh sách mốc thời gian tương ứng.</param>
        public TrajectoryData(List<Vector3> points, List<float> timeStamps)
        {
            Points = points ?? new List<Vector3>();
            this.timeStamps = timeStamps ?? new List<float>();
        }

        /// <summary>Tạo quỹ đạo từ danh sách <see cref="TrajectoryPoint"/> do bộ mô phỏng sinh ra.</summary>
        /// <param name="trajectoryPoints">Các mẫu quỹ đạo thô; null hoặc rỗng thì tạo quỹ đạo rỗng.</param>
        public TrajectoryData(List<TrajectoryPoint> trajectoryPoints)
        {
            int count = trajectoryPoints?.Count ?? 0;
            Points = new List<Vector3>(count);
            timeStamps = new List<float>(count);

            for (int i = 0; i < count; i++)
            {
                TrajectoryPoint p = trajectoryPoints[i];
                if (p == null) continue;
                Points.Add(p.position);
                timeStamps.Add(p.time);
            }
        }

        /// <summary>Số mẫu hợp lệ (min của hai list, phòng khi dữ liệu lệch).</summary>
        public int Count => Points == null || timeStamps == null
            ? 0
            : Mathf.Min(Points.Count, timeStamps.Count);

        /// <summary>True khi quỹ đạo có ít nhất 2 mẫu và hai list cùng độ dài.</summary>
        public bool IsValid => Points != null && timeStamps != null
                               && Points.Count == timeStamps.Count && Points.Count >= 2;

        /// <summary>
        /// Vị trí bóng tại thời điểm <paramref name="t"/> bằng nội suy tuyến tính giữa hai mẫu kề nhau.
        /// </summary>
        /// <param name="t">Thời gian (giây) kể từ lúc đánh bóng. Ngoài khoảng sẽ bị kẹp về đầu/cuối quỹ đạo.</param>
        /// <returns><see cref="Vector3.zero"/> nếu quỹ đạo rỗng.</returns>
        public Vector3 GetPositionAtTime(float t)
        {
            int count = Count;
            if (count == 0) return Vector3.zero;
            if (count == 1) return Points[0];

            if (t <= timeStamps[0]) return Points[0];
            if (t >= timeStamps[count - 1]) return Points[count - 1];

            for (int i = 1; i < count; i++)
            {
                if (timeStamps[i] < t) continue;

                float span = timeStamps[i] - timeStamps[i - 1];
                float k = span <= Mathf.Epsilon ? 0f : (t - timeStamps[i - 1]) / span;
                return Vector3.Lerp(Points[i - 1], Points[i], k);
            }

            return Points[count - 1];
        }

        /// <summary>Điểm cao nhất (theo trục Y) của quỹ đạo — dùng để canh tầm vợt / camera.</summary>
        /// <param name="time">Mốc thời gian của điểm cao nhất; 0 nếu quỹ đạo rỗng.</param>
        /// <returns><see cref="Vector3.zero"/> nếu quỹ đạo rỗng.</returns>
        public Vector3 GetHighestPoint(out float time)
        {
            time = 0f;
            int count = Count;
            if (count == 0) return Vector3.zero;

            int best = 0;
            for (int i = 1; i < count; i++)
            {
                if (Points[i].y > Points[best].y) best = i;
            }

            time = timeStamps[best];
            return Points[best];
        }

        /// <summary>Xoá toàn bộ mẫu nhưng giữ lại list để tái sử dụng bộ nhớ.</summary>
        public void Clear()
        {
            Points?.Clear();
            timeStamps?.Clear();
        }
    }
}
