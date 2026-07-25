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
            
            CalculateStats();
        }

        public void UpgradeLevel()
        {
            level = Mathf.Min(5, level + 1);
            CalculateStats();
        }

        private void CalculateStats()
        {
            // Moderate scaling to prevent DPS explosion
            // Extra Damage: Lv 1..2: +0, Lv 3..4: +1, Lv 5: +2
            this.extraDamage = (level >= 5) ? 2 : ((level >= 3) ? 1 : 0);

            // Extra Projectiles: Lv 1..3: +0 (1 proj), Lv 4..5: +1 (2 proj max)
            this.extraProjectiles = (level >= 4) ? 1 : 0;

            this.scoreMultiplier = 1.0f + (level * 0.1f);
        }
    }
}
