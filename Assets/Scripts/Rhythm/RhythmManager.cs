using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ConductorSymphony.Instrument;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;
using ConductorSymphony.Settings;

namespace ConductorSymphony.Rhythm
{
    public class RhythmManager : MonoSingleton<RhythmManager>
    {
        public event System.Action<HitRating, RhythmLane> OnHitSuccessEvent;
        public static event System.Action<int, int, HitRating> OnScoreUpdatedEvent; // score, combo, rating

        // 홀드(롱노트) 전용 이벤트 - 10종 악기별 공격 메커니즘 기획서의 바이올린/프렌치호른/첼로/팀파니용.
        public static event System.Action<RhythmLane> OnHoldTickEvent; // 홀드 유지 중 매 프레임 (지속 피해 등에 사용)
        public static event System.Action<RhythmLane, float, bool> OnHoldReleasedEvent; // lane, progress01, completedFully(끝까지 채웠는지)

        // Single source of truth for the game's fixed tempo — SyncCalibrationController references
        // this directly so the calibration metronome can never drift from actual gameplay BPM.
        public const float Bpm = 97f;

        [Header("Rhythm Sequencer Settings")]
        [SerializeField] private float spawnDistance = 4.0f;
        [SerializeField] private float noteTravelDuration = 2.474f; // Exactly 4 beats (1 bar) at 97 BPM for smooth readable travel speed

        [Header("Timing Windows (Seconds)")]
        [SerializeField] private float perfectWindow = 0.10f;
        [SerializeField] private float greatWindow = 0.22f;
        [SerializeField] private float missWindow = 0.45f; // Outer input window: presses outside greatWindow but inside this consume the note as a Miss

        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;

        [Header("Judgment Ring")]
        [SerializeField] private float judgmentRadius = 0.6f; // Fixed radius of the Perfect judgment line around the character

        private List<RhythmNote> activeNotes = new List<RhythmNote>();
        private float stepDuration; // Seconds per step in 32-step grid
        private int lastProcessedStep = -1; // Last step index fired, derived fresh from AudioLayerManager.SongTime each frame

        private int currentScore = 0;
        private int currentCombo = 0;
        public int CurrentCombo => currentCombo; // 피아노 6연타 캐스케이드/글록켄슈필 4·8버스트 등 "N연속 성공" 트리거용 공개 게터

        // 레인(4개)별 현재 진행 중인 홀드 노트. null이면 해당 레인은 홀드 중이 아님.
        private readonly RhythmNote[] activeHoldByLane = new RhythmNote[4];

        // Rolling accuracy window backing M_rhythm (game_balance_design.docx section 1).
        // Perfect/Great both count as a "success"; Miss counts as a failure. Window (not lifetime total)
        // so the multiplier tracks recent play rather than being dominated by an early streak or a single miss.
        private const int SuccessRateWindowSize = 20;
        private readonly Queue<bool> recentHitWindow = new Queue<bool>();

        // 0% (no recent hits) -> 0.5x, 50% -> 1.25x, 100% (full recent combo) -> 2.0x
        public float RhythmSuccessRate01 { get; private set; } = 0f;

        private Sprite defaultNoteSprite;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            // 97 BPM: 32-step cycle over 2 bars (16 beats) => 0.309278s per 16th-note step
            stepDuration = (60f / Bpm) / 2f;

            defaultNoteSprite = ProceduralSpriteFactory.CreateFilledCircle(32, 14f, Color.cyan);
        }

        private void Start()
        {
            if (targetTransform == null && PlayerController.Instance != null)
            {
                targetTransform = PlayerController.Instance.transform;
            }

            if (targetTransform != null)
            {
                GameObject ringObj = new GameObject("JudgmentRing");
                JudgmentRing ring = ringObj.AddComponent<JudgmentRing>();
                ring.Initialize(targetTransform, judgmentRadius, Color.white, 0.6f);
            }

            lastProcessedStep = -1;
        }

