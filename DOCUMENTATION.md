# 🎼 Conductor's Symphony - 상세 개발 기록, 트러블슈팅 및 기획/기술 의도 문서 (DOCUMENTATION.md)

본 문서는 **Conductor's Symphony** 프로젝트의 전체 개발 과정, 발생했던 주요 오류 및 트러블슈팅 내역, 4마디 음악 곡 구조 설계, 엘리트 보스전 및 특수 전리품 보상 시스템, 그리고 코드 아키텍처를 상세히 기록한 종합 개발 보고서입니다.

IDE(VS Code, Visual Studio, Rider 등)에서 이 문서를 열람하여 언제든지 개발 잔혹사, 개선 이력, 코드 아키텍처를 파악할 수 있습니다.

---

## 📌 1. 프로젝트 개요 (Overview)

* **프로젝트명:** Conductor's Symphony
* **장르:** 리듬 + 로그라이크 탑뷰 생존 게임 (Rhythm Roguelite)
* **핵심 컨셉:** 지휘자가 된 캐릭터가 10종의 악기 중 4개를 조합하여 몰려오는 음표 몬스터들을 리듬 연주로 물리치고 곡을 완성하는 게임.
* **양손 분할 조작계:**
  * **오른손 (`방향키 ↑, ↓, ←, →`):** 2D 탑뷰 캐릭터 이동 및 탄막 회피 전담
  * **왼손 (`W, A, S, D`):** 4방향 리듬 노트 판정 연주 전담 (`A`: Left, `W`: Up, `S`: Down, `D`: Right)

---

## 🛠️ 2. 단계별 개발 히스토리 & 기획 의도 (Development Steps)

### 🔹 Step 1: 코어 플레이어 조작 & 4방향 리듬 연주 MVP
* **구현 목표:** 지휘자 2D 이동, 4방향 리듬 노트 판정선 및 BPM 노트 생성기 구현
* **의도:** 뱀파이어 서바이버 특유의 이동과 리듬 게임의 판정 조작을 한 화면에서 결합하는 최소 기능 프로토타입(MVP) 검증.

### 🔹 Step 2: 음표 몬스터 스폰 & 오토 타겟팅 유도 공격 시스템
* **구현 목표:** 360도 화면 외곽 몬스터 스폰 AI, 리듬 성공 시 가장 가까운 적 오토 타겟팅 공격, 플레이어 HP 및 충돌 데미지 시스템
* **의도:** 마우스 조준 부담을 완전히 없애고, 리듬 노트(`WASD`)만 성공시키면 유도 음파 발사체가 몬스터를 자동 파괴하도록 하여 리듬 타격 쾌감에 몰입하도록 설계.

### 🔹 Step 3: 10종 악기 로그라이크 덱빌딩 & 32비트 음악 시퀀서 시스템
* **구현 목표:** 시작 악기 선택 카드 + EXP 보석 습득 레벨업 + 10종 악기 중 4개 슬롯 선택 덱빌딩 + 32박자(4마디) 음악 시퀀서 루프 엔진 + 10종 악기 고유 키음(Key-Sound)
* **의도:** 게임 시작 시 바로 1개 악기(A키)를 가지고 시작하여 전투 가능하게 하고, 레벨업 시 3장 카드로 악기를 추가/강화하는 로그라이크 재미 요소와 음악성을 결합.

### 🔹 Step 4-A: 엘리트 보스 주기적 순환 & 특수 전리품 상자 보상 시스템
* **구현 목표:** 60초 주기 엘리트 보스 재등장 + 3가지 360도 탄막 공격 + 직접 근접 습득 황금 보물상자(`EliteRewardChest.cs`) + `ELITE CHEST REWARD` 3카드 선택 팝업
* **의도:** 보스 처치 시 게임이 종료되지 않고 잡몹전과 보스전이 주기적으로 순환하여 무한 생존을 즐기게 하고, 먼거리에서 자동 흡수되는 경험치 보석과 달리 플레이어가 직접 발로 다가가 부딪쳐야 습득되는 엄격한 전리품 상자로 특수 성장의 쾌감을 제공.

---

## 🎼 3. 32비트 4마디 곡 구조(Song Form) & 템포 밸런싱

### 🎵 4마디 곡 구조 (4-Bar Song Form)
단순한 8비트 조각 패턴의 반복을 지양하고, **32박자(약 10.6초) 전체가 4마디의 생생한 곡 구조**를 형성하도록 설계했습니다:
* **Bar 1 (Step 0 ~ 7):** 메인 주제 비트 제시 (Intro)
* **Bar 2 (Step 8 ~ 15):** 엇박 변형 및 싱코페이션 (Variation)
* **Bar 3 (Step 16 ~ 23):** 비트 빌드업 및 긴장감 조성 (Development)
* **Bar 4 (Step 24 ~ 31):** **드럼 필인(Fill-in) & 턴어라운드 클라이맥스** → 다시 Bar 1 메인 비트로 자연스럽게 순환!

### ⏳ 템포 감속 & 노트 밀도 다이어트
* **90 BPM 템포 조율:** 120 BPM(0.25s)의 촉박함을 `90 BPM (0.33s/step)`으로 완화하고, 판정 범위(Perfect 0.10s, Great 0.22s)를 넓혀 여유 있는 손맛 제공.
* **패턴 밀도 다이어트:** 3~5단계로 악기가 늘어나도 만렙(Lv.5) 시 32박자 동안 **최대 12개 이하(마디당 3개 꼴)**로 비트를 정돈하여 4개 악기가 만렙이 되어도 왼손 연주와 오른손 이동의 최적 피로도 밸런스를 달성.

