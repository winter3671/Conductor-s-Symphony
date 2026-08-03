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

        public int DamageToPlayer => damageToPlayer;

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
            if (playerTransform == null) return;

            // Move towards player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

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
            currentHealth -= damage;

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
