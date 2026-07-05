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

### 볼 궤적 조준 개선 (`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`)

실제 플레이 테스트 중 궤적 프리뷰와 조준 관련 다섯 가지 문제가 순차적으로 발견되어 하나의 task로 정리하여 수정하였다.

- **배경**: 이슈 1~3(궤적이 터치할 때만 보임, 손가락 방향과 궤적 각도의 미묘한 어긋남, 궤적 프리뷰 색상/크기가 원본과 다름)을 먼저 research.md/plan.md로 정리해 구현하였다. 이후 이슈 2 구현 완료분에 대한 실제 플레이 테스트 과정에서 이슈 4(조준 모델 자체의 괴리감)가, 이슈 4 구현 완료분에 대한 실제 플레이 테스트 과정에서 이슈 5(터치 시작 단계 폴링 누락)가 추가로 재발견되어 같은 task 문서에 이어서 반영하였다.
- **이슈 1 (궤적 상시 표시)**: 기존 `TrajectoryPreview.cs`는 `InputHandler`의 `OnAimBegin`/`OnDrag`/`OnRelease` 이벤트에만 반응해 터치 중에만 궤적을 그리고 릴리즈 시 숨기는 구조였다. 몬스터가 볼 사이클과 무관하게 항상 이동하므로(`GameplayMechanics.md` 섹션 2) 터치하지 않는 동안에도 궤적 충돌 지점이 실시간으로 갱신되어야 정확하다는 점을 근거로, 이벤트 구독을 전부 제거하고 `Update()`에서 매 프레임 `BallLauncher.Instance.LaunchDirection`(터치 중엔 드래그 방향, 아닐 땐 마지막 조준 방향) 기준으로 궤적을 재계산하도록 전환하였다. `UIRules.md` 섹션 11의 "조준 중에만 표시" 문구도 함께 갱신하였다.
- **이슈 2 (조준 정확도)**: `InputHandler.cs`가 스크린(픽셀) 좌표 차이를 스크린→월드 변환 없이 그대로 정규화해 조준 방향으로 쓰고 있어, 화면 종횡비/투영 배율에 따라 손가락 방향과 궤적 각도가 어긋나는 왜곡이 발생하고 있었다. `Camera.main`을 `Awake()`에서 캐싱한 뒤 `ScreenToWorldPoint`로 변환한 월드 좌표 기준으로 방향을 계산하도록 수정하였다.
- **이슈 3 (색상/크기 불일치)**: `_hitDot`과 `_hitRing`이 둘 다 `_hitColor`(순수 빨강)를 참조하는 버그가 있어 링도 의도와 달리 빨간색으로 그려지고 있었다. 원본 게임(`Assets/_Project/Docs/targetUI/`) 스크린샷을 픽셀 단위로 실측하여, 링은 `_ringColor`(회백색 계열)로 분리하고 레드닷은 톤 다운된 브릭레드로, `_dotRadius`는 더 작게, 점선 주기(`DASH_WORLD_SIZE`)는 더 촘촘하게, 점선 색(`_lineColor`)은 살짝 톤 다운된 회백색으로 각각 조정하였다.
- **이슈 4 (조준 모델의 괴리감, 이슈 2 구현 완료 후 재발견)**: 이슈 2 반영 이후 실 플레이 테스트에서, 터치 시작 지점을 고정 기준점으로 삼아 그로부터의 상대 이동량을 방향으로 쓰는 기존 "상대 드래그" 모델 자체가 손가락과 궤적 사이의 괴리감을 유발한다는 점이 확인되었다. `BallLauncher.Instance.LaunchPoint`(발사 지점)에서 현재 손가락 위치를 향하는 절대 방향을 매 프레임(터치 시작 프레임 포함) 계산하는 "절대 조준" 모델로 전환하였다(`ComputeAimDirection` 헬퍼 신설). `GameplayMechanics.md` 섹션 1의 기존 스펙 문구("터치하는 순간 조준 방향이 즉시 정해진다", "드래그 위치를 목표로 실시간으로 따라간다")가 상대 드래그 모델보다 이 절대 조준 모델과 더 정확히 부합한다는 점도 확인되어, 해당 스펙 서술 자체는 그대로 유지하였다.
- **이슈 5 (터치 시작 폴링 누락, 이슈 4 구현 완료 후 재발견)**: 이슈 4 반영 이후 실 플레이 테스트에서, 터치를 대자마자 바로 살짝 움직이면 같은 프레임에 `TouchPhase.Began`과 `Moved`가 뭉개져 `Began`이 관측되지 않고 곧바로 드래그로 인식되는 버그가 발견되었다. `TouchPhase.Began` 값 자체에 의존하던 시작 판정을 "아직 드래그 중이 아닌 상태(`!_isDragging`)에서 터치가 감지되면 phase와 무관하게 그 자체를 시작으로 취급"하는 `_isDragging` 상태 기반 판정으로 재구성하였다. 이 과정에서 마우스 분기도 터치와 동일한 구조로 통일하여, 기존 클릭 첫 프레임의 `OnDrag` 중복 발행 문제도 함께 정리되었다.
- **검증**: 이슈 1~5 모두 `InputHandler.cs`/`TrajectoryPreview.cs`에 구현되어 main에 반영되었으며, 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인하였다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/research.md`, `plan.md` 참고.

---

## 2026-07-04

### 배경 격자 정사각형 보정 (`_Task/2026-07-04/16-40_background-square-grid-fix`)

몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기 기반 정사각형 그리드 점유 체크)의 선행 작업으로 진행하였다.

- **문제**: research.md에서 배경 텍스처(`Background_1_Stage.png`)를 픽셀 단위로 실측한 결과, 장식용 격자가 140×85px(비정사각형, 약 1.65:1)로 그려져 있음을 확인하였다. 몬스터/블록 스프라이트(`Fluffy`/`Spider`/`Block_1x1` 등)는 정사각형 1유닛 체계로 제작되어 있고, 원본 게임 실제 플레이 레퍼런스 스크린샷을 동일하게 실측하면 격자선 간격이 가로/세로 모두 약 97px로 정사각형이라, 우리 프로젝트 배경 에셋만 비정사각형인 상태였다.
- **해결**: `BackgroundFitter.cs`/`WallFitter.cs`가 채택 중이던 "기기별 독립 scaleX/scaleY Stretch" 방식을 "텍스처 고유 비율 보정(`_cellAspectCorrection ≈ 1.647`, 고정값) + 격자 영역 기준 균일 Cover 배율" 2단계 공식으로 교체하였다. 신규 필드(`_cellAspectCorrection`, `_gridAreaWidth = 14.53`, `_gridAreaHeight = 10.16`) 주입은 기존 `SceneSetupEditor.cs`를 건드리지 않고 신규 `BackgroundGridFitSetupEditor.cs`(별도 메뉴 `PurpleCow/Setup/Background Grid Fit Setup`)로 분리하였다. 리소스(PNG)는 수정하지 않고 순수 코드 변경으로만 처리하였다.
- **실기기 검증**: 사용자가 로컬에서 여러 실기기(Galaxy Note 10 등)로 테스트한 결과 격자가 정사각형으로 정상 렌더링됨을 확인하였다. 다만 새 계산식(격자 영역 기준 Cover)이 기존 방식(이미지 전체 기준 Stretch)보다 필요 배율이 커서, 기존 `_zoomFactor` 기본값 1.3을 그대로 쓰면 화면이 과도하게 확대되는 문제가 발견되었다. 에디터 미리보기(Free Aspect)에서는 0.6, 실기기(Note 10 포함 복수 기기)에서는 0.5가 적절함을 확인하여 `BackgroundFitter.cs`/`WallFitter.cs`의 `_zoomFactor` 기본값을 0.5로 최종 갱신하였다(커밋 완료). 에디터 미리보기와 실기기 결과가 다를 수 있는 이유(세이프 에어리어/노치/펀치홀 카메라, 정확한 해상도 재현 한계)도 함께 논의되었으며, 실기기 테스트 결과를 최종 기준으로 삼았다. `_nativeLeftX` 등 5개 벽 기준값 자체의 재조정 여부는 이번 대화에서 사용자가 별도 문제를 보고하지 않아 기존 값을 유지한 채 마무리되었다(사용자가 확인 완료; 이번에 정확히 확정된 조정 사항은 `_zoomFactor` 값뿐이다). 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-04/16-40_background-square-grid-fix/research.md`, `plan.md` 참고.

