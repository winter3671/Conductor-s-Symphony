using UnityEngine;
using ConductorSymphony.UI;

namespace ConductorSymphony.Player
{
    public class PlayerExperience : MonoBehaviour
    {
        public static PlayerExperience Instance { get; private set; }

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int MaxExp { get; private set; } = 40;

        public static event System.Action<int, int, int> OnExpChangedEvent; // level, currentExp, maxExp

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);

            if (LevelUpUI.Instance != null && Instrument.InstrumentManager.Instance != null && Instrument.InstrumentManager.Instance.AcquiredInstruments.Count == 0)
            {
                LevelUpUI.Instance.ShowLevelUpSelection(isGameStart: true);
            }
        }

        public void AddExp(int amount)
        {
            CurrentExp += amount;
            if (CurrentExp >= MaxExp)
            {
                LevelUp();
            }
            else
            {
                OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);
            }
        }

        private void LevelUp()
        {
            CurrentExp -= MaxExp;
            CurrentLevel++;
            // Exponential EXP scaling: Lv1->2: 40, Lv2->3: 55, Lv3->4: 76, Lv4->5: 105, etc.
            MaxExp = Mathf.RoundToInt(40f * Mathf.Pow(1.38f, CurrentLevel - 1));

            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);

            if (LevelUpUI.Instance != null)
            {
                LevelUpUI.Instance.ShowLevelUpSelection(isGameStart: false);
            }
        }
    }
}
