using UnityEngine;
using ConductorSymphony.Enemy;

namespace ConductorSymphony.Combat
{
    public class AttackProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12.0f;
        [SerializeField] private int damage = 1;

        private EnemyMonster targetEnemy;
        private Vector3 targetPos;
        private bool hasTarget = false;
        private SpriteRenderer spriteRenderer;

        public void Initialize(EnemyMonster enemy, Vector3 startPos, Sprite sprite, Color color, int damageAmount = 1)
        {
            targetEnemy = enemy;
            damage = damageAmount;
            transform.position = startPos;
            hasTarget = true;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 15;

            if (targetEnemy != null)
            {
                targetPos = targetEnemy.transform.position;
            }
            else
            {
                targetPos = startPos + Vector3.up * 5f;
            }
        }

        private void Update()
        {
            if (!hasTarget) return;

            if (targetEnemy != null)
            {
                targetPos = targetEnemy.transform.position;
            }

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist <= 0.3f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
