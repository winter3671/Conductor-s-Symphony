using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Instrument
{
    public enum InstrumentType
    {
        Drums = 0,
        Piano = 1,
        Violin = 2,
        Flute = 3,
        FrenchHorn = 4,
        Glockenspiel = 5,
        Cello = 6,
        Timpani = 7,
        Marimba = 8,
        Bell = 9
    }

    [System.Serializable]
    public class InstrumentDefinition
    {
        public InstrumentType type;
        public string name;
        public Color themeColor;
        public string description;
        public List<int[]> patternByLevel;

        public InstrumentDefinition(InstrumentType type, string name, Color color, string desc, List<int[]> patterns)
        {
            this.type = type;
            this.name = name;
            this.themeColor = color;
            this.description = desc;
            this.patternByLevel = patterns;
        }
    }

    public static class InstrumentPatternDatabase
    {
        public static InstrumentGroup GetGroup(InstrumentType type)
        {
            switch (type)
            {
                case InstrumentType.Drums:
                case InstrumentType.Cello:
                case InstrumentType.Timpani:
                    return InstrumentGroup.GroupA_Downbeat;

                case InstrumentType.Piano:
                case InstrumentType.Marimba:
                    return InstrumentGroup.GroupB_Offbeat;

                case InstrumentType.Flute:
                case InstrumentType.FrenchHorn:
                    return InstrumentGroup.GroupC_Midbeat;

                case InstrumentType.Violin:
                case InstrumentType.Glockenspiel:
                case InstrumentType.Bell:
                default:
                    return InstrumentGroup.GroupD_Upbeat;
            }
        }

        private static Dictionary<InstrumentType, InstrumentDefinition> database;

        static InstrumentPatternDatabase()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            database = new Dictionary<InstrumentType, InstrumentDefinition>();

            // 1. Drums (1 Downbeat per 2.474s Measure = every 8 steps, matching noteTravelDuration)
            database[InstrumentType.Drums] = new InstrumentDefinition(
                InstrumentType.Drums, "Drums", new Color(0.9f, 0.3f, 0.3f), "360° Shockwave Beat Bang",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (1 hit per 2.474s measure = Step 0, 8, 16, 24)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2
                    ParsePattern("10000000100000001000000010000000"), // Lv 3
                    ParsePattern("10000000100000001000000010000001"), // Lv 4 (base beat + pickup hit before loop)
                    ParsePattern("10000000100010001000000010001001")  // Lv 5 MAX (base beat + syncopated accents)
                }
            );

            // 2. Piano (Rapid Chord Taps & Bar 4 Cascade)
            database[InstrumentType.Piano] = new InstrumentDefinition(
                InstrumentType.Piano, "Piano", new Color(0.9f, 0.9f, 0.9f), "Auto-Target Chord Laser & Piano Cascade Volley",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1
                    ParsePattern("10001000100010001000100010001000"), // Lv 2
                    ParsePattern("10001000100010001000100011111100"), // Lv 3 (Bar 4 6-tap cascade)
                    ParsePattern("10011001100110011001100111111100"), // Lv 4
                    ParsePattern("11011011101110111101101111111100")  // Lv 5 MAX
                }
            );

            // 3. Violin (13-Step Long Note Hold & Release Arc Slash)
            database[InstrumentType.Violin] = new InstrumentDefinition(
                InstrumentType.Violin, "Violin", new Color(1.0f, 0.5f, 0.2f), "Orbiting Blades (Hold) & Crescent Arc Slash (Release)",
                new List<int[]> {
                    ParsePattern("11111111111110001111111111111000"), // Lv 1 (13-step hold + 3-step rest release)
                    ParsePattern("11111111111110001111111111111000"), // Lv 2
                    ParsePattern("11111111111110001111111111111000"), // Lv 3
                    ParsePattern("11111111111110001111111111111000"), // Lv 4
                    ParsePattern("11111111111110001111111111111000")  // Lv 5 MAX
                }
            );

            // 4. Flute (Short Hold Swells & Mini Vortex Pull)
            database[InstrumentType.Flute] = new InstrumentDefinition(
                InstrumentType.Flute, "Flute", new Color(0.2f, 0.9f, 1.0f), "Mini Vortex (Release Pull) & Woodwind Swells",
                new List<int[]> {
                    ParsePattern("00111000001110000011100000111000"), // Lv 1 (3-step short holds)
                    ParsePattern("00111000001110000011100000111000"), // Lv 2
                    ParsePattern("00111000001110000011100000111000"), // Lv 3
                    ParsePattern("00111000001110000011100000111000"), // Lv 4
                    ParsePattern("00111000001110000011100000111000")  // Lv 5 MAX
                }
            );

            // 5. French Horn (6-Step Swell Long Note & Sonic Brass Cannon)
            database[InstrumentType.FrenchHorn] = new InstrumentDefinition(
                InstrumentType.FrenchHorn, "FrenchHorn", new Color(1.0f, 0.85f, 0.2f), "Sonic Brass Cannon Cone Knockback & Swell Hold",
                new List<int[]> {
                    ParsePattern("11111100000000001111110000000000"), // Lv 1 (6-step swells)
                    ParsePattern("11111100000000001111110000000000"), // Lv 2
                    ParsePattern("11111100000000001111110000000000"), // Lv 3
                    ParsePattern("11111100000000001111110000000000"), // Lv 4
                    ParsePattern("11111100000000001111110000000000")  // Lv 5 MAX
                }
            );

            // 6. Glockenspiel (Star Fall & Bar 4 Finale Burst)
            database[InstrumentType.Glockenspiel] = new InstrumentDefinition(
                InstrumentType.Glockenspiel, "Glockenspiel", new Color(0.4f, 1.0f, 0.8f), "Star Fall on Highest HP Enemy & Finale Star Burst",
                new List<int[]> {
                    ParsePattern("10000000100000001000000011111111"), // Lv 1 (Starlight taps + 8-step finale burst)
                    ParsePattern("10000000100000001000000011111111"), // Lv 2
                    ParsePattern("10000000100000001000000011111111"), // Lv 3
                    ParsePattern("10000000100000001000000011111111"), // Lv 4
                    ParsePattern("10000000100000001000000011111111")  // Lv 5 MAX
                }
            );

            // 7. Cello (13-Step Deep Bass Long Note & Gravity Binding)
            database[InstrumentType.Cello] = new InstrumentDefinition(
                InstrumentType.Cello, "Cello", new Color(0.6f, 0.3f, 0.1f), "Gravity Binding Slow Zone (13-step Deep Bass Hold)",
                new List<int[]> {
                    ParsePattern("11111111111110001111111111111000"), // Lv 1 (13-step hold + 3-step rest)
                    ParsePattern("11111111111110001111111111111000"), // Lv 2
                    ParsePattern("11111111111110001111111111111000"), // Lv 3
                    ParsePattern("11111111111110001111111111111000"), // Lv 4
                    ParsePattern("11111111111110001111111111111000")  // Lv 5 MAX
                }
            );

            // 8. Timpani (Timpani Cannon & Roll Carpet Bomb)
            database[InstrumentType.Timpani] = new InstrumentDefinition(
                InstrumentType.Timpani, "Timpani", new Color(0.7f, 0.4f, 0.2f), "Timpani Cannon Mortar Impact & 16-Bar Roll Carpet Bomb",
                new List<int[]> {
                    ParsePattern("10000000100000001000000011111111"), // Lv 1 (Mortar taps + 13-step roll hold)
                    ParsePattern("10000000100000001000000011111111"), // Lv 2
                    ParsePattern("10000000100000001000000011111111"), // Lv 3
                    ParsePattern("10000000100000001000000011111111"), // Lv 4
                    ParsePattern("10000000100000001000000011111111")  // Lv 5 MAX
                }
            );

            // 9. Marimba (Steps 3 & 11 Off-beat Wood Ricochet)
            database[InstrumentType.Marimba] = new InstrumentDefinition(
                InstrumentType.Marimba, "Marimba", new Color(0.9f, 0.6f, 0.2f), "Off-Beat Marimba Ricochet Wave (Steps 3 & 11 Taps)",
                new List<int[]> {
                    ParsePattern("00100000001000000010000000100000"), // Lv 1 (Steps 3 & 11 offbeat taps)
                    ParsePattern("00100000001000000010000000100000"), // Lv 2
                    ParsePattern("00100000001000000010000000100000"), // Lv 3
                    ParsePattern("00100000001000000010000000100000"), // Lv 4
                    ParsePattern("00100000001000000010000000100000")  // Lv 5 MAX
                }
            );

            // 10. Bell (Steps 5 & 13 8-Direction Starlight Burst)
            database[InstrumentType.Bell] = new InstrumentDefinition(
                InstrumentType.Bell, "Bell", new Color(0.9f, 1.0f, 0.3f), "8-Direction Starlight Burst (Steps 5 & 13 Accent Taps)",
                new List<int[]> {
                    ParsePattern("00001000000010000000100000001000"), // Lv 1 (Steps 5 & 13 accent taps)
                    ParsePattern("00001000000010000000100000001000"), // Lv 2
                    ParsePattern("00001000000010000000100000001000"), // Lv 3
                    ParsePattern("00001000000010000000100000001000"), // Lv 4
                    ParsePattern("00001000000010000000100000001000")  // Lv 5 MAX
                }
            );
        }

        private static int[] ParsePattern(string str)
        {
            int[] pattern = new int[32];
            for (int i = 0; i < 32 && i < str.Length; i++)
            {
                pattern[i] = (str[i] == '1') ? 1 : 0;
            }
            return pattern;
        }

        public static InstrumentDefinition GetDefinition(InstrumentType type)
        {
            if (database.TryGetValue(type, out var def)) return def;
            return database[InstrumentType.Drums];
        }

        public static int[] GetPattern(InstrumentType type, int level)
        {
            InstrumentDefinition def = GetDefinition(type);
            int clampedLevel = Mathf.Clamp(level - 1, 0, def.patternByLevel.Count - 1);
            return def.patternByLevel[clampedLevel];
        }
    }

    public enum InstrumentGroup
    {
        GroupA_Downbeat,
        GroupB_Offbeat,
        GroupC_Midbeat,
        GroupD_Upbeat
    }
}
