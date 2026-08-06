# 코드 리팩토링 제안

`Assets/Scripts/` 전체(44개 파일 — Combat/Instrument/Enemy/Player/Rhythm/Passive/Audio/UI/Utility/Item
전 영역)를 다시 읽고 정리한 리팩토링 제안입니다. **이 문서는 분석/제안만 담고 있으며, 코드는 전혀
수정하지 않았습니다.** 실제 적용 여부와 순서는 설계자/Cowork 세션에서 판단해주세요.

전반적으로 코드 품질은 양호합니다(각 클래스가 대체로 단일 책임에 가깝고, 절차적 스프라이트/사운드 생성
같은 이 프로젝트 특유의 패턴이 일관되게 지켜지고 있습니다). 아래 제안들은 "지금 당장 고장난 것"이
아니라 "10종 악기 + 8종 패시브 + 4단계 밸런스 조정을 거치며 자연스럽게 쌓인, 앞으로 더 커질수록
손대기 힘들어질 구조적 부채"에 초점을 맞췄습니다.

---

## 우선순위 높음 — 구조적 개선

### 1. 탭 5종 vs 홀드 5종의 디스패치 방식이 비대칭적입니다

- **현상**: 홀드 5종(바이올린/프렌치호른/첼로/팀파니/플루트)은 `IHoldAttackEffect` 인터페이스 +
  개별 `MonoBehaviour` 클래스로 다형성 있게 구현되어 있어, `HoldEffectCoordinator`는 구체 타입을
  전혀 몰라도 됩니다. 반면 탭 5종(드럼/피아노/벨/마림바/글록켄슈필)은 `InstrumentAttackDispatcher`
  안에 `private static void ExecuteXxx(...)` 형태로 몰아넣고 `Execute()`의 `switch`문 하나로
  분기합니다. 게다가 "이 타입이 구현되어 있는가"를 판별하는 로직도 `IsImplemented()` /
  `IsHoldImplemented()` 두 개의 별도 `switch`/`||` 체인으로 나뉘어 있습니다.
- **영향**: 신규 탭 악기를 추가하려면 `InstrumentAttackDispatcher.cs` 한 파일 안에서 4곳
  (`IsImplemented`, `Execute`의 `switch`, 새 `private static` 메서드, 필요시 `EnsureSprites`)을
  동시에 고쳐야 합니다. 반면 홀드 악기는 새 파일 하나 추가 + `IsHoldImplemented`/
  `CreateHoldEffect`의 `switch` 케이스 추가만으로 끝나 상대적으로 안전합니다. 두 경로가 다르다는
  사실 자체가 "다음 개발자(혹은 다음 Cowork 세션)가 탭 악기 추가 시 무엇을 놓치기 쉬운가"의 원인이
  됩니다.
- **제안**: 탭 악기도 홀드 악기와 동일하게 작은 인터페이스(예: `ITapAttackEffect.Execute(level,
  damage, currentCombo, origin, color)`)로 감싸고, `InstrumentAttackDispatcher`는
  `Dictionary<InstrumentType, ITapAttackEffect>` 룩업 테이블로 단순화합니다. 이렇게 하면
  `IsImplemented(type)`도 `dictionary.ContainsKey(type)` 한 줄로 줄고, 탭/홀드 구분 없이 "이
  악기가 구현되어 있는가"를 하나의 통합 룩업으로 처리할 수 있어 향후 신규 악기 추가 시 손댈 지점이
  파일 하나로 줄어듭니다.

### 2. 악기별 레벨 스케일링 수치가 코드에 하드코딩되어 있습니다

- **현상**: `InstrumentDamageTable.cs`는 "악기·레벨별 전체 배율"만 데이터화되어 있고, 그 외
  레벨별 수치(범위 +20%/+25%/+30%, 관통 +2, 발사 수 +1, 틱 간격 -33%, 감속량 40%→60% 등)는 전부
  `InstrumentAttackDispatcher.cs`와 7개 이펙트 클래스(`ViolinOrbitEffect`,
  `FrenchHornConeEffect`, `CelloGravityFieldEffect`, `TimpaniBombardmentEffect` 등) 안에
  `level >= N ? A : B` 삼항식으로 흩어져 있습니다.
