using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 2. 벨: 가장 가까운 적 중심 8방향 성광(星光) ----
    // 밸런스 doc 5번 항목: Lv2 사거리 +30%(1회성 - 레벨마다 복리 아님) / Lv3 관통력 강화 / Lv4 8방향
    // 2연속 발사 / Lv5 "지나간 자리가 1.5초간 빛나며 경로상 적 지속 타격" → 중심점에 잔향 장판 추가.
    public class BellStarburstEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            Vector3 center = nearest != null ? nearest.transform.position : origin;

            // Lv2+: 사거리 +30% (1회성 적용) × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            float range = 2.5f * (level >= 2 ? 1.3f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            int pierce = 3 + (level >= 3 ? 2 : 0);
            int bursts = (level >= 4) ? 2 : 1; // Lv4+: 8방향 섬광 2연속 발사

            for (int b = 0; b < bursts; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, 0f, i * 45f) * Vector3.right;
                    TapAttackHelpers.SpawnBeam(center, dir, damage, pierce, range, false, color);
                }
            }

            if (level >= 5)
            {
                int tickDamage = Mathf.Max(1, damage / 2);
                GameObject glowObj = new GameObject("BellAfterglow");
                LingeringZoneEffect glow = glowObj.AddComponent<LingeringZoneEffect>();
                glow.Initialize(center, radius: 1.0f, tickDamage, tickInterval: 0.3f, duration: 1.5f, color);
            }
        }
    }
}
