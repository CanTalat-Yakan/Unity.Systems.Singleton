using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a base class for creating globally accessible singleton components in Unity.
    /// GlobalSingleton provides a way to ensure that only one instance of a specified component
    /// type exists in the application. Instances are lazily initialized and auto-created if they
    /// do not exist, following specific lifecycle management behaviors:
    /// - Instances are registered upon creation and unregistered upon destruction.
    /// - All singleton instances are managed under a root GameObject to ensure proper hierarchy management.
    /// This is a generic class and must be inherited to define a concrete singleton type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the singleton, which must inherit from UnityEngine.Component.
    /// </typeparam>
    public class GlobalSingleton<T> : MonoBehaviour where T : Component
    {
        public static T Instance => GlobalSingletonRegistrar.GetOrCreate<T>();

        protected virtual void Awake() =>
            GlobalSingletonRegistrar.BindInstance(this);

        protected virtual void OnDestroy() =>
            GlobalSingletonRegistrar.UnbindInstance(this);
    }
}
