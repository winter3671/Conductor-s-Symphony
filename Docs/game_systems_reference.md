# 게임플레이 시스템 종합 정리 — 악기 메커니즘 · 밸런스 · 패시브 · 리팩토링 · 팀 리뷰

이 문서는 그동안 `Docs/`에 흩어져 있던 5개의 개별 요약/분석 문서를 하나로 통합한 것입니다. 원본은
각자 다른 시점에 다른 목적(구현 정리, 밸런스 분석, 패시브 연동, 리팩토링 제안, 팀 리뷰 목록)으로
작성됐지만, 지금은 전부 "현재 시스템이 어떤 상태인가"를 설명하는 상시 참고 문서라는 점에서 하나로
모아두는 게 찾아보기 편합니다. 원본 5개 파일(`dps_balance_gap_analysis.md`,
`instrument_mechanics_implementation_summary.md`, `range_passive_implementation_summary.md`,
`refactoring_recommendations.md`, `team_review_needed.md`)은 `Docs/archive/`로 이동했습니다.

기획서를 md로 변환한 3개 문서(`10종 악기별 공격 메커니즘 기획서.md`, `game_balance_design.md`,
`리듬 뱀서 리듬 노트 종합 설계 가이드.md`)는 이번 통합 대상이 아닙니다 — 저 셋은 "기획 의도 원본"이고
이 문서는 "그 기획을 실제로 어떻게 구현했고 지금 상태가 어떤가"를 다루는 별개의 문서입니다.

각 섹션 하단에는 원본이 어느 문서였는지, 그리고 관련 실측 검증 로그가 `archive/` 어디에 있는지
표기해뒀습니다.

---

## 1. 시스템 구조 개요

모든 악기 공격은 판정 성공 이벤트(`RhythmManager.OnHitSuccessEvent`)를
`RhythmAttackManager.HandleRhythmHit()`가 받아서 처리합니다. 악기는 노트 입력 방식에 따라 두 그룹으로
나뉩니다.

- **탭(Tap) 기반 5종** — 피아노, 벨, 마림바, 글록켄슈필, 드럼. 판정 성공 즉시 1회성 공격이 발동합니다.
  `InstrumentAttackDispatcher`가 `Dictionary<InstrumentType, ITapAttackEffect>` 룩업으로 처리합니다.
- **홀드(Hold) 기반 5종** — 바이올린, 프렌치호른, 첼로, 팀파니, 플루트. 판정 성공 시점이 홀드
  "시작"이고, 이후 키를 누르고 있는 동안 매 프레임 유지 효과가, 떼는 순간 해제 효과가 발동합니다.
  `IHoldAttackEffect` + `HoldEffectCoordinator`(레인별 지속 이펙트 관리)가 담당합니다.

드럼만 예외적으로 위 두 파이프라인과 완전히 별개인 **판정과 무관한 상시 효과**(비트 오라)를 하나 더
가지고 있으며, 별도 컴포넌트 `DrumAuraController`가 매 프레임 독립적으로 처리합니다.

딜량 계산 자체(기본 DPS × M_rhythm × M_stat × 악기별 DPS 보정 배율, 8종 패시브 스탯)는 순수 함수
`DamageFormula`로 분리되어 있고, 이 부분은 밸런스 doc 1번·4번 항목을 그대로 따릅니다.

주요 공용 부품:
- `CombatTargetingUtility` — 가장 가까운 적 / 체력 최고 적 탐색 + 패시브 배율 3종(범위/쿨타임/지속시간)
  래핑 (4절 참고)
- `PiercingBeamProjectile` — 직선 관통 투사체(피아노/벨/마림바/바이올린 참격이 공유)
- `AreaImpactEffect` — 지연 낙하 광역 피해(글록켄슈필/팀파니가 공유)
- `LingeringZoneEffect` — 잔류 장판(바이올린/팀파니/벨 Lv5가 공유)
- `IHoldAttackEffect` + `HoldEffectCoordinator` — 홀드 5종의 시작/유지/해제 생명주기 관리
- `ProceduralSpriteFactory.CreateUnitRing` — scale=1일 때 정확히 반지름 1 월드유닛인 링 스프라이트
  (4절 참고)

*(원본: `instrument_mechanics_implementation_summary.md` §1)*

---

## 2. 악기별 구현 내역

각 악기마다 (a) 기획서 요약 (b) 실제 구현 (c) 밸런스 doc 레벨별 요구사항 대비 실제 구현 표를 함께
정리했습니다. 표의 판정 기준: **일치** = 방향/구조가 doc과 부합, **부분일치** = 같은 스탯을 다루지만
수치 곡선이나 적용 방식이 다름, **미구현** = 해당 레벨 효과가 코드에 없음.

### 드럼 (Drums) — Target DPS 60→75→130→180→300
플레이어 중심 360°. 상시 "비트 오라"(근접 적에게 소량 지속 타격, `DrumAuraController`) + 정박(1,5,9,13)
타격 시 "비트 뱅"(장판 팽창 + 전방위 넉백). 오라는 판정 성공 여부와 완전히 무관하게 항상 켜져 있고,
M_rhythm은 의도적으로 미반영(시포르찬도 M_stat만 반영). 오라·비트 뱅 둘 다 실제 판정 반경에 맞춰
정확히 그려지는 링 비주얼을 갖고 있습니다(4절 참고).

| Lv | 밸런스 doc 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 충격파 피해량·범위 +20% | 비트 뱅 반경·데미지 모두 1회성 +20% | 일치 |
| 3 | 넉백 거리 2배 + 0.5초 둔화 | 넉백 임펄스 2배 + 0.5초 50% 한시적 감속 | 일치 |
| 4 | 비트 오라 지속 피해량 +50% | `auraLevelMultiplier`로 오라 데미지 +50% | 일치 |
| 5 | 정박 타격 시 2연속 중첩 충격파 | 비트 뱅 2회 연속 발동 | 일치 |

### 피아노 (Piano) — Target DPS 30→45→70→100→150
가장 가까운 적 방향 관통 레이저. 전역 콤보 6배수 성공 시 "건반 폭포"(3발 부채꼴 추가 발사).

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 레이저 피해량 +25% | 전용 데미지 +25% 스케일링 | 일치 |
| 3 | 관통 횟수 +2 | 관통 2→4 | 일치 |
| 4 | 발사 수 +1 | 1발→2발 | 일치 |
| 5 | 6연타 성공 시 레이저 폭포 | 전역 콤보 6배수마다 부채꼴 3발 추가 | 일치(전역 콤보 이슈는 3절 2항) |

### 바이올린 (Violin) — Target DPS 30→45→70→100→150
13칸 롱노트. 홀드 중 플레이어 둘레 회전 활 칼날(0.35초 재타격 쿨다운), 릴리즈 시 이동 방향 부채꼴
참격. Lv5 참격 경로에 잔향 장판 3개(2초).

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 칼날 범위 +20% & 회전 속도 증가 | 반경 +20% & 회전 속도 +30% | 일치 |
| 3 | 칼날 개수 +1(총 3개) | 2개→3개 | 일치 |
| 4 | 참격 "크기" +50% | 발수 3발 고정, `sizeMultiplier` +50% | 일치 |
| 5 | 참격 자리에 2초 검기 잔향 | `LingeringZoneEffect` 3개, 2초간 지속 타격 | 일치 |

### 플루트 (Flute) — Target DPS 30→45→70→100→150
2~4칸 숏 홀드(3칸으로 근사). 릴리즈 시 미니 소용돌이 생성, 순수 CC(무피해).

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 유지시간 +40% | 지속시간 1회성 +40% | 일치 |
| 3 | 흡입 범위 & 당기는 힘 +50% | 범위·당김 세기 모두 1회성 +50% | 일치 |
| 4 | 동시 유지 소용돌이 +1개 | `activeVortices` 정적 리스트로 최대 2개, 초과 시 가장 오래된 것 정리 | 일치 |
| 5 | 소멸 시 바람 파편 폭발 | 무피해 외곽 넉백 | 일치 |

### 프렌치호른 (French Horn) — Target DPS 30→45→70→100→150
6칸 스웰 롱노트. 홀드 중 이동 방향 전방 부채꼴에 지속 충격파 + 넉백.

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 사거리 +25% | 사거리 1회성 +25% | 일치 |
| 3 | 넉백 거리 +40% | 넉백 속도 +40% | 일치 |
| 4 | 범위 내 적 피해량 +15% 증폭 디버프 | `SetDamageAmpMultiplier(1.15f)`, 범위 이탈/릴리즈 시 해제 | 일치 |
| 5 | 충격파 각도 120°→180° 확장 | Lv5에서 확장 | 일치 |

### 글록켄슈필 (Glockenspiel) — Target DPS 30→45→70→100→150
체력 최고 적 머리 위 별빛 낙하. 전역 콤보 4배수(Lv4+) 성공 시 별 추가 낙하.

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 별빛 낙하 피해량 +30% | 전용 데미지 +30% 스케일링 | 일치 |
| 3 | 스플래시 피해 추가 | 스플래시 반경 확대로 구현 | 일치 |
| 4 | 버스트 시 별빛 수 +2개 | 항상 +2개 | 일치 |
| 5 | 2차 유도 파편 폭발 + 0.5초 기절 | `HomingShrapnelProjectile`, 다른 적 없으면 스킵 | 일치 |

### 첼로 (Cello) — Target DPS 30→45→70→100→150
13칸 베이스 롱노트. 홀드 중 최근접 적 발밑에 고정 중력장(적을 따라가지 않음), 이속 감소 + 지속 타격.

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 1 | 이속 감소 40% | 40% | 일치 |
| 2 | 범위 +20% | 반경 1회성 +20% | 일치 |
| 3 | 감소 40%→60% | 60% | 일치 |
| 4 | 중력장 지속시간 +30% | 홀드 릴리즈 후 필드가 즉시 사라지지 않고 잔류시간(1.0초×1.3)만큼 유지되는 방식으로 재해석 | 부분일치(해석 다름 — 설계자 확인 필요, 7절 2-1) |
| 5 | 중앙으로 지속 끌어당김 | 범위 내 적을 중앙으로 지속 끌어당김 | 일치 |

