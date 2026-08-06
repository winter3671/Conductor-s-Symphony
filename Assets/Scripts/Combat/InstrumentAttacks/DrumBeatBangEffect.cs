using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 5. 드럼: 정박(1,5,9,13) 타격 시 플레이어 중심 360도 비트 뱅(넉백 파동) ----
    // 밸런스 doc(game_balance_design.docx) 5번 항목: Lv2 충격파 피해량·범위 +20% / Lv3 넉백 거리 2배 +
    // 0.5초 둔화(둔화 배율 자체는 doc에 수치가 없어 50%로 가정) / Lv5 2연속 중첩 충격파.
    // (Lv4 "비트 오라 지속 피해량 +50%"는 여기가 아니라 RhythmAttackManager.UpdateDrumAura()가 담당)
    public class DrumBeatBangEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            float radius = 2.0f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier(); // Lv2+: 범위 +20%
            int shockwaveDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (level >= 2 ? 1.2f : 1f))); // Lv2+: 피해량 +20%
            float knockbackImpulse = 0.6f * (level >= 3 ? 2f : 1f);                                // Lv3+: 넉백 거리 2배
            bool applySlow = level >= 3;                                                            // Lv3+: 0.5초 둔화
            int shockwaveCount = (level >= 5) ? 2 : 1;                                              // Lv5: 2연속 중첩 충격파

            for (int i = 0; i < shockwaveCount; i++)
            {
                FireBeatBangShockwave(origin, radius, shockwaveDamage, knockbackImpulse, applySlow, color);
            }
        }

        private static void FireBeatBangShockwave(Vector3 origin, float radius, int damage, float knockbackImpulse, bool applySlow, Color color)
        {
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > radius) continue;

                enemy.TakeDamage(damage);
                if (dist > 0.01f)
                {
                    enemy.transform.position += toEnemy.normalized * knockbackImpulse;
                }
                if (applySlow)
                {
                    enemy.ApplyTemporarySlow(0.5f, 0.5f); // 50% 감속, 0.5초 (감속 배율은 doc에 명시 없어 임의값)
                }
            }

            if (BossMonster.Instance != null && Vector3.Distance(origin, BossMonster.Instance.transform.position) <= radius)
            {
                BossMonster.Instance.TakeDamage(damage);
            }

            GameObject ringObj = new GameObject("DrumBeatBang");
            ShockwaveVisualEffect ring = ringObj.AddComponent<ShockwaveVisualEffect>();
            ring.Initialize(origin, radius, color);
        }
    }
}
