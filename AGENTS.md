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
| UIRules.md | `Assets/_Project/Docs/UIRules.md` | UI 시스템 구현 규칙. Canvas 구조, 해상도 대응, Safe Area, 패널 제어, 애니메이션, 버튼 피드백, 성능 최적화, 데미지 텍스트, 몬스터 HP바, 캐릭터 HP/XP/레벨, 궤적 프리뷰 시각 규칙 정의 |
| PlayerActiveSkillDesign.md | `Assets/_Project/Docs/PlayerActiveSkillDesign.md` | 플레이어 액티브 스킬 시스템 기획. 버서크/분신 효과, FIFO 재발사 규칙, UI 명세 정의 |
| GameplayMechanics.md | `Assets/_Project/Docs/GameplayMechanics.md` | 게임 내 알고리즘/메커닉 스펙 문서. 볼 발사/궤도 시스템(조준, 프리뷰, 귀환, 재발사), 캐릭터 조준 연동 시각 표현(좌우 반전, 무기/머리 회전) 등 확정된 게임플레이 메커닉을 계속 추가 기록. 현재 구현과의 차이(TODO) 포함 |
| MonsterRules.md | `Assets/_Project/Docs/MonsterRules.md` | 몬스터 시스템 통합 규칙 문서. 스폰/전진 메커닉, 몬스터 종류/스탯, HP/상태이상 처리, 웨이브 시스템 정의 |
| TODO.md | `Assets/_Project/Docs/TODO.md` | 게임 다듬기(Polish) 작업 백로그. 논의를 마쳤으나 아직 구현하지 않은 항목들의 현재 상태/확정된 목표/비고 기록 |

---

## 참고 자료

이 프로젝트는 원본 게임(통통 디펜스: 핀볼 마스터)을 카피하는 채용 과제입니다. 아래는 문서가 아닌 원본 참고 자료(스펙 PDF, 레퍼런스 이미지) 인덱스입니다.

| 자료 | 경로 | 설명 |
|------|------|------|
| PurpleCow_클라이언트_채용과제.pdf | `PurpleCow_클라이언트_채용과제.pdf` (레포 루트) | 공식 요구사항 스펙 PDF. 원본 게임(통통 디펜스: 핀볼 마스터) 1스테이지를 카피하는 채용 과제 요구사항. 필수 구현 항목/구현 제외 항목이 명시되어 있음. 코드/UI 작업 전 최우선 기준 |
| targetUI/ | `Assets/_Project/Docs/targetUI/` | 원본 게임(통통 디펜스: 핀볼 마스터, CookApps, com.cookapps.bouncedefense) 실제 플레이 캡처 이미지 6장. UI 레이아웃/게임플레이 메커닉 확인용 레퍼런스 |

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

task 문서는 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH-MM_작업요약/` 경로에 생성됩니다.
개별 task 폴더 목록은 이 문서에서 별도로 관리하지 않으며, 필요할 때 해당 경로를 직접 탐색하세요.
작성 규칙은 [TaskRules.md](Assets/_Project/Docs/TaskRules.md)를 참고하세요.
