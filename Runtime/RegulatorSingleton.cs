using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Ensures only the most recently initialized instance persists across scene loads (runtime).
    /// </summary>
    public class RegulatorSingleton<T> : Singleton<T> where T : Component
    {
        internal float _initializationTime;

        internal override void InitializeSingleton()
        {
            _initializationTime = Time.time;

            DontDestroyOnLoad(gameObject);

            foreach (var old in FindExistingInstances())
                if (old._initializationTime < _initializationTime)
                    Destroy(old.gameObject);

            if (s_instance == null)
                s_instance = this as T;
        }

        private IEnumerable<RegulatorSingleton<T>> FindExistingInstances()
        {
            var oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var old in oldInstances)
            {
                var rs = old.GetComponent<RegulatorSingleton<T>>();
                if (rs != null)
                    yield return rs;
            }
        }
    }
}