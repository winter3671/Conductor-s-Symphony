using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 바이올린: 홀드("11칸 롱노트", 2026-08-08부터 - InstrumentPatternDatabase.holdLengthSteps 참고)
    // 중 플레이어 둘레를 회전하는 활(String) 칼날로 지속 타격하고,
    // 릴리즈하는 순간 이동 방향으로 부채꼴 참격(Melodic Arc Slash)을 날린다.
    // 기획서 3번(회전 활 칼날 & 이동 방향 참격) 참고. 레벨별 수치는 밸런스 doc(game_balance_design.docx)
    // 5번 항목을 반영: Lv2 칼날 범위+20%·회전속도 증가 / Lv3 칼날+1개 / Lv4 참격 "크기"+50%(발수 아님) /
    // Lv5 참격이 지난 자리에 2초간 검기 잔향.
    public class ViolinOrbitEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int level;
        private int damage;
        private Color color;
        private Transform playerTransform;

        private int bladeCount;
        private float radius;
        private float spinSpeedDegPerSec;
        private const float BaseSpinSpeedDegPerSec = 260f;
        private const float HitCooldown = 0.35f; // 칼날이 같은 적을 매 프레임 때리지 않도록 하는 히트당 쿨다운

        private readonly List<Transform> blades = new List<Transform>();
        private readonly Dictionary<EnemyMonster, float> hitCooldowns = new Dictionary<EnemyMonster, float>();
        private float currentAngleDeg;

        // 2026-08-08 버그 수정: 아래 TickDamage()가 지금까지 CombatTargetingUtility.GetActiveEnemies()
        // (EnemyMonster)만 순회해서, 회전 칼날이 보스(BossMonster)에게는 전혀 피해를 주지 못하고
        // 있었다(릴리즈 참격은 PiercingBeamProjectile을 통해서라 보스도 정상적으로 맞았음 - 회전 칼날의
        // "지속 틱 피해"만 빠져있던 것). BossMonster는 EnemyMonster가 아니라 위 Dictionary 키로 못 써서
        // 보스 전용 쿨다운을 별도 필드로 둔다(보스는 인스턴스가 항상 최대 1개라 Dictionary가 필요 없음).
        private float bossHitCooldownRemaining;

        private static Sprite bladeSprite;
        private static Sprite slashSprite;
        // 기존 CreateDiamond(16,7f,...) 풀캔버스 bounds(16px/100) - 손그림 아트로 교체해도 화면상 크기를
        // 동일하게 유지하기 위한 기준값(첼로/빔과 동일 패턴).
        private const float BladeReferenceContentSize = 0.16f;
        // 2026-08-08: 위 기준값 그대로면(=예전 절차적 다이아몬드 크기) 회전 반경(1.4~1.68) 대비 너무
        // 작아 "하나도 안 보인다"는 피드백을 받아 순수 시각 배율을 추가. 판정 반경(radius+0.4)과는
        // 무관해서 밸런스에 영향 없음. 4배 시도 시 지름 약 0.64 - 칼날 5개(레가토+Multi 최대)일 때도
        // 인접 칼날 간격(약 1.76)보다 충분히 작아 안 겹침. 더 키우고 싶으면 이 값만 올리면 됨.
        private const float BladeVisualScale = 4f;

        public void Init(int level, int damage, Vector3 origin, Color color, int extraProjectiles)
        {
            this.level = level;
            this.damage = damage;
            this.color = color;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            EnsureSprites();

            // Lv3+: 회전 칼날 1개 추가 + 레가토(Legato) 패시브/악기 Lv4 Multi+1(extraProjectiles)만큼
            // 칼날을 더 추가한다 - 기존 "칼날 개수 늘리기" 스탯과 완전히 같은 파라미터를 공유.
            bladeCount = (level >= 3) ? 3 : 2;
            bladeCount += extraProjectiles;
            // Lv2+: 회전 반경 +20% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 1.4f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            spinSpeedDegPerSec = BaseSpinSpeedDegPerSec * (level >= 2 ? 1.3f : 1f); // Lv2+: 회전 속도 증가

            // 실제 칼날 궤도 반경(radius)을 표시하는 얇은 테두리 링 - 칼날이 실제로 돌면서 어느 정도
            // 범위가 체감되긴 하지만, 칼날이 없는 빈 구간에서는 경계가 안 보인다. 프렌치호른/첼로/
            // 플루트와 동일한 CreateUnitRing 패턴(2026-08-07). 이 컴포넌트의 transform.localScale은
            // 항상 1로 고정(칼날 스프라이트 자체가 각도만 바꿔 배치되는 구조)이라, 부모 스케일 상쇄
            // 없이 radius를 그대로 곱하면 정확한 반경이 나온다.
            GameObject rangeRingObj = new GameObject("ViolinRangeRing");
            rangeRingObj.transform.SetParent(transform, false);
            SpriteRenderer ringSr = rangeRingObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.CreateUnitRing(0.985f, 1f, new Color(color.r, color.g, color.b, 0.8f));
            ringSr.sortingOrder = 4;
            rangeRingObj.transform.localScale = Vector3.one * radius;

            // 손그림 아트(Assets/Resources/Sprites/Effects/Blade/Blade.png)는 콘텐츠 크기가 기존
            // CreateDiamond(16,7f,...)와 전혀 달라서(대략 1682x1718px vs 16x16px), 그대로 뒀으면 100배
            // 넘게 이중 확대됐을 것 - 기존 기준 크기(BladeReferenceContentSize)로 정규화한 뒤
            // BladeVisualScale(4배)만큼 더 키워서 실제로 보이게 한다.
            float bladeMaxDim = Mathf.Max(bladeSprite.bounds.size.x, bladeSprite.bounds.size.y);
            float bladeScale = (bladeMaxDim > 0.0001f) ? (BladeReferenceContentSize * BladeVisualScale / bladeMaxDim) : BladeVisualScale;

            for (int i = 0; i < bladeCount; i++)
            {
                GameObject bladeObj = new GameObject($"ViolinBlade_{i}");
                bladeObj.transform.SetParent(transform);
                SpriteRenderer sr = bladeObj.AddComponent<SpriteRenderer>();
                sr.sprite = bladeSprite;
                sr.color = color;
                sr.sortingOrder = 13;
                bladeObj.transform.localScale = Vector3.one * bladeScale;
                blades.Add(bladeObj.transform);
            }

            transform.position = playerTransform != null ? playerTransform.position : origin;
        }

        public void OnHoldTick(float deltaTime)
        {
            if (playerTransform != null) transform.position = playerTransform.position;

            currentAngleDeg += spinSpeedDegPerSec * deltaTime;

            for (int i = 0; i < blades.Count; i++)
            {
                float angle = currentAngleDeg + (360f / blades.Count) * i;
                Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.right * radius;
                blades[i].position = transform.position + offset;
            }

            TickDamage(deltaTime);
        }

        private void TickDamage(float deltaTime)
        {
            // 히트 쿨다운 갱신. 키 스냅샷을 떠서 순회해야 한다 - foreach로 hitCooldowns를 순회하는 도중
            // 같은 딕셔너리를 인덱서로 갱신하면(만료되지 않은 항목의 남은 시간 갱신) .NET이 "컬렉션이
            // 수정됨(InvalidOperationException)"을 던진다. 실측 검증(phase2_test_result.md)에서 발견된
            // 치명적 버그 - 바이올린으로 적을 한 번이라도 맞히면 다음 틱부터 100% 재현되던 크래시.
            List<EnemyMonster> keys = new List<EnemyMonster>(hitCooldowns.Keys);
            foreach (var key in keys)
            {
                float remaining = hitCooldowns[key] - deltaTime;
                if (remaining <= 0f)
                {
                    hitCooldowns.Remove(key);
                }
                else
                {
                    hitCooldowns[key] = remaining;
                }
            }

            foreach (var enemy in CombatTargetingUtility.GetActiveEnemies())
            {
                if (enemy == null || hitCooldowns.ContainsKey(enemy)) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= radius + 0.4f) // 칼날 두께만큼의 약간의 여유
                {
                    enemy.TakeDamage(damage);
                    // 알레그로(Allegro) 패시브 "쿨타임 감축" 반영 - 재타격 쿨다운이 짧아질수록 같은
                    // 적을 더 자주 다시 때릴 수 있음(2026-08-06, 사용자 결정으로 포함).
                    hitCooldowns[enemy] = HitCooldown * CombatTargetingUtility.GetCooldownMultiplier();
                }
            }

            // 2026-08-08 버그 수정: 보스도 칼날 범위 안에 있으면 동일하게 틱 피해를 받도록.
            if (bossHitCooldownRemaining > 0f)
            {
                bossHitCooldownRemaining -= deltaTime;
            }
            else if (BossMonster.Instance != null)
            {
                float bossDist = Vector3.Distance(transform.position, BossMonster.Instance.transform.position);
                if (bossDist <= radius + 0.4f)
                {
                    BossMonster.Instance.TakeDamage(damage);
                    bossHitCooldownRemaining = HitCooldown * CombatTargetingUtility.GetCooldownMultiplier();
                }
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            Vector2 facing = PlayerController.Instance != null
                ? PlayerController.Instance.GetFacingDirectionVector()
                : Vector2.down;

            const int slashCount = 3; // 밸런스 doc은 발수 변화를 언급하지 않아 고정 - Lv4는 대신 "크기" 증가
            int pierce = 3 + (level >= 3 ? 2 : 0);
            float sizeMultiplier = (level >= 4) ? 1.5f : 1f; // Lv4+: 참격 크기 +50%
            const float spreadDeg = 14f;
            // 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv" 반영.
            float range = 6f * CombatTargetingUtility.GetRangeMultiplier();

            Vector3 origin = transform.position;
            float startAngle = -(slashCount - 1) / 2f * spreadDeg;
            for (int i = 0; i < slashCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, 0f, startAngle + i * spreadDeg) * (Vector3)facing;
                GameObject beamObj = new GameObject("ViolinSlash");
                PiercingBeamProjectile beam = beamObj.AddComponent<PiercingBeamProjectile>();
                beam.Initialize(origin, dir, speed: 16f, damage, pierce, maxRange: range, bounceOnMaxRange: false, slashSprite, color, visualLength: 1.1f, sizeMultiplier: sizeMultiplier);

                if (level >= 5)
                {
                    SpawnAfterglow(origin, dir, range);
                }
            }

            Destroy(gameObject);
        }

        // Lv5: "참격이 지난 자리에 2초간 검기 잔향" - 참격 경로를 따라 몇 개 지점에 잔향 장판을 남긴다.
        private void SpawnAfterglow(Vector3 origin, Vector3 dir, float range)
        {
            const int sampleCount = 3;
            for (int i = 1; i <= sampleCount; i++)
            {
                Vector3 pos = origin + dir.normalized * (range * i / (sampleCount + 1));
                GameObject glowObj = new GameObject("ViolinAfterglow");
                LingeringZoneEffect glow = glowObj.AddComponent<LingeringZoneEffect>();
                // Lv5 잔향도 "참격의 연장선"이라 범위 패시브 적용 대상에 포함하기로 결정함(2026-08-06).
                // 알레그로(쿨타임 감축)는 tickInterval에, 페르마타(지속시간 증가)는 duration에 반영.
                glow.Initialize(pos, radius: 0.6f * CombatTargetingUtility.GetRangeMultiplier(), tickDamage: Mathf.Max(1, damage / 2),
                    tickInterval: 0.4f * CombatTargetingUtility.GetCooldownMultiplier(), duration: 2f * CombatTargetingUtility.GetDurationMultiplier(), color);
            }
        }

        private static void EnsureSprites()
        {
            if (bladeSprite == null)
            {
                // 2026-08-08: 손그림 정지 이미지 1장(Assets/Resources/Sprites/Effects/Blade/Blade.png -
                // 무채색 4방향 별 모양). 기존처럼 무채색 + sr.color 런타임 틴트 방식 그대로라 코드
                // 흐름은 안 바뀜, 크기 정규화만 추가됨(위 BladeReferenceContentSize).
                Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Effects/Blade");
                bladeSprite = (loaded != null && loaded.Length > 0) ? loaded[0] : ProceduralSpriteFactory.CreateDiamond(16, 7f, Color.white);
            }
            if (slashSprite == null) slashSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 7f, Color.white);
        }
    }
}
