using UnityEngine;
using ConductorSymphony.Enemy;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 프렌치 호른: 홀드("6칸 스웰 롱노트") 중 플레이어 이동 방향 전방 부채꼴 구역에
    // 초음파 충격파를 지속 분사 - 주기적 타격 + 지속 밀쳐냄(Knockback).
    // 기획서 5번(공명 호른 포) 참고. 각도/사거리/넉백 수치는 정성적 설명만 있어 임의로 정했다.
    public class FrenchHornConeEffect : MonoBehaviour, IHoldAttackEffect
    {
        private int damage;
        private Transform playerTransform;

        private float range;
        private float halfAngleDeg;
        private const float TickInterval = 0.2f;
        private float tickTimer;
        private const float KnockbackSpeed = 2.2f;

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.damage = damage;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            range = 3.0f + 0.4f * Mathf.Max(0, level - 1); // 레벨당 사거리 소폭 증가
            halfAngleDeg = (level >= 4) ? 90f : 60f;        // Lv4+: 전방 180도로 확장 (기본 120도)

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Color faded = color;
            faded.a = 0.35f;
            sr.sprite = ProceduralSpriteFactory.CreateFilledCircle(24, 11f, faded); // 정확한 부채꼴 대신 원형으로 범위를 근사 표시(단순화)
            sr.sortingOrder = 3;
            transform.localScale = Vector3.one * (range * 0.8f);

            transform.position = playerTransform != null ? playerTransform.position : origin;
        }

        public void OnHoldTick(float deltaTime)
        {
            if (playerTransform == null) return;

            Vector2 facing = PlayerController.Instance != null ? PlayerController.Instance.GetFacingDirectionVector() : Vector2.down;
            Vector3 forwardOffset = (Vector3)facing * (range * 0.4f);
            transform.position = playerTransform.position + forwardOffset;

            tickTimer += deltaTime;
            if (tickTimer < TickInterval) return;
            tickTimer = 0f;

            Vector3 playerPos = playerTransform.position;
            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - playerPos;
                float dist = toEnemy.magnitude;
                if (dist > range || dist < 0.01f) continue;

                float angle = Vector2.Angle(facing, toEnemy);
                if (angle > halfAngleDeg) continue;

                enemy.TakeDamage(damage);
                Vector3 pushDir = toEnemy.normalized;
                enemy.transform.position += pushDir * KnockbackSpeed * TickInterval;
            }
        }

        public void OnHoldReleased(bool completedFully)
        {
            Destroy(gameObject);
        }
    }
}
