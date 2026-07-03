# ProjectHistory.md

이 문서는 프로젝트의 작업 히스토리를 누적 기록합니다. 날짜와 함께 작업 내용을 순서대로 기록합니다.

---

## 2026-06-30

### 프로젝트 환경 구성
- Unity 6000.3.10f1 + Universal 2D URP 프로젝트 생성
- 리소스 복사 (Ball 6종, Monster 8종, Character, Passive 아이콘 7개, Background)
- Assets/_Project 폴더 구조 생성

### 문서 구조 확립
- CLAUDE.md: 모든 에이전트 공통 행동 지침
- AGENTS.md: 전체 문서 인덱스
- DevRules.md: 개발 에이전트 전용 규칙 (네이밍 컨벤션, Unity 규칙 포함)
- TaskRules.md: task 문서 작성 규칙

### 에이전트 구성
- dev, qa, design, docs 4개 에이전트 생성 (project memory)
- Claude가 orchestrator 역할 수행

### 아키텍처 설계 확정
- 매니저 패턴 + 인터페이스 + C# event + ScriptableObject + 오브젝트 풀링
- 네이밍 컨벤션 확정 (PascalCase, _camelCase 등)
- Unity 규칙 확정 (SerializeField, 싱글톤, 생명주기 등)
- 스크립트 폴더 구조 확정

### 프로젝트 셋업 및 아키텍처 설계

**에이전트 시스템 구축**
- Claude를 orchestrator로 하는 4-에이전트 구조 확립 (dev / qa / design / docs)
- 각 에이전트 파일 생성: `.claude/agents/{dev,qa,design,docs}.md`
- 에이전트별 project memory 기록 규칙 추가
- orchestrator 전용 에이전트 시도 → 실패 → Claude가 직접 orchestrator 역할 수행으로 변경

**문서 시스템 구축**
- CLAUDE.md: 공통 행동 지침 (작업 전 확인, task 문서 흐름, 외과적 변경, orchestrator 역할)
- AGENTS.md: 전체 문서 인덱스
- DevRules.md: 개발 에이전트 전용 규칙 (네이밍, Unity 규칙, git 규칙)
- TaskRules.md: task 문서 작성 규칙 (폴더 구조, research/plan 형식)
- ProjectStatus.md / ProjectHistory.md / AIFailures.md 신규 생성
- TaskRules.md 폴더명 포맷 HH:MM → HH-MM 수정 (Windows 콜론 제약)

**아키텍처 결정사항**
- Manager Pattern + Interfaces + C# event + ScriptableObject + Object Pooling 채택
- EventBus 미사용, C# event만 사용 (단순성 우선)
- Generic Singleton, DontDestroyOnLoad 미사용 (단일 씬)
- SerializeField private, public 변수 미노출
- MonoBehaviour lifecycle: Awake(GetComponent), Start(외부참조), OnEnable/OnDisable(이벤트)
- ScriptableObject: 원본 read-only, 런타임 변경은 별도 클래스
- Object Pooling 대상: Ball, Monster, 데미지 텍스트
- 구현 방식 B 채택: 시스템별 설계 → 구현 반복

**스크립트 폴더 구조 확정**
```
Assets/_Project/Scripts/
├── Core/
├── Ball/
├── Monster/
├── Skill/Base/, Skill/Active/, Skill/Passive/
├── UI/
├── Data/
└── Util/
```

**구현 순서 확정**
1. Core → 2. Ball → 3. Monster → 4. Skill → 5. UI

**AI 실패 기록**
- orchestrator background agent 실패 (완료 알림 수신 불가)
- Claude가 docs 에이전트를 거치지 않고 DevRules.md 직접 수정
- Korean 폴더명 PowerShell/Bash 인코딩 오류
- settings.json orchestrator 설정 시 Claude 도구 제한 문제

### Core 시스템 task 문서 작성

