# 레벨업 카드 2차 개선(추가 확대 + 레이아웃 재배치 + 패시브 아이콘 아트 연동) - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다. 직전 라운드(`archive/levelup_card_redesign_test_guide.md`
- 프레임 아트 연동 + 1차 확대 220x270→380x570 + 아이콘 미표시 버그 수정)는 이미 PASS로 검증
완료됐고, 이번 문서는 그 위에 사용자가 실제로 플레이해보고 준 2차 피드백을 반영한 작업입니다.

## 0. 무엇을 고쳤나

1차 검증 후 사용자가 스크린샷과 함께 준 피드백 4가지 + 별도로 준비해둔 패시브 아이콘 아트를
반영했습니다.

- **카드 추가 확대**: "가로/세로 각각 1.5배씩 더 커져도 괜찮다"는 요청으로 380×570 → **570×855**로
  확대(2:3 비율 유지). `CardSpacing`도 20→30으로 비례 확대. **주의**: `CardWidth*3 +
  CardSpacing*2 = 1770px`로, 1차 때(1180px)보다 훨씬 넓어짐 - 실제 Game 뷰/빌드 해상도가 좁으면
  카드 3장이 화면 밖으로 잘리거나 겹칠 수 있으므로 **이번 라운드에서 가장 먼저, 가장 꼼꼼히
  확인해야 할 항목**입니다. 잘린다면 사용자에게 실제 해상도를 확인하고 `CardWidth`/`CardSpacing`을
  조정해야 합니다.
- **"[Key N]" 라벨을 카드 밖으로 분리**: 기존엔 카드 프레임 안쪽 헤더에 제목과 같이 있었는데,
  사용자가 "카드 위에 따로 올라가면 좋겠다"고 요청 - 새 `cardKeyLabels[]` 배열/`KeyLabel`
  GameObject를 카드 버튼의 자식으로 추가하되, 앵커를 포인트 앵커(0.5,1) + `pivot(0.5,0)`으로 잡아
  카드 프레임 **바깥 위쪽**(anchoredPosition y=+14)에 표시되도록 함. 카드 자식이라 카드가 움직이면
  같이 따라다님.
- **테마 뱃지("방어"/"기동성" 등)를 하단 패널 맨 아래로 분리**: 기존엔 `[NEW]`/`[Lv.N]` 뱃지
  바로 뒤에 붙어 있었는데, 사용자가 "하단 텍스트 공간의 맨 아래로 옮기자"고 요청 - 새
  `cardThemeTexts[]`/`ThemeText`를 추가해 하단 패널 영역(세로 9~16%, 설명 텍스트보다 아래)에
  배치. 악기 카드는 테마 개념이 없어 빈 문자열(자동으로 안 보임).
- **카드 텍스트에 폰트 적용**: 지금까지 `LevelUpUI.cs`의 카드 텍스트들은 `Text.font`를 한 번도
  명시적으로 설정한 적이 없어(다른 파일들과 달리) 유니티 기본 폰트로 표시되고 있었음 - 제목/
  KeyLabel엔 `GameFonts.Headline`, 설명/테마 뱃지엔 `GameFonts.Body`를 적용(다른 UI 전체와 동일한
  갈무리 폰트 통일).
- **패시브 아이콘 실제 아트 연동**: 사용자가 나노바나나로 8종 패시브(Sforzando/Allegro/Crescendo/
  Vivace/Legato/Fermata/Resonance/Tuning) 도트 아이콘을 생성해 `Assets/Resources/Sprites/
  Passives/{Type}.png`(전부 1254×1254, 배경 제거 완료)에 저장 완료. `ShowLevelUpSelection()`의
  패시브 아이콘 로딩을 `Resources.Load<Sprite>($"Sprites/Passives/{type}")`로 먼저 시도하고,
  못 찾으면 기존 절차적 원형 플레이스홀더로 자동 폴백하도록 수정(악기 아이콘 로딩과 동일한
  패턴). `.meta`는 기존 UI 프레임/배경 아트와 동일한 컨벤션(Single/Full Rect Mesh)으로 8개 전부
  신규 작성함 - 아직 Unity 재임포트 확인 전.

## 1. 사전 준비 상태

- [x] 코드 변경 완료 (`LevelUpUI.cs`: `CardWidth`/`CardHeight`/`CardSpacing` 값 변경,
      `EnsureCardKeyAndThemeLabels()` 신규, `EnsureCardVisualUpgrade()`에 KeyLabel/ThemeText
      배치 + 폰트 적용 추가, `ShowLevelUpSelection()`의 텍스트/아이콘 조합 로직 갱신)
