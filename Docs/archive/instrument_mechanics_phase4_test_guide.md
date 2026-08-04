# 10종 악기별 공격 메커니즘 - 4단계(밸런스 doc 정합화) 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 4단계 작업을 실측 검증할 때 참고하는
절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서 진행하고, **아직
커밋하지 않은 상태**입니다 — 이번 라운드도 테스트 결과를 먼저 확인한 뒤에 커밋할 예정입니다.

검증이 끝나면 **`Docs/instrument_mechanics_phase4_test_result.md`** 파일에 결과를 정리해주세요
(1~3단계 가이드/결과 문서와 동일한 형식). 버그를 발견하면 재현 절차·원인·(가능하다면) 수정 제안까지
적어주시면 Cowork 세션에서 그대로 반영합니다.

## 0. 이번 라운드가 다른 이유

1~3단계는 "메커니즘 doc(정성적 설명)"만 보고 레벨별 수치를 임의로 정해서 구현했습니다. 이번 4단계는
`game_balance_design.docx` 5번 항목(악기별 Lv1~5 성장표 + Target DPS)을 뒤늦게 교차 검토하면서 발견된
차이를 메우는 작업으로, **10종 전체의 레벨별 수치와 Lv4~5 부가 효과(기절/디버프/장판/유도탄/끌어당김
등)를 새로 추가**했습니다. 새 기능이 많은 만큼 이번 라운드는 회귀 검증(1~3단계에서 이미 PASS했던
동작이 깨지지 않았는지)도 함께 봐주셔야 합니다.

---

## 1. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 특히 이번
   라운드는 `Initialize()` 계열 메서드에 명명 인자(named argument)와 위치 인자를 섞어 쓴 호출부가
   여러 곳 있습니다 (`InstrumentAttackDispatcher.cs`, `ViolinOrbitEffect.cs`,
   `TimpaniBombardmentEffect.cs` 등). C# 규칙상 명명 인자가 자신의 실제 선언 위치와 일치하면 그 뒤에
   위치 인자가 와도 합법이라 코드 리뷰로는 문제없다고 판단했지만, **로컬에 C# 컴파일러가 없어 Unity
   컴파일로 실제 확인한 적이 없으니 이 라운드의 1번 체크리스트 항목으로 반드시 확인해주세요.**
2. 신규/변경 파일 목록:
   - `Assets/Scripts/Enemy/EnemyMonster.cs` (수정 — 기절/피해증폭/한시적 감속 공용 인프라 추가)
   - `Assets/Scripts/Combat/InstrumentAttacks/LingeringZoneEffect.cs` (신규 — 잔류 장판)
   - `Assets/Scripts/Combat/InstrumentAttacks/HomingShrapnelProjectile.cs` (신규 — 유도 파편)
   - `Assets/Scripts/Combat/InstrumentAttacks/PiercingBeamProjectile.cs` (수정 — `sizeMultiplier`,
     `onHitEnemy` 콜백 추가)
   - `Assets/Scripts/Combat/InstrumentAttacks/AreaImpactEffect.cs` (수정 — `onHitEnemy`, `onImpact`
     콜백 추가)
   - `Assets/Scripts/Combat/InstrumentAttacks/InstrumentAttackDispatcher.cs` (수정 — 드럼/피아노/벨/
     마림바/글록켄슈필 전부 레벨 스케일링 재조정)
   - `Assets/Scripts/Combat/InstrumentAttacks/ViolinOrbitEffect.cs`,
     `FrenchHornConeEffect.cs`, `CelloGravityFieldEffect.cs`, `TimpaniBombardmentEffect.cs`,
     `FluteVortexEffect.cs` (전부 수정 — 레벨 스케일링 재조정 + 신규 부가 효과)
