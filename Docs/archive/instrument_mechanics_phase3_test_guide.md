# 10종 악기별 공격 메커니즘 - 3단계(드럼/플루트) 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 3단계 구현을 실측 검증할 때 참고하는
절차서입니다. 코드 작성은 Cowork 세션에서, 실측 검증은 Claude Code(Unity MCP)에서 진행하고, **아직
커밋하지 않은 상태**입니다 — 이번 라운드도 테스트 결과를 먼저 확인한 뒤에 커밋할 예정입니다.

검증이 끝나면 **`Docs/instrument_mechanics_phase3_test_result.md`** 파일에 결과를 정리해주세요
(1·2단계 가이드/결과 문서와 동일한 형식). 버그를 발견하면 재현 절차·원인·(가능하다면) 수정 제안까지
적어주시면 Cowork 세션에서 그대로 반영합니다.

`10종 악기별 공격 메커니즘 기획서.docx`의 3단계 구현(드럼/플루트) 검증 절차입니다. 이 두 악기로 10종 전체
구현이 완료됩니다. 드럼은 게임 시작 시 슬롯 0(Q)에 자동 장착되는 기본 악기라 **가장 실측 빈도가 높은
악기**이니 특히 꼼꼼히 봐주세요.

---

## 0. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인. 신규/변경 파일:
   `Combat/InstrumentAttacks/ShockwaveVisualEffect.cs`, `Combat/InstrumentAttacks/FluteVortexEffect.cs`,
   `Combat/InstrumentAttacks/FluteVortexHoldEffect.cs`, `Combat/InstrumentAttacks/InstrumentAttackDispatcher.cs`(수정),
   `Combat/RhythmAttackManager.cs`(수정 - 드럼 상시 오라 `Update()` 추가), `Instrument/InstrumentPatternDatabase.cs`(수정 - 플루트를 홀드 기반으로 전환).
2. Play Mode 실측 시 이전 라운드 팁 재사용: `Application.runInBackground = true`, 필요시 `Time.timeScale` 조정.
3. **드럼은 게임 시작 시 자동 장착됩니다** (`InstrumentManager.Start()`가 `AcquireOrUpgradeInstrument(Drums)`를
   자동 호출). 즉 별도로 장착하지 않아도 Play Mode 진입 직후부터 상시 오라가 켜져 있어야 정상입니다 -
   만약 오라가 안 보인다면 그 자체가 버그일 수 있습니다.
4. **(1·2단계에서 정립된 방법론, 재사용 권장)** 실시간 `Bash sleep` 대기 대신 `Time.timeScale = 0f` +
   리플렉션으로 private 필드/메서드 직접 제어. 드럼 오라처럼 "매 프레임 자동 실행"되는 로직은 private
   메서드(`UpdateDrumAura`)를 리플렉션으로 직접 호출해 원하는 시점을 결정론적으로 재현하는 편이 안전합니다:
   ```csharp
   var attackMgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;
   var method = attackMgr.GetType().GetMethod("UpdateDrumAura",
       System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
   method.Invoke(attackMgr, null); // 0.5초어치 누적처럼 만들려면 drumAuraTickTimer 필드를 리플렉션으로 먼저 세팅
   ```

---

## 1. 무엇이 바뀌었나

**드럼**은 다른 9종과 근본적으로 다릅니다 - "판정 성공 시에만 발동"하는 나머지 9종과 달리, 드럼은 판정과
무관하게 항상 켜져 있는 **상시 오라**와, 정박(1,5,9,13) 판정 성공 시 발동하는 **비트 뱅**(넉백 파동) 두
가지를 동시에 갖습니다. 오라는 `RhythmAttackManager.Update()`가 매 프레임 "드럼이 언락된 슬롯에
장착되어 있는가"만 확인해서 독립적으로 처리하고, 비트 뱅은 기존 판정 성공 파이프라인
(`InstrumentAttackDispatcher.Execute` → `ExecuteDrums`)을 그대로 탑니다.

