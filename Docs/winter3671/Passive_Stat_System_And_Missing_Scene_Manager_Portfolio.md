# 🏆 [Portfolio Case Study] 패시브 스탯(장신구) 8종 시스템 구현 & "코드는 맞는데 씬에 없다" 소환 실패 버그 진단기

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)
> **핵심 성과:** `game_balance_design.docx` section 4의 패시브 8종(시포르찬도/알레그로/크레센도/비바체/레가토/페르마타/공명패널/악보튜닝)을 기존 악기 시스템과 대칭 구조로 신규 구현하고, Unity MCP 실측 테스트로 "getter 계산은 전부 정확한데 실제 플레이에선 패시브가 하나도 안 뜨는" Critical 버그를 발견 — 원인이 코드가 아니라 씬(Scene) 자산의 오브젝트 배치 누락이었음을 규명하고 수정

---

## 📌 1. 작업 배경 (Context)

문서 1~3번(딜량 공식 / 레벨링·EXP / 몬스터 스폰·보스전)을 먼저 구현한 뒤, 문서 1번 공식의 `M_stat`(스탯 강화 배율)을 실제로 채워 넣기 위해 4번 항목인 패시브 스탯(장신구) 8종을 구현하는 차례였다. 문서는 8종 모두 "레벨당 효과, 최대 5레벨"이라는 동일한 골격을 갖고 있었지만, 실제로 영향을 주는 대상은 제각각이었다:

* **딜량에 직접 영향:** 시포르찬도(피해량 +10%/Lv)뿐. → 이것이 문서 1번의 `M_stat`이 된다.
* **기존 시스템에 바로 연결 가능:** 비바체(이동속도), 레가토(투사체 수), 공명 패널(EXP 자석범위), 악보 튜닝(피해감소·최대HP) — 전부 이미 존재하는 필드(`PlayerController.moveSpeed`, `ExpGem.magnetDistance` 등)에 배율만 곱하면 되는 구조.
* **연결 대상 자체가 없음:** 알레그로(쿨타임), 크레센도(공격 범위), 페르마타(장판 지속시간) — 지금 모든 악기가 "가장 가까운 적에게 원형 투사체 하나"로 통일된 상태라, 쿨타임이나 범위, 지속시간이라는 개념 자체가 코드에 없다. 이 3종은 별도 "10종 악기별 공격 메커니즘" 작업이 끝나야 진짜 의미가 생긴다.

이 판단(무엇을 지금 연결하고 무엇을 스텁으로 남길지)은 사용자와 사전에 합의한 뒤 진행했다.

---

## 🏗️ 2. 구현 내역 (What Was Built)

### 2-1. 기존 `Instrument*` 3종 구조를 그대로 미러링

`InstrumentType` / `InstrumentPatternDatabase` / `InstrumentInfo` / `InstrumentManager` 4종 구조를, 이름만 바꿔 `Passive` 네임스페이스에 대칭으로 새로 만들었다. 차이는 딱 하나 — 악기는 최대 4슬롯 제한이 있지만, 패시브는 슬롯 제한 없이 8종을 전부 모을 수 있다.

```csharp
// Assets/Scripts/Passive/PassiveStatData.cs
public enum PassiveStatType { Sforzando, Allegro, Crescendo, Vivace, Legato, Fermata, Resonance, Tuning }

public class PassiveStatManager : MonoSingleton<PassiveStatManager>
{
    private readonly List<PassiveStatInfo> acquired = new List<PassiveStatInfo>();
    // InstrumentManager.AcquireOrUpgradeInstrument()와 달리 슬롯 카운트 체크가 없다.
    public void AcquireOrUpgrade(PassiveStatType type) { ... }
}
```

### 2-2. 즉시 연결 5종 + 스텁 3종