---

## 2026-07-05

### PDF 스펙 대비 문서 재감사 (main에 이미 병합됨, PR #12)

`MonsterRules.md`/`UIRules.md`를 공식 요구사항 PDF(`PurpleCow_클라이언트_채용과제.pdf`)와 다시 대조하는 작업을 진행하였다.

- **첫 시도(방향 오류)**: 커밋 `d5a3b06`에서 두 문서에 "구현 상태"만 갱신하는 방식으로 작업했으나, 실제로 필요했던 것은 "문서에 적힌 규칙 자체가 PDF의 목표와 모순되는지"를 감사하는 것이었다는 점이 확인되어, 방향이 잘못된 작업이라는 사용자 판단에 따라 되돌렸다(`16ec529`).
- **재감사**: 이후 "문서 내 규칙이 PDF 목표와 모순되는지 여부"만을 기준으로 재감사를 진행하였으나, 확실한 모순은 발견되지 않아 문서 수정 없이 결과만 보고하는 것으로 마무리하였다(코드/문서 변경 없음).
- PR #12로 main에 머지 완료.

### PrismPanel(융합 시스템 잔재) 제거 (main에 이미 병합됨, PR #12)

`UISetupEditor.cs`의 Canvas_Panel 생성 목록을 검토하던 중, 이름만 존재하고 실제 로직이 전혀 없던 빈 스텁 패널 `PrismPanel`을 발견하였다.

