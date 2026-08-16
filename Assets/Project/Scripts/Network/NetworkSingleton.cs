using UnityEngine;

namespace Pickleball.Network
{
    /// <summary>
    /// Bản Phase 1 của NetworkSingleton: chạy hoàn toàn cục bộ (offline single-player).
    /// <para>
    /// Bản gốc kế thừa <c>Mirror.NetworkBehaviour</c>. Ở đây ta giữ NGUYÊN bộ hook vòng đời
    /// (<see cref="OnStartServer"/>, <see cref="OnStopServer"/>, <see cref="OnStartClient"/>,
    /// <see cref="OnStopClient"/>) và cờ <see cref="IsServer"/>/<see cref="IsClient"/> để khi
    /// gắn Mirror ở Phase 2 chỉ cần đổi lớp cha, không phải viết lại logic.
    /// </para>
    /// </summary>
    public abstract class NetworkSingleton<T> : MonoBehaviour where T : NetworkSingleton<T>
    {
        private static T _instance;

        public static T Instance => _instance;
        public static bool HasInstance => _instance != null;

        /// <summary>Ở Phase 1 luôn true — máy cục bộ vừa là server vừa là client (host).</summary>
        public bool IsServer { get; protected set; } = true;

        /// <summary>Ở Phase 1 luôn true.</summary>
        public bool IsClient { get; protected set; } = true;

        private bool _started;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            OnAwake();
        }

        protected virtual void OnAwake() { }

        protected virtual void Start()
        {
            if (_started) return;
            _started = true;
            if (IsServer) OnStartServer();
            if (IsClient) OnStartClient();
        }

        protected virtual void OnDestroy()
        {
            if (_started)
            {
                if (IsClient) OnStopClient();
                if (IsServer) OnStopServer();
                _started = false;
            }
            if (_instance == this) _instance = null;
        }

        public virtual void OnStartServer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopClient() { }
    }
}
