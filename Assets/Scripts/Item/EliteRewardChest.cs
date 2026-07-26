using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.UI;

namespace ConductorSymphony.Item
{
    public class EliteRewardChest : MonoBehaviour
    {
        private float bobSpeed = 3.0f;
        private float bobHeight = 0.15f;
        private Vector3 startPos;
        private SpriteRenderer spriteRenderer;

        private void Start()
        {
            startPos = transform.position;
            SetupSprite();
        }

        private void SetupSprite()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateChestSprite();
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 9;

            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.75f; // Expanded pickup radius for player
            collider.isTrigger = true;
        }

        private static Sprite CreateChestSprite()
        {
            int size = 28;
            Texture2D tex = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 10f && d >= 6f)
                    {
                        px[y * size + x] = new Color(1.0f, 0.85f, 0.0f); // Gold Border
                    }
                    else if (d < 6f)
                    {
                        px[y * size + x] = new Color(0.6f, 0.2f, 0.9f); // Purple Royal Core
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
            // Gentle bobbing effect
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (LevelUpUI.Instance != null)
                {
                    LevelUpUI.Instance.ShowEliteRewardSelection();
                }
                Destroy(gameObject);
            }
        }
    }
}
