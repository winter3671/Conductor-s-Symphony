# 🏆 [Portfolio Case Study] 리듬 뱀서 "Conductor's Symphony" 오디오-비트 동기화 & 트러블슈팅 종합 보고서

> **작성자:** winter3671 (Role 3: 메인 게임플레이 & 오디오/리듬 엔진 프로그래머)  
> **프로젝트명:** Conductor's Symphony (Unity / C# / Rhythm Roguelite)  
> **주요 성과:** 97 BPM 16마디 10종 악기 멀티트랙 레이어링, 0ms 무한 심리스 루프, 하드웨어 오디오 클록 Single Source of Truth 아키텍처 구축  

---

## 📌 1. 프로젝트 & 담당 역할 개요 (Project & Role Overview)

* **장르:** 탑뷰 리듬 뱀서 (Rhythm Roguelite)
* **핵심 메커니즘:** 플레이어가 지휘자가 되어 10종의 악기 중 최대 4개를 선택·강화하며 몰려오는 음표 몬스터들을 QWER 4방향 리듬 연주로 분쇄하는 게임.
* **담당 역할 (Role 3):**
  * 97 BPM 32스텝(4마디) 리듬 시퀀서 엔진 설계 및 QWER 부채꼴 판정 구축
  * 10종 악기 고유 WAV 음원 멀티트랙 동기화 레이어링 시스템 구현
  * 오디오 파형과 시각 노트 간의 0.000ms 오차 방지 및 일시정지 후 비트 밀림(Beat Drift) 트러블슈팅

---

## 🎯 2. 기술적 도전 과제 (Technical Challenges)

1. **멀티트랙 오케스트라 레이어링 (Multi-Track Synchronized Layering):**
   * 악기를 획득할 때마다 기존 재생 중이던 드럼 음원이 끊기지 않고, 새로 추가된 악기(피아노, 팀파니 등)가 **동일한 오디오 타임스탬프(`timeSamples`) 위치에 1ms 오차 없이 덧씌워져 연주**되어야 함.
2. **일시정지 후 비트 밀림 방지 (Zero-Drift on Pause/Unpause):**
   * 레벨업 팝업이나 보물상자 팝업으로 `Time.timeScale = 0` (일시정지) 후 다시 재개(`Time.timeScale = 1`)할 때마다 청각 오디오와 시각 노트의 도착 시점이 어긋나는 현상 방지.
3. **0ms 무한 심리스 루프 (Seamless Zero-Gap Looping):**
   * 16마디(39.5876초) 음악이 루프할 때 파일 끝부분의 무음 여백으로 인한 0.1~1.3초 툭 끊김 현상 제거.

---

## 🛠️ 3. 주요 트러블슈팅 히스토리 (Troubleshooting & Iterations)

### 🚨 CASE 1. 유니티 Inspector 직렬화 오버라이드 함정
* **증상:** 스크립트 코드(`.cs`)에서 `bpm = 97f`, `noteTravelDuration = 2.474f`로 수정했으나 런타임에서 박자와 노트 속도가 바뀌지 않는 기현상 발생.
* **원인:** Unity의 `[SerializeField]` 필드는 C# 코드 기본값을 바꿔도, 씬 파일(`Gameplay.unity`) Inspector에 박제된 실제 저장값(`bpm: 120`, `noteTravelDuration: 1.2`)이 항상 최우선으로 코드값을 덮어씌움.
* **해결:** Unity MCP 에디터 API를 통해 씬 직렬화 수치를 코드 기본값과 완벽히 검증 및 동기화하여 해결.
* **교훈:** 리듬/밸런스 관련 `[SerializeField]` 수치를 수정할 때는 반드시 씬/프리팹 직렬화 값도 함께 검증해야 함.

---

### 🚨 CASE 2. 단음 타건 피치 변경이 음원 재생 속도를 왜곡시키던 오디오 간섭 버그
* **증상:** 키 노트를 타격할 때 `PERFECT` 판정이 뜨면 피치를 `1.05f`로 올려 경쾌한 사운드를 연출하려 했으나, 40초짜리 전체 배경 WAV 음원의 재생 속도가 함께 빨라지거나 왜곡되는 문제 발생.
* **원인:** 단음 타건 SFX와 긴 WAV 음원 트랙이 단 하나의 `AudioSource` 채널을 공유하고 있었음.
* **해결:** `acquisitionSource` (1.0x 피치 고정 WAV 멀티트랙 전용)와 `sfxSource` (단음 SFX 전용)로 AudioSource 채널 아키텍처를 명확히 이중 분리.

