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

---

## 2026-07-04

### 캐릭터 비주얼 구현 (`_Task/2026-07-04/01-38_character-visual-implementation`)

사용자 요청("캐릭터 에셋을 사용해서 캐릭터를 구현할 것")에 따라, 그동안 씬에 전혀 존재하지 않던 캐릭터 시각 표현(Body/Head/Weapon 스프라이트)을 구현하였다. research.md/plan.md 작성 후 사용자 승인을 거쳐 진행하였다.

**research.md 조사 결과**
- `CharacterManager.cs`는 HP/XP/레벨 등 스탯 로직만 담당하며 `SpriteRenderer`, 회전 등 시각 요소가 전혀 없음을 확인
- `SceneSetupEditor.cs`의 `Step7_ConnectBallLauncherRefs()`가 생성하는 `LaunchPoint`는 좌표 기준점 전용 빈 GameObject일 뿐 스프라이트가 붙어 있지 않음 — 씬 어디에도 캐릭터 모습을 렌더링하는 오브젝트가 없다는 점 확인
- `Character_main_weapon.png`가 중앙 Pivot(0.5, 0.5)으로 임포트되어 있어 실제 그립(손잡이) 위치와 어긋나 있음을 확인
- 좌우 반전과 자식 회전 각도의 부호 충돌 문제를 열린 이슈로 남기고, plan.md 단계에서 오케스트레이터가 사용자와 논의를 거쳐 `SpriteRenderer.flipX` 기반 처리 방식으로 확정

**design 에이전트 (커밋 `1cf5ab5`)**
- `Character_main_weapon.png.meta` 확인 결과, 무기 스프라이트가 `spriteMode: 2`(Multiple)라 최상위 `spritePivot`이 아닌 `spriteSheet.sprites[0]`의 `alignment`/`pivot` 값이 실제 Import에 적용됨을 확인
- `alignment: 0`(Center) → `9`(Custom)로 변경하고 Pivot을 그립(손잡이) 위치(0.39, 0.43)로 재설정

**dev 에이전트 (커밋 `1cf5ab5`)**
- 신규 `CharacterAimController.cs`(`Assets/_Project/Scripts/Character/`) 작성 — `BallLauncher.LaunchDirection`을 매 프레임 읽어 Weapon은 조준 방향을 거의 그대로 따라가는 회전, Head는 감쇠된 약한 회전, Body는 회전 없이 flipX만 적용
- 좌우 반전은 `localScale` 반전이 아닌 `SpriteRenderer.flipX`만 사용해 반전-회전 부호 충돌을 원천 차단(plan.md에서 확정한 방향대로 구현)
- `SceneSetupEditor.cs`에 `Step10_SetupCharacterVisual()`(이후 main과 병합하며 `Step11_SetupCharacterVisual()`로 재번호 부여) 신규 추가 — `Character` 오브젝트를 `LaunchPoint`와 동일 위치(BallLauncher의 형제 오브젝트)에 생성하고 Body/Head/Weapon 자식 3개를 자동 배치

**qa 에이전트 검토 및 오케스트레이터 수정 (커밋 `794713d`)**
- 1차 구현 검토에서 Major 2건 발견: (1) Body/Head/Weapon이 모두 Character 원점(0,0,0)에 겹쳐 배치되어 캐릭터 형태로 보이지 않는 문제, (2) design 에이전트가 작성한 agent-memory 파일에 툴 호출 잔재 텍스트(`</content>`)가 잘못 섞여 들어간 문제
- 오케스트레이터가 원본 합성 이미지(`Character_Main.png`, 파츠 미리보기)를 픽셀 템플릿 매칭으로 직접 분석해 Head/Body의 정확한 상대 위치(Head: 0.51,-0.23 / Body: 0.42,-0.75, Weapon 그립 기준)를 역산하고 시각적으로 검증한 뒤 dev 에이전트에게 수정 지시
- 추가로 `SpriteRenderer.flipX`가 Transform 위치에는 영향을 주지 않아 좌우 반전 시 Head/Body 위치가 미러링되지 않던 버그도 함께 발견해 수정(정면 기준 위치를 캐싱한 뒤 반전 상태에 따라 X 부호를 반전하는 방식)

**범위 및 미완료 사항**
- `CharacterManager.cs`(HP/XP 로직), `BallLauncher.cs`/`Ball.cs`(볼 발사/귀환 로직)는 이번 작업에서 전혀 수정하지 않음 — 순수 시각 레이어 추가
- 이 작업이 진행된 원격 환경에는 Unity 에디터가 없어, 코드/에셋 변경만으로는 이미 커밋된 `SampleScene.unity`에 자동 반영되지 않는다(기존 WaveTableData/Wall_Top 사례와 동일한 제약). 사용자가 로컬 Unity에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해야 씬에 `Character` 오브젝트(Body/Head/Weapon 포함)가 실제로 생성/갱신되며, 그 후 실제 플레이 테스트로 조준 반응(회전/반전)이 자연스러운지 확인이 필요하다

