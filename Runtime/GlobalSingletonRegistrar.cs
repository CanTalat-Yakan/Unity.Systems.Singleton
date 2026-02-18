using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Minimal registry backing <see cref="GlobalSingleton{T}"/>.
    ///
    /// Contract:
    /// - All GlobalSingleton instances live under exactly one root GameObject named "[GlobalSingletons]".
    /// - Each singleton type gets exactly one child GameObject named "&lt;TypeName&gt; [GlobalSingleton]".
    /// - Instances are auto-created on AfterSceneLoad and lazy-load on editor time.
    /// - Objects are never saved.
    /// - In play mode: root (and thus all children) is DontDestroyOnLoad.
    /// </summary>
    public static class GlobalSingletonRegistrar
    {
        private const string RootName = "[GlobalSingletons]";
        private const string ChildSuffix = " [GlobalSingleton]";

        private static readonly Dictionary<Type, Component> s_instances = new();

        public static T GetOrCreate<T>() where T : Component
        {
            var type = typeof(T);

            if (s_instances.TryGetValue(type, out var cached) && cached != null)
                return (T)cached;

            // If there are already objects in the scene (e.g. hot reload / manual placement), keep one.
            var existing = FindExistingAndDedupe(type);
            if (existing != null)
            {
                s_instances[type] = existing;
                return (T)existing;
            }

            var created = CreateUnderRoot(type);
            ApplyFlagsAndPersistence(created);
            s_instances[type] = created;
            return (T)created;
        }

        public static void BindInstance(Component inst)
        {
            if (inst == null)
                return;

            var type = inst.GetType();

            // If duplicates exist, keep one and destroy the rest.
            var kept = FindExistingAndDedupe(type);
            if (kept != null && kept != inst)
            {
                DestroyComponentGameObject(inst, immediate: !Application.isPlaying);
                s_instances[type] = kept;
                return;
            }

            ApplyFlagsAndPersistence(inst);
            s_instances[type] = inst;
        }

        public static void UnbindInstance(Component inst)
        {
            if (inst == null)
                return;

            var type = inst.GetType();
            if (s_instances.TryGetValue(type, out var existing) && existing == inst)
                s_instances.Remove(type);
        }

        public static void DestroyAll(bool immediate)
        {
            s_instances.Clear();

            var root = GameObject.Find(RootName);
            if (root == null)
                return;

            if (immediate)
                UnityEngine.Object.DestroyImmediate(root);
            else
                UnityEngine.Object.Destroy(root);
        }

        private static Component CreateUnderRoot(Type type)
        {
            var root = GetOrCreateRoot();

            // Ensure there is exactly one child GO per singleton type.
            var childName = type.Name + ChildSuffix;
            var t = root.transform.Find(childName);
            GameObject child;
            if (t != null)
            {
                child = t.gameObject;
            }
            else
            {
                child = new GameObject(childName);
                child.transform.SetParent(root.transform, worldPositionStays: false);
            }

            // Never save, but keep visible.
            child.hideFlags = HideFlags.DontSave;
            child.SetActive(true);

            // Ensure component exists.
            var existing = child.GetComponent(type) as Component;
            return existing != null ? existing : child.AddComponent(type) as Component;
        }

        private static void ApplyFlagsAndPersistence(Component c)
        {
            if (c == null)
                return;

            var go = c.gameObject;
            if (go == null)
                return;

            go.hideFlags = HideFlags.DontSave;

            var root = GetOrCreateRoot();
            if (go.transform.parent != root.transform)
                go.transform.SetParent(root.transform, worldPositionStays: false);

            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(root);
        }

        private static Component FindExistingAndDedupe(Type type)
        {
            var objs = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include);
            if (objs == null || objs.Length == 0)
                return null;

            Component chosen = null;
            for (var i = 0; i < objs.Length; i++)
            {
                chosen = objs[i] as Component;
                if (chosen != null)
                    break;
            }

            for (var i = 0; i < objs.Length; i++)
            {
                var c = objs[i] as Component;
                if (c == null || c == chosen)
                    continue;

                DestroyComponentGameObject(c, immediate: !Application.isPlaying);
            }

            if (chosen != null)
                ApplyFlagsAndPersistence(chosen);

            return chosen;
        }

        private static void DestroyComponentGameObject(Component c, bool immediate)
        {
            if (c == null)
                return;

            var go = c.gameObject;
            if (go == null)
                return;

            if (immediate)
                UnityEngine.Object.DestroyImmediate(go);
            else
                UnityEngine.Object.Destroy(go);
        }

        private static GameObject GetOrCreateRoot()
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                root.hideFlags = HideFlags.DontSave;

                if (Application.isPlaying)
                    UnityEngine.Object.DontDestroyOnLoad(root);
            }

            root.hideFlags = HideFlags.DontSave;
            root.SetActive(true);

            return root;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistrationInit()
        {
            s_instances.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateAllGlobalSingletonTypes()
        {
            try
            {
                var types = DiscoverAllGlobalSingletonConcreteTypes();
                if (types.Count == 0)
                    return;

                var getOrCreate = typeof(GlobalSingletonRegistrar)
                    .GetMethod(nameof(GetOrCreate), BindingFlags.Public | BindingFlags.Static);

                if (getOrCreate == null)
                    return;

                for (var i = 0; i < types.Count; i++)
                {
                    try
                    {
                        var t = types[i];
                        var generic = getOrCreate.MakeGenericMethod(t);
                        generic.Invoke(null, null);
                    }
                    catch { }
                }
            }
            catch { }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInit()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode || 
                state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                DestroyAll(immediate: true);
        }
#endif

        private static List<Type> DiscoverAllGlobalSingletonConcreteTypes()
        {
            var result = new List<Type>(32);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (var i = 0; i < assemblies.Length; i++)
            {
                var asm = assemblies[i];
                if (asm == null || asm.IsDynamic)
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
                    if (t == null || t.IsAbstract)
                        continue;
                    if (!typeof(Component).IsAssignableFrom(t))
                        continue;
                    if (!IsDerivedFromOpenGeneric(t, typeof(GlobalSingleton<>)))
                        continue;

                    result.Add(t);
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return result;
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
