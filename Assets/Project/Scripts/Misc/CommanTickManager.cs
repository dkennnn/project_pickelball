using System.Collections.Generic;
using UnityEngine;

namespace CommanTickManager
{
    /// <summary>Nhận Update() tập trung thay vì mỗi MonoBehaviour tự có Update().</summary>
    public interface ITick
    {
        void Tick();
        void RegisterTick();
        void UnregisterTick();
    }

    public interface ILateTick
    {
        void LateTick();
        void RegisterLateTick();
        void UnregisterLateTick();
    }

    public interface IFixedTick
    {
        void FixedTick();
        void RegisterFixedTick();
        void UnregisterFixedTick();
    }

    /// <summary>
    /// Gom toàn bộ Update/LateUpdate/FixedUpdate của game vào một MonoBehaviour duy nhất.
    /// Tránh chi phí interop hàng trăm Update() rời rạc trên mobile.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ProcessingUpdate : MonoBehaviour
    {
        private static ProcessingUpdate _instance;

        private static readonly List<ITick> Ticks = new List<ITick>();
        private static readonly List<ILateTick> LateTicks = new List<ILateTick>();
        private static readonly List<IFixedTick> FixedTicks = new List<IFixedTick>();

        // Buffer để cho phép đăng ký/huỷ đăng ký ngay trong lúc đang duyệt.
        private static readonly List<ITick> TickBuffer = new List<ITick>();
        private static readonly List<ILateTick> LateBuffer = new List<ILateTick>();
        private static readonly List<IFixedTick> FixedBuffer = new List<IFixedTick>();

        public static ProcessingUpdate Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[ProcessingUpdate]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ProcessingUpdate>();
                }
                return _instance;
            }
        }

        public static void Add(ITick t) { _ = Instance; if (!Ticks.Contains(t)) Ticks.Add(t); }
        public static void Remove(ITick t) => Ticks.Remove(t);
        public static void Add(ILateTick t) { _ = Instance; if (!LateTicks.Contains(t)) LateTicks.Add(t); }
        public static void Remove(ILateTick t) => LateTicks.Remove(t);
        public static void Add(IFixedTick t) { _ = Instance; if (!FixedTicks.Contains(t)) FixedTicks.Add(t); }
        public static void Remove(IFixedTick t) => FixedTicks.Remove(t);

        private void Update()
        {
            TickBuffer.Clear();
            TickBuffer.AddRange(Ticks);
            for (int i = 0; i < TickBuffer.Count; i++) TickBuffer[i]?.Tick();
        }

        private void LateUpdate()
        {
            LateBuffer.Clear();
            LateBuffer.AddRange(LateTicks);
            for (int i = 0; i < LateBuffer.Count; i++) LateBuffer[i]?.LateTick();
        }

        private void FixedUpdate()
        {
            FixedBuffer.Clear();
            FixedBuffer.AddRange(FixedTicks);
            for (int i = 0; i < FixedBuffer.Count; i++) FixedBuffer[i]?.FixedTick();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            Ticks.Clear();
            LateTicks.Clear();
            FixedTicks.Clear();
        }
    }
}
