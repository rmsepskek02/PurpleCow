# AGENTS.md

이 문서는 프로젝트 내 모든 문서의 인덱스입니다.
새로운 문서가 추가될 때마다 이 문서에 함께 등록합니다.

---

## 루트 문서

| 문서 | 경로 | 설명 |
|------|------|------|
| CLAUDE.md | `/CLAUDE.md` | Claude Code 행동 지침. 작업 원칙, git 규칙, 작업 전 확인 절차 정의 |
| AGENTS.md | `/AGENTS.md` | 프로젝트 내 모든 문서의 인덱스 (현재 문서) |

---

## Docs 문서

| 문서 | 경로 | 설명 |
|------|------|------|
| TaskRules.md | `Assets/_Project/Docs/TaskRules.md` | task 문서 작성 규칙. 폴더 구조, 작업 흐름, research/plan 문서 형식 정의 |
| DevRules.md | `Assets/_Project/Docs/DevRules.md` | 개발 에이전트 전용 규칙. 코딩 원칙, git 규칙, 구현 방식 정의 |
| ProjectStatus.md | `Assets/_Project/Docs/ProjectStatus.md` | 현재 프로젝트 상태. 작업 완료마다 업데이트 |
| ProjectHistory.md | `Assets/_Project/Docs/ProjectHistory.md` | 작업 히스토리 누적 기록 |
| AIFailures.md | `Assets/_Project/Docs/AIFailures.md` | AI 실패 사례 및 재발 방지 기록 |
| UIRules.md | `Assets/_Project/Docs/UIRules.md` | UI 시스템 구현 규칙. Canvas 구조, 해상도 대응, Safe Area, 패널 제어, 애니메이션, 버튼 피드백, 성능 최적화 정의 |

---

## 에이전트

| 에이전트 | 파일 | 도구 | 역할 |
|----------|------|------|------|
| 개발 | `.claude/agents/dev.md` | Read, Edit, Write, Bash, Glob, Grep | C# 스크립트 생성/수정, 프로젝트 설정 변경 |
| QA | `.claude/agents/qa.md` | Read, Glob, Grep, Bash | 코드 리뷰, 버그 탐지, 요구사항 검증 |
| 디자인 | `.claude/agents/design.md` | Read, Write, Glob, Grep | 에셋 프롬프트 작성 지원, 에셋 관리 |
| 문서 | `.claude/agents/docs.md` | Read, Edit, Write, Glob, Grep | 문서 작성/관리, AGENTS.md 인덱스 유지 |

---

## Task 문서

task 문서는 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH:MM_작업요약/` 경로에 생성됩니다.
자세한 규칙은 [TaskRules.md](Assets/_Project/Docs/TaskRules.md)를 참고하세요.

### 2026-06-30

| 폴더 | research | plan | 설명 |
|------|----------|------|------|
| `02-30_Core시스템구현` | O | O | Singleton, ObjectPool, GameManager, InputHandler 구현 |
| `10-00_Ball시스템구현` | O | O | BallLauncher, Ball, BallData, ObjectPool 연동 구현 |
| `14-00_Monster시스템구현` | O | O | MonsterBase, MonsterData, WaveManager 구현 |
| `18-00_Skill시스템구현` | O | O | SkillManager, BallSkillBase, PassiveSkillBase, 스킬 5종+7종 구현 |
| `20-00_UI시스템구현` | O | O | UIManager, HUDPanel, ResultPanel, SkillSelectionPanel, SkillCardUI, SkillFactory 구현 계획 |
| `HH-MM_UI재작업` | O | O | UI 전체 재작업. UIRules 위반 수정(CanvasGroup, DOTween, UIButton), 미구현 8종 신규 생성(CharacterManager, MonsterHpBar, DamageTextFx 등) |
