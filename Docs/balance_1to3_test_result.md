# game_balance_design.docx 1~3번(+4번) 구현 검증 결과

`balance_1to3_test_guide.md`에 정리된 절차에 따라 **Unity MCP(Unity 6000.5.5f1, `My project` 인스턴스)**로
실측 검증을 진행한 결과입니다. 총 3차에 걸쳐 검증했습니다.

- **1차 검증**: 딜량/레벨링 공식은 전부 정상이었으나, 몬스터 스폰 로직에서 보스 전환 시점 치명적 회귀
  버그(엘리트 생존 중 10:00 도달 시 최종 보스 미스폰 소프트락) 1건 발견.
- **2차 검증**: `MonoSingleton.ClearInstance()` + `BossMonster.ReleaseSingletonSlot()` 수정 적용 후 동일
  시나리오로 회귀 테스트 → **버그 해결 확인**. 그 외 항목은 코드 변경이 없어 스팟체크로 재확인, 전부 정상.
- **3차 검증(가이드 5번 항목 = 문서 4번 "패시브 스탯" 신규 검증)**: 8종 패시브의 수치 계산 로직 자체는
  전부 정상이지만, **`PassiveStatManager`가 실제 `Gameplay.unity` 씬에 배치되어 있지 않아 실제 플레이에서는
  패시브 시스템 전체가 완전히 죽어있는 상태**임을 발견 (Critical).
- **수정 및 재검증(이 문서 상단에 결과 추가)**: Unity MCP로 직접 `_Managers` 하위에 `PassiveStatManager`
  GameObject를 추가하고 씬을 저장한 뒤 재검증 → **해결 확인**. 상세는 "3차 검증" 섹션의 "수정 작업" 항목 참고.

**최종 종합 결과: 문서 1~4번(딜량 / 레벨링 / 몬스터 스폰·보스전 / 패시브 스탯) 전부 PASS.**
(아래 "3차 검증" 섹션과 최하단 종합 결론 표 참고)

---

## 3차 검증 (패시브 스탯 시스템, 문서 4번 / 가이드 5번 항목) — 2026-08-03

### 무엇을 테스트했나
신규 파일 `Assets/Scripts/Passive/PassiveStatData.cs`, `PassiveStatManager.cs` + 기존 파일 수정
(`RhythmAttackManager.cs`의 `mStat`, `PlayerController.cs`의 이동속도/피해감소/최대HP, `ExpGem.cs`의 자석범위,
`LevelUpUI.cs`의 카드풀 혼합)을 가이드 5번 절차대로 검증.

### ✅ Edit Mode — 8종 getter 수치 검증: PASS
가이드 제시 스크립트를 확장해서, 만렙(Lv5) 수치뿐 아니라 **Legato의 계단식 지급(Lv1~5: 0,0,1,1,2)**과
**Lv5 이후 추가 획득 시도 시 레벨 캡 유지**까지 검증:
```
[Legato] Lv1:0, Lv2:0, Lv3:1, Lv4:1, Lv5:2 - 전부 OK (계단식 지급 정확)
[Legato] Lv5 이후 추가 획득 시도해도 레벨 캡 유지: True
Sforzando dmg mult=1.5, Vivace move mult=1.4, Resonance pickup mult=2.25,
Tuning dmg reduction=0.25, Tuning maxHP bonus=0.5,
Allegro cooldown reduction=0.3(미사용), Crescendo range mult=1.5(미사용), Fermata duration mult=1.75(미사용)
ALL_MATCH=True
```

### 🔴 발견된 문제 (Critical, 수정 완료) — **`PassiveStatManager`가 씬에 없어서 패시브 시스템이 실플레이에서 완전히 비활성 상태**

**증상**: Play Mode 진입 직후 `PassiveStatManager.Instance`가 계속 `null`.
```
PlayerController.Instance=True
PassiveStatManager.Instance=False   // ← 다른 매니저와 다르게 존재하지 않음
InstrumentManager.Instance=True
LevelUpUI.Instance=True
```
`find_gameobjects`로 씬 전체를 찾아봐도 `PassiveStatManager` 컴포넌트를 가진 GameObject가 **하나도 없음**
(이름에 "Manager" 들어간 오브젝트 자체가 0개 — `InstrumentManager`/`EnemySpawner`/`RhythmManager` 등 기존
매니저들도 전부 씬에 미리 배치된 GameObject 방식이라, 이번 패시브 매니저만 씬 배치가 누락된 것으로 보임).

