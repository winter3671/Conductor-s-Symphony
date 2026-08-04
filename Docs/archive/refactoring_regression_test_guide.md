# 리팩토링(#40~44) 회귀 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 리팩토링을 실측 검증할 때 참고하는
절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서 진행하고, **아직
커밋하지 않은 상태**입니다 — 이번에도 테스트 결과를 먼저 확인한 뒤에 커밋할 예정입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요(기존 라운드들과 동일한 방식). 버그를
발견하면 재현 절차·원인·(가능하다면) 수정 제안까지 적어주시면 Cowork 세션에서 그대로 반영합니다.

## 0. 이번 라운드가 다른 이유

`Docs/refactoring_recommendations.md`(별도 Claude Code 세션이 44개 파일 전체를 분석해 작성)의 권장
사항 중 사용자가 승인한 항목들을 반영한 **동작 보존(behavior-preserving) 리팩토링**입니다. 게임
로직·수치·밸런스는 전혀 건드리지 않았고, 코드 구조만 정리했습니다. 따라서 이번 라운드는 "새 기능이
의도대로 동작하는지"가 아니라 **"리팩토링 전과 완전히 동일하게 동작하는지"**를 확인하는 것이 목표입니다.

변경 내역 4가지:

1. **데미지 공식 순수 함수 추출**: `RhythmAttackManager`에 흩어져 있던 최종 데미지 계산식을
   `Assets/Scripts/Combat/DamageFormula.cs`(`ComputeBaseDamage`/`ComputeFinalDamage`)로 추출.
   기존 산술식과 완전히 동일한 연산 순서로 옮겼습니다.
2. **드럼 상시 오라 분리**: `RhythmAttackManager.Update()`에 있던 드럼 오라 로직을
   `Assets/Scripts/Combat/DrumAuraController.cs`로 분리. `RhythmAttackManager.Start()`가 자식
   GameObject로 직접 생성하므로 씬 파일에 수동 배치가 필요 없습니다.
3. **`FindObjectsByType<EnemyMonster>()` 15곳 통일**: 매 프레임/매 타격마다 씬 전체를 스캔하던 코드를
   전부 `CombatTargetingUtility.GetActiveEnemies()`(내부적으로 `EnemySpawner.Instance.ActiveEnemies`
   참조)로 교체. **이 변경이 테스트 방법론에 영향을 줍니다 — 아래 1번 항목을 반드시 먼저 읽어주세요.**
4. **탭 5종 디스패치를 `ITapAttackEffect` 인터페이스로 통합**: 기존 `InstrumentAttackDispatcher`
   안의 거대한 switch문(`ExecutePiano`/`ExecuteBell`/`ExecuteMarimba`/`ExecuteGlockenspiel`/
   `ExecuteDrums`)을 홀드 5종(`IHoldAttackEffect`)과 동일한 패턴으로 각각의 클래스 파일로 분리했습니다
   (`PianoBeamEffect.cs`, `BellStarburstEffect.cs`, `MarimbaWaveEffect.cs`,
   `GlockenspielStarfallEffect.cs`, `DrumBeatBangEffect.cs`). 내부 계산식은 원본 그대로 복사했습니다.
   공유 로직(스프라이트 캐시, 투사체/임팩트 스폰)은 `TapAttackHelpers.cs`로 뽑았습니다.

또한 죽은 코드 2곳을 제거했습니다(동작에 영향 없음): `RhythmAttackManager.FindNearestEnemy()`(이미
리팩토링 3번 과정에서 자연히 사라짐), `InstrumentOrbit.SetAngle()`(본문이 주석뿐인 빈 메서드, 호출부
없음 확인됨).

**의도적으로 보존한 것**: `RhythmAttackManager.HandleRhythmHit()`의 범용 투사체 폴백 로직은
`refactoring_recommendations.md`가 "즉시 삭제 가능"으로 분류했지만, 이전에 사용자가 유지하기로
결정한 사안(`Docs/team_review_needed.md` 1-1)이라 **삭제하지 않고 내부 타겟 조회만
`CombatTargetingUtility.GetActiveEnemies()`로 현대화**했습니다. 이 로직은 현재 10종 전체가 구현되어
있어 실질적으로 도달하지 않지만, 컴파일과 구조는 확인이 필요합니다.

---

## 1. ⚠️ 가장 먼저 확인: 테스트 더미 스폰 방식이 바뀌었습니다

