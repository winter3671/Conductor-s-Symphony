using UnityEngine;

namespace ConductorSymphony.Rhythm
{
    public enum RhythmLane
    {
        Left = 0,    // Q key (180 deg)
        UpLeft = 1,  // W key (135 deg)
        UpRight = 2, // E key (45 deg)
        Right = 3    // R key (0 deg)
    }

    public enum HitRating
    {
        Perfect,
        Great,
        Miss
    }

    public class RhythmNote : MonoBehaviour
    {
        public RhythmLane Lane { get; private set; }
        public float TargetTime { get; private set; }

        private Transform targetTransform;
        private Vector3 laneDirection;
        private float initialDistance;
        private float travelDuration;
        private float spawnTime;
        private bool isInitialized = false;

        public void Initialize(RhythmLane lane, Transform target, Vector3 direction, float initialDistance, float targetTime, float travelDuration)
        {
            Lane = lane;
            targetTransform = target;
            laneDirection = direction;
            this.initialDistance = initialDistance;
            TargetTime = targetTime;
            this.travelDuration = travelDuration;
            spawnTime = targetTime - travelDuration;
            isInitialized = true;

            UpdatePosition(0f);
        }

        private void Update()
        {
            if (!isInitialized) return;

            float currentTime = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongTime : 0f;
            float elapsed = currentTime - spawnTime;

            // Audio loop wrap-around guard: the master track loops (~every clip length), so
            // SongTime can jump back to ~0 while this note is still in flight. Detect the
            // impossible negative jump and unwrap it, otherwise this note would freeze forever
            // (progress stuck negative, Destroy condition never true).
            if (elapsed < -travelDuration)
            {
                float loopLength = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongLoopLength : 0f;
                if (loopLength > 0f) elapsed += loopLength;
            }

            UpdatePosition(elapsed / travelDuration);

            // If note has passed the target time beyond Miss window threshold (e.g. 0.2s late), trigger Miss
            if (elapsed > travelDuration + 0.2f)
            {
                RhythmManager.Instance?.OnNoteMissed(this);
                Destroy(gameObject);
            }
        }

        private void UpdatePosition(float progress)
        {
            float currentDistance = Mathf.Lerp(initialDistance, 0f, progress);
            Vector3 centerPos = (targetTransform != null) ? targetTransform.position : Vector3.zero;
            transform.position = centerPos + laneDirection * currentDistance;
        }

        public void DestroyNote()
        {
            Destroy(gameObject);
        }
    }
}