**실측 영향 — 레벨업 카드풀이 의도보다 훨씬 빈약해짐**:
```
[PassiveStatManager 없는 현재 씬 상태] Lv1→2 레벨업 카드 수=1, 패시브 후보=0, 악기 후보=1
                                        (드럼 업그레이드 카드 1장만 뜸)
```
**대조 실험** (같은 Play 세션에 `PassiveStatManager`를 임시로 추가하고 동일 시점에 재호출):
```
[PassiveStatManager 존재 시] 카드 수=3, 패시브 후보=2, 악기 후보=1  (의도된 정상 동작)
```
즉 코드 로직 자체(카드 후보 셔플, `AcquireOrUpgrade` 등)는 문제 없고, **씬에 매니저 오브젝트를 추가하는
작업만 누락**된 상태입니다. 이 상태로 빌드하면:
- 저레벨 구간(Lv1~4, 악기 슬롯이 꽉 찬 상태)에서 레벨업 카드가 1장(악기 업그레이드)만 뜨거나 극단적으로는
  0장이 뜰 수도 있음 (모든 장착 악기가 Lv5 만렙이고 슬롯도 가득 찬 경우).
- 패시브 8종을 플레이어가 절대 획득할 수 없으므로, `RhythmAttackManager`/`PlayerController`/`ExpGem`에
  이미 잘 연결해둔 시포르찬도/비바체/레가토/공명패널/악보튜닝 효과가 **게임에서 영원히 발동하지 않음**.

### 🔧 수정 작업 (Unity MCP로 직접 처리)

1. `manage_scene(get_hierarchy)`로 씬 구조 확인 → 모든 매니저가 `_Managers`(instanceID 74756) 오브젝트
   하위에 "컴포넌트명과 동일한 이름의 자식 GameObject" 패턴(`RhythmManager`, `EnemySpawner`,
   `RhythmAttackManager`, `AudioLayerManager`, `InstrumentManager`)으로 배치되어 있음을 확인.
2. `manage_gameobject(action="create", name="PassiveStatManager", parent=74756, components_to_add=["PassiveStatManager"])`
   로 동일한 패턴의 자식 GameObject를 생성 — 기존 매니저들과 이름/배치 방식 일치.
3. `manage_scene(get_hierarchy, parent=74756)`로 정상 생성 확인 (`Transform` + `ConductorSymphony.Passive.PassiveStatManager`
   컴포넌트만 붙은 깨끗한 오브젝트).
4. `manage_scene(action="save")`로 `Assets/Scenes/Gameplay.unity`에 저장 (`git status` 기준
   `M Assets/Scenes/Gameplay.unity`로 변경사항 반영 확인).

### ✅ 수정 후 재검증 — RESOLVED

Play Mode 재진입 직후:
```
PassiveStatManager.Instance exists at Play start: True   // 수정 전에는 False였음
```
`LevelUpUI.ShowLevelUpSelection()`도 재호출해 확인:
```
수정 후 씬 상태 -> 카드 수=3, 패시브 후보=3, 악기 후보=0
```
(이번엔 셔플 결과 패시브만 3장 뽑혔음 — 총 후보 풀이 악기 1 + 패시브 8 = 9개이므로 매번 랜덤 조합이
나오는 게 정상 동작. 중요한 건 이제 패시브 후보가 풀에 실제로 섞여 들어간다는 점.)

씬에 **영구적으로** 배치된(임시 오브젝트가 아닌) 실제 `PassiveStatManager` 인스턴스로 Tuning 효과도
재확인:
```
[씬에 영구 배치된 실제 매니저로 재확인] MaxHealth 100->150 (expected 150) OK
```
콘솔 에러 없음. `manage_editor(stop)`으로 Play Mode 정상 종료 확인.

**결론**: `_Managers` 하위에 `PassiveStatManager` GameObject를 추가하는 것만으로 문제가 완전히
해결되었습니다. 코드 변경은 전혀 없었고 순수하게 씬 자산에 누락된 배치를 보완한 것입니다.

### ✅ Play Mode 통합 검증 (임시로 씬에 매니저를 추가한 상태에서 진행) — PASS
위 버그로 인해 정상 씬 상태에서는 패시브를 하나도 얻을 수 없어, `PassiveStatManager`를 Play 세션에
임시로 추가한 뒤 각 연결 지점을 실측:

