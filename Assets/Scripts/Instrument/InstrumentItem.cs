using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

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
            itemSprite = ProceduralSpriteFactory.CreateRingWithCore(24, 4f, 9f, Color.white, new Color(1f, 1f, 1f, 0.8f));
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
