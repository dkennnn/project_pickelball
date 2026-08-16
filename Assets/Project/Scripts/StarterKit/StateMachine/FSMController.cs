using System;
using System.Collections.Generic;

namespace StarterKit.StateMachine
{
    /// <summary>
    /// FSM tổng quát dùng khoá enum. Bảo đảm Exit() của state cũ luôn chạy trước Enter() của state mới.
    /// </summary>
    public class FSMController<TKey> where TKey : struct, Enum
    {
        private readonly Dictionary<TKey, IState> _states = new Dictionary<TKey, IState>();

        /// <summary>Khoá của state đang chạy.</summary>
        public TKey CurrentKey { get; private set; }

        /// <summary>Khoá của state ngay trước đó.</summary>
        public TKey PreviousKey { get; private set; }

        /// <summary>State đang chạy, có thể null nếu FSM chưa khởi động.</summary>
        public IState Current { get; private set; }

        /// <summary>Phát ra sau mỗi lần chuyển state thành công: (from, to).</summary>
        public event Action<TKey, TKey> OnStateChanged;

        public void Register(TKey key, IState state) => _states[key] = state;

        public bool Has(TKey key) => _states.ContainsKey(key);

        public IState Get(TKey key) => _states.TryGetValue(key, out var s) ? s : null;

        /// <summary>Chuyển sang state mới. Trả về false nếu key chưa đăng ký.</summary>
        public bool ChangeState(TKey key)
        {
            if (!_states.TryGetValue(key, out var next)) return false;

            var from = CurrentKey;
            Current?.Exit();
            PreviousKey = from;
            CurrentKey = key;
            Current = next;
            Current.Enter();
            OnStateChanged?.Invoke(from, key);
            return true;
        }

        /// <summary>Gọi mỗi frame từ MonoBehaviour chủ.</summary>
        public void Tick() => Current?.Update();

        /// <summary>Thoát state hiện tại mà không vào state mới.</summary>
        public void Stop()
        {
            Current?.Exit();
            Current = null;
        }

        public void Clear()
        {
            Stop();
            _states.Clear();
        }
    }
}
