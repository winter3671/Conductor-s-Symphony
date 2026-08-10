# 레벨업 카드 3차 개선(위치 조정 + 폰트 확대 + 악기 설명 한글화) - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다. 1차(`archive/levelup_card_redesign_test_guide.md`)와
2차(`archive/levelup_card_redesign_v2_test_guide.md`)는 이미 PASS로 검증 완료됐습니다. 2차 검증에서
"좁은 해상도(1600×900 등)에서 카드 3장이 잘릴 수 있다"는 위험이 미확정으로 남아있으니, 이번
라운드에서 가능하면 같이 확인해주세요.

## 0. 무엇을 고쳤나

2차 검증 후 사용자가 스크린샷과 함께 준 피드백을 반영했습니다.

- **카드+타이틀 그룹을 세로로 살짝 위로 이동**: "카드가 화면 세로 중앙에 오면 좋겠다"는 요청 -
  `LevelUpUI.cs`에 `GroupVerticalShift = 60f` 상수를 추가해 카드 버튼과 "LEVEL UP!" 타이틀 둘 다
  같은 오프셋만큼 위로 이동시킴(서로의 상대적 간격은 유지). 이 김에, 카드 버튼의 앵커/피벗을
  `(0.5, 0.5)`로 **명시적으로 강제**하도록 추가했습니다 - 카드가 씬에 예전에 직접 만들어진
  오브젝트라는 걸 이미 알고 있었는데(1차 작업에서 발견), 앵커 값도 정확히 중앙이 아니었을 가능성을
  배제하기 위한 방어적 조치입니다.
- **"LEVEL UP!" 타이틀 스타일링**: 씬에 `TitleText`라는 이름으로 원래 있던 오브젝트인데, 지금까지
  `LevelUpUI.cs` 어디에서도 참조된 적이 없어 유니티 기본 폰트(Arial)/32pt 그대로였습니다.
  `EnsureLevelUpTitleStyling()`을 새로 추가해 이름으로 찾아 갈무리 폰트(`GameFonts.Headline`) +
  48pt로 확대, Outline도 추가.
- **폰트 크기 확대**: 테마 뱃지("방어"/"기동성" 등) 18→22pt, 설명 텍스트 24→30pt.
- **패시브 설명 줄바꿈**: `PassiveStatData.cs`의 8종 설명 문자열에서 "(최대 +N%)" 같은 괄호
  부분을 `\n`으로 둘째 줄로 내림(폰트가 커지면서 한 줄에 안 들어가 보일 수 있어서). 예:
  "모든 무기 피해량 +10%/Lv (최대 +50%)" → "모든 무기 피해량 +10%/Lv\n(최대 +50%)".
- **악기 설명 10종 전부 한글화**: `InstrumentPatternDatabase.cs`를 확인해보니 10종 악기의
  설명(`description`)이 **전부** 영어로 되어 있었습니다(사용자는 "일부"라고 표현했지만 실제로는
  10종 전부). 게임스러운 한글 문구로 전부 교체했습니다:
  - Drums: "360° Shockwave Beat Bang" → "360도 충격파로 주변을 강타"
  - Piano: "Auto-Target Chord Laser & Piano Cascade Volley" → "자동 조준 화음 레이저와 연속 연타"
  - Violin: "Orbiting Blades & Crescent Arc Slash" → "궤도를 도는 칼날과 초승달 베기"
  - Flute: "Mini Vortex (Release Pull) & Woodwind Swells" → "적을 끌어당기는 소용돌이와 목관의 파동"
  - FrenchHorn: "Sonic Brass Cannon Cone Knockback" → "부채꼴 음파포로 넉백"
  - Glockenspiel: "Star Fall on Highest HP Enemy" → "체력이 가장 높은 적에게 별똥별 낙하"
  - Cello: "Gravity Binding Slow Zone" → "중력으로 속박하는 슬로우 장판"
  - Timpani: "Timpani Cannon Mortar Impact" → "팀파니 포격으로 융단폭격"
  - Marimba: "Off-Beat Marimba Ricochet Wave" → "엇박에 튕겨나가는 마림바 파동"
  - Bell: "8-Direction Starlight Burst" → "8방향으로 터지는 별빛 폭발"
  - 추가로, `LevelUpUI.cs`가 악기 카드 설명 끝에 항상 덧붙이던 "(Dmg +X, Multi +Y)"도 영어로
    남아있던 걸 발견해 "(피해 +X, 투사체 +Y)"로 교체(패시브 카드 등 게임 내 다른 곳의 기존
    한글 용어와 통일).

## 1. 사전 준비 상태

