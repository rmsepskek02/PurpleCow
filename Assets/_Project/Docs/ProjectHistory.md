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

### 볼 조준 방향 Y좌표 하한 제한 (`_Task/2026-07-05/18-30_aim-direction-y-clamp`, 아직 main에 병합 안 됨)

실제 플레이 테스트 중 사용자가 "볼 궤도를 설정할 때 일정 y좌표 밑으로는 설정하지 못하게 하자"고 요청하여 진행하였다.

- **기준점 논의 과정**: 처음에는 기준점을 "격자타일 밑변"(배경 그리드의 시각적 바닥, `WallFitter`가 기기별로 동적 재계산하는 `Ground` Transform 위치)으로 잡고 research.md까지 작성했으나, `WallFitter._ground`가 private 필드라 `InputHandler`에서 접근하려면 씬 참조 연결이 추가로 필요하다는 복잡성이 확인되었다. 이후 사용자가 방향을 단순화하여, 이미 존재하는 몬스터 바닥 도달 게임오버 판정 기준선(`WaveManager._bottomBoundaryY`)을 그대로 재사용하기로 확정하였다. `WaveManager`가 이미 싱글톤이라 씬 참조 연결이나 에디터 스크립트 수정이 전혀 필요 없어져 구현이 단순해졌다.
- **구현**: `WaveManager.cs`에 `public float BottomBoundaryY => _bottomBoundaryY;` 프로퍼티를 추가하였다. `InputHandler.ComputeAimDirection()`에서 터치 위치를 월드 좌표로 변환한 직후 `worldPos.y = Mathf.Max(worldPos.y, WaveManager.Instance.BottomBoundaryY);`로 clamp한 뒤 발사 지점 기준 방향을 계산하도록 수정하였다. `GameplayMechanics.md` 섹션 1에도 이 규칙을 문서화하는 줄을 함께 추가하였다.
- **참고**: 이 clamp는 "조준 가능한 목표 지점의 범위"만 제한하며, 발사된 볼이 물리 반사로 실제 기준선 아래까지 내려가는 것 자체를 막는 장치는 아니다(별개 사안). `TrajectoryPreview.cs`는 `BallLauncher.Instance.LaunchDirection`(이미 clamp된 방향)을 그대로 받아 그리므로 수정이 필요 없었다.
- **검증**: 사용자가 로컬 Unity에서 직접 플레이 테스트하여 정상 동작을 확인하였다("잘되니까"라고 명시적으로 확인). 이 작업 역시 위 볼-볼 물리 충돌 방지 항목과 같은 브랜치(`claude/project-review-bugs-qq65d1`)에 커밋되어 있으며 아직 main에 병합되지 않았다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-05/18-30_aim-direction-y-clamp/research.md`, `plan.md` 참고.

### 플레이어 액티브 스킬 2종 구현 (`_Task/2026-07-05/21-30_player-active-skill-system`)

- 회수된 원본/분신 볼을 도착 순서대로 FIFO 큐에 넣고, 초기 볼 발사와 같은 간격으로 현재 조준 궤도에 맞춰 재발사하도록 변경하였다.
- 스피드업은 30초 쿨타임과 6초 지속시간을 사용하며, 지속 중 활성 상태이거나 새로 발사되는 모든 볼에 속도 1.5배를 적용한다.
- 분신은 원본 로스터 볼만 복제해 순차 발사한다. 복사본은 복사 대상에서 제외되며, 두 번째 회수 시 발사 지점에서 풀로 반환된다.
- `PlayerActiveSkillData`, `PlayerActiveSkillManager`, `PlayerActiveSkillButton`과 Skill/UI/Scene Setup Editor 자동 구성을 추가하고, 기존 4종 기획 문서를 이번 범위인 스피드업/분신 2종으로 갱신하였다.
- 테스트 단계에서는 씬의 `speedUp`/`illusion` 버튼을 직접 재사용하고, 두 스킬 모두 게임 시작 즉시 사용할 수 있도록 시작 쿨타임을 0초로 설정하였다.
- 런타임/에디터 C# 어셈블리 빌드는 오류 0개로 통과했다.

### 캐릭터 스프라이트 프리팹 + 조준 방향 연동 회전 (`_Task/2026-07-05/17-27_character-sprite-prefab`)

plan.md 작성 당시의 초기 설계(`WeaponPivot`이라는 빈 부모 오브젝트 + `flipX` 기반 좌우 반전)는 로컬 실플레이 테스트 과정에서 여러 버그가 발견되며 시행착오를 거쳐 최종적으로 상당히 다른 구조로 귀결되었다. plan.md 자체는 과거 계획 기록으로 그대로 두고, 이 항목은 최종 구현 기준으로 기록한다.

- **신규 파일**: `Assets/_Project/Scripts/Character/CharacterAimView.cs`(신규 폴더), `Assets/_Project/Scripts/Editor/CharacterSetupEditor.cs`(신규, 메뉴 `PurpleCow/Setup/Character System Setup`, 기존 에디터 스크립트는 수정하지 않음), `Assets/_Project/Prefabs/Character/Character.prefab`(사용자가 로컬 Unity에서 위 메뉴 실행 후 직접 여러 차례 수동 수정을 거쳐 완성).
- **최종 구조**: `Character`(루트, `CharacterAimView` 컴포넌트) → `Body`/`Head`(SpriteRenderer) + `Weapon`(SpriteRenderer). 원래 계획에 있던 `WeaponPivot`(빈 부모, 회전축 용도)은 무기 스프라이트 자체의 피벗을 Sprite Editor에서 손잡이 위치로 재설정하면서 더 이상 필요 없어져 최종적으로 제거되었다. 다만 `CharacterSetupEditor.cs`의 기존 참조 연결 코드를 건드리지 않기 위해 코드상 필드명(`_weaponPivot`)은 그대로 유지하고, 실제로는 그 슬롯에 `Weapon` 오브젝트를 연결하였다. `Character.prefab`은 `BallLauncher`의 자식인 `LaunchPoint` 밑에 배치되어 `WallFitter`의 화면비 대응 리프레임을 자동으로 상속받는다.
- **시행착오 1 (좌우 반전 방식)**: 캐릭터 기본 아트가 왼쪽을 바라보는 모습이라, 1차 구현에서는 개별 스프라이트 `flipX` + 무기 위치 수동 반전 방식을 시도했으나 실제 로컬 테스트에서 반전 조건이 반대로 되는 버그가 발견되었다. 최종적으로 조준 방향(`BallLauncher.Instance.LaunchDirection`)의 x가 양수(오른쪽 조준)일 때만 캐릭터 루트 전체의 `transform.localScale.x`를 -1로 반전시키는 방식으로 재설계하면서 확정하였다.
- **시행착오 2 (무기/머리 회전 방식)**: 처음엔 `Mathf.Atan2` 기반으로 각도(도 단위)를 직접 계산하는 방식을 여러 차례 시도했으나, Unity의 Z축 회전 방향(CW/CCW) 규약을 매번 잘못 추측해 실제 플레이테스트에서 반복적으로 무기가 반대 방향을 가리키는 문제가 발생하였다. 사용자가 보내준 스크린샷 두 장을 픽셀 단위로 분석했으나 서로 다른 패턴을 보여 정확한 원인 특정에는 실패하였다. 최종적으로 `Quaternion.FromToRotation(Vector3.up, 목표방향)`으로 Unity가 직접 회전을 계산하게 하는 방식으로 전면 교체하여 각도 부호를 손으로 추측할 필요를 없애 근본적으로 해결하였다. 캐릭터 루트가 반전된 상태일 때는 목표 방향의 x부호를 미리 뒤집어서 로컬 회전을 계산해야 루트의 반전과 상쇄되어 최종 결과가 맞게 된다는 점도 함께 확인되었다.
- **머리 추종**: 머리는 무기 회전의 일부 비율(`_headRotationRatio`, 기본 0.25)만 `Quaternion.Slerp`로 보간하며 보조적으로 따라가도록 구현하였다.
- **미세 조정**: 실제 플레이테스트에서 "조준이 수평에 가까울수록 무기가 실제보다 덜 눕는 것 같다"는 피드백이 있어, `_horizontalBiasDegrees`(기본 15도, `[SerializeField]`) 값을 무기 회전에 추가하는 보정을 넣었다. 처음엔 조준 각도에 비례해서 넣었다가, 사용자 요청으로 "각도와 무관하게 항상 고정값 적용"으로 최종 변경하였다.
- **의도적 미사용 필드**: `_bodySpriteRenderer`/`_headSpriteRenderer` 필드는 1차 구현(`flipX` 방식)의 잔재로 코드 로직상으로는 더 이상 사용되지 않지만, `CharacterSetupEditor.cs`의 기존 참조 연결 코드를 건드리지 않기 위해 필드 선언 자체는 의도적으로 그대로 남겨두었다.
- **영향 범위 외**: `CharacterManager.cs`(HP/XP 로직, `Scripts/Core/`)는 이번 작업과 완전히 분리되어 전혀 수정하지 않았다.
- **검증**: 위 시행착오(1차 WeaponPivot+flipX → 좌우 반전 버그 발견 → 루트 스케일 반전 방식으로 재설계하며 WeaponPivot도 함께 제거 → Atan2 각도 계산 방식으로 재구현했으나 회전 방향이 다시 반대로 나오는 문제 반복 → Quaternion.FromToRotation 기반으로 전면 교체 → 수평 보정치 고정값 적용까지 미세조정)를 모두 거친 뒤, 사용자가 로컬 Unity에서 실제 플레이 테스트로 최종 정상 동작을 확인하였다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-05/17-27_character-sprite-prefab/research.md`, `plan.md`(초기 계획, 최종 구현과 다름) 참고.
- 액티브 스킬 버튼 터치가 동시에 볼 조준 입력으로 처리되던 문제를 수정했다. 씬에 누락된 `EventSystem` + `InputSystemUIInputModule`을 Setup Editor가 생성하도록 보강하고, `InputHandler`가 처음 관측한 활성 터치에서 클릭 가능한 UI 여부를 검사해 UI에서 시작한 포인터를 릴리즈까지 조준 이벤트에서 제외한다.
- 런타임/에디터 C# 어셈블리 빌드는 오류 0개로 통과했다. 사용자가 Unity 플레이 테스트를 완료해 스피드업/분신 발동, 쿨타임 UI, 버튼 터치 시 조준 입력 차단이 정상 동작함을 확인했다.

