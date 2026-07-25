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
            MaxExp = CurrentLevel * 40;

            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);

            if (LevelUpUI.Instance != null)
            {
                LevelUpUI.Instance.ShowLevelUpSelection(isGameStart: false);
            }
        }
    }
}
