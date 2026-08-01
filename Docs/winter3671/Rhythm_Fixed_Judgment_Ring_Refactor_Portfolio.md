# 🏆 [Portfolio Case Study] 리듬 뱀서 "Conductor's Symphony" 고정 판정 링(Perfect Judgment Ring) 도입 및 박자 수축 링 제거 보고서

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)
> **핵심 성과:** 노트의 논리적 판정 시각과 시각적 도착 지점을 분리 설계하여, 판정 타이밍 코드를 한 줄도 건드리지 않고 "캐릭터 주변 고정 원 = Perfect 판정선"이라는 새로운 시각 규약을 도입

---

## 📌 1. 문제 발견 경위 (Background)

* **요청 내용:** "판정선이 캐릭터 한가운데로 되어있는데, 캐릭터 주변에 고정된 흰색 원을 Perfect 판정선으로 삼고 싶다. 노트가 그 원에 정확히 닿을 때 QWER을 치면 Perfect가 나야 하고, 원을 지나 미스 판정 지점까지 내려가면 노트가 사라져야 한다. 기존에 박자마다 줄어드는 흰색 원은 삭제해달라."
* **기존 상태:** 노트는 4방향 외곽에서 스폰되어 캐릭터 정중앙(`distance = 0`)을 향해 직선 이동하도록 설계되어 있었고, 그 자리가 곧 논리적 판정 지점이었다. 화면에는 대신 매 시퀀서 스텝마다 새로 생성되어 `spawnDistance(4.0f)`에서 `0`까지 박자에 맞춰 계속 수축·소멸하는 `ShrinkingRhythmRing`이 여러 겹 표시되고 있어, "정확히 어디에 닿아야 Perfect인지"를 짚어낼 고정된 기준선이 없었다.
* **부가 문제:** 놓친 노트는 캐릭터 중앙에 도달한 뒤 하드코딩된 `0.2초`(`RhythmManager`의 실제 `missWindow=0.45f`와 무관한 매직넘버) 뒤에야 사라져, "판정선을 지나쳐 계속 내려가다 미스 판정 지점에서 사라진다"는 시각 피드백이 없었다.

---

## 🎯 2. 기존 구조 분석 (Existing Architecture)

`RhythmManager.CheckHit()`의 판정은 노트의 실제 화면 위치를 전혀 참조하지 않는다. 순수하게 DSP 오디오 클럭 기반 `SongTime`과 노트의 `TargetTime`의 차이만으로 `Perfect`/`Great`/`Miss`를 가른다.

```csharp
// RhythmManager.CheckHit() — 변경하지 않음
float diff = Mathf.Abs(currentTime - activeNotes[i].TargetTime);
if (diff < closestDiff && diff <= missWindow) { ... }
```

그리고 `TargetTime`은 스폰 시 `songTimeNow + noteTravelDuration`으로 고정되며, `RhythmNote`는 이를 역산해 `spawnTime = targetTime - travelDuration`을 구한다. 따라서:

```
progress = elapsed / travelDuration
```

이 정확히 `1`이 되는 순간은, 곧 `currentTime == TargetTime`이 되는 순간과 수학적으로 동일하다. 즉 **"progress=1일 때 노트를 화면 어디에 그릴 것인가"는 순수한 렌더링 문제이며, 판정 타이밍 로직과는 완전히 분리되어 있다.** 기존 코드는 이 도착 지점을 `distance = 0`(캐릭터 중앙)으로 그렸을 뿐이다.

```csharp
// 수정 전 — RhythmNote.UpdatePosition()
private void UpdatePosition(float progress)
{
    float currentDistance = Mathf.Lerp(initialDistance, 0f, progress);
    transform.position = centerPos + laneDirection * currentDistance;
}
```

`Mathf.Lerp`는 `progress`를 0~1로 clamp하기 때문에, 판정을 놓친 뒤(`progress > 1`)에도 노트는 캐릭터 중앙(`distance=0`)에 그대로 멈춰 있었고, 별도의 타임아웃(`elapsed > travelDuration + 0.2f`)이 지나야 사라졌다. "판정선을 지나쳐 내려가다 사라진다"는 움직임 자체가 존재하지 않았던 것이다.

---

## 🛠️ 3. 해결 방법 (The Fix)

### 3-1. 도착 지점을 캐릭터 중앙 → 고정 반경으로 이동

`RhythmManager`에 `judgmentRadius`를 새로 노출하고, `RhythmNote.UpdatePosition()`을 두 구간으로 나눴다.

