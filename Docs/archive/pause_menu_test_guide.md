# ESC 일시정지 메뉴 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 만들었나

Gameplay 씬에서 ESC 키를 누르면 게임이 일시정지되면서 **계속하기 / 환경설정 / 메인으로 /
게임종료** 4개 버튼이 있는 메뉴가 뜨는 기능입니다. 씬/프리팹 편집 없이 `RhythmUI.cs`(기존
`Ensure*Elements()` 코드 생성 패턴)에 전부 추가했습니다.

- **버튼 스타일**: `MainMenuCanvas.prefab`의 StartButton 등이 커스텀 스프라이트 없이 연회색
  `Image.color(0.85,0.85,0.85,1)` + `Button` ColorTint(Highlighted 0.96/Pressed 0.784/
  Selected 0.96/Disabled 0.784,0.502) 조합이라, 이 값을 그대로 코드로 재현했습니다
  (`CreateMenuStyleButton()`).
- **ESC 토글 (`RhythmUI.HandleEscapeInput()`, `Update()`에서 매 프레임 호출)**: 새 Input
  System(`Keyboard.current.escapeKey.wasPressedThisFrame`)을 사용. `Time.timeScale`이 0이어도
  `Update()`는 계속 호출되므로 정지 중에도 ESC로 재개 가능(PlayerController가 이미 같은 전제로
  동작 중). 컨텍스트에 따라 다르게 동작:
  - 확인 다이얼로그가 열려 있으면 → 다이얼로그만 닫고 4버튼 패널로 복귀
  - 환경설정 패널이 열려 있으면 → 설정만 닫고 4버튼 패널로 복귀
  - 일시정지 중이면 → 재개
  - 평상시 → 일시정지 메뉴 열기
  - **가드**: 승리/패배 화면(`hasEnded`)이거나 레벨업 카드 선택 화면(`LevelUpUI.IsSelectionActive`,
    이번에 새로 추가한 public 프로퍼티)이 열려 있으면 ESC를 무시함 - 둘 다 마찬가지로
    `Time.timeScale=0`을 쓰는 풀스크린 모달이라 겹치면 안 되기 때문.
- **일시정지 자체**: 기존 승리/패배 화면과 동일한 패턴 재사용 - `Time.timeScale = 0f` +
  `AudioLayerManager.PauseAllAudio()`. 계속하기는 반대로 `Time.timeScale = 1f` +
  `AudioLayerManager.ResumeAllAudio()`.
- **환경설정 (사용자 결정 - 축소판)**: 메인 메뉴의 전체 설정 화면(키 리바인드 8행 + 싱크 보정)은
  재사용하지 않고, **볼륨 3종(BGM/SFX/악기) 슬라이더만** 있는 축소판을 새로 코드로 만듦
  (`EnsurePauseSettingsPanel()` + `CreateSimpleSlider()` - Unity 기본 Slider 계층
  Background/Fill Area/Fill/Handle Slide Area/Handle을 코드로 구성). `GameSettings`(기존
  `PlayerPrefs` 래퍼, 메인 메뉴 설정과 동일한 키 공유)를 그대로 읽고 씀 - 메인 메뉴에서 바꾼
  값이 여기 슬라이더 초기값으로도 반영되고, 반대로도 마찬가지.
  - **버그 사전 발견+수정**: `AudioLayerManager`의 BGM/악기 소스 볼륨은 원래 "재생을 시작하는
    시점"에만 `GameSettings`를 한 번 읽어와 곱하는 구조라(`PlayBossBattleBGM()`,
    `ActivateInstrumentAudio()`), 이미 재생 중인 소스는 슬라이더를 움직여도 볼륨이 반영되지
    않는 gap이 있었음(지금까지 볼륨 조절이 메인 메뉴에서만 가능했고 거긴 이 소스들이 재생 중이지
    않아 드러나지 않았던 문제). `AudioLayerManager.RefreshVolumesFromSettings()`를 새로 추가해
    슬라이더 값이 바뀔 때마다 호출, 재생 중인 모든 소스에 즉시 재적용되도록 함.
  - 뒤로가기 버튼 클릭 시 `GameSettings.Save()` 호출(슬라이더 드래그마다 저장하진 않음 -
    `SettingsPanelController.OnDisable()`과 동일한 이유).
