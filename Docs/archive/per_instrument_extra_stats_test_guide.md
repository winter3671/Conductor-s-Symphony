# extraDamage/extraProjectiles "판정 악기 전용" 정정 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

`InstrumentManager.GetTotalExtraDamage()`/`GetTotalExtraProjectiles()`는 "지금 판정된 그 악기"가
아니라 장착된 4슬롯 전체의 `extraDamage`/`extraProjectiles`를 합산해서 반환하고 있었습니다. 문서/
커밋 이력 어디에도 이게 의도된 "장착 시너지" 설계라는 근거가 없어(`game_systems_reference.md` §7-1),
사용자 결정으로 "판정된 그 악기만의 값을 쓰도록" 정정했습니다.

`RhythmAttackManager.HandleRhythmHit()`가 이제 `hitInstrument.extraDamage`/
`hitInstrument.extraProjectiles`를 직접 읽습니다(`hitInstrument`가 null이면 둘 다 0). 레가토 패시브
합산(`PassiveStatManager.GetExtraProjectiles()`)은 원래도 전역 패시브라 영향 없이 그대로입니다.
`InstrumentManager`의 두 합산 메서드는 더 이상 호출되는 곳이 없어 삭제했습니다.

**참고**: 이번 수정과 별개로, 같은 세션에서 문서만 정리한 판단 대기 항목 2건이 있습니다(코드 변경
없음) — 첼로 Lv4 "릴리즈 후 잔류시간" 해석 확정, 전역 콤보 공유 방식 유지 확정. 벨 패턴 주석 오류
(Lv4/Lv5 "8-accent"/"9-accent" → 실제 6/7개)도 사소한 주석 수정으로 별도 처리했습니다.

## 1. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인.
2. 악기 2종 이상을 장착하고, 그중 하나는 Lv3~5(extraDamage>0)로, 방금 새로 장착한 악기는 Lv1로
   세팅해 비교하면 효과가 뚜렷합니다.

## 2. 검증 항목

- [ ] 악기 A(Lv1, extraDamage=0)만 장착했을 때와 악기 B(Lv5, extraDamage=2)를 추가로 장착한 뒤에도,
      악기 A를 타격했을 때의 `baseDamage`(및 최종 `damage`)가 동일한지 (예전엔 B의 +2가 A의 히트에도
      더해졌음 — 이번 수정으로 사라져야 함)
- [ ] 악기 B(Lv5)를 직접 타격하면 여전히 자기 자신의 extraDamage(+2)가 정상 반영되는지
- [ ] `extraProjectiles`도 동일한 방식으로 "판정된 그 악기만" 반영되는지 (예: 피아노 Lv1 + 바이올린
      Lv4(extraProjectiles=1) 장착 시, 피아노를 타격해도 `shots`가 늘어나지 않아야 함)
- [ ] 레가토 패시브(전역 패시브, `PassiveStatManager.GetExtraProjectiles()`)는 이번 변경과 무관하게
      계속 모든 악기에 반영되는지 — extraProjectiles의 "슬롯 합산" 부분만 제거됐고 레가토 자체는
      원래도 전역
- [ ] hitInstrument가 null인 경우(슬롯 범위 밖 등 방어 코드 경로) 에러 없이 extraDamage/extraProj가
      0으로 처리되는지
- [ ] **회귀**: 데미지 공식의 나머지 부분(M_rhythm, M_stat, instrumentDpsMultiplier)은 이번 변경과
      무관하게 그대로인지

## 4. 검증 결과

**검증 일시**: 2026-08-07 / Unity MCP 연결 세션, Play Mode에서 `RhythmAttackManager.HandleRhythmHit()`를
리플렉션으로 직접 호출해 측정. `InstrumentManager`의 `acquiredInstruments` 리스트를 리플렉션으로 직접
구성해(레벨/슬롯 자유 제어) 두 악기 조합을 정밀 통제했다.

### 0. 사전 준비
- `refresh_unity(force, compile=request)` → 컴파일 성공, 에러/경고 0건.
- Play Mode 진입 성공. `InstrumentManager.Instance`, `RhythmAttackManager.Instance` 정상 확인.

### 1~2. extraDamage "판정 악기 전용" 격리 확인

| 시나리오 | 관측된 beam.damage | 비고 |
|---|---|---|
| 피아노 Lv1(slot0) 단독 장착, 피아노 타격(Left) | 75 | 기준값 |
| 위 상태에 벨 Lv5(extraDamage=2)를 slot1에 추가 장착 후, **다시 피아노 타격(Left)** | 75 | 기준값과 완전히 동일 — 벨의 +2가 더 이상 새지 않음(수정 전이었다면 더 높은 값이 나왔어야 함) |

