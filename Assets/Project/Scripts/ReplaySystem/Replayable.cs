using UnityEngine;

namespace ReplaySystem
{
    /// <summary>
    /// Gắn lên object cần ghi lại để phát chậm (bóng, người chơi, lưới). Mỗi <c>FixedUpdate</c>
    /// component đẩy một khung <c>(time, position, rotation)</c> vào ring buffer dài
    /// <see cref="maxSeconds"/> giây; khung cũ nhất bị ghi đè khi buffer đầy.
    /// <para>
    /// Trong lúc phát lại, mọi <see cref="Rigidbody"/> trên object và con của nó được đặt
    /// kinematic để vật lý không "đánh nhau" với dữ liệu replay, rồi được trả về trạng thái cũ
    /// khi phát xong.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class Replayable : MonoBehaviour
    {
        /// <summary>Không gian toạ độ được ghi lại.</summary>
        public enum TrackingSpaceMode
        {
            /// <summary>Ghi localPosition / localRotation.</summary>
            Local = 0,

            /// <summary>Ghi position / rotation trong world.</summary>
            Global = 1
        }

        /// <summary>Một khung dữ liệu replay.</summary>
        protected struct ReplayFrame
        {
            /// <summary>Mốc thời gian tính từ lúc bắt đầu ghi, đơn vị giây.</summary>
            public float time;

            /// <summary>Vị trí tại khung này.</summary>
            public Vector3 position;

            /// <summary>Góc quay tại khung này.</summary>
            public Quaternion rotation;
        }

        [Tooltip("Ghi toạ độ local hay world.")]
        public TrackingSpaceMode trackingSpaceMode = TrackingSpaceMode.Global;

        [Tooltip("Độ dài tối đa của đoạn ghi, tính bằng giây. Vượt quá thì khung cũ nhất bị ghi đè.")]
        public float maxSeconds = 8f;

        [Tooltip("Tự đăng ký với ReplayManager khi được bật.")]
        public bool autoRegister = true;

        private ReplayFrame[] buffer;
        private int head;      // Vị trí ghi khung tiếp theo.
        private int count;     // Số khung đang có (tối đa = buffer.Length).
        private float recordStartTime;

        private Rigidbody[] rigidbodies;
        private bool[] rigidbodyKinematicStates;
        private bool physicsSuspended;

        /// <summary>True khi đang ghi.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>True khi đang phát lại.</summary>
        public bool IsPlayingBack { get; private set; }

        /// <summary>Số khung đang có trong buffer.</summary>
        public int FrameCount => count;

        protected virtual void Awake()
        {
            rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            rigidbodyKinematicStates = new bool[rigidbodies.Length];
        }

        protected virtual void OnEnable()
        {
            if (autoRegister && ReplayManager.HasInstance && ReplayManager.Instance != null)
                ReplayManager.Instance.RegisterReplayable(this);
        }

        protected virtual void OnDisable()
        {
            if (ReplayManager.HasInstance && ReplayManager.Instance != null)
                ReplayManager.Instance.DeregisterReplayable(this);

            StopRecording();
            StopPlayback();
        }

        /// <summary>Bắt đầu ghi từ đầu (xoá dữ liệu cũ).</summary>
        public void StartRecording()
        {
            EnsureBuffer();
            ClearReplayData();

            recordStartTime = Time.time;
            IsRecording = true;
            IsPlayingBack = false;

            // Ghi ngay khung đầu tiên để đoạn replay không bị trống nếu dừng ngay lập tức.
            RecordFrame();
        }

        /// <summary>Dừng ghi (giữ nguyên dữ liệu đã ghi).</summary>
        public void StopRecording()
        {
            IsRecording = false;
        }

        /// <summary>Xoá sạch dữ liệu đã ghi.</summary>
        public virtual void ClearReplayData()
        {
            head = 0;
            count = 0;
        }

        /// <summary>Tổng thời lượng đoạn đã ghi, tính bằng giây. Trả 0 nếu chưa có dữ liệu.</summary>
        public virtual float GetTotalRecordedDuration()
        {
            if (count < 2) return 0f;
            return GetFrame(count - 1).time - GetFrame(0).time;
        }

        /// <summary>Mốc thời gian của khung đầu tiên (thường là 0).</summary>
        public float GetStartTime()
        {
            return count == 0 ? 0f : GetFrame(0).time;
        }

        /// <summary>
        /// Đặt object về trạng thái tại thời điểm <paramref name="t"/> giây kể từ lúc bắt đầu ghi,
        /// nội suy tuyến tính giữa hai khung gần nhất.
        /// </summary>
        /// <param name="t">Mốc thời gian cần phát; tự clamp về đoạn đã ghi.</param>
        public virtual void Playback(float t)
        {
            if (count == 0) return;

            if (!IsPlayingBack)
            {
                IsPlayingBack = true;
                IsRecording = false;
                SuspendPhysics(true);
            }

            ReplayFrame frame = SampleAt(t);
            ApplyFrame(frame);
        }

        /// <summary>Kết thúc phát lại và trả vật lý về trạng thái trước đó.</summary>
        public virtual void StopPlayback()
        {
            if (!IsPlayingBack) return;
            IsPlayingBack = false;
            SuspendPhysics(false);
        }

        /// <summary>Nội suy ra khung tại thời điểm <paramref name="t"/>.</summary>
        /// <param name="t">Mốc thời gian, giây.</param>
        protected ReplayFrame SampleAt(float t)
        {
            if (count == 1) return GetFrame(0);

            float first = GetFrame(0).time;
            float last = GetFrame(count - 1).time;
            float clamped = Mathf.Clamp(t, first, last);

            // Tìm tuyến tính từ cuối về đầu: replay thường chạy tiến nên khung cần tìm ở gần cuối.
            for (int i = count - 1; i > 0; i--)
            {
                ReplayFrame a = GetFrame(i - 1);
                ReplayFrame b = GetFrame(i);
                if (clamped < a.time) continue;

                float span = b.time - a.time;
                float u = span <= 0f ? 0f : Mathf.Clamp01((clamped - a.time) / span);
                return new ReplayFrame
                {
                    time = clamped,
                    position = Vector3.Lerp(a.position, b.position, u),
                    rotation = Quaternion.Slerp(a.rotation, b.rotation, u)
                };
            }

            return GetFrame(0);
        }

        /// <summary>Áp một khung lên transform.</summary>
        /// <param name="frame">Khung cần áp.</param>
        protected virtual void ApplyFrame(ReplayFrame frame)
        {
            if (trackingSpaceMode == TrackingSpaceMode.Local)
            {
                transform.localPosition = frame.position;
                transform.localRotation = frame.rotation;
                return;
            }

            transform.position = frame.position;
            transform.rotation = frame.rotation;
        }

        private void FixedUpdate()
        {
            if (!IsRecording) return;
            RecordFrame();
        }

        /// <summary>Đẩy trạng thái hiện tại vào ring buffer.</summary>
        protected virtual void RecordFrame()
        {
            EnsureBuffer();

            buffer[head] = new ReplayFrame
            {
                time = Time.time - recordStartTime,
                position = trackingSpaceMode == TrackingSpaceMode.Local ? transform.localPosition : transform.position,
                rotation = trackingSpaceMode == TrackingSpaceMode.Local ? transform.localRotation : transform.rotation
            };

            head = (head + 1) % buffer.Length;
            if (count < buffer.Length) count++;
        }

        /// <summary>Lấy khung thứ <paramref name="index"/> tính từ khung cũ nhất.</summary>
        /// <param name="index">Chỉ số trong [0, <see cref="FrameCount"/>).</param>
        protected ReplayFrame GetFrame(int index)
        {
            if (buffer == null || count == 0) return default;

            int oldest = (head - count + buffer.Length) % buffer.Length;
            return buffer[(oldest + Mathf.Clamp(index, 0, count - 1)) % buffer.Length];
        }

        /// <summary>Cấp phát buffer theo <see cref="maxSeconds"/> và bước FixedUpdate hiện tại.</summary>
        protected void EnsureBuffer()
        {
            float step = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            int capacity = Mathf.Max(8, Mathf.CeilToInt(Mathf.Max(0.1f, maxSeconds) / step) + 1);

            if (buffer != null && buffer.Length == capacity) return;

            buffer = new ReplayFrame[capacity];
            head = 0;
            count = 0;
        }

        /// <summary>Bật/tắt kinematic cho mọi Rigidbody để vật lý không can thiệp lúc replay.</summary>
        /// <param name="suspend">True = treo vật lý.</param>
        private void SuspendPhysics(bool suspend)
        {
            if (rigidbodies == null || rigidbodies.Length == 0) return;
            if (suspend == physicsSuspended) return;

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null) continue;

                if (suspend)
                {
                    rigidbodyKinematicStates[i] = rb.isKinematic;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = rigidbodyKinematicStates[i];
                    if (rb.isKinematic) continue;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            physicsSuspended = suspend;
        }
    }
}