        private void Update()
        {
            // Block rhythm note updates and QWER inputs when paused
            if (Time.timeScale <= 0f) return;
            if (Audio.AudioLayerManager.Instance == null) return;

            // 32-Step Sequencer Loop — step index is derived fresh every frame from the actual
            // audio playback position (SongTime), not accumulated independently. This means
            // there is no separate gameplay clock that can fall out of sync with the audio
            // after a pause/resume cycle (see pause_beat_drift_analysis.md).
            float songTime = Audio.AudioLayerManager.Instance.SongTime;
            if (songTime >= -noteTravelDuration)
            {
                float audioOffsetSongTime = songTime + noteTravelDuration;
                float cycleDuration = 32f * stepDuration;
                float normalizedTime = audioOffsetSongTime;
                while (normalizedTime < 0f) normalizedTime += cycleDuration;

                int stepIndex = Mathf.FloorToInt(normalizedTime / stepDuration) % 32;
                if (stepIndex != lastProcessedStep)
                {
                    lastProcessedStep = stepIndex;
                    ProcessSequencerStep(stepIndex);
                }
            }

            // Left-hand inputs (QWER Arc Keys) via New Input System
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[GameSettings.GetBinding(GameAction.HitLeft)].wasPressedThisFrame) CheckHit(RhythmLane.Left);
                if (keyboard[GameSettings.GetBinding(GameAction.HitUpLeft)].wasPressedThisFrame) CheckHit(RhythmLane.UpLeft);
                if (keyboard[GameSettings.GetBinding(GameAction.HitUpRight)].wasPressedThisFrame) CheckHit(RhythmLane.UpRight);
                if (keyboard[GameSettings.GetBinding(GameAction.HitRight)].wasPressedThisFrame) CheckHit(RhythmLane.Right);
            }

            UpdateActiveHolds(keyboard);
        }

        // 진행 중인 홀드 노트들의 "키를 계속 누르고 있는지"를 매 프레임 확인.
        // 도중에 떼면 조기 이탈(완료되지 못함), 지속시간을 다 채우면 정상 완료로 각각 OnHoldReleasedEvent를 발행.
        private void UpdateActiveHolds(Keyboard keyboard)
        {
            for (int i = 0; i < activeHoldByLane.Length; i++)
            {
                RhythmNote note = activeHoldByLane[i];
                if (note == null) continue;

                RhythmLane lane = (RhythmLane)i;
                bool keyDown = IsLaneKeyPressed(keyboard, lane);

                if (!keyDown)
                {
                    activeHoldByLane[i] = null;
                    OnHoldReleasedEvent?.Invoke(lane, note.HoldProgress01, false);
                    note.DestroyNote();
                    continue;
                }

                bool stillHolding = note.TickHold(Time.deltaTime);
                OnHoldTickEvent?.Invoke(lane);

                if (!stillHolding)
                {
                    activeHoldByLane[i] = null;
                    OnHoldReleasedEvent?.Invoke(lane, 1.0f, true);
                    note.DestroyNote();
                }
            }
        }

        private bool IsLaneKeyPressed(Keyboard keyboard, RhythmLane lane)
        {
            if (keyboard == null) return false;
            switch (lane)
            {
                case RhythmLane.Left:    return keyboard[GameSettings.GetBinding(GameAction.HitLeft)].isPressed;
                case RhythmLane.UpLeft:  return keyboard[GameSettings.GetBinding(GameAction.HitUpLeft)].isPressed;
                case RhythmLane.UpRight: return keyboard[GameSettings.GetBinding(GameAction.HitUpRight)].isPressed;
                case RhythmLane.Right:   return keyboard[GameSettings.GetBinding(GameAction.HitRight)].isPressed;
                default: return false;
            }
        }

        private void ProcessSequencerStep(int step)
        {
            if (InstrumentManager.Instance == null) return;

            var equipped = InstrumentManager.Instance.AcquiredInstruments;
            int maxUnlocked = InstrumentManager.Instance.GetUnlockedSlotsCount();

            for (int slot = 0; slot < equipped.Count && slot < maxUnlocked; slot++)
            {
                InstrumentInfo inst = equipped[slot];
                int[] pattern = InstrumentPatternDatabase.GetPattern(inst.type, inst.level);

                if (pattern != null && step < pattern.Length && pattern[step] == 1)
                {
                    RhythmLane lane = GetLaneForSlot(slot);
                    bool isHold = InstrumentPatternDatabase.IsHoldBased(inst.type);

                    // 홀드 기반 악기(바이올린/프렌치호른/첼로/팀파니)는 같은 레인에 이미 홀드가 진행 중이면
                    // 새 노트를 스폰하지 않는다 - 고레벨 패턴은 onset 간격이 홀드 길이보다 짧아질 수 있어
                    // 겹쳐 스폰되면 activeHoldByLane 슬롯이 충돌한다.
                    if (isHold && activeHoldByLane[(int)lane] != null)
                    {
                        continue;
                    }

                    int holdSteps = isHold ? InstrumentPatternDatabase.GetHoldLengthSteps(inst.type) : 0;
                    float holdDurationSeconds = holdSteps * stepDuration;

                    SpawnNoteForLane(lane, inst.themeColor, isHold ? NoteKind.Hold : NoteKind.Tap, holdDurationSeconds);
                }
            }
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

        private void RecordHitResult(bool success)
        {
            recentHitWindow.Enqueue(success);
            if (recentHitWindow.Count > SuccessRateWindowSize)
            {
                recentHitWindow.Dequeue();
            }

            int hitCount = 0;
            foreach (bool wasHit in recentHitWindow)
            {
                if (wasHit) hitCount++;
            }
            RhythmSuccessRate01 = recentHitWindow.Count > 0 ? (float)hitCount / recentHitWindow.Count : 0f;
        }

        // M_rhythm from game_balance_design.docx section 1: linear from 0.5x (0% success) to 2.0x (100% success),
        // passing exactly through 1.25x at 50% success.
        public float GetRhythmDamageMultiplier()
        {
            return 0.5f + 1.5f * RhythmSuccessRate01;
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

        private void SpawnNoteForLane(RhythmLane lane, Color color, NoteKind kind = NoteKind.Tap, float holdDurationSeconds = 0f)
        {
            Vector3 spawnDir = GetLaneDirection(lane);
            float songTimeNow = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongTime : 0f;
            float targetTime = songTimeNow + noteTravelDuration;

            GameObject noteObj = new GameObject($"Note_{lane}_{Time.frameCount}");
            SpriteRenderer sr = noteObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultNoteSprite;
            sr.color = color;
            sr.sortingOrder = 10;

            RhythmNote note = noteObj.AddComponent<RhythmNote>();
            note.Initialize(lane, targetTransform, spawnDir, spawnDistance, targetTime, noteTravelDuration, judgmentRadius, missWindow, kind, holdDurationSeconds);

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
            float currentTime = Audio.AudioLayerManager.Instance != null
                ? Audio.AudioLayerManager.Instance.SongTime + GameSettings.RhythmSyncOffsetSeconds
                : 0f;

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
                    if (diff < closestDiff && diff <= missWindow)
                    {
                        closestDiff = diff;
                        targetNote = activeNotes[i];
                    }
                }
            }

            if (targetNote == null) return;

            if (closestDiff <= greatWindow)
            {
                HitRating rating = (closestDiff <= perfectWindow) ? HitRating.Perfect : HitRating.Great;
                ProcessHit(targetNote, rating);
            }
            else
            {
                // Pressed too early/late relative to the note (outside greatWindow but still the
                // nearest reachable note within missWindow) — consume it as a Miss instead of
                // silently ignoring the input. This closes the spam exploit where mashing a key
                // early cost nothing and let the player wait risk-free for a later press to land
                // inside the window.
                OnNoteMissed(targetNote);
                targetNote.DestroyNote();
            }
        }

        private void ProcessHit(RhythmNote note, HitRating rating)
        {
            activeNotes.Remove(note);

            // Hold 노트는 초기 판정에 성공해도 즉시 파괴하지 않고 판정 링에 고정한 채 지속시간을 추적한다.
            // (UpdateActiveHolds()가 매 프레임 키 유지 여부를 확인해 조기 이탈/정상 완료를 각각 처리)
            if (note.Kind == NoteKind.Hold)
            {
                activeHoldByLane[(int)note.Lane] = note;
                note.BeginHold();
            }
            else
            {
                note.DestroyNote();
            }

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

            RecordHitResult(success: true);
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
            RecordHitResult(success: false);
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
