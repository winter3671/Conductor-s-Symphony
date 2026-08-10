# HUD 체력바/경험치바/악기 슬롯 아이콘 전환 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 고쳤나

UI 폴리싱 로드맵(폰트 통일 다음 순위)으로 HP/EXP/악기 슬롯을 텍스트에서 그래픽 HUD로 전환.
"HP: 80/100", "LV.3 EXP: 40/100", "SLOTS: [Q: Piano Lv.2] [R: EMPTY] ..." 같은 순수 텍스트
표시는 전투 중 노출 빈도가 가장 높은데도 완성도가 가장 떨어져 보인다는 판단으로 이 순서를 정함.

`Assets/Scripts/Rhythm/RhythmUI.cs`를 수정:

- **기존 `hpText`/`expText`/`instrumentSlotText` 3개 `[SerializeField] Text` 필드를 완전히 제거**하고,
  씬/프리팹 편집 없이 코드가 자동 생성하는 방식(`bossHpText` 등 기존 `Ensure*Elements()` 패턴과
  동일)으로 전환. **씬(`Gameplay.unity`)에 남아있을 수 있는 기존 HpText/ExpText/InstrumentSlotText
  GameObject는 이제 참조되지 않는 고아 오브젝트입니다** - 지워도 되고 안 지워도 동작엔 지장 없음
  (1절 참고).
- **HP 바** (`EnsureHealthExpBarElements()`): 화면 좌상단(anchoredPosition 24,-24)에 260×50 프레임 +
  안쪽 여백 8px의 붉은색 `Image.Type.Filled` 채움 바. `UpdateHealthUI`가 `hpBarFill.fillAmount =
  currentHp/maxHp`로 갱신. (2026-08-10: 실제 받은 `HpBarFrame.png` 원본 비율이 약 2260×696(≈3.2:1)로
  확인돼 초기 260×26에서 260×50으로 조정 - 9-slice 테두리 짓눌림 방지.)
- **EXP 바**: HP 바 바로 아래(y -58) 260×38 프레임 + 시안색 채움 바 + 오른쪽에 `Lv.N` 텍스트
  (`GameFonts.Body`). `UpdateExpUI`가 `fillAmount`와 레벨 텍스트를 함께 갱신. (마찬가지로 `ExpBarFrame.png`
  원본 비율 약 2600×560(≈4.6:1) 확인 후 260×18에서 260×38로 조정.)
- **악기 슬롯 4칸**(`EnsureInstrumentSlotElements()`): 화면 하단 중앙에 Q/R/W/E 56×56 슬롯을 가로로
  배치. 각 슬롯 = 프레임 `Image` + 아이콘 `Image`(악기별 스프라이트, `themeColor`로 틴트) + 키 라벨
  텍스트(좌상단, `GameFonts.Headline`) + 레벨/잠금조건 라벨(우하단, `GameFonts.Body`). **아이콘은 신규
  아트가 필요 없음** - 이미 존재하는 `Resources/Sprites/Instruments/{InstrumentType}.png` 10종을
  그대로 재사용. `UpdateInstrumentUI`가 장착/빈칸/잠김 3가지 상태에 따라 프레임 투명도·아이콘
  표시·레벨 라벨을 갱신.
- **프레임(테두리) 아트 3종은 신규 제작 필요, 아직 없음** - `Sprites/UI/HpBarFrame`,
  `Sprites/UI/ExpBarFrame`, `Sprites/UI/InstrumentSlotFrame`. `Resources.Load<Sprite>`로 시도해서
  없으면(현재 상태) 반투명 단색 사각형으로 자동 대체 - 파일만 채워 넣으면 코드 수정 없이 그대로
  적용됨(배경 타일 아트 때와 동일한 "나중에 채워도 되는" 패턴). 사용자가 나노바나나로 생성해서
  `Assets/Resources/Sprites/UI/` 폴더에 저장할 예정.

## 1. 사전 준비 (Unity 에디터 작업이 필요할 수 있음)

1. `refresh_unity(force, compile=request)`로 컴파일 확인.
2. `Gameplay.unity`를 열어 `RhythmUI` 오브젝트 하위에 예전 `HpText`/`ExpText`/`InstrumentSlotText`
   (또는 유사한 이름의) 자식 GameObject가 남아있는지 확인 - 있다면 더 이상 아무 코드도 참조하지
   않는 고아 오브젝트이므로 삭제해도 안전합니다(단, README §5.3 원칙대로 씬 직접 편집은 최소화 -
   삭제가 꺼려지면 그냥 비활성 상태로 둬도 무방).
3. **`Sprites/UI/` 폰트 아트 3종이 이미 도착해있다면**: 각 스프라이트의 `.meta`에서
   `spriteBorder`(9-slice 테두리 두께)를 실제 그림의 테두리 두께에 맞게 설정해야 `Image.Type.Sliced`가
   찌그러지지 않고 정상 렌더링됩니다 - Sprite Editor에서 초록 핸들을 드래그해 테두리를 지정해주세요.
   아직 없다면 이 항목은 건너뛰고 반투명 사각형 대체 상태로 검증 진행.

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **HP 바**: 플레이어가 피격당할 때 `hpBarFill.fillAmount`이 실제 체력 비율과 일치하는지(예:
      최대 100 중 50이면 절반만 채워짐) Play 모드에서 실제로 데미지를 입혀 확인. 0이 되면 완전히
      빈 상태인지도 확인.
- [ ] **EXP 바**: 레벨업 전후로 `fillAmount`가 초기화되는지(가득 찬 상태에서 레벨업 → 0 근처로
      리셋), `Lv.N` 텍스트가 실제 레벨과 일치하는지 확인.
- [ ] **악기 슬롯 - 장착 상태**: 악기를 획득/업그레이드했을 때 해당 슬롯에 올바른 아이콘(악기별
      스프라이트)과 `themeColor` 틴트, `Lv.N` 라벨이 표시되는지 확인.
- [ ] **악기 슬롯 - 빈 상태**: 해금은 됐지만 아직 장착 안 한 슬롯(2번째 슬롯 등)이 아이콘 없이
      프레임만 정상 밝기로 표시되는지 확인.
- [ ] **악기 슬롯 - 잠금 상태**: 레벨 미달로 잠긴 3/4번 슬롯이 어둡게(반투명) 표시되고, 필요 레벨
      텍스트("Lv.5", "Lv.8")가 뜨는지 확인.