**플루트**는 2단계에서 만든 홀드 인프라(`IHoldAttackEffect`)를 재사용하지만, 다른 4종과 달리 홀드
"유지 중"에는 아무 효과가 없고 **릴리즈하는 순간에만** 미니 소용돌이(`FluteVortexEffect`)가 생성됩니다.
이 소용돌이는 직접 피해를 주지 않고 범위 내 적을 중앙으로 끌어당기기만 하는 순수 CC(군집) 효과입니다 -
다른 광역 공격(팀파니 융단폭격, 벨 8방향 섬광 등)과 조합했을 때 진가를 발휘하도록 설계된 유틸리티
악기입니다.

| 악기 | 노트 특성 | 핵심 메커니즘 | 레벨 스케일링 |
|---|---|---|---|
| 드럼 | 1,5,9,13 정박 단타 | 상시 비트 오라(지속 소량 타격) + 정박 타격 시 360도 비트 뱅(넉백) | 레벨당 오라/비트 뱅 범위 소폭 증가 |
| 플루트 | 3칸 숏 홀드(2~4칸 근사) | 릴리즈 시 이동 방향 반대쪽에 미니 소용돌이(끌어당김, 무피해) | 레벨당 범위/유지시간 소폭 증가 |

---

## 2. execute_code로 검증 (Play Mode, 몹이 화면에 있는 상태)

### 드럼 - 상시 오라
```csharp
// Play Mode 진입 직후(드럼은 이미 자동 장착된 상태) 씬에서 "DrumBeatAura" GameObject를 찾아 활성 상태 확인
var auraObj = GameObject.Find("DrumBeatAura");
Debug.Log($"오라 GameObject 존재={auraObj != null}, 활성={auraObj?.activeSelf}");

// 오라의 지속 타격이 실제로 들어가는지: 근처에 적을 하나 스폰해두고 0.5초(DrumAuraTickInterval)어치
// UpdateDrumAura()를 반복 호출해 hp 감소를 확인 (Time.timeScale=0 상태에서 리플렉션으로 강제 호출 권장)
```

Play Mode 체크리스트:
- [ ] Play Mode 진입 즉시(드럼 자동 장착) 플레이어 발밑에 `DrumBeatAura` 링 비주얼이 보이는지
- [ ] 오라 범위 안에 있는 적이 정박 판정 여부와 무관하게(가만히 서있기만 해도) 0.5초 간격으로 소량의
      데미지를 받는지
- [ ] 오라 범위 밖의 적은 전혀 영향받지 않는지
- [ ] 만약 드럼을 4슬롯 모두 다른 악기로 교체할 수 있는 상황이 된다면(현재는 슬롯0 고정이라 불가능할
      수 있음 - 코드상 가능 여부 확인) 드럼이 비활성 슬롯으로 밀려났을 때 오라가 꺼지는지

### 드럼 - 비트 뱅
- [ ] 정박(1,5,9,13) 노트를 판정 성공하면 오라와 별개로 확장하는 링(비트 뱅)이 발생하고, 범위 내 적이
      추가 피해 + 밀쳐남(넉백)을 받는지
- [ ] 비트 뱅 범위가 오라 범위보다 확실히 넓은지 (문서상 "장판이 거대하게 팽창" - 오라보다 임팩트가 커야 함)

