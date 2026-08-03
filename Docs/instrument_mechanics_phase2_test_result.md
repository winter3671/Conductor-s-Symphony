# 10종 악기별 공격 메커니즘 - 2단계(바이올린/프렌치호른/첼로/팀파니, 홀드 기반) 검증 결과

`instrument_mechanics_phase2_test_guide.md`에 정리된 절차에 따라 **Unity MCP(Unity 6000.5.5f1,
`My project` 인스턴스)**로 실측 검증을 진행한 결과입니다. 1단계에서 정립한 방법론
(`Time.timeScale=0` + 리플렉션으로 private 필드/메서드 직접 제어)을 그대로 재사용했습니다.

- **1차 검증**: HoldEffectCoordinator 공통 인프라 및 프렌치호른/첼로/팀파니 3종은 전부 PASS. 바이올린에서
  치명적 버그 1건 발견 (홀드 유지 중 데미지 판정이 두 번째 틱부터 100% 예외를 던짐).
- **수정 및 2차 검증(이 문서 상단에 결과 추가)**: `ViolinOrbitEffect.TickDamage()`를 키 스냅샷 방식으로
  수정 후 재검증 → **해결 확인**. 상세는 "2차 검증" 섹션 참고.

**최종 종합 결과: 2단계(바이올린/프렌치호른/첼로/팀파니) 전부 PASS.**

---

## 2차 검증 (바이올린 버그 수정 후 재테스트) — 2026-08-03

### 수정 내용 확인
`ViolinOrbitEffect.cs:74-106`의 `TickDamage()`가 `hitCooldowns.Keys`의 스냅샷(`new
List<EnemyMonster>(hitCooldowns.Keys)`)을 만들어 그 스냅샷을 순회하도록 수정됨을 코드로 확인. 1차
검증에서 제안한 수정 방향과 정확히 일치.