---

## 2026-07-06

### 원본 게임 UI 오버홀 및 실기기 검증 (`_Task/2026-07-06/02-05_ui-overhaul`)

- 실제 원본 캡처와 공식 PDF를 재대조해 홈, 설정, `Best!`, Auto, 배속, 보스, 다시 뽑기, 융합을 제외하고 HUD, 레벨업 3택지, 일시정지, 결과 팝업을 재구성하였다.
- 모든 TMP 텍스트를 `Maplestory Bold SDF.asset`으로 통일하고, 상단 스테이지 진행률/캐릭터 XP/레벨, 캐릭터 하단 HP `현재/최대`, 우측 하단 스피드업·분신 버튼을 구성하였다.
- 레벨업 카드의 공격력과 상단 슬롯 레벨을 별도 배지로 분리하고, 아이콘 `preserveAspect`를 적용해 패시브 검 비율과 액티브 스킬 식별성을 개선하였다.
- 캐릭터 HP World Space Canvas가 캐릭터 루트 좌우 반전을 상속해 텍스트가 뒤집히던 문제를 역스케일 보정으로 해결하였다.
- 캐릭터 위치 조정 중 Ground와 LaunchPoint를 같은 값으로 설정해 공이 하단 콜라이더 안에서 생성되는 회귀가 발생했다. 이후 Ground `-7.5`, LaunchPoint `-6.7`, Character 로컬 Y `-0.4`로 분리하고 최소 물리 간격 `0.35`를 코드에서 강제하였다. 사용자가 공 발사·반사와 캐릭터 위치를 실기기에서 재검증하였다.
- 좌측 상단 테스트 전용 `S`/`F` 버튼으로 성공·실패 결과를 호출하도록 추가하였다. 첫 테스트에서 결과 제목이 화면 밖으로 빠지는 문제를 확인해 중앙 패널 자식 배치로 수정하고, 결과 진입 시 게임 정지 및 Unscaled Tween, 재시작 시 `Time.timeScale` 복구를 적용하였다.
- 사용자가 성공/실패 표시와 `다시 시작` 동작까지 실제 플레이로 확인해 UI 오버홀 전체 검증을 완료하였다.

