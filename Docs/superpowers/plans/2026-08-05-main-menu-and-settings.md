# 메인화면 & 설정창 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `MainMenu.unity` 씬(Start/Settings/Quit)과 설정창(볼륨 3채널, 키 리바인딩 8종, 리듬 싱크 자동 계측)을 신규 구축한다.

**Architecture:** 씬 배치가 필요 없는 정적 클래스 `GameSettings`(PlayerPrefs 기반)를 단일 설정 저장소로 두고, `RhythmManager`/`PlayerController`/`AudioLayerManager`가 각자 이를 직접 참조한다. UI는 프리팹 기반, `MainMenuCanvas` 하나에 `MainPanel`/`SettingsPanel`/`SyncCalibrationPanel`을 자식으로 두고 활성/비활성으로 전환한다.

**Tech Stack:** Unity 6 / URP, UnityEngine.InputSystem(직접 폴링 방식, Input Actions 에셋 미사용), UnityEngine.UI(레거시 `Text`/`Image`/`Button`, TextMeshPro 미사용 — 기존 `LevelUpUI.cs` 컨벤션과 통일).

## Global Constraints

- 볼륨/키 스킴/싱크 오프셋 기본값은 현재 하드코딩된 동작과 동일해야 한다(볼륨 배율 기본 1.0, 기본 키맵 QWER+방향키, 오프셋 0ms) — 기존 플레이어 체감이 바뀌면 안 됨.
- `RhythmManager.Bpm`은 `public const float Bpm = 97f;` 단일 소스로만 존재한다. 씬에 직렬화된 `bpm` 필드값(현재 `Gameplay.unity`에 `97`로 저장되어 있어 이 변경으로 동작이 바뀌지 않음, 확인 완료)은 제거한다.
- 이번 작업 범위에서 게임패드 리바인딩, 그래픽 옵션, 게임 중 일시정지 메뉴 접근은 다루지 않는다(스펙의 Out of Scope 참고).
- **git 작업**: (2026-08-05, 사용자 승인으로 변경, 2026-08-05 재조정) 이 계획을 Subagent-Driven Development로 실행하는 동안 로컬 커밋(푸시 제외)은 허용되지만, **구현 서브에이전트는 커밋하지 않는다** — 서브에이전트는 컨트롤러가 채팅으로 전달한 "사용자가 승인했다"는 말을 검증할 수 없으므로 CLAUDE.md의 git 제한을 그대로 따르는 게 맞고, 실제로 한 서브에이전트가 이를 근거로 커밋을 거부한 사례가 있었다(정당한 판단). 대신 **컨트롤러(사용자와 직접 대화하는 세션)가** 구현 서브에이전트의 보고와 diff를 확인한 뒤 직접 `git add`(해당 태스크 파일만)와 `git commit`을 수행한다. Unity 프로젝트이므로 `.cs` 파일과 함께 대응하는 `.meta` 파일도 반드시 같이 스테이징한다.
- 프로젝트에 유닛테스트 프레임워크(asmdef/Tests 폴더)가 구성되어 있지 않다. 각 태스크의 검증은 Unity Editor Play Mode + `mcp__unity-mcp__Unity_RunCommand`(에디터 내 C# 스크립트 실행)를 통한 실측으로 대체한다 — 정식 TDD(실패하는 테스트 먼저 작성)가 아니라 "구현 → 콘솔 에러 없음 확인 → RunCommand로 동작 실측"의 사이클을 따른다.

---

## Task 1: GameSettings 기반 구축

**Files:**
- Create: `Assets/Scripts/Settings/GameAction.cs`
- Create: `Assets/Scripts/Settings/GameSettings.cs`

**Interfaces:**
- Produces: `enum GameAction { HitLeft, HitUpLeft, HitUpRight, HitRight, MoveUp, MoveDown, MoveLeft, MoveRight }` (namespace `ConductorSymphony.Settings`)
- Produces: `static class GameSettings` (namespace `ConductorSymphony.Settings`) with `float BgmVolume01 { get; set; }`, `float SfxVolume01 { get; set; }`, `float InstrumentVolume01 { get; set; }`, `float RhythmSyncOffsetMs { get; set; }`, `float RhythmSyncOffsetSeconds { get; }`, `Key GetBinding(GameAction)`, `void SetBinding(GameAction, Key)`, `bool IsKeyBoundToOtherAction(Key, GameAction excluding)`.

- [ ] **Step 1: `GameAction.cs` 작성**

```csharp
namespace ConductorSymphony.Settings
{
    public enum GameAction
    {
        HitLeft,
        HitUpLeft,
        HitUpRight,
        HitRight,
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight
    }
}
```

- [ ] **Step 2: `GameSettings.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConductorSymphony.Settings
{
    public static class GameSettings
    {
        private const string BgmVolumeKey = "Settings.BgmVolume01";
        private const string SfxVolumeKey = "Settings.SfxVolume01";
        private const string InstrumentVolumeKey = "Settings.InstrumentVolume01";
        private const string SyncOffsetKey = "Settings.RhythmSyncOffsetMs";
        private const string BindingKeyPrefix = "Settings.Binding.";

        private static readonly Dictionary<GameAction, Key> DefaultBindings = new Dictionary<GameAction, Key>
        {
            { GameAction.HitLeft, Key.Q },
            { GameAction.HitUpLeft, Key.W },
            { GameAction.HitUpRight, Key.E },
            { GameAction.HitRight, Key.R },
            { GameAction.MoveUp, Key.UpArrow },
            { GameAction.MoveDown, Key.DownArrow },
            { GameAction.MoveLeft, Key.LeftArrow },
            { GameAction.MoveRight, Key.RightArrow },
        };

        public static float BgmVolume01
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume01
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static float InstrumentVolume01
        {
            get => PlayerPrefs.GetFloat(InstrumentVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(InstrumentVolumeKey, Mathf.Clamp01(value));
        }

        // Positive = 입력을 SongTime 기준보다 "늦게" 한 것으로 보정(늦게 누르는 성향 보정), 음수 = 반대.
        public static float RhythmSyncOffsetMs
        {
            get => PlayerPrefs.GetFloat(SyncOffsetKey, 0f);
            set => PlayerPrefs.SetFloat(SyncOffsetKey, value);
        }

        public static float RhythmSyncOffsetSeconds => RhythmSyncOffsetMs / 1000f;

        public static Key GetBinding(GameAction action)
        {
            string raw = PlayerPrefs.GetString(BindingKeyPrefix + action, string.Empty);
            if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, out Key parsed))
            {
                return parsed;
            }
            return DefaultBindings[action];
        }

        public static void SetBinding(GameAction action, Key key)
        {
            PlayerPrefs.SetString(BindingKeyPrefix + action, key.ToString());
        }

        public static bool IsKeyBoundToOtherAction(Key key, GameAction excluding)
        {
            foreach (GameAction action in DefaultBindings.Keys)
            {
                if (action != excluding && GetBinding(action) == key)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

`mcp__unity-mcp__Unity_GetConsoleLogs`(logTypes: "Error")로 컴파일 에러 없음을 확인한다.

- [ ] **Step 4: RunCommand로 동작 실측**

`mcp__unity-mcp__Unity_RunCommand`로 아래를 실행해 기본값·저장·리셋 동작을 확인한다(사람이 읽을 로그로 결과 남기기):

```csharp
using UnityEngine;
using ConductorSymphony.Settings;
using UnityEngine.InputSystem;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PlayerPrefs.DeleteAll();

        result.Log("Default BgmVolume01 (expect 1): {0}", GameSettings.BgmVolume01);
        result.Log("Default HitLeft binding (expect Q): {0}", GameSettings.GetBinding(GameAction.HitLeft));

        GameSettings.BgmVolume01 = 0.3f;
        GameSettings.SetBinding(GameAction.HitLeft, Key.A);
        result.Log("After set BgmVolume01 (expect 0.3): {0}", GameSettings.BgmVolume01);
        result.Log("After rebind HitLeft (expect A): {0}", GameSettings.GetBinding(GameAction.HitLeft));
        result.Log("A already bound excluding HitLeft (expect False): {0}", GameSettings.IsKeyBoundToOtherAction(Key.A, GameAction.HitLeft));
        result.Log("A already bound excluding HitUpLeft (expect True): {0}", GameSettings.IsKeyBoundToOtherAction(Key.A, GameAction.HitUpLeft));

        PlayerPrefs.DeleteAll();
    }
}
```

로그가 전부 "expect" 값과 일치하는지 확인한다. 마지막에 `PlayerPrefs.DeleteAll()`로 정리했는지 확인(다음 태스크 검증에 영향 없도록).

- [ ] **Step 5: 사용자에게 보고**

`Assets/Scripts/Settings/GameAction.cs`, `Assets/Scripts/Settings/GameSettings.cs` 두 파일을 새로 만들었다고 보고하고 스테이징/커밋은 사용자에게 맡긴다.

---

## Task 2: RhythmManager / PlayerController에 GameSettings 연결

**Files:**
- Modify: `Assets/Scripts/Rhythm/RhythmManager.cs`
- Modify: `Assets/Scripts/Player/PlayerController.cs`

**Interfaces:**
- Consumes: `GameSettings.GetBinding(GameAction)`, `GameSettings.RhythmSyncOffsetSeconds` (Task 1)
- Produces: `public const float Bpm = 97f;` on `RhythmManager` (Task 7의 `SyncCalibrationController`가 참조)

- [ ] **Step 1: `RhythmManager.cs` 상단에 `using ConductorSymphony.Settings;` 추가**

- [ ] **Step 2: `bpm` 필드를 `Bpm` const로 교체**

`Assets/Scripts/Rhythm/RhythmManager.cs:19-22`의

```csharp
[Header("Rhythm Sequencer Settings")]
[SerializeField] private float bpm = 97f;
[SerializeField] private float spawnDistance = 4.0f;
[SerializeField] private float noteTravelDuration = 2.474f; // Exactly 4 beats (1 bar) at 97 BPM for smooth readable travel speed
```

를 다음으로 교체:

```csharp
// Single source of truth for the game's fixed tempo — SyncCalibrationController references
// this directly so the calibration metronome can never drift from actual gameplay BPM.
public const float Bpm = 97f;

