# 인게임 무한 타일링 배경 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 고쳤나

콘서트홀 마룻바닥 도트 패턴을 나노바나나로 생성해 `Assets/Resources/Sprites/Background/
ParquetFloor.png`(2048×2048, RGBA)에 배치했습니다. `CameraController.cs`가 플레이어를 그대로
따라다니고(경계 없음) 몬스터도 플레이어 중심 반경으로 계속 스폰되는 기존 구조상, 벽으로 막힌
공간이 아니라 **무한 반복 배경**으로 가기로 결정했습니다(자세한 이유는 대화 기록 참고 - 코드에
이동/카메라 경계 제한이 전혀 없어서 벽을 두려면 별도 충돌/클램핑 로직을 새로 만들어야 함).

- **신규: `Assets/Scripts/Environment/BackgroundTiler.cs`**. `SpriteRenderer.drawMode = Tiled`로
  텍스처를 이어붙여서 카메라 화면을 항상 꽉 채우는 배경을 만듭니다. `LateUpdate()`에서 카메라
  위치를 따라가고, 매 프레임 `orthographicSize`/`aspect` 기준으로 크기를 재계산해 화면보다 살짝
  더 크게(`extraMargin`) 그립니다. `sortingOrder = -100`으로 항상 맨 뒤에 그려집니다.
- **아직 코드에서 참조되지 않음**: 이 스크립트를 붙인 GameObject가 씬에 아직 없습니다. 아래
  1절 참고.
- 순수 배경 시각 요소 추가입니다. 기존 게임플레이 로직(스폰/판정/밸런스)은 전혀 건드리지 않았습니다.

## 1. 사전 준비 (2026-08-09 완료)

1. `ParquetFloor.png` 임포트 설정 확인/수정: Texture Type = Sprite(2D and UI), Sprite Mode = Single,
   **Wrap Mode = Repeat**(기본값은 Clamp라 바꿔야 함 - 이게 안 되어 있으면 `Tiled` draw mode를
   써도 타일링이 아니라 가장자리가 늘어나 보임). Filter Mode는 프로젝트 기존 관례대로 Bilinear
   유지(다른 스프라이트들도 전부 Bilinear).
2. README §5.3 규칙(씬을 직접 건드리지 말고 프리팹으로 작업)에 따라 `Assets/Prefabs/Environment/
   Background.prefab`을 새로 만들어서: 빈 GameObject + `SpriteRenderer`(sprite = ParquetFloor) +
   `BackgroundTiler` 컴포넌트 구성. 이후 `Gameplay.unity` 씬에 인스턴스 1개를 배치.
3. `refresh_unity(force, compile=request)`로 재컴파일 - 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [x] 컴파일 에러/경고 0건
- [x] **텍스처 임포트**: `ParquetFloor.png`의 Wrap Mode가 Repeat으로 설정됐는지 확인.
- [x] **타일링 육안 확인**: Play 모드에서 배경이 화면 전체를 빈틈없이 덮는지, 이음매가 부자연스럽게
      튀는 지점 없이 매끄럽게 반복되는지 확인.
- [x] **카메라 추적**: 플레이어를 상하좌우로 이동시켰을 때 배경이 화면 가장자리에서 끊기거나
      빈 공간이 보이는 프레임 없이 계속 따라오는지 확인(빠르게 이동/보스 넉백 등 극단적인 이동
      속도에서도 확인).
- [x] **정렬 순서**: 플레이어/몬스터/이펙트/UI 등 다른 모든 요소보다 뒤에 그려지는지(항상 배경으로만
      보이는지) 확인.
- [x] **화면 비율/해상도 변경 대응**: (가능하면) 게임 창 크기를 바꿔봐서 배경이 항상 화면을 꽉
      채우는지 확인(에디터 Game 뷰 크기 조절로 대체 가능).
- [x] **기존 게임플레이 회귀 없음**: 몬스터 스폰/판정/UI 등 배경과 무관한 기존 동작이 그대로인지
      확인(신규 추가 요소라 회귀 없을 것으로 예상).

## 4. 검증 결과

**검증일: 2026-08-09 / 검증 환경: Unity MCP (Unity 6000.5.5f1, 씬 Assets/Scenes/Gameplay.unity)**

### 4-0. 구현 작업 (1절 "아직 안 됨" 상태였던 것을 이번 세션에서 완료)

- `ParquetFloor.png` 텍스처 임포터를 `TextureImporter`/`TextureImporterSettings` API로 직접 수정:
  `spriteImportMode = Single`, `wrapMode = Repeat`, `filterMode = Bilinear` 적용.
- **추가로 발견**: Play 모드 첫 진입 시 콘솔에 `"Sprite Tiling might not appear correctly because
  the Sprite used is not generated with Full Rect"` 경고가 떴음 - 가이드 1절에 없던 항목이었으나
  `Tiled` draw mode를 쓰려면 Mesh Type도 Full Rect여야 한다는 걸 실측으로 발견. `spriteMeshType =
  FullRect`로 추가 수정 후 재진입하니 경고 사라짐.
- `Assets/Prefabs/Environment/Background.prefab` 신규 생성(빈 GameObject + `SpriteRenderer`(sprite=
  ParquetFloor) + `BackgroundTiler`). README §5.3 규칙대로 프리팹으로 먼저 만든 뒤 `Gameplay.unity`
  씬에 인스턴스 1개만 배치, 씬 저장.

