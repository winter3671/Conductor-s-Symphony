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
        private const float BaseHitRadius = 0.5f;
        private float hitRadius = BaseHitRadius;

        // 마림바 Lv3("파동 크기 +30%") 등 - 관통 파동/레이저의 히트 반경과 시각적 크기를 함께 키운다.
        private float sizeMultiplier = 1f;

        // 마림바 Lv5(피격 시 감속+밀쳐냄), 바이올린 Lv5(참격 자리 잔향) 등 - 명중한 적/위치별로 악기 고유의
        // 추가 처리가 필요할 때 이 콜백으로 위임한다. 공용 투사체 클래스 자체는 특정 악기를 모른다.
        private System.Action<EnemyMonster, Vector3> onHitEnemy;

        private readonly HashSet<EnemyMonster> hitEnemies = new HashSet<EnemyMonster>();
        private bool hasHitBoss = false;

        public void Initialize(Vector3 startPos, Vector3 dir, float speed, int damage, int pierceCount, float maxRange, bool bounceOnMaxRange, Sprite sprite, Color color, float visualLength = 1.1f, float sizeMultiplier = 1f, System.Action<EnemyMonster, Vector3> onHitEnemy = null)
        {
            transform.position = startPos;
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            this.speed = speed;
            this.damage = damage;
            remainingPierce = Mathf.Max(1, pierceCount);
            this.maxRange = maxRange;
            this.bounceOnMaxRange = bounceOnMaxRange;
            this.sizeMultiplier = sizeMultiplier;
            this.hitRadius = BaseHitRadius * sizeMultiplier;
            this.onHitEnemy = onHitEnemy;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 15;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(visualLength * sizeMultiplier, 0.28f * sizeMultiplier, 1f);
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

                if (Vector3.Distance(transform.position, enemy.transform.position) <= hitRadius)
                {
                    enemy.TakeDamage(damage);
                    onHitEnemy?.Invoke(enemy, transform.position);
                    hitEnemies.Add(enemy);
                    remainingPierce--;
                    if (remainingPierce <= 0) return;
                }
            }

            if (!hasHitBoss && BossMonster.Instance != null)
            {
                if (Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= hitRadius)
                {
                    BossMonster.Instance.TakeDamage(damage);
                    hasHitBoss = true;
                    remainingPierce--;
                }
            }
        }
    }
}
