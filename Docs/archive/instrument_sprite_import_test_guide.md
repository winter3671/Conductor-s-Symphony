# 신규 악기 이미지 5종(벨/프렌치호른/글록켄슈필/마림바/팀파니) 임포트 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 코드 변경은 없고(아래 0번 참고), Unity 에디터에서만 처리 가능한 텍스처 임포트 작업입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 했나 / 왜 코드 변경이 없는가

사용자가 `Assets/Resources/Sprites/Instruments/`에 이미지가 없던 5종 악기(벨/프렌치호른/글록켄슈필/
마림바/팀파니) 픽셀아트 PNG를 추가했습니다. 확인 결과:

- 5개 전부 올바른 악기 이미지(벨=황금 핸드벨, 프렌치호른=금관+스탠드, 글록켄슈필=금속 바 부채꼴,
  마림바=나무 바+말렛+공명관, 팀파니=구리 케틀드럼+교차 말렛+스탠드)이고, RGBA + 정상 알파 채널
  확인됨(Python/PIL로 검증, 검은 배경처럼 보인 건 투명 배경이 미리보기에서 검게 렌더링된 것뿐).
- `Frenchhorn.png` → `FrenchHorn.png`로 이름을 정정했습니다(대소문자만 다른 리네임이라 임시 파일명을
  거쳐 2단계로 처리). `InstrumentType.FrenchHorn` enum 값과 대소문자까지 정확히 일치해야
  `Resources.Load<Sprite>($"Sprites/Instruments/{type}")`가 찾을 수 있기 때문입니다.
- 스프라이트 로딩 코드(`InstrumentOrbit.cs:27`, `LevelUpUI.cs:308`)는 이미 `InstrumentType` 이름으로
  동적 로딩하도록 되어 있어서, **파일명만 맞으면 코드 변경 없이 자동으로 연결됩니다.**
- 다만 이 5개 파일은 아직 `.meta`가 없는(=Unity가 한 번도 임포트하지 않은) 상태라, 에디터에서 첫
  임포트 시 기본 설정(Texture Type: Default, 즉 일반 텍스처)으로 들어가면 `Resources.Load<Sprite>`가
  못 찾고 조용히 `null`을 반환 → 코드가 폴백으로 `defaultSprite`(프로시저럴 원)를 계속 쓰게 됩니다.
  **Sprite (2D and UI) 타입으로 명시적으로 바꿔줘야 합니다.**
- 기존 5개(피아노/바이올린/첼로/플루트/드럼) 스프라이트는 `.meta`를 보면 전체 캔버스가 아니라
  내용물에 맞춰 수동으로 크롭된 사각형(`rect`)을 쓰고 있었습니다. `InstrumentOrbit.cs`의 궤도 동반체
  크기 정규화 로직(`Mathf.Max(bounds.size.x, bounds.size.y)`를 0.68 유닛으로 스케일)이 스프라이트의
  `bounds`(=임포트된 rect 크기)를 기준으로 삼기 때문에, 신규 5개도 투명 여백을 크롭하지 않으면 다른
  5개보다 화면에서 눈에 띄게 작게 보일 수 있습니다.

## 1. 임포트 절차 (Unity 에디터에서 수행)

1. `refresh_unity(mode=force, compile=request)`로 새 텍스처 5개를 최초 임포트.
2. 5개 파일(`Bell.png`, `FrenchHorn.png`, `Glockenspiel.png`, `Marimba.png`, `Timpani.png`) 각각:
   - Texture Type = **Sprite (2D and UI)**
   - Sprite Mode = **Single**
   - Pixels Per Unit = 기존 5개와 동일값으로 통일(기존 `.meta` 확인 후 맞추기)
   - Filter Mode = 기존 5개와 동일하게(기존 `.meta` 기준 Bilinear)
3. (권장) 각 스프라이트를 **Sprite Editor**로 열어 **Trim** 버튼으로 투명 여백을 알파 기준
   자동 크롭. 대략적인 기대 크롭 크기(참고용, Trim이 알아서 계산하므로 직접 입력할 필요는 없음):
   - Bell: 원본 2048×2048 → 약 1270×1619
   - FrenchHorn: 원본 2048×2048 → 약 1722×1966
   - Glockenspiel: 원본 2106×2048 → 약 1729×957
   - Marimba: 원본 2048×2048 → 약 1886×1312
   - Timpani: 원본 2048×2048 → 약 1476×1967
