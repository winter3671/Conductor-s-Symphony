using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 프렌치 호른: 홀드("6칸 스웰 롱노트") 중 플레이어 이동 방향 전방 부채꼴 구역에
    // 초음파 충격파를 지속 분사 - 주기적 타격 + 지속 밀쳐냄(Knockback).
    // 기획서 5번(공명 호른 포) 참고. 레벨별 수치는 밸런스 doc(game_balance_design.docx) 5번 항목 반영:
    // Lv2 사거리+25% / Lv3 넉백거리+40% / Lv4 범위 내 적 피해량+15% 증폭 디버프 / Lv5 각도 120°→180° 확장
    // (이전 버전엔 각도 확장이 Lv4에서 일어났는데, doc 기준 Lv5로 정정).
    public class FrenchHornConeEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int damage;
        private Transform playerTransform;

        private float range;
        private float halfAngleDeg;
        private float knockbackSpeed;
        private bool applyDamageAmp;
        private const float TickInterval = 0.2f;
        private float tickTimer;
        private const float BaseKnockbackSpeed = 2.2f;
        private const float DamageAmpMultiplier = 1.15f; // Lv4: 피해량 +15%

        private readonly HashSet<EnemyMonster> ampedEnemies = new HashSet<EnemyMonster>();

        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 지속 부채꼴 판정이라 "낱개로 셀 수 있는
        // 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함).
        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.damage = damage;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            // Lv2+: 사거리 +25% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            range = 3.0f * (level >= 2 ? 1.25f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            halfAngleDeg = (level >= 5) ? 90f : 60f;                     // Lv5+: 전방 180도로 확장 (기본 120도, 각도는 범위 패시브 대상 아님)
            knockbackSpeed = BaseKnockbackSpeed * (level >= 3 ? 1.4f : 1f); // Lv3+: 넉백 거리 +40%
            applyDamageAmp = level >= 4;                                 // Lv4+: 범위 내 적 피해량 증폭 디버프

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.35f;
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(24, 11f, faded); // 정확한 부채꼴 대신 원형으로 범위를 근사 표시(단순화)
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (range * 0.8f);

            // 실제 사거리(range)를 정확히 표시하는 얇은 테두리 링 - 위 채워진 원은 부채꼴을 원으로
            // 근사한 장식이라 반경이 부정확하다(CreateFilledCircle의 픽셀 반경/텍스처 크기 조합이
            // localScale 계산과 안 맞아 실제로는 range의 약 8.8%로만 그려짐). 자식 오브젝트로 만들어
            // 부모(이 transform)의 스케일(range*0.8)을 상쇄하는 로컬 스케일(1/0.8=1.25)을 곱하면
            // 최종 월드 반경이 정확히 range가 된다. 드럼 오라 링과 동일하게 아주 얇게(0.985~1.0) 설정
            // (2026-08-07, 사용자 결정 - 채워진 원은 유지하고 얇은 테두리만 추가).
            GameObject rangeRingObj = new GameObject("FrenchHornRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * (1f / 0.8f);

            transform.position = playerTransform != null ? playerTransform.position : origin;
        }

        public void OnHoldTick(float deltaTime)
        {
            if (playerTransform == null) return;

            Vector2 facing = PlayerController.Instance != null ? PlayerController.Instance.GetFacingDirectionVector() : Vector2.down;
            Vector3 forwardOffset = (Vector3)facing * (range * 0.4f);
            transform.position = playerTransform.position + forwardOffset;

            // 알레그로(Allegro) 패시브 "쿨타임 감축" 반영 - 값이 작을수록(배율<1) 더 자주 틱.
            tickTimer += deltaTime;
            if (tickTimer < TickInterval * CombatTargetingUtility.GetCooldownMultiplier()) return;
            tickTimer = 0f;

            Vector3 playerPos = playerTransform.position;
            HashSet<EnemyMonster> currentlyInCone = new HashSet<EnemyMonster>();

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - playerPos;
                float dist = toEnemy.magnitude;
                if (dist > range || dist < 0.01f) continue;

                float angle = Vector2.Angle(facing, toEnemy);
                if (angle > halfAngleDeg) continue;

                if (applyDamageAmp)
                {
                    currentlyInCone.Add(enemy);
                    if (!ampedEnemies.Contains(enemy))
                    {
                        enemy.SetDamageAmpMultiplier(DamageAmpMultiplier);
                    }
                }

                enemy.TakeDamage(damage);
                Vector3 pushDir = toEnemy.normalized;
                enemy.transform.position += pushDir * knockbackSpeed * TickInterval;
            }

            if (applyDamageAmp)
            {
                // 부채꼴을 벗어난 적은 증폭 디버프 해제
                foreach (var enemy in ampedEnemies)
                {
                    if (enemy != null && !currentlyInCone.Contains(enemy))
                    {
                        enemy.SetDamageAmpMultiplier(1f);
                    }
                }
                ampedEnemies.Clear();
                foreach (var e in currentlyInCone) ampedEnemies.Add(e);
            }

            // 2026-08-08 버그 수정: 위 루프가 CombatTargetingUtility.GetActiveEnemies()(EnemyMonster)만
            // 순회해서 보스는 부채꼴 안에 있어도 전혀 피해를 못 받고 있었다. 보스는 SetDamageAmpMultiplier/
            // 넉백용 EnemyMonster 전용 API가 없으므로(다른 곳의 보스 처리와 동일한 관례 - AreaImpactEffect,
            // TimpaniBombardmentEffect 등도 보스에게는 부가 효과 없이 피해만 적용) 피해만 적용한다.
            if (BossMonster.Instance != null)
            {
                Vector3 toBoss = BossMonster.Instance.transform.position - playerPos;
                float bossDist = toBoss.magnitude;
                if (bossDist <= range && bossDist > 0.01f && Vector2.Angle(facing, toBoss) <= halfAngleDeg)
                {
                    BossMonster.Instance.TakeDamage(damage);
                }
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            foreach (var enemy in ampedEnemies)
            {
                if (enemy != null) enemy.SetDamageAmpMultiplier(1f);
            }
            ampedEnemies.Clear();
            Destroy(gameObject);
        }
    }
}
