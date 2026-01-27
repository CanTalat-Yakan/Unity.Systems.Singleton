using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEssentials
{
    /// <summary>
    /// Internal auto-create registry for GlobalSingletons that should exist without any manual references.
    /// </summary>
    internal static class GlobalSingletonAutoCreate
    {
        private static volatile bool s_Running;

        internal static void Register(System.Type singletonType)
        {
            if (singletonType == null)
                return;

            GlobalSingletonBootstrap.RegisterAutoCreateType(singletonType);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureAutoCreatedRuntime() =>
            EnsureAutoCreated();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureAutoCreatedEditor() =>
            // Best-effort ensure in edit mode.
            EditorApplication.delayCall += EnsureAutoCreated;
#endif

        private static void EnsureAutoCreated()
        {
            if (s_Running)
                return;

            s_Running = true;
            try { GlobalSingletonBootstrap.EnsureAllAutoCreated(); }
            finally { s_Running = false; }
        }
    }
}
