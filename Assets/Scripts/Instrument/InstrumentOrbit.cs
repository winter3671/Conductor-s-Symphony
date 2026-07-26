using UnityEngine;

namespace ConductorSymphony.Instrument
{
    public class InstrumentOrbit : MonoBehaviour
    {
        public InstrumentType Type { get; private set; }

        private Transform targetTransform;
        private Vector3 baseOffset;
        private int slotIndex;
        private float followSpeed = 6.0f;
        private float bobSpeed = 2.5f;
        private float bobHeight = 0.25f;
        private SpriteRenderer spriteRenderer;

        public void Initialize(InstrumentType type, Transform target, int slot, Sprite defaultSprite, Color color)
        {
            Type = type;
            targetTransform = target;
            slotIndex = slot;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5; // Render behind player (player sortingOrder = 10)

            // Load custom pixel art instrument sprite from Resources/Sprites/Instruments/
            Sprite customSprite = Resources.Load<Sprite>($"Sprites/Instruments/{type}");
            if (customSprite != null)
            {
                spriteRenderer.sprite = customSprite;
                spriteRenderer.color = Color.white; // Preserve original pixel art colors

                // Max Dimension Normalization: Scale sprite so its largest dimension is smaller than conductor's head (0.45 units)
                float maxDim = Mathf.Max(customSprite.bounds.size.x, customSprite.bounds.size.y);
                if (maxDim > 0.001f)
                {
                    float targetMaxDimension = 0.45f; // Smaller than conductor's head
                    float scale = targetMaxDimension / maxDim;
                    transform.localScale = new Vector3(scale, scale, 1.0f);
                }
            }
            else
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = color;
            }

            SetSlotIndex(slot);

            if (targetTransform != null)
            {
                transform.position = targetTransform.position + baseOffset;
            }
        }

        public void SetSlotIndex(int slot)
        {
            slotIndex = slot;
            // Arrange companion positions around player like pets (Top-Left, Top-Right, Bottom-Left, Bottom-Right)
            switch (slotIndex)
            {
                case 0: baseOffset = new Vector3(-0.98f,  0.83f, 0f); break; // Top-Left Pet (Q)
                case 1: baseOffset = new Vector3( 0.98f,  0.83f, 0f); break; // Top-Right Pet (R)
                case 2: baseOffset = new Vector3(-1.13f, -0.38f, 0f); break; // Bottom-Left Pet (W)
                case 3: baseOffset = new Vector3( 1.13f, -0.38f, 0f); break; // Bottom-Right Pet (E)
                default: baseOffset = new Vector3(0f, 0.9f, 0f); break;
            }
        }

        private void Update()
        {
            if (targetTransform == null) return;

            // Gentle pet bobbing up and down
            float floatOffsetY = Mathf.Sin(Time.time * bobSpeed + slotIndex * 1.5f) * bobHeight;
            Vector3 desiredPos = targetTransform.position + baseOffset + new Vector3(0f, floatOffsetY, 0f);

            // Smooth pet follow movement (Lerp)
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        }

        public void SetAngle(float angle)
        {
            // Backward compatibility
        }
    }
}
