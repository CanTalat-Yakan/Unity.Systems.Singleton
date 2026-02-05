using System;
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

                // Rebind + dedupe first.
                s_instance = FindAndDedupeExistingInstances();
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
            // Ensure the host visibility is correct for the current mode.
            ApplyVisibility(gameObject);

            if (s_instance == null)
            {
                // In case we survived a domain reload, prefer to rebind to the existing object
                // (and make sure duplicates are removed).
                s_instance = FindAndDedupeExistingInstances() ?? (this as T);

                ConfigureRoot(gameObject);

                if (Application.isPlaying)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }

                // If we ended up not being the chosen instance, delete this object.
                if (s_instance != this)
                    DestroyImmediate(gameObject);
            }
            else if (s_instance != this)
            {
                DestroyImmediate(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        private static void ConfigureRoot(GameObject go)
        {
            // Never save this object into scenes.
            // Also control visibility: hidden in edit mode, visible during play mode.
            go.hideFlags = HideFlags.DontSave;
            ApplyVisibility(go);
        }

        private static void ApplyVisibility(GameObject go)
        {
            if (go == null)
                return;

            // In Edit Mode, keep it out of the hierarchy/inspector. In Play Mode, show it.
            if (Application.isPlaying)
                go.hideFlags &= ~HideFlags.HideInHierarchy;
            else
                go.hideFlags |= HideFlags.HideInHierarchy;
        }


        private static T FindAndDedupeExistingInstances()
        {
            // Note: "hidden" objects are still returned by FindObjectsByType; hideFlags don't exclude them.
            // We include inactive so we can rebind even if it was disabled.
            var instances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (instances == null || instances.Length == 0)
                return null;

            // Prefer an instance that already carries our marker suffix.
            T chosen = null;
            for (var i = 0; i < instances.Length; i++)
            {
                var inst = instances[i];
                if (inst == null)
                    continue;

                if (inst.gameObject != null && inst.gameObject.name.EndsWith(AutoSuffix))
                {
                    chosen = inst;
                    break;
                }
            }

            chosen ??= instances[0];

            // Remove duplicates.
            for (var i = 0; i < instances.Length; i++)
            {
                var inst = instances[i];
                if (inst == null || inst == chosen)
                    continue;

                // Keep behavior consistent in edit-time.
                DestroyImmediate(inst.gameObject);
            }

            // Bring flags and (if needed) persistence back in line.
            if (chosen != null)
            {
                ConfigureRoot(chosen.gameObject);
                if (Application.isPlaying)
                    DontDestroyOnLoad(chosen.gameObject);
            }

            return chosen;
        }
    }
}