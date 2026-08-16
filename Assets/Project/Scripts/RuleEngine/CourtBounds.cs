using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hộp chữ nhật trục-song-song mô tả một vùng của sân (nửa sân, kitchen, ô giao bóng...).
    /// <para>
    /// Vùng được xét trên mặt phẳng XZ; trục Y bị bỏ qua hoàn toàn.
    /// <see cref="extends"/> là <b>NỬA</b> kích thước: <c>extends.x</c> theo trục X,
    /// <c>extends.y</c> theo trục Z.
    /// </para>
    /// </summary>
    public class CourtBounds
    {
        /// <summary>Nửa kích thước của hộp: (nửa bề rộng theo X, nửa bề dài theo Z).</summary>
        public Vector2 extends;

        /// <summary>Tâm hộp trong world space (thành phần Y chỉ để tiện vẽ Gizmos).</summary>
        public Vector3 center;

        /// <summary>Tạo một vùng sân từ nửa kích thước và tâm.</summary>
        /// <param name="extends">Nửa kích thước (X, Z).</param>
        /// <param name="center">Tâm hộp trong world space.</param>
        public CourtBounds(Vector2 extends, Vector3 center)
        {
            this.extends = extends;
            this.center = center;
        }

        /// <summary>Giá trị X nhỏ nhất của vùng.</summary>
        public float MinX => center.x - extends.x;

        /// <summary>Giá trị X lớn nhất của vùng.</summary>
        public float MaxX => center.x + extends.x;

        /// <summary>Giá trị Z nhỏ nhất của vùng.</summary>
        public float MinZ => center.z - extends.y;

        /// <summary>Giá trị Z lớn nhất của vùng.</summary>
        public float MaxZ => center.z + extends.y;

        /// <summary>Kích thước đầy đủ của vùng (X, Z).</summary>
        public Vector2 Size => extends * 2f;

        /// <summary>
        /// True nếu <paramref name="position"/> nằm trong vùng khi chiếu xuống mặt phẳng XZ
        /// (bao gồm cả điểm nằm đúng trên biên).
        /// </summary>
        public bool Contains(Vector3 position)
        {
            return Mathf.Abs(position.x - center.x) <= extends.x
                && Mathf.Abs(position.z - center.z) <= extends.y;
        }

        public override string ToString()
        {
            return $"CourtBounds(center={center}, extends={extends})";
        }
    }
}
