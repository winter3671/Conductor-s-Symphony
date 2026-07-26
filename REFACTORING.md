# 🔧 REFACTORING.md — 매니저 결합도 감소 리팩토링 기록

이 문서는 2026-07-26 진행된 "매니저 간 결합도 낮추기" 리팩토링의 배경, 변경 내용, 검증 결과를 기록합니다. 신규 기능 개발 이력은 `DOCUMENTATION.md`에, 순수 구조 개선(리팩토링) 이력은 이 문서에 분리해서 관리합니다.

---

## 1. 배경 (Why)

기능 개발이 누적되며 게임 로직/매니저 클래스 10개(`AudioLayerManager`, `BossMonster`, `EnemySpawner`, `LevelUpUI`, `InstrumentManager`, `RhythmAttackManager`, `PlayerController`, `PlayerExperience`, `RhythmManager`, `RhythmUI`)가 모두 `public static X Instance` 싱글톤을 갖게 되었고, 서로가 서로의 `Instance`를 직접 호출하는 사실상 완전 연결(fully-connected) 그래프 형태로 결합되어 있었습니다. 특히 게임 로직 매니저가 UI 매니저의 구체적인 표시 메서드를 직접 호출하는 역방향 의존(예: `BossMonster` → `RhythmUI.Instance.ShowBossHpBar(...)`)이 다수 발견되어, UI를 변경하려면 게임 로직 코드까지 건드려야 하는 구조였습니다.

## 2. 발견한 문제

### 문제 1 — 중복된 `FindAnyObjectByType` 호출
`PlayerController`, `EnemySpawner`는 이미 자체 `Instance` 싱글톤이 있음에도, 7개 파일에서 굳이 `FindAnyObjectByType<T>()`로 씬을 매번 재검색하고 있었습니다.

### 문제 2 — 게임로직 → UI 역방향 직접 호출
- `BossMonster` → `RhythmUI.Instance.ShowBossHpBar` / `UpdateBossHp` (3곳)
- `RhythmManager` → `RhythmUI.Instance.ShowHitRating` / `UpdateScoreAndCombo` (2곳)
- `PlayerExperience` → `LevelUpUI.Instance.ShowLevelUpSelection` (2곳)
- `EliteRewardChest` → `LevelUpUI.Instance.ShowEliteRewardSelection` (1곳)

이미 `RhythmManager.OnHitSuccessEvent`, `PlayerController.OnHealthChangedEvent`, `PlayerExperience.OnExpChangedEvent`, `InstrumentManager.OnInstrumentsChangedEvent` 등 C# 이벤트 구독 패턴이 부분적으로 쓰이고 있었기 때문에, 같은 패턴을 위 4곳에도 일관되게 적용하는 방향으로 결정했습니다.

### 발견된 부수 버그
`PlayerExperience.Start()`와 `LevelUpUI.Start()`가 각각 독립적으로 "보유 악기 0개면 시작 선택 팝업 표시" 조건을 중복 검사하고 있었습니다. 두 컴포넌트의 스크립트 실행 순서에 따라 동일 팝업이 이중으로 트리거될 수 있는 잠재적 위험이 있었습니다.

## 3. 적용한 변경

### A. `FindAnyObjectByType` → 기존 `.Instance` 치환 (동작 변경 없음, 기계적 수정)
`CameraController`, `EnemySpawner`, `InstrumentManager`, `RhythmAttackManager`, `ExpGem`, `RhythmManager`, `BossMonster` — 총 7개 파일.

### B. 이벤트 기반 디커플링 (게임로직 → UI 방향 전환)

| 매니저 | 추가된 이벤트 | 구독자 |
|---|---|---|
| `BossMonster` | `OnBossSpawnedEvent(int maxHp)`<br>`OnBossHpChangedEvent(int cur, int max)`<br>`OnBossDefeatedEvent()` | `RhythmUI` |
| `RhythmManager` | `OnScoreUpdatedEvent(int score, int combo, HitRating rating)` | `RhythmUI` |
| `PlayerExperience` | `OnLevelUpEvent(bool isGameStart)` | `LevelUpUI` |
| `EliteRewardChest` | `OnEliteChestCollectedEvent()` | `LevelUpUI` |

모두 클래스 정적(static) `event Action<...>` 필드로 선언하고, 구독 측(`RhythmUI`, `LevelUpUI`)의 `OnEnable`/`OnDisable`에서 구독/해제하도록 통일했습니다 (기존 `PlayerController.OnHealthChangedEvent` 등과 동일한 컨벤션).

부수적으로 `LevelUpUI.Start()`의 중복 조건 검사를 제거하고 `PlayerExperience.OnLevelUpEvent` 구독으로 대체하여, 위에서 발견한 이중 트리거 위험도 함께 해소했습니다.

