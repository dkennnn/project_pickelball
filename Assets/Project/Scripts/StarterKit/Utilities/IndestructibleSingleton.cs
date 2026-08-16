using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>Singleton sống xuyên scene (DontDestroyOnLoad).</summary>
    public abstract class IndestructibleSingleton<T> : Singleton<T> where T : IndestructibleSingleton<T>
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
    }
}
