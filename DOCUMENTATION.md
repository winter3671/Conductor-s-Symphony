# 🎼 Conductor's Symphony - 상세 개발 기록, 트러블슈팅 및 기획/기술 의도 문서 (DOCUMENTATION.md)

본 문서는 **Conductor's Symphony** 프로젝트의 전체 개발 과정, 발생했던 주요 오류 및 트러블슈팅 내역, 97 BPM 4마디 음악 곡 구조 설계, 오디오 음원 구조(`Assets/Resources/Audio/`), 엘리트 보스전 및 특수 전리품 보상 시스템, QWER 조작계 및 리듬 밸런싱, 3D 월드 플로팅 타격 텍스트 팝업, 10종 악기 픽셀 아트 및 사운드 연동, 초미세 수축 비트 링 시각화, 그리고 코드 아키텍처를 상세히 기록한 종합 개발 보고서입니다.

IDE(VS Code, Visual Studio, Rider 등) 및 AI 에이전트(Antigravity / Claude Code / Cursor)에서 이 문서를 열람하여 언제든지 개발 이력, 개선 사항, 코드 아키텍처를 파악할 수 있습니다.

---

## 📌 1. 프로젝트 개요 (Overview)

* **프로젝트명:** Conductor's Symphony
* **장르:** 리듬 + 로그라이크 탑뷰 생존 게임 (Rhythm Roguelite)
* **핵심 컨셉:** 지휘자가 된 캐릭터가 10종의 악기 중 4개를 조합하여 몰려오는 음표 몬스터들을 리듬 연주로 물리치고 곡을 완성하는 게임.
* **현재 작업 브랜치:** `feat/gameplay_core` (Role 3: 메인 게임플레이 프로그래머)
* **양손 분할 조작계:**
  * **오른손 (`방향키 ↑, ↓, ←, →`):** 2D 탑뷰 캐릭터 이동 및 탄막 회피 전담
  * **왼손 (`Q, W, E, R`):** 지휘자 상단 부채꼴(V자) 4방향 리듬 노트 판정 연주 전담
    * `Q` 키: Left (180° - 1번 슬롯: `{-100, 0}` 정밀 안착, 시작 시 드럼 기본 장착)
    * `R` 키: Right (0° - 2번 슬롯: `{100, 0}` 정밀 안착)
    * `W` 키: Up-Left (135° - 3번 슬롯: `{-80, 80}` 정밀 안착, Lv.5 해금)
    * `E` 키: Up-Right (45° - 4번 슬롯: `{80, 80}` 정밀 안착, Lv.8 해금)

---

## 🛠️ 2. 단계별 개발 히스토리 & 기획 의도 (Development Steps)

### 🔹 Step 1: 코어 플레이어 조작 & 4방향 리듬 연주 MVP
* **구현 목표:** 지휘자 2D 이동, 4방향 리듬 노트 판정선 및 BPM 노트 생성기 구현.

### 🔹 Step 2: 음표 몬스터 스폰 & 오토 타겟팅 유도 공격 시스템
* **구현 목표:** 360도 화면 외곽 몬스터 스폰 AI, 리듬 성공 시 가장 가까운 적 오토 타겟팅 공격, 플레이어 HP 및 충돌 데미지 시스템.

### 🔹 Step 3: 10종 악기 로그라이크 덱빌딩 & 32비트 음악 시퀀서 시스템
* **구현 목표:** 10종 악기 중 4개 슬롯 선택 덱빌딩 + 32박자(4마디) 음악 시퀀서 루프 엔진 + 10종 악기 고유 키음(Key-Sound).

### 🔹 Step 4-A: 엘리트 보스 주기적 순환 & 특수 전리품 상자 보상 시스템
* **구현 목표:** 2분(120초) 주기 엘리트 보스 재등장 + 3가지 360도 탄막 공격 + 직접 근접 습득 황금 보물상자(`EliteRewardChest.cs`) + `ELITE CHEST REWARD` 3카드 선택 팝업.

### 🔹 Step 4-B: QWER 부채꼴 4레인 조작계, 단계적 슬롯 해금 & 8회 MAX 다이내믹 리듬 시스템
* **구현 목표:** QWER 부채꼴 4방향 전환 + 단계적 슬롯 해금(Lv 1~4: 2개, Lv 5~7: 3개, Lv 8+: 4개) + 10종 악기 100% 고유 비트 명세.

### 🔹 Step 4-C: 지휘자 머리 위 3D 월드 플로팅 타격 텍스트 팝업 (`HitFloatingText.cs`) 연동
* **구현 목표:** 지휘자 머리 위에 생동감 있게 튀어오르는 3D 월드 플로팅 텍스트 팝업(`PERFECT!`, `GREAT!`, `MISS`) 시스템 구축.

