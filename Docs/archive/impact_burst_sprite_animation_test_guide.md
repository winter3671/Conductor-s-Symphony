# 임팩트 버스트 9프레임 애니메이션 연동 (글록켄슈필/팀파니) - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

`AreaImpactEffect`(글록켄슈필 별빛 낙하, 팀파니 캐논/융단폭격이 공용으로 쓰는 "예고 후 착탄" 이펙트)가
지금까지는 프로시저럴 다이아몬드 하나를 `transform.localScale`만으로 키우는 방식이었는데, 나노바나나로
그리고 영상 프레임 추출+배경제거로 만든 9프레임 무채색 애니메이션(`Assets/Resources/Sprites/Effects/
ImpactBurst/spark1~9.png` - 작게 시작 → spark5에서 최대로 만개 → 다시 작아지며 소멸)을 붙였습니다.

- **예고(딜레이) 구간**: 진행도 0→1을 프레임 0→4(spark1→spark5)에 매핑. 착탄 순간에 정확히 가장
  만개한 프레임(spark5)이 보이도록 맞췄습니다.
- **착탄 후 플래시 구간**: 기존엔 `Destroy(gameObject, 0.12f)`로 마지막 프레임이 잠깐 정지된 채
  보이다 사라졌는데, 이제 0.2초 동안 프레임 4→8(spark5→spark9)을 재생해 실제로 만개했다가 사그라드는
  모습을 보여줍니다.
- **폴백**: `Resources.LoadAll<Sprite>("Sprites/Effects/ImpactBurst")`가 비어있으면(에셋 미임포트 등)
  기존처럼 `Initialize()`로 전달받은 단일 스프라이트(다이아몬드)를 그대로 씁니다. 호출부
  (`TapAttackHelpers.SpawnImpact`, `TimpaniBombardmentEffect.SpawnImpact`)는 전혀 건드리지 않았습니다.
- **색**: 프레임 자체는 무채색(흰색~밝은 회색)이라, 기존과 동일하게 `SpriteRenderer.color`로 악기별
  테마 컬러가 그대로 곱해집니다.
- 글록켄슈필 Lv5 유도 파편(`HomingShrapnelProjectile`, `TapAttackHelpers.StarSprite` 직접 사용)은 이
  애니메이션과 무관 - 날아가는 투사체라 정지 다이아몬드 그대로 둡니다.

## 1. 사전 준비 (Unity 에디터에서 수행)

1. `Assets/Resources/Sprites/Effects/ImpactBurst/spark1~9.png` 9개 파일에 아직 `.meta`가 없습니다
   (한 번도 임포트 안 됨). `refresh_unity(mode=force, compile=request)`로 최초 임포트.
2. 9개 파일 전부: Texture Type = **Sprite (2D and UI)**, Sprite Mode = **Single**. Pixels Per Unit은
   9개 전부 서로 동일하기만 하면 되고(프레임끼리 스케일이 안 맞으면 재생 중 크기가 들쭉날쭉해짐),
   Pivot은 기본값(Center) 유지.
   - 배경제거가 이미 잘 되어있고(전 프레임 캔버스 500×508, 별 중심 좌표 (253,253) 근처로 프레임 간
     흔들림 없음 - Python/PIL로 사전 확인함), 별도 크롭/트림은 필요 없습니다. 오히려 프레임마다 다르게
     트림하면 애니메이션 중심이 흔들리니 **하지 마세요**.
3. `refresh_unity(force, compile=request)`로 재적용 후 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [ ] 글록켄슈필 판정 성공 시, 낙하 예고~착탄까지 다이아몬드 대신 스파크 애니메이션이 재생되는지
- [ ] 팀파니 홀드 시작(즉발 캐논) + 홀드 중 융단폭격 착탄 각각에서도 애니메이션이 재생되는지
- [ ] 착탄 순간(데미지가 실제로 들어가는 시점)에 프레임이 최대로 만개한 모양(spark5 부근)인지
- [ ] 착탄 후 약 0.2초간 만개→소멸하는 플래시가 자연스럽게 보이는지(딱 멈춘 정지 프레임이 아니라)
- [ ] 악기별 테마 컬러(글록켄슈필 옅은 청록, 팀파니 갈색 계열)가 스파크에 정상적으로 틴트되는지
- [ ] 9프레임 로딩 순서가 올바른지(파일명 정렬 spark1→spark9 = 실제 애니메이션 진행 순서와 일치하는지,
      뒤죽박죽 재생되지 않는지)
- [ ] **회귀**: 데미지/판정 로직(스플래시 반경, Lv4 버스트+2, 레가토 추가 낙하, 팀파니 기절/지진지대
      등)은 이번 변경과 무관하게 그대로인지
