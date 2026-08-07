using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 4. 글록켄슈필: 체력이 가장 높은 적 머리 위 별빛 낙하 ----
    // 밸런스 doc 5번 항목: Lv2 피해량 +30% / Lv3 스플래시 추가 / Lv4 버스트 성공 시 별빛 수 +2(항상) /
    // Lv5 2차 유도 파편 폭발 + 0.5초 기절.
    public class GlockenspielStarfallEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles)
        {
            EnemyMonster target = CombatTargetingUtility.GetHighestHpEnemy(origin);
            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때도 낙하 지점이 보스 위치를 노리도록
            // GetHighestHpTargetPosition으로 폴백(기존엔 origin=플레이어 위치에 계속 떨어지던 버그).
            // target(EnemyMonster) 자체는 null로 남겨둔다 - 아래 2차 유도 파편의 "1차 타겟 제외" 비교에서
            // 보스는 애초에 GetActiveEnemies() 루프에 안 들어오니 null이어도 자연스럽게 아무것도
            // 제외하지 않을 뿐, 별도 처리가 필요 없다.
            Vector3 pos = CombatTargetingUtility.GetHighestHpTargetPosition(origin, origin);

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

            // 레가토(Legato) 패시브/악기 Lv4 Multi+1(extraProjectiles): Lv4 버스트(extraStars)와 같은
            // 랜덤 오프셋 방식으로 낙하 지점을 추가한다. 버스트 조건(전역 콤보 4배수)과 무관하게 매
            // 타격마다 적용된다.
            for (int e = 0; e < extraProjectiles; e++)
            {
                Vector3 legatoOffset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), 0f);
                TapAttackHelpers.SpawnImpact(pos + legatoOffset, 0.15f, splashRadius, scaledDamage, color);
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
