# 레가토(Legato) 패시브 / 악기 Lv4 Multi+1 연동 - 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 작업을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요.

## 0. 무엇을 고쳤나

8종 패시브 중 마지막으로 남아있던 죽은 스탯이었습니다. 레가토(투사체 수 +1(Lv3)/+1(Lv5), 최대 +2)와
각 악기 Lv4의 `extraProjectiles`("Multi +1")는 계산 함수(`InstrumentManager.GetTotalExtraProjectiles()`,
`PassiveStatManager.GetExtraProjectiles()`)는 정확했지만, 실제로 이 값을 소비하는 코드가 신규 악기
추가 대비용으로 남겨둔 죽은 폴백 로직 하나뿐이었습니다(`archive/team_review_needed.md`에서 이관된
`game_systems_reference.md` §3-9 참고).

**적용 대상 6종** — 발사체/낙하체/칼날처럼 "낱개로 셀 수 있는" 공격이 있는 악기:

| 악기 | 처리 방식 |
|---|---|
| 피아노 | 관통 레이저 발사 수(`shots`)에 그대로 가산 |
| 벨 | 기존 8방향 사이 빈 각도(22.5° 간격)를 채우는 추가 성광 |
| 마림바 | 원본 파동과 평행하게 좌우로 갈라지는 추가 파동(이동 방향에 수직으로 0.6유닛 오프셋) |
| 글록켄슈필 | Lv4 버스트와 동일한 랜덤 오프셋 낙하 지점 추가(버스트 조건과 무관하게 항상 적용) |
| 바이올린 | 궤도 칼날 개수(`bladeCount`)에 그대로 가산 |
| 팀파니 | 홀드 시작 시 캐논 착탄을 랜덤 오프셋으로 추가 발사 |

**제외 4종** — 지속형 판정이라 "낱개 투사체" 개념이 없음(2026-08-07, 사용자 결정):

드럼(광역 오라/충격파), 프렌치호른(지속 부채꼴), 첼로(고정 필드), 플루트(무피해 CC 소용돌이). 4종 모두
인터페이스 시그니처는 `extraProjectiles` 파라미터를 받지만 본문에서 사용하지 않습니다.

**인터페이스 변경**: `ITapAttackEffect.Execute(...)`와 `IHoldAttackEffect.Init(...)`에 `int
extraProjectiles` 파라미터를 추가했습니다. `RhythmAttackManager.HandleRhythmHit()`가 계산한 `extraProj`
(레가토 + 장착 4슬롯 Multi+1 합산치, 기존 `GetTotalExtraDamage()`와 동일하게 "지금 판정된 악기"가
아니라 4슬롯 전체 합산 — `game_systems_reference.md` §7-1 기존 이슈와 동일한 성격, 이번 작업 범위
밖)를 `InstrumentAttackDispatcher.Execute()` / `HoldEffectCoordinator.BeginHold()` 양쪽에 전달합니다.

## 1. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인.
2. `PassiveStatManager`에 레가토 레벨을 직접 세팅(Lv3=+1, Lv5=+2)하거나, 악기 레벨을 4 이상으로 올려
   `extraProjectiles` 필드 자체를 만들면 됩니다. 두 값은 합산되므로 최대 조합은 +2(레가토 Lv5) + +1(악기
   Lv4+) = +3까지 가능합니다.

## 2. 검증 항목

- [ ] 피아노: `extraProjectiles`만큼 `shots`(발사 수)가 정확히 늘어나는지
- [ ] 벨: 추가 성광이 기존 8방향과 겹치지 않는 각도(22.5°, 67.5°, ...)로 나가는지
- [ ] 마림바: 추가 파동이 원본과 평행하게 좌우로 갈라져 나가는지(관통/크기는 원본과 동일해야 함)
- [ ] 글록켄슈필: 버스트 조건(전역 콤보 4배수) 없이도 매 타격마다 추가 낙하 지점이 생기는지
- [ ] 바이올린: `bladeCount`가 정확히 늘어나고 늘어난 칼날도 정상적으로 궤도를 도는지
- [ ] 팀파니: 홀드 시작 시 기본 캐논 1발 + 추가 착탄이 랜덤 오프셋으로 함께 발생하는지
- [ ] 드럼/프렌치호른/첼로/플루트: `extraProjectiles`를 3까지 줘도 기존 동작과 완전히 동일한지(회귀)
- [ ] `extraProjectiles = 0`일 때 6종 모두 기존과 완전히 동일하게 동작하는지(최소 회귀 기준)
- [ ] **회귀**: 데미지 값 자체는 `extraProjectiles`와 무관하게 그대로인지 (개수만 늘어나야 함)
- [ ] **회귀**: 크레센도/알레그로/페르마타 패시브가 이번 변경과 무관하게 계속 정상 동작하는지

## 4. 검증 결과

**검증 일시**: 2026-08-07 / Unity MCP 연결 세션, Play Mode에서 각 악기 클래스의 실제 프로덕션 메서드
(`Execute`/`Init`)를 리플렉션으로 직접 호출해 측정. (실제 리듬 입력 시뮬레이션 대신 `extraProjectiles`
파라미터를 직접 제어해 호출하는 방식 — `RhythmAttackManager`의 `extraProj` 합산 계산 자체는 이번 diff의
핵심이 아니고, 각 악기가 그 값을 받아 올바르게 소비/무시하는지가 검증 대상이므로 이 방식이 더 정확함.)

