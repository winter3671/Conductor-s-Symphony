using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 3. 마림바: 이동 방향 일직선 목재 파동 ----
    // 밸런스 doc 5번 항목: Lv2 관통 +2 / Lv3 파동 크기 +30%(오프비트 여부 구분 없이 레벨 조건만으로 적용
    // - 이전 버전엔 이 분기가 아예 빠져있던 버그였음, 이번에 추가) / Lv4 화면 끝 1회 바운스 /
    // Lv5 피격 시 이속 30% 감소 + 밀쳐냄.
    public class MarimbaWaveEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color, int extraProjectiles)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            // 2026-08-09: 레벨별 배율/수치를 InstrumentLevelStats로 데이터화(순수 추출, 값 변경 없음).
            int pierce = InstrumentLevelStats.GetPierceCount(InstrumentType.Marimba, level);
            bool bounce = level >= 4;
            float sizeMultiplier = InstrumentLevelStats.GetSizeMultiplier(InstrumentType.Marimba, level); // Lv3+: 파동 크기 +30% (시각 크기 + 히트 반경 모두)

            System.Action<EnemyMonster, Vector3> onHit = null;
            if (level >= 5)
            {
                onHit = (enemy, hitPos) =>
                {
                    enemy.ApplyTemporarySlow(0.7f, 1.0f); // 이속 30% 감소, 1초간 (지속시간은 doc에 수치 없어 임의값)
                    Vector3 push = enemy.transform.position - origin;
                    if (push.sqrMagnitude > 0.0001f)
                    {
                        enemy.transform.position += push.normalized * 0.5f; // 밀쳐냄
                    }
                };
            }

            // 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv" 반영.
            float maxRange = 10f * CombatTargetingUtility.GetRangeMultiplier();
            TapAttackHelpers.SpawnBeam(origin, facing, damage, pierce, maxRange, bounce, color, sizeMultiplier, onHit);

            // 레가토(Legato) 패시브/악기 Lv4 Multi+1(extraProjectiles): 원본 파동과 평행하게 좌우로
            // 갈라지는 추가 파동으로 구현(샷건처럼 옆으로 퍼짐). 이동 방향에 수직인 벡터로 오프셋.
            if (extraProjectiles > 0)
            {
                Vector2 perp = new Vector2(-facing.y, facing.x);
                for (int e = 0; e < extraProjectiles; e++)
                {
                    float side = (e % 2 == 0) ? 1f : -1f;
                    Vector3 offsetOrigin = origin + (Vector3)(perp * side * 0.6f);
                    TapAttackHelpers.SpawnBeam(offsetOrigin, facing, damage, pierce, maxRange, bounce, color, sizeMultiplier, onHit);
                }
            }
        }
    }
}
