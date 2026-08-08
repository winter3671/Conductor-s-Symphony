# 첼로 중력장 11프레임 스월 애니메이션 연동 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

`CelloGravityFieldEffect`에 손그림 11프레임 스월 애니메이션
(`Assets/Resources/Sprites/Effects/GravityField/gravity_field1~11.png`)을 붙였습니다. 사용자가 단순
회전만으로는 밋밋할 것 같다며 프레임별로 형태가 변하는 애니메이션을 직접 그려서 제공.

- 0.08초 간격으로 11프레임을 순환 재생(약 0.88초 주기 루프). 홀드 중(`OnHoldTick`)과 릴리즈 후
  잔류시간(`Update`의 `isLingering`) 양쪽 다 공유 로직(`TickFieldLogic`)에서 프레임을 갱신하므로,
  Lv4 잔류시간 중에도 애니메이션이 끊기지 않고 계속 돕니다.
- **정렬 버그 사전 방지**: 프레임이 11장(두 자리 번호 포함)이라, 기존 다른 이펙트(Beam/ImpactBurst)에
  쓰던 `string.CompareOrdinal` 문자열 정렬을 그대로 썼다면 `field1, field10, field11, field2, field3...`
  순으로 잘못 정렬됐을 것입니다(사전식 비교라 자릿수를 모름 - 한 자리 번호만 있던 기존 에셋들에선
  드러나지 않았던 문제). 파일명 끝의 숫자를 추출해 정수로 비교하는 자연 정렬(`ExtractTrailingNumber`)로
  새로 작성해 미리 방지했습니다.
- **이중 확대 버그 사전 방지**: 기존엔 이 오브젝트의 루트 `transform.localScale = radius*0.9`를
  (a) 절차적 원 스프라이트의 시각 크기와 (b) 범위 링의 부모-스케일 상쇄(`1/0.9`) 두 가지 용도로
  동시에 재사용하고 있었습니다. 새 아트(500×494 캔버스, 콘텐츠가 캔버스를 거의 꽉 채움)는 기존
  절차적 원(28px 캔버스)과 bounds 크기가 완전히 다르기 때문에, 그 매직넘버를 그대로 뒀다면
  임팩트 버스트 때와 같은 이중 확대 버그가 났을 것입니다. 아트를 `CelloFieldArt`라는 별도 자식
  오브젝트로 분리해 `frame.bounds` 기준으로 독립적으로 크기를 계산하고(`ApplyFrame`), 루트
  트랜스폼은 identity로 되돌려 범위 링도 `radius`를 직접 곱하는 방식으로 단순화했습니다(과거 0.9
  상쇄 매직넘버 완전히 제거).
- **색상 처리**: 새 아트는 보라-파랑 톤으로 이미 색이 입혀져 있습니다. 첼로의 공식 색(갈색,
  `InstrumentPatternDatabase`에 정의되어 범위 링에 계속 사용됨)과는 의도적으로 다른 톤을 그대로
  쓰기로 사용자가 결정(2026-08-08) - "필드 이펙트 고유색"과 "악기 식별 링 색"을 분리한 것으로, 코드
  버그가 아닙니다. 색상 틴트(`sr.color`)는 걸지 않고 알파값만 0.45로 곱해 기존처럼 장판 아래 적이
  비쳐 보이는 반투명함만 유지합니다.
- 프레임 로딩 실패 시(에셋 미존재 등) 기존과 동일한 절차적 원(`CreateFilledCircle(28,13f,color)`)으로
  폴백하며, 이때 크기 계산도 기존 공식과 수학적으로 동일한 결과가 나오도록 정규화했습니다.

## 1. 사전 준비

1. `Assets/Resources/Sprites/Effects/GravityField/gravity_field1~11.png`에 아직 `.meta`가 없다면
   `refresh_unity(mode=force, compile=request)`로 최초 임포트.
2. Texture Type = **Sprite (2D and UI)** 확인. Sprite Mode는 Single이든 Multiple(자동 트림)이든
   상관없음 - 코드가 `frame.bounds`로 실제 크기를 매번 역산함(빔 테스트 때 이미 확인된 패턴).
3. `refresh_unity(force, compile=request)`로 재적용 후 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [ ] 첼로 홀드 시전 시 절차적 원 대신 새 11프레임 스월 아트가 보이는지
- [ ] 11프레임이 자연 정렬(`field1→2→...→11`) 순서로 순환하는지 - 특히 `field9→field10→field11`
      구간이 사전식 정렬처럼 `field1→field10→field11→field2`로 꼬이지 않는지 확인
