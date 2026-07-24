using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Header("Player Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        private int currentHealth;
        private float invulnerableTimer = 0f;
        private Rigidbody2D rb;
        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        public static event System.Action<int, int> OnHealthChangedEvent;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            col = GetComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            spriteRenderer = GetComponent<SpriteRenderer>();

            currentHealth = maxHealth;
        }

        private void Start()
        {
            OnHealthChangedEvent?.Invoke(currentHealth, maxHealth);
        }

        private void Update()
        {
            if (invulnerableTimer > 0f)
            {
                invulnerableTimer -= Time.deltaTime;
            }

            float moveX = 0f;
            float moveY = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rightArrowKey.isPressed) moveX += 1f;
                if (keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.upArrowKey.isPressed) moveY += 1f;
                if (keyboard.downArrowKey.isPressed) moveY -= 1f;
            }

            moveInput = new Vector2(moveX, moveY).normalized;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (invulnerableTimer > 0f) return;

            EnemyMonster enemy = other.GetComponent<EnemyMonster>();
            if (enemy != null)
            {
                TakeDamage(enemy.DamageToPlayer);
            }
        }

        public void TakeDamage(int amount)
        {
            if (invulnerableTimer > 0f) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            invulnerableTimer = invulnerabilityDuration;

            OnHealthChangedEvent?.Invoke(currentHealth, maxHealth);

            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0)
            {
                OnPlayerDeath();
            }
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                if (spriteRenderer != null) spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer != null) spriteRenderer.color = Color.yellow;
                yield return new WaitForSeconds(0.08f);
            }
        }

        private void OnPlayerDeath()
        {
            Debug.Log("Player Died!");
        }
    }
}
