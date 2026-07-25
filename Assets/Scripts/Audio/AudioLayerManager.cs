using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Instrument;

namespace ConductorSymphony.Audio
{
    public class AudioLayerManager : MonoBehaviour
    {
        public static AudioLayerManager Instance { get; private set; }

        private AudioSource baseBgmSource;
        private AudioSource sfxSource;

        private Dictionary<InstrumentType, AudioClip> instrumentKeySounds = new Dictionary<InstrumentType, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            SetupBaseBgm();
            GenerateInstrumentKeySounds();
        }

        private void SetupBaseBgm()
        {
            baseBgmSource = gameObject.AddComponent<AudioSource>();
            baseBgmSource.clip = CreateMetronomeBeatClip();
            baseBgmSource.loop = true;
            baseBgmSource.volume = 0.35f;
            baseBgmSource.Play();
        }

        private AudioClip CreateMetronomeBeatClip()
        {
            int sampleRate = 44100;
            int lengthSamples = sampleRate * 2; // 2 sec (120 BPM = 4 beats)
            float[] samples = new float[lengthSamples];

            for (int beat = 0; beat < 4; beat++)
            {
                int startSample = beat * (sampleRate / 2);
                int pulseLen = sampleRate / 20; // 0.05s click

                float freq = (beat == 0) ? 800f : 400f; // High click on beat 1

                for (int i = 0; i < pulseLen && (startSample + i) < lengthSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1f - ((float)i / pulseLen);
                    samples[startSample + i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.15f;
                }
            }

            AudioClip clip = AudioClip.Create("MetronomeBeat", lengthSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void GenerateInstrumentKeySounds()
        {
            // Frequencies for 10 instruments
            instrumentKeySounds[InstrumentType.Drums]     = CreateSynthTone(120f, 0.15f, SynthWaveType.Noise);
            instrumentKeySounds[InstrumentType.Violin]    = CreateSynthTone(523.25f, 0.25f, SynthWaveType.Sawtooth); // C5
            instrumentKeySounds[InstrumentType.Flute]     = CreateSynthTone(659.25f, 0.2f, SynthWaveType.Sine);     // E5
            instrumentKeySounds[InstrumentType.Trumpet]   = CreateSynthTone(392.00f, 0.22f, SynthWaveType.Square);  // G4
            instrumentKeySounds[InstrumentType.Guitar]    = CreateSynthTone(220.00f, 0.25f, SynthWaveType.Square);  // A3
            instrumentKeySounds[InstrumentType.Piano]     = CreateSynthTone(440.00f, 0.3f, SynthWaveType.Triangle); // A4
            instrumentKeySounds[InstrumentType.Cello]     = CreateSynthTone(130.81f, 0.35f, SynthWaveType.Sawtooth); // C3
            instrumentKeySounds[InstrumentType.Saxophone] = CreateSynthTone(349.23f, 0.25f, SynthWaveType.Square);  // F4
            instrumentKeySounds[InstrumentType.Harp]      = CreateSynthTone(880.00f, 0.2f, SynthWaveType.Sine);     // A5
            instrumentKeySounds[InstrumentType.Xylophone] = CreateSynthTone(1046.50f, 0.15f, SynthWaveType.Triangle);// C6
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
                float env = Mathf.Exp(-5.0f * (t / duration)); // Exponential decay envelope
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

                samples[i] = wave * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create($"KeySound_{freq}_{waveType}", lengthSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayInstrumentKeySound(InstrumentType type, bool isPerfect)
        {
            if (instrumentKeySounds.TryGetValue(type, out AudioClip clip))
            {
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(clip, 0.8f);
            }
        }

        public void ActivateInstrumentAudio(InstrumentType type)
        {
            // Play a celebratory chime when a new instrument is acquired
            PlayInstrumentKeySound(type, true);
        }
    }
}