| 패시브 | 검증 방법 | 결과 |
|---|---|---|
| Tuning(최대HP) | Lv5까지 획득 → `PlayerController.MaxHealth` 100→150 확인 (늘어난 만큼 CurrentHealth도 동시 회복) | PASS |
| Tuning(피해감소) | `TakeDamage(100)` 실손실 75(=100×(1-0.25)) 확인 | PASS |
| Vivace(이동속도) | `GetMoveSpeedMultiplier()`가 `PlayerController.FixedUpdate()`와 동일 소스를 참조함을 코드/값으로 재확인 | PASS |
| Resonance(자석범위) | 거리 6.0 지점에 EXP 젬 스폰 → Resonance Lv5(사거리 x2.25=7.875) 상태에서는 자동 흡수됨, **대조군**(Resonance Lv0, 사거리 3.5)에서는 흡수 안 되고 그대로 남음 | PASS |
| Sforzando(데미지) | 리듬 성공률 100% 고정(`mRhythm=2.0`) + Sforzando Lv5(`mStat=1.5`) 상태에서 `RhythmAttackManager.HandleRhythmHit(Perfect)` 실행 → 더미 타겟 실제 데미지 6 (= base2×2.0×1.5) 확인 | PASS |
| Legato(투사체) | Edit Mode에서 계단식 지급 검증 완료 (`RhythmAttackManager`가 `GetExtraProjectiles()`를 그대로 더해 씀을 코드로 확인) | PASS |

> 참고: `TakeDamage` 첫 시도에서 실손실이 0으로 나온 적이 있었는데, 이는 코드 버그가 아니라 씬에서 실제
> 몬스터에게 방금 맞아 무적프레임(`invulnerabilityDuration=0.5s`)이 남아있던 테스트 타이밍 문제였음.
> `invulnerableTimer`를 리플렉션으로 0으로 만든 뒤 재시도해 정상 확인.

### ✅ 미연결 3종(Allegro/Crescendo/Fermata) — 가이드 설명과 일치 확인
`GetCooldownReductionFraction`/`GetRangeMultiplier`/`GetDurationMultiplier`를 프로젝트 전체에서 검색한
결과, `PassiveStatManager.cs` 자기 자신 외에는 **어디에서도 호출되지 않음** — 가이드 문서에 적힌 "값 계산은
정상이지만 소비하는 대상이 아직 없다"는 설명과 정확히 일치. 버그 아님, 의도된 상태.

---

## 2차 검증 (버그 수정 후 재테스트) — 2026-08-03

### 수정 내용 확인
`git status` 기준 1차 검증 이후 `Assets/Scripts/Utility/MonoSingleton.cs`, `BossMonster.cs`,
`EnemySpawner.cs` 3개 파일이 추가로 수정됨. 코드 리뷰 결과:
- `MonoSingleton<T>`에 `protected static void ClearInstance() { Instance = null; }` 추가.
- `BossMonster`에 `public void ReleaseSingletonSlot() { if (Instance == this) ClearInstance(); }` 추가.
- `EnemySpawner.TriggerFinalBossPhase()`에서 기존 `Destroy(BossMonster.Instance.gameObject)` 호출 **직전에**
  `oldBoss.ReleaseSingletonSlot()`을 먼저 호출하도록 변경 — 1차 검증에서 지적한 "정적 Instance를 먼저
  비우는" 수정 방향(제안 2번)과 일치.

### 컴파일 확인
`refresh_unity(mode=force, compile=request)` 실행 → `ready_for_tools=true`, 컴파일 에러 없음 (콘솔에는
MCP 플러그인 자체의 WebSocket 경고 1건만 존재, 프로젝트 코드와 무관).

### 회귀 시나리오 재현 — ✅ 해결 확인
1차와 동일하게 Play Mode 진입 → `Application.runInBackground=true` + `Time.timeScale=20f` 설정 →
**엘리트를 죽이지 않고 방치**한 채 경과시간이 600초를 넘도록 대기.

