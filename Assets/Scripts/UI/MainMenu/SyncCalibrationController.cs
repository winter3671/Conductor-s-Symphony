using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ConductorSymphony.Settings;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.UI
{
    public class SyncCalibrationController : MonoBehaviour
    {
        private const int LeadInBeats = 2;
        private const int MeasuredBeats = 6;
        private const int MinValidSamples = 4;
        private const float CaptureWindowSeconds = 0.3f;
        private const float CountdownSeconds = 1.0f;

        // 노트 낙하 연출 튜닝값. NoteLane 프리팹 레이아웃(높이 420, 판정선이 바닥에서 60 위)과
        // 짝을 맞춰 하드코딩되어 있음 — 레인 크기를 바꾸면 SpawnY/JudgmentY도 같이 조정해야 한다.
        private const float NoteTravelSeconds = 1.4f;
        private const float SpawnY = 190f;
        private const float JudgmentY = -150f;
        private const float CapturedFlashSeconds = 0.18f;
        private const float MissedFlashSeconds = 0.18f;

        private static readonly Color DefaultNoteColor = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color LeadInNoteColor = new Color(0.6f, 0.6f, 0.65f, 0.7f);
        private static readonly Color CapturedNoteColor = new Color(0.3f, 1f, 0.4f, 1f);
        private static readonly Color MissedNoteColor = new Color(1f, 0.3f, 0.3f, 0.9f);

        [SerializeField] private SettingsPanelController settingsPanelController;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform noteLane;
        [SerializeField] private RectTransform judgmentLine;

        private AudioSource[] tickSources;
        private AudioClip tickClip;
        private double[] beatTargetDspTimes;
        private bool[] beatCaptured;
        private double[] captureDspTime;
        private RectTransform[] notePool;
        private Image[] noteImages;
        private readonly List<double> offsetSamplesSeconds = new List<double>();
        private bool measuring;
        private float pendingOffsetMs;

        private void Awake()
        {
            tickClip = CreateTickClip();

            applyButton.onClick.AddListener(ApplyResult);
            retryButton.onClick.AddListener(StartMeasurement);
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            StartMeasurement();
        }

        private AudioClip CreateTickClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-40f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1000f * t) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("CalibrationTick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void StartMeasurement()
        {
            measuring = true;
            offsetSamplesSeconds.Clear();
            resultLabel.text = string.Empty;
            applyButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);

            int totalBeats = LeadInBeats + MeasuredBeats;
            beatTargetDspTimes = new double[totalBeats];
            beatCaptured = new bool[totalBeats];
            captureDspTime = new double[totalBeats];

            if (tickSources == null || tickSources.Length != totalBeats)
            {
                tickSources = new AudioSource[totalBeats];
                for (int i = 0; i < totalBeats; i++)
                {
                    AudioSource src = gameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.clip = tickClip;
                    tickSources[i] = src;
                }
            }

            EnsureNotePool(totalBeats);
            for (int i = 0; i < totalBeats; i++)
            {
                notePool[i].gameObject.SetActive(false);
            }

            double beatInterval = 60.0 / RhythmManager.Bpm;
            double startDsp = AudioSettings.dspTime + CountdownSeconds;

            for (int i = 0; i < totalBeats; i++)
            {
                beatTargetDspTimes[i] = startDsp + i * beatInterval;
                tickSources[i].Stop();
                tickSources[i].PlayScheduled(beatTargetDspTimes[i]);
            }
        }

        private void Update()
        {
            if (!measuring) return;

            UpdateNotes();
            UpdateInstructionText();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Key hitKey = GameSettings.GetBinding(GameAction.HitLeft);
                if (keyboard[hitKey].wasPressedThisFrame)
                {
                    CapturePress(AudioSettings.dspTime);
                }
            }

            if (AudioSettings.dspTime > beatTargetDspTimes[beatTargetDspTimes.Length - 1] + CaptureWindowSeconds)
            {
                FinishMeasurement();
            }
        }

        // 어떤 키를, 몇 박자 남았을 때 눌러야 하는지 실시간으로 안내한다.
        // 리드인 구간(입력 불필요)과 측정 구간(입력 필요)을 문구로 명확히 구분한다.
        private void UpdateInstructionText()
        {
            double now = AudioSettings.dspTime;
            string keyName = GameSettings.GetBinding(GameAction.HitLeft).ToString();

            if (now < beatTargetDspTimes[LeadInBeats])
            {
                instructionLabel.text = $"준비하세요... 박자에 맞춰 [{keyName}] 키를 누르게 됩니다";
            }
            else
            {
                instructionLabel.text = $"박자에 맞춰 [{keyName}] 키를 누르세요  ({offsetSamplesSeconds.Count}/{MeasuredBeats})";
            }
        }

        // 8개(리드인 2 + 측정 6) 노트를 매 프레임 재사용하는 풀. StartMeasurement()가 재시도마다
        // 다시 호출되므로 노트 개수가 바뀌지 않는 한(totalBeats는 상수라 안 바뀜) 한 번만 생성한다.
        private void EnsureNotePool(int totalBeats)
        {
            if (notePool != null && notePool.Length == totalBeats) return;
            if (noteLane == null) return;

            notePool = new RectTransform[totalBeats];
            noteImages = new Image[totalBeats];
            for (int i = 0; i < totalBeats; i++)
            {
                GameObject noteObj = new GameObject($"Note_{i}", typeof(RectTransform));
                RectTransform rt = (RectTransform)noteObj.transform;
                rt.SetParent(noteLane, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(160f, 40f);

                Image img = noteObj.AddComponent<Image>();
                img.color = DefaultNoteColor;

                notePool[i] = rt;
                noteImages[i] = img;
                noteObj.SetActive(false);
            }
        }

        // 낙하하는 노트로 "어떤 박자가 언제 오는지"를 시각화한다: 판정선까지 남은 시간에 비례해
        // 레인 위쪽에서 판정선으로 이동시키고, 캡처/미스 여부를 색으로 즉시 피드백한다.
        private void UpdateNotes()
        {
            if (notePool == null) return;
            double now = AudioSettings.dspTime;

            for (int i = 0; i < beatTargetDspTimes.Length; i++)
            {
                RectTransform note = notePool[i];
                double remaining = beatTargetDspTimes[i] - now;

                if (beatCaptured[i])
                {
                    double sinceCapture = now - captureDspTime[i];
                    if (sinceCapture > CapturedFlashSeconds)
                    {
                        note.gameObject.SetActive(false);
                        continue;
                    }
                    note.gameObject.SetActive(true);
                    note.anchoredPosition = new Vector2(0f, JudgmentY);
                    noteImages[i].color = CapturedNoteColor;
                    continue;
                }

                if (remaining > NoteTravelSeconds || remaining < -(CaptureWindowSeconds + MissedFlashSeconds))
                {
                    note.gameObject.SetActive(false);
                    continue;
                }

                note.gameObject.SetActive(true);
                float t = 1f - Mathf.Clamp01((float)(remaining / NoteTravelSeconds));
                note.anchoredPosition = new Vector2(0f, Mathf.Lerp(SpawnY, JudgmentY, t));

                bool isLeadIn = i < LeadInBeats;
                bool missed = remaining < -CaptureWindowSeconds;
                noteImages[i].color = missed ? MissedNoteColor : (isLeadIn ? LeadInNoteColor : DefaultNoteColor);
            }
        }

        private void CapturePress(double pressDspTime)
        {
            for (int i = LeadInBeats; i < beatTargetDspTimes.Length; i++)
            {
                if (beatCaptured[i]) continue;
                double diff = pressDspTime - beatTargetDspTimes[i];
                if (System.Math.Abs(diff) <= CaptureWindowSeconds)
                {
                    beatCaptured[i] = true;
                    captureDspTime[i] = pressDspTime;
                    offsetSamplesSeconds.Add(diff);
                    return;
                }
            }
        }

        private void FinishMeasurement()
        {
            measuring = false;
            instructionLabel.text = string.Empty;

            if (offsetSamplesSeconds.Count < MinValidSamples)
            {
                resultLabel.text = "박자를 다시 맞춰보세요";
                retryButton.gameObject.SetActive(true);
                return;
            }

            double sum = 0;
            foreach (double s in offsetSamplesSeconds) sum += s;
            double averageLagSeconds = sum / offsetSamplesSeconds.Count;

            // averageLagSeconds > 0 이면 플레이어가 박자보다 "늦게" 눌렀다는 뜻 —
            // 이후 판정 시 SongTime을 그만큼 앞당겨(음수 오프셋) 보정해야 늦은 입력이 "정타"로 읽힌다.
            pendingOffsetMs = -(float)(averageLagSeconds * 1000.0);

            resultLabel.text = $"측정된 오프셋: {pendingOffsetMs:+0;-0;0}ms";
            applyButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
        }

        private void ApplyResult()
        {
            GameSettings.RhythmSyncOffsetMs = pendingOffsetMs;
            GameSettings.Save();
            settingsPanelController.RefreshSyncLabel();
            gameObject.SetActive(false);
        }
    }
}
