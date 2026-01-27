using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEssentials
{
    internal static class GlobalSingletonBootstrap
    {
        private static readonly HashSet<Type> SAutoCreate = new();

        internal static void RegisterAutoCreateType(Type type)
        {
            if (type == null)
                return;

            // Only register valid GlobalSingleton subclasses.
            if (!typeof(Component).IsAssignableFrom(type))
                return;

            lock (SAutoCreate)
                SAutoCreate.Add(type);
        }

        internal static void EnsureAllAutoCreated()
        {
            if (!CanUseUnityObjectApi)
                return;

            Type[] types;
            lock (SAutoCreate)
                types = SAutoCreate.ToArray();

            foreach (var t in types)
            {
                try
                {
                    // Touch Instance via reflection: GlobalSingleton<T>.Instance
                    var baseType = t;
                    // Ensure we’re dealing with a GlobalSingleton<>
                    while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(GlobalSingleton<>)))
                        baseType = baseType.BaseType;

                    if (baseType == null)
                        continue;

                    var instanceProp = baseType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    _ = instanceProp?.GetValue(null, null);
                }
                catch
                {
                    // Best-effort only; never break domain load.
                }
            }
        }

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
        private static int _sMainThreadId;

        internal static bool IsMainThread
        {
            get
            {
                if (_sMainThreadId == 0)
                    return false;
                return Environment.CurrentManagedThreadId == _sMainThreadId;
            }
        }

        internal static void Touch()
        {
            if (_sMainThreadId == 0)
                _sMainThreadId = Environment.CurrentManagedThreadId;
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
            DontDestroyOnLoad(go);
        }
    }
}