```
[중간] Elapsed=413.7, Segment=2, Active=90, BossExists(isFinal=False, HP=200/200)   // 엘리트 생존 중 확인
[600초 도달 직후]
Elapsed=600.0, IsMobPhaseActive=False, finalBossTriggered=True, ActiveMobs=0,
BossMonster.Instance=True,
foundBossObjects=[FinalBossMonster(isFinal=True, HP=180000/180000)]
```
1차 검증 때와 동일하게 엘리트가 살아있는 상태로 10:00을 넘겼음에도, 이번에는 **최종 보스가 정상 스폰**됨
(1차 결과는 `foundBossObjects=[NONE]`으로 소프트락). 이후 프레임에서도 보스가 사라지지 않고 지속 생존
확인. 이어서 120초 타임어택 만료 시 `"[BossMonster] Final boss time limit exceeded - Defeat (Time Over)"`
로그와 `Time.timeScale=0` 정지도 정상 동작 확인 — 기존에 PASS였던 하위 동작들도 이번 수정으로 깨지지
않았음을 확인.

### 일반 엘리트 처치 경로 회귀 확인 — PASS
`ReleaseSingletonSlot()`이 `TriggerFinalBossPhase()`에서만 호출되고 엘리트의 자체 `Die()` 경로는 손대지
않았음을 코드로 확인. 실측으로도 새 Play 세션에서 엘리트를 `TakeDamage`로 처치 → `OnBossDefeatedEvent`
정상 발생 + `EliteRewardChest` GameObject 정상 스폰 확인. (참고: 이 세션에서 `Time.timeScale=20`을 켠 채
여러 차례 도구 호출 사이 실제 대기 시간이 누적되면서, 확인 도중 게임 시간이 자연스럽게 또 한 번 600초를
넘겨 두 번째 최종 보스 사이클(스폰→타임업 패배)이 **엘리트 없이** 정상적으로 재현되는 것도 부수적으로
확인함 — 동일 세션에서 보스 전환 로직이 반복 동작해도 안정적임을 보여주는 추가 근거.)

### 기존 통과 항목 스팟체크 — PASS (회귀 없음)
이번 수정에서 건드리지 않은 `PlayerExperience`/`InstrumentManager`/`RhythmManager`는 대표 경계값만 빠르게
재확인 (전체 19단계 재검증은 1차 결과 참고):
```
[EXP] Lv2/MaxExp283=True, Lv19/MaxExp2800=True, Lv20/MaxExp0=True
[Slots] Lv4=1slot:True, Lv5=2slots:True, Lv15=4slots:True
[Rhythm] 0%->0.5x:True, 100%->2.0x:True
ALL_MATCH=True
```

### 2차 검증 결론
1차에서 발견된 Critical 버그(엘리트 생존 중 최종 보스 미스폰 소프트락)는 **해결 확인**. 사이드 이펙트 없이
기존 정상 동작(엘리트 처치, 타임어택, 클리어/패배 이벤트, EXP/슬롯/리듬 배율)도 모두 유지됨.

---

## 1차 검증 결과 (아래부터는 이전 실측 기록 원문)

---

## 0. 사전 준비 — 컴파일 확인

- `editor/state` 리소스: `is_compiling=false`, `ready_for_tools=true` — 컴파일 정상 완료 상태.
- `read_console`(error/warning): 가이드에 명시된 7개 파일 관련 에러/경고 없음. 콘솔에 남은 항목은
  `Unity AI Assistant` 플러그인의 `RelayService`/`Account API` 관련 무관 경고·에러뿐 (프로젝트 코드와 무관).
- Play Mode 진입/종료 전후 재확인해도 신규 컴파일 에러 없음.

→ **PASS**

---

## 1. 레벨링 & EXP 구조 (문서 2번) — PASS

### 1-1. EXP 누적 곡선 (`PlayerExperience.AddExp`)
가이드에 제시된 코드 그대로 `execute_code`(Edit Mode)로 실행. Lv1→20까지 19회 레벨업 전 구간에서
`MaxExp` 값이 문서 표(250~2800)와 정확히 일치, 최종 `CurrentLevel=20`에서 추가 `AddExp(99999)` 호출 시
레벨/MaxExp 변화 없음(만렙 캡 정상 동작) 확인.

```
Step 0: Lv=2, MaxExp=283 ... (중략, 19단계 전부 OK)
Step 18: Lv=20, MaxExp=0 (expectedNext=0) OK
Final Level = 20 (expected 20), MaxExp after over-cap AddExp = 0 (expected 0) OK
ALL_MATCH=True
```

### 1-2. 무기 슬롯 해금 기준 (`InstrumentManager.GetUnlockedSlotsCount`)
Lv1/4/5/9/10/14/15/20 각 경계값에서 슬롯 수(1/1/2/2/3/3/4/4) 전부 기대값과 일치.

