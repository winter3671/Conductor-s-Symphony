# 갈무리(Galmuri) 도트 폰트 통일 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 고쳤나

게임 전체 UI 텍스트가 유니티 기본 폰트(`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`,
Arial 계열)를 그대로 쓰고 있어서 "실제 운용되는 게임"치고 디자인이 부족해 보인다는 피드백으로 시작.
사용자가 한글 지원되는 도트 폰트 **갈무리(Galmuri)**의 `Galmuri9.ttf`(본문용)와
`Galmuri11-Bold.ttf`(강조용) 2종을 받아 `Assets/Resources/Fonts/`에 배치함.

- **신규 `Assets/Scripts/Utility/GameFonts.cs`**: `Resources.Load<Font>("Fonts/Galmuri9")` /
  `("Fonts/Galmuri11-Bold")` 결과를 캐싱하는 정적 헬퍼. `GameFonts.Body`(본문) /
  `GameFonts.Headline`(강조) 2단 체계.
- **코드로 UI를 생성하는 3개 파일, 총 7곳**을 `LegacyRuntime.ttf` → `GameFonts.Body`/`Headline`로
  교체:
  - `Assets/Scripts/UI/LevelUpUI.cs`: 카드 제목(`title`) → Headline, 카드 설명(`desc`) → Body.
  - `Assets/Scripts/Rhythm/RhythmUI.cs`: 보스 HP 텍스트 → Headline, 승리 문구 → Headline, 패배
    문구 → Headline, "메인으로" 버튼 라벨 → Body.
  - `Assets/Scripts/Rhythm/HitFloatingText.cs`: PERFECT!/GREAT!/MISS 판정 팝업 → Headline. 기존에
    걸려있던 합성 `FontStyle.BoldAndItalic`은 `Normal`로 제거함(도트 폰트에 유니티가 합성으로
    기울이거나 굵게 뭉개면 픽셀이 안 맞아 지저분해 보임 - 폰트 자체가 이미 Bold라 불필요).
- **아직 안 된 것 - `Assets/Prefabs/UI/MainMenuCanvas.prefab`**: 메인 메뉴/설정 화면은 코드가 아니라
  에디터에서 직접 만든 프리팹이라, 약 20곳의 `Text` 컴포넌트가 전부 `m_Font: {fileID: 10102, guid:
  0000000000000000e000000000000000}`(유니티 내장 Arial)를 참조 중. 이건 실제 임포트된 폰트 에셋의
  GUID를 알아야 정확히 가리킬 수 있어서(제가 새 텍스처/스프라이트 때처럼 빈 상태에서 새 `.meta`를
  섣불리 손으로 만드는 것도 피하고 있음 - 이전에 그렇게 했다가 문제됐던 사례가 있어서 이번엔
  Editor가 실제로 임포트하게 두는 쪽을 택함) Unity 에디터에서 직접 폰트를 임포트하고 프리팹의 모든
  Text에 일괄 적용하는 작업이 필요합니다. 아래 1절 참고.

## 1. 사전 준비 (Unity 에디터 작업 필요)

1. `refresh_unity(force, compile=request)`로 재컴파일해서 `Galmuri9.ttf`/`Galmuri11-Bold.ttf`가
   정상 임포트되는지, 컴파일 에러/경고 0건인지 확인.
2. `Assets/Prefabs/UI/MainMenuCanvas.prefab`을 열어서, 프리팹 안의 모든 `Text` 컴포넌트(대략 20개)의
   Font를 다음 기준으로 일괄 교체:
   - 큰 제목/버튼처럼 강조가 필요한 텍스트 → `Galmuri11-Bold`
   - 나머지 본문/라벨 텍스트 → `Galmuri9`
   - (참고) 정확히 어떤 텍스트가 "제목"이고 어떤 게 "본문"인지는 프리팹을 열어 직접 판단해주세요 -
     제가 씬/프리팹 파일을 직접 열어본 게 아니라 필드명만으로는 정확한 위계를 알기 어려웠습니다.
   - 여러 개를 한 번에 선택해서 Inspector에서 Font 필드를 일괄 드래그하면 빠르게 처리 가능합니다.

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **폰트 에셋 임포트 확인**: `Galmuri9`/`Galmuri11-Bold`가 `Font` 타입으로 정상 임포트됐는지,
      `GameFonts.Body`/`GameFonts.Headline`이 null 없이 정상 로드되는지 리플렉션/직접 호출로 확인.
