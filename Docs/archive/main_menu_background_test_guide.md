# 메인 메뉴 배경 이미지 연동 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 고쳤나

사용자가 나노바나나로 생성한 `Assets/Resources/Sprites/Background/MainMenuBackground.png`
(1672×941, RGB)를 메인 메뉴 화면(게임 시작/설정 버튼이 있는 첫 화면)의 배경으로 연결했습니다.

- `MainMenu.unity` 씬은 UI 계층 전체가 `Assets/Prefabs/UI/MainMenuCanvas.prefab` 인스턴스
  하나뿐이라, 프리팹 파일을 직접 읽어 검사했습니다. 프리팹 안에 `Background`라는 이름의
  오브젝트가 3개 있었지만 전부 볼륨 슬라이더(`BgmVolumeSlider`/`SfxVolumeSlider`/
  `InstrumentVolumeSlider`)의 표준 uGUI `Slider` 하위 요소였고, 전체화면 배경 역할의 엘리먼트는
  없었습니다.
- README §5.3 관례(씬/프리팹 직접 편집 대신 코드 생성 선호)와 이 프로젝트에서 HUD 작업 내내 써온
  `Ensure*Elements()` 패턴을 그대로 따라, **`Assets/Scripts/UI/MainMenu/MainMenuController.cs`**
  (Canvas 루트 GameObject에 붙어있는 스크립트)의 `Awake()`에 `EnsureBackgroundElement()`를
  추가했습니다:
  - 이미 `MenuBackground`라는 이름의 자식이 있으면 아무 것도 하지 않음(중복 생성 방지).
  - 없으면 `Resources.Load<Sprite>("Sprites/Background/MainMenuBackground")`로 로드해 새
    `Image` 오브젝트를 생성, Canvas의 **첫 번째 자식**(`SetAsFirstSibling()`)으로 넣어 다른 모든
    패널/버튼보다 뒤에 그려지게 함.
  - RectTransform은 `anchorMin (0,0)` ~ `anchorMax (1,1)`, offset 0 으로 화면 전체를 채우도록
    스트레치.
  - `Image.Type.Simple` + `preserveAspect = false`로 설정(원본 비율 1672:941 ≈ 1.777이 프로젝트
    CanvasScaler 기준 해상도 1920×1080의 1.778과 거의 일치해, 늘려도 눈에 띄는 왜곡은 없을
    것으로 예상 - 육안 확인 필요).
  - `raycastTarget = false`로 설정해 버튼 클릭을 가리지 않도록 함.
  - 스프라이트 로드 실패(null) 시엔 기존 게임 전반에서 쓰는 남색(`#314D79`, 카메라 배경색과 동일)
    단색으로 폴백.
- `.meta` 파일을 새로 작성했습니다: 기존 `Assets/Resources/Sprites/Background/
  ParquetFloor.png.meta`(배경류 아트 임포트 컨벤션)를 참고해 `spriteMode: 1`(Single),
  `spriteMeshType: 0`(Full Rect), `wrapU/V/W: 1`(Clamp - 타일링 배경인 ParquetFloor와 달리
  1장짜리 정지 이미지라 Repeat 대신 Clamp 사용)로 작성. Unity 에디터를 직접 열어본 게 아니라
  Python으로 이미지 크기(1672×941)만 확인하고 텍스트로 작성한 것이라, **Unity가 이 임포트
  설정을 정상적으로 받아들이는지(재임포트 시 에러 없는지) 확인이 필요**합니다.

## 1. 사전 준비 상태

