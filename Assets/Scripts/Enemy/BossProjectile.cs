using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Enemy
{
    public class BossProjectile : MonoBehaviour
    {
        private Vector3 moveDirection;
        private float speed = 5.0f;
        private int damage = 10;
        private float lifetime = 6.0f;

        private SpriteRenderer spriteRenderer;

        public void Initialize(Vector3 direction, float speed, Color color, int damage = 10)
        {
            this.moveDirection = direction.normalized;
            this.speed = speed;
            this.damage = damage;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ProceduralSpriteFactory.CreateRingWithCore(24, 4f, 9f, Color.red, Color.yellow);
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 9;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.45f; // Active trigger collider for bullet damage
            col.isTrigger = true;

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckHitPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckHitPlayer(other);
        }

        private void CheckHitPlayer(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