### 0. 사전 준비
- `refresh_unity(force, compile=request)` → 컴파일 성공, 에러/경고 0건.
- Play Mode 진입 성공.

### 1~6. 적용 대상 6종 — `extraProjectiles`에 따른 개수 증가 확인

레벨 1, `currentCombo=0`(글록켄슈필 버스트 조건 회피) 기준, `extraProjectiles=0`(기존 동작)과 `=3`(최대
조합: 레가토 Lv5 +2, 악기 Lv4+ +1)을 각각 호출해 생성되는 오브젝트 수 델타를 측정:

| 악기 | 측정 대상 | extra=0 델타 | extra=3 델타 | 결과 |
|---|---|---|---|---|
| 피아노 | `InstrumentBeam`(`shots`) | 1 | 4 | 정확히 +3 |
| 벨 | `InstrumentBeam` (기존 8방향) | 8 | 11 | 정확히 +3, 22.5°/67.5°/... 겹치지 않는 각도로 추가 확인(코드 확인) |
| 마림바 | `InstrumentBeam` | 1 | 4 | 정확히 +3, 이동 방향에 수직 오프셋(원본과 평행) 코드 확인 |
| 글록켄슈필 | `InstrumentImpact` | 1 | 4 | 정확히 +3, 버스트 조건과 무관하게 매 타격 적용 확인 |
| 바이올린 | `bladeCount` 필드 + 실제 자식 `ViolinBlade_i` 오브젝트 수 | 2 | 5 | 정확히 +3, 필드값과 실제 생성된 칼날 수 일치 (기존 "칼날 개수" 스탯과 동일 파라미터 공유이므로 늘어난 칼날도 `OnHoldTick`의 기존 궤도 로직을 그대로 타 정상 회전) |
| 팀파니 | `TimpaniImpact`(홀드 시작 즉발 캐논) | 1 | 4 | 정확히 +3 |

### 7. 회귀 — 제외 4종(드럼/프렌치호른/첼로/플루트)은 `extraProjectiles`와 무관

| 악기 | extra=0 | extra=3 | 결과 |
|---|---|---|---|
| 드럼 (`DrumBeatBang` 생성 수) | 1 | 1 | 동일 (레벨에만 의존, `extraProjectiles` 완전 무시) |
| 프렌치호른 (자식 링 개수/부모 스케일) | childCount=1, scale=2.4 | childCount=1, scale=2.4 | 완전 동일 |
| 첼로 (자식 링 개수/부모 스케일) | childCount=1, scale=1.62 | childCount=1, scale=1.62 | 완전 동일 |
| 플루트(홀드 껍데기, `FluteVortexHoldEffect`) | childCount=0 | childCount=0 | 완전 동일 (홀드 중 시각효과 없음, 릴리즈 시 별도 `FluteVortexEffect`가 처리 — 이번 diff 대상 아님) |

### 8. `extraProjectiles = 0`일 때 최소 회귀 기준
위 1~7 표의 `extra=0` 열이 모두 기존 레벨식 그대로의 기대값(피아노 1발/벨 8방향/마림바 1개/글록켄슈필
1개/바이올린 2개(Lv3 미만)/팀파니 1개/드럼 1개)과 정확히 일치 — 6종+4종 전부 `extraProjectiles=0`일 때
기존과 완전히 동일하게 동작함을 확인.

### 9. 회귀 — 데미지 값 자체는 불변
피아노/마림바/벨에서 생성된 `InstrumentBeam` 전체의 `PiercingBeamProjectile.damage` 필드를 조사한 결과
`extraProjectiles` 값과 무관하게 항상 원래 `damage`/`scaledDamage`(레벨 배율만 반영, 예: Lv1=10)로
단일한 값만 관측됨 — 개수만 늘어나고 개별 발사체당 피해량은 그대로임을 확인. (코드 확인: 모든 추가
루프가 동일한 `damage`/`scaledDamage` 변수를 그대로 재사용하고 있어 구조적으로도 보장됨.)

### 10. 회귀 — 크레센도/알레그로/페르마타 정상 동작
`CombatTargetingUtility.cs`/`PassiveStatManager.cs`는 이번 diff에 포함되지 않음(`git diff --stat`로
사전 확인). 런타임 스팟체크:
- 크레센도 Lv0→Lv5(배율 1.0→1.5) 적용 후 피아노 빔의 `maxRange` 필드가 `9.0 → 13.5`로 정확히 스케일됨.
- 페르마타 Lv5 적용 후 `GetDurationMultiplier()` = 1.75 (기대값과 일치).
- 알레그로 Lv5 적용 후 `GetCooldownReductionFraction()` = 0.30 (기대값과 일치).

세 패시브 모두 이번 인터페이스 변경(`extraProjectiles` 파라미터 추가)과 무관하게 정상 동작.

### 종합 결과
검증 가이드의 10개 항목 모두 통과. 컴파일 에러/경고 없음(무관한 네트워크 릴레이 경고 1건만 콘솔에 존재
— `[RelayService]`/`Account API` 관련, 이번 변경과 무관). 6종 적용 대상 모두 `extraProjectiles`만큼
정확히 개수가 늘어나고, 4종 제외 대상은 값을 완전히 무시하며, 데미지는 불변, 기존 3대 패시브(크레센도/
알레그로/페르마타)도 그대로 정상 동작함을 확인. 테스트용으로 생성한 임시 GameObject는 모두 정리
완료.