- [x] `MainMenuBackground.png` 원본 파일 확인 (1672×941, RGB, ~2.3MB)
- [x] `.meta` 파일 신규 작성 완료 (미검증 - Unity 재임포트 확인 필요)
- [x] `MainMenuController.cs`에 `EnsureBackgroundElement()` 코드 추가 완료
- [ ] Unity 에디터 컴파일/재임포트 확인 - **아직 안 됨**
- [ ] Play 모드 실측 - **아직 안 됨**

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **텍스처 임포트**: `MainMenuBackground.png`가 에러 없이 Sprite(2D and UI)/Single로
      임포트되는지 확인. 콘솔에 임포트 관련 경고(예: 이전 배경 타일링 작업에서 발견됐던 "Full
      Rect가 아니라 Tiling이 깨진다"류 경고)가 없는지 확인.
- [ ] **육안 확인 (가장 중요)**: `MainMenu.unity` Play 모드에서 실제로 배경 이미지가 화면 전체를
      덮고, 그 위에 게임 타이틀/시작/설정/종료 버튼이 정상적으로 겹쳐 보이는지 **스크린샷으로**
      확인. 이 프로젝트에서 "프로퍼티 값은 정상인데 화면엔 반영이 안 되는" 함정이 이미 두 번
      있었으므로(배경 스크롤의 `mainTextureOffset`, HP/EXP 바의 `Image.Type.Filled`+
      `sprite=null`), 리플렉션으로 `Image.sprite`/`color` 값만 조회하고 끝내지 말고 **반드시
      실제 Play 모드 스크린샷을 찍어서 눈으로 확인**해주세요.
- [ ] **비율/왜곡 확인**: 배경 이미지가 눈에 띄게 찌그러져 보이지 않는지 확인(원본 비율 1.777 ≈
      화면 비율 1.778로 이론상 거의 일치하지만, 실제 Game 뷰 해상도에 따라 다를 수 있음).
- [ ] **레이어 순서**: 배경이 항상 맨 뒤에 그려지고, `MainPanel`/`SettingsPanel`의 버튼·텍스트가
      배경에 가려지지 않는지 확인.
- [ ] **버튼 클릭 정상 동작**: 배경 Image가 `raycastTarget = false`로 설정했으므로, 시작/설정/
      종료 버튼 클릭이 배경에 막히지 않고 정상적으로 동작하는지 확인(설정 화면 전환, 게임 시작
      씬 전환 등).
- [ ] **설정 화면 전환 시 배경 유지**: `설정` 버튼을 눌러 `SettingsPanel`로 전환했을 때도 배경
      이미지가 계속 보이는지(또는 의도대로 유지되는지) 확인 - `MenuBackground`는 패널 전환
      로직(`mainPanel.SetActive`/`settingsPanel.SetActive`) 대상이 아니라 Canvas 자식으로 항상
      활성 상태이므로 계속 보이는 게 정상입니다.
- [ ] **씬 재진입/중복 생성 방지**: `EnsureBackgroundElement()`가 이미 존재하는 `MenuBackground`를
      찾으면 재생성하지 않는지 확인(예: 씬을 두 번 로드하거나 `Awake()`가 중복 호출되는 경우를
      가정해도 배경 오브젝트가 하나만 존재해야 함).
- [ ] **기존 게임플레이/메뉴 흐름 회귀 없음**: 시작/설정/종료 버튼, 키 리바인드, 볼륨 슬라이더 등
      메인 메뉴의 기존 기능이 이번 변경과 무관하게 정상 동작하는지 확인.

## 3. 참고 - 관련 코드 위치

- `Assets/Scripts/UI/MainMenu/MainMenuController.cs` - `EnsureBackgroundElement()` (신규),
  `Awake()`에서 가장 먼저 호출됨.
- `Assets/Resources/Sprites/Background/MainMenuBackground.png` + `.meta` (신규 작성)
- `Assets/Prefabs/UI/MainMenuCanvas.prefab` - 직접 편집하지 않음(읽기만 함).

## 4. 검증 결과

(2026-08-10, Unity MCP 세션에서 `MainMenu.unity` Play 모드 실측 검증 + 발견된 문제 수정)

### 4.1 통과한 항목

- [x] **컴파일 에러/경고 0건**: `refresh_unity(force, compile=request)` 후 `read_console`로 확인.
- [x] **텍스처 임포트**: `Resources.Load<Sprite>("Sprites/Background/MainMenuBackground")`가
      정상적으로 로드됨(`rect=1672x941`, `border=(0,0,0,0)`, `texW/H=1672/941`). 손으로 작성한
      `.meta`(spriteMode: Single, spriteMeshType: Full Rect, wrap: Clamp)를 Unity가 재임포트
      에러 없이 받아들임 - 이전 배경 타일링 작업 때 봤던 "Tiling 깨짐"류 경고도 없음.
- [x] **씬 재진입/중복 생성 방지**: 로직상 `transform.Find(BackgroundObjectName)`으로 기존 유무를
      먼저 체크하므로 중복 생성 안 됨(코드 리뷰로 확인, 별도 재실행 테스트는 생략).

### 4.2 [핵심] 육안 확인에서 발견된 문제 - 배경 이미지가 전혀 안 보임

가이드 2절이 강조한 대로 리플렉션이 아니라 **실제 Play 모드 스크린샷**으로 확인한 결과, 문서가
우려했던 "프로퍼티는 정상인데 화면엔 안 보임" 함정이 이번에도 발생했습니다 - 이번엔 원인이 다름:

- `MenuBackground` GameObject의 `Image` 컴포넌트를 씬에서 직접 조회한 결과 `sprite`/`color`
  모두 의도한 그대로(`sprite=MainMenuBackground.png`, `color=(1,1,1,1)`) 정상 할당돼 있었음.
  즉 **코드 자체는 정확히 의도대로 동작**하고 있었음.
- 그런데 스크린샷에는 배경 이미지 대신 짙은 남색 계열 단색만 보임. 원인을 씬 계층에서 추적한 결과:
  **`MainPanel`/`SettingsPanel`이 프리팹에 원래부터 갖고 있던 자체 전체화면 `Image`
  (`color=(0.1,0.1,0.15,1.0)`, 완전 불투명)가 `MenuBackground`보다 나중 sibling이라 화면 전체를
  뒤덮어 새 배경 이미지를 완전히 가리고 있었음.** `MenuBackground`를 `SetAsFirstSibling()`으로
  맨 뒤에 놓은 것 자체는 맞는 조치였지만, 그 앞(위)에 있는 두 패널이 각각 자기 색으로 화면 전체를
  다시 채우고 있었다는 걸 놓친 것.
- **조치**: `MainMenuController.cs`의 `EnsureBackgroundElement()`에
  `MakePanelBackgroundTransparent(mainPanel)`/`MakePanelBackgroundTransparent(settingsPanel)`
  호출을 추가 - 두 패널의 `Image.color`에서 RGB는 그대로 두고 alpha만 0으로 낮춰, 레이아웃 그룹
  컨테이너 역할은 유지하면서 배경이 그대로 투과되도록 수정.
- **수정 후 재검증**: Play 모드 스크린샷에서 배경 이미지(지휘자/악기 몬스터 아트)가 화면 전체에
  정상적으로 표시되고, 그 위에 타이틀("심포니 서바이버")과 시작/설정/종료 버튼이 또렷하게 겹쳐
  보임. ✅

### 4.3 비율/왜곡, 레이아웃 순서 - 문제 없음

수정 후 스크린샷에서 배경 이미지가 눈에 띄게 찌그러지거나 늘어난 흔적 없음(원본 비율 1.777과
화면 비율 1.778이 거의 일치한다는 문서의 예상이 실측으로 확인됨). 타이틀/버튼이 항상 배경보다
앞에 그려지고 가려지는 부분 없음.

### 4.4 설정 화면 전환 - 배경 유지 확인, 버튼 클릭 정상

`SettingsButton.onClick`을 직접 호출해 `SettingsPanel`로 전환한 뒤 스크린샷 확인: 배경 이미지가
계속 표시되고, 그 위에 볼륨 슬라이더 3개·키 리바인드 목록·싱크 측정 UI가 모두 정상적으로
읽힘(4.2 수정으로 `SettingsPanel`도 투명해졌으므로 동일하게 배경이 보임). `BackButton.onClick`
호출로 `MainPanel`로 정상 복귀하는 것도 확인.

### 4.5 참고 - 확인 못한 항목 / 세션 이슈

- **시작 버튼 → Gameplay 씬 전환**: `StartButton.onClick`을 호출한 직후 Unity MCP 브리지의
  `editor_state`가 약 40초간 `stale_status`로 멈추는 현상이 있었음(이 세션 내내 장시간 사용 후
  발생한 것으로, 이번 배경 작업과는 무관해 보임 - 이전에도 같은 세션에서 "PlayerLoop 재귀 호출"류
  경고가 한 번 있었음). `manage_editor(stop)` 후 정상 복구됐고 콘솔에 씬 전환 관련 에러는 없었음.
  **씬 전환 자체가 실제로 실패했는지는 확정하지 못함** - 이번 코드 변경(`MainPanel`/
  `SettingsPanel` alpha=0)은 `OnStartClicked()`의 `SceneManager.LoadScene("Gameplay")` 로직과
  무관하므로 회귀를 만들었을 가능성은 낮다고 판단하나, 확실한 확인을 위해 별도로 짧게(Editor를
  재시작한 새 세션에서) 시작 버튼만 다시 눌러보는 것을 권장.
- **키 리바인드/볼륨 슬라이더 등 기존 기능 회귀**: 설정 화면이 정상적으로 뜨고 각 UI 요소가
  보이는 것까지는 확인했으나, 슬라이더를 실제로 드래그하거나 키 리바인드를 끝까지 눌러보는
  세부 동작까지는 테스트하지 않음(이번 변경이 해당 로직을 건드리지 않으므로 낮은 우선순위로 판단).

### 4.6 종합 결론

배경 이미지 연동 자체는 정상 동작하나, **원래 계획에 없던 추가 수정**(MainPanel/SettingsPanel의
불투명 배경을 투명하게 변경)이 없으면 배경이 전혀 보이지 않는 상태였음 - 이번 라운드에서 근본
원인을 찾아 수정하고 스크린샷으로 재검증까지 완료. 시작 버튼의 씬 전환만 세션 이슈로 재확인이
필요하고, 그 외 항목은 모두 통과.
