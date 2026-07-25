using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;

namespace ConductorSymphony.Rhythm
{
    public class RhythmManager : MonoBehaviour
    {
        public static RhythmManager Instance { get; private set; }

        public event System.Action<HitRating, RhythmLane> OnHitSuccessEvent;

        [Header("Rhythm Sequencer Settings")]
        [SerializeField] private float bpm = 90f;
        [SerializeField] private float spawnDistance = 4.0f;
        [SerializeField] private float noteTravelDuration = 1.4f;

        [Header("Timing Windows (Seconds)")]
        [SerializeField] private float perfectWindow = 0.10f;
        [SerializeField] private float greatWindow = 0.22f;

        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;

        private List<RhythmNote> activeNotes = new List<RhythmNote>();
        private float stepDuration; // Seconds per step in 32-step grid
        private float nextStepTime;
        private int currentStep = 0; // 0 to 31

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

            // 90 BPM: 32-beat cycle over ~10.6s => 0.333s per step
            stepDuration = (60f / bpm) / 2f;
            nextStepTime = Time.time + stepDuration;

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
                    pixels[y * size + x] = (dist <= radius) ? Color.cyan : Color.clear;
                }
            }
            defaultNoteTexture.SetPixels(pixels);
            defaultNoteTexture.Apply();
            defaultNoteSprite = Sprite.Create(defaultNoteTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // Block rhythm note updates and WASD inputs when paused
            if (Time.timeScale <= 0f) return;

            // 32-Step Sequencer Loop
            if (Time.time >= nextStepTime)
            {
                ProcessSequencerStep(currentStep);
                currentStep = (currentStep + 1) % 32;
                nextStepTime += stepDuration;
            }

            // Left-hand inputs (WASD) via New Input System
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.wasPressedThisFrame) CheckHit(RhythmLane.Left);
                if (keyboard.wKey.wasPressedThisFrame) CheckHit(RhythmLane.Up);
                if (keyboard.sKey.wasPressedThisFrame) CheckHit(RhythmLane.Down);
                if (keyboard.dKey.wasPressedThisFrame) CheckHit(RhythmLane.Right);
            }
        }

        private void ProcessSequencerStep(int step)
        {
            if (InstrumentManager.Instance == null) return;

            var equipped = InstrumentManager.Instance.AcquiredInstruments;
            for (int slot = 0; slot < equipped.Count && slot < 4; slot++)
            {
                InstrumentInfo inst = equipped[slot];
                int[] pattern = InstrumentPatternDatabase.GetPattern(inst.type, inst.level);

                if (pattern != null && step < pattern.Length && pattern[step] == 1)
                {
                    RhythmLane lane = (RhythmLane)slot;
                    SpawnNoteForLane(lane, inst.themeColor);
                }
            }
        }

        private void SpawnNoteForLane(RhythmLane lane, Color color)
        {
            Vector3 spawnDir = GetLaneDirection(lane);
            float targetTime = Time.time + noteTravelDuration;

            GameObject noteObj = new GameObject($"Note_{lane}_{Time.frameCount}");
            SpriteRenderer sr = noteObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultNoteSprite;
            sr.color = color;
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