[Header("Rhythm Sequencer Settings")]
[SerializeField] private float spawnDistance = 4.0f;
[SerializeField] private float noteTravelDuration = 2.474f; // Exactly 4 beats (1 bar) at 97 BPM for smooth readable travel speed
```

`Awake()`의 `stepDuration = (60f / bpm) / 2f;`를 `stepDuration = (60f / Bpm) / 2f;`로 변경.

- [ ] **Step 3: QWER 입력 검사를 GameSettings 바인딩 조회로 교체**

`RhythmManager.cs:111-119`의

```csharp
var keyboard = Keyboard.current;
if (keyboard != null)
{
    if (keyboard.qKey.wasPressedThisFrame) CheckHit(RhythmLane.Left);    // Slot 0 (Q = Left)
    if (keyboard.wKey.wasPressedThisFrame) CheckHit(RhythmLane.UpLeft);  // Slot 1 (W = Upper-Left)
    if (keyboard.eKey.wasPressedThisFrame) CheckHit(RhythmLane.UpRight); // Slot 2 (E = Upper-Right)
    if (keyboard.rKey.wasPressedThisFrame) CheckHit(RhythmLane.Right);   // Slot 3 (R = Right)
}
```

를 다음으로 교체:

```csharp
var keyboard = Keyboard.current;
if (keyboard != null)
{
    if (keyboard[GameSettings.GetBinding(GameAction.HitLeft)].wasPressedThisFrame) CheckHit(RhythmLane.Left);
    if (keyboard[GameSettings.GetBinding(GameAction.HitUpLeft)].wasPressedThisFrame) CheckHit(RhythmLane.UpLeft);
    if (keyboard[GameSettings.GetBinding(GameAction.HitUpRight)].wasPressedThisFrame) CheckHit(RhythmLane.UpRight);
    if (keyboard[GameSettings.GetBinding(GameAction.HitRight)].wasPressedThisFrame) CheckHit(RhythmLane.Right);
}
```

- [ ] **Step 4: `IsLaneKeyPressed`(홀드 유지 검사)도 동일하게 교체**

`RhythmManager.cs:156-167`:

```csharp
private bool IsLaneKeyPressed(Keyboard keyboard, RhythmLane lane)
{
    if (keyboard == null) return false;
    switch (lane)
    {
        case RhythmLane.Left:    return keyboard[GameSettings.GetBinding(GameAction.HitLeft)].isPressed;
        case RhythmLane.UpLeft:  return keyboard[GameSettings.GetBinding(GameAction.HitUpLeft)].isPressed;
        case RhythmLane.UpRight: return keyboard[GameSettings.GetBinding(GameAction.HitUpRight)].isPressed;
        case RhythmLane.Right:   return keyboard[GameSettings.GetBinding(GameAction.HitRight)].isPressed;
        default: return false;
    }
}
```

- [ ] **Step 5: `CheckHit`의 판정 시각에 싱크 오프셋 반영**

`RhythmManager.cs:283`의

```csharp
float currentTime = Audio.AudioLayerManager.Instance != null ? Audio.AudioLayerManager.Instance.SongTime : 0f;
```

를 다음으로 교체:

```csharp
float currentTime = Audio.AudioLayerManager.Instance != null
    ? Audio.AudioLayerManager.Instance.SongTime + GameSettings.RhythmSyncOffsetSeconds
    : 0f;
