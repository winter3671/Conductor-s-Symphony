using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;
using ConductorSymphony.Utility;

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

        /// <summary>
        /// The current playback position of the song, in seconds, derived directly from the
        /// master AudioSource's actual sample position (or, before playback truly starts, from
        /// the audio engine's own dspTime clock). This is the single master clock that
        /// RhythmManager/RhythmNote/ShrinkingRhythmRing read every frame instead of Time.time,
        /// so there is no second independently-advancing clock that can drift out of sync with
        /// the audio across a pause/resume cycle.
        /// Returns a negative value while the master track is scheduled but not yet audible
        /// (pre-roll), and 0 if no track has been scheduled at all yet.
        /// </summary>
        public float SongTime
        {
            get
            {
                AudioSource src = GetAnyActiveInstrumentSource();
                if (src != null && src.clip != null && src.timeSamples > 0)
                {
                    return src.timeSamples / (float)src.clip.frequency;
                }
                if (masterStartDspTime > 0.0)
                {
                    return (float)(AudioSettings.dspTime - masterStartDspTime);
                }
                return -1f;
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
                sfxSource.volume = 0.8f;
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
                samples[i] = wave * env * 0.7f;
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
                    sfxSource.PlayOneShot(clip);
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
                source.volume = 0.85f;
                source.pitch = 1.0f; // Fixed speed!
                activeInstrumentSources[type] = source;
            }

            source.clip = clip;

            // Find an active reference source to synchronize timeSamples
            AudioSource referenceSource = GetActiveReferenceSource(excludeType: type);

            if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
            {
                source.timeSamples = referenceSource.timeSamples % clip.samples;
                source.Play();
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

        public void PlayBossBattleBGM()
        {
            AudioClip bossBgm = Resources.Load<AudioClip>("Audio/BGM_BossBattle");
            if (bossBgm != null && bgmSource != null)
            {
                bgmSource.clip = bossBgm;
                bgmSource.Play();
            }
        }

        public void PauseAllAudio()
        {
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
            foreach (var kvp in activeInstrumentSources)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.UnPause();
                }
            }
            if (bgmSource != null)
            {
                bgmSource.UnPause();
            }
        }
    }
}
