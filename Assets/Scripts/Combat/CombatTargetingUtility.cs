using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat
{
    // 10종 악기별 공격 메커니즘 기획서의 타겟팅 방식("가장 가까운 적", "체력이 가장 높은 적") 공용 헬퍼.
    // 악기별 고유 공격 로직을 만들 때 이 클래스만 참조하면 되도록 한 곳에 모아둔다.
    public static class CombatTargetingUtility
    {
        public static EnemyMonster GetNearestEnemy(Vector3 origin)
        {
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            EnemyMonster nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        // 글록켄슈필: "체력이 가장 높은 적" 타겟팅. 동률이면 더 가까운 쪽을 우선한다(플레이어 체감상 자연스러움).
        public static EnemyMonster GetHighestHpEnemy(Vector3 originForTieBreak)
        {
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            EnemyMonster best = null;
            int bestHp = int.MinValue;
            float bestDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                int hp = enemy.CurrentHealth;
                float dist = Vector3.Distance(originForTieBreak, enemy.transform.position);

                if (hp > bestHp || (hp == bestHp && dist < bestDist))
                {
                    best = enemy;
                    bestHp = hp;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