3. 상태이상 인프라(`EnemyMonster`)가 이번에 새로 생겼으므로, 몹 하나를 스폰해두고 리플렉션 없이도
   공개 API로 직접 검증 가능합니다:
   ```csharp
   var enemy = GameObject.FindObjectOfType<ConductorSymphony.Enemy.EnemyMonster>();
   enemy.ApplyStun(1.0f);
   Debug.Log($"기절 상태={enemy.IsStunned}"); // true
   enemy.SetDamageAmpMultiplier(1.15f);
   int before = enemy.CurrentHealth;
   enemy.TakeDamage(10);
   Debug.Log($"피해량(증폭 15% 적용시 11이어야 정상)={before - enemy.CurrentHealth}");
   ```
4. **(1~3단계에서 정립된 방법론, 재사용 권장)** 실시간 `Bash sleep` 대기 대신
   `Time.timeScale = 0f` + 리플렉션으로 private 필드/메서드 직접 제어. 홀드형 악기는
   `HoldEffectCoordinator.BeginHold/Tick/Release`를 직접 호출해 원하는 레벨/타이밍을 결정론적으로
   재현하는 편이 안전합니다.

---

## 2. 악기별 검증 항목

### 드럼 (Drums) — Lv3 둔화, Lv5 이중 충격파
```csharp
var dispatcher = typeof(ConductorSymphony.Combat.InstrumentAttacks.InstrumentAttackDispatcher);
// ExecuteDrums는 private이 아니라 Execute(InstrumentType.Drums, level, ...)로 진입
```
- [ ] Lv1~2: 기존과 동일하게 넉백 + 피해(Lv2부터 피해량 +20%)
- [ ] Lv3+: 비트 뱅에 맞은 적이 0.5초간 이동속도가 절반으로 느려지는지(`ApplyTemporarySlow`), 0.5초
      후 자동으로 원래 속도로 복귀하는지
- [ ] Lv5: 비트 뱅이 눈에 띄게 2회 연속 발동하는지(링 이펙트도 2번 재생되는지)
- [ ] **회귀**: 상시 비트 오라(3단계 기능)는 이번 변경과 무관하게 그대로 동작하는지

### 피아노 (Piano) — Lv2 피해량 +25%
- [ ] Lv1: 기존과 동일한 데미지
- [ ] Lv2+: 같은 조건에서 데미지가 정확히 25% 증가하는지 (`Mathf.RoundToInt` 반올림 감안)
- [ ] **회귀**: 관통(Lv3), 발사 수(Lv4), 6연타 폭포(Lv5)는 이전과 동일하게 동작

### 바이올린 (Violin) — Lv4 참격 크기, Lv5 검기 잔향
- [ ] Lv4+: 릴리즈 참격의 시각적 크기와 히트 판정 범위가 커지는지(발수는 3발 고정으로 바뀜 — 이전
      버전의 "Lv4=5발"과 달라졌으니 회귀 아님)
- [ ] Lv5: 참격이 지나간 자리를 따라 `ViolinAfterglow` 장판 3개가 생성되고, 2초간 주기적으로 추가
      피해를 주는지
- [ ] **회귀**: 홀드 중 회전 칼날 지속 타격(1~3단계에서 검증된 dictionary 크래시 수정 포함)이 여전히
      정상 동작하는지

### 프렌치 호른 (French Horn) — Lv3 넉백, Lv4 피해증폭, Lv5 각도확장(레벨 이동)
- [ ] Lv3+: 넉백 거리가 이전보다 확실히 커지는지(1.4배)
- [ ] Lv4+: 부채꼴 범위 안에 있는 적에게 `SetDamageAmpMultiplier(1.15f)`가 적용되어 다른 악기의
      피해도 15% 증폭되는지, 범위를 벗어나면 증폭이 해제되는지
- [ ] Lv5: 각도가 120°→180°로 확장되는지 (**주의**: 이전 버전엔 Lv4에서 확장됐었습니다 — 이번에
      Lv5로 정정된 게 의도된 변경입니다)
- [ ] 홀드 릴리즈 시 증폭 디버프가 걸려있던 모든 적에게서 확실히 해제되는지(디버프가 영구로 남지
      않는지)

