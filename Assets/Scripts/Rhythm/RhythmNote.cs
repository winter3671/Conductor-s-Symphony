using UnityEngine;
using ConductorSymphony.Utility;

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

        // ---- 롱노트(Hold) 전용 "머리+꼬리 바" 비주얼 ----
        // 기존에는 Tap/Hold 구분 없이 항상 같은 원형 스프라이트 하나만 그려서, 플레이어가 "이 노트는
        // 계속 눌러야 한다"는 걸 화면만 보고는 알 수 없었다(실제 홀드 판정/공격 로직은 정상 동작).
        // 이 꼬리 바는 머리(기존 원형 노트)에서 laneDirection(바깥쪽) 방향으로 뻗어나가며, 길이는
        // 홀드 지속시간을 접근 구간의 이동 속도로 환산한 값(단, 화면 밖으로 과하게 튀어나가지 않도록
        // 스폰~판정선 거리로 상한선을 둠)이다. 홀드 판정에 성공해 실제로 누르고 있는 동안에는
        // HoldProgress01에 맞춰 꼬리가 점점 줄어들어(판정선 쪽부터 "먹히는" 형태) 남은 유지 시간을
        // 계속 보여준다.
        private static Sprite tailSprite;
        private const float TailThickness = 0.16f;
        private Transform tailTransform;
        private float tailFullLength;

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

            if (kind == NoteKind.Hold && holdDurationSeconds > 0f)
            {
                SetUpTailVisual();
            }

            UpdatePosition(0f);
            UpdateTailVisual();
        }

        private void SetUpTailVisual()
        {
            if (tailSprite == null) tailSprite = ProceduralSpriteFactory.CreateUnitSquare(Color.white);

            // 접근 구간(스폰 -> 판정선)의 초당 이동 속도로 홀드 지속시간을 길이로 환산 - 탭 노트와
            // 같은 시각적 척도를 써서 "얼마나 길게 눌러야 하는지"가 직관적으로 보이게 한다. 다만 팀파니
            // Lv5(16스텝, 약 5초) 같은 경우 원 환산 길이가 스폰~판정선 거리보다 길어질 수 있어, 화면
            // 밖으로 과하게 튀어나가지 않도록 그 거리로 상한을 둔다.
            float travelSpeed = (initialDistance - judgmentRadius) / Mathf.Max(0.0001f, travelDuration);
            tailFullLength = Mathf.Min(HoldDurationSeconds * travelSpeed, initialDistance - judgmentRadius);
            if (tailFullLength <= 0.001f) return;

            GameObject tailObj = new GameObject("HoldTail");
            tailObj.transform.SetParent(transform, false);

            SpriteRenderer headSr = GetComponent<SpriteRenderer>();
            Color tailColor = headSr != null ? headSr.color : Color.white;
            tailColor.a *= 0.55f; // 머리보다 옅게 - "지금 눌러야 할 지점"인 머리와 시각적으로 구분

            SpriteRenderer tailSr = tailObj.AddComponent<SpriteRenderer>();
            tailSr.sprite = tailSprite;
            tailSr.color = tailColor;
            tailSr.sortingOrder = 8; // 노트 머리(10)/판정 링(9)보다 뒤에 그려짐

            float angleDeg = Mathf.Atan2(laneDirection.y, laneDirection.x) * Mathf.Rad2Deg;
            tailObj.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

            tailTransform = tailObj.transform;
        }

        // 꼬리 바는 항상 "머리(=이 오브젝트 원점)에서 laneDirection 바깥쪽으로" 뻗어나가는 상대 위치이므로,
        // 접근 구간에서는 머리가 매 프레임 이동해도 로컬 좌표 기준 꼬리 길이는 그대로다(부모를 따라 함께
        // 이동). 홀드 중에만 HoldProgress01에 맞춰 길이가 줄어들도록 매 프레임 갱신한다.
        private void UpdateTailVisual()
        {
            if (tailTransform == null) return;

            float remaining01 = isHolding ? Mathf.Clamp01(1f - HoldProgress01) : 1f;
            float currentLength = tailFullLength * remaining01;

            if (currentLength <= 0.001f)
            {
                tailTransform.gameObject.SetActive(false);
                return;
            }

            tailTransform.gameObject.SetActive(true);
            tailTransform.localPosition = laneDirection * (currentLength / 2f);
            tailTransform.localScale = new Vector3(currentLength, TailThickness, 1f);
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (isHolding)
            {
                // 홀드 중에는 BeginHold()가 호출된 그 순간의 위치에 고정된 채 멈춰있었다 - 판정 링
                // 자체는 플레이어를 따라다니는데(JudgmentRing.Update) 홀드 노트만 그 자리에 남아있어서,
                // 홀드 도중 화살표 키로 이동하면 노트가 판정 링에서 눈에 띄게 벗어나 보이는 버그였다.
                // 매 프레임 다시 판정 반경 위치로 갱신해서 플레이어(=흰색 판정 링)를 계속 따라가게 한다.
                UpdatePosition(1f);
                UpdateTailVisual();
                return;
            }

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
            UpdateTailVisual();
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