4. Apply 후 `refresh_unity(force, compile=request)`로 재적용.

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건(애초에 C# 변경이 없으므로 발생하면 무관한 이슈일 가능성이 큼)
- [ ] 5개 악기(벨/프렌치호른/글록켄슈필/마림바/팀파니)를 각각 장착 시 `InstrumentOrbit`이
      프로시저럴 원이 아니라 방금 임포트한 실제 픽셀아트를 사용하는지
- [ ] 레벨업 카드 UI(`LevelUpUI`)에서도 5개 악기 아이콘이 실제 아트로 표시되는지
- [ ] 5개 신규 악기의 화면상 크기가 기존 5개(피아노/바이올린/첼로/플루트/드럼)와 시각적으로 비슷한
      비중으로 보이는지(크롭 여부에 따라 달라짐 - 크롭 안 하면 작아 보일 수 있음)
- [ ] **회귀**: 기존 5개 악기(피아노/바이올린/첼로/플루트/드럼)의 표시/동작은 변화 없는지
- [ ] **회귀**: 아직 아트가 없는 나머지 5개는 여전히 프로시저럴 도형으로 정상 폴백되는지(있다면 - 현재
      기준 10종 전부 채워졌다면 이 항목은 생략 가능)

## 4. 검증 결과

**검증 일시**: 2026-08-08, Unity MCP 연결 세션(`My project@c018c67b9a01a4e5`, Unity 6000.5.5f1)

**사전 확인**: 검증 시작 시점에 5개 파일(`Bell/FrenchHorn/Glockenspiel/Marimba/Timpani.png`)의 `.meta`가
이미 존재했고, 내용을 확인한 결과 1절에서 요구하는 설정(Texture Type=Sprite(2D and UI), Sprite
Mode=Single, Pixels Per Unit=100, Filter Mode=Bilinear)과 Sprite Editor Trim까지 전부 기존 5개
(Piano 등, PPU 100 / Bilinear)와 동일하게 이미 적용되어 있었음. 트림된 rect 크기도 3절의 기대 크롭
크기와 거의 일치(Bell 1272×1621, FrenchHorn 1724×1968, Glockenspiel 1731×959, Marimba 1888×1314,
Timpani 1478×1969). 즉 1절 임포트 절차는 이번 세션 시작 전에 이미 완료되어 있었고, 이번 세션은
`refresh_unity(force, compile=request)` 재적용 + 2절 검증만 수행함.

- [x] **컴파일 에러/경고 0건**: `refresh_unity(force, compile=request)` 후 `read_console(types=[error,warning])`
      결과 0건 확인(코드 변경이 없으므로 예상대로).
- [x] **InstrumentOrbit이 실제 아트 사용**: Play Mode 진입 후 `InstrumentOrbit.Initialize()`를 10종
      전체(Drums/Piano/Violin/Flute/FrenchHorn/Glockenspiel/Cello/Timpani/Marimba/Bell)에 대해 직접
      실행 → 전부 `Resources.Load<Sprite>` 성공(null 아님), `SpriteRenderer.color`가 흰색으로 설정됨
      (폴백 경로였다면 매개변수로 넘긴 `Color.magenta`가 찍혔을 것). 스크린샷으로 5개 신규 악기가
      각각 벨(황금 핸드벨)/프렌치호른(금관)/글록켄슈필(금속 바 부채꼴)/마림바(목재 바+말렛)/
      팀파니(구리 케틀드럼+교차 말렛+스탠드)로 육안 식별됨. 회귀 확인용으로 같이 렌더링한 기존 5개도
      정상 표시.
- [x] **LevelUpUI 아이콘 로딩**: `LevelUpUI.cs:308`의 `Resources.Load<Sprite>($"Sprites/Instruments/{type}")`는
      `InstrumentOrbit.cs:27`과 동일한 로딩 코드 경로이며, 위 10종 전체 로딩 테스트가 이 경로도 그대로
      검증함(별도 UI 카드 트리거 없이도 동일 API 호출 결과로 충분히 확인 가능). 10종 모두 null이
      아니었으므로 카드 아이콘도 정상 표시될 것으로 판단.
- [x] **신규 5개 vs 기존 5개 크기 비교**: `InstrumentOrbit`의 정규화 로직(`bounds` 최댓값을 0.68 유닛으로
      스케일)은 트림된 rect를 기준으로 하므로, 신규 5개도 Trim이 이미 적용되어 있어 기존 5개와 동일한
      방식으로 최댓값이 0.68 유닛에 맞춰짐(예: Bell scale=0.042, Piano scale=0.041, Timpani scale=0.035,
      Drums scale=0.049 — 전부 같은 자릿수). 스크린샷상으로도 신규 5개가 기존 5개와 비슷한 비중으로
      보임(Flute처럼 원래 가로로 긴 악기는 그 형태 그대로 얇게 보이는 것이 정상).
- [x] **회귀: 기존 5개(피아노/바이올린/첼로/플루트/드럼) 정상**: 동일 테스트에서 5개 전부
      `Resources.Load` 성공, 스크린샷에서도 이상 없이 표시됨.
- [x] **회귀: 아트 없는 나머지 항목의 프로시저럴 폴백**: `InstrumentPatternDatabase.cs`의
      `InstrumentType` enum을 확인한 결과 현재 정의된 악기는 정확히 10종(Drums/Piano/Violin/Flute/
      FrenchHorn/Glockenspiel/Cello/Timpani/Marimba/Bell)이며 10종 전부 스프라이트가 채워진 상태라
      가이드 문구대로 이 항목은 해당 없음(생략).

**결론**: 5종 악기 이미지 임포트 및 InstrumentOrbit/LevelUpUI 연동 정상 동작 확인. 코드 변경 불필요,
추가 조치 없음. 테스트 중 생성한 임시 GameObject 및 스크린샷 파일은 모두 정리함(프로젝트에 남은
변경사항은 `Assets/Resources/Sprites/Instruments/`의 신규 PNG 5개 + `.meta` 5개뿐).
