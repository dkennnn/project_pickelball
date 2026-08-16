using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>Gắn vào bất kỳ GameObject nào để giữ nó qua các lần load scene.</summary>
    public class DoNotDestroyOnLoadComponent : MonoBehaviour
    {
        private void Awake()
        {
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
    }
}