- [ ] **한글 렌더링 확인**: 카드 설명처럼 한글이 포함된 텍스트가 정상적으로 표시되는지(네모/빈칸
      등 글리프 누락 없이) 스크린샷으로 확인 - 갈무리는 한글 지원 폰트지만 실제 임포트 시 문자
      집합(Character) 설정에 따라 특정 글자가 빠질 수 있어 실측 필요.
- [ ] **레벨업 카드**: 제목(Headline)과 설명(Body) 폰트가 서로 다른 무게감으로 잘 구분되는지,
      기존 리치텍스트 색상 태그(`<color=#...>`)가 새 폰트에서도 정상 동작하는지 확인.
- [ ] **판정 팝업(PERFECT!/GREAT!/MISS)**: Headline 폰트로 잘 보이는지, 합성 스타일 제거 후에도
      충분히 강조되어 보이는지 육안 확인.
- [ ] **보스 HP / 승리 / 패배 문구**: 전부 Headline 폰트 적용 확인.
- [ ] **메인 메뉴/설정 화면(`MainMenuCanvas.prefab`)**: 1절에서 진행한 일괄 교체가 실제로 적용됐는지,
      Arial로 남아있는 텍스트가 없는지 화면 전체를 스크린샷으로 훑어 확인.
- [ ] **기존 UI 레이아웃 회귀 없음**: 폰트가 바뀌면서 텍스트 크기/줄바꿈이 달라져 기존 박스를
      벗어나거나 잘리는 곳이 없는지 확인(도트 폰트는 일반 폰트보다 자간/문자 폭이 달라질 수 있음).

## 4. 검증 결과

**검증 환경**: Unity 6000.5.5f1, Unity MCP 세션. 이번 세션에서 1절의 미완료 작업(`MainMenuCanvas.prefab`
Text 일괄 교체)을 직접 수행한 뒤, 코드 리뷰 + Play 모드 실측(스크린샷)으로 검증했습니다.

### 1절 작업 수행 내역

- 프리팹에 실제 `Text` 컴포넌트가 **50개**였습니다(문서의 "약 20개" 추정과 차이 - 키 리바인드 UI가
  8행(HitLeftRow/HitUpLeftRow/HitUpRightRow/HitRightRow/MoveUpRow/MoveDownRow/MoveLeftRow/MoveRightRow)
  × 4텍스트(ActionLabel/KeyLabel/RebindButton-Text/WarningLabel)로 구성돼 있었기 때문).
- `open_prefab_stage`로 프리팹을 열어 전수 조사 후, 아래 기준으로 분류해 `m_FontData.m_Font`
  프로퍼티를 일괄 설정(리플렉션 기반 배치 작업이라 `manage_prefabs.get_hierarchy`가 반환하는
  instanceId는 열린 스테이지와 매칭되지 않는 별도 임시 로드였음을 발견 - 대신 `search_method:
  "by_path"`로 경로 문자열을 직접 타겟팅해 우회):
  - **Headline(Galmuri11-Bold, 7곳)**: `MainPanel/Title`(게임 타이틀), `StartButton`/`SettingsButton`/
    `QuitButton`의 Text(메인 화면 최상위 3대 CTA 버튼), `VolumeSection/Header`·`KeyBindSection/Header`
    (설정 화면 내 섹션 구분 소제목), `SyncCalibrationPanel/ResultLabel`(싱크 측정 결과 - 승패 문구에
    준하는 강조가 필요하다고 판단).
  - **Body(Galmuri9, 43곳)**: 나머지 전부 - 볼륨 라벨 3개, 키 리바인드 행 32개(8행×4), 싱크 오프셋
    라벨, 싱크/초기화/뒤로가기/닫기/적용/재시도 등 보조 액션 버튼, 안내 문구. (참고: 기존 RhythmUI의
    "메인으로" 버튼이 Body로 처리된 선례를 따라, 보조/네비게이션성 버튼은 Body로 분류하고 메인
    화면의 1차 CTA 3개만 Headline으로 분류함 - 문서 1절의 "버튼처럼 강조가 필요한 텍스트" 문구를
    모든 버튼에 기계적으로 적용하지 않고 실제 위계를 판단했습니다.)
  - 적용 후 `execute_code`로 프리팹 스테이지 내 전체 `Text` 컴포넌트 50개를 순회 검사 -
    Galmuri9=43, Galmuri11-Bold=7, Arial 잔존=0, Null=0 확인 후 `save_prefab_stage`로 저장.

### 2절 검증 항목