### main 병합 후 발견: WallFitter-Character 위치 연동 누락

캐릭터 비주얼 구현 작업 이후 main에 병합된 배경/해상도 대응(`WallFitter`) 작업과 통합하는 과정에서 새로운 문제가 드러났다.

- `WallFitter.Apply()`(`[ExecuteAlways]`, `Start()`/`OnValidate()`에서 호출)가 기기 화면 비율에 맞춰 `LaunchPoint`의 월드 좌표(`_nativeLaunchPointY` 기준)를 런타임에 동적으로 재배치하는데, `Character`는 `Step11_SetupCharacterVisual()`에서 `LaunchPoint`의 `localPosition`을 에디터 설정 시점에 한 번만 복사한 형제 오브젝트로 만들어져 있어, `WallFitter`가 이후 `LaunchPoint`를 움직여도 `Character`는 따라가지 못하고 원래 위치에 남는다.
- 캐릭터 비주얼 구현 당시에는 아직 `WallFitter`가 main에 없었기 때문에 예상하지 못한 상호작용이며, main과 병합하는 과정에서 오케스트레이터가 직접 발견하였다. 별도 후속 수정이 필요한 상태로 남겨두었다(`ProjectStatus.md` "다음 작업 순서" 참고).

### LaunchPoint 궤도화 재설계 (`_Task/2026-07-04/09-41_launchpoint-character-orbit`)

위에서 발견된 "WallFitter-Character 위치 연동 누락" 문제를 계기로, 오케스트레이터와 사용자가 논의를 거쳐 발사/귀환 지점의 관계 자체를 재설계하였다.

**research.md 조사 결과**
- `LaunchPoint`(`BallLauncher._launchPoint`)가 겸하고 있던 4가지 역할을 확인: (1) 볼 발사 스폰 위치(`BallLauncher.LaunchRosterEntry()`), (2) 귀환 목적지(`Ball.cs` 도착 판정/`ReturnToLaunchPoint()`), (3) 궤적 프리뷰 원점(`TrajectoryPreview.UpdateTrajectory()`), (4) `WallFitter`가 화면비에 맞춰 런타임에 재배치하는 대상
- `Character`는 `SceneSetupEditor.Step11_SetupCharacterVisual()`에서 씬 설정 시점에 `LaunchPoint`의 로컬 좌표를 한 번만 복사하는 형제 오브젝트일 뿐, `WallFitter`의 런타임 재배치와는 아무 연결이 없다는 점이 문제의 원인으로 확인됨
- 무기 스프라이트 그립(Pivot)에서 갈고리 끝까지의 거리를 `Character_main_weapon.png.meta`(`pivot.y=0.43`, `height=116`, `spritePixelsToUnits=100`) 기준으로 역산하면 약 0.6612 유닛
- 노출 주체, 계산 방식, WallFitter 재배치 대상, 무기 길이 소유 클래스, 씬 배선 스크립트 분리 여부 등 5가지를 열린 이슈로 남기고 plan.md 단계로 이관

**plan.md 확정 사항 (사용자와 논의)**
- 발사/귀환 지점 노출 창구는 계속 `BallLauncher`로 유지하고, 내부적으로 `CharacterAimController`(시각 레이어)를 참조해 계산 — 게임플레이 로직이 시각 레이어를 직접 알 필요 없도록 유지
- 별도 GameObject(Transform)를 매 프레임 갱신하는 방식이 아닌 계산 프로퍼티 방식 채택: 발사 시작점 = `Character 위치 + LaunchDirection(정규화) × 무기 길이`, 귀환 목적지 = `CharacterAimController._bodyRenderer.transform.position`. 이번 세션에서 이미 두 차례 겪은 "복사된 값이 원본과 어긋나는" 버그 유형을 재도입하지 않기 위함
- `WallFitter`의 재배치 대상을 `LaunchPoint`에서 `Character`로 변경, 필드명도 `_launchPoint`→`_character`, `_nativeLaunchPointY`→`_nativeCharacterY`로 리네이밍(기본값 `-6.0f`는 유지)
- 무기 길이(`0.6612f`)는 `CharacterAimController`의 `SerializeField`로 소유
- `SceneSetupEditor.cs`는 무의미해진 코드만 삭제하고, 새 배선(`Character` 초기 위치, `WallFitter`↔`Character` 연결)은 신규 `CharacterLaunchOrbitSetupEditor.cs`로 분리 — 기존 `SceneSetupEditor.cs`/`UISetupEditor.cs`/`MonsterSetupEditor.cs`처럼 관심사별 `*SetupEditor.cs`를 두는 프로젝트 관례를 따름

