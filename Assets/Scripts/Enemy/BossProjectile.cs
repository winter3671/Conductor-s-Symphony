using UnityEngine;
using ConductorSymphony.Player;

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
            spriteRenderer.sprite = CreateBossBulletSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 9;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.45f; // Active trigger collider for bullet damage
            col.isTrigger = true;

            Destroy(gameObject, lifetime);
        }

        private static Sprite CreateBossBulletSprite()
        {
            int size = 24;
            Texture2D tex = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 9f && d >= 4f)
                    {
                        px[y * size + x] = Color.red;
                    }
                    else if (d < 4f)
                    {
                        px[y * size + x] = Color.yellow;
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
