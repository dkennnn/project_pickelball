using System;
using System.Collections;
using UnityEngine;

namespace StarterKit.Utilities
{
    /// <summary>Chạy một callback sau N giây mà không cần MonoBehaviour riêng.</summary>
    public class DelayedAction : MonoBehaviour
    {
        private static DelayedAction _runner;

        private static DelayedAction Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[DelayedAction]");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<DelayedAction>();
                }
                return _runner;
            }
        }

        /// <summary>Gọi <paramref name="action"/> sau <paramref name="delay"/> giây (scaled time).</summary>
        public static Coroutine Run(float delay, Action action)
        {
            if (action == null) return null;
            return Runner.StartCoroutine(RunRoutine(delay, action, false));
        }

        /// <summary>Như <see cref="Run"/> nhưng dùng unscaled time (không bị slow-motion ảnh hưởng).</summary>
        public static Coroutine RunUnscaled(float delay, Action action)
        {
            if (action == null) return null;
            return Runner.StartCoroutine(RunRoutine(delay, action, true));
        }

        public static void Cancel(Coroutine routine)
        {
            if (routine != null && _runner != null) _runner.StopCoroutine(routine);
        }

        private static IEnumerator RunRoutine(float delay, Action action, bool unscaled)
        {
            if (delay > 0f)
            {
                if (unscaled) yield return new WaitForSecondsRealtime(delay);
                else yield return new WaitForSeconds(delay);
            }
            action.Invoke();
        }
    }
}