### 첼로 (Cello) — Lv1 40%, Lv3 60%, Lv4 잔류시간, Lv5 끌어당김
- [ ] Lv1: 감속이 기존 50%가 아니라 40%로 시작하는지 (**의도된 하향 정정**)
- [ ] Lv3+: 감속이 60%로 증가하는지
- [ ] Lv4+: 홀드를 릴리즈해도 필드가 즉시 사라지지 않고 약 1.3초간 유지되며 계속 감속/타격하는지,
      잔류 시간이 끝나면 필드 안에 있던 적의 감속이 정상적으로 해제되는지
- [ ] Lv5: 필드 범위 안의 적이 중앙을 향해 서서히 끌려오는지
- [ ] **회귀**: 필드가 캐스팅 시점 위치에 고정되고 적을 따라가지 않는 기존 동작 유지

### 팀파니 (Timpani) — Lv4 기절, Lv5 지진지대
- [ ] Lv4+: 포탄(즉발 캐논이든 융단폭격이든)에 맞은 적이 1초간 기절(`IsStunned`)하는지
- [ ] Lv5: 착탄 지점에 `TimpaniSeismicZone` 장판이 생성되고 3초간 주기적으로 추가 피해를 주는지
- [ ] **회귀**: 낙하 범위(Lv2), 폭격 빈도(Lv3)가 이전 버전의 Lv3/Lv4 대신 정확히 Lv2/Lv3에서
      적용되는지 (**레벨이 한 단계씩 앞당겨진 게 이번 정정의 핵심**)

### 마림바 (Marimba) — Lv3 버그 수정, Lv5 감속+밀쳐냄
- [ ] **Lv3 버그 수정 확인이 최우선**: Lv2와 Lv3에서 파동의 시각적 크기와 히트 판정 범위를 비교해
      Lv3에서 실제로 30% 커지는지 (이전 버전은 Lv3에 아무 분기가 없어 Lv2와 완전히 동일했음)
- [ ] Lv5: 파동에 맞은 적이 이동속도 30% 감소(1초) + 발사 원점 반대 방향으로 밀쳐남을 동시에 받는지
- [ ] **회귀**: 관통(Lv2), 화면 끝 바운스(Lv4)는 이전과 동일하게 동작

### 글록켄슈필 (Glockenspiel) — Lv2 피해량, Lv5 유도파편+기절
- [ ] Lv2+: 별빛 낙하 데미지가 30% 증가하는지
- [ ] Lv4+: 버스트 시 항상 별빛 2개 추가(이전 "8배수면 2개, 아니면 1개" 조건 분기가 사라지고 doc대로
      "항상 +2"로 단순·통일됐습니다 — 의도된 변경)
- [ ] Lv5: 주 타겟(체력 최고 적)과 "다른" 적이 하나라도 있으면 그 적을 향해 유도 파편이 날아가고,
      명중 시 피해 + 0.5초 기절을 동시에 주는지. 다른 적이 없으면(1마리만 있을 때) 예외 없이
      조용히 스킵되는지
- [ ] **회귀**: 스플래시 반경(Lv3)은 이전과 동일

### 벨 (Bell) — Lv2 사거리 단발 적용, Lv5 잔향
- [ ] Lv2 이상 모든 레벨에서 사거리가 "1회성으로 30% 증가"한 값에서 고정되는지 (이전 버전은 레벨마다
      복리로 계속 늘어났었는데, 이번에 doc 기준 한 번만 적용되도록 정정됨 — Lv2와 Lv5의 사거리가
      같아야 정상)
- [ ] Lv5: 8방향 성광의 중심점에 `BellAfterglow` 장판이 생성되고 1.5초간 주기적으로 추가 피해를
      주는지
- [ ] **회귀**: 관통(Lv3), 더블 버스트(Lv4)는 이전과 동일

### 플루트 (Flute) — 참고용 (4단계에서 추가 변경 있음)
- [ ] Lv2+: 유지시간이 "레벨마다 완만히 증가"가 아니라 Lv2 이상에서 한 번에 +40%로 적용되는지
- [ ] Lv3+: 흡입 범위와 당기는 힘이 동시에 50% 증가하는지 (이전엔 당김 세기가 레벨 무관 고정값이었음)
- [ ] Lv4+: 소용돌이를 2개 이상(연속 홀드 등으로) 동시에 발동시켰을 때, 3번째가 생성되는 순간 가장
      오래된 소용돌이가 자동으로 사라지는지(Lv4 미만이면 1개 초과 시 즉시 정리되는지)
