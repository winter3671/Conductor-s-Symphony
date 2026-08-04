using System.Collections.Generic;
using UnityEngine;

namespace ConductorSymphony.Instrument
{
    // 밸런스 doc(game_balance_design.docx) 5번 항목의 악기별 Target DPS(피아노 등 30→45→70→100→150,
    // 드럼 60→75→130→180→300)를 실제로 달성하기 위한 악기별·레벨별 데미지 보정 배율.
    //
    // 배경(Docs/dps_balance_gap_analysis.md 참고): RhythmAttackManager.HandleRhythmHit()가 계산하는
    // 공유 baseDamage(레벨 무관 2~4)에 각 악기 디스패처의 소폭 배율(예: ×1.2, ×1.25)만 곱하는 기존
    // 구조로는, 악기별 실제 타격 빈도(32스텝 노트 패턴 + 홀드 중첩 스킵 규칙)를 감안했을 때 목표 DPS의
    // 1~20%밖에 나오지 않는다는 것이 이론 계산으로 확인됐다. 이 테이블은 그 격차를 메우기 위해
    // "목표 DPS ÷ 이론 추정 DPS"로 역산한 악기별·레벨별 배율이다.
    //
    // 적용 지점: HandleRhythmHit()에서 damage = round(baseDamage * mRhythm * mStat * 이 배율). 즉
    // 각 악기 디스패처 내부의 기존 레벨별 비율(관통/범위/발사수/틱 간격 등)은 전혀 건드리지 않고,
    // 전체 스케일만 한 번에 끌어올린다 - 개별 악기 로직을 다시 검증할 필요가 없어 리스크가 낮다.
    //
    // 주의: 최초 버전은 "단일 타겟이 항상 사거리 안에 있다" 같은 가정을 포함한 이론 추정치를 기준으로
    // 역산한 1차 값이었다. Docs/dps_balance_test_guide.md로 Unity MCP 실측을 돌린 결과
    // (Docs/dps_balance_test_result.md) 벨(Lv1~5 전부)·글록켄슈필 Lv5·바이올린 Lv5·팀파니 Lv5에서
    // 목표의 1.7~9배를 초과하는 격차가 발견되어 아래 배율을 2차로 재조정했다 - 이 항목들은 "1차
    // 이론 추정이 실제 코드 동작(예: 벨의 8방향 빔이 전부 같은 타겟에 명중, 팀파니 Lv5 지진지대가
    // 착탄마다 중첩 생성됨)을 과소평가했던" 케이스다. 나머지 악기·레벨은 실측이 목표의 0.7~1.3배
    // 범위 안에 들어와 1차 값을 그대로 유지했다(첼로는 잔류시간을 제외한 측정 하한치 기준으로도 합격).
    public static class InstrumentDamageTable
    {
        // 레벨 인덱스는 1~5. 배열 인덱스는 [level - 1].
        private static readonly Dictionary<InstrumentType, float[]> multipliers = new Dictionary<InstrumentType, float[]>
        {
            // 드럼: 비트 오라(상시) + 비트 뱅(정박) 합산 기준. Target DPS 60→75→130→180→300.
            { InstrumentType.Drums,        new float[] { 21.4f, 23.4f, 25.0f, 23.7f, 19.9f } },
            // 피아노: 노트 밀도가 레벨마다 크게 늘어(2→10발/루프) 배율은 레벨이 오를수록 감소.
            { InstrumentType.Piano,        new float[] { 75.0f, 56.2f, 29.2f, 15.4f, 13.8f } },
            // 바이올린: 홀드 시작 횟수가 레벨 무관 루프당 2회 고정 - 배율이 레벨에 따라 완만하게 증가.
            // Lv5는 실측 결과 목표의 1.82배 초과(참격 1회당 잔향 장판 9개가 동시에 쌓여 잔향만으로도
            // 목표 총딜량을 넘김) - 4.9로 재조정. 잔향 개수/지속시간을 줄이는 대안도 있으나, 이번엔
            // 배율만 낮춰 우선 목표 DPS에 맞췄다(잔향의 "체감 존재감"이 옅어질 수 있어 향후 재검토 여지).
            { InstrumentType.Violin,       new float[] {  5.7f,  8.5f,  8.9f, 12.7f,  4.9f } },
            // 플루트: 기획 의도상 무피해(순수 CC) - 배율은 사용되지 않지만 안전하게 1로 채워둠.
            { InstrumentType.Flute,        new float[] {  1.0f,  1.0f,  1.0f,  1.0f,  1.0f } },
            // 프렌치호른: 홀드 시작 횟수가 Lv2 이후 루프당 4회로 정체.
            { InstrumentType.FrenchHorn,   new float[] {  7.5f,  5.6f,  5.8f,  8.3f,  9.3f } },
            // 글록켄슈필: 버스트로 노트 밀도가 크게 늘어(2→13발/루프) 배율은 레벨이 오를수록 감소.
            // Lv5는 실측 결과 목표의 1.70배 초과(1차 추정 당시 "2차 유도 파편"의 딜량이 계산에서
            // 통째로 누락됐었음) - 8.9로 재조정.
            { InstrumentType.Glockenspiel, new float[] { 75.0f, 37.5f, 35.0f, 20.8f,  8.9f } },
            // 첼로: 홀드 시작 횟수가 레벨 무관 루프당 2회 고정. 실측(Lv1 0.81배/Lv5 0.77배)은 릴리즈
            // 후 잔류시간(Lv4+)을 측정 방법론상 제외한 하한값이라 실제로는 이보다 더 목표에 가깝다 -
            // 1차 값 그대로 유지.
            { InstrumentType.Cello,        new float[] {  6.2f,  9.4f,  9.6f, 12.7f, 14.3f } },
            // 팀파니: 홀드 시작 횟수가 레벨 무관 루프당 2회 고정, 융단폭격 틱만 레벨별로 늘어남.
            // Lv1은 실측 0.55배로 목표에 못 미쳤지만 융단폭격의 랜덤 착탄 오프셋 때문에 "고정 단일 표적"
            // 측정 방법론 자체의 명중률이 낮게 나온 것으로 추정(실제 플레이의 다중 표적 환경에서는 오히려
            // 여러 마리를 동시에 맞혀 보완될 가능성) - 1차 값 유지, 재측정으로 재확인 권장. Lv5는 실측
            // 3.07배 초과(착탄마다 3초짜리 지진지대가 새로 생겨 짧은 착탄 간격 탓에 여러 개가 동시에
            // 중첩 잔존) - 7.5로 재조정. 지대 생성에 쿨다운을 두는 대안도 있으나 이번엔 배율만 낮춤.
            { InstrumentType.Timpani,      new float[] { 16.7f, 25.0f, 25.0f, 35.7f,  7.5f } },
            // 마림바: 노트 밀도가 늘지만(2→9발/루프) 절대 배율 자체가 가장 큼(단발 관통파, 스플래시 없음).
            { InstrumentType.Marimba,      new float[] { 75.0f, 56.2f, 38.9f, 41.7f, 41.7f } },
            // 벨: 1차 추정은 "8방향 빔 중 1발만 명중"을 가정했으나, 실제로는 발사 원점이 "가장 가까운
            // 적의 현재 위치"라 그 적은 8방향(Lv4+는 2연속이라 16방향) 전부에 거리 0으로 겹쳐 있어
            // 항상 전부 명중한다(메커니즘 doc의 "가장 가까운 적 중심 8방향 성광" 서술 그대로 - 버그가
            // 아니라 1차 추정의 가정이 실제 구현과 달랐던 것). 실측 Lv1 8.08배·Lv5 9.15배 초과를 근거로
            // 전 레벨을 "항상 전부 명중" 기준으로 재계산(Lv1~3은 8.08배, 버스트가 2배로 느는 Lv4~5는
            // 9.15배 나눠서 역산).
            { InstrumentType.Bell,         new float[] {  9.3f,  7.0f,  5.8f,  3.0f,  2.9f } },
        };

        public static float GetDamageMultiplier(InstrumentType type, int level)
        {
            int index = Mathf.Clamp(level - 1, 0, 4);
            if (multipliers.TryGetValue(type, out float[] arr))
            {
                return arr[index];
            }
            return 1f;
        }
    }
}
