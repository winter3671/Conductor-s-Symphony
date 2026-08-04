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

                // Max Dimension Normalization: Scale companion sprite size to 0.68 units (1.5x larger)
                float maxDim = Mathf.Max(customSprite.bounds.size.x, customSprite.bounds.size.y);
                if (maxDim > 0.001f)
                {
                    float targetMaxDimension = 0.68f; // 1.5x scale increase for beautiful pixel visibility
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
            // Align companion pet orbit locations 1:1 with QWER key directions
            switch (slotIndex)
            {
                case 0: baseOffset = new Vector3(-1.25f,  0.00f, 0f); break; // 1st Instrument: Q key (Left)
                case 1: baseOffset = new Vector3( 1.25f,  0.00f, 0f); break; // 2nd Instrument: R key (Right)
                case 2: baseOffset = new Vector3(-0.90f,  0.90f, 0f); break; // 3rd Instrument: W key (Up-Left)
                case 3: baseOffset = new Vector3( 0.90f,  0.90f, 0f); break; // 4th Instrument: E key (Up-Right)
                default: baseOffset = new Vector3(-1.25f, 0.00f, 0f); break;
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
    }
}
