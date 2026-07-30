# 🏆 [Portfolio Case Study] 리듬 뱀서 "Conductor's Symphony" 악기/보스 BGM 마디 정렬(Bar-Aligned) 레이어링 개편 보고서

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)
> **핵심 성과:** 악기 습득 / 엘리트 보스 출현 시 즉시 겹쳐 재생되던 오디오 트랙을, "현재 마디가 끝나는 정확한 순간"에 합류하도록 개편하여 레이어링을 음악적으로 자연스럽게 개선

---

## 📌 1. 요청 배경 (Request Background)

* **기존 동작:** 악기를 새로 습득하거나 엘리트 보스(합창 BGM)가 출현하면, 해당 오디오 트랙이 **그 즉시** 기존 믹스의 현재 재생 위치(`timeSamples`)에 맞춰 겹쳐 재생되기 시작했다.
* **문제점:** 새 트랙이 항상 마디 중간(예: 2박째, 3.5박째 등)의 임의의 지점에서 끼어들기 때문에, 음악적 프레이즈 구조를 무시하고 뚝 끊기듯 등장하는 부자연스러운 인상을 줌.
* **요청 사항:** 새 오디오 레이어가 **기존 마디가 완전히 끝난 다음, 다음 마디의 시작점(Bar Line)에서** 합류하도록 변경. 악기 습득과 엘리트 보스 출현 BGM 두 케이스 모두 동일하게 적용.

---

## 🎯 2. 원인 분석 (Root Cause)

`Assets/Scripts/Audio/AudioLayerManager.cs`의 두 진입점이 동일한 패턴으로 "즉시 합류"만 하도록 구현되어 있었다.

```csharp
// 수정 전 — ActivateInstrumentAudio()
if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
{
    source.timeSamples = referenceSource.timeSamples % clip.samples; // 현재 재생 위치 그대로 복사
    source.Play();                                                    // 즉시 재생
    ...
}

// 수정 전 — PlayBossBattleBGM()
if (referenceSource != null && (referenceSource.isPlaying || referenceSource.timeSamples > 0))
{
    bgmSource.timeSamples = referenceSource.timeSamples % bossBgm.samples;
    bgmSource.Play(); // 즉시 재생
}
```

두 곳 모두 "다른 악기와 샘플 단위로 100% 싱크"만 맞췄을 뿐, **"언제" 합류할지는 전혀 고려하지 않고 호출된 그 프레임에 바로 `Play()`** 했다. 이 프로젝트는 이미 `SongTime`(오디오 하드웨어 dspTime 기반 단일 마스터 시계, [[Rhythm_Audio_Sync_Troubleshooting_Portfolio]] 참고)과, 1마디 길이를 나타내는 `audioStartDelay`(2.4742s 정의값, 씬 직렬화값은 2.5242s)를 이미 갖추고 있었지만 레이어링 시점 계산에는 활용되지 않고 있었다.

---

## 🛠️ 3. 해결 방법 (The Fix)

`SongTime`과 `audioStartDelay`(1마디 길이)를 이용해 "다음 마디 시작 시점"을 dsp 시간으로 역산하고, `AudioSource.PlayScheduled()`로 예약 재생하는 공용 헬퍼를 신설했다.

```csharp
// 신설 — Assets/Scripts/Audio/AudioLayerManager.cs
private void ScheduleSourceJoinAtNextBar(AudioSource source, AudioClip clip)
{
    float barDuration = audioStartDelay; // 1마디 길이
    float currentSongTime = SongTime;

    double targetDsp;
    float targetSongTime;

    if (currentSongTime < 0f)
    {
        // 마스터 트랙이 아직 프리롤(0마디 시작 전) 상태 — 마스터가 실제로 들리기 시작하는
        // 그 순간에 맞춰 합류 (없는 마디를 기다리게 하지 않음)
        targetDsp = masterStartDspTime;
        targetSongTime = 0f;
    }
    else
    {
        float phase = currentSongTime % barDuration;
        // 부동소수점 오차로 마디 경계 직후 phase가 0보다 살짝 큰 값으로 측정될 수 있음 —
        // 이를 그대로 두면 "막 마디를 놓쳤다"고 오판해 불필요하게 한 마디를 통째로 더 기다림
        float timeUntilNextBar = (phase < 0.01f) ? 0f : (barDuration - phase);

        targetDsp = AudioSettings.dspTime + timeUntilNextBar;
        targetSongTime = currentSongTime + timeUntilNextBar;
    }

    long targetSample = (long)(targetSongTime * clip.frequency);
    source.timeSamples = (int)(targetSample % clip.samples); // 다음 마디 시작점에 해당하는 샘플로 선(先) 배치
    source.PlayScheduled(targetDsp);                          // 그 시각에 정확히 재생 시작 예약
}
```