- **메인으로 / 게임종료 확인 다이얼로그**: 공용 컴포넌트(`EnsureConfirmDialogElements()`) -
  메시지 텍스트 + 취소/확인 버튼. `ShowConfirmDialog(message, onConfirm)`로 재사용.
  - 메인으로 확인 → `Time.timeScale = 1f`(기존 `OnReturnToMenuClicked()`와 동일하게, 다음 씬이
    멈춰있지 않도록 복원) + `SceneManager.LoadScene("MainMenu")`
  - 게임종료 확인 → 기존 `MainMenuController.OnQuitClicked()`와 동일한 로직
    (`#if UNITY_EDITOR EditorApplication.isPlaying=false #else Application.Quit() #endif`)
- **레이어 순서**: `EnsurePauseMenuElements()`가 `Awake()`에서 다른 모든 `Ensure*` 호출보다
  나중에 실행되므로, `RhythmCanvas`의 마지막 자식이 되어 자동으로 HP/EXP 바·악기 슬롯·보스 HP
  텍스트 등 다른 모든 HUD 요소보다 위에 그려짐(별도 sortingOrder 조작 불필요).

## 1. 사전 준비 상태

- [x] `RhythmUI.cs`에 일시정지 메뉴 전체 구현 완료 (필드, `Ensure*` 빌드 메서드, ESC 핸들러,
      버튼 콜백)
- [x] `AudioLayerManager.cs`에 `RefreshVolumesFromSettings()` 추가 완료
- [x] `LevelUpUI.cs`에 `IsSelectionActive` public 프로퍼티 추가 완료 (레벨업 카드 선택 중 ESC
      가드용, 최소 변경)
- [ ] Unity 에디터 컴파일 확인 - **아직 안 됨**
- [ ] Play 모드 실측 - **아직 안 됨**

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **ESC로 일시정지 메뉴 열림**: Gameplay Play 모드에서 ESC를 누르면 화면이 어두워지며(dim
      오버레이) 계속하기/환경설정/메인으로/게임종료 4버튼이 중앙에 뜨는지 스크린샷으로 확인.
      `Time.timeScale`이 0이 되는지, 음악이 멈추는지 확인.
- [ ] **일시정지 중 게임 진행 정지**: 일시정지 상태에서 몬스터/노트/플레이어가 전혀 움직이지
      않는지 확인(`Time.timeScale=0` 실측).
- [ ] **다시 ESC로 닫힘**: 일시정지 메뉴가 열린 상태에서 ESC를 다시 누르면 "계속하기"를 누른
      것과 동일하게 재개되는지 확인.
- [ ] **계속하기 버튼**: 클릭 시 메뉴가 닫히고 `Time.timeScale=1`로 복원, 음악이 다시 재생되고
      게임이 정상적으로 계속 진행되는지 확인.
- [ ] **환경설정 버튼**: 클릭 시 4버튼 패널이 볼륨 3종 슬라이더(배경음/효과음/악기 음량) +
      뒤로가기 버튼 패널로 전환되는지 확인. 슬라이더 초기값이 현재 `GameSettings` 값(메인
      메뉴에서 바꾼 값이 있다면 그 값)과 일치하는지 확인.
  - 참고: 일시정지 중엔 `AudioLayerManager.PauseAllAudio()`로 오디오 소스 자체가 멈춰있어서
    슬라이더를 움직여도 **그 자리에서 들리는 소리 변화는 없는 게 정상**입니다(버그 아님) -
    `RefreshVolumesFromSettings()`가 `.volume` 프로퍼티는 정상적으로 갱신해두고, "계속하기"로
    재개(`UnPause()`)하는 순간부터 바뀐 볼륨이 실제로 들리는지 확인해주세요. 이 프로젝트에서
    반복된 "프로퍼티는 맞는데 화면/소리엔 반영 안 됨" 함정과 헷갈리지 않도록 미리 안내드립니다 -
    이번엔 재생이 정지된 상태라 당연히 안 들리는 것이지, `Image.Type.Filled`+`sprite=null`
    때처럼 값이 반영 안 되는 게 아닙니다. 재개 후 실제로 볼륨이 바뀌었는지가 진짜 검증 포인트.