- [x] `LevelUpUI.cs` 코드 변경 완료 (`GroupVerticalShift` 상수, `EnsureLevelUpTitleStyling()` 신규,
      카드 앵커 강제, 폰트 크기 조정, 한글 문구 교체)
- [x] `PassiveStatData.cs` 8종 설명 줄바꿈 완료
- [x] `InstrumentPatternDatabase.cs` 10종 설명 한글화 완료
- [ ] Unity 에디터 컴파일 확인 - **아직 안 됨**
- [ ] Play 모드 실측 - **아직 안 됨**

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **카드 세로 위치**: 레벨업 발생시켜 카드 3장이 화면 세로 중앙 부근에 오는지, "LEVEL UP!"
      타이틀이 카드 위에 적당한 간격을 두고 같이 위로 이동해 있는지 스크린샷으로 확인.
- [ ] **"LEVEL UP!" 타이틀 스타일**: 갈무리 폰트로 표시되는지, 크기가 눈에 띄게 커졌는지(32→48pt)
      확인.
- [ ] **테마 뱃지/설명 폰트 크기**: 하단의 "(위력)"/"(기동성)" 등과 카드 설명 텍스트가 이전보다
      커진 게 육안으로 확인되는지.
- [ ] **패시브 설명 줄바꿈**: 예를 들어 Sforzando 카드가 뜨면 "모든 무기 피해량 +10%/Lv"와
      "(최대 +50%)"가 두 줄로 나뉘어 표시되는지 확인.
- [ ] **악기 설명 한글화**: 악기 카드(예: Drums, Violin 등)가 후보로 뜨면 설명이 영어가 아니라
      한글로 표시되는지, "(피해 +X, 투사체 +Y)" 부분도 한글인지 확인. 10종 전부는 아니어도 몇 종만
      확인해도 충분.
- [ ] **레이아웃 겹침/잘림 없음**: 설명 폰트가 커지고 패시브 설명이 2줄이 되면서 카드 하단 패널
      영역을 벗어나거나 테마 뱃지와 겹치지 않는지 확인 - 이번 라운드에서 가장 주의 깊게 볼 부분.
- [ ] **[2차에서 넘어온 미확정 항목] 좁은 해상도에서 카드 3장이 화면에 들어오는지**: 가능하면
      Editor Game 뷰 해상도를 1600×900 이하로 낮춰서(2차 세션은 이 조작 수단이 없어 확인 못 함)
      카드가 잘리는지 확인 부탁드립니다. 여전히 불가능하면 계산상 위험만 재확인(`CardWidth*3 +
      CardSpacing*2 = 1770px` 대비 해당 해상도 폭)해서 보고해주세요.
- [ ] **카드 선택/일시정지 상호작용 회귀 없음**: 기존 동작이 여전히 정상인지 간단히 확인(로직은
      안 건드리고 레이아웃/텍스트만 변경했으므로 낮은 우선순위).

## 3. 참고 - 관련 코드 위치

- `Assets/Scripts/UI/LevelUpUI.cs` - `GroupVerticalShift` 상수, `EnsureLevelUpTitleStyling()`(신규),
  `EnsureCardVisualUpgrade()`의 카드 앵커 강제 + 폰트 크기, `ShowLevelUpSelection()`의 카드
  anchoredPosition.y 및 악기 설명 문구.
- `Assets/Scripts/Passive/PassiveStatData.cs` - 8종 설명 문자열.
- `Assets/Scripts/Instrument/InstrumentPatternDatabase.cs` - 10종 설명 문자열.
- 씬/프리팹 직접 편집 없음(전부 런타임 코드).

## 4. 검증 결과

(2026-08-10, Unity MCP 세션에서 `Gameplay.unity` Play 모드 실측 검증 + 발견된 버그 수정)

### 4.1 통과한 항목

- [x] **컴파일 에러/경고 0건**.
- [x] **카드 세로 위치**: 카드 3장이 화면 세로 중앙 부근에 오고 "LEVEL UP!" 타이틀이 카드 위에
      적당한 간격을 두고 같이 위로 이동해 있는 것을 스크린샷으로 확인.
- [x] **"LEVEL UP!" 타이틀 스타일**: 갈무리 폰트, 32→48pt로 확대된 것을 육안으로 확인.
- [x] **테마 뱃지/설명 폰트 크기**: "(기동성)"/"(지속)" 등과 카드 설명 텍스트가 이전 라운드보다
      커진 것을 육안으로 확인.
- [x] **패시브 설명 줄바꿈**: Sforzando 카드에서 "모든 무기 피해량 +10%/Lv"와 "(최대 +50%)"가
      두 줄로 정확히 나뉘어 표시되는 것을 확인.
