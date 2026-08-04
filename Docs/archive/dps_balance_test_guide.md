# DPS 밸런스 보정 실측 검증 가이드

`Docs/dps_balance_gap_analysis.md`에서 발견한 문제(악기별 실제 DPS가 밸런스 doc 목표의 1~20%에
불과했던 문제)를 고치기 위해 `InstrumentDamageTable.cs`(악기·레벨별 보정 배율)를 새로 추가하고
`RhythmAttackManager`에 연결했습니다. **이 배율표는 여러 가정을 깔고 이론적으로 역산한 1차 값이라,
Unity MCP 실측으로 실제 DPS가 목표에 얼마나 근접하는지 확인하고 필요하면 배율을 재조정해야 합니다.**

이 문서는 그 실측 절차입니다. 코드 작성은 Cowork 세션에서, 실측은 Claude Code(Unity MCP)에서
진행하고, **아직 커밋하지 않은 상태**입니다.

검증이 끝나면 **`Docs/dps_balance_test_result.md`** 파일에 결과를 정리해주세요. 목표와 크게 어긋나는
악기·레벨이 있으면 재현 절차와 함께 적어주시면, `InstrumentDamageTable.cs`의 배율을 다시 계산해서
반영하겠습니다.

---

## 0. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 신규/변경 파일:
   `Instrument/InstrumentDamageTable.cs`(신규), `Combat/RhythmAttackManager.cs`(수정 -
   `HandleRhythmHit()`와 `UpdateDrumAura()`에 배율 적용).
2. 측정용 더미가 필요합니다 — 기존 `EnemyMonster`를 그대로 쓰되 `Initialize(playerTransform,
   sprite, color, initialHp: 999999)`로 체력을 사실상 무한대로 만들어서, 측정 도중 죽어서 사라지는
   일이 없게 합니다. 더미는 **플레이어 위치에서 아주 가까운 고정 위치**(예: player.position +
   Vector3.up * 0.5f)에 둡니다 — 대부분의 악기가 "가장 가까운 적" 또는 "플레이어 중심"을 기준으로
   조준하므로, 더미가 항상 그 기준점 역할을 하게 하기 위함입니다.
3. **측정 전 필수**: 다른 악기는 전부 미장착 상태로 측정 대상 악기 1개만 장착하세요(`InstrumentDamageTable`의
   배율은 "악기 1개만 장착"을 가정해 역산했고, `GetTotalExtraDamage()`가 장착된 슬롯 전체를 합산하는
   기존 특성 때문에 다른 악기가 같이 있으면 결과가 오염됩니다 — `dps_balance_gap_analysis.md` §4 참고).
4. BPM 97, 32스텝 1루프 = **9.897초**(`stepDuration=(60/97)/2≈0.309278`). 아래 모든 측정은 "정확히
   1루프(9.897초)" 동안 발생하는 데미지를 재는 것을 기준으로 합니다.
5. (1~4단계에서 정립된 방법론 재사용) `Time.timeScale=0` + 리플렉션/직접 API 호출로 결정론적으로
   진행하세요. 실시간 대기보다 훨씬 안정적입니다.

---

## 1. 측정 방법론

### 1-1. 탭 5종(피아노/벨/마림바/글록켄슈필/드럼) — 간단

레벨별 정확한 노트 스텝 위치(아래 표)만큼 `RhythmAttackManager.Instance.HandleRhythmHit(HitRating.Perfect,
해당악기의_레인)`을 호출하면 됩니다. 스텝 간격은 무시하고(어차피 즉발이라 타이밍은 DPS에 영향 없음)
"1루프당 몇 번 호출되는가"만 정확하면 됩니다.

```csharp
// 예: 피아노 Lv5 (레인=악기가 장착된 슬롯의 레인, GetLaneForSlot 참고)
var enemy = /* 스폰한 더미 */;
int hpBefore = enemy.CurrentHealth;
var mgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;
for (int i = 0; i < 10; i++) // 피아노 Lv5 = 루프당 10회 (아래 표)
{
    mgr.HandleRhythmHit(ConductorSymphony.Rhythm.HitRating.Perfect, ConductorSymphony.Rhythm.RhythmLane.Left);
}
int totalDamage = hpBefore - enemy.CurrentHealth;
float dps = totalDamage / 9.897f;
Debug.Log($"Piano Lv5 DPS = {dps}");
```

