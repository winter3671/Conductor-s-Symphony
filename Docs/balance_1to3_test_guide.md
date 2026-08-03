# game_balance_design.docx 1~3번 구현 검증 가이드

이 문서는 `game_balance_design.docx`의 1~3번(딜량 공식 / 레벨링·EXP / 몬스터 스폰·보스전)을
코드에 반영한 뒤, **Unity MCP가 연결된 별도 Claude Code 세션**에서 어떻게 검증하면 되는지
정리한 절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서
진행하는 워크플로우를 전제로 합니다.

---

## 0. 공통 사전 준비

1. Unity 에디터에서 프로젝트를 열고 `MCP for Unity` 플러그인 세션이 Active인지 확인.
2. Claude Code에서 `refresh_unity` 호출 → 컴파일 에러 없이 도메인 리로드되는지 확인.
3. `read_console` (pattern 없이 전체, 또는 `"error"` 패턴)로 컴파일 에러/경고 없는지 1차 확인.
4. Play Mode 진입 직후 `execute_code`로 `Application.runInBackground = true;`를 먼저 설정할 것.
   (1차 실측에서 에디터 창이 포커스를 잃으면 `Time.timeScale`을 올려도 `Time.frameCount`가 전혀
   증가하지 않아 Play가 사실상 멈추는 현상이 있었음 — 이 설정으로 해결됨)
   - 특히 아래 4개 파일은 이번에 크게 바뀐 파일이라 우선 확인:
     `Assets/Scripts/Player/PlayerExperience.cs`, `Assets/Scripts/Enemy/EnemySpawner.cs`,
     `Assets/Scripts/Enemy/BossMonster.cs`, `Assets/Scripts/Rhythm/RhythmManager.cs`,
     `Assets/Scripts/Combat/RhythmAttackManager.cs`, `Assets/Scripts/Instrument/InstrumentManager.cs`,
     `Assets/Scripts/Enemy/EnemyMonster.cs`

컴파일이 깨끗하면 아래 3개 항목을 순서대로 검증합니다.

---

## 1. 레벨링 & EXP 구조 (문서 2번)

### 무엇이 바뀌었나
- `PlayerExperience.cs`: 지수 성장 곡선(`40 * 1.38^n`) → 문서의 4구간 누적 EXP 표로 교체 (Lv20 만렙, 이후 `AddExp` 무시)
- `EnemyMonster.Die()`: 고정 15 EXP → `EnemySpawner.GetCurrentExpPerKill()`로 경과시간 기준 10/12/15/20 가변
- `InstrumentManager.GetUnlockedSlotsCount()`: Lv5/8 기준 → Lv5/10/15 기준으로 교체 (Lv1~4는 드럼 1슬롯만)

### execute_code로 EXP 곡선 검증 (Edit Mode 가능, Play 안 해도 됨)
```csharp
// Claude Code의 execute_code 툴에 아래 C#을 그대로 전달
var pe = new GameObject("TestPE").AddComponent<ConductorSymphony.Player.PlayerExperience>();
int[] expected = {250,283,317,350,500,550,600,650,700,1000,1100,1200,1300,1400,2000,2200,2400,2600,2800};
for (int i = 0; i < expected.Length; i++)
{
    int need = expected[i];
    pe.AddExp(need); // MaxExp를 정확히 채워서 레벨업 1회 유발
    Debug.Log($"Step {i}: Lv={pe.CurrentLevel}, NextMaxExp={pe.MaxExp} (expected next {(i+1<expected.Length?expected[i+1].ToString():"0 (만렙)")})");
}
Debug.Log($"Final Level = {pe.CurrentLevel} (expected 20)");
GameObject.DestroyImmediate(pe.gameObject);
```
기대 로그: Lv가 1→20까지 정확히 19번 올라가고, 각 단계의 `MaxExp`가 위 표와 일치. 마지막에 `CurrentLevel=20`에서 `AddExp` 호출해도 더 이상 레벨업 안 되는지(`MaxExp=0` 유지) 확인.

### Play Mode에서 무기 슬롯 해금 확인
1. Play 진입 → `read_console`로 `HasInstrument`/카드 팝업 로그 확인 (또는 `manage_scene`으로 `InstrumentManager` 컴포넌트의 `acquiredInstruments` 개수 조회).
2. Lv1~4 구간: 드럼만 장착, 레벨업 카드가 "드럼 업그레이드"만 뜨는지 (신규 악기 카드 없어야 함).
3. Lv5 도달 순간: 신규 악기 카드가 처음으로 등장하는지 확인 (`LevelUpUI.ShowLevelUpSelection`에서 `slotsFull=false` 전환 시점).
4. Lv10, Lv15에서 각각 3번째/4번째 슬롯이 열리는지 동일하게 확인.

---

## 2. 몬스터 스폰 & 보스전 (문서 3번)

### 무엇이 바뀌었나
- `EnemySpawner.cs`: `stageLevel`(120초마다 증가) 구조 전면 제거 → 경과시간(0~600초) 기준 4구간
  (동접 15-30/30-60/60-100/100-150, 몹 HP 15-30/50-100/150-250/350-500, 엘리트 HP 200/600/1800/4000, 마리당 EXP 10/12/15/20)
