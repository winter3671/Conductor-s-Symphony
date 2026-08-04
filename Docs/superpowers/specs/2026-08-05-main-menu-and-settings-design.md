# 메인화면 & 설정창 Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to turn this spec into an implementation plan before touching code.

## Goal

게임 시작 시 진입하는 메인화면(Start/Settings/Quit)과, 볼륨/키 리바인딩/리듬 싱크 자동 보정을 제공하는 설정창을 신규 구축한다. 현재 프로젝트는 `Gameplay.unity` 단일 씬으로만 구성되어 있고 메인메뉴·설정·씬 전환·세이브 시스템이 전혀 없는 상태이므로 이번 작업이 해당 기반을 처음 놓는다.

## Architecture

- 신규 씬 `MainMenu.unity`(빌드 인덱스 0)를 추가하고 `Gameplay.unity`(인덱스 1)가 뒤를 잇는다. Start 버튼은 `SceneManager.LoadScene("Gameplay")`로 전환한다.
- 설정값은 `MonoSingleton` 매니저가 아니라 순수 정적 클래스 `GameSettings`(PlayerPrefs 로드/저장)로 관리한다. 기존 코드가 이미 쓰는 정적 Database 패턴(`InstrumentPatternDatabase` 등)과 결이 같고, 씬에 GameObject를 배치하지 않아도 되므로 문서에 기록된 "스크립트는 완성했는데 씬 배치를 빼먹어 `Instance`가 null이었던" 버그 클래스를 원천적으로 피한다. `MainMenu`/`Gameplay` 양쪽에서 별도 설정 없이 바로 참조 가능.
- 설정창은 별도 씬이 아니라 `MainMenu` 씬 안에서 패널 전환(메인 패널 ↔ 설정 패널 ↔ 싱크 계측 패널)으로 처리한다. 게임 중 일시정지 메뉴에서의 설정 접근은 이번 범위 밖이다.
- UI는 프리팹 기반(`MainMenuCanvas` 프리팹 하나에 `MainPanel`/`SettingsPanel`/`SyncCalibrationPanel` 자식을 두고 컨트롤러가 활성 패널을 토글).

## Components

**신규 파일:**
- `Assets/Scenes/MainMenu.unity` — 메인화면 씬
- `Assets/Scripts/Settings/GameAction.cs` — 리바인딩 가능한 입력을 나타내는 enum (`HitLeft, HitUpLeft, HitUpRight, HitRight, MoveUp, MoveDown, MoveLeft, MoveRight`)
- `Assets/Scripts/Settings/GameSettings.cs` — 정적 클래스. PlayerPrefs 기반 볼륨(BGM/SFX/악기트랙, 0~1), `GameAction → Key` 바인딩, `RhythmSyncOffsetMs`(float)의 Load/Save/typed getter-setter 제공
- `Assets/Scripts/UI/MainMenu/MainMenuController.cs` — Start/Settings/Quit 버튼 처리, 패널 전환
- `Assets/Scripts/UI/MainMenu/SettingsPanelController.cs` — 볼륨 슬라이더 3개, 키 리바인딩 행 8개, 싱크 오프셋 표시 + "다시 측정" 버튼, 뒤로가기
- `Assets/Scripts/UI/MainMenu/KeyRebindRow.cs` — 리바인딩 행 1개의 "키 입력 대기" 상태 캡처 로직 (재사용 컴포넌트)
- `Assets/Scripts/UI/MainMenu/SyncCalibrationController.cs` — 메트로놈 스케줄링(리드인 2박 + 측정 6박), 입력 캡처, 평균 오프셋 계산, 결과 표시/재시도
- `Assets/Prefabs/UI/MainMenuCanvas.prefab` — 위 세 패널을 담는 루트 프리팹

**수정 파일:**
- `Assets/Scripts/Rhythm/RhythmManager.cs` — QWER 하드코딩 키 검사(115-118행)를 `GameSettings.GetBinding(GameAction)` 조회로 교체, `public const float Bpm = 97f` 추가 후 기존 `bpm` 필드 대신 참조, `CheckHit()`의 `currentTime`에 `GameSettings.RhythmSyncOffsetSeconds` 반영
- `Assets/Scripts/Player/PlayerController.cs` — 방향키 하드코딩 검사(214-217행)를 `GameSettings.GetBinding(GameAction)` 조회로 교체
- `Assets/Scripts/Audio/AudioLayerManager.cs` — `Awake()`에서 `GameSettings` 볼륨값을 읽어 `bgmSource.volume`/`sfxSource.volume`에 반영하고, `ActivateInstrumentAudio`/`PlayInstrumentKeySound`/`PlayBossBattleBGM`에서 설정된 배율을 곱해 적용
- `ProjectSettings/EditorBuildSettings.asset` — 빌드 씬 목록에 `MainMenu`(0), `Gameplay`(1) 등록

## Data Flow

1. 앱 시작 → `MainMenu` 씬 로드 → `GameSettings`가 최초 접근 시 PlayerPrefs에서 저장된 값(없으면 기본값)을 lazy load.
2. 설정 패널에서 값 변경 시 즉시 `GameSettings`에 반영 + PlayerPrefs 저장 (별도 "적용" 버튼 없이 즉시 반영·저장).
3. Start 클릭 → `Gameplay` 씬 로드 → `RhythmManager`/`PlayerController`/`AudioLayerManager`가 각자 `Awake()`/입력 처리 시점에 `GameSettings`를 직접 참조 — 씬 전환 간 별도 전달 로직 불필요(정적 클래스이므로 씬이 바뀌어도 값 유지).

