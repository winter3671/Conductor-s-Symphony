using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 첼로: 홀드("13칸 베이스 롱노트") 시작 시 그 시점의 가장 가까운 적 발밑에 고정된 중력장을 생성한다.
    // 필드는 캐스팅 위치에 고정되며 적을 추적하지 않는다(기획서 "고정된 중력장" 문구 그대로 반영).
    // 범위 내 적의 이동 속도를 감소시키고 주기적으로 타격. 기획서 7번(중력의 구속) 참고.
    public class CelloGravityFieldEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int damage;
        private float radius;
        private float slowFraction;
        private const float TickInterval = 0.4f;
        private float tickTimer;

        private readonly HashSet<EnemyMonster> affectedEnemies = new HashSet<EnemyMonster>();

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.damage = damage;
            radius = 1.8f + 0.2f * Mathf.Max(0, level - 1);                          // 레벨당 범위 소폭 증가
            slowFraction = Mathf.Min(0.7f, 0.5f + 0.05f * Mathf.Max(0, level - 1));   // 이속 감소량(최대 70%)

            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            transform.position = nearest != null ? nearest.transform.position : origin;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.45f;
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(28, 13f, faded);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);
        }

        public void OnHoldTick(float deltaTime)
        {
            HashSet<EnemyMonster> currentlyInRange = new HashSet<EnemyMonster>();
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.transform.position) <= radius)
                {
                    currentlyInRange.Add(enemy);
                    if (!affectedEnemies.Contains(enemy))
                    {
                        enemy.SetSpeedMultiplier(1f - slowFraction);
                    }
                }
            }

            // 필드를 벗어난 적은 감속을 해제해야 원래 속도로 되돌아온다.
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null && !currentlyInRange.Contains(enemy))
                {
                    enemy.SetSpeedMultiplier(1f);
                }
            }

            affectedEnemies.Clear();
            foreach (var e in currentlyInRange) affectedEnemies.Add(e);

            tickTimer += deltaTime;
            if (tickTimer < TickInterval) return;
            tickTimer = 0f;

            foreach (var enemy in currentlyInRange)
            {
                if (enemy != null) enemy.TakeDamage(damage);
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null) enemy.SetSpeedMultiplier(1f);
            }
            affectedEnemies.Clear();
            Destroy(gameObject);
        }
    }
}
