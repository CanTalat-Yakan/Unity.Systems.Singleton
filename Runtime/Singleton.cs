using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a generic singleton pattern implementation for Unity components.
    /// </summary>
    /// <remarks>This class ensures that only one instance of the specified component type <typeparamref
    /// name="T"/>  exists in the scene. If no instance is found, one will be automatically created. The singleton 
    /// instance is accessible through the <see cref="Instance"/> property.   The singleton is initialized during the
    /// <see cref="Awake"/> method and cleaned up during the  <see cref="OnDestroy"/> method. This implementation is
    /// designed for use in Unity projects and  assumes that the component type <typeparamref name="T"/> is attached to
    /// a GameObject.  Note: This implementation is not thread-safe and is intended for use in Unity's main
    /// thread.</remarks>
    /// <typeparam name="T">The type of the component that will be used as the singleton instance.  Must inherit from <see
    /// cref="Component"/>.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static bool HasInstance => s_instance != null;
        public static T TryGetInstance() => HasInstance ? s_instance : null;
        public static T Current => s_instance;

        /// <summary>
        /// Gets the singleton instance of the type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This property ensures that only one instance of <typeparamref name="T"/> exists in
        /// the scene.  If an instance is not found, a new GameObject is created, and the component of type
        /// <typeparamref name="T"/>  is added to it. The GameObject is automatically named based on the type of
        /// <typeparamref name="T"/>.</remarks>
        public static T Instance => s_instance ??= 
            FindAnyObjectByType<T>() ?? new GameObject(typeof(T).Name + "AutoCreated").AddComponent<T>();

        /// <summary>
        /// Represents a static instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This field is intended for internal use only and provides a shared instance of the
        /// specified type. It is not thread-safe and should be accessed with appropriate synchronization if used in a
        /// multithreaded context.</remarks>
        internal static T s_instance;

        /// <summary>
        /// Performs cleanup operations when the object is destroyed.
        /// </summary>
        /// <remarks>If this instance is the current singleton instance, it resets the singleton reference
        /// to <see langword="null"/>. Override this method in a derived class to implement additional cleanup
        /// logic.</remarks>
        public virtual void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        /// <summary>
        /// Performs initialization logic when the script instance is being loaded.
        /// </summary>
        /// <remarks>If the application is running, this method initializes the singleton instance  for
        /// the associated class. Override this method in derived classes to provide  additional initialization
        /// logic.</remarks>
        public virtual void Awake()
        {
            if (Application.isPlaying)
                InitializeSingleton();
        }

        /// <summary>
        /// Initializes the singleton instance of the current class.
        /// </summary>
        /// <remarks>This method assigns the current instance to the static singleton field.  It should be
        /// called to ensure the singleton instance is properly initialized.</remarks>
        internal virtual void InitializeSingleton() =>
            s_instance = this as T;
    }

    /// <summary>
    /// Represents a singleton component that persists across scene loads.
    /// </summary>
    /// <remarks>This class ensures that only one instance of the specified component type exists in the
    /// application. If an additional instance is created, it will be automatically destroyed. The singleton instance is
    /// detached from its parent in the scene hierarchy and marked to persist across scene loads using  <see
    /// cref="UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)"/>.</remarks>
    /// <typeparam name="T">The type of the component that will be used as the singleton instance.  Must inherit from <see
    /// cref="Component"/>.</typeparam>
    public class PersistentSingleton<T> : Singleton<T> where T : Component
    {
        /// <summary>
        /// Initializes the singleton instance of the class.
        /// </summary>
        /// <remarks>Ensures that only one instance of the class exists. If an instance already exists, 
        /// any additional instances are destroyed. The singleton instance is detached from its  parent and marked to
        /// persist across scene loads.</remarks>
        internal override void InitializeSingleton()
        {
            if (s_instance == null)
            {
                s_instance = this as T;

                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (s_instance != this)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Provides a base class for creating singleton components of type <typeparamref name="T"/> that regulate their own
    /// initialization and ensure only the most recently initialized instance persists.
    /// </summary>
    /// <remarks>This class extends the functionality of a standard singleton by ensuring that only the most
    /// recently initialized instance of the singleton persists across scene loads. Older instances are automatically
    /// destroyed. The class also prevents the destruction of the current instance when loading new scenes.</remarks>
    /// <typeparam name="T">The type of the component that inherits from <see cref="RegulatorSingleton{T}"/>.</typeparam>
    public class RegulatorSingleton<T> : Singleton<T> where T : Component
    {
        internal float _initializationTime;

        /// <summary>
        /// Initializes the singleton instance of the class, ensuring only the most recently initialized instance
        /// persists.
        /// </summary>
        /// <remarks>This method sets the initialization time, prevents the destruction of the current
        /// instance on scene loads,  and removes any older instances of the singleton. If no instance exists, the
        /// current instance is assigned  as the singleton.</remarks>
        internal override void InitializeSingleton()
        {
            _initializationTime = Time.time;

            DontDestroyOnLoad(gameObject);

            foreach (var old in FindExistingInstances())
                if (old._initializationTime < this._initializationTime)
                    Destroy(old.gameObject);

            if (s_instance == null)
                s_instance = this as T;
        }

        /// <summary>
        /// Finds and returns all existing instances of <see cref="RegulatorSingleton{T}"/> components associated with
        /// objects of type <typeparamref name="T"/> in the current context.
        /// </summary>
        /// <remarks>This method searches for all objects of type <typeparamref name="T"/> and retrieves
        /// their associated <see cref="RegulatorSingleton{T}"/> components, if any. The search does not apply any
        /// specific sorting to the results.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> containing all <see cref="RegulatorSingleton{T}"/> components found on
        /// objects of type <typeparamref name="T"/>. The sequence will be empty if no such components exist.</returns>
        private IEnumerable<RegulatorSingleton<T>> FindExistingInstances()
        {
            T[] oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var old in oldInstances)
                yield return old.GetComponent<RegulatorSingleton<T>>();
        }
    }
}