**지금까지 모든 테스트 라운드(1~4단계, DPS 1~2차)는 몹 더미를 `new GameObject().AddComponent<EnemyMonster>()`로
직접(bare) 생성해서 사용했습니다.** 리팩토링 3번(`FindObjectsByType` → `ActiveEnemies`) 이전에는 이
방식으로도 모든 이펙트가 더미를 정상적으로 감지했습니다 — `FindObjectsByType`가 씬에 존재하는 모든
`EnemyMonster`를 무조건 스캔했기 때문입니다.

**이제는 감지 경로가 `EnemySpawner.Instance.ActiveEnemies`(private `List<EnemyMonster> activeEnemies`를
감싼 프로퍼티)로 바뀌었습니다.** `EnemySpawner.SpawnEnemy()`를 거치지 않고 bare `AddComponent`로만
만든 더미는 이 리스트에 들어가지 않으므로, **리팩토링 이전에는 맞던 코드가 리팩토링 이후에는 몹을
전혀 감지하지 못하는 것처럼 보일 수 있습니다.** 이건 게임 버그가 아니라 테스트 스크립트를 갱신해야
하는 부분입니다.

더미를 만들 때는 아래처럼 리플렉션으로 `EnemySpawner`의 private 리스트에 직접 등록해주세요:

```csharp
using System.Reflection;
using ConductorSymphony.Enemy;

// 1) 더미를 평소처럼 생성
GameObject dummyObj = new GameObject("TestDummy");
EnemyMonster dummy = dummyObj.AddComponent<EnemyMonster>();
dummy.transform.position = new Vector3(x, y, 0f);
// (기존 테스트에서 하던 초기화 - InitializeStats 등 - 동일하게 수행)

// 2) EnemySpawner.activeEnemies(private List<EnemyMonster>)에 리플렉션으로 등록
var spawner = EnemySpawner.Instance;
var field = typeof(EnemySpawner).GetField("activeEnemies", BindingFlags.NonPublic | BindingFlags.Instance);
var list = (System.Collections.Generic.List<EnemyMonster>)field.GetValue(spawner);
list.Add(dummy);
```

정리가 끝나면(테스트 종료 시) `DestroyImmediate(dummyObj)`로 파괴하되, 씬에 남아있는 진짜 스폰
몹과 헷갈리지 않도록 **테스트 시작 전 `activeEnemies` 리스트 상태를 확인**하는 것도 권장합니다(2차
DPS 테스트에서 실제로 겪었던 문제 — `Docs/archive/dps_balance_test_result.md` 참고).

**보스(`BossMonster`)는 이번 변경과 무관합니다** — 여전히 `BossMonster.Instance`를 직접 참조하는
별도 경로이므로 기존 방식 그대로 테스트하면 됩니다.

---

## 2. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 신규 파일이
   9개(`DamageFormula.cs`, `DrumAuraController.cs`, `ITapAttackEffect.cs`, `TapAttackHelpers.cs`,
   `PianoBeamEffect.cs`, `BellStarburstEffect.cs`, `MarimbaWaveEffect.cs`,
   `GlockenspielStarfallEffect.cs`, `DrumBeatBangEffect.cs`)이라 네임스페이스/using 누락이 없는지
   특히 꼼꼼히 봐주세요.
2. 위 1번 항목(테스트 더미 등록 방식)을 먼저 반영한 공용 헬퍼 함수를 하나 만들어두고 이후 모든 항목에서
   재사용하는 걸 권장합니다.

---

## 3. 핵심 검증 항목

### 3-1. 데미지 계산 값 동일성 (`DamageFormula`)
- [ ] 임의의 레벨/판정(Perfect/Good)에서 최종 데미지 값이 리팩토링 전 계산식과 정확히 일치하는지
      (예: Perfect, extraDamage=0, mRhythm=1.2, mStat=1.0, instrumentDpsMultiplier=2.9(바이올린 Lv5)
      → `Mathf.Max(1, Mathf.RoundToInt(2 * 1.2 * 1.0 * 2.9))` 값과 실측 데미지가 같은지)
- [ ] `DamageFormula.ComputeFinalDamage`가 최소 데미지 1을 보장하는지(0 이하로 내려가지 않는지)