### 4-1. 컴파일 에러/경고: 0건 ✅

`refresh_unity(force, compile=request)` 후 `read_console` 확인 - 에러/경고 0건.

### 4-2. 텍스처 임포트 ✅

재확인 결과 `wrapU/V/W: 0`(Repeat), `spriteMode: 1`(Single), `spriteMeshType: 0`(Full Rect). 로드한
스프라이트의 `rect`가 텍스처 전체(2048×2048)를 100% 덮음을 코드로 확인.

### 4-3. 타일링 육안 확인 ✅

Play 모드 스크린샷으로 확인 - 마룻바닥 대각선 패턴이 화면 전체를 이음매 없이 매끄럽게 덮음. 눈에
띄는 크롭/반복 어긋남 없음.

### 4-4. 카메라 추적 ✅

플레이어를 (0,0) / (50,50) / (-200,300) / (1000,-1000) 등 일반 이동 범위를 크게 벗어나는 극단적
좌표로 순간이동시키며 `CameraController`/`BackgroundTiler`의 `LateUpdate()`를 실제 로직 그대로
호출해 확인 - 매번 배경 위치가 카메라 위치와 정확히 일치함(`bg.position == cam.position` on xy).
(1000,-1000) 위치에서 스크린샷을 찍어도 타일링이 끊김 없이 화면을 꽉 채움을 육안 확인.

**참고(테스트 환경 이슈, 코드와 무관)**: 이 에디터는 창이 OS 포커스를 잃으면 Play 모드 시간이
멈추는 환경 문제가 있음(이전 검증 세션에서도 발견). 이 때문에 자동 `Update` 루프에 의존하는 대신
`LateUpdate()`를 리플렉션으로 직접 호출해 매 위치 변경마다 카메라→배경 순서로 동기화시켜 검증했음
- 실제 플레이(연속 프레임)에서는 두 스크립트 모두 매 프레임 자동 호출되므로 이런 수동 동기화가
  필요 없음.

### 4-5. 정렬 순서 ✅

`SpriteRenderer.sortingOrder = -100`(코드에서 고정) 확인. 스크린샷에서도 플레이어/몬스터가 항상
배경 위에 그려짐을 육안 확인.

### 4-6. 화면 비율/해상도 변경 대응 ✅

카메라 `orthographicSize`를 5→10으로, `aspect`를 1.78→3.5(초광각 시뮬레이션)로 각각 바꿔가며
`BackgroundTiler.LateUpdate()`를 호출한 결과, `size = (orthographicSize*2 + extraMargin,
그 값 * aspect)` 공식대로 매번 정확히 재계산됨을 확인(예: orthoSize=10 → size=(42.67, 24.00),
orthoSize=5·aspect=3.5 → size=(49.00, 14.00)). 원래 값으로 되돌리면 원래 크기로 정확히 복귀.

### 4-7. 기존 게임플레이 회귀 없음 ✅

Play 모드에서 `EnemySpawner`가 정상적으로 몹을 스폰함을 확인(강제 호출로 6마리 스폰, 정상 동작),
UI(콤보/EXP/HP 텍스트 등)도 스크린샷에 정상 표시됨. 배경 추가와 무관한 기존 로직에 영향 없음.

### 4-8. 스코프 안내: 씬 저장 시 함께 반영된 무관한 변경 사항

`Gameplay.unity`를 저장하면서, 이번 작업과 무관하게 **씬 파일이 이미 최신 스크립트 정의를
따라가지 못하고 있던 것들**이 같이 정리됐습니다(제가 의도적으로 건드린 게 아니라 Unity가 씬을
재직렬화하며 자동으로 따라잡은 것):
- `RhythmUI`에 이미 존재하던 `defeatText`/`returnToMenuButton` 필드가 씬 YAML에 처음으로
  기록됨(스크립트엔 이미 있었지만 씬 저장이 이 필드 추가 이후 처음이었던 것으로 보임).
- `RhythmManager`의 `bpm` 필드가 씬에서 제거됨 - 현재 `RhythmManager.cs`에 `bpm` 필드가 이미
  없어서(다른 세션에서 제거된 것으로 보임), 씬에 남아있던 값이 정리된 것.
- UI Sprite 하나의 내부 직렬화 블록(`settingsRaw` 등)이 재작성됨 - Unity가 씬 저장 시 흔히
  정규화하는 캐시성 데이터로 보이며 기능적 영향은 없어 보입니다.

**셋 다 배경 타일링 기능과는 무관**하고, 되돌릴 필요가 있다고 판단되진 않지만(스크립트와 씬의
정합성을 오히려 맞추는 방향), 씬 diff 리뷰 시 참고하시라고 투명하게 남겨둡니다.

### 결론

전부 통과. `BackgroundTiler`가 실제로 씬에 배치되어 정상 동작 중이며, 사전 준비(1절)에서
누락됐던 텍스처 임포트 설정(Wrap Repeat, Single, **Full Rect** - 마지막 건 실측 중 추가 발견)과
프리팹/씬 배치까지 이번 세션에서 완료했습니다. 테스트 중 생성한 임시 GameObject와 스크린샷 파일은
모두 정리했습니다. git 조작(스테이징/커밋)은 하지 않았으며, 이 세션은 git 조작을 하지 않습니다
(사용자가 직접 스테이징/커밋 필요) - 특히 4-8절의 무관한 씬 변경사항은 커밋 전에 diff를 한번
훑어보시길 권합니다.