- `Assets/_Project/Docs/_Task/2026-06-30/02-30_Core시스템구현/research.md` 생성
- `Assets/_Project/Docs/_Task/2026-06-30/02-30_Core시스템구현/plan.md` 생성
- 구현 대상: Singleton<T>, IPoolable, ObjectPool<T>, GameManager, InputHandler
- 현재 상태: plan.md 사용자 승인 대기 중

---

## 2026-07-01

### Inspector 연결 에디터 스크립트 자동화 완성

기존 STEP 3~6 완료 이후, 추가적인 자동화 항목 7건을 구현하였다.

**SceneSetupEditor.cs**
- `Step7_ConnectBallLauncherRefs()`에 LaunchPoint 빈 GameObject 자동 생성 및 `BallLauncher._launchPoint` 연결 로직 추가
- 씬 오브젝트 변경 후 `EditorSceneManager.SaveScene()` 호출 추가

**UISetupEditor.cs**
- `Step6_CreateSkillCardPrefab()` 수정: 프리팹 존재 여부와 무관하게 `EditPrefabContentsScope`로 SkillCardUI 내부 참조(`_iconImage`, `_nameText`, `_descriptionText`, `_typeText`, `_selectButton`, `_canvasGroup`) 항상 연결
- `Step8_ConnectDamageTextManagerRefs()` 수정: DamageTextPool 자식이 없으면 자동 생성 후 `_poolParent` 연결 (기존엔 경고만 출력)
- `Step9_SetupHUDPanelContent()` 신규: HUDPanel 하위에 WaveText(TMP_Text), ScoreText(TMP_Text), LaunchReadyIndicator(CanvasGroup) 자식 생성 및 HUDPanel 참조 연결
- `Step10_SetupResultPanelContent()` 신규: ResultPanel 하위에 TitleText(TMP_Text), ScoreText(TMP_Text), RestartButton(Button) 자식 생성 및 ResultPanel 참조 연결
- `Step11_SetupSkillSelectionPanelContent()` 신규: SkillCard.prefab 3개 인스턴스화 후 `SkillSelectionPanel._skillCards` 연결, SkillData 10종 로드 후 `_allSkillDatas` 연결
- 씬 오브젝트 변경 후 `EditorSceneManager.SaveScene()` 호출 추가

**MonsterSetupEditor.cs**
- `SetupWaveSpawnEntries()` 신규: Wave 1~20 MonsterSpawnEntry 데이터 자동 설정
  - Wave 1~5: Fluffy만
  - Wave 6~10: Fluffy + Spider
  - Wave 11~15: Fluffy + Spider + StoneBug
  - Wave 16~20: Fluffy + Spider + StoneBug + ForestDeer (4종 전부)

### 런타임 버그 수정

**InputHandler.cs**
- `UnityEngine.Input` → `UnityEngine.InputSystem.Mouse / Touchscreen` 교체
- Player Settings에서 New Input System 사용 설정 시 `InvalidOperationException`이 발생하던 문제 수정

**GameManager.cs**
- `Start()` 메서드 추가하여 게임 시작 시 자동으로 `StartGame()` 호출
- 기존에는 게임 상태가 Ready로 고정되어 HUD가 숨겨지고 볼 발사가 불가했던 문제 수정

**SampleScene.unity**
- 카메라 `orthographic size: 5 → 10` 수정
- 기본값 5로는 플레이 영역(x:±5.5, y:-10~+8)의 약 1/4만 보였던 문제 수정

---

## 2026-07-03

### 볼 발사 메커닉 재설계 (`_Task/2026-07-01/21-15_ball-launch-mechanics`)

`GameplayMechanics.md` 섹션 1(볼 발사/궤도)의 요구사항을 실제 코드로 재구현하였다.