### 플루트
```csharp
// 릴리즈 시 소용돌이가 생성되는지, 그리고 소용돌이가 적을 끌어당기기만 하고 피해는 주지 않는지 확인
var attackMgr = ConductorSymphony.Combat.RhythmAttackManager.Instance;
attackMgr.HandleRhythmHit(ConductorSymphony.Rhythm.HitRating.Perfect, ConductorSymphony.Rhythm.RhythmLane.Left); // 홀드 시작
ConductorSymphony.Combat.InstrumentAttacks.HoldEffectCoordinator.Release(ConductorSymphony.Rhythm.RhythmLane.Left, completedFully: true); // 즉시 릴리즈
// 이 시점에 씬에 "FluteVortex" GameObject가 생성되어 있어야 함
```
- [ ] 홀드 시작~유지 중에는 아무 이펙트도 나타나지 않는지 (플루트는 유지 중 효과 없음 - 의도된 동작)
- [ ] 릴리즈 시 `FluteVortex` GameObject가 플레이어 위치 근처(바라보는 방향 반대쪽)에 생성되는지
- [ ] 소용돌이 범위 내 적이 중심으로 서서히 끌려오는지, 그 과정에서 **피해는 전혀 발생하지 않는지**
      (의도된 설계 - 순수 CC)
- [ ] 소용돌이가 일정 시간(레벨1 기준 1.5초) 후 자동으로 사라지는지

---

## 3. 알려진 단순화/가정 (설계자 확인 필요)

- **드럼 오라 데미지 산식**: 오라는 판정 성공과 무관한 상시 효과라 M_rhythm(리듬 정확도 배율)을 의도적으로
  제외하고 시포르찬도(M_stat) 패시브만 반영했습니다. "실력과 무관한 baseline 딜 + 판정 성공 시 폭발적
  비트 뱅"이라는 의도인데, 이 설계 방향이 맞는지 확인 필요합니다.
- **드럼 슬롯 고정 가정**: 드럼이 슬롯 0(Q)에서 다른 악기로 교체 가능한지 여부를 현재 코드로는 확실히
  검증하지 못했습니다(`InstrumentManager`에 "장착 해제" API가 없어 보임 - 사실상 영구 고정일 가능성).
  만약 드럼을 절대 뗄 수 없는 구조라면, `RhythmAttackManager.IsDrumsActive()`의 "언락된 슬롯 확인" 로직은
  사실상 항상 true를 반환하는 방어적 코드가 됩니다 (동작엔 문제 없음).
- **플루트 "지나간 자리" 근사**: 기획서는 "플레이어가 지나간 자리"(이동 경로의 실제 궤적)를 명시하지만,
  이동 궤적을 기록하는 인프라가 없어 "바라보는 방향의 반대쪽에 고정 오프셋(0.8유닛)으로 스폰"으로
  근사했습니다. 실제 이동 경로 추적이 필요하면 별도 트레일 버퍼 구현이 필요합니다.
- **플루트 완주 여부 무관 발동**: 홀드를 끝까지 채우지 못하고 조기 이탈해도(`completedFully=false`)
  소용돌이가 동일하게 발동합니다(2단계의 바이올린 등과 같은 관례 - 완주 여부를 구분하지 않음).
- **플루트 무피해**: 기획서에 피해량 언급이 없어 순수 CC로 구현했습니다. 밸런스상 소용돌이에도 약한
  지속딜을 추가할지는 플레이테스트 후 판단이 필요합니다.
- **Lv5 전용 효과 스텁**: 드럼/플루트 모두 레벨 조건이 전부 `>=`/선형 증가 형태라 별도의 Lv5 전용 효과는
  없습니다 (1·2단계와 같은 전제 유지).

---

## 4. 10종 전체 완료 후 남는 것

이 라운드가 통과하면 `10종 악기별 공격 메커니즘 기획서.docx`의 10종 전체(드럼/피아노/바이올린/플루트/
프렌치호른/글록켄슈필/첼로/팀파니/마림바/벨) 구현이 끝납니다. `RhythmAttackManager`의 기존 "범용
투사체 폴백 로직"(`IsImplemented`/`IsHoldImplemented` 둘 다 false일 때만 실행되는 코드)은 이제 어떤
악기에도 도달하지 않는 죽은 코드가 되므로, 원하시면 다음 라운드에 정리(제거 또는 안전장치로 유지)를
검토할 수 있습니다.