## 리듬 싱크 자동 측정 (SyncCalibrationController)

- "싱크 측정" 버튼 → 계측 패널로 전환 → 리드인 2박(입력 없이 박자 안내) → 측정 6박(박자에 맞춰 `GameSettings.GetBinding(GameAction.HitLeft)` 키 입력).
- 메트로놈은 `RhythmManager.Bpm`(97, const 참조로 값 드리프트 방지)을 그대로 사용해 `AudioSettings.dspTime` 기준으로 스케줄링. `AudioLayerManager`에 의존하지 않는 계측 전용 독립 오디오 소스(간단한 tick 사운드, `AudioLayerManager.CreateSynthTone`과 동일한 절차적 생성 방식 재사용 가능)를 사용한다 — `AudioLayerManager`는 `Gameplay` 씬 전용이라 `MainMenu` 씬에는 없기 때문.
- 각 측정 박자의 목표 dspTime 대비 ±300ms 이내 입력만 유효 샘플. 6박 중 유효 샘플이 4개 미만이면 "박자를 다시 맞춰보세요" 안내 후 재시도 유도(저장하지 않음).
- 유효 샘플 (입력 시각 − 목표 시각)의 단순 평균을 ms 단위 오프셋으로 계산해 `GameSettings.RhythmSyncOffsetMs`에 저장.
- 결과 화면: "현재 오프셋: {value}ms" + "적용"(패널 닫고 저장) / "다시 측정"(재계측).
- 저장된 오프셋은 `RhythmManager.CheckHit()`에서 `currentTime`을 비교할 때 `+ offsetSeconds`로 단순 가산한다. `AudioLayerManager.SongTime`(단일 마스터 시계) 자체는 건드리지 않는다 — 판정 비교 지점에서만 보정해 기존 "시계는 하나"라는 불변조건을 유지.

## Key Rebinding

- `KeyRebindRow`가 행을 클릭하면 "키 입력 대기" 상태로 전환, 다음 키 입력(`Keyboard.current` 폴링)을 캡처.
- 캡처된 키가 이미 다른 액션에 바인딩되어 있으면 거부하고 "이미 사용 중인 키입니다" 안내(스왑 없이 단순 차단).
- 유효하면 `GameSettings.SetBinding(action, key)` 호출 → 즉시 PlayerPrefs 저장.
- `RhythmManager`/`PlayerController`는 하드코딩된 `keyboard.qKey` 등을 `keyboard[GameSettings.GetBinding(action)]`(Input System의 `Keyboard` 인덱서, `KeyControl` 반환) 조회로 교체.

## Volume

- 3채널: BGM(`bgmSource`), 타격음(`sfxSource`), 악기 트랙(동적 생성되는 `activeInstrumentSources`).
- `GameSettings`에 각각 0~1 배율(`BgmVolume01`, `SfxVolume01`, `InstrumentVolume01`) 저장.
- `AudioLayerManager`의 각 소스 볼륨 대입 지점(현재 하드코딩된 0.5f/0.75f/0.85f)에 해당 배율을 곱해 적용.

## Error Handling

- `GameSettings`가 PlayerPrefs에 값이 없을 때(최초 실행)는 각 항목의 기본값(볼륨 1.0, 기본 키맵 QWER+방향키, 오프셋 0ms)을 반환한다 — 예외를 던지지 않는다.
- 키 리바인딩 충돌은 UI 레벨에서 안내 후 무시(저장하지 않음)로 처리, 예외 없음.
- 싱크 측정 실패(유효 샘플 부족)는 재시도만 유도, `GameSettings`는 이전 값을 유지한다.

## Testing / Verification

유닛테스트 프레임워크가 확인되지 않는 UI/씬 작업이라 Unity Editor Play Mode 수동 검증으로 진행한다:
1. `MainMenu` 씬에서 Start → `Gameplay` 진입, Quit → 애플리케이션 종료(에디터에서는 Play 종료) 확인
2. 볼륨 슬라이더 조작 시 `Gameplay` 진입 후 실제 음량 변화 확인(BGM/타격음/악기 트랙 각각)
3. 키 리바인딩 후 `Gameplay`에서 변경된 키로 판정이 정상 동작하는지, 충돌 키 입력 시 거부되는지 확인
4. 싱크 측정 실행 → 의도적으로 빠르게/느리게 입력해 오프셋 부호가 올바른 방향으로 계산되는지 확인 → `Gameplay`에서 판정 타이밍에 반영되는지 확인
5. 앱 재시작(에디터 재생 재시작) 후 PlayerPrefs에 저장된 설정이 유지되는지 확인

## Out of Scope

- 게임 중 일시정지 메뉴에서의 설정 접근
- 그래픽 옵션(해상도/전체화면 등), 로컬라이제이션
- 키 리바인딩 스왑(충돌 시 자동 교체) — 단순 차단으로 대체
- 게임패드/컨트롤러 리바인딩 (현재 코드가 키보드 전용)