### 재현 시나리오 재검증 — ✅ 해결 확인
1차와 동일한 시나리오(바이올린 홀드 → 적 1회 타격 → 홀드 유지한 채 추가 틱 호출)를 반복:
```
Tick 1 (누적 0.1s): 성공, hp=99995   // 첫 타격
Tick 2 (누적 0.2s): 성공, hp=99995   // 쿨다운 중 - 안전하게 통과 (이전엔 여기서 크래시)
Tick 3 (누적 0.3s): 성공, hp=99995
Tick 4 (누적 0.4s): 성공, hp=99995
Tick 5 (누적 0.5s): 성공, hp=99990   // 쿨다운(0.35s) 만료 후 재타격
크래시 여부=False
```
1차 검증 때 발견하지 못했던 **히트 쿨다운 타이밍 자체("같은 적을 매 프레임 때리지 않고 0.35초 간격으로만
타격")**까지 이번엔 예외 없이 정확하게 확인되었습니다 — 정확히 쿨다운 만료 시점(누적 0.5s > 0.35s)에서만
재타격이 발생했습니다.

**다중 적 동시 쿨다운 스트레스 테스트** (스냅샷 방식이 여러 키를 동시에 안전하게 처리하는지 추가 확인):
```
3마리 동시 쿨다운 상태에서 6회 Tick(누적0.6s) 후 크래시=False
d1 hp=99990, d2 hp=99990, d3 hp=99990 (0.6s 동안 쿨다운 0.35s 기준 2번씩 정확히 타격)
릴리즈 시 참격 개수=3(Lv3 기대3) - 여러 틱 이후에도 릴리즈 정상 동작
```
3마리를 동시에 걸어도 크래시 없이 정확히 계산되고, 여러 차례 틱을 거친 뒤의 `OnHoldReleased`(부채꼴
참격)도 여전히 정상 동작함을 확인.

### 2차 검증 결론
1차에서 발견된 유일한 Critical 버그(바이올린 홀드 유지 중 데미지 판정 크래시)가 완전히 해결되었고,
버그 때문에 1차에서 검증하지 못했던 "히트 쿨다운 타이밍" 항목까지 이번에 추가로 확인되었습니다. 사이드
이펙트도 없습니다.

---

## 1차 검증 결과 (아래부터는 이전 실측 기록 원문)

---

## 0. 사전 준비

- `refresh_unity(mode=force, compile=request)` → 최초 호출은 60초 타임아웃 응답을 반환했으나 (신규 파일
  6개 추가로 인한 도메인 리로드가 평소보다 오래 걸린 것으로 추정), 직후 `editor/state`를 확인하니
  `ready_for_tools=true`로 정상 완료된 상태였음. 컴파일 에러/경고 없음.
- Play Mode 진입 직후 `Application.runInBackground = true`, `Time.timeScale = 0f` 설정, 실제
  `EnemySpawner`는 비활성화하고 기존 몹을 전부 정리해 테스트 오염 방지 (1단계와 동일한 절차).

→ **PASS**

---

## 1. HoldEffectCoordinator 공통 동작 — PASS

- **홀드 시작**: `BeginHold(lane, type, ...)` 호출 시 `HoldEffect_<악기명>` GameObject가 정확한 컴포넌트와
  함께 생성됨을 확인 (`HoldEffect_Violin`+`ViolinOrbitEffect`, `HoldEffect_Cello`+`CelloGravityFieldEffect`).
- **레인별 독립성**: Left(바이올린)와 Right(첼로)를 동시에 시작한 뒤 Left만 `Release`해도 Right는 전혀
  영향받지 않고 유지됨을 확인.
- **해제된 레인에 Tick 호출**: 내부 딕셔너리에서 이미 제거된 레인에 `Tick()`을 호출해도 예외 없이 조용히
  무시됨.
- **Stale 이펙트 안전장치**: 같은 레인에 이미 홀드가 있는 상태에서(Release 없이) 새 `BeginHold`를 호출하면,
  코디네이터 내부 딕셔너리(`activeEffects`)가 새 이펙트(첼로)로 정확히 교체됨을 리플렉션으로 확인. 이전
  바이올린 GameObject 자체는 Unity의 `Destroy()` 지연 특성상 그 프레임엔 씬에 남아있지만(1단계에서도
  확인된 정상 동작), 코디네이터가 더 이상 그것을 추적하지 않으므로 이후 `Tick`은 새 이펙트에만 전달됨.

---

## 2. 바이올린 — 🔴 Critical 버그 발견 (홀드 유지 중 데미지 판정 100% 크래시)

### 정상 확인된 부분
- **Init 레벨 스케일링**: Lv1 `bladeCount=2, radius=1.4` / Lv3 `bladeCount=3, radius=1.7` 전부 정확.
- **OnHoldReleased(부채꼴 참격)**: Lv3(레벨4 미만) 참격 3발·관통5, Lv4 참격 5발 정확히 생성. 5발 전부
  플레이어가 바라보는 방향(부채꼴 스프레드 14도) 근처에 정확히 분포함을 각도 계산으로 확인. 릴리즈 직후
  최초 히트(캐스팅 시점, `hitCooldowns`가 비어있는 상태)는 정상적으로 5데미지를 입힘을 확인.

### 🔴 재현된 버그: `ViolinOrbitEffect.TickDamage()`가 두 번째 유지 틱부터 `InvalidOperationException`으로 크래시

**재현 절차**: 바이올린 홀드를 시작해 적을 1회 타격(히트 쿨다운 등록)한 뒤, 홀드를 유지한 채
`OnHoldTick`을 한 번 더 호출(=다음 프레임).

**실측 로그**:
```
Tick 1: 성공, hp=99995   // 최초 타격 (hitCooldowns 비어있음 → 정상)
Tick 2: 예외 발생! InvalidOperationException: Collection was modified; enumeration operation may not execute.
```
스택트레이스는 정확히 `ViolinOrbitEffect.TickDamage()` 78번째 줄(`ConductorSymphony.Combat.InstrumentAttacks.ViolinOrbitEffect.TickDamage`)을 가리킴.

**근본 원인** (`ViolinOrbitEffect.cs:74-104`):
```csharp
private void TickDamage(float deltaTime)
{
    List<EnemyMonster> expired = new List<EnemyMonster>();
    foreach (var kv in hitCooldowns)              // ← hitCooldowns를 foreach로 순회 중
    {
        float remaining = kv.Value - deltaTime;
        if (remaining <= 0f)
        {
            expired.Add(kv.Key);                  // 만료분은 별도 리스트에 모아뒀다가 순회 후 제거 (이 부분은 안전)
        }
        else
        {
            hitCooldowns[kv.Key] = remaining;     // ★ 문제: 같은 딕셔너리를 순회 도중 인덱서로 값 갱신
        }
    }
    ...
}
```
.NET의 `Dictionary<TKey,TValue>`는 `foreach`로 순회하는 도중 인덱서(`dict[key] = value`)로 **기존 키의
값만 바꿔도** 내부 버전 카운터가 증가해 다음 `MoveNext()` 호출 시 "컬렉션이 수정됨" 예외를 던집니다.
만료된 항목을 별도 리스트에 모았다가 순회 후 제거하는 패턴(`expired` 리스트)은 정확히 적용했지만,
**아직 만료되지 않은 항목의 남은 시간을 갱신하는 `else` 분기는 같은 안전장치 없이 순회 중인 딕셔너리를
직접 수정**하고 있습니다.

**실제 게임플레이 영향**: `OnHoldTick`은 `RhythmManager.Update()` → `UpdateActiveHolds()` →
`OnHoldTickEvent` 구독을 통해 **매 프레임** 호출됩니다. 즉 바이올린으로 적을 단 한 번이라도 맞히는 순간,
그 히트 쿨다운(0.35초)이 끝나기 전까지 **다음 프레임부터 계속 이 예외가 발생**합니다. Unity 엔진이
예외를 잡아 콘솔에 에러로 로그를 남기고 다음 프레임으로 넘어가긴 하지만(앱 전체가 크래시하진 않음),
`RhythmManager.UpdateActiveHolds()` 내부의 `for` 루프가 이 지점에서 중단되므로 **그 프레임에 처리
중이던 다른 레인의 홀드 로직(완료 체크 등)도 함께 스킵**될 수 있고, 콘솔에는 바이올린을 사용하는 내내
매 프레임 에러가 쏟아지게 됩니다. 사실상 **바이올린은 적을 한 번이라도 맞히는 순간부터 사실상
사용 불가능한 상태**입니다.

이 버그 때문에 "히트 쿨다운 동안 재타격 안 함" / "쿨다운 만료 후 재타격" 타이밍은 이번 라운드에서
실측으로 더 검증할 수 없었습니다 (크래시 이전 최초 1회 타격까지는 정상 동작 확인함).

**제안 수정**: `expired` 리스트와 동일한 패턴으로, 갱신할 항목도 별도로 모았다가 순회 후 반영하거나,
가장 간단하게는 키 목록의 스냅샷을 떠서 순회하면 됩니다.
```csharp
private void TickDamage(float deltaTime)
{
    // 키 스냅샷을 만들어 순회 (원본 딕셔너리를 안전하게 갱신 가능)
    var keys = new List<EnemyMonster>(hitCooldowns.Keys);
    foreach (var key in keys)
    {
        float remaining = hitCooldowns[key] - deltaTime;
        if (remaining <= 0f) hitCooldowns.Remove(key);
        else hitCooldowns[key] = remaining;
    }
    ...
}
```

**다른 3종 교차 점검**: 같은 종류의 "딕셔너리/컬렉션을 순회하며 그 자리에서 수정" 패턴이 있는지 나머지
3종도 코드로 확인했습니다.
- `CelloGravityFieldEffect`: `affectedEnemies`(HashSet)를 순회하는 동안 직접 수정하지 않고, 순회가 끝난
  뒤 `Clear()` + 재구성하는 안전한 패턴 사용 → 문제 없음.
- `FrenchHornConeEffect`: `FindObjectsByType`이 반환한 배열(스냅샷)만 순회 → 문제 없음.
- `TimpaniBombardmentEffect`: `OnHoldTick`에서 컬렉션 순회 자체가 없음 → 해당 없음.

→ **이 버그는 바이올린(`ViolinOrbitEffect`) 한 곳에 국한됩니다.**

---

## 3. 프렌치호른 — PASS

- **Init 레벨 스케일링**: Lv1 `range=3.0, halfAngle=60°` / Lv4 `range=4.2, halfAngle=90°` 정확.
- **부채꼴 각도 판정**: 정면(각도0, 사거리 안)의 적은 타격+넉백, 정반대(각도180)의 적과 사거리 밖(거리10)의
  적은 전혀 영향받지 않음을 확인.
- **넉백 방향**: 맞은 적이 플레이어로부터 멀어지는 방향(+X)으로 정확히 밀려남을 좌표 비교로 확인.
- **틱 간격 게이팅**: `TickInterval(0.2초)` 미만 누적으로는 타격이 발화하지 않고, 누적 후에만 발화함을 확인.
- **방향 추적**: 플레이어 바라보는 방향을 바꾸면 부채꼴도 즉시 따라 회전 — 처음엔 정확히 경계값(90도)에
  걸리는 케이스를 잘못 골라 혼란이 있었으나(버그 아님, 테스트 시나리오 실수), 명확히 각도 밖(180도 반대
  방향)으로 재검증해 정상 확인.

---

## 4. 첼로 — PASS

- **Init 레벨 스케일링**: Lv1 `radius=1.8, slowFraction=0.5` 정확.
- **고정 위치(적 추적 안 함)**: 캐스팅 시점 최근접 적 위치에 필드가 생성된 뒤, 그 적을 멀리 이동시켜도
  필드 위치가 전혀 변하지 않음을 확인 — 기획서의 "고정된 중력장" 의도와 정확히 일치.
- **범위 내 감속**: 필드 범위 안에 들어온 적의 `speedMultiplier`가 정확히 `1-slowFraction`(Lv1=0.5)으로
  설정됨을 확인.
- **범위 이탈 시 감속 해제 (가이드가 지목한 치명적 케이스)**: 감속된 적을 필드 범위 밖으로 이동시킨 뒤
  다음 틱에서 `speedMultiplier`가 정확히 `1.0`으로 복원됨을 확인 — **버그 없음**.
- **주기적 데미지**: `TickInterval(0.4초)` 간격으로만 데미지가 들어감을 확인.
- **홀드 종료 시 잔여 해제**: 홀드가 끝난 시점에 필드 범위 안에 남아있던 적의 감속도 함께 정상 해제됨을
  확인.

---

## 5. 팀파니 — PASS

- **즉발 캐논**: 홀드 시작 즉시 최근접 적 위치에 착탄 이펙트 생성, `delay=0.05`, 데미지는 전체 데미지
  그대로(감쇄 없음) 확인. 스플래시 반경 Lv1=1.0 / Lv3=1.4로 레벨 스케일링 정확.
- **주기적 융단폭격**: `bombardInterval` Lv1~3=0.65초 / Lv4+=0.45초 각각 정확한 간격에서만 추가 착탄이
  발생함을 확인. 추가 착탄의 데미지는 `Max(1, damage/2)`로 정확히 절반 적용됨을 확인(즉발 캐논은 원본
  데미지 그대로).
- **홀드 도중 해제 시 추가 폭격 중단**: `Release()` 이후 `Tick()`을 호출해도(코디네이터 딕셔너리에서 이미
  제거되어) 착탄이 더 이상 추가되지 않음을 확인. 이미 스폰된 착탄 이펙트는 각자의 생명주기대로 별도로
  완료되므로 이 부분은 가이드에 명시된 대로 정상입니다.

---

## 6. 가이드에 명시된 알려진 단순화 사항 — 코드로 재확인

- **팀파니 탭/롤링 통합**: `TimpaniBombardmentEffect.Init()`이 "즉발 캐논" 스폰을, `OnHoldTick()`이 "융단
  폭격"을 각각 담당하는 구조로 실제 구현되어 있음을 확인. 가이드 설명과 일치.
- **프렌치호른 부채꼴=원형 근사 시각화**: `Init()`에서 정확히 `ProceduralSpriteFactory.CreateFilledCircle`로
  범위를 표시하고, 실제 판정은 `Vector2.Angle` 기반 정확한 부채꼴 계산을 별도로 수행함을 코드로 확인 —
  가이드 설명과 일치.
- **Lv5 전용 효과 스텁**: 4종 모두 레벨 조건이 전부 `>=` 형태라 Lv5는 Lv4 조건을 그대로 상속함을 코드로
  확인. 가이드 설명대로 미구현 상태.

---

## 7. 종합 결론 (1차 기준 — 2차 검증 결과는 최상단 참고)

| 항목 | 1차 결과 | 2차 결과 |
|---|---|---|
| 컴파일 | PASS | PASS |
| HoldEffectCoordinator (시작/해제/레인독립성/stale 안전장치) | PASS | - (재변경 없어 미재검) |
| 바이올린 - Init/릴리즈 참격 | PASS | PASS (여러 틱 이후에도 정상) |
| **바이올린 - 홀드 유지 중 데미지 판정 (`TickDamage`)** | **FAIL (Critical — 두 번째 틱부터 100% 크래시)** | **PASS (수정 확인, 쿨다운 타이밍까지 검증됨)** |
| 프렌치호른 (각도판정/넉백/방향추적/틱간격) | PASS | - (재변경 없어 미재검) |
| 첼로 (고정위치/감속/범위이탈해제/주기데미지/릴리즈해제) | PASS | - (재변경 없어 미재검) |
| 팀파니 (즉발캐논/주기융단폭격/레벨스케일링/릴리즈중단) | PASS | - (재변경 없어 미재검) |

**결론**: 1차에서 발견된 유일한 Critical 버그가 수정 후 해결 확인되었으며, 버그로 인해 1차에서 검증하지
못했던 히트 쿨다운 타이밍(0.35초 간격 재타격)까지 이번엔 정확히 확인되었습니다. 2단계(바이올린/
프렌치호른/첼로/팀파니) 구현은 **전부 PASS**로 판단합니다.
