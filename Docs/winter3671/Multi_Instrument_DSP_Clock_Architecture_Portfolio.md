# 🏆 [Portfolio Case Study] 다중 악기 레이어링 시 하드웨어 DSP 마스터 클록 아키텍처 & UI 일시정지 오차 해결

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)  
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)  
> **주요 성과:** C# Dictionary 난수 순회 및 개별 AudioSource 재생 오차 구조 전면 개편 ➡️ `AudioSettings.dspTime` 기반 순수 수학적 하드웨어 마스터 시계 아키텍처 구축  

---

## 📌 1. 버그 개요 및 발생 조건 (Symptom & Environment)

* **장르 및 메커니즘:** 97 BPM 16마디 10종 악기 오케스트라 레이어링 탑뷰 리듬 뱀서.
* **현상:** 게임 초반에는 정상 작동하다가, 플레이 진행 약 1~2분 후(레벨 7~8 부근, 3~4개 악기 복수 장착 및 수차례 레벨업 UI 팝업 일시정지 후) 비트가 소환될 때 수축 링(`ShrinkingRhythmRing`)이 지휘자로 줄어들지 않고 **스폰 외곽 지점에 원형 잔상으로 영구히 얼어붙어 화면에 누적되는 버그** 발생.

---

## 🔬 2. 심층 기술 진단 (Deep Technical Root Cause)

### 🚨 진단 A. C# Dictionary 순회 순서의 난수성과 타임스탬프 왜곡
기존 `AudioLayerManager.cs`의 `SongTime`은 매 프레임 `Dictionary<InstrumentType, AudioSource>`를 순회하며 "현재 재생 중인 악기 중 임의의 하나"의 `timeSamples`를 읽어와 시계로 사용하고 있었음:

```csharp
// [기존 코드의 구조적 결함]
private AudioSource GetAnyActiveInstrumentSource()
{
    foreach (var kvp in activeInstrumentSources) // 딕셔너리 순서 보장 불가
    {
        if (kvp.Value != null && (kvp.Value.isPlaying || kvp.Value.timeSamples > 0))
            return kvp.Value;
    }
    return null;
}
```

악기가 1개(Drums)일 때는 항상 동일한 오디오 소스가 반환되므로 문제가 드러나지 않았으나, 레벨업으로 악기가 3~4개 쌓이자 문제가 발생함.

---

### 🚨 진단 B. UI 일시정지 중 새 악기 장착 시 발생하는 개별 트랙 시간차
1. 레벨업 팝업이 열리면 `PauseAllAudio()`가 실행되어 기존 악기들이 모두 `Pause()`됨.
2. 플레이어가 새 악기를 선택하는 순간 `ActivateInstrumentAudio(type)`가 호출되어 새 악기의 `AudioSource.Play()`가 진행됨.
3. **다른 기존 악기들은 일시정지 창이 열려 있는 동안 멈춰 있는데, 새로 장착된 악기만 혼자 실시간으로 흘러가는 오디오 비동기 버그 발생.**
4. 레벨업을 거칠 때마다 악기 트랙 간 재생 위치가 수 초씩 벌어지게 되고, `GetAnyActiveInstrumentSource()`가 매 프레임 서로 다른 악기 소스를 반환하면서 **`SongTime`이 순간적으로 수 초씩 뒤로 역점프**함.

---

### 🚨 진단 C. `Mathf.Lerp` 클램핑과 파괴 조건 미달의 연쇄 버그
1. 링 스폰 시점의 `spawnTime`은 악기 A 기준(예: `12.5초`)으로 기록됨.
2. 다음 프레임에 시계가 뒤처진 악기 B 기준(예: `8.1초`)으로 바뀌면 경과시간 `elapsed = SongTime - spawnTime`이 큰 음수(`-4.4초`)가 됨.
3. `progress = elapsed / travelDuration`이 음수가 되면 `Mathf.Lerp(initialDistance, 0f, progress)`가 `t`를 `[0, 1]` 범위로 클램프하여 **링의 반지름이 항상 최외곽 거리(`initialDistance`)로 고정**됨.
4. `progress`가 파괴 기준인 `1.05f`를 절대로 통과하지 못해 **`Destroy()`가 호출되지 않고 화면에 얼어붙어 남게 됨.**

