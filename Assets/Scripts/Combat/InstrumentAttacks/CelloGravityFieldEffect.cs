using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 첼로: 홀드("13칸 베이스 롱노트") 시작 시 그 시점의 가장 가까운 적 발밑에 고정된 중력장을 생성한다.
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

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.level = level;
            this.damage = damage;
            // Lv2+: 범위 +20% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 1.8f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            slowFraction = (level >= 3) ? 0.6f : 0.4f;         // Lv1: 40%, Lv3+: 60%

            EnemyMonster nearest = CombatTargetingUtility.GetNearestEnemy(origin);
            transform.position = nearest != null ? nearest.transform.position : origin;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.45f;
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(28, 13f, faded);
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (radius * 0.9f);
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
