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
            spriteRenderer.sprite = defaultSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 12;

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
                case 0: baseOffset = new Vector3(-1.0f,  1.1f, 0f); break; // Top-Left Pet
                case 1: baseOffset = new Vector3( 1.0f,  1.1f, 0f); break; // Top-Right Pet
                case 2: baseOffset = new Vector3(-1.2f, -0.5f, 0f); break; // Bottom-Left Pet
                case 3: baseOffset = new Vector3( 1.2f, -0.5f, 0f); break; // Bottom-Right Pet
                default: baseOffset = new Vector3(0f, 1.2f, 0f); break;
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

        // Deprecated angle method fallback
        public void SetAngle(float angle)
        {
            // Kept for backward compatibility
        }
    }
}
