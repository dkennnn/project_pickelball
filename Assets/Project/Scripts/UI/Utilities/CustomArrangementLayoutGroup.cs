using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// LayoutGroup tuỳ biến: xếp các con vào những vị trí cấu hình sẵn thay vì theo hàng/cột đều.
    /// Bản gốc dùng cho bố cục quà không đều (ô lớn ở giữa, ô nhỏ hai bên).
    /// <para>
    /// Vị trí trong <see cref="positions"/> tính từ góc TRÊN-TRÁI của vùng nội dung (đã trừ
    /// <see cref="LayoutGroup.padding"/>), trục Y hướng xuống — giống quy ước của mọi
    /// LayoutGroup dựng sẵn của Unity. Con thứ <c>i</c> lấy phần tử thứ <c>i</c>; thừa con so
    /// với số vị trí thì các con dư được xếp tiếp bên dưới theo <see cref="overflowStep"/>.
    /// </para>
    /// </summary>
    [AddComponentMenu("Layout/Custom Arrangement Layout Group")]
    [ExecuteAlways]
    public class CustomArrangementLayoutGroup : LayoutGroup
    {
        /// <summary>Vị trí góc trên-trái của từng con, tính từ vùng nội dung.</summary>
        [SerializeField] private List<Vector2> positions = new List<Vector2>();

        /// <summary>Kích thước mặc định của một con khi <see cref="sizes"/> không có mục tương ứng.</summary>
        [SerializeField] private Vector2 cellSize = new Vector2(150f, 150f);

        /// <summary>Kích thước riêng của từng con; để trống sẽ dùng <see cref="cellSize"/>.</summary>
        [SerializeField] private List<Vector2> sizes = new List<Vector2>();

        /// <summary>Bước dịch cho các con vượt quá số vị trí đã khai báo.</summary>
        [SerializeField] private Vector2 overflowStep = new Vector2(0f, 160f);

        /// <summary>Vị trí góc trên-trái của từng con.</summary>
        public List<Vector2> Positions => positions;

        /// <summary>Kích thước mặc định của một con.</summary>
        public Vector2 CellSize
        {
            get => cellSize;
            set
            {
                cellSize = value;
                SetDirty();
            }
        }

        /// <inheritdoc/>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateAxis(0);
        }

        /// <inheritdoc/>
        public override void CalculateLayoutInputVertical()
        {
            CalculateAxis(1);
        }

        /// <inheritdoc/>
        public override void SetLayoutHorizontal()
        {
            ArrangeItems(0);
        }

        /// <inheritdoc/>
        public override void SetLayoutVertical()
        {
            ArrangeItems(1);
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Tính kích thước tối thiểu/ưu tiên của cả nhóm trên một trục.</summary>
        /// <param name="axis">0 là ngang, 1 là dọc.</param>
        private void CalculateAxis(int axis)
        {
            float extent = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                Vector2 position = GetPosition(i);
                Vector2 size = GetSize(i);

                float end = axis == 0 ? position.x + size.x : position.y + size.y;
                if (end > extent) extent = end;
            }

            float pad = axis == 0 ? padding.horizontal : padding.vertical;
            float total = extent + pad;

            SetLayoutInputForAxis(total, total, -1f, axis);
        }

        /// <summary>Đặt từng con vào vị trí đã cấu hình trên một trục.</summary>
        /// <param name="axis">0 là ngang, 1 là dọc.</param>
        private void ArrangeItems(int axis)
        {
            float offset = axis == 0 ? padding.left : padding.top;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child == null) continue;

                Vector2 position = GetPosition(i);
                Vector2 size = GetSize(i);

                SetChildAlongAxis(
                    child,
                    axis,
                    offset + (axis == 0 ? position.x : position.y),
                    axis == 0 ? size.x : size.y);
            }
        }

        /// <summary>Vị trí của con thứ <paramref name="index"/>, có xử lý phần dư.</summary>
        /// <param name="index">Chỉ số con trong <see cref="LayoutGroup.rectChildren"/>.</param>
        private Vector2 GetPosition(int index)
        {
            int count = positions != null ? positions.Count : 0;
            if (count == 0) return new Vector2(overflowStep.x * index, overflowStep.y * index);
            if (index < count) return positions[index];

            // Con dư: lặp lại vị trí cuối rồi dịch dần theo overflowStep.
            int overflow = index - count + 1;
            return positions[count - 1] + overflowStep * overflow;
        }

        /// <summary>Kích thước của con thứ <paramref name="index"/>.</summary>
        /// <param name="index">Chỉ số con trong <see cref="LayoutGroup.rectChildren"/>.</param>
        private Vector2 GetSize(int index)
        {
            if (sizes != null && index >= 0 && index < sizes.Count)
            {
                Vector2 size = sizes[index];
                if (size.x > 0f && size.y > 0f) return size;
            }

            return cellSize;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (cellSize.x < 0f) cellSize.x = 0f;
            if (cellSize.y < 0f) cellSize.y = 0f;
        }
#endif
    }
}