```
Lv=1: slots=1 (expected 1) OK
Lv=5: slots=2 (expected 2) OK
Lv=10: slots=3 (expected 3) OK
Lv=15: slots=4 (expected 4) OK
Lv=20: slots=4 (expected 4) OK
ALL_MATCH=True
```

> **테스트 방법론 노트**: 첫 시도에서는 전 구간이 `slots=1`로 잘못 나왔는데, 이는 코드 버그가 아니라
> **Edit Mode에서 `AddComponent`해도 `MonoSingleton.Awake()`가 자동 호출되지 않아
> `PlayerExperience.Instance`가 계속 `null`로 남아있었기 때문**(→ `GetUnlockedSlotsCount()`가 기본값 Lv1로 폴백).
> 정적 `Instance` 프로퍼티를 리플렉션으로 직접 주입한 뒤 재검증하여 위 결과를 얻음. 이후 Unity MCP로
> `MonoSingleton` 기반 매니저를 Edit Mode에서 단위 검증할 때 동일하게 주의 필요.

---

## 2. 몬스터 스폰 & 보스전 (문서 3번) — 1차 결과: 부분 FAIL (치명적 버그 1건, 이후 수정되어 2차 검증에서 해결 확인됨)

Play Mode 진입 후 `Time.timeScale = 20f`로 가속하여 실측. (아래 "테스트 환경 이슈" 참고)

### 확인된 정상 동작
- [x] 구간 전환: Elapsed 429s → Segment=2(300~450 구간), ExpPerKill=15로 정확히 일치. Elapsed 600s 도달 시
      `IsMobPhaseActive=false`, `Segment=3` 값도 경계값과 일치.
- [x] 구간별 동시 존재 몹 수: Segment 2(60~100 min~max) 구간에서 `ActiveEnemies.Count=94`로 범위 내 확인.
- [x] **엘리트 생존 중 잡몹 스폰 지속** (이번 리팩토링의 핵심 변경점): Elapsed=429s 시점에 엘리트(HP 200/200,
      Segment 0에서 스폰)가 살아있는 상태에서 동시에 잡몹 94마리가 존재 — 기존처럼 엘리트 생존 중 스폰이
      멈추는 회귀가 없음을 확인.
- [x] 엘리트 HP: 최초 엘리트가 Segment 0(120초 시점)에 스폰되어 `eliteHp[0]=200`과 일치.
- [x] 10:00 도달 시 잡몹 전부 제거: `Active=0` 확인.
- [x] 최종 보스 타임어택 시간초과: `finalBossTimer >= 120s` 시점에 콘솔에
      `"[BossMonster] Final boss time limit exceeded - Defeat (Time Over)"` 로그 출력 + `Time.timeScale=0`으로
      정지 확인.
- [x] 최종 보스 클리어 이벤트: `TakeDamage`로 HP를 0 이하로 만들면 `OnFinalBossClearedEvent` 발생 +
      `"[BossMonster] Final boss cleared within time limit - Victory!"` 로그 확인.

### 🔴 발견된 버그 (Critical): **10:00 시점에 엘리트가 아직 살아있으면 최종 보스가 아예 스폰되지 않고 게임이 멈춤**

**재현 절차**: 엘리트가 죽지 않은 채(예: 플레이어 입력 없음/방치) 경과시간이 600초에 도달.

**실측 로그**:
```
Elapsed=429.0, ... BossExists(isFinal=False, HP=200/200)      // 엘리트 생존 중
Elapsed=600.0, ... NoBoss, no-active-mob                       // 잡몹은 정상 제거됐지만 보스도 없음
finalBossTriggered=True, IsMobPhaseActive=False, BossMonster.Instance=False, foundBossObjects=[NONE]
```
그 후 4초(=80게임초)를 더 대기해도 `BossComponents=0`으로 최종 보스가 영구히 나타나지 않음(사실상 소프트락).

**대조 실험**: 동일 상태에서 `BossMonster.Instance`가 `null`인 채로(엘리트 없음) `TriggerFinalBossPhase()`를
다시 호출하면 → `스폰 성공: isFinal=True, HP=180000/180000`으로 **정상 스폰됨**. 즉 버그는 "전환 시점에
살아있는 엘리트가 있었는가"에만 의존.

