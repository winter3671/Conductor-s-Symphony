# 🏆 [Portfolio Case Study] 리듬 뱀서 "Conductor's Symphony" QWER 조기입력(Early Hit) 무판정 버그 & 광클 콤보 악용 수정 보고서

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)
> **핵심 성과:** QWER 판정 로직에 "실패(Miss) 입력 구간" 개념을 도입하여, 박자를 무시하고 연타(광클)만 해도 콤보가 유지되던 판정 허점을 근본 차단

---

## 📌 1. 문제 발견 경위 (How the Bug Was Found)

* **제보 내용:** "노트를 일찍 쳤을 때 실패 판정이 잘 안 되는 것 같다. QWER을 광클하면 콤보가 알아서 다 이어지는 느낌이다."
* **재현:** 노트가 화면에 나타나자마자 정박과 무관하게 Q/W/E/R 중 아무 키나 빠르게 연타하면, 실제 박자감과 상관없이 `Great` 판정이 계속 뜨며 콤보가 끊기지 않음.

---

## 🎯 2. 원인 분석 (Root Cause Analysis)

`RhythmManager.CheckHit()`은 키 입력 시점(`currentTime`)과 노트의 정박 시각(`TargetTime`)의 차이(`diff`)를 계산해 판정하는 구조였다.

```csharp
// 수정 전
float diff = Mathf.Abs(currentTime - activeNotes[i].TargetTime);
if (diff < closestDiff && diff <= greatWindow)   // greatWindow = 0.22s
{
    closestDiff = diff;
    targetNote = activeNotes[i];
}
...
if (targetNote != null)
{
    HitRating rating = (closestDiff <= perfectWindow) ? HitRating.Perfect : HitRating.Great;
    ProcessHit(targetNote, rating);
}
```

* **판정 후보 조건 자체가 `diff <= greatWindow`였다.** 즉 `greatWindow`(±0.22초) 밖에서 누른 입력은 애초에 `targetNote`로 선택되지도 않았고, `targetNote == null`이면 아무 것도 실행되지 않았다.
* 다시 말해 **"너무 일찍 누른 입력"은 아무 페널티 없이 그냥 씹혔다.** 노트는 소모되지 않고 그대로 살아있었다.
* 늦게 눌러서 판정을 놓친 경우에는 `RhythmNote.Update()`의 자체 타임아웃(`elapsed > travelDuration + 0.2f`)이 별도로 Miss를 발동시키지만, 이는 **"아무 키도 안 눌렀을 때"**를 대비한 안전장치일 뿐, 조기입력에는 관여하지 않는다.
* 결과적으로 **조기입력은 리스크 제로(Risk-Free)** 였다: 판정 구간 밖에서 눌러도 손해가 전혀 없고, 노트가 판정 구간 안으로 들어올 때까지 계속 다시 시도할 수 있었다. QWER을 연타(광클)하면 매 프레임마다 입력이 발생하므로, 결국 노트가 `greatWindow` 안에 들어오는 순간의 입력이 반드시 걸려 `Great`로 판정되고, 실제 리듬감 없이도 콤보가 자동으로 이어지는 것처럼 보였다.

**핵심 진단: 판정 시스템에 `Perfect`/`Great`만 있고, "조기입력 실패(Miss)" 개념이 없었다.**

---

## 🛠️ 3. 해결 방법 (The Fix)

`greatWindow`보다 넓은 **`missWindow`(±0.45초)** 를 새로 도입해, 입력이 반응하는 전체 구간과 "성공(Perfect/Great) 판정 구간"을 분리했다.

```csharp
// 수정 후 — Assets/Scripts/Rhythm/RhythmManager.cs
[SerializeField] private float missWindow = 0.45f; // 입력이 반응하는 최대 범위

...

if (diff < closestDiff && diff <= missWindow)   // 후보 판정 범위를 missWindow로 확장
{
    closestDiff = diff;
    targetNote = activeNotes[i];
}

...

if (targetNote == null) return;

if (closestDiff <= greatWindow)
{
    HitRating rating = (closestDiff <= perfectWindow) ? HitRating.Perfect : HitRating.Great;
    ProcessHit(targetNote, rating);
}
else
{
    // greatWindow 밖 ~ missWindow 안: 노트를 소모하며 Miss로 확정 (콤보 초기화)
    OnNoteMissed(targetNote);
    targetNote.DestroyNote();
}
```

* **입력 반응 범위(`missWindow`)는 넓히되, 성공 판정 범위(`greatWindow`)는 그대로 유지**했다. 대신 그 사이(0.22초 ~ 0.45초) 구간에서 입력이 들어오면 **노트를 그 자리에서 소모시키고 Miss로 확정**하도록 변경했다.
* 이제 노트가 `missWindow` 안으로 들어온 순간 QWER을 조기에 연타하면, 첫 입력에서 즉시 해당 노트가 **Miss로 소모**되어 콤보가 끊긴다. 이후 같은 노트를 다시 노려서 재시도하는 것 자체가 불가능해진다 (노트가 이미 `Destroy`됨).
* 반대로 `missWindow`보다 훨씬 이전(예: 노트가 막 화면에 스폰된 시점)에 누르면 여전히 아무 판정도 나지 않는다 — 이는 의도된 동작으로, 아직 다가오지도 않은 노트를 미리 씹었다고 벌점을 줄 필요는 없기 때문이다.
* 정박에 맞춰 정확히 `greatWindow` 안에서 누르면 기존과 동일하게 `Perfect`/`Great`가 정상적으로 나온다.

---

## ✅ 4. 검증 (Verification)

* Unity MCP를 통해 `refresh_unity(compile=request)` 실행 후 `read_console`로 컴파일 에러/경고 0건 확인.
* 기존 로직(정확한 타이밍의 `Perfect`/`Great` 판정, 미입력 시 자동 Miss)은 변경하지 않고, "조기입력이 판정 후보에서 아예 제외되던" 그 한 지점만 좁혀서 수정하여 회귀 위험을 최소화했다.

---

## 💎 5. 핵심 교훈 (Key Takeaway)

**"입력이 반응하는 범위"와 "성공으로 인정하는 범위"는 반드시 분리해서 설계해야 한다.**
두 범위를 하나(`greatWindow`)로 합쳐 놓으면, 그 경계 밖의 입력은 "실패"가 아니라 "무반응"이 되어버린다. 무반응은 플레이어 입장에서 페널티가 없기 때문에, 연타로 경계 안쪽을 계속 노크하는 전략이 항상 성공하게 된다. 판정 구간 바깥을 "무시(ignore)"가 아니라 "실패(consume as Miss)"로 처리해야만, 정확한 타이밍에 대한 진짜 리스크가 생기고 광클 전략이 무력화된다.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
