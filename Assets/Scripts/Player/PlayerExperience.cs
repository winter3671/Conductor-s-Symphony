using UnityEngine;
using ConductorSymphony.Utility;

namespace ConductorSymphony.Player
{
    public class PlayerExperience : MonoSingleton<PlayerExperience>
    {
        // Cumulative EXP required per level-up (Lv1->2 ... Lv19->20), per game_balance_design.docx section 2.
        // Arithmetic progression per phase, matching the doc's per-level min~max range and phase total exactly:
        //   Lv 1~5   (4 steps, 250~350/lv):   250, 283, 317, 350            => 1,200 EXP
        //   Lv 5~10  (5 steps, 500~700/lv):   500, 550, 600, 650, 700       => 3,000 EXP
        //   Lv 10~15 (5 steps, 1000~1400/lv): 1000,1100,1200,1300,1400      => 6,000 EXP
        //   Lv 15~20 (5 steps, 2000~2800/lv): 2000,2200,2400,2600,2800      => 12,000 EXP
        // Total across the 10-minute mob phase: 22,200 EXP (matches doc's grand total).
        private static readonly int[] ExpToNextLevel = new int[]
        {
            250, 283, 317, 350,
            500, 550, 600, 650, 700,
            1000, 1100, 1200, 1300, 1400,
            2000, 2200, 2400, 2600, 2800
        };

        public const int MaxLevel = 20;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int MaxExp { get; private set; } = ExpToNextLevel[0];

        public static event System.Action<int, int, int> OnExpChangedEvent; // level, currentExp, maxExp
        public static event System.Action<bool> OnLevelUpEvent; // isGameStart

        private void Start()
        {
            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);
        }

        public void AddExp(int amount)
        {
            if (CurrentLevel >= MaxLevel) return; // Lv20 만렙 도달: 잡몹전 EXP 획득 종료 (보스전은 EnemySpawner의 절대 시간 10:00 기준으로 별도 트리거됨)

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

            if (CurrentLevel >= MaxLevel)
            {
                CurrentExp = 0;
                MaxExp = 0; // 만렙: 더 이상 게이지가 필요 없음
            }
            else
            {
                MaxExp = ExpToNextLevel[CurrentLevel - 1]; // e.g. CurrentLevel==2 -> index1 -> Lv2->3에 필요한 EXP
            }

            OnExpChangedEvent?.Invoke(CurrentLevel, CurrentExp, MaxExp);

            OnLevelUpEvent?.Invoke(false);
        }
    }
}
