using System.Collections;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Enemy
{
    public class BossMonster : MonoSingleton<BossMonster>
    {
        public static event System.Action<int> OnBossSpawnedEvent;       // maxHp
        public static event System.Action<int, int> OnBossHpChangedEvent; // currentHp, maxHp
        public static event System.Action OnBossDefeatedEvent;           // elite defeated (reward chest)
        public static event System.Action OnFinalBossClearedEvent;       // 10:00~12:00 final boss defeated in time
        public static event System.Action OnFinalBossTimeUpEvent;        // final boss NOT defeated within time limit (defeat)
        public static event System.Action<float, float> OnFinalBossTimeChangedEvent; // remaining, limit

        [Header("Boss Stats")]
        [SerializeField] private int maxHp = 120;
        private int currentHp;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.2f;

        private PlayerController player;
        private SpriteRenderer spriteRenderer;
        private CircleCollider2D circleCollider;

        private float attackTimer = 0f;
        private float attackInterval = 3.5f;
        private int attackPatternIndex = 0;

        // Final boss (10:00~12:00 time-attack) specific state - see game_balance_design.docx section 3
        private bool isFinalBoss = false;
        private float finalBossTimeLimit = 120f;
        private float finalBossTimer = 0f;
        private bool timeUpTriggered = false;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public bool IsFinalBoss => isFinalBoss;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            currentHp = maxHp;
            SetupComponents();
        }

        public void Initialize(int hp)
        {
            maxHp = hp;
            currentHp = maxHp;
            isFinalBoss = false;
            OnBossSpawnedEvent?.Invoke(maxHp);
        }

        // 같은 프레임 안에서 이 인스턴스를 Destroy()하고 곧바로 새 BossMonster(예: 최종 보스)를
        // AddComponent 하는 경우, Destroy()는 프레임 끝까지 지연 적용되므로 새 인스턴스의 Awake()가
        // 아직 살아있는 이 인스턴스를 보고 자기 자신을 파괴해버리는 소프트락이 발생할 수 있다.
        // (실측: 엘리트 생존 중 10:00 도달 시 최종 보스가 영구히 스폰되지 않던 버그, balance_1to3_test_result.md 참고)
        // Destroy() 호출 "직전"에 이 메서드로 정적 Instance를 먼저 비워서 새 인스턴스가 정상적으로 등록되게 한다.
        public void ReleaseSingletonSlot()
        {
            if (Instance == this)
            {
                ClearInstance();
            }
        }

        // 10:00~12:00 최종 보스전 (HP 180,000 / 120초 타임어택) 전용 초기화
        public void InitializeFinalBoss(int hp, float timeLimit = 120f)
        {
            maxHp = hp;
            currentHp = maxHp;
            isFinalBoss = true;
            finalBossTimeLimit = timeLimit;
            finalBossTimer = 0f;
            timeUpTriggered = false;
            OnBossSpawnedEvent?.Invoke(maxHp);
        }

        private void SetupComponents()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ProceduralSpriteFactory.CreateRingWithCore(48, 12f, 20f, new Color(1.0f, 0.85f, 0.0f), new Color(0.9f, 0.1f, 0.1f));
            spriteRenderer.color = new Color(1.0f, 0.2f, 0.2f);
            spriteRenderer.sortingOrder = 7;

            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.8f;
            circleCollider.isTrigger = true;

            transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        }

        private void Start()
        {
            player = PlayerController.Instance;
            OnBossSpawnedEvent?.Invoke(maxHp);
        }

        private void Update()
        {
            if (isFinalBoss && !timeUpTriggered)
            {
                finalBossTimer += Time.deltaTime;
                OnFinalBossTimeChangedEvent?.Invoke(Mathf.Max(0f, finalBossTimeLimit - finalBossTimer), finalBossTimeLimit);

                if (finalBossTimer >= finalBossTimeLimit)
                {
                    TriggerTimeUpDefeat();
                    return;
                }
            }

            if (player == null) player = PlayerController.Instance;
            if (player != null)
            {
                Vector3 dir = (player.transform.position - transform.position).normalized;
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist > 4.0f)
                {
                    transform.position += dir * moveSpeed * Time.deltaTime;
                }
            }

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                ExecuteAttackPattern(attackPatternIndex);
                attackPatternIndex = (attackPatternIndex + 1) % 3;
            }
        }

        private void ExecuteAttackPattern(int index)
        {
            switch (index)
            {
                case 0:
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = i * (360f / 12f);
                        Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                        SpawnBossBullet(dir, 4.0f, Color.red);
                    }
                    break;

                case 1:
                    if (player != null)
                    {
                        StartCoroutine(TargetedVolleyRoutine());
                    }
                    break;

                case 2:
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = i * (360f / 16f) + 15f;
                        Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                        SpawnBossBullet(dir, 4.8f, Color.magenta);
                    }
                    break;
            }
        }

        private IEnumerator TargetedVolleyRoutine()
        {
            for (int burst = 0; burst < 3; burst++)
            {
                if (player != null)
                {
                    Vector3 dir = (player.transform.position - transform.position).normalized;
                    SpawnBossBullet(dir, 6.0f, Color.yellow);
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void SpawnBossBullet(Vector3 dir, float speed, Color color)
        {
            GameObject bulletObj = new GameObject($"BossBullet_{Time.frameCount}");
            bulletObj.transform.position = transform.position;
            BossProjectile bullet = bulletObj.AddComponent<BossProjectile>();
            bullet.Initialize(dir, speed, color, 10);
        }

        public void TakeDamage(int damage)
        {
            currentHp -= damage;
            StartCoroutine(FlashDamageRoutine());

            OnBossHpChangedEvent?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashDamageRoutine()
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = new Color(1.0f, 0.2f, 0.2f);
        }

        private void Die()
        {
            if (isFinalBoss)
            {
                // 최종 보스 클리어 (게임 클리어). 전리품 상자는 엘리트 전용이라 여기서는 스폰하지 않음.
                OnFinalBossClearedEvent?.Invoke();
                Debug.Log("[BossMonster] Final boss cleared within time limit - Victory!");
            }
            else
            {
                OnBossDefeatedEvent?.Invoke();

                GameObject chestObj = new GameObject("EliteRewardChest");
                chestObj.transform.position = transform.position;
                chestObj.AddComponent<Item.EliteRewardChest>();
            }

            Destroy(gameObject);
        }

        private void TriggerTimeUpDefeat()
        {
            if (timeUpTriggered) return;
            timeUpTriggered = true;

            Debug.Log("[BossMonster] Final boss time limit exceeded - Defeat (Time Over)");
            // RhythmUI.HandleFinalBossTimeUp()가 이 이벤트를 구독해 패배 화면 표시 + Time.timeScale 정지를 전담한다.
            OnFinalBossTimeUpEvent?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(15);
            }
        }
    }
}