- **영향**: 이번 세션에서 두 차례 진행한 DPS 밸런스 조정처럼 "배율만" 바꾸면 되는 작업은
  `InstrumentDamageTable.cs` 한 곳만 고치면 되지만, "범위를 +20%가 아니라 +25%로 바꾸고 싶다"
  같은 요청은 여전히 해당 악기의 이펙트 클래스 코드를 직접 열어 수정 + 재컴파일해야 합니다. 밸런스
  수치와 로직이 한 파일에 섞여 있어, 순수 수치 조정도 "코드 변경"으로 취급되어 리스크가 커집니다.
- **제안**: `InstrumentDamageTable`을 확장하거나 악기별로 `ScriptableObject`(또는 단순
  `Dictionary<InstrumentType, float[]>` 여러 개)를 만들어 "레벨별 범위 배율", "레벨별 관통 추가치"
  같은 나머지 튜닝 수치도 데이터로 분리하는 것을 권장합니다. 코드는 `def.GetRangeMultiplier(level)`
  처럼 데이터를 읽기만 하고, 실제 숫자는 전부 한 곳(이상적으로는 Inspector에서 디자이너가 직접
  조정 가능한 ScriptableObject)에 모입니다. `EnemySpawner`가 이미 구간별 수치를
  `[SerializeField] float[]` 배열로 인스펙터에 노출해둔 패턴이 있으니, 그 스타일을 악기 밸런스에도
  그대로 확장하면 됩니다.

### 3. `RhythmAttackManager.HandleRhythmHit()`이 여러 책임을 한 메서드에 떠안고 있습니다

- **현상**: 이 메서드 하나가 (a) 장착 악기 조회, (b) 사운드 재생, (c) `baseDamage`/`M_rhythm`/
  `M_stat`/악기 배율을 곱하는 최종 데미지 공식 계산, (d) 탭/홀드/폴백 3갈래 분기까지 담당합니다
  (~100줄). 게다가 `RhythmAttackManager`는 이 메서드와 전혀 무관한 "드럼 상시 비트 오라"
  로직(`UpdateDrumAura`, `drumAuraVisual`, `IsDrumsActive` 등)까지 같은 클래스 안에 섞여 있습니다.
- **영향**: 데미지 공식만 단위 테스트하고 싶어도 `RhythmAttackManager` 전체(싱글톤, `PlayerController`
  의존, `Instrument.InstrumentManager` 의존 등)를 Play Mode로 띄워야만 검증 가능합니다(이번
  대화의 두 차례 DPS 테스트에서도 실제로 리플렉션으로 private 필드를 여러 개 주입해야 했던 이유 중
  하나입니다). 드럼 오라 로직이 같은 클래스에 있다 보니 "타격 디스패치"를 읽으려는 사람이 무관한
  오라 코드까지 같이 봐야 합니다.
- **제안**:
  - 데미지 공식(`baseDamage * mRhythm * mStat * instrumentDpsMultiplier`)을 순수 함수로 뽑아내
    (예: `static class DamageFormula { public static int Compute(HitRating, extraDamage, mRhythm,
    mStat, dpsMultiplier) }`) Play Mode 없이도 검증 가능하게 만듭니다.
  - 드럼 상시 오라 로직을 `DrumAuraController`라는 별도 컴포넌트로 분리합니다. `RhythmAttackManager`
    는 판정 성공 시점의 디스패치에만 집중하게 됩니다.
  - 아래 "즉시 삭제 가능한 죽은 코드" 섹션의 폴백 로직을 제거하면 이 메서드는 추가로 30줄 이상
    줄어듭니다.

### 4. `EnemyMonster`의 상태이상 필드가 즉흥적으로 쌓이고 있습니다

- **현상**: `speedMultiplier`(첼로 지속 감속), `stunTimer`(팀파니/글록켄슈필 기절),
  `damageAmpMultiplier`(프렌치호른 증폭), `tempSlowTimer`/`tempSlowMultiplier`(드럼/마림바 한시적
  감속) — 악기 하나가 새 CC 효과를 필요로 할 때마다 필드 쌍이 하나씩 늘어났고, `Update()`에는 이
  필드들의 우선순위를 손으로 짠 공식(`stunTimer>0 ? 0 : (tempSlowTimer>0 ? Min(...) :
  speedMultiplier)`)이 있습니다.
