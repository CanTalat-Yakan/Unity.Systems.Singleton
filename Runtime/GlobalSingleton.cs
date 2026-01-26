using UnityEditor;
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

        public static T Instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                // Rebind if an instance already exists (including hidden objects in editor).
                s_instance = FindExistingInstance(includeInactive: true, includeHiddenInEditor: true);

                if (s_instance != null)
                    return s_instance;

                // Create if missing.
                var go = new GameObject(typeof(T).Name + AutoSuffix);
                ConfigureRoot(go);

                s_instance = go.AddComponent<T>();

                // Keep runtime alive across scene loads; editor persistence is handled by hideFlags.
                if (Application.isPlaying)
                    DontDestroyOnLoad(go);

                return s_instance;
            }
        }

        internal static T s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Always clear statics; the hidden object (if any) remains and will be rebound via Instance.
            s_instance = null;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureEditor()
        {
            // Ensure it exists in edit mode (and after assembly reloads).
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    _ = Instance;
            };

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Rebind/create on both sides of the transition.
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                s_instance = null;
                _ = Instance;
            }
        }
#endif

        protected virtual void Awake()
        {
            // Ensure single instance; if created manually, it becomes the singleton.
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
            {
                // Replace-or-destroy policy: destroy duplicates.
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
            go.hideFlags = HideFlags.HideAndDontSave;
        }

        private static T FindExistingInstance(bool includeInactive, bool includeHiddenInEditor)
        {
#if UNITY_EDITOR
            if (includeHiddenInEditor)
            {
                // Finds HideAndDontSave objects too.
                var all = Resources.FindObjectsOfTypeAll<T>();
                for (int i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null) continue;
                    if (!includeInactive && !c.gameObject.activeInHierarchy) continue;
                    return c;
                }
                return null;
            }
#endif
            // Runtime / non-hidden search
            return UnityEngine.Object.FindFirstObjectByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude
            );
        }
    }
}