# 10종 악기별 공격 메커니즘 - 1단계(피아노/벨/마림바/글록켄슈필) 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 1단계 구현을 실측 검증할 때 참고하는
절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서 진행하고, **아직
커밋하지 않은 상태**입니다 — 이번 라운드는 테스트 결과를 먼저 확인한 뒤에 커밋할 예정입니다.

검증이 끝나면 **`Docs/instrument_mechanics_phase1_test_result.md`** 파일에 결과를 정리해주세요
(이전 라운드의 `balance_1to3_test_guide.md` / `balance_1to3_test_result.md` 관계와 동일한 형식).
버그를 발견하면 재현 절차·원인·(가능하다면) 수정 제안까지 적어주시면 Cowork 세션에서 그대로 반영합니다.

`10종 악기별 공격 메커니즘 기획서.docx`의 1단계 구현(탭+오토타겟 4종) 검증 절차입니다. 0단계 공용 기반
(홀드 노트, 콤보 공개, 타겟팅 헬퍼, 플레이어 방향 공개)을 커밋한 다음 단계 작업입니다.

---

## 0. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 신규/변경 파일:
   `Combat/InstrumentAttacks/PiercingBeamProjectile.cs`, `Combat/InstrumentAttacks/AreaImpactEffect.cs`,
   `Combat/InstrumentAttacks/InstrumentAttackDispatcher.cs`, `Combat/RhythmAttackManager.cs`,
   그리고 아직 검증되지 않은 0단계 파일 `Rhythm/RhythmNote.cs`, `Rhythm/RhythmManager.cs`,
   `Instrument/InstrumentPatternDatabase.cs`, `Combat/CombatTargetingUtility.cs`, `Enemy/EnemyMonster.cs`,
   `Player/PlayerController.cs`도 함께 확인 대상입니다.
2. Play Mode 실측 시 이전 라운드에서 확인된 팁 재사용: 진입 직후 `Application.runInBackground = true`
   (에디터 창 포커스 없이도 프레임이 흐르게), 시간을 압축해야 하면 `Time.timeScale` 조정.
3. Edit Mode에서 `MonoSingleton` 기반 매니저를 직접 `AddComponent`해서 테스트할 경우, `Awake()`가
   자동 호출되지 않아 `Instance`가 계속 `null`로 남는 함정이 반복적으로 나왔던 부분이니
   (`balance_1to3_test_result.md` 1차/3차 검증 노트 참고) 필요하면 리플렉션으로 `Instance`를 직접 주입할 것.
4. **(1차 실측에서 정립된 방법론)** 발사체/타이머 기반 메커니즘(관통 빔, 지연 낙하, 바운스 등)은
   "스폰하고 `Bash sleep`으로 실시간 대기" 방식을 쓰지 말 것 — 에디터가 포커스를 잃으면 프레임이
   불규칙하게 몰려서(`Time.deltaTime` 폭주) 빔이 적을 그대로 스킵하는 등 결과가 왜곡된다.
   대신 **`Time.timeScale = 0f`로 자동 갱신을 차단한 뒤, 검증 대상의 private `Update()`/`CheckHits()`
   등을 리플렉션으로 직접 호출**하거나 위치·경과시간 필드를 직접 세팅해 원하는 지점을 결정론적으로
   재현할 것.

---

## 1. 무엇이 바뀌었나

`RhythmAttackManager.HandleRhythmHit()`에서 판정 성공 시, 그 슬롯의 악기가 피아노/벨/마림바/글록켄슈필이면
`InstrumentAttackDispatcher.Execute()`로 위임하고 `return` — 기존 "가장 가까운 N명에게 범용 원형 투사체"
로직을 완전히 건너뜁니다. 나머지 6종(드럼/바이올린/플루트/프렌치호른/첼로/팀파니)은 아직 미이관 상태라
기존 로직 그대로 유지됩니다 (`InstrumentAttackDispatcher.IsImplemented()`가 false 반환).

| 악기 | 타겟팅 | 핵심 메커니즘 | 레벨 스케일링 |
|---|---|---|---|
| 피아노 | 가장 가까운 적 방향 | 직선 관통 레이저(`PiercingBeamProjectile`) | Lv3 관통+2, Lv4 발사수+1, Lv5 6연타마다 부채꼴 3발 추가 |
| 벨 | 가장 가까운 적 **위치 중심** | 8방향 방사 레이저 | Lv2~ 사거리 점증, Lv3 관통+2, Lv4 8방향 2연속 발사 |
| 마림바 | 플레이어 바라보는 방향 | 직선 관통 파동 | Lv2 관통+2, Lv4 최대사거리 도달 시 1회 반전(바운스) |
| 글록켄슈필 | 체력 최고 적(동률 시 근거리 우선) | 지연 낙하 광역(`AreaImpactEffect`) | Lv3 스플래시 반경 확대, Lv4 4콤보마다 별 추가 낙하 |

---

## 2. execute_code로 타겟팅/스케일링 검증 (Play Mode, 몹이 화면에 있는 상태)

```csharp
// 가장 가까운 적 vs 체력 최고 적이 실제로 다른 대상을 가리키는지 확인
var player = ConductorSymphony.Player.PlayerController.Instance;
var nearest = ConductorSymphony.Combat.CombatTargetingUtility.GetNearestEnemy(player.transform.position);
var highestHp = ConductorSymphony.Combat.CombatTargetingUtility.GetHighestHpEnemy(player.transform.position);
Debug.Log($"Nearest={nearest?.name} (hp={nearest?.CurrentHealth}), HighestHP={highestHp?.name} (hp={highestHp?.CurrentHealth})");
// 의도적으로 "가까운데 체력 낮은 몹"과 "멀지만 체력 높은 몹"을 각각 배치해두고 둘이 달라지는지 확인할 것
```