같은 악기(벨)를 레벨만 고정(Lv1)한 채 `extraDamage` 필드만 0→2로 직접 조작해 인스트루먼트 DPS
배율 등 다른 변수를 완전히 통제한 재측정: `extraDamage=0`→`beam.damage=9`, `extraDamage=2`→
`beam.damage=19`. `DamageFormula.ComputeBaseDamage/ComputeFinalDamage`를 실제 런타임의
`mRhythm=0.5, mStat=1, instrumentDpsMultiplier=9.3`로 그대로 재계산한 결과도 각각 9/19로 **정확히
일치** — 벨 자신을 직접 타격하면 자기 자신의 extraDamage(+2)가 정상적으로 반영됨을 확인(항목 2 충족).

(참고: 벨 Lv1→Lv5로 레벨을 실제로 올려 비교했을 때는 오히려 데미지가 9→6으로 낮아졌는데, 이는
`InstrumentDamageTable`의 벨 레벨별 DPS 보정 배율이 레벨이 오를수록 낮아지도록 설계되어 있기
때문(레벨업 시 8방향×2연속 등 타수 자체가 늘어나므로 DPS 밸런스를 맞추기 위한 기존 설계) — 이번
diff와 무관한 별개 시스템이라 위 표에서는 레벨을 Lv1로 고정해 extraDamage 변수만 격리했다.)

### 3. extraProjectiles "판정 악기 전용" 격리 확인

| 시나리오 | 관측된 shots(InstrumentBeam 수) |
|---|---|
| 피아노 Lv1(extraProjectiles=0) 단독, 피아노 타격 | 1 |
| 위 상태에 바이올린 Lv4(extraProjectiles=1)를 slot1에 추가 장착 후, 다시 피아노 타격 | 1 (동일, 격리 확인) |

### 4. 레가토(전역 패시브) 정상 반영 확인
`PassiveStatManager`에 레가토 Lv3(`GetExtraProjectiles()`=1)을 적용한 뒤, 피아노(extraProjectiles=0,
슬롯 합산 격리 대상)를 타격 → `shots=2`(기본 1 + 레가토 전역 +1)로 정확히 반영됨. 슬롯 합산 로직만
제거되고 레가토 자체(전역 패시브)는 원래 설계대로 계속 작동함을 확인.

### 5. `hitInstrument == null` 방어 경로
슬롯을 1개(피아노)만 장착한 상태에서 `RhythmLane.Right`(→slot 1, 범위 밖)로 타격 → 예외 없이 정상
처리됨(콘솔 에러 0건). `hitInstrument`가 null일 때 `extraDamage`/`extraProj`가 각각 0으로 처리되어
`IsImplemented`/`IsHoldImplemented` 분기를 모두 건너뛰고 기존 범용 투사체 폴백 로직으로 안전하게
빠짐(코드 확인).

### 6. 회귀 — 데미지 공식 나머지 부분 불변
위 1~2번 항목에서 `DamageFormula`를 실제 런타임 값(`mRhythm`, `mStat`, `instrumentDpsMultiplier`)으로
독립적으로 재계산한 결과가 `RhythmAttackManager`의 실제 출력과 소수 오차 없이 정확히 일치 —
M_rhythm/M_stat/악기별 DPS 보정 배율 전체 파이프라인이 이번 변경과 무관하게 온전히 연결되어 있음을
확인.

### 부가 확인 — 벨 패턴 주석 오류 수정 (InstrumentPatternDatabase.cs)
Lv4/Lv5 패턴 문자열의 실제 `'1'` 개수를 세어보면 각각 6개/7개로, 수정된 주석("6/7개 accent")과 정확히
일치(수정 전 주석은 "8/9개"로 오기재되어 있었음 — 코드 변경 없이 주석만 정정된 건이라 별도 실행 테스트
없이 문자열 카운트로 교차검증).

### 종합 결과
검증 가이드의 6개 항목 모두 통과. 컴파일 에러/경고 없음(무관한 네트워크 릴레이 경고 1건만 존재).
`GetTotalExtraDamage()`/`GetTotalExtraProjectiles()` 삭제 후에도 참조 남은 곳 없음(코드베이스 전체
grep 결과 주석에서만 언급, 실제 호출 0건 — 컴파일 성공으로도 이미 증명됨). extraDamage/extraProjectiles
모두 "판정된 그 악기"만의 값으로 정확히 격리되었고, 레가토 전역 패시브와 나머지 데미지 공식은 기존대로
정상 동작함을 확인.
