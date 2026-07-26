# 🎼 Conductor's Symphony

## 1. 개요 (Overview)

* **프로젝트명:** Conductor's Symphony
* **플랫폼:** WebGL / PC Desktop (Unity 6 URP 2D Pipeline)
* **장르:** 리듬 + 뱀파이어 서바이버 (로그라이트)
* **핵심 컨셉:** 지휘자가 된 캐릭터가 최대 4개의 악기를 조율하며, 몰려오는 음표 몬스터들을 연주(공격)로 물리치는 탑뷰 생존 게임.
* 📖 **[상세 개발 기록 및 기술/기획 의도 문서 (DOCUMENTATION.md)](file:///c:/Users/admin/Desktop/My%20project/DOCUMENTATION.md)**
* 🔧 **[매니저 아키텍처 리팩토링 기록 문서 (REFACTORING.md)](file:///c:/Users/admin/Desktop/My%20project/REFACTORING.md)**

---

## 2. 아트 및 비주얼 디자인 (Visual Design)

* **화풍 (Art Style):** 직관적이고 고전적인 매력이 있는 **2D 도트(Pixel Art) 그래픽**으로 제작합니다. 이를 통해 후반부로 갈수록 피아식별이 안 되는 시각적 피로도와 난잡함을 방지합니다.
* **캐릭터 및 무기 연출:** 플레이어 캐릭터는 턱시도나 연주복을 입은 '지휘자' 형태를 띱니다. 획득한 악기들(최대 4개)은 캐릭터 주변을 둥둥 떠다니며 스스로 연주되거나, '브레멘 음악대'처럼 캐릭터의 뒤를 졸졸 따라다니는 형태로 디자인합니다.
* **적 몬스터 (음표 군단):** 맵에 등장하는 모든 잡몹은 8분음표, 높은음자리표, 쉼표 등 다양한 '음표'를 형상화한 도트 캐릭터로 디자인하여 음악 테마를 직관적으로 전달합니다.

---

## 3. 핵심 시스템: 4방향 콤팩트 리듬 노트 (Core System)

* **직관적인 UI:** 지휘자 아바타 안쪽(`Q/R=100, W/E=80` 정밀 안착) 부채꼴 가이드 UI를 사용하며, 지휘자 머리 위 3D 월드 플로팅 타격 텍스트 팝업(`PERFECT!`, `GREAT!`, `MISS`)으로 화면 시야를 완벽히 확보합니다.
* **조작계:** 4방향 외곽에서 중앙의 캐릭터(판정선)를 향해 다가오는 노트를 왼손(`Q, W, E, R`)으로 연주하고, 오른손(`방향키`)으로는 이동을 전담합니다.
* **악기 시스템:** 10종 악기 중 4개를 조합하여 나만의 오케스트라 덱을 빌딩합니다.

---

## 4. 전투 흐름 및 레벨 디자인 (Gameplay & Level Design)

### 4.1. 일반 웨이브 (잡몹전)
* 마우스 사용 없이 오토 타겟팅으로 적을 공격합니다.
* 오른손 이동의 부담이 적은 상태에서 정박자 위주의 리듬 연주(왼손)에 집중하며 다수의 음표 몬스터를 시원하게 쓸어버리는 쾌감을 제공합니다.

### 4.2. 보스전 (협주곡 클라이맥스)
* **컨트롤의 무게중심 이동:** 2분 주기 엘리트 보스 등장 시 360도 탄막과 조준 탄막이 쏟아집니다. 익숙해진 왼손 연주를 무의식적으로 유지하면서 오른손으로 정교한 회피 기동을 해내야 하는 '복합적 컨트롤'을 요구합니다.

---

## 🤝 5. 팀원 개발 환경 & 협업 가이드 (Team Onboarding & Setup)

### 5.1 필수 개발 환경 (Required Environment)
* **Unity Editor Version:** `Unity 6 (6000.0.x LTS)` 권장 (또는 `Unity 2022.3 LTS`, Universal Render Pipeline 2D)
* **권장 IDE:** Visual Studio 2022 / Rider / VS Code
* **버전 관리 시스템:** Git + Git LFS (Large File Storage)
* **에셋 직렬화 설정:** `Force Text` (프로젝트 세팅 ➡️ Editor ➡️ Asset Serialization = Force Text)

---

### 🤖 5.2 Unity MCP (Model Context Protocol) 연동 및 설정 방법

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

### 5.3 씬(Scene) 작업 & 충돌 방지 지침
* **메인 게임 씬:** `Assets/Scenes/Gameplay.unity`
* **씬 충돌 방지 규칙:**
  1. `Gameplay.unity` 씬 자체를 여러 팀원이 동시에 수정하여 커밋하면 YAML 충돌이 일어납니다.
  2. UI, 몬스터, 오브젝트 작업 시 씬을 직접 건드리지 말고 **`Assets/Prefabs/` 폴더 내 프리팹(Prefab)으로 만든 뒤 작업**하세요.
  3. 기능 개발 시 `feature/기능명` 브랜치를 생성하여 개발 후 `PR (Pull Request)`을 통해 `master` 브랜치로 머지합니다.