### 의도적으로 손대지 않은 부분
`InstrumentManager.Instance.AcquiredInstruments`, `AudioLayerManager.Instance.PlayInstrumentKeySound(...)`, `PlayerExperience.Instance.CurrentLevel` 등 **데이터 조회 목적의 `Instance` 참조는 그대로 유지**했습니다. 이 프로젝트 규모에서 Service Locator 스타일로 상태를 읽는 것은 합리적인 절충이며, 전면 DI 컨테이너 도입은 과설계로 판단했습니다.

`BossMonster.Initialize()`와 `Start()`가 동일하게 `OnBossSpawnedEvent`를 두 번 호출하는 기존의 중복 호출 구조는 이번 범위(결합도 감소) 밖이라 손대지 않았고, 그대로 유지됩니다(무해한 중복이지만 향후 정리 후보로 남겨둠).

## 4. 변경된 파일 목록

```
Assets/Scripts/Camera/CameraController.cs
Assets/Scripts/Enemy/EnemySpawner.cs
Assets/Scripts/Enemy/BossMonster.cs
Assets/Scripts/Instrument/InstrumentManager.cs
Assets/Scripts/Combat/RhythmAttackManager.cs
Assets/Scripts/Player/ExpGem.cs
Assets/Scripts/Player/PlayerExperience.cs
Assets/Scripts/Rhythm/RhythmManager.cs
Assets/Scripts/Rhythm/RhythmUI.cs
Assets/Scripts/UI/LevelUpUI.cs
Assets/Scripts/Item/EliteRewardChest.cs
```

## 5. 검증

- Unity 스크립트 컴파일: 에러/경고 0건
- Play 모드 스모크 테스트: 게임 시작 시 `PlayerExperience.OnLevelUpEvent` → `LevelUpUI` 이벤트 체인이 정상 동작하여 `LEVEL UP!` 카드 선택 팝업이 정상 표시됨, SCORE/COMBO/HP UI 정상 렌더링, 콘솔 에러 없음
- 보스 관련 이벤트(`OnBossSpawnedEvent` 등)는 보스 스폰까지 2분 대기가 필요해 이번 세션에서는 실시간 플레이로 직접 검증하지 못했으며, 기존에 검증된 것과 동일한 구독/해제 패턴을 그대로 재사용했다는 점으로 리스크를 낮췄습니다. 다음 플레이테스트 시 보스전 진입 시 HP 바 표시/갱신을 육안 확인하는 것을 권장합니다.

---

## 6. Round 2 — 싱글톤 보일러플레이트 및 프로시저럴 스프라이트 생성 코드 중복 제거

Round 1에서 결합도(coupling)를 낮췄다면, Round 2는 같은 코드베이스에 남아있던 **반복(duplication)** 문제 두 가지를 해소하는 후속 작업입니다.

### 6.1 배경 (Why)

Round 1 조사 중, 매니저 10개가 각자 `public static X Instance { get; private set; }` 필드와 `Awake()`에서 "이미 인스턴스가 있으면 자신을 파괴, 없으면 등록" 로직을 토씨 하나 다르지 않게 반복 구현하고 있다는 점을 확인했습니다. 또한 아이템/투사체 계열 스크립트 5개가 픽셀 단위로 원/링/다이아몬드를 그려 `Sprite`를 만드는 30~40줄짜리 이중 for문을 각자 복사-붙여넣기한 상태였습니다(도형 종류와 색상 파라미터만 다름). 두 경우 모두 로직에 손댈 여지가 없을 만큼 기계적으로 동일해서, 공용 유틸리티로 추출하는 쪽이 유지보수 비용을 확실히 낮춘다고 판단했습니다.

### 6.2 적용한 변경

**A. `Assets/Scripts/Utility/MonoSingleton.cs` 신설**

```csharp
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance { get; private set; }
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = (T)this;
    }
}
```

다음 10개 매니저가 자체 `Instance`/`Awake` 중복 구현을 제거하고 이 베이스 클래스를 상속하도록 전환했습니다: `AudioLayerManager`, `BossMonster`, `EnemySpawner`, `LevelUpUI`, `InstrumentManager`, `RhythmAttackManager`, `PlayerController`, `PlayerExperience`, `RhythmManager`, `RhythmUI`. 각 클래스의 기존 `Awake()`는 `protected override void Awake()`로 바뀌었고, `base.Awake()` 호출 후 `if (Instance != this) return;`로 중복 인스턴스 시 초기화 로직(콜백 등록, 컴포넌트 캐싱 등)을 건너뛰도록 통일했습니다.

**B. `Assets/Scripts/Utility/ProceduralSpriteFactory.cs` 신설**

`CreateFilledCircle`, `CreateRingWithCore`, `CreateDiamond` 3개 정적 메서드로 도형별 픽셀 생성 로직을 일원화했습니다. 다음 9개 호출부가 자체 `Texture2D` 생성 for문을 지우고 팩토리 호출로 교체되었습니다.