### 🔹 Step 4-D: 10종 악기 픽셀 아트 연동 & QWER 1:1 오케스트라 펫 배치
* **구현 목표:** 10종 악기 픽셀 아트 PNG 에셋 연동 + 1.5배 스케일 감축(`0.68m`) + QWER 조작키 방향과 1:1 일치하는 호위 펫 배치(`Q`=좌, `R`=우, `W`=좌상, `E`=우상).

### 🔹 Step 5-A: 드럼(Drums) 기본 장착 시작 규칙 & 그룹 제한 해제 무작위 3카드 선택
* **구현 목표:** 게임 시작 시 시작 선택 팝업 없이 **드럼(Drums)을 기본 악기로 Slot 0(`Q`키)에 자동 장착**하여 전투 즉시 시작 + 레벨업 시 그룹 제한 없이 10종 악기 무작위 3카드 팝업 등장.

### 🔹 Step 5-B: 97 BPM 오디오 싱크 & 11종 WAV 음원 파일 아키텍처 구축
* **구현 목표:** `97 BPM` 리듬 시퀀서 싱크 + `Assets/Resources/Audio/` 내 11종 고품질 WAV 음원 파일 세팅 + 악기 획득 시 1회 고유 WAV 재생(`acquisitionSource`, 1.0x 피치 고정) + 노트 판정 시 정갈한 단음 SFX 재생(`sfxSource`).

---

## 🎼 3. 오디오 음원 구조 및 사운드 시스템 (`Assets/Resources/Audio/`)

| 구분 | 유니티 에셋 파일 경로 | 런타임 재생 역할 | 비고 |
|---|---|---|---|
| 🥁 **드럼** | `Assets/Resources/Audio/Sound_Drums.wav` | 악기 습득 시 1회 고유 음원 연주 | 시작 기본 장착 악기 |
| 🎹 **피아노** | `Assets/Resources/Audio/Sound_Piano.wav` | 악기 습득 시 1회 고유 음원 연주 | 1번 악기 |
| 🎻 **바이올린** | `Assets/Resources/Audio/Sound_Violin.wav` | 악기 습득 시 1회 고유 음원 연주 | 2번 악기 |
| 🪈 **플루트** | `Assets/Resources/Audio/Sound_Flute.wav` | 악기 습득 시 1회 고유 음원 연주 | 3번 악기 |
| 📯 **프렌치호른** | `Assets/Resources/Audio/Sound_FrenchHorn.wav` | 악기 습득 시 1회 고유 음원 연주 | 4번 악기 |
| 🔔 **글록켄슈필** | `Assets/Resources/Audio/Sound_Glockenspiel.wav` | 악기 습득 시 1회 고유 음원 연주 | 5번 악기 |
| 🎻 **첼로** | `Assets/Resources/Audio/Sound_Cello.wav` | 악기 습득 시 1회 고유 음원 연주 | 6번 악기 |
| 🥁 **팀파니** | `Assets/Resources/Audio/Sound_Timpani.wav` | 악기 습득 시 1회 고유 음원 연주 | 7번 악기 |
| 🪵 **마림바** | `Assets/Resources/Audio/Sound_Marimba.wav` | 악기 습득 시 1회 고유 음원 연주 | 8번 악기 |
| 🔔 **벨** | `Assets/Resources/Audio/Sound_Bell.wav` | 악기 습득 시 1회 고유 음원 연주 | 9번 악기 |
| 👿 **보스전 BGM** | `Assets/Resources/Audio/BGM_BossBattle.wav` | 엘리트 보스 출현 시 배경음악 연주 | 보스전 BGM |

---

## 🔍 4. 트러블슈팅 & 문제 해결 기록 (Troubleshooting Log)

