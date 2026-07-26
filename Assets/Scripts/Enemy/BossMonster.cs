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
        public static event System.Action OnBossDefeatedEvent;

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

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;

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
            OnBossDefeatedEvent?.Invoke();

            GameObject chestObj = new GameObject("EliteRewardChest");
            chestObj.transform.position = transform.position;
            chestObj.AddComponent<Item.EliteRewardChest>();

            Destroy(gameObject);
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
