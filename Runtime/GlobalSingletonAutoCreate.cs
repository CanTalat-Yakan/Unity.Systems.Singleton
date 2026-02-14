using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static volatile bool s_Discovered;

        internal static void Register(Type singletonType)
        {
            if (singletonType == null)
                return;

            GlobalSingletonBootstrap.RegisterAutoCreateType(singletonType);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            // Domain reload / enter play mode: clear our one-shot discovery so we rediscover in the new domain.
            s_Discovered = false;

            // Unity doesn't allow RuntimeInitializeOnLoad methods in generic classes,
            // so we reset all registered GlobalSingleton<T>/Singleton<T> statics here.
            GlobalSingletonBootstrap.ResetAllRegisteredSingletonStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureAutoCreatedRuntime()
        {
            UnityMainThread.Touch();
            DiscoverAndRegisterAllGlobalSingletonTypes();
            EnsureAutoCreated();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureAutoCreatedEditor()
        {
            // Track play state transitions so eager creation happens reliably.
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Best-effort ensure in edit mode (won't create if Unity APIs are disallowed).
            EditorApplication.delayCall += () =>
            {
                UnityMainThread.Touch();
                DiscoverAndRegisterAllGlobalSingletonTypes();
                EnsureAutoCreated();
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            UnityMainThread.Touch();
            DiscoverAndRegisterAllGlobalSingletonTypes();

            // If Unity is still "updating" during the transition, retry on the next editor tick.
            if (EditorApplication.isUpdating)
                EditorApplication.delayCall += EnsureAutoCreated;
            else
                EnsureAutoCreated();
        }
#endif

        private static void EnsureAutoCreated()
        {
            if (s_Running)
                return;

            s_Running = true;
            try { GlobalSingletonBootstrap.EnsureAllAutoCreated(); }
            finally { s_Running = false; }
        }

        private static void DiscoverAndRegisterAllGlobalSingletonTypes()
        {
            if (s_Discovered)
                return;

            s_Discovered = true;

            try
            {
                foreach (var t in EnumerateAllConcreteComponentTypes())
                {
                    if (t == null || t.IsAbstract)
                        continue;

                    if (!IsDerivedFromOpenGeneric(t, typeof(GlobalSingleton<>)))
                        continue;

                    Register(t);
                }
            }
            catch
            {
                // Best-effort only.
            }
        }

        private static IEnumerable<Type> EnumerateAllConcreteComponentTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var asm = assemblies[i];
                if (asm == null)
                    continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                if (types == null)
                    continue;

                for (var j = 0; j < types.Length; j++)
                {
                    var t = types[j];
                    if (t == null)
                        continue;
                    if (!typeof(Component).IsAssignableFrom(t))
                        continue;
                    if (t.IsGenericTypeDefinition)
                        continue;

                    yield return t;
                }
            }
        }

        private static bool IsDerivedFromOpenGeneric(Type candidate, Type openGenericBase)
        {
            var t = candidate;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == openGenericBase)
                    return true;

                t = t.BaseType;
            }

            return false;
        }
    }
}
