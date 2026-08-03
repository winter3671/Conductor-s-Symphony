# 🏆 [Portfolio Case Study] 기획 문서 기반 밸런스 시스템 3종 전면 재구현 & Unity MCP 실측 테스트로 잡아낸 MonoSingleton Destroy 지연 실행 소프트락

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)
> **핵심 성과:** `game_balance_design.docx` 기획 스펙(딜량 공식 / 20레벨 EXP 곡선 / 4구간 난이도-보스전)을 코드로 전면 구현하고, Unity MCP 기반 Edit/Play Mode 실측 테스트 2회전으로 "엘리트 생존 중 10:00 도달 시 최종 보스가 영구히 나타나지 않는" 100% 재현 소프트락을 발견·근본 원인 규명·수정·회귀 검증까지 완료

---

## 📌 1. 작업 배경 (Context)

기존 코드는 밸런스 기획서 없이 임의로 잡힌 수치로 굴러가고 있었다.

* **레벨링:** `PlayerExperience`가 `40 * 1.38^n` 지수 곡선으로 무한정 레벨업, 만렙 개념 없음.
* **몬스터 스폰:** `EnemySpawner`가 절대 시간이 아니라 "보스 처치 후 120초"마다 `stageLevel`을 1씩 증가시키는 상대적 스테이지 구조. 몹 HP도 `4 + (stageLevel-1)*10`처럼 디자인 근거 없는 임의 공식.
* **딜량:** `RhythmAttackManager`가 판정 등급(Perfect/Great)에 고정 정수(2 또는 1)만 더하는 방식이라, 플레이어의 리듬 정확도가 실제 딜량에 전혀 반영되지 않음.

새로 작성된 `game_balance_design.docx`는 이 세 가지에 정확한 수치와 공식을 규정했다: **최종 DPS = 기본 DPS × M_rhythm(리듬 성공률 배율) × M_stat(스탯 강화 배율)**, **Lv1~20 절대 시간 기준 EXP 곡선(4구간, 총 22,200 EXP)과 Lv1/5/10/15 무기 슬롯 해금**, 그리고 **00:00~10:00 절대 시간 4구간 난이도 곡선 + 10:00~12:00 단일 최종 보스 타임어택(HP 180,000)**. 기획 문서를 코드 시스템으로 그대로 옮기는 작업이었다.

---

## 🏗️ 2. 구현 내역 (What Was Built)

### 2-1. 절대 시간 기반 20레벨 EXP 곡선

`PlayerExperience.cs`의 지수 곡선을, 문서의 구간별 min~max 범위와 구간 총합이 정확히 일치하는 등차수열로 교체했다.

```csharp
// Lv1~5(4단): 250,283,317,350 → 1,200 / Lv5~10(5단): 500..700 → 3,000
// Lv10~15(5단): 1000..1400 → 6,000 / Lv15~20(5단): 2000..2800 → 12,000 (총 22,200)
private static readonly int[] ExpToNextLevel = new int[]
{
    250, 283, 317, 350,
    500, 550, 600, 650, 700,
    1000, 1100, 1200, 1300, 1400,
    2000, 2200, 2400, 2600, 2800
};
public const int MaxLevel = 20;
```

`Lv20` 도달 시 `AddExp()`가 조용히 무시되도록 캡을 걸었고, `InstrumentManager.GetUnlockedSlotsCount()`도 기존 Lv5/8 임의 기준에서 문서가 규정한 **Lv1(드럼 1슬롯)/5/10/15** 기준으로 교체했다. `LevelUpUI`는 이미 "슬롯이 꽉 찼으면 업그레이드만, 비어있으면 신규 악기도" 로직을 갖고 있었기 때문에, 슬롯 해금 임계값 하나만 바꿔도 카드 팝업 동작이 자동으로 문서 스펙에 맞춰졌다.

### 2-2. 4구간 난이도 곡선 + 2분 주기 엘리트 + 10:00 단일 최종 보스

`EnemySpawner.cs`의 "보스 처치 후 경과시간으로 증가하는 `stageLevel`" 구조를 걷어내고, **게임 시작 후 절대 경과 시간(`Time.time - startTime`)** 기준 4구간(00:00~02:30 / ~05:00 / ~07:30 / ~10:00)으로 완전히 재설계했다.

