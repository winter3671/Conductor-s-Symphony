using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Enemy
{
    public class EnemySpawner : MonoSingleton<EnemySpawner>
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 8.0f;
        [SerializeField] private float bossInterval = 120.0f; // 2 minutes of farming after boss defeat

        private float bossTimer = 0f; // Accumulates only during normal farming phase
        private int stageLevel = 1; // Stage 1, Stage 2, Stage 3...
        private float nextSpawnTime;
        private Transform playerTransform;
        private List<EnemyMonster> activeEnemies = new List<EnemyMonster>();

        private Sprite enemySprite;

        public IReadOnlyList<EnemyMonster> ActiveEnemies => activeEnemies;
        public int StageLevel => stageLevel;

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
            nextSpawnTime = Time.time + GetSpawnIntervalForStage();
            bossTimer = 0f;
        }

        private float GetSpawnIntervalForStage()
        {
            if (stageLevel == 1) return 0.9f;
            else if (stageLevel == 2) return 0.55f;
            else return 0.35f;
        }

        private int GetMaxEnemiesForStage()
        {
            if (stageLevel == 1) return 25;
            else if (stageLevel == 2) return 45;
            else return 65;
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

            // Pause trash mob spawns & freeze boss timer during Boss battle
            if (BossMonster.Instance != null) return;

            // Increment boss timer only during normal farming phase
            bossTimer += Time.deltaTime;
            if (bossTimer >= bossInterval)
            {
                bossTimer = 0f;
                stageLevel++;
                SpawnBoss();
                return;
            }

            float currentSpawnInterval = GetSpawnIntervalForStage();
            int currentMaxEnemies = GetMaxEnemiesForStage();

            if (Time.time >= nextSpawnTime)
            {
                if (activeEnemies.Count < currentMaxEnemies)
                {
                    SpawnEnemy();
                }
                nextSpawnTime = Time.time + currentSpawnInterval;
            }
        }

        private void SpawnBoss()
        {
            Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 spawnPos = centerPos + new Vector3(0f, spawnRadius, 0f);

            GameObject bossObj = new GameObject($"EliteBossMonster_Stage_{stageLevel}");
            bossObj.transform.position = spawnPos;
            BossMonster boss = bossObj.AddComponent<BossMonster>();

            // Scaled Elite Boss HP: Stage 1 Boss = 120 HP, Stage 2 Boss = 200 HP, Stage 3 Boss = 280 HP
            int bossHp = 120 + (stageLevel - 1) * 80;
            boss.Initialize(bossHp);
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

            // High Tense Scaling HP per Stage: Stage 1 = 4 HP (2 Perfect hits), Stage 2 = 14 HP (7 Perfect hits), Stage 3 = 30 HP (15 Perfect hits)
            int currentMonsterHp = 4 + (stageLevel - 1) * 10;

            EnemyMonster enemy = enemyObj.AddComponent<EnemyMonster>();
            enemy.Initialize(playerTransform, enemySprite, new Color(1.0f, 0.3f, 0.8f), currentMonsterHp);

            activeEnemies.Add(enemy);
        }
    }
}
