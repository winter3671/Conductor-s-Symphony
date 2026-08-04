# 10종 악기별 공격 메커니즘 - 2단계(바이올린/프렌치호른/첼로/팀파니, 홀드 기반) 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 2단계 구현을 실측 검증할 때 참고하는
절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서 진행하고, **아직
커밋하지 않은 상태**입니다 — 이번 라운드도 테스트 결과를 먼저 확인한 뒤에 커밋할 예정입니다.

검증이 끝나면 **`Docs/instrument_mechanics_phase2_test_result.md`** 파일에 결과를 정리해주세요
(1단계 `instrument_mechanics_phase1_test_guide.md` / `..._test_result.md` 관계와 동일한 형식).
버그를 발견하면 재현 절차·원인·(가능하다면) 수정 제안까지 적어주시면 Cowork 세션에서 그대로 반영합니다.

> **1차 검증 결과 반영 (수정 완료, 재검증 필요)**: `ViolinOrbitEffect.TickDamage()`에서 `hitCooldowns`
> 딕셔너리를 `foreach`로 순회하는 도중 같은 딕셔너리를 인덱서로 갱신해 `InvalidOperationException`이
> 발생하는 치명적 버그가 있었습니다(바이올린으로 적을 한 번이라도 맞히면 다음 틱부터 100% 재현).
> 키 스냅샷(`new List<EnemyMonster>(hitCooldowns.Keys)`)을 떠서 순회하도록 수정했습니다. **2차 검증에서는
> 아래 "2. 바이올린" 체크리스트 중 "같은 적을 매 프레임 때리는 게 아니라 히트 쿨다운 간격으로만 타격되는지"
> 항목을, 한 번의 타격 이후에도 크래시 없이 여러 틱을 정상 통과하는지까지 포함해서 다시 확인해주세요.

`10종 악기별 공격 메커니즘 기획서.docx`의 2단계 구현(홀드 기반 4종: 바이올린/프렌치호른/첼로/팀파니)
검증 절차입니다. 0단계(홀드 노트 인프라)와 1단계(탭 4종)를 이미 커밋한 다음 단계 작업입니다.

---

## 0. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 신규/변경 파일:
   `Combat/InstrumentAttacks/IHoldAttackEffect.cs`, `Combat/InstrumentAttacks/HoldEffectCoordinator.cs`,
   `Combat/InstrumentAttacks/ViolinOrbitEffect.cs`, `Combat/InstrumentAttacks/FrenchHornConeEffect.cs`,
   `Combat/InstrumentAttacks/CelloGravityFieldEffect.cs`, `Combat/InstrumentAttacks/TimpaniBombardmentEffect.cs`,
   `Combat/InstrumentAttacks/InstrumentAttackDispatcher.cs`(수정), `Combat/RhythmAttackManager.cs`(수정),
   `Enemy/EnemyMonster.cs`(수정 - `SetSpeedMultiplier` 추가).
2. Play Mode 실측 시 이전 라운드 팁 재사용: `Application.runInBackground = true`, 필요시 `Time.timeScale` 조정.
3. **(1차 실측에서 정립된 방법론, 재사용 권장)** 발사체/타이머 기반 메커니즘은 실시간 `Bash sleep` 대기
   대신 **`Time.timeScale = 0f`로 자동 갱신을 차단한 뒤, 리플렉션으로 private 필드/메서드를 직접
   조작·호출**해 원하는 지점을 결정론적으로 재현할 것. 홀드 계열은 특히 아래 이벤트를 직접 호출해서
   테스트하는 편이 실제 키 입력 타이밍을 맞추는 것보다 훨씬 안정적입니다:
   ```csharp
   // 홀드 시작을 직접 트리거 (실제로는 RhythmManager.ProcessHit -> OnHitSuccessEvent가 호출)
   var mgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;
   var method = mgr.GetType().GetMethod("HandleRhythmHit");
   method.Invoke(mgr, new object[] { ConductorSymphony.Rhythm.HitRating.Perfect, ConductorSymphony.Rhythm.RhythmLane.Left });
   ```

---

## 1. 무엇이 바뀌었나

