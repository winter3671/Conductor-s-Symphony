using System.Collections.Generic;
using UnityEngine;
using ConductorSymphony.Audio;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Instrument
{
    public class InstrumentManager : MonoSingleton<InstrumentManager>
    {
        private List<InstrumentInfo> acquiredInstruments = new List<InstrumentInfo>();
        private List<InstrumentOrbit> activeOrbits = new List<InstrumentOrbit>();
        private Transform playerTransform;

        private Sprite orbitSprite;

        public List<InstrumentInfo> AcquiredInstruments => acquiredInstruments;

        public static event System.Action<List<InstrumentInfo>> OnInstrumentsChangedEvent;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            orbitSprite = ProceduralSpriteFactory.CreateFilledCircle(24, 10f, Color.white);
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }
        }

        public int GetUnlockedSlotsCount()
        {
            int pLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
            if (pLevel < 5) return 2;      // Lv 1 ~ 4: 2 slots (Q & R)
            else if (pLevel < 8) return 3; // Lv 5 ~ 7: 3 slots (Q, W, R)
            else return 4;                // Lv 8+: 4 slots (Q, W, E, R)
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
                // Acquire new instrument if slots < maxUnlockedSlots
                int maxUnlocked = GetUnlockedSlotsCount();
                if (acquiredInstruments.Count >= maxUnlocked) return false;

                InstrumentInfo newInfo = new InstrumentInfo(type, 1);
                acquiredInstruments.Add(newInfo);

                if (playerTransform == null && PlayerController.Instance != null)
                {
                    playerTransform = PlayerController.Instance.transform;
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
