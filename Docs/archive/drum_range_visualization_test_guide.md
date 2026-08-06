# 드럼 공격 범위 시각화 + 범위 패시브 연동 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 수정을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

실플레이 중 발견된 문제 3가지, 원인은 전부 연결되어 있습니다.

1. **드럼 상시 오라의 판정 범위가 안 보임**: 오라 링 비주얼(`DrumBeatAura`)이 생성 시점에 딱 한 번만
   `BaseRadius` 기준 고정 크기로 그려지고, 이후 레벨이 올라 실제 판정 반경(`BaseRadius + 0.1 ×
   (레벨-1)`)이 커져도 비주얼은 그대로였습니다.
2. **비트 뱅(정박 타격) 충격파 모션 크기가 항상 일정해 보임**: `ShockwaveVisualEffect`가 실제
   `radius`를 받긴 했지만 `radius * 2f`라는 근사 배율로 스케일했는데, 이 배율이 스프라이트 내부
   픽셀 비율을 감안하지 않은 값이라 실제 판정 범위보다 훨씬 작게(대략 0.3배) 그려지고 있었습니다.
   레벨업으로 반경이 2.0→2.4로 커져도 그 차이가 화면상으로는 거의 안 보일 만큼 작았던 것도 같은
   원인입니다.
3. **"범위 공격 증가" 패시브(크레센도)가 아무 효과도 없었음**: `PassiveStatManager.GetRangeMultiplier()`
   함수 자체는 있었지만 실제로 이 값을 곱해 쓰는 코드가 프로젝트 전체에 단 한 곳도 없었습니다(카드를
   먹어도 수치 계산만 되고 소비하는 곳이 없어 사실상 죽은 스탯). 카드 설명은 "모든 공격 범위 +10%/Lv"
   지만, 이번엔 요청하신 범위인 **드럼(오라 + 비트 뱅)에만** 반영했습니다 — 나머지 9종 악기는 여전히
   이 패시브를 소비하지 않습니다(아래 3번 참고).

### 수정 내용

- `Assets/Scripts/Utility/ProceduralSpriteFactory.cs`: `CreateUnitRing(innerRadius01, outerRadius01,
  color)` 신설 — `scale=1`일 때 링 바깥쪽 끝이 정확히 "월드 유닛 반지름 1"이 되도록 만들어서,
  이후로는 `transform.localScale = Vector3.one * 실제반경`만 하면 항상 정확히 일치한다(기존
  `CreateRingWithCore`는 픽셀 좌표계라 실제 반경과 맞추려면 매번 별도 환산 상수가 필요했고, 그게
  이번 버그들의 공통 원인이었음). 보스/상자/투사체 등 기존 `CreateRingWithCore` 사용처는 전혀 건드리지
  않음(장식용이라 정확한 반경 매칭이 필요 없는 곳들).
- `Assets/Scripts/Combat/InstrumentAttacks/ShockwaveVisualEffect.cs`: `CreateUnitRing` 기반으로
  다시 작성 — `maxScale`이 이제 정확히 실제 `radius`와 일치.
- `Assets/Scripts/Combat/DrumAuraController.cs`: 오라 링 비주얼을 매 프레임 실제 판정 반경으로
  갱신(레벨업/범위 패시브 즉시 반영), 판정 반경 계산에 `PassiveStatManager.GetRangeMultiplier()` 반영.
- `Assets/Scripts/Combat/InstrumentAttacks/DrumBeatBangEffect.cs`: 비트 뱅 판정 반경 계산에도
  동일하게 `GetRangeMultiplier()` 반영(피해 판정과 비주얼 둘 다 자동으로 같이 커짐 - 같은 `radius`
  변수를 씀).

## 1. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인.
2. 드럼을 장착한 상태로 확인하는 게 가장 빠릅니다. 크레센도 패시브는 레벨업 카드에서 뽑거나,
   `PassiveStatManager.Instance.AcquireOrUpgrade(PassiveStatType.Crescendo)`를 직접 호출해 강제로
   레벨을 올려 테스트할 수 있습니다.

## 2. 검증 항목

- [ ] 드럼 장착 시 상시 오라 범위 링이 항상 보이는지 (기존엔 안 보이던 문제는 아니고, 크기가
      실제와 다르던 문제였음 - 링 자체는 원래도 있었음)
- [ ] 드럼 레벨을 1→5로 올리면서 오라 링이 매 레벨마다 조금씩 커지는지(`BaseRadius + 0.1 ×
      (레벨-1)`, Lv1=1.6 → Lv5=2.0)
- [ ] 오라 링의 실제 시각적 반지름과 오라가 실제로 적을 때리는 거리(적을 링 안쪽/바깥쪽에 세워두고
      비교)가 일치하는지
- [ ] 정박 타격 시 비트 뱅 충격파 모션이 실제 넉백/피해 판정 범위와 시각적으로 일치하는지 (적을
      충격파 경계 근처에 세워두고 맞는지/안 맞는지로 확인)