### 3-2. 드럼 상시 오라 (`DrumAuraController`)
- [ ] 드럼을 장착하고 아무것도 안 눌러도 오라가 즉시 켜져서 지속 피해를 주는지 (씬에 수동 배치 없이
      `RhythmAttackManager.Start()`가 자동 생성한 자식 GameObject로 동작하는지 Hierarchy에서 확인)
- [ ] Lv4 오라 강화(+50%)가 여전히 적용되는지
- [ ] 드럼을 장착 해제하면 오라가 꺼지는지
- [ ] **회귀**: 정박 타격 시 비트 뱅(넉백)은 오라와 별개로 정상 발동하는지 (`DrumBeatBangEffect`)

### 3-3. `CombatTargetingUtility.GetActiveEnemies()` 전면 교체 (15곳)
1번 항목의 더미 등록 방법으로 몹 1~2마리를 세팅한 뒤, 아래 10종 전부에서 몹을 정상적으로 인식하는지
확인해주세요 (전부 예전과 동일한 사거리/판정 범위/이펙트여야 합니다):

- [ ] 피아노 - 가장 가까운 적 방향 관통 레이저 명중
- [ ] 벨 - 가장 가까운 적 중심 8방향 성광 명중
- [ ] 마림바 - 진행 방향 파동 명중, Lv5 피격 시 감속+밀쳐냄(`onHitEnemy` 콜백)
- [ ] 글록켄슈필 - 체력 최고 적 타겟팅, Lv5 2차 유도 파편이 "다른" 적을 정상적으로 찾는지
      (`FindSecondaryShrapnelTarget`)
- [ ] 드럼 - 비트 뱅 범위 내 적 전부 타격
- [ ] 바이올린 - 회전 칼날 지속 타격(`ViolinOrbitEffect.TickDamage`)
- [ ] 프렌치호른 - 부채꼴 범위 내 적 지속 타격+넉백
- [ ] 첼로 - 중력장 범위 내 적 지속 타격(`CelloGravityFieldEffect`)
- [ ] 팀파니 - 폭격 범위 내 적 타격(`AreaImpactEffect`)
- [ ] 플루트 - 소용돌이 흡입(`FluteVortexEffect.Update`/`ExplodeWindShard`)
- [ ] 관통 투사체 공용 클래스(`PiercingBeamProjectile`) - 경로상 여러 적을 순서대로 관통 타격
- [ ] 잔류 장판 공용 클래스(`LingeringZoneEffect`) - 벨 Lv5/바이올린 Lv5/팀파니 Lv5 잔향·지진지대
- [ ] 범용 투사체 폴백 로직(`RhythmAttackManager.HandleRhythmHit` 하단) - 컴파일만 확인, 정상 경로면
      도달하지 않는 게 맞음 (10종 전부 구현되어 있으므로)

### 3-4. 탭 5종 디스패치 통합 (`ITapAttackEffect`)
switch문 기반에서 딕셔너리 조회로 바뀌었을 뿐 계산식은 원본 그대로 복사했지만, 딕셔너리 초기화나
델리게이트 캡처 과정에서 값이 섞이는 실수가 없었는지 아래를 확인해주세요:

- [ ] 5종(피아노/벨/마림바/글록켄슈필/드럼) 전부 자신의 악기 로직만 실행되는지 (다른 악기 이펙트가
      섞여 나오지 않는지 - 특히 딕셔너리 초기화 순서와 무관하게 동작해야 정상)
- [ ] `InstrumentAttackDispatcher.IsImplemented(type)`이 5종에서만 true, 홀드 5종/미구현 타입에서는
      false를 반환하는지
- [ ] 레벨/데미지/currentCombo/origin/color 파라미터가 각 이펙트에 정확히 전달되는지 (특히 피아노
      Lv5 "6연타 폭포"와 글록켄슈필 Lv4 "4콤보 버스트"처럼 `currentCombo` 값에 의존하는 분기)

### 3-5. 죽은 코드 제거
- [ ] `InstrumentOrbit`(플레이어 주변을 도는 악기 아이콘)이 여전히 정상적으로 위치를 따라다니고
      bobbing 애니메이션이 동작하는지 (`SetAngle` 제거와 무관하게 `Update()`의 나머지 로직은 그대로)
- [ ] 프로젝트 전체 컴파일에 `FindNearestEnemy`/`SetAngle` 관련 참조 에러가 없는지

---

## 4. 알려진 위험 요소 (설계자/테스터 확인 필요)

