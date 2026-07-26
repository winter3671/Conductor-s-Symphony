using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Item
{
    public class EliteRewardChest : MonoBehaviour
    {
        public static event System.Action OnEliteChestCollectedEvent;

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
            spriteRenderer.sprite = ProceduralSpriteFactory.CreateRingWithCore(28, 6f, 10f, new Color(1.0f, 0.85f, 0.0f), new Color(0.6f, 0.2f, 0.9f));
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 9;

            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.75f; // Expanded pickup radius for player
            collider.isTrigger = true;
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
                OnEliteChestCollectedEvent?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