**주의**: 피아노 Lv5와 글록켄슈필 Lv4+는 "전역 콤보"(`RhythmManager.CurrentCombo`)가 6배수/4배수일
때 추가 발사가 나갑니다. 위처럼 단순 반복 호출로는 콤보가 정상적으로 오르지 않을 수 있으니, 콤보를
리플렉션으로 직접 세팅하거나(`RhythmManager.Instance`의 private 콤보 필드), 10회 호출 전체를
"콤보가 1부터 순서대로 오른다"고 가정하고 진행해주세요 — 정확히는 안 맞아도 근사치로 충분합니다.

### 1-2. 홀드 5종(바이올린/프렌치호른/첼로/팀파니/플루트) — 홀드 사이클 반복

`HoldEffectCoordinator.BeginHold(lane, type, level, damage, origin, color)` →
`HoldEffectCoordinator.Tick(lane, deltaTime)`를 **작은 간격(0.05초)으로 여러 번** → 마지막에
`HoldEffectCoordinator.Release(lane, completedFully:true)`를 "1루프당 실제 홀드 시작 횟수"만큼
반복합니다. tick을 큰 deltaTime 한 번으로 몰아서 주면 안 됩니다 — 바이올린의 0.35초 히트 쿨다운,
첼로의 0.4초 틱 간격처럼 각 이펙트 내부 타이머가 여러 번 갱신돼야 실제와 같은 횟수만큼 데미지가
들어갑니다.

```csharp
// 예: 첼로 Lv3 (홀드 길이 13스텝 = 13*0.309278 ≈ 4.021초, 루프당 2회 - 아래 표)
var mgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;
var coordinator = typeof(ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator);
int hpBefore = enemy.CurrentHealth;
for (int cycle = 0; cycle < 2; cycle++) // 첼로 = 루프당 2회 홀드 시작
{
    ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.BeginHold(
        ConductorSymphony.Rhythm.RhythmLane.Left, ConductorSymphony.Instrument.InstrumentType.Cello,
        level: 3, damage: /* HandleRhythmHit이 실제로 계산한 damage 값을 리플렉션으로 확인하거나 동일 공식으로 직접 계산 */,
        origin: enemy.transform.position, color: Color.white);

    float holdDuration = 13 * 0.309278f; // 홀드 길이(스텝) * stepDuration
    float elapsed = 0f;
    while (elapsed < holdDuration)
    {
        ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.Tick(ConductorSymphony.Rhythm.RhythmLane.Left, 0.05f);
        elapsed += 0.05f;
    }
    ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.Release(ConductorSymphony.Rhythm.RhythmLane.Left, completedFully: true);
}
int totalDamage = hpBefore - enemy.CurrentHealth;
float dps = totalDamage / 9.897f;
Debug.Log($"Cello Lv3 DPS = {dps}");
```

`damage` 값은 실제 게임에서 `HandleRhythmHit()`이 계산하는 것과 똑같이 맞춰야 정확합니다 — 가장
쉬운 방법은 악기를 실제로 장착한 상태에서 해당 레인을 1회 `HandleRhythmHit`으로 쳐보고 그 직후
`InstrumentDamageTable.GetDamageMultiplier(type, level)` 등을 참고해 직접 계산하거나, 아니면
`HandleRhythmHit` 자체를 호출해서(=`mgr.HandleRhythmHit(HitRating.Perfect, lane)`) 홀드를
시작시키는 방법을 대신 써도 됩니다(1-1과 동일한 방식으로 대체 가능 — 이 경우 `BeginHold`를 직접
호출할 필요 없이 `HandleRhythmHit`이 알아서 `HoldEffectCoordinator.BeginHold`를 호출합니다. 다만
tick/release는 여전히 직접 반복 호출해야 합니다).

---