| 파일 | 호출 |
|---|---|
| `RhythmAttackManager` | `CreateFilledCircle(20, 8f, Color.yellow)` |
| `RhythmManager` | `CreateFilledCircle(32, 14f, Color.cyan)` |
| `InstrumentManager` | `CreateFilledCircle(24, 10f, Color.white)` |
| `Player/ExpGem` | `CreateFilledCircle(16, 6f, Color.white)` |
| `Enemy/EnemySpawner` | `CreateDiamond(32, 12f, Color.magenta)` |
| `Enemy/BossMonster` | `CreateRingWithCore(48, 12f, 20f, gold, red)` |
| `Enemy/BossProjectile` | `CreateRingWithCore(24, 4f, 9f, red, yellow)` |
| `Instrument/InstrumentItem` | `CreateRingWithCore(24, 4f, 9f, white, white 80%)` |
| `Item/EliteRewardChest` | `CreateRingWithCore(28, 6f, 10f, gold, purple)` |

**C. 부수적으로 발견한 잔여 `FindAnyObjectByType` 정리**

Round 1에서 놓쳤던 `Player/ExpGem.Start()`의 `FindAnyObjectByType<PlayerController>()` 호출을 `PlayerController.Instance` 참조로 교체했습니다(Round 1의 "문제 1"과 동일 패턴).

### 6.3 의도적으로 손대지 않은 부분

각 매니저의 `Awake()` 이후 로직(이벤트 초기화, 필드 세팅 등)은 그대로 유지했습니다. `MonoSingleton<T>`는 인스턴스 생명주기 관리만 담당하고, 매니저별 초기화 내용에는 관여하지 않도록 범위를 한정했습니다. `ProceduralSpriteFactory`도 순수 함수형 유틸리티로만 남기고, 캐싱(동일 파라미터 호출 시 스프라이트 재사용)은 이번 범위 밖으로 남겨뒀습니다 — 현재 각 호출부가 정적 필드로 이미 자체 캐싱(`static Sprite ...Sprite`)을 하고 있어 실익이 적다고 판단했습니다.

### 6.4 변경된 파일 목록 (Round 2 신규/수정)

```
Assets/Scripts/Utility/MonoSingleton.cs                (신규)
Assets/Scripts/Utility/ProceduralSpriteFactory.cs      (신규)
Assets/Scripts/Audio/AudioLayerManager.cs
Assets/Scripts/Enemy/BossMonster.cs
Assets/Scripts/Enemy/BossProjectile.cs
Assets/Scripts/Enemy/EnemySpawner.cs
Assets/Scripts/UI/LevelUpUI.cs
Assets/Scripts/Instrument/InstrumentManager.cs
Assets/Scripts/Instrument/InstrumentItem.cs
Assets/Scripts/Combat/RhythmAttackManager.cs
Assets/Scripts/Player/PlayerController.cs
Assets/Scripts/Player/PlayerExperience.cs
Assets/Scripts/Player/ExpGem.cs
Assets/Scripts/Rhythm/RhythmManager.cs
Assets/Scripts/Rhythm/RhythmUI.cs
Assets/Scripts/Item/EliteRewardChest.cs
```

### 6.5 검증

- `refresh_unity`로 강제 재컴파일 요청 후 `editor_state.compilation` 확인: `is_compiling: false`, `is_domain_reload_pending: false` — 컴파일 에러 없음.
- 10개 매니저 전원이 `MonoSingleton<T>`를 상속하는지, 프로젝트 전역에 `new Texture2D`가 `ProceduralSpriteFactory` 내부(1곳) 외에 남아있지 않은지 정적 검색으로 확인 완료.
- **Play 모드 실시간 스모크 테스트는 이번 세션에서 완료하지 못했습니다.** Play 버튼 진입 후 에디터가 `playmode_transition` 상태에서 멈춰(에디터 창이 포커스를 갖지 못한 백그라운드 세션 환경 특성으로 추정) `is_playing`/`Instance` 필드가 정상적으로 채워지는지 런타임으로 직접 확인할 수 없었습니다. 콘솔에 나타난 "referenced script missing" 오류 4건은 씬(`Gameplay.unity`) 및 전체 `Assets/` 내 `m_Script: {fileID: 0}` 마커 검색 결과 0건으로, 실제 에셋 손상이 아닌 것으로 확인했습니다(원인 미상, 이번 변경과 무관한 것으로 판단됨). **다음 세션에서 에디터에 포커스가 있는 상태로 Play 모드 진입 → 각 매니저 `Instance` 정상 초기화, 절차적 스프라이트(투사체/아이템/보스/엘리트 상자) 정상 렌더링, 콘솔 에러 0건을 육안으로 재확인할 것을 권장합니다.**
