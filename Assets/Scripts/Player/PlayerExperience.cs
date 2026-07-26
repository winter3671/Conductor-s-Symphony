using UnityEngine;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Player
{
    public class PlayerExperience : MonoSingleton<PlayerExperience>
    {
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int MaxExp { get; private set; } = 40;

        public static event System.Action<int, int, int> OnExpChangedEvent; // level, currentExp, maxExp
        public static event System.Action<bool> OnLevelUpEvent; // isGameStart

        private void Start()
        {
            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);

            if (Instrument.InstrumentManager.Instance != null && Instrument.InstrumentManager.Instance.AcquiredInstruments.Count == 0)
            {
                OnLevelUpEvent?.Invoke(true);
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

            OnLevelUpEvent?.Invoke(false);
        }
    }
}
