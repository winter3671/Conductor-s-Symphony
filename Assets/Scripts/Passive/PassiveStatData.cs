using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Passive
{
    // game_balance_design.docx section 4: 인게임 패시브 스탯 (장신구) 8종
    public enum PassiveStatType
    {
        Sforzando, // 시포르찬도 - 위력 (모든 무기 피해량)
        Allegro,   // 알레그로 - 템포 (공격 속도/쿨타임 감축)
        Crescendo, // 크레센도 - 확장 (모든 공격 범위)
        Vivace,    // 비바체 - 기동성 (캐릭터 이동 속도)
        Legato,    // 레가토 - 연결 (투사체 수)
        Fermata,   // 페르마타 - 지속 (장판 및 지속 효과 유효 시간)
        Resonance, // 공명 패널 - 자석 (EXP 구슬 획득 범위)
        Tuning     // 악보 튜닝 - 방어 (피해 감소 & 최대 HP)
    }

    public class PassiveStatDefinition
    {
        public PassiveStatType type;
        public string name;
        public string theme;
        public Color themeColor;
        public string description; // 레벨당 효과 요약 (UI 카드 표시용)

        public PassiveStatDefinition(PassiveStatType type, string name, string theme, Color themeColor, string description)
        {
            this.type = type;
            this.name = name;
            this.theme = theme;
            this.themeColor = themeColor;
            this.description = description;
        }
    }

    public static class PassiveStatDatabase
    {
        public const int MaxLevel = 5;

        private static readonly Dictionary<PassiveStatType, PassiveStatDefinition> database = new Dictionary<PassiveStatType, PassiveStatDefinition>
        {
            { PassiveStatType.Sforzando, new PassiveStatDefinition(PassiveStatType.Sforzando, "Sforzando", "위력",
                new Color(0.9f, 0.25f, 0.25f), "모든 무기 피해량 +10%/Lv (최대 +50%)") },

            { PassiveStatType.Allegro, new PassiveStatDefinition(PassiveStatType.Allegro, "Allegro", "템포",
                new Color(0.95f, 0.75f, 0.2f), "공격 속도/쿨타임 감축 +6%/Lv (최대 +30%)") },

            { PassiveStatType.Crescendo, new PassiveStatDefinition(PassiveStatType.Crescendo, "Crescendo", "확장",
                new Color(0.4f, 0.7f, 0.95f), "모든 공격 범위 +10%/Lv (최대 +50%)") },

            { PassiveStatType.Vivace, new PassiveStatDefinition(PassiveStatType.Vivace, "Vivace", "기동성",
                new Color(0.4f, 0.9f, 0.5f), "캐릭터 이동 속도 +8%/Lv (최대 +40%)") },

            { PassiveStatType.Legato, new PassiveStatDefinition(PassiveStatType.Legato, "Legato", "연결",
                new Color(0.75f, 0.5f, 0.9f), "투사체 수 +1 (Lv3, Lv5 적용, 최대 +2개)") },

            { PassiveStatType.Fermata, new PassiveStatDefinition(PassiveStatType.Fermata, "Fermata", "지속",
                new Color(0.9f, 0.6f, 0.85f), "장판 및 지속 효과 유효 시간 +15%/Lv (최대 +75%)") },

            { PassiveStatType.Resonance, new PassiveStatDefinition(PassiveStatType.Resonance, "Resonance", "자석",
                new Color(0.3f, 0.85f, 0.8f), "EXP 구슬 획득 범위 +25%/Lv (최대 +125%)") },

            { PassiveStatType.Tuning, new PassiveStatDefinition(PassiveStatType.Tuning, "Tuning", "방어",
                new Color(0.6f, 0.6f, 0.65f), "피해 감소 +5%/Lv & 최대 HP +10%/Lv (최대 피해감소 25% / HP +50%)") },
        };

        public static PassiveStatDefinition GetDefinition(PassiveStatType type)
        {
            return database[type];
        }

        public static IEnumerable<PassiveStatType> AllTypes => database.Keys;
    }

    [System.Serializable]
    public class PassiveStatInfo
    {
        public PassiveStatType type;
        public int level;

        public PassiveStatInfo(PassiveStatType type, int level = 1)
        {
            this.type = type;
            this.level = level;
        }

        public void UpgradeLevel()
        {
            level = Mathf.Min(PassiveStatDatabase.MaxLevel, level + 1);
        }
    }
}
