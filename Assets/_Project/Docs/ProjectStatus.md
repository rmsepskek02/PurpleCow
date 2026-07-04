# ProjectStatus.md

이 문서는 현재 프로젝트의 상태를 기록합니다. 작업 완료마다 업데이트합니다.

---

## 현재 상태 (2026-07-04 기준)

**단계**: 실제 플레이 테스트 진행 중 — 몬스터 시스템 개편(4종 랜덤 웨이브, 정사각형 그리드 점유 체크)의 선행 작업인 배경 격자 정사각형 보정까지 실기기 검증 완료

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
- [x] WaveData → WaveTableData 리팩토링: `Assets/_Project/Data/`에 asset이 과다하다는 지적에 따라 `WaveData.cs` + `WaveData_Wave1~20.asset`(20개 개별 asset) 구조를 `WaveTableData.cs`(`WaveEntry` 리스트를 담는 단일 ScriptableObject) 구조로 통합, `WaveManager`/`MonsterSetupEditor`/`SceneSetupEditor` 함께 수정. task 문서(research.md/plan.md) 없이 예외적으로 바로 구현 진행, main에 직접 커밋(`9c188a8`). 이후 사용자가 로컬 Unity에서 `PurpleCow/Setup/Monster System Setup` → `PurpleCow/Setup/Scene Setup`을 실행해 `WaveTableData.asset` 생성 및 `SampleScene.unity`의 `WaveManager._waveTable` 참조 재연결 완료, 커밋(`ceeb9e2`)/푸시 완료. 오케스트레이터가 직접 검증: `WaveTableData.asset`의 웨이브 1~20 스폰 데이터가 의도한 진행과 정확히 일치, 씬의 구 `_waveDatas` 필드 완전 제거 및 `_waveTable` 단일 참조로 정상 교체, `Assets/_Project/Data/` asset 개수 35개 → 16개로 감소 확인
- [x] 볼 천장 이탈 버그 수정 (`2026-07-03/12-48_ball-ceiling-wall-fix`): 실 플레이 테스트 중 볼이 맵 외곽에서 튕기지 않고 맵 밖(천장)으로 나가버리는 버그 발견, research.md로 원인(`SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단 벽 콜라이더 생성 코드 자체가 없어 위쪽만 완전히 뚫려 있음)을 특정하고 plan.md 사용자 승인 후 dev 에이전트가 `Wall_Top` 콜라이더 생성 1줄 추가(커밋 `345ae29`). 사용자가 로컬 Unity에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해 `SampleScene.unity`에 `Wall_Top` 반영 완료, 실제 플레이 테스트로 천장 반사 정상 동작 검증 완료
- [x] 배경/해상도 대응 (`_Task/2026-07-03/12-30_background-resolution-fix`): 다양한 Android 기기 종횡비 대응을 위해 배경 스케일 방식이 Cover → Contain → Stretch 순으로 시행착오를 거쳐 Stretch로 최종 확정됐고, 카메라 시야를 기기별로 동적 확장하는 `CameraFitter`는 도입 후 "Wall이 화면에서 차지하는 비율이 orthographic size와 무관하게 항상 일정하다"는 사실이 수학적으로 밝혀지며 불필요해져 삭제됐으며, 대신 `WallFitter`를 도입해 벽/Ground/LaunchPoint를 배경 격자 그림에 비례해 연동시키고 `_zoomFactor` 공통 확대 배율과 Inspector 실시간 반영(`[ExecuteAlways]`/`OnValidate`) 기능을 추가한 뒤 여러 차례 실기기 테스트로 벽 기준값을 최종 확정 완료
- [x] 볼 궤적 조준 개선 (`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`): 궤적 프리뷰 상시 표시 전환, 스크린→월드 변환을 통한 조준 정확도 보정, 궤적 프리뷰 색상/크기 원본 레퍼런스 맞춤 조정, 상대 드래그 → 절대 조준 모델 전환, 터치 시작 폴링 누락 버그 수정까지 5개 이슈를 모두 구현 완료했고, 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인함
- [x] 배경 격자 정사각형 보정 (`_Task/2026-07-04/16-40_background-square-grid-fix`): 몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기 기반 정사각형 그리드 점유 체크)의 선행 작업으로, 배경 텍스처(`Background_1_Stage.png`) 격자가 140×85px(비정사각형, 약 1.65:1)로 그려져 몬스터/블록 스프라이트 및 원본 게임 실제 격자(정사각형)와 어긋나던 문제를 `BackgroundFitter.cs`/`WallFitter.cs`의 스케일 계산식을 "텍스처 고유 비율 보정(`_cellAspectCorrection ≈ 1.647`) + 격자 영역 기준 균일 Cover 배율" 2단계 공식으로 교체해 해결(신규 필드 주입은 기존 `SceneSetupEditor.cs`를 건드리지 않고 별도 `BackgroundGridFitSetupEditor.cs`로 분리, 리소스 PNG는 수정하지 않음). 사용자가 로컬에서 여러 실기기(Galaxy Note 10 등)로 검증해 격자가 정사각형으로 정상 렌더링됨을 확인했고, 새 계산식이 필요로 하는 배율이 기존보다 커진 것을 반영해 `_zoomFactor` 기본값을 1.3 → 0.5(에디터 미리보기는 0.6)로 최종 조정, 커밋 완료

**진행 중**
없음

**다음 작업 순서**
1. 몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기 기반 정사각형 그리드 점유 체크) research.md 작성 착수
2. 실제 플레이 테스트를 진행하면서 발견되는 문제를 하나씩 수정해나가는 단계 (별도 큰 항목 사전 나열보다 테스트 중 발견되는 버그/밸런스/UI 이슈를 그때그때 task로 정리해 처리)

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
