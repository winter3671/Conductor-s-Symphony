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

            // Automatically equip Drums as default starting instrument on Slot 0 (Q)
            if (!HasInstrument(InstrumentType.Drums))
            {
                AcquireOrUpgradeInstrument(InstrumentType.Drums);
            }
        }

        public int GetUnlockedSlotsCount()
        {
            // game_balance_design.docx section 2: Lv1(드럼 고정 시작)=1슬롯, Lv5=2번째, Lv10=3번째, Lv15=4번째(풀세팅)
            int pLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
            if (pLevel < 5) return 1;        // Lv 1 ~ 4: 1 slot (Q, 드럼 고정)
            else if (pLevel < 10) return 2;  // Lv 5 ~ 9: 2 slots (Q & R)
            else if (pLevel < 15) return 3;  // Lv 10 ~ 14: 3 slots (Q, R, W)
            else return 4;                  // Lv 15+: 4 slots (Q, R, W, E)
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

        // (2026-08-07) 예전엔 여기 GetTotalExtraDamage()/GetTotalExtraProjectiles()가 장착된 4슬롯
        // 전체를 합산해 RhythmAttackManager.HandleRhythmHit()에 넘겼는데, "판정된 그 악기만의 값을
        // 써야 한다"고 정정되면서 이 두 메서드는 더 이상 쓰이지 않는다(호출부가 InstrumentInfo.
        // extraDamage/extraProjectiles를 직접 읽도록 변경됨). 죽은 코드라 삭제함 - game_systems_reference.md
        // §7-1 참고.
    }
}