```csharp
[SerializeField] private int[] concurrentMin = { 15, 30, 60, 100 };
[SerializeField] private int[] concurrentMax = { 30, 60, 100, 150 };
[SerializeField] private int[] mobHpMin = { 15, 50, 150, 350 };
[SerializeField] private int[] mobHpMax = { 30, 100, 250, 500 };
[SerializeField] private int[] eliteHp  = { 200, 600, 1800, 4000 };
[SerializeField] private int[] expPerKill = { 10, 12, 15, 20 };
```

동시 존재 몹 수는 구간 진행률에 따라 min→max로 선형 보간해 자연스러운 난이도 곡선을 만들었고, 엘리트는 기존처럼 전체 스폰을 멈추지 않고 **2분 주기로 잡몹 스폰과 동시에 등장**하도록 바꿨다(이 부분이 기존 "엘리트 생존 중 스폰 전면 정지" 구조와의 핵심 차이다). 10:00(600초)에 도달하면 잔여 잡몹/엘리트를 전부 정리하고, HP 180,000·120초 타임어택의 `FinalBossMonster`가 단독으로 등장하도록 `BossMonster.cs`에 `InitializeFinalBoss()`와 시간초과 패배(`OnFinalBossTimeUpEvent`)/제한시간 내 클리어(`OnFinalBossClearedEvent`) 이벤트를 신설했다.

### 2-3. 리듬 성공률 → 딜량 배율(M_rhythm)

`RhythmManager.cs`에 최근 20개 판정(Perfect/Great=성공, Miss=실패)의 롤링 윈도우를 추가하고, 문서의 "0%→0.5배 / 50%→1.25배 / 100%→2.0배"를 그대로 만족하는 선형식을 구현했다.

```csharp
public float RhythmSuccessRate01 { get; private set; } = 0f; // 최근 20개 판정 성공률
public float GetRhythmDamageMultiplier() => 0.5f + 1.5f * RhythmSuccessRate01;
```

`RhythmAttackManager.cs`의 데미지 계산은 `Mathf.Max(1, Mathf.RoundToInt(baseDamage * mRhythm * mStat))`로 교체했다. `M_stat`(패시브 스탯 강화 배율)은 아직 패시브 시스템 자체가 없어 `1.0f` 상수로 자리만 마련해두고, 실제 강화도를 꽂아넣을 지점을 주석으로 명시해뒀다 — 공식의 뼈대만 먼저 심고 세부 시스템은 후속 작업으로 분리하는 판단이었다.

---

## 🔬 3. Unity MCP 실측 테스트로 발견한 Critical 버그

세 시스템 모두 코드 작성 후 Unity MCP(Claude Code, `execute_code`/`read_console`/`refresh_unity`)로 실측 검증을 거쳤다. EXP 곡선과 리듬 배율은 Edit Mode에서 리플렉션으로 직접 함수를 호출하는 격리 테스트로 1차 검증에서 바로 전부 일치했다. 그런데 **몬스터 스폰/보스전 항목에서 100% 재현되는 Critical 소프트락이 발견됐다.**

### CASE. 엘리트 생존 중 10:00 도달 시 최종 보스가 영구히 스폰되지 않는 소프트락

**재현 절차:** 엘리트를 처치하지 않고 방치한 채 경과시간이 600초(10:00)에 도달하도록 `Time.timeScale`을 가속.

**실측 로그:**
```
Elapsed=429.0, ... BossExists(isFinal=False, HP=200/200)   // 엘리트 생존 중
Elapsed=600.0, ... NoBoss, no-active-mob                    // 잡몹은 제거됐는데 보스도 없음
finalBossTriggered=True, BossMonster.Instance=False, foundBossObjects=[NONE]
```
그 이후 아무리 대기해도 최종 보스가 나타나지 않았다. 반대로 **엘리트가 없는 상태**에서 동일 전환 로직을 호출하면 `HP=180000/180000`으로 정상 스폰됨을 대조 실험으로 확인 — 버그는 정확히 "전환 시점에 살아있는 엘리트가 있었는가"에만 의존했다.

