using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Pickleball
{
    /// <summary>
    /// Bộ đọc input swipe/tap của gameplay (legacy <see cref="UnityEngine.Input"/>: hỗ trợ cả
    /// cảm ứng trên thiết bị lẫn chuột trong Editor).
    ///
    /// <para><b>QUY TẮC MAP INPUT → CÚ ĐÁNH</b> (được các hệ thống đánh bóng tiêu thụ):</para>
    /// <list type="bullet">
    /// <item><description><b>Vuốt dài + nhanh</b> → <see cref="SwipeData.Velocity"/> lớn và
    /// <see cref="SwipeData.DestinationPoint"/> xa → cú đánh mạnh, quỹ đạo phẳng (drive/smash).</description></item>
    /// <item><description><b>Vuốt ngắn + chậm</b> → velocity thấp, điểm rơi gần lưới → drop shot / dink.</description></item>
    /// <item><description><b>Vuốt lên trên</b> (Direction hướng về phía sân đối phương, độ dài trung bình
    /// nhưng vận tốc thấp) → lob: điểm rơi sâu, thời gian bay dài.</description></item>
    /// <item><description><b>Vuốt cong</b> → <see cref="SwipeData.CurveDirection"/> cho biết xoáy sang
    /// trái hay phải, <see cref="SwipeData.CurveIntensity"/> cho biết mức topspin/slice áp dụng.</description></item>
    /// </list>
    ///
    /// <para>
    /// Swipe bắt đầu trên UI sẽ bị bỏ qua hoàn toàn (<see cref="IsPointerOverUI"/>).
    /// Trong lúc kéo, <see cref="onSwipe"/> được bắn liên tục với <see cref="SwipePhase.Progress"/>
    /// để UI vẽ marker điểm rơi theo thời gian thực.
    /// </para>
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        /// <summary>Bật để vẽ debug đường vuốt trong Scene view.</summary>
        public bool visualizeSwipe;

        /// <summary>Layer của mặt sân dùng để raycast toạ độ màn hình xuống world.</summary>
        public LayerMask groundLayer;

        /// <summary>Callback mỗi khi có dữ liệu swipe mới (Start / Progress / End / Invalid).</summary>
        public UnityAction<SwipeData> onSwipe;

        /// <summary>Callback khi người chơi chạm nhanh (không đủ ngưỡng swipe).</summary>
        public UnityAction<Vector3> onTap;

        [Header("Swipe Thresholds")]
        [SerializeField] private float swipeTimeThreshold = 0.2f;
        [SerializeField] private float swipeDistanceThreshold = 40f;
        [SerializeField] private float maxSwipeTime = 1.5f;
        [SerializeField] private float maxSwipeDistance = 600f;
        [SerializeField] private float swipeVelocityMultiplier = 1f;
        [SerializeField] private float minCurveIntensity = 0f;
        [SerializeField] private float maxCurveIntensity = 1f;

        [Header("Destination")]
        [SerializeField] private float destinationDistanceMultiplier = 0.02f;

        private Vector2 startTouchPosition;
        private Vector3 startWorldPosition;
        private float startTime;
        private bool isSwipeDetected;
        private bool isSwipeProcessed;
        private bool wasPointerOverUI;
        private Camera mainCamera;
        private readonly List<Vector3> swipePath = new List<Vector3>();
        private Vector3 lastSwipePosition;
        private Vector3 currentDestination;

        /// <summary>Khoảng cách world tối thiểu giữa hai điểm liên tiếp được ghi vào đường vuốt.</summary>
        private const float MinPathPointDistanceSqr = 0.0004f;

        /// <summary>Số điểm tối đa lưu trong một đường vuốt (chống phình bộ nhớ khi kéo lâu).</summary>
        private const int MaxPathPoints = 128;

        /// <summary>Bật/tắt toàn bộ việc đọc input (tắt khi đang ở menu, cutscene, replay...).</summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>Điểm rơi dự kiến gần nhất được tính từ cú vuốt đang diễn ra.</summary>
        public Vector3 CurrentDestination => currentDestination;

        protected override void OnAwake()
        {
            base.OnAwake();
            mainCamera = Camera.main;
            if (groundLayer.value == 0) groundLayer = LayerMask.GetMask(StringConstants.GroundLayer);
        }

        private void Update()
        {
            if (!InputEnabled) return;
            HandleInput();
        }

        // ------------------------------------------------------------------
        // Đọc input thô
        // ------------------------------------------------------------------

        /// <summary>
        /// Đọc input thô mỗi frame: ưu tiên ngón chạm đầu tiên, nếu không có touch thì dùng chuột trái.
        /// </summary>
        public void HandleInput()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        StartTouch(touch.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        MoveTouch(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        EndTouch(touch.position);
                        break;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0)) StartTouch(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) EndTouch(Input.mousePosition);
            else if (Input.GetMouseButton(0)) MoveTouch(Input.mousePosition);
        }

        /// <summary>Bắt đầu một cú vuốt tại toạ độ màn hình <paramref name="position"/>.</summary>
        public void StartTouch(Vector2 position)
        {
            wasPointerOverUI = IsPointerOverUI(position);
            if (wasPointerOverUI) return;

            startTouchPosition = position;
            startTime = Time.time;
            isSwipeDetected = false;
            isSwipeProcessed = false;

            startWorldPosition = GetWorldPositionFromScreen(position);
            lastSwipePosition = startWorldPosition;
            currentDestination = startWorldPosition;

            swipePath.Clear();
            swipePath.Add(startWorldPosition);

            ProcessSwipe(position, 0f, 0f, SwipePhase.Start);
        }

        /// <summary>
        /// Cập nhật cú vuốt đang diễn ra: ghi thêm điểm vào đường vuốt và bắn
        /// <see cref="SwipePhase.Progress"/> khi đã vượt ngưỡng khoảng cách.
        /// </summary>
        public void MoveTouch(Vector2 position)
        {
            if (wasPointerOverUI || isSwipeProcessed) return;

            Vector3 world = GetWorldPositionFromScreen(position);
            if ((world - lastSwipePosition).sqrMagnitude > MinPathPointDistanceSqr && swipePath.Count < MaxPathPoints)
            {
                swipePath.Add(world);
                lastSwipePosition = world;
            }

            float distance = Vector2.Distance(startTouchPosition, position);
            float duration = Time.time - startTime;

            if (distance >= swipeDistanceThreshold) isSwipeDetected = true;
            if (!isSwipeDetected) return;

            ProcessSwipe(position, distance, duration, SwipePhase.Progress);

            if (visualizeSwipe) Debug.DrawLine(startWorldPosition, world, Color.yellow, 0.1f);
        }

        /// <summary>
        /// Kết thúc cú vuốt: phân loại thành tap, swipe hợp lệ hoặc swipe không hợp lệ (quá chậm/quá ngắn).
        /// </summary>
        public void EndTouch(Vector2 position)
        {
            if (wasPointerOverUI)
            {
                wasPointerOverUI = false;
                return;
            }

            float distance = Vector2.Distance(startTouchPosition, position);
            float duration = Time.time - startTime;

            // Tap: chưa đạt ngưỡng khoảng cách và thời gian chạm ngắn.
            if (!IsValidSwipe(startTouchPosition, position))
            {
                if (duration < swipeTimeThreshold) onTap?.Invoke(GetWorldPositionFromScreen(position));
                else ProcessSwipe(position, distance, duration, SwipePhase.Invalid);

                isSwipeDetected = false;
                isSwipeProcessed = true;
                return;
            }

            // Vuốt quá lâu -> coi như không hợp lệ (người chơi giữ tay quá lâu).
            SwipePhase phase = duration > maxSwipeTime ? SwipePhase.Invalid : SwipePhase.End;
            ProcessSwipe(position, distance, duration, phase);

            isSwipeDetected = false;
            isSwipeProcessed = true;
        }

        // ------------------------------------------------------------------
        // Phân tích swipe
        // ------------------------------------------------------------------

        /// <summary>
        /// Đóng gói cú vuốt thành <see cref="SwipeData"/> và bắn <see cref="onSwipe"/>.
        /// </summary>
        /// <param name="position">Toạ độ màn hình hiện tại.</param>
        /// <param name="distance">Khoảng cách vuốt tính bằng pixel.</param>
        /// <param name="duration">Thời gian vuốt tính bằng giây.</param>
        /// <param name="phase">Giai đoạn của cú vuốt.</param>
        public void ProcessSwipe(Vector2 position, float distance, float duration, SwipePhase phase)
        {
            Vector3 endWorld = GetWorldPositionFromScreen(position);

            Vector3 delta = endWorld - startWorldPosition;
            delta.y = 0f;

            Vector3 direction = delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.zero;

            // Vuốt dài + nhanh => velocity lớn => lực mạnh, quỹ đạo phẳng.
            // Vuốt ngắn + chậm => velocity nhỏ => drop shot / dink.
            float clampedDistance = Mathf.Clamp(distance, 0f, maxSwipeDistance);
            float clampedDuration = Mathf.Clamp(duration, 0.0001f, maxSwipeTime);
            float velocity = (clampedDistance / clampedDuration) * swipeVelocityMultiplier;

            (Vector3 curveDirection, float curveIntensity) = AnalyzeSwipeCurve(swipePath);

            Vector3 destination = CalculateDestinationPoint(direction, clampedDistance);

            SwipeData data = new SwipeData(
                direction,
                velocity,
                startWorldPosition,
                endWorld,
                delta.magnitude,
                phase,
                new List<Vector3>(swipePath),
                curveDirection,
                curveIntensity,
                destination);

            onSwipe?.Invoke(data);
        }

        /// <summary>
        /// Phân tích độ cong của đường vuốt.
        /// </summary>
        /// <param name="path">Danh sách điểm world của đường vuốt.</param>
        /// <returns>
        /// <c>dir</c> = vector vuông góc chỉ chiều xoáy (phải/trái), <c>intensity</c> = cường độ xoáy
        /// đã chuẩn hoá trong [<c>minCurveIntensity</c>, <c>maxCurveIntensity</c>].
        /// </returns>
        public (Vector3 dir, float intensity) AnalyzeSwipeCurve(List<Vector3> path)
        {
            if (path == null || path.Count < 3) return (Vector3.zero, minCurveIntensity);

            Vector3 start = path[0];
            Vector3 end = path[path.Count - 1];

            float intensity = CalculateCurveAmount(path, start, end);
            Vector3 direction = CalculateCurveDirection(path);

            if (direction == Vector3.zero) intensity = minCurveIntensity;

            return (direction, intensity);
        }

        /// <summary>
        /// Đo mức cong trung bình của đường vuốt so với đoạn thẳng start→end.
        /// <para>
        /// Với mỗi điểm giữa: lấy khoảng cách vuông góc tới đoạn thẳng
        /// (<see cref="Utilities.DistanceToSegment"/>) rồi gán DẤU theo
        /// <c>Mathf.Sign(Vector3.Cross(end - start, p - start).y)</c> — nhờ có dấu, một đường vuốt
        /// zigzag sẽ triệt tiêu lẫn nhau và cho độ cong gần 0, còn đường cong đều một chiều thì cộng dồn.
        /// Trung bình có dấu được chia cho độ dài đoạn start→end để chuẩn hoá, cuối cùng lấy
        /// giá trị tuyệt đối và clamp vào [minCurveIntensity, maxCurveIntensity].
        /// </para>
        /// </summary>
        public float CalculateCurveAmount(List<Vector3> points, Vector3 start, Vector3 end)
        {
            if (points == null || points.Count < 3) return minCurveIntensity;

            Vector3 axis = end - start;
            axis.y = 0f;
            float axisLength = axis.magnitude;
            if (axisLength < 0.00001f) return minCurveIntensity;

            float signedSum = 0f;
            int count = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 p = points[i];
                float perpendicular = Utilities.DistanceToSegment(p, start, end);
                float sign = Mathf.Sign(Vector3.Cross(end - start, p - start).y);
                signedSum += perpendicular * sign;
                count++;
            }

            if (count == 0) return minCurveIntensity;

            float normalized = (signedSum / count) / axisLength;
            return Mathf.Clamp(Mathf.Abs(normalized), minCurveIntensity, maxCurveIntensity);
        }

        /// <summary>
        /// Suy ra CHIỀU xoáy của cú vuốt.
        /// <para>
        /// So sánh vector đầu (<c>points[1] - points[0]</c>) với vector cuối
        /// (<c>last - secondLast</c>) qua <c>Vector3.Cross(firstDir, lastDir).y</c>:
        /// dấu dương = đường vuốt bẻ sang PHẢI, dấu âm = bẻ sang TRÁI.
        /// Kết quả trả về là vector vuông góc với hướng vuốt tổng thể, nằm trong mặt phẳng XZ:
        /// <c>Vector3.Cross(Vector3.up, overallDirection).normalized * sign</c>.
        /// </para>
        /// </summary>
        public Vector3 CalculateCurveDirection(List<Vector3> points)
        {
            if (points == null || points.Count < 3) return Vector3.zero;

            Vector3 firstDir = points[1] - points[0];
            Vector3 lastDir = points[points.Count - 1] - points[points.Count - 2];
            firstDir.y = 0f;
            lastDir.y = 0f;

            if (firstDir.sqrMagnitude < 0.000001f || lastDir.sqrMagnitude < 0.000001f) return Vector3.zero;

            float turn = Vector3.Cross(firstDir.normalized, lastDir.normalized).y;
            if (Mathf.Abs(turn) < 0.0001f) return Vector3.zero;

            float sign = Mathf.Sign(turn);

            Vector3 overallDirection = points[points.Count - 1] - points[0];
            overallDirection.y = 0f;
            if (overallDirection.sqrMagnitude < 0.000001f) return Vector3.zero;

            return Vector3.Cross(Vector3.up, overallDirection.normalized).normalized * sign;
        }

        /// <summary>True nếu quãng đường vuốt trên màn hình đủ dài để tính là swipe (không phải tap).</summary>
        public bool IsValidSwipe(Vector2 start, Vector2 end)
        {
            return Vector2.Distance(start, end) >= swipeDistanceThreshold;
        }

        // ------------------------------------------------------------------
        // Tiện ích toạ độ
        // ------------------------------------------------------------------

        /// <summary>
        /// True nếu con trỏ đang nằm trên một phần tử UI (dùng EventSystem, có raycast dự phòng).
        /// </summary>
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (eventSystem.IsPointerOverGameObject()) return true;

            // Dự phòng cho thiết bị cảm ứng: raycast thủ công qua EventSystem.
            PointerEventData pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            return results.Count > 0;
        }

        /// <summary>
        /// Chiếu một toạ độ màn hình xuống mặt sân: raycast vào <see cref="groundLayer"/>,
        /// nếu trượt thì cắt với mặt phẳng <c>y = 0</c>.
        /// </summary>
        public Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
        {
            Camera cam = GetCamera();
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(screenPosition);

            if (groundLayer.value != 0 && Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
            {
                return hit.point;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance)) return ray.GetPoint(distance);

            return Vector3.zero;
        }

        /// <summary>Chuyển toạ độ màn hình sang toạ độ viewport [0..1].</summary>
        public Vector2 GetViewportPoint(Vector2 screenPosition)
        {
            Camera cam = GetCamera();
            if (cam == null) return Vector2.zero;
            return cam.ScreenToViewportPoint(screenPosition);
        }

        /// <summary>
        /// Biến hướng + quãng đường vuốt thành điểm rơi trên sân:
        /// <c>startWorldPosition + direction * deltaDistance * destinationDistanceMultiplier</c>.
        /// Vuốt càng dài thì điểm rơi càng xa (bóng càng sâu).
        /// </summary>
        /// <param name="direction">Hướng vuốt (world, sẽ được ép về mặt phẳng XZ).</param>
        /// <param name="deltaDistance">Quãng đường vuốt trên màn hình (pixel).</param>
        public Vector3 CalculateDestinationPoint(Vector3 direction, float deltaDistance)
        {
            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            if (flat.sqrMagnitude < 0.000001f)
            {
                currentDestination = startWorldPosition;
                return currentDestination;
            }

            flat.Normalize();
            float travel = Mathf.Clamp(deltaDistance, 0f, maxSwipeDistance) * destinationDistanceMultiplier;

            Vector3 destination = startWorldPosition + flat * travel;
            destination.y = 0f;

            currentDestination = destination;
            return destination;
        }

        private Camera GetCamera()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            return mainCamera;
        }
    }
}
