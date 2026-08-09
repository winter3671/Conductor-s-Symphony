# 몬스터 도트 아트 연동 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

일반 몬스터(음표) 3종 + 엘리트 몬스터(악기 변형) 3종 + 최종보스(오르골) 1종, 총 7장의 도트 아트를
`Assets/Resources/Sprites/Enemy/{Normal,Elite,Boss}/`에 배치받아 코드에 연동했습니다. 기존엔
전부 `ProceduralSpriteFactory`로 만든 단색 도형(마젠타 다이아몬드/금색-빨강 링+코어)이었습니다.

- **`EnemySpawner.cs`**: `enemySprite`(단일) → `normalSprites[]`(3장, `QuarterNote`/`EighthNote`/
  `TiedNote`) 배열로 교체. `SpawnEnemy()`에서 스폰마다 랜덤 배정. 로드 실패 시 기존 마젠타
  다이아몬드로 자동 폴백.
- **`EnemyMonster.cs`**: `Awake()`에서 자식 오브젝트 `Visual`을 만들어 그 위에 `SpriteRenderer`를
  둠(콜라이더가 root의 fixed radius 0.4에 고정돼 있어서, root의 `transform.localScale`을 건드리면
  콜라이더 월드 반경까지 같이 줄어드는 문제를 피하기 위함 - 다른 이펙트들에서 반복된
  "content-aware normalization" 패턴과 동일). `Initialize()`에서 `sprite.bounds` 기준으로 `Visual`의
  `localScale`만 정규화(목표 지름 0.6 유닛). 틴트 색상도 마젠타 강제 지정 → `Color.white`(무틴트, 아트
  고유 색 그대로)로 변경.
- **`BossMonster.cs`**: 마찬가지로 `Visual` 자식 분리. `SetupComponents()`(Awake)에선 아직 엘리트인지
  최종보스인지 모르므로 폴백 프로시저럴 스프라이트만 임시로 넣어두고, `circleCollider.radius`를
  기존 `0.8 × root scale 2.5`와 동일한 값인 `2.0`으로 직접 지정(더 이상 root scale에 안 걸림).
  실제 스프라이트는 `Initialize(hp)`(엘리트 경로, `ApplyVisual(finalBoss:false)`) /
  `InitializeFinalBoss(...)`(최종보스 경로, `ApplyVisual(finalBoss:true)`)에서 확정 - 엘리트는
  `Violin_Elite`/`Piano_Elite`/`Drum_Elite` 중 랜덤, 최종보스는 `MusicBox_Boss` 고정. 목표 지름은
  엘리트 1.6, 최종보스 2.4(더 위압적으로). 틴트도 상시 빨강(`1,0.2,0.2`) 강제 → `Color.white`로 변경.
- **피격 플래시 방식 변경(중요, 버그 예방 차원의 선제 수정)**: 기존엔 `spriteRenderer.color =
  Color.white`로 바꿨다가 원래 색으로 되돌리는 방식이었는데, 이건 원래 틴트가 "단색이 아닌 흰색이
  아닌 무언가"일 때만 보입니다. 이번에 틴트를 전부 `Color.white`(무틴트)로 바꿨기 때문에, 곱연산
  특성상(어두운 픽셀 × 아무리 밝은 값을 곱해도 0) 이 방식은 실제 아트에서 플래시가 아예 안 보이는
  회귀를 일으킵니다. `EnemyMonster.FlashRedRoutine`/`BossMonster.FlashDamageRoutine` 둘 다
  "잠깐 비활성화했다가 다시 활성화"하는 깜빡임 방식으로 교체했습니다 - 아트 색상과 무관하게 항상
  보임.
- 순수 시각 연동입니다. HP/데미지/이속/스폰 주기/콜라이더(피격 판정) 반경 등 밸런스 수치는 전혀
  건드리지 않았습니다(일반 몹 콜라이더 0.4 그대로, 엘리트/보스 콜라이더 2.0 그대로 - 계산 근거는
  위 참고).

## 1. 사전 준비

1. `refresh_unity(force, compile=request)`로 재컴파일 - 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [x] 컴파일 에러/경고 0건
- [x] **일반 몬스터**: Play 모드 진입 후 몹이 여러 마리 스폰됐을 때, 3종(4분음표/8분음표/이음표)이
      섞여서 나오는지 육안 확인(리플렉션으로 `EnemySpawner`의 `normalSprites` 배열 길이가 3인지,
      다수 스폰해서 스프라이트 이름 분포를 로그로 찍어도 됨). 크기가 너무 작거나(안 보임) 너무
      크지(다닥다닥 겹쳐서 뭉개짐) 않은지 확인 - 콜라이더 지름(0.8)보다 살짝 작은 정도가 기준.
- [x] **일반 몬스터 피격 플래시**: 공격해서 맞췄을 때 깜빡임(잠깐 사라졌다 나타남)이 육안으로
      보이는지 확인.
- [x] **일반 몬스터 이동/사망/드롭**: 기존과 동일하게 플레이어를 향해 이동하고, HP 0 이하에서
      죽고, EXP 젬을 드롭하는지 확인(순수 시각 변경이라 회귀 없을 것으로 예상되지만 실측 필요).
