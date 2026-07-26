using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat
{
    public class AttackProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12.0f;
        [SerializeField] private int damage = 1;

        private Transform targetTransform;
        private BossMonster targetBoss;
        private EnemyMonster targetEnemy;
        private Vector3 targetPos;
        private bool hasTarget = false;
        private SpriteRenderer spriteRenderer;

        public void Initialize(Component target, Vector3 startPos, Sprite sprite, Color color, int damageAmount = 1)
        {
            damage = damageAmount;
            transform.position = startPos;
            hasTarget = true;

            if (target is BossMonster boss)
            {
                targetBoss = boss;
                targetTransform = boss.transform;
            }
            else if (target is EnemyMonster enemy)
            {
                targetEnemy = enemy;
                targetTransform = enemy.transform;
            }

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 15;

            transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);

            if (targetTransform != null)
            {
                targetPos = targetTransform.position;
            }
            else
            {
                targetPos = startPos + Vector3.up * 5f;
            }
        }

        private void Update()
        {
            if (!hasTarget) return;

            if (targetTransform != null)
            {
                targetPos = targetTransform.position;
            }

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Rotate projectile to face flight direction
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }

            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist <= 0.4f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (targetBoss != null)
            {
                targetBoss.TakeDamage(damage);
            }
            else if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
