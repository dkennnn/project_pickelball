using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>
    /// Singleton MonoBehaviour cơ bản. Kế thừa và override <see cref="OnAwake"/> thay vì Awake().
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        /// <summary>Thể hiện duy nhất; null nếu chưa có object nào trong scene.</summary>
        public static T Instance => _instance;

        /// <summary>True khi đã có thể hiện hợp lệ.</summary>
        public static bool HasInstance => _instance != null;

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

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
