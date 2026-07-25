using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Audio;
using ConductorSymphony.Player;

namespace ConductorSymphony.Instrument
{
    public class InstrumentManager : MonoBehaviour
    {
        public static InstrumentManager Instance { get; private set; }

        [SerializeField] private int maxSlots = 4;

        private List<InstrumentInfo> acquiredInstruments = new List<InstrumentInfo>();
        private List<InstrumentOrbit> activeOrbits = new List<InstrumentOrbit>();
        private Transform playerTransform;

        private Texture2D orbitTexture;
        private Sprite orbitSprite;

        public List<InstrumentInfo> AcquiredInstruments => acquiredInstruments;

        public static event System.Action<List<InstrumentInfo>> OnInstrumentsChangedEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateOrbitSprite();
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void CreateOrbitSprite()
        {
            int size = 24;
            orbitTexture = new Texture2D(size, size);
            Color[] px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 10f)
                    {
                        px[y * size + x] = Color.white;
                    }
                    else
                    {
                        px[y * size + x] = Color.clear;
                    }
                }
            }
            orbitTexture.SetPixels(px);
            orbitTexture.Apply();
            orbitSprite = Sprite.Create(orbitTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public bool HasInstrument(InstrumentType type)
        {
            return acquiredInstruments.Exists(x => x.type == type);
        }

        public int GetInstrumentLevel(InstrumentType type)
        {
            InstrumentInfo info = acquiredInstruments.Find(x => x.type == type);
            return info != null ? info.level : 0;
        }

        public bool AcquireOrUpgradeInstrument(InstrumentType type)
        {
            InstrumentInfo existing = acquiredInstruments.Find(x => x.type == type);
            if (existing != null)
            {
                // Upgrade existing instrument
                existing.UpgradeLevel();
            }
            else
            {
                // Acquire new instrument if slots < maxSlots
                if (acquiredInstruments.Count >= maxSlots) return false;

                InstrumentInfo newInfo = new InstrumentInfo(type, 1);
                acquiredInstruments.Add(newInfo);

                if (playerTransform == null)
                {
                    PlayerController player = FindAnyObjectByType<PlayerController>();
                    if (player != null) playerTransform = player.transform;
                }

                GameObject companionObj = new GameObject($"Companion_{type}_{acquiredInstruments.Count}");
                InstrumentOrbit orbit = companionObj.AddComponent<InstrumentOrbit>();
                int slot = activeOrbits.Count;
                orbit.Initialize(type, playerTransform, slot, orbitSprite, newInfo.themeColor);
                activeOrbits.Add(orbit);

                RealignOrbits();

                if (AudioLayerManager.Instance != null)
                {
                    AudioLayerManager.Instance.ActivateInstrumentAudio(type);
                }
            }

            OnInstrumentsChangedEvent?.Invoke(acquiredInstruments);
            return true;
        }

        private void RealignOrbits()
        {
            int count = activeOrbits.Count;
            for (int i = 0; i < count; i++)
            {
                activeOrbits[i].SetSlotIndex(i);
            }
        }

        public int GetTotalExtraDamage()
        {
            int total = 0;
            foreach (var inst in acquiredInstruments)
            {
                total += inst.extraDamage;
            }
            return total;
        }

        public int GetTotalExtraProjectiles()
        {
            int total = 0;
            foreach (var inst in acquiredInstruments)
            {
                total += inst.extraProjectiles;
            }
            return total;
        }
    }
}
