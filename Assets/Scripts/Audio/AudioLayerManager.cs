using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;
using ConductorSymphony.Utility;
using ConductorSymphony.Settings;

namespace ConductorSymphony.Audio
{
    public class AudioLayerManager : MonoSingleton<AudioLayerManager>
    {
        [Header("Audio Delay Sync Offset")]
        [SerializeField] private float audioStartDelay = 2.4742f; // Exactly 1 measure (2.4742s) for 1-bar pre-roll note arrival

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        private Dictionary<InstrumentType, AudioSource> activeInstrumentSources = new Dictionary<InstrumentType, AudioSource>();
        private Dictionary<InstrumentType, AudioClip> instrumentAcquisitionClips = new Dictionary<InstrumentType, AudioClip>();
        private Dictionary<InstrumentType, AudioClip> instrumentKeySounds = new Dictionary<InstrumentType, AudioClip>();

        // dspTime (AudioSettings.dspTime) at which the master track becomes audible.
        // Used as the single source of truth for rhythm timing (see SongTime) so that
        // gameplay/visual timing never has to be re-synced against audio after a pause.
        private double masterStartDspTime = -1.0;

        // dspTime at which the mix was last paused via PauseAllAudio(), or -1.0 while playing.
        // Combined with totalPausedDuration below, this lets SongTime stay a pure function of
        // AudioSettings.dspTime instead of any individual AudioSource's timeSamples (see
        // ring_freeze_bug_fix.md for why reading timeSamples off "whichever instrument source
        // happens to be active" was unsafe once multiple instruments are equipped).
        private double pauseStartDspTime = -1.0;
        private double totalPausedDuration = 0.0;

        // Sources whose bar-aligned join was requested while the mix was paused (e.g. picking a
        // new instrument on the level-up screen, which acquires it before ResumeAllAudio() is
        // called). Scheduling against AudioSettings.dspTime is meaningless while paused — that
        // clock keeps ticking in real time regardless of how long the player lingers on the
        // popup — so these are held here and only actually scheduled once ResumeAllAudio() makes
        // the master clock live again.
        private List<AudioSource> pendingBarAlignedJoins = new List<AudioSource>();

        /// <summary>
        /// The current playback position of the song, in seconds, derived purely from
        /// AudioSettings.dspTime relative to masterStartDspTime, with cumulative paused time
        /// subtracted out. This is the single master clock that RhythmManager/RhythmNote/
        /// ShrinkingRhythmRing read every frame instead of Time.time or any AudioSource's
        /// timeSamples, so there is no second clock — and no per-instrument AudioSource — that
        /// can independently drift out of sync with it across a pause/resume cycle or once
        /// multiple instruments are layered in.
        /// Returns a negative value while the master track is scheduled but not yet audible
        /// (pre-roll), and -1 if no track has been scheduled at all yet.
        /// </summary>
        public float SongTime
        {
            get
            {
                if (masterStartDspTime <= 0.0) return -1f;

                double referenceDsp = (pauseStartDspTime > 0.0) ? pauseStartDspTime : AudioSettings.dspTime;
                double elapsed = referenceDsp - masterStartDspTime - totalPausedDuration;

                if (elapsed < 0.0) return (float)elapsed;

                float loopLength = SongLoopLength;
                if (loopLength > 0f) elapsed %= loopLength;

                return (float)elapsed;
            }
        }

        /// <summary>
        /// Length in seconds of the currently looping master track. SongTime wraps back to 0
        /// every time this many seconds pass, since the underlying AudioSource loops. Callers
        /// tracking an elapsed duration across frames (RhythmNote, ShrinkingRhythmRing) must
        /// add this value back in if they observe SongTime jump backwards mid-flight.
        /// </summary>
        public float SongLoopLength
        {
            get
            {
                AudioSource src = GetAnyActiveInstrumentSource();
                return (src != null && src.clip != null) ? src.clip.length : 0f;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureAudioSources();
            LoadAudioClipsAndKeySounds();
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = 0.5f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = 0.5f; // Set to exact 0.5f per user request
            }
        }

