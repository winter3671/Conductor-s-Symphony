using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 짧은 지연 후 특정 지점에 원형 범위 피해를 1회 입히는 공용 이펙트.
    // 글록켄슈필(별빛 낙하)이 사용한다 - "낙하 시간"을 표현하기 위해 작게 시작했다가 착탄 시 커진다.
    public class AreaImpactEffect : MonoBehaviour
    {
        private float remainingDelay;
        private float initialDelay;
        private float radius;
        private int damage;
        private bool hasImpacted = false;
        private SpriteRenderer spriteRenderer;

        // 팀파니 Lv4(1초 기절) 등 - 착탄으로 맞은 개별 적에 대한 추가 처리를 호출자에게 위임.
        private System.Action<EnemyMonster> onHitEnemy;
        // 팀파니 Lv5(착탄 지점 3초 지진지대 잔류) 등 - 실제로 착탄이 발생한(딜레이가 끝난) 정확한 시점/위치를 알려준다.
        private System.Action<Vector3> onImpact;

        public void Initialize(Vector3 pos, float delaySeconds, float impactRadius, int damageAmount, Sprite sprite, Color color, System.Action<EnemyMonster> onHitEnemy = null, System.Action<Vector3> onImpact = null)
        {
            transform.position = pos;
            remainingDelay = Mathf.Max(0.01f, delaySeconds);
            initialDelay = remainingDelay;
            radius = impactRadius;
            damage = damageAmount;
            this.onHitEnemy = onHitEnemy;
            this.onImpact = onImpact;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 14;
            transform.localScale = Vector3.one * 0.35f; // 낙하 중: 작은 예고 표시
        }

        private void Update()
        {
            if (hasImpacted) return;

            remainingDelay -= Time.deltaTime;

            // 낙하 예고 표시가 착탄 시점까지 점점 커지도록
            float t = 1f - Mathf.Clamp01(remainingDelay / initialDelay);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.35f, Vector3.one * (radius * 1.6f), t);

            if (remainingDelay <= 0f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            hasImpacted = true;

            EnemyMonster[] enemies = Object.FindObjectsByType<EnemyMonster>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(transform.position, enemy.transform.position) <= radius)
                {
                    enemy.TakeDamage(damage);
                    onHitEnemy?.Invoke(enemy);
                }
            }

            if (BossMonster.Instance != null && Vector3.Distance(transform.position, BossMonster.Instance.transform.position) <= radius)
            {
                BossMonster.Instance.TakeDamage(damage);
            }

            onImpact?.Invoke(transform.position);

            Destroy(gameObject, 0.12f); // 착탄 플래시가 잠깐 보이도록 약간의 딜레이 후 정리
        }
    }
}
