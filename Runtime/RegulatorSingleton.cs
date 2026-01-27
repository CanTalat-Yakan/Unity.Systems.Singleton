using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Ensures only the most recently initialized instance persists across scene loads (runtime).
    /// </summary>
    public class RegulatorSingleton<T> : Singleton<T> where T : Component
    {
        // Kept for debugging/inspection; not relied upon for correctness.
        internal float _initializationTime;

        internal override void InitializeSingleton()
        {
            _initializationTime = Time.time;

            // Ensure persistence root.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // "Last initialized wins": when this instance initializes, it becomes the singleton.
            // We don't rely on timestamps (ties can happen within the same frame).
            foreach (var other in FindExistingInstances())
            {
                if (other == null || other == this)
                    continue;

                Destroy(other.gameObject);
            }

            s_instance = this as T;
        }

        private static IEnumerable<RegulatorSingleton<T>> FindExistingInstances()
        {
            var instances = Object.FindObjectsByType<RegulatorSingleton<T>>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var rs in instances)
                if (rs != null)
                    yield return rs;
        }
    }
}