using UnityEngine;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 1. 피아노: 가장 가까운 적 방향 건반 관통 레이저 ----
    // 밸런스 doc 5번 항목: Lv2 피해량 +25% / Lv3 관통 +2 / Lv4 발사 수 +1 / Lv5 6연타 성공 시 폭포 추가 발사
    public class PianoBeamEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles)
        {
            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때 기존엔 방향을 못 구해 무조건 Vector3.up
            // (화면 위쪽)으로 쏘던 문제 - GetNearestTargetPosition으로 보스 위치까지 폴백하도록 교체.
            Vector3 targetPos = CombatTargetingUtility.GetNearestTargetPosition(origin, origin + Vector3.up);
            Vector3 dir = targetPos - origin;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up; // 타겟이 origin과 완전히 겹치는 극단 케이스 방어

            int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.25f : 1f))); // Lv2+: 피해량 +25%
            int pierce = 2 + (level >= 3 ? 2 : 0); // Lv1~2: 2관통, Lv3+: 4관통
            // Lv1~3: 1발, Lv4+: 2발 + 레가토(Legato) 패시브/악기 Lv4 Multi+1 합산치(extraProjectiles)
            int shots = 1 + (level >= 4 ? 1 : 0) + extraProjectiles;
            // 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv" 반영.
            float maxRange = 9f * CombatTargetingUtility.GetRangeMultiplier();

            for (int i = 0; i < shots; i++)
            {
                TapAttackHelpers.SpawnBeam(origin, dir, scaledDamage, pierce, maxRange, bounce: false, color);
            }

            // Lv5: 6연타 성공마다("건반 폭포") 부채꼴로 3발 추가 발사
            if (level >= 5 && currentCombo > 0 && currentCombo % 6 == 0)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 spreadDir = Quaternion.Euler(0f, 0f, i * 12f) * dir;
                    TapAttackHelpers.SpawnBeam(origin, spreadDir, scaledDamage, pierce, maxRange, false, color);
                }
            }
        }
    }
}