### 삼택지 스킬 효과 및 캐릭터 성장 구조 구현 (`_Task/2026-07-06/10-03_skill-effects-progression`)

- 기존 `CharacterManager`의 필요 경험치 `{10, 20, 30}` 때문에 기본 몬스터 한 마리 처치 직후 삼택지가 열리고 Lv.4에서 성장이 멈추던 구조를 변경하였다. 최대 레벨은 시작 Lv.1과 스킬 선택 18회(6개 × 3레벨)를 합친 Lv.19이며, 필요 XP는 50부터 레벨마다 18씩 증가한다.
- `SkillData` ScriptableObject에 런타임 `_currentLevel`을 저장하던 규칙 위반을 제거하였다. 읽기 전용 데이터와 별도로 `SkillRuntimeState`가 현재 레벨을 보관하며 액티브 로스터 볼과 패시브 효과가 같은 상태를 공유한다.
- 액티브 5종 데이터에 Fire/Ice/Ghost/Laser/Cluster 전용 볼 Sprite를 연결하였다. 특수 볼은 공통 기본 피해 8 대신 현재 스킬 레벨의 `BallDamage`를 사용하고, 신규 획득 때만 로스터에 한 개 추가되며 재선택은 기존 상태만 레벨업한다.
- 파이어 볼은 타격마다 독립 화상 스택 하나를 추가하고 최대 중첩을 지키며, 몬스터 풀 반환 시 모든 DOT 상태를 정리하도록 변경하였다. 클러스터 서브 볼은 전용 Sprite와 고정 피해를 사용하되 클러스터 효과를 다시 갖지 않아 연쇄 생성을 막는다.
- 따뜻한 양철 심장은 노멀 볼에만 적용하고, 마법 거울은 벽에 부딪힌 볼 자신이 다음 타격 배율을 보관·소비하도록 바꾸었다. 자수정/에메랄드 단검은 몬스터에 다음 타격 보너스를 저장하던 방식 대신 현재 전면/후면 충돌의 치명타 판정 전에 확률을 더한다.
- 삼택지 카드는 미보유 여부를 실제 런타임 상태로 판정해 `New!`를 표시하고, 선택 후 도달할 레벨과 그 레벨의 공격력을 보여주도록 수정하였다.
- `SkillManager.OnDestroy()`에서 패시브 이벤트 구독을 해제해 씬 재시작 후 정적 이벤트가 중복될 가능성을 차단하였다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드는 오류 0개로 통과하였다. 에디터 빌드의 `SceneSetupEditor.cs` 내 `Rigidbody2D.isKinematic` 사용 경고 1개는 이번 작업과 무관한 기존 경고다.
- 사용자가 Unity 플레이 모드 테스트를 완료했으며 전용 볼 외형, 삼택지 선택 효과, XP 진행과 성장 구조가 정상 동작함을 확인하였다.

