using UnityEngine;
using ConductorSymphony.Passive;
using ConductorSymphony.Utility;

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
            gemSprite = ProceduralSpriteFactory.CreateFilledCircle(16, 6f, Color.white);
        }

        private void Start()
        {
            if (PlayerController.Instance != null) playerTransform = PlayerController.Instance.transform;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // 공명 패널(Resonance) 패시브: EXP 구슬 획득 범위 +25%/Lv (최대 +125%)
            float rangeMultiplier = PassiveStatManager.Instance != null ? PassiveStatManager.Instance.GetPickupRangeMultiplier() : 1.0f;
            float effectiveMagnetDistance = magnetDistance * rangeMultiplier;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= effectiveMagnetDistance)
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
