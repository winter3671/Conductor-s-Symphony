using UnityEngine;
using ConductorSymphony.Player;

namespace ConductorSymphony.Instrument
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class InstrumentItem : MonoBehaviour
    {
        public InstrumentType Type { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Vector3 startPos;
        private float bobSpeed = 3.0f;
        private float bobHeight = 0.2f;

        private static Texture2D itemTexture;
        private static Sprite itemSprite;

        public void Initialize(InstrumentType type, Vector3 spawnPos)
        {
            Type = type;
            transform.position = spawnPos;
            startPos = spawnPos;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (itemSprite == null) CreateItemSprite();
            spriteRenderer.sprite = itemSprite;

            InstrumentDefinition def = InstrumentPatternDatabase.GetDefinition(type);
            spriteRenderer.color = def.themeColor;
            spriteRenderer.sortingOrder = 8;
        }

        private static void CreateItemSprite()
        {
            int size = 24;
            itemTexture = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 9f && d >= 4f)
                    {
                        px[y * size + x] = Color.white;
                    }
                    else if (d < 4f)
                    {
                        px[y * size + x] = new Color(1f, 1f, 1f, 0.8f);
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }
            itemTexture.SetPixels(px);
            itemTexture.Apply();
            itemSprite = Sprite.Create(itemTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (InstrumentManager.Instance != null)
                {
                    bool acquired = InstrumentManager.Instance.AcquireOrUpgradeInstrument(Type);
                    if (acquired)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
