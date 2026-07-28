using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Audio
{
    public class AudioLayerManager : MonoSingleton<AudioLayerManager>
    {
        [Header("Audio Delay Sync Offset")]
        [SerializeField] private float audioStartDelay = 2.5242f; // Fine-tuned +50ms offset so audio beat matches Perfect window

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource acquisitionSource;

        private Dictionary<InstrumentType, AudioClip> instrumentAcquisitionClips = new Dictionary<InstrumentType, AudioClip>();
        private Dictionary<InstrumentType, AudioClip> instrumentKeySounds = new Dictionary<InstrumentType, AudioClip>();

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

            if (acquisitionSource == null)
            {
                acquisitionSource = gameObject.AddComponent<AudioSource>();
                acquisitionSource.loop = true; // Continuous seamless looping throughout gameplay!
                acquisitionSource.playOnAwake = false;
                acquisitionSource.volume = 0.85f;
                acquisitionSource.pitch = 1.0f; // Fixed speed!
            }
        }

        private void LoadAudioClipsAndKeySounds()
        {
            // 1. Acquisition WAV audio tracks (Played ONCE when acquiring an instrument / starting game)
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
                        wave = Mathf.Sin(2 * Mathf.PI * freq * t);
                        break;
                    case SynthWaveType.Square:
                        wave = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * freq * t));
                        break;
                    case SynthWaveType.Sawtooth:
                        wave = 2f * (t * freq - Mathf.Floor(0.5f + t * freq));
                        break;
                    case SynthWaveType.Triangle:
                        wave = Mathf.PingPong(t * freq * 4f, 1f) * 2f - 1f;
                        break;
                    case SynthWaveType.Noise:
                        wave = Random.Range(-1f, 1f);
                        break;
                }

                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create($"KeySound_{freq}_{waveType}", lengthSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Called on EVERY beat note hit during gameplay. Plays a clean, crisp single-note key tap sound on sfxSource.
        /// </summary>
        public void PlayInstrumentKeySound(InstrumentType type, bool isPerfect)
        {
            if (instrumentKeySounds.TryGetValue(type, out AudioClip clip))
            {
                sfxSource.pitch = isPerfect ? 1.05f : 0.95f;
                sfxSource.PlayOneShot(clip, 0.7f);
            }
        }

        /// <summary>
        /// Called ONCE when an instrument is acquired (including game start with Drums).
        /// Plays the corresponding WAV audio track on a dedicated acquisitionSource (locked at 1.0x speed).
        /// </summary>
        public void ActivateInstrumentAudio(InstrumentType type)
        {
            if (instrumentAcquisitionClips.TryGetValue(type, out AudioClip clip) && clip != null)
            {
                if (acquisitionSource != null)
                {
                    acquisitionSource.pitch = 1.0f; // Always 1.0x normal speed!
                    acquisitionSource.clip = clip;
                    acquisitionSource.loop = true; // Seamless continuous looping!
                    acquisitionSource.PlayDelayed(audioStartDelay);
                }
            }
            else
            {
                PlayInstrumentKeySound(type, true);
            }
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
    }
}
