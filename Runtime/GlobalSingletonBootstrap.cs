using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEssentials
{
    internal static class GlobalSingletonBootstrap
    {
        internal static bool CanUseUnityObjectApi
        {
            get
            {
#if UNITY_EDITOR
                // During import/serialization it's illegal to call Find* APIs.
                if (EditorApplication.isUpdating)
                    return false;
#endif
                // Best-effort main-thread guard. Unity throws if called from a loading thread.
                if (!UnityMainThread.IsMainThread)
                    return false;

                return true;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // Ensure our main-thread marker exists early.
            UnityMainThread.Touch();
        }
#endif
    }

    /// <summary>
    /// Minimal main-thread marker. It becomes valid once a Unity callback runs.
    /// </summary>
    internal sealed class UnityMainThread : MonoBehaviour
    {
        private static int s_mainThreadId;

        internal static bool IsMainThread
        {
            get
            {
                if (s_mainThreadId == 0)
                    return false;
                return Environment.CurrentManagedThreadId == s_mainThreadId;
            }
        }

        internal static void Touch()
        {
            if (s_mainThreadId == 0)
                s_mainThreadId = Environment.CurrentManagedThreadId;
        }

        private void Awake() =>
            Touch();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            // Creates a small hidden object on the main thread.
            var go = new GameObject("[UnityMainThread]");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<UnityMainThread>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
    }
}