```csharp
// 딜량(문서 1번 M_stat 그 자리) — 유일하게 실제로 영향을 주는 패시브
public float GetDamageMultiplier() => 1.0f + 0.10f * GetLevel(PassiveStatType.Sforzando); // Lv5=1.5x

// 아직 소비할 대상이 없는 3종 — 값 계산은 완성해두고, 나중에 악기 고유 메커니즘 작업 때
// 그대로 곱해서 쓰기만 하면 되도록 getter만 미리 뚫어놓음
public float GetCooldownReductionFraction() => Mathf.Min(0.30f, 0.06f * GetLevel(PassiveStatType.Allegro));
public float GetRangeMultiplier()           => 1.0f + 0.10f * GetLevel(PassiveStatType.Crescendo);
public float GetDurationMultiplier()        => 1.0f + 0.15f * GetLevel(PassiveStatType.Fermata);
```

`RhythmAttackManager`(시포르찬도→`mStat`), `PlayerController`(비바체→이동속도, 악보튜닝→피해감소/최대HP), `ExpGem`(공명 패널→자석범위), 그리고 `RhythmAttackManager`의 투사체 수 합산(레가토)까지 5곳에 실제 배율을 곱해 넣었다.

### 2-3. `LevelUpUI` 카드 풀 통합

기존엔 레벨업 카드가 "악기 업그레이드/신규 악기"만 뽑았다. 문서 2번이 요구하는 "성장 구간엔 기존 무기 레벨업 또는 패시브 스탯 강화 무작위 제시"를 만족시키려면, 두 종류의 후보를 하나의 풀에서 같이 셔플해야 했다. 서로 다른 타입(`InstrumentInfo` vs `PassiveStatInfo`)을 하나의 카드 목록으로 다루기 위해 작은 래퍼를 도입했다.

```csharp
private class LevelUpChoice
{
    public bool isPassive;
    public InstrumentType instrumentType; public int instrumentTargetLevel;
    public PassiveStatType passiveType;   public int passiveTargetLevel;
}
```

렌더링/선택 로직만 `isPassive` 분기로 갈라주면, 기존의 "슬롯 꽉 찼으면 업그레이드만" 같은 나머지 로직은 그대로 재사용됐다.

---

## 🔬 3. Unity MCP 실측 테스트로 발견한 문제 — "계산은 완벽한데 게임엔 존재하지 않는다"

Edit Mode에서 8종 getter를 전부 리플렉션으로 검증했을 때는 만렙 수치, 레가토의 계단식 지급(Lv1~5: 0,0,1,1,2), 레벨 캡까지 전부 문서와 정확히 일치했다. **그런데 Play Mode에 들어가자 `PassiveStatManager.Instance`가 계속 `null`이었다.**

```
PlayerController.Instance=True
PassiveStatManager.Instance=False   // ← 다른 매니저와 다르게 존재하지 않음
InstrumentManager.Instance=True
```

씬 전체를 뒤져봐도 `PassiveStatManager` 컴포넌트를 가진 GameObject가 하나도 없었다. **원인은 코드가 아니라 씬(Scene) 쪽이었다.** `InstrumentManager`, `EnemySpawner`, `RhythmManager` 같은 기존 매니저들은 전부 `Gameplay.unity`의 `_Managers` 오브젝트 하위에 "컴포넌트명과 같은 이름의 자식 GameObject"로 미리 배치되어 있었는데, 이번에 새로 만든 `PassiveStatManager`만 그 배치 작업이 빠져 있었던 것 — 스크립트 파일을 만드는 것과 그 스크립트를 실제로 씬의 어떤 오브젝트에 붙이는 것은 완전히 다른 두 단계인데, 후자를 누락했다.

**실측으로 확인된 실제 영향:**
```
[PassiveStatManager 없는 상태] Lv1→2 레벨업 카드 수=1, 패시브 후보=0, 악기 후보=1
[PassiveStatManager 존재 시]   카드 수=3, 패시브 후보=2, 악기 후보=1  ← 의도된 정상 동작
```
카드 풀 셔플 로직도, `AcquireOrUpgrade`도, 각 getter도 전부 정확했다. 하지만 그 로직에 접근할 진입점(`Instance`) 자체가 씬에 존재하지 않았으니, **패시브 시스템 전체가 플레이어 입장에서는 처음부터 존재한 적이 없는 기능**이었던 셈이다.