- [ ] **크기 검증(핵심)**: `ApplyFrame()`을 리플렉션으로 호출해 각 프레임의 `fieldArtTransform.localScale
      × frame.bounds.size`(실제 화면 표시 지름)를 계산 - 콘텐츠 크기가 프레임마다 미세하게 달라도
      (실측: 478~480 x 472~474px) 최종 표시 지름이 `radius*0.9*0.28`로 일정하게 유지되는지, 그리고
      Lv2(범위+20%) 등 `radius`가 커질 때 그 비율 그대로 커지는지
- [ ] 범위 링(`CelloRangeRing`)이 실제 판정 반경(`radius`)과 정확히 일치하는지(기존과 동일한 방식,
      루트 스케일 단순화 이후에도 회귀 없는지)
- [ ] 홀드 중(`OnHoldTick`)과 Lv4 잔류시간 중(`Update`의 linger) 양쪽 다 애니메이션이 끊기지 않고
      계속 재생되는지
- [ ] 폴백 상황(에셋 없음 가정)에서 기존 절차적 원과 수학적으로 동일한 크기가 나오는지(회귀)
- [ ] **회귀**: 감속(`SetSpeedMultiplier`)/지속 피해 틱/Lv5 끌어당김/보스 틱 피해 로직이 이번 변경과
      무관하게 정상 동작하는지
- [ ] **회귀**: 필드 이탈 시 감속 해제가 정상 동작하는지

## 4. 검증 결과

**검증 일시**: 2026-08-08, Unity MCP 연결 세션(`My project@c018c67b9a01a4e5`, Unity 6000.5.5f1)

**1절 사전 준비**: `refresh_unity(force, compile=request)`로 11개 파일 최초 임포트 → `.meta` 11개 모두
자동 생성됨. Sprite Mode는 (이 프로젝트 기본값대로) `Multiple`로 임포트됐으나 문서가 이미 언급했듯
`frame.bounds` 기준 계산이라 무관.

### 🐛 발견된 버그 (수정 완료)

**자연 정렬(`ExtractTrailingNumber`)이 실제로는 전혀 동작하지 않고 있었음** - 문서가 방지하려던 바로
그 "1,10,11,2,3..." 순서 버그가 **다른 경로로 재발**한 상태였습니다.

- **원인**: Sprite Mode가 `Multiple`으로 임포트되면 Unity가 알파 트림된 서브스프라이트 이름 끝에
  자동으로 `_인덱스`를 붙입니다(예: `gravity_field10.png` → 서브스프라이트 이름 `gravity_field10_0`).
  `ExtractTrailingNumber()`는 문자열 "끝에서부터의 연속된 숫자"를 프레임 번호로 추출하는데, 실제
  런타임 이름은 전부 `_0`으로 끝나서(서브스프라이트 인덱스가 항상 0) **모든 프레임이 숫자 "0"으로
  추출되어 동률 처리**됐습니다. 이 상태에서 `Array.Sort`는 전부 동등한 키를 비교하므로 원본
  `Resources.LoadAll` 반환 순서를 거의 그대로 통과시키는데, 실측해보니 그 원본 순서가 마침 사전식
  문자열 순서(`field1, field10, field11, field2, field3, ..., field9`)였습니다 - 즉 자연 정렬 코드가
  있으나 마나 한 상태로, 정확히 막으려던 버그가 재발해 있었습니다.
- **실측 확인**: `EnsureGravityFrames()`를 리플렉션으로 직접 호출해 정적 필드 `gravityFrames`의 실제
  순서를 확인한 결과 `field1→field10→field11→field2→field3→...→field9`로 잘못 나왔습니다.
- **수정**: `ExtractTrailingNumber()`에서 숫자를 추출하기 전에, 이름 끝의 `_<숫자>` 형태(Unity가 붙인
  서브스프라이트 접미사)를 먼저 한 번 제거하도록 고쳤습니다(`CelloGravityFieldEffect.cs`). Single 모드로
  임포트되어 접미사가 없는 경우에도 정상 동작합니다(접미사가 없으면 제거 로직이 아무 것도 안 함).
- **재검증**: 수정 후 스크립트 재컴파일(에러 0건) → `EnsureGravityFrames()` 재호출 결과
  `field1→field2→field3→...→field10→field11`로 정확히 순서대로 나옴을 확인.

