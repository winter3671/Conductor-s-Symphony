using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 4. 글록켄슈필: 체력이 가장 높은 적 머리 위 별빛 낙하 ----
    // 밸런스 doc 5번 항목: Lv2 피해량 +30% / Lv3 스플래시 추가 / Lv4 버스트 성공 시 별빛 수 +2(항상) /
    // Lv5 2차 유도 파편 폭발 + 0.5초 기절.
    public class GlockenspielStarfallEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster target = CombatTargetingUtility.GetHighestHpEnemy(origin);
            Vector3 pos = target != null ? target.transform.position : origin;

            int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.3f : 1f))); // Lv2+: 피해량 +30%
            // Lv3+: 스플래시 추가 × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            float splashRadius = (0.6f + (level >= 3 ? 0.5f : 0f)) * CombatTargetingUtility.GetRangeMultiplier();
            TapAttackHelpers.SpawnImpact(pos, 0.15f, splashRadius, scaledDamage, color);

            bool burstReady = level >= 4 && currentCombo > 0 && currentCombo % 4 == 0;
            if (burstReady)
            {
                const int extraStars = 2; // Lv4+: 버스트 성공 시 별빛 수 +2 (doc 명시대로 항상 +2)
                for (int i = 0; i < extraStars; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), 0f);
                    TapAttackHelpers.SpawnImpact(pos + offset, 0.15f, splashRadius, scaledDamage, color);
                }
            }

            // Lv5: 2차 유도 파편 - 가장 가까운 "다른" 적을 향해 유도 투사체 발사, 명중 시 피해+0.5초 기절
            if (level >= 5)
            {
                EnemyMonster secondaryTarget = FindSecondaryShrapnelTarget(target, origin);
                if (secondaryTarget != null)
                {
                    GameObject shrapnelObj = new GameObject("GlockenspielShrapnel");
                    HomingShrapnelProjectile shrapnel = shrapnelObj.AddComponent<HomingShrapnelProjectile>();
                    shrapnel.Initialize(pos, secondaryTarget, scaledDamage, speed: 10f, stunDuration: 0.5f, TapAttackHelpers.StarSprite, color);
                }
            }
        }

        private static EnemyMonster FindSecondaryShrapnelTarget(EnemyMonster primaryTarget, Vector3 origin)
        {
            EnemyMonster best = null;
            float bestDist = float.MaxValue;
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null || enemy == primaryTarget) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }
            return best;
        }
    }
}
