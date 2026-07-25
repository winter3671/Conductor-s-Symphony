using UnityEngine;

namespace ConductorSymphony.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class ExpGem : MonoBehaviour
    {
        [SerializeField] private int expValue = 15;
        [SerializeField] private float magnetDistance = 3.5f;
        [SerializeField] private float moveSpeed = 8.0f;

        private Transform playerTransform;
        private SpriteRenderer spriteRenderer;
        private bool isMagnetized = false;

        private static Texture2D gemTexture;
        private static Sprite gemSprite;

        public void Initialize(Vector3 spawnPos, int amount = 15)
        {
            transform.position = spawnPos;
            expValue = amount;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (gemSprite == null) CreateGemSprite();
            spriteRenderer.sprite = gemSprite;
            spriteRenderer.color = new Color(0.3f, 1.0f, 0.4f); // Bright emerald green
            spriteRenderer.sortingOrder = 7;
        }

        private static void CreateGemSprite()
        {
            int size = 16;
            gemTexture = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    px[y * size + x] = (d <= 6f) ? Color.white : Color.clear;
                }
            }
            gemTexture.SetPixels(px);
            gemTexture.Apply();
            gemSprite = Sprite.Create(gemTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= magnetDistance)
            {
                isMagnetized = true;
            }

            if (isMagnetized)
            {
                Vector3 dir = (playerTransform.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;

                if (dist <= 0.4f)
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            if (PlayerExperience.Instance != null)
            {
                PlayerExperience.Instance.AddExp(expValue);
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                Collect();
            }
        }
    }
}
