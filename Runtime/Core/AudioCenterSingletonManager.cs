using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Persistent singleton base: the first instance survives scene loads
    /// (DontDestroyOnLoad) and any later duplicate destroys itself.
    ///
    /// Accessing <see cref="Instance"/> auto-loads the singleton: if none exists
    /// in the scene it spawns a new GameObject hosting <typeparamref name="T"/>,
    /// so the static API is always usable without manual scene setup.
    /// </summary>
    public class AudioCenterSingletonManager<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T instance;
        private static bool applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<T>();
                if (instance != null)
                    return instance;

                // Don't resurrect the singleton while the app is tearing down.
                if (applicationIsQuitting)
                    return null;

                // Auto-load: no instance in the scene, so create one.
                var go = new GameObject($"[{typeof(T).Name}]");
                instance = go.AddComponent<T>();
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                // DontDestroyOnLoad requires a root object; detach if parented.
                if (transform.parent != null)
                    transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }
    }
}
