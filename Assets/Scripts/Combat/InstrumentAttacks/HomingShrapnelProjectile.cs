using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat.InstrumentAttacks
{
    // 글록켄슈필 Lv5("별빛 낙하 시 주변으로 2차 유도 파편 폭발 분사 & 맞은 적 0.5초 기절") 전용 유도 투사체.
    // 지정된 단일 목표를 향해 직선으로 유도되며, 명중 시 피해 + 기절을 동시에 적용한다.
    public class HomingShrapnelProjectile : MonoBehaviour
    {
        private EnemyMonster target;
        private int damage;
        private float speed;
        private float stunDuration;

        public void Initialize(Vector3 startPos, EnemyMonster target, int damage, float speed, float stunDuration, Sprite sprite, Color color)
        {
            transform.position = startPos;
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            this.stunDuration = stunDuration;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 14;
            transform.localScale = Vector3.one * 0.5f;
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            Vector3 step = toTarget.normalized * speed * Time.deltaTime;

            if (step.magnitude >= dist)
            {
                target.TakeDamage(damage);
                target.ApplyStun(stunDuration);
                Destroy(gameObject);
                return;
            }

            transform.position += step;
        }
    }
}