Play Mode 체크리스트:
- [ ] 피아노 장착 후 Q/W/E/R 판정 성공 시, 가장 가까운 적 쪽으로 레이저가 날아가고 경로상 여러 마리를 관통하는지
- [ ] 피아노 Lv4 이상에서 레이저가 2발 나가는지 (동시 판정 1회에 대해)
- [ ] 벨 판정 성공 시 이펙트가 **플레이어가 아니라 가장 가까운 적의 위치**를 중심으로 8방향으로 뻗는지
- [ ] 벨 Lv4 이상에서 8방향 발사가 두 번 연속(더블 버스트)되는지
- [ ] 마림바 판정 성공 시 방향키로 바라보고 있는 방향으로 파동이 나가는지 (이동 안 하고 가만히 있어도 마지막 방향 유지되는지)
- [ ] 마림바 Lv4 이상에서 최대 사거리(10유닛) 도달 시 반대 방향으로 한 번 튕겨 돌아오는지
- [ ] 글록켄슈필 판정 성공 시 화면에서 **체력이 가장 높은** 적 머리 위에 낙하 이펙트가 생기는지 (가장 가까운 적이 아님을 확인)
- [ ] 글록켄슈필 Lv4 이상 + 콤보가 4의 배수일 때 별이 추가로 더 떨어지는지

---

## 3. (선택) 0단계 홀드 노트 기반 스모크 테스트

바이올린/프렌치호른/첼로/팀파니는 아직 이번 1단계에 포함되지 않지만, 그 기반이 되는 홀드 노트
시스템(`RhythmNote.NoteKind.Hold`, `RhythmManager.UpdateActiveHolds`)은 지난 라운드에서 커밋만 하고
실측 검증은 하지 않았습니다. 지금 당장 필수는 아니지만, 다음 단계(홀드 기반 4종)에서 문제를 늦게
발견하는 것보다 지금 기초 동작만이라도 확인해두면 좋습니다.

```csharp
// 바이올린을 임시로 장착해 홀드 노트가 실제로 스폰되는지, 키를 계속 누르고 있으면
// RhythmManager.OnHoldTickEvent/OnHoldReleasedEvent가 발생하는지 확인
ConductorSymphony.Rhythm.RhythmManager.OnHoldTickEvent += (lane) => Debug.Log($"HoldTick lane={lane}");
ConductorSymphony.Rhythm.RhythmManager.OnHoldReleasedEvent += (lane, progress, completed) =>
    Debug.Log($"HoldReleased lane={lane} progress={progress} completed={completed}");
// 이후 바이올린 장착 상태에서 Q(또는 해당 레인 키)를 노트 타이밍에 맞춰 누르고 "계속 눌러본 뒤",
// 도중에 떼는 경우(completed=false)와 끝까지 버티는 경우(completed=true) 둘 다 로그로 확인
```
- [ ] (선택) 홀드 노트 판정 성공 시 노트가 사라지지 않고 판정 링에 고정되는지
- [ ] (선택) 키를 계속 누르고 있으면 `OnHoldTickEvent`가 매 프레임 발생하는지
- [ ] (선택) 도중에 키를 떼면 `completed=false`로, 끝까지 유지하면 `completed=true`로 `OnHoldReleasedEvent`가 발생하는지
- [ ] (선택) 같은 레인에 홀드가 진행 중일 때 다음 onset이 겹쳐도 에러 없이 무시되는지 (고레벨 패턴에서 발생 가능)

---

## 4. 알려진 단순화/가정 (설계자 확인 필요)

- **콤보 카운터가 악기별이 아니라 전역(global)임**: 피아노의 "6연타 캐스케이드", 글록켄슈필의 "4/8버스트"는
  기획서상 그 악기 자신의 연속 히트를 의미하는 것으로 보이는데, 지금은 0단계에서 만든
  `RhythmManager.CurrentCombo`(모든 레인 통틀어 전역 콤보)를 그대로 재사용했습니다. 즉 피아노가 아닌 다른
  악기(예: 드럼)로 쌓은 콤보도 피아노의 6연타 캐스케이드를 트리거할 수 있습니다. 4개 악기를 장착한 이후
  (Lv5+) 구간에서는 이 차이가 체감될 수 있어, 실제 플레이해보고 "악기별 전용 콤보 카운터"로 분리할지
  설계자가 판단해야 합니다.
- **오프비트 구분 없음**: 마림바 Lv3("엇박자 타격 성공 시 파동 크기 +30%")는 패턴상 특정 스텝(엇박)에서만
  터져야 하지만, 지금은 그 구분 없이 Lv3 이상이면 항상 파동 크기가 커지도록 단순화했습니다.
- **Lv5 전용 효과 스텁**: 벨의 "지나간 자리 잔향 지속 타격", 마림바의 "감속+밀쳐냄", 글록켄슈필의
  "2차 유도 파편+기절"은 이번에 구현하지 않았습니다(Lv5는 그냥 Lv4와 동일하게 동작). 후속 세션에서 추가.
- **피어스/사거리 수치는 임의값**: 기획서엔 "관통", "사거리 증가" 같은 정성적 설명만 있고 정확한 수치가
  없어서, 감으로 넣은 값(피어스 2~4, 사거리 2.5~4.5 등)입니다. 플레이테스트 후 조정 필요.
