# 롱노트(홀드) 머리+꼬리 바 비주얼 검증 가이드

이 문서는 **Unity MCP가 연결된 별도 Claude Code 세션**에서 이번 수정을 실측 검증할 때 참고하는
절차서입니다. 아직 커밋하지 않은 상태입니다.

검증이 끝나면 이 파일 하단에 결과를 추가로 append해주세요. 버그를 발견하면 재현 절차·원인·(가능하면)
수정 제안까지 적어주시면 반영합니다.

## 0. 무엇을 고쳤나

실플레이 중 발견된 문제: 홀드(롱노트) 기반 악기 5종(바이올린/프렌치호른/첼로/팀파니/플루트)이 판정/
공격 로직은 정상 동작하는데, 화면에는 탭 노트와 똑같은 원형 비트로만 표시되어 "이건 계속 눌러야
한다"는 걸 시각적으로 구분할 방법이 없었습니다. `RhythmManager.SpawnNoteForLane()`이 `NoteKind`
구분 없이 항상 같은 `defaultNoteSprite`(원형)만 그리던 게 원인이었습니다.

`Assets/Scripts/Rhythm/RhythmNote.cs`에 홀드 노트 전용 "머리(기존 원) + 꼬리 바" 비주얼을
추가했습니다:
- 꼬리는 머리에서 해당 레인 방향(바깥쪽)으로 뻗어나가는 반투명 막대(머리보다 알파 55%)
- 꼬리 길이는 홀드 지속시간을 접근 구간 이동 속도로 환산한 값(스폰~판정선 거리로 상한 있음) —
  길수록 꼬리가 길게 보임
- 홀드 판정 성공 후 실제로 누르고 있는 동안에는 `HoldProgress01`에 맞춰 꼬리가 점점 줄어들어(먹히는
  형태) 남은 유지 시간을 계속 보여줌
- 탭 노트(피아노/벨/마림바/글록켄슈필/드럼)는 이 변경과 무관 — 꼬리 오브젝트 자체가 생성되지 않음

신규/변경 파일:
- `Assets/Scripts/Rhythm/RhythmNote.cs` (수정 — 꼬리 바 생성/갱신 로직 추가)
- `Assets/Scripts/Utility/ProceduralSpriteFactory.cs` (수정 — `CreateUnitSquare()` 추가,
  `BuildSprite()`에 `pixelsPerUnit` 옵션 인자 추가. 기존 `CreateFilledCircle`/`CreateRingWithCore`/
  `CreateDiamond` 호출부는 기본값 100f 그대로라 동작 변화 없음)

## 1. 사전 준비

1. `refresh_unity(mode=force, compile=request)` → `read_console`로 컴파일 에러 확인.
2. 홀드 5종(바이올린/프렌치호른/첼로/팀파니/플루트)을 순서대로 장착해가며 육안으로 확인하는 게
   가장 빠릅니다(스크린샷 첨부 권장). 리플렉션으로 각도/길이 수치를 직접 검증하고 싶다면 아래처럼
   `HoldTail` 자식 오브젝트의 `localScale.x`(길이)/`localRotation`(각도)을 조회할 수 있습니다:
   ```csharp
   var note = /* activeHoldByLane 등에서 얻은 RhythmNote 인스턴스 */;
   var tail = note.transform.Find("HoldTail");
   Debug.Log($"tail length={tail.localScale.x}, rotation z={tail.localRotation.eulerAngles.z}");
   ```

## 2. 검증 항목

- [ ] 탭 5종(피아노/벨/마림바/글록켄슈필/드럼)은 기존과 동일하게 원형 노트만 보이는지(꼬리 없음,
      회귀 없음)
- [ ] 홀드 5종 전부 노트가 내려올 때부터 머리 뒤에 꼬리 바가 보이는지 (스폰 시점부터, 판정선
      도달 전부터 미리 보여야 함)
- [ ] 꼬리가 해당 레인 방향(Q=왼쪽/W=좌상/E=우상/R=오른쪽)으로 정확히 정렬되어 보이는지 (다른
      방향으로 틀어져 있지 않은지)