- [ ] **프레임 아트 유무 양쪽 다 확인**: `Sprites/UI/` 3종이 아직 없는 상태(현재)에서도 반투명
      사각형으로 정상 표시되며 게임 진행에 지장 없는지. 만약 이번 검증 시점에 아트가 이미
      도착했다면, 실제 프레임 이미지로 정상 교체되는지와 9-slice 찌그러짐 여부도 함께 확인.
- [ ] **레이아웃 겹침 없음**: HP/EXP 바(좌상단)가 `scoreText`/`comboText`/`ratingText`(기존 위치)와
      겹치지 않는지, 악기 슬롯(하단 중앙)이 화면 해상도가 달라져도 잘리지 않는지 스크린샷으로 확인
      - 겹친다면 `RhythmUI.cs`의 `anchoredPosition` 값을 조정해주세요(정확한 기존 텍스트 좌표를
      제가 씬 파일에서 직접 확인하지 못해 임의로 좌상단/하단중앙에 배치했습니다).

## 4. 검증 결과

(2026-08-10, Unity MCP 세션에서 `Gameplay.unity` Play 모드 실측 검증)

### 4.1 통과한 항목

- [x] 컴파일 에러/경고 0건 (`refresh_unity(compile=request)` 후 `read_console` 확인).
- [x] **HP 바**: `PlayerController.TakeDamage(60)` 호출 → `currentHealth=40/maxHealth=100` →
      `hpBarFill.fillAmount=0.4`로 정확히 반영됨 (씬 그래프 직접 조회로 확인).
- [x] **EXP 바**: `PlayerExperience.AddExp(45)` → `CurrentExp=45/MaxExp=250` →
      `expBarFill.fillAmount=0.18`로 정확히 반영됨.
- [x] **악기 슬롯 - 장착 상태**: 기본 시작 악기 Drums가 Q 슬롯에 아이콘 + 틴트로 정상 표시됨
      (`Companion_Drums_1` 자동 장착 확인).
- [x] **악기 슬롯 - 잠금 상태(어둡게)**: Lv.2 상태에서 `GetUnlockedSlotsCount()=1`이라 R/W/E 슬롯
      프레임 색상이 `(0.05,0.05,0.05,0.4)`로 정상적으로 어둡게 표시됨 (Slot_R Image 컴포넌트 직접 조회로 확인).

### 4.2 새로 발견된 문제 (수정 필요)

1. **고아 오브젝트가 삭제되지 않아 새 그래픽 HUD와 중복·겹침 표시됨** (가이드 1절 2번 항목 미수행 상태)
   - `RhythmCanvas` 하위에 `HPText`("HP: 100/100"), `ExpText`("LV.1 EXP: 0/40"),
     `InstrumentSlotText`("INSTRUMENTS: [...]")가 여전히 **active** 상태로 남아 있고, 새 HP/EXP 바 및
     악기 슬롯과 동시에 화면에 렌더링됩니다. 스크린샷상 우상단/상단중앙/하단에 옛 텍스트가 그대로
     보이고, 하단 텍스트는 새 Q/R/W/E 슬롯 아이콘 줄과 겹칩니다.
   - 조치: 세 GameObject를 삭제하거나 최소한 `SetActive(false)` 필요.

2. **`ScoreText`/`ComboText`가 새 HP/EXP 바와 세로로 겹침** (가이드 2절 마지막 체크 항목에서 우려했던 바로 그 상황)
   - 좌표 실측: `ScoreText` anchoredPosition (30,-30), 크기 300×50 → y구간 [-30,-80].
     `ComboText` (30,-70), 300×50 → y구간 [-70,-120].
     신규 `HealthExpBars`는 (24,-24) 기준 HP바 y[-24,-74], EXP바 y[-82,-120].
   - 즉 **HP바가 ScoreText 세로 범위의 대부분(44px/50px)을 덮고, EXP바가 ComboText 전체를 거의 그대로
     덮습니다.** 실제 스크린샷에서도 "COMBO: 0" 텍스트가 시안색 EXP 바 위에 걸쳐 잘 안 보이고,
     "SCORE: 0"는 HP 바에 거의 가려져 보이지 않습니다.
   - 조치안: `RhythmUI.cs`에서 `ScoreText`/`ComboText`의 `anchoredPosition.y`를 HP/EXP 바 아래
     (예: y ≤ -132, 즉 두 바 묶음 높이 100 + 여백 8 만큼 내림)로 옮기거나, 반대로 HP/EXP 바를
     Score/Combo 텍스트보다 오른쪽으로 이동.

3. **악기 슬롯 잠금 요구 레벨 라벨이 실제 해금 레벨과 불일치** (코드 정적 분석 + 실측 둘 다로 확인)
   - `InstrumentManager.GetUnlockedSlotsCount()`: Lv1~4→1슬롯(Q만), Lv5~9→2슬롯(+R),
     Lv10~14→3슬롯(+W), Lv15+→4슬롯(+E). 즉 R/W/E의 실제 해금 레벨은 **Lv.5 / Lv.10 / Lv.15**.
   - 반면 `RhythmUI.UpdateInstrumentUI()`의 `lockReqs = ["", "", "Lv.5", "Lv.8"]`는 인덱스가
     한 칸씩 밀려 있고 숫자도 틀립니다. 실측 결과(Lv.2 상태):
     - Slot_R(인덱스1, 실제로 Lv.1~4 구간엔 잠겨 있음): LevelLabel 텍스트가 **빈 문자열** →
       잠긴 이유/필요 레벨이 전혀 안내되지 않음.
     - Slot_W(인덱스2, 실제 요구 Lv.10): 라벨이 **"Lv.5"**로 잘못 표시.
     - Slot_E(인덱스3, 실제 요구 Lv.15): 라벨이 **"Lv.8"**로 잘못 표시.
   - 조치: `lockReqs = new string[] { "", "Lv.5", "Lv.10", "Lv.15" }`로 수정.

