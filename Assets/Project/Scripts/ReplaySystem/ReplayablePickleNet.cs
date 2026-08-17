using Pickleball;
using UnityEngine;
using UnityEngine.Events;

namespace ReplaySystem
{
    /// <summary>
    /// Bản <see cref="Replayable"/> dành riêng cho lưới: ngoài transform còn ghi lại trạng thái
    /// gợn sóng (ripple) của <see cref="PickleNet"/> để đoạn quay chậm tái hiện đúng lúc bóng chạm lưới.
    /// <para>
    /// GIỚI HẠN ĐÃ BIẾT: <c>Gameplay/PickleNet.cs</c> chỉ phơi ra <c>rippleStrength</c>,
    /// <c>rippleWidth</c>, <c>rippleDirection</c>, <c>maxRippleTime</c>, <c>brightness</c>.
    /// Tâm sóng (<c>rippleOrigin</c>) và thời gian sóng (<c>rippleTime</c>) là private nên KHÔNG
    /// ghi lại được — replay tái hiện đúng cường độ/bề rộng sóng nhưng tâm sóng giữ nguyên như
    /// lần va chạm gần nhất. Muốn chính xác tuyệt đối thì PickleNet cần thêm property đọc
    /// tâm sóng (xem báo cáo).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ReplayablePickleNet : Replayable
    {
        /// <summary>Một khung trạng thái ripple.</summary>
        private struct NetReplayFrame
        {
            public float time;
            public float rippleStrength;
            public float rippleWidth;
            public float maxRippleTime;
            public float brightness;
            public Vector3 rippleDirection;
        }

        [Tooltip("Lưới cần ghi. Bỏ trống thì tự tìm trên GameObject này hoặc trong con.")]
        [SerializeField] private PickleNet pickleNet;

        [Tooltip("Phát mỗi khi trạng thái ripple được áp lại trong lúc replay.")]
        public UnityEvent OnReplayValueChanged = new UnityEvent();

        private NetReplayFrame[] netBuffer;
        private int netHead;
        private int netCount;

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            if (pickleNet == null) pickleNet = GetComponent<PickleNet>();
            if (pickleNet == null) pickleNet = GetComponentInChildren<PickleNet>(true);
        }

        /// <inheritdoc/>
        public override void ClearReplayData()
        {
            base.ClearReplayData();
            netHead = 0;
            netCount = 0;
        }

        /// <inheritdoc/>
        protected override void RecordFrame()
        {
            base.RecordFrame();

            if (pickleNet == null) return;
            EnsureNetBuffer();

            netBuffer[netHead] = new NetReplayFrame
            {
                time = GetFrame(FrameCount - 1).time,
                rippleStrength = pickleNet.rippleStrength,
                rippleWidth = pickleNet.rippleWidth,
                maxRippleTime = pickleNet.maxRippleTime,
                brightness = pickleNet.brightness,
                rippleDirection = pickleNet.rippleDirection
            };

            netHead = (netHead + 1) % netBuffer.Length;
            if (netCount < netBuffer.Length) netCount++;
        }

        /// <inheritdoc/>
        public override void Playback(float t)
        {
            base.Playback(t);

            if (pickleNet == null || netCount == 0) return;

            NetReplayFrame frame = SampleNetAt(t);
            pickleNet.rippleStrength = frame.rippleStrength;
            pickleNet.rippleWidth = frame.rippleWidth;
            pickleNet.maxRippleTime = frame.maxRippleTime;
            pickleNet.brightness = frame.brightness;
            pickleNet.rippleDirection = frame.rippleDirection;

            OnReplayValueChanged?.Invoke();
        }

        private NetReplayFrame SampleNetAt(float t)
        {
            if (netCount == 1) return GetNetFrame(0);

            for (int i = netCount - 1; i > 0; i--)
            {
                NetReplayFrame a = GetNetFrame(i - 1);
                NetReplayFrame b = GetNetFrame(i);
                if (t < a.time) continue;

                float span = b.time - a.time;
                float u = span <= 0f ? 0f : Mathf.Clamp01((t - a.time) / span);
                return new NetReplayFrame
                {
                    time = t,
                    rippleStrength = Mathf.Lerp(a.rippleStrength, b.rippleStrength, u),
                    rippleWidth = Mathf.Lerp(a.rippleWidth, b.rippleWidth, u),
                    maxRippleTime = Mathf.Lerp(a.maxRippleTime, b.maxRippleTime, u),
                    brightness = Mathf.Lerp(a.brightness, b.brightness, u),
                    rippleDirection = Vector3.Slerp(a.rippleDirection, b.rippleDirection, u)
                };
            }

            return GetNetFrame(0);
        }

        private NetReplayFrame GetNetFrame(int index)
        {
            if (netBuffer == null || netCount == 0) return default;

            int oldest = (netHead - netCount + netBuffer.Length) % netBuffer.Length;
            return netBuffer[(oldest + Mathf.Clamp(index, 0, netCount - 1)) % netBuffer.Length];
        }

        private void EnsureNetBuffer()
        {
            float step = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            int capacity = Mathf.Max(8, Mathf.CeilToInt(Mathf.Max(0.1f, maxSeconds) / step) + 1);

            if (netBuffer != null && netBuffer.Length == capacity) return;

            netBuffer = new NetReplayFrame[capacity];
            netHead = 0;
            netCount = 0;
        }
    }
}
