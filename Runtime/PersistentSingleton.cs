using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a singleton component that persists across scene loads (runtime).
    /// </summary>
    public class PersistentSingleton<T> : Singleton<T> where T : Component
    {
        internal override void InitializeSingleton()
        {
            // Ensure this object becomes a root object before moving it to the DDOL scene.
            transform.SetParent(null);

            if (s_instance == null)
            {
                s_instance = this as T;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (s_instance != this)
                Destroy(gameObject);
            else
                DontDestroyOnLoad(gameObject);
        }
    }
}