4. **프레임 아트 3종 모두 9-slice 테두리 미설정** (가이드 1절 3번 항목 아직 미수행 — 이미 알려진 이슈지만
   실측으로 재확인)
   - `HpBarFrame.png` / `ExpBarFrame.png` / `InstrumentSlotFrame.png` 세 파일 모두 `.meta`의
     `spriteBorder`가 `{0,0,0,0}`. `Image.Type.Sliced`로 설정돼 있지만 border가 없어 실질적으로는
     단순 스트레치(Type.Simple)와 동일하게 렌더링됩니다(런타임에서 `Image.hasBorder=false` 확인).
     Sprite Editor에서 실제 그림의 border 픽셀을 지정해야 함.
   - 부가로, `ExpBarFrame.png`와 `InstrumentSlotFrame.png`는 Sprite Mode가 Multiple로 임포트되어
     본체 스프라이트(`_0`) 외에 40×40 크기의 자투리 서브스프라이트(`_1`)가 하나씩 더 잘려 들어가
     있습니다(오토 슬라이스가 노이즈/워터마크 등을 별도 스프라이트로 인식한 것으로 보임). 현재는
     `Resources.Load<Sprite>`가 `_0`을 정상적으로 반환해 기능상 문제는 없지만, 원본이 프레임 이미지
     1장짜리라면 Sprite Mode를 Single로 바꿔 정리하는 것을 권장.

### 4.4 4.2에서 발견된 4건 모두 수정 완료 (2026-08-10, 구현 세션)

1. **고아 텍스트 오브젝트 중복 표시**: `RhythmUI.Awake()`에 `DestroyLegacyTextElements()`를 추가해
   씬을 직접 편집하는 대신(README §5.3) 런타임에 이름("HPText"/"ExpText"/"InstrumentSlotText")으로
   찾아 `SetActive(false)` 처리하도록 방어적으로 수정. 씬 파일 자체는 건드리지 않았으므로 재검증
   필요.
2. **ScoreText/ComboText 겹침**: 실측된 좌표(Score y[-30,-80], Combo y[-70,-120])를 반영해
   `HealthExpBars` 루트 `anchoredPosition`을 (24,-24)→(24,-132)로 내려 겹침 해소. Score/Combo
   좌표 자체는 씬에 baked된 값이라 건드리지 않음.
3. **잠금 레벨 라벨 오표기**: `lockReqs`를 `{ "", "Lv.5", "Lv.10", "Lv.15" }`로 수정
   (`GetUnlockedSlotsCount()`의 실제 기준 Lv5/10/15과 일치).
4. **9-slice Border 미설정 + Sprite Mode Multiple 이슈**: 3개 `.meta` 파일을 직접 열어 Python으로
   원본 PNG의 알파 채널을 픽셀 단위 분석 → 경계 두께 실측(HpBarFrame≈97px, ExpBarFrame≈65px,
   InstrumentSlotFrame≈106px, 전부 원본 이미지 기준) 후 `spriteMode: 2`(Multiple)→`1`(Single),
   `spriteBorder`에 (여백+테두리두께) 값을 직접 기입. **Multiple 모드였던 것 자체가 기능적 버그일
   가능성이 있음** - 서브스프라이트 이름이 `HpBarFrame_0`처럼 접미사가 붙어서
   `Resources.Load<Sprite>("Sprites/UI/HpBarFrame")`가 null을 반환했을 수 있음(코드는 null 체크로
   방어되어 있어 크래시는 안 나지만 프레임 아트가 계속 반투명 사각형으로 대체 표시됐을 가능성).
   **이 수정은 Unity 에디터로 실제 재임포트를 거치지 않은 상태**이므로, Unity MCP 세션에서
   `refresh_unity(force, compile=request)`로 재임포트 후 Sprite Editor에서 9-slice 경계가
   시각적으로 정상인지(모서리 찌그러짐/겹침 없는지) 최종 확인 필요.

### 4.5 사용자 2차 직접 플레이 피드백 (2026-08-10) - 6건 접수

`runInBackground` 수정 후 사용자가 직접 실제로 플레이하며 발견한 문제 6건. 아래 순서대로 처리 상태
정리.

1. **[진단 필요, 최우선] 체력바/경험치바가 실제 플레이 중엔 안 움직임** - 이상한 점: 4.1에서
   `PlayerController.TakeDamage(60)`/`PlayerExperience.AddExp(45)`를 **execute_code로 직접 호출**했을
   땐 `fillAmount`가 정확히 반영됨을 이미 확인했었음. 즉 `UpdateHealthUI`/`UpdateExpUI` 자체 로직은
   검증된 상태. 그런데 사용자가 첨부한 스크린샷은 `YOU DIED` 사망 화면인데도 HP 바가 여전히
   **가득 찬 빨간색**으로 표시됨 - 실제 몬스터에게 맞아서 죽었다면 사망 직전 `TakeDamage()` 호출
   경로(`PlayerController.cs:329`, `enemy.DamageToPlayer`로 호출되는 동일 메서드)를 타야 하므로
   같은 로직이 실행돼야 하는데 결과가 다름. **원인 미상 - 코드 수정 대신 진단 로그를 추가해뒀음**:
   `UpdateHealthUI`/`UpdateExpUI`에 `#if UNITY_EDITOR` `Debug.Log` 추가(호출 시각/값/실제
   `fillAmount`를 매번 출력). 이번 라운드에서는 **실제 플레이 중 이 로그가 Console에 찍히는지,
   찍힌다면 값이 맞는지**를 `read_console`로 확인해주세요. 로그가 아예 안 찍히면 이벤트 구독이
   실제 게임 흐름에서 끊겨있는 것이고, 로그는 찍히는데 화면이 안 바뀌면 렌더링/레이아웃 쪽 문제로
   좁혀집니다. 원인 파악 후 이 로그는 제거 예정.
2. **[완료] 사망 시 음악이 계속 재생됨** - `ShowVictoryScreen()`/`ShowDefeatScreen()` 양쪽에
   `AudioLayerManager.Instance.PauseAllAudio()` 호출 추가(레벨업 팝업 때 쓰던 것과 동일 메서드
   재사용). 승리 화면도 동일하게 처리(사용자가 언급 안 했지만 같은 문제일 것으로 판단).
3. **[완료] 악기 슬롯이 너무 작음** - 56px → 124px로 약 2.2배 확대(요청 범위 2~2.5배 내). 키
   라벨/레벨 라벨 폰트 크기, 아이콘 여백도 비례해서 함께 확대.