### 제출 정리 및 README 작성 (`_Task/2026-07-06/13-14_submission-cleanup-readme`)

- 사용자가 Android 실제 기기에서 20웨이브 전체 플레이를 완료한 사실을 최종 검증 내역으로 기록하였다.
- 성공/실패 결과 확인을 위해 임시로 사용했던 좌측 상단 `S`/`F` 버튼을 `HUDPanel`, `UIOverhaulSetupEditor`, `SampleScene.unity`에서 제거하였다. 실제 성공 조건과 HP 0 실패 조건은 그대로 유지하였다.
- 플레이어 액티브 스킬의 명칭을 코드 enum·필드·메서드, 데이터 에셋, Setup Editor, 씬 오브젝트, 문서 전체에서 `SpeedUp/스피드업`으로 통일하였다.
- 데이터 에셋은 `PlayerActiveSkillData_SpeedUp.asset`으로 이동하면서 `.meta` GUID를 유지해 기존 씬 직렬화 참조가 끊기지 않게 하였다.
- 루트 `README.md`를 새로 작성해 실행·조작법, 필수/가산점 구현, 주요 기술 설계, 제외 항목, 실기기 검증과 프로젝트 문서 링크를 정리하였다.
- AI 활용 내역에는 Codex와 Claude/Claude Code의 개발 지원 범위, GPT로 제작한 버튼 이미지 `SpeedUp.png`와 `Copy.png`, 사용자의 최종 의사결정 및 실기기 검증 책임을 구분해 기록하였다.