- **영향**: 다음 악기가 새로운 종류의 CC(예: 침묵/도발/무장해제)를 필요로 하면 또 필드 2개 + `Update()`
  우선순위 공식 수정이 필요합니다. 이런 패턴이 반복될수록 `EnemyMonster.Update()`는 계속 커지고,
  "이 필드가 어떤 필드보다 우선하는가"를 코드를 직접 읽지 않으면 알 수 없습니다.
- **제안**: 범용 `StatusEffect`(타입, 배율/수치, 남은 시간)의 리스트를 갖는 작은 컴포넌트로
  일반화하는 것을 고려해볼 만합니다. "이동속도" 채널처럼 여러 효과가 동시에 걸릴 수 있는 채널은
  "가장 강한 값이 이긴다"는 리듀서 함수 하나로 통일하고, 새 CC 타입 추가 시 `EnemyMonster`의 핵심
  로직을 다시 건드리지 않고 새 `StatusEffectType`만 추가하면 되게 만드는 방향입니다. 지금 당장
  필요한 리팩토링은 아니지만(현재 CC 4종은 아직 관리 가능한 수준), 팀파니 Lv4/글록켄슈필 Lv5처럼
  "기존 CC에 새 발동 조건을 얹는" 패턴이 앞으로도 반복될 가능성이 높아 미리 검토해두면 좋습니다.

---

## 우선순위 중간 — 성능/일관성

### 5. `FindObjectsByType<EnemyMonster>()`가 10개 파일, 15곳에서 각자 호출됩니다

grep 기준 `CombatTargetingUtility`(2), `RhythmAttackManager`(3), `InstrumentAttackDispatcher`(2),
`AreaImpactEffect`/`CelloGravityFieldEffect`/`FluteVortexEffect`(2)/`FrenchHornConeEffect`/
`PiercingBeamProjectile`/`LingeringZoneEffect`/`ViolinOrbitEffect` 각 1곳씩, 총 15곳에서 매번
독립적으로 씬 전체를 스캔합니다.

- **영향**: 지금은 몹 수가 적어 체감되지 않지만, `game_balance_design.docx`가 명시한 07:30~10:00
  구간(동시 100~150마리)에서는 홀드 악기 하나의 `OnHoldTick`만으로도 프레임마다(또는 0.2~0.4초
  간격으로) 최대 150마리를 스캔하는 호출이 여러 악기·여러 이펙트에서 중복 발생합니다. `EnemySpawner`
  가 이미 `ActiveEnemies`(`IReadOnlyList<EnemyMonster>`)로 살아있는 적 목록을 유지하고 있으므로,
  나머지 15곳은 사실 이미 존재하는 단일 소스를 재사용하지 않고 각자 다시 조회하고 있는 셈입니다.
- **제안**: 위 호출부들을 `EnemySpawner.Instance.ActiveEnemies`를 사용하도록 통일하는 것을
  권장합니다(엘리트/보스는 `BossMonster.Instance`로 이미 별도 처리되고 있어 그대로 두면 됩니다).
  성능 이점뿐 아니라, "같은 프레임 안에서 여러 스캔 결과가 서로 다를 수 있는" 미묘한 불일치
  가능성도 줄어듭니다(예: 한 스캔 도중 몹이 파괴되어 다음 스캔에는 안 보이는 경우).

### 6. 타이머/자동소멸 이펙트 보일러플레이트가 여러 클래스에 중복되어 있습니다

- **틱 누적 패턴**(`tickTimer += dt; if (tickTimer < interval) return; tickTimer = 0f;`)이
  `RhythmAttackManager`(드럼 오라), `FrenchHornConeEffect`, `CelloGravityFieldEffect`,
  `TimpaniBombardmentEffect`, `LingeringZoneEffect` 5곳에 동일한 3줄로 반복됩니다.