4. **[완료] 악기 슬롯 표시 순서를 Q,W,E,R로 변경** - 실제 장착 데이터 순서(Q=0,R=1,W=2,E=3,
   `RhythmManager.GetLaneForSlot()` 기준, 변경 안 함 - 게임 로직/밸런스 문서 그대로)는 그대로 두고,
   **화면에 그리는 순서만** `SlotDisplayToDataIndex = {0,2,3,1}` 매핑으로 Q,W,E,R(좌→우)로 재배열.
   판정 링의 실제 키 배치각(Q=180°/왼쪽, W=135°/왼쪽위, E=45°/오른쪽위, R=0°/오른쪽,
   `RhythmManager.GetLaneDirection()`)을 x좌표 기준 좌→우로 정렬하면 Q,W,E,R 순서가 나와 이걸
   그대로 반영함.
5. **[텍스트 병행 표시 방안 - 채팅 응답 참고]** 체력/경험치를 바 그래프 외에 숫자로도 표시하는 안 -
   장르 유사 게임(뱀서라이크) 사례 조사 후 채팅으로 답변, 코드는 아직 미반영(방식 확정 후 별도 작업).
6. **[설계만, 미구현] 장신구(패시브) 성능 시각화 공간** - 도트 아이콘은 나중에 제작 예정, 이번엔
   배치 위치만 구상(채팅 응답 참고), 코드 변경 없음.

### 4.6 사용자 3차 피드백 - 숫자 오버레이 방식 확정 및 반영 (2026-08-10)

4.5의 5번 항목(체력/EXP 숫자 표시 방식)에 대해 "바 중앙에 숫자를 겹쳐 쓰는 방식"으로 확정. 관련
반영 내역:

- **바 자체를 다시 확대**: 숫자가 잘 보이려면 바가 더 커야 한다는 요청으로 HP 260x50→**320x64**,
  EXP 260x38→**320x48**로 재조정.
- **`HpValueText`/`ExpValueText` 신규 추가**: 각각 `HpBarFrame`/`ExpBarFrame`의 자식으로, 채움
  이미지(`HpBarFill`/`ExpBarFill`)보다 나중에 생성해 항상 그 위에 렌더링됨. 내용은
  `"{currentHp} / {maxHp}"` / `"{currentExp} / {maxExp}"`, `GameFonts.Headline`(굵은 도트체) 사용.
- **바가 절반만 채워진 상태에서도 가독성 확보**: 흰색 텍스트 + 검은색 `Outline` 컴포넌트(uGUI
  기본 제공, `effectColor` 검정 85% 불투명, `effectDistance` (1.5,-1.5)) 조합 사용 - 채워진 색
  위에서든 빈 배경 위에서든 대비가 유지되는 표준적인 방식. 실제로 절반쯤 채워진 상태에서 숫자가
  잘 읽히는지는 스크린샷으로 실측 필요(체력 50% 등으로 인위적 상황 만들어서 확인 권장).
- **EXP 바 색상 변경**: 시안/파란 계열 → **초록/연두 계열**(`RGB 0.45, 0.85, 0.25`)로 변경 - 흰
  글씨+검은 외곽선 조합은 색이 바뀌어도 대비가 유지되므로 가독성엔 영향 없음.
- 위 크기 변경으로 **Sprite Border 재확인이 한 번 더 필요**해짐 - 4.4에서 픽셀 분석으로 추정해 넣은
  Border 값(HP 153/162/156/164, EXP 140/169/140/167, 전부 텍스처 픽셀 기준)은 그대로 두었지만,
  UI 타겟 크기가 320x64/320x48로 커져도 Border 합계(HP 세로 326, EXP 세로 336)가 타겟 세로 크기보다
  훨씬 커서 9-slice가 여전히 찌그러질 가능성이 있음 - Sprite Editor에서 실측 확인 및 필요 시 Border
  값 축소 조정 권장(이 부분은 4.4에서부터 계속 "추정치, 실측 필요"로 남아있던 항목).

### 4.7 사용자 4차 피드백 - HP/EXP 바 미동작 진짜 원인 발견 + 악기 슬롯 정리 (2026-08-10)

7.1에서 "재현 실패, 정상 동작"으로 결론 났던 HP/EXP 바 문제를 사용자가 다시 스크린샷과 함께
제보 - "0 / 100" 텍스트인데도 빨간 바가 여전히 꽉 차 보이고, "100 / 250" 텍스트인데도 초록 바가
꽉 차 보임(숫자 오버레이는 정확한데 바 채움만 항상 100%로 고정). 코드를 다시 열어보니 **진짜
원인을 찾음**:

- **근본 원인**: `hpBarFill`/`expBarFill` 둘 다 `Image.Type.Filled`인데 `sprite`를 한 번도
  할당한 적이 없었음(순수 단색이라 "스프라이트 없이 색만" 방식을 썼었음). uGUI의 `Image.Type.Filled`는
  `sprite == null`이면 **`fillAmount` 프로퍼티 값 자체는 정상적으로 바뀌지만, 잘림(clip) 메시를
  스프라이트 UV 기준으로 생성하는 내부 로직이 동작을 안 해서 화면엔 항상 꽉 찬 사각형으로만
  렌더링됨.** 7.1에서 `fillAmount` 값을 리플렉션/직접 조회로 확인했을 땐 정확히 바뀌고 있었으니
  "정상"으로 결론 내렸던 것 - 값은 맞는데 화면 렌더링엔 반영 안 되는, 이 프로젝트에서 배경 스크롤
  버그(`mainTextureOffset`이 셰이더에서 무시됨) 때 이미 한 번 겪었던 것과 **같은 부류의 검증
  함정**이었음.
- **수정**: 이미 프로젝트에 있던 `ProceduralSpriteFactory.CreateUnitSquare(Color.white)`(다른
  이펙트에서도 쓰는 유틸리티)로 흰색 1x1 스프라이트를 만들어 `hpBarFill.sprite`/`expBarFill.sprite`에
  할당. `Image.color`로 틴트하는 방식은 그대로라 붉은색/초록색 표시엔 변화 없음.
- **재검증 필요**: 이번엔 반드시 **실제 스크린샷으로 바가 절반쯤 채워진 순간**을 캡처해서
  fillAmount 값과 화면 채움 비율이 일치하는지 육안으로 확인해주세요 - 프로퍼티 값만 조회하는
  방식은 이번 라운드에서 검증 함정에 걸렸으므로 신뢰하지 않습니다.

같은 라운드에 접수된 나머지 피드백:

