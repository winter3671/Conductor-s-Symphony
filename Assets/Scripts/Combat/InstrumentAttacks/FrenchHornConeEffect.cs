using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Instrument;
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

        // 2026-08-08: 손그림 정지 이미지 1장(Assets/Resources/Sprites/Effects/HornCone/HornCone.png -
        // 뾰족한 끝(플레이어 위치)에서 넓게 퍼지는 실제 부채꼴 모양). 기존엔 원으로 근사 표시하면서
        // 회전 없이 "부모 위치를 range*0.4만큼 앞으로 미리 밀어두는" 트릭으로 방향성을 흉내냈는데, 진짜
        // 방향성 있는 아트로 바뀌면서 그 트릭은 더 이상 안 맞다(원처럼 대칭이 아니라서 밀어두면 아트
        // 자체가 어긋나 보임) - 이번에 제거하고 실제 회전으로 교체한다. 에셋 로딩 실패 시(폴백)에는
        // 기존 원+포워드오프셋 방식을 100% 그대로 유지해 회귀 없게 한다.
        private static Sprite coneSprite;
        private static bool triedLoadConeSprite = false;
        private SpriteRenderer fieldSr;
        private Transform fieldArtTransform;
        private bool usingRealArt;

        private const float ReferenceContentSize = 0.24f; // 기존 CreateFilledCircle(24,11f,...) 풀캔버스 bounds(24px/100)

        // extraProjectiles(레가토/Multi+1)는 사용하지 않는다 - 지속 부채꼴 판정이라 "낱개로 셀 수 있는
        // 투사체" 개념이 없음(2026-08-07, 사용자 결정으로 4종 제외 대상에 포함).
        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.damage = damage;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            // 2026-08-09: 레벨별 배율/수치를 InstrumentLevelStats로 데이터화(순수 추출, 값 변경 없음).
            // Lv2+: 사거리 +25% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            range = 3.0f * InstrumentLevelStats.GetRangeMultiplier(InstrumentType.FrenchHorn, level) * CombatTargetingUtility.GetRangeMultiplier();
            halfAngleDeg = InstrumentLevelStats.FrenchHornHalfAngleDeg[InstrumentLevelStats.Idx(level)]; // Lv5+: 전방 180도로 확장 (기본 120도, 각도는 범위 패시브 대상 아님)
            knockbackSpeed = BaseKnockbackSpeed * InstrumentLevelStats.GetKnockbackMultiplier(InstrumentType.FrenchHorn, level); // Lv3+: 넉백 거리 +40%
            applyDamageAmp = level >= 4;                                 // Lv4+: 범위 내 적 피해량 증폭 디버프

            EnsureConeSprite();
            usingRealArt = coneSprite != null;

            GameObject fieldArtObj = new GameObject("FrenchHornConeArt");
            fieldArtObj.transform.SetParent(transform, false);
            fieldArtTransform = fieldArtObj.transform;
            fieldSr = fieldArtObj.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.35f;
            fieldSr.sortingOrder = 3;

            if (usingRealArt)
            {
                // 아트 자체가 이미 브라스 골드 톤으로 그려져 있어 틴트 없이 알파만 곱한다.
                fieldSr.color = new Color(1f, 1f, 1f, 0.6f);
                ApplyConeArt(coneSprite);
            }
            else
            {
                // 폴백: 기존 원 근사 방식 그대로(회귀 없음).
                fieldSr.color = faded;
                fieldSr.sprite = ProceduralSpriteFactory.CreateFilledCircle(24, 11f, Color.white);
                float maxDim = 0.24f;
                fieldArtTransform.localScale = Vector3.one * (range * 0.8f * ReferenceContentSize / maxDim);
            }

            // 실제 사거리(range)를 정확히 표시하는 얇은 테두리 링. 드럼 오라 링과 동일하게 아주
            // 얇게(0.985~1.0) 설정(2026-08-07, 사용자 결정).
            GameObject rangeRingObj = new GameObject("FrenchHornRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            ringSr.sortingOrder = 4;
            // 부채꼴이라 실제 판정은 원의 일부(각도 안쪽)뿐이지만, "최대 사거리가 어디까지인지"는
            // 여전히 유효한 정보라 기존처럼 계속 표시한다(2026-08-07 결정 유지, 변경 없음).
            rangeRingObj.transform.localScale = Vector3.one * range;

            transform.position = playerTransform != null ? playerTransform.position : origin;
        }

        private static void EnsureConeSprite()
        {
            if (triedLoadConeSprite) return;
            triedLoadConeSprite = true;

            Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/HornCone");
            if (loaded != null && loaded.Length > 0)
            {
                coneSprite = loaded[0]; // 정지 이미지 1장만 사용
            }
        }

        // 아트의 뾰족한 끝(플레이어 위치)이 이 오브젝트 로컬 원점에 오도록 위치를 보정하고, 콘텐츠
        // 가로폭(끝~밑변)이 정확히 range가 되도록 균등 스케일한다. 부모(this.transform)가 위치/회전을
        // 맡으므로 자식은 로컬 오프셋만 가지면 회전에 자동으로 딸려온다.
        private void ApplyConeArt(Sprite sprite)
        {
            Bounds b = sprite.bounds;
            float contentWidth = b.size.x;
            float scale = (contentWidth > 0.0001f) ? (range / contentWidth) : 1f;
            fieldSr.sprite = sprite;
            fieldArtTransform.localScale = Vector3.one * scale;
            fieldArtTransform.localPosition = new Vector3(-b.min.x * scale, -b.center.y * scale, 0f);
        }

        public void OnHoldTick(float deltaTime)
        {
            if (playerTransform == null) return;

            Vector2 facing = PlayerController.Instance != null ? PlayerController.Instance.GetFacingDirectionVector() : Vector2.down;

            if (usingRealArt)
            {
                // 실제 방향성 있는 아트: 플레이어 위치에 고정하고 바라보는 방향으로 회전만 시킨다.
                transform.position = playerTransform.position;
                float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                // 폴백: 대칭 원이라 회전은 의미 없고, 기존처럼 앞으로 미리 밀어두는 트릭 유지(회귀 없음).
                Vector3 forwardOffset = (Vector3)facing * (range * 0.4f);
                transform.position = playerTransform.position + forwardOffset;
            }

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