1. **WASD 인지 부조화 & 손가락 꼬임:** 왼손 `Q, W, E, R` 가로 4키 + 오른손 `방향키`로 전면 개편.
2. **2번째 악기 `R`키 오디오 미재생 버그:** `RhythmAttackManager`에서 `GetSlotForLane(lane)`으로 수정하여 Slot 1 오디오 키음 정상 재생.
3. **오디오 재생 중 속도 늘어짐 현상:** 단음 타건음 피치 변경(`0.95f`)이 WAV 음원 오디오 채널에 간섭하던 원인 발견 ➡️ **`acquisitionSource` 채널 신설 후 1.0x 피치로 독립 고정**.
4. **노트 이동 속도 및 오디오 박자 어긋남 현상:** 97 BPM 기준 1마디(2.474초) 이동 시간(`noteTravelDuration = 2.474s`) 및 오디오 정밀 지연(`PlayDelayed(1.8557s)`) 적용으로 1ms 오차 없는 정밀 싱크 달성.
5. **드럼 노트 박자가 여전히 빠르게 내려오던 2단 원인 규명 & 해결:**
   * **원인 A (패턴 밀도 2배 버그):** `InstrumentPatternDatabase.cs`의 Drums Lv1~3 패턴이 4스텝(1.237초)마다 노트를 생성 ➡️ 설계된 8스텝(2.474초, 1마디) 간격보다 정확히 2배 빠르게 노트가 쏟아지던 문제. `"10001000..."` ➡️ `"10000000..."`(Step 0, 8, 16, 24)로 재배치하여 1마디당 1노트로 수정.
   * **원인 B (씬 직렬화 오버라이드가 코드 기본값을 덮어씀):** 패턴을 고쳐도 체감 박자가 그대로였던 진짜 원인은 `Gameplay.unity`에 저장된 `RhythmManager` 컴포넌트의 **Inspector 직렬화 값**이 `bpm: 120`, `noteTravelDuration: 1.2`, `perfectWindow: 0.08`, `greatWindow: 0.18`로 코드의 새 기본값(`97` / `2.474` / `0.10` / `0.22`)을 그대로 덮어쓰고 있었기 때문. **`[SerializeField]` 필드는 스크립트 기본값을 바꿔도, 씬/프리팹에 이미 저장된 값이 항상 우선 적용된다** ➡️ Unity MCP(`manage_editor stop` → `manage_components set_property` → `manage_scene save`)로 씬에 저장된 실제 값을 코드 기본값과 동기화하여 해결.
   * ⚠️ **교훈:** 리듬/밸런스 관련 `[SerializeField]` 수치를 코드에서 조정할 때는 반드시 `Gameplay.unity`(또는 해당 프리팹)에 박제된 실제 Inspector 값도 함께 확인·동기화해야 함. 코드만 고치고 "값이 안 바뀐다"고 착각하기 쉬운 대표적인 Unity 함정.

---

## 📂 5. 전체 C# 소스코드 구조 & 파일별 역할 (File Architecture)

```text
Assets/
├── Resources/
│   ├── Audio/                      # 11종 고품질 WAV 음원 에셋 (Sound_Drums, Sound_Piano, BGM_BossBattle 등)
│   ├── Sprites/
│   │   ├── Instruments/            # 10종 악기 픽셀 아트 PNG 에셋 (Drums, Piano, Violin 등)
│   │   └── Player/                 # 대기(Idle), 이동(Move), 지휘(Hit) 픽셀 아트 PNG 에셋
Assets/Prefabs/
├── Combat/                         # 발사체 프리팹
├── Enemies/                        # EnemyMonster.prefab, BossMonster.prefab
├── Instruments/                    # InstrumentOrbit.prefab
├── Items/                          # ExpGem.prefab, EliteRewardChest.prefab
├── Player/                         # Player.prefab
└── UI/                             # UI 프리팹
Assets/Scripts/
├── Audio/
│   └── AudioLayerManager.cs        # 11종 WAV 음원 습득 연주 & 단음 SFX 키음 재생 제어
├── Camera/
│   └── CameraController.cs         # 지휘자 1:1 화면 중앙 고정 카메라 추적
├── Combat/
│   ├── AttackProjectile.cs         # 유도 음파 발사체
│   └── RhythmAttackManager.cs      # QWER 리듬 성공 시 오토 타겟팅 Multi-Shot 발사
├── Enemy/
│   ├── BossMonster.cs              # 거대 엘리트 보스 AI & 탄막 패턴
│   ├── BossProjectile.cs           # 보스 전용 360도/조준/스파이럴 탄막 발사체
│   ├── EnemyMonster.cs             # 음표 몬스터 추적 AI
│   └── EnemySpawner.cs             # 몬스터 스폰 루프
├── Instrument/
│   ├── InstrumentData.cs            # 악기 데이터 클래스
│   ├── InstrumentManager.cs         # 시작 시 드럼 기본 장착 & 4슬롯 덱빌딩
│   ├── InstrumentOrbit.cs           # QWER 방향 1:1 매핑 호위 펫 0.68m 부유
│   └── InstrumentPatternDatabase.cs # 10종 악기 97 BPM 32비트 고유 비트 DB
├── Player/
│   ├── ExpGem.cs                    # 에메랄드 경험치 보석
│   ├── PlayerController.cs          # 방향키 이동 4방향 애니메이션, QWER 지휘 포즈
│   └── PlayerExperience.cs         # 경험치 획득 & 레벨업 전달
├── Rhythm/
│   ├── HitFloatingText.cs           # 3D 월드 플로팅 타격 텍스트 팝업 (PERFECT, GREAT, MISS)
│   ├── RhythmManager.cs             # 97 BPM 32비트 시퀀서 루프 엔진 & QWER 판정
│   ├── RhythmNote.cs                # 실시간 상대 좌표 추적 노트 개체
│   └── ShrinkingRhythmRing.cs       # 동시타 식별용 0.005f 초미세 수축 비트 링
└── UI/
    └── LevelUpUI.cs                 # 그룹 제한 해제 10종 악기 무작위 3카드 팝업
```

---

*본 문서는 개발 과정 전반을 완벽히 보관하며, 프로젝트 루트의 `DOCUMENTATION.md`에서 언제든지 열람할 수 있습니다.*