- 터치 즉시 조준 시작(`InputHandler.OnAimBegin` 신설), 릴리즈는 더 이상 발사 트리거가 아님
- 2단계 궤적 프리뷰 신규 구현(`TrajectoryPreview.cs`) — 1차/2차 충돌 지점 점선 + 2차 지점 레드닷/원형 궤적선
- 화면 하단 귀환 후 자동 재발사 사이클 도입 (`BallLauncher`/`Ball`)
- 노말볼 5개(순차 발사, `_rosterLaunchInterval`로 발사 간격 Inspector 조절) + 특수볼 최대 4종(액티브 스킬 슬롯 대응)의 개별 로스터 모델 도입 — `SkillManager`/`SkillSelectionPanel` 연동
- 몬스터 하강을 볼 사이클(`OnAllBallsReturned`)에서 완전히 분리, `MonsterBase`가 매 프레임 `MonsterData.MoveSpeed` 기반 시간 연속 하강을 수행하도록 재설계(`WaveManager`도 함께 수정), 냉동/슬로우를 턴 기반 → 초 기반으로 전환

**QA 검토 및 수정**
- 1차 QA 검토에서 Critical 2건(벽 반사 소진 시 로스터 볼 영구 이탈, `BallData.asset._maxBounces` 0→10 데이터 오류) + Major 1건(게임 종료 후에도 재발사 지속) 발견 → 수정 완료
- 이후 사용자 재확인을 거쳐 "로스터 볼은 벽에서 반사 횟수 무관하게 항상 순수 반사만 하고 Ground 충돌에서만 귀환"으로 최종 정정
- PR #6으로 main에 머지 완료

### UISetupEditor 버그 수정 (후속)

- 실 테스트 중 몬스터 처치 시 `CharacterXpBar.UpdateXp`에서 `NullReferenceException` 발생 확인
- 원인: `UISetupEditor.cs`가 `CharacterHpBar._slider`, `CharacterXpBar._slider`/`_levelText` 필드를 `SerializedObject`로 연결하는 코드가 누락되어 있었음 (`_hpText` 연결 코드는 있었으나 `_slider` 등은 빠져 있었음)
- PR #7로 main에 머지 완료

### 문서 정리

- `ProjectStatus.md` / `AIFailures.md` 갱신, agent-memory 보강, `AGENTS.md` Task 문서 인덱스에 `2026-07-01` 섹션(`18-41_ui-hud-gap-fill`, `21-15_ball-launch-mechanics`) 추가

### WaveData → WaveTableData 리팩토링

`Assets/_Project/Data/`에 asset이 지나치게 많다는(웨이브 1개당 asset 1개, 총 20개) 지적에 따라 웨이브 데이터 구조를 단일 테이블 asset으로 축약하였다. task 문서(research.md/plan.md) 없이 예외적으로 바로 구현을 진행하였고, main 브랜치에 직접 커밋(`9c188a8`)/푸시하였다.

- 삭제: `Scripts/Data/WaveData.cs`, `Data/WaveData_Wave1.asset` ~ `WaveData_Wave20.asset`(20개 + 각 .meta)
- 신규: `Scripts/Data/WaveTableData.cs` — `WaveEntry`(WaveNumber, SpawnEntries) + `WaveTableData`(ScriptableObject, `_waves` List, `Waves`/`WaveCount` 프로퍼티), 기존 `MonsterSpawnEntry`(Data, GridPosition)는 그대로 이전
- 수정: `WaveManager.cs`(`_waveDatas` 배열 → `_waveTable` 단일 필드로 교체), `MonsterSetupEditor.cs`(웨이브 생성/스폰 데이터 채우기 로직을 단일 테이블 asset 기준으로 변경, 계산 로직 자체는 변경 없음), `SceneSetupEditor.cs`(WaveManager 참조 연결 로직 단순화)
- 미완료 사항: 이 작업이 진행된 원격 환경에는 Unity 에디터가 없어 새 `WaveTableData.asset` 생성과 `SampleScene.unity`의 `WaveManager._waveTable` 필드 재연결이 아직 되어 있지 않음. 사용자가 로컬 Unity에서 `PurpleCow/Setup/Monster System Setup` → `PurpleCow/Setup/Scene Setup`을 순서대로 재실행해야 완전히 동작하며, 그 전까지는 씬의 구 `_waveDatas` 직렬화 데이터가 고아 데이터로 남아 웨이브 스폰이 동작하지 않는 상태