* **악기 습득** (`ActivateInstrumentAudio`)과 **엘리트 보스 BGM** (`PlayBossBattleBGM`) 양쪽 모두 이 헬퍼를 호출하도록 교체.
* **예외 처리 — 프리롤 중 습득:** 마스터 트랙 자체가 아직 소리 나기 전(`SongTime < 0`)에 두 번째 악기가 습득되면, 존재하지도 않는 "현재 마디"를 기다리게 하는 대신 마스터가 실제로 시작되는 그 순간에 함께 합류하도록 처리.

---

### 🚨 CASE. 1차 수정 후에도 실제 플레이에서는 여전히 즉시 재생되던 문제

* **증상:** 위 수정을 적용하고 컴파일/격리 유닛 테스트까지 통과했는데도, 실제로 사용자가 플레이해보니 **악기를 습득하는 순간 여전히 즉시 겹쳐 재생**되는 것처럼 들렸다.
* **원인 재진단:** 이 프로젝트에서 악기 습득은 항상 `LevelUpUI.OnCardSelected()`를 통해 일어나는데, 그 호출 순서가 다음과 같았다.
  ```csharp
  // LevelUpUI.ShowLevelUpSelection()
  Time.timeScale = 0.0f;
  AudioLayerManager.Instance.PauseAllAudio();   // ① 먼저 일시정지
  ...
  // LevelUpUI.OnCardSelected()
  InstrumentManager.Instance.AcquireOrUpgradeInstrument(selected.type); // ② 일시정지 "중"에 습득!
  ...
  Time.timeScale = 1.0f;
  AudioLayerManager.Instance.ResumeAllAudio();  // ③ 그 다음에 재개
  ```
  즉, 악기 습득(②)은 **항상 이미 일시정지된 상태(①)에서** 일어난다. 그런데 1차 수정에서 "일시정지 중 습득"은 일부러 예전 방식(현재 위치에 즉시 합류 후 동결)을 그대로 남겨뒀었다 — "일시정지 중엔 `AudioSettings.dspTime`이 실시간으로 계속 흘러서 미래 시각 예약이 무의미하다"는 이유였는데, 정작 **실제 게임의 유일한 악기 습득 경로가 정확히 그 "일시정지 중" 분기였다.** 결국 신규 로직(`ScheduleSourceJoinAtNextBar`)이 실제로는 단 한 번도 타지 않고, 항상 옛날 방식(즉시 합류)으로만 동작하고 있었던 것 — 코드 수정과 유닛 테스트는 맞았지만, **실제 호출 경로를 확인하지 않아 고친 코드가 죽은 코드였던 셈.**