- [ ] **뒤로가기 버튼(환경설정 → 4버튼 패널)**: 클릭 시 4버튼 패널로 복귀하는지, `GameSettings.
      Save()`가 호출되어 볼륨 값이 `PlayerPrefs`에 실제로 저장됐는지(재시작 후에도 유지되는지,
      또는 `PlayerPrefs.GetFloat`로 직접 확인) 확인.
- [ ] **메인으로 버튼 → 확인 다이얼로그**: 클릭 시 "정말 메인으로 이동하시겠습니까?" 메시지와
      취소/확인 버튼이 뜨는지 확인.
  - 취소: 다이얼로그가 닫히고 4버튼 패널로 복귀(계속 일시정지 상태 유지)하는지 확인.
  - 확인: `Time.timeScale=1`로 복원된 뒤 `MainMenu` 씬으로 정상 전환되는지, 전환된 MainMenu가
    멈춰있지 않고 정상 동작하는지(기존 `background_grid_scroll_rewrite_test_guide.md` 등에서
    이미 확인된 "timeScale 복원 안 하면 다음 씬이 얼어붙는" 패턴과 동일한 위험 지점이므로 특히
    꼼꼼히 확인) 확인.
- [ ] **게임종료 버튼 → 확인 다이얼로그**: 클릭 시 "정말 게임을 종료하시겠습니까?" 메시지가
      뜨는지, 확인 클릭 시 에디터에서는 Play 모드가 정상적으로 정지되는지 확인.
- [ ] **레벨업 카드 선택 중 ESC 무시**: 레벨업 팝업이 떠 있는 동안 ESC를 눌러도 일시정지
      메뉴가 열리지 않는지 확인(`LevelUpUI.IsSelectionActive` 가드 실측).
- [ ] **승리/패배 화면에서 ESC 무시**: 승리 또는 패배 화면이 뜬 상태에서 ESC를 눌러도 일시정지
      메뉴가 열리지 않는지 확인(`hasEnded` 가드 실측).
- [ ] **레이어/클릭 차단 확인**: 일시정지 메뉴가 떠 있을 때 배경의 게임 화면을 클릭해도(예:
      캐릭터 근처를 클릭) 게임에 영향이 없는지(dim 오버레이의 `raycastTarget=true`가 아래 클릭을
      막는지) 확인.
- [ ] **기존 메인 메뉴 설정 화면 회귀 없음**: 이번 작업은 `SettingsPanelController.cs`/
      `MainMenuController.cs`를 건드리지 않았으므로 회귀 가능성은 낮지만, 메인 메뉴의 기존
      설정 화면(볼륨/키 리바인드/싱크 보정)이 여전히 정상 동작하는지 간단히 확인.

## 3. 참고 - 관련 코드 위치

- `Assets/Scripts/Rhythm/RhythmUI.cs` - 일시정지 메뉴 전체 구현(필드 선언부 "Pause Menu" 헤더
  부터 파일 끝 "일시정지 메뉴" 섹션까지).
- `Assets/Scripts/Audio/AudioLayerManager.cs` - `RefreshVolumesFromSettings()` (신규, 파일 끝).
- `Assets/Scripts/UI/LevelUpUI.cs` - `IsSelectionActive` (신규, 필드 선언부 바로 아래).
- 씬/프리팹 직접 편집 없음(전부 런타임 코드 생성).

## 4. 검증 결과

(2026-08-10, Unity MCP 세션에서 `Gameplay.unity` Play 모드 실측 검증)

