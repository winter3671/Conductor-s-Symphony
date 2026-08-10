# 레벨업 카드 "실제 효과 미리보기" 개선 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다. 3차 개선(`archive/levelup_card_redesign_v3_test_guide.md`)은
이미 PASS로 검증 완료됐습니다.

## 0. 무엇을 고쳤나

사용자가 플레이 중 Drums Lv.2 레벨업 카드에서 "360도 충격파로 주변을 강타 (피해 +0, 투사체 +0)"를
보고 "이거 버그야?"라고 질문했습니다.

원인: 악기 카드 설명 끝의 "(피해 +X, 투사체 +Y)"는 `InstrumentInfo.extraDamage`/`extraProjectiles`를
그대로 표시하는데, 이 두 값은 **전 악기 공통 범용 보너스**(`extraDamage`는 Lv3+에서 +1, Lv5에서 +2 /
`extraProjectiles`는 Lv4+에서 +1)라 Lv2에서는 항상 0입니다. 그런데 드럼은 `InstrumentLevelStats.cs`에
따로 정의된 **악기 전용 배율**(Lv2+: 범위 +20%, 피해량 +20%)로 실제 효과를 받고 있어서, 계산 자체는
틀리지 않았지만 카드에는 그 진짜 효과가 전혀 안 보이는 "오해를 부르는 미리보기"였습니다(버그는 아님).

사용자에게 설명 후 "실제로 이 카드를 고름으로써 얻는 효과를 명시적으로 작성"하는 방향으로
합의했습니다.

### 변경 파일

- **`Assets/Scripts/Instrument/InstrumentLevelStats.cs`**: `GetLevelUpHighlights(InstrumentType type, int
  targetLevel)`를 새로 추가. 파일에 이미 있던 배율/카운트 테이블(`rangeMultiplier`,
  `durationMultiplier`, `damageMultiplier`, `knockbackMultiplier`, `sizeMultiplier`, `pierceCount`,
  `stepCount`) 및 5개 전용 단일 수치(`ViolinSpinSpeedMultiplier`, `CelloSlowFraction`,
  `FrenchHornHalfAngleDeg`, `GlockenspielSplashRadiusBase`, `TimpaniBombardIntervalBaseSeconds`)를
  `targetLevel-1` 시점 값과 `targetLevel` 시점 값으로 직접 diff해서, 값이 바뀐 항목만 한글 문구로
  변환해 리스트로 반환합니다. **하드코딩된 % 텍스트가 아니라 배열 값에서 직접 계산**하므로, 나중에
  이 파일의 밸런스 수치를 조정해도 카드 문구가 자동으로 같이 바뀝니다. `targetLevel <= 1`(신규 습득)
  이면 빈 리스트를 돌려줍니다.
- **`Assets/Scripts/UI/LevelUpUI.cs`**: `BuildInstrumentLevelUpEffectText()` 신규 추가 - 위 함수의
  결과에 `extraDamage`/`extraProjectiles`의 (targetLevel-1 → targetLevel) 델타까지 합쳐서 괄호 문구를
  만듭니다. 결과가 비어있으면(레벨업으로 딱히 바뀌는 게 없는 극히 드문 경우, 또는 신규 습득) 괄호
  없이 `def.description`만 표시합니다. 기존의 `$"...(피해 +{extraDamage}, 투사체 +{extraProjectiles})"`
  한 줄을 이 함수 호출로 교체.

### 예상 결과 (스크린샷에 있던 Drums Lv.2 카드 기준)

- **이전**: "360도 충격파로 주변을 강타 (피해 +0, 투사체 +0)"
- **이후**: "360도 충격파로 주변을 강타 (범위 +20%, 피해량 +20%)"

## 1. 사전 준비 상태

- [x] `InstrumentLevelStats.cs` - `GetLevelUpHighlights()` 신규 추가
- [x] `LevelUpUI.cs` - `BuildInstrumentLevelUpEffectText()` 신규 추가, 카드 설명 조합부 교체
- [ ] Unity 에디터 컴파일 확인 - **아직 안 됨**
- [ ] Play 모드 실측 - **아직 안 됨**

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건.
- [ ] **Drums Lv.2 카드**: 레벨업을 유도해 Drums가 Lv.1→Lv.2로 오르는 후보 카드가 뜨면 설명이
      "(범위 +20%, 피해량 +20%)"로 표시되는지 확인(더 이상 "+0, +0" 아님).
- [ ] **Drums Lv.3 카드**: Lv.2→Lv.3 카드에서 "(넉백 거리 2배, 고정 피해 +1)"이 뜨는지 확인
      (`knockbackMultiplier`가 Lv3에서 1→2배, `extraDamage`가 Lv3에서 0→1로 바뀌므로 둘 다 표시돼야 함).
- [ ] **다른 악기 최소 2~3종 표본 확인**: 예를 들어
  - Piano Lv.2: "(피해량 +25%)"
  - Bell Lv.3: "(관통 3→5)"
  - Violin Lv.2: "(회전 반경 +20%, 회전 속도 +30%)"
  - FrenchHorn Lv.5: "(공격 각도 120°→180°)"
  가 실제로 위 규칙과 일치하게 표시되는지 몇 개만 골라 확인해도 충분(전 조합을 다 볼 필요는 없음).
- [ ] **신규 악기 습득(Lv.1, "[NEW]" 뱃지) 카드**: 괄호 문구 없이 기본 설명만 표시되는지 확인
      (레벨업이 아니라 최초 습득이라 "무엇이 좋아지는가" 문구 자체가 없어야 정상).
- [ ] **레이아웃 깨짐 없음**: 문구가 이전보다 길어질 수 있으므로(예: "범위 +20%, 피해량 +20%"가
      "피해 +0, 투사체 +0"보다 김) 카드 설명란을 벗어나거나 잘리지 않는지 확인. 특히 여러 항목이
      한꺼번에 뜨는 레벨(Lv.3, Lv.5처럼 악기 전용 배율 + extraDamage/extraProjectiles가 동시에
      바뀌는 경우)을 우선 확인.
- [ ] **패시브 카드는 이번 변경과 무관** - 회귀만 간단히 확인(패시브 카드 설명은 그대로 `def.description`).

## 3. 참고 - 관련 코드 위치

- `Assets/Scripts/Instrument/InstrumentLevelStats.cs` - `GetLevelUpHighlights()`(신규), 파일 하단에
  라벨 Dictionary들과 `FormatMultiplierDelta()` 헬퍼도 함께 추가됨.
- `Assets/Scripts/UI/LevelUpUI.cs` - `BuildInstrumentLevelUpEffectText()`(신규), `ShowLevelUpSelection()`
  내 악기 카드 설명 조합부(`cardDescTexts[i].text = ...`).
- 씬/프리팹 직접 편집 없음(전부 런타임 코드/데이터).

## 4. 검증 결과

(검증 전 - Unity MCP 세션에서 작성 예정)