- [x] 패시브 아이콘 8종 원본 파일 확인 (전부 1254×1254 RGBA)
- [x] 패시브 아이콘 8종 `.meta` 신규 작성 완료 (미검증)
- [ ] Unity 에디터 컴파일 확인 - **아직 안 됨**
- [ ] Play 모드 실측 - **아직 안 됨**

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **패시브 아이콘 8종 텍스처 임포트**: `Sforzando`/`Allegro`/`Crescendo`/`Vivace`/`Legato`/
      `Fermata`/`Resonance`/`Tuning`.png가 전부 에러 없이 Sprite(2D and UI)/Single로 임포트되는지
      확인.
- [ ] **[최우선] 카드 3장이 화면 안에 들어오는지**: 570×855 카드 3장 + 간격이 실제 Game 뷰/화면
      폭 안에 잘리지 않고 들어오는지 스크린샷으로 확인. 잘리거나 겹치면 발견된 문제로 보고하고,
      가능하면 실제 해상도 수치도 같이 알려주세요(다음 라운드에서 크기 재조정 근거로 씀).
- [ ] **"[Key N]" 위치**: 카드 프레임 안이 아니라 카드 바로 위 바깥쪽에 떠 있는지 확인.
- [ ] **테마 뱃지 위치**: "방어"/"기동성" 등 텍스트가 `[NEW]`/`[Lv.N]` 뱃지 옆이 아니라 하단
      설명 텍스트 아래쪽에 별도로 표시되는지 확인(패시브 카드만 - 악기 카드는 원래 표시 안 함,
      빈 텍스트라 아무것도 안 보이는 게 정상).
- [ ] **폰트 적용**: 카드의 제목/설명/KeyLabel/테마 뱃지 전부 갈무리 폰트(도트 폰트)로 보이는지
      확인 - 유니티 기본 Arial류 폰트가 남아있지 않은지.
- [ ] **패시브 아이콘 실제 아트 표시**: 패시브 카드(예: Sforzando/Vivace 등)에 원형 플레이스홀더가
      아니라 실제 도트 아이콘(메트로놈, 날개 부츠 등)이 표시되는지 확인 - 8종 전부는 아니어도
      후보로 뜨는 몇 종만 확인해도 충분. 색조 틴트 없이 원본 색 그대로 나오는지도 확인(악기
      아이콘과 동일하게 `Color.white`로 로드하도록 코드 작성함).
- [ ] **레이아웃 겹침 없음**: 제목/설명/테마 뱃지/아이콘이 카드 프레임의 장식 요소나 서로와
      겹쳐서 안 읽히는 부분이 없는지 확인(1차 검증 때 패시브 타이틀 3줄 넘침 버그가 있었으니,
      이번에도 특히 테마 뱃지가 설명 텍스트와 겹치지 않는지 꼼꼼히 확인).
- [ ] **카드 선택/일시정지 상호작용 회귀 없음**: 카드 선택, ESC 가드 등 기존 동작이 여전히
      정상인지 간단히 확인(레이아웃만 바뀌고 로직은 안 건드렸으므로 낮은 우선순위).

## 3. 참고 - 관련 코드 위치

- `Assets/Scripts/UI/LevelUpUI.cs` - `CardWidth`/`CardHeight`/`CardSpacing` 상수,
  `EnsureCardKeyAndThemeLabels()`(신규), `EnsureCardVisualUpgrade()`(KeyLabel/ThemeText 배치 +
  폰트 적용 추가), `ShowLevelUpSelection()`(텍스트/아이콘 조합 로직).
- `Assets/Resources/Sprites/Passives/{8종}.png` + `.meta` (신규).
- 씬/프리팹 직접 편집 없음(전부 런타임 코드).

## 4. 검증 결과

(2026-08-10, Unity MCP 세션에서 `Gameplay.unity` Play 모드 실측 검증)

### 4.1 통과한 항목

- [x] **컴파일 에러/경고 0건**.
- [x] **패시브 아이콘 8종 텍스처 임포트**: 8개 전부 에러 없이 Sprite(2D and UI)/Single로 임포트됨
      (`spriteMode:1`, `spriteMeshType:0`, `textureType:8` - 기존 UI 아트 컨벤션과 동일).
- [x] **[최우선] 카드 3장이 화면 안에 들어오는지**: 스크린샷 확인 결과 3장(Crescendo/Sforzando/
      Vivace) 모두 잘리지 않고 좌우 여백을 두고 정렬됨. 실측: 현재 Game 뷰 해상도
      `Screen.width=2560`, `CanvasScaler`는 Constant Pixel Size(`scaleFactor=1`)라
      `canvas.pixelRect`가 실제 해상도와 동일. `CardWidth*3+CardSpacing*2=1770px`이므로 2560px
      기준으로는 좌우 395px씩 여유. ✅ (단, 아래 4.2 참고 - 더 좁은 해상도에서의 위험은 남아있음)
