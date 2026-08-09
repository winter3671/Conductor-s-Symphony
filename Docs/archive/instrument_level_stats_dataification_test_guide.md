# 악기별 레벨 스케일링 수치 데이터화 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 리팩토링을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

리팩토링 문서(`game_systems_reference.md` §6 "아직 남은 항목" 1번 - "악기별 레벨 스케일링 수치의
데이터화")를 반영했습니다. 순수 리팩토링이며 **밸런스 수치 변경은 전혀 없습니다.**

- 신규 파일 `Assets/Scripts/Instrument/InstrumentLevelStats.cs`: `InstrumentDamageTable.cs`와 동일한
  관례(`Dictionary<InstrumentType, T[]>`, 배열 인덱스 `[level-1]`, 1~5 범위 밖은 클램프)로, 10개 악기
  이펙트 클래스에 흩어져 있던 `(level >= N ? A : B)` 형태의 삼항식 30곳을 전부 이 파일 하나로 모았습니다.
  범위/피해량/관통/발사수/넉백/크기/틱간격 배율 등 "값"만 데이터화했고, `if (level >= N) { 특수동작(); }`
  형태의 행동 분기(바이올린 Lv5 검기잔향, 마림바 Lv5 감속+밀쳐냄, 팀파니 Lv4/5 기절·지진지대 on/off
  등 9곳)는 로직이라 코드에 그대로 뒀습니다.
- 10개 이펙트 클래스(`Cello/Flute/Bell/Drum/FrenchHorn/Glockenspiel/Marimba/Piano/Violin/Timpani`)의
  `Init()`/`Execute()`에서 인라인 삼항식을 `InstrumentLevelStats.GetXxx(InstrumentType.Yyy, level)`
  조회로 교체했습니다.
- **수학적 교차검증 완료**: Python으로 "기존 삼항식이 레벨 1~5에서 만들어내던 값"과 "새 테이블 조회가
  레벨 1~5에서 반환하는 값"을 30개 스탯 전부, 5개 레벨씩(총 150개 케이스) 비교한 결과 **전부
  소수점 오차 없이 완전히 일치**함을 확인했습니다(스크립트 자체는 임시 파일이라 저장 안 함, 결과만
  아래 4절에 기록).

## 1. 사전 준비

1. `refresh_unity(force, compile=request)`로 재컴파일 - **이번 검증에서 가장 중요한 단계**입니다.
   Python 교차검증은 "제가 옮겨적은 숫자가 서로 일치하는지"만 증명하고, 실제 C# 코드가 문법적으로
   맞는지(네임스페이스 `ConductorSymphony.Instrument` using 추가, `InstrumentType` enum 값 철자 등)는
   증명하지 못합니다. 컴파일 에러/경고 0건이 나오는지 반드시 확인해주세요.

## 2. 검증 항목

- [x] 컴파일 에러/경고 0건
- [x] **10개 악기 전부 Lv1~Lv5 각각 1회씩 실측**: `InstrumentLevelStats.GetRangeMultiplier` 등을
      리플렉션 또는 직접 호출로 확인하거나, 각 이펙트의 `Init()`/`Execute()` 호출 후 필드값(radius,
      pierce, damage 등)이 리팩토링 전과 동일한지 확인. 아래 표가 레벨별 기대값입니다(전부 기존
      코드와 100% 동일해야 정상 - 하나라도 다르면 데이터 옮기는 과정에서 오타난 것).

| 악기 | 스탯 | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 |
|---|---|---|---|---|---|---|
| 첼로 | 범위배율 | 1.0 | 1.2 | 1.2 | 1.2 | 1.2 |
| 첼로 | 감속률 | 0.4 | 0.4 | 0.6 | 0.6 | 0.6 |
| 첼로 | 잔류시간배율 | 1.0 | 1.0 | 1.0 | 1.3 | 1.3 |
| 플루트 | 범위배율(=흡입력배율) | 1.0 | 1.0 | 1.5 | 1.5 | 1.5 |
| 플루트 | 지속시간배율 | 1.0 | 1.4 | 1.4 | 1.4 | 1.4 |
| 플루트 | 동시개수 | 1 | 1 | 1 | 2 | 2 |
| 벨 | 범위배율 | 1.0 | 1.3 | 1.3 | 1.3 | 1.3 |
| 벨 | 관통 | 3 | 3 | 5 | 5 | 5 |
| 벨 | 버스트수 | 1 | 1 | 1 | 2 | 2 |
| 드럼 | 범위배율 | 1.0 | 1.2 | 1.2 | 1.2 | 1.2 |
| 드럼 | 피해배율 | 1.0 | 1.2 | 1.2 | 1.2 | 1.2 |
| 드럼 | 넉백배율 | 1.0 | 1.0 | 2.0 | 2.0 | 2.0 |
| 드럼 | 충격파중첩수 | 1 | 1 | 1 | 1 | 2 |
| 프렌치호른 | 범위배율 | 1.0 | 1.25 | 1.25 | 1.25 | 1.25 |
| 프렌치호른 | 반각(도) | 60 | 60 | 60 | 60 | 90 |
| 프렌치호른 | 넉백배율 | 1.0 | 1.0 | 1.4 | 1.4 | 1.4 |
| 글록켄슈필 | 피해배율 | 1.0 | 1.3 | 1.3 | 1.3 | 1.3 |
| 글록켄슈필 | 스플래시반경기준값 | 0.6 | 0.6 | 1.1 | 1.1 | 1.1 |
| 마림바 | 관통 | 3 | 5 | 5 | 5 | 5 |
| 마림바 | 크기배율 | 1.0 | 1.0 | 1.3 | 1.3 | 1.3 |
| 피아노 | 피해배율 | 1.0 | 1.25 | 1.25 | 1.25 | 1.25 |
| 피아노 | 관통 | 2 | 2 | 4 | 4 | 4 |
| 피아노 | 발사수(extraProjectiles 별도) | 1 | 1 | 1 | 2 | 2 |
| 바이올린 | 칼날수(extraProjectiles 별도) | 2 | 2 | 3 | 3 | 3 |
| 바이올린 | 범위배율 | 1.0 | 1.2 | 1.2 | 1.2 | 1.2 |
| 바이올린 | 회전속도배율 | 1.0 | 1.3 | 1.3 | 1.3 | 1.3 |
| 바이올린 | 참격관통 | 3 | 3 | 5 | 5 | 5 |
| 바이올린 | 참격크기배율 | 1.0 | 1.0 | 1.0 | 1.5 | 1.5 |
| 팀파니 | 범위배율 | 1.0 | 1.25 | 1.25 | 1.25 | 1.25 |
| 팀파니 | 폭격간격기준값(초) | 0.65 | 0.65 | 0.4333 | 0.4333 | 0.4333 |

- [x] **회귀 - 10개 악기 실전 동작**: 각 악기 Lv1과 Lv5(최소 두 지점) 정도로 허수아비/보스 대상 실측해
      피해/범위/관통/넉백 등 실제 겜플레이 결과가 리팩토링 전과 체감상 동일한지 스팟 체크. (수치
      자체는 위 표로 이미 증명됐으므로, 여기서는 "새 코드가 실제로 그 값을 정상적으로 사용하는지"만
      확인하면 충분 - 즉 값을 읽어서 안 쓰거나 엉뚱한 곳에 쓰는 배선 실수가 없는지.)

## 4. 검증 결과

**검증일: 2026-08-09 / 검증 환경: Unity MCP (Unity 6000.5.5f1, 씬 Assets/Scenes/Gameplay.unity)**

### 4-1. 컴파일 에러/경고: 0건 ✅

`refresh_unity(mode=force, compile=request)`로 강제 재컴파일 후 `read_console`으로 확인.
에러/경고 0건(무관한 MCP 웹소켓 로그 1건 제외).

### 4-2. 10개 악기 Lv1~Lv5 수치 실측: 30개 스탯 전부 표와 100% 일치 ✅

Play 모드 진입 없이, 에디터에서 `InstrumentLevelStats`의 public API(`GetRangeMultiplier`,
`GetDurationMultiplier`, `GetDamageMultiplier`, `GetKnockbackMultiplier`, `GetSizeMultiplier`,
`GetPierceCount`, `GetStepCount`, 및 4개 전용 배열)를 Lv1~5 전부 직접 호출해 실측. 가이드 2절 표와
비교한 결과 **30개 스탯 × 5레벨 = 150개 케이스 전부 완전히 일치**(팀파니 폭격간격은
0.4333333... vs 표기 0.4333 - 반올림 표기 차이일 뿐 동일 값).

```
Cello.Range: 1,1.2,1.2,1.2,1.2
Cello.Slow: 0.4,0.4,0.6,0.6,0.6
Cello.Duration: 1,1,1,1.3,1.3
Flute.Range: 1,1,1.5,1.5,1.5
Flute.Duration: 1,1.4,1.4,1.4,1.4
Flute.StepCount: 1,1,1,2,2
Bell.Range: 1,1.3,1.3,1.3,1.3
Bell.Pierce: 3,3,5,5,5
Bell.StepCount: 1,1,1,2,2
Drums.Range: 1,1.2,1.2,1.2,1.2
Drums.Damage: 1,1.2,1.2,1.2,1.2
Drums.Knockback: 1,1,2,2,2
Drums.StepCount: 1,1,1,1,2
FrenchHorn.Range: 1,1.25,1.25,1.25,1.25
FrenchHorn.HalfAngle: 60,60,60,60,90
FrenchHorn.Knockback: 1,1,1.4,1.4,1.4
Glockenspiel.Damage: 1,1.3,1.3,1.3,1.3
Glockenspiel.SplashBase: 0.6,0.6,1.1,1.1,1.1
Marimba.Pierce: 3,5,5,5,5
Marimba.Size: 1,1,1.3,1.3,1.3
Piano.Damage: 1,1.25,1.25,1.25,1.25
Piano.Pierce: 2,2,4,4,4
Piano.StepCount: 1,1,1,2,2
Violin.StepCount: 2,2,3,3,3
Violin.Range: 1,1.2,1.2,1.2,1.2
Violin.SpinSpeed: 1,1.3,1.3,1.3,1.3
Violin.Pierce: 3,3,5,5,5
Violin.Size: 1,1,1,1.5,1.5
Timpani.Range: 1,1.25,1.25,1.25,1.25
Timpani.BombardInterval: 0.65,0.65,0.4333333,0.4333333,0.4333333
```

추가로 10개 이펙트 클래스의 diff(`git diff Assets/Scripts/Combat/InstrumentAttacks/`)를 전부
줄 단위로 대조해, 각 호출부가 원래 삼항식과 동일한 `InstrumentType`/의미의 `GetXxx()`로
정확히 치환됐는지(오배선 없음) 확인함. 특이사항 1건: `FluteVortexEffect`의 `pullStrength`는
원래도 `radius`와 동일한 `(level>=3?1.5f:1f)` 배율을 공유했는데, 데이터화 후에도
`GetRangeMultiplier(Flute, level)`를 그대로 재사용해 동일하게 유지됨(별도 테이블로
분리하지 않은 것은 의도된 설계).

### 4-3. 회귀 - 10개 악기 실전 동작: 배선 정상 확인 ✅

Play 모드 진입 후(컴파일 상태 그대로, 콘솔 에러/경고 0건) 실제 게임 코드 경로로 10개 악기를
전부 Lv1/Lv5로 실행:

- **탭 5종**(피아노/벨/마림바/글록켄슈필/드럼): `InstrumentAttackDispatcher.Execute()`를
  실제 진입점 그대로 호출 - 전부 예외 없이 정상 실행, 콘솔 에러/경고 0건.
- **홀드 5종**(바이올린/프렌치호른/첼로/팀파니/플루트): `InstrumentAttackDispatcher.CreateHoldEffect()`로
  실제 게임과 동일하게 컴포넌트를 생성하고 `Init()`(플루트는 홀드 해제 시 스폰되는
  `FluteVortexEffect.Initialize()`까지)을 호출한 뒤, 리플렉션으로 내부 필드를 실측해
  테이블 값이 실제로 배선되어 쓰이고 있는지 확인:

```
Violin Lv1: radius=1.4 bladeCount=2
Violin Lv5: radius=1.68 bladeCount=3
FrenchHorn Lv1: range=3 halfAngleDeg=60
FrenchHorn Lv5: range=3.75 halfAngleDeg=90
Cello Lv1: radius=1.8 slowFraction=0.4
Cello Lv5: radius=2.16 slowFraction=0.6
Timpani Lv1: bombardInterval=0.65
Timpani Lv5: bombardInterval=0.4333333
Flute Lv1: radius=2 pullStrength=2.5 duration=1.5
Flute Lv5: radius=3 pullStrength=3.75 duration=2.1
```

전부 기대값과 일치(예: Violin radius 1.4×1.2=1.68, FrenchHorn range 3×1.25=3.75 등). 값을
읽어서 안 쓰거나 엉뚱한 곳에 쓰는 배선 실수 없음을 확인.

### 결론

모든 검증 항목 통과. 밸런스 수치 변경 없이 순수 데이터화 리팩토링이 의도대로 완료됨.
커밋 전 상태이며, 이 세션은 git 조작을 하지 않음(사용자가 직접 스테이징/커밋 필요).
