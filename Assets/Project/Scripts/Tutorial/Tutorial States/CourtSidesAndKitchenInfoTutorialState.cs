using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 3 — giới thiệu hình học sân: làm nổi bật hai nửa sân rồi tới vùng kitchen.
    /// Kích thước khối highlight lấy từ <see cref="Court.GetHalfCourtBounds"/> và
    /// <see cref="Court.GetKitchenBounds"/> nên luôn khớp với sân thật.
    /// </summary>
    public class CourtSidesAndKitchenInfoTutorialState : BaseTutorialState
    {
        private const string SidesMessage =
            "The net splits the court in two. You play on your half, your opponent on the other.";

        private const string KitchenMessage =
            "The area next to the net is the Kitchen. You cannot volley while standing inside it.";

        /// <summary>Số giây hiện phần giới thiệu hai nửa sân trước khi chuyển sang kitchen.</summary>
        private const float SidesDuration = ThreeSeconds;

        /// <summary>Tổng thời lượng của cả bước.</summary>
        private const float TotalDuration = SidesDuration + FiveSeconds;

        private bool kitchenShown;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public CourtSidesAndKitchenInfoTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.CourtSidesAndKitchenInfo;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            kitchenShown = false;

            ShowMessage(SidesMessage);
            ShowHalfCourtHighlights();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!kitchenShown && ElapsedTime >= SidesDuration)
            {
                kitchenShown = true;

                HideHalfCourtHighlights();
                ShowKitchenHighlight();
                ShowMessage(KitchenMessage);
            }

            if (ElapsedTime >= TotalDuration) Complete();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            HideHalfCourtHighlights();
            SetActive(context != null ? context.kitchenHighlight : null, false);
        }

        /// <summary>Bật hai khối highlight nửa sân và khớp chúng với kích thước sân thật.</summary>
        private void ShowHalfCourtHighlights()
        {
            Court court = CourtRef;
            if (context == null) return;

            FitToBounds(context.positiveCourtHighlight,
                court != null ? court.GetHalfCourtBounds(true) : null);
            FitToBounds(context.negativeCourtHighlight,
                court != null ? court.GetHalfCourtBounds(false) : null);
        }

        private void HideHalfCourtHighlights()
        {
            if (context == null) return;

            SetActive(context.positiveCourtHighlight, false);
            SetActive(context.negativeCourtHighlight, false);
        }

        /// <summary>Bật khối highlight kitchen và khớp với vùng cấm volley của cả hai bên.</summary>
        private void ShowKitchenHighlight()
        {
            Court court = CourtRef;
            if (context == null) return;

            if (court == null)
            {
                SetActive(context.kitchenHighlight, true);
                return;
            }

            // Kitchen của hai bên đối xứng qua lưới → gộp thành một khối ở giữa.
            CourtBounds positive = court.GetKitchenBounds(true);
            CourtBounds full = new CourtBounds(
                new Vector2(positive.extends.x, court.kitchenDepth),
                Vector3.zero);

            FitToBounds(context.kitchenHighlight, full);
        }

        /// <summary>Đặt một khối highlight trùng khít với vùng sân cho trước.</summary>
        /// <param name="highlight">Khối cần đặt; bỏ qua nếu null.</param>
        /// <param name="bounds">Vùng sân đích; null thì chỉ bật khối lên.</param>
        private static void FitToBounds(GameObject highlight, CourtBounds bounds)
        {
            if (highlight == null) return;

            if (bounds != null)
            {
                Transform t = highlight.transform;
                t.position = new Vector3(bounds.center.x, t.position.y, bounds.center.z);

                Vector2 size = bounds.Size;
                t.localScale = new Vector3(size.x, t.localScale.y, size.y);
            }

            highlight.SetActive(true);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