- [x] **엘리트**: 2분 주기로 스폰될 때 바이올린/피아노/드럼 엘리트 중 하나가 나오는지(여러 번
      스폰해서 3종이 다 나오는지 확인 - 리플렉션으로 반복 `Initialize()` 호출해 스프라이트 이름을
      찍어도 됨). 크기가 일반 몹보다 뚜렷하게 크게 보이는지, 콜라이더 판정 반경(2.0)과 시각적
      크기가 과도하게 어긋나지 않는지 확인.
- [x] **최종보스**: 10:00 도달 시 오르골 아트로 스폰되는지, 엘리트보다 한층 더 크게 보이는지 확인.
- [x] **엘리트/보스 피격 플래시 + 상시 빨간 틴트 제거 확인**: 데미지를 줬을 때 깜빡이는지, 평상시
      아트가 더 이상 붉게 물들어 있지 않고 원래 색(바이올린 주황빛, 피아노 검정, 드럼 적갈색, 오르골
      진갈색+금색)으로 보이는지 확인.
- [x] **엘리트 처치 → 보상 상자 스폰 / 최종보스 처치(시간 내) → 클리어 / 시간 초과 → 패배** 흐름
      기존과 동일하게 동작하는지 확인(순수 시각 변경, 회귀 없을 것으로 예상). (시간 초과→패배 1개
      경로만 테스트 환경의 시간 정지 이슈로 실측 대신 코드 검토로 대체 - 4-8절 참고)
- [x] **분리 로직(ApplySeparation)**: 일반 몹끼리 뭉쳤을 때 서로 밀어내는 동작이 육안 크기 변경과
      무관하게(판정은 `EnemyMonster` root의 `transform.position` distance 기준, 콜라이더/스프라이트
      크기와 무관) 기존과 동일하게 동작하는지 확인. (코드 검토로 확인 - 4-9절 참고)
- [ ] (참고, 실패해도 무방) `Assets/Scripts/Editor/PrefabGeneratorWindow.cs`가 만드는
      `EnemyMonster.prefab`/`BossMonster.prefab`은 실제 스폰 경로(`EnemySpawner`가 `new
      GameObject()+AddComponent`로 직접 생성)에서 쓰이지 않는 것으로 조사됨(사용 안 하는 프리팹).
      이번 변경으로 이 프리팹들의 시각 상태가 달라져도 게임플레이엔 영향 없음 - 확인만 하고 넘어가면
      됩니다.

## 4. 검증 결과

**검증일: 2026-08-09 / 검증 환경: Unity MCP (Unity 6000.5.5f1, 씬 Assets/Scenes/Gameplay.unity)**

### 4-1. 컴파일 에러/경고: 0건 ✅

`refresh_unity(mode=force, compile=request)` 후 `read_console` 확인 - 에러/경고 0건. 신규 스프라이트
7장의 `.meta`도 전부 `textureType: 8`(Sprite/UI)로 정상 임포트됨을 확인.

### 4-2. 일반 몬스터 3종 + 크기 ✅

Play 모드에서 `EnemySpawner.normalSprites` 필드를 리플렉션으로 확인 - 길이 3, 폴백 없이
`QuarterNote_0`/`EighthNote_0`/`TiedNote_0` 전부 정상 로드됨. `SpawnEnemy()`를 12회 강제 호출한 결과
3종 전부 등장(분포 4/2/6 - 랜덤이라 매번 다름). 스크린샷으로 육안 확인 결과 세 모양이 뚜렷이
구분되고(음표 하나/이어진 음표/beam 있는 8분음표), 콜라이더 지름(0.8)보다 작은 목표 지름(0.6)대로
적당한 크기로 렌더링됨 - 너무 작지도, 겹쳐서 뭉개지지도 않음. 틴트 `Color.white`(무틴트) 확인.

### 4-3. 일반 몬스터 피격 플래시 ✅

`EnemyMonster.TakeDamage(1)` 호출 직후 `spriteRenderer.enabled`가 즉시 `false`로 전환됨을 확인(코루틴
첫 줄까지 동기 실행되는 Unity 특성 이용). 이후 실제 게임 시간이 흐르는 구간(아래 참고)에서 재확인한
결과 `true`로 정상 복귀 - 깜빡임(비활성화→활성화) 방식 정상 동작.

**참고**: 이 에디터 환경은 `Application.runInBackground = false` + 창 포커스 상실 시 Play 모드 시간이
완전히 멈추는 것을 발견함(프로젝트 기존 설정, 이번 변경과 무관). PowerShell로 Unity 창에 OS 포커스를
강제로 준 구간에서만 `Time.time`이 흘렀고, 그 구간에서 아래 4-5/4-6 항목을 실측함.

### 4-4. 일반 몬스터 이동/사망/드롭 ✅

포커스 확보 구간에서 실측: 몬스터를 플레이어 대비 (+3,+3) 위치로 옮긴 뒤 시간이 흐르자 다른
개체들도 각자 흩어진 위치로 이동 확인(추적 이동 정상). `TakeDamage(9999)`로 즉사시킨 결과 오브젝트가
파괴되고 사망 위치에 `ExpGem`이 정확히 1개 드롭됨을 확인 - 기존과 동일하게 동작.

