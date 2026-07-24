using UnityEngine;

namespace ConductorSymphony.Enemy
{
    public class EnemyMonster : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private int maxHealth = 2;
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

        public void Initialize(Transform targetPlayer, Sprite defaultSprite, Color color)
        {
            playerTransform = targetPlayer;
            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = 5;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
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

        private System.Collections.IEnumerator FlashRedRoutine()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