**dev 에이전트 구현**
- `CharacterAimController.cs`: `_weaponLength`(기본값 `0.6612f`), `BodyPosition`/`WeaponLength` 프로퍼티 추가, `MonoBehaviour` → `Singleton<CharacterAimController>` 상속으로 변경
- `BallLauncher.cs`: 고정 `_launchPoint`(Transform) 필드와 `LaunchPoint` 프로퍼티 삭제, `LaunchOrigin`/`ReturnPoint` 계산 프로퍼티 신설, `LaunchRosterEntry()`의 스폰 위치 참조 변경
- `Ball.cs`(도착 판정, `ReturnToLaunchPoint()` 내부)와 `TrajectoryPreview.cs`(궤적 원점)의 `LaunchPoint` 참조를 각각 `ReturnPoint`/`LaunchOrigin`으로 변경
- `WallFitter.cs`: `_launchPoint`→`_character`, `_nativeLaunchPointY`→`_nativeCharacterY` 리네이밍, `Apply()` 내부 재배치 로직도 함께 변경
- `SceneSetupEditor.cs`: `Step8_ConnectBallLauncherRefs()`의 `LaunchPoint` GameObject 생성/연결 코드, `Step6_SetupWallFitter()`의 `_launchPoint`/`_nativeLaunchPointY` 연결 코드, `Step11_SetupCharacterVisual()`의 `LaunchPoint` 탐색 및 위치 복사(`localPosition = launchPoint.localPosition`) 코드를 삭제(그 외 로직은 그대로 유지)
- 신규 `Assets/_Project/Scripts/Editor/CharacterLaunchOrbitSetupEditor.cs`(`PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴) 작성: `Character`의 초기 로컬 좌표를 기존 `LaunchPoint` 기본값과 동일한 `(0, -8, 0)`으로 지정하고, `WallFitter._character` 필드에 `Character` Transform을 연결

**오케스트레이터 추가 정리**
- `LaunchPoint`→`ReturnPoint`로 역할이 바뀐 뒤 남아있던 옛 이름(변수 `toLaunchPoint`→`toReturnPoint`, 메서드 `ReturnToLaunchPoint()`→`ReturnToCharacter()`, 관련 주석들, `SceneSetupEditor.cs`의 스테일 주석)을 로직 변경 없이 리네이밍

**qa 에이전트 검토**
- `LaunchOrigin` 계산식과 `CharacterAimController`의 무기 회전 공식(`aimAngle = Atan2(dir.y, dir.x) * Rad2Deg - 90f`)이 회전행렬로 봤을 때 수학적으로 정확히 일치함을 검증
- `Singleton<T>` 전환도 기존 `Awake()` 부재로 충돌 없음을 확인
- Major 1건(`CharacterLaunchOrbitSetupEditor`가 `Character`를 못 찾은 경우에도 `WallFitter._character`를 null로 덮어쓸 수 있던 문제) 발견 → 즉시 수정 완료

**범위 및 미완료 사항**
- `CharacterManager.cs`(HP/XP 로직), 볼의 물리/충돌/데미지 판정 로직은 전혀 수정하지 않음
- 원격 환경에 Unity 에디터가 없어 코드 수정만으로는 `SampleScene.unity`에 자동 반영되지 않으며, 사용자가 로컬 Unity에서 (1) `PurpleCow/Setup/Scene Setup` 재실행, (2) 씬에 남아있는 구 `LaunchPoint` GameObject 수동 정리(필요 시), (3) 신규 `PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴 실행이 필요하다
- `WallFitter` 필드 리네이밍으로 로컬에서 튜닝했던 값이 초기화될 수 있어 재설정이 필요할 수 있다
- 실제 조준 시 무기 끝에서 볼이 발사되는 것처럼 보이는지, 귀환 시 캐릭터 몸통으로 들어오는지는 사용자의 로컬 실제 플레이 테스트로 검증 필요

### 볼 궤적 조준 개선 (`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`)

실제 플레이 테스트 중 궤적 프리뷰와 조준 관련 다섯 가지 문제가 순차적으로 발견되어 하나의 task로 정리하여 수정하였다.

- **배경**: 이슈 1~3(궤적이 터치할 때만 보임, 손가락 방향과 궤적 각도의 미묘한 어긋남, 궤적 프리뷰 색상/크기가 원본과 다름)을 먼저 research.md/plan.md로 정리해 구현하였다. 이후 이슈 2 구현 완료분에 대한 실제 플레이 테스트 과정에서 이슈 4(조준 모델 자체의 괴리감)가, 이슈 4 구현 완료분에 대한 실제 플레이 테스트 과정에서 이슈 5(터치 시작 단계 폴링 누락)가 추가로 재발견되어 같은 task 문서에 이어서 반영하였다.
- **이슈 1 (궤적 상시 표시)**: 기존 `TrajectoryPreview.cs`는 `InputHandler`의 `OnAimBegin`/`OnDrag`/`OnRelease` 이벤트에만 반응해 터치 중에만 궤적을 그리고 릴리즈 시 숨기는 구조였다. 몬스터가 볼 사이클과 무관하게 항상 이동하므로(`GameplayMechanics.md` 섹션 2) 터치하지 않는 동안에도 궤적 충돌 지점이 실시간으로 갱신되어야 정확하다는 점을 근거로, 이벤트 구독을 전부 제거하고 `Update()`에서 매 프레임 `BallLauncher.Instance.LaunchDirection`(터치 중엔 드래그 방향, 아닐 땐 마지막 조준 방향) 기준으로 궤적을 재계산하도록 전환하였다. `UIRules.md` 섹션 11의 "조준 중에만 표시" 문구도 함께 갱신하였다.
- **이슈 2 (조준 정확도)**: `InputHandler.cs`가 스크린(픽셀) 좌표 차이를 스크린→월드 변환 없이 그대로 정규화해 조준 방향으로 쓰고 있어, 화면 종횡비/투영 배율에 따라 손가락 방향과 궤적 각도가 어긋나는 왜곡이 발생하고 있었다. `Camera.main`을 `Awake()`에서 캐싱한 뒤 `ScreenToWorldPoint`로 변환한 월드 좌표 기준으로 방향을 계산하도록 수정하였다.
- **이슈 3 (색상/크기 불일치)**: `_hitDot`과 `_hitRing`이 둘 다 `_hitColor`(순수 빨강)를 참조하는 버그가 있어 링도 의도와 달리 빨간색으로 그려지고 있었다. 원본 게임(`Assets/_Project/Docs/targetUI/`) 스크린샷을 픽셀 단위로 실측하여, 링은 `_ringColor`(회백색 계열)로 분리하고 레드닷은 톤 다운된 브릭레드로, `_dotRadius`는 더 작게, 점선 주기(`DASH_WORLD_SIZE`)는 더 촘촘하게, 점선 색(`_lineColor`)은 살짝 톤 다운된 회백색으로 각각 조정하였다.
- **이슈 4 (조준 모델의 괴리감, 이슈 2 구현 완료 후 재발견)**: 이슈 2 반영 이후 실 플레이 테스트에서, 터치 시작 지점을 고정 기준점으로 삼아 그로부터의 상대 이동량을 방향으로 쓰는 기존 "상대 드래그" 모델 자체가 손가락과 궤적 사이의 괴리감을 유발한다는 점이 확인되었다. `BallLauncher.Instance.LaunchPoint`(발사 지점)에서 현재 손가락 위치를 향하는 절대 방향을 매 프레임(터치 시작 프레임 포함) 계산하는 "절대 조준" 모델로 전환하였다(`ComputeAimDirection` 헬퍼 신설). `GameplayMechanics.md` 섹션 1의 기존 스펙 문구("터치하는 순간 조준 방향이 즉시 정해진다", "드래그 위치를 목표로 실시간으로 따라간다")가 상대 드래그 모델보다 이 절대 조준 모델과 더 정확히 부합한다는 점도 확인되어, 해당 스펙 서술 자체는 그대로 유지하였다.
- **이슈 5 (터치 시작 폴링 누락, 이슈 4 구현 완료 후 재발견)**: 이슈 4 반영 이후 실 플레이 테스트에서, 터치를 대자마자 바로 살짝 움직이면 같은 프레임에 `TouchPhase.Began`과 `Moved`가 뭉개져 `Began`이 관측되지 않고 곧바로 드래그로 인식되는 버그가 발견되었다. `TouchPhase.Began` 값 자체에 의존하던 시작 판정을 "아직 드래그 중이 아닌 상태(`!_isDragging`)에서 터치가 감지되면 phase와 무관하게 그 자체를 시작으로 취급"하는 `_isDragging` 상태 기반 판정으로 재구성하였다. 이 과정에서 마우스 분기도 터치와 동일한 구조로 통일하여, 기존 클릭 첫 프레임의 `OnDrag` 중복 발행 문제도 함께 정리되었다.
- **검증**: 이슈 1~5 모두 `InputHandler.cs`/`TrajectoryPreview.cs`에 구현되어 main에 반영되었으며, 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인하였다. 상세 내용은 `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/research.md`, `plan.md` 참고.
