using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // ---- 3. 마림바: 이동 방향 일직선 목재 파동 ----
    // 밸런스 doc 5번 항목: Lv2 관통 +2 / Lv3 파동 크기 +30%(오프비트 여부 구분 없이 레벨 조건만으로 적용
    // - 이전 버전엔 이 분기가 아예 빠져있던 버그였음, 이번에 추가) / Lv4 화면 끝 1회 바운스 /
    // Lv5 피격 시 이속 30% 감소 + 밀쳐냄.
    public class MarimbaWaveEffect : ITapAttackEffect
    {
        public void Execute(int level, int damage, int currentCombo, Vector3 origin, Color color)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            int pierce = 3 + (level >= 2 ? 2 : 0);
            bool bounce = level >= 4;
            float sizeMultiplier = (level >= 3) ? 1.3f : 1f; // Lv3+: 파동 크기 +30% (시각 크기 + 히트 반경 모두)

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
        }
    }
}