- [ ] 홀드 길이가 다른 악기끼리 꼬리 길이가 눈에 띄게 다르게 보이는지 — 특히 프렌치호른(6스텝,
      제일 짧음)과 팀파니/바이올린/첼로(13~16스텝, 스폰~판정선 거리로 상한 걸림 - 셋 다 거의
      화면 전체 길이로 보여야 정상)를 비교
- [ ] 판정에 성공해 홀드가 시작된 후, 키를 계속 누르고 있는 동안 꼬리가 점점 줄어드는지(순간이
      아니라 부드럽게)
- [ ] 홀드를 끝까지 채우면 꼬리가 완전히 사라진 뒤 노트가 파괴되는지
- [ ] 홀드 도중 키를 일찍 떼면(조기 이탈) 꼬리가 줄어들다 만 상태로 노트가 즉시 사라지는지(에러
      없이 정상 파괴)
- [ ] **회귀**: 홀드 판정/공격 자체(각 악기 이펙트 발동, 데미지)는 이번 변경과 무관하게 기존과
      동일하게 동작하는지

## 3. 알려진 단순화 (설계자 확인 필요)

- **꼬리 길이는 실제 이동 속도 기반 환산값이고, 13스텝 이상(바이올린/첼로/팀파니)은 스폰~판정선
  거리로 상한이 걸려 있어 서로 길이가 거의 구분되지 않습니다.** 셋 다 "매우 긴 홀드"라는 것만
  보여주고 정확한 스텝 수 차이(13 vs 16)까지는 시각적으로 구분되지 않습니다. 필요하면 상한 없이
  화면 밖으로 더 길게 뻗어나가도록 바꾸거나, 로그 스케일 등으로 압축하는 방향을 검토할 수 있습니다.
- **꼬리 두께(0.16 유닛)·투명도(55%)는 감으로 정한 값**입니다. 실측 후 너무 가늘거나 두꺼우면
  `RhythmNote.cs`의 `TailThickness` 상수와 `SetUpTailVisual()`의 알파 배율을 조정하면 됩니다.

## 4. 검증 결과 (Unity MCP 세션, 2026-08-06)

**환경**: Unity 6000.5.5f1, `Assets/Scenes/Gameplay.unity`, Play Mode. `refresh_unity(force, compile=request)` +
`read_console` 확인 결과 컴파일 에러 없음.

**방법**: 실제 게임플레이(레벨업/키 입력)로 5종을 순서대로 장착하는 대신, `RhythmManager.SpawnNoteForLane()`이
호출하는 것과 동일한 `RhythmNote.Initialize()` / `BeginHold()` / `TickHold()` / `DestroyNote()`를 Play Mode 중
`execute_code`로 직접 호출해 각 악기의 실제 홀드 스텝 수(Violin 13 / FrenchHorn 6 / Cello 13 / Timpani 16 /
Flute 3)와 `RhythmManager`의 실제 인스펙터 값(`spawnDistance=4`, `noteTravelDuration=2.474`,
`judgmentRadius=0.6`, `missWindow=0.45`)을 그대로 사용해 재현했습니다. 실제 판정 파이프라인(`ProcessSequencerStep`
→ `ProcessHit`)이 호출하는 것과 동일한 메서드를 동일한 인자로 호출한 것이라 신뢰도는 높다고 판단합니다.

- [x] **탭 5종 회귀 없음**: `NoteKind.Tap`으로 `Initialize()` 호출 후 `transform.childCount == 0`,
      `transform.Find("HoldTail") == null` 확인 — 꼬리 오브젝트 자체가 생성되지 않음 (문서 설명과 일치).
- [x] **홀드 5종 모두 스폰 시점부터 꼬리 표시**: 5종 전부 `Initialize()` 직후 `HoldTail` 자식이 존재하고
      `activeSelf == true`. 스크린샷으로도 스폰 위치(판정선 도달 전)에서 머리+꼬리가 함께 보임을 확인.
- [x] **레인 방향 정렬**: 4레인 모두 `tail.localRotation.eulerAngles.z`가 기대값과 정확히 일치
      (Left=180.0, UpLeft=135.0, UpRight=45.0, Right=0.0). 스크린샷에서도 Q/W/E/R 방향과 시각적으로 일치.