### 팀파니 (Timpani) — Target DPS 30→45→70→100→150
홀드 시작 = 즉발 "캐논", 홀드 유지 = "융단폭격"(랜덤 오프셋 소형 착탄 연속). 스플래시·오프셋에는
크레센도 패시브를 항상 같은 배율로 곱해 명중 확률(≈38.5%)을 보존합니다(4절).

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 포탄 낙하 범위 +25% | 반경 +25% | 일치 |
| 3 | 폭격 빈도 +50% | 빈도 +50% | 일치 |
| 4 | 포탄에 맞은 적 1초 기절 | `ApplyStun(1.0f)` | 일치 |
| 5 | 낙하 지점 3초간 지진 지대 잔류 | `LingeringZoneEffect` 3초간 지속 타격 | 일치 |

### 마림바 (Marimba) — Target DPS 30→45→70→100→150
3, 11번 엇박자 단타. 이동 방향 일직선 관통 파동.

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 관통 수 +2 | 관통 3→5 | 일치 |
| 3 | 엇박 성공 시 파동 크기 +30% | `sizeMultiplier = 1.3f` 분기(과거 공백 레벨이었던 버그 수정 완료) | 일치 |
| 4 | 화면 끝 1회 바운스 | 그대로 구현 | 일치 |
| 5 | 이속 30% 감소 + 밀쳐냄 | 동시 적용 | 일치 |

### 벨 (Bell) — Target DPS 30→45→70→100→150
5, 13번 엇박자 단타. **가장 가까운 적의 현재 위치**를 발사 원점으로 8방향 성광(중요 — 7절 2-5 참고).

| Lv | 요구사항 | 실제 구현 | 판정 |
|---|---|---|---|
| 2 | 사거리 +30% | 사거리 1회성 +30% | 일치 |
| 3 | 관통력 강화 | 관통 3→5 | 일치 |
| 4 | 엑센트 박자 타격 시 8방향 2연속 발사 | 엑센트 타이밍 구분 없이 Lv4 이상이면 항상 더블 버스트 | 부분일치(엑센트 판정 미구현, 7절 2-2) |
| 5 | 지나간 자리 1.5초 발광 지속 타격 | 중심점에 `LingeringZoneEffect` 1.5초간 지속 타격 | 일치 |

*(원본: `instrument_mechanics_implementation_summary.md` §2, §4, §6)*

---

## 3. 메커니즘 doc과 달라진 점 (공통 사항)

1. **레벨별 수치는 전부 임의값**: 메커니즘 doc은 정성적 설명만(예: "관통력 강화") 제공하고, 구체적
   수치는 밸런스 doc 5번 항목 기준으로 구현했습니다.
2. **전역 콤보 카운터(악기별 콤보 아님)**: 피아노 "6연타 캐스케이드", 글록켄슈필 "4/8버스트"는 해당
   악기 자신의 연속 히트가 아니라 모든 레인을 통틀은 전역 콤보(`RhythmManager.CurrentCombo`)를
   재사용합니다. 다른 악기로 쌓은 콤보가 트리거할 수 있습니다.
3. **팀파니의 두 메커니즘 통합**: 메커니즘 doc은 "단타 캐논"과 "홀드 롤ing 폭격"을 다른 노트 특성으로
   설명하지만, 홀드 인프라는 악기당 1가지 입력 모드만 지원해 "홀드 시작=캐논, 유지=폭격"으로 합쳤습니다.
4. **오프비트(엇박) 구분 없음**: 마림바(3,11번)·벨(5,13번)은 노트 패턴 자체가 이미 해당 스텝에 배치돼
   있을 뿐, "엇박 타이밍에 맞았는가"를 별도 판정하지는 않습니다.
5. **플루트 "지나간 자리" 근사**: 실제 이동 궤적을 기록하는 인프라가 없어 "바라보는 방향 반대쪽 고정
   오프셋(0.8유닛)"으로 근사했습니다.
6. **Lv5 전용 추가 효과 전부 구현 완료**: 벨/마림바/글록켄슈필/바이올린/프렌치호른/첼로/팀파니/드럼/
   플루트의 Lv4~5 부가 효과 전부 구현 및 실측 PASS.
7. **프렌치호른 부채꼴 시각화 = 원형 근사**: 피해 판정은 정확한 부채꼴이지만, 화면 표시는 반투명 원형
   스프라이트로 근사.
8. **드럼 오라는 M_rhythm 미적용**: 판정 성공과 무관한 상시 효과라 리듬 정확도 배율은 제외하고
   시포르찬도(M_stat)만 반영.
9. **레가토(Legato) 패시브 / 악기 Lv4 "Multi+1" 스탯 — 2026-08-07 연동 완료**: "투사체 수 +1/+2"와
   각 악기 Lv4의 `extraProjectiles`는 과거 범용 폴백 투사체 로직에서만 소비되던 값이었으나(그 폴백
   로직은 신규 악기 추가 대비 안전장치로 코드에 의도적으로 남아있음 — 6절 "완료 항목" 참고), 이제
   6종 악기의 실제 디스패처가 이 값을 직접 소비합니다. 상세 내용은 4-5절 참고. 8종 패시브 전부
   해결 완료.
10. **팀파니 홀드 겹침 방지로 레벨별 패턴 밀도가 사실상 무효화 — 2026-08-08 발견 및 수정.**
    `RhythmManager.ProcessSequencerStep`은 같은 레인에 홀드가 이미 진행 중이면 새 온셋을 조용히
    무시합니다(정상적인 방어 로직). 문제는 팀파니의 홀드 길이(`holdLengthSteps`)가 기존 16스텝 =
    32스텝 사이클의 정확히 절반이었고, Lv1~5 패턴 전부에 스텝0/16 온셋이 공통으로 있어서, **레벨과
    무관하게 항상 정확히 2회/사이클만 실제 홀드가 발동**했습니다(Lv3~5가 추가한 "격렬해지는" 온셋들은
    전부 겹쳐서 스킵됨 - 패턴 주석의 "8 hits"/"12 hits" 같은 문구는 처음부터 실제 발동 횟수와 무관한
    수치였습니다). 결과적으로 팀파니는 항상(모든 레벨) 한 치의 쉬는 구간도 없이 홀드가 바로 이어
    붙는 구조였고, 사용자의 실측 리포트("고레벨일수록 키다운이 끝나자마자 바로 다음 키다운이 옴")로
    발견됐습니다. 사용자 결정(2026-08-08)에 따라 `holdLengthSteps[Timpani]`를 16→10으로 줄여서
    32스텝 사이클마다 "홀드 10칸 + 휴식 6칸"이 2회 반복되도록 했습니다 — 레벨별 강도 차이는 패턴
    밀도가 아니라 `TimpaniBombardmentEffect` 자체의 레벨별 수치(범위/빈도/기절/지진지대)로 이미
    표현되고 있어 그대로 둡니다. 패턴 문자열과 주석은 그대로 두되(장식성 참고 자료), 주석의 실제
    발동 횟수만 정정했습니다.
11. **바이올린/첼로도 같은 홀드 과밀 버그 확인, 수정 — 2026-08-08.** 팀파니 수정 직후 사용자 요청으로
    홀드 4종(바이올린/프렌치호른/첼로/플루트) 전체를 시뮬레이션 재점검한 결과, 바이올린/첼로가 팀파니와
    완전히 같은 원인(홀드 길이가 온셋 간격보다 넓어 레벨과 무관하게 항상 2회/사이클·휴식 3스텝 고정)을
    갖고 있음을 확인했습니다. `holdLengthSteps`를 13→11로 축소 - Lv1~3은 휴식 3→5스텝, Lv4~5는 온셋
    간격(4스텝)이 새 홀드 길이보다 좁아지며 평균 2.7회/사이클(휴식 1스텝)까지 자연스럽게 늘어나
    레벨업 밀도 상승이 처음으로 작동합니다. 프렌치호른/플루트는 반대로 이미 레벨이 오를수록 실제
    발동 횟수도 늘어나고 있어(온셋 간격이 홀드 길이보다 좁음) 버그가 아니라고 판단해 그대로 뒀습니다.
    Unity MCP 실측 PASS(`archive/violin_cello_hold_density_fix_test_guide.md`) - Lv1~3 휴식 3→5스텝,
    Lv4~5 평균 2.75회/사이클(예측 2.7과 사실상 일치)까지 실측으로 확인됨.

*(원본: `instrument_mechanics_implementation_summary.md` §3)*

---

## 4. 패시브 스탯 시스템

8종 패시브(시포르찬도/비바체/레가토/레조넌스/튜닝/알레그로/크레센도/페르마타) 중 처음부터 정상
동작했던 건 5종(시포르찬도-데미지, 비바체-이속, 레조넌스-픽업범위, 튜닝-피해감소/최대체력)뿐이었고,
**크레센도(범위)·알레그로(쿨타임 감축)·페르마타(지속시간 증가)·레가토(투사체 수 증가) 4종은 계산
함수(`PassiveStatManager`)는 정확했지만 그 값을 실제로 소비하는 코드가 프로젝트 전체에 단 한 곳도
없어 완전히 죽어있었습니다.** 드럼의 범위 표시 버그를 조사하다가 크레센도가 죽어있음을 처음
발견했고, 이후 나머지 7종 패시브 전부를 "실제 소비 코드 유무" 기준으로 감사해 알레그로·페르마타·
레가토도 같은 상태임을 추가로 확인했습니다. **지금은 8종 전부 연동을 마쳤습니다.**