**원인 (`EnemySpawner.TriggerFinalBossPhase()` + `MonoSingleton<T>.Awake()`):**
```csharp
// 수정 전
if (BossMonster.Instance != null)
{
    Destroy(BossMonster.Instance.gameObject); // ★ Destroy()는 그 프레임 끝까지 지연 적용됨
}
GameObject bossObj = new GameObject("FinalBossMonster");
BossMonster boss = bossObj.AddComponent<BossMonster>(); // ★ AddComponent 시점에 Awake()가 동기 실행됨
```
```csharp
// MonoSingleton<T>.Awake()
if (Instance != null && Instance != this) { Destroy(gameObject); return; } // ★ 아직 안 지워진 옛 Instance를 봄
Instance = (T)this;
```
`Destroy(elite.gameObject)`가 실제로 적용되기 전(같은 프레임 내)에 `AddComponent<BossMonster>()`가 `Awake()`를 동기 호출하면서, 아직 "살아있는" 옛 엘리트를 `Instance != null && Instance != this`로 감지해 **새로 만든 최종 보스 GameObject 자신을 그 자리에서 파괴**해버렸다. 결과적으로 옛 엘리트도 신규 보스도 그 프레임 끝에 함께 사라지고, `BossMonster.Instance`는 계속 `null`로 남는다. `EnemySpawner.Update()`는 `finalBossTriggered=true`를 이미 세팅해 재시도조차 하지 않으므로, **엘리트를 미리 처치해두지 않으면 100% 재현되는 소프트락**이었다.

### CASE. (부수 발견) Edit Mode에서 `MonoSingleton.Awake()`가 자동 호출되지 않는 테스트 함정

같은 검증 과정에서, `InstrumentManager.GetUnlockedSlotsCount()`를 Edit Mode `execute_code`로 단위 테스트할 때 전 구간이 `slots=1`로 잘못 나오는 현상이 있었다. 코드 버그가 아니라 **Edit Mode에서 `AddComponent`해도 `MonoSingleton.Awake()`가 자동 호출되지 않아 `PlayerExperience.Instance`가 계속 `null`로 남아 있던 것**(→ 함수가 기본값 Lv1로 폴백)이 원인이었다. 정적 `Instance`를 리플렉션으로 직접 주입한 뒤 재검증해서 실제 로직은 정상임을 확인했다 — `MonoSingleton` 기반 매니저를 Edit Mode에서 단위 테스트할 때 반복적으로 마주칠 수 있는 함정이라 기록해둔다.

---

## 🛠️ 4. 해결 방법 (The Fix)

`Destroy()`는 그 프레임 끝까지 실제 파괴를 미루지만, `MonoSingleton.Awake()`는 정적 `Instance` 필드를 직접 참조하는 동기 코드다. 즉 **"아직 안 지워졌다"와 "이미 Destroy()가 예약됐다"는 서로 다른 상태인데, 코드는 전자만 보고 있었다.** 해법은 `Destroy()`를 호출하기 전에 정적 `Instance`를 먼저 명시적으로 비우는 것이다.

```csharp
// Assets/Scripts/Utility/MonoSingleton.cs — 신설
protected static void ClearInstance()
{
    Instance = null;
}
```
```csharp
// Assets/Scripts/Enemy/BossMonster.cs — 신설
public void ReleaseSingletonSlot()
{
    if (Instance == this)
    {
        ClearInstance();
    }
}
```
```csharp
// Assets/Scripts/Enemy/EnemySpawner.cs — TriggerFinalBossPhase() 수정 후
if (BossMonster.Instance != null)
{
    BossMonster oldBoss = BossMonster.Instance;
    oldBoss.ReleaseSingletonSlot(); // Destroy() 이전에 정적 슬롯부터 비운다
    Destroy(oldBoss.gameObject);
}
```
`ReleaseSingletonSlot()`은 `TriggerFinalBossPhase()`(같은 프레임 내 교체가 실제로 일어나는 유일한 지점)에서만 호출되도록 최소 범위로 적용했다. 엘리트 자체의 처치 경로(`BossMonster.Die()` → `Destroy(gameObject)`)는 같은 프레임에 새 인스턴스를 만들지 않으므로 건드리지 않았다.

---