---

### 🚨 CASE 3. 레벨업 일시정지 후 비트 미세 누적 밀림 버그 (Beat Drift)
* **증상:** 가만히 플레이할 때는 음악과 비트가 정확히 맞으나, 레벨업 팝업을 3~4회 이상 열었다 닫을 때마다 비트 노트와 음악 정박 소리가 수십 ms 단위로 미세하게 밀리며 누적되는 현상.
* **원인 진단 (이중 시계 오차):**
  * 프로젝트 내에 **독립된 두 개의 시계**가 존재했음:
    1. **게임/프레임 시계 (`Time.time`):** `Time.timeScale = 0` 시 프레임 단위로 완벽 정지.
    2. **오디오 DSP 시계 (`AudioSource.time`):** 사운드 카드의 오디오 버퍼(10~40ms) 단위로 처리.
  * 일시정지를 해제할 때 `Time.time`은 프레임 0부터 즉시 가동되지만, 사운드 카드는 오디오 믹싱 버퍼를 채우느라 20~40ms의 양자화 레이턴시가 발생함. 이 오차가 일시정지할 때마다 무작위 누적(Random Walk Drift)되었던 것.
* **아키텍처 혁신 (Single Source of Truth `SongTime` 건축):**
  * **"두 시계를 매번 재동기화하려 하지 말고, 프레임 시계를 없애고 오디오 하드웨어 시계 하나만 쓴다!"**
  * `AudioLayerManager`에 사운드 카드 실제 출력 샘플 위치(`timeSamples / clip.frequency`)를 읽어오는 `SongTime` 단일 진실 소스 프로퍼티 신설.
  * `RhythmManager`(시퀀서 스텝 계산), `RhythmNote`(노트 이동 `progress`), `ShrinkingRhythmRing`(수축 링) 모두 프레임 시계(`Time.time`)를 폐지하고 `SongTime`만을 바라보도록 개편.
* **결과:** 일시정지를 100번 넘게 반복해도 **0.000ms 오차 없는 완전 무결점 오디오 동기화 달성**.

---

### 🚨 CASE 4. 오디오 루프 경계 튐 (Wrap-Around Edge Case)
* **증상:** 오디오 하드웨어 시계(`SongTime`) 적용 후, 약 40초(16마디) 루프 지점을 통과할 때 비트에 맞춰 줄어들어야 할 수축 링(`ShrinkingRhythmRing`)이나 노트가 줄어들지 않고 화면에 얼어붙어 남는 현상 발생.
* **원인:** `SongTime`이 16마디 끝(`39.58s`)을 지나 0초로 리셋(Wrap-around)될 때, 비행 중이던 노트의 경과시간(`elapsed = SongTime - spawnTime`)이 `0.1s - 38.5s = -38.4s`라는 큰 음수로 튀어 `Destroy()` 조건을 만족하지 못함.
* **해결:** `AudioLayerManager.SongLoopLength` (클립 1회 루프 시간) 프로퍼티 신설 후, `elapsed < -travelDuration` 조건 검출 시 `elapsed += SongLoopLength`로 루프 경계를 자동 연장 보정하도록 처리.

---

## 💎 4. 핵심 교훈 및 포트폴리오 기술 면접 대비 요약 (Key Takeaways)

1. **"리듬 게임 엔진의 핵심은 '독립 시계의 동기화'가 아니라 '단일 하드웨어 시계의 종속'이다."**
   * 게임 프레임 기반의 시계와 사운드 카드 하드웨어 시계를 따로 두고 동기화하려 하면 레이턴시 오차가 반드시 발생한다. 사운드 카드의 파형 샘플 위치를 Single Source of Truth로 정하고 모든 비주얼을 이에 종속시키는 것이 정답이다.
2. **"루프 기반 아키텍처에서는 경계값(Wrap-Around) 처리까지 고려해야 완전하다."**
   * 오디오 파형이 0초로 리셋되는 시점의 모듈러 보정 수식을 설계함으로써, 무한 플레이 상태에서도 메모리 누수나 렌더링 튐 없이 안정적인 시스템을 완성했다.

---

*본 문서는 `Docs/winter3671/` 폴더에 보관되어 개발 포트폴리오 및 기술 블로그 자료로 즉시 활용할 수 있습니다.*