### 4-1. 정확한 반경-비주얼 매칭 인프라

기존 `ShockwaveVisualEffect`(비트 뱅)는 `radius * 2f`라는 근사 배율을 썼는데, 실제 스프라이트
(`CreateRingWithCore`, 픽셀 좌표 기반)와 비례하지 않아 화면상 반경이 의도치의 약 0.3배로 그려지고
있었습니다. `ProceduralSpriteFactory.CreateUnitRing(innerRadius01, outerRadius01, color)`를 신설해
`scale=1`일 때 링 바깥쪽 끝이 정확히 "월드 유닛 반지름 1"이 되도록 픽셀 좌표계를 재설계했습니다.
`transform.localScale = Vector3.one * 실제반경`만 하면 항상 정확히 일치합니다. 장식용 링(보스/엘리트
상자/보스 투사체/악기 아이템)은 정확한 반경 매칭이 필요 없어 기존 `CreateRingWithCore`를 그대로 뒀습니다.
드럼 오라 링(`DrumAuraController`)과 비트 뱅 모션(`ShockwaveVisualEffect`) 둘 다 이 인프라로 다시
작성해, 매 프레임 실제 판정 반경에 맞춰 갱신됩니다.

**프렌치호른·첼로·플루트 확장(2026-08-07)**: 이 세 악기도 홀드 중 반투명 채워진 원
(`CreateFilledCircle`)으로 범위를 근사 표시하고 있었는데, 같은 종류의 스케일 버그가 있었습니다 —
프렌치호른은 실제 사거리의 약 8.8%, 첼로/플루트는 약 11.7% 크기로만 그려지고 있었습니다. 채워진 원
자체는 유지하고(장식으로서의 가치는 있으므로), 그 위에 드럼과 동일한 `CreateUnitRing(0.985f, 1f,
...)` 얇은 테두리 링을 **자식 오브젝트**로 추가했습니다. 자식의 `localScale`을 부모의 근사 배율
(`range × 0.8` 또는 `radius × 0.9`)을 상쇄하는 값(`1 / 0.8 = 1.25`, `1 / 0.9 ≈ 1.111`)으로 고정해,
부모-자식 스케일이 곱해진 최종 월드 반경이 항상 정확히 실제 판정 반경과 일치하도록 만들었습니다 —
크레센도 패시브로 반경이 커져도 부모 스케일만 바뀌면 자동으로 같이 커집니다. 실측으로 소수 5자리까지
정확히 일치함을 확인했고(`archive/range_ring_precision_test_guide.md`), 자식 오브젝트라 부모가
`Destroy()`될 때 별도 처리 없이 함께 정리됩니다.

**바이올린·팀파니 확장(2026-08-07)**: 이 둘은 기존에 채워진 원조차 없었습니다. 바이올린은
`transform.localScale`이 항상 1로 고정된 구조(칼날이 각도만 바뀌며 배치됨)라 부모 스케일 상쇄 계산
없이 `radius`를 링의 `localScale`에 그대로 곱하면 됐습니다. 팀파니는 "융단폭격" 착탄 오프셋
(±1.0×크레센도 배율의 **정사각형** 범위)을 프렌치호른의 부채꼴 근사와 같은 방식으로 반지름
1.0×배율의 원으로 근사 표시했고, 이 컴포넌트의 GameObject transform은 다른 용도로 쓰이지 않아
자식 링의 월드 위치를 `targetPos`(홀드 시작 시점 고정 위치)로 직접 지정했습니다. 실측으로 반경
정확도·추종/고정 위치·크레센도 스케일링·레가토(추가 칼날/캐논)와의 무간섭까지 전부 확인했습니다
(`archive/violin_timpani_range_ring_test_guide.md`). 이로써 지속 유지형 악기 6종(드럼/프렌치호른/
첼로/플루트/바이올린/팀파니) 전부에 정확한 범위 인디케이터가 생겼습니다.

### 4-2. `CombatTargetingUtility`의 패시브 배율 헬퍼 3종

```csharp
public static float GetRangeMultiplier()      // 크레센도: 사거리/반경에 곱함 (1.0 + 0.10×Lv)
public static float GetCooldownMultiplier()   // 알레그로: interval에 곱함, 값이 작을수록 자주 발동
public static float GetDurationMultiplier()   // 페르마타: duration에 곱함 (1.0~1.75)
```

### 4-3. 10종 악기 연동 현황

| 악기 | 크레센도(범위) | 알레그로(쿨타임) | 페르마타(지속시간) |
|---|---|---|---|
| 드럼 | 오라·비트 뱅 반경 | 오라 tick interval | - |
| 피아노 | 빔 사거리 | - | - |
| 벨 | 8방향 빔 사거리, Lv5 잔향 반경 | Lv5 잔향 tick | Lv5 잔향 duration |
| 마림바 | 파동 사거리 | - | - |
| 글록켄슈필 | 스플래시 반경 | - | - |
| 바이올린 | 칼날 반경, 참격 사거리, Lv5 잔향 반경 | 재타격 쿨다운, Lv5 잔향 tick | Lv5 잔향 duration |
| 프렌치호른 | 부채꼴 사거리 | 부채꼴 tick interval | - |
| 첼로 | 중력장 반경 | 필드 tick interval | Lv4 잔류시간 |
| 팀파니 | 캐논 반경, Lv5 지진지대 반경, 융단폭격 스플래시·오프셋 | 폭격 간격, Lv5 지진지대 tick | Lv5 지진지대 duration |
| 플루트 | 흡입 반경 | - | 소용돌이 유지시간 |

**적용하지 않은 것(판단 근거)**: 글록켄슈필/팀파니의 착탄 지연(0.05~0.15초, 발동 주기가 아니라 연출
지연)은 알레그로 대상에서 제외. `pullStrength`·`slowFraction` 같은 세기 값은 셋 중 어느 패시브와도
무관해 제외. 홀드 노트의 `HoldDurationSeconds`는 페르마타를 곱하면 플레이어가 더 오래 버텨야 해서
오히려 불리해지므로 절대 곱하지 않음.

**팀파니 융단폭격 — "동일 배율" 결정**: 명중 확률은 스플래시 원 넓이 ÷ 오프셋 정사각형 넓이 비율
(≈38.5%)로 정해지므로, 스플래시와 오프셋에 정확히 같은 배율을 곱해야 이미 튜닝된 이 확률이 배율과
무관하게 유지됩니다(스플래시만 늘리면 ~85%, 오프셋만 늘리면 ~17%로 계산됨). 실측(40회 반복,
크레센도 미보유 25.0% vs 만렙 37.5%)으로 이론치 근방임을 확인.

**바이올린 Lv5 잔향 / 팀파니 Lv5 지진지대**: "범위 공격이 지나간 자리에 남는 잔류 효과"라는 성격상
범위·쿨타임·지속시간 패시브 전부의 적용 대상에 포함하기로 결정.

### 4-4. 레가토(Legato) — 투사체 수 +1/+2

레가토와 각 악기 Lv4의 `extraProjectiles`("Multi +1")는 과거 범용 폴백 투사체 로직에서만 소비되던
값이었고, 10종 전체가 전용 디스패처로 처리되면서 이 폴백 로직에 도달하지 않게 되자 UI에는 "Multi
+N"이 표시되지만 실제로는 아무 효과가 없는 상태였습니다(전체 패시브 감사에서 발견).

크레센도/알레그로/페르마타와 달리 "범위/주기/지속시간에 배율을 곱하는" 방식이 아니라 "발사체 개수를
늘리는" 방식이라, 애초에 "낱개로 셀 수 있는 발사체/낙하체"가 있는 악기에만 적용할 수 있습니다. 이
기준으로 10종을 나눴습니다.

**적용 6종** (`ITapAttackEffect.Execute()` / `IHoldAttackEffect.Init()`에 `int extraProjectiles`
파라미터를 추가해 전달):

| 악기 | 처리 방식 |
|---|---|
| 피아노 | 관통 레이저 발사 수(`shots`)에 그대로 가산 |
| 벨 | 기존 8방향 사이 빈 각도(22.5° 간격)를 채우는 추가 성광 |
| 마림바 | 원본 파동과 평행하게 좌우로 갈라지는 추가 파동(이동 방향 수직으로 0.6유닛 오프셋) |
| 글록켄슈필 | Lv4 버스트와 동일한 랜덤 오프셋 낙하 지점 추가(버스트 조건과 무관하게 항상 적용) |
| 바이올린 | 궤도 칼날 개수(`bladeCount`)에 그대로 가산 |
| 팀파니 | 홀드 시작 시 캐논 착탄을 랜덤 오프셋으로 추가 발사 |

**제외 4종** — 지속형 판정(오라·부채꼴·고정 필드·소용돌이)이라 "낱개 투사체" 개념이 원래 없음(사용자
결정, 2026-08-07): 드럼, 프렌치호른, 첼로, 플루트. 인터페이스 시그니처는 파라미터를 받지만 본문에서
사용하지 않습니다.

`extraProj` 값 자체는 기존 `extraDamage`(7-1절 `GetTotalExtraDamage()` 이슈)와 동일하게 "지금 판정된
악기"가 아니라 장착된 4슬롯 전체의 합산치입니다 — 이번 작업에서 이 집계 방식 자체는 손대지 않았습니다.

### 4-5. 검증 완료

