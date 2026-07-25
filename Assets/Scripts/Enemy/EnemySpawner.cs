using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Player;

namespace ConductorSymphony.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float spawnRadius = 8.0f;
        [SerializeField] private int maxActiveEnemies = 20;
        [SerializeField] private float bossInterval = 60.0f; // Elite boss spawns every 60s

        private float nextBossSpawnTime = 60.0f;
        private int bossCycleCount = 0;
        private float nextSpawnTime;
        private Transform playerTransform;
        private List<EnemyMonster> activeEnemies = new List<EnemyMonster>();

        private Texture2D enemyTexture;
        private Sprite enemySprite;

        public IReadOnlyList<EnemyMonster> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            CreateEnemySprite();
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            nextSpawnTime = Time.time + spawnInterval;
            nextBossSpawnTime = Time.time + bossInterval;
        }

        private void CreateEnemySprite()
        {
            int size = 32;
            enemyTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            // Diamond / Note shape
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    if (dist <= 12f)
                    {
                        pixels[y * size + x] = Color.magenta;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            enemyTexture.SetPixels(pixels);
            enemyTexture.Apply();
            enemySprite = Sprite.Create(enemyTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // Trigger Recurring Elite Boss Spawn every bossInterval (60s)
            if (BossMonster.Instance == null && Time.time >= nextBossSpawnTime)
            {
                bossCycleCount++;
                SpawnBoss();
                nextBossSpawnTime = Time.time + bossInterval;
            }

            // Clean dead enemies
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            // Pause trash mob spawns during Boss battle
            if (BossMonster.Instance != null) return;

            if (Time.time >= nextSpawnTime)
            {
                if (activeEnemies.Count < maxActiveEnemies)
                {
                    SpawnEnemy();
                }
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void SpawnBoss()
        {
            Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 spawnPos = centerPos + new Vector3(0f, spawnRadius, 0f);

            GameObject bossObj = new GameObject($"EliteBossMonster_{bossCycleCount}");
            bossObj.transform.position = spawnPos;
            bossObj.AddComponent<BossMonster>();
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

            EnemyMonster enemy = enemyObj.AddComponent<EnemyMonster>();
            enemy.Initialize(playerTransform, enemySprite, new Color(1.0f, 0.3f, 0.8f));

            activeEnemies.Add(enemy);
        }
    }
}