```

- [ ] **Step 6: `PlayerController.cs` 방향키 검사 교체**

상단에 `using ConductorSymphony.Settings;` 추가 후, `PlayerController.cs:211-218`의

```csharp
var keyboard = Keyboard.current;
if (keyboard != null)
{
    if (keyboard.rightArrowKey.isPressed) moveX += 1f;
    if (keyboard.leftArrowKey.isPressed) moveX -= 1f;
    if (keyboard.upArrowKey.isPressed) moveY += 1f;
    if (keyboard.downArrowKey.isPressed) moveY -= 1f;
}
```

를 다음으로 교체:

```csharp
var keyboard = Keyboard.current;
if (keyboard != null)
{
    if (keyboard[GameSettings.GetBinding(GameAction.MoveRight)].isPressed) moveX += 1f;
    if (keyboard[GameSettings.GetBinding(GameAction.MoveLeft)].isPressed) moveX -= 1f;
    if (keyboard[GameSettings.GetBinding(GameAction.MoveUp)].isPressed) moveY += 1f;
    if (keyboard[GameSettings.GetBinding(GameAction.MoveDown)].isPressed) moveY -= 1f;
}
```

- [ ] **Step 7: 컴파일 확인**

`Unity_GetConsoleLogs`(logTypes: "Error")로 에러 없음 확인.

- [ ] **Step 8: Play Mode 실측**

Play Mode 진입 후 `Unity_RunCommand`로 `RhythmManager.Bpm`이 97인지, 기본 바인딩으로 기존과 동일하게 Q/W/E/R·방향키가 동작하는지(리바인딩 없이 기본값이므로 기존 플레이와 체감 차이 없어야 함) 확인. `Unity_GetConsoleLogs`로 NullReferenceException 등 런타임 에러 없는지 확인.

- [ ] **Step 9: 사용자에게 보고**

`RhythmManager.cs`, `PlayerController.cs` 수정 내역 보고.

---

## Task 3: AudioLayerManager에 볼륨 배율 연결

**Files:**
- Modify: `Assets/Scripts/Audio/AudioLayerManager.cs`

**Interfaces:**
- Consumes: `GameSettings.BgmVolume01`, `GameSettings.SfxVolume01`, `GameSettings.InstrumentVolume01` (Task 1)

- [ ] **Step 1: 상단에 `using ConductorSymphony.Settings;` 추가**

- [ ] **Step 2: 타격음(SFX) 볼륨 반영**

`AudioLayerManager.cs:185-190`의

```csharp
if (sfxSource != null)
{
    sfxSource.pitch = isPerfect ? 1.05f : 1.0f;
    sfxSource.PlayOneShot(clip, 1.0f);
}
```

를 다음으로 교체:

```csharp
if (sfxSource != null)
{
    sfxSource.pitch = isPerfect ? 1.05f : 1.0f;
    sfxSource.PlayOneShot(clip, GameSettings.SfxVolume01);
}
```

- [ ] **Step 3: 악기 트랙(습득음) 볼륨 반영**

`AudioLayerManager.cs:211`의 `source.volume = 0.85f;`를 `source.volume = 0.85f * GameSettings.InstrumentVolume01;`로 교체.

- [ ] **Step 4: 보스전 BGM 볼륨 반영**

`AudioLayerManager.cs:372`의 `bgmSource.volume = 0.75f;`를 `bgmSource.volume = 0.75f * GameSettings.BgmVolume01;`로 교체.

- [ ] **Step 5: 컴파일 확인**

`Unity_GetConsoleLogs`(logTypes: "Error").

- [ ] **Step 6: Play Mode 실측 (리플렉션으로 실제 적용값 확인)**

Play Mode 진입 후 `Unity_RunCommand`로 아래를 실행해 배율이 실제로 곱해지는지 확인:

```csharp
using UnityEngine;
using ConductorSymphony.Audio;
using ConductorSymphony.Settings;
using ConductorSymphony.Instrument;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        GameSettings.BgmVolume01 = 0.4f;
        AudioLayerManager.Instance.PlayBossBattleBGM();

        FieldInfo field = typeof(AudioLayerManager).GetField("bgmSource", BindingFlags.NonPublic | BindingFlags.Instance);
        AudioSource bgmSource = (AudioSource)field.GetValue(AudioLayerManager.Instance);
        result.Log("bgmSource.volume after BgmVolume01=0.4 (expect 0.3 = 0.75*0.4): {0}", bgmSource.volume);

        GameSettings.BgmVolume01 = 1f;
    }
}
```

로그값이 `0.75 * 0.4 = 0.3`과 일치하는지 확인한다. SFX는 `PlayOneShot`의 휘발성 파라미터라 사후 검사가 불가능하므로 Step 2 코드 리뷰로 대체하고, 필요시 사람이 직접 헤드폰으로 들어보는 것을 권장 노트로 남긴다.

- [ ] **Step 7: 사용자에게 보고**

---

## Task 4: MainMenu 씬 뼈대 (MainPanel + MainMenuController)

**Files:**
- Create: `Assets/Scripts/UI/MainMenu/MainMenuController.cs`
- Create: `Assets/Scenes/MainMenu.unity`
- Create: `Assets/Prefabs/UI/MainMenuCanvas.prefab`

**Interfaces:**
- Produces: `MainMenuController.ShowMainPanel()` (public, Task 5의 뒤로가기 버튼이 호출)

**계층 스펙 (MainMenuCanvas prefab root):**

| GameObject | Components | 비고 |
|---|---|---|
| `MainMenuCanvas` (root) | `Canvas`(Screen Space - Overlay), `CanvasScaler`(Scale With Screen Size, 1920x1080, Match 0.5), `GraphicRaycaster`, `MainMenuController` | |
| `MainMenuCanvas/MainPanel` | `RectTransform`(전체 채움), `Image`(배경) | 자식: `Title`(Text, "지휘자의 교향곡"), `StartButton`(Button+Text "시작"), `SettingsButton`(Button+Text "설정"), `QuitButton`(Button+Text "종료"), 세로 `VerticalLayoutGroup`으로 정렬 |
| `MainMenuCanvas/SettingsPanel` | `RectTransform`(전체 채움), `Image`(배경) | 초기 비활성. Task 5~7에서 내부 채움 |
| `MainMenuCanvas/EventSystem` (씬 루트, 프리팹 밖) | `EventSystem`, `StandaloneInputModule` | 씬에 하나만 존재하면 됨 |

- [ ] **Step 1: `MainMenuController.cs` 작성**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConductorSymphony.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            ShowMainPanel();
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene("Gameplay");
        }

        private void OnSettingsClicked()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인** (`Unity_GetConsoleLogs`, logTypes: "Error")

- [ ] **Step 3: `Unity_RunCommand`로 씬·계층·프리팹 생성**

위 계층 스펙대로 새 빈 씬을 만들고(`EditorSceneManager.NewScene`), `EventSystem`, `MainMenuCanvas`(Canvas/CanvasScaler/GraphicRaycaster/MainMenuController)와 `MainPanel`(Title/Start/Settings/Quit 버튼, `VerticalLayoutGroup`), 빈 `SettingsPanel`(비활성)을 구성한다. `MainMenuController`의 직렬화 필드(`mainPanel`, `settingsPanel`, `startButton`, `settingsButton`, `quitButton`)를 인스펙터 필드에 연결한다. `MainMenuCanvas`를 `Assets/Prefabs/UI/MainMenuCanvas.prefab`으로 저장하고, 씬을 `Assets/Scenes/MainMenu.unity`로 저장한다. (`result.RegisterObjectCreation`으로 생성 오브젝트 등록, `PrefabUtility.SaveAsPrefabAsset`, `EditorSceneManager.SaveScene` 사용.)

- [ ] **Step 4: 콘솔 에러 확인 + 스크린샷 검증**

`Unity_GetConsoleLogs`(logTypes: "Error")로 에러 없음 확인 후, `Unity_Camera_Capture` 또는 `Unity_SceneView_Capture2DScene`으로 `MainPanel`이 3개 버튼과 함께 정상적으로 배치되어 보이는지 스크린샷으로 확인한다.

- [ ] **Step 5: Play Mode에서 Start/Quit 동작 확인**

Play Mode 진입 → Start 버튼 클릭 시 `Gameplay` 씬으로 전환되는지, 다시 `MainMenu`로 돌아와 Quit 클릭 시 Play Mode가 종료되는지 확인. (씬 전환 전 `EditorBuildSettings`에 두 씬이 등록되어 있어야 하므로 Task 8과 함께 최종 확인해도 됨 — 이 시점엔 `EditorSceneManager.LoadScene`으로 대체 확인 가능.)

- [ ] **Step 6: 사용자에게 보고**

---

## Task 5: 설정 패널 — 볼륨

**Files:**
- Modify: `Assets/Prefabs/UI/MainMenuCanvas.prefab` (`SettingsPanel` 내부 채움)
- Create: `Assets/Scripts/UI/MainMenu/SettingsPanelController.cs`

**Interfaces:**
- Consumes: `MainMenuController.ShowMainPanel()` (Task 4)
- Produces: `SettingsPanelController.RefreshSyncLabel()` (public, Task 7이 호출)

**계층 스펙 (`SettingsPanel` 내부):**

| GameObject | Components |
|---|---|
| `SettingsPanel/VolumeSection` | `Text`("볼륨") 헤더 + `BgmVolumeSlider`(Slider, 0~1), `SfxVolumeSlider`(Slider, 0~1), `InstrumentVolumeSlider`(Slider, 0~1), 각 슬라이더 옆 라벨 Text |
| `SettingsPanel/BackButton` | `Button`+`Text`("뒤로") |
| `SettingsPanel` (컴포넌트 추가) | `SettingsPanelController` |

- [ ] **Step 1: `SettingsPanelController.cs` 작성 (볼륨 부분만)**

```csharp
using UnityEngine;
using UnityEngine.UI;
using ConductorSymphony.Settings;

