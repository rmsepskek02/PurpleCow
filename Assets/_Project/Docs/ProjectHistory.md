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