### 4.1 통과한 항목

- [x] **컴파일 에러/경고 0건**.
- [x] **ESC로 일시정지 메뉴 열림**: 실제로 시뮬레이션한 ESC 키 입력(`InputSystem.QueueStateEvent`로
      `Keyboard.current`에 진짜 키 상태 이벤트를 주입 - 리플렉션으로 값만 확인하는 방식이 이
      프로젝트에서 반복적으로 검증 함정에 걸렸던 것을 감안해, 이번엔 실제 입력 경로를 그대로
      태워서 확인)을 `HandleEscapeInput()`(가드 포함 실제 진입점)에 흘려보내 확인. `timeScale=0`,
      dim 오버레이 `color=(0,0,0,0.65)` 확인, 스크린샷에서 계속하기/환경설정/메인으로/게임종료
      4버튼 정상 표시. ✅
- [x] **다시 ESC로 닫힘(재개)**: 같은 방식으로 ESC를 한 번 더 흘려보내 `pauseMenuRoot` 비활성화,
      `timeScale=1`로 복원되는 것 확인. ✅
- [x] **환경설정 버튼**: 4버튼 패널 → 볼륨 3종 슬라이더 + 뒤로가기 패널로 정상 전환(스크린샷
      확인). 슬라이더 초기값이 `GameSettings` 현재값과 일치.
- [x] **슬라이더 → GameSettings/PlayerPrefs 반영**: BGM 슬라이더 값을 1→0.3으로 바꾼 뒤
      `PlayerPrefs.GetFloat("Settings.BgmVolume01")`이 실제로 0.3으로 갱신되는 것을 raw
      PlayerPrefs 조회로 확인. ✅ (아래 4.3 참고 - 검증 과정에서 한 차례 혼선이 있었음)
