# 🎼 Conductor's Symphony

[![Unity CI](https://github.com/winter3671/Conductor-s-Symphony/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/winter3671/Conductor-s-Symphony/actions/workflows/unity-ci.yml)

## 1. 개요 (Overview)

* **프로젝트명:** Conductor's Symphony
* **플랫폼:** PC Desktop — Windows / Linux / macOS (Unity 6 URP 2D Pipeline)
* **장르:** 리듬 + 뱀파이어 서바이버 (로그라이트)
* **핵심 컨셉:** 지휘자가 된 캐릭터가 최대 4개의 악기를 조율하며, 몰려오는 음표 몬스터들을 리듬 연주(공격)로 물리치고 한 곡을 완성하는 탑뷰 생존 게임.
* **한 판 구성:** 10분 잡몹전(레벨 1→20) + 2분 최종 보스전(타임어택), 총 12분.
* 📖 **[상세 개발 기록 및 기술/기획 의도 문서 (DOCUMENTATION.md)](file:///c:/Users/admin/Desktop/My%20project/DOCUMENTATION.md)**
* 🧩 **[팀원용 프리팹(Prefab) 사용 설명서 (PREFAB_GUIDE.md)](file:///c:/Users/admin/Desktop/My%20project/PREFAB_GUIDE.md)**
* 📋 **[게임플레이 시스템 종합 정리 (Docs/game_systems_reference.md)](file:///c:/Users/admin/Desktop/My%20project/Docs/game_systems_reference.md)** — 악기 메커니즘 · 밸런스 · 패시브 · 구현 현황을 다루는 상시 참고 문서
* 📐 **[밸런스 & 시스템 기획서 원본 (Docs/game_balance_design.md)](file:///c:/Users/admin/Desktop/My%20project/Docs/game_balance_design.md)**

---

## 2. 게임 플레이 방법 (How to Play)

### 2.1 조작법

양손을 분리해서 쓰는 조작계입니다 — **오른손은 이동, 왼손은 리듬 연주**를 전담합니다. (모든 키는 설정에서 재바인딩 가능)

| 손 | 키 | 역할 |
|---|---|---|
| 오른손 | `↑` `↓` `←` `→` (방향키) | 캐릭터 이동 & 탄막 회피 |
| 왼손 | `Q` | 리듬 노트 판정 (좌, Lv.1부터 사용 가능) |
| 왼손 | `R` | 리듬 노트 판정 (우, Lv.5에 해금) |
| 왼손 | `W` | 리듬 노트 판정 (좌상, Lv.10에 해금) |
| 왼손 | `E` | 리듬 노트 판정 (우상, Lv.15에 해금) |
| 공통 | `Esc` | 일시정지 메뉴 (계속하기 / 환경설정 / 메인으로 / 게임종료) |

지휘자 주변에 고정된 판정 링을 향해 다가오는 노트를 정확한 타이밍에 눌러 연주합니다. 판정 성공 즉시(또는 홀드 중) 장착된 악기가 자동으로 가장 가까운 적을 공격합니다 — 별도의 마우스 조준은 필요 없습니다.

### 2.2 리듬 판정

노트가 판정 링에 도달하는 순간을 기준으로 3단계로 판정됩니다.

| 판정 | 허용 오차 | 효과 |
|---|---|---|
| **PERFECT** | ±0.10초 | 최대 딜량 배율 |
| **GREAT** | ±0.22초 | 중간 딜량 배율 |
| **MISS** | ±0.45초를 벗어남 | 공격 불발 |

풀 콤보(100% PERFECT)에 가까울수록 최종 DPS가 최대 2배까지 상승하므로, 이동으로 회피하면서도 왼손 박자를 놓치지 않는 것이 핵심입니다.

### 2.3 악기 시스템 (10종 중 4개 조합)

게임 시작 시 **드럼(Drums)이 Q 슬롯에 고정 장착**된 상태로 바로 전투가 시작되고, 이후 R/W/E 슬롯이 레벨업과 함께 열리면서 무작위 3카드 중 하나를 선택해 새 악기를 얻거나 기존 악기를 강화합니다. 각 악기는 5레벨까지 성장하며, 입력 방식에 따라 두 그룹으로 나뉩니다.

**탭(Tap) 계열 — 판정 성공 즉시 1회성 공격**

| 악기 | Lv.1 효과 |
|---|---|
| 🥁 드럼 (Drums) | 정박 타격 시 주변 360° 충격파 + 넉백 |
| 🎹 피아노 (Piano) | 가장 가까운 적 방향으로 관통 레이저 발사 |
| 🔔 글록켄슈필 (Glockenspiel) | 체력이 가장 높은 적 머리 위로 별빛 낙하 |
| 🪵 마림바 (Marimba) | 이동 방향 일직선으로 목재 파동 발사 |
| 🔔 벨 (Bell) | 가장 가까운 적 중심으로 8방향 섬광 발사 |

**홀드(Hold) 계열 — 누르는 동안 유지 효과, 떼는 순간 해제 효과**

| 악기 | Lv.1 효과 |
|---|---|
| 🎻 바이올린 (Violin) | 홀드 중 회전 칼날 유지 + 릴리즈 시 부채꼴 참격 |
| 📯 프렌치호른 (French Horn) | 전방 120° 부채꼴 충격파 + 지속 넉백 |
| 🎻 첼로 (Cello) | 가장 가까운 적 발밑에 이동속도 감소 중력장 생성 |
| 🥁 팀파니 (Timpani) | 가장 가까운 적 구역에 충격파 포탄 낙하 |
| 🪈 플루트 (Flute) | 지나간 자리에 적을 끌어모으는 소용돌이 생성 |

악기별 5레벨 전체 성장 곡선과 Target DPS는 `Docs/game_balance_design.md` §5, 실제 구현 대비 검증 결과는 `Docs/game_systems_reference.md` §2에 정리돼 있습니다.

### 2.4 패시브 스탯 (장신구, 8종)

악기 대신 아래 8종의 패시브 스탯이 카드로 제시되기도 합니다. 각 5레벨까지 강화 가능합니다.

| 이름 | 테마 | 레벨당 효과 |
|---|---|---|
| 시포르찬도 (Sforzando) | 위력 | 모든 무기 피해량 +10%/Lv (최대 +50%) |
| 알레그로 (Allegro) | 템포 | 공격 속도·쿨타임 감축 +6%/Lv (최대 +30%) |
| 크레센도 (Crescendo) | 확장 | 모든 공격 범위 +10%/Lv (최대 +50%) |
| 비바체 (Vivace) | 기동성 | 이동 속도 +8%/Lv (최대 +40%) |
| 레가토 (Legato) | 연결 | 투사체 수 +1 (Lv3, Lv5 적용, 최대 +2개) |
| 페르마타 (Fermata) | 지속 | 장판·지속 효과 유효 시간 +15%/Lv (최대 +75%) |
| 공명 패널 (Resonance) | 자석 | EXP 구슬 획득 범위 +25%/Lv (최대 +125%) |
| 악보 튜닝 (Tuning) | 방어 | 피해 감소 +5%/Lv & 최대 HP +10%/Lv (최대 피해감소 25% / HP +50%) |

### 2.5 레벨업 & 엘리트 보스

* 적 처치 시 드롭되는 EXP 구슬을 모아 레벨업하면, 악기 강화 / 신규 악기 / 패시브 강화 중 무작위 3장의 카드가 뜨고 그중 하나를 선택합니다.
* 잡몹전 도중 **120초마다 엘리트 보스**가 등장합니다. 처치하면 보물상자가 드롭되고, 상자를 주우면 레벨업 카드와 동일한 3택1 보상 카드가 뜹니다.
* 레벨 20(만렙)에 도달하면 10:00 시점에 잡몹전이 종료되고 최종 보스전으로 전환됩니다.

### 2.6 최종 보스전 & 승패 조건

* 10:00 시점부터 잡몹 스폰이 멈추고 **HP 180,000의 최종 보스**가 단독 등장, **120초 제한 시간**의 타임어택이 시작됩니다.
* **승리:** 제한 시간 안에 최종 보스를 처치하면 승리 화면 표시.
* **패배:** 캐릭터 HP가 0이 되거나(`YOU DIED`), 120초 안에 최종 보스를 처치하지 못하면(`TIME OVER`) 패배 화면 표시.

### 2.7 메인 메뉴 & 설정

메인 메뉴에서 **시작 / 환경설정 / 게임종료**를 선택할 수 있고, 환경설정과 인게임 일시정지 메뉴(`Esc`) 양쪽에서 **BGM 음량 / 효과음 음량 / 악기 음량**을 개별 조절할 수 있습니다. 일시정지 메뉴에서는 추가로 **메인으로 / 게임종료**(둘 다 확인 다이얼로그 포함)를 선택할 수 있습니다.

---

## 3. 아트 및 비주얼 디자인 (Visual Design)

* **화풍 (Art Style):** 직관적이고 고전적인 매력이 있는 **2D 도트(Pixel Art) 그래픽**으로 제작합니다. 후반부로 갈수록 피아식별이 안 되는 시각적 피로도와 난잡함을 방지합니다.
* **캐릭터 및 무기 연출:** 플레이어 캐릭터는 턱시도나 연주복을 입은 '지휘자' 형태를 띱니다. 획득한 악기들(최대 4개)은 캐릭터 주변을 둥둥 떠다니며 QWER 방향과 1:1로 매칭되어 스스로 연주됩니다.
* **적 몬스터 (음표 군단):** 맵에 등장하는 모든 잡몹은 8분음표, 높은음자리표, 쉼표 등 다양한 '음표'를 형상화한 도트 캐릭터로 디자인하여 음악 테마를 직관적으로 전달합니다.
* **UI:** 레벨업/엘리트 보상 카드는 오케스트라 테마의 남색+금색 액자 프레임 아트를 사용하며, 모든 카드·메뉴 텍스트에 갈무리(Galmuri) 도트 폰트를 통일 적용합니다.

---

## 🤝 4. 팀원 개발 환경 & 협업 가이드 (Team Onboarding & Setup)

### 4.1 필수 개발 환경 (Required Environment)
* **Unity Editor Version:** `Unity 6 (6000.5.5f1)` — `ProjectSettings/ProjectVersion.txt` 기준
* **권장 IDE:** Visual Studio 2022 / Rider / VS Code
* **버전 관리 시스템:** Git + Git LFS (Large File Storage)
* **에셋 직렬화 설정:** `Force Text` (프로젝트 세팅 ➡️ Editor ➡️ Asset Serialization = Force Text)

---

### 🤖 4.2 Unity MCP (Model Context Protocol) 연동 및 설정 방법

본 프로젝트는 AI 에이전트(Antigravity / Cursor / Claude Code 등)가 유니티 에디터와 직접 통신하여 스크립트 컴파일, 콘솔 로그 감지, 씬 저장을 자동화할 수 있도록 **Unity MCP Server** 환경을 연동하여 개발 중입니다.

📺 **참고 튜토리얼 영상:** [YouTube Unity MCP 연동 가이드](https://www.youtube.com/watch?v=5nEUSJKgfzM)

#### 1단계: 유니티 에디터 플러그인 확인
* 프로젝트 오픈 시 `Assets/Plugins/MCP` (또는 MCP 패키지)가 포함되어 웹소켓 백그라운드 서버가 실행됩니다.

#### 2단계: AI 클라이언트 MCP 설정 (`mcp_config.json` / `antigravity.json`)
팀원의 AI 에이전트 설정 파일에 아래 `unityMCP` 서버 설정을 추가합니다:

```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "npx",
      "args": [
        "-y",
        "@antigravity/unity-mcp"
      ]
    }
  }
}
```

#### 3단계: 연동 검증
1. 유니티 에디터에서 프로젝트를 엽니다.
2. AI 에이전트에게 `read_console` 또는 `refresh_unity`를 호출하도록 하여 정상 연결(Connected)을 확인합니다.

#### 주요 제공 MCP 툴셋:
* `refresh_unity`: 유니티 에디터 스크립트 강제 compilation & 도메인 리로드
* `read_console`: 유니티 런타임 콘솔 에러/경고 실시간 수집 및 로그 판독
* `manage_scene`: Active Scene (`Gameplay.unity`) 저장 및 씬 제어
* `execute_code`: 유니티 에디터 내 C# 정적 테스트 코드 실시간 파이프라인 실행

---

### 4.3 씬(Scene) 작업 & 충돌 방지 지침
* **메인 게임 씬:** `Assets/Scenes/Gameplay.unity`
* **씬 충돌 방지 규칙:**
  1. `Gameplay.unity` 씬 자체를 여러 팀원이 동시에 수정하여 커밋하면 YAML 충돌이 일어납니다.
  2. UI, 몬스터, 오브젝트 작업 시 씬을 직접 건드리지 말고 **`Assets/Prefabs/` 폴더 내 프리팹(Prefab)으로 만든 뒤 작업**하세요(자세한 내용은 `PREFAB_GUIDE.md` 참고).
  3. 기능 개발 시 `feature/기능명` 브랜치를 생성하여 개발 후 `PR (Pull Request)`을 통해 `master` 브랜치로 머지합니다.

---

## 🚀 5. CI/CD — Unity GitHub Actions

`master`, `ci/**`, `feat/**` 브랜치 push 및 `master` 대상 PR마다 `.github/workflows/unity-ci.yml`이 자동 실행되어 **Windows / Linux / macOS 3개 플랫폼을 병렬(matrix)로 빌드**합니다.

* **워크플로 파일:** `.github/workflows/unity-ci.yml`
* **빌드 엔진:** [game-ci/unity-builder](https://github.com/game-ci/unity-builder)
* **활성화(라이선스) 시크릿 설정 (필수, 저장소 관리자 1회 설정):** GitHub 저장소 `Settings → Secrets and variables → Actions`에 아래 시크릿 등록
  * `UNITY_LICENSE` — Personal 라이선스 `.ulf` 파일 전체 내용 ([발급 방법: GameCI 공식 문서](https://game.ci/docs/github/activation/))
  * `UNITY_EMAIL` / `UNITY_PASSWORD` — Unity 계정 이메일/비밀번호 (구글 로그인 계정은 [id.unity.com](https://id.unity.com) 보안 설정에서 별도 비밀번호를 먼저 생성해야 함)
* **결과물:** 성공 시 각 플랫폼별 빌드가 `Build-StandaloneWindows64` / `Build-StandaloneLinux64` / `Build-StandaloneOSX` 아티팩트로 Actions 실행 결과 페이지에 7일간 보관됩니다.
* **Unity Library 캐시:** 플랫폼·에디터 버전·패키지 매니페스트 해시 기준으로 캐싱되어 반복 빌드 속도를 단축합니다.