- [ ] **회귀**: `transform.localScale`로 표현되는 예고 크기 확대(`radius * 1.6`)는 기존과 동일하게
      동작하는지(프레임 변경은 스케일과 독립적으로 추가된 것)
- [ ] **회귀**: 글록켄슈필 Lv5 유도 파편(`HomingShrapnelProjectile`)은 여전히 기존 정지 다이아몬드
      스프라이트를 쓰는지(이번 변경 대상이 아님)
- [ ] **폴백 확인(선택)**: 에셋 경로를 임시로 틀리게 하거나 폴더를 비웠을 때, 에러 없이 기존 다이아몬드
      스프라이트로 정상 폴백되는지

## 4. 검증 결과

**검증 일시**: 2026-08-08, Unity MCP 연결 세션(`My project@c018c67b9a01a4e5`, Unity 6000.5.5f1)

**1절 사전 준비**: `refresh_unity(force, compile=request)`로 9개 파일 최초 임포트 → `.meta` 9개 모두
자동 생성됨(Texture Type=Sprite(2D and UI), Sprite Mode=Single, PPU=100, Filter Mode=Bilinear, Pivot=
Center 전부 동일 확인). 컴파일 에러/경고 0건.

- 특이사항: 임포트 결과 각 프레임의 `rect`가 전체 캔버스(500×508)가 아니라 알파 내용에 맞춰 개별
  트림된 상태로 나타남(spark1 63×65 ~ spark5 460×472 ~ spark9 26×27, 프레임마다 내용 크기가 다르므로
  당연함). 문서 2절 우려("프레임마다 다르게 트림하면 중심이 흔들림")를 실측으로 재확인한 결과, 9개
  프레임의 트림된 rect 중심 좌표가 전부 (253±1, 253~256)로 사실상 동일했음(alignment=Center이므로
  pivot이 각 프레임의 트림된 rect 중심을 가리킴). 즉 별 모양이 원본에서 (253,253) 기준 방사대칭이라
  자동 트림돼도 애니메이션 중심이 흔들리지 않음 - 실측으로 안전 확인됨. 다만 이 자동 트림이 어떤
  메커니즘으로 발생했는지는(수동 Sprite Editor Trim을 호출하지 않았음) 특정하지 못함 - 프로젝트의
  텍스처 임포트 프리셋/이전 세션 흔적일 가능성. 문제는 없으나 참고용으로 남김.

- [x] **글록켄슈필/팀파니 착탄 시 스파크 애니메이션 재생**: Play Mode에서 `AreaImpactEffect.Initialize()`를
      직접 호출해 실측. 초반(progress 47.8%) 스크린샷에서 다이아몬드가 아닌 8방향 별 모양 스파크
      프레임(`spark3_0`)이 렌더링됨을 확인. `transform.localScale`도 코드의 Lerp 공식(0.35 → radius×1.6)
      예측값과 정확히 일치(0.7943).
- [x] **팀파니도 동일하게 재생**: 같은 씬에 팀파니 색(갈색 계열)으로 별도 인스턴스를 동시 스폰,
      스크린샷에서 글록켄슈필(청록)과 팀파니(갈색) 두 스파크가 나란히 정상 렌더링됨을 확인
      (`TapAttackHelpers.SpawnImpact`/`TimpaniBombardmentEffect.SpawnImpact` 모두 동일한
      `AreaImpactEffect.Initialize` 경로를 타므로 로직 자체는 공용 - 코드 검토로 교차 확인).
- [x] **착탄 순간 = spark5(최대로 만개)**: 실시간 재생은 에디터 창이 포커스를 잃어 Update 루프가
      정지되는 현상이 있어(자세한 내용 하단 참고) 정밀 타이밍 캡처가 어려워, 리플렉션으로
      `ApplyFrame(float)` 비공개 메서드를 직접 호출해 결정론적으로 매핑을 검증함:
      `ApplyFrame(0.0/0.1/0.25/0.4/0.5)` → `spark1/spark2/spark3/spark4/spark5`. 예고 구간 진행도
      t=1.0(착탄 직전)일 때 실제 `Update()`가 넘기는 인자는 `t*0.5=0.5` → `spark5_0` 정확히 일치.
- [x] **착탄 후 0.2초 만개→소멸 플래시**: 같은 방식으로 플래시 구간 매핑
      (`ApplyFrame(0.5 + ft*0.5)`, ft=0/0.25/0.5/0.75/1) → `spark5/spark6/spark7/spark8/spark9`로 순서대로
      정확히 진행됨을 확인. 정지 프레임이 아니라 5→9 프레임이 순서대로 넘어가는 실제 애니메이션임.
- [x] **테마 컬러 틴트**: 스크린샷에서 글록켄슈필(옅은 청록 `(0.4,1.0,0.8)`)과 팀파니(갈색
      `(0.7,0.4,0.2)`) 색이 무채색 스파크 프레임에 정상적으로 곱해져 표시됨을 육안 확인(코드상
      `SpriteRenderer.color = color`로 기존과 동일 방식이라 회귀 없음도 같이 확인됨).