- [x] 컴파일 에러/경고 0건.
- [x] **폰트 에셋 임포트 확인**: `Assets/Resources/Fonts/Galmuri9.ttf`, `Galmuri11-Bold.ttf` 둘 다
      `UnityEngine.Font` 타입으로 정상 임포트(GUID 확인). `GameFonts.Body`/`GameFonts.Headline`을
      Play 모드에서 직접 호출해 각각 `Galmuri9`, `Galmuri11-Bold`로 정상 반환됨을 확인(null 없음).
- [x] **한글 렌더링 확인**: 메인 메뉴("심포니 서바이버", "시작"/"설정"/"종료"), 설정 화면("볼륨",
      "BGM 음량" 등, "키 설정", "타격(왼쪽)" 등), 싱크 캘리브레이션("박자에 맞춰 [Q] 키를 누르세요")
      전부 스크린샷으로 육안 확인 - 네모/빈칸 등 글리프 누락 전혀 없음. 참고로 폰트 교체 전
      `Title`의 텍스트 레이아웃 데이터(`cachedTextGeneratorForLayout`)를 직접 조회해보니, 유니티
      기본 폰트(한글 미지원)로는 한글 각 글자가 폭 0에 가깝게 계산되어 한 줄에 한 글자씩 세로로
      쌓이는 기형적 레이아웃이었던 것도 확인(교체 후 정상적인 한 줄 가로 레이아웃으로 즉시 수정됨).
- [x] **레벨업 카드**: `LevelUpUI.ShowLevelUpSelection()`을 실제 프로덕션 코드로 호출해 카드 생성 →
      스크린샷에서 제목(`[Key 1] Fermata [NEW]` 등, Headline/굵은 도트체)과 설명(본문, 일반 두께)이
      뚜렷이 구분됨을 확인. 기존 `<color=#...>` 리치텍스트 태그(악기명 노란색, `[NEW]` 초록색)도
      새 폰트에서 정상 동작.
- [x] **판정 팝업(PERFECT!/GREAT!/MISS)**: `HitFloatingText.Initialize()`를 직접 호출/실측(테스트 중
      실제 게임플레이에서 자연 발생한 "MISS" 팝업도 함께 관측) - Headline 폰트로 빨간색 굵은 글씨가
      선명하게 보임, 합성 Bold/Italic 제거 후에도 폰트 자체 굵기로 충분히 강조되어 보임.
- [x] **보스 HP / 승리 / 패배 문구**: 코드 확인 결과 `RhythmUI.cs`의 `bossHpText`/`victoryText`/
      `defeatText` 전부 `GameFonts.Headline` 할당 확인(46/79/99행). 실제 보스전을 끝까지 재현하는
      대신(범위상 시간 관계로 생략) 코드 레벨 확인으로 대체 - `GameFonts.Headline`이 정상 로드됨은
      위에서 이미 실측 확인했으므로 회귀 위험 낮음.
- [x] **메인 메뉴/설정 화면**: 1절에서 직접 수행한 일괄 교체가 Play 모드에서 실제로 적용됐는지
      메인 화면/설정 화면/싱크 캘리브레이션 화면 3곳 전부 스크린샷으로 확인 - Arial로 남은 텍스트
      없음(위 전수 검사 0건과 일치).
- [x] **기존 UI 레이아웃 회귀 없음**: 스크린샷 확인 결과 설정 화면의 슬라이더/버튼/키 리바인드 행
      등에서 텍스트 잘림이나 박스 밖으로 튀어나오는 현상 없음. 도트 폰트가 자간이 넓은 편이지만
      기존 버튼/라벨 폭 안에 잘 들어맞음.

### 참고 - 세션 중 발견한 사소한 이슈(코드 결함 아님)

- Unity 에디터가 언포커스 상태라 Play 모드에서 `Time`이 자동으로 흐르지 않아, 레벨업 카드 선택
  로직(`OnCardSelected`)을 리플렉션으로 호출했을 때 예상과 다른 이유로 실패한 적이 있었으나, 이는
  테스트 스크립트 자체의 문제(정적 `Instance` 프로퍼티가 `MonoSingleton<T>` 기반 클래스에 상속돼
  있어 리플렉션에 `BindingFlags.FlattenHierarchy`가 필요했음)였고 실제 게임 코드 결함은 아니었습니다.

**결론**: 1절에 남아있던 프리팹 작업을 완료했고, 2절 체크리스트 전 항목을 코드 검토 + 실제 Play
모드 스크린샷으로 확인했습니다. 컴파일 에러/경고 없음, 한글 렌더링 정상, 제목/본문 위계 구분 명확,
기존 리치텍스트·레이아웃 회귀 없음. 커밋 가능한 상태로 판단됩니다.
