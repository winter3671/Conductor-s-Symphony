# 빔/투사체 4프레임 반짝임 애니메이션 연동 (피아노/벨/마림바/바이올린 참격) - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

`PiercingBeamProjectile`(피아노 레이저/벨 8방향 성광/마림바 파동이 공유하고, 바이올린 릴리즈 참격도
동일한 절차적 원 스프라이트를 써서 사실상 같이 쓰던 클래스)에 나노바나나로 그린 4프레임 "에너지 볼트"
반짝임 애니메이션(`Assets/Resources/Sprites/Effects/Beam/Beam1~4.png`)을 붙였습니다.

- 이 클래스 **한 곳에서만** 프레임을 로딩하므로, 호출부(`TapAttackHelpers.SpawnBeam`,
  `ViolinOrbitEffect`의 릴리즈 참격) 코드는 전혀 건드리지 않았는데도 4곳(피아노/벨/마림바/바이올린
  참격) 모두 자동으로 새 아트가 적용됩니다.
- 0.06초 간격으로 4프레임을 순환 재생해 은은한 반짝임 효과를 냅니다.
- **크기 버그 사전 방지**: 이 클래스는 `transform.localScale = (visualLength × sizeMultiplier, 0.28 ×
  sizeMultiplier, 1)`처럼 스프라이트를 진행방향으로 길게 늘리는데, 기존 폴백 원은 정사각형(콘텐츠
  0.16×0.16 월드유닛)이라 이 배율을 그대로 곱해도 문제가 없었습니다. 반면 새 빔 아트는 원본부터
  이미 가로로 길쭉한 모양(약 7~8:1)이라, 예전처럼 콘텐츠 크기를 무시하고 고정 배율만 곱하면 여기에
  또 곱해져서(이중 확대) 임팩트 버스트 때와 같은 종류의 버그가 날 뻔했습니다. `ApplyBeamFrame()`에서
  프레임의 실제 콘텐츠 크기로 먼저 나눠 "기존 원과 같은 기준 크기"로 정규화한 뒤 기존 배율을 곱하도록
  미리 방지해뒀습니다 - 기존 원 폴백을 쓸 때는 수학적으로 이전과 완전히 동일한 결과가 나옵니다.
- 프레임 로딩 실패 시(에셋 미존재 등) 기존처럼 `Initialize()`로 전달받은 단일 sprite(각 호출부의
  절차적 원)로 폴백합니다.

## 1. 사전 준비

1. `Assets/Resources/Sprites/Effects/Beam/Beam1~4.png`에 아직 `.meta`가 없습니다(한 번도 임포트 안
   됨). `refresh_unity(mode=force, compile=request)`로 최초 임포트.
2. 4개 파일 전부: Texture Type = **Sprite (2D and UI)**, Sprite Mode = **Single**. PPU/트림 여부는
   코드가 `frame.bounds`로 실제 크기를 읽어 매번 역산하므로 크게 상관없지만, 다른 에셋들과 통일성
   있게 PPU 100으로 맞추는 걸 권장합니다.
3. `refresh_unity(force, compile=request)`로 재적용 후 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [ ] 피아노/벨/마림바 공격 시 프로시저럴 원 대신 새 빔 아트가 보이는지
- [ ] 바이올린 릴리즈 참격에도 (코드 변경 없이) 같은 빔 아트가 자동으로 적용되는지
- [ ] 4프레임이 순환하며 은은한 반짝임처럼 보이는지(형태가 크게 안 바뀌고 미세한 변화만)
- [ ] **크기 검증**: 새 빔 아트 사용 시 화면상 길이/두께가 기존 원 폴백 사용 시와 (수학적으로) 동일한
      비율(`visualLength : 0.28`)로 나오는지 - `transform.localScale`을 리플렉션으로 읽어 프레임별
      콘텐츠 크기가 달라도 최종 스케일이 일정한지 확인
- [ ] 마림바 Lv3(파동 크기 +30%, `sizeMultiplier`)처럼 크기 배율이 걸릴 때도 새 아트가 비율 그대로
      커지는지
- [ ] 진행 방향에 맞춰 스프라이트가 정확히 회전하는지(가로로 긴 모양이 실제 이동 방향을 향하는지)
- [ ] 프레임 폴백 상황(에셋 없음)에서 기존 원과 완전히 동일한 스케일 결과가 나오는지(회귀)
- [ ] **회귀**: 관통(pierce)/데미지/보스 적중/바운스(마림바 Lv4) 로직은 이번 변경과 무관하게 그대로인지
- [ ] **회귀**: 마림바 Lv5 피격 시 감속+밀쳐냄(`onHitEnemy` 콜백)이 정상 동작하는지

## 4. 검증 결과

