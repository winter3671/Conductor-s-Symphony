using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Instrument
{
    public enum InstrumentType
    {
        Drums = 0,
        Violin = 1,
        Flute = 2,
        Trumpet = 3,
        Guitar = 4,
        Piano = 5,
        Cello = 6,
        Saxophone = 7,
        Harp = 8,
        Xylophone = 9
    }

    [System.Serializable]
    public class InstrumentDefinition
    {
        public InstrumentType type;
        public string name;
        public Color themeColor;
        public string description;
        public List<int[]> patternByLevel; // Lv 1..5 (32 bits array each)

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
        private static Dictionary<InstrumentType, InstrumentDefinition> database;

        static InstrumentPatternDatabase()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            database = new Dictionary<InstrumentType, InstrumentDefinition>();

            // 1. Drums (A-key) - Balanced 4-bar groove (Lv1: 4 hits, Lv2: 6 hits, Lv3: 8 hits, Lv4: 10 hits, Lv5: 12 hits)
            database[InstrumentType.Drums] = new InstrumentDefinition(
                InstrumentType.Drums, "Drums", new Color(0.9f, 0.3f, 0.3f), "Groovy 4-bar drum beat",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (4 hits - 1/bar)
                    ParsePattern("10000000100010001000000000101000"), // Lv 2 (6 hits)
                    ParsePattern("10000100100000001000010000101000"), // Lv 3 (8 hits)
                    ParsePattern("10000100100010001000010001101000"), // Lv 4 (10 hits)
                    ParsePattern("10001010100010001000101001101000")  // Lv 5 (12 hits max)
                }
            );

            // 2. Violin (W-key) - Melodic syncopated strings
            database[InstrumentType.Violin] = new InstrumentDefinition(
                InstrumentType.Violin, "Violin", new Color(1.0f, 0.5f, 0.2f), "Melodic syncopated strings",
                new List<int[]> {
                    ParsePattern("00100000001000000010000000100000"), // Lv 1 (4 hits)
                    ParsePattern("00100000000010000010000000101000"), // Lv 2 (6 hits)
                    ParsePattern("00100000001010000010000000101000"), // Lv 3 (8 hits)
                    ParsePattern("00100100001010000010010001101000"), // Lv 4 (10 hits)
                    ParsePattern("01100100001010000110010001101000")  // Lv 5 (12 hits max)
                }
            );

            // 3. Flute (S-key) - Light woodwind arpeggios
            database[InstrumentType.Flute] = new InstrumentDefinition(
                InstrumentType.Flute, "Flute", new Color(0.2f, 0.9f, 1.0f), "Light woodwind arpeggios",
                new List<int[]> {
                    ParsePattern("00001000000010000000100000001000"), // Lv 1 (4 hits)
                    ParsePattern("00001000001000000000100010001000"), // Lv 2 (6 hits)
                    ParsePattern("10001000001000001000100010001000"), // Lv 3 (8 hits)
                    ParsePattern("10001000011000001000100011001000"), // Lv 4 (10 hits)
                    ParsePattern("10011000011010001001100011001000")  // Lv 5 (12 hits max)
                }
            );

            // 4. Trumpet (D-key) - Majestic brass fanfare
            database[InstrumentType.Trumpet] = new InstrumentDefinition(
                InstrumentType.Trumpet, "Trumpet", new Color(1.0f, 0.85f, 0.2f), "Majestic brass fanfare",
                new List<int[]> {
                    ParsePattern("00000010000000100000001000000010"), // Lv 1 (4 hits)
                    ParsePattern("00000010010000000000001001000000"), // Lv 2 (6 hits)
                    ParsePattern("01000010010000000100001001000100"), // Lv 3 (8 hits)
                    ParsePattern("01000010010001000100001001100100"), // Lv 4 (10 hits)
                    ParsePattern("01000110010001000100011001100100")  // Lv 5 (12 hits max)
                }
            );

            // 5. Electric Guitar - Power rock riffs
            database[InstrumentType.Guitar] = new InstrumentDefinition(
                InstrumentType.Guitar, "Guitar", new Color(0.8f, 0.2f, 1.0f), "Power rock riffs",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (4 hits)
                    ParsePattern("10000000100100001000000010000000"), // Lv 2 (6 hits)
                    ParsePattern("10000000100100001000000010011000"), // Lv 3 (8 hits)
                    ParsePattern("10010000100100001001000010011000"), // Lv 4 (10 hits)
                    ParsePattern("10010100100100001001010011011000")  // Lv 5 (12 hits max)
                }
            );

            // 6. Piano - Jazz/pop chord accompaniment
            database[InstrumentType.Piano] = new InstrumentDefinition(
                InstrumentType.Piano, "Piano", new Color(0.9f, 0.9f, 0.9f), "Jazz/pop chord accompaniment",
                new List<int[]> {
                    ParsePattern("00010000000100000001000000010000"), // Lv 1 (4 hits)
                    ParsePattern("00010000000101000001000000010000"), // Lv 2 (6 hits)
                    ParsePattern("00010000000101000001000010010100"), // Lv 3 (8 hits)
                    ParsePattern("01010000000101000101000010010100"), // Lv 4 (10 hits)
                    ParsePattern("01010000010101000101000011010100")  // Lv 5 (12 hits max)
                }
            );

            // 7. Cello - Deep bassline progression
            database[InstrumentType.Cello] = new InstrumentDefinition(
                InstrumentType.Cello, "Cello", new Color(0.6f, 0.3f, 0.1f), "Deep bassline progression",
                new List<int[]> {
                    ParsePattern("00000100000001000000010000000100"), // Lv 1 (4 hits)
                    ParsePattern("00000100001001000000010000000100"), // Lv 2 (6 hits)
                    ParsePattern("00000100001001000000010000100100"), // Lv 3 (8 hits)
                    ParsePattern("00100100001001000010010000100110"), // Lv 4 (10 hits)
                    ParsePattern("01100100001001000110010001100110")  // Lv 5 (12 hits max)
                }
            );

            // 8. Saxophone - Jazz solo offbeats
            database[InstrumentType.Saxophone] = new InstrumentDefinition(
                InstrumentType.Saxophone, "Saxophone", new Color(1.0f, 0.4f, 0.7f), "Jazz solo offbeats",
                new List<int[]> {
                    ParsePattern("00000010000000100000001000000010"), // Lv 1 (4 hits)
                    ParsePattern("00000010001000000000001000100000"), // Lv 2 (6 hits)
                    ParsePattern("00000010001000000000001001100000"), // Lv 3 (8 hits)
                    ParsePattern("00100010001001000010001001100000"), // Lv 4 (10 hits)
                    ParsePattern("01100010001001000110001001100100")  // Lv 5 (12 hits max)
                }
            );

            // 9. Harp - Cascading glissando
            database[InstrumentType.Harp] = new InstrumentDefinition(
                InstrumentType.Harp, "Harp", new Color(0.4f, 1.0f, 0.8f), "Cascading glissando",
                new List<int[]> {
                    ParsePattern("00000001000000010000000100000001"), // Lv 1 (4 hits)
                    ParsePattern("00000001000010010000000100001001"), // Lv 2 (6 hits)
                    ParsePattern("00000001000010010000000100011001"), // Lv 3 (8 hits)
                    ParsePattern("00010001000110010001000100011001"), // Lv 4 (10 hits)
                    ParsePattern("01010001000110010101000101011001")  // Lv 5 (12 hits max)
                }
            );

            // 10. Xylophone - Pop melody relay
            database[InstrumentType.Xylophone] = new InstrumentDefinition(
                InstrumentType.Xylophone, "Xylophone", new Color(0.9f, 1.0f, 0.3f), "Pop melody relay",
                new List<int[]> {
                    ParsePattern("01000000010000000100000001000000"), // Lv 1 (4 hits)
                    ParsePattern("01000000010001000100000001000100"), // Lv 2 (6 hits)
                    ParsePattern("01000000010001000100000001001100"), // Lv 3 (8 hits)
                    ParsePattern("01000100010001000100010001001100"), // Lv 4 (10 hits)
                    ParsePattern("01000100010001100100010001001100")  // Lv 5 (12 hits max)
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
}
