using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static bool HasInstance => _instance != null;
        public static T TryGetInstance() => HasInstance ? _instance : null;
        public static T Current => _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        GameObject obj = new();
                        obj.name = typeof(T).Name + "AutoCreated";
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        public static T _instance { get; set; }

        public virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public virtual void Awake()
        {
            if (Application.isPlaying)
                InitializeSingleton();
        }

        public virtual void InitializeSingleton() =>
            _instance = this as T;
    }

    public class PersistentSingleton<T> : Singleton<T> where T : Component
    {
        public override void InitializeSingleton()
        {
            if (_instance == null)
            {
                _instance = this as T;

                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
                Destroy(gameObject);
        }
    }

    public class RegulatorSingleton<T> : Singleton<T> where T : Component
    {
        public float _initializationTime { get; private set; }

        public override void InitializeSingleton()
        {
            _initializationTime = Time.time;

            DontDestroyOnLoad(gameObject);

            foreach (var old in FindExistingInstances())
                if (old._initializationTime < this._initializationTime)
                    Destroy(old.gameObject);

            if (_instance == null)
                _instance = this as T;
        }

        private IEnumerable<RegulatorSingleton<T>> FindExistingInstances()
        {
            T[] oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var old in oldInstances)
                yield return old.GetComponent<RegulatorSingleton<T>>();
        }
    }
}