using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 5. 드럼: 정박(1,5,9,13) 타격 시 플레이어 중심 360도 비트 뱅(넉백 파동) ----
    // 밸런스 doc(game_balance_design.docx) 5번 항목: Lv2 충격파 피해량·범위 +20% / Lv3 넉백 거리 2배 +
    // 0.5초 둔화(둔화 배율 자체는 doc에 수치가 없어 50%로 가정) / Lv5 2연속 중첩 충격파.
    // (Lv4 "비트 오라 지속 피해량 +50%"는 여기가 아니라 RhythmAttackManager.UpdateDrumAura()가 담당)
    public class DrumBeatBangEffect : ITapAttackEffect
    {
        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 비트 뱅은 광역 판정이라 "낱개로 셀 수
        // 있는 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함).
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles)
        {
            // 2026-08-09: 레벨별 배율/수치를 InstrumentLevelStats로 데이터화(순수 추출, 값 변경 없음).
            float radius = 2.0f * InstrumentLevelStats.GetRangeMultiplier(InstrumentType.Drums, level) * CombatTargetingUtility.GetRangeMultiplier(); // Lv2+: 범위 +20%
            int shockwaveDamage = Mathf.Max(1, Mathf.RoundToInt(damage * InstrumentLevelStats.GetDamageMultiplier(InstrumentType.Drums, level))); // Lv2+: 피해량 +20%
            float knockbackImpulse = 0.6f * InstrumentLevelStats.GetKnockbackMultiplier(InstrumentType.Drums, level); // Lv3+: 넉백 거리 2배
            bool applySlow = level >= 3;                                                            // Lv3+: 0.5초 둔화
            int shockwaveCount = InstrumentLevelStats.GetStepCount(InstrumentType.Drums, level);    // Lv5: 2연속 중첩 충격파

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

            if (BossMonster.Instance != null && Vector3.Distance(origin, BossMonster.Instance.transform.position) <= radius + BossMonster.Instance.HitboxRadius)
            {
                BossMonster.Instance.TakeDamage(damage);
            }

            GameObject ringObj = new GameObject("DrumBeatBang");
            ShockwaveVisualEffect ring = ringObj.AddComponent<ShockwaveVisualEffect>();
            ring.Initialize(origin, radius, color);
        }
    }
}
