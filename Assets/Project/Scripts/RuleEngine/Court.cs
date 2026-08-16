using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Mô hình hình học của sân pickleball và toàn bộ phép kiểm tra vùng (giao bóng, kitchen, biên sân).
    ///
    /// <para><b>QUY ƯỚC HỆ TOẠ ĐỘ (bắt buộc tuân thủ ở mọi hệ thống khác):</b></para>
    /// <list type="bullet">
    /// <item><description>Sân nằm trên mặt phẳng <b>XZ</b>, mặt sân ở <c>y = 0</c>.</description></item>
    /// <item><description>Mặt phẳng lưới nằm tại <b>z = 0</b>; sân trải dài theo trục <b>Z</b>.</description></item>
    /// <item><description><c>z &gt; 0</c> = <b>positive court</b>; <c>z &lt; 0</c> = <b>negative court</b>.
    /// Mọi API nhận cờ <c>isPositiveCourt</c> đều hiểu theo nghĩa này.</description></item>
    /// <item><description>Với <c>courtDimentions = (6.096, 13.4112)</c> thì
    /// <c>x ∈ [-3.048, 3.048]</c> và <c>z ∈ [-6.7056, 6.7056]</c>.</description></item>
    /// <item><description><b>Ô giao bóng Left/Right</b> luôn hiểu theo góc nhìn của người đứng ở nửa sân đó
    /// (hai người chơi quay mặt ngược nhau), nên:
    /// <br/>• positive court: <see cref="ServeSide.Right"/> → <c>x ∈ [0, halfWidth]</c>,
    /// <see cref="ServeSide.Left"/> → <c>x ∈ [-halfWidth, 0]</c>;
    /// <br/>• negative court: <see cref="ServeSide.Right"/> → <c>x ∈ [-halfWidth, 0]</c>,
    /// <see cref="ServeSide.Left"/> → <c>x ∈ [0, halfWidth]</c>.
    /// <br/>Nhờ quy ước này, ô giao <b>chéo sân</b> của một cú giao chỉ đơn giản là
    /// <c>GetServeBoxBounds(!isServerPositiveCourt, serveSide)</c> — cùng <see cref="ServeSide"/>,
    /// khác nửa sân, và tự động lệch nhau theo trục X.</description></item>
    /// <item><description>GameObject gắn <see cref="Court"/> phải đặt tại <b>world origin, không xoay,
    /// scale 1</b>. Toàn bộ phép tính bên dưới làm việc trực tiếp trên world space.</description></item>
    /// </list>
    /// </summary>
    public class Court : MonoBehaviour
    {
        /// <summary>Kích thước TOÀN SÂN theo mét: (bề ngang X, chiều dài Z). Mặc định 20ft × 44ft.</summary>
        public Vector2 courtDimentions = new Vector2(6.096f, 13.4112f);

        /// <summary>Chiều sâu vùng cấm volley (kitchen / non-volley zone) tính từ lưới, mặc định 7ft.</summary>
        public float kitchenDepth = 2.1336f;

        /// <summary>
        /// Khoảng lùi tối thiểu (mét) so với baseline khi hệ thống ngắm tự chọn điểm rơi,
        /// để AI/aim-assist không nhắm sát vạch cuối sân.
        /// </summary>
        public float targetDepthRestriction = 1f;

        /// <summary>Cấu hình trận đấu dùng chung (ballRadius, serveTimeout...).</summary>
        public GameSettings gameSettings;

        /// <summary>Nửa kích thước sân, cache lại từ <see cref="courtDimentions"/>.</summary>
        private Vector2 courtHalfDimentions;

        /// <summary>Khoảng lùi của vị trí đứng giao bóng so với baseline (người giao phải đứng SAU vạch).</summary>
        private const float ServeBaselineOffset = 0.25f;

        /// <summary>Chiều cao lưới dùng để vẽ Gizmos.</summary>
        private const float NetGizmoHeight = 0.86f;

        /// <summary>Nửa bề ngang sân theo trục X (mặc định 3.048).</summary>
        public float HalfWidth => HalfDimentions.x;

        /// <summary>Nửa chiều dài sân theo trục Z (mặc định 6.7056).</summary>
        public float HalfLength => HalfDimentions.y;

        /// <summary>
        /// Nửa kích thước sân, tự tính lại nếu chưa được khởi tạo.
        /// Cần thiết vì trong EditMode test <c>Awake()</c>/<c>Start()</c> không được Unity gọi.
        /// </summary>
        private Vector2 HalfDimentions
        {
            get
            {
                if (courtHalfDimentions.x <= 0f || courtHalfDimentions.y <= 0f) RecalculateDimentions();
                return courtHalfDimentions;
            }
        }

        private void Awake()
        {
            RecalculateDimentions();
        }

        private void Start()
        {
            RecalculateDimentions();
        }

        private void OnValidate()
        {
            RecalculateDimentions();
        }

        /// <summary>Tính lại cache nửa kích thước sân. Gọi sau khi đổi <see cref="courtDimentions"/> lúc runtime.</summary>
        public void RecalculateDimentions()
        {
            courtHalfDimentions = courtDimentions * 0.5f;
        }

        // ------------------------------------------------------------------
        // Kiểm tra luật vùng
        // ------------------------------------------------------------------

        /// <summary>
        /// Kiểm tra một cú giao bóng có rơi đúng ô giao <b>chéo sân</b> hay không.
        /// Hợp lệ khi đồng thời: (1) rơi trong nửa sân đối diện người giao,
        /// (2) nằm NGOÀI kitchen của bên nhận, (3) nằm trong ô giao chéo tương ứng.
        /// </summary>
        /// <param name="landingPosition">Điểm chạm đất đầu tiên của bóng (world space).</param>
        /// <param name="isServerPositiveCourt">True nếu người giao đứng ở nửa sân dương (z &gt; 0).</param>
        /// <param name="serveSide">Ô giao (Left/Right) mà người giao đang đứng.</param>
        public bool IsServeValid(Vector3 landingPosition, bool isServerPositiveCourt, ServeSide serveSide)
        {
            bool receiverPositiveCourt = !isServerPositiveCourt;

            // (1) phải rơi trong biên sân và ở nửa sân của người nhận.
            if (!IsBounceInCorrectArea(landingPosition, receiverPositiveCourt)) return false;

            // (2) rơi vào kitchen của bên nhận là lỗi giao bóng.
            if (IsInKitchen(landingPosition)) return false;

            // (3) phải nằm trong ô giao chéo sân: cùng ServeSide nhưng ở nửa sân đối diện.
            return GetServeBoxBounds(receiverPositiveCourt, serveSide).Contains(landingPosition);
        }

        /// <summary>
        /// True nếu điểm nảy nằm trong biên sân VÀ đúng nửa sân được chỉ định.
        /// Điểm nằm đúng trên vạch được coi là trong sân (luật pickleball: vạch tính là in,
        /// riêng vạch kitchen khi giao bóng thì tính là lỗi — xử lý ở <see cref="IsServeValid"/>).
        /// </summary>
        /// <param name="bouncePosition">Vị trí nảy trong world space.</param>
        /// <param name="isPositiveCourt">Nửa sân mong đợi: true = z &gt; 0.</param>
        public bool IsBounceInCorrectArea(Vector3 bouncePosition, bool isPositiveCourt)
        {
            if (!IsInsideCourt(bouncePosition)) return false;
            return isPositiveCourt ? bouncePosition.z >= 0f : bouncePosition.z <= 0f;
        }

        /// <summary>
        /// True nếu vị trí nằm trong vùng kitchen (non-volley zone) của BẤT KỲ bên nào:
        /// <c>|z| &lt; kitchenDepth</c> và vẫn trong biên ngang của sân.
        /// </summary>
        public bool IsInKitchen(Vector3 position)
        {
            return Mathf.Abs(position.z) < kitchenDepth
                && Mathf.Abs(position.x) <= HalfWidth;
        }

        /// <summary>True nếu vị trí nằm trong biên sân (cả hai nửa), xét trên mặt phẳng XZ.</summary>
        public bool IsInsideCourt(Vector3 position)
        {
            return Mathf.Abs(position.x) <= HalfWidth
                && Mathf.Abs(position.z) <= HalfLength;
        }

        // ------------------------------------------------------------------
        // Truy vấn vùng
        // ------------------------------------------------------------------

        /// <summary>
        /// Vùng ô giao bóng của một nửa sân: kéo dài từ vạch kitchen tới baseline,
        /// rộng nửa sân theo trục X (xem quy ước Left/Right ở phần mô tả lớp).
        /// </summary>
        /// <param name="isPositiveSideOfCourt">Nửa sân chứa ô giao (true = z &gt; 0).</param>
        /// <param name="serveSide">Ô trái hay phải theo góc nhìn của người đứng ở nửa sân đó.</param>
        public CourtBounds GetServeBoxBounds(bool isPositiveSideOfCourt, ServeSide serveSide)
        {
            float halfWidth = HalfWidth;
            float halfLength = HalfLength;

            // Chiều sâu ô giao = từ vạch kitchen tới baseline.
            float boxDepth = Mathf.Max(0f, halfLength - kitchenDepth);
            float halfBoxDepth = boxDepth * 0.5f;
            float zSign = isPositiveSideOfCourt ? 1f : -1f;
            float centerZ = zSign * (kitchenDepth + halfBoxDepth);

            // Right ở nửa sân dương nằm phía x > 0; ở nửa sân âm nằm phía x < 0 (người chơi quay ngược lại).
            bool usePositiveX = (serveSide == ServeSide.Right) == isPositiveSideOfCourt;
            float halfBoxWidth = halfWidth * 0.5f;
            float centerX = usePositiveX ? halfBoxWidth : -halfBoxWidth;

            return new CourtBounds(new Vector2(halfBoxWidth, halfBoxDepth), new Vector3(centerX, 0f, centerZ));
        }

        /// <summary>
        /// Vị trí đứng giao bóng: canh giữa ô giao theo trục X và lùi ra SAU baseline một khoảng nhỏ
        /// (<see cref="ServeBaselineOffset"/>) đúng theo luật "chân người giao phải sau vạch cuối sân".
        /// </summary>
        public Vector3 GetServePosition(bool isPositiveSideOfCourt, ServeSide serveSide)
        {
            CourtBounds box = GetServeBoxBounds(isPositiveSideOfCourt, serveSide);
            float zSign = isPositiveSideOfCourt ? 1f : -1f;
            return new Vector3(box.center.x, 0f, zSign * (HalfLength + ServeBaselineOffset));
        }

        /// <summary>
        /// Khoảng X cho phép người giao bóng di chuyển ngang: <c>(minX, maxX)</c> của ô giao tương ứng.
        /// </summary>
        public Vector2 GetServeHorizontalClamp(bool isPositiveSideOfCourt, ServeSide serveSide)
        {
            CourtBounds box = GetServeBoxBounds(isPositiveSideOfCourt, serveSide);
            return new Vector2(box.MinX, box.MaxX);
        }

        /// <summary>Vùng kitchen (non-volley zone) của một nửa sân, tính từ lưới ra <see cref="kitchenDepth"/>.</summary>
        public CourtBounds GetKitchenBounds(bool isPositiveCourt)
        {
            float zSign = isPositiveCourt ? 1f : -1f;
            float halfDepth = kitchenDepth * 0.5f;
            return new CourtBounds(new Vector2(HalfWidth, halfDepth), new Vector3(0f, 0f, zSign * halfDepth));
        }

        /// <summary>Toàn bộ một nửa sân (từ lưới tới baseline).</summary>
        public CourtBounds GetHalfCourtBounds(bool isPositiveCourt)
        {
            float zSign = isPositiveCourt ? 1f : -1f;
            float halfLength = HalfLength;
            return new CourtBounds(new Vector2(HalfWidth, halfLength * 0.5f),
                                   new Vector3(0f, 0f, zSign * halfLength * 0.5f));
        }

        // ------------------------------------------------------------------
        // Gizmos
        // ------------------------------------------------------------------

        /// <summary>
        /// Vẽ khung sân (trắng), lưới (đỏ), hai vùng kitchen (vàng) và bốn ô giao bóng
        /// (cyan/xanh lá cho positive court, magenta/xanh dương cho negative court)
        /// để kiểm tra hệ toạ độ bằng mắt ngay trong Scene view.
        /// </summary>
        private void OnDrawGizmos()
        {
            RecalculateDimentions();

            float halfWidth = HalfWidth;
            float halfLength = HalfLength;

            // Khung sân.
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfWidth * 2f, 0.01f, halfLength * 2f));

            // Vạch giữa sân (đường chia hai ô giao).
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(new Vector3(0f, 0f, -halfLength), new Vector3(0f, 0f, halfLength));

            // Lưới tại z = 0.
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-halfWidth, 0f, 0f), new Vector3(halfWidth, 0f, 0f));
            Gizmos.DrawLine(new Vector3(-halfWidth, NetGizmoHeight, 0f), new Vector3(halfWidth, NetGizmoHeight, 0f));
            Gizmos.DrawLine(new Vector3(-halfWidth, 0f, 0f), new Vector3(-halfWidth, NetGizmoHeight, 0f));
            Gizmos.DrawLine(new Vector3(halfWidth, 0f, 0f), new Vector3(halfWidth, NetGizmoHeight, 0f));

            // Kitchen hai bên.
            Gizmos.color = Color.yellow;
            DrawBoundsGizmo(GetKitchenBounds(true));
            DrawBoundsGizmo(GetKitchenBounds(false));

            // Bốn ô giao bóng.
            Gizmos.color = Color.cyan;
            DrawBoundsGizmo(GetServeBoxBounds(true, ServeSide.Right));
            Gizmos.color = Color.green;
            DrawBoundsGizmo(GetServeBoxBounds(true, ServeSide.Left));
            Gizmos.color = Color.magenta;
            DrawBoundsGizmo(GetServeBoxBounds(false, ServeSide.Right));
            Gizmos.color = Color.blue;
            DrawBoundsGizmo(GetServeBoxBounds(false, ServeSide.Left));

            // Vị trí đứng giao bóng.
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(GetServePosition(true, ServeSide.Right), 0.15f);
            Gizmos.DrawWireSphere(GetServePosition(true, ServeSide.Left), 0.15f);
            Gizmos.DrawWireSphere(GetServePosition(false, ServeSide.Right), 0.15f);
            Gizmos.DrawWireSphere(GetServePosition(false, ServeSide.Left), 0.15f);
        }

        private static void DrawBoundsGizmo(CourtBounds bounds)
        {
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.extends.x * 2f, 0.02f, bounds.extends.y * 2f));
        }
    }
}