### 선택 Polish 1·5·6번 구현 (`_Task/2026-07-06/14-45_critical-bottom-ice-polish`)

- 치명타 데미지 텍스트의 코드 기본값과 프리팹 직렬화 색상을 `#FF4B3E`로 통일하였다.
- 아이스볼의 확률 판정 성공 시 직접 대상보다 위쪽에 있고 Collider 가로 점유 범위가 실제로 겹치는 몬스터를 조회해 Freeze/Slow를 전파하도록 구현하였다. 추가 피해는 직접 피격 대상에게만 유지하였다.
- 몬스터가 바닥 경계에 도달하면 즉시 풀로 반환하던 흐름을 0.35초 진동(강도 0.12), 0.25초 캐릭터 중심 돌진, 도착 순간 피해 이벤트, 풀 반환 순서로 변경하였다.
- 바닥 공격 상태에서는 일반 이동·피격·Collider 충돌을 중단하고, 연출 완료 전까지 활성 목록에 유지해 중복 실행과 조기 웨이브 클리어를 방지하였다.
- `WaveManager._characterTarget`을 실제 `LaunchPoint/Character` Transform에 연결하고 `SceneSetupEditor`의 현재 4종 몬스터 프리팹 및 캐릭터 참조 연결을 갱신하였다.
- 최초 아이스볼 후방 전파 구현은 적중 순간 존재한 몬스터만 정지해, 빙결 후 같은 열에 새로 배치된 몬스터가 계속 하강하며 겹치는 회귀가 확인되었다. 몬스터 이동 직전에 아래쪽 같은 열의 빙결 몬스터를 동적으로 조회해 하강을 차단하고 빙결 종료·처치 시 자동으로 재개하도록 보완하였다.
- 스폰 겹침에는 별도 원인도 있었다. `IsCellFree()`가 그리드 셀과 몬스터 중심의 거리만 비교해 실제 Collider가 일부 겹친 상태를 빈 셀로 오판했으며, 이를 스폰 셀 Bounds와 실제 몬스터 Collider Bounds의 x·y 양수 교집합 판정으로 교체하였다.
- 바닥 공격의 진동 시간 `0.35`, 진동 강도 `0.12`, 돌진 시간 `0.25`를 `WaveManager` Inspector에서 조정 가능한 직렬화 필드로 전환하고 씬과 Setup Editor 기본값을 함께 갱신하였다.
- 후속 사용자 검증에서 동적 빙결 열 차단이 거리에 관계없이 같은 열 전체를 멈추고, 아이스볼과 무관한 가로 2칸 몬스터 이동 겹침도 남아 있음이 확인되었다. 이에 후방 전체 Freeze/Slow 전파와 `HasFrozenMonsterAhead()`를 모두 제거하였다.
- 최종적으로 모든 활성 몬스터가 이동 전 실제 Collider Bounds를 기준으로 가장 가까운 앞 몬스터까지의 간격을 계산하고, 그 간격까지만 이동하도록 변경하였다. 멀리 떨어진 몬스터는 계속 전진하고 접촉한 몬스터만 정지한다.
- 돌진 전 진동의 코드/Setup 기본값을 시간 `0.25`, 강도 `0.18`, 횟수 `20`으로 변경하고 횟수를 Inspector에 추가 노출하였다. 현재 씬의 사용자 조정 강도 `0.3`은 보존하였다.
- 후속 플레이 이미지에서 일부 몬스터가 정상 최고 스폰 행을 넘어 상단 벽까지 밀리는 회귀가 확인되었다. 이미 겹친 Collider의 음수 간격을 안전 이동거리로 반환한 뒤 `Vector3.down`에 곱해 위쪽 이동으로 뒤집힌 것이 원인이었다. 후보 간격과 최종 반환값을 `0..희망 이동거리`로 제한해 접촉·겹침 상태에서는 정지하고 일반 이동이 위쪽으로 실행되지 않도록 수정하였다.
- 간격 여유, 이동량 제한, 경계 접촉, 기존 겹침 정지, 인접 열 무관, 가로 2칸 교차와 반환 범위 불변식이 모두 통과했고 런타임/Editor C# 빌드 오류 0개를 확인하였다.
- 상단 방향 밀림 수정 후 Android 실기기에서 몬스터가 겹친 채 유지되는 잔존 회귀가 확인되었다. 기존 `IsCellFree()`는 후보 쪽을 오프셋 없는 셀 Bounds로, 활성 몬스터 쪽을 실제 Collider Bounds로 비교해 두 기준의 중심이 약 `0.203646` 어긋나 있었다. 기존 몬스터 하강량 `0~0.85`의 851개 표본 중 204개에서 배치 가능 오판을 결정적으로 재현하였다.
- `MonsterBase.TryGetProjectedColliderBounds()`를 추가해 BlockSize별 Collider 크기, 프리팹 오프셋, 루트 및 풀 부모 스케일을 반영한 생성 후 후보 Bounds를 계산하도록 했다. `WaveManager`는 `MonsterData→Prefab` 매핑과 `CanPlaceMonster()`를 사용하고, 일반 상단 스폰과 1·11웨이브 전체 그리드 스폰 모두 같은 후보 Bounds 검사를 거치도록 변경하였다.
- 기존 셀 단위 `IsCellFree()`와 `topRowFree` 사전 캐시는 제거하였다. 같은 틱에 먼저 배치된 인스턴스는 즉시 활성 목록에 추가되므로 다음 후보 검사에도 실제 Bounds로 포함된다.
- `0.75`칸 하강 겹침 거부, 정확히 `0.85`칸 경계 접촉 허용, 인접 열 무관, 2×1 전체 폭, 1×2 전체 높이 사례를 통과했고 런타임/Editor 빌드 오류 0개를 확인하였다. Android 실기기 재검증은 대기 상태다.
- 런타임/Editor C# 빌드는 오류 0개로 통과했다. 기존 `SceneSetupEditor.cs`의 `Rigidbody2D.isKinematic` 사용 경고 1개는 이번 작업 범위 밖이다.
- Android 실기기에서의 최종 시각·동작 재검증은 사용자 확인 대기 상태다.