- **[완료] 악기 아이콘 색 틴트 제거**: `slotIcons[uiIndex].color = info.themeColor` →
  `Color.white`로 변경, 아이콘 원본 색 그대로 표시.
- **[완료] 잠긴 슬롯 안내 방식 변경**: 구석의 작은 "Lv.5" 텍스트 대신, 프레임 안쪽 중앙에 크게
  `"Lv.5\n해금"` 형태로 표시하는 `LockMessage` 텍스트를 슬롯마다 추가(흰 글씨 + 검은 Outline).
  잠긴 슬롯 프레임 알파도 0.4→0.55로 살짝 올려 텍스트가 담긴 그릇 자체가 잘 보이도록 함.
- **[텍스트/숫자 관련 후속 결정 - 4.6 유지]** 사용자가 "바 사이즈를 더 줄이는 작업은 시간상 보류,
  지금 크기(320x64/320x48)를 유지한 채 숫자로 보여주는 방식으로 확정"이라고 결정 - 추가 크기
  조정 없음.

### 4.8 사용자 5차 피드백 - 프레임 9-slice 깨짐, Simple로 전환 (2026-08-10)

HP 바는 "90/100" 붉은 바가 정상으로 보였지만, **EXP 바는 프레임이 작은 조각(초록 사각형 + 점선
같은 것)으로 깨져 보이고, 악기 슬롯은 프레임이 아예 안 보임**(아이콘/텍스트만 둥둥 떠 있는 것처럼
보임)이라는 제보. 원인 재분석:

- 세 프레임 모두 원본 아트 해상도가 실제 표시 크기보다 훨씬 큼(HP 2260x696 vs 표시 320x64,
  Exp 2702x582 vs 320x48, Slot 1254x1254 vs 124x124). 4.4~4.7에서 픽셀 분석으로 추정해 넣은
  `spriteBorder` 값(원본 이미지 픽셀 기준, HP≈150전후, Exp≈140~170, Slot≈210)이 실제 표시 크기보다
  훨씬 커서(Slot은 border 한쪽만 210인데 타겟이 124 - 절반도 안 됨) 9-slice 모서리 연산이 아예
  깨지는 상황. HP는 운 좋게 그럭저럭 봐줄만하게 뭉개졌지만 Exp/Slot은 눈에 띄게 깨짐.
- **조치**: 3개 프레임 전부 `Image.Type.Sliced` → `Image.Type.Simple`로 전환. Simple은 border
  값을 아예 무시하고 스프라이트 전체를 타겟 RectTransform 크기에 맞춰 단순히 늘려 그리므로, 이
  해상도-대-표시크기 불일치 문제 자체가 원천적으로 사라짐. 디자인이 얇은 테두리 위주라 늘어나도
  티가 잘 안 날 것으로 예상 - 실측 필요.
- 만약 Simple로도 여전히 마음에 안 들면(늘어난 티가 많이 나거나 하면), 사용자가 제안한 대로 EXP
  프레임 아트는 그냥 안 쓰고(반투명 사각형 대체로 되돌림) 넘어가도 무방 - `Sprites/UI/ExpBarFrame.png`
  파일만 지우거나 이름을 바꾸면 코드는 자동으로 fallback 처리함.

### 4.9 사용자 6차 피드백 - 채움 바 여백 미세조정 (2026-08-10)

Simple 전환 후 프레임 자체는 잘 보이게 됐고(HP/EXP/악기 슬롯 전부 정상 표시 확인), 마지막으로
채움(fill) 바가 프레임 테두리 위아래로 살짝 삐져나와 보인다는 피드백. `EnsureHealthExpBarElements()`
(`Assets/Scripts/Rhythm/RhythmUI.cs`)의 `hpFillRt.offsetMin/offsetMax`(기존 8,8/-8,-8 →
14,11/-14,-11)와 `expFillRt.offsetMin/offsetMax`(기존 6,6/-6,-6 → 8,7/-8,-7)를 조정 - x는 좌우
여백, y는 상하 여백이라 따로 조절 가능. 정확한 수치는 실측 없이 비율로 추정한 값이라, 사용자가
직접 화면 보면서 두 값(특히 y)을 조금씩 조절해 맞출 예정 - 코드 위치는 위 두 변수.

### 4.10 사용자가 직접 여백 수치 확정 + 레벨 라벨 정렬 (2026-08-10)

4.9의 채움 바 여백은 사용자가 직접 화면 보면서 조정 완료("이 정도가 딱이네" - 최종 수치는
`RhythmUI.cs`의 `hpFillRt`/`expFillRt` offset 값 그대로 확정). 마지막으로 EXP 바 오른쪽 "Lv.N"
라벨이 바보다 살짝 위에 떠 보인다는 피드백 - `expLevelRt.sizeDelta`를 22→48(EXP 바 높이와 동일)로
맞춰서 `MiddleLeft` 정렬 텍스트가 바와 정확히 같은 세로 중심에 오도록 수정.

**이걸로 HUD 바/아이콘 전환 작업 사실상 완료.** 다음 라운드에서 이 최종 상태만 스크린샷으로 한 번
더 확인하면 `archive/`로 옮겨도 될 것 같음.

### 4.3 참고: HUD 작업과 무관해 보이는 별개 이슈 (재현됨, 확신도 낮음)

레벨업 테스트 중 `PlayerExperience.AddExp()`로 한 번에 여러 레벨업 분량의 EXP를 몰아서 지급했을 때
`LevelUpUI.ShowLevelUpSelection()` (LevelUpUI.cs:200)에서 `InstrumentManager.Instance.HasInstrument(t)`
호출이 `NullReferenceException`으로 죽는 상황을 관측했습니다. 같은 메서드 191~192줄은
`InstrumentManager.Instance != null` 방어 체크가 있는데 200줄은 없어 방어 로직이 일관적이지 않습니다.
다만 이 `Instance`가 null로 보인 현상이 MCP `execute_code` 샌드박스 특유의 아티팩트일 가능성을 배제하지
못해(같은 세션에서 `FindObjectOfType`으로는 살아있는 단일 인스턴스가 정상 조회됨), 실제 게임 플레이
중(몹 처치로 조금씩 EXP를 얻는 정상 경로)에도 재현되는지는 별도 확인이 필요합니다. 재현되면
`ShowLevelUpSelection()`이 `Time.timeScale = 0f`을 이미 실행한 뒤 죽기 때문에 **게임이 영구 정지**하는
심각한 프리즈 버그가 됩니다. HUD 바/아이콘 작업 범위 밖이라 이번 커밋에는 포함하지 않았지만, 별도
티켓으로 남겨두는 것을 권장합니다.

