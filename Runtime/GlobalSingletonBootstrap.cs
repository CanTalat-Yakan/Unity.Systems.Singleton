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
        private static readonly HashSet<Type> s_autoCreate = new();
        private static readonly HashSet<Type> s_singletonStaticsToReset = new();

        internal static void RegisterAutoCreateType(Type type)
        {
            if (type == null)
                return;

            // Only register valid GlobalSingleton subclasses.
            if (!typeof(Component).IsAssignableFrom(type))
                return;

            lock (s_autoCreate)
                s_autoCreate.Add(type);
        }

        internal static void RegisterSingletonTypeForDomainReset(Type type)
        {
            if (type == null)
                return;

            if (!typeof(Component).IsAssignableFrom(type))
                return;

            lock (s_singletonStaticsToReset)
                s_singletonStaticsToReset.Add(type);
        }

        internal static void EnsureAllAutoCreated()
        {
            if (!CanUseUnityObjectApi)
                return;

            Type[] types;
            lock (s_autoCreate)
                types = s_autoCreate.ToArray();

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

        internal static void ResetAllRegisteredSingletonStatics()
        {
            Type[] types;
            lock (s_autoCreate)
                types = s_autoCreate.ToArray();

            foreach (var t in types)
            {
                try
                {
                    // We want GlobalSingleton<TDerived>.s_instance to be cleared.
                    // ResetStatics() is internal static on the generic base.
                    var baseType = t;
                    while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(GlobalSingleton<>)))
                        baseType = baseType.BaseType;

                    if (baseType == null)
                        continue;

                    var resetMethod = baseType.GetMethod("ResetStatics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    resetMethod?.Invoke(null, null);
                }
                catch
                {
                    // Best-effort only; never break domain load.
                }
            }

            Type[] singletonTypes;
            lock (s_singletonStaticsToReset)
                singletonTypes = s_singletonStaticsToReset.ToArray();

            foreach (var t in singletonTypes)
            {
                try
                {
                    var baseType = t;
                    while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(Singleton<>)))
                        baseType = baseType.BaseType;

                    if (baseType == null)
                        continue;

                    var resetMethod = baseType.GetMethod("ResetStatics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    resetMethod?.Invoke(null, null);
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
