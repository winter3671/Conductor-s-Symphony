using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Rhythm
{
    public class RhythmManager : MonoSingleton<RhythmManager>
    {
        public event System.Action<HitRating, RhythmLane> OnHitSuccessEvent;
        public static event System.Action<int, int, HitRating> OnScoreUpdatedEvent; // score, combo, rating

        [Header("Rhythm Sequencer Settings")]
        [SerializeField] private float bpm = 97f;
        [SerializeField] private float spawnDistance = 4.0f;
        [SerializeField] private float noteTravelDuration = 2.474f; // Exactly 4 beats (1 bar) at 97 BPM for smooth readable travel speed

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

        private Sprite defaultNoteSprite;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            // 97 BPM: 32-step cycle over 2 bars (16 beats) => 0.309278s per 16th-note step
            stepDuration = (60f / bpm) / 2f;

            defaultNoteSprite = ProceduralSpriteFactory.CreateFilledCircle(32, 14f, Color.cyan);
        }

        private void Start()
        {
            if (targetTransform == null && PlayerController.Instance != null)
            {
                targetTransform = PlayerController.Instance.transform;
            }

            nextStepTime = Time.time;
            currentStep = 0;
        }

        private void Update()
        {
            // Block rhythm note updates and QWER inputs when paused
            if (Time.timeScale <= 0f) return;

            // 32-Step Sequencer Loop
            if (Time.time >= nextStepTime)
            {
                ProcessSequencerStep(currentStep);
                currentStep = (currentStep + 1) % 32;
                nextStepTime += stepDuration;
            }

            // Left-hand inputs (QWER Arc Keys) via New Input System
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame) CheckHit(RhythmLane.Left);    // Slot 0 (Q = Left)
                if (keyboard.wKey.wasPressedThisFrame) CheckHit(RhythmLane.UpLeft);  // Slot 1 (W = Upper-Left)
                if (keyboard.eKey.wasPressedThisFrame) CheckHit(RhythmLane.UpRight); // Slot 2 (E = Upper-Right)
                if (keyboard.rKey.wasPressedThisFrame) CheckHit(RhythmLane.Right);   // Slot 3 (R = Right)
            }
        }

        private void ProcessSequencerStep(int step)
        {
            if (InstrumentManager.Instance == null) return;

            var equipped = InstrumentManager.Instance.AcquiredInstruments;
            int maxUnlocked = InstrumentManager.Instance.GetUnlockedSlotsCount();
            bool stepHasNotes = false;

            for (int slot = 0; slot < equipped.Count && slot < maxUnlocked; slot++)
            {
                InstrumentInfo inst = equipped[slot];
                int[] pattern = InstrumentPatternDatabase.GetPattern(inst.type, inst.level);

                if (pattern != null && step < pattern.Length && pattern[step] == 1)
                {
                    RhythmLane lane = GetLaneForSlot(slot);
                    SpawnNoteForLane(lane, inst.themeColor);
                    stepHasNotes = true;
                }
            }

            if (stepHasNotes)
            {
                SpawnShrinkingRingForStep(0.85f);
            }
        }

        private void SpawnShrinkingRingForStep(float alphaAmount)
        {
            if (targetTransform == null) return;

            GameObject ringObj = new GameObject($"RhythmRing_{Time.frameCount}");
            ShrinkingRhythmRing ring = ringObj.AddComponent<ShrinkingRhythmRing>();
            ring.Initialize(targetTransform, spawnDistance, Time.time, noteTravelDuration, Color.white, alphaAmount);
        }

        public static RhythmLane GetLaneForSlot(int slot)
        {
            switch (slot)
            {
                case 0: return RhythmLane.Left;    // Slot 0 = Q (Left)
                case 1: return RhythmLane.Right;   // Slot 1 = R (Right)
                case 2: return RhythmLane.UpLeft;  // Slot 2 = W (UpLeft)
                case 3: return RhythmLane.UpRight; // Slot 3 = E (UpRight)
                default: return RhythmLane.Left;
            }
        }

        public static int GetSlotForLane(RhythmLane lane)
        {
            switch (lane)
            {
                case RhythmLane.Left:    return 0; // Q
                case RhythmLane.Right:   return 1; // R
                case RhythmLane.UpLeft:  return 2; // W
                case RhythmLane.UpRight: return 3; // E
                default: return 0;
            }
        }

        private void SpawnNoteForLane(RhythmLane lane, Color color, float explicitTargetTime = -1f)
        {
            Vector3 spawnDir = GetLaneDirection(lane);
            float targetTime = explicitTargetTime > 0f ? explicitTargetTime : Time.time + noteTravelDuration;

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
                case RhythmLane.Left:    return Vector3.left; // 180 deg
                case RhythmLane.UpLeft:  return new Vector3(-0.707f, 0.707f, 0f); // 135 deg (Upper Left)
                case RhythmLane.UpRight: return new Vector3(0.707f, 0.707f, 0f);  // 45 deg (Upper Right)
                case RhythmLane.Right:   return Vector3.right; // 0 deg
                default: return Vector3.left;
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
                    if (diff < closestDiff && diff <= greatWindow)
                    {
                        closestDiff = diff;
                        targetNote = activeNotes[i];
                    }
                }
            }

            if (targetNote != null)
            {
                HitRating rating = (closestDiff <= perfectWindow) ? HitRating.Perfect : HitRating.Great;
                ProcessHit(targetNote, rating);
            }
        }

        private void ProcessHit(RhythmNote note, HitRating rating)
        {
            activeNotes.Remove(note);
            note.DestroyNote();

            if (rating == HitRating.Perfect)
            {
                currentScore += 100;
                currentCombo++;
            }
            else if (rating == HitRating.Great)
            {
                currentScore += 50;
                currentCombo++;
            }

            OnScoreUpdatedEvent?.Invoke(currentScore, currentCombo, rating);

            if (targetTransform != null)
            {
                GameObject popupObj = new GameObject("HitPopup");
                HitFloatingText popup = popupObj.AddComponent<HitFloatingText>();
                popup.Initialize(targetTransform.position, rating);
            }

            OnHitSuccessEvent?.Invoke(rating, note.Lane);

            int slot = GetSlotForLane(note.Lane);
            if (InstrumentManager.Instance != null && slot < InstrumentManager.Instance.AcquiredInstruments.Count)
            {
                InstrumentType type = InstrumentManager.Instance.AcquiredInstruments[slot].type;

                if (Audio.AudioLayerManager.Instance != null)
                {
                    Audio.AudioLayerManager.Instance.PlayInstrumentKeySound(type, rating == HitRating.Perfect);
                }
            }
        }

        public void OnNoteMissed(RhythmNote note)
        {
            activeNotes.Remove(note);
            currentCombo = 0;
            OnScoreUpdatedEvent?.Invoke(currentScore, currentCombo, HitRating.Miss);

            if (targetTransform != null)
            {
                GameObject popupObj = new GameObject("HitPopup");
                HitFloatingText popup = popupObj.AddComponent<HitFloatingText>();
                popup.Initialize(targetTransform.position, HitRating.Miss);
            }
        }
    }
}