        private void LoadAudioClipsAndKeySounds()
        {
            // 1. Acquisition WAV audio tracks (Played in sync as instruments are acquired)
            instrumentAcquisitionClips[InstrumentType.Drums]        = Resources.Load<AudioClip>("Audio/Sound_Drums");
            instrumentAcquisitionClips[InstrumentType.Piano]        = Resources.Load<AudioClip>("Audio/Sound_Piano");
            instrumentAcquisitionClips[InstrumentType.Violin]       = Resources.Load<AudioClip>("Audio/Sound_Violin");
            instrumentAcquisitionClips[InstrumentType.Flute]        = Resources.Load<AudioClip>("Audio/Sound_Flute");
            instrumentAcquisitionClips[InstrumentType.FrenchHorn]   = Resources.Load<AudioClip>("Audio/Sound_FrenchHorn");
            instrumentAcquisitionClips[InstrumentType.Glockenspiel] = Resources.Load<AudioClip>("Audio/Sound_Glockenspiel");
            instrumentAcquisitionClips[InstrumentType.Cello]        = Resources.Load<AudioClip>("Audio/Sound_Cello");
            instrumentAcquisitionClips[InstrumentType.Timpani]      = Resources.Load<AudioClip>("Audio/Sound_Timpani");
            instrumentAcquisitionClips[InstrumentType.Marimba]      = Resources.Load<AudioClip>("Audio/Sound_Marimba");
            instrumentAcquisitionClips[InstrumentType.Bell]         = Resources.Load<AudioClip>("Audio/Sound_Bell");

            // 2. Clean single-note key tap sounds (Played on EVERY beat note hit)
            instrumentKeySounds[InstrumentType.Drums]        = CreateSynthTone(120f, 0.12f, SynthWaveType.Noise);
            instrumentKeySounds[InstrumentType.Piano]        = CreateSynthTone(440f, 0.15f, SynthWaveType.Triangle);
            instrumentKeySounds[InstrumentType.Violin]       = CreateSynthTone(523.25f, 0.18f, SynthWaveType.Sawtooth);
            instrumentKeySounds[InstrumentType.Flute]        = CreateSynthTone(659.25f, 0.15f, SynthWaveType.Sine);
            instrumentKeySounds[InstrumentType.FrenchHorn]   = CreateSynthTone(392.00f, 0.18f, SynthWaveType.Square);
            instrumentKeySounds[InstrumentType.Glockenspiel] = CreateSynthTone(880.00f, 0.12f, SynthWaveType.Sine);
            instrumentKeySounds[InstrumentType.Cello]        = CreateSynthTone(130.81f, 0.22f, SynthWaveType.Sawtooth);
            instrumentKeySounds[InstrumentType.Timpani]      = CreateSynthTone(180.00f, 0.15f, SynthWaveType.Noise);
            instrumentKeySounds[InstrumentType.Marimba]      = CreateSynthTone(523.25f, 0.14f, SynthWaveType.Triangle);
            instrumentKeySounds[InstrumentType.Bell]         = CreateSynthTone(1046.50f, 0.12f, SynthWaveType.Sine);
        }

        private enum SynthWaveType { Sine, Square, Sawtooth, Triangle, Noise }

