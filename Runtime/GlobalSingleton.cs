using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Editor- and runtime-persistent singleton intended to exist "always":
    /// - Survives scene loads (runtime).
    /// - Persists across Play Mode transitions without being saved into scenes (HideAndDontSave).
    /// - Rebinds the static Instance after SubsystemRegistration / domain reload behavior.
    /// </summary>
    /// <remarks>
    /// This is appropriate for systems you want available in Edit Mode and Play Mode without requiring scene setup.
    /// The singleton root GameObject is not saved to scenes and is hidden from hierarchy.
    /// </remarks>
    public class GlobalSingleton<T> : MonoBehaviour where T : Component
    {
        private const string AutoSuffix = " [GlobalSingleton]";

        public static bool HasInstance => s_instance != null;
        public static T Current => s_instance;

        /// <summary>
        /// Attempts to get an already-created instance without triggering creation or any expensive searches.
        /// Safe to call from constructors / serialization contexts.
        /// </summary>
        public static bool TryGetInstance(out T instance)
        {
            instance = s_instance;
            return instance != null;
        }

        public static T Instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                // Important: field initializers / constructors (e.g. ScriptableObject / CustomPass) can run
                // on Unity's loading thread or during serialization where Find* APIs are disallowed.
                // In those cases we must *not* call any Unity object-finding/creation APIs.
                if (!GlobalSingletonBootstrap.CanUseUnityObjectApi)
                    return null;

                // Try to rebind to an existing instance (runtime-safe API).
                s_instance = FindExistingInstance(includeInactive: true);
                if (s_instance != null)
                    return s_instance;

                // Create if missing.
                var go = new GameObject(typeof(T).Name + AutoSuffix);
                ConfigureRoot(go);

                s_instance = go.AddComponent<T>();

                if (Application.isPlaying)
                    DontDestroyOnLoad(go);

                return s_instance;
            }
        }

        internal static T s_instance;

        internal static void ResetStatics() =>
            s_instance = null;


        static GlobalSingleton()
        {
            // Default behavior: all GlobalSingletons should exist without manual scene setup.
            // Skip abstract types.
            if (!typeof(T).IsAbstract)
                GlobalSingletonAutoCreate.Register(typeof(T));
        }

        protected virtual void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this as T;
                ConfigureRoot(gameObject);

                if (Application.isPlaying)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (s_instance != this)
                DestroyImmediate(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        private static void ConfigureRoot(GameObject go) =>
            go.hideFlags = HideFlags.DontSave;

        private static T FindExistingInstance(bool includeInactive) =>
            FindFirstObjectByType<T>(includeInactive? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
    }
}