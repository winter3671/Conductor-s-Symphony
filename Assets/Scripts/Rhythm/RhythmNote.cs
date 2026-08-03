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

    // Tap: 기존 순간 판정 노트. Hold: 10종 악기별 공격 메커니즘 기획서의 "롱노트"(바이올린/프렌치호른/첼로/팀파니 전용).
    public enum NoteKind
    {
        Tap,
        Hold
    }

    public class RhythmNote : MonoBehaviour
    {
        public RhythmLane Lane { get; private set; }
        public float TargetTime { get; private set; }
        public NoteKind Kind { get; private set; } = NoteKind.Tap;

        // ---- Hold 전용 상태 ----
        public float HoldDurationSeconds { get; private set; }
        public float HoldElapsedSeconds { get; private set; }
        public float HoldProgress01 => HoldDurationSeconds > 0f ? Mathf.Clamp01(HoldElapsedSeconds / HoldDurationSeconds) : 1f;

        private Transform targetTransform;
        private Vector3 laneDirection;
        private float initialDistance;
        private float travelDuration;
        private float spawnTime;
        private float judgmentRadius;
        private float missWindow;
        private bool isInitialized = false;
        private bool isHolding = false; // true가 되는 순간부터는 RhythmManager가 생명주기를 직접 관리(자체 travel/miss 로직 정지)

        public void Initialize(RhythmLane lane, Transform target, Vector3 direction, float initialDistance, float targetTime, float travelDuration, float judgmentRadius, float missWindow, NoteKind kind = NoteKind.Tap, float holdDurationSeconds = 0f)
        {
            Lane = lane;
            targetTransform = target;
            laneDirection = direction;
            this.initialDistance = initialDistance;
            TargetTime = targetTime;
            this.travelDuration = travelDuration;
            this.judgmentRadius = judgmentRadius;
            this.missWindow = missWindow;
            Kind = kind;
            HoldDurationSeconds = holdDurationSeconds;
            spawnTime = targetTime - travelDuration;
            isInitialized = true;

            UpdatePosition(0f);
        }

        private void Update()
        {
            if (!isInitialized || isHolding) return;

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

            // If note has passed the target time beyond the Miss window, it has fully descended
            // past the judgment ring and reached the miss point — trigger Miss and destroy it.
            if (elapsed > travelDuration + missWindow)
            {
                RhythmManager.Instance?.OnNoteMissed(this);
                Destroy(gameObject);
            }
        }

        // Hold 노트가 초기 판정(Perfect/Great)에 성공한 순간 RhythmManager가 호출.
        // 판정 링 위치에 고정시키고, 이후 지속시간 추적은 전적으로 RhythmManager.TickHold()에 맡긴다.
        public void BeginHold()
        {
            isHolding = true;
            HoldElapsedSeconds = 0f;
            UpdatePosition(1f);
        }

        // 매 프레임 RhythmManager가 호출. 아직 지속시간이 안 찼으면 true, 이번 호출로 다 채웠으면 false를 반환.
        public bool TickHold(float deltaTime)
        {
            HoldElapsedSeconds += deltaTime;
            return HoldElapsedSeconds < HoldDurationSeconds;
        }

        private void UpdatePosition(float progress)
        {
            float currentDistance;
            if (progress <= 1f)
            {
                // Approach phase: travel from spawn point down to the fixed judgment ring.
                currentDistance = Mathf.Lerp(initialDistance, judgmentRadius, progress);
            }
            else
            {
                // Missed phase: only dip slightly past the judgment ring (not a fast rush to the
                // character) and vanish near the line once the Miss window (above) expires.
                float lateProgress = Mathf.Clamp01((progress - 1f) * travelDuration / missWindow);
                float missOvershoot = judgmentRadius * 0.25f;
                currentDistance = Mathf.Lerp(judgmentRadius, judgmentRadius - missOvershoot, lateProgress);
            }

            Vector3 centerPos = (targetTransform != null) ? targetTransform.position : Vector3.zero;
            transform.position = centerPos + laneDirection * currentDistance;
        }

        public void DestroyNote()
        {
            Destroy(gameObject);
        }
    }
}
