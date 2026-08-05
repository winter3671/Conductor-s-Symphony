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

        [SerializeField] private SettingsPanelController settingsPanelController;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform beatPulseIcon;

        private AudioSource[] tickSources;
        private AudioClip tickClip;
        private double[] beatTargetDspTimes;
        private bool[] beatCaptured;
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
            instructionLabel.text = "박자에 맞춰 준비하세요...";

            int totalBeats = LeadInBeats + MeasuredBeats;
            beatTargetDspTimes = new double[totalBeats];
            beatCaptured = new bool[totalBeats];

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

            UpdateBeatPulse();

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

        private void UpdateBeatPulse()
        {
            if (beatPulseIcon == null) return;
            double now = AudioSettings.dspTime;
            double nearestDiff = double.MaxValue;
            foreach (double target in beatTargetDspTimes)
            {
                double diff = System.Math.Abs(target - now);
                if (diff < nearestDiff) nearestDiff = diff;
            }
            float scale = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((float)(nearestDiff / 0.15)));
            beatPulseIcon.localScale = Vector3.one * scale;
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
            settingsPanelController.RefreshSyncLabel();
            gameObject.SetActive(false);
        }
    }
}
