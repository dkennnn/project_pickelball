using System.Collections;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Vẽ vệt sáng theo đường vuốt của người chơi bằng <see cref="LineRenderer"/>.
    /// <para>
    /// Lắng nghe <c>InputManager.Instance.onSwipe</c>: <see cref="SwipePhase.Start"/> khởi tạo vệt,
    /// <see cref="SwipePhase.Progress"/> nối thêm điểm, <see cref="SwipePhase.End"/> làm mượt
    /// (Catmull-Rom) rồi tự xoá sau <see cref="ClearDelay"/> giây.
    /// </para>
    /// </summary>
    public class SwipeTracer : Singleton<SwipeTracer>
    {
        /// <summary>LineRenderer dùng để vẽ; nếu bỏ trống sẽ tự lấy trên cùng GameObject.</summary>
        [SerializeField] private LineRenderer lineRenderer;

        /// <summary>Độ nâng vệt vẽ lên khỏi mặt sân để tránh z-fighting.</summary>
        [SerializeField] private float heightOffset = 0.02f;

        /// <summary>Độ sâu chiếu khi cần chuyển một điểm màn hình sang world (<see cref="AddScreenPoint"/>).</summary>
        [SerializeField] private float zDepth = 10f;

        /// <summary>Số đoạn nội suy giữa hai điểm gốc khi làm mượt.</summary>
        [SerializeField, Range(1, 10)] private int smoothingSubdivisions = 4;

        /// <summary>Khoảng cách world tối thiểu giữa hai điểm liên tiếp.</summary>
        [SerializeField] private float minPointDistance = 0.02f;

        /// <summary>Thời gian giữ vệt trên màn hình sau khi cú vuốt kết thúc.</summary>
        private const float ClearDelay = 0.5f;

        private readonly List<Vector3> points = new List<Vector3>();
        private Camera _camera;
        private Coroutine clearRoutine;
        private bool subscribed;

        protected override void OnAwake()
        {
            base.OnAwake();

            _camera = Camera.main;
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = 0;
            }
        }

        private IEnumerator Start()
        {
            // InputManager có thể được tạo ở scene khác / muộn hơn -> chờ tới khi sẵn sàng.
            yield return new WaitUntil(() => InputManager.HasInstance);

            InputManager.Instance.onSwipe += OnSwipe;
            subscribed = true;
        }

        protected override void OnDestroy()
        {
            if (subscribed && InputManager.HasInstance)
            {
                InputManager.Instance.onSwipe -= OnSwipe;
                subscribed = false;
            }

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // API công khai
        // ------------------------------------------------------------------

        /// <summary>
        /// Thêm một điểm (world space) vào vệt vuốt. Điểm quá gần điểm trước sẽ bị bỏ qua.
        /// </summary>
        public void AddPoint(Vector3 point)
        {
            point.y += heightOffset;

            if (points.Count > 0 && (points[points.Count - 1] - point).sqrMagnitude < minPointDistance * minPointDistance)
            {
                return;
            }

            points.Add(point);
            ApplyToRenderer(points);
        }

        /// <summary>Thêm một điểm từ toạ độ MÀN HÌNH, chiếu ra world ở khoảng cách <c>zDepth</c>.</summary>
        public void AddScreenPoint(Vector2 screenPosition)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            AddPoint(_camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDepth)));
        }

        /// <summary>
        /// Làm mượt vệt vuốt bằng nội suy Catmull-Rom rồi đẩy kết quả vào <see cref="LineRenderer"/>.
        /// </summary>
        public void SmoothenSwipe()
        {
            if (points.Count < 3)
            {
                ApplyToRenderer(points);
                return;
            }

            List<Vector3> smoothed = new List<Vector3>((points.Count - 1) * smoothingSubdivisions + 1);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[Mathf.Max(i - 1, 0)];
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

                for (int step = 0; step < smoothingSubdivisions; step++)
                {
                    float t = step / (float)smoothingSubdivisions;
                    smoothed.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            smoothed.Add(points[points.Count - 1]);
            ApplyToRenderer(smoothed);
        }

        /// <summary>Xoá toàn bộ vệt vuốt ngay lập tức.</summary>
        public void Clear()
        {
            points.Clear();
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        private void OnSwipe(SwipeData swipeData)
        {
            if (swipeData == null) return;

            switch (swipeData.SwipePhase)
            {
                case SwipePhase.Start:
                    CancelScheduledClear();
                    Clear();
                    AddPoint(swipeData.StartPoint);
                    break;

                case SwipePhase.Progress:
                    AddPoint(swipeData.EndPoint);
                    break;

                case SwipePhase.End:
                    AddPoint(swipeData.EndPoint);
                    SmoothenSwipe();
                    ScheduleClear();
                    break;

                case SwipePhase.Invalid:
                    ScheduleClear();
                    break;
            }
        }

        private void ScheduleClear()
        {
            CancelScheduledClear();
            clearRoutine = DelayedAction.Run(ClearDelay, Clear);
        }

        private void CancelScheduledClear()
        {
            if (clearRoutine == null) return;
            DelayedAction.Cancel(clearRoutine);
            clearRoutine = null;
        }

        private void ApplyToRenderer(List<Vector3> source)
        {
            if (lineRenderer == null) return;

            lineRenderer.positionCount = source.Count;
            for (int i = 0; i < source.Count; i++) lineRenderer.SetPosition(i, source[i]);
        }

        /// <summary>Nội suy Catmull-Rom giữa <paramref name="p1"/> và <paramref name="p2"/>.</summary>
        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