`RhythmManager`는 홀드 노트의 최초 판정 성공 시 기존과 동일하게 `OnHitSuccessEvent`를 발행하고(=홀드
시작 신호), 이후 키가 눌려있는 동안 매 프레임 `OnHoldTickEvent`를, 떼거나 완주하면 1회
`OnHoldReleasedEvent`를 발행합니다. `RhythmAttackManager`는 이 세 이벤트를 모두 받아
`HoldEffectCoordinator`(레인別 활성 이펙트 1개를 관리하는 정적 클래스)로 위임하고, `HoldEffectCoordinator`는
악기 타입에 맞는 `IHoldAttackEffect` 구현체(`Init`/`OnHoldTick`/`OnHoldReleased`)를 생성·호출합니다.
나머지 2종(드럼/플루트)은 아직 미이관 상태라 기존 로직 그대로 유지됩니다.

| 악기 | 홀드 길이 | 홀드 중 | 릴리즈 시 | 레벨 스케일링 |
|---|---|---|---|---|
| 바이올린 | 13칸 | 플레이어 둘레 회전 칼날(2~3개)로 지속 타격 | 이동 방향 부채꼴 참격(빔 3~5발) | Lv3 칼날+1, Lv4 참격 2발 추가+피어스 |
| 프렌치호른 | 6칸 | 이동 방향 전방 부채꼴(반각 60~90도) 지속 타격+밀쳐냄 | 없음(홀드 종료 시 그냥 소멸) | Lv4 부채꼴 반각 확대(120→180도), 레벨당 사거리 증가 |
| 첼로 | 13칸 | 캐스팅 시점 가장 가까운 적 위치에 고정 중력장, 이속감소+지속타격 | 남은 감속 해제 | 레벨당 범위/감속량 증가 |
| 팀파니 | 16칸 | 홀드 시작 즉시 착탄 1회 + 유지 중 주기적 소형 융단폭격 | 없음(진행 중이던 폭격만 중단) | Lv3 최초 착탄 반경 확대, Lv4 융단폭격 간격 단축 |

---

## 2. execute_code로 홀드 생명주기 검증 (Play Mode, 몹이 화면에 있는 상태)

```csharp
// 4종 중 하나를 임시 장착(InstrumentManager.Instance.AcquireOrUpgradeInstrument 등 기존 방식 사용)한 뒤,
// 홀드 시작 -> 유지 -> 해제 흐름을 직접 호출해 결정론적으로 재현
var attackMgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;

// 1) 홀드 시작 (실제로는 RhythmManager.ProcessHit -> OnHitSuccessEvent가 자동 호출)
attackMgr.HandleRhythmHit(ConductorSymphony.Rhythm.HitRating.Perfect, ConductorSymphony.Rhythm.RhythmLane.Left);
Debug.Log("홀드 시작: 씬에 HoldEffect_<악기명> GameObject가 생성되었는지 Hierarchy에서 확인");

// 2) 유지 틱 강제 발화 (실제로는 RhythmManager.UpdateActiveHolds가 매 프레임 자동 호출).
//    가장 간단하고 결정론적인 방법은 RhythmAttackManager를 거치지 않고 HoldEffectCoordinator를 직접 호출하는 것:
ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.Tick(ConductorSymphony.Rhythm.RhythmLane.Left, 0.4f);
// 위 한 줄을 Time.timeScale=0f 상태에서 여러 번 반복 호출하면 "0.4초씩 여러 번 홀드 유지"를 결정론적으로 재현할 수 있음

// 3) 해제 (실제로는 키를 떼거나 홀드를 완주하면 RhythmManager가 자동 호출)
ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.Release(ConductorSymphony.Rhythm.RhythmLane.Left, completedFully: true);
```

Play Mode 체크리스트 (공통):
- [ ] 홀드 시작 시 `HoldEffect_<악기명>` GameObject가 씬에 생성되는지 (Hierarchy에서 확인)
- [ ] 같은 레인에서 홀드가 끝나면(조기 이탈이든 완주든) 해당 GameObject가 `Destroy`되어 Hierarchy에서 사라지는지
- [ ] 홀드 도중 다른 레인에서 새 홀드가 시작돼도 서로 간섭 없이 독립적으로 동작하는지 (레인별 딕셔너리 분리 확인)

