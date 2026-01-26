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
            if (s_instance == null)
            {
                s_instance = this as T;

                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}