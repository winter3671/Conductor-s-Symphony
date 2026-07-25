using UnityEngine;

namespace ConductorSymphony.Instrument
{
    [System.Serializable]
    public class InstrumentInfo
    {
        public InstrumentType type;
        public string instrumentName;
        public Color themeColor;
        public int level;
        public int extraDamage;
        public int extraProjectiles;
        public float scoreMultiplier;

        public InstrumentInfo(InstrumentType type, int level = 1)
        {
            this.type = type;
            this.level = level;

            InstrumentDefinition def = InstrumentPatternDatabase.GetDefinition(type);
            this.instrumentName = def.name;
            this.themeColor = def.themeColor;
            
            // Calculate stats based on level
            this.extraDamage = level - 1;
            this.extraProjectiles = (level >= 3) ? (level >= 5 ? 2 : 1) : 0;
            this.scoreMultiplier = 1.0f + (level * 0.1f);
        }

        public void UpgradeLevel()
        {
            level = Mathf.Min(5, level + 1);
            extraDamage = level - 1;
            extraProjectiles = (level >= 3) ? (level >= 5 ? 2 : 1) : 0;
            scoreMultiplier = 1.0f + (level * 0.1f);
        }
    }
}