**근본 원인** (`EnemySpawner.cs:169-196` + `Utility/MonoSingleton.cs:9-17`):
```csharp
// EnemySpawner.TriggerFinalBossPhase()
if (BossMonster.Instance != null)
{
    Destroy(BossMonster.Instance.gameObject); // ★ Destroy()는 해당 프레임 끝까지 지연 실행됨
}
...
GameObject bossObj = new GameObject("FinalBossMonster");
BossMonster boss = bossObj.AddComponent<BossMonster>(); // ★ AddComponent 시점에 Awake()가 동기 실행됨
```
```csharp
// MonoSingleton<T>.Awake()
if (Instance != null && Instance != this) { Destroy(gameObject); return; } // ★ 아직 안 지워진 옛 Instance를 봄
Instance = (T)this;
```
`Destroy(elite.gameObject)`가 실제로 적용되기 전(같은 프레임 내)에 `AddComponent<BossMonster>()`가
`Awake()`를 동기 호출하면서, 아직 살아있는 옛 엘리트를 `Instance != null && Instance != this`로 감지해
**새로 만든 최종 보스 GameObject 자신을 그 자리에서 파괴**해 버립니다. 결과적으로 옛 엘리트도, 신규
보스도 모두 그 프레임 끝에 사라지고 `BossMonster.Instance`는 계속 `null`로 남아 최종 보스전이 영원히
시작되지 않습니다. `EnemySpawner.Update()`는 `finalBossTriggered=true`를 이미 세팅해 재시도도 하지 않으므로
**플레이어가 엘리트를 미리 처치해두지 않으면 100% 재현되는 소프트락**입니다.

**제안 수정 방향** (택1, 코드 수정은 하지 않고 제안만 드립니다):
1. `Destroy()` 대신 `DestroyImmediate()`로 옛 엘리트를 즉시 제거한 뒤 신규 보스를 생성 (Play Mode에서
   `DestroyImmediate` 사용은 일반적으로 권장되지 않으나 여기선 같은 프레임 순서 보장이 핵심이므로 검토 가능).
2. 옛 `BossMonster.Instance`의 `Instance` 정적 필드를 **먼저 `null`로 리셋**한 뒤 `Destroy()`를 호출하고
   나서 신규 보스를 생성 (가장 안전 — `MonoSingleton`에 `protected static void ClearInstance()` 같은
   헬퍼를 추가하거나, `BossMonster`에 명시적 정리 메서드를 두는 방법).
3. 엘리트 스폰 시점부터 최종 보스 트리거 이전에 남은 엘리트를 먼저 `Destroy` + 한 프레임 대기(코루틴)
   후 보스를 생성.

### 참고(버그 아님) — HP 바 잔존
위 버그의 부작용으로, 소프트락 상태에서 UI상 `BossHpText`가 마지막 엘리트 HP(200/200) 그대로 남아있는 게
관찰됨. 이는 가이드 4번 섹션에 이미 알려진 "최종 보스 클리어 시 HP바 자동 미제거" 이슈와는 별개로, 이번에
새로 발견된 소프트락의 파생 증상입니다.

---

## 3. 딜량 공식: 기본 DPS × M_rhythm × M_stat (문서 1번) — PASS

### 3-1. `RhythmManager` 롤링 성공률 → `GetRhythmDamageMultiplier()`
리플렉션으로 `RecordHitResult(bool)`를 직접 호출해 4개 시나리오 검증:

```
[0 hits]              rate=0,   mult=0.5  (기대 0.5)  OK
[20/20 success]       rate=1,   mult=2.0  (기대 2.0)  OK
[10/20 success]       rate=0.5, mult=1.25 (기대 1.25) OK
[0/20 success 재전환] rate=0,   mult=0.5  (기대 0.5)  OK
ALL_MATCH=True
```
0%→0.5x, 50%→1.25x, 100%→2.0x 선형 공식이 정확히 구현되어 있고, 최근 20개 롤링 윈도우가 성공→실패
전환 시에도 정상적으로 다시 하락하는 것까지 확인.

### 3-2. `RhythmAttackManager` 최종 데미지 공식
`Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat))` 계산을 몇 가지 케이스로 검산:
- Great(base=1) × mRhythm=0.5 → 0.5 → **최소 1 보장** 정상 (0 데미지로 죽지 않는 문제 없음)
- Perfect(base=2) × mRhythm=2.0 → 4 정상