- 엘리트는 2분(120초) 주기로 등장하되 **더 이상 잡몹 스폰을 막지 않음** (기존엔 엘리트 생존 중 전체 스폰 정지됐음)
- 10:00(600초) 도달 시 잔여 잡몹/엘리트 전부 정리 후 `FinalBossMonster` 단독 스폰 (HP 180,000, 120초 타임어택)
- `BossMonster.cs`: `InitializeFinalBoss()`, 타임어택 타이머, `OnFinalBossClearedEvent`(클리어) / `OnFinalBossTimeUpEvent`(시간초과 패배) 이벤트 신설

### 실측 시간이 10분+2분이라 오래 걸림 → 2가지 단축 방법 중 택1

**방법 A (권장, 코드 안 건드림): Time.timeScale 가속**
```csharp
// execute_code, Play Mode 진입 후 실행
Time.timeScale = 20f; // 10분을 30초로 압축해서 관찰 (물리/이동도 같이 빨라짐 주의)
```
`read_console`로 `EnemySpawner.Instance.ElapsedTime`, `CurrentSegmentIndex`, `GetCurrentExpPerKill()`를 주기적으로 Debug.Log 찍어서 구간 전환이 150/300/450/600초 지점에서 정확히 일어나는지 확인.

**방법 B (인스펙터 값 임시 조정): 구간 스케줄 자체를 축소**
`EnemySpawner` 컴포넌트의 `Mob Phase Duration`과 `Segment End Times`를 `manage_components set_property`로
`{15, 30, 45, 60}` / `mobPhaseDuration=60`처럼 1/10로 줄여서 1분 안에 전체 페이즈를 빠르게 훑어봄.
**검증 후 반드시 원래 값(150/300/450/600, 600)으로 되돌릴 것.**

### 확인 포인트 체크리스트
- [ ] 각 구간 진입 시 동시 존재 몹 수가 min→max로 자연스럽게 늘어나는지 (씬에서 `EnemySpawner.Instance.ActiveEnemies.Count` 로그)
- [ ] 각 구간에서 스폰되는 일반 몹 HP가 해당 구간 범위(예: 1구간 15~30) 안에 있는지 (`EnemyMonster`에 임시 로그 추가하거나 `TakeDamage` 몇 대 맞혀서 죽는 타수로 역산)
- [ ] 엘리트가 대략 120초 간격으로 등장하고, HP가 구간별 200/600/1800/4000과 일치하는지
- [ ] 엘리트가 떠 있는 동안에도 잡몹이 계속 스폰되는지 (기존처럼 스폰이 멈추면 회귀 버그)
- [ ] 10:00 시점에 잡몹이 전부 사라지고 `FinalBossMonster` 하나만 남는지, HP가 180,000인지
- [ ] **(회귀 테스트, 1차 실측에서 FAIL 발견 후 수정됨)** 엘리트를 일부러 안 잡고 살려둔 채 10:00을 넘겨도
      최종 보스가 정상적으로 스폰되는지 — `MonoSingleton`의 `Destroy()` 지연 실행 때문에
      `BossMonster.Instance`를 먼저 비우지 않고 옛 엘리트를 `Destroy()`만 하면, 같은 프레임에 생성되는
      최종 보스가 `Awake()`에서 자기 자신을 파괴해버리는 소프트락이 있었음
      (`MonoSingleton.ClearInstance()` + `BossMonster.ReleaseSingletonSlot()`로 수정, `EnemySpawner.TriggerFinalBossPhase()`에서
      `Destroy()` 호출 전에 반드시 `ReleaseSingletonSlot()`을 먼저 호출하도록 되어 있는지 확인)
- [ ] 보스전 120초 이내에 처치 시 `read_console`에 `"Final boss cleared within time limit"` 로그
- [ ] 120초 초과 시 `"Final boss time limit exceeded - Defeat"` 로그 + `Time.timeScale=0`으로 게임이 멈추는지

---

## 3. 딜량 공식: 최종 DPS = 기본 DPS × M_rhythm × M_stat (문서 1번)

### 무엇이 바뀌었나
- `RhythmManager.cs`: 최근 20개 판정의 성공률(`RhythmSuccessRate01`, Perfect/Great=성공, Miss=실패)을 추적하는
  롤링 윈도우 추가, `GetRhythmDamageMultiplier()` = `0.5 + 1.5 × 성공률` (0%→0.5배, 50%→1.25배, 100%→2.0배)
- `RhythmAttackManager.cs`: 기존 정수 데미지 계산에 `M_rhythm`을 곱하고, `M_stat`은 `1.0` 고정 상수로 자리만 마련
  (패시브 스탯 시스템은 이번 범위 제외 — 문서 4번 항목, 추후 작업)

### 검증 방법 (Play Mode 필요, 실제 QWER 입력 있어야 함)
1. `read_console`로 실시간 확인하려면 `RhythmAttackManager.HandleRhythmHit` 근처에 임시로
   `Debug.Log($"dmg={damage}, mRhythm={mRhythm}, successRate={RhythmManager.Instance.RhythmSuccessRate01}")`를
   추가해서 잠깐 찍어보고, 검증 끝나면 제거해도 되고 그대로 둬도 무방 (성능 영향 미미).