- [x] **9프레임 로딩 순서**: `Resources.LoadAll("Sprites/Effects/ImpactBurst")` 실행 결과 9개 전부 로드,
      `System.Array.Sort(..., string.CompareOrdinal)`로 정렬 후 이름이 `spark1_0 → spark2_0 → ... →
      spark9_0` 순서로 정확히 나열됨(한 자리 숫자만 있어 문자열 정렬로도 뒤섞이지 않음). 위 두 항목의
      프레임 매핑 테스트도 이 정렬된 배열 기준으로 순서대로 나온 것이라 재확인됨.
- [x] **회귀 - 데미지/판정 로직**: `AreaImpactEffect.Impact()`의 피해 순회/스플래시 반경/`onHitEnemy`/
      `onImpact` 콜백 호출부는 이번 변경에서 프레임 관련 코드만 앞뒤로 추가됐을 뿐 로직 자체는 한 글자도
      바뀌지 않음을 코드 diff로 직접 확인(팀파니 Lv4 기절, Lv5 지진지대, 글록켄슈필 Lv3 스플래시/Lv4
      버스트+2/레가토 등은 `AreaImpactEffect` 바깥의 호출부 코드라 더더욱 무관). 별도 라이브 전투 통합
      테스트(실제 적 스폰 후 피해 확인)는 이번 변경 범위 밖이라 생략.
- [x] **회귀 - localScale 확대**: `radius * 1.6` 스케일 로직도 프레임 변경과 별개 줄로 그대로 유지됨을
      코드로 확인, 위 실측 스크린샷의 scale=0.7943 값도 이 공식과 정확히 일치해 실제로도 정상 동작함을
      재확인.
- [x] **회귀 - 글록켄슈필 Lv5 유도 파편**: `HomingShrapnelProjectile.cs`는 이번 커밋에서 전혀 수정되지
      않았고, `Initialize()`가 호출부(`GlockenspielStarfallEffect.cs:49`)로부터 그대로 전달받은
      `TapAttackHelpers.StarSprite`(정지 다이아몬드)를 그대로 `SpriteRenderer.sprite`에 대입할 뿐
      `AreaImpactEffect`/`burstFrames`와는 완전히 무관한 코드 경로임을 확인.
- [x] **폴백 확인**: `burstFrames`/`triedLoadFrames` 정적 필드를 리플렉션으로 저장해두고 일시적으로
      `burstFrames=null, triedLoadFrames=true`(로드 실패/에셋 없음 상황 재현)로 바꾼 뒤
      `Initialize()` 호출 → `SpriteRenderer.sprite`가 정확히 `Initialize()`에 전달한 폴백 스프라이트
      (다이아몬드) 그대로 유지됨을 참조 동일성으로 확인. 이후 `ApplyFrame(0.3)`, `ApplyFrame(0.9)`를
      호출해도(예고~플래시 전 구간 시뮬레이션) `burstFrames`가 비어 있으므로 얼리 리턴되어 계속
      다이아몬드 그대로였고 예외도 없었음. 테스트 후 정적 필드는 원래 캐시된 9프레임으로 복원함(실제
      게임에는 영향 없음).

**부가 관찰(테스트 중 발견, 코드 이슈 아님)**: Unity 에디터 창이 OS 포커스를 잃은 상태로 Play Mode를
유지하면 `Time.time`이 거의 진행되지 않는(스크린샷/메뉴 호출 등 특정 트리거 시점에만 간헐적으로
점프하는) 현상이 관찰됨. 이 때문에 실시간 관찰 대신 리플렉션 기반 결정론적 검증으로 전환했음 - 이
프로젝트 코드의 버그가 아니라 헤드리스 MCP 세션에서 에디터 창이 백그라운드에 있을 때 나타나는 환경
특성으로 보임(다음 실측 세션에서도 재현되면 참고).

**결론**: 임팩트 버스트 9프레임 애니메이션이 글록켄슈필/팀파니 착탄 이펙트에 정상 연동됨. 프레임
순서/매핑/색 틴트/폴백 전부 설계 의도대로 동작하며 기존 데미지·스케일·글록켄슈필 유도 파편 로직에
회귀 없음. 코드 변경 불필요, 추가 조치 없음. 테스트 중 생성한 임시 GameObject와 스크린샷 파일은 모두
정리함(프로젝트에 남은 변경사항은 `Assets/Scripts/Combat/InstrumentAttacks/AreaImpactEffect.cs` 수정
+ `Assets/Resources/Sprites/Effects/ImpactBurst/`의 신규 PNG 9개 + `.meta` 9개뿐).