### 선택 Polish 1·6번 실기기 검증 완료

치명타 데미지 텍스트 색상 변경(1번)과 몬스터 바닥 도달 시 진동 → 박치기 돌진 → 소멸 연출(6번)에 대해 사용자가 Android 실기기에서 최종 검증을 완료하였다. 치명타 텍스트는 `DamageTextFx.cs` 기본값과 `DamageTextFx.prefab` 직렬화 값을 모두 `#FF4B3E`로 변경해 일반 데미지(흰색)와 치명타(붉은색)가 구분되는 것을 확인하였다. 바닥 도달 연출은 몬스터가 바닥에 닿으면 짧고 빠르게 진동한 뒤 0.25초 동안 실제 캐릭터 Transform 중심으로 돌진하고, 돌진 도착 시점에 `OnMonsterReachedBottom` 이벤트로 HP를 감소시킨 뒤 활성 목록 제거와 풀 반환을 수행하며, 연출 중에는 일반 이동·피격·Collider 상호작용을 차단해 중복 연출과 조기 웨이브 클리어를 방지하도록 구현되어 있다. 두 항목 모두 실기기에서 시각·동작이 정상 확인되어 `TODO.md`에서 완전히 제거되었다. 5번(아이스볼 및 몬스터 이동 겹침 방지)은 이번 검증에 포함되지 않았으며 여전히 Android 실기기 재검증 대기 상태다.