### 바이올린
- [ ] 홀드 중 플레이어 주위를 도는 칼날(2개, Lv3+는 3개)이 보이고 실제로 근처 적에게 반복 타격이 들어가는지
- [ ] 같은 적을 매 프레임 때리는 게 아니라 `HitCooldown`(0.35초) 간격으로만 타격되는지 (데미지 로그 타임스탬프로 확인)
- [ ] 키를 떼는 순간 이동 방향으로 부채꼴 참격(빔 3발, Lv4+는 5발)이 발사되고 칼날은 사라지는지
- [ ] 이동을 멈춘 채(마지막 방향 유지) 홀드를 떼도 마지막 이동 방향 기준으로 참격이 나가는지

### 프렌치호른
- [ ] 홀드 중 이동 방향 앞쪽에 반투명 원형(부채꼴 근사 표시)이 따라다니는지
- [ ] 부채꼴 각도 안(반각 60도, Lv4+는 90도)에 있는 적만 타격/넉백되고 각도 밖의 적은 영향받지 않는지
- [ ] 타격된 적이 플레이어로부터 멀어지는 방향으로 조금씩 밀려나는지 (Knockback)
- [ ] 홀드 중 이동 방향을 바꾸면 부채꼴도 즉시 따라 회전하는지

### 첼로
- [ ] 홀드 시작 시점의 가장 가까운 적 위치에 원형 장판이 고정 생성되고, 이후 그 적이 움직여도 장판은 따라가지 않는지 (기획서 "고정된 중력장" 의도 확인)
- [ ] 장판 범위 안에 있는 적의 이동 속도가 눈에 띄게 느려지는지
- [ ] 장판 범위를 벗어난 적은 원래 속도로 복귀하는지 (`EnemyMonster.SetSpeedMultiplier(1f)` 리셋 확인 - 이 부분이 안 되면 적이 계속 느려진 채로 버그가 남는 치명적 케이스이니 특히 주의 깊게 확인)
- [ ] 홀드가 끝났을 때, 그 순간 장판 범위 안에 남아있던 적들의 이속도 정상으로 복귀하는지

### 팀파니
- [ ] 홀드 시작 즉시(딜레이 0.05초) 가장 가까운 적 위치에 착탄 이펙트가 발생하는지
- [ ] 홀드를 유지하는 동안 주기적으로(Lv1~3: 0.65초, Lv4+: 0.45초) 같은 구역 주변에 작은 착탄이 랜덤 오프셋으로 추가 발생하는지
- [ ] 홀드를 도중에 떼면 예정되어 있던 다음 융단폭격이 더 이상 발생하지 않는지 (이미 스폰된 `AreaImpactEffect`는 자기 생명주기대로 완료됨 - 정상)

---

## 3. 알려진 단순화/가정 (설계자 확인 필요)

- **팀파니 탭/롤ing 통합**: 기획서는 "강세 단타 캐논"과 "16마디 롤ing 융단폭격"을 별개 노트 특성으로
  설명하지만, 0단계 홀드 인프라는 악기당 1가지 입력 모드(이번엔 전부 홀드)만 지원합니다. 그래서
  "홀드 시작 = 즉발 캐논, 홀드 유지 = 융단폭격"으로 합쳤습니다. 팀파니를 순수 단타 악기로도 쓰고 싶다면
  패턴 데이터/입력 인프라를 더 확장해야 합니다.
- **프렌치호른 부채꼴 시각화 = 원형 근사**: 실제 판정(각도 체크)은 정확한 부채꼴이지만, 화면에 보이는
  범위 표시는 단순 반투명 원형 스프라이트로 근사했습니다. 부채꼴 모양 스프라이트가 필요하면 후속 작업.
- **레벨별 수치는 전부 임의값**: 기획서엔 정성적 설명(칼날 수/각도/범위 "증가")만 있고 정확한 수치가
  없어서, 1단계와 같은 관례로 감으로 넣은 값입니다(바이올린 칼날 반경 1.4~1.85, 프렌치호른 반각
  60~90도, 첼로 감속 50~70%, 팀파니 융단 간격 0.45~0.65초 등). 플레이테스트 후 조정 필요.
- **바이올린/글록켄슈필류의 "전용 콤보" 이슈는 이번 범위 밖**: 1단계 문서에 기록된 전역 콤보 카운터
  단순화는 이번 2단계(홀드 기반 4종)엔 콤보 트리거 자체가 없어 해당 없음.
- **Lv5 전용 효과 스텁**: 4종 모두 Lv5는 Lv4와 동일하게 동작합니다(기획서에 Lv5 전용 추가 설명이 없어
  1단계와 같은 전제를 유지).