2. 시나리오별 기대값:
   - **게임 시작 직후 (판정 이력 0개):** `RhythmSuccessRate01=0` → `mRhythm=0.5` (문서의 "실패 0.5배"와 동일선상 출발)
   - **최근 20개 노트를 연속 Perfect/Great로 성공:** `RhythmSuccessRate01→1.0` → `mRhythm→2.0`
   - **최근 20개 중 10개만 성공(50%):** `mRhythm≈1.25`
3. `execute_code`로 강제 시뮬레이션도 가능 (private 필드라 리플렉션 필요):
```csharp
var rm = ConductorSymphony.Rhythm.RhythmManager.Instance;
var method = typeof(ConductorSymphony.Rhythm.RhythmManager).GetMethod("RecordHitResult",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
for (int i = 0; i < 20; i++) method.Invoke(rm, new object[] { true }); // 20개 전부 성공 가정
Debug.Log($"SuccessRate={rm.RhythmSuccessRate01}, Multiplier={rm.GetRhythmDamageMultiplier()} (expected 1.0 / 2.0)");
```

---

## 4. 알아두어야 할 가정/후속 작업 (설계자 확인 필요)

- **M_stat=1.0 고정**: 패시브 스탯 강화 시스템(장신구 8종, 문서 4번)은 이번 범위에서 구현하지 않음. 나중에
  `RhythmAttackManager.cs`의 `const float mStat = 1.0f;` 자리에 실제 강화도(0~100%)를 꽂아넣기만 하면 됨.
- **리듬 성공률 = 최근 20개 판정 롤링 윈도우**: 문서엔 "콤보 성공률"이라고만 되어 있어 정확한 창(window) 크기가
  명시되어 있지 않음. 미스 1번에 곧바로 0%로 떨어지는 극단적 스트릭 방식 대신, 최근 20개 기준 완만한 방식을
  채택함 — 실제 플레이 느낌 보고 창 크기(20)를 조정할 수 있음.
- **구간별 스폰 간격(spawnIntervalPerSegment)**: 문서에 명시되지 않은 값이라 임의로 넣은 튜닝값(1.0/0.6/0.4/0.28초).
  실측 플레이 후 체감 난이도에 맞춰 `EnemySpawner` 인스펙터에서 조정 필요.
  - 이 항목만 SerializeField라 씬에 저장된 프리팹/인스턴스 값이 코드 기본값을 덮어쓸 수 있음(DOCUMENTATION.md
    트러블슈팅 5번 사례와 동일 함정) — 인스펙터에서 실제 적용 값 재확인할 것.
- **엘리트 처치 시 보상 상자, 최종 보스 처치 시 상자 없음**: 최종 보스는 게임 클리어 취급이라 `EliteRewardChest`를
  스폰하지 않도록 분기함. 클리어 후 별도 결과 화면이 필요하면 추가 작업 필요.
- **최종 보스 시간초과 패배 처리**: 아직 별도 게임오버 UI가 없어서 `PlayerController.OnPlayerDeath()`와 동일한
  수준(Debug.Log + `Time.timeScale=0`)으로만 처리해둠. 게임오버 화면 붙이는 건 후속 작업.
- **RhythmUI 보스 HP바**: `OnBossDefeatedEvent`는 이제 엘리트 처치 때만 발생(최종 보스는 `OnFinalBossClearedEvent`로 분리).
  최종 보스 클리어 시 HP바가 자동으로 안 사라질 수 있음 — UI 쪽 후속 확인 필요.
- **BossMonster 기존 버그(회귀 아님)**: `Awake()`와 `Initialize()`가 각각 `OnBossSpawnedEvent`를 호출해서
  스폰 직후 HP바가 잠깐 기본값(120)으로 표시됐다가 실제 값으로 바뀌는 기존 동작이 남아있음. 이번 작업 범위 밖.
- **(수정 완료) 엘리트 생존 중 10:00 도달 시 최종 보스 미스폰 소프트락**: 1차 실측(`balance_1to3_test_result.md`)에서
  발견된 Critical 버그. `MonoSingleton<T>`에 `protected static void ClearInstance()`를 추가하고,
  `BossMonster.ReleaseSingletonSlot()`이 이를 사용해 `Destroy()` 호출 직전에 정적 `Instance`를 먼저 비우도록 수정함.
  회귀 테스트 필요 (위 체크리스트 참고).
- **데미지 반올림 규칙**: `Mathf.RoundToInt`가 `.5`를 짝수로 반올림(banker's rounding)하는 특성 때문에
  `base=2 × mRhythm=1.25 = 2.5` 같은 케이스가 3이 아닌 2로 계산됨. 문서에 반올림 규칙이 명시되어 있지 않아
  버그는 아니지만, 체감 딜량 손실이 있다면 `Mathf.CeilToInt`로 교체할지 설계자가 검토 가능.