- [ ] 드럼 Lv1→Lv2로 올리면 비트 뱅 충격파가 눈에 띄게 커지는지(반경 2.0→2.4, +20%)
- [ ] 크레센도(범위 공격 증가) 패시브를 획득/레벨업하면 오라 링과 비트 뱅 충격파가 둘 다 함께
      커지는지, 그리고 실제 피해 판정 범위도 커진 크기만큼 넓어지는지(적 배치로 확인)
- [ ] **회귀**: 오라/비트 뱅의 데미지 수치 자체(크기가 아니라 딜량)는 이번 변경으로 달라지지
      않았는지 - `radius`만 건드렸고 `auraDamage`/`shockwaveDamage` 계산식은 그대로임
- [ ] **회귀**: 보스 몹 링 비주얼(`BossMonster.cs`), 엘리트 보상 상자(`EliteRewardChest.cs`), 보스
      투사체(`BossProjectile.cs`), 악기 아이템(`InstrumentItem.cs`) 등 기존 `CreateRingWithCore`
      사용처는 이번 변경과 무관하게 기존과 동일하게 보이는지 (건드리지 않았지만 혹시 몰라 확인)

## 3. 알려진 범위 (설계자 확인 필요)

- **크레센도 패시브는 아직 드럼에만 반영됨**: 카드 설명("모든 공격 범위 +10%/Lv")대로라면 나머지
  9종 악기(피아노 사거리, 벨 사거리, 마림바 사거리, 글록켄슈필 스플래시, 바이올린 칼날 반경,
  프렌치호른 부채꼴 범위, 첼로 중력장 범위, 팀파니 폭격 범위, 플루트 소용돌이 범위)에도 전부 적용돼야
  하는데, 이번 라운드는 요청하신 범위(드럼)만 처리했습니다. 나머지 9종까지 반영할지는 별도 라운드로
  진행하는 게 좋을지 판단이 필요합니다 — 각 악기 이펙트마다 "범위"로 취급할 파라미터가 다 달라서
  (예: 프렌치호른은 `range`뿐 아니라 `halfAngleDeg`도 있음, 첼로는 필드가 캐스팅 시점에 고정이라 범위
  적용 방식을 다시 정의해야 함) 작업량이 꽤 됩니다.
- **다른 9종 악기도 같은 "판정 범위가 안 보이는" 문제가 있을 수 있음**: 이번엔 드럼만 요청받아
  고쳤지만, 벨/마림바/글록켄슈필/바이올린 등 범위 기반 공격들도 대부분 판정 범위를 화면에 명시적으로
  보여주지 않습니다(투사체나 임팩트 이펙트 자체가 발동 지점을 보여주긴 하지만, "여기까지가 사거리다"
  라는 상시 표시는 없음). 드럼처럼 상시 오라가 있는 악기가 아니라서 우선순위가 낮다고 판단해 이번
  범위에서는 제외했습니다.

## 4. 검증 결과 (Unity MCP 세션, 2026-08-06)

**환경**: Unity 6000.5.5f1, `Assets/Scenes/Gameplay.unity`, Play Mode. `refresh_unity(force, compile=request)` +
`read_console` 확인 결과 컴파일 에러 없음.

**방법**: `execute_code`로 Play Mode 중 실제 싱글턴(`InstrumentManager`/`PassiveStatManager`/
`EnemySpawner`)을 직접 조작해 레벨업·패시브 습득을 즉시 재현하고, `DrumAuraController`/
`ShockwaveVisualEffect`의 private 필드(`auraVisual`, `maxScale`)를 리플렉션으로 읽어 "화면에 그려지는
실제 크기"를 수치로 확인했습니다. 판정 범위 일치 여부는 `EnemySpawner.activeEnemies`에 정지 상태(움직이지
않도록 `playerTransform=null`로 `Initialize`) 테스트 몹을 정확한 거리에 배치해 HP 변화로 검증했습니다
(비주얼만 보고 판단하지 않고 실제 피해 판정 결과로 교차 확인).

- [x] **드럼 장착 시 상시 오라 링이 항상 보임**: 별도 조작 없이 Play Mode 진입 직후(드럼은 기본
      장착) `auraVisual.activeSelf == true`, 스크린샷에서도 붉은 오라 링이 즉시 보임.
- [x] **드럼 Lv1→Lv5로 오라 링이 매 레벨 커짐**: 실측 `auraScaleX` — Lv1=1.600, Lv2=1.700,
      Lv3=1.800, Lv4=1.900, Lv5=2.000. `BaseRadius + 0.1×(레벨-1)` 공식과 정확히 일치. 스크린샷
      비교로도 Lv1 대비 Lv5 링이 눈에 띄게 커짐을 확인(반지름 비율 실측 ≈1.25, 기대값 2.0/1.6=1.25).
- [x] **오라 링의 시각적 반지름 = 실제 판정 거리**: Lv5(반지름 2.0) 상태에서 플레이어로부터 정확히
      1.9 거리(링 안쪽)에 세운 고정 테스트 몹은 0.5초 틱마다 피해를 입었고(HP 100000→99970),
      2.1 거리(링 바깥쪽)에 세운 몹은 전혀 피해를 입지 않음 — 링 경계와 실제 판정 경계가 정확히 일치.