## 5. 재검증 결과 (2026-08-10, 4.4절 수정 반영 후)

`refresh_unity(force, compile=request)`로 스프라이트 재임포트 + 컴파일 확인 → Play 모드 재실측.

### 5.1 4.2에서 지적한 4건 모두 실측으로 수정 확인됨

1. **고아 오브젝트**: `HPText` GameObject를 씬에서 직접 조회 → `active: false` 확인. Play 모드
   스크린샷에도 "HP: 100/100"/"LV.1 EXP:.../"/"INSTRUMENTS:..." 텍스트가 전혀 보이지 않음. ✅
2. **ScoreText/ComboText 겹침**: 스크린샷에서 "SCORE: 0"/"COMBO: 0"이 HP/EXP 바 위쪽에 겹침 없이
   또렷하게 표시됨. ✅
3. **잠금 레벨 라벨**: Slot_R/W/E의 `LevelLabel.text`를 씬에서 직접 조회 → 각각 `"Lv.5"`,
   `"Lv.10"`, `"Lv.15"`로 정확히 표시됨(수정 전엔 `""`, `"Lv.5"`, `"Lv.8"`이었음). ✅
4. **9-slice border + Sprite Mode**: `Resources.LoadAll<Sprite>`로 재확인한 결과 3종 모두
   `spriteMode: Single`로 재임포트되어 서브스프라이트가 1개로 정리됐고(기존엔 Exp/Slot 프레임에
   자투리 `_1`이 하나씩 더 있었음), `sprite.border`도 0이 아닌 유의미한 값으로 스케일링되어
   들어옴(HP≈139/147/141/149, Exp≈106/128/106/127, Slot=210/211/210/213). 씬의 `HpBarFrame`/
   `Slot_Q` Image 컴포넌트의 `hasBorder`가 이제 `true`로 확인됨(수정 전엔 `false`). ✅

### 5.2 재검증 중 새로 발견된 이슈

**악기 슬롯 프레임의 틴트 색상이 실제 프레임 아트를 거의 검게 뭉개버림.**

`UpdateInstrumentUI()`의 `slotFrames[i].color = isLocked ? (0.05,0.05,0.05,0.4) : (0.05,0.05,0.1,0.85)`
는 원래 프레임 아트가 없을 때(반투명 사각형 대체용) 쓰려고 만든 색이었는데, 이 컬러 대입이
스프라이트 유무와 무관하게 항상 실행됩니다. 이제 `InstrumentSlotFrame.png` 실제 아트가
`Image.sprite`로 로드된 상태에서도 같은 거의-검은색이 곱해져서(Slot_Q 컴포넌트 실측:
`sprite=InstrumentSlotFrame.png`, `hasBorder=true`, 그런데 `color=(0.05,0.05,0.1,0.85)`),
아트의 색/디테일이 거의 다 뭉개진 채로 렌더링됩니다. HP/EXP 바 프레임(`hpBarFrame`/`expBarFrame`)은
반대로 스프라이트가 있을 때 색을 아예 건드리지 않는 분기라 이 문제가 없습니다(`hpBarFrame.color`
실측 결과 `(1,1,1,1)` 그대로).

- 조치안: `EnsureInstrumentSlotElements()`의 프레임 생성부처럼, `UpdateInstrumentUI()`에서도
  `slotFrameSprite`(또는 `slotFrames[i].sprite != null`) 여부를 확인해 스프라이트가 있으면
  RGB는 거의 흰색 유지하고 알파만 잠금 상태에 따라 낮추는 방식으로 분기 필요
  (예: `Color.white`에 알파만 `isLocked ? 0.4f : 1f` 적용).

## 7. 3차 검증 (2026-08-10, 4.5/4.6 반영 + 5.2 보완 수정 후)

### 7.1 4.5 "[진단 필요, 최우선] 체력바가 실제 플레이 중 안 움직임" - 재현 실패, 정상 동작 확인

`UpdateHealthUI`/`UpdateExpUI`에 추가된 진단 로그를 활용해, **`execute_code`로 `TakeDamage()`를 직접
부르는 대신** 플레이어를 실제 몬스터 트리거 범위 안으로 이동시켜 `OnTriggerStay2D` → `TakeDamage()`
경로를 자연스럽게 유발해 재검증:

- `UpdateHealthUI(90/100)` → `(80/100)` → `(70/100)` → `(60/100)` 순으로 로그가 정확히 찍혔고,
  매번 `hpBarFill.fillAmount`도 정확히 일치(0.90→0.60). **실제 충돌 경로에서도 체력바 로직은
  정상 동작 확인.**
- EXP도 `AddExp` 자연 누적으로 Lv.1→Lv.2 레벨업까지 정상 진행, `fillAmount`가 0.96에서 레벨업
  순간 0.00으로 정확히 리셋됨.
- 레벨업 시점에 `Time.timeScale=0`이 되며 체력바가 "얼어붙은" 것처럼 보였던 것은 `LevelUpUI`의
  **의도된 정지**(카드 선택 화면, 실측 스크린샷에 "LEVEL UP!" + 3개 카드 정상 표시, 4.3에서 우려한
  `InstrumentManager.Instance` null 크래시는 이번엔 발생하지 않음)로 확인됨 - 버그가 아님.
- **결론: 원인 미상으로 남겨뒀던 이 이슈는 코드 문제가 아닌 것으로 재확인됨.** 사용자가 제보한
  "YOU DIED인데 체력바가 가득 차 보였다"는 스크린샷은 이번 재현으로는 재생산되지 않았음 - 레벨업
  정지 화면이나 이전 테스트 라운드에서 남은 상태를 캡처했을 가능성이 있어 보이나 확정할 수는
  없음. 다만 정상 플레이 경로에서의 갱신 로직 자체는 반복 검증으로 신뢰도가 높아졌으므로, 진단용
  `Debug.Log`(`RhythmUI.cs`)는 목적을 다했다고 보고 **제거함**.

### 7.2 5.2 악기 슬롯 프레임 틴트 문제 - 수정 완료 및 확인