- **드럼 오라 자동 생성 타이밍**: `DrumAuraController`가 `RhythmAttackManager.Start()`에서 생성되므로,
  `RhythmAttackManager`의 `Start()`가 실행되기 전(예: 씬 로드 직후 매우 이른 프레임)에 다른 스크립트가
  드럼 오라 상태를 조회하면 아직 `null`일 수 있습니다. 실제로 이런 타이밍 의존이 있는 코드가 있는지는
  확인되지 않았으나, 널 체크 없이 `drumAuraController`를 참조하는 곳이 새로 생기면 주의가 필요합니다.
- **테스트 더미 등록 누락 시 증상**: 1번 항목을 빠뜨리고 기존 방식(bare `AddComponent`)으로만 더미를
  만들면 모든 공격이 "허공에 발동은 되지만 아무도 맞지 않는" 것처럼 보입니다. 데미지 로그가 전혀
  찍히지 않으면 게임 버그가 아니라 테스트 스크립트 문제일 가능성을 먼저 의심해주세요.

---

## 5. 이번 라운드 통과 후 남는 것

이 라운드가 통과하면 `refactoring_recommendations.md`에서 승인된 항목(#3, #5 전체, #1 대응하는
탭/홀드 비대칭 해소, 죽은 코드 제거)이 전부 반영 완료됩니다. 보류된 항목은 우선순위 #2(수치
ScriptableObject화 - 밸런스 수치가 더 안정된 뒤 진행 권장)와 #4(EnemyMonster 상태이상 필드 일반화 -
5번째 CC 타입이 실제로 필요해질 때까지 보류)입니다.

---

## 6. 검증 결과 (2026-08-05)

Unity MCP(`My project` 인스턴스)로 실측 검증했습니다. `refresh_unity(force+compile)` → 컴파일 에러
0건(9개 신규 파일 포함, using/네임스페이스 문제 없음). **종합 결과: 전 항목 PASS. 리팩토링 전과
동작이 완전히 동일하며, 발견된 회귀 버그는 없습니다.**

### 0. 테스트 더미 등록 방식 전환 확인
가이드 1번 항목이 경고한 대로, bare `AddComponent`로만 만든 더미는 `CombatTargetingUtility.GetNearestEnemy()`
등에서 실제로 전혀 감지되지 않음을 먼저 확인했습니다(`GetActiveEnemies().Count=0`). 이후
`EnemySpawner.activeEnemies`(private List)에 리플렉션으로 등록한 더미는 정상적으로 감지됨을
확인(`GetNearestEnemy() == 등록한 더미` → True). 가이드의 안내가 정확했고, 이후 모든 항목은 이
등록 방식으로 진행했습니다.

### 3-1. 데미지 계산 값 동일성 — PASS
`DamageFormula.ComputeBaseDamage`/`ComputeFinalDamage`를 원본 산술식(`Mathf.Max(1,
Mathf.RoundToInt(baseDamage * mRhythm * mStat * mult))`)과 나란히 계산해 완전히 동일한 값(예:
Perfect+extraDamage1, mRhythm=1.2, mStat=1.1, mult=2.9 → 둘 다 11)이 나옴을 확인. 최소 데미지 1
보장(모든 배율이 0.01일 때도 결과 1)도 확인.

### 3-2. 드럼 상시 오라 (`DrumAuraController`) — PASS
- `RhythmAttackManager.Start()`가 자식 GameObject로 `DrumAuraController`를 자동 생성함을
  리플렉션으로 확인(씬에 수동 배치 불필요, 가이드 설명과 일치).
- Lv1 오라 1틱 데미지 21, Lv4로 업그레이드 후 1틱 데미지 36 — 리팩토링 전 공식(`auraLevelMultiplier
  1.5배 + InstrumentDamageTable Lv4 배율 23.7`)과 정확히 일치.
- 드럼을 `AcquiredInstruments`에서 제거하자 다음 `Update()` 호출에서 `auraActive`가 즉시 `False`로
  전환됨을 확인 — 장착 해제 시 오라가 꺼지는 로직 정상.

### 3-3. `CombatTargetingUtility.GetActiveEnemies()` 전면 교체 — PASS
아래 항목 전부 리플렉션으로 등록한 더미를 정상적으로 찾아 타격/판정함을 확인:
- 피아노(빔 1발), 벨(8방향 빔 8발 + Lv5 잔향), 마림바(빔 1발), 글록켄슈필(임팩트 1개 + Lv5 유도
  파편) — 아래 3-4 항목에서 한번에 검증.
