using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.Combat
{
    public class RhythmAttackManager : MonoBehaviour
    {
        public static RhythmAttackManager Instance { get; private set; }

        private PlayerController player;
        private EnemySpawner spawner;

        private Texture2D projectileTexture;
        private Sprite projectileSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateProjectileSprite();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            spawner = FindAnyObjectByType<EnemySpawner>();

            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent += HandleRhythmHit;
            }
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent -= HandleRhythmHit;
            }
        }

        private void CreateProjectileSprite()
        {
            int size = 20;
            projectileTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= 8f)
                    {
                        pixels[y * size + x] = Color.yellow;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            projectileTexture.SetPixels(pixels);
            projectileTexture.Apply();
            projectileSprite = Sprite.Create(projectileTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public void HandleRhythmHit(HitRating rating, RhythmLane lane)
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (spawner == null) spawner = FindAnyObjectByType<EnemySpawner>();

            Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;

            // Find nearest enemy
            EnemyMonster nearestEnemy = FindNearestEnemy(spawnPos);
            int damage = (rating == HitRating.Perfect) ? 2 : 1;
            Color projColor = (rating == HitRating.Perfect) ? Color.yellow : Color.cyan;

            GameObject projObj = new GameObject($"Proj_{Time.frameCount}");
            AttackProjectile proj = projObj.AddComponent<AttackProjectile>();
            proj.Initialize(nearestEnemy, spawnPos, projectileSprite, projColor, damage);
        }

        private EnemyMonster FindNearestEnemy(Vector3 originPos)
        {
            EnemyMonster[] enemies = FindObjectsByType<EnemyMonster>();
            EnemyMonster nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(originPos, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
