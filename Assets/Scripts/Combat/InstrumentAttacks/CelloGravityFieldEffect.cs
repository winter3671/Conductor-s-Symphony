using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 첼로: 홀드("11칸 베이스 롱노트", 2026-08-08부터 - InstrumentPatternDatabase.holdLengthSteps 참고)
    // 시작 시 그 시점의 가장 가까운 적 발밑에 고정된 중력장을 생성한다.
    // 필드는 캐스팅 위치에 고정되며 적을 추적하지 않는다(기획서 "고정된 중력장" 문구 그대로 반영).
    // 범위 내 적의 이동 속도를 감소시키고 주기적으로 타격. 기획서 7번(중력의 구속) 참고.
    // 레벨별 수치는 밸런스 doc(game_balance_design.docx) 5번 항목 반영: Lv1 이속감소 40%(기존 50%에서 정정) /
    // Lv2 범위+20% / Lv3 감소 40%→60% / Lv4 필드 잔류시간+30%(홀드 종료 후에도 잠시 유지) /
    // Lv5 중앙으로 지속 끌어당김 기믹 추가.
    public class CelloGravityFieldEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int level;
        private int damage;
        private float radius;
        private float slowFraction;
        private const float TickInterval = 0.4f;
        private float tickTimer;

        // Lv4: 홀드가 끝난 뒤에도 필드가 잠시 더 유지되는 "잔류시간". HoldEffectCoordinator는 릴리즈 즉시
        // 이 컴포넌트를 더 이상 추적하지 않으므로(OnHoldTick이 더 안 불림), 잔류 동안은 자체 Update()로
        // 계속 틱을 굴린다.
        private const float BaseLingerDuration = 1.0f;
        private bool isLingering;
        private float lingerTimer;

        // Lv5: 중앙으로 지속 끌어당김
        private const float PullStrength = 1.5f;

        private readonly HashSet<EnemyMonster> affectedEnemies = new HashSet<EnemyMonster>();

        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 고정 위치 필드 판정이라 "낱개로 셀 수
        // 있는 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함).
        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.level = level;
            this.damage = damage;
            // Lv2+: 범위 +20% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 1.8f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            slowFraction = (level >= 3) ? 0.6f : 0.4f;         // Lv1: 40%, Lv3+: 60%

            // 2026-08-08 버그 수정: 잡몹 없이 보스만 남았을 때도 필드가 보스 발밑에 생성되도록
            // GetNearestTargetPosition으로 교체(기존엔 origin=플레이어 위치에 생성되던 버그).
            transform.position = CombatTargetingUtility.GetNearestTargetPosition(origin, origin);

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.45f;
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(28, 13f, faded);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);

            // 실제 판정 반경(radius)을 정확히 표시하는 얇은 테두리 링 - 채워진 원은 근사치라
            // CreateFilledCircle의 픽셀 반경/텍스처 크기 조합상 실제로는 radius의 약 11.7%로만
            // 그려진다. 부모 스케일(radius*0.9)을 상쇄하는 로컬 스케일(1/0.9)을 곱해 정확히 맞춘다.
            // 드럼 오라 링과 동일하게 아주 얇게(0.985~1.0) 설정(2026-08-07, 사용자 결정).
            GameObject rangeRingObj = new GameObject("CelloRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * (1f / 0.9f);
        }

        public void OnHoldTick(float deltaTime)
        {
            TickFieldLogic(deltaTime);
        }

        // 홀드 중(OnHoldTick)과 릴리즈 후 잔류 기간(Update) 양쪽에서 공유하는 실제 필드 로직.
        private void TickFieldLogic(float deltaTime)
        {
            HashSet<EnemyMonster> currentlyInRange = new HashSet<EnemyMonster>();
            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= radius)
                {
                    currentlyInRange.Add(enemy);
                    if (!affectedEnemies.Contains(enemy))
                    {
                        enemy.SetSpeedMultiplier(1f - slowFraction);
                    }

                    // Lv5: 범위 안의 적을 중앙으로 서서히 끌어당김
                    if (level >= 5 && dist > 0.1f)
                    {
                        Vector3 toCenter = (transform.position - enemy.transform.position).normalized;
                        enemy.transform.position += toCenter * PullStrength * deltaTime;
                    }
                }
            }

            // 필드를 벗어난 적은 감속을 해제해야 원래 속도로 되돌아온다.
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null && !currentlyInRange.Contains(enemy))
                {
                    enemy.SetSpeedMultiplier(1f);
                }
            }

            affectedEnemies.Clear();
            foreach (var e in currentlyInRange) affectedEnemies.Add(e);

            // 알레그로(Allegro) 패시브 "쿨타임 감축" 반영 - 값이 작을수록(배율<1) 더 자주 틱.
            tickTimer += deltaTime;
            if (tickTimer < TickInterval * CombatTargetingUtility.GetCooldownMultiplier()) return;
            tickTimer = 0f;

            foreach (var enemy in currentlyInRange)
            {
                if (enemy != null) enemy.TakeDamage(damage);
            }

            // 2026-08-08 버그 수정: 위 로직 전체가 CombatTargetingUtility.GetActiveEnemies()(EnemyMonster)만
            // 다뤄서 보스는 중력장 범위 안에 있어도 감속/끌어당김/틱 피해를 전혀 못 받고 있었다. 감속
            // (SetSpeedMultiplier)·끌어당김은 EnemyMonster 전용 API라 그대로 두고(다른 곳의 보스 처리와
            // 동일한 관례 - 보스는 부가 효과 없이 피해만 적용), 틱 피해만 동일한 주기로 함께 적용한다.
            if (BossMonster.Instance != null)
            {
                float bossDist = Vector3.Distance(transform.position, BossMonster.Instance.transform.position);
                if (bossDist <= radius)
                {
                    BossMonster.Instance.TakeDamage(damage);
                }
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            // Lv4: 즉시 파괴하지 않고 잔류시간(+30%) 동안 필드를 유지한다. 자체 Update()가 이어받는다.
            // 페르마타(Fermata) 패시브 "지속시간 증가"도 함께 반영.
            isLingering = true;
            lingerTimer = BaseLingerDuration * (level >= 4 ? 1.3f : 1f) * CombatTargetingUtility.GetDurationMultiplier();
        }

        private void Update()
        {
            if (!isLingering) return;

            lingerTimer -= Time.deltaTime;
            if (lingerTimer <= 0f)
            {
                foreach (var enemy in affectedEnemies)
                {
                    if (enemy != null) enemy.SetSpeedMultiplier(1f);
                }
                affectedEnemies.Clear();
                Destroy(gameObject);
                return;
            }

            TickFieldLogic(Time.deltaTime);
        }
    }
}