### 나머지 검증 항목

- [x] **크기 검증(핵심)**: `ApplyFrame()`을 11프레임 + 폴백 원 전부에 리플렉션으로 호출해
      `fieldArtTransform.localScale × frame.bounds.size`(실제 표시 지름)를 계산한 결과, 콘텐츠 최대
      치수가 4.80~4.82(프레임마다 미세하게 다름)로 달랐음에도 **12개 케이스(11프레임+폴백) 전부
      visDiameter=0.453600으로 완전히 동일**했습니다(`radius(1.8)×0.9×0.28`과 정확히 일치). 이중 확대
      버그 없음.
- [x] **Lv2 범위 확대**: `radius` Lv1=1.8 → Lv2=2.16(=1.8×1.2)로 정확히 반영, 범위 링
      (`CelloRangeRing`) 스케일도 정확히 2.16으로 일치.
- [x] **애니메이션 순환**: `OnHoldTick(0.02f)`를 20회 호출(0.4초 누적, `FrameInterval=0.08`) →
      `frameIndex=5`(gravity_field6)로 정확히 예상값과 일치.
- [x] **홀드/잔류 양쪽 애니메이션 지속**: `OnHoldReleased()` 호출 후 `isLingering=True`,
      `lingerTimer=1.0`(Lv1, 페르마타 없음 기준)으로 정확히 설정됨을 확인, 이후 `Update()`를 반복
      호출해도 `frameIndex`가 계속 진행됨(끊기지 않음)을 확인.
- [x] **범위 링 정확성**: 위 Lv2 테스트에서 이미 확인(회귀 없음, 단순화된 루트 identity 방식에서도
      `radius` 그대로 곱하는 것으로 정확).
- [x] **폴백 크기 회귀**: 위 12-way 비교에 폴백 원(`CreateFilledCircle(28,13f,...)`)도 포함되어 동일한
      0.453600 값으로 확인됨.
- [x] **회귀 - 감속/지속 피해/Lv5 끌어당김/보스 틱 피해**: 가짜 `EnemyMonster`+`BossMonster`를 배치해
      Lv5 필드로 실측. `SetSpeedMultiplier`가 정확히 `1-slowFraction`(Lv5=0.4)로 걸리고, 틱 간격(0.4초)
      마다 데미지(15)가 적/보스 양쪽에 정확히 들어가며, Lv5 끌어당김도 `PullStrength(1.5)×deltaTime`만큼
      정확히 중심으로 이동함을 확인(주의: 필드가 `GetNearestTargetPosition`으로 가장 가까운 적 위치에
      생성되므로, 첫 테스트에서 적이 필드 중심과 겹쳐 끌어당김이 관찰 안 됐던 것은 테스트 설계 문제였고
      - 적을 필드 생성 후 이동시켜 거리를 벌린 재테스트에서 정확히 확인됨).
- [x] **회귀 - 필드 이탈 시 감속 해제**: 적을 사거리 밖으로 이동 후 틱 → `speedMultiplier`가 정확히
      `1.0`으로 복원됨을 확인.

**부가 관찰**: 이전 문서들과 동일하게 에디터 창이 포커스를 잃은 상태에서 Play Mode의 `Time.time`이
거의 진행되지 않는 환경 특성이 재현되어(또한 `Destroy()`가 실제 프레임 종료 시점까지 지연되어 이
환경에서는 거의 반영되지 않음), 실제 씬 틱 대신 `OnHoldTick`/`Update`/`ApplyFrame` 등 비공개 메서드를
리플렉션으로 직접 반복 호출하는 결정론적 방식으로 검증함. 삭제가 필요한 테스트 객체는 `Destroy()` 대신
`DestroyImmediate()`를 사용해 확실히 정리함.

**결론**: 11프레임 스월 애니메이션 자체의 크기 정규화/링/틱 로직/보스 처리는 전부 정상이었으나,
**자연 정렬 로직에 실제 버그가 있어 수정**했습니다(위 참고). 수정 후 재검증 완료. 감속·지속피해·Lv5
끌어당김·보스 피해·감속 해제 전부 회귀 없음. 테스트 중 생성한 임시 GameObject/스크린샷 파일은 모두
정리함. **코드 변경 있음**: `Assets/Scripts/Combat/InstrumentAttacks/CelloGravityFieldEffect.cs`의
`ExtractTrailingNumber()` 수정.