---

## 🛠️ 4. 해결 방법 (The Fix)

코드 한 줄도 고치지 않고, Unity MCP로 씬 자산만 보완했다.

1. `manage_scene(get_hierarchy)`로 `_Managers` 오브젝트(instanceID) 하위에 기존 매니저들이 어떤 패턴으로 배치돼 있는지 확인.
2. `manage_gameobject(action="create", name="PassiveStatManager", parent=_Managers, components_to_add=["PassiveStatManager"])`로 동일한 패턴의 자식 GameObject를 생성.
3. `manage_scene(get_hierarchy)`로 `Transform` + `PassiveStatManager` 컴포넌트만 깨끗하게 붙은 걸 재확인.
4. `manage_scene(action="save")`로 `Assets/Scenes/Gameplay.unity`에 영구 저장.

---

## ✅ 5. 검증 결과

| 항목 | 결과 |
|---|---|
| 8종 getter 수치 계산 (만렙값, 레가토 계단식, 레벨 캡) | PASS |
| **`PassiveStatManager` 씬 배치 여부** | **최초 FAIL(Critical) → 씬에 GameObject 추가 후 PASS** |
| 레벨업 카드풀에 패시브 혼합 (수정 후 실제 씬 상태로 재확인) | PASS |
| 시포르찬도 → 실제 데미지 공식 반영 (base×mRhythm×mStat 검산) | PASS |
| 비바체 → 이동속도 실연동 | PASS |
| 레가토 → 투사체 수 계단식 지급 반영 | PASS |
| 공명 패널 → EXP 자석범위 실연동 (대조군 포함 검증) | PASS |
| 악보 튜닝 → 최대HP/피해감소 실연동 (영구 배치된 매니저로 재확인) | PASS |
| 알레그로/크레센도/페르마타 미연결 상태 | PASS (의도된 스텁, 버그 아님 — 프로젝트 전체 검색으로 호출부 없음 확인) |

수정 후 Play Mode 재진입 시 `PassiveStatManager.Instance exists at Play start: True`로 확인했고, Tuning 레벨업으로 `MaxHealth 100→150`이 씬에 영구 배치된 실제 매니저 기준으로 재현됨을 검증했다.

---

## 💎 6. 핵심 교훈 (Key Takeaways)

**"코드가 컴파일된다"와 "그 코드가 게임에서 실행된다"는 서로 다른 명제다.** `MonoSingleton<T>` 패턴은 `Instance`를 static 프로퍼티로 노출하지만, 그 프로퍼티가 채워지려면 누군가 실제로 그 컴포넌트를 씬의 GameObject에 붙여줘야 한다. 이 프로젝트의 매니저들은 전부 "스크립트 작성"과 "씬 배치"가 분리된 2단계 워크플로우를 쓰고 있었는데, 새 매니저를 추가할 때 이 사실 자체를 놓치기 쉽다 — 컴파일러도, Edit Mode 단위 테스트도 이 누락을 잡아주지 못한다. Edit Mode에서 `AddComponent`로 직접 인스턴스를 만들어 테스트하면 (이 프로젝트에서 반복적으로 나타난 패턴대로) 오히려 "잘 동작하는 것처럼" 보이기 쉽고, 그 격리 테스트가 통과했다는 사실이 "씬에도 있다"는 착각을 강화한다.

**그래서 새 `MonoSingleton` 매니저를 추가하는 체크리스트에는 반드시 "씬 배치 확인"이 별도 항목으로 있어야 한다.** 격리된 단위 테스트(Edit Mode getter 검증)와 통합 테스트(Play Mode에서 `Instance != null`부터 확인)는 서로 다른 종류의 결함을 잡는다 — 이번 버그는 전자를 아무리 촘촘히 돌려도 절대 드러나지 않고, 오직 후자에서만 드러나는 종류였다. Play Mode 진입 직후 가장 먼저 확인해야 할 것은 기능의 정확성이 아니라, **그 기능을 담당하는 매니저가 애초에 존재하기는 하는지**였다.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