```csharp
// 수정 후 — RhythmNote.UpdatePosition()
private void UpdatePosition(float progress)
{
    float currentDistance;
    if (progress <= 1f)
    {
        // 접근 단계: 스폰 지점 → 고정 판정 링까지 이동
        currentDistance = Mathf.Lerp(initialDistance, judgmentRadius, progress);
    }
    else
    {
        // 미스 단계: 판정 링을 지나 캐릭터 중심 쪽으로 계속 하강,
        // missWindow가 끝나는 시점에 정확히 distance=0에 도달
        float lateProgress = Mathf.Clamp01((progress - 1f) * travelDuration / missWindow);
        currentDistance = Mathf.Lerp(judgmentRadius, 0f, lateProgress);
    }
    transform.position = centerPos + laneDirection * currentDistance;
}
```

동시에 자동 미스 타임아웃도 매직넘버 `0.2f` 대신 실제 `missWindow`를 사용하도록 정정했다.

```csharp
// 수정 전: if (elapsed > travelDuration + 0.2f)
// 수정 후:
if (elapsed > travelDuration + missWindow)
{
    RhythmManager.Instance?.OnNoteMissed(this);
    Destroy(gameObject);
}
```

이 두 변경으로 "노트가 고정 원(judgmentRadius)에 닿는 순간 = Perfect 판정 시각"이 자동으로 성립하고(판정 코드는 무변경), "놓친 노트는 판정 링을 지나 캐릭터 쪽으로 계속 하강하다 미스가 확정되는 정확히 그 순간 사라진다"가 동시에 구현됐다.

### 3-2. 박자 수축 링(`ShrinkingRhythmRing`) 제거, 고정 링(`JudgmentRing`) 신설

* `Assets/Scripts/Rhythm/ShrinkingRhythmRing.cs` 전체 삭제. `RhythmManager.ProcessSequencerStep()`에서 매 스텝 `SpawnShrinkingRingForStep()`을 호출하던 코드도 함께 제거.
* 신규 `Assets/Scripts/Rhythm/JudgmentRing.cs`: 기존 링과 동일한 `LineRenderer` 기반 원 렌더링 패턴을 재사용하되, 반경·색상·알파를 고정값으로 유지하고 수축·페이드 로직을 제거했다. `RhythmManager.Start()`에서 `targetTransform` 확정 직후 **단 1회만** 생성해 캐릭터를 계속 따라다니게 했다(기존처럼 매 스텝 재생성되지 않음).

---

## ✅ 4. 검증 (Verification)

* Unity MCP `refresh_unity(compile=request, mode=force)` 실행 후 `read_console`로 컴파일 에러/경고 0건 확인.
* Play 모드 진입 후 `find_gameobjects`로 확인: `JudgmentRing` 오브젝트가 정확히 1개만 존재하고(매 스텝 재생성되지 않음), 기존 `RhythmRing_*`(수축 링) 오브젝트는 0개.
* `execute_code`로 `RhythmManager`의 private `SpawnNoteForLane`/`CheckHit`을 리플렉션 호출해 실측:
  * 접근 단계 노트의 거리(distance-to-target)가 시간에 따라 `spawnDistance(4.0) → judgmentRadius(1.5)`로 단조 감소함을 확인.
  * 입력 없이 방치한 노트가 `judgmentRadius`보다 안쪽(예: `dist=1.071`)까지 하강한 뒤, `missWindow` 만료 시점에 실제로 `Destroy`되어 활성 노트 목록에서 사라짐을 확인.
  * 노트 스폰 후 정확히 `travelDuration(2.474s)` 경과 시점(=노트가 고정 링 위에 있는 순간)에 해당 레인 `CheckHit()`을 호출하자 `OnHitSuccessEvent`가 `HitRating.Perfect`로 발화됨을 확인.
* 런타임 예외(NullReference 등) 0건.

---

## 💎 5. 핵심 교훈 (Key Takeaway)

**판정 로직(언제 성공인가)과 시각 표현(어디에 그릴 것인가)을 처음부터 분리해 설계해두면, 시각적 요구사항이 완전히 바뀌어도 타이밍 로직은 단 한 줄도 건드릴 필요가 없다.**
이번 작업은 "판정선을 캐릭터 중앙에서 고정 원으로 옮긴다"는, 얼핏 판정 시스템 전체를 손봐야 할 것 같은 요청이었다. 그러나 `TargetTime`(판정) ↔ `progress`(렌더링)이 애초에 별도 축으로 설계되어 있었기 때문에, 실제 변경은 `UpdatePosition()`의 도착 지점 하나를 옮기는 것으로 끝났다. `Rhythm_Early_Hit_Miss_Judgment_Fix_Portfolio.md`에서 확립한 "입력 반응 범위와 성공 판정 범위를 분리하라"는 원칙과 마찬가지로, **관심사를 미리 분리해 둔 설계는 이후의 큰 시각적 변경 요청을 국소적인 수정으로 흡수한다.**

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