- [ ] Lv5: 소용돌이가 자연 소멸하는 순간, 범위 내 적이 바깥으로 밀려나는(무피해) "바람 파편" 연출이
      발생하는지

---

## 3. 상태이상 인프라 공용 검증 (`EnemyMonster.cs`)

여러 악기가 공유하는 신규 인프라라 개별 악기와 별개로 한 번 더 확인해주세요.

- [ ] `ApplyStun`으로 이동이 완전히 멈추는지, 기절이 풀리면(시간 경과) 다시 정상 이동을 재개하는지
- [ ] 기절 중에 첼로/드럼 등으로 걸려있던 감속(`speedMultiplier`)이 있었다면, 기절이 풀린 뒤 그
      감속 상태로 자연히 복귀하는지 (기절 이전 상태를 덮어쓰지 않는지)
- [ ] `ApplyTemporarySlow`가 걸린 상태에서 첼로 중력장(지속형 `SetSpeedMultiplier`) 안에 동시에
      들어갔을 때, 둘 중 더 강한(낮은) 배율이 적용되는지
- [ ] `SetDamageAmpMultiplier(1.15f)`가 걸린 적이 다른 악기의 공격을 받았을 때도 증폭이 적용되는지
      (프렌치 호른뿐 아니라 임의의 `TakeDamage` 호출 전부에 적용되는 전역 배율인지 확인)

---

## 4. 알려진 단순화/가정 (설계자 확인 필요)

- **Target DPS 미검증**: 이번 라운드도 밸런스 doc이 명시한 레벨별 목표 DPS(예: 피아노
  30→45→70→100→150) 자체를 역산해서 수치를 맞추지는 않았습니다 — doc이 요구하는 "효과의 종류"만
  채워 넣은 상태이고, 실제 DPS 측정/튜닝은 별도 밸런스 패스가 필요합니다.
- **감속/기절 수치 임의값**: 드럼 Lv3 둔화 배율(50%), 마림바 Lv5 감속(30%)·지속시간(1초) 등 doc에
  구체적 수치가 없는 항목은 감으로 정했습니다. 실측 후 너무 강하거나 약하면 조정이 필요합니다.
- **첼로 Lv4 "지속시간 +30%"를 "릴리즈 후 잔류시간"으로 해석**: doc 원문은 "중력장 지속시간"인데,
  중력장은 홀드를 누르고 있는 동안은 플레이어 입력으로 지속시간이 결정되는 구조라 "자동 연장"
  개념이 애초에 안 맞습니다. 그래서 "릴리즈 이후에도 필드가 잠깐 더 남아있는 잔류시간"으로 재해석해
  구현했습니다 — 이 해석이 기획 의도와 맞는지 확인이 필요합니다.
- **글록켄슈필 Lv5 2차 타겟 없을 시 무동작**: 적이 1마리뿐이면 유도 파편이 발동하지 않습니다. doc에
  이 경우에 대한 명시가 없어 "타겟이 없으면 스킵"으로 처리했습니다.
- **명명 인자+위치 인자 혼용 문법**: 위 0번 항목 참고 — 컴파일 통과 여부를 이번 라운드에서 반드시
  1차로 확인해주세요.

---

## 5. 이번 라운드 통과 후 남는 것

이 라운드가 통과하면 `game_balance_design.docx` 5번 항목(악기별 Lv1~5 성장표) 반영 작업이 일단락됩니다.
남는 건 Target DPS 실측/튜닝(정식 밸런스 패스 때 진행)과, 레가토/Multi+1 스탯이 사실상 비활성 상태인
문제(요약 문서 3번 섹션 9항 참고 — 사용자가 "일단 놔두고 나중에 문서화"하기로 결정한 사안이라 이번
라운드 대상은 아닙니다)입니다.