- [x] **뒤로가기 버튼**: 클릭 시 4버튼 패널로 복귀 확인.
- [x] **메인으로 → 확인 다이얼로그**: 메시지 "정말 메인으로 이동하시겠습니까?" 정확히 표시.
  - 취소: 다이얼로그만 닫히고 4버튼 패널로 복귀, `timeScale=0` 유지(계속 일시정지 상태) 확인.
  - 확인: `timeScale=1`로 복원된 뒤 `MainMenu` 씬으로 정상 전환, 전환된 MainMenu가 얼어붙지 않고
    타이틀/버튼이 정상적으로 표시되는 것을 스크린샷으로 확인(README에서 우려한 "timeScale 복원
    안 하면 다음 씬이 얼어붙는" 패턴 재현 안 됨). ✅
- [x] **게임종료 → 확인 다이얼로그**: 메시지 "정말 게임을 종료하시겠습니까?" 정확히 표시. 확인
      클릭 시 `EditorApplication.isPlaying`이 실제로 `false`가 되어 Play 모드가 정상 종료됨. ✅
- [x] **레벨업 카드 선택 중 ESC 무시**: `LevelUpUI.ShowLevelUpSelection()`으로 카드 선택 화면을
      띄운 뒤(`IsSelectionActive=True`), 실제 시뮬레이션 ESC 키 입력을 `HandleEscapeInput()`에
      흘려도 `pauseMenuRoot`가 활성화되지 않는 것을 확인. 카드 선택을 닫은 뒤(`IsSelectionActive=
      False`) 같은 방식으로 ESC를 누르면 정상적으로 일시정지 메뉴가 열리는 것까지 대조 확인. ✅
- [x] **승리/패배 화면에서 ESC 무시**: `ShowDefeatScreen()` 호출로 `hasEnded=True`를 만든 뒤 같은
      방식으로 ESC를 흘려도 일시정지 메뉴가 열리지 않는 것을 확인. ✅
- [x] **레이어/클릭 차단**: `PauseMenuRoot`의 `Image.raycastTarget=true`이고 크기가 캔버스
      전체(2560×1440)를 덮으며, `Awake()`에서 다른 모든 `Ensure*` 호출보다 나중에 생성되어 항상
      최상단 sibling이 되는 것을 구조적으로 확인(uGUI 표준 레이캐스트 동작상 이 조건만으로 아래
      클릭이 차단됨이 보장됨 - 별도 클릭 시뮬레이션은 생략).

### 4.2 참고 - 테스트 중 발생한 시각적 겹침 (버그 아님)

`hasEnded` 가드를 테스트하던 시점에, 실제 플레이가 백그라운드에서 계속 진행되고 있어서(제 테스트
호출 사이사이 실시간이 흘러 EXP가 누적됨) 우연히 자연스러운 레벨업이 동시에 발생 - `LevelUpUI`
카드 화면과 제가 강제로 띄운 패배 화면 텍스트가 스크린샷에서 겹쳐 보였습니다. 이는 두 시스템이
서로의 상태를 확인하지 않고 각자 `Time.timeScale=0`을 거는 기존 구조 때문인데, 이번 일시정지
메뉴 작업의 범위 밖이고(가이드 0절에서도 이 둘이 상호 배타적이어야 한다고 명시했지만 "레벨업↔승리/
패배" 간 상호 가드는 이번 작업 대상이 아니었음), 정상적인 게임 진행에서는 레벨업과 패배가 정확히
같은 프레임에 동시 발생할 확률이 매우 낮아 실질적 영향은 적다고 판단됩니다. 참고 사항으로만 남깁니다.

### 4.3 참고 - 슬라이더 리스너 검증 중 혼선 (실제 버그 아닌 것으로 결론)

첫 Play 세션에서 오래(수십 분) 반복적으로 리플렉션 호출을 이어간 뒤 BGM 슬라이더의
`onValueChanged`에 등록된 런타임 리스너 수를 확인했을 때 0개로 나와, `CreateVolumeSliderRow()`의
`AddListener(onChanged)` 호출이 실패한 것처럼 보였습니다(슬라이더 값을 바꿔도 `PlayerPrefs`에
반영 안 됨). 하지만 Play 모드를 완전히 새로 시작한 깨끗한 세션에서 동일한 절차를 다시 밟자
리스너가 정상적으로 1개 등록돼 있었고 값 변경도 즉시 `PlayerPrefs`에 반영됨을 확인했습니다 -
코드 자체엔 문제가 없고, 같은 장기 세션 안에서 Play 모드를 여러 번 껐다 켜며 리플렉션으로 깊이
파고든 것이 원인 불명의 세션 아티팩트를 만든 것으로 보입니다(이 프로젝트에서 반복됐던 "값은
맞는데 반영 안 됨" 함정과는 다른 종류의 이슈 - 실제 게임 코드가 아니라 검증 도구 쪽 문제로 판단).

### 4.4 확인하지 않은 항목

- **RefreshVolumesFromSettings()가 재생 중인 실제 오디오 소스에 즉시 반영되는지**: 이번 테스트
  시점엔 `bgmSource.clip`이 비어 있어(보스전 BGM이 아직 시작 안 된 상태) 실측하지 못했습니다.
  코드 리뷰로는 로직이 올바르나(재생 중인 소스가 있을 때만 조건부로 volume 갱신), 보스전까지
  플레이를 진행해 실측하는 것을 권장.
- **기존 메인 메뉴 설정 화면 회귀**: 별도 작업(`main_menu_background_test_guide.md`)에서 이미
  설정 화면이 정상 동작하는 것을 확인했고 이번 작업이 `SettingsPanelController.cs`를 건드리지
  않았으므로 회귀 위험은 낮다고 판단, 별도 재확인은 생략.

### 4.5 종합 결론

가이드 2절의 체크리스트 대부분을 실제 시뮬레이션 키 입력과 스크린샷으로 확인했고, 발견된 버그는
없습니다(4.3의 혼선은 검증 도구 세션 아티팩트로 결론, 4.2는 이번 작업 범위 밖의 기존 구조적
특성). `archive/`로 이동 가능한 상태로 판단됩니다.
