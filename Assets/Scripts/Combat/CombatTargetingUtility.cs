using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat
{
    // 10종 악기별 공격 메커니즘 기획서의 타겟팅 방식("가장 가까운 적", "체력이 가장 높은 적") 공용 헬퍼.
    // 악기별 고유 공격 로직을 만들 때 이 클래스만 참조하면 되도록 한 곳에 모아둔다.
    public static class CombatTargetingUtility
    {
        private static readonly EnemyMonster[] EmptyEnemies = new EnemyMonster[0];

        // (리팩토링 배경) 예전엔 이 메서드 대신 15곳에서 각자 Object.FindObjectsByType<EnemyMonster>()로
        // 씬 전체를 스캔했다 - EnemySpawner가 이미 살아있는 적 목록(ActiveEnemies)을 유지하고 있는데도
        // 매번 다시 조회한 셈이다. 잡몹 밀도가 높은 후반부(game_balance_design.docx 3번 항목,
        // 07:30~10:00 구간 동시 100~150마리)에서 특히 낭비가 컸다. 지금은 이 메서드 하나로 통일해
        // EnemySpawner.Instance.ActiveEnemies를 그대로 재사용한다(엘리트/보스는 EnemyMonster가 아니라
        // BossMonster라 원래도 별도 처리 - 이 통합으로 영향받지 않는다).
        public static IReadOnlyList<EnemyMonster> GetActiveEnemies()
        {
            return EnemySpawner.Instance != null ? EnemySpawner.Instance.ActiveEnemies : EmptyEnemies;
        }

        public static EnemyMonster GetNearestEnemy(Vector3 origin)
        {
            EnemyMonster nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in GetActiveEnemies())
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
            EnemyMonster best = null;
            int bestHp = int.MinValue;
            float bestDist = float.MaxValue;

            foreach (var enemy in GetActiveEnemies())
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