        private AudioClip CreateSynthTone(float freq, float duration, SynthWaveType waveType)
        {
            int sampleRate = 44100;
            int lengthSamples = (int)(sampleRate * duration);
            float[] samples = new float[lengthSamples];

            for (int i = 0; i < lengthSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-6.0f * (t / duration));
                float wave = 0f;

                switch (waveType)
                {
                    case SynthWaveType.Sine:
                        wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                        break;
                    case SynthWaveType.Square:
                        wave = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0f ? 1f : -1f;
                        break;
                    case SynthWaveType.Sawtooth:
                        wave = 2f * (t * freq - Mathf.Floor(t * freq + 0.5f));
                        break;
                    case SynthWaveType.Triangle:
                        wave = Mathf.PingPong(t * freq * 4f, 2f) - 1f;
                        break;
                    case SynthWaveType.Noise:
                        wave = Random.Range(-1f, 1f);
                        break;
                }
                samples[i] = wave * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create($"KeySound_{freq}Hz", lengthSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayInstrumentKeySound(InstrumentType type, bool isPerfect)
        {
            if (instrumentKeySounds.TryGetValue(type, out AudioClip clip) && clip != null)
            {
                if (sfxSource != null)
                {
                    sfxSource.pitch = isPerfect ? 1.05f : 1.0f;
                    sfxSource.PlayOneShot(clip, GameSettings.SfxVolume01);
                }
            }
        }

        /// <summary>
        /// Activates and layers an instrument's WAV audio track.
        /// If another instrument is already playing (e.g. Drums), the new instrument's AudioSource
        /// is synchronized to the exact same sample position (timeSamples) so all instruments play in 100% sync.
        /// </summary>
        public void ActivateInstrumentAudio(InstrumentType type)
        {
            if (!instrumentAcquisitionClips.TryGetValue(type, out AudioClip clip) || clip == null)
            {
                PlayInstrumentKeySound(type, true);
                return;
            }

            if (!activeInstrumentSources.TryGetValue(type, out AudioSource source) || source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.loop = true; // Continuous seamless looping!
                source.playOnAwake = false;
                source.volume = 0.85f * GameSettings.InstrumentVolume01;
                source.pitch = 1.0f; // Fixed speed!
                activeInstrumentSources[type] = source;
            }

            source.clip = clip;

            // Find an active reference source to synchronize timeSamples
            AudioSource referenceSource = GetActiveReferenceSource(excludeType: type);

            if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
            {
                if (pauseStartDspTime > 0.0)
                {
                    // Mix is currently paused (e.g. level-up popup open, which is exactly when
                    // instruments are normally acquired) — there is no live playback to "wait for
                    // the next bar" against, and AudioSettings.dspTime keeps ticking in real time
                    // regardless of how long the player lingers on the popup, so a PlayScheduled
                    // target computed now would go stale by an unknown amount. Leave this source
                    // unplayed and defer the actual bar-aligned scheduling to ResumeAllAudio(),
                    // once the master clock is live again and "next bar" is a real answer.
                    pendingBarAlignedJoins.Add(source);
                }
                else
                {
                    // Join at the next bar line instead of cutting in on whatever sample the mix
                    // is currently on — an instrument acquired mid-phrase used to layer in abruptly.
                    ScheduleSourceJoinAtNextBar(source, clip);
                }
            }
            else
            {
                // First instrument (e.g. Drums at game start). Scheduled via the audio engine's
                // own dspTime clock (not PlayDelayed's frame-relative timer) so SongTime can
                // report an accurate pre-roll countdown before real playback begins.
                double startDsp = AudioSettings.dspTime + audioStartDelay;
                source.PlayScheduled(startDsp);
                masterStartDspTime = startDsp;
            }
        }

        /// <summary>
        /// Computes the dsp time of the next bar line of the master clock (see SongTime), and the
        /// corresponding SongTime value at that moment. Shared by both bar-aligned scheduling
        /// paths below — the two differ only in what sample position they start playback from,
        /// not in when they start.
        /// </summary>
        private double ComputeNextBarDspTime(out float targetSongTime)
        {
            float barDuration = audioStartDelay; // 1 bar (2.4742s) — see field comment above
            float currentSongTime = SongTime;

            double targetDsp;

            if (currentSongTime < 0f)
            {
                // Master track is still in its pre-roll countdown (no bar has started yet) —
                // join at the same moment the master track itself becomes audible.
                targetDsp = masterStartDspTime;
                targetSongTime = 0f;
            }
            else
            {
                float phase = currentSongTime % barDuration;
                // Floating point accumulation can land "phase" a hair above 0 right as a bar
                // starts; without this guard that would be misread as "just missed the line"
                // and schedule a full extra bar of silence before joining.
                float timeUntilNextBar = (phase < 0.01f) ? 0f : (barDuration - phase);

                targetDsp = AudioSettings.dspTime + timeUntilNextBar;
                targetSongTime = currentSongTime + timeUntilNextBar;
            }

            return targetDsp;
        }

        /// <summary>
        /// Schedules an instrument's AudioSource to start exactly on the next bar line of the
        /// master clock rather than immediately, so a newly layered instrument joins on a musical
        /// phrase boundary instead of cutting in mid-bar. The source's playhead (timeSamples) is
        /// pre-positioned to the sample that corresponds to that same future bar-line moment in
        /// the shared master timeline — correct here because every instrument clip is a track of
        /// the *same* underlying composition, so "the same absolute song-time sample" is also the
        /// musically correct position within this clip.
        /// </summary>
        private void ScheduleSourceJoinAtNextBar(AudioSource source, AudioClip clip)
        {
            double targetDsp = ComputeNextBarDspTime(out float targetSongTime);

            long targetSample = (long)(targetSongTime * clip.frequency);
            source.timeSamples = (int)(targetSample % clip.samples);
            source.PlayScheduled(targetDsp);
        }

        /// <summary>
        /// Schedules an unrelated clip (e.g. the boss battle BGM) to start on the next bar line of
        /// the master clock, always from the clip's own beginning. Unlike
        /// ScheduleSourceJoinAtNextBar, this clip is not another track of the main composition, so
        /// borrowing a sample position from the main song's elapsed time would start it from an
        /// arbitrary point instead of its own intro, clipping off the start of the cue.
        /// </summary>
        private void ScheduleClipStartAtNextBar(AudioSource source)
        {
            double targetDsp = ComputeNextBarDspTime(out _);
            source.timeSamples = 0;
            source.PlayScheduled(targetDsp);
        }

        private AudioSource GetActiveReferenceSource(InstrumentType excludeType)
        {
            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Key != excludeType && kvp.Value != null && (kvp.Value.isPlaying || kvp.Value.timeSamples > 0))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private AudioSource GetAnyActiveInstrumentSource()
        {
            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Value != null && (kvp.Value.isPlaying || kvp.Value.timeSamples > 0))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private void OnEnable()
        {
            Enemy.BossMonster.OnBossSpawnedEvent += HandleBossSpawned;
            Enemy.BossMonster.OnBossDefeatedEvent += HandleBossDefeated;
        }

        private void OnDisable()
        {
            Enemy.BossMonster.OnBossSpawnedEvent -= HandleBossSpawned;
            Enemy.BossMonster.OnBossDefeatedEvent -= HandleBossDefeated;
        }

        private void HandleBossSpawned(int maxHp)
        {
            PlayBossBattleBGM();
        }

        private void HandleBossDefeated()
        {
            StopBossBattleBGM();
        }

        public void PlayBossBattleBGM()
        {
            AudioClip bossBgm = Resources.Load<AudioClip>("Audio/BGM_BossBattle");
            if (bossBgm == null || bgmSource == null) return;

            bgmSource.clip = bossBgm;
            bgmSource.loop = true;
            bgmSource.volume = 0.75f * GameSettings.BgmVolume01;

            AudioSource referenceSource = GetAnyActiveInstrumentSource();
            if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
            {
                // Join at the next bar line instead of cutting in immediately — an elite boss
                // spawning mid-phrase used to layer its BGM in abruptly on top of the mix. Starts
                // from the clip's own beginning (see ScheduleClipStartAtNextBar) since the boss
                // theme is a separate composition, not another track of the main song.
                ScheduleClipStartAtNextBar(bgmSource);
            }
            else
            {
                bgmSource.Play();
            }
        }

        public void StopBossBattleBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
        }

        public void PauseAllAudio()
        {
            // Idempotent: a second PauseAllAudio() before the matching Resume (e.g. an elite
            // chest popup opening while already paused) must not overwrite the original
            // pause timestamp, or the paused-duration accounting below would undercount.
            if (pauseStartDspTime < 0.0)
            {
                pauseStartDspTime = AudioSettings.dspTime;
            }

            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Value != null && kvp.Value.isPlaying)
                {
                    kvp.Value.Pause();
                }
            }
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        public void ResumeAllAudio()
        {
            if (pauseStartDspTime > 0.0)
            {
                totalPausedDuration += AudioSettings.dspTime - pauseStartDspTime;
                pauseStartDspTime = -1.0;
            }

            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Value != null && !pendingBarAlignedJoins.Contains(kvp.Value))
                {
                    kvp.Value.UnPause();
                }
            }
            if (bgmSource != null)
            {
                bgmSource.UnPause();
            }

