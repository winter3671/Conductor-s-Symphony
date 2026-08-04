using System.Collections;
using UnityEngine;

namespace ConductorSymphony.Enemy
{
    public class EnemyMonster : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float moveSpeed = 1.1f;
        [SerializeField] private int maxHealth = 8;
        [SerializeField] private int damageToPlayer = 10;

        private int currentHealth;
        private Transform playerTransform;
        private SpriteRenderer spriteRenderer;

        // 첼로(중력의 구속) 등 이속 감소 효과용. 1.0 = 정상 속도. 필드를 벗어나면 다시 1.0으로 복원되어야 한다.
        private float speedMultiplier = 1f;

        // 팀파니 Lv4(기절)/글록켄슈필 Lv5(유도 파편 기절) 공용. 0보다 크면 이동이 완전히 멈춘다.
        // speedMultiplier와 별개 필드로 관리 - 기절이 풀리면 원래 감속 상태(있었다면)로 자연히 복귀해야 하기 때문.
        private float stunTimer = 0f;

        // 프렌치호른 Lv4(피해 증폭 디버프) 등 "받는 피해량 배율" 효과용. 1.0 = 정상.
        private float damageAmpMultiplier = 1f;

        // 드럼 Lv3(0.5초 둔화)/마림바 Lv5(피격 시 이속 30% 감소) 등 - SetSpeedMultiplier(지속형, 예: 첼로
        // 중력장)와 달리 "일정 시간 뒤 자동으로 풀리는" 한시적 감속. speedMultiplier와는 별개로 관리하고,
        // 매 프레임 둘 중 더 강한(낮은) 배율을 적용한다.
        private float tempSlowTimer = 0f;
        private float tempSlowMultiplier = 1f;

        public int DamageToPlayer => damageToPlayer;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsStunned => stunTimer > 0f;

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        // duration(초) 동안 이동을 완전히 정지시킨다. 이미 더 긴 기절이 걸려있으면 짧은 것으로 덮어쓰지 않는다.
        public void ApplyStun(float duration)
        {
            stunTimer = Mathf.Max(stunTimer, duration);
        }

        // multiplier(예: 0.7 = 30% 감속) 배율로 duration(초) 동안만 한시적으로 감속시킨다.
        public void ApplyTemporarySlow(float multiplier, float duration)
        {
            if (tempSlowTimer <= 0f || multiplier < tempSlowMultiplier)
            {
                tempSlowMultiplier = multiplier;
            }
            tempSlowTimer = Mathf.Max(tempSlowTimer, duration);
        }

        public void SetDamageAmpMultiplier(float multiplier)
        {
            damageAmpMultiplier = multiplier;
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        public void Initialize(Transform targetPlayer, Sprite defaultSprite, Color color, int initialHp = 8)
        {
            playerTransform = targetPlayer;
            maxHealth = initialHp;
            currentHealth = maxHealth;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = 5;
            }
        }

        private void Update()
        {
            if (stunTimer > 0f) stunTimer -= Time.deltaTime;
            if (tempSlowTimer > 0f) tempSlowTimer -= Time.deltaTime;

            if (playerTransform == null) return;

            // 기절 중에는 이속 배율과 무관하게 완전히 정지 (speedMultiplier는 그대로 유지되어, 기절이
            // 풀리면 감속 등 기존 상태로 자연히 복귀한다). 한시적 감속(tempSlowTimer)이 걸려있으면
            // 지속형 speedMultiplier와 비교해 더 강한(낮은) 쪽을 적용한다.
            float baseMultiplier = (tempSlowTimer > 0f) ? Mathf.Min(speedMultiplier, tempSlowMultiplier) : speedMultiplier;
            float effectiveSpeedMultiplier = (stunTimer > 0f) ? 0f : baseMultiplier;

            // Move towards player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * effectiveSpeedMultiplier * Time.deltaTime;

            // Apply mutual separation force from neighboring enemies to prevent stacking into a single point
            ApplySeparation();
        }

        private void ApplySeparation()
        {
            if (EnemySpawner.Instance == null) return;
            var enemies = EnemySpawner.Instance.ActiveEnemies;
            if (enemies == null) return;

            Vector3 separation = Vector3.zero;
            int count = 0;
            float minRadius = 0.65f;

            for (int i = 0; i < enemies.Count; i++)
            {
                var other = enemies[i];
                if (other == null || other == this) continue;

                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < minRadius && dist > 0.01f)
                {
                    Vector3 pushDir = (transform.position - other.transform.position).normalized;
                    separation += pushDir / dist; // Stronger push when closer
                    count++;
                }
            }

            if (count > 0)
            {
                transform.position += separation.normalized * 0.9f * Time.deltaTime;
            }
        }

        public void TakeDamage(int damage)
        {
            // 프렌치호른 Lv4 "범위 내 적 피해량 +15% 증폭 디버프" 등 - 이 적이 받는 모든 피해에 곱연산 적용
            int amplifiedDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageAmpMultiplier));
            currentHealth -= amplifiedDamage;

            // Flash red on hit
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRedRoutine());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void Die()
        {
            // 100% Guaranteed EXP Gem Drop on every kill.
            // EXP amount scales with elapsed game time per game_balance_design.docx section 2 (10/12/15/20 across 4 segments).
            int expAmount = EnemySpawner.Instance != null ? EnemySpawner.Instance.GetCurrentExpPerKill() : 15;

            GameObject gemObj = new GameObject($"ExpGem_{Time.frameCount}");
            Player.ExpGem gem = gemObj.AddComponent<Player.ExpGem>();
            gem.Initialize(transform.position, expAmount);

            Destroy(gameObject);
        }
    }
}