- PDF 스펙의 "구현 제외 항목"(튜토리얼, 배속 기능, 1스테이지 보스, 자동 조준 기능, 선택지 다시뽑기, 융합 시스템) 중 융합 시스템 관련 잔재로 판단되어, 사용자 확인 후 삭제를 확정하였다.
- `UISetupEditor.cs`의 `panelNames` 배열에서 `"PrismPanel"` 항목을 제거하고, `SampleScene.unity`에 이미 생성되어 있던 빈 GameObject도 YAML 파일에서 직접 제거하였다. `UIRules.md`의 Canvas 계층도에서도 해당 줄을 함께 삭제하였다.
- `LevelUpPanel`/`PausePanel`/`BallLevelUpPanel`은 실제 로직이 존재하는 정상 패널이므로 그대로 유지하였다.
- PR #12로 main에 머지 완료.

### 볼 궤적 프리뷰 고리(Ring) 점선화 + 회전 효과 (`_Task/2026-07-05/11-20_trajectory-ring-dash-rotate`, main에 이미 병합됨, PR #12)

실제 플레이 레퍼런스 대비, `TrajectoryPreview.cs`의 2차 충돌 지점 레드닷을 감싸는 고리(`_hitRing`)가 완전한 실선으로 렌더링되던 것을 점선 + 회전 효과로 개선하였다.

- **요구사항**: 원본 게임 레퍼런스(`targetUI/` 스크린샷 실측)처럼 고리가 끊어진 점선(파선) 형태로 보이고, 조준 여부와 무관하게 항상 시계방향으로 회전해야 한다는 사용자 지적을 반영하였다. 별도로 요청되었던 "궤적선 색상 등 Inspector 조절 가능화"는 조사 결과 이미 기존 코드(`_lineColor` 등 6개 `[SerializeField]` 필드)로 구현이 끝나 있음을 확인하여 추가 구현이 필요 없었다.
- **시행착오 (점선화)**:
  1. 첫 시도로 텍스처 반복(타일링) 방식을 적용해 원 둘레에 10개의 점선 세그먼트를 목표로 구현했으나, 실제로는 2개로 보이는 문제가 있었다. 원인은 특정하지 못했다(이 작업이 진행된 원격 환경에는 Unity 에디터가 없어 렌더링 결과를 직접 검증할 수 없었음).
  2. 두 번째 시도로 `LineRenderer.colorGradient`(alphaKeys 8개, 4개 피크 + 4개 골 배치) 방식으로 교체하여 정확히 4개의 점선이 보이도록 보장했으나, 사용자가 실제 레퍼런스 이미지(`targetUI/circle.jpg`)를 보내 대조한 결과 점선 경계가 너무 부드럽게 흐려지는 문제가 발견되었다. 이는 그라데이션 방식 자체의 근본적 한계(alphaKeys 8개만으로는 선명한 경계 구현 불가)로 판단되었다.
  3. 세 번째 시도로 텍스처 타일링 방식으로 재전환하되, 목표 개수를 4개로 조정하고, 기존 `_hitRing.loop = true` 대신 원을 명시적으로 닫는 정점(`CIRCLE_SEGMENTS + 1`개, explicit close)을 추가하는 방식으로 재구현하였다. 이는 1번 시도의 실패가 Unity의 loop 옵션과 Tile 텍스처 모드 조합에서 발생하는 알려진 문제일 가능성에 근거한 시도였다.