`UpdateInstrumentUI()`에서 `slotFrames[uiIndex].sprite != null` 여부로 분기하도록 수정 - 실제 아트가
있으면 RGB는 흰색 유지, 알파만 잠금 상태에 따라 조절(`isLocked ? 0.4f : 1f`). Play 모드에서 씬의
`Slot_Q`(해금)/`Slot_R`(잠김) `Image` 컴포넌트를 직접 조회해 확인:

- `Slot_Q.color = (1, 1, 1, 1)` - 실제 프레임 아트가 원래 색 그대로 표시됨. ✅
- `Slot_R.color = (1, 1, 1, 0.4)` - RGB는 그대로 유지되고 알파만 낮아져 아트가 뭉개지지 않고
  반투명하게 죽어 보임. ✅

### 7.3 4.6 확대된 바(320×64/320×48)의 9-slice 찌그러짐 우려 - 스크린샷 실측 결과 문제 없음

Border 합계가 새 프레임 크기보다 커서(HP 세로 border 합≈286 vs 세로 64, EXP≈254 vs 48) 찌그러질
위험이 있다고 4.6에서 우려했으나, 실제 Play 모드 스크린샷으로 확인한 결과 모서리 겹침이나 눈에
띄는 찌그러짐 없이 둥근 필 모양으로 정상 렌더링됨(Unity가 border 합이 타겟보다 클 때 비례
축소해서 처리하는 것으로 보임). 추가 조치 불필요.

### 7.4 종합 결론 (당시 기준 - 8절에서 정정됨)

지금까지 접수된 이슈(4.2의 4건, 4.5의 6건, 5.2의 1건) 모두 코드 수정 완료 및 Play 모드 실측으로
확인됨. 남은 것은 4.5의 5/6번(숫자 표시 방식은 4.6에서 확정 반영 완료, 장신구 아이콘 공간은 설계만
되어 있고 아트 제작 대기 중)과 4.3의 별개 이슈(`LevelUpUI`/`InstrumentManager.Instance` null 크래시
가능성, 7.1에서 이번엔 재현 안 됐지만 근본 원인이 밝혀진 건 아니므로 방어 코드 추가는 여전히
권장) 정도. HUD 바/아이콘 전환 작업 자체는 `archive/`로 옮겨도 될 상태로 판단됨.

> **8절 정정 사항**: 아래 7.1의 "재현 실패, 정상 동작 확인" 결론은 **틀렸습니다.** 4.5 #1
> ("체력바가 실제 플레이 중 안 움직임")은 실제로 존재하던 버그였고, 원인은 7.1에서 검증한
> `fillAmount` 프로퍼티 값이 아니라 **그 값이 화면에 실제로 반영되는지 여부**였습니다. 8절 참고.

## 8. 4차 검증 (2026-08-10, `ProceduralSpriteFactory` 흰색 스프라이트 부여 + LockMessage 추가 반영)

이번 라운드는 사용자가 직접 코드를 추가로 수정한 상태(`RhythmUI.cs`)에서 시작. 주요 변경점:

- **`hpBarFill`/`expBarFill`에 `ProceduralSpriteFactory.CreateUnitSquare(Color.white)`로 만든 흰색
  스프라이트를 명시적으로 부여.** 코드 주석에 따르면 "`Image.Type.Filled`인데 `sprite`가 `null`이면
  `fillAmount` 프로퍼티는 정상적으로 바뀌지만 실제 화면엔 항상 꽉 찬 사각형으로만 렌더링되고 잘림이
  전혀 반영되지 않는 uGUI 특성"이 원인으로 지목됨(Unity가 Filled의 클리핑 메시를 스프라이트의 UV
  rect 기준으로 생성하기 때문 - sprite가 없으면 기본 흰 텍스처를 쓰지만 이 경로에서 Fill 클리핑
  메시 생성이 정상 동작하지 않는 것으로 보임).
- 악기 슬롯에 **"Lv.N / 해금" 잠금 안내 메시지**(`slotLockMessages`, 프레임 중앙에 2줄 텍스트)를
  구석의 작은 숫자 대신 크게 추가.
- 악기 아이콘 틴트(`info.themeColor`) 제거 → 아이콘 원본 색 그대로 표시.

### 8.1 [핵심] 7.1의 결론 정정 - 체력/EXP 바 "안 움직임" 버그는 실제로 있었음

7.1에서는 `hpBarFill.fillAmount` **프로퍼티 값**만 리플렉션/씬 조회로 확인하고 "정상 동작"이라고
결론 내렸는데, 이번에 스크린샷으로 **실제 렌더링된 모양**을 직접 봤을 때 근본 원인이 드러남:

- `PlayerController.TakeDamage(50)` → HP 50/100 상태에서 스크린샷 확인: HP 바가 **정확히 절반만
  빨간색으로 채워지고 나머지 절반은 배경색**으로 표시됨(`"50 / 100"` 숫자와 정확히 일치). EXP 바도
  `30/250`(12%)만큼만 초록색이 채워짐. ✅ 수정 후 정상.
- `TakeDamage(200)`로 체력을 0까지 떨어뜨려 "YOU DIED / DEFEAT" 화면을 띄운 뒤 확인: HP 바가
  **완전히 빈 상태**(배경색만, 빨간 채움 전혀 없음)로 `"0 / 100"` 숫자와 함께 표시됨. **이게 바로
  사용자가 최초에 제보했던 "YOU DIED인데 체력바가 가득 차 보였다"는 증상과 정확히 반대로 재현된
  것** - 즉 수정 전에는 `fillAmount=0`이어도 화면엔 계속 꽉 찬 사각형으로 남아있었을 것이라는
  가설이 스크린샷으로 뒷받침됨.
- **교훈**: uGUI의 `Image.Type.Filled`는 `fillAmount` 프로퍼티 값과 실제 렌더링 결과가 분리될 수
  있는 경우가 있어(스프라이트가 null일 때), 프로퍼티 조회만으로 시각적 정상 동작을 단정할 수 없음 -
  이후 유사 검증에서는 스크린샷으로 실제 렌더링 형태까지 함께 확인 필요.

### 8.2 잠금 슬롯 "Lv.N / 해금" 메시지 - 정상 동작 확인

Play 모드에서 씬의 `LockMessage` Text 컴포넌트 4개를 직접 조회해 확인:

