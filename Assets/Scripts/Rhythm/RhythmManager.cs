using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Player;

namespace ConductorSymphony.Rhythm
{
    public class RhythmManager : MonoBehaviour
    {
        public static RhythmManager Instance { get; private set; }

        public event System.Action<HitRating, RhythmLane> OnHitSuccessEvent;

        [Header("Rhythm Settings")]
        [SerializeField] private float bpm = 120f;
        [SerializeField] private float spawnDistance = 4.0f;
        [SerializeField] private float noteTravelDuration = 1.2f;

        [Header("Timing Windows (Seconds)")]
        [SerializeField] private float perfectWindow = 0.08f;
        [SerializeField] private float greatWindow = 0.18f;

        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;

        private List<RhythmNote> activeNotes = new List<RhythmNote>();
        private float secPerBeat;
        private float nextBeatTime;
        private int currentScore = 0;
        private int currentCombo = 0;

        private Texture2D defaultNoteTexture;
        private Sprite defaultNoteSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            secPerBeat = 60f / bpm;
            nextBeatTime = Time.time + secPerBeat;

            CreateDefaultSprite();
        }

        private void Start()
        {
            if (targetTransform == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    targetTransform = player.transform;
                }
            }
        }

        private void CreateDefaultSprite()
        {
            int size = 32;
            defaultNoteTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        pixels[y * size + x] = Color.cyan;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            defaultNoteTexture.SetPixels(pixels);
            defaultNoteTexture.Apply();
            defaultNoteSprite = Sprite.Create(defaultNoteTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // BPM Beat Spawner
            if (Time.time >= nextBeatTime)
            {
                SpawnBeatNote();
                nextBeatTime += secPerBeat;
            }

            // Left-hand inputs (W, A, S, D) via New Input System
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.wasPressedThisFrame) CheckHit(RhythmLane.Left);
                if (keyboard.wKey.wasPressedThisFrame) CheckHit(RhythmLane.Up);
                if (keyboard.sKey.wasPressedThisFrame) CheckHit(RhythmLane.Down);
                if (keyboard.dKey.wasPressedThisFrame) CheckHit(RhythmLane.Right);
            }
        }

        private void SpawnBeatNote()
        {
            // Randomly select lane (Left=A, Up=W, Down=S, Right=D)
            RhythmLane lane = (RhythmLane)Random.Range(0, 4);
            Vector3 spawnDir = GetLaneDirection(lane);
            float targetTime = Time.time + noteTravelDuration;

            GameObject noteObj = new GameObject($"Note_{lane}_{Time.frameCount}");
            SpriteRenderer sr = noteObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultNoteSprite;
            sr.color = GetLaneColor(lane);
            sr.sortingOrder = 10;

            RhythmNote note = noteObj.AddComponent<RhythmNote>();
            note.Initialize(lane, targetTransform, spawnDir, spawnDistance, targetTime, noteTravelDuration);

            activeNotes.Add(note);
        }

        private Vector3 GetLaneDirection(RhythmLane lane)
        {
            switch (lane)
            {
                case RhythmLane.Left:  return Vector3.left;
                case RhythmLane.Up:    return Vector3.up;
                case RhythmLane.Down:  return Vector3.down;
                case RhythmLane.Right: return Vector3.right;
                default: return Vector3.up;
            }
        }

        private Color GetLaneColor(RhythmLane lane)
        {
            switch (lane)
            {
                case RhythmLane.Left:  return new Color(1.0f, 0.4f, 0.4f); // Reddish Q
                case RhythmLane.Up:    return new Color(0.4f, 0.8f, 1.0f); // Cyan W
                case RhythmLane.Down:  return new Color(1.0f, 0.9f, 0.3f); // Yellow E
                case RhythmLane.Right: return new Color(0.4f, 1.0f, 0.4f); // Green R
                default: return Color.white;
            }
        }

        private void CheckHit(RhythmLane lane)
        {
            RhythmNote targetNote = null;
            float closestDiff = float.MaxValue;
            float currentTime = Time.time;

            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                if (activeNotes[i] == null)
                {
                    activeNotes.RemoveAt(i);
                    continue;
                }

                if (activeNotes[i].Lane == lane)
                {
                    float diff = Mathf.Abs(currentTime - activeNotes[i].TargetTime);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        targetNote = activeNotes[i];
                    }
                }
            }

            if (targetNote != null && closestDiff <= greatWindow)
            {
                HitRating rating = (closestDiff <= perfectWindow) ? HitRating.Perfect : HitRating.Great;
                ProcessHit(targetNote, rating);
            }
            else if (targetNote != null && closestDiff <= greatWindow * 1.5f)
            {
                ProcessHit(targetNote, HitRating.Miss);
            }
        }

        private void ProcessHit(RhythmNote note, HitRating rating)
        {
            activeNotes.Remove(note);
            note.DestroyNote();

            if (rating == HitRating.Perfect)
            {
                currentCombo++;
                currentScore += 100 + (currentCombo * 10);
            }
            else if (rating == HitRating.Great)
            {
                currentCombo++;
                currentScore += 50 + (currentCombo * 5);
            }
            else
            {
                currentCombo = 0;
            }

            if (rating == HitRating.Perfect || rating == HitRating.Great)
            {
                OnHitSuccessEvent?.Invoke(rating, note.Lane);
            }

            if (RhythmUI.Instance != null)
            {
                RhythmUI.Instance.ShowHitRating(rating);
                RhythmUI.Instance.UpdateScoreAndCombo(currentScore, currentCombo);
            }
        }

        public void OnNoteMissed(RhythmNote note)
        {
            if (activeNotes.Contains(note))
            {
                activeNotes.Remove(note);
                currentCombo = 0;
                if (RhythmUI.Instance != null)
                {
                    RhythmUI.Instance.ShowHitRating(HitRating.Miss);
                    RhythmUI.Instance.UpdateScoreAndCombo(currentScore, currentCombo);
                }
            }
        }
    }
}
