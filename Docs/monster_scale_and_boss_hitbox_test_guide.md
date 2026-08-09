# 엘리트/보스 시각 크기 확대 + 지휘자 축소 + 판정 범위 동기화 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단(4절)에 결과를 추가로 append하고, `archive/`로 옮겨주세요.

## 0. 무엇을 고쳤나

`archive/monster_art_integration_test_guide.md`(일반/엘리트/보스 몬스터 도트 아트 연동) 검증 이후
"몬스터가 너무 작아서 안 보인다"는 사용자 피드백에서 시작된 후속 조정입니다.

- **`Assets/Scripts/Enemy/EnemyMonster.cs`**: `ArtVisualScale` 1 → 2.5 (일반 몬스터 시각 지름이
  기존 대비 2.5배로 커짐. `ReferenceContentSize`(0.6) 자체는 안 건드림 - 순수 시각 배율 튜너블만 조정).
- **`Assets/Scripts/Enemy/BossMonster.cs`**:
  - `EliteReferenceContentSize` 1.6 → 3.2, `BossReferenceContentSize` 2.4 → 4.8 (둘 다 2배).
  - `HitboxRadius` 프로퍼티 신설 (`(isFinalBoss ? BossReferenceContentSize : EliteReferenceContentSize) / 2f`,
    즉 현재 비주얼 반지름 그대로 - 엘리트 1.6 / 최종보스 2.4).
  - `Initialize()`/`InitializeFinalBoss()`에서 `circleCollider.radius`를 `Mathf.Max(HitboxRadius,
    2.0f)`로 설정 - 보스(2.4)는 커진 비주얼만큼 몸박 판정도 늘어나지만, 엘리트는 새 비주얼
    반지름(1.6)이 기존 고정값(2.0)보다 오히려 작아서 그대로 썼다면 "늘려달라"는 요청과 반대로
    몸박 판정이 줄어드는 역효과가 생겼을 것 - 하한선을 둬서 엘리트는 기존 2.0 그대로 유지(회귀 없음),
    보스만 실질적으로 늘어남.
- **`Assets/Scripts/Player/PlayerController.cs`**: `targetWorldHeight` 기본값 1.0 → 0.5 → (너무 작다는
  피드백으로 1.2배 재확대) → 0.6. 단, 이 필드는 `[SerializeField]`라 씬/프리팹에 저장된 값이 코드
  기본값을 덮어쓰므로 아래도 함께 수정:
  - `Assets/Scenes/Gameplay.unity`의 `PlayerController.targetWorldHeight`: 1.8 → 0.9 → 1.08 (실제
    게임에 적용되는 값 - **여기가 진짜 중요**, 코드 기본값만 봐서는 반영 안 됨).
  - `Assets/Prefabs/Player/Player.prefab`의 동일 필드: 1 → 0.5 → 0.6 (미사용 프리팹이지만 값 일관성 유지).
- **공격 판정 보정(사이드이펙트 발견+수정)**: 이 프로젝트의 모든 원거리/AoE 공격 이펙트는 물리
  콜라이더가 아니라 `Vector3.Distance(공격원점, 적.transform.position) <= radius` 순수 좌표 거리
  비교로 명중을 판정합니다. 즉 보스/엘리트를 항상 반지름 0인 점으로 취급 - 몸통이 아무리 커져도
  판정 거리엔 전혀 반영되지 않았습니다. 아래 7개 파일의 `BossMonster.Instance` 대상 거리 비교에
  `+ BossMonster.Instance.HitboxRadius`를 더해 보정했습니다(일반 `EnemyMonster` 대상 판정은 이번
  요청 범위 밖이라 손대지 않음):
  - `Assets/Scripts/Combat/InstrumentAttacks/AreaImpactEffect.cs`
  - `Assets/Scripts/Combat/InstrumentAttacks/CelloGravityFieldEffect.cs`
  - `Assets/Scripts/Combat/InstrumentAttacks/DrumBeatBangEffect.cs`
  - `Assets/Scripts/Combat/InstrumentAttacks/FrenchHornConeEffect.cs` (부채꼴 `range` 비교에 더함,
    각도 조건은 그대로)
  - `Assets/Scripts/Combat/InstrumentAttacks/LingeringZoneEffect.cs`
  - `Assets/Scripts/Combat/InstrumentAttacks/PiercingBeamProjectile.cs`
  - `Assets/Scripts/Combat/InstrumentAttacks/ViolinOrbitEffect.cs` (기존 `+ 0.4f` 칼날 여유는
    유지한 채 추가)
- HP/데미지/이동속도/스폰 주기 등 다른 밸런스 수치는 건드리지 않았습니다.

## 1. 사전 준비

1. `refresh_unity(force, compile=request)`로 재컴파일 - 컴파일 에러/경고 0건 확인.

## 2. 검증 항목

- [ ] 컴파일 에러/경고 0건
- [ ] **일반 몬스터 크기**: 스폰된 일반 몹의 시각 지름이 기존보다 뚜렷하게 커졌는지(약 2.5배) 육안
      확인. 콜라이더(0.4)는 그대로라 서로 다닥다닥 겹쳐도 판정 자체는 기존과 동일해야 함.
- [ ] **엘리트 크기**: 일반 몹보다 훨씬 크게(약 2배 확대 반영) 보이는지, 3종(바이올린/피아노/드럼)
      전부 정상 로드되는지 확인.
- [ ] **최종보스 크기**: 엘리트보다 한층 더 크게 보이는지 확인.
- [ ] **지휘자 캐릭터 크기**: 기존(1.8 기준)보다 눈에 띄게 작아졌는지 육안 확인. `Gameplay.unity`에
      저장된 실제 값(1.08)이 반영되는지 - 리플렉션으로 `PlayerController.targetWorldHeight` 필드값이
      0.6인지, 실제 렌더링된 `transform.localScale`이 그에 맞게 계산되는지 확인.
- [ ] **엘리트 공격 판정**: 리듬 노트 히트로 엘리트를 공격했을 때, 화면상 시각적으로 겹쳐 보이는
      상황에서 데미지가 실제로 들어가는지 확인(이번 수정의 핵심 - `HitboxRadius`가 판정 거리에 더해져
      기존보다 더 관대하게 맞아야 함). 가능하면 8종 공격 중 몇 개(예: 드럼/바이올린/피아노)를 직접
      맞혀서 `BossMonster.CurrentHp`가 실제로 줄어드는지 리플렉션으로 확인.
- [ ] **최종보스 공격 판정**: 동일하게 최종보스 대상으로도 확인.
- [ ] **엘리트/보스 몸박(접촉 데미지) 판정**: `circleCollider.radius`가 엘리트는 2.0 그대로(하한선
      적용, 회귀 없음), 보스는 2.0 → 2.4로 커졌는지 리플렉션으로 확인. 실제 플레이 중 보스에 닿았을
      때 데미지 받는 범위가 이전보다 넓어졌는지 육안 확인.
- [ ] **밸런스 회귀 없음**: HP/데미지/이동속도 등 이번에 건드리지 않은 수치들이 그대로인지 코드
      검토로 확인(순수 크기/판정 범위 조정이라 회귀 없을 것으로 예상).

## 4. 검증 결과

(검증 완료 후 이 아래에 append)