- [x] **비트 뱅 충격파가 실제 판정 범위와 시각적으로 일치**: Lv1(반지름 2.0)에서 1.9 거리 몹은
      즉시 10 피해를 입고, 2.1/2.2 거리 몹은 무피해. 이때 `ShockwaveVisualEffect.maxScale`도 정확히
      2.000으로 판정 반경과 동일.
- [x] **드럼 Lv1→Lv2로 비트 뱅이 커짐(2.0→2.4, +20%)**: Lv2로 올리자 이전엔 무피해였던 2.1/2.2
      거리 몹이 모두 피해를 입었고(각 12 피해 = 10×1.2, 피해량도 함께 +20% 확인), 비주얼
      `maxScale`도 2.400으로 정확히 갱신됨.
- [x] **크레센도 패시브 획득/레벨업 시 오라 링·비트 뱅 충격파가 함께 커지고, 실제 판정 범위도 그만큼
      넓어짐**: 크레센도 Lv5(`GetRangeMultiplier()=1.500`) 상태에서 드럼 Lv5 오라 `auraScaleX`=
      3.000(=2.0×1.5), 비트 뱅 Lv1 `maxScale`=3.000(=2.0×1.5)/Lv2 `maxScale`=3.600(=2.4×1.5) 모두
      공식과 정확히 일치. 실제 판정도 확인 — 크레센도 적용 전이면 무피해였을 2.9 거리 몹이 크레센도
      Lv5 + 드럼 Lv1(반지름 3.0) 상태에서는 정확히 피해를 입음.
  - **테스트 중 발견한 해프닝(코드 버그 아님, 기록용)**: 초기에 크레센도 반영 여부를 비주얼
    `maxScale`로만 확인했을 때 비트 뱅만 배율이 반영 안 된 것처럼 보이는 순간이 있었습니다. 원인은
    `DrumBeatBangEffect`가 매번 이름이 같은("DrumBeatBang") 새 링 오브젝트를 만드는데, 이 세션에서
    에디터가 언포커스 상태라 `ShockwaveVisualEffect.Update()`의 자연 소멸(0.25초 뒤 `Destroy`)이 실제
    프레임 진행 없이는 일어나지 않아, 이전 테스트에서 만든 낡은 링이 씬에 남아있었고
    `GameObject.Find("DrumBeatBang")`가 그 낡은 링을 집어 온 것이었습니다. 남은 링을 전부 정리하고
    `FindObjectsByType<ShockwaveVisualEffect>`로 정확히 새로 생긴 것만 골라 다시 재보니 위 결과처럼
    정상적으로 배율이 반영되어 있었습니다 — `DrumBeatBangEffect.cs`/`PassiveStatManager.cs` 자체의
    문제는 아닙니다.
- [x] **회귀: 데미지 수치 자체는 변하지 않음**: 오라 데미지는 `radius`와 무관한 별도 계산식
      (`DamageFormula.ComputeFinalDamage`)이라 애초에 영향 없음. 비트 뱅은 크레센도 Lv5(레인지
      1.5배) 상태에서도 Lv1 데미지가 정확히 10 그대로였음(레벨 기반 데미지 공식과 레인지 배율이
      완전히 분리되어 있음을 실측으로 확인) — `radius`만 커졌고 `shockwaveDamage`/`auraDamage`
      계산식은 그대로.
- [x] **회귀: 기존 `CreateRingWithCore` 사용처(보스/엘리트 상자/보스 투사체/악기 아이템)**: 4곳 모두
      직접 스폰해 스크린샷으로 확인 — 보스(큰 노랑/빨강 링), 엘리트 상자(노랑/보라 링), 보스 투사체
      (작은 빨강/노랑 링), 악기 아이템(흰색 링)이 전부 기존과 동일하게 정상 렌더링됨. 보스 스폰 시
      "BOSS HP" UI도 정상적으로 갱신됨.

**버그**: 발견되지 않았습니다. 코드 변경분(`DrumAuraController.cs`/`DrumBeatBangEffect.cs`/
`ShockwaveVisualEffect.cs`/`ProceduralSpriteFactory.cs`)은 문서에 적힌 대로 정확히 동작합니다.

**참고(세션 환경 이슈, 코드와 무관)**: 이번 세션에서도 지난 홀드노트 검증 때와 동일하게, Unity
에디터 창이 OS 포커스를 잃은 상태로 오래 있으면 Play Mode의 `Update()` 틱이 사실상 멈추는 현상이
있었습니다(`Time.frameCount`가 여러 번의 `execute_code` 호출 동안 전혀 증가하지 않음). 이번엔
`manage_editor(action="pause")` 후 `UnityEditor.EditorApplication.Step()`을 필요한 횟수만큼 반복
호출해 프레임을 수동으로 강제 진행시키는 방식으로 우회했습니다 — 포커스 여부와 무관하게 안정적으로
동작했습니다. 다음 검증 세션에서도 이 방식을 재사용하면 좋을 것 같습니다.
