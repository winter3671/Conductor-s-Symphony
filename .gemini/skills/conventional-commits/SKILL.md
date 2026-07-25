---
name: conventional-commits
description: Inspects git status and diff, categorizes changed files into feat/refactor/docs/fix, recommends Conventional Commit messages in Korean, and provides copy-paste ready git add and git commit commands for split commits.
---

# Conventional Commits Skill for Antigravity

이 스킬은 Antigravity가 현재 로컬 깃 저장소의 변경 사항을 직접 분석하여 **Conventional Commits 규격 (`feat:`, `refactor:`, `docs:`, `fix:`, `chore:`)**에 부합하는 정교한 한글 커밋 메시지와 **분할 커밋용 `git add` / `git commit` 명령어 세트**를 자동 추천하도록 유도합니다.

---

## 🛠️ 실행 워크플로우 (Workflow)

사용자가 깃 커밋 메시지 추천을 요청할 때 다음 순서대로 실행합니다:

1. **깃 변경 상태 정밀 조사 (Git Inspection):**
   * `git status -s` 실행하여 변경/신규 파일 목록 파악
   * `git diff --stat` 실행하여 파일별 수정 및 추가 라인 수 분석

2. **변경 사항 카테고리 분류 (Categorization):**
   * **`feat:`** 신규 기능 추가, UI 패널 구현, 오디오 키음 합성 엔진, 새 아키텍처 구축
   * **`refactor:`** 기존 로직 개선, 비트 패턴 재설계, BPM 템포 조율, 밸런싱 다이어트
   * **`docs:`** `DOCUMENTATION.md`, `README.md`, `walkthrough.md` 등 문서화 작업
   * **`fix:`** 버그 수정, 널 참조 처리, EventSystem 누락 해결, 피치 고정 조치
   * **`chore:`** Unity ProjectSettings, 패키지 설정 등 빌드 및 환경 변경

3. **추천 결과 제공 (Output Formatting):**
   * **단일 전체 커밋 추천 메시지**
   * **기능/분야별 분할 커밋 추천 메시지**
   * **복사-붙여넣기 전용 `git add <파일들>` 및 `git commit -m "..."` 명령어 블록**

---

## 💡 응답 예시 양식 (Response Template)

```markdown
### 📊 깃 상태 분석 결과
- **신규 파일:** `...`
- **수정된 파일:** `...`

---

### 📌 분할 커밋 가이드 (Copy & Paste Commands)

#### 1번째 커밋 (기능 구현 - feat)
\`\`\`bash
git add <관련 파일들>
git commit -m "feat: <기능 설명>"
\`\`\`

#### 2번째 커밋 (리팩토링 - refactor)
\`\`\`bash
git add <관련 파일들>
git commit -m "refactor: <개선 설명>"
\`\`\`

#### 3번째 커밋 (문서화 - docs)
\`\`\`bash
git add <문서 파일들>
git commit -m "docs: <문서 설명>"
\`\`\`
```