## 2. 악기별 루프당 실제 타격/홀드 횟수 (정확히 계산됨 — 재계산 불필요)

`dps_balance_gap_analysis.md` §2에서 32스텝 패턴과 홀드 중첩 스킵 규칙을 시뮬레이션해 확정한 값입니다.

### 탭 5종 (노트 스텝 위치)
| 악기 | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 |
|---|---|---|---|---|---|
| 드럼 | [0,8,16,24] (4회) | 6회 | 8회 | 9회 | 11회 |
| 피아노 | [0,16] (2회) | 4회 | 6회 | 8회 | 10회 |
| 벨 | [4,20] (2회) | 4회 | 5회 | **6회**(주석은 8회로 잘못 적혀있음) | **7회**(주석은 9회로 잘못 적혀있음) |
| 마림바 | [2,18] (2회) | 4회 | 6회 | 8회 | 9회 |
| 글록켄슈필 | [0,16] (2회) | 4회 | 5회 | 8회 | 13회 |

### 홀드 5종 (실제 홀드 시작 횟수 — 원본 노트 개수와 다름, §2-2 참고)
| 악기 | 홀드길이(스텝→초) | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 |
|---|---|---|---|---|---|---|
| 바이올린 | 13→4.021s | 2 | 2 | 2 | 2 | 2 |
| 프렌치호른 | 6→1.856s | 2 | 4 | 4 | 4 | 4 |
| 첼로 | 13→4.021s | 2 | 2 | 2 | 2 | 2 |
| 팀파니 | 16→4.948s | 2 | 2 | 2 | 2 | 2 |
| 플루트 | 3→0.928s | 2 | 4 | 6 | 8 | 8 |

플루트는 기획 의도상 무피해(CC 전용)라 DPS 측정 대상이 아닙니다(끌어당김 세기/범위만 확인하면 됨 -
4단계 가이드에서 이미 검증됨).

---

## 3. 측정 대상 및 기록 형식

10종 × 5레벨 = 50개 조합 전부 측정을 권장하지만, 시간이 부족하면 **각 악기의 Lv1과 Lv5(최소/최대)만
우선 측정**해주세요 — 배율표가 대략적으로 맞는지 확인하는 데는 이것만으로도 충분합니다.

`Docs/dps_balance_test_result.md`에 아래 형식으로 기록해주세요:

```
| 악기 | 레벨 | 목표 DPS | 실측 DPS | 비율 |
|---|---|---|---|---|
| 피아노 | 1 | 30 | (측정값) | (측정/목표) |
| 피아노 | 5 | 150 | (측정값) | (측정/목표) |
...
```

목표 DPS 대비 실측 비율이 **0.7~1.3배 범위 안이면 합격**으로 간주하고 넘어가도 됩니다(밸런스는
어차피 나중에 실제 플레이 감으로 미세조정하는 영역이라, 정확히 1.00배를 맞출 필요는 없습니다).
0.5배 미만이거나 1.5배 초과처럼 크게 벗어나는 항목만 표시해주시면, 해당 악기·레벨의
`InstrumentDamageTable.cs` 배율을 다시 계산해서 반영하겠습니다.

---

## 4. 알려진 가정/한계

- 더미가 항상 조준선 위·사거리 안에 있다고 가정합니다. 실제 게임에서는 적이 이동하고 여러 마리가
  겹쳐 있어 명중률이 이보다 낮거나(빗나감) 높을(스플래시로 여러 마리) 수 있습니다 — 이번 측정은
  "단일 고정 타겟 기준 이론 상한에 가까운 값"으로 이해해주세요.
- 드럼은 비트 뱅 + 비트 오라 두 효과가 합산되어야 목표와 비교할 수 있습니다(1-1 방식으로 비트 뱅만
  측정 후, 오라는 `UpdateDrumAura()`를 0.5초 간격으로 여러 번 호출해서 별도로 더해야 합니다 - 3단계
  가이드의 드럼 오라 측정 방식 참고).
- 피아노 Lv5/글록켄슈필 Lv4+의 콤보 의존 보너스는 위에서 언급했듯 근사치입니다.