> **참고(버그 아님, 사소한 미세 조정 여지)**: `base=2 × mRhythm=1.25 = 2.5`인 경우 `Mathf.RoundToInt`가
> "0.5는 짝수로 반올림"(banker's rounding) 규칙을 따라 **3이 아닌 2**로 내림됩니다. 문서에 반올림 규칙이
> 명시되어 있지 않아 버그로 보긴 어렵지만, 딜량 손실 체감이 있다면 `Mathf.CeilToInt` 등으로 바꾸는 걸
> 설계자가 검토해볼 수 있습니다.

---

## 테스트 환경 이슈 (버그 아님 — 향후 Unity MCP 테스트 시 참고)

Play Mode 진입 직후 Unity 에디터 창이 포커스를 잃은 상태(`editor.is_focused=false`)로 유지되자,
**`Time.timeScale`을 20으로 올려도 `Time.frameCount`가 전혀 증가하지 않고 플레이 자체가 사실상 멈추는
현상**이 있었습니다. `Application.runInBackground = true`를 `execute_code`로 강제 설정하자 즉시 프레임이
정상적으로 흐르기 시작했습니다. 이후 Unity MCP로 Play Mode 실측 테스트를 할 때는 **Play 진입 직후
`Application.runInBackground = true`를 먼저 설정**하는 단계를 `balance_1to3_test_guide.md` 0번 섹션에
추가해두는 것을 권장합니다.

---

## 종합 결론 (1~2차 기준 — 3차 패시브 검증 결과는 별도 표)

| 항목 | 1차 결과 | 2차 결과 |
|---|---|---|
| 컴파일 | PASS | PASS |
| EXP 누적 곡선 (Lv1~20) | PASS | PASS (스팟체크) |
| 무기 슬롯 해금 기준 (Lv5/10/15) | PASS | PASS (스팟체크) |
| 구간별 동시 몹 수 / HP / EXP | PASS | - (재변경 없어 미재검) |
| 엘리트 생존 중 잡몹 스폰 지속 | PASS | - (재변경 없어 미재검) |
| **엘리트 생존 중 10:00 도달 → 최종 보스 전환** | **FAIL (소프트락, Critical)** | **PASS (수정 확인)** |
| 일반 엘리트 처치(이벤트/보상상자) | (암묵적 확인) | PASS |
| 최종 보스 타임어택(시간초과 패배) | PASS | PASS |
| 최종 보스 클리어 이벤트 | PASS | - (재변경 없어 미재검) |
| 리듬 성공률 → M_rhythm 배율 | PASS | PASS (스팟체크) |
| 최종 데미지 공식(최소 1 보장 등) | PASS | - (재변경 없어 미재검) |

**결론**: 1차 검증에서 발견된 유일한 Critical 버그가 수정 후 해결 확인되었고, 사이드 이펙트도 없습니다.
문서 1~3번(딜량 공식 / 레벨링·EXP / 몬스터 스폰·보스전) 구현은 **전부 PASS**로 판단합니다.

---

## 종합 결론 — 3차 검증 (패시브 스탯, 문서 4번)

| 항목 | 결과 |
|---|---|
| 컴파일 | PASS |
| 8종 getter 수치 계산 (만렙값, Legato 계단식, 레벨 캡) | PASS |
| **`PassiveStatManager` 씬 배치 여부** | **최초 FAIL(Critical) → 수정 완료, 재검증 PASS** |
| 레벨업 카드풀에 패시브 혼합 (수정 후 실제 씬 상태로 재확인) | PASS |
| Tuning → 최대HP/피해감소 실제 연동 (수정 후 영구 매니저로 재확인) | PASS |
| Vivace → 이동속도 실제 연동 | PASS |
| Resonance → EXP 자석범위 실제 연동 (대조군 포함) | PASS |
| Sforzando → 실제 데미지 공식 반영 | PASS |
| Legato → 투사체 수 계단식 지급 반영 | PASS |
| Allegro/Crescendo/Fermata 미연결 상태 확인 | PASS (의도된 상태, 버그 아님) |

**결론**: 8종 패시브의 **계산 로직·연동 코드는 전부 정확**했고, 유일한 문제였던 `PassiveStatManager`
씬 배치 누락은 Unity MCP로 `_Managers` 하위에 GameObject를 추가하고 씬을 저장하는 방식으로 **직접
수정 완료**했습니다. 수정 후 재검증까지 마쳐 문서 4번(패시브 스탯) 항목도 **전부 PASS**로 최종 판단합니다.