**검증 일시**: 2026-08-08, Unity MCP 연결 세션(`My project@c018c67b9a01a4e5`, Unity 6000.5.5f1)

**1절 사전 준비**: `refresh_unity(force, compile=request)`로 4개 파일 최초 임포트 → `.meta` 4개 모두
자동 생성됨. 컴파일 에러/경고 0건.

- 특이사항: `TextureImporter.textureType`는 `Sprite`, PPU는 100으로 정상이었으나, **Sprite Mode가
  `Single`이 아니라 `Multiple`**로 임포트됐음(문서 1절 권장과 다름). 그런데 `AssetDatabase.LoadAllAssetsAtPath`
  확인 결과 각 PNG마다 알파 기준으로 자동 트림된 서브스프라이트가 정확히 1개씩(`Beam1_0`~`Beam4_0`)
  생성돼 있었고, `Resources.LoadAll<Sprite>("Sprites/Effects/Beam")`도 이 4개를 정확히 로드함(순서도
  `Array.Sort(CompareOrdinal)`로 `Beam1_0→Beam2_0→Beam3_0→Beam4_0` 정확). 문서 0절이 이미 명시했듯
  코드가 `frame.bounds`로 실제 크기를 역산하므로 Multiple/Single 여부와 무관하게 동작에 영향 없음을
  실측으로 확인 - 굳이 Single로 바꿀 필요 없음(각 프레임 알파 트림 결과: Beam1 470×65px, Beam2
  469×62px, Beam3 462×62px, Beam4 470×57px, 전부 가로세로 비율 약 7~8:1로 문서 설명과 일치).

- [x] **피아노/벨/마림바 공격 시 새 빔 아트 표시**: Play Mode에서 `PiercingBeamProjectile.Initialize()`를
      직접 호출해(4개 인스턴스, 각각 피아노/벨/마림바Lv3/바이올린 테마 색) 카메라 앞에 배치 후 스크린샷
      촬영. 프로시저럴 원이 아닌 "혜성 꼬리" 모양의 새 에너지 볼트 아트가 렌더링됨을 육안 확인, 악기별
      색(백색/황색/주황/보라)도 정상적으로 틴트됨.
- [x] **바이올린 릴리즈 참격 자동 적용**: `ViolinOrbitEffect.OnHoldReleased()` 코드 검토 결과 `PiercingBeamProjectile`을
      동일하게 사용(`slashSprite`를 폴백으로 전달)함을 확인 - 프레임 로딩이 클래스 정적 필드 1곳에서만
      일어나므로 별도 실측 없이도 코드상 자동 적용이 보장됨. 위 스크린샷의 "VizBeam_Violin" 인스턴스가
      동일 `Initialize()` 경로로 정확히 같은 아트를 그린 것으로 교차 확인됨.
- [x] **4프레임 순환 반짝임**: `Update()` 비공개 메서드를 리플렉션으로 직접 반복 호출(에디터 창이
      포커스를 잃어 `Time.time`이 거의 진행되지 않는 환경 특성 - 하단 참고 - 때문에 결정론적 방식 사용).
      `Time.deltaTime≈0.02`, `FrameInterval=0.06` 기준으로 3~4틱마다 `Beam1_0→Beam2_0→Beam3_0→Beam4_0→(순환)`
      순서로 정확히 전환됨을 확인(형태 변화 거의 없이 반짝임만 있는 원본 아트 특성과 일치).
- [x] **크기 검증(핵심)**: `ApplyBeamFrame()`을 리플렉션으로 직접 호출해 4프레임 + 폴백 원 각각에 대해
      `transform.localScale × frame.bounds.size`(실제 화면 표시 크기)를 계산한 결과, **contentW가
      4.62~4.70, contentH가 0.57~0.65로 프레임마다 달랐음에도 최종 표시 크기는 5개 케이스 전부
      visW=0.176000, visH=0.044800으로 소수점 6자리까지 완전히 동일**했음. 이 값은 기존 코드
      (`localScale = visualLength×sizeMultiplier, 0.28×sizeMultiplier`를 원 콘텐츠(0.16×0.16)에 그대로
      곱한 값)과도 정확히 일치 - 이중 확대 없이 회귀 없이 정규화가 의도대로 동작함을 확인.
- [x] **마림바 Lv3 sizeMultiplier(+30%) 검증**: `sizeMultiplier=1.3`으로 초기화 시 visW=0.228800,
      visH=0.058240 (=0.176×1.3, 0.0448×1.3과 정확히 일치), `hitRadius`도 `0.5×1.3=0.65`로 정확히 반영됨.
- [x] **회전 방향 검증**: 4가지 방향((1,0,0)/(0,1,0)/(1,1,0)/(-1,0.5,0))으로 초기화 시
      `transform.eulerAngles.z`가 `Atan2(dir.y,dir.x)`의 기대값과 소수점 2자리까지 정확히 일치(0°/90°/45°/153.44°).