크레센도(2라운드), 알레그로/페르마타(1라운드, private 필드 리플렉션 + tickTimer 리셋 경계 탐지 방식),
프렌치호른·첼로·플루트 범위 링 정확도(1라운드), 레가토(1라운드, 리플렉션으로 `Execute`/`Init` 직접
호출해 생성 오브젝트 수 델타 측정), 바이올린·팀파니 범위 링(1라운드) 모두 Unity MCP 실측 PASS.
상세 수치는 `archive/drum_range_visualization_test_guide.md`,
`archive/instrument_range_passive_test_guide.md`, `archive/allegro_fermata_passive_test_guide.md`,
`archive/range_ring_precision_test_guide.md`, `archive/legato_extra_projectile_test_guide.md`,
`archive/violin_timpani_range_ring_test_guide.md` 참고.

**남은 고려 사항**: DPS 밸런스(5절)는 "크레센도/알레그로/페르마타/레가토 전부 미보유" 기준으로만
검증되어 있습니다. 네 패시브를 적극적으로 찍는 빌드의 종합 밸런스(사거리 확장으로 인한 다중 명중
증가, 발동 빈도 증가, 잔류시간 증가, 발사체 개수 증가로 인한 총 딜량 변화)는 별도 확인이 필요할 수
있습니다. 상시 범위 인디케이터는 이제 지속 유지형 악기 6종(드럼/프렌치호른/첼로/플루트/바이올린/
팀파니) 전부에 있습니다. 나머지 4종(피아노/벨/마림바/글록켄슈필)은 일회성 투사체/파동이라 발사
궤적 자체가 사거리를 보여주므로 상대적으로 우선순위가 낮다고 판단해 대상에서 제외했습니다 —
필요하면 다음 라운드 후보로 남아있습니다.

*(원본: `range_passive_implementation_summary.md`, `archive/allegro_fermata_passive_test_guide.md`,
`archive/range_ring_precision_test_guide.md`, `archive/legato_extra_projectile_test_guide.md`,
`archive/violin_timpani_range_ring_test_guide.md`)*

---

## 5. DPS 밸런스 갭 분석 및 보정

### 5-1. 문제의 근원

모든 악기가 공유하는 기본 데미지 공식은 레벨과 무관하게 2~4 사이였습니다(`baseDamage = (Perfect?
2:1) + extraDamage`, `extraDamage`는 Lv1~2: +0, Lv3~4: +1, Lv5: +2). 밸런스 doc의 Target DPS(예:
피아노 30→45→70→100→150, 드럼 60→75→130→180→300)는 이 스케일로는 애초에 도달 불가능한 크기였습니다.

### 5-2. 계산 방법론과 1차 결과

32스텝 노트 패턴을 문자열째로 파싱하고, 홀드형 4종(바이올린/첼로/팀파니 등)은 "같은 레인에 이미
홀드가 진행 중이면 새 노트를 스폰하지 않는" 규칙까지 4루프 시뮬레이션으로 반영해 발동 횟수를 정확히
확정했습니다. 그 결과 **바이올린·첼로·팀파니는 레벨을 올려도 루프당 홀드 시작 횟수가 2회로 고정**되어,
이 3종은 DPS 성장을 전부 "홀드 1회당 데미지"로만 만들어야 한다는 제약이 드러났습니다.

이론 추정 결과, **거의 모든 악기·레벨이 목표 DPS의 1~20% 수준(10~100배 부족)**이었습니다 — 가장 심한
쪽(피아노·마림바·벨·글록켄슈필)은 1~7%, 가장 나은 쪽(바이올린·프렌치호른·첼로)도 8~18%.

### 5-3. 해결: 단일 진입점 보정

개별 악기의 관통/범위/발사수/틱 간격 로직은 전혀 건드리지 않고, `InstrumentDamageTable.cs`(신규)가
악기·레벨별 배율을 반환하도록 만들어 `RhythmAttackManager.HandleRhythmHit()`의 데미지 계산 지점
딱 한 곳에 곱했습니다. 배율은 "목표 DPS ÷ 이론 추정 DPS"로 역산. 드럼은 "비트 뱅 + 비트 오라"가 서로
다른 코드 경로를 타므로, 두 경로 모두에 같은 배율을 적용해야 했습니다(오라 몫을 빠뜨리면 계속 목표
미달). 플루트는 기획 의도상 무피해(CC 전용)라 배율 1.0.

### 5-4. 실측 3라운드

| 라운드 | 대상 | 결과 |
|---|---|---|
| 1차 | 10종 × Lv1/Lv5 | 6종 PASS(드럼/피아노/마림바/프렌치호른/첼로/플루트) / **벨(전레벨)·글록켄슈필Lv5·바이올린Lv5·팀파니Lv5 FAIL(1.7~9배 초과)** / 팀파니Lv1 경계미달 |
| 2차 | 위 4개 항목 배율 재조정 | 코드 반영 완료 |
| 3차 | 벨 Lv1/Lv3/Lv5 + 나머지 Lv5 재측정 | **전부 PASS(0.86~1.05배)** |

**벨**: 8방향 발사 원점이 "가장 가까운 적의 현재 위치"라, 그 적은 8(Lv4+는 16)방향 전부와 거리 0으로
겹쳐 있어 항상 전부 명중(메커니즘 doc 서술과 정확히 일치 — 버그 아님). 실측 8.08~9.15배 초과 →
배율을 그 비율로 재계산(예: Lv1 75.0→9.3).
**글록켄슈필 Lv5**: 유도 파편 딜량이 1차 추정에서 통째로 누락, 1.70배 초과 → 15.2→8.9.
**바이올린 Lv5**: 잔향 장판 9개 동시 생성, 1.82배 초과 → 9.0→4.9.
**팀파니 Lv5**: 착탄마다(≈0.43초) 3초짜리 지진지대가 새로 생겨 중첩, 3.07배 초과 → 23.1→7.5.

**결론: DPS 밸런스 갭은 2차 배율 재조정으로 전부 해소됐습니다. 추가 코드 수정은 필요 없습니다.**
남은 것은 "바이올린/팀파니 Lv5 기믹의 존재감이 옅어질 수 있다"는 설계 감각 문제뿐이며(7절 3-1), 이는
실제 플레이테스트 후 판단할 사안입니다.

*(원본: `dps_balance_gap_analysis.md`, 상세 로그: `archive/dps_balance_test_guide.md`,
`archive/dps_balance_test_result.md`)*

---

## 6. 코드 구조 & 리팩토링 현황

`Assets/Scripts/` 전체(44개 파일)를 분석해 나온 제안 중, 아래 항목들은 **이후 세션에서 실제로 반영
완료**됐습니다.

### 완료된 항목

| 항목 | 반영 내용 |
|---|---|
| 탭/홀드 디스패치 통합 | 탭 5종을 `ITapAttackEffect` 인터페이스 + `Dictionary<InstrumentType, ITapAttackEffect>` 룩업으로 전환, 홀드 5종과 대칭 구조로 통일 |
| `RhythmAttackManager` 책임 분리 | 데미지 공식을 순수 함수 `DamageFormula`로 추출, 드럼 상시 오라 로직을 `DrumAuraController` 별도 컴포넌트로 분리 |
| `FindObjectsByType<EnemyMonster>()` 중복 호출 통합 | 10개 파일·15곳을 전부 `EnemySpawner.Instance.ActiveEnemies` 재사용으로 통일 |
| 죽은 코드 제거(일부) | `RhythmAttackManager.FindNearestEnemy()`(중복 코드), `InstrumentOrbit.SetAngle()`(빈 메서드) 삭제 완료. **단, 범용 투사체 폴백 로직은 신규 악기 추가 대비 안전장치로 의도적으로 남겨둠**(3절 9항 레가토와 연결된 부분이라 그대로 유지) |

리팩토링 회귀 검증(Unity MCP)도 PASS로 완료됐습니다 — `archive/refactoring_regression_test_guide.md`.

### 아직 남은 항목

- **악기별 레벨 스케일링 수치의 데이터화 - 2026-08-09 완료.** `Assets/Scripts/Instrument/
  InstrumentLevelStats.cs` 신설 - `InstrumentDamageTable.cs`와 동일한 관례(`Dictionary<InstrumentType,
  T[]>`, 배열 인덱스 `[level-1]`)로 10개 이펙트 클래스에 흩어져 있던 `(level >= N ? A : B)` 삼항식
  30곳(범위/피해량/관통/발사수/넉백/크기/틱간격 배율)을 전부 데이터화했습니다. `if(level>=N){동작()}`
  형태의 행동 분기 9곳은 로직이라 코드에 그대로 둠(데이터화 실익 없음). 순수 추출이라 밸런스 변경
  없음 - Python으로 기존 삼항식 vs 새 테이블 조회를 레벨 1~5 전부(150개 케이스) 교차검증해 완전히
  일치함을 확인. **Unity MCP 실측 PASS(2026-08-09)**: 컴파일 에러/경고 0건, 10종 전부 Lv1~5 API
  실측값이 표와 100% 일치, Play 모드 실전 배선(홀드 5종 리플렉션 실측 포함)까지 정상 확인.
  검증 가이드: `archive/instrument_level_stats_dataification_test_guide.md`.
- **`EnemyMonster`의 상태이상 필드 일반화**: `speedMultiplier`/`stunTimer`/`damageAmpMultiplier`/
  `tempSlowTimer` 등이 악기가 늘 때마다 필드 쌍으로 즉흥적으로 추가되고 있습니다. 범용
  `StatusEffect` 리스트 + 우선순위 리듀서로 일반화하는 걸 고려할 만하지만, 설계 변경 성격이 커서
  신중한 검토가 필요합니다(장기 과제).
- **타이머/자동소멸 이펙트 보일러플레이트 중복**: 틱 누적 패턴 5곳, 경과시간 기반 자동 소멸 패턴
  6곳이 각자 구현되어 있습니다. 작은 공용 헬퍼(`Ticker` 구조체 등)로 추출 가능하지만 우선순위는
  낮습니다.

### 선택적/장기 검토 사항

