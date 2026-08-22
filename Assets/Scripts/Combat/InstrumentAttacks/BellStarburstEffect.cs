using UnityEngine;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 2. 벨: 가장 가까운 적 중심 8방향 성광(星光) ----
    // 밸런스 doc 5번 항목: Lv2 사거리 +30%(1회성 - 레벨마다 복리 아님) / Lv3 관통력 강화 / Lv4 8방향
    // 2연속 발사 / Lv5 "지나간 자리가 1.5초간 빛나며 경로상 적 지속 타격" → 중심점에 잔향 장판 추가.
    public class BellStarburstEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles)
        {
            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때도(예: 최종보스 단독 페이즈) 보스 위치를
            // 중심으로 쓰도록 GetNearestTargetPosition으로 교체 - 기존 GetNearestEnemy는 EnemyMonster만
            // 봐서 이 경우 null을 반환, center가 플레이어 위치(origin)로 새서 보스전에 저조하게 만들던
            // 원인이었다(game_systems_reference.md §7-2 "벨 보스전" 항목 참고).
            Vector3 center = CombatTargetingUtility.GetNearestTargetPosition(origin, origin);

            // 2026-08-09: 레벨별 배율/수치를 InstrumentLevelStats로 데이터화(순수 추출, 값 변경 없음).
            // Lv2+: 사거리 +30% (1회성 적용) × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            float range = 2.5f * InstrumentLevelStats.GetRangeMultiplier(InstrumentType.Bell, level) * CombatTargetingUtility.GetRangeMultiplier();
            int pierce = InstrumentLevelStats.GetPierceCount(InstrumentType.Bell, level);
            int bursts = InstrumentLevelStats.GetStepCount(InstrumentType.Bell, level); // Lv4+: 8방향 섬광 2연속 발사

            // 2026-08-22 버그 수정(team_review_needed.md §2-5): 모든 성광이 center(가장 가까운 적의
            // 현재 위치)에서 발사되다 보니, 그 자리에 서 있는 보스/엘리트는 발사 즉시 모든 빔과 거리 0로
            // 겹쳐 한 번의 연주로 8~16회 중복 피격당했다(보스전이 다른 악기 대비 유독 쉬워지는 원인).
            // 이 연주(Execute 1회) 안에서 스폰되는 모든 빔이 이 배열 하나를 공유해, 보스는 총 1회만
            // 맞도록 캡을 건다. 잡몹(EnemyMonster)은 기존처럼 빔마다 독립적으로 각자 맞는다 - 다수
            // 잡몹을 흩어 때리는 광역 소탕력은 그대로 유지된다.
            bool[] bossHitGuard = new bool[1];

            for (int b = 0; b < bursts; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, 0f, i * 45f) * Vector3.right;
                    TapAttackHelpers.SpawnBeam(center, dir, damage, pierce, range, false, color, sharedBossHitGuard: bossHitGuard);
                }
            }

            // 레가토(Legato) 패시브/악기 Lv4 Multi+1(extraProjectiles): 기존 8방향 사이 빈 각도(22.5°
            // 간격)를 채우는 추가 성광으로 구현 - 기존 8방향과 겹치지 않게 자연스럽게 화력이 늘어난다.
            for (int e = 0; e < extraProjectiles; e++)
            {
                Vector3 extraDir = Quaternion.Euler(0f, 0f, 22.5f + e * 45f) * Vector3.right;
                TapAttackHelpers.SpawnBeam(center, extraDir, damage, pierce, range, false, color, sharedBossHitGuard: bossHitGuard);
            }

            if (level >= 5)
            {
                int tickDamage = Mathf.Max(1, damage / 2);
                GameObject glowObj = new GameObject("BellAfterglow");
                LingeringZoneEffect glow = glowObj.AddComponent<LingeringZoneEffect>();
                // 알레그로(쿨타임 감축)는 tickInterval에, 페르마타(지속시간 증가)는 duration에 반영(2026-08-06).
                glow.Initialize(center, radius: 1.0f, tickDamage,
                    tickInterval: 0.3f * CombatTargetingUtility.GetCooldownMultiplier(), duration: 1.5f * CombatTargetingUtility.GetDurationMultiplier(), color);
            }
        }
    }
}