- [x] **폴백 시나리오 수학적 동일성**: 위 크기 검증에 폴백 원(`CreateFilledCircle(16,7f,...)`, 콘텐츠
      0.16×0.16 확인됨)도 포함시켜 같은 5-way 비교에서 동일값(0.176, 0.0448)임을 실측 확인 - "폴백 원
      기준으로 완전히 동일한 결과" 요구사항 충족.
- [x] **회귀 - 관통/데미지**: 가짜 `EnemyMonster`를 `EnemySpawner.activeEnemies`에 리플렉션으로 등록,
      `pierceCount=3`인 빔이 관통 통과하며 `remainingPierce`가 정확히 3→2로 감소, `enemy.CurrentHealth`가
      100→90(데미지 10)으로 정확히 감소함을 확인. `onHitEnemy` 콜백도 정확히 1회 호출됨을 카운터로 확인.
- [x] **회귀 - 보스 적중**: 가짜 `BossMonster.Initialize(5000)`를 배치, 빔이 관통하며 `BossMonster.TakeDamage`가
      정확히 1회(피해 25, 5000→4975) 호출되고 `hasHitBoss=True`, `remainingPierce`가 3→2로 정확히
      1만 감소함을 확인(잡몹과 보스를 같은 틱에 동시에 맞추지 않는 별개 테스트).
- [x] **회귀 - 마림바 Lv4 바운스**: `bounceOnMaxRange=true`, `maxRange=2f`로 초기화 후 `Update()`를
      반복 호출 → `traveledDistance`가 maxRange에 도달하는 순간 `direction`이 `(1,0,0)→(-1,0,0)`으로 정확히
      반전되고 `hasBounced=True`, `transform.eulerAngles.z`가 `0°→180°`로 정확히 갱신됨을 확인. 이후
      `traveledDistance`가 0부터 다시 누적되는 것도 확인.
- [x] **회귀 - 마림바 Lv5 감속+밀쳐냄**: `git diff` 확인 결과 `MarimbaWaveEffect.cs`(Lv5 `onHit` 콜백:
      `ApplyTemporarySlow(0.7f,1.0f)` + 밀쳐냄)는 이번 커밋에서 **전혀 수정되지 않음** - 호출부가 그대로인
      채 위에서 이미 실측한 "`onHitEnemy` 콜백이 정확히 1회, 올바른 (enemy, hitPos) 인자로 호출됨"이
      그대로 적용되므로 회귀 없음을 코드+실측 교차 확인.
- [x] **코드 diff 전체 검토**: `git diff HEAD -- PiercingBeamProjectile.cs` 결과, `CheckHits()`(관통/데미지/
      보스 판정)와 `Update()`의 바운스 분기는 프레임 순환 코드가 앞에 삽입된 것 외에 **한 글자도 변경되지
      않음**을 확인. 호출부 5개 파일(`TapAttackHelpers`/`PianoBeamEffect`/`BellStarburstEffect`/
      `MarimbaWaveEffect`/`ViolinOrbitEffect`) 전부 `git status`상 미변경(untouched) 확인.

**부가 관찰(테스트 중 발견, 코드 이슈 아님)**: 이전 `impact_burst_sprite_animation_test_guide.md`와 동일하게,
Unity 에디터 창이 OS 포커스를 잃은 채 Play Mode를 유지하면 `Time.time`이 거의 진행되지 않는 현상이 재현됨
(5초 실측 대기 후에도 `Time.time≈0.02`, `frameCount=2`). 이 때문에 실제 적 스폰(EnemySpawner 자동 스폰)을
기다리는 대신, `Update()`/`ApplyBeamFrame()` 비공개 메서드를 리플렉션으로 직접 반복 호출하는 결정론적
방식으로 전환해 검증함 - 프로젝트 코드 버그가 아니라 헤드리스 MCP 세션의 환경 특성.

**결론**: 4프레임 에너지 볼트 애니메이션이 피아노/벨/마림바/바이올린 참격에 정상 연동됨. 프레임 순서/
크기 정규화(이중 확대 버그 없음)/회전/폴백 전부 설계 의도대로 동작하며, 관통·데미지·보스 적중·바운스·
마림바 Lv5 감속+밀쳐냄 로직에 회귀 없음(코드 diff 확인 + 실측 교차 검증). 코드 변경 불필요, 추가 조치
없음. 테스트 중 생성한 임시 GameObject/스크린샷 파일은 모두 정리함(프로젝트에 남은 변경사항은
`Assets/Scripts/Combat/InstrumentAttacks/PiercingBeamProjectile.cs` 수정 + `Assets/Resources/Sprites/Effects/Beam/`의
신규 PNG 4개 + `.meta` 4개뿐).
