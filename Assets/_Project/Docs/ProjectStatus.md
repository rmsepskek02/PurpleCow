# ProjectStatus.md

이 문서는 현재 프로젝트의 상태를 기록합니다. 작업 완료마다 업데이트합니다.

---

## 현재 상태 (2026-07-03 기준)

**단계**: 볼 발사 메커닉 재설계 완료 및 main 반영 — 실제 플레이 테스트 단계 진입

**완료된 작업**
- [x] 프로젝트 생성 (Unity 6000.3.10f1, Universal 2D URP, Android)
- [x] 폴더 구조 생성 (Scripts/Core, Ball, Monster, Skill, UI, Data, Util)
- [x] 에이전트 시스템 구축 (dev / qa / design / docs)
- [x] 문서 시스템 구축 (CLAUDE.md, AGENTS.md, DevRules.md, TaskRules.md 등)
- [x] 아키텍처 설계 확정
- [x] Core 시스템 task 문서 작성 (research.md + plan.md)
- [x] Inspector 연결 에디터 스크립트 자동화 완성 (LaunchPoint, SkillCard, HUD/Result/SkillSelection 패널, WaveData 스폰 데이터)
- [x] 런타임 버그 수정: InputHandler (New Input System), GameManager 자동 시작, 카메라 orthographic size 조정
- [x] 볼 발사 메커닉 재설계 (`2026-07-01/21-15_ball-launch-mechanics`): 터치 즉시 조준(`InputHandler.OnAimBegin`), 2단계 궤적 프리뷰(`TrajectoryPreview.cs`), 화면 하단 귀환 후 자동 재발사 사이클, 노말볼 5개+특수볼 최대 4종 로스터 모델(`BallLauncher`/`Ball`) 도입, 몬스터 하강을 볼 사이클에서 분리해 시간 연속 하강으로 재설계(`WaveManager`/`MonsterBase`), 냉동/슬로우 초 단위 전환. QA 검토로 발견된 Critical 2건(벽 반사 소진 시 로스터 볼 영구 이탈, `BallData.asset._maxBounces` 데이터 오류) + Major 1건(게임 종료 후 재발사 지속) 수정 완료, 이후 "로스터 볼은 벽 반사 횟수 무관하게 항상 순수 반사만 하고 Ground 충돌에서만 귀환"으로 최종 정정. PR #6으로 main 머지 완료
- [x] `UISetupEditor` 버그 수정: `CharacterHpBar`/`CharacterXpBar`의 `_slider`/`_levelText` 참조 연결 누락으로 인한 몬스터 처치 시 `NullReferenceException` 수정, PR #7로 main 머지 완료

**진행 중**
없음

**다음 작업 순서**
1. 실제 플레이 테스트를 진행하면서 발견되는 문제를 하나씩 수정해나가는 단계 (별도 큰 항목 사전 나열보다 테스트 중 발견되는 버그/밸런스/UI 이슈를 그때그때 task로 정리해 처리)

## 주요 기술 결정

| 항목 | 결정 | 이유 |
|------|------|------|
| 이벤트 시스템 | C# event | 단순성, 프로젝트 규모에 적합 |
| 매니저 패턴 | Generic Singleton | 단일 씬, DontDestroyOnLoad 불필요 |
| 데이터 | ScriptableObject (read-only) | Unity 표준, 인스펙터 편집 용이 |
| 오브젝트 풀 | Generic ObjectPool<T> | Ball/Monster/데미지텍스트 재사용 |
| 입력 | InputHandler (C# event 발행) | BallLauncher가 구독 |

## 리소스 현황

- `Assets/_Project/Resource/` 폴더에 과제 제공 리소스 배치 완료
- 실제 사용 시 임포트 설정 필요