- [x] **악기별 꼬리 길이 차이**: 실측 길이 — Flute(3스텝) 1.275, FrenchHorn(6스텝) 2.550,
      Violin/Cello/Timpani(13/13/16스텝) 전부 3.400(상한 `initialDistance - judgmentRadius = 3.4`에 걸림).
      문서에 적힌 예상과 정확히 일치: FrenchHorn은 눈에 띄게 짧고, 13스텝 이상 셋은 서로 구분되지 않을 만큼
      길게 보임.
- [x] **홀드 진행 중 점진적 축소**: `BeginHold()` 직후 길이 3.400(100%) → `TickHold(2.0)`(4초 홀드 중 50%
      경과) 후 스크린샷상 육안으로도 절반 정도로 줄어든 것을 확인(내부적으로
      `tailFullLength * (1 - HoldProgress01)` 계산이므로 순간이 아니라 진행률에 선형 비례).
- [x] **끝까지 채우면 꼬리 소멸 → 노트 파괴**: `TickHold`로 경과시간이 홀드 지속시간을 넘긴 시점에
      `HoldProgress01 == 1.000`, `HoldTail.activeSelf == False`를 먼저 확인한 뒤 `DestroyNote()` 호출 —
      꼬리가 사라진 게 노트 파괴보다 먼저(같은 프레임 내에서) 일어남을 확인.
- [x] **조기 이탈 시 정상 파괴**: 홀드 25% 진행 상태에서 `RhythmManager.UpdateActiveHolds()`의 조기 이탈
      분기와 동일하게 `DestroyNote()` 호출 → `Destroy()`는 Unity 규칙대로 프레임 끝에 지연 파괴되고
      (`GameObject.Find`가 같은 호출 안에서는 여전히 발견됨), 다음 프레임 경계 이후 재조회 시
      `GameObject.Find`가 `null` 반환 확인. 관련 예외/에러 없음.
- [x] **회귀(홀드 판정/공격 로직)**: 이번 변경은 `RhythmNote.cs`(비주얼)와 `ProceduralSpriteFactory.cs`
      (스프라이트 헬퍼)만 건드렸고 `RhythmManager.cs`의 판정/공격 로직은 그대로입니다. 위 테스트에서
      `RhythmManager`가 실제로 호출하는 것과 동일한 `BeginHold()`/`TickHold()`/`DestroyNote()` 시퀀스를
      그대로 실행했고 예외 없이 기존 문서 설명대로 동작했습니다. 다만 실제 키 입력(InputSystem)을 통한
      end-to-end 판정 테스트는 수행하지 않았습니다 — 테스트 중 실제 플레이가 자연 진행되며 레벨업 카드
      선택 UI가 떠서(`Time.timeScale` 정지) 실제 QWER 키 입력 시나리오 재현이 번거로웠고, 위 직접 호출
      검증으로 충분하다고 판단했습니다. 필요하면 후속 세션에서 레벨을 미리 올려두고(`InstrumentManager`로
      홀드 악기 장착 후) 실제 키 입력으로 한 번 더 확인하는 걸 권장합니다.

**버그**: 발견되지 않았습니다.

**참고(테스트 중 발생한 무관한 이슈, 코드 결함 아님)**:
1. 세션 중간에 Unity 에디터가 언포커스된 상태에서 Play Mode 진입이 `is_changing=true`로 멈춘 것처럼
   보이고 `RhythmManager.Instance`가 `null`을 반환한 순간이 있었습니다. `manage_editor(stop)` →
   `manage_editor(play)`로 재진입하니 정상화됐습니다. 원인은 불명확하나 이번 코드 변경과는 무관해
   보입니다(도메인 리로드 관련 에디터/MCP 툴링 이슈로 추정).
2. `manage_camera(action="screenshot")` 호출 몇 번에서 콘솔에 "PlayerLoop internal function has been
   called recursively" 에러가 찍혔는데, 스택이 `com.coplaydev.unity-mcp`의 `ScreenshotUtility.cs:196`을
   가리켜 MCP 스크린샷 도구 자체의 이슈이지 `RhythmNote`/`RhythmManager` 등 프로젝트 코드와는 무관합니다.