namespace ConductorSymphony.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider instrumentVolumeSlider;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            bgmVolumeSlider.onValueChanged.AddListener(v => GameSettings.BgmVolume01 = v);
            sfxVolumeSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume01 = v);
            instrumentVolumeSlider.onValueChanged.AddListener(v => GameSettings.InstrumentVolume01 = v);
            backButton.onClick.AddListener(() => mainMenuController.ShowMainPanel());
        }

        private void OnEnable()
        {
            bgmVolumeSlider.SetValueWithoutNotify(GameSettings.BgmVolume01);
            sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume01);
            instrumentVolumeSlider.SetValueWithoutNotify(GameSettings.InstrumentVolume01);
        }
    }
}
```

(`RefreshSyncLabel()`과 관련 필드는 Task 7에서 이 파일에 추가한다.)

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: `Unity_RunCommand`로 `SettingsPanel` 내부 계층 구성**

위 표대로 `VolumeSection`(3개 슬라이더+라벨)과 `BackButton`을 생성하고 `SettingsPanelController` 필드를 연결한 뒤 `MainMenuCanvas.prefab`을 덮어써 저장한다.

- [ ] **Step 4: 콘솔 에러 확인 + 스크린샷**

- [ ] **Step 5: Play Mode 실측**

Settings 버튼 클릭 → 슬라이더 3개가 보이는지, 슬라이더 조작 시 `Unity_RunCommand`로 `GameSettings.BgmVolume01` 등의 값이 실제로 바뀌는지 확인. 뒤로가기 클릭 시 `MainPanel`로 복귀하는지 확인.

- [ ] **Step 6: 사용자에게 보고**

---

## Task 6: 설정 패널 — 키 리바인딩

**Files:**
- Modify: `Assets/Prefabs/UI/MainMenuCanvas.prefab` (`SettingsPanel`에 `KeyBindSection` 추가)
- Create: `Assets/Scripts/UI/MainMenu/KeyRebindRow.cs`

**Interfaces:**
- Consumes: `GameSettings.GetBinding`/`SetBinding`/`IsKeyBoundToOtherAction` (Task 1)

**계층 스펙:** `SettingsPanel/KeyBindSection` 하위에 8개 행(`HitLeftRow` ~ `MoveRightRow`), 각 행은 `ActionLabel`(Text, 예: "Q 레인(왼쪽)"), `KeyLabel`(Text, 현재 키), `RebindButton`(Button, "변경"), `WarningLabel`(Text, 평소 비어있음). 각 행 루트에 `KeyRebindRow` 컴포넌트 부착, `action` 필드를 해당 `GameAction` 값으로 설정.

- [ ] **Step 1: `KeyRebindRow.cs` 작성**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ConductorSymphony.Settings;

namespace ConductorSymphony.UI
{
    public class KeyRebindRow : MonoBehaviour
    {
        [SerializeField] private GameAction action;
        [SerializeField] private Text keyLabel;
        [SerializeField] private Button rebindButton;
        [SerializeField] private Text warningLabel;

        private bool waitingForKey;

        private void Awake()
        {
            rebindButton.onClick.AddListener(BeginRebind);
            if (warningLabel != null) warningLabel.text = string.Empty;
            RefreshLabel();
        }

        private void BeginRebind()
        {
            waitingForKey = true;
            keyLabel.text = "키 입력...";
            if (warningLabel != null) warningLabel.text = string.Empty;
        }

        private void Update()
        {
            if (!waitingForKey) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control.wasPressedThisFrame)
                {
                    TryAssign(control.keyCode);
                    break;
                }
            }
        }

        private void TryAssign(Key key)
        {
            waitingForKey = false;

            if (key == Key.Escape)
            {
                RefreshLabel();
                return;
            }

            if (GameSettings.IsKeyBoundToOtherAction(key, action))
            {
                if (warningLabel != null) warningLabel.text = "이미 사용 중인 키입니다";
                RefreshLabel();
                return;
            }

            GameSettings.SetBinding(action, key);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            keyLabel.text = GameSettings.GetBinding(action).ToString();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: `Unity_RunCommand`로 8개 행 생성 및 필드 연결**, `MainMenuCanvas.prefab` 갱신 저장.

- [ ] **Step 4: 콘솔 에러 확인 + 스크린샷**

- [ ] **Step 5: Play Mode 실측**

`Unity_RunCommand`로 특정 행의 `RebindButton.onClick.Invoke()`를 호출해 대기 상태로 만든 뒤, 테스트 입력을 시뮬레이션하기 어렵다면 대신 `GameSettings.SetBinding`/`GetBinding`/`IsKeyBoundToOtherAction`을 직접 호출해 Task 1에서 이미 검증한 로직과 UI 라벨(`keyLabel.text`)이 일치하는지 확인하는 것으로 대체 가능. 실제 키 입력 캡처는 사람이 에디터에서 직접 클릭 후 키를 눌러 확인하도록 안내 노트를 남긴다.

- [ ] **Step 6: 사용자에게 보고**

---

## Task 7: 설정 패널 — 리듬 싱크 자동 계측

**Files:**
- Modify: `Assets/Prefabs/UI/MainMenuCanvas.prefab` (`SettingsPanel`에 싱크 표시/버튼 추가, `SyncCalibrationPanel` 신규 추가)
- Modify: `Assets/Scripts/UI/MainMenu/SettingsPanelController.cs` (싱크 표시 관련 필드/메서드 추가)
- Create: `Assets/Scripts/UI/MainMenu/SyncCalibrationController.cs`

**Interfaces:**
- Consumes: `RhythmManager.Bpm`(Task 2), `GameSettings.GetBinding(GameAction.HitLeft)`, `GameSettings.RhythmSyncOffsetMs`(Task 1), `SettingsPanelController.RefreshSyncLabel()`(Task 5에서 시그니처 예약)

**계층 스펙:**
- `SettingsPanel/SyncSection`: `SyncOffsetLabel`(Text, "현재 오프셋: 0ms"), `SyncCalibrationButton`(Button, "싱크 측정")
- `MainMenuCanvas/SyncCalibrationPanel` (SettingsPanel과 형제, 초기 비활성, 불투명 배경으로 전체 덮음): `InstructionLabel`(Text), `BeatPulseIcon`(Image, RectTransform 스케일 애니메이션 대상), `ResultLabel`(Text), `ApplyButton`(Button, 초기 비활성), `RetryButton`(Button, 초기 비활성), `CloseButton`(Button). 루트에 `SyncCalibrationController` 부착.

- [ ] **Step 1: `SettingsPanelController.cs`에 싱크 표시 필드/메서드 추가**

기존 필드 목록에 아래 두 줄 추가:

```csharp
[SerializeField] private Text syncOffsetLabel;
[SerializeField] private Button syncCalibrationButton;
[SerializeField] private GameObject syncCalibrationPanel;
```

`Awake()`에 추가:

```csharp
syncCalibrationButton.onClick.AddListener(() => syncCalibrationPanel.SetActive(true));
```

`OnEnable()`에 추가:

```csharp
RefreshSyncLabel();
```

새 public 메서드 추가:

```csharp
public void RefreshSyncLabel()
{
    float ms = GameSettings.RhythmSyncOffsetMs;
    syncOffsetLabel.text = $"현재 오프셋: {ms:+0;-0;0}ms";
}
```

- [ ] **Step 2: `SyncCalibrationController.cs` 작성**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ConductorSymphony.Settings;
using ConductorSymphony.Rhythm;

namespace ConductorSymphony.UI
{
    public class SyncCalibrationController : MonoBehaviour
    {
        private const int LeadInBeats = 2;
        private const int MeasuredBeats = 6;
        private const int MinValidSamples = 4;
        private const float CaptureWindowSeconds = 0.3f;
        private const float CountdownSeconds = 1.0f;

        [SerializeField] private SettingsPanelController settingsPanelController;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform beatPulseIcon;

        private AudioSource tickSource;
        private double[] beatTargetDspTimes;
        private bool[] beatCaptured;
        private readonly List<double> offsetSamplesSeconds = new List<double>();
        private bool measuring;
        private float pendingOffsetMs;

        private void Awake()
        {
            tickSource = gameObject.AddComponent<AudioSource>();
            tickSource.playOnAwake = false;
            tickSource.clip = CreateTickClip();

            applyButton.onClick.AddListener(ApplyResult);
            retryButton.onClick.AddListener(StartMeasurement);
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            StartMeasurement();
        }

        private AudioClip CreateTickClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-40f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1000f * t) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("CalibrationTick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void StartMeasurement()
        {
            measuring = true;
            offsetSamplesSeconds.Clear();
            resultLabel.text = string.Empty;
            applyButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            instructionLabel.text = "박자에 맞춰 준비하세요...";

            int totalBeats = LeadInBeats + MeasuredBeats;
            beatTargetDspTimes = new double[totalBeats];
            beatCaptured = new bool[totalBeats];

            double beatInterval = 60.0 / RhythmManager.Bpm;
            double startDsp = AudioSettings.dspTime + CountdownSeconds;

            for (int i = 0; i < totalBeats; i++)
            {
                beatTargetDspTimes[i] = startDsp + i * beatInterval;
                tickSource.PlayScheduled(beatTargetDspTimes[i]);
            }
        }

        private void Update()
        {
            if (!measuring) return;

            UpdateBeatPulse();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Key hitKey = GameSettings.GetBinding(GameAction.HitLeft);
                if (keyboard[hitKey].wasPressedThisFrame)
                {
                    CapturePress(AudioSettings.dspTime);
                }
            }

            if (AudioSettings.dspTime > beatTargetDspTimes[beatTargetDspTimes.Length - 1] + CaptureWindowSeconds)
            {
                FinishMeasurement();
            }
        }

        private void UpdateBeatPulse()
        {
            if (beatPulseIcon == null) return;
            double now = AudioSettings.dspTime;
            double nearestDiff = double.MaxValue;
            foreach (double target in beatTargetDspTimes)
            {
                double diff = System.Math.Abs(target - now);
                if (diff < nearestDiff) nearestDiff = diff;
            }
            float scale = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((float)(nearestDiff / 0.15)));
            beatPulseIcon.localScale = Vector3.one * scale;
        }

        private void CapturePress(double pressDspTime)
        {
            for (int i = LeadInBeats; i < beatTargetDspTimes.Length; i++)
            {
                if (beatCaptured[i]) continue;
                double diff = pressDspTime - beatTargetDspTimes[i];
                if (System.Math.Abs(diff) <= CaptureWindowSeconds)
                {
                    beatCaptured[i] = true;
                    offsetSamplesSeconds.Add(diff);
                    return;
                }
            }
        }

        private void FinishMeasurement()
        {
            measuring = false;
            instructionLabel.text = string.Empty;

            if (offsetSamplesSeconds.Count < MinValidSamples)
            {
                resultLabel.text = "박자를 다시 맞춰보세요";
                retryButton.gameObject.SetActive(true);
                return;
            }

            double sum = 0;
            foreach (double s in offsetSamplesSeconds) sum += s;
            double averageLagSeconds = sum / offsetSamplesSeconds.Count;

            // averageLagSeconds > 0 이면 플레이어가 박자보다 "늦게" 눌렀다는 뜻 —
            // 이후 판정 시 SongTime을 그만큼 앞당겨(음수 오프셋) 보정해야 늦은 입력이 "정타"로 읽힌다.
            pendingOffsetMs = -(float)(averageLagSeconds * 1000.0);

            resultLabel.text = $"측정된 오프셋: {pendingOffsetMs:+0;-0;0}ms";
            applyButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
        }

        private void ApplyResult()
        {
            GameSettings.RhythmSyncOffsetMs = pendingOffsetMs;
            settingsPanelController.RefreshSyncLabel();
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

- [ ] **Step 4: `Unity_RunCommand`로 `SyncSection`/`SyncCalibrationPanel` 계층 구성 및 필드 연결**, `MainMenuCanvas.prefab` 갱신 저장.

- [ ] **Step 5: 콘솔 에러 확인 + 스크린샷**

- [ ] **Step 6: Play Mode 실측 — 부호 검증**

Play Mode에서 "싱크 측정" 클릭 후, 의도적으로 각 박자보다 눈에 띄게 늦게(예: +150ms 근처) 입력해 측정을 완료한다. `resultLabel.text`(또는 `Unity_RunCommand`로 `pendingOffsetMs`를 읽어)가 **음수**로 나오는지 확인한다(늦게 눌렀으므로 보정값은 음수여야 함 — Step 2의 부호 설명 참고). "적용" 클릭 후 `GameSettings.RhythmSyncOffsetMs`에 해당 값이 저장되고 `SettingsPanel`의 `현재 오프셋` 라벨이 갱신되는지 확인. 유효 샘플 4개 미만(예: 아예 입력하지 않음)일 때 "박자를 다시 맞춰보세요"가 뜨고 `GameSettings` 값이 바뀌지 않는지도 확인.

- [ ] **Step 7: 사용자에게 보고**

---

## Task 8: 빌드 씬 등록 및 종단 검증

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset` (Unity Editor의 Build Settings 창 통해 변경, 직접 텍스트 편집 금지)

