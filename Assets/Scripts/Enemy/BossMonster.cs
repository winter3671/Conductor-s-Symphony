using System.Collections;
using System.Collections.Generic;
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
        private Transform visualTransform;

        // 2026-08-09: 엘리트(악기 변형 몬스터 3종 중 랜덤)/최종보스(오르골) 도트 아트 연동.
        // 콜라이더는 root에 고정 월드 반경(2.0 = 기존 collider.radius 0.8 × root scale 2.5와 동일)으로
        // 붙이고, root의 localScale은 더 이상 건드리지 않는다 - 아트는 별도 자식(Visual)에 그리고 그
        // 자식의 localScale만 sprite.bounds 기준으로 정규화한다(다른 이펙트들과 동일한 패턴).
        private const float EliteReferenceContentSize = 3.2f; // 목표 월드 지름 - 2026-08-09: 너무 작아 안 보인다는 피드백으로 2배 확대(기존 1.6)
        private const float BossReferenceContentSize = 4.8f;  // 최종보스는 더 위압적으로 크게 - 2026-08-09: 동일 사유로 2배 확대(기존 2.4)

        private static Sprite[] eliteSprites;
        private static bool triedLoadEliteSprites;
        private static Sprite bossSprite;
        private static bool triedLoadBossSprite;

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

        // 2026-08-09: 엘리트/보스 시각 크기 2배 확대 이후 "공격했는데 판정이 안 맞는다"는 피드백으로 추가.
        // 공격 판정(각 InstrumentAttacks 이펙트의 Vector3.Distance(...) <= radius 체크)은 원래 보스를
        // 반지름 0인 점(transform.position)으로 취급해서, 보스 본체가 아무리 크게 보여도 판정 거리엔
        // 전혀 반영되지 않았다. 이 프로퍼티(= 현재 비주얼 반지름)를 각 이펙트의 radius에 더해주면
        // "보스 몸통 크기만큼 판정도 같이 커진다". 몸박(OnTriggerEnter2D) 콜라이더 반지름도 동일 값으로
        // 맞춰서 두 판정(공격 피격/몸박 충돌)이 항상 같은 크기를 쓰도록 통일한다.
        public float HitboxRadius => (isFinalBoss ? BossReferenceContentSize : EliteReferenceContentSize) / 2f;

        // 몸박(플레이어 접촉 데미지) 콜라이더 전용 하한선. 엘리트의 새 비주얼 반지름(1.6)이 기존
        // 고정 콜라이더(2.0)보다 오히려 작아서, HitboxRadius를 그대로 쓰면 "크기에 맞춰 늘려달라"는
        // 요청과 반대로 엘리트 몸박 판정이 줄어드는 역효과가 생긴다. 최종보스(2.4)는 이미 2.0보다
        // 커서 이 하한선의 영향을 받지 않는다.
        private const float LegacyContactRadius = 2.0f;

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
            ApplyVisual(finalBoss: false);
            if (circleCollider != null) circleCollider.radius = Mathf.Max(HitboxRadius, LegacyContactRadius);
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
            ApplyVisual(finalBoss: true);
            if (circleCollider != null) circleCollider.radius = Mathf.Max(HitboxRadius, LegacyContactRadius);
            OnBossSpawnedEvent?.Invoke(maxHp);
        }

        private void SetupComponents()
        {
            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualTransform = visualObj.transform;
            spriteRenderer = visualObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 7;
            // 실제 스프라이트/색/크기는 Initialize()/InitializeFinalBoss()에서 엘리트인지 최종보스인지가
            // 확정된 뒤 ApplyVisual()로 적용한다 - Awake 시점엔 아직 알 수 없다. 그 전까지 잠깐 보일
            // 폴백으로 기존 프로시저럴 스프라이트를 임시로 넣어둔다.
            spriteRenderer.sprite = ProceduralSpriteFactory.CreateRingWithCore(48, 12f, 20f, new Color(1.0f, 0.85f, 0.0f), new Color(0.9f, 0.1f, 0.1f));
            visualTransform.localScale = Vector3.one * 2.5f;

            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            // Awake 시점엔 아직 엘리트/보스가 안 정해져서 HitboxRadius를 못 씀 - Initialize()/
            // InitializeFinalBoss()에서 곧바로 HitboxRadius로 덮어쓰므로 여기 값은 잠깐만 쓰이는 폴백.
            circleCollider.radius = 2.0f;
            circleCollider.isTrigger = true;
        }

        // finalBoss=false: 엘리트(악기 변형 3종 중 랜덤). finalBoss=true: 최종보스(오르골) 고정.
        // 로드 실패 시 SetupComponents()에서 넣어둔 기존 프로시저럴 스프라이트를 그대로 유지한다.
        private void ApplyVisual(bool finalBoss)
        {
            if (spriteRenderer == null || visualTransform == null) return;

            Sprite sprite = finalBoss ? EnsureBossSprite() : PickRandomEliteSprite();
            if (sprite == null) return; // 폴백 프로시저럴 스프라이트 유지

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white; // 실제 아트는 자체 색을 갖고 있으므로 틴트 없음

            float targetSize = finalBoss ? BossReferenceContentSize : EliteReferenceContentSize;
            Bounds b = sprite.bounds;
            float maxDim = Mathf.Max(b.size.x, b.size.y);
            float scale = (maxDim > 0.0001f) ? (targetSize / maxDim) : 1f;
            visualTransform.localScale = Vector3.one * scale;
        }

        private static Sprite PickRandomEliteSprite()
        {
            Sprite[] sprites = EnsureEliteSprites();
            if (sprites == null || sprites.Length == 0) return null;
            return sprites[Random.Range(0, sprites.Length)];
        }

        private static Sprite[] EnsureEliteSprites()
        {
            if (!triedLoadEliteSprites)
            {
                triedLoadEliteSprites = true;
                string[] names = { "Violin_Elite", "Piano_Elite", "Drum_Elite" };
                var list = new List<Sprite>();
                foreach (var n in names)
                {
                    Sprite s = Resources.Load<Sprite>($"Sprites/Enemy/Elite/{n}");
                    if (s != null) list.Add(s);
                }
                eliteSprites = list.ToArray();
            }
            return eliteSprites;
        }

        private static Sprite EnsureBossSprite()
        {
            if (!triedLoadBossSprite)
            {
                triedLoadBossSprite = true;
                bossSprite = Resources.Load<Sprite>("Sprites/Enemy/Boss/MusicBox_Boss");
            }
            return bossSprite;
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

        // 2026-08-09: EnemyMonster.FlashRedRoutine과 동일한 이유로 tint 방식 대신 깜빡임 방식으로 교체
        // (곱연산 tint는 어두운 색이 섞인 실제 아트에선 흰색 플래시가 안 보임). 기존엔 이 메서드가
        // 상시 빨간 tint(1,0.2,0.2)로 되돌리는 역할도 겸했는데, 그 상시 tint 자체가 실제 아트 고유
        // 색상을 다 붉게 덮어버리는 문제였으므로 제거 - ApplyVisual()에서 Color.white(무틴트)로 고정.
        private IEnumerator FlashDamageRoutine()
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.enabled = true;
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