- **회전 효과**: 회전 속도를 `[SerializeField] private float _ringRotationSpeed = 90f;`(단위 deg/sec)로 신규 필드 추가해 Inspector에 노출하였다.
- **검증 상태 — 중요**: 이번 문서 갱신 작업에서 사용자가 실제로 검증 완료를 확인한 항목은 "볼 발사/볼-볼 충돌 방지"(아래 4번째 항목)뿐이며, 이 고리 점선화+회전 작업의 최종(3번째 시행착오) 버전이 실제로 정확히 4개의 호로 보이는지, 회전이 의도한 시계방향으로 자연스럽게 보이는지는 **사용자가 아직 로컬 Unity에서 재확인하지 않았다.** 즉 구현 자체는 완료되어 main(PR #12)에 반영되었으나, 최종 시각 확인은 사용자 로컬 테스트 대기 중인 상태로 별도 구분해서 기록한다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-05/11-20_trajectory-ring-dash-rotate/research.md`, `plan.md` 참고.

### 볼-볼 물리 충돌 방지 (`_Task/2026-07-05/16-40_ball-ball-collision-fix`, 아직 main에 병합 안 됨)

실제 플레이 테스트 중 여러 볼이 동시에 존재할 때 볼끼리 물리적으로 서로 튕겨나가는 버그가 발견되어 원인을 조사하고 수정하였다.

- **원인**: `Ball`/`Wall`/`Ground`/`Monster` GameObject가 전부 Default 레이어(0)에 있었고, `Physics2DSettings.asset`의 레이어 충돌 매트릭스가 Default-Default 쌍의 충돌을 허용하고 있어, 여러 볼이 동시에 존재하는 상황(로스터 다중 볼 등)에서 물리 엔진이 볼-볼 간 실제 충돌 반응(속도 변경)을 계산해 적용하고 있었다. `Ball.OnCollisionEnter2D`의 태그 분기(`Monster`/`Wall`/`Ground`만 처리)는 물리 반응이 이미 적용된 이후에 호출되는 콜백이라 코드 레벨에서는 이 물리적 튕김 자체를 막을 수 없었다.
- **해결**: 전용 "Ball" Physics2D 레이어를 신설하였다. `BallSetupEditor.cs`에 `AddBallLayer()`(TagManager.asset에 레이어 등록)/`AssignBallPrefabLayer()`(Ball.prefab의 m_Layer를 신설 레이어로 설정) 신규 메서드를 추가하고, 기존 `PurpleCow/Setup/Ball System Setup` 메뉴 실행 시 자동으로 처리되도록 통합하였다. 런타임에서는 `BallLauncher.Awake()`에서 `Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true)`를 1회 호출해 볼-볼 충돌만 전역적으로 비활성화하였다. Wall/Ground/Monster는 그대로 Default 레이어에 남겨두었으며, Unity가 새 레이어 추가 시 기존 레이어와의 충돌 비트를 기본적으로 켠 채 초기화하므로 Ball-Wall/Ground/Monster 충돌은 별도 조치 없이 정상 유지된다.
- **검증**: 사용자가 로컬에서 `PurpleCow/Setup/Ball System Setup` 메뉴를 재실행해 "Ball" 레이어 등록 및 `Ball.prefab` 레이어 할당을 완료한 뒤, 실제 플레이 테스트로 "볼 발사 정상 동작"과 "볼-볼 물리 충돌 방지(서로 안 튕김)" 둘 다 검증 완료를 확인하였다. 이 항목은 위 궤적 고리 작업과 달리 **구현 완료 + 사용자 실기기/로컬 검증 완료**로 명확히 구분된다.
- **잠재적 위험(참고)**: `BallLauncher.Awake()`에 `Physics2D.IgnoreLayerCollision` 호출을 추가할 때, "Ball" 레이어가 아직 등록되지 않은 상태(로컬에서 Setup 메뉴를 먼저 재실행하지 않은 경우)에서 실행하면 `LayerMask.NameToLayer("Ball")`이 -1을 반환해 예외가 발생할 수 있는 위험이 있었으나, 사용자가 먼저 Setup 메뉴를 재실행해 레이어를 등록한 뒤 테스트했기 때문에 실제로는 문제없이 정상 동작이 확인되었다.
- **병합 상태**: 이 작업은 아직 main에 병합되지 않았으며, 현재 브랜치(`claude/project-review-bugs-qq65d1`)에만 커밋되어 있다. main 병합은 다음 작업으로 남아있다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-05/16-40_ball-ball-collision-fix/research.md`, `plan.md` 참고.