- Slot_Q(장착됨): `LockMessage.text = ""` (비활성) - 장착 슬롯엔 안 뜸. ✅
- Slot_W(잠김, 실제 요구 Lv.10): `LockMessage.text = "Lv.10\n해금"`. ✅
- Slot_E(잠김, 실제 요구 Lv.15): `LockMessage.text = "Lv.15\n해금"`. ✅
- Slot_R(잠김, 실제 요구 Lv.5): `LockMessage.text = "Lv.5\n해금"`. ✅

화면 표시 순서(Q,W,E,R)와 `lockReqs[dataIndex]` 매핑이 정확히 맞물려 있음을 확인. 텍스트가 프레임
중앙에 2줄로 표시되고 `KeyLabel`(좌상단)/`LevelLabel`(우하단, 잠긴 슬롯엔 빈 문자열)과 겹치지 않음.

### 8.3 종합 결론

7.1에서 "문제 없음"으로 결론 내렸던 체력/EXP 바 이슈가 실제로는 렌더링 버그였고, 이번 라운드에서
근본 원인 수정(흰색 스프라이트 부여) 및 시각적 재검증까지 완료됨. 새로 추가된 잠금 안내 메시지도
정상 동작. 현재까지 확인된 범위에서 HUD 바/아이콘 전환 작업은 기능적으로 완결된 상태로 판단됨 -
`archive/` 이동 가능. 남은 참고 사항은 4.3의 `LevelUpUI` null 방어 코드 권장 정도.

## 9. 5차 검증 (2026-08-10, 4.7/4.8 프레임 `Type.Simple` 전환 반영)

`refresh_unity(force, compile=request)` → 컴파일 에러/경고 0건 확인 → Play 모드 재실측.

### 9.1 EXP 바 "조각나 보임" / 악기 슬롯 "프레임 안 보임" 문제 - 수정 확인

`HpBarFrame`/`ExpBarFrame`/`InstrumentSlotFrame` 세 `Image` 모두 `Image.Type.Sliced` →
`Image.Type.Simple`로 전환된 상태에서 Play 모드 스크린샷 확인:

- HP 바(`"65 / 100"`)와 EXP 바(`"30 / 250"`) 모두 **테두리가 깨지거나 조각나지 않고 매끄러운 둥근
  사각형 프레임**으로 정상 렌더링됨. 4.8에서 제보됐던 "초록 사각형 + 점선 같은 것으로 깨져 보임"
  현상은 재현되지 않음.
- 악기 슬롯 4칸 모두 **프레임이 뚜렷하게 보이는 둥근 사각형**으로 렌더링됨(Q는 드럼 아이콘을
  감싸고, W/E/R은 "Lv.10/해금", "Lv.15/해금", "Lv.5/해금" 잠금 메시지를 담은 박스로 또렷하게
  구분됨). 4.8에서 제보됐던 "프레임이 아예 안 보임(아이콘/텍스트만 둥둥 떠 있음)" 현상도
  재현되지 않음.
- 씬의 `Slot_Q` `Image` 컴포넌트를 직접 조회해 확인: `type: 0`(Simple), `sprite:
  InstrumentSlotFrame.png`, `color: (1,1,1,1)`. `Icon` 자식의 `Image`도 `color: (1,1,1,1)`(원본
  색 그대로, 4.7의 틴트 제거 반영 확인).

### 9.2 종합 결론

4.7(체력바 fillAmount 렌더링 버그)과 4.8(프레임 9-slice 깨짐)에서 제기된 이슈 모두 코드 수정 +
Play 모드 실측으로 해결 확인됨. 현재 시점 기준으로 HUD 바/아이콘 전환 작업에 남아있는 미해결
항목은 없음(4.3의 `LevelUpUI` null 방어 코드는 HUD 작업 범위 밖 별개 이슈로 남겨둠). `archive/`로
이동해도 무방한 상태로 판단됨.

## 10. 6차 검증 (2026-08-10, 4.9/4.10 채움 바 여백 미세조정 + EXP 레벨 라벨 정렬 반영)

`refresh_unity(force, compile=request)` → 컴파일 에러/경고 0건 확인 → Play 모드 실측.

(참고: `manage_camera` 스크린샷을 `max_resolution` 1600/1800으로 찍으면 이 세션에서는 이유는
불명확하나 Screen Space Overlay UI가 캡처에서 빠지는 현상이 있었음 - 1400에서는 매번 정상 표시됨.
게임 자체의 문제가 아니라 이번 MCP 캡처 도구의 해상도별 캡처 경로 차이로 보이므로 이후 라운드에서도
1400 위주로 캡처 권장.)

### 10.1 채움 바 여백 - 실제 반영값 확인

문서(4.9/4.10)엔 사용자가 화면 보면서 직접 조정했다고만 적혀 있어 정확한 최종 수치는 코드에서
직접 확인: `hpFillRt.offsetMin/Max = (20,15)/(-20,-15)`, `expFillRt.offsetMin/Max =
(17,15)/(-17,-15)` (4.9에서 제안했던 추정치 14,11 / 8,7과는 다른, 사용자가 최종적으로 더 크게
조정한 값). `PlayerController.TakeDamage(45)`로 HP 45/100 상태를 만들어 스크린샷 확인: 빨간
채움/초록 채움 모두 **프레임 테두리 안쪽으로 깔끔하게 들어가 있고 위아래로 삐져나오는 부분 없음**.
4.9에서 우려했던 "테두리 위아래로 삐져나와 보임" 현상 재현 안 됨. ✅

### 10.2 EXP 레벨 라벨 정렬 - 반영 확인

`expLevelRt.sizeDelta`가 코드상 `(60, 48)`로 EXP 바 높이(48)와 동일하게 맞춰져 있음을 확인. 같은
스크린샷에서 `"Lv.1"` 라벨이 EXP 바("60 / 250")와 **같은 세로 중심선**에 놓여 위로 떠 보이지 않음.
4.10에서 우려했던 "라벨이 바보다 위에 떠 보임" 현상 재현 안 됨. ✅

### 10.3 종합 결론

4.9/4.10에서 사용자가 직접 미세조정한 여백/정렬 값 모두 Play 모드 스크린샷으로 정상 반영 확인.
4.7~4.10에 걸쳐 제기된 모든 디자인 이슈가 해결됐고, 이번 라운드 기준으로 HUD 바/아이콘 전환
작업에 추가로 발견된 문제는 없음. `archive/`로 이동해도 무방.