## ✅ 5. 검증 (1차 발견 → 수정 → 2차 회귀 테스트)

| 검증 항목 | 1차 결과 | 2차 결과(수정 후) |
|---|---|---|
| 컴파일 | PASS | PASS |
| EXP 누적 곡선 (Lv1~20, 19단계 전부) | PASS | PASS (스팟체크) |
| 무기 슬롯 해금 기준 (Lv1/5/10/15) | PASS | PASS (스팟체크) |
| 구간별 동시 몹 수 / HP / EXP | PASS | 재변경 없어 미재검 |
| 엘리트 생존 중 잡몹 스폰 지속 | PASS | 재변경 없어 미재검 |
| **엘리트 생존 중 10:00 도달 → 최종 보스 전환** | **FAIL (소프트락)** | **PASS** |
| 일반 엘리트 처치(이벤트/보상상자) | (암묵적 확인) | PASS |
| 최종 보스 타임어택(시간초과 패배) | PASS | PASS |
| 리듬 성공률 → M_rhythm 배율(0%/50%/100%) | PASS | PASS (스팟체크) |
| 최종 데미지 공식(최소 1 보장) | PASS | 재변경 없어 미재검 |

2차 검증에서는 1차와 동일한 재현 절차(엘리트 생존 상태로 600초 통과)를 그대로 반복했다.

```
[600초 도달 직후]
BossMonster.Instance=True, foundBossObjects=[FinalBossMonster(isFinal=True, HP=180000/180000)]
```
1차에서 `foundBossObjects=[NONE]`이었던 것과 대비된다. 이어서 같은 세션에서 120초 타임어택 만료 시 시간초과 패배 로그, 그리고 별도 세션에서 엘리트를 정상적으로 처치했을 때 보상 상자가 여전히 스폰되는지(회귀 없음)까지 확인했다.

---

## 💎 6. 핵심 교훈 (Key Takeaways)

**"Destroy()가 호출됐다"와 "실제로 파괴됐다" 사이에는 최소 한 프레임의 간극이 있고, 그 사이에 실행되는 동기 코드는 여전히 "살아있는" 옛 객체를 본다.** `Destroy(obj)`는 해당 프레임의 나머지 로직에서 `obj == null` 비교가 참이 되는 마법 같은 순간을 보장해주지 않는다 — 그 판정은 실제 네이티브 오브젝트가 파괴된 뒤에만 유효하다. 같은 프레임 안에서 무언가를 지우고 곧바로 그 자리를 대체하는 코드를 짤 때는, "지워질 예정"과 "이미 지워짐"을 혼동하지 않도록 상태를 직접 통제해야 한다.

**싱글톤 패턴에 "교체" 개념이 없으면, 교체가 필요한 순간마다 임시방편이 하나씩 쌓인다.** `MonoSingleton<T>`는 원래 "최초 생성"만 상정한 설계였다 — 이번처럼 "기존 것을 없애고 새것으로 바꿔치기"하는 시나리오는 설계 당시 고려되지 않았다. `ClearInstance()`라는 한 줄짜리 헬퍼를 베이스 클래스에 추가하는 것만으로, 앞으로 어떤 파생 싱글톤이든 같은 문제를 안전하게 처리할 수 있는 공용 해법이 생겼다 — 문제가 생긴 그 자리(`BossMonster`)만 땜질하지 않고 원인이 있는 계층(`MonoSingleton`)에 해법을 심은 것이 핵심이다.

**기획 문서를 코드로 옮기는 작업일수록, 실측 테스트가 스펙 준수 여부보다 더 큰 것을 잡아낼 때가 있다.** 이번 세 시스템은 전부 문서의 수치와 정확히 일치하게 구현됐지만, 그 정확한 구현 자체가 "엘리트와 최종 보스가 같은 싱글톤 슬롯을 공유한다"는 새로운 상호작용을 만들어냈고, 그 상호작용의 타이밍 버그는 스펙만 대조해서는 절대 드러나지 않았다. 시간 가속(`Time.timeScale`)과 리플렉션 기반 강제 시나리오 재현으로 "정상적으론 몇 분에 한 번 우연히 걸릴 수도 있는" 경계 조건을 결정론적으로 반복 재현한 것이 발견의 핵심이었다.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
