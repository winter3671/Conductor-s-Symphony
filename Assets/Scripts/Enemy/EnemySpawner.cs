using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Enemy
{
    // Time-segment based spawn/difficulty driver, aligned with game_balance_design.docx section 3
    // (10-minute trash-mob phase in 4 absolute-time segments + a single 10:00~12:00 final boss encounter).
    public class EnemySpawner : MonoSingleton<EnemySpawner>
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 8.0f;

        [Header("Mob Phase Timeline (seconds, matches doc's 00:00~10:00 wave phase)")]
        [SerializeField] private float mobPhaseDuration = 600f; // 10:00 -> trash mobs stop, final boss appears
        // Segment absolute end-times: 02:30, 05:00, 07:30, 10:00
        [SerializeField] private float[] segmentEndTimes = { 150f, 300f, 450f, 600f };

        [Header("구간별 동시 존재 몹 수 (min~max, 구간 내 시간에 따라 선형 보간)")]
        [SerializeField] private int[] concurrentMin = { 15, 30, 60, 100 };
        [SerializeField] private int[] concurrentMax = { 30, 60, 100, 150 };

        [Header("구간별 일반 몹 HP (min~max, 스폰 시 랜덤)")]
        [SerializeField] private int[] mobHpMin = { 15, 50, 150, 350 };
        [SerializeField] private int[] mobHpMax = { 30, 100, 250, 500 };

        [Header("구간별 엘리트 몹 HP")]
        [SerializeField] private int[] eliteHp = { 200, 600, 1800, 4000 };

        [Header("구간별 마리당 EXP")]
        [SerializeField] private int[] expPerKill = { 10, 12, 15, 20 };

        [Header("구간별 스폰 간격 (초, 미세조정용 - 문서에 없는 값이라 플레이테스트로 보정 필요)")]
        [SerializeField] private float[] spawnIntervalPerSegment = { 1.0f, 0.6f, 0.4f, 0.28f };

        [Header("Elite Cadence")]
        [SerializeField] private float eliteInterval = 120.0f; // 2-minute cadence while mob phase is active

        [Header("Final Boss")]
        [SerializeField] private int finalBossHp = 180000;
        [SerializeField] private float finalBossTimeLimit = 120f; // 2-minute time-attack

        private float startTime;
        private float eliteTimer = 0f;
        private float nextSpawnTime;
        private bool finalBossTriggered = false;
        private Transform playerTransform;
        private List<EnemyMonster> activeEnemies = new List<EnemyMonster>();

        private Sprite enemySprite;

        public IReadOnlyList<EnemyMonster> ActiveEnemies => activeEnemies;
        public float ElapsedTime => Mathf.Min(Time.time - startTime, mobPhaseDuration);
        public bool IsMobPhaseActive => !finalBossTriggered;
        public int CurrentSegmentIndex => GetSegmentIndex(ElapsedTime);

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            enemySprite = ProceduralSpriteFactory.CreateDiamond(32, 12f, Color.magenta);
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }

            startTime = Time.time;
            nextSpawnTime = Time.time + GetCurrentSpawnInterval();
            eliteTimer = 0f;
        }

        private int GetSegmentIndex(float elapsed)
        {
            for (int i = 0; i < segmentEndTimes.Length; i++)
            {
                if (elapsed < segmentEndTimes[i]) return i;
            }
            return segmentEndTimes.Length - 1;
        }

        private float GetSegmentProgress01(int segmentIndex, float elapsed)
        {
            float segStart = segmentIndex == 0 ? 0f : segmentEndTimes[segmentIndex - 1];
            float segEnd = segmentEndTimes[segmentIndex];
            if (segEnd <= segStart) return 1f;
            return Mathf.Clamp01((elapsed - segStart) / (segEnd - segStart));
        }

        private float GetCurrentSpawnInterval()
        {
            int segment = CurrentSegmentIndex;
            return spawnIntervalPerSegment[segment];
        }

        private int GetCurrentMaxConcurrentEnemies()
        {
            int segment = CurrentSegmentIndex;
            float t = GetSegmentProgress01(segment, ElapsedTime);
            return Mathf.RoundToInt(Mathf.Lerp(concurrentMin[segment], concurrentMax[segment], t));
        }

        public int GetCurrentExpPerKill()
        {
            return expPerKill[CurrentSegmentIndex];
        }

        private void Update()
        {
            // Clean dead enemies
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            if (finalBossTriggered) return; // Mob phase fully over; final boss encounter owns the game now

            float elapsed = ElapsedTime;

            // 10:00 진입: 모든 잡몹 스폰 중단 및 소멸, 보스 단독 등장 (design doc section 3)
            if (elapsed >= mobPhaseDuration)
            {
                TriggerFinalBossPhase();
                return;
            }

            // Elite cadence: only while no elite/boss is currently alive, so encounters don't stack
            if (BossMonster.Instance == null)
            {
                eliteTimer += Time.deltaTime;
                if (eliteTimer >= eliteInterval)
                {
                    eliteTimer = 0f;
                    SpawnElite();
                }
            }

            // Trash mobs keep spawning continuously through the farming phase (elites do not pause this)
            int currentMaxEnemies = GetCurrentMaxConcurrentEnemies();
            if (Time.time >= nextSpawnTime)
            {
                if (activeEnemies.Count < currentMaxEnemies)
                {
                    SpawnEnemy();
                }
                nextSpawnTime = Time.time + GetCurrentSpawnInterval();
            }
        }

        private void SpawnElite()
        {
            Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 spawnPos = centerPos + new Vector3(0f, spawnRadius, 0f);

            GameObject eliteObj = new GameObject($"EliteMonster_Segment_{CurrentSegmentIndex}");
            eliteObj.transform.position = spawnPos;
            BossMonster elite = eliteObj.AddComponent<BossMonster>();
            elite.Initialize(eliteHp[CurrentSegmentIndex]);
        }

        private void TriggerFinalBossPhase()
        {
            finalBossTriggered = true;

            // 잔여 잡몹 전부 소멸
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i].gameObject);
                }
            }
            activeEnemies.Clear();

            // 남아있던 엘리트가 있다면 정리하고 보스 단독 등장.
            // ReleaseSingletonSlot()을 Destroy() 이전에 호출해 정적 Instance를 먼저 비워야 한다 —
            // Destroy()는 프레임 끝까지 지연 적용되므로, 그냥 Destroy()만 하면 바로 다음에 생성하는
            // 최종 보스의 Awake()가 아직 "살아있는" 옛 엘리트를 보고 자기 자신을 파괴해버리는
            // 소프트락이 발생한다 (실측 확인된 버그, balance_1to3_test_result.md 참고).
            if (BossMonster.Instance != null)
            {
                BossMonster oldBoss = BossMonster.Instance;
                oldBoss.ReleaseSingletonSlot();
                Destroy(oldBoss.gameObject);
            }

            Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 spawnPos = centerPos + new Vector3(0f, spawnRadius, 0f);

            GameObject bossObj = new GameObject("FinalBossMonster");
            bossObj.transform.position = spawnPos;
            BossMonster boss = bossObj.AddComponent<BossMonster>();
            boss.InitializeFinalBoss(finalBossHp, finalBossTimeLimit);
        }

        private void SpawnEnemy()
        {
            Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnOffset = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f) * spawnRadius;
            Vector3 spawnPos = centerPos + spawnOffset;

            GameObject enemyObj = new GameObject($"NoteEnemy_{Time.frameCount}");
            enemyObj.transform.position = spawnPos;

            CircleCollider2D collider = enemyObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true;

            int segment = CurrentSegmentIndex;
            int currentMonsterHp = Random.Range(mobHpMin[segment], mobHpMax[segment] + 1); // Random.Range(int,int) max is exclusive

            EnemyMonster enemy = enemyObj.AddComponent<EnemyMonster>();
            enemy.Initialize(playerTransform, enemySprite, new Color(1.0f, 0.3f, 0.8f), currentMonsterHp);

            activeEnemies.Add(enemy);
        }
    }
}