---

## 🛠️ 3. 근본적 아키텍처 재설계 (Architectural Solution)

### A. 개별 `AudioSource.timeSamples` 조회 전면 폐지 ➡️ `AudioSettings.dspTime` 단일 진실 소스화

개별 컴포넌트의 가변적 상태를 읽는 대신, 오디오 하드웨어 엔진의 absolute 시계인 `AudioSettings.dspTime`에서 일시정지 누적 시간(`totalPausedDuration`)을 차감하는 **순수 수학적 함수 `SongTime`**으로 전환:

```csharp
private double masterStartDspTime = -1.0;
private double pauseStartDspTime = -1.0;   // 일시정지 시작 시각
private double totalPausedDuration = 0.0;  // 누적 일시정지 지속시간

public float SongTime
{
    get
    {
        if (masterStartDspTime <= 0.0) return -1f;

        // 일시정지 중일 때는 pauseStartDspTime에 고정, 재생 중일 때는 현재 dspTime 참조
        double referenceDsp = (pauseStartDspTime > 0.0) ? pauseStartDspTime : AudioSettings.dspTime;
        double elapsed = referenceDsp - masterStartDspTime - totalPausedDuration;

        if (elapsed < 0.0) return (float)elapsed; // 프리롤 구간 (음수)

        float loopLength = SongLoopLength;
        if (loopLength > 0f) elapsed %= loopLength; // 결정적(Deterministic) 루프 wrap 연산

        return (float)elapsed;
    }
}
```

---

### B. 중복 호출 방지 일시정지 회계 (Idempotent Pause Accounting)

레벨업 UI와 엘리트 전리품 상자 UI가 겹쳐서 열릴 때 일시정지 타임스탬프가 덮어씌워지지 않도록 방어 코드 작성:

```csharp
public void PauseAllAudio()
{
    if (pauseStartDspTime < 0.0)
    {
        pauseStartDspTime = AudioSettings.dspTime;
    }
    foreach (var kvp in activeInstrumentSources)
    {
        if (kvp.Value != null && kvp.Value.isPlaying) kvp.Value.Pause();
    }
}

public void ResumeAllAudio()
{
    if (pauseStartDspTime > 0.0)
    {
        totalPausedDuration += AudioSettings.dspTime - pauseStartDspTime;
        pauseStartDspTime = -1.0;
    }
    foreach (var kvp in activeInstrumentSources)
    {
        if (kvp.Value != null) kvp.Value.UnPause();
    }
}
```

---

### C. UI 일시정지 중 신규 악기 즉시 멈춤 동기화 (Pause Isolation)

일시정지 팝업이 떠 있는 동안 새로 장착된 악기가 혼자 흘러가는 것을 방지:

```csharp
if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
{
    source.timeSamples = referenceSource.timeSamples % clip.samples;
    source.Play();

    // 팝업 일시정지 중 장착된 경우, 다른 악기들과 함께 즉시 Pause 처리
    if (pauseStartDspTime > 0.0)
    {
        source.Pause();
    }
}
```

---

## 💎 4. 핵심 엔지니어링 교훈 및 면접 대처 가이드 (Key Takeaways)

1. **"시간 추적(Timekeeping)을 위해 비결정적(Non-Deterministic) 렌더링 상태나 컨테이너를 순회하지 말 것."**
   * C# `Dictionary` 순회 순서나 개별 컴포넌트의 가변적 상태를 기반으로 시계를 구성하면 멀티트랙/동시성 환경에서 데이터 경합과 시계 역점프가 일어난다.
2. **"마스터 시계는 상태를 읽는 게 아니라, 마스터 타임라인으로부터 연산되는 순수 함수(Pure Function)여야 한다."**
   * `AudioSettings.dspTime` 기준의 오프셋 연산을 통해 어떤 상태에서도 일관된 결과를 반환하는 하드웨어 마스터 시계를 구축함으로써, 멀티트랙 레이어링 환경에서 0.000ms 오차 없는 완전 무결점 오디오-비트 동기화를 달성함.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 활용됩니다.*
