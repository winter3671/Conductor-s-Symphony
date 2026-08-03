using UnityEngine;

namespace ConductorSymphony.Utility
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        // Destroy(obj) only takes effect at the end of the current frame, so an old instance
        // is still "alive" (Instance != null) for the rest of this frame. If something creates
        // a replacement singleton in that same frame (AddComponent -> Awake runs synchronously),
        // Awake() above would see the stale Instance and destroy the new object instead.
        // Callers that intentionally replace a singleton within one frame (e.g. EnemySpawner
        // swapping an elite BossMonster for the final boss) should call this BEFORE Destroy()
        // on the old instance so the replacement's Awake() sees Instance == null.
        protected static void ClearInstance()
        {
            Instance = null;
        }
    }
}