- **벨의 발사 원점=타겟 위치 구조**: DPS 배율로 수치는 맞췄지만, "발사 원점 = 타겟 현재 위치"라는
  구조 자체는 남아있어 여러 마리가 흩어진 실전에서는 광역 소탕력이 이론보다 약할 수 있습니다(7절 2-5).
- **"doc에 없어 임의로 정한" 매직넘버의 산발적 배치**: 드럼 Lv3 둔화(0.5), 팀파니 Lv4 기절(1.0초) 등
  6곳 이상. 한 파일에 모아두면 일괄 조정이 쉬워집니다.
- **`GetTotalExtraDamage()`가 "지금 타격한 악기"가 아니라 장착된 4슬롯 전체를 합산**(7절 1-2).
- **런타임 절차적 UI 생성**: `LevelUpUI.EnsureUIComponents()` 등이 전부 코드로 조립됩니다. 프로젝트
  전반의 "절차적 생성" 스타일과 일관되므로 필수 리팩토링은 아님.
- **싱글톤 의존 아키텍처**: 핵심 매니저 대부분이 `MonoSingleton<T>`라, 실측 테스트마다 private 필드를
  리플렉션으로 주입해야 했습니다. 순수 계산 로직(데미지 공식·타겟팅 등)을 싱글톤 밖으로 뽑는 흐름은
  이미 일부 진행됐고(`DamageFormula`, `CombatTargetingUtility`), 계속 같은 방향으로 확장하면 다음
  밸런스 조정 라운드의 테스트가 더 간단해집니다.

*(원본: `refactoring_recommendations.md`)*

---

## 7. 팀 리뷰 필요 항목 (판단 대기)

코드로는 판단할 수 없는(직접 플레이해보고 감을 잡거나, 기획 의도를 확정해야 하는) 항목 모음입니다.
전부 게임이 정상 동작하는 데 지장이 없는 항목이고(치명적 버그 아님), 다음 라운드 방향을 정할 때
참고할 목록입니다.

### 7-1. 리듬/전투 공용 시스템

**레가토 패시브·Lv4 "Multi+1" 스탯 — 2026-08-07 해결 완료.** "추가 투사체"의 악기별 의미(피아노는
발사 수 추가, 벨은 빈 각도 성광 추가 등)를 정의해 6종에 연동했습니다. 4-4절 참고. (참고용으로 남겨둠:
드럼/프렌치호른/첼로/플루트는 투사체 개념이 없어 사용자 결정으로 제외됐습니다.)

**`GetTotalExtraDamage()`/`GetTotalExtraProjectiles()`가 장착 4슬롯 전체를 합산 — 2026-08-07 해결
완료.** 문서/커밋 이력 어디에도 "장착 시너지"가 의도된 설계라는 근거가 없어, "지금 판정된 그 악기만의
값을 쓰도록" 정정했습니다. `RhythmAttackManager.HandleRhythmHit()`가 `hitInstrument.extraDamage`/
`hitInstrument.extraProjectiles`를 직접 읽고, `InstrumentManager`의 두 합산 메서드는 더 이상 쓰이지
않아 삭제했습니다. Unity MCP 실측 PASS(`archive/per_instrument_extra_stats_test_guide.md`) — 다른
악기를 추가 장착해도 이미 판정 중인 악기의 데미지/투사체 수가 더 이상 새지 않고, 레가토(전역 패시브)는
원래대로 정상 반영됨을 확인.

