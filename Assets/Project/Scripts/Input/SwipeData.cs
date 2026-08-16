using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Gói dữ liệu mô tả một cú vuốt màn hình đã được <see cref="InputManager"/> phân tích.
    /// <para>
    /// Toàn bộ các điểm (<see cref="StartPoint"/>, <see cref="EndPoint"/>, <see cref="SwipePath"/>,
    /// <see cref="DestinationPoint"/>) đều là <b>world space trên mặt phẳng sân (y ≈ 0)</b>,
    /// đã được raycast từ toạ độ màn hình xuống <c>groundLayer</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public class SwipeData
    {
        /// <summary>Hướng vuốt đã chuẩn hoá trên mặt phẳng XZ.</summary>
        public Vector3 Direction;

        /// <summary>Tốc độ vuốt (pixel/giây, đã nhân <c>swipeVelocityMultiplier</c>) — quyết định lực đánh.</summary>
        public float Velocity;

        /// <summary>Điểm bắt đầu vuốt trong world space.</summary>
        public Vector3 StartPoint;

        /// <summary>Điểm kết thúc vuốt trong world space.</summary>
        public Vector3 EndPoint;

        /// <summary>Giai đoạn hiện tại của cú vuốt.</summary>
        public SwipePhase SwipePhase;

        /// <summary>Độ dài đường vuốt trong world space (mét).</summary>
        public float length;

        /// <summary>Toàn bộ các điểm đã đi qua của cú vuốt (world space), dùng để phân tích độ cong.</summary>
        public List<Vector3> SwipePath;

        /// <summary>Vector vuông góc chỉ chiều xoáy (trái/phải) trên mặt phẳng XZ; <c>Vector3.zero</c> nếu vuốt thẳng.</summary>
        public Vector3 CurveDirection;

        /// <summary>Cường độ xoáy đã chuẩn hoá trong khoảng [minCurveIntensity, maxCurveIntensity].</summary>
        public float CurveIntensity;

        /// <summary>Điểm rơi dự kiến trên sân suy ra từ hướng và độ dài vuốt.</summary>
        public Vector3 DestinationPoint;

        /// <summary>Tạo một SwipeData rỗng (dùng cho serialization).</summary>
        public SwipeData()
        {
            SwipePath = new List<Vector3>();
        }

        /// <summary>Tạo đầy đủ một SwipeData.</summary>
        /// <param name="direction">Hướng vuốt đã chuẩn hoá trên XZ.</param>
        /// <param name="velocity">Tốc độ vuốt.</param>
        /// <param name="startPoint">Điểm bắt đầu (world).</param>
        /// <param name="endPoint">Điểm kết thúc (world).</param>
        /// <param name="length">Độ dài đường vuốt (world).</param>
        /// <param name="swipePhase">Giai đoạn của cú vuốt.</param>
        /// <param name="swipePath">Danh sách điểm đã đi qua (world).</param>
        /// <param name="curveDirection">Vector chỉ chiều xoáy.</param>
        /// <param name="curveIntensity">Cường độ xoáy.</param>
        /// <param name="destinationPoint">Điểm rơi dự kiến.</param>
        public SwipeData(Vector3 direction, float velocity, Vector3 startPoint, Vector3 endPoint, float length,
            SwipePhase swipePhase, List<Vector3> swipePath, Vector3 curveDirection, float curveIntensity,
            Vector3 destinationPoint)
        {
            Direction = direction;
            Velocity = velocity;
            StartPoint = startPoint;
            EndPoint = endPoint;
            this.length = length;
            SwipePhase = swipePhase;
            SwipePath = swipePath ?? new List<Vector3>();
            CurveDirection = curveDirection;
            CurveIntensity = curveIntensity;
            DestinationPoint = destinationPoint;
        }
    }
}
