using System.Collections;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.Enemy
{
    public class BossMonster : MonoBehaviour
    {
        public static BossMonster Instance { get; private set; }

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentHp = maxHp;
            SetupComponents();
        }

        public void Initialize(int hp)
        {
            maxHp = hp;
            currentHp = maxHp;
            if (RhythmUI.Instance != null)
            {
                RhythmUI.Instance.ShowBossHpBar(true, maxHp);
            }
        }

        private void SetupComponents()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateBossSprite();
            spriteRenderer.color = new Color(1.0f, 0.2f, 0.2f);
            spriteRenderer.sortingOrder = 7;

            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.8f;
            circleCollider.isTrigger = true;

            transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        }

        private static Sprite CreateBossSprite()
        {
            int size = 48;
            Texture2D tex = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 20f && d >= 12f)
                    {
                        px[y * size + x] = new Color(1.0f, 0.85f, 0.0f);
                    }
                    else if (d < 12f)
                    {
                        px[y * size + x] = new Color(0.9f, 0.1f, 0.1f);
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (RhythmUI.Instance != null)
            {
                RhythmUI.Instance.ShowBossHpBar(true, maxHp);
            }
        }

        private void Update()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
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

            if (RhythmUI.Instance != null)
            {
                RhythmUI.Instance.UpdateBossHp(currentHp, maxHp);
            }

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
            if (RhythmUI.Instance != null)
            {
                RhythmUI.Instance.ShowBossHpBar(false, maxHp);
            }

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
