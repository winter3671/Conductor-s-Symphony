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

        // 크레센도(Crescendo) 패시브의 "모든 공격 범위 +10%/Lv" - 예전엔 계산 함수만 있고 소비하는
        // 곳이 없어 죽은 스탯이었다(드럼에서 처음 실제로 연결함, Docs/drum_range_visualization_test_guide.md
        // 참고). 이후 악기들도 각자 반복해서 null 체크하지 않도록 여기 하나로 모은다 - 각 이펙트의
        // "사거리/반경" 필드를 계산하는 지점에서 이 값을 곱해 쓰면 된다.
        public static float GetRangeMultiplier()
        {
            return Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetRangeMultiplier() : 1f;
        }

        // 알레그로(Allegro) 패시브의 "쿨타임 감축" - 크레센도와 같은 이유로 죽어있던 스탯이었다
        // (Docs/allegro_fermata_passive_test_guide.md 참고). tickInterval/bombardInterval처럼
        // "일정 주기로 반복 발동"하는 값에 곱하면 그만큼 더 자주 발동한다. 반환값은 0~1 사이 배율
        // (감축률 자체가 아니라 "얼마나 남았는지")이라 곱하기만 하면 됨 - 예: interval *= 이 값.
        public static float GetCooldownMultiplier()
        {
            return Passive.PassiveStatManager.Instance != null ? 1f - Passive.PassiveStatManager.Instance.GetCooldownReductionFraction() : 1f;
        }

        // 페르마타(Fermata) 패시브의 "지속시간 증가" - 위와 같은 이유로 죽어있던 스탯. 잔류 장판/
        // 필드의 duration 값에 곱하면 더 오래 유지된다. 리듬 노트 자체의 홀드 길이(HoldDurationSeconds)는
        // "얼마나 오래 유지해야 하는가"라 늘어나면 오히려 플레이어에게 불리해지므로 절대 곱하지 않는다.
        public static float GetDurationMultiplier()
        {
            return Passive.PassiveStatManager.Instance != null ? Passive.PassiveStatManager.Instance.GetDurationMultiplier() : 1f;
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
