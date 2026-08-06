using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 바이올린: 홀드("13칸 롱노트") 중 플레이어 둘레를 회전하는 활(String) 칼날로 지속 타격하고,
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

        private static Sprite bladeSprite;
        private static Sprite slashSprite;

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.level = level;
            this.damage = damage;
            this.color = color;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            EnsureSprites();

            bladeCount = (level >= 3) ? 3 : 2;                             // Lv3+: 회전 칼날 1개 추가
            // Lv2+: 회전 반경 +20% × 크레센도(Crescendo) 패시브 "모든 공격 범위 +10%/Lv"
            radius = 1.4f * (level >= 2 ? 1.2f : 1f) * CombatTargetingUtility.GetRangeMultiplier();
            spinSpeedDegPerSec = BaseSpinSpeedDegPerSec * (level >= 2 ? 1.3f : 1f); // Lv2+: 회전 속도 증가

            for (int i = 0; i < bladeCount; i++)
            {
                GameObject bladeObj = new GameObject($"ViolinBlade_{i}");
                bladeObj.transform.SetParent(transform);
                SpriteRenderer sr = bladeObj.AddComponent<SpriteRenderer>();
                sr.sprite = bladeSprite;
                sr.color = color;
                sr.sortingOrder = 13;
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
                    hitCooldowns[enemy] = HitCooldown;
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
                glow.Initialize(pos, radius: 0.6f * CombatTargetingUtility.GetRangeMultiplier(), tickDamage: Mathf.Max(1, damage / 2), tickInterval: 0.4f, duration: 2f, color);
            }
        }

        private static void EnsureSprites()
        {
            if (bladeSprite == null) bladeSprite = ProceduralSpriteFactory.CreateDiamond(16, 7f, Color.white);
            if (slashSprite == null) slashSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 7f, Color.white);
        }
    }
}
