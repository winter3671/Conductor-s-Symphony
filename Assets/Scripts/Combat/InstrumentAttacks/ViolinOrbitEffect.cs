using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 바이올린: 홀드("13칸 롱노트") 중 플레이어 둘레를 회전하는 활(String) 칼날로 지속 타격하고,
    // 릴리즈하는 순간 이동 방향으로 부채꼴 참격(Melodic Arc Slash)을 날린다.
    // 기획서 3번(회전 활 칼날 & 이동 방향 참격) 참고. 정성적 설명만 있는 수치(칼날 수/반경/참격 발수 등)는
    // 1단계(피아노/벨/마림바/글록켄슈필)와 같은 관례로 임의로 정했다 - 플레이테스트 후 조정 필요.
    public class ViolinOrbitEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int level;
        private int damage;
        private Color color;
        private Transform playerTransform;

        private int bladeCount;
        private float radius;
        private const float SpinSpeedDegPerSec = 260f;
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

            bladeCount = (level >= 3) ? 3 : 2;               // Lv3+: 회전 칼날 1개 추가
            radius = 1.4f + 0.15f * Mathf.Max(0, level - 1); // 레벨당 회전 반경 소폭 증가

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

            currentAngleDeg += SpinSpeedDegPerSec * deltaTime;

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

            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
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

            int slashCount = (level >= 4) ? 5 : 3; // Lv4+: 부채꼴 참격 2발 추가
            int pierce = 3 + (level >= 3 ? 2 : 0);
            const float spreadDeg = 14f;

            Vector3 origin = transform.position;
            float startAngle = -(slashCount - 1) / 2f * spreadDeg;
            for (int i = 0; i < slashCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, 0f, startAngle + i * spreadDeg) * (Vector3)facing;
                GameObject beamObj = new GameObject("ViolinSlash");
                PiercingBeamProjectile beam = beamObj.AddComponent<PiercingBeamProjectile>();
                beam.Initialize(origin, dir, speed: 16f, damage, pierce, maxRange: 6f, bounceOnMaxRange: false, slashSprite, color);
            }

            Destroy(gameObject);
        }

        private static void EnsureSprites()
        {
            if (bladeSprite == null) bladeSprite = ProceduralSpriteFactory.CreateDiamond(16, 7f, Color.white);
            if (slashSprite == null) slashSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 7f, Color.white);
        }
    }
}
