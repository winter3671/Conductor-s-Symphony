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
                    return InstrumentGroup.GroupA_Downbeat;

                case InstrumentType.Guitar:
                case InstrumentType.Violin:
                    return InstrumentGroup.GroupB_Offbeat;

                case InstrumentType.Flute:
                case InstrumentType.Harp:
                    return InstrumentGroup.GroupC_Midbeat;

                case InstrumentType.Trumpet:
                case InstrumentType.Saxophone:
                case InstrumentType.Piano:
                case InstrumentType.Xylophone:
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

            // 1. Drums (Dynamic 4-bar Song-Form Groove & Turnaround Fills)
            database[InstrumentType.Drums] = new InstrumentDefinition(
                InstrumentType.Drums, "Drums", new Color(0.9f, 0.3f, 0.3f), "Dynamic 4-bar song-form kick & snare fill-ins",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (4 hits: Straight Downbeats)
                    ParsePattern("10000000100000000010000010001000"), // Lv 2 (5 hits: Bar 3 Offbeat Shift & Bar 4 Fill)
                    ParsePattern("10000000100010000010000010001001"), // Lv 3 (6 hits: Bar 2 Variation & Bar 4 Turnaround)
                    ParsePattern("10000100100010000010010010001001"), // Lv 4 (7 hits: Synco-Pushes)
                    ParsePattern("10000100100110000010010010001010")  // Lv 5 (8 hits MAX: Complete Dynamic 4-Bar Song Form)
                }
            );

            // 2. Cello (Dynamic Low Bass Pushes & Rhythmic Variations)
            database[InstrumentType.Cello] = new InstrumentDefinition(
                InstrumentType.Cello, "Cello", new Color(0.6f, 0.3f, 0.1f), "Deep low-pitch bass pushes with bar variations",
                new List<int[]> {
                    ParsePattern("01000000010000000100000001000000"), // Lv 1 (4 hits: Step 2 Push)
                    ParsePattern("01000000010000000010000001000100"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Fill)
                    ParsePattern("01000000010001000010000001000101"), // Lv 3 (6 hits: Bar 2 Fill & Bar 4 End Accent)
                    ParsePattern("01000010010001000010010001000101"), // Lv 4 (7 hits: Poly-rhythmic Bass Pushes)
                    ParsePattern("01000010010101000010010001000110")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Cello Groove)
                }
            );

            // 3. Guitar (Dynamic Power Riffs & Turnarounds)
            database[InstrumentType.Guitar] = new InstrumentDefinition(
                InstrumentType.Guitar, "Guitar", new Color(0.8f, 0.2f, 1.0f), "Power rock offbeat riffs with bar shifts",
                new List<int[]> {
                    ParsePattern("00100000001000000010000000100000"), // Lv 1 (4 hits: Step 3 Offbeats)
                    ParsePattern("00100000001000001000000000100100"), // Lv 2 (5 hits: Bar 3 Downbeat Shift & Bar 4 Riff)
                    ParsePattern("00100000001001001000000000100101"), // Lv 3 (6 hits: Bar 2 Accent & Bar 4 Turnaround)
                    ParsePattern("00100010001001001000010000100101"), // Lv 4 (7 hits: Synco-Riffs)
                    ParsePattern("00100010001101001000010000100110")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Rock Riff)
                }
            );

            // 4. Violin (Melodic Sweeping Variations)
            database[InstrumentType.Violin] = new InstrumentDefinition(
                InstrumentType.Violin, "Violin", new Color(1.0f, 0.5f, 0.2f), "Melodic sweeping strings with bar variations",
                new List<int[]> {
                    ParsePattern("00010000000100000001000000010000"), // Lv 1 (4 hits: Step 4 Strokes)
                    ParsePattern("00010000000100000000100000010010"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Run)
                    ParsePattern("00010000000100100000100000010011"), // Lv 3 (6 hits: Bar 2 Accent & Bar 4 Turnaround)
                    ParsePattern("00010100000100100000101000010011"), // Lv 4 (7 hits: Poly String Runs)
                    ParsePattern("00010100000110100000101000010110")  // Lv 5 (8 hits MAX: Dynamic 4-Bar String Melody)
                }
            );

            // 5. Flute (Light Woodwind Arpeggio Variations)
            database[InstrumentType.Flute] = new InstrumentDefinition(
                InstrumentType.Flute, "Flute", new Color(0.2f, 0.9f, 1.0f), "Light woodwind arpeggios with bar shifts",
                new List<int[]> {
                    ParsePattern("00001000000010000000100000001000"), // Lv 1 (4 hits: Step 5 Midbeats)
                    ParsePattern("00001000000010000001000000001001"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Trill)
                    ParsePattern("00001000000010010001000000001010"), // Lv 3 (6 hits: Bar 2 Fill & Bar 4 Turnaround)
                    ParsePattern("00001010000010010001001000001010"), // Lv 4 (7 hits: Synco Woodwind Trills)
                    ParsePattern("00001010000011010001001000001011")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Flute Riff)
                }
            );

            // 6. Harp (Cascading Glissando Runs with Bar Shifts)
            database[InstrumentType.Harp] = new InstrumentDefinition(
                InstrumentType.Harp, "Harp", new Color(0.4f, 1.0f, 0.8f), "Cascading glissando runs with bar shifts",
                new List<int[]> {
                    ParsePattern("00000100000001000000010000000100"), // Lv 1 (4 hits: Step 6 Runs)
                    ParsePattern("00000100000001000000100000000101"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Run)
                    ParsePattern("00000100000001010000100000000110"), // Lv 3 (6 hits: Bar 2 Fill & Bar 4 Turnaround)
                    ParsePattern("00000110000001010000101000000110"), // Lv 4 (7 hits: Poly Harp Runs)
                    ParsePattern("00000110000001110000101000000111")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Harp Cascade)
                }
            );

            // 7. Trumpet (High Brass Fanfare & Turnaround Accent)
            database[InstrumentType.Trumpet] = new InstrumentDefinition(
                InstrumentType.Trumpet, "Trumpet", new Color(1.0f, 0.85f, 0.2f), "Majestic upbeat brass fanfare with bar shifts",
                new List<int[]> {
                    ParsePattern("00000010000000100000001000000010"), // Lv 1 (4 hits: Step 7 Upbeats)
                    ParsePattern("00000010000000100100000000000011"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Fanfare)
                    ParsePattern("00000010000000110100000000000110"), // Lv 3 (6 hits: Bar 2 Accent & Bar 4 Turnaround)
                    ParsePattern("01000010000000110100001000000110"), // Lv 4 (7 hits: Synco Brass Calls)
                    ParsePattern("01000010010000110100001000000111")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Brass Fanfare)
                }
            );

            // 8. Saxophone (Jazz Swing Accents & Fill-Ins)
            database[InstrumentType.Saxophone] = new InstrumentDefinition(
                InstrumentType.Saxophone, "Saxophone", new Color(1.0f, 0.4f, 0.7f), "Jazz swing end-bar accents & fill-ins",
                new List<int[]> {
                    ParsePattern("00000001000000010000000100000001"), // Lv 1 (4 hits: Step 8 End-Bar)
                    ParsePattern("00000001000000010000001000000101"), // Lv 2 (5 hits: Bar 3 Shift & Bar 4 Swing)
                    ParsePattern("00000001000001010000001000000110"), // Lv 3 (6 hits: Bar 2 Accent & Bar 4 Turnaround)
                    ParsePattern("01000001000001010000001100000110"), // Lv 4 (7 hits: Poly Sax Accents)
                    ParsePattern("01000001010001010000001100000111")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Sax Riff)
                }
            );

            // 9. Piano (Syncopated Alternating Chords & Groove Shifts)
            database[InstrumentType.Piano] = new InstrumentDefinition(
                InstrumentType.Piano, "Piano", new Color(0.9f, 0.9f, 0.9f), "Syncopated alternating piano chords & groove shifts",
                new List<int[]> {
                    ParsePattern("10000000100000000010000000100000"), // Lv 1 (4 hits: Alternating Chords)
                    ParsePattern("10000000100000000010000010001000"), // Lv 2 (5 hits: Bar 4 Groove Shift)
                    ParsePattern("10000000100010000010000010001001"), // Lv 3 (6 hits: Bar 2 Fill & Bar 4 Turnaround)
                    ParsePattern("10000100100010000010010010001001"), // Lv 4 (7 hits: Synco Chords)
                    ParsePattern("10000100100110000010010010001010")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Piano Groove)
                }
            );

            // 10. Xylophone (Bright Popping Tones & Polyrhythmic Shifts)
            database[InstrumentType.Xylophone] = new InstrumentDefinition(
                InstrumentType.Xylophone, "Xylophone", new Color(0.9f, 1.0f, 0.3f), "Bright popping alternating high tones with bar shifts",
                new List<int[]> {
                    ParsePattern("00001000000010000000001000000010"), // Lv 1 (4 hits: Alternating High Tones)
                    ParsePattern("00001000000010000000001001000010"), // Lv 2 (5 hits: Bar 4 Pop Shift)
                    ParsePattern("00001000010010000000001001000010"), // Lv 3 (6 hits: Bar 2 Accent & Bar 4 Fill)
                    ParsePattern("01001000010010000000011001000010"), // Lv 4 (7 hits: Poly Xylo Pops)
                    ParsePattern("01001000010010000001011001000011")  // Lv 5 (8 hits MAX: Dynamic 4-Bar Xylo Riff)
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
