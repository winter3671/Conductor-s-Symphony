using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Enemy;
using ConductorSymphony.Rhythm;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Player
{
    public enum PlayerFacingDirection
    {
        Down,  // Front
        Up,    // Back
        Left,  // Leftside
        Right  // Rightside
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class PlayerController : MonoSingleton<PlayerController>
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float targetWorldHeight = 1.0f; // Scaled to 1.0m height

        [Header("Player Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        private int currentHealth;
        private float invulnerableTimer = 0f;
        private Rigidbody2D rb;
        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;

        // Sprite Resources
        private Sprite idleFront;
        private Sprite idleBack;
        private Sprite idleLeft;
        private Sprite idleRight;

        private Sprite hitPointLeft;
        private Sprite hitPointLeftTop;
        private Sprite hitPointRightTop;
        private Sprite hitPointRight;

        private Sprite[] walkFrontFrames;
        private Sprite[] walkBackFrames;
        private Sprite[] walkLeftFrames;
        private Sprite[] walkRightFrames;

        // Animation State
        private PlayerFacingDirection facingDirection = PlayerFacingDirection.Down;
        private float animTimer = 0f;
        private int currentAnimFrame = 0;
        private float frameDuration = 0.08f;

        private float hitPoseTimer = 0f;
        private Sprite currentHitPoseSprite;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        public static event System.Action<int, int> OnHealthChangedEvent;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            col = GetComponent<CircleCollider2D>();
            col.radius = 0.65f; // Expanded hurtbox radius for reliable bullet hit detection
            col.isTrigger = true;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            spriteRenderer.sortingOrder = 10;

            currentHealth = maxHealth;

            LoadPlayerSprites();
        }

        private void LoadPlayerSprites()
        {
            // Idle Sprites
            idleFront = Resources.Load<Sprite>("Sprites/Player/Player_Idle/front");
            idleBack = Resources.Load<Sprite>("Sprites/Player/Player_Idle/back");
            idleLeft = Resources.Load<Sprite>("Sprites/Player/Player_Idle/leftside");
            idleRight = Resources.Load<Sprite>("Sprites/Player/Player_Idle/rightside");

            // Hit Conduct Pose Sprites
            hitPointLeft = Resources.Load<Sprite>("Sprites/Player/Player_Hit/point_left");
            hitPointLeftTop = Resources.Load<Sprite>("Sprites/Player/Player_Hit/point_lefttop");
            hitPointRightTop = Resources.Load<Sprite>("Sprites/Player/Player_Hit/point_righttop");
            hitPointRight = Resources.Load<Sprite>("Sprites/Player/Player_Hit/point_right");

            // Move Frames
            walkFrontFrames = LoadFramesSorted("Sprites/Player/Player_Move/frontwalk");
            walkBackFrames = LoadFramesSorted("Sprites/Player/Player_Move/backwalk");
            walkLeftFrames = LoadFramesSorted("Sprites/Player/Player_Move/leftwalk");
            walkRightFrames = LoadFramesSorted("Sprites/Player/Player_Move/rightwalk");

            // Set Initial Sprite
            if (idleFront != null)
            {
                SetSprite(idleFront);
            }
        }

        private Sprite[] LoadFramesSorted(string path)
        {
            Object[] rawObjs = Resources.LoadAll(path, typeof(Sprite));
            if (rawObjs == null || rawObjs.Length == 0) return new Sprite[0];

            List<Sprite> sprites = new List<Sprite>();
            foreach (var obj in rawObjs)
            {
                if (obj is Sprite s) sprites.Add(s);
            }

            // Sort naturally by name (e.g. frontwalk1, frontwalk2 ... frontwalk9)
            sprites.Sort((a, b) => a.name.CompareTo(b.name));
            return sprites.ToArray();
        }

        private void Start()
        {
            OnHealthChangedEvent?.Invoke(currentHealth, maxHealth);

            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent += OnHitSuccess;
            }
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnHitSuccessEvent -= OnHitSuccess;
            }
        }

        private void OnHitSuccess(HitRating rating, RhythmLane lane)
        {
            if (rating == HitRating.Miss) return;

            Sprite hitSprite = null;
            switch (lane)
            {
                case RhythmLane.Left:    hitSprite = hitPointLeft; break;     // Q key
                case RhythmLane.UpLeft:  hitSprite = hitPointLeftTop; break;  // W key
                case RhythmLane.UpRight: hitSprite = hitPointRightTop; break; // E key
                case RhythmLane.Right:   hitSprite = hitPointRight; break;    // R key
            }

            if (hitSprite != null)
            {
                currentHitPoseSprite = hitSprite;
                hitPoseTimer = 0.32f; // Show conductor hit pose for 0.32s
            }
        }

        private void Update()
        {
            // Block player movement and facing direction updates when game is paused (e.g. LevelUpUI)
            if (Time.timeScale <= 0f)
            {
                moveInput = Vector2.zero;
                return;
            }
            if (invulnerableTimer > 0f)
            {
                invulnerableTimer -= Time.deltaTime;
            }

            if (hitPoseTimer > 0f)
            {
                hitPoseTimer -= Time.deltaTime;
            }

            float moveX = 0f;
            float moveY = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rightArrowKey.isPressed) moveX += 1f;
                if (keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.upArrowKey.isPressed) moveY += 1f;
                if (keyboard.downArrowKey.isPressed) moveY -= 1f;
            }

            moveInput = new Vector2(moveX, moveY).normalized;

            UpdateFacingDirection();
            UpdateAnimation();
        }

        private void UpdateFacingDirection()
        {
            if (moveInput.magnitude < 0.1f) return;

            if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
            {
                facingDirection = (moveInput.y > 0) ? PlayerFacingDirection.Up : PlayerFacingDirection.Down;
            }
            else
            {
                facingDirection = (moveInput.x > 0) ? PlayerFacingDirection.Right : PlayerFacingDirection.Left;
            }
        }

        private void UpdateAnimation()
        {
            // Priority 1: Conductor Hit Pose
            if (hitPoseTimer > 0f && currentHitPoseSprite != null)
            {
                SetSprite(currentHitPoseSprite);
                return;
            }

            // Priority 2: Walk Animation vs Idle
            bool isMoving = moveInput.magnitude > 0.1f;
            if (isMoving)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= frameDuration)
                {
                    animTimer = 0f;
                    currentAnimFrame++;
                }

                Sprite[] currentFrames = GetWalkFrames(facingDirection);
                if (currentFrames != null && currentFrames.Length > 0)
                {
                    int frameIndex = currentAnimFrame % currentFrames.Length;
                    SetSprite(currentFrames[frameIndex]);
                }
            }
            else
            {
                currentAnimFrame = 0;
                animTimer = 0f;
                SetSprite(GetIdleSprite(facingDirection));
            }
        }

        private Sprite GetIdleSprite(PlayerFacingDirection dir)
        {
            switch (dir)
            {
                case PlayerFacingDirection.Up:    return idleBack != null ? idleBack : idleFront;
                case PlayerFacingDirection.Left:  return idleLeft != null ? idleLeft : idleFront;
                case PlayerFacingDirection.Right: return idleRight != null ? idleRight : idleFront;
                default:                          return idleFront;
            }
        }

        private Sprite[] GetWalkFrames(PlayerFacingDirection dir)
        {
            switch (dir)
            {
                case PlayerFacingDirection.Up:    return walkBackFrames;
                case PlayerFacingDirection.Left:  return walkLeftFrames;
                case PlayerFacingDirection.Right: return walkRightFrames;
                default:                          return walkFrontFrames;
            }
        }

        private void SetSprite(Sprite newSprite)
        {
            if (newSprite == null || spriteRenderer == null) return;

            spriteRenderer.sprite = newSprite;

            // Height Normalization: Scale sprite dynamically so every frame renders at exact target world height (1.8 units)
            float spriteHeight = newSprite.bounds.size.y;
            if (spriteHeight > 0.001f)
            {
                float scale = targetWorldHeight / spriteHeight;
                transform.localScale = new Vector3(scale, scale, 1.0f);
            }
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (invulnerableTimer > 0f) return;

            EnemyMonster enemy = other.GetComponent<EnemyMonster>();
            if (enemy != null)
            {
                TakeDamage(enemy.DamageToPlayer);
            }
        }

        public void TakeDamage(int amount)
        {
            if (invulnerableTimer > 0f) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            invulnerableTimer = invulnerabilityDuration;

            OnHealthChangedEvent?.Invoke(currentHealth, maxHealth);

            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0)
            {
                OnPlayerDeath();
            }
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                if (spriteRenderer != null) spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.08f);
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.08f);
            }
        }

        private void OnPlayerDeath()
        {
            Debug.Log("Player Died!");
        }
    }
}