- **경과시간 기반 자동 소멸 패턴**(`elapsed += dt; t = elapsed/duration; ... if (t>=1) Destroy`)도
  `ShockwaveVisualEffect`, `SoundwaveRingEffect`, `FluteVortexEffect`, `HitFloatingText`,
  `AreaImpactEffect`(딜레이 카운트다운 버전), `LingeringZoneEffect`(지속시간 카운트다운 버전)에서
  각자 구현되어 있습니다.
- **제안**: 작은 재사용 가능한 헬퍼(`struct Ticker { float timer; interval; bool TryConsume(float
  dt) }`, 또는 "일정 시간 후 자동 파괴"용 공용 베이스 `MonoBehaviour`)를 만들면 중복을 줄이고,
  "간격 로직을 고쳐야 할 일"(예: 오버슈트된 나머지 시간을 다음 틱에 이월할지 여부)이 생겼을 때
  한 곳만 고치면 되게 됩니다. 다만 이 항목은 성능보다는 유지보수성 문제라 우선순위는 중간입니다.

---

## 즉시 삭제 가능한 죽은 코드 (위험 없음)

- **`RhythmAttackManager.HandleRhythmHit()`의 범용 투사체 폴백 로직** (약 240~262번 줄, `Collect
  all potential target components...` 이하): `IsImplemented()`/`IsHoldImplemented()`가 10종
  전부를 커버하므로 이 분기는 코드상 도달이 불가능합니다. 3단계 검증 결과 문서부터 반복적으로
  플래그되어 왔는데도 아직 남아있습니다. 삭제를 권장합니다(신규 악기 추가에 대비한 안전장치로
  일부러 남겨두고 싶다면, 최소한 그 의도를 명시하는 주석 + 그 경로가 실제로 살아있는지 확인하는
  테스트 하나 정도는 필요합니다).
- **`RhythmAttackManager.FindNearestEnemy(Vector3)`**: 정의만 있고 파일 전체에서 호출되는 곳이
  없습니다(grep으로 확인). `CombatTargetingUtility.GetNearestEnemy()`와 로직도 완전히 동일한
  중복 코드입니다 — 죽은 코드이면서 동시에 중복 코드입니다.
- **`InstrumentOrbit.SetAngle(float angle)`**: 본문이 `// Backward compatibility` 주석뿐인 빈
  메서드이고, 호출하는 곳이 없습니다.

---

## 선택적/장기 검토 사항

- **벨(Bell)의 발사 시작점=타겟 위치 구조**: 이번 DPS 밸런스 테스트에서 "8방향 빔이 전부 같은
  적(발사 중심에 서 있는 그 적)에게 명중"하는 구조적 문제를 발견했고, 이번엔 배율을 낮춰 수치상
  균형은 맞췄습니다(`Docs/dps_balance_test_result.md` §2-1, §5 참고). 다만 코드 상으로는 "발사
  원점 = 타겟의 현재 위치"라는 구조 자체가 그대로 남아있어, 여러 마리가 흩어진 실제 전투에서는
  광역 소탕력이 이론(1발씩 분산 명중)보다 약할 수 있습니다. 언젠가 벨을 다시 만질 일이 있다면
  발사 원점을 플레이어 위치로 옮기거나 프레임당 동일 타겟 중복 판정을 막는 방향을 함께 검토할
  가치가 있습니다.
- **"doc에 없어 임의로 정한 값" 매직넘버의 산발적 배치**: 드럼 Lv3 둔화 배율(0.5), 팀파니 Lv4
  기절 지속시간(1.0초), 글록켄슈필 Lv5 기절 지속시간(0.5초), 마림바 Lv5 감속(0.7배/1초) 등 "기획
  문서에 수치가 없어 코드 작성 시점에 즉흥적으로 정한 값"이라는 주석이 6곳 이상에 흩어져 있습니다.
  당장 문제는 아니지만, 이런 값들을 `InstrumentDamageTable.cs`처럼 한 파일에 모아두면 향후
  플레이테스트 피드백으로 일괄 조정할 때(예: "모든 기절 지속시간을 20% 늘려달라") 더 빠르게 대응할
  수 있습니다.