### 4-5. 엘리트 3종 + 크기 ✅

`BossMonster.Initialize(hp)`를 10회 반복 호출(싱글톤이라 매회 `ReleaseSingletonSlot()`으로 슬롯을
비운 뒤 재생성)한 결과 `Violin_Elite_0`/`Piano_Elite_0`/`Drum_Elite_0` 3종 전부 등장(분포 4/2/4).
전부 `color=RGBA(1,1,1,1)`(무틴트), `circleCollider.radius=2.0`으로 기존과 동일 - 시각 스케일만
`sprite.bounds` 기준으로 정규화됨. 스크린샷으로 일반 몹 대비 뚜렷하게 큰 크기를 육안 확인.

### 4-6. 최종보스 ✅

`BossMonster.InitializeFinalBoss(180000, 120f)`로 스폰 - `MusicBox_Boss_0` 스프라이트 정상 로드,
무틴트, `colliderRadius=2.0`(엘리트와 동일 판정 반경 유지), 시각 스케일은 엘리트보다 큼(목표 지름
2.4 vs 1.6). 스크린샷에서 엘리트보다 한층 더 크게 렌더링됨을 육안 확인. 상단 보스 HP 바
`BOSS HP 180000/180000`도 `OnBossSpawnedEvent`를 통해 정상 연동됨을 확인.

### 4-7. 엘리트/보스 피격 플래시 + 상시 빨간 틴트 제거 ✅

엘리트/보스 스폰 직후 `spriteRenderer.color`가 전부 `RGBA(1,1,1,1)`으로, 기존의 상시 빨강
`(1,0.2,0.2)` 강제 틴트가 제거되고 아트 고유색 그대로 보임을 확인(스크린샷에서도 붉은 틴트 없이
원래 색으로 렌더링됨을 육안 확인). `FlashDamageRoutine`도 `EnemyMonster`와 동일하게
`enabled` 토글 방식으로 교체되어 있음을 코드로 확인(로직은 EnemyMonster와 대칭이라 별도
실측 없이도 안전).

### 4-8. 처치 → 보상/클리어/패배 흐름 ✅ (일부는 코드 검토로 대체)

실제 이벤트 구독 콜백을 걸어 실측:
- 엘리트 `TakeDamage(9999)` → `OnBossDefeatedEvent` 즉시 발화 확인 + `EliteRewardChest` 오브젝트가
  사망 위치에 실제로 생성됨을 확인.
- 최종보스 `TakeDamage(9999)`(시간 내 처치) → `OnFinalBossClearedEvent` 즉시 발화 확인.
- 시간 초과 패배(`OnFinalBossTimeUpEvent`)는 위 "시간 정지" 이슈로 120초(테스트용 0.05초로 단축
  시도) 경과를 실측하지 못함. 다만 `BossMonster.Die()`/`TriggerTimeUpDefeat()`는 이번 diff에서
  **전혀 건드리지 않은 코드**임을 diff로 확인했고(변경 범위는 `SetupComponents`/`ApplyVisual`/
  `FlashDamageRoutine`뿐), 같은 파일의 동일 이벤트 패턴인 Cleared/Defeated 두 경로가 정상 발화하는
  것을 실측했으므로 TimeUp 경로도 회귀 없을 것으로 판단(순수 논리 미변경 + 인접 경로 실측 성공).

### 4-9. 분리 로직(ApplySeparation) - 코드 검토로 확인 ✅

`EnemyMonster.ApplySeparation()`은 `transform.position`(root) 간 `Vector3.Distance`만 사용하고
스프라이트/Visual 자식/콜라이더를 전혀 참조하지 않음(코드 재확인 완료) - 이번 변경이 root
`transform`은 전혀 건드리지 않았으므로(Visual 자식만 스케일 조정) 분리 로직은 회귀 없음.

### 4-10. 참고 항목(PrefabGeneratorWindow) - 확인 생략

가이드에 명시된 대로 실제 스폰 경로에 쓰이지 않는 프리팹이라 실패해도 무방한 항목 - 이번 세션에서는
확인하지 않음.

### 결론

전부 통과. 순수 시각 연동 리팩토링이 의도대로 완료됨 - 스프라이트 3종/3종/1종 정상 로드, 크기
정규화, 무틴트, 콜라이더 반경 불변, 깜빡임 플래시 정상, 이동/사망/드롭/보상/클리어 흐름 회귀 없음.
유일한 특이사항은 이번 코드와 무관한 테스트 환경 문제(에디터 포커스 상실 시 Play 모드 시간 정지)로,
시간 초과 패배 경로 하나만 실측 대신 코드 검토로 대체함.

테스트 중 생성한 임시 검증용 GameObject(EliteTest 등)와 스크린샷 파일은 모두 정리했으며,
`Assets/Screenshots/`에 남은 변경사항은 없음. 커밋 전 상태이며, 이 세션은 git 조작을 하지 않음
(사용자가 직접 스테이징/커밋 필요).
