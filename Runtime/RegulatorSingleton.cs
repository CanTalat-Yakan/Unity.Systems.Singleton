using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// A specialized implementation of the singleton pattern for Unity components
    /// that ensures only one instance of the class exists and persists across scene
    /// loads. This implementation prioritizes the most recently initialized object
    /// as the singleton instance, replacing any previous instances.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the component that will be used as the singleton instance.
    /// </typeparam>
    /// <remarks>
    /// This class builds upon the base Singleton class by adding additional behavior
    /// to manage multiple instances. Any previously existing instances of the
    /// singleton in the scene are automatically destroyed in favor of the
    /// newly initialized instance. The singleton instance is also set to persist
    /// across scene loads by default.
    /// </remarks>
    public class RegulatorSingleton<T> : Singleton<T> where T : Component
    {
        internal override void InitializeSingleton()
        {
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