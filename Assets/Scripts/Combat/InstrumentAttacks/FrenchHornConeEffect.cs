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

        public void Init(int level, int damage, Vector3 origin, Color color)
        {
            this.damage = damage;
            playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

            range = 3.0f * (level >= 2 ? 1.25f : 1f);                    // Lv2+: 사거리 +25%
            halfAngleDeg = (level >= 5) ? 90f : 60f;                     // Lv5+: 전방 180도로 확장 (기본 120도)
            knockbackSpeed = BaseKnockbackSpeed * (level >= 3 ? 1.4f : 1f); // Lv3+: 넉백 거리 +40%
            applyDamageAmp = level >= 4;                                 // Lv4+: 범위 내 적 피해량 증폭 디버프

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
