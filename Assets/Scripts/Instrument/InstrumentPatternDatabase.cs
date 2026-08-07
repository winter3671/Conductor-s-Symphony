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
            //
            // (2026-08-08 실측 리포트로 발견/수정) 팀파니와 같은 원인으로, 예전 holdLengthSteps=13일
            // 때는 Lv1~3까지 항상 정확히 2회/32스텝 사이클(휴식 3스텝)로 고정되어 레벨업으로 늘어난
            // 온셋이 낭비되고 있었다. holdLengthSteps를 11로 줄인 뒤에는 Lv1~3은 여전히 2회/휴식
            // 5스텝으로 고정이지만, Lv4~5는 패턴 온셋 간격(4스텝)이 홀드 길이(11)보다 짧아 위상이
            // 사이클마다 미묘하게 밀리면서 평균 2.7회/사이클(휴식 1스텝)까지 자연스럽게 늘어난다 -
            // 프렌치호른/플루트처럼 "레벨이 오를수록 실제로 더 자주 발동"이 처음으로 제대로 작동한다.
            // 아래 "N hits"는 여전히 패턴 문자열의 '1' 개수일 뿐 실제 발동 횟수와 다르다.
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
            //
            // (2026-08-08) 바이올린과 동일한 이유로 holdLengthSteps를 13→11로 줄였다 - Lv1~3은 여전히
            // 2회/사이클(휴식 5스텝)로 고정, Lv4~5는 위상이 밀리며 평균 2.7회/사이클(휴식 1스텝)까지
            // 늘어난다. 상세 내용은 위 바이올린 항목 주석 참고.
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
            //
            // (2026-08-08 실측 리포트로 발견/수정) 아래 주석의 "N hits"는 패턴 문자열의 '1' 개수를 세서
            // 적은 것일 뿐, 실제로 몇 개가 진짜 홀드로 발동하는지는 holdLengthSteps(아래 참고)와의 겹침
            // 여부에 달려있다 - 이전 값(16스텝)에서도, 지금 값(10스텝)에서도 레벨과 무관하게 실제로는
            // 매 32스텝 사이클당 정확히 2회만 진짜 홀드가 발동한다(첫 온셋이 이미 진행 중인 홀드와 겹치는
            // 나머지는 RhythmManager.ProcessSequencerStep의 겹침 방지 로직에 의해 조용히 무시됨 - 이 자체는
            // 버그가 아니라 "같은 레인에 홀드가 겹쳐서 스폰되면 안 된다"는 정상적인 방어 로직). 즉 레벨이
            // 올라가도 "홀드가 얼마나 자주 오는가"는 거의 그대로이고, 대신 TimpaniBombardmentEffect 자체의
            // 레벨별 수치(Lv2 범위+25%, Lv3 폭격 빈도+50%, Lv4 기절, Lv5 지진지대 잔류)로 강도가 올라간다.
            // 패턴 문자열 자체는 "장식성 온셋이 있다"는 걸 보여주는 참고용으로 그대로 두되, 주석의 실제
            // 발동 횟수만 정정했다.
            database[InstrumentType.Timpani] = new InstrumentDefinition(
                InstrumentType.Timpani, "Timpani", new Color(0.7f, 0.4f, 0.2f), "Timpani Cannon Mortar Impact",
                new List<int[]> {
                    ParsePattern("10000000000000001000000000000000"), // Lv 1 (실제 발동 2회: Step 0, 16)
                    ParsePattern("10000000100000001000000010000000"), // Lv 2 (실제 발동 2회: Step 0, 16 - Step 8/24는 항상 겹쳐서 스킵됨)
                    ParsePattern("10000000100010001000000010001000"), // Lv 3 (실제 발동 2회: 정상상태에서 Step 8, 24로 위상 이동)
                    ParsePattern("11110000000000001111000000000000"), // Lv 4 (실제 발동 2회: Step 0, 16 - 나머지 1,2,3/17,18,19는 항상 스킵됨)
                    ParsePattern("11111111000000001111000000000000")  // Lv 5 MAX (실제 발동 2회: Step 0, 16 - 나머지는 항상 스킵됨)
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
        // (2026-08-08) 팀파니 홀드 과밀 버그 수정 후, 사용자 요청으로 나머지 홀드 악기 4종도 같은
        // "레벨업 온셋이 겹쳐서 낭비되고 실제 휴식이 없거나 아주 좁다"는 현상이 있는지 전부 시뮬레이션
        // 검토했다(RhythmManager.ProcessSequencerStep의 겹침 방지 로직을 그대로 재현). 결과:
        // - 바이올린/첼로: 홀드 길이(13)가 Lv1~3 온셋 간격(8~16)보다 넓어서 팀파니와 동일하게 항상
        //   2회/사이클(휴식 단 3스텝)로 고정 - 레벨업으로 추가된 온셋(Lv4~5)이 전부 낭비되고 있었다.
        //   → 11로 축소(아래).
        // - 프렌치호른: 온셋 간격이 홀드 길이(6)보다 좁아서 오히려 레벨이 오를수록 실제 발동 횟수도
        //   2→4회/사이클로 실제로 늘어나고 있었다(레벨업 온셋이 낭비되지 않음) - 다만 Lv5는 휴식이
        //   1스텝까지 좁아짐. 의도한 대로 "레벨이 오를수록 바빠진다"가 실제로 작동 중이라 버그가
        //   아니라고 판단해 건드리지 않음.
        // - 플루트: 온셋 간격이 홀드 길이(3)보다 좁아 레벨이 오를수록 2→4→6→8회/사이클로 실제로
        //   늘어남(Lv3+ 휴식 1스텝) - 애초에 "숏 홀드 다발"이 컨셉인 악기라 이 정도 밀집은 의도된
        //   설계로 판단, 건드리지 않음.
        private static readonly Dictionary<InstrumentType, int> holdLengthSteps = new Dictionary<InstrumentType, int>
        {
            // (2026-08-08) 13→11: Lv1~3은 여전히 2회/사이클이지만 휴식이 3→5스텝으로 늘었고, Lv4~5는
            // 온셋 간격(4스텝)이 홀드 길이(11)보다 짧아지면서 평균 2.7회/사이클(휴식 1스텝)까지 실제로
            // 늘어나 레벨업 밀도 상승이 처음으로 의미 있게 작동한다(위 요약 참고).
            { InstrumentType.Violin, 11 },     // 11칸 롱노트 (홀드 중 회전 칼날, 릴리즈 시 부채꼴 참격)
            { InstrumentType.FrenchHorn, 6 },  // 6칸 스웰 롱노트 (홀드 중 전방 부채꼴 충격파 지속)
            { InstrumentType.Cello, 11 },      // 11칸 베이스 롱노트 (홀드 중 중력장 유지) - 바이올린과 동일 이유
            { InstrumentType.Timpani, 10 },    // 10칸 롤ing (32스텝 사이클당 홀드 10+휴식 6이 2회 반복 - 2026-08-08, 16에서 축소)
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