- [x] **"[Key N]" 위치**: 카드 프레임 바깥 위쪽에 별도로 떠 있는 것을 스크린샷으로 확인(노란색 +
      Outline, 헤더 장식과 안 겹침).
- [x] **테마 뱃지 위치**: "(확장)"/"(위력)"/"(기동성)" 텍스트가 설명 텍스트 아래쪽 하단 패널
      맨 아래에 별도로 표시되는 것을 확인. 씬 조회로 `themeText[0].text = "(확장)"` 확인.
- [x] **폰트 적용**: `titleFont=Galmuri11-Bold`, `descFont=Galmuri9`, `keyFont=Galmuri11-Bold`,
      `themeFont=Galmuri9` - 전부 갈무리 폰트로 확인, 유니티 기본 Arial 없음.
- [x] **패시브 아이콘 실제 아트 표시**: Crescendo(파란 파동 원), Sforzando(붉은 글러브), Vivace(녹색
      날개 부츠) 3종을 스크린샷으로 확인 - 원형 플레이스홀더가 아니라 실제 도트 아이콘이 원본
      색 그대로(틴트 없이) 표시됨. ✅
- [x] **레이아웃 겹침 없음**: 제목/아이콘/설명/테마 뱃지가 서로 또는 프레임 장식과 겹치지 않고
      깔끔하게 분리되어 표시됨(1차 검증 때의 3줄 타이틀 넘침 문제는 이번 레이아웃 재구성으로
      자연히 해소됨 - 제목이 다시 한 줄로 단순화됨).
- [x] **카드 선택 회귀 없음**: Crescendo 카드 선택 → `PassiveStatManager`의 Crescendo 레벨이
      0→1로 실제 반영, `IsSelectionActive=False`, `Time.timeScale=1`로 정상 복원.

### 4.2 참고 - 더 좁은 해상도에서의 카드 폭 위험 (실측 불가, 계산상 위험 존재)

가이드가 최우선으로 확인하라고 한 항목과 관련해, **이 Unity MCP 세션에서는 Editor Game 뷰 해상도를
실제로 좁게 바꿔서 테스트할 수 없었습니다**(`Screen.SetResolution()`을 Play 모드 중 호출해봤지만
Editor Game 뷰에는 반영되지 않고 `Screen.width`가 계속 2560으로 유지됨 - Editor의 Game 뷰 해상도는
런타임 API가 아니라 에디터 UI의 드롭다운으로만 바뀌는데, 이번 세션에 그 컨트롤을 조작할 수단이
없었음).

계산으로 확인한 사실:
- 프로젝트 기본 빌드 해상도(`ProjectSettings.asset`): **1920×1080**.
- `RhythmCanvas`의 `CanvasScaler`는 Constant Pixel Size(`scaleFactor=1`) - 해상도가 바뀌어도 UI
  크기가 그대로라, `canvas.pixelRect`가 항상 실제 화면 해상도와 같아짐.
- `CardWidth*3 + CardSpacing*2 = 1770px`.
- 1920×1080 기준으로는 좌우 75px씩 여유가 있어 **잘리지는 않을 것**으로 계산되나, 여유가 크지
  않음. 1770px보다 좁은 해상도(예: 1600×900, 1366×768 등 저해상도 노트북/창모드)에서는 **실제로
  카드가 화면 밖으로 잘릴 수 있음** - 이건 이번 카드 작업이 새로 만든 문제가 아니라 `RhythmCanvas`
  전체가 원래부터 Constant Pixel Size 방식을 쓰고 있었던 것의 연장선(카드가 유난히 넓어지면서
  위험이 커진 것).
- **권장**: 실제 빌드나 창 크기를 줄인 상태에서 한 번 더 확인 필요. 문제가 확인되면 (a)
  `CanvasScaler`를 Scale With Screen Size로 바꾸거나 (b) 좁은 해상도에서만 `CardWidth`를
  동적으로 줄이는 방향을 검토.

### 4.3 종합 결론

가이드 2절의 항목 대부분을 실측으로 확인했고 버그는 발견되지 않았습니다. 다만 4.2의 좁은 해상도
카드 잘림 위험은 이번 세션 환경 제약으로 직접 재현/반증하지 못해 "위험 있음, 미확정"으로
남겨둡니다 - 실제 빌드에서 한 번 더 확인 후 `archive/`로 옮기는 것을 권장합니다.