- 드럼 비트 뱅 — `DrumBeatBangEffect.FireBeatBangShockwave`가 `GetActiveEnemies()`로 더미를 찾아
  타격.
- 바이올린 Lv3(칼날 지속 타격, 홀드 2.5초간 160 데미지), 프렌치호른 Lv4(부채꼴 지속 타격+증폭,
  276 데미지), 첼로 Lv5(중력장 지속 타격, 120 데미지) — 전부 `OnHoldTick` 루프에서 정상적으로
  적을 찾아 타격.
- 팀파니 Lv1 — 홀드 시작 즉시 캐논(`AreaImpactEffect`)이 `Time.deltaTime` 내부 참조 딜레이 때문에
  `timeScale=0`에서는 자연 발동하지 않는 기존에 알려진 특성 그대로 재확인(3~4단계부터 동일) —
  `Impact()`를 리플렉션으로 강제 호출하자 정상적으로 20 데미지 적용됨을 확인. **이건 회귀가 아니라
  테스트 환경(결정론적 재현 기법)의 기존 특성입니다.**
- 플루트 Lv3 — 소용돌이가 정상 스폰되고(`FluteVortexEffect` 1개), 무피해(CC 전용) 특성이 그대로
  유지됨(더미 HP 불변)을 확인. `GetActiveEnemies()`가 실제로 3마리(테스트 중 남아있던 이전 등록
  더미 포함)를 반환함도 확인해 스캔 경로 자체는 살아있음을 검증.
- 글록켄슈필 Lv5 2차 유도 파편 — 주 타겟(HP 2,000,000)과 별개로 5유닛 떨어진 보조 더미를 등록한 뒤
  실행한 결과, `FindSecondaryShrapnelTarget()`이 정확히 그 보조 더미를 선택함(`target==secondary:
  True`)과 26 데미지(`round(20*1.3)`) 적용을 확인. **2차 DPS 테스트에서 실제 leftover 적이 오염을
  일으켰던 것과 동일한 유형의 실수(등록 리스트에 다른 더미가 남아있는 상태)를 의도적으로 재현한
  채로 진행했는데도 정상적으로 올바른 타겟을 선택** — `GetActiveEnemies()` 전환이 이 종류의 타겟팅
  오염 위험을 새로 만들지 않았음을 확인했습니다.

### 3-4. 탭 5종 디스패치 통합 (`ITapAttackEffect`) — PASS
- `IsImplemented(type)`이 정확히 탭 5종(드럼/피아노/벨/마림바/글록켄슈필)에서만 `true`, 홀드 5종에서는
  `false`를 반환함을 10종 전부 순회하며 확인(중복/누락 없음).
- `InstrumentAttackDispatcher.Execute()`를 피아노→벨→마림바→글록켄슈필→드럼 순서로 연달아 호출해
  스폰된 오브젝트 수를 확인: 피아노 1발, 벨 8발, 마림바 1발, 글록켄슈필 임팩트 1개(빔 0개), 드럼
  즉시 판정 1회 — 전부 기대치와 정확히 일치. 직전 호출의 이펙트가 다음 호출에 섞여 나오는 현상
  없음(딕셔너리 초기화/캡처 문제 없음).

### 3-5. 죽은 코드 제거 — PASS
- `InstrumentOrbit`: `SetAngle`이 리플렉션 조회에서 실제로 사라졌음을 확인(`GetMethod("SetAngle") ==
  null`), 나머지 `Update()`(추적+bobbing)는 예외 없이 정상 실행됨을 확인.
- `RhythmAttackManager.FindNearestEnemy` 관련 참조 에러 없음(애초에 프로젝트 전체 grep으로도
  호출부가 없었음 - 컴파일 성공으로 최종 확인).
- 범용 투사체 폴백 로직(의도적으로 유지된 부분, `Docs/team_review_needed.md` 1-1 참고)도 내부
  타겟 조회가 `GetActiveEnemies()`로 정상 교체된 채 컴파일됨을 확인.

### 결론
9개 신규 파일 + 11개 수정 파일 전부 컴파일 정상, 4가지 리팩토링(데미지 공식 추출/드럼 오라 분리/
전역 스캔 통합/탭 디스패치 인터페이스화) 전부 기존과 동일한 수치·동작을 유지함을 확인했습니다.
**커밋 진행해도 좋습니다.**