            // The master clock is live again now — resolve any instruments that were acquired
            // while paused into a real bar-aligned PlayScheduled call (see ActivateInstrumentAudio).
            for (int i = 0; i < pendingBarAlignedJoins.Count; i++)
            {
                AudioSource pendingSource = pendingBarAlignedJoins[i];
                if (pendingSource != null && pendingSource.clip != null)
                {
                    ScheduleSourceJoinAtNextBar(pendingSource, pendingSource.clip);
                }
            }
            pendingBarAlignedJoins.Clear();
        }

        // 2026-08-10: 일시정지 메뉴 볼륨 슬라이더 지원용 추가. bgmSource/악기 소스들의 volume은
        // 원래 "재생을 시작하는 시점"에만 GameSettings 값을 한 번 읽어와 곱하는 구조라(예:
        // PlayBossBattleBGM(), ActivateInstrumentAudio()), 이미 재생 중인 소스는 슬라이더를
        // 움직여도 볼륨이 즉시 바뀌지 않는 문제가 있었음(지금까지는 볼륨 조절이 메인 메뉴에서만
        // 가능했고 메인 메뉴에선 이 소스들이 재생 중이지 않아서 드러나지 않았던 gap). 슬라이더
        // 값이 바뀔 때마다 이 메서드를 호출해 현재 재생 중인 모든 소스에 즉시 재적용한다.
        public void RefreshVolumesFromSettings()
        {
            if (bgmSource != null && bgmSource.clip != null)
            {
                // PlayBossBattleBGM()과 동일한 0.75배 기준을 공유.
                bgmSource.volume = 0.75f * GameSettings.BgmVolume01;
            }
            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Value != null)
                {
                    // ActivateInstrumentAudio()와 동일한 0.85배 기준을 공유.
                    kvp.Value.volume = 0.85f * GameSettings.InstrumentVolume01;
                }
            }
        }
    }
}