- [ ] **Step 1: `Unity_RunCommand`로 Build Settings에 씬 등록**

```csharp
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity", true),
        };
        result.Log("Build scenes registered: MainMenu (0), Gameplay (1)");
    }
}
```

- [ ] **Step 2: 종단 Play Mode 검증 (스펙의 Testing/Verification 항목 전체)**

`MainMenu` 씬에서 Play 진입 후 아래를 순서대로 실측하고 각각의 결과를 `Unity_GetConsoleLogs`/스크린샷으로 근거를 남긴다:

1. Start → `Gameplay` 진입 확인, 되돌아와서 Quit → Play Mode 종료 확인
2. 볼륨 슬라이더 3종 조작 후 `Gameplay` 진입 시 `Unity_RunCommand` 리플렉션으로 실제 `AudioSource.volume`에 반영되는지 확인 (Task 3 Step 6과 동일한 방식)
3. 키 리바인딩 후 `Gameplay`에서 변경된 키로 QWER 판정이 정상 동작하는지, 충돌 키 입력 시 거부되는지 확인
4. 싱크 측정 실행 → 오프셋 부호 검증(Task 7 Step 6) → `Gameplay` 진입 후 `RhythmManager.CheckHit()`에 반영되는지 확인
5. Play Mode 재시작 후 PlayerPrefs에 저장된 설정(볼륨/키/오프셋)이 유지되는지 확인

- [ ] **Step 3: 사용자에게 최종 보고**

변경된 전체 파일 목록(신규 8개 스크립트, 신규 씬, 신규 프리팹, 수정 3개 스크립트, `EditorBuildSettings.asset`)을 정리해 보고하고 스테이징/커밋은 사용자에게 맡긴다.

---

## Self-Review 체크리스트 (참고용, 실행 시 재확인)

- **스펙 커버리지:** 볼륨(Task 3,5) / 키 리바인딩(Task 2,6) / 싱크 자동 계측(Task 2,7) / 씬 전환(Task 4,8) / 저장(Task 1, 전 항목) 모두 태스크로 커버됨.
- **타입 일관성:** `GameSettings.GetBinding(GameAction)` 반환형 `Key`, `RhythmSyncOffsetSeconds`(초 단위)와 `RhythmSyncOffsetMs`(ms 단위) 네이밍을 전 태스크에서 동일하게 사용.
- **플레이스홀더 없음:** 전 태스크에 실제 코드/정확한 파일·라인 위치 명시됨.