- [x] **악기 설명 한글화**: Drums 카드에서 "360도 충격파로 주변을 강타 (피해 +0, 투사체 +0)"로
      완전히 한글로 표시되는 것을 확인(영어 잔존 없음). 코드 리뷰로 나머지 9종 문구도 가이드가
      설명한 한글 문구와 정확히 일치하는 것을 확인.
- [x] **카드 선택/일시정지 상호작용 회귀 없음**: Vivace 카드 선택 → `PassiveStatManager`의 Vivace
      레벨 0→1 반영, `IsSelectionActive=False`, `Time.timeScale=1` 복원 정상.

### 4.2 [핵심] 좁은 해상도 카드 잘림 - 실제로 재현됨, 수정 완료

2차 검증에서 미확정으로 남았던 항목을 이번엔 **직접 재현해서 확인**했습니다. Unity Editor의
`GameView`/`GameViewSizes` 내부 API를 리플렉션으로 조작해 Game 뷰 해상도를 실제로 1600×900으로
바꿔서(`Screen.width/height`가 실제로 1600/900을 반환하는 것까지 확인) 테스트:

- **수정 전 실측**: 왼쪽 카드(Fermata)와 오른쪽 카드(Resonance)가 화면 좌우로 잘려 보였고,
  "[Key N]" 라벨과 "LEVEL UP!" 타이틀이 전부 화면 위로 완전히 밀려나 아예 보이지 않았음(카드 높이
  855px가 화면 높이 900px의 95%를 차지해, 그 위에 얹히는 KeyLabel/타이틀이 있을 자리가 없었던 것).
  가이드가 우려한 문제가 그대로 재현됨.
- **원인**: `RhythmCanvas`의 `CanvasScaler`가 Constant Pixel Size라 `canvas.pixelRect`가 실제
  화면 해상도와 같아지는데, 카드 크기/간격/타이틀 오프셋이 전부 2560×1440 기준 고정 픽셀값이라
  더 작은 화면에서 그대로 화면 밖으로 넘침.
- **조치**: `CardWidth`/`CardHeight`/`CardSpacing`/`GroupVerticalShift`를 `Base*` 상수로 바꾸고,
  `ComputeCardScale()`(현재 `Screen.width/height`를 2560×1440 기준 대비 비율로 계산, 1.0 초과
  안 함)을 추가해 카드 크기·폰트·KeyLabel 위치·간격 전체에 곱함. `ShowLevelUpSelection()`을 열 때
  마다 `EnsureCardVisualUpgrade()`/`EnsureLevelUpTitleStyling()`을 다시 호출해 매번 현재 화면
  기준으로 재계산하도록 함(창 크기가 중간에 바뀌어도 다음 레벨업부터 자동으로 맞음).
- **수정 후 1차 재검증에서 발견된 2차 버그**: "LEVEL UP!" 타이틀이 원래 화면 세로 85% 고정 비율
  앵커를 썼는데, 이 비율은 화면 크기와 무관해서 scale을 아무리 곱해도 카드+KeyLabel이 차지하는
  절대 공간과의 간격이 좁은 화면에서 부족해져 "[Key 2]" 라벨과 타이틀이 서로 겹쳐 보임. 타이틀의
  앵커를 카드와 동일한 화면 중앙 기준으로 바꾸고, 카드 절반 높이+KeyLabel 공간+여유 간격을 직접
  계산해서 더하는 방식(`BaseTitleClearance`)으로 재수정.
- **최종 재검증**: 1600×900에서 카드 3장 모두 잘리지 않고, "[Key 1/2/3]"과 "LEVEL UP!"이 서로
  겹치지 않고 전부 화면 안에 표시되는 것을 스크린샷으로 확인. ✅ 이어서 2560×1440(설계 기준
  해상도)으로 되돌려 동일하게 확인 - 겹침/잘림 없이 이전과 동일하게 보여 **회귀 없음** 확인. ✅

### 4.3 종합 결론

가이드가 3차에 걸쳐 계속 미확정으로 남겨둔 좁은 해상도 문제를 이번에 직접 재현하고 근본적으로
수정했습니다(화면 크기에 비례하는 동적 스케일링 도입). 수정 과정에서 타이틀-KeyLabel 겹침이라는
2차 버그도 함께 발견해 수정했고, 두 해상도(1600×900, 2560×1440) 모두 재검증을 마쳤습니다. 이번
라운드에서 지적된 항목 중 남은 미해결 사항은 없습니다 - `archive/`로 이동 가능한 상태로 판단됩니다.
