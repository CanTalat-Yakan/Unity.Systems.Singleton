using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEssentials
{
    /// <summary>
    /// Provides a generic singleton pattern implementation for Unity components.
    /// </summary>
    /// <typeparam name="T">The type of the component that will be used as the singleton instance.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static bool HasInstance => s_instance != null;
        public static T TryGetInstance() => HasInstance ? s_instance : null;
        public static T Current => s_instance;

        /// <summary>
        /// Returns the singleton instance.
        /// If none exists, this will try to find one in the current loaded objects and may auto-create one.
        /// 
        /// Note: Unity object APIs are only valid on the main thread and not during certain editor update/import phases.
        /// In those contexts, this returns null instead of throwing.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                if (!CanUseUnityObjectApi)
                    return null;

                s_instance = FindExistingInstance(includeInactive: true);
                return s_instance ??= CreateHiddenAutoSingleton();
            }
        }

        internal static T s_instance;

        internal static void ResetStatics() =>
            s_instance = null;

        private static bool CanUseUnityObjectApi
        {
            get
            {
#if UNITY_EDITOR
                if (EditorApplication.isUpdating)
                    return false;
#endif
                // Reuse the package's best-effort main-thread guard if available.
                return GlobalSingletonBootstrap.CanUseUnityObjectApi;
            }
        }

        private static T FindExistingInstance(bool includeInactive) =>
            FindAnyObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

        private static T CreateHiddenAutoSingleton()
        {
            var go = new GameObject(typeof(T).Name + " AutoCreated");
            go.hideFlags = HideFlags.HideAndDontSave;
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

        static Singleton()
        {
            // Track this singleton type so statics can be reset on domain reload
            // without using RuntimeInitializeOnLoadMethod on a generic class.
            if (!typeof(T).IsAbstract)
                GlobalSingletonBootstrap.RegisterSingletonTypeForDomainReset(typeof(T));
        }
    }
}