### 볼 천장 이탈 버그 수정 (`_Task/2026-07-03/12-48_ball-ceiling-wall-fix`)

실제 플레이 테스트 중 볼이 맵 외곽에서 튕겨야 하는데 맵 밖(천장)으로 나가버리는 버그를 발견하였다.

- **원인**: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step5_PlaceWallsAndGround()`가 `Wall_Left`/`Wall_Right`/`Ground` 3개 콜라이더만 생성하고 상단(천장) 벽을 생성하는 코드가 없었음. 좌/우/아래 3면만 막혀 있고 위쪽이 완전히 뚫려 있어, 몬스터가 없는 빈 레인 등으로 볼이 위쪽으로 진행할 경우 이를 막는 콜라이더가 전혀 없어 플레이 영역 밖으로 무한히 날아가는 구조였다(`research.md`).
- **수정**: `Step5_PlaceWallsAndGround()`에 `PlaceColliderObject("Wall_Top", "Wall", new Vector3(0f, 8f, 0f), new Vector2(12f, 0.2f));` 1줄 추가 (`plan.md` 사용자 승인 후 dev 에이전트 구현, 커밋 `345ae29`, 브랜치 `claude/ball-ceiling-wall-fix`). y=8은 `AIFailures.md`에 문서화된 실제 플레이 영역(`x: ±5.5, y: -10 ~ +8`) 상단 값, size(12, 0.2)는 `Ground`와 동일한 크기를 재사용해 좌우 벽과의 경계에 빈틈이 없도록 하였다.
- **검증**: 코드 수정만으로는 이미 커밋된 `Assets/Scenes/SampleScene.unity`에 자동 반영되지 않는 구조라, 사용자가 로컬 Unity 에디터에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해 `Wall_Top`을 씬에 반영하였다. 이후 사용자가 실제 플레이 테스트로 천장 반사가 정상 동작함을 확인하였다.

### 배경/해상도 대응 (`_Task/2026-07-03/12-30_background-resolution-fix`)

다양한 Android 기기 종횡비에서 배경/플레이 영역이 여백 없이, 잘리지 않고 표시되도록 대응하는 작업을 진행하였다.

- **배경**: 이 프로젝트는 특정 해상도를 못박지 않은 Android 타겟이라, 기기마다 다른 화면 종횡비에서도 원본 게임과 비슷하게 보여야 했다. 그런데 사용자가 보내준 실기기 빌드 스크린샷에서 배경 여백과 화면 좌우 잘림 현상이 함께 확인되며 이번 task가 시작되었다.
- **원인**: (1) Player Settings가 Auto Rotation 상태로 남아 있어 Portrait 고정이 되어 있지 않았고, (2) 배경 스프라이트(`Background_1_Stage.png`)가 정사각형 크롭 에셋이라 카메라 뷰포트에 맞춰 스케일되는 로직 없이 원본 크기로 고정 배치돼 있어 기기 종횡비가 표준을 벗어나면 여백이 생길 수 있었으며, (3) Python PIL로 배경 텍스처를 픽셀 단위로 실측한 결과 Wall_Left/Right/Top/Ground 좌표가 배경 이미지 속 격자 그림 경계와 애초에 일치하지 않는다는 사실도 추가로 드러났다.
- **해결 과정 (시행착오 포함)**:
  1. `BackgroundFitter.cs` 신규 작성 — 카메라 뷰포트에 맞춰 배경을 스케일하는 방식을 Cover(`max(가로비,세로비)`) → Contain(`min(가로비,세로비)`, 실기기에서 원인 불명의 사방 여백 발생해 폐기) 순으로 시도한 끝에, 가로/세로를 독립적으로 늘리는 Stretch 방식으로 최종 확정하였다.
  2. Wall 좌표가 배경 격자 그림과 어긋나는 문제를 해결하기 위해, 처음에는 카메라 시야(`orthographic size`)를 기기별로 동적으로 넓히는 `CameraFitter.cs`를 도입하려 했으나, "Wall 좌표 = 실측값 × 배경 배율"로 계산하면 Wall이 화면에서 차지하는 상대적 비율이 `orthographic size`와 무관하게 항상 일정함이 수학적으로 도출되어(가로 약 0.29, 세로 약 0.54, 항상 화면 안) 불필요함이 밝혀졌다. 이에 따라 `CameraFitter.cs`는 삭제하고 `orthographic size`는 원래 설계값 10으로 고정 유지하기로 하였다.
  3. 대신 `WallFitter.cs`를 신규 작성하여 `Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`를 "배경 이미지 실측 격자 경계값 × 그 순간 배경 배율"로 계산해 배치하도록 하였다. `SceneSetupEditor.cs`도 `CameraFitter` 연동 코드를 제거하고 `WallFitter` 연동 코드(`Step6_SetupWallFitter()`)로 대체하였다.
  4. 실기기 테스트 결과 격자가 화면에서 차지하는 비중이 작다는 피드백에 따라, `BackgroundFitter`/`WallFitter` 양쪽에 공통 확대 배율 `_zoomFactor = 1.3f`를 추가하였다(두 스크립트가 반드시 같은 배율을 써야 벽-격자 정렬이 유지됨).
  5. Unity 에디터에서 Play 모드 진입 없이 Inspector 값을 바꾸면 씬 뷰에 즉시 반영되도록, `BackgroundFitter`/`WallFitter` 둘 다 `[ExecuteAlways]`를 추가하고 기존 `Start()` 로직을 `Apply()` 메서드로 분리해 `Start()`와 신규 `OnValidate()` 둘 다 호출하도록 리팩터링하였다. 이후의 세부 수치 조정은 이 기능으로 사용자가 직접 진행하였다.
  6. 위 Inspector 실시간 반영 기능을 활용해 여러 차례 실기기 테스트를 거치며 `WallFitter`의 벽 기준값을 최종 조정하였다: `_nativeBottomY`는 -5.33(격자 실측값) → -10(원래 설계값) → -7.5(줌 배율 적용 후 카메라 시야 밖으로 나가는 문제 발견) → -6.5(최종, 격자 아래 덩쿨 장식 사이 캐릭터 위치 감안), `_nativeLeftX`는 -6.04 → -6.5(최종), `_nativeRightX`는 5.89 → 6.3(최종), `_nativeTopY`는 5.55 → 6.0(최종)로 조정하였다. 좌우 절대값이 다른 것은 배경 텍스처 자체의 미세한 비대칭(약 2.5%)이 실측값에 그대로 반영된 것이며, 사용자 확인 후 대칭으로 보정하지 않고 실측 기반으로 유지하기로 하였다. 또한 볼 발사 지점(`LaunchPoint`)도 `WallFitter`에 `_nativeLaunchPointY = -6.0f`로 편입해 같은 배율로 연동하였다 — `LaunchPoint`가 `SceneSetupEditor.Step8_ConnectBallLauncherRefs()`에서 생성되므로, `Step6_SetupWallFitter()` 호출 순서를 `Step8` 이후로 재배치하여 실행 순서 문제를 해결하였다.
- **최종 결과**: 위 과정을 거쳐 완성된 배경/벽 연동 방식을 실기기에서 최종 테스트하였고, 사용자가 결과를 확인해 만족하며 task를 완료하였다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/research.md`, `plan.md` 참고.
