using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Instrument
{
    // 2026-08-09: 리팩토링 문서(game_systems_reference.md §6 "아직 남은 항목" 1번) 반영 - 기존엔
    // 10개 악기 이펙트 클래스(Assets/Scripts/Combat/InstrumentAttacks/*.cs)의 Init() 안에
    // "(level >= N ? A : B)" 삼항식으로 범위/피해량/관통/발사수/틱간격 등의 레벨별 수치가 흩어져
    // 있어서, 순수 수치 조정을 하려면 매번 코드를 찾아 고치고 재컴파일해야 했다.
    //
    // 이 파일은 InstrumentDamageTable.cs와 동일한 관례(Dictionary<InstrumentType, T[]>, 배열 인덱스는
    // [level-1], 1~5 범위 밖은 클램프)로 그 수치들을 전부 한 곳에 모은 것이다. 중요: 이번 작업은
    // "순수 추출"이다 - 기존 삼항식이 만들어내던 레벨 1~5 결과값과 완전히 동일한 숫자가 나오도록만
    // 옮겼고, 밸런스 수치 자체는 하나도 바꾸지 않았다(교차검증:
    // Docs/archive/instrument_level_stats_dataification_test_guide.md).
    //
    // 이 파일에 포함하지 않은 것: "if (level >= N) { 특수동작(); }" 형태의 행동 분기(예: 바이올린
    // Lv5 검기잔향 스폰, 마림바 Lv5 감속+밀쳐냄, 플루트 Lv5 소멸폭발, 첼로 Lv5 끌어당김, 팀파니 Lv4/5
    // 기절·지진지대 on/off, 프렌치호른 Lv4 피해증폭 on/off, 벨/피아노/글록켄슈필의 콤보 연동 발동 조건).
    // 이런 건 "값"이 아니라 "로직"이라 데이터로 뽑아봐야 재컴파일 없이 바꿀 수 있는 게 없고, 오히려
    // 코드에서 바로 읽는 편이 명확하다 - doc이 콕 집어 언급한 "범위/관통/발사수/틱 간격 등" 수치형
    // 항목만 데이터화 대상으로 삼았다.
    public static class InstrumentLevelStats
    {
        // 레벨 인덱스는 1~5. 배열 인덱스는 [level - 1]. InstrumentDamageTable과 동일한 관례.
        public static int Idx(int level) => Mathf.Clamp(level - 1, 0, 4);

        // ==================== 범위/반경 배율 ====================
        // 각 이펙트가 기존에 "기본거리 * (level >= N ? mult : 1f) * CombatTargetingUtility.GetRangeMultiplier()"
        // 형태로 쓰던 그 mult 부분만 뽑음. 크레센도(Crescendo) 패시브의 GetRangeMultiplier()와는
        // 완전히 별개(그건 전역 패시브, 이건 악기 자체 레벨 스케일링)라 곱하는 순서/위치는 호출부에 그대로 둔다.
        private static readonly Dictionary<InstrumentType, float[]> rangeMultiplier = new Dictionary<InstrumentType, float[]>
        {
            { InstrumentType.Cello,        new float[] { 1f, 1.2f,  1.2f,  1.2f,  1.2f  } }, // Lv2+: 범위 +20%
            { InstrumentType.Flute,        new float[] { 1f, 1f,    1.5f,  1.5f,  1.5f  } }, // Lv3+: 흡입범위 +50%
            { InstrumentType.Bell,         new float[] { 1f, 1.3f,  1.3f,  1.3f,  1.3f  } }, // Lv2+: 사거리 +30%
            { InstrumentType.Drums,        new float[] { 1f, 1.2f,  1.2f,  1.2f,  1.2f  } }, // Lv2+: 범위 +20%
            { InstrumentType.FrenchHorn,   new float[] { 1f, 1.25f, 1.25f, 1.25f, 1.25f } }, // Lv2+: 사거리 +25%
            { InstrumentType.Violin,       new float[] { 1f, 1.2f,  1.2f,  1.2f,  1.2f  } }, // Lv2+: 회전 반경 +20%
            { InstrumentType.Timpani,      new float[] { 1f, 1.25f, 1.25f, 1.25f, 1.25f } }, // Lv2+: 낙하 범위 +25%
        };
        public static float GetRangeMultiplier(InstrumentType type, int level) =>
            rangeMultiplier.TryGetValue(type, out var arr) ? arr[Idx(level)] : 1f;

        // ==================== 지속시간 배율 ====================
        private static readonly Dictionary<InstrumentType, float[]> durationMultiplier = new Dictionary<InstrumentType, float[]>
        {
            { InstrumentType.Flute, new float[] { 1f, 1.4f, 1.4f, 1.4f, 1.4f } }, // Lv2+: 유지시간 +40%
            { InstrumentType.Cello, new float[] { 1f, 1f,   1f,   1.3f, 1.3f } }, // Lv4+: 릴리즈 후 잔류시간 +30%
        };
        public static float GetDurationMultiplier(InstrumentType type, int level) =>
            durationMultiplier.TryGetValue(type, out var arr) ? arr[Idx(level)] : 1f;

        // ==================== 이펙트 자체 피해량 배율 ====================
        // 주의: InstrumentDamageTable.GetDamageMultiplier()(전체 DPS 보정 배율, RhythmAttackManager에서
        // baseDamage에 적용)와는 완전히 다른 별개의 배율이다. 이건 개별 이펙트 클래스 안에서 전달받은
        // damage 파라미터에 한 번 더 곱하던 "악기 자체 Lv2+ 피해량 보너스"만 데이터화한 것.
        private static readonly Dictionary<InstrumentType, float[]> damageMultiplier = new Dictionary<InstrumentType, float[]>
        {
            { InstrumentType.Drums,        new float[] { 1f, 1.2f,  1.2f,  1.2f,  1.2f  } }, // Lv2+: 피해량 +20%
            { InstrumentType.Glockenspiel, new float[] { 1f, 1.3f,  1.3f,  1.3f,  1.3f  } }, // Lv2+: 피해량 +30%
            { InstrumentType.Piano,        new float[] { 1f, 1.25f, 1.25f, 1.25f, 1.25f } }, // Lv2+: 피해량 +25%
        };
        public static float GetDamageMultiplier(InstrumentType type, int level) =>
            damageMultiplier.TryGetValue(type, out var arr) ? arr[Idx(level)] : 1f;

        // ==================== 넉백 배율 ====================
        private static readonly Dictionary<InstrumentType, float[]> knockbackMultiplier = new Dictionary<InstrumentType, float[]>
        {
            { InstrumentType.Drums,      new float[] { 1f, 1f, 2f,   2f,   2f   } }, // Lv3+: 넉백 거리 2배
            { InstrumentType.FrenchHorn, new float[] { 1f, 1f, 1.4f, 1.4f, 1.4f } }, // Lv3+: 넉백 거리 +40%
        };
        public static float GetKnockbackMultiplier(InstrumentType type, int level) =>
            knockbackMultiplier.TryGetValue(type, out var arr) ? arr[Idx(level)] : 1f;

        // ==================== 시각/판정 크기 배율 ====================
        private static readonly Dictionary<InstrumentType, float[]> sizeMultiplier = new Dictionary<InstrumentType, float[]>
        {
            { InstrumentType.Violin,  new float[] { 1f, 1f, 1f,   1.5f, 1.5f } }, // Lv4+: 릴리즈 참격 크기 +50%
            { InstrumentType.Marimba, new float[] { 1f, 1f, 1.3f, 1.3f, 1.3f } }, // Lv3+: 파동 크기 +30%
        };
        public static float GetSizeMultiplier(InstrumentType type, int level) =>
            sizeMultiplier.TryGetValue(type, out var arr) ? arr[Idx(level)] : 1f;

        // ==================== 관통 수(최종값) ====================
        private static readonly Dictionary<InstrumentType, int[]> pierceCount = new Dictionary<InstrumentType, int[]>
        {
            { InstrumentType.Bell,    new int[] { 3, 3, 5, 5, 5 } }, // Lv3+: 관통 3→5
            { InstrumentType.Piano,   new int[] { 2, 2, 4, 4, 4 } }, // Lv3+: 관통 2→4
            { InstrumentType.Violin,  new int[] { 3, 3, 5, 5, 5 } }, // Lv3+: 관통 3→5 (릴리즈 참격)
            { InstrumentType.Marimba, new int[] { 3, 5, 5, 5, 5 } }, // Lv2+: 관통 3→5
        };
        public static int GetPierceCount(InstrumentType type, int level, int fallback = 1) =>
            pierceCount.TryGetValue(type, out var arr) ? arr[Idx(level)] : fallback;

        // ==================== 개수/버스트/샷 수 등 스텝형 카운트 ====================
        // 악기마다 의미가 전혀 달라(벨=연속발사 수, 피아노=발사 수, 드럼=중첩 충격파 수, 플루트=동시
        // 유지 소용돌이 수, 바이올린=칼날 개수) 공용 개념으로 묶진 않았지만, 전부 "레벨 X부터 값이
        // 한 단계 올라가는" 동일 패턴이라 하나의 Dictionary로 관리한다(인스트루먼트 타입이 키라
        // 의미 충돌 없음).
        private static readonly Dictionary<InstrumentType, int[]> stepCount = new Dictionary<InstrumentType, int[]>
        {
            { InstrumentType.Bell,   new int[] { 1, 1, 1, 2, 2 } }, // Lv4+: 8방향 성광 연속발사 2회
            { InstrumentType.Piano,  new int[] { 1, 1, 1, 2, 2 } }, // Lv4+: 발사 수 +1 (extraProjectiles는 호출부에서 별도 가산)
            { InstrumentType.Drums,  new int[] { 1, 1, 1, 1, 2 } }, // Lv5: 충격파 2연속 중첩
            { InstrumentType.Flute,  new int[] { 1, 1, 1, 2, 2 } }, // Lv4+: 동시 유지 가능 소용돌이 개수
            { InstrumentType.Violin, new int[] { 2, 2, 3, 3, 3 } }, // Lv3+: 칼날 개수 (extraProjectiles는 호출부에서 별도 가산)
        };
        public static int GetStepCount(InstrumentType type, int level, int fallback = 1) =>
            stepCount.TryGetValue(type, out var arr) ? arr[Idx(level)] : fallback;

        // ==================== 악기별 전용 단일 수치 ====================
        // 딱 한 악기에서만 쓰이는 값들 - Dictionary로 감쌀 실익이 없어 배열 상수로 바로 둔다.
        public static readonly float[] ViolinSpinSpeedMultiplier = { 1f, 1.3f, 1.3f, 1.3f, 1.3f }; // Lv2+: 회전 속도 +30%
        public static readonly float[] CelloSlowFraction = { 0.4f, 0.4f, 0.6f, 0.6f, 0.6f }; // Lv1: 40%, Lv3+: 60%
        public static readonly float[] FrenchHornHalfAngleDeg = { 60f, 60f, 60f, 60f, 90f }; // Lv5+: 전방 각도(반각) 60도→90도(전체 120도→180도)
        public static readonly float[] GlockenspielSplashRadiusBase = { 0.6f, 0.6f, 1.1f, 1.1f, 1.1f }; // Lv3+: 스플래시 반경 기준값(범위 패시브 적용 전)
        public static readonly float[] TimpaniBombardIntervalBaseSeconds = { 0.65f, 0.65f, 0.65f / 1.5f, 0.65f / 1.5f, 0.65f / 1.5f }; // Lv3+: 융단폭격 빈도 +50%(간격 감소)

        // ==================== 레벨업 카드 미리보기용 실제 효과 요약 ====================
        // 2026-08-10: 사용자 피드백 - 레벨업 카드에 찍히던 "(피해 +0, 투사체 +0)"이 위 표들의
        // 실제 배율 인상(범위/피해량/넉백 등)을 전혀 반영하지 못해 "버그처럼 보인다"는 지적을 받음.
        // 아래는 하드코딩된 % 문자열이 아니라 위 배열들에서 직접 diff해서 "이 레벨업으로 뭐가
        // 좋아지는가"를 한글 문구로 만들어주는 헬퍼 - 나중에 이 파일의 밸런스 수치를 조정해도
        // 카드 문구가 자동으로 같이 바뀐다(값과 문구가 따로 놀 일이 없음).
        private const string DamageLabel = "피해량";
        private const string KnockbackLabel = "넉백 거리";

        private static readonly Dictionary<InstrumentType, string> rangeLabel = new Dictionary<InstrumentType, string>
        {
            { InstrumentType.Cello, "범위" }, { InstrumentType.Flute, "흡입범위" }, { InstrumentType.Bell, "사거리" },
            { InstrumentType.Drums, "범위" }, { InstrumentType.FrenchHorn, "사거리" }, { InstrumentType.Violin, "회전 반경" },
            { InstrumentType.Timpani, "낙하 범위" },
        };
        private static readonly Dictionary<InstrumentType, string> durationLabel = new Dictionary<InstrumentType, string>
        {
            { InstrumentType.Flute, "유지시간" }, { InstrumentType.Cello, "잔류시간" },
        };
        private static readonly Dictionary<InstrumentType, string> sizeLabel = new Dictionary<InstrumentType, string>
        {
            { InstrumentType.Violin, "참격 크기" }, { InstrumentType.Marimba, "파동 크기" },
        };
        private static readonly Dictionary<InstrumentType, string> pierceLabel = new Dictionary<InstrumentType, string>
        {
            { InstrumentType.Bell, "관통" }, { InstrumentType.Piano, "관통" }, { InstrumentType.Violin, "관통" }, { InstrumentType.Marimba, "관통" },
        };
        private static readonly Dictionary<InstrumentType, string> stepLabel = new Dictionary<InstrumentType, string>
        {
            { InstrumentType.Bell, "연속 발사" }, { InstrumentType.Piano, "발사 수" }, { InstrumentType.Drums, "충격파 중첩" },
            { InstrumentType.Flute, "동시 유지 개수" }, { InstrumentType.Violin, "칼날 개수" },
        };

        private static string FormatMultiplierDelta(float from, float to)
        {
            if (Mathf.Approximately(from, to)) return null;
            float ratio = to / from;
            if (Mathf.Approximately(ratio, Mathf.Round(ratio)) && ratio >= 2f)
            {
                return $"{Mathf.RoundToInt(ratio)}배";
            }
            int percent = Mathf.RoundToInt((ratio - 1f) * 100f);
            return $"+{percent}%";
        }

        // targetLevel로 "레벨업"했을 때 새로 켜지는 실제 효과 목록(카드 설명용). targetLevel<=1은
        // 신규 습득이라 "레벨업 효과"라는 개념 자체가 없으므로 빈 리스트를 돌려준다.
        public static List<string> GetLevelUpHighlights(InstrumentType type, int targetLevel)
        {
            var highlights = new List<string>();
            if (targetLevel <= 1) return highlights;

            int newIdx = Idx(targetLevel);
            int prevIdx = Idx(targetLevel - 1);

            void CheckFloatTable(Dictionary<InstrumentType, float[]> table, string label)
            {
                if (!table.TryGetValue(type, out var arr)) return;
                string delta = FormatMultiplierDelta(arr[prevIdx], arr[newIdx]);
                if (delta != null) highlights.Add($"{label} {delta}");
            }

            void CheckFloatTableWithLabels(Dictionary<InstrumentType, float[]> table, Dictionary<InstrumentType, string> labels)
            {
                if (!table.TryGetValue(type, out var arr) || !labels.TryGetValue(type, out var label)) return;
                string delta = FormatMultiplierDelta(arr[prevIdx], arr[newIdx]);
                if (delta != null) highlights.Add($"{label} {delta}");
            }

            void CheckIntTable(Dictionary<InstrumentType, int[]> table, Dictionary<InstrumentType, string> labels)
            {
                if (!table.TryGetValue(type, out var arr) || !labels.TryGetValue(type, out var label)) return;
                if (arr[prevIdx] == arr[newIdx]) return;
                highlights.Add($"{label} {arr[prevIdx]}→{arr[newIdx]}");
            }

            CheckFloatTableWithLabels(rangeMultiplier, rangeLabel);
            CheckFloatTableWithLabels(durationMultiplier, durationLabel);
            CheckFloatTable(damageMultiplier, DamageLabel);
            CheckFloatTable(knockbackMultiplier, KnockbackLabel);
            CheckFloatTableWithLabels(sizeMultiplier, sizeLabel);
            CheckIntTable(pierceCount, pierceLabel);
            CheckIntTable(stepCount, stepLabel);

            // 딱 한 악기에서만 쓰이는 전용 수치들(위 Dictionary 테이블에 안 들어있음) - 개별 처리.
            if (type == InstrumentType.Violin)
            {
                string d = FormatMultiplierDelta(ViolinSpinSpeedMultiplier[prevIdx], ViolinSpinSpeedMultiplier[newIdx]);
                if (d != null) highlights.Add($"회전 속도 {d}");
            }
            if (type == InstrumentType.Cello)
            {
                float from = CelloSlowFraction[prevIdx], to = CelloSlowFraction[newIdx];
                if (!Mathf.Approximately(from, to))
                {
                    highlights.Add($"슬로우 효과 {Mathf.RoundToInt(from * 100f)}%→{Mathf.RoundToInt(to * 100f)}%");
                }
            }
            if (type == InstrumentType.FrenchHorn)
            {
                float from = FrenchHornHalfAngleDeg[prevIdx] * 2f, to = FrenchHornHalfAngleDeg[newIdx] * 2f;
                if (!Mathf.Approximately(from, to))
                {
                    highlights.Add($"공격 각도 {from:0}°→{to:0}°");
                }
            }
            if (type == InstrumentType.Glockenspiel)
            {
                string d = FormatMultiplierDelta(GlockenspielSplashRadiusBase[prevIdx], GlockenspielSplashRadiusBase[newIdx]);
                if (d != null) highlights.Add($"스플래시 반경 {d}");
            }
            if (type == InstrumentType.Timpani)
            {
                float from = TimpaniBombardIntervalBaseSeconds[prevIdx], to = TimpaniBombardIntervalBaseSeconds[newIdx];
                if (!Mathf.Approximately(from, to))
                {
                    int percent = Mathf.RoundToInt((from / to - 1f) * 100f);
                    highlights.Add($"공격 빈도 +{percent}%");
                }
            }

            return highlights;
        }
    }
}
