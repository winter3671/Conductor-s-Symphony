using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 직선으로 날아가며 지나가는 모든 적을 관통(pierce) 타격하는 공용 투사체.
    // 피아노(건반 레이저), 벨(8방향 성광), 마림바(직선 파동)가 전부 이 클래스를 공유한다.
    public class PiercingBeamProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private int damage;
        private int remainingPierce;
        private float maxRange;
        private float traveledDistance;
        private bool bounceOnMaxRange;
        private bool hasBounced;
        private const float HitRadius = 0.5f;

        private readonly HashSet<EnemyMonster> hitEnemies = new HashSet<EnemyMonster>();
        private bool hasHitBoss = false;

        public void Initialize(Vector3 startPos, Vector3 dir, float speed, int damage, int pierceCount, float maxRange, bool bounceOnMaxRange, Sprite sprite, Color color, float visualLength = 1.1f)
        {
            transform.position = startPos;
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            this.speed = speed;
            this.damage = damage;
            remainingPierce = Mathf.Max(1, pierceCount);
            this.maxRange = maxRange;
            this.bounceOnMaxRange = bounceOnMaxRange;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 15;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(visualLength, 0.28f, 1f);
        }

        private void Update()
        {
            Vector3 step = direction * speed * Time.deltaTime;
            transform.position += step;
            traveledDistance += step.magnitude;

            CheckHits();
            if (remainingPierce <= 0)
            {
                Destroy(gameObject);
                return;
            }

            if (traveledDistance >= maxRange)
            {
                if (bounceOnMaxRange && !hasBounced)
                {
                    hasBounced = true;
                    traveledDistance = 0f;
                    direction = -direction;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void CheckHits()
        {
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null || hitEnemies.Contains(enemy)) continue;

                if (Vector3.Distance(transform.position, enemy.transform.position) <= HitRadius)
                {
                    enemy.TakeDamage(damage);
                    hitEnemies.Add(enemy);
                    remainingPierce--;
                    if (remainingPierce <= 0) return;
                }
            }

            if (!hasHitBoss && BossMonster.Instance != null)
            {
                if (Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= HitRadius)
                {
                    BossMonster.Instance.TakeDamage(damage);
                    hasHitBoss = true;
                    remainingPierce--;
                }
            }
        }
    }
}
