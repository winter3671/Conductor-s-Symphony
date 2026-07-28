using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Instrument
{
    public enum InstrumentType
    {
        Drums,
        Piano,
        Violin,
        Flute,
        FrenchHorn,
        Glockenspiel,
        Cello,
        Timpani,
        Marimba,
        Bell
    }

    public enum InstrumentGroup
    {
        RhythmPercussion,
        MelodyHarmony
    }

    public class InstrumentDefinition
    {
        public InstrumentType type;
        public string name;
        public Color themeColor;
        public string description;
        public List<int[]> levelPatterns;

        public InstrumentDefinition(InstrumentType type, string name, Color themeColor, string description, List<int[]> levelPatterns)
        {
            this.type = type;
            this.name = name;
            this.themeColor = themeColor;
            this.description = description;
            this.levelPatterns = levelPatterns;
        }
    }

    public static class InstrumentPatternDatabase
    {
        private static Dictionary<InstrumentType, InstrumentDefinition> database;

        static InstrumentPatternDatabase()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            database = new Dictionary<InstrumentType, InstrumentDefinition>();

            // 1. Drums (Slot 0 - Q: Downbeats every 8 steps = 1 per 2.474s measure)
            database[InstrumentType.Drums] = new InstrumentDefinition(
                InstrumentType.Drums, "Drums", new Color(0.9f, 0.3f, 0.3f), "360° Shockwave Beat Bang",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (Step 0, 8, 16, 24 = 4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 3 (4 hits)
                    ParsePattern("10000000100000001000000010000001"), // Lv 4 (4 hits + pickup)
                    ParsePattern("10000000100010001000000010001001")  // Lv 5 MAX (6 hits)
                }
            );

            // 2. Piano (Slot 1 - R: Crisp chord taps)
            database[InstrumentType.Piano] = new InstrumentDefinition(
                InstrumentType.Piano, "Piano", new Color(0.9f, 0.9f, 0.9f), "Auto-Target Chord Laser & Piano Cascade Volley",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits)
                    ParsePattern("10000000100000001000000010001000"), // Lv 3 (5 hits)
                    ParsePattern("10001000100010001000100010001000"), // Lv 4 (8 hits)
                    ParsePattern("10001000100010001000100010001001")  // Lv 5 MAX (9 hits)
                }
            );

            // 3. Violin (Slot 2 - W: Clean arc melody taps)
            database[InstrumentType.Violin] = new InstrumentDefinition(
                InstrumentType.Violin, "Violin", new Color(1.0f, 0.5f, 0.2f), "Orbiting Blades & Crescent Arc Slash",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000000000001000000000000000"), // Lv 2 (2 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 3 (4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 4 (4 hits)
                    ParsePattern("10001000100010001000100010001000")  // Lv 5 MAX (8 hits)
                }
            );

            // 4. Flute (Slot 3 - E: Woodwind swell taps)
            database[InstrumentType.Flute] = new InstrumentDefinition(
                InstrumentType.Flute, "Flute", new Color(0.2f, 0.9f, 1.0f), "Mini Vortex (Release Pull) & Woodwind Swells",
                new List<int[]> {
                    ParsePattern("00100000000000000010000000000000"), // Lv 1 (2 hits: Step 2, 18)
                    ParsePattern("00100000000000000010000000000000"), // Lv 2 (2 hits)
                    ParsePattern("00100000001000000010000000100000"), // Lv 3 (4 hits)
                    ParsePattern("00100000001000000010000000100000"), // Lv 4 (4 hits)
                    ParsePattern("00100000001000000010000000100000")  // Lv 5 MAX (4 hits)
                }
            );

            // 5. French Horn (Sonic Brass Cannon)
            database[InstrumentType.FrenchHorn] = new InstrumentDefinition(
                InstrumentType.FrenchHorn, "FrenchHorn", new Color(1.0f, 0.85f, 0.2f), "Sonic Brass Cannon Cone Knockback",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000000000001000000000000000"), // Lv 2 (2 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 3 (4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 4 (4 hits)
                    ParsePattern("10000000100000001000000010000000")  // Lv 5 MAX (4 hits)
                }
            );

            // 6. Glockenspiel (Star Fall & Gentle Chimes)
            database[InstrumentType.Glockenspiel] = new InstrumentDefinition(
                InstrumentType.Glockenspiel, "Glockenspiel", new Color(0.4f, 1.0f, 0.8f), "Star Fall on Highest HP Enemy",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits)
                    ParsePattern("10000000100000001000000010001000"), // Lv 3 (5 hits)
                    ParsePattern("10000000100000001000000010001000"), // Lv 4 (5 hits)
                    ParsePattern("10000000100000001000000010001001")  // Lv 5 MAX (6 hits)
                }
            );

            // 7. Cello (Deep Bass Gravity Binding)
            database[InstrumentType.Cello] = new InstrumentDefinition(
                InstrumentType.Cello, "Cello", new Color(0.6f, 0.3f, 0.1f), "Gravity Binding Slow Zone",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000000000001000000000000000"), // Lv 2 (2 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 3 (4 hits)
                    ParsePattern("10000000100000001000000010000000"), // Lv 4 (4 hits)
                    ParsePattern("10000000100000001000000010000000")  // Lv 5 MAX (4 hits)
                }
            );

            // 8. Timpani (Timpani Cannon Mortar Impact)
            database[InstrumentType.Timpani] = new InstrumentDefinition(
                InstrumentType.Timpani, "Timpani", new Color(0.7f, 0.4f, 0.2f), "Timpani Cannon Mortar Impact",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits)
                    ParsePattern("10000000100000001000000010001000"), // Lv 3 (5 hits)
                    ParsePattern("10000000100000001000000010001000"), // Lv 4 (5 hits)
                    ParsePattern("10000000100000001000000010001001")  // Lv 5 MAX (6 hits)
                }
            );

            // 9. Marimba (Wood Ricochet Wave)
            database[InstrumentType.Marimba] = new InstrumentDefinition(
                InstrumentType.Marimba, "Marimba", new Color(0.9f, 0.6f, 0.2f), "Off-Beat Marimba Ricochet Wave",
                new List<int[]> {
                    ParsePattern("00100000000000000010000000000000"), // Lv 1 (2 hits: Step 2, 18)
                    ParsePattern("00100000000000000010000000000000"), // Lv 2 (2 hits)
                    ParsePattern("00100000001000000010000000100000"), // Lv 3 (4 hits)
                    ParsePattern("00100000001000000010000000100000"), // Lv 4 (4 hits)
                    ParsePattern("00100000001000000010000000100000")  // Lv 5 MAX (4 hits)
                }
            );

            // 10. Bell (8-Direction Starlight Burst)
            database[InstrumentType.Bell] = new InstrumentDefinition(
                InstrumentType.Bell, "Bell", new Color(0.9f, 1.0f, 0.3f), "8-Direction Starlight Burst",
                new List<int[]> {
                    ParsePattern("00001000000000000000100000000000"), // Lv 1 (2 hits: Step 4, 20)
                    ParsePattern("00001000000000000000100000000000"), // Lv 2 (2 hits)
                    ParsePattern("00001000000010000000100000001000"), // Lv 3 (4 hits)
                    ParsePattern("00001000000010000000100000001000"), // Lv 4 (4 hits)
                    ParsePattern("00001000000010000000100000001000")  // Lv 5 MAX (4 hits)
                }
            );
        }

        private static int[] ParsePattern(string str)
        {
            int[] pattern = new int[32];
            for (int i = 0; i < Mathf.Min(32, str.Length); i++)
            {
                pattern[i] = (str[i] == '1') ? 1 : 0;
            }
            return pattern;
        }

        public static InstrumentDefinition GetDefinition(InstrumentType type)
        {
            if (database.TryGetValue(type, out var def)) return def;
            return null;
        }

        public static int[] GetPattern(InstrumentType type, int level)
        {
            var def = GetDefinition(type);
            if (def != null && def.levelPatterns != null)
            {
                int index = Mathf.Clamp(level - 1, 0, def.levelPatterns.Count - 1);
                return def.levelPatterns[index];
            }
            return new int[32];
        }

        public static InstrumentGroup GetGroup(InstrumentType type)
        {
            switch (type)
            {
                case InstrumentType.Drums:
                case InstrumentType.Timpani:
                case InstrumentType.Glockenspiel:
                case InstrumentType.Bell:
                case InstrumentType.Marimba:
                    return InstrumentGroup.RhythmPercussion;

                case InstrumentType.Piano:
                case InstrumentType.Violin:
                case InstrumentType.Cello:
                case InstrumentType.Flute:
                case InstrumentType.FrenchHorn:
                default:
                    return InstrumentGroup.MelodyHarmony;
            }
        }
    }
}
