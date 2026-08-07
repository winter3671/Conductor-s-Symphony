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

            // 1. Drums (Slot 0 - Q: Downbeats every bar/measure, Kick/Snare progression)
            // Design Doc: Kick(1, 9) + Snare(5, 13) downbeats (0-indexed: 0, 4, 8, 12, 16, 20, 24, 28)
            database[InstrumentType.Drums] = new InstrumentDefinition(
                InstrumentType.Drums, "Drums", new Color(0.9f, 0.3f, 0.3f), "360° Shockwave Beat Bang",
                new List<int[]> {
                    ParsePattern("10000000100000001000000010000000"), // Lv 1 (Original Baseline: 4 downbeats at Step 0, 8, 16, 24)
                    ParsePattern("10000000100010001000000010001000"), // Lv 2 (6 hits: Step 0, 8, 12, 16, 24, 28 - Snare accents)
                    ParsePattern("10001000100010001000100010001000"), // Lv 3 (8 hits: Step 0, 4, 8, 12, 16, 20, 24, 28 - Quarter beat drive)
                    ParsePattern("10001000100010001000100010001001"), // Lv 4 (9 hits: Quarter drive + pickup fill)
                    ParsePattern("10001000101010001000100010101001")  // Lv 5 MAX (11 hits: Full drum groove fill)
                }
            );

            // 2. Piano (Slot 1 - R: Syncopated chord taps & rapid burst)
            // Design Doc: Tap only, 0% long notes. Off-beats & syncopated 6-burst cascades
            database[InstrumentType.Piano] = new InstrumentDefinition(
                InstrumentType.Piano, "Piano", new Color(0.9f, 0.9f, 0.9f), "Auto-Target Chord Laser & Piano Cascade Volley",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits: Step 0, 8, 16, 24)
                    ParsePattern("10000100100000001000010010000000"), // Lv 3 (6 hits: Syncopated off-beats 5, 21)
                    ParsePattern("10000100100001001000010010000100"), // Lv 4 (8 hits: Off-beat chord cascade)
                    ParsePattern("10101010100000001010101010000000")  // Lv 5 MAX (10 hits: 6-burst piano volley cascade!)
                }
            );

            // 3. Violin (Slot 2 - W: Main melody arc & crescent slashes)
            // Design Doc: Main melody arc taps & holds
            database[InstrumentType.Violin] = new InstrumentDefinition(
                InstrumentType.Violin, "Violin", new Color(1.0f, 0.5f, 0.2f), "Orbiting Blades & Crescent Arc Slash",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits: Step 0, 8, 16, 24)
                    ParsePattern("10000000100010001000000010001000"), // Lv 3 (6 hits: Step 0, 8, 12, 16, 24, 28)
                    ParsePattern("10001000100010001000100010001000"), // Lv 4 (8 hits: Step 0, 4, 8, 12, 16, 20, 24, 28)
                    ParsePattern("10101000100010001010100010001000")  // Lv 5 MAX (10 hits: Melodic arc stream)
                }
            );

            // 4. Flute (Slot 3 - E: Off-beat woodwind swells & mini vortex pull)
            // Design Doc: Off-beat 16th steps (2, 10, 18, 26)
            database[InstrumentType.Flute] = new InstrumentDefinition(
                InstrumentType.Flute, "Flute", new Color(0.2f, 0.9f, 1.0f), "Mini Vortex (Release Pull) & Woodwind Swells",
                new List<int[]> {
                    ParsePattern("00100000000000000010000000000000"), // Lv 1 (2 off-beats: Step 2, 18)
                    ParsePattern("00100000001000000010000000100000"), // Lv 2 (4 off-beats: Step 2, 10, 18, 26)
                    ParsePattern("00100000001000100010000000100010"), // Lv 3 (6 off-beats)
                    ParsePattern("00100010001000100010001000100010"), // Lv 4 (8 off-beats)
                    ParsePattern("00100010001000100010001000100011")  // Lv 5 MAX (9 off-beats + finale)
                }
            );

            // 5. French Horn (Sonic Brass Cannon)
            // Design Doc: Mid-measure brass swells (Step 4, 12, 20, 28)
            database[InstrumentType.FrenchHorn] = new InstrumentDefinition(
                InstrumentType.FrenchHorn, "FrenchHorn", new Color(1.0f, 0.85f, 0.2f), "Sonic Brass Cannon Cone Knockback",
                new List<int[]> {
                    ParsePattern("00001000000000000000100000000000"), // Lv 1 (2 swell hits: Step 4, 20)
                    ParsePattern("00001000000010000000100000001000"), // Lv 2 (4 swell hits: Step 4, 12, 20, 28)
                    ParsePattern("10001000000010001000100000001000"), // Lv 3 (6 swell hits)
                    ParsePattern("10001000100010001000100010001000"), // Lv 4 (8 swell hits)
                    ParsePattern("10001000100010001000100010001001")  // Lv 5 MAX (9 swell hits)
                }
            );

            // 6. Glockenspiel (Star Fall & Gentle Chimes)
            // Design Doc: High-point accents & 4/8-burst chimes
            database[InstrumentType.Glockenspiel] = new InstrumentDefinition(
                InstrumentType.Glockenspiel, "Glockenspiel", new Color(0.4f, 1.0f, 0.8f), "Star Fall on Highest HP Enemy",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits: Step 0, 8, 16, 24)
                    ParsePattern("10000000100000001000000010001000"), // Lv 3 (5 hits)
                    ParsePattern("11110000000000001111000000000000"), // Lv 4 (8 hits: 4-burst chime volleys!)
                    ParsePattern("11110000100000001111111100000000")  // Lv 5 MAX (13 hits: 8-burst star finale!)
                }
            );

            // 7. Cello (Deep Bass Gravity Binding)
            // Design Doc: Deep bass sustained pulses & heavy downbeats
            database[InstrumentType.Cello] = new InstrumentDefinition(
                InstrumentType.Cello, "Cello", new Color(0.6f, 0.3f, 0.1f), "Gravity Binding Slow Zone",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits: Step 0, 8, 16, 24)
                    ParsePattern("10000000100001001000000010000100"), // Lv 3 (6 hits: Bass syncopation)
                    ParsePattern("10001000100010001000100010001000"), // Lv 4 (8 hits: Steady bass pulse)
                    ParsePattern("10001000100010001000100010001001")  // Lv 5 MAX (9 hits: Bass resonance)
                }
            );

            // 8. Timpani (Timpani Cannon Mortar Impact & Rolling Bombardment)
            // Design Doc: Heavy accent downbeats + 16-bar rolling drum bombardment (Step 0, 1, 2, 3...)
            database[InstrumentType.Timpani] = new InstrumentDefinition(
                InstrumentType.Timpani, "Timpani", new Color(0.7f, 0.4f, 0.2f), "Timpani Cannon Mortar Impact",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (2 hits: Heavy downbeats Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (4 hits: Step 0, 8, 16, 24)
                    ParsePattern("10000000100010001000000010001000"), // Lv 3 (6 hits: Mortar double strike)
                    ParsePattern("11110000000000001111000000000000"), // Lv 4 (8 hits: Timpani rolling mortar!)
                    ParsePattern("11111111000000001111000000000000")  // Lv 5 MAX (12 hits: Full rolling seismic bombardment!)
                }
            );

            // 9. Marimba (Off-Beat Wood Ricochet Wave)
            // Design Doc: Off-beats 3 & 11 (0-indexed: Step 2 & 10)
            database[InstrumentType.Marimba] = new InstrumentDefinition(
                InstrumentType.Marimba, "Marimba", new Color(0.9f, 0.6f, 0.2f), "Off-Beat Marimba Ricochet Wave",
                new List<int[]> {
                    ParsePattern("00100000000000000010000000000000"), // Lv 1 (2 off-beats: Step 2, 18)
                    ParsePattern("00100000001000000010000000100000"), // Lv 2 (4 off-beats: Step 2, 10, 18, 26)
                    ParsePattern("00100000001000100010000000100010"), // Lv 3 (6 off-beats)
                    ParsePattern("00100010001000100010001000100010"), // Lv 4 (8 off-beats)
                    ParsePattern("00100010001000100010001000100011")  // Lv 5 MAX (9 off-beats)
                }
            );

            // 10. Bell (8-Direction Starlight Burst)
            // Design Doc: Accent 2 & 4 beats (0-indexed: Step 4 & 12)
            database[InstrumentType.Bell] = new InstrumentDefinition(
                InstrumentType.Bell, "Bell", new Color(0.9f, 1.0f, 0.3f), "8-Direction Starlight Burst",
                new List<int[]> {
                    ParsePattern("00001000000000000000100000000000"), // Lv 1 (2 accent beats: Step 4, 20)
                    ParsePattern("00001000000010000000100000001000"), // Lv 2 (4 accent beats: Step 4, 12, 20, 28)
                    ParsePattern("00001000000010000000100000001001"), // Lv 3 (5 accent beats)
                    ParsePattern("00001000010010000000100001001000"), // Lv 4 (6 accent beats - 실제 문자열 기준, 이전 "8"은 주석 오류였음)
                    ParsePattern("00001000010010000000100001001001")  // Lv 5 MAX (7 accent beats - 실제 문자열 기준, 이전 "9"는 주석 오류였음)
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

        // 10종 악기별 공격 메커니즘 기획서: 홀드(롱노트) 기반 악기 4종 + 각자의 노트 길이(32스텝 기준).
        // 기존 탭 패턴(levelPatterns)의 onset 스텝은 그대로 재사용하고, 이 악기들만 "그 스텝에서 홀드가
        // 시작된다"는 의미로 재해석한다 - 레벨별 홀드 전용 패턴을 별도로 새로 만들 필요가 없다.
        private static readonly Dictionary<InstrumentType, int> holdLengthSteps = new Dictionary<InstrumentType, int>
        {
            { InstrumentType.Violin, 13 },     // 13칸 롱노트 (홀드 중 회전 칼날, 릴리즈 시 부채꼴 참격)
            { InstrumentType.FrenchHorn, 6 },  // 6칸 스웰 롱노트 (홀드 중 전방 부채꼴 충격파 지속)
            { InstrumentType.Cello, 13 },      // 13칸 베이스 롱노트 (홀드 중 중력장 유지)
            { InstrumentType.Timpani, 16 },    // 16마디 롤ing (홀드 중 지진 융단폭격)
            { InstrumentType.Flute, 3 },       // 2~4칸 숏 홀드 (릴리즈 시 미니 소용돌이 - 3단계에서 추가)
        };

        public static bool IsHoldBased(InstrumentType type)
        {
            return holdLengthSteps.ContainsKey(type);
        }

        public static int GetHoldLengthSteps(InstrumentType type)
        {
            return holdLengthSteps.TryGetValue(type, out int steps) ? steps : 0;
        }
    }
}