---

## 🔍 4. 트러블슈팅 & 문제 해결 기록 (Troubleshooting Log)

개발 과정에서 발생한 **총 8가지 핵심 문제**와 해결 내역입니다:

1. **Unity New Input System 예외 (`InvalidOperationException`):** `UnityEngine.InputSystem.Keyboard.current` API로 전면 교체하여 런타임 예외 해결.
2. **QWER 키의 수평 배치 공간 인지 부조화:** 십자형 `WASD` (왼손 연주) + `방향키` (오른손 이동) 전속 분리.
3. **카메라 미동기화 & 리듬 UI 이격:** `CameraController.cs` 추가로 지휘자를 1:1 화면 중앙 고정 밀착 추적.
4. **플레이어 이동 시 노트 이탈 문제 (월드 좌표 고정):** `RhythmNote.cs`를 플레이어 실시간 상대 좌표(`targetTransform + direction * distance`) 추적으로 개편.
5. **레벨업 카드 팝업 시 클릭 불가능 현상:** 씬 계층 구조에 `InputSystemUIInputModule` 탑재 `EventSystem` 추가 및 키보드 `1`, `2`, `3` 단축키 탑재.
6. **Perfect vs Great 타격 시 피치 이탈:** `sfxSource.pitch = 1.0f`로 완전 고정하여 일관되고 아름다운 키음 정음 연주.
7. **비트의 단조로운 반복 현상:** 32비트 4마디 완성형 곡 구조(Bar 1~4: 도입-변형-빌드업-필인/턴어라운드) 도입.
8. **3~5단계 수많은 노트로 인한 난이도 수직 상승:** 90 BPM 템포 감속 및 Lv.5 기준 32박자 중 최대 12개 패턴 다이어트로 조작 쾌적함 완성.

---

## 📂 5. 전체 C# 소스코드 구조 & 파일별 역할 (File Architecture)

```text
Assets/Scripts/
├── Audio/
│   └── AudioLayerManager.cs        # 10종 악기 고유 키음(Sine/Saw/Square/Triangle/Noise) 합성 및 메트로놈 BGM
├── Camera/
│   └── CameraController.cs         # 지휘자 1:1 화면 중앙 고정 카메라 추적 스크립트
├── Combat/
│   ├── AttackProjectile.cs         # 유도 음파 발사체 (일반 몬스터 & 엘리트 보스 타겟팅)
│   └── RhythmAttackManager.cs      # WASD 리듬 성공 시 오토 타겟팅 Multi-Shot 발사 & 키음 재생
├── Enemy/
│   ├── BossMonster.cs              # 거대 엘리트 보스 AI, 60 HP, 3가지 360도 탄막 패턴 및 특수 상자 드롭
│   ├── BossProjectile.cs           # 보스 전용 360도/조준/스파이럴 탄막 발사체
│   ├── EnemyMonster.cs             # 음표 몬스터 추적 AI, HP, 피격 플래시 & ExpGem 드롭
│   └── EnemySpawner.cs             # 60초 주기 엘리트 보스 스폰 및 잡몹 스폰 억제/재개 루프
├── Instrument/
│   ├── InstrumentData.cs            # 악기 데이터 클래스 & 레벨업 스탯(데미지/멀티샷/점수)
│   ├── InstrumentItem.cs            # 몬스터 드롭 악기 수집 아이템 개체
│   ├── InstrumentManager.cs         # 10종 악기 중 4슬롯 덱빌딩 및 호위 펫 관리
│   ├── InstrumentOrbit.cs           # 지휘자 호위 펫 둥둥 부유 & Lerp 추적 모션
│   └── InstrumentPatternDatabase.cs # 10종 악기 Lv.1~5 (4마디 32비트 곡 구조 패턴 DB)
├── Item/
│   └── EliteRewardChest.cs         # 엘리트 보스 처치 시 드롭되는 황금 보물상자 (엄격한 근접 픽업)
├── Player/
│   ├── ExpGem.cs                    # 몬스터 사망 시 드롭되는 에메랄드 경험치 보석 (자석 흡수)
│   ├── PlayerController.cs          # 오른손 방향키 이동, HP 및 무적 프레임 관리
│   └── PlayerExperience.cs         # 경험치 획득, 레벨업 감지 및 Event 전달
├── Rhythm/
│   ├── RhythmManager.cs             # 90 BPM 32비트(4마디) 시퀀서 루프 엔진 & WASD 판정
│   ├── RhythmNote.cs                # 실시간 상대 좌표 추적 노트 개체
│   └── RhythmUI.cs                  # Score, Combo, HP, EXP, Boss HP, Instrument Slot UI
└── UI/
    └── LevelUpUI.cs                 # 스타팅/레벨업/엘리트상자 3카드 팝업, EventSystem 검증 및 키보드 1,2,3 단축키
```

---

*본 문서는 개발 과정 전반을 완벽히 보관하며, 프로젝트 루트의 `DOCUMENTATION.md`에서 언제든지 열람할 수 있습니다.*