- **`InstrumentManager.GetTotalExtraDamage()`가 "지금 타격한 악기"가 아니라 "장착된 4슬롯 전체"의
  `extraDamage` 합을 반환하는 문제**: `Docs/dps_balance_gap_analysis.md` §4에서 이미 발견해뒀던
  항목으로, 버그라기보다는 "구조적으로 악기별 데미지 계산이 자기 자신의 상태만으로 완결되지 않고
  다른 슬롯의 상태에 암묵적으로 의존한다"는 설계 문제에 가깝습니다. 영향은 작다고 분석되어 있지만
  (`extraDamage` 최대 +2/악기, 4개 합쳐도 +8), 리팩토링 시 데미지 계산 경로를 손보는 김에 함께
  정리하면 좋을 항목입니다.
- **런타임 절차적 UI 생성 (`LevelUpUI`, `RhythmUI`)**: 레벨업 카드 3장, 보스 HP 텍스트, 승리 화면
  등을 전부 `new GameObject()` + 컴포넌트 추가 + `RectTransform` 수치 지정으로 코드에서 직접
  조립하고 있습니다(`LevelUpUI.EnsureUIComponents()`만 약 90줄). 이 프로젝트가 전반적으로 "에셋을
  손으로 배치하기보다 코드로 절차적으로 생성"하는 스타일을 의도적으로 유지하고 있는 것으로
  보이므로 필수 리팩토링으로 제안하지는 않지만, UI 레이아웃을 자주 조정할 계획이라면 프리팹 +
  Inspector 참조 방식으로 옮기는 편이 반복 작업(레이아웃 수치 조정마다 재컴파일)을 줄여줄 수
  있습니다.
- **싱글톤(`MonoSingleton<T>`) 의존이 많은 아키텍처와 테스트 용이성**: `RhythmManager`,
  `InstrumentManager`, `PassiveStatManager`, `PlayerController`, `EnemySpawner`,
  `AudioLayerManager` 등 핵심 매니저 대부분이 싱글톤입니다. 소규모 프로젝트에서는 합리적인
  선택이지만, 이번 대화를 포함해 지금까지의 모든 실측 테스트 라운드에서 "private 필드를
  리플렉션으로 직접 주입해야 결정론적으로 재현 가능"했던 이유가 대부분 여기서 비롯됩니다(예:
  `RhythmManager.RhythmSuccessRate01`, `RhythmAttackManager.drumAuraTickTimer` 등). 아키텍처
  전체를 바꿀 필요는 없지만, 데미지 공식·타겟팅·패턴 조회처럼 "순수 계산" 성격이 강한 로직을
  싱글톤 MonoBehaviour 밖으로 뽑아 정적 함수/일반 클래스로 만들어두면(위 3번 제안과 연결) 다음
  밸런스 조정 라운드의 테스트 스크립트가 훨씬 간단해집니다.

---

## 요약

| 항목 | 우선순위 | 예상 작업 규모 |
|---|---|---|
| 1. 탭/홀드 디스패치 통합 | 높음 | 중 (인터페이스 1개 + 5개 클래스 소규모 리팩터) |
| 2. 레벨별 밸런스 수치 데이터화 | 높음 | 중~대 (7개 이펙트 클래스에 걸쳐 있음) |
| 3. RhythmAttackManager 책임 분리 | 높음 | 중 (드럼 오라 컴포넌트 분리 + 데미지 공식 함수 추출) |
| 4. EnemyMonster 상태이상 일반화 | 높음(장기) | 대 (설계 변경 성격, 신중한 검토 필요) |
| 5. FindObjectsByType 중복 호출 통합 | 중간 | 소~중 (호출부만 교체, 로직 변경 없음) |
| 6. 타이머/이펙트 보일러플레이트 추출 | 중간 | 소 |
| 죽은 코드 3건 제거 | 즉시 가능 | 매우 작음 (삭제만 하면 됨) |
| 선택 사항 4건 | 낮음/장기 | 항목별 상이 |

가장 리스크가 낮고 즉시 효과가 있는 것은 **죽은 코드 3건 제거**와 **5번(FindObjectsByType 통합)**
입니다 — 둘 다 동작 변경 없이 안전하게 적용 가능합니다. 반면 **4번(상태이상 일반화)**은 설계
변경이 커서 다음 악기 확장 계획이 확정된 뒤에 함께 검토하는 것을 권장합니다.
