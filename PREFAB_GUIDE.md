# 🧩 Conductor's Symphony - 팀원용 프리팹(Prefab) 가이드 & 사용 설명서 (PREFAB_GUIDE.md)

본 문서는 **Conductor's Symphony** 프로젝트의 3인 협업 체계에 맞춰 구축된 **`Assets/Prefabs/`** 구조와 프리팹 활용법을 정리한 팀원용 가이드입니다.

---

## 📂 1. 프리팹 폴더 구조 및 담당자 매핑

| 폴더 경로 | 프리팹 이름 | 주요 포함 컴포넌트 | 담당자 |
|---|---|---|---|
| `Assets/Prefabs/Player/` | `Player.prefab` | `PlayerController`, `PlayerExperience`, `SpriteRenderer`, `CircleCollider2D`, `Rigidbody2D` | **[팀원 B/C]** |
| `Assets/Prefabs/Enemies/` | `EnemyMonster.prefab`<br>`BossMonster.prefab` | `EnemyMonster`, `BossMonster`, `SpriteRenderer`, `CircleCollider2D`, `Rigidbody2D` | **[팀원 C]** |
| `Assets/Prefabs/Instruments/`| `InstrumentOrbit.prefab` | `InstrumentOrbit`, `SpriteRenderer` | **[팀원 B/C]** |
| `Assets/Prefabs/Items/` | `ExpGem.prefab`<br>`EliteRewardChest.prefab` | `ExpGem`, `EliteRewardChest`, `SpriteRenderer`, `CircleCollider2D` | **[팀원 B/C]** |

---

## 🛠️ 2. 프리팹 수정 및 작업 수칙 (CRITICAL)

### ⚠️ 규칙 1: 씬(`Gameplay.unity`)에서 직접 수정 금지!
* 씬 뷰(Hierarchy)의 오브젝트를 직접 건드리면 `Gameplay.unity` 파일에 수많은 YAML 변경 사항이 생겨 **Git 병합 충돌의 원인**이 됩니다.
* **올바른 수정 방법:**
  1. Project 창에서 해당 `.prefab` 파일(예: `Assets/Prefabs/Player/Player.prefab`)을 **더블클릭**합니다.
  2. **프리팹 전용 독립 화면(Prefab Mode)**에 들어간 상태에서 스프라이트 교체, 스크립트 수치 조정을 진행합니다.
  3. 좌측 상단의 `<` (뒤로가기) 버튼을 눌러 나온 뒤 저장(`Ctrl + S`)합니다.
  * 🎉 **결과:** `Gameplay.unity` 파일은 단 1바이트도 수정되지 않고, 오직 해당 `.prefab` 파일만 깔끔하게 커밋 대상으로 잡힙니다!

---

### ⚠️ 규칙 2: 씬에서 실수로 수정한 경우 `Apply All` 필수!
* 실수로 하이러키 씬 화면에서 인스펙터 수치를 바꾼 경우:
  1. 해당 오브젝트의 인스펙터 우측 상단 **`Overrides`** 드롭다운 버튼 클릭.
  2. **`Apply All`** 버튼을 누르면 변경된 수치가 오리지널 `.prefab` 에셋 파일로 적용되며 씬이 다시 깨끗해집니다.

---

### 💡 규칙 3: 프리팹 바리안트(Prefab Variant) 활용법
* 잡몹이나 무기의 외형/체력만 다른 버전을 새로 만들 때:
  1. 원본 프리팹(예: `EnemyMonster.prefab`) 우클릭 ➡️ `Create` ➡️ **`Prefab Variant`** 선택.
  2. 파생된 프리팹(예: `Enemy_QuarterNote.prefab`)의 체력, 스프라이트만 따로 설정.
  * 🎉 **결과:** 원본 구조가 유지되므로 공통 기능 수정 시 모든 바리안트에 자동 반영됩니다.

---

## 🚀 3. 새로 프리팹을 추가할 때
유니티 상단 메뉴의 **`Tools` ➡️ `Generate Team Prefabs`**를 누르면 언제든지 기본 템플릿 프리팹이 자동 생성 및 동기화됩니다.
