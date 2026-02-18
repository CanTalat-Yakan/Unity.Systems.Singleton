using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// A generic singleton implementation designed for Unity components that persists
    /// across scenes. This class ensures that only one instance of the specified type
    /// exists and is not destroyed when loading a new scene.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the component that will be used as the singleton instance. It must
    /// inherit from UnityEngine.Component.
    /// </typeparam>
    /// <remarks>
    /// This class builds upon the basic singleton pattern by ensuring that the instance
    /// persists between scenes using Unity's DontDestroyOnLoad method. If a duplicate
    /// instance is detected, it is immediately destroyed.
    /// </remarks>
    /// <example>
    /// Recommended to be used as a base class for components that require a persistent
    /// singleton pattern, such as managers or controllers that need to span multiple scenes.
    /// </example>
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