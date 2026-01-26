using System.Collections.Generic;
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

        public static T Instance => s_instance ??= FindAnyObjectByType<T>() ?? CreateHiddenAutoSingleton();
        internal static T s_instance;

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

        internal virtual void InitializeSingleton() =>
            s_instance = this as T;

        public virtual void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }
    }
}