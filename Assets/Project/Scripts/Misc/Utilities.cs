using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Hàm tiện ích dùng chung toàn game.</summary>
    public static class Utilities
    {
        /// <summary>Ánh xạ tuyến tính value từ [inMin,inMax] sang [outMin,outMax] (có clamp).</summary>
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            if (Mathf.Approximately(inMax, inMin)) return outMin;
            float t = Mathf.Clamp01((value - inMin) / (inMax - inMin));
            return Mathf.Lerp(outMin, outMax, t);
        }

        /// <summary>Chuẩn hoá chỉ số thang 0..100 của item về giá trị thật trong [min,max].</summary>
        public static float StatToValue(float stat0To100, float min, float max)
        {
            return Mathf.Lerp(min, max, Mathf.Clamp01(stat0To100 / 100f));
        }

        /// <summary>Bốc ngẫu nhiên có trọng số. Trả về default nếu danh sách rỗng.</summary>
        public static T WeightedRandom<T>(IList<T> items, Func<T, float> weightSelector)
        {
            if (items == null || items.Count == 0) return default;

            float total = 0f;
            for (int i = 0; i < items.Count; i++) total += Mathf.Max(0f, weightSelector(items[i]));
            if (total <= 0f) return items[UnityEngine.Random.Range(0, items.Count)];

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < items.Count; i++)
            {
                roll -= Mathf.Max(0f, weightSelector(items[i]));
                if (roll <= 0f) return items[i];
            }
            return items[items.Count - 1];
        }

        /// <summary>Định dạng số lớn: 1250 thành "1.2K", 3400000 thành "3.4M".</summary>
        public static string FormatCount(long value)
        {
            if (value >= 1000000000) return (value / 1000000000f).ToString("0.#") + "B";
            if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 1000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString();
        }

        /// <summary>Định dạng thời gian còn lại cho timer locker/daily.</summary>
        public static string FormatTimeSpan(TimeSpan span)
        {
            if (span.TotalSeconds <= 0) return "0s";
            if (span.TotalHours >= 1) return ((int)span.TotalHours) + "h " + span.Minutes.ToString("00") + "m";
            if (span.TotalMinutes >= 1) return span.Minutes + "m " + span.Seconds.ToString("00") + "s";
            return span.Seconds + "s";
        }

        /// <summary>Giữ một điểm nằm trong hộp chữ nhật trên mặt phẳng XZ quanh center.</summary>
        public static Vector3 ClampToBounds(Vector3 position, Vector3 center, Vector2 extents)
        {
            position.x = Mathf.Clamp(position.x, center.x - extents.x, center.x + extents.x);
            position.z = Mathf.Clamp(position.z, center.z - extents.y, center.z + extents.y);
            return position;
        }

        /// <summary>True nếu vector chứa NaN hoặc Infinity (bảo vệ physics khỏi vỡ).</summary>
        public static bool IsInvalid(Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
        }

        /// <summary>
        /// Giải bài toán ném xiên: vận tốc ban đầu để bay từ <paramref name="from"/> tới
        /// <paramref name="to"/> trong đúng <paramref name="time"/> giây dưới trọng lực.
        /// </summary>
        public static Vector3 SolveBallisticVelocity(Vector3 from, Vector3 to, float time, float gravity)
        {
            if (time <= 0.0001f) return Vector3.zero;
            Vector3 displacement = to - from;
            Vector3 horizontal = new Vector3(displacement.x, 0f, displacement.z) / time;
            float vy = displacement.y / time + 0.5f * Mathf.Abs(gravity) * time;
            return new Vector3(horizontal.x, vy, horizontal.z);
        }

        /// <summary>Khoảng cách vuông góc từ point tới đoạn thẳng a-b (dùng để đo độ cong swipe).</summary>
        public static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0000001f) return Vector3.Distance(point, a);
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
            return Vector3.Distance(point, a + ab * t);
        }
    }
}
