using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEssentials
{
    /// <summary>
    /// A generic Singleton base class that provides functionality for creating and managing a single instance
    /// of a Unity Component. This class ensures that only one instance of the derived type exists at any time
    /// and offers options to handle duplicate instances.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the component that will inherit from the Singleton class. This must be a class derived from
    /// UnityEngine.Component.
    /// </typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        internal static T s_instance;
        public static T Instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                s_instance = FindExistingInstance(includeInactive: true);
                return s_instance ??= CreateHiddenAutoSingleton();
            }
        }

        private static T FindExistingInstance(bool includeInactive) =>
            FindAnyObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

        private static T CreateHiddenAutoSingleton()
        {
            var go = new GameObject(typeof(T).Name + " AutoCreated");
            go.hideFlags = HideFlags.DontSave;
            return go.AddComponent<T>();
        }

        public virtual void Awake()
        {
            if (Application.isPlaying)
                InitializeSingleton();
        }

        internal virtual void InitializeSingleton()
        {
            if (s_instance == null)
            {
                s_instance = this as T;
                return;
            }

            if (s_instance != this)
            {
                // Default duplicate handling: keep the first instance.
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        public virtual void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }
    }
}