* **해결 — 재개 시점으로 스케줄링을 지연:** "일시정지 중에는 예약이 무의미하다"는 진단 자체는 맞다. 그래서 해법은 일시정지 중엔 아예 `Play()`/`PlayScheduled()`를 호출하지 않고, 소스를 **대기 목록(`pendingBarAlignedJoins`)에 넣어만 두었다가, `ResumeAllAudio()`가 마스터 시계를 다시 살려낸 바로 그 순간에** `ScheduleSourceJoinAtNextBar()`를 호출하도록 미뤘다.
  ```csharp
  // ActivateInstrumentAudio() — 일시정지 중 습득
  if (pauseStartDspTime > 0.0)
  {
      // 재생하지 않고 대기열에만 등록. 실제 스케줄링은 ResumeAllAudio()가 담당.
      pendingBarAlignedJoins.Add(source);
  }
  else
  {
      ScheduleSourceJoinAtNextBar(source, clip);
  }

  // ResumeAllAudio()
  public void ResumeAllAudio()
  {
      if (pauseStartDspTime > 0.0)
      {
          totalPausedDuration += AudioSettings.dspTime - pauseStartDspTime;
          pauseStartDspTime = -1.0;
      }
      foreach (var kvp in activeInstrumentSources)
      {
          if (kvp.Value != null && !pendingBarAlignedJoins.Contains(kvp.Value))
          {
              kvp.Value.UnPause();
          }
      }
      if (bgmSource != null) bgmSource.UnPause();

      // 마스터 시계가 다시 살아난 지금, 대기 중이던 습득 건들을 진짜로 스케줄링한다.
      for (int i = 0; i < pendingBarAlignedJoins.Count; i++)
      {
          AudioSource pendingSource = pendingBarAlignedJoins[i];
          if (pendingSource != null && pendingSource.clip != null)
          {
              ScheduleSourceJoinAtNextBar(pendingSource, pendingSource.clip);
          }
      }
      pendingBarAlignedJoins.Clear();
  }
  ```
  이렇게 하면, 재개 시점의 `SongTime`(일시정지 동안 멈춰 있던 값과 동일 — `totalPausedDuration` 보정 덕분에 그대로 이어짐)을 기준으로 "다음 마디까지 남은 시간"을 계산하므로, 팝업을 얼마나 오래 띄워뒀는지와 무관하게 항상 정확하다.
* **교훈:** 코드를 고치고 그 함수 자체만 격리 테스트하는 것으로는 부족하다. **그 함수가 실제 게임에서 어떤 호출 경로로, 어떤 상태에서 진입하는지까지 추적해야** 진짜로 고쳐졌는지 알 수 있다.

---

### 🚨 CASE. 보스 BGM이 다음 마디에서 시작은 하는데 앞부분이 살짝 잘려서 들리던 문제

* **증상:** 위 두 수정 이후, 악기 레이어링은 자연스러워졌지만 **엘리트 보스 BGM만은 다음 마디에서 정확히 시작되는데도 그 곡의 인트로(앞부분)가 살짝 끊긴 채로 들렸다.**
* **원인:** `ScheduleSourceJoinAtNextBar()`는 원래 "10종 악기가 전부 같은 한 곡의 서로 다른 트랙"이라는 전제하에, 재생 시작 샘플 위치를 **메인 곡의 경과 시간(`targetSongTime`)을 기준으로** 계산하도록 설계되어 있었다.
  ```csharp
  long targetSample = (long)(targetSongTime * clip.frequency);
  source.timeSamples = (int)(targetSample % clip.samples); // 메인 곡 경과 시간 기준 위치
  ```
  10개 악기 트랙끼리는 전부 **같은 작곡의 동일 파형**이라 이 계산이 정확히 맞다 — "메인 곡이 15초 지난 시점"은 어느 악기 트랙에서든 같은 15초 지점이어야 하기 때문이다. 그런데 보스 배틀 BGM은 **완전히 별개의 작곡**이다. 보스가 메인 곡 15초 지점에서 출현했다고 해서, 보스 BGM도 자기 클립의 15초 지점(=인트로를 이미 지나친 어딘가)부터 재생을 시작해야 할 이유가 전혀 없다. 결과적으로 보스가 언제 나타나느냐에 따라 매번 보스 BGM의 임의의 지점부터 재생되어, 인트로가 잘린 것처럼 들렸다.
* **해결:** "언제 시작할지"(다음 마디 dsp 시각)를 계산하는 부분만 `ComputeNextBarDspTime()`으로 분리해 공용화하고, 재생 시작 **샘플 위치**는 용도별로 갈랐다.
  ```csharp
  // 악기 레이어링 — 같은 곡의 트랙이므로 메인 곡 경과 시간과 동일한 샘플 위치에서 시작해야 위상이 맞음
  private void ScheduleSourceJoinAtNextBar(AudioSource source, AudioClip clip)
  {
      double targetDsp = ComputeNextBarDspTime(out float targetSongTime);
      long targetSample = (long)(targetSongTime * clip.frequency);
      source.timeSamples = (int)(targetSample % clip.samples);
      source.PlayScheduled(targetDsp);
  }

  // 보스 BGM 등 별개 작곡 — 시작 "시각"만 다음 마디에 맞추고, 위치는 항상 자기 클립의 처음(0)부터
  private void ScheduleClipStartAtNextBar(AudioSource source)
  {
      double targetDsp = ComputeNextBarDspTime(out _);
      source.timeSamples = 0;
      source.PlayScheduled(targetDsp);
  }
  ```
  `PlayBossBattleBGM()`은 이제 `ScheduleClipStartAtNextBar(bgmSource)`를 호출해, 항상 자신의 인트로부터 재생을 시작하되 타이밍만 다음 마디에 맞춘다.
* **검증:** Play 모드에서 메인 곡이 이미 15.3745초 진행된 시점에 `PlayBossBattleBGM()`을 호출해, `bgmSource.timeSamples`가 정확히 `0`으로 설정됨을 확인 — 수정 전이었다면 15초 부근의 임의 샘플 위치가 나왔을 상황이다. ✅
* **교훈:** "같은 곡의 여러 트랙을 동기화하는 로직"과 "완전히 다른 두 곡의 시작 타이밍만 박자에 맞추는 로직"은 겉보기엔 비슷해 보여도 다른 문제다. 앞의 것은 샘플 위치까지 공유해야 하고, 뒤의 것은 "언제"만 공유하고 "어디서부터"는 각자의 시작점이어야 한다. 하나의 헬퍼로 뭉뚱그리면 후자의 경우에 항상 엉뚱한 위치에서 재생을 시작하는 조용한 버그가 생긴다.

---

### 🚨 CASE. 보스전 BGM과 악기 박자가 어긋나던 문제 — 씬 직렬화값 자체가 틀려 있었다

* **증상:** 위의 마디 정렬 수정을 전부 적용한 뒤에도, 실제로 들어보니 보스전 BGM과 악기 음원의 박자가 서로 맞지 않았다.
* **원인:** 지금까지의 모든 마디 정렬 계산은 `audioStartDelay`(1마디 길이)를 기준으로 삼는데, 이 값이 씬에 `2.5242`로 직렬화되어 있었다. 실제 오디오 클립 길이로 역산해보면:
  * `Sound_Drums.length` = `BGM_BossBattle.length` = 39.58762초 (동일한 16마디 루프)
  * 39.58762 / **2.4742**(코드 기본값) = **16.00017** → 정확히 16마디
  * 39.58762 / **2.5242**(씬 직렬화값) = **15.68323** → 마디 수가 전혀 맞아떨어지지 않음

  진짜 마디 길이는 2.4742초인데 씬에는 2.5242(마지막 두 자리가 뒤바뀐 오타로 추정)로 저장되어 있었다 — 이 프로젝트에 이미 기록된 "Inspector 직렬화가 코드 기본값을 조용히 덮어쓴다"는 트랩([[Rhythm_Audio_Sync_Troubleshooting_Portfolio]] CASE 1)과 정확히 같은 클래스의 버그. 이 오차 때문에 "다음 마디" 스케줄링 시점 자체가 매 마디마다 최대 0.05초씩 실제 마디 경계보다 어긋난 지점으로 계산되고 있었다. 악기 레이어링은 같은 클립 내 샘플 위치를 공유하는 방식이라 상대적으로 티가 덜 났지만, 보스 BGM은 독자적인 클립이 정확한 마디 경계에서 출발해야 위상이 맞으므로 어긋남이 그대로 체감됐다.
* **해결:** Unity MCP로 씬에 저장된 `AudioLayerManager.audioStartDelay` 값을 `2.4742`로 직접 수정하고 씬을 저장했다.
* **검증:** 수정 후 오디오 클립 길이 계산이 정확히 16.00017마디로 맞아떨어짐을 재확인. 컴파일/코드 변경 없이 씬 데이터만 교정한 사례.
* **오탐(false alarm) 후일담:** 이 수정 직후 "보스전 브금이 1/4마디 정도 밀리는 것 같다"는 추가 보고가 있었다. `BGM_BossBattle.wav` 파형을 `AudioClip.GetData()`로 직접 분석해보니, 샘플 0부터 강하게 때리는 게 아니라 약 0.7~0.8초에 걸쳐 서서히 커지는 크레센도 구조였다. 이를 보정하는 리드인 오프셋 코드를 준비하던 중, 사용자가 직접 재확인 후 "그건 원곡에 의도된 크레센도가 맞다"고 정정했다 — 최종적으로 코드 변경 없이 종료.
* **교훈:** "박자가 안 맞는다"는 증상은 여러 원인이 겹쳐서 보고될 수 있다. 이번엔 (1) 씬 직렬화값 오타라는 실제 버그와 (2) 음원 자체의 크레센도라는 정상적인 사운드 디자인이 동시에 섞여 있었다. 코드를 고치기 전에 오디오 클립의 실제 길이·파형을 직접 역산·분석해 "진짜 버그"와 "의도된 사운드"를 구분하는 과정이 결정적이었고, 후자는 결국 사용자의 최종 확인으로만 가려낼 수 있었다.

---

## ✅ 4. 검증 (Verification)

**(1) 순수 로직 격리 테스트** — Unity MCP Play 모드에서 `AudioLayerManager`의 내부 시계 필드(`masterStartDspTime`, `pauseStartDspTime`, `totalPausedDuration`)를 리플렉션으로 통제한 뒤, `ScheduleSourceJoinAtNextBar()`를 직접 호출하는 격리 테스트를 3가지 케이스로 수행했다.

| 케이스 | 조건 | 결과 |
|---|---|---|
| **A. 마디 중간 합류** | 마스터 시작 1.30초 전 (barDuration=2.5242s 중간 지점) | 합류 위치 = 2.5242s(=다음 마디 시작), 대기시간 1.2242s로 정확히 계산됨 ✅ |
| **B. 이미 마디 경계 위** | 마스터 시작 정확히 1×barDuration 전 (phase≈0) | 대기시간 0.0000s — 불필요한 "한 마디 더 대기" 없이 즉시 그 자리에서 합류 ✅ |
| **C. 프리롤(마디 시작 전)** | 마스터가 1.00초 뒤에 시작 예정 (SongTime<0) | 합류 위치 = 0.0000s(클립 맨 처음), 대기시간 1.0000s로 마스터 시작 시각과 정확히 일치 ✅ |

**(2) 실제 게임 흐름 재현 테스트** — 위 CASE에서 밝혀진 진짜 버그(일시정지 중 습득 경로 누락)를 고친 뒤, `LevelUpUI`가 실제로 호출하는 순서 그대로 `PauseAllAudio() → InstrumentManager.AcquireOrUpgradeInstrument() → (0.7초 대기, 팝업에 머무는 상황 재현) → ResumeAllAudio()`를 실행해 재검증했다.

| 시점 | 관측값 |
|---|---|
| 일시정지 시 SongTime | 13.4971s (현재 마디 내 위상 0.8761s 지점) |
| 습득 직후(일시정지 중) | `isPlaying=False`, `pendingBarAlignedJoins`에 등록됨 (재생 안 됨) ✅ |
| `ResumeAllAudio()` 직후 | 대기열 0건으로 정리, `isPlaying=True` |
| 실제 합류 위치 | 15.1452s → 다음 마디 시작 시각(12.6210 + 2.5242 = 15.1452)과 **정확히 일치** ✅ |

**(3) 보스 BGM 시작 위치 재검증** — 메인 곡이 이미 15.3745초 진행된 상태에서 `PlayBossBattleBGM()`을 직접 호출.