**데미지 반올림 규칙(banker's rounding)** — `Mathf.RoundToInt`는 `.5`를 가장 가까운 짝수로 내림합니다.
체감 손실이 느껴지면 `Mathf.CeilToInt`로 전환 검토. (플레이 감각 필요, 미해결)

**전역 콤보 공유 — 2026-08-07 유지 결정.** 피아노/글록켄슈필 버스트가 "자기 악기 콤보"가 아니라
전역 콤보를 재사용하는 방식(3절 2항)을 그대로 유지하기로 확정했습니다. 악기 조합에 따라 서로 다른
악기의 버스트를 유발하는 것도 시너지로 볼 수 있다는 판단— 코드 변경 없음.

### 7-2. 기획서 대비 구현이 달라진 부분 (설계 의도 확인 필요)

**2-1. 첼로 Lv4 "지속시간 +30%" → "릴리즈 후 잔류시간"으로 재해석 — 2026-08-07 확정.** 홀드 중에만
유지되는 구조라 "자동으로 지속시간이 늘어난다"는 개념이 성립하지 않아, "홀드를 뗀 뒤 1.3초 더 잔류"로
재해석한 현재 구현을 그대로 확정하기로 했습니다(홀드 유지 가능 시간 자체를 늘리는 대안은 채택하지
않음) — 코드 변경 없음.

**2-2. 벨 Lv4 "엑센트 박자 타격 시" 조건 미구현** — 항상 더블 버스트가 나갑니다. 엑센트 판정을
추가하려면 노트에 강세 플래그를 붙이는 인프라 작업이 필요.

**2-3. 마림바·벨의 "엇박자" 판정 없음** — 노트 배치로만 근사(3절 4항). 실제로 기획 의도와 동일한
결과인지(노트가 애초에 엇박 위치에만 있으므로) 확인 필요.

**2-4. 플루트 "지나간 자리" 근사** — 실제 이동 궤적 대신 고정 오프셋(3절 5항). 실제 궤적 추적이
필요하면 트레일 버퍼 구현 필요.

**2-5. 벨의 8방향 발사가 "가장 가까운 적" 1명에게 항상 전부 명중 — 보스 단독 페이즈 화력 저하 문제,
2026-08-08 수정 완료.** 2026-08-07 재조사에서 예상했던 대로, 잡몹이 있을 때 벨은 보스를 `center`
후보로 아예 고려하지 못해(`GetNearestEnemy`가 `EnemyMonster`만 검색) 보스 단독 페이즈에서 8방향
성광이 플레이어 위치를 중심으로 발사되는 실제 버그로 확인됐습니다(사용자 실측 리포트). `CombatTargetingUtility.
GetNearestTargetPosition`(잡몹 0마리일 때만 보스로 폴백)으로 교체해 해결 — Unity MCP 실측 PASS
(`archive/bugfix_burst_scale_and_boss_targeting_test_guide.md`). 같은 조사에서 피아노/첼로/팀파니/
글록켄슈필도 동일한 구조적 문제가 있었음이 드러나 함께 수정했고(4-1절 신설 예정 없이 이 표 및 §3
10항 참고), 추가로 프렌치호른/첼로/바이올린의 지속 틱 데미지가 보스에게 아예 안 들어가던(0딜) 별개
버그도 같은 조사에서 발견해 수정했습니다.

### 7-3. DPS·밸런스 — 실제 플레이 감각으로 판단 필요

**3-1. 바이올린·팀파니 Lv5 기믹의 "존재감"이 약해질 수 있음** — 로직은 그대로 두고 배율만 낮춰
목표 DPS를 맞췄습니다(5-4절). 실제 플레이에서 "화면 연출 임팩트는 있으니 괜찮다" vs "장판 개수/생성
빈도 자체를 줄이고 개당 데미지를 올려야 한다" 중 판단 필요.

**3-2. 팀파니 Lv1 융단폭격 명중률** — 단일 고정 표적 기준 실측 DPS가 목표의 0.55배. 다수 적 환경에서
랜덤 오프셋이 오히려 "동시 다중 타격" 기회로 작용해 체감 DPS가 더 높을 가능성이 있어 배율은 조정하지
않고 남겨뒀습니다. 실전에서 부족하면 재조정 필요.

**3-3. Target DPS 자체가 "단일 고정 표적" 기준으로만 검증됨** — 광역 스킬은 실전에서 더 높은 총딜량을,
단일 표적형은 표적이 움직이면 명중률이 낮아질 수 있습니다. 실전 밸런스 패스는 별도로 필요.

**3-4. 팀파니 Lv5 "지진지대 잔류"가 체감상 안 보인다는 리포트 — 사용자 확인 결과 저레벨 오인,
코드 버그 아님(2026-08-08).** 처음엔 팀파니 Lv5 + 페르마타 장착 상태에서 홀드 종료 후 잔여 장판이
안 남는다는 리포트였으나, 조사 결과 `TimpaniSeismicZone` 메커니즘 자체는 이미 `allegro_fermata_
passive_test_guide.md`(2026-08-06, PASS)에서 리플렉션으로 duration 3.0→5.25(Lv5 배율) 확인까지
끝난 상태라 코드상 문제를 찾지 못했습니다. 사용자가 재확인한 결과 "아마 저레벨 때 보고 착각한 것
같다"로 정리 — Lv5 미만이면 애초에 `lingeringZone` 자체가 꺼져있어(`level >= 5`) 잔류 장판이 생기지
않는 게 정상 동작입니다. 코드 변경 없음, 종결.

### 7-4. 문서·스코프 정합성

**4-1. "리듬 노트 종합 설계 가이드"와 실제 구현이 완전히 다른 스코프 — 2026-08-07 폐기 확정.** 가이드
문서는 1~16마디 전체를 마디마다 다르게 짠 완성형 노트 차트인데, 실제 구현(`InstrumentPatternDatabase.cs`)
은 "레벨 1~5마다 32스텝 패턴 1개가 계속 반복"되는 구조입니다. 지금 방식으로 대체된 예전 기획으로
간주하기로 확정했고, `Docs/리듬 뱀서 리듬 노트 종합 설계 가이드.md`를 `archive/`로 옮겼습니다(원본
`.docx`는 `Docs/원본_기획서/`에 그대로 보관).

**4-2. (사소함) 벨 노트 패턴 코드 주석이 실제 패턴과 다름 — 2026-08-07 수정 완료.**
`InstrumentPatternDatabase.cs`의 벨 Lv4/Lv5 주석("8-accent"/"9-accent")이 실제 문자열(6개/7개)과
달랐던 걸 정정했습니다. 게임 동작에는 원래도 영향 없었음(문자열 자체가 실제 소스).

*(원본: `team_review_needed.md`)*

---

## 8. 관련 파일 위치

| 분류 | 파일 |
|---|---|
| 판정 이벤트 → 악기 디스패치 | `Assets/Scripts/Combat/RhythmAttackManager.cs` |
| 탭 5종 디스패처 | `Assets/Scripts/Combat/InstrumentAttacks/InstrumentAttackDispatcher.cs` |
| 홀드 공용 인터페이스/코디네이터 | `IHoldAttackEffect.cs`, `HoldEffectCoordinator.cs` |
| 홀드 5종 이펙트 | `ViolinOrbitEffect.cs`, `FrenchHornConeEffect.cs`, `CelloGravityFieldEffect.cs`, `TimpaniBombardmentEffect.cs`, `FluteVortexHoldEffect.cs`/`FluteVortexEffect.cs` |
| 드럼 상시 오라 | `Assets/Scripts/Combat/DrumAuraController.cs` |
| 공용 투사체/광역/연출 | `PiercingBeamProjectile.cs`, `AreaImpactEffect.cs`, `ShockwaveVisualEffect.cs`, `LingeringZoneEffect.cs`, `HomingShrapnelProjectile.cs` |
| 타겟팅 + 패시브 배율 헬퍼 | `Assets/Scripts/Combat/CombatTargetingUtility.cs` |
| 데미지 공식(순수 함수) | `Assets/Scripts/Combat/DamageFormula.cs` |
| 악기별 DPS 보정 배율 테이블 | `Assets/Scripts/Instrument/InstrumentDamageTable.cs` |
| 패시브 스탯 계산 | `Assets/Scripts/Passive/PassiveStatManager.cs` |
| 절차적 스프라이트(링 포함) | `Assets/Scripts/Utility/ProceduralSpriteFactory.cs` |
| 홀드 노트/콤보/타겟 방향 공개 | `Assets/Scripts/Rhythm/RhythmNote.cs`, `RhythmManager.cs`, `Assets/Scripts/Player/PlayerController.cs` |
| 악기별 홀드 길이/패턴 | `Assets/Scripts/Instrument/InstrumentPatternDatabase.cs` |
| 상태이상 공용 인프라 | `Assets/Scripts/Enemy/EnemyMonster.cs` (`SetSpeedMultiplier`, `ApplyStun`, `ApplyTemporarySlow`, `SetDamageAmpMultiplier`) |
| 악기 레벨별 공통 스탯 | `Assets/Scripts/Instrument/InstrumentData.cs` |

*(원본: `instrument_mechanics_implementation_summary.md` §5)*

---

## 8-1. 아트 에셋(악기 스프라이트) 현황

`Assets/Resources/Sprites/Instruments/`의 스프라이트는 `Resources.Load<Sprite>($"Sprites/Instruments/{type}")`
(`InstrumentOrbit.cs:27`, `LevelUpUI.cs:308`)로 `InstrumentType` 이름 기준 동적 로딩되며, 파일이 없거나
임포트 설정이 안 맞으면 자동으로 프로시저럴 도형(원)으로 폴백됩니다.

**2026-08-08 기준: 10종 전부 실제 픽셀아트 연결 완료.**

| 시기 | 대상 |
|---|---|
| 기존 | Drums, Piano, Violin, Flute, Cello |
| 2026-08-08 신규 추가 | Bell, FrenchHorn, Glockenspiel, Marimba, Timpani |

신규 5종은 파일명을 `InstrumentType` enum과 정확히 일치시키고(`Frenchhorn.png`→`FrenchHorn.png` 대소문자
정정 포함), Sprite(2D and UI)/Single 타입 임포트 + Sprite Editor Trim(투명 여백 제거)까지 적용해 기존
5종과 동일한 방식으로 `InstrumentOrbit`의 0.68유닛 최대치수 정규화가 일관되게 동작함을 확인했습니다.
코드 변경은 필요 없었습니다(동적 로딩 구조 덕분).

`Guitar.png`/`Harp.png`/`Saxophone.png`/`Trumpet.png`/`Xylophone.png`는 `InstrumentType` enum(10종:
Drums/Piano/Violin/Flute/FrenchHorn/Glockenspiel/Cello/Timpani/Marimba/Bell)에 대응하는 값이 없어
코드에서 전혀 참조되지 않는 고아 파일로 확인됨(2026-08-07 조사). `Resources/` 하위에 있으면 코드가
안 쓰더라도 빌드에 무조건 포함되므로, 2026-08-08에 `Assets/Sprites/UnusedInstruments/`(Resources 밖,
기존 `Assets/Sprites/Player/`와 같은 상위 `Assets/Sprites/` 컨벤션)로 이동했습니다. `.meta`(GUID)를
함께 옮겨서 참조 자체가 없는 상태라 안전하며, 별도 코드 변경도 필요 없습니다. 다음 Unity 에디터
세션에서 한 번 Refresh해서 정상 인식되는지만 확인하면 됩니다.

**몬스터/보스 픽셀아트 - 2026-08-09 착수+코드 연동+Unity MCP 실측 PASS, 검증 완료.** 일반 몬스터 3종
(4분음표/8분음표/이음표), 엘리트 3종(바이올린/피아노/드럼 변형), 최종보스 1종(오르골 변형) 총 7장을
`Assets/Resources/Sprites/Enemy/{Normal,Elite,Boss}/`에 배치, `EnemySpawner`/`EnemyMonster`/
`BossMonster`에 연동함. 콜라이더(피격 판정 반경)는 root에 고정값으로 유지하고 아트는 별도 자식
`Visual`에서 `sprite.bounds` 기준 정규화(다른 이펙트들과 동일한 content-aware normalization 패턴).
기존 상시 색상 틴트(마젠타/빨강)를 제거하고 아트 고유 색을 그대로 노출하는 과정에서, 곱연산 tint
기반 피격 플래시가 무틴트 상태에선 안 보이는 문제를 미리 발견해 깜빡임(비활성화) 방식으로 교체함.
**Unity MCP 실측 PASS(2026-08-09)**: 컴파일 0건, 일반 3종/엘리트 3종/보스 1종 정상 로드+랜덤 배정,
크기 정규화·무틴트·콜라이더 반경(0.4/2.0) 불변 확인, 피격 깜빡임 정상, 이동/사망/드롭/처치보상/
클리어 흐름 회귀 없음(시간초과 패배 경로 1건만 테스트 환경 이슈로 코드 검토 대체 - 해당 코드는
이번 변경에서 아예 건드리지 않은 부분). 검증 가이드: `archive/monster_art_integration_test_guide.md`.

**부록 버그 - `Player_Hit` 지휘 포즈가 안 보이던 문제, 발견+수정 완료(2026-08-09).** 위 몬스터 아트
검증과 같은 세션에서, 이번 작업과는 무관하게 QWER 박자 히트 시 지휘 포즈 모션이 전혀 안 보인다는
리포트가 있어 같이 조사함. 원인은 그날 아침 커밋(`f8e4048`, 공격 모션 해상도 상향)에서
`Player_Hit/point_left·lefttop·right·righttop.png` 4개를 고해상도 이미지로 교체했는데, 텍스처
임포트 크롭 영역(`spriteSheet` rect)이 예전 저해상도 이미지 기준 값으로 남아있어서 새 텍스처의
약 14~19%만 잘려서 스프라이트가 되고 있었던 것(`Resources.Load<Sprite>`는 null 없이 로드되므로
콘솔 에러 無 - "존재하지만 텅 빈" 스프라이트). 4개 `.meta`의 `spriteImportMode`를 Multiple → Single로
재임포트해 해결(코드 변경 없음). 상세: `archive/monster_art_integration_test_guide.md` 4절 부록.

**엘리트/보스 시각 크기 2배 확대 + 지휘자 캐릭터 축소 + 엘리트/보스 판정 범위 동기화 - Unity MCP
실측 PASS, 검증 완료(2026-08-09).** 위 검증이 끝난 뒤 "몬스터가 너무 작아서 안 보인다"는 피드백으로
`EnemyMonster.ArtVisualScale`(1→2.5)·`BossMonster.EliteReferenceContentSize`(1.6→3.2)·
`BossReferenceContentSize`(2.4→4.8)를 확대하고, 균형을 맞추려 `PlayerController.targetWorldHeight`를
1.0→0.5로 축소(코드 기본값, `Gameplay.unity`/`Player.prefab` 직렬화 값도 동기화: 1.8→0.9, 1→0.5).
이후 "너무 작아졌다"는 피드백으로 1.2배 재확대해 최종 0.6(코드 기본값), `Gameplay.unity` 1.08,
`Player.prefab` 0.6으로 조정함(2026-08-09).
이 과정에서 발견한 사이드이펙트: 이 프로젝트의 모든 공격 판정(`AreaImpactEffect` 등 7개 이펙트)은
`Vector3.Distance(공격원점, 적.transform.position) <= radius` 순수 좌표 비교라 보스/엘리트를 반지름 0인
점으로 취급 - 보스 몸통이 아무리 커져도 판정 거리엔 전혀 반영되지 않아 "공격했는데 판정이 안 맞는다"는
문제가 있었음. `BossMonster.HitboxRadius`(엘리트 1.6 / 보스 2.4, 현재 비주얼 반지름) 프로퍼티를 추가해
`AreaImpactEffect`/`CelloGravityFieldEffect`/`DrumBeatBangEffect`/`FrenchHornConeEffect`/
`LingeringZoneEffect`/`PiercingBeamProjectile`/`ViolinOrbitEffect` 7곳의 보스 판정 거리 비교에
`+ HitboxRadius`를 더해 보정. 몸박(플레이어 접촉 데미지) 콜라이더도 `Mathf.Max(HitboxRadius, 2.0f)`로
맞춰서 보스(2.4)는 커진 만큼 넓어지고, 엘리트는 새 비주얼 반지름(1.6)이 기존 고정값(2.0)보다 작아
그대로 뒀다면 오히려 좁아졌을 것을 하한선으로 방지(회귀 없이 기존 2.0 유지). **PASS** - Unity MCP
세션이 리플렉션으로 값만 확인한 게 아니라 실제로 공격을 명중시켜 `BossMonster.CurrentHp`가
줄어드는 것까지 실측 확인(엘리트: 순수 반경 밖 거리에서도 `HitboxRadius` 보정으로 명중, 대조군은
정상 미스 확인 / 최종보스: 몸박 접촉으로 플레이어 HP도 정상 감소 확인). 검증 로그:
`archive/monster_scale_and_boss_hitbox_test_guide.md`.

**이펙트(VFX) 픽셀아트 - 착수함(2026-08-08).** 절차적 이펙트를 역할별로 나눠 접근하기로 함(§2 각
악기 절 + 코드 조사 기준): 범위 링(현행 유지, 손그림 불필요) / 임팩트·버스트(다이아몬드) / 빔·투사체
(늘어난 원) / 필드·존(채워진 원) / 바이올린 칼날·참격(전용). 제작 파이프라인은 나노바나나로 흑백
베이스 이미지 생성 → 이미지→영상 변환 → 프레임 추출 → 배경제거, 캐릭터/악기 아이콘과 동일 방식.

- **임팩트·버스트 9프레임 애니메이션 완료**: `Assets/Resources/Sprites/Effects/ImpactBurst/spark1~9.png`
  (작게 시작 → spark5 최대 만개 → 다시 작아지며 소멸, 무채색이라 `SpriteRenderer.color`로 악기별 틴트
  적용됨). `AreaImpactEffect.cs`에 로딩+2단계 매핑(예고 1→5, 착탄 후 플래시 5→9) 로직 추가 - 실제
  사용처는 글록켄슈필(별빛 낙하)과 팀파니(캐논/융단폭격) 2곳뿐(피아노/벨/마림바는 빔 방식이라 무관).
  프레임 로딩 실패 시 기존 다이아몬드로 자동 폴백, 호출부 코드는 변경 없음.
- **빔/투사체 4프레임 반짝임 애니메이션 완료(2026-08-08)**: `Assets/Resources/Sprites/Effects/Beam/
  Beam1~4.png`. `PiercingBeamProjectile.cs`(피아노/벨/마림바 공유 + 바이올린 릴리즈 참격도 동일 클래스
  사용) 한 곳에서만 로딩해 4곳 전부 자동 적용됨, 호출부 코드 변경 없음. 새 아트가 원본부터 길쭉한
  모양(~7~8:1)이라 기존 정사각형 폴백 원 기준 고정 배율을 그대로 곱하면 이중 확대될 뻔한 것을
  콘텐츠 크기 역산 정규화로 사전 방지(`ReferenceContentSize` 기준, 폴백 시 수학적으로 이전과 100%
  동일한 결과 확인됨). 테스트 PASS 후 실측정으로 화면상 크기가 너무 작다는 사용자 피드백(기존
  절차적 원 자체가 원래도 작았던 것과 동일 재현)을 받아 `ArtVisualScale`(순수 시각 배율, 판정
  반경과 무관) 상수를 추가해 3배로 조정 - 추가 플레이테스트 필요 시 이 상수만 조정하면 됨.
- **필드/존 - 첼로 중력장 11프레임 스월 애니메이션 연동 완료, 테스트 PASS(2026-08-08)**:
  `Assets/Resources/Sprites/Effects/GravityField/gravity_field1~11.png`(사용자가 회전만으론 밋밋할
  것 같다며 프레임별로 형태가 변하는 11장을 직접 그림). `CelloGravityFieldEffect.cs`에 로딩+자연
  정렬+애니메이션 연동. 두 가지를 사전에 방지함: (1) 프레임이 11장(두 자리 번호)이라 기존
  `string.CompareOrdinal` 정렬을 그대로 썼다면 `1,10,11,2,3...` 순으로 잘못 정렬됐을 것 - 파일명
  끝 숫자를 정수로 비교하는 자연 정렬로 교체. (2) 손그림 아트(콘텐츠가 캔버스를 거의 꽉 채움)가
  기존 절차적 원(28px 캔버스)과 bounds 크기가 완전히 달라 기존 매직넘버(`radius*0.9`를 아트 크기와
  링 보정 양쪽에 재사용)를 그대로 뒀으면 이중 확대됐을 것 - 아트를 별도 자식 오브젝트로 분리해
  콘텐츠 크기 기반으로 독립 계산하도록 재구성(`ReferenceContentSize`/`ArtVisualScale` 패턴, 빔과
  동일). 색상은 아트 자체의 보라-파랑 톤을 그대로 쓰기로 결정(첼로 공식색인 갈색은 범위 링에만
  계속 사용 - 사용자 결정, 코드 버그 아님). **Unity MCP 배치 테스트에서 실제 버그 1건 발견+수정**:
  Sprite Mode가 Multiple로 임포트되면 Unity가 서브스프라이트 이름 끝에 `_인덱스`를 자동으로 붙여서
  (예: `gravity_field10` → `gravity_field10_0`) `ExtractTrailingNumber()`가 그 인덱스(항상 "0")를
  프레임 번호로 착각 - 자연 정렬이 사실상 무력화되어 `field1,field10,field11,field2...` 순으로
  잘못 재생되고 있었다. 번호 추출 전에 `_<숫자>` 접미사를 먼저 제거하도록 수정, 재검증 결과
  `field1→2→...→11` 순서로 정확히 재생됨을 확인. 검증 완료(archive 이동).
- **필드/존 - 플루트 소용돌이 정지 이미지 + 펄스 연동, 테스트 PASS(2026-08-08)**:
  `Assets/Resources/Sprites/Effects/Vortex/Vortex.png`(동심원 형태). 영상 생성 토큰 소진으로 영상/
  프레임 추출 없이 정지 이미지 1장만 사용하기로 결정. 동심원이라 회전은 시각적으로 티가 안 나서
  (완전 대칭) 회전 대신 코드 쪽 스케일 펄스(±6%, `Mathf.Sin`)로 "숨쉬는" 느낌만 보완 - 판정 반경/
  흡입력에는 영향 없는 순수 시각 효과. 첼로와 동일한 이유로 아트를 별도 자식 오브젝트로 분리해 콘텐츠
  크기 기반 정규화 적용(이중 확대 버그 사전 방지, 실측으로 기존과 동일한 크기 확인). 코드 변경
  불필요, 그대로 PASS.
- **프렌치호른 부채꼴 / 잔류 장판(팀파니·벨·바이올린 Lv5 공유) / 바이올린 칼날 - 정지 이미지 연동,
  테스트 PASS(2026-08-08)**: `Assets/Resources/Sprites/Effects/HornCone/HornCone.png`,
  `LingeringZone/LingeringZone.png`, `Blade/Blade.png`. 프렌치호른은 이번에 처음으로 진짜 방향성
  있는 부채꼴 아트가 생기면서, 기존 "원으로 근사 + 위치를 앞으로 밀어두는 트릭"을 제거하고 실제
  `transform.rotation`으로 이동 방향을 향하도록 바꿈(아트의 뾰족한 끝을 `sprite.bounds.min.x` 기준으로
  플레이어 위치에 정확히 앵커링 - 실측으로 회전 중에도 흔들리지 않음 확인). 잔류 장판은 팀파니/벨/
  바이올린 3개 악기가 색이 다른 채로 클래스 하나를 공유하는데, 기존엔 색을 텍스처에 직접 구웠던 걸
  무채색 아트 + `SpriteRenderer.color` 런타임 틴트(빔/버스트와 동일 패턴)로 바꿔서 3개 악기 색 전부
  정확히 반영됨을 실측 확인. 바이올린 칼날은 콘텐츠 크기 정규화만 추가(정규화 없이 그대로 붙였으면
  100배 넘게 이중 확대됐을 크기 차이, 정규화 후 기존과 동일한 0.16 크기로 확인). 세 곳 다 판정/넉백/
  디버프/틱데미지/보스 피해 회귀 없음. 코드 변경 불필요, 그대로 PASS.
**인게임 배경(Background) - 착수, 가독성 문제는 해결, 스크롤 버그는 2차 시도 끝에 완전히
재작성(2026-08-09).** 콘서트홀 마룻바닥 도트 패턴을 나노바나나로 생성해 `Assets/Resources/Sprites/
Background/ParquetFloor.png`에 배치. `CameraController`가 플레이어를 그대로 따라다니고 이동/카메라
경계 제한이 전혀 없는 기존 구조상, 벽으로 막힌 공간이 아니라 무한 반복 배경으로 결정함.
`Assets/Prefabs/Environment/Background.prefab`으로 만들어 `Gameplay.unity`에 배치, 텍스처 임포트
(Wrap Repeat/Single/Full Rect)까지 **1차 Unity MCP 실측 PASS**(`archive/background_tiling_test_guide.md`).

이후 사용자 실플레이에서 리플렉션 기반 자동 검증으론 못 잡은 문제 2건 발견:
1. **가독성 - 배경이 흰색 계열이라 판정 링/이펙트 등 흰색 UI가 잘 안 보임.** 게임 배경색(카메라
   `m_BackGroundColor` ≈ `#314D79` 남색)에 맞춰 남색 헤링본 마룻바닥 이미지로 `ParquetFloor.png`
   교체. **PASS** - 실제 플레이 스크린샷에서 흰색 판정 링/UI가 배경과 뚜렷하게 대비됨을 확인.
2. **이동감 부재(버그) - 배경이 캐릭터를 따라다니는 스티커처럼 보임.** 1차 수정은
   `material.mainTextureOffset`을 카메라 월드 좌표에 비례해 스크롤하는 방식이었고, Unity MCP
   세션이 리플렉션으로 수치 확인 + 스크린샷 비교로 **PASS 판정**했었으나(`archive/
   background_worldlock_scroll_test_guide.md`), **사용자가 직접 플레이해보니 여전히 배경이
   고정돼 있었음 - 이전 PASS 판정이 오판이었던 것으로 드러남.** 진짜 원인 파악: `Sprites-Default`를
   비롯한 대부분의 스프라이트 셰이더는 `_MainTex_ST`(Tiling/Offset)를 셰이더 코드에서 아예 읽지
   않는다 - 스프라이트는 아틀라스 패킹 방식이라 머티리얼 오프셋으로 스크롤시키는 걸 원천적으로
   지원하지 않음. `mainTextureOffset` 프로퍼티 값 자체는 정상 설정됐지만(그래서 리플렉션 수치
   검증은 통과) 실제 렌더링엔 전혀 반영 안 됐던 것 - "값이 맞다"와 "화면에 반영된다"를 구분 못한
   검증 함정. **전면 재작성**: 셰이더 트릭을 버리고 실제 타일 스프라이트 여러 장을 격자로 배치해
   카메라를 따라 재배치하는 순수 Transform 기반 방식으로 교체(`BackgroundTiler.cs`). **PASS,
   재검증 완료(2026-08-09)** - 이번엔 리플렉션 수치만이 아니라 실제 프로덕션 `LateUpdate()`를
   직접 구동해 스크린샷 픽셀 단위로 무늬 위치가 실제로 이동함을 확인(플레이어 5유닛 이동 시 화면상
   약 220px 이동, 이론값 216px와 거의 일치), 타일 래핑/왕복 일관성도 전부 확인. 알려진 한계 1건:
   `BuildGrid()`가 `Awake()` 시점 1회만 격자 크기를 계산해서, 런타임 중 `orthographicSize`/화면
   비율이 바뀌면 격자가 부족해질 수 있음(현재 게임은 런타임에 이 값이 안 바뀌므로 실무 영향 없음
   - 버그가 아니라 알려진 제약). 성능(배칭)은 자동화 환경 한계로 수치 미실측, 수동 확인 권장.
   검증 로그: `archive/background_grid_scroll_rewrite_test_guide.md`.

검증 중 사소한 소동 하나: 처음 전달된 `ParquetFloor.png`가 파일 내용이 전부 0바이트인 빈 파일로
확인되어(PNG 헤더조차 없음) 한 차례 재저장을 요청했고, 재저장 후 정상 진행됨.

- **남은 항목**: 없음 - 인게임 배경(타일링/격자 스크롤/남색 아트), 엘리트/보스 크기·판정 범위
  동기화 포함 이번 세션에서 진행한 항목 전부 Unity MCP 실측 완료. VFX + 몬스터/보스 픽셀아트도
  전부 완료. 배경 격자의 "런타임 화면 비율 변경 미대응" 1건만 알려진 제약사항으로 남아있음(필요
  시 추후 개선).

*(관련 검증: `archive/instrument_sprite_import_test_guide.md`, `archive/impact_burst_sprite_animation_test_guide.md`)*

---

## 9. 검증 상태 총괄

| 대상 | 검증 방식 | 결과 | 상세 로그 |
|---|---|---|---|
| 10종 악기 메커니즘 1~3단계 | Unity MCP 실측 | PASS (유일한 치명적 버그는 바이올린 딕셔너리 순회 크래시, 2단계에서 수정 확인) | `archive/instrument_mechanics_phase1~3_test_result.md` |
| 밸런스 doc 5번 항목 정합화(4단계) | Unity MCP 실측 | PASS, 10종 전체 + 공용 상태이상 인프라 | `archive/instrument_mechanics_phase4_test_result.md` |
| DPS 밸런스 보정 | Unity MCP 실측 3라운드 | PASS (전부 0.7~1.3배 수렴) | `archive/dps_balance_test_guide.md`, `archive/dps_balance_test_result.md` |
| 리팩토링 회귀(디스패치 통합/타겟팅 통일/죽은코드 제거) | Unity MCP 실측 | PASS | `archive/refactoring_regression_test_guide.md` |
| 홀드 노트 머리+꼬리 비주얼 | Unity MCP 실측 | PASS | `archive/holdnote_tail_visual_test_guide.md` |
| 드럼 범위 시각화 + 크레센도 연동 | Unity MCP 실측 | PASS | `archive/drum_range_visualization_test_guide.md` |
| 나머지 9종 크레센도 연동 | Unity MCP 실측 | PASS | `archive/instrument_range_passive_test_guide.md` |
| 홀드 노트 이동 추적 버그 수정 | Unity MCP 실측 | PASS | `archive/holdnote_follow_player_fix_test_guide.md` |
| 알레그로/페르마타 연동 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/allegro_fermata_passive_test_guide.md` |
| 프렌치호른/첼로/플루트 범위 링 정확도 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/range_ring_precision_test_guide.md` |
| 레가토(투사체 수 증가) 6종 연동 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/legato_extra_projectile_test_guide.md` |
| 바이올린/팀파니 범위 링 추가 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/violin_timpani_range_ring_test_guide.md` |
| extraDamage/extraProjectiles 판정 악기 전용 격리 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/per_instrument_extra_stats_test_guide.md` |
| 신규 악기 이미지 5종(벨/프렌치호른/글록켄슈필/마림바/팀파니) 임포트·연동 | Unity MCP 실측 | PASS | `archive/instrument_sprite_import_test_guide.md` |
| 임팩트 버스트 9프레임 애니메이션 연동(글록켄슈필/팀파니) | Unity MCP 실측(일부 리플렉션 기반) | PASS | `archive/impact_burst_sprite_animation_test_guide.md` |
| 임팩트 버스트 크기 정규화 / 보스 단독 페이즈 타겟팅·틱데미지 누락(5+3악기) / 팀파니 홀드 밀도(16→10) | Unity MCP 실측(리플렉션+실스폰) | PASS | `archive/bugfix_burst_scale_and_boss_targeting_test_guide.md` |
| 바이올린/첼로 홀드 밀도(13→11), 프렌치호른/플루트 무변경 판단 | Unity MCP 실측(시뮬레이션 재현) | PASS | `archive/violin_cello_hold_density_fix_test_guide.md` |
| 빔/투사체 4프레임 반짝임 애니메이션 연동(피아노/벨/마림바/바이올린 참격) | Unity MCP 실측(리플렉션 기반) | PASS | `archive/beam_sprite_animation_test_guide.md` |
| 첼로 중력장 11프레임 스월 애니메이션 연동(자연 정렬 버그 발견+수정 포함) | Unity MCP 실측(리플렉션 기반) | PASS | `archive/cello_gravity_field_animation_test_guide.md` |
| 플루트 소용돌이 정지 이미지 + 펄스 연동 | Unity MCP 실측(리플렉션 기반) | PASS | `archive/flute_vortex_static_art_test_guide.md` |
| 프렌치호른 부채꼴 회전+피벗 보정 / 잔류 장판(팀파니·벨·바이올린 Lv5) 색 틴트 / 바이올린 칼날 크기 정규화 | Unity MCP 실측(리플렉션+실스폰) | PASS | `archive/horncone_lingeringzone_blade_art_test_guide.md` |
| 악기별 레벨 스케일링 수치 데이터화(`InstrumentLevelStats.cs`, 10종 30개 스탯) | Unity MCP 실측(API 직접호출+리플렉션+Play모드) | PASS | `archive/instrument_level_stats_dataification_test_guide.md` |
| 몬스터/보스 도트 아트 연동(일반 3종/엘리트 3종/보스 1종, content-aware 정규화 + 무틴트 전환 + 피격 플래시 방식 교체) | Unity MCP 실측(리플렉션+Play모드+이벤트 콜백) | PASS | `archive/monster_art_integration_test_guide.md` |
| 엘리트/보스 시각 크기 2배 확대 + 지휘자 축소 + 판정 범위 동기화(HitboxRadius, 7개 공격 이펙트) | Unity MCP 실측(실제 공격 명중시켜 HP 감소까지 확인, 대조군 미스 확인) | PASS | `archive/monster_scale_and_boss_hitbox_test_guide.md` |
| 인게임 무한 타일링 배경(BackgroundTiler, 화면 커버리지/카메라 추적/정렬순서) | Unity MCP 실측 | PASS | `archive/background_tiling_test_guide.md` |
| 남색 헤링본 아트 교체(가독성) | Unity MCP 실측(스크린샷 비교) | PASS | `archive/background_worldlock_scroll_test_guide.md` |
| ~~배경 월드 고정 스크롤 버그 수정(mainTextureOffset)~~ - **오판으로 정정**: 사용자 실플레이에서 재현 안 됨. 스프라이트 셰이더가 `_MainTex_ST`를 지원 안 해서 값만 맞고 화면엔 반영 안 됐음 | Unity MCP 실측(수치만 검증, 렌더링 결과 미확인 - 검증 함정) | PASS 취소 → 아래 항목으로 대체 | `archive/background_worldlock_scroll_test_guide.md` |
| 배경 스크롤 격자 재배치 방식 재작성(BackgroundTiler 전면 재작성, 셰이더 의존 제거) | Unity MCP 실측(리플렉션 아닌 스크린샷 픽셀 단위 대조 + 실제 프로덕션 코드 직접 구동) | PASS (런타임 화면 비율 변경 미대응은 알려진 제약으로 남김) | `archive/background_grid_scroll_rewrite_test_guide.md` |

*(원본: `instrument_mechanics_implementation_summary.md` §6 + 각 섹션 산재 검증 요약)*
