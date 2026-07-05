# ProjectStatus.md

이 문서는 현재 프로젝트의 상태를 기록합니다. 작업 완료마다 업데이트합니다.

---

## 현재 상태 (2026-07-05 기준)

**단계**: 실제 플레이 테스트 진행 중 — 볼-볼 물리 충돌 방지, 볼 조준 방향 Y좌표 하한 제한까지 사용자 실기기/로컬 검증 완료(단, 아직 main 미병합 브랜치 상태, 병합 대기 중), 볼 궤적 프리뷰 고리(Ring) 점선화+회전 효과는 구현 완료했으나 최종 시각 확인은 사용자 로컬 Unity 테스트 대기 중

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
- [x] PDF 스펙 대비 문서 재감사: `MonsterRules.md`/`UIRules.md`를 공식 요구사항 PDF와 재대조. 첫 시도(커밋 `d5a3b06`)는 "문서에 구현 상태만 기록"하는 방향이었는데, 실제로 필요했던 건 "문서에 적힌 규칙 자체가 PDF 목표와 모순되는지" 감사였음이 드러나 사용자가 되돌림(`16ec529`). 이후 올바른 기준(규칙-목표 모순 여부만 검사)으로 재감사했으나 확실한 모순은 발견되지 않아 문서 수정 없이 결과만 보고, PR #12로 main 머지 완료
- [x] PrismPanel(융합 시스템 잔재) 제거: `UISetupEditor.cs`의 Canvas_Panel 생성 목록에서 이름만 있고 실제 로직이 전혀 없던 빈 스텁 패널 `PrismPanel`을 발견, PDF 스펙의 "구현 제외 항목"인 융합 시스템 관련 잔재로 판단해 삭제 확정. `panelNames` 배열에서 `"PrismPanel"` 제거, `SampleScene.unity`에 이미 생성돼 있던 빈 GameObject도 YAML에서 직접 제거, `UIRules.md`의 Canvas 계층도에서도 해당 줄 삭제(`LevelUpPanel`/`PausePanel`/`BallLevelUpPanel`은 그대로 유지). PR #12로 main 머지 완료
- [x] 볼 궤적 프리뷰 고리(Ring) 점선화 + 회전 효과 (`_Task/2026-07-05/11-20_trajectory-ring-dash-rotate`): 2차 충돌 지점 레드닷을 감싸는 고리(`_hitRing`)가 완전한 실선이던 것을, 원본 게임 레퍼런스처럼 끊어진 점선 + 조준 여부와 무관하게 항상 시계방향으로 회전하는 효과로 재구현. 궤적선 색상 등 Inspector 조절 가능화는 기존 코드(`_lineColor` 등 6개 `[SerializeField]` 필드)로 이미 완료돼 있음을 확인해 별도 구현은 불필요했음. 구현 과정에서 시행착오를 거쳤음 — (1) 텍스처 반복(타일링) 방식으로 10개 점선을 목표했으나 실제로는 2개로 보이는 문제(원인 미확인, 원격 환경에 Unity가 없어 검증 불가) → (2) `LineRenderer.colorGradient`(alphaKeys 8개) 방식으로 교체해 정확히 4개를 보장했으나, 사용자가 보내준 실제 레퍼런스 이미지(`targetUI/circle.jpg`) 대조 결과 경계가 과도하게 흐려지는 근본적 한계 확인 → (3) 텍스처 타일링 방식으로 재전환하되 목표를 4개로 조정하고, `loop = true` 대신 원을 명시적으로 닫는 정점(`CIRCLE_SEGMENTS + 1`개, explicit close)을 추가하는 방식으로 재구현. 회전 속도는 `[SerializeField] private float _ringRotationSpeed = 90f;`(deg/sec)로 Inspector 노출. **이 최종(3번) 버전이 실제로 정확히 4개의 호로 보이는지는 사용자가 아직 로컬 Unity에서 재확인하지 않은 상태 — "구현 완료, 최종 시각 확인은 사용자 로컬 테스트 대기 중"으로 구분**. PR #12로 main 머지 완료(구현 코드 기준, 시각 확인은 별개)
- [x] 볼-볼 물리 충돌 방지 (`_Task/2026-07-05/16-40_ball-ball-collision-fix`): `Ball`/`Wall`/`Ground`/`Monster`가 전부 Default 레이어(0)에 있고 `Physics2DSettings.asset`의 레이어 충돌 매트릭스가 Default-Default 충돌을 허용해, 여러 볼이 동시에 존재할 때 물리적으로 서로 튕겨나가던 버그를 발견(`Ball.OnCollisionEnter2D`의 태그 분기는 물리 반응 이후 호출되는 콜백이라 코드로는 막을 수 없었음). 전용 "Ball" Physics2D 레이어를 신설(`BallSetupEditor.cs`에 `AddBallLayer()`/`AssignBallPrefabLayer()` 신규 메서드 추가, `PurpleCow/Setup/Ball System Setup` 메뉴 실행 시 자동 처리)하고 `BallLauncher.Awake()`에서 `Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true)` 1회 호출로 볼-볼 충돌만 전역 비활성화, Wall/Ground/Monster는 Default 레이어 그대로 유지. **사용자가 로컬에서 `PurpleCow/Setup/Ball System Setup` 메뉴를 재실행한 뒤 실제 플레이 테스트로 "볼 발사 정상 동작"과 "볼-볼 물리 충돌 방지(서로 안 튕김)" 둘 다 검증 완료 확인함.** 다만 **아직 main에 병합되지 않고 현재 브랜치(`claude/project-review-bugs-qq65d1`)에만 커밋된 상태 — main 병합 대기 중**
- [x] 볼 조준 방향 Y좌표 하한 제한 (`_Task/2026-07-05/18-30_aim-direction-y-clamp`): 실제 플레이 테스트 중 사용자가 "볼 궤도를 설정할 때 일정 y좌표 밑으로는 설정하지 못하게 하자"고 요청. 처음엔 기준점을 "격자타일 밑변"(`WallFitter`가 기기별로 동적 재계산하는 `Ground` Transform 위치)으로 논의해 research.md까지 작성했으나, `WallFitter._ground`가 private이라 `InputHandler`에서 접근하려면 씬 참조 연결이 추가로 필요하다는 복잡성이 확인됨. 이후 사용자가 방향을 단순화 — 이미 존재하는 몬스터 바닥 도달 게임오버 판정 기준선(`WaveManager._bottomBoundaryY`)을 재사용하기로 확정, `WaveManager`가 이미 싱글톤이라 씬 참조 연결/에디터 스크립트 수정이 전혀 필요 없어짐. `WaveManager.cs`에 `public float BottomBoundaryY => _bottomBoundaryY;` 프로퍼티를 추가하고, `InputHandler.ComputeAimDirection()`에서 터치 위치를 월드 좌표로 변환한 직후 `worldPos.y = Mathf.Max(worldPos.y, WaveManager.Instance.BottomBoundaryY);`로 clamp한 뒤 발사 지점 기준 방향을 계산하도록 수정(이 clamp는 조준 가능한 목표 지점 범위만 제한하며, 발사된 볼이 물리 반사로 기준선 아래까지 내려가는 것 자체를 막는 장치는 아님). `TrajectoryPreview.cs`는 이미 clamp된 `BallLauncher.Instance.LaunchDirection`을 그대로 받아 그리므로 별도 수정 불필요. `GameplayMechanics.md` 섹션 1에도 이 규칙을 문서화. **사용자가 로컬 Unity에서 직접 플레이 테스트하여 정상 동작 확인 완료.** 다만 **아직 main에 병합되지 않고 현재 브랜치에만 커밋된 상태 — main 병합 대기 중**

**진행 중**
없음

**다음 작업 순서**
1. 볼-볼 물리 충돌 방지 + 볼 조준 방향 Y좌표 하한 제한 브랜치(`claude/project-review-bugs-qq65d1`)를 main에 병합
2. 볼 궤적 프리뷰 고리(Ring) 점선 호 개수(목표 4개)가 실제로 의도대로 보이는지 사용자 로컬 Unity 최종 시각 확인
3. 몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기 기반 정사각형 그리드 점유 체크) research.md 작성 착수
4. 실제 플레이 테스트를 진행하면서 발견되는 문제를 하나씩 수정해나가는 단계 (별도 큰 항목 사전 나열보다 테스트 중 발견되는 버그/밸런스/UI 이슈를 그때그때 task로 정리해 처리)

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