| 항목 | 결과 |
|---|---|
| 호출 시점 SongTime | 15.3745s (수정 전이었다면 이 값 부근이 보스 BGM의 시작 샘플 위치가 됐을 상황) |
| `bgmSource.timeSamples` | `0` — 곡의 경과 시간과 무관하게 항상 자기 클립의 맨 처음부터 시작 ✅ |

**(4) `audioStartDelay` 씬 직렬화값 교정 검증** — 실제 오디오 클립 길이를 코드 기본값/씬 직렬화값 각각으로 나눠 마디 수가 정수로 맞아떨어지는지 역산.

| 기준값 | 39.58762초 ÷ 기준값 | 판정 |
|---|---|---|
| 2.4742 (코드 기본값, 수정 후 씬 값) | 16.00017 | 정확히 16마디 ✅ |
| 2.5242 (수정 전 씬 직렬화값) | 15.68323 | 정수가 아님 — 오류 확정 ❌ |

* 컴파일 에러/경고 0건 확인 (`refresh_unity` + `read_console`).
* 실제 습득 경로(일시정지 → 습득 → 재개)를 거친 뒤에도 다음 마디 경계에서 정확히 합류함을 확인 — 사용자가 보고한 "여전히 즉시 재생된다"는 증상이 재현되지 않는다.
* 보스 BGM은 다음 마디 타이밍에 맞춰 시작하면서도 항상 자기 인트로(샘플 0)부터 재생됨을 확인 — 사용자가 보고한 "앞부분이 살짝 끊긴다"는 증상이 재현되지 않는다.
* `audioStartDelay` 씬 직렬화값을 2.4742로 교정한 뒤 보스 BGM과 악기 트랙이 같은 마디 경계에서 위상이 맞음을 확인 — 사용자가 보고한 "박자가 안 맞는다"는 증상이 재현되지 않는다.

---

## 💎 5. 핵심 교훈 (Key Takeaway)

**"샘플 위치를 맞추는 것"과 "언제 재생을 시작할지를 맞추는 것"은 서로 다른 문제다.**
기존 코드는 `timeSamples`를 정확히 맞춰 샘플 단위 동기화는 완벽했지만, 정작 "언제 `Play()`를 호출하느냐"는 전혀 통제하지 않고 있었다. 이미 갖춰져 있던 `SongTime` 단일 마스터 시계와 1마디 길이 상수를 "재생 시작 시각을 역산하는 데" 재사용하고, `AudioSource.PlayScheduled()`로 그 시각을 dsp 하드웨어 시계에 직접 예약함으로써, 프레임 단위의 근사가 아닌 오디오 엔진 레벨의 정확한 마디 정렬을 달성할 수 있었다.

**그리고 더 중요하게는: 함수 단위 테스트가 통과해도, 그 함수로 들어가는 실제 호출 경로(caller)를 확인하지 않으면 "고쳤다고 착각"할 수 있다.** 이번 버그의 진짜 원인은 새로 만든 로직 자체의 결함이 아니라, 실제 게임의 유일한 진입 경로가 그 로직을 우회하는 예외 분기로 빠지고 있었다는 점이었다. 사용자의 "직접 플레이해서 들어보라"는 피드백이 없었다면 격리 테스트만 통과한 채 실제로는 고쳐지지 않은 상태로 넘어갈 뻔했다.

**마지막으로: "언제"와 "어디서부터"는 독립적인 두 개의 질문이다.** 여러 트랙이 하나의 곡을 공유하는 경우(악기 레이어링)와, 서로 다른 두 곡이 박자만 맞추면 되는 경우(보스 BGM)는 "언제 시작할지" 계산은 재사용할 수 있어도 "어디서부터 재생할지"는 반드시 구분해야 한다. 하나로 뭉뚱그린 헬퍼는 컴파일도 되고, 타이밍 테스트도 통과하고, 심지어 "마디에 맞춰 시작은 한다" — 그런데도 조용히 틀린 소리를 낸다.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
