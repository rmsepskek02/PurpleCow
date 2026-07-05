## 2026-07-05 (볼-볼 충돌 버그 research)

### 작업 내용
- 볼-볼 물리 충돌 버그 task research.md 생성 (오케스트레이터가 사전 조사한 원인을 코드/설정 파일 직접 열람으로 검증)
- 경로: `Assets/_Project/Docs/_Task/2026-07-05/16-40_ball-ball-collision-fix/research.md`

### 결과
- `Ball.prefab`(m_Layer:0, Untagged), `Physics2DSettings.asset`(m_LayerCollisionMatrix 전부 ffff), `TagManager.asset`(태그 3개만 등록, 레이어 전부 미사용) 직접 확인
- `SceneSetupEditor.cs`/`MonsterSetupEditor.cs`에 레이어 설정 코드 전무함을 확인, Monster/Block 프리팹 4+4종 및 실제 게임 씬(`Assets/Scenes/SampleScene.unity`) 전체 오브젝트가 m_Layer:0임을 grep으로 재확인
- 부가 발견: `SceneSetupEditor.Step1_CreateBallPrefab()`이 `TrySetTag(go, "Ball")`을 호출하지만 "Ball" 태그가 `BallSetupEditor.AddRequiredTags()`의 등록 목록(Monster/Wall/Ground)에 빠져 있어 조용히 실패 → Ball.prefab이 Untagged로 남은 실제 이유임을 특정
- `TrajectoryPreview.cs` line 96 주석이 이 "태그 없음" 상태를 다른 볼 필터링에 의존하고 있음을 확인 → 해결 방식 선택 시 상호작용 검토 필요 항목으로 명시
- `ObjectPool.Get()`이 고정 크기가 아니라 동적으로 늘어나는 구조임을 검증, 이를 근거로 해결 방식 (a) 전용 Ball 레이어 신설+`IgnoreLayerCollision` vs (b) 볼 쌍마다 `IgnoreCollision` 두 후보의 트레이드오프를 "문제점/구현 대상 파악" 섹션에 정리
- AGENTS.md는 "개별 task 폴더 목록은 별도 관리하지 않음" 정책이 명시되어 있어 인덱스 등록 생략 (정책 재확인 후 판단)

### 주요 결정사항
- plan.md는 작성하지 않음 (사용자 확인 대기, TaskRules.md 워크플로우 준수)
- 코드/프리팹/ProjectSettings 파일은 읽기만 하고 수정하지 않음
- 열린 질문(사용자 확인 필요): (a) 전용 Ball 레이어 신설 방식 vs (b) 볼 쌍마다 IgnoreCollision 호출 방식 중 선택 필요, (a) 선택 시 TrajectoryPreview.cs의 태그 기반 필터링 로직과의 상호작용 처리 방식도 함께 결정 필요

---

## 2026-07-05 (추가 정정)

### 작업 내용
- UIRules.md 섹션 11 "구현 방식" 항목 재정정: dev 에이전트가 `_hitRing`의 텍스처 타일링 방식(`CreateRingDashTexture()`, `RING_DASH_COUNT`, `mainTextureScale` 계산)을 완전 폐기하고 `LineRenderer.colorGradient` 기반 방식(`BuildRingDashGradient()`)으로 교체한 실제 구현(오케스트레이터가 git diff로 검증)에 맞춰 문서 서술 갱신
- 경로: `Assets/_Project/Docs/UIRules.md` (코드 미수정, GameplayMechanics.md는 확인만 하고 변경 없음)

### 결과
- 섹션 11 "구현 방식" 고리(ring) 관련 문단: `_hitRing`도 레드닷과 동일하게 `CreateSolidTexture()`(단색 텍스처)를 사용하고, 점선 효과는 `_hitRing.colorGradient`(`BuildRingDashGradient()`)의 alphaKeys 8개(4등분 중앙 t=0/0.25/0.5/0.75 알파 1, 경계 t=0.125/0.375/0.625/0.875 알파 0)로 만들어 정확히 4개의 밝은 호가 나타남을 서술, 폐기된 이전 텍스처 타일링 방식(10개 의도했으나 실제 2개로 렌더링되어 폐기)도 참고용으로 한 문장 남김
- 회전 로직 문단에 "`colorGradient`는 정점 인덱스 기준으로 알파를 매기므로 정점 각도가 회전해도 4개 호 형태가 유지된 채 함께 회전한다" 설명 추가 (회전 로직 자체는 `rotationOffsetDeg`/`Time.time * _ringRotationSpeed`로 기존과 동일, 변경 없음)
- Inspector 조절 값 표 `_ringRotationSpeed` 행은 변경 없이 유지
- 신규 문서 생성 없음 → AGENTS.md 인덱스 변경 불필요

### 주요 결정사항
- 코드는 이미 구현 완료 상태이므로 건드리지 않고 문서만 실제 구현에 정확히 맞춤
- GameplayMechanics.md는 사용자 관점 서술("점선(끊어진 호) + 시계방향 회전")만 담고 구현 디테일(텍스처/Gradient)을 언급하지 않으므로 이번 정정 범위에서 제외

---

## 2026-07-05

### 작업 내용
- `_Task/2026-07-05/11-20_trajectory-ring-dash-rotate/plan.md` STEP 3~4에 따라 GameplayMechanics.md / UIRules.md 갱신 (dev 에이전트가 이미 구현 완료한 `TrajectoryPreview.cs`의 실제 코드 내용을 오케스트레이터가 git diff로 검증해 전달, 이를 문서에 반영만 함 — 코드 미수정)
- 경로: `Assets/_Project/Docs/GameplayMechanics.md`, `Assets/_Project/Docs/UIRules.md`

### 결과
- GameplayMechanics.md 섹션 1: 2차 충돌 지점 고리 설명 문장에 "실선이 아니라 끊어진 점선 형태이며, 조준(터치) 여부와 무관하게 항상 시계방향으로 계속 회전한다" 추가
- UIRules.md 섹션 11: (1) 2차 충돌 지점 서술에 점선/회전 특징 추가, (2) 구현 방식 항목을 레드닷(`CreateSolidTexture()`, 회전 없음)과 고리(`CreateRingDashTexture()`, `RING_DASH_COUNT=10`, 둘레 기준 스케일)로 분리 서술 + 회전이 `DrawCircle()`의 `rotationOffsetDeg` 파라미터(`angle = 기존각도 - offsetRad`, `Time.time * _ringRotationSpeed`)로 구현되며 터치 여부와 무관하게 항상 진행됨을 명시, (3) Inspector 조절 값 표에 `_ringRotationSpeed`(deg/sec, 기본값 90) 행 추가
- 신규 문서 생성 없음 → AGENTS.md 인덱스 변경 불필요

### 주요 결정사항
- 코드/씬 파일은 건드리지 않고 문서 서술만 실제 구현(오케스트레이터 제공 git diff 검증 내용)에 정확히 맞춰 갱신
- GameplayMechanics.md의 "구현 현황" 하위 섹션(task 이력 기록)은 이번 지시 범위(섹션 1의 특정 문장 정정)를 벗어나므로 건드리지 않음 (외과적 변경 원칙)

---

## 2026-07-04

### 작업 내용
- 몬스터 시스템 개편 task plan.md 생성
- 경로: Assets/_Project/Docs/_Task/2026-07-04/22-53_monster-system-overhaul/plan.md
- research.md는 이미 사용자 확인 완료 상태였으며, 오케스트레이터-사용자 논의로 확정된 설계 결정사항을 그대로 STEP 1~6으로 구조화

### 결과
- plan.md 생성 완료: 구현 목표, STEP 1(MonsterData BlockSize 필드) ~ STEP 6(MonsterSetupEditor 정리) 상세 계획, 예상 변경/생성 파일 목록 테이블(코드 5종 + 프리팹 4종 + asset 2종), 주의사항 5건 작성
- AGENTS.md는 "개별 task 폴더 목록은 별도 관리하지 않음" 정책이 이미 명시되어 있어 이번 task 인덱스 등록은 생략(현재 AGENTS.md 정책 확인 후 판단)

### 주요 결정사항
- MonsterData에 BlockSize enum(OneByOne/TwoByOne/OneByTwo) 추가, MonsterBase가 OnSpawn/ApplyData 시점에 콜라이더 크기와 HP바 폭을 이 값 기준으로 런타임 자동 적용(데이터가 단일 기준)
- 프리팹에 BlockVisual 자식(SpriteRenderer 전용, 콜라이더 없음) 추가해 블록+캐릭터 합성, 콜라이더/MonsterBase/tag는 루트 유지 → Ball.cs 완전히 수정 불필요
- HpBarCanvas를 BlockVisual 자식으로 재배치(MonsterHpBar.cs의 GetComponentInParent는 그대로 MonsterBase 탐색 가능하므로 스크립트 무수정)
- WaveTableData: WaveEntry/MonsterSpawnEntry(좌표 포함) 구조 완전 제거 → 공식 파라미터(baseSpawnCount/spawnCountPerWave/baseTwoCellWeight/twoCellWeightPerWave/totalWaves) + MonsterData 4종 참조 구조로 전환
- WaveManager.SpawnWave(): 9열×5행 그리드, 웨이브 인덱스 기반 공식으로 spawnCount/twoCellWeight 계산, 그리드 용량 기준 상한(약 22)으로 사전 제한, occupied bool[,] 매 웨이브 재초기화, 2칸 몬스터 우선 배치 후 1칸 몬스터 배치
- MonsterSetupEditor.SetupWaveSpawnEntries() 전체 폐기, CreateWaveDataAssets/CreateMonsterDataAssets 새 구조에 맞게 재작성 필요
- WaveTableData.asset/MonsterData_*.asset 실제 재생성은 로컬 Unity 에디터 실행 필요 — 주의사항에 명시

---

### 작업 내용 (추가)
- 몬스터 시스템 개편 plan.md의 STEP 6 수정 (사용자 피드백 반영)
- 경로: Assets/_Project/Docs/_Task/2026-07-04/22-53_monster-system-overhaul/plan.md

### 결과
- STEP 6 제목을 "MonsterSetupEditor.cs 대폭 정리" → "신규 MonsterOverhaulSetupEditor.cs 작성 (새 데이터 구조 세팅 전용)"으로 교체
- 예상 변경/생성 파일 목록 테이블에서 MonsterSetupEditor.cs 행 제거, MonsterOverhaulSetupEditor.cs(신규 생성) 행으로 교체
- 주의사항에 "기존 MonsterSetupEditor.cs는 수정하지 않는다" 문장 신규 추가(6번), 5번 항목에 "결정한 수치는 구현 완료 보고 시 함께 명시한다" 문구 추가
- STEP 1~5, 구현 목표, 서두 요약 등 나머지 섹션은 전혀 건드리지 않음

### 주요 결정사항
- 기존 MonsterSetupEditor.cs는 이번 작업에서 전혀 수정하지 않음 — 문자열 기반 리플렉션 API(SerializedObject/FindProperty)를 쓰므로 데이터 구조가 바뀌어도 컴파일에는 영향 없음을 확인, 기존 메뉴 항목은 쓸모없어져도 그대로 방치(제거하지 않음)
- 새 데이터 구조(MonsterData._blockSize 채우기, WaveTableData.asset 신규 파라미터 구조 생성/갱신, 프리팹 4종 BlockVisual 합성 + HpBarCanvas 재배치)는 전부 신규 MonsterOverhaulSetupEditor.cs(별도 MenuItem: PurpleCow/Setup/Monster Overhaul Setup)가 독립적으로 담당
- 이전 배경 정사각형 보정 task에서 BackgroundGridFitSetupEditor.cs를 분리했던 전례와 동일한 원칙(기존 다역할 에디터 스크립트를 계속 편집하면 혼선 발생) 적용

---

## 2026-06-30

### 작업 내용
- TaskRules.md 폴더명 포맷 HH:MM → HH-MM 으로 수정 (Windows 콜론 제약)
- Core 시스템 task 문서 생성: research.md, plan.md

### 결과
- Assets/_Project/Docs/_Task/2026-06-30/02-30_Core시스템구현/ 폴더 및 문서 생성 완료

### 주요 결정사항
- Windows에서 폴더명 콜론 사용 불가 → HH-MM 형식 채택

---

### 작업 내용 (추가)
- ProjectHistory.md에 2026-06-30 상세 섹션 추가 (에이전트 시스템, 문서 시스템, 아키텍처 결정, Core task 문서 현황)
- ProjectStatus.md 전면 갱신 (현재 상태, 완료 체크리스트, 기술 결정 표, 리소스 현황)

### 결과
- 두 문서 모두 업데이트 완료

### 주요 결정사항
- ProjectHistory.md는 기존 내용 유지 + 신규 섹션 추가 방식으로 편집

---

### 작업 내용 (추가)
- Ball 시스템 구현 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/10-00_Ball시스템구현/

### 결과
- research.md: Ball 관련 파일 현황, Core 의존성, 구현 대상 클래스 목록, 게임 요구사항 분석 작성 완료
- plan.md: BallData(SO), Ball(MonoBehaviour+IPoolable), BallLauncher(Singleton) 3종 상세 구현 계획 작성 완료

### 주요 결정사항
- Ball 폴더: .cs 파일 없음 (신규 생성 필요)
- 스프라이트 6종(Normal, Fire, Ice, Ghost, Laser, Cluster) 이미 준비됨
- 이번 task 범위: Normal 볼 기본 동작만 구현, 특수 볼은 스킬 시스템 task에서 확장
- BallData는 ScriptableObject로 분리 (하드코딩 금지 원칙 적용)
- Wall 반사는 PhysicsMaterial2D(bounciness=1)로 처리, 코드 불필요
- OnHitMonster / OnAllBallsReturned 이벤트는 발행만 하고 구독자는 후속 시스템에서 추가
- Tag("Monster", "Wall", "Ground") 및 BallData SO 에셋은 Unity Editor에서 수동 생성 필요

---

### 작업 내용 (추가)
- Ball 시스템 plan.md 수정: Editor 자동화 스크립트(BallSetupEditor) 추가
- 수정 대상: Assets/_Project/Docs/_Task/2026-06-30/10-00_Ball시스템구현/plan.md

### 결과
- 생성 파일 목록에 BallSetupEditor.cs (Assets/_Project/Scripts/Editor/) 추가
- 예상 변경/생성 파일 목록에도 동일하게 추가
- Step 4. BallSetupEditor 새 섹션 추가 (MenuItem, 수행 작업 3가지, 마무리 처리 기술)
- 주의사항 수정: "PhysicsMaterial2D 직접 조작", "Tag 수동 추가", "BallData SO 수동 생성" 항목 삭제
- 새 주의사항 1번으로 "BallSetupEditor 실행 필요" 항목 추가
- 번호 재정렬 완료 (총 5개 항목)

### 주요 결정사항
- Editor 자동화로 PhysicsMaterial2D 생성, Tag 등록, BallData SO 에셋 생성을 모두 커버
- Ball 프리팹 Rigidbody2D 설정(Continuous, GravityScale=0)은 여전히 Editor 직접 조작 필요 (프리팹 생성 자체가 Editor 작업이므로)

---

### 작업 내용 (추가)
- Monster 시스템 구현 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/14-00_Monster시스템구현/

### 결과
- research.md: Monster/Wave 폴더 현황, Core/Ball 의존성, Ball.OnHitMonster 시그니처 분석, 충돌 데미지 처리 방식 결정, 구현 대상 클래스 목록 작성 완료
- plan.md: MonsterData(SO), MonsterBase(MonoBehaviour+IPoolable), WaveData(SO), WaveManager(Singleton), MonsterSetupEditor(Editor) 5종 상세 구현 계획 작성 완료

### 주요 결정사항
- Monster 폴더: .cs 파일 없음 (신규 생성 필요)
- 스프라이트 8종(캐릭터 4종 + 블록 4종) 이미 준비됨
- Ball.OnHitMonster가 static event이므로 MonsterBase는 OnCollisionEnter2D로 Ball 감지 후 Ball.LastDamage 프로퍼티로 데미지 수신
- Ball.cs에 LastDamage public 프로퍼티 추가 필요 (외과적 변경 1건)
- MonsterBase.OnMonsterDied static event → WaveManager가 구독하여 생존 몬스터 추적
- BallLauncher.OnAllBallsReturned 구독 → WaveManager가 턴 종료 감지 → 몬스터 전진
- MonsterData / WaveData는 ScriptableObject로 분리 (하드코딩 금지 원칙 적용)
- WaveData SpawnEntries는 Editor Inspector에서 수동 편집 (MonsterSetupEditor는 빈 에셋만 생성)

---

### 작업 내용 (추가)
- Skill 시스템 구현 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/18-00_Skill시스템구현/

### 결과
- research.md: Skill 폴더 현황(Base/Active/Passive 폴더만 존재, 스크립트 없음), 스프라이트 현황(Active 6종, Passive 7종), Core/Ball/Monster 의존성 분석, 구현 대상 목록 작성 완료
- plan.md: SkillData(SO), BallSkillBase(abstract), PassiveSkillBase(abstract), Active 5종(Fire/Ice/Ghost/Laser/Cluster), Passive 7종(3000/3002/3003/3006/3007/3013/3014), SkillManager(Singleton), SkillSetupEditor(Editor), Ball/BallLauncher/MonsterBase/WaveManager 외과적 수정 계획 포함, 총 21개 파일 변경/생성 계획 작성 완료

### 주요 결정사항
- Active 스킬은 BallSkillBase(MonoBehaviour)를 Ball에 부착하는 컴포넌트 방식 채택
- Passive 스킬은 PassiveSkillBase(순수 C# 클래스)로 구현, Apply()/Remove()로 이벤트 구독/해제
- SkillManager가 Passive 보너스 누적값(데미지/크리티컬/속도/반사)을 관리, Ball이 참조
- Ball.cs에 SetSkill(), SetGhostMode(), ForceReturn(), LaunchDirection 프로퍼티, OnBeforeReturn 이벤트 추가
- MonsterBase.cs에 ApplyFreeze(), IsFrozen 프로퍼티, _frozenTurnsRemaining 필드 추가
- BallLauncher.cs에 LaunchSubBalls() 추가 (Cluster/KillShot 연동)
- WaveManager.cs에 GetWeakestMonster() 추가 (LastHitPassive 연동)
- PDF 스킬 수치는 확인 후 SkillData SO 에셋에 반영 필요 (plan.md에 주의사항 명시)

---

### 작업 내용 (추가)
- UI 시스템 구현 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/20-00_UI시스템구현/

### 결과
- research.md: UI 폴더 현황(스크립트 없음), 기존 시스템 이벤트 목록 분석, GameState 상태 머신, WaveManager 흐름, SkillManager 구조, 구현 대상 5종 파악 완료
- plan.md: UIManager(Singleton), HUDPanel, ResultPanel, SkillSelectionPanel, SkillCardUI, SkillFactory 6종 상세 구현 계획 + WaveManager/GameManager/BallSkillBase 외과적 수정 포함, 총 6개 신규 파일 + 5개 기존 파일 수정 계획 작성 완료
- AGENTS.md Task 문서 섹션에 2026-06-30 날짜별 task 목록 테이블 추가

### 주요 결정사항
- UI 스크립트 디렉터리(Assets/_Project/Scripts/UI/) 신규 생성 필요
- BallSkillBase를 MonoBehaviour에서 순수 C# 클래스로 변환(옵션 A 채택) → SkillFactory 정적 팩토리 패턴으로 스킬 인스턴스 생성
- 점수는 ScoreManager 분리 없이 UIManager 내부에서 MonsterBase.OnMonsterDied 구독으로 단순 관리
- 스킬 선택 중 발사 차단: BallLauncher에 SetLaunchEnabled(bool) 추가 방식 권장
- WaveManager: OnWaveCleared 이벤트 추가, AdvanceToNextWave() public 전환, TotalWaves 프로퍼티 추가
- GameManager: IsLastGameSuccess 프로퍼티 추가, EndGame()에서 저장
- PDF 파일 열람 불가(poppler-utils 미설치)로 PDF 요구사항 직접 반영 불가 — 코드베이스 분석으로 대체

---

### 작업 내용 (추가)
- EditorSetup 개선 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_EditorSetup개선/

### 결과
- research.md: SkillSetupEditor 아이콘 경로 오타 4곳 특정 (라인 55/67/79/91), 관련 스크립트 7종(Ball, BallLauncher, MonsterBase, WaveManager, GameManager, InputHandler, SkillManager) 컴포넌트 구조 분석 완료
- plan.md: 작업 1(오타 수정 4건) + 작업 2(SceneSetupEditor 7단계 구현 계획, 프리팹 9종 + 씬 오브젝트) 작성 완료

### 주요 결정사항
- SkillSetupEditor 오타: Ball_Ice/Ghost/Laser/Cluster _ball.png → _Ball.png (4곳), Ball_Fire는 요청서에 없어 제외
- SceneSetupEditor Step 7 BallLauncher 연결: _ballPrefab + _poolParent(PoolRoot) 자동 연결, _launchPoint는 수동 연결 안내
- MonsterData/BallData SO는 자동 연결 제외 (DataSetupEditor 또는 수동 처리)
- PhysicsMaterial2D(BallBounce) 경로 미정 → 자동 연결 보류, 경고 로그로 안내
- UIManager 스크립트 미존재 가능성 → try-catch 예외 처리 후 계속 진행

---

### 작업 내용 (추가)
- PDF 스펙 정합 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_PDF스펙정합/

### 결과
- research.md: 7개 항목(BallData 기본값, 레벨 시스템, 액티브 5종 로직, 패시브 7→5 교체, 장착 제한, 선택 로직, MonsterData 이름) 현재 상태와 문제점 분석 완료
- plan.md: STEP 1~12 상세 구현 계획 작성 완료 (수정 16개, 삭제 7개, 생성 5개 파일)

### 주요 결정사항
- SkillData 레벨 구조: SkillLevelData 구조체 + _levels[3] 배열 방식 채택 (기존 _value1~3 제거)
- PassiveSkillId 열거형: 기존 7개 전부 제거, WarmTinHeart/MagicMirror/AmethystDagger/EmeraldDagger/LastMatch 5개로 재정의
- 스킬 선택 트리거: OnWaveCleared → OnKillCountReached (WaveManager에 처치 수 카운터 추가)
- 볼 데미지 오버라이드: 스킬 활성화 시 BallData.Damage 대신 SkillData.BallDamage 사용 구조 도입 필요
- 전면/후면 판정: 볼 이동 방향 vs 몬스터 정면 방향 내적으로 판정 (몬스터는 항상 아래를 향한다고 가정)
- LaserBallSkill: 직선 전체 Raycast → 같은 행(Y좌표 동일) 몬스터 추가 피해로 변경
- SlowDuration: Time.deltaTime 아닌 턴 기반(MoveDown 호출 시 차감) 방식 채택

---

### 작업 내용 (추가)
- UIRules.md 신규 생성 (Assets/_Project/Docs/UIRules.md)
- AGENTS.md Docs 문서 인덱스에 UIRules.md 등록

### 결과
- UIRules.md 생성 완료: Canvas 구조/레이어, 해상도 대응, Safe Area, 패널 표시/숨김, UI 애니메이션, 버튼 피드백, 성능 최적화 7개 섹션 포함
- AGENTS.md 인덱스 업데이트 완료

### 주요 결정사항
- DevRules.md 스타일(마크다운 테이블, 코드블록, 한국어 설명) 일관성 유지
- 사용자 확정 내용 그대로 반영, 임의 추가 없음

---

### 작업 내용 (추가)
- UIRules.md 수정 (Assets/_Project/Docs/UIRules.md)
- Canvas_HUD 구조에 CharacterXP 항목 추가 (섹션 1)
- 섹션 8 (데미지 텍스트), 섹션 9 (몬스터 HP바), 섹션 10 (캐릭터 HP / 경험치 / 레벨 시스템) 신규 추가

### 결과
- UIRules.md 수정 완료: 총 4곳 변경 (CharacterXP 항목, 섹션 8/9/10 추가)

### 주요 결정사항
- DamageTextFx: World Space TMP 직접 배치 방식 (Canvas 없음)
- MonsterHpBar: 각 몬스터 프리팹에 World Space Canvas + Slider 자식으로 부착
- CharacterManager가 HP, XP, 레벨 모두 담당 (Singleton)
- WaveManager에 OnMonsterReachedBottom static event 추가 필요 (CharacterManager 연동)
- XP 획득 조건: 몬스터 처치 및 통과 모두 reward만큼 획득

---

### 작업 내용 (추가)
- UI 전체 재작업 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_UI재작업/
- AGENTS.md Task 문서 섹션에 HH-MM_UI재작업 항목 추가

### 결과
- research.md: UI 스크립트 5종 문제점 분석, 미구현 항목 8종 목록, 의존성 누락(OnHpChanged/OnMonsterReachedBottom 이벤트, DOTween 패키지) 기록 완료
- plan.md: STEP 0~12 상세 구현 계획 작성 완료 (신규 8종, 수정 7종, DOTween 선행 설치 안내 포함)

### 주요 결정사항
- DOTween 패키지 미설치 확인 → STEP 0으로 선행 설치 명시
- UIManager OnWaveCleared 구독이 이중 트리거 버그 유발 → STEP 3에서 제거
- SkillSelectionPanel OnEnable ShowRandomSkills() 호출이 CanvasGroup 방식 전환 시 타이밍 버그 유발 → STEP 12에서 제거
- CheckGameOver의 직접 EndGame 호출 제거 → CharacterManager가 HP 0 시 EndGame 담당 (STEP 8, 9 동시 구현 필요)
- MonsterBase에 Data 프로퍼티(public MonsterData Data) 추가 필요 (STEP 6에서 외과적 추가)

---

### 작업 내용 (추가)
- QA 수정 task research.md 생성
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_QA수정/research.md

### 결과
- research.md 생성 완료: CRITICAL 5건, WARNING 6건, INFO 3건 총 14개 항목을 현재 상태 / 관련 파일 의존성 / 문제점 분석 / 결론 구조로 작성

### 주요 결정사항
- CRITICAL 1(스킬 Lv3 도달 불가): LevelUp() 조건 off-by-one 수정 필요, SkillSelectionPanel 동일 조건 함께 수정
- CRITICAL 2(WaveData MonsterData 미반영): SpawnWave()에서 entry.Data를 MonsterBase에 주입하는 코드 추가 필요
- CRITICAL 3(Time.timeScale 미처리): OpenPanel/OnSkillSelectionComplete에 timeScale 0/1 설정 추가 필요
- CRITICAL 4(재시작 초기화 미구현): 씬 재로드 vs 개별 Reset 방식 결정이 plan.md 단계에서 필요
- CRITICAL 5(스킬 인스턴스 공유): 레벨업 후 각 Ball에 새 인스턴스 재적용 구조 필요
- WARNING 2(패시브 이중 구독): CRITICAL에 준하는 우선순위로 처리 권장

---

### 작업 내용 (추가)
- QA 수정 task plan.md 생성
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_QA수정/plan.md

### 결과
- plan.md 생성 완료: STEP 1(CRITICAL 2 — WaveData MonsterData 미반영 수정) 상세 구현 계획 작성
- 논의 예정 항목 8건을 plan.md 하단에 별도 섹션으로 나열 (CRITICAL 3/4, WARNING 2/3/4/6, INFO 2/3)

### 주요 결정사항
- ApplyData(MonsterData data) 메서드: _monsterData 교체 → _currentHp 재초기화 → OnHpChanged 이벤트 발행 순서
- WaveManager.SpawnWave()에서 entry.Data != null 조건부로 ApplyData() 호출 (프리팹 기본값 보호)
- 나머지 항목은 논의 완료 후 STEP으로 순차 추가 예정

---

### 작업 내용 (추가)
- QA 수정 task plan.md에 STEP 2(CRITICAL 3 — 스킬 선택 중 게임 일시정지 처리) 추가
- "논의 예정 항목"에서 CRITICAL 3 항목 제거
- 서문 설명 업데이트 (CRITICAL 2 + CRITICAL 3 두 건 확정 명시)

### 결과
- plan.md 수정 완료: STEP 2 신규 추가, 파일 목록에 SkillSelectionPanel.cs / UIManager.cs 추가, 논의 예정 항목에서 CRITICAL 3 제거

### 주요 결정사항
- SkillSelectionPanel.OpenPanel()에 Time.timeScale = 0f 추가
- UIManager.OnSkillSelectionComplete()에 Time.timeScale = 1f 추가
- SkillSelectionPanel.Show()/Hide() DOTween Sequence에 .SetUpdate(true) 추가 (timeScale=0 시 unscaled time 애니메이션 재생)
- ResultPanel/HUDPanel은 게임이 이미 멈춘 상태에서 열리므로 SetUpdate 불필요 — SkillSelectionPanel만 해당

---

### 작업 내용 (추가)
- QA 수정 task plan.md에 STEP 3(CRITICAL 4 — 재시작 초기화 구현) 추가
- "논의 예정 항목"에서 CRITICAL 4 항목 제거
- 서문 설명 업데이트 (CRITICAL 2 + CRITICAL 3 + CRITICAL 4 세 건 확정 명시)

### 결과
- plan.md 수정 완료: STEP 3 신규 추가, 파일 목록에 SkillData.cs / SkillSelectionPanel.cs(추가 로직) / GameManager.cs 추가, 논의 예정 항목에서 CRITICAL 4 제거

### 주요 결정사항
- SceneManager.LoadScene으로 MonoBehaviour 기반 시스템 전체 초기화
- SkillData는 ScriptableObject 에셋이라 씬 재로드로 리셋 안 됨 → SkillSelectionPanel이 GameState.Ready 수신 시 명시적으로 ResetLevel() 호출
- GameManager.RestartGame()에 using UnityEngine.SceneManagement + SceneManager.LoadScene(buildIndex) 추가

---

### 작업 내용 (추가)
- QA 수정 task plan.md에 STEP 4(WARNING 2 — 패시브 스킬 레벨업 시 이벤트 이중 구독 수정) 추가
- "논의 예정 항목"에서 WARNING 2 항목 제거
- 서문 설명 업데이트 (CRITICAL 2/3/4 + WARNING 2 네 건 확정 명시)

### 결과
- plan.md 수정 완료: STEP 4 신규 추가, 파일 목록에 SkillManager.cs 추가, 논의 예정 항목에서 WARNING 2 제거

### 주요 결정사항
- AddPassiveSkill() 레벨업 분기: existing.Remove() → existing.SkillData.LevelUp() → existing.Apply() 순서로 수정
- 이벤트 구독 방식 패시브(MagicMirror, AmethystDagger, EmeraldDagger, LastMatch)는 Apply() 중복 호출 시 핸들러 누적 → Remove() 선행 필수
- WarmTinHeart는 AddDamageMultiplier() 배율 누적 방지를 위해 Remove() 선행 필수

---

### 작업 내용 (추가)
- QA 수정 task plan.md에 STEP 5(WARNING 3 — MonsterBase 빈 이벤트 핸들러 제거) 및 STEP 6(WARNING 4 — SkillSelectionPanel Hide() 이중 호출 수정) 추가
- "논의 예정 항목"에서 WARNING 3, WARNING 4 두 항목 제거
- 서문 설명 업데이트 (CRITICAL 2/3/4 + WARNING 2/3/4 여섯 건 확정 명시)

### 결과
- plan.md 수정 완료: STEP 5/6 신규 추가, 파일 목록에 MonsterBase.cs(핸들러 제거) / SkillSelectionPanel.cs(Hide 중복 제거) 추가, 논의 예정 항목에서 WARNING 3/4 제거

### 주요 결정사항
- MonsterBase: OnHitMonster 구독/해제 라인 및 빈 HandleHitMonster() 메서드 전체 제거 (Ball.CalculateDamage()에서 TakeDamage() 직접 호출로 이미 대체된 잔재 코드)
- SkillSelectionPanel.OnSkillSelected()에서 직접 호출하는 Hide() 라인 제거 (UIManager.OnSkillSelectionComplete() 내부에서 이미 ShowSkillSelection(false) → Hide() 호출하므로 DOTween Sequence 이중 실행 방지)

---

### 작업 내용 (추가)
- Inspector 연결 및 에디터 수정 task plan.md 생성
- 경로: Assets/_Project/Docs/_Task/2026-06-30/HH-MM_Inspector연결및에디터수정/plan.md
- AGENTS.md Task 문서 섹션에 누락된 항목 4건(HH-MM_EditorSetup개선, HH-MM_PDF스펙정합, HH-MM_QA수정, HH-MM_Inspector연결및에디터수정) 일괄 등록

### 결과
- plan.md 생성 완료: STEP 1~6 상세 계획 작성 (코드 수정 2건 + Inspector 연결 4건)
- AGENTS.md 인덱스 업데이트 완료

### 주요 결정사항
- STEP 1: BallSetupEditor.CreateBallDataAsset()에 _maxBounces = 10 초기값 추가 (_criticalMultiplier 라인 바로 아래)
- STEP 2: SceneSetupEditor.Step6_PlaceManagers()에서 PlaceManager<DamageTextManager>() 호출 제거 (UISetupEditor가 전담)
- STEP 3: Ball.prefab에 BallData.asset 연결, _maxBounces = 10, BallBounce.physicsMaterial2D 연결
- STEP 4: Monster 프리팹 4종(Fluffy/Spider/StoneBug/ForestDeer)에 각 MonsterData 에셋 연결
- STEP 5: 씬 WaveManager에 _waveDatas 20개, _monsterPrefab, _poolParent, _spawnRoot 연결
- STEP 6: 씬 DamageTextManager에 _prefab(DamageTextFx), _poolParent(DamageTextPool) 연결

---

### 작업 내용 (추가)
- Inspector 연결 및 에디터 수정 task research.md 생성
- 경로: Assets/_Project/Docs/_Task/2026-06-30/15-50_Inspector연결및에디터수정/research.md

### 결과
- research.md 생성 완료: 코드 문제 2건 + Inspector 미연결 4건 현재 상태, 의존성 테이블, 문제점 분석, 결론 작성

### 주요 결정사항
- 코드 수정 2건(BallSetupEditor _maxBounces 누락, SceneSetupEditor DamageTextManager 중복 생성)은 에디터 재실행 전 선행 수정 필요
- Inspector 연결 4건(Ball.prefab, Monster 프리팹 4종, 씬 WaveManager, 씬 DamageTextManager)은 씬/프리팹 저장 후 Push로 완료

---

### 작업 내용 (추가)
- Inspector 연결 및 에디터 수정 task plan.md 전면 수정
- 경로: Assets/_Project/Docs/_Task/2026-06-30/15-50_Inspector연결및에디터수정/plan.md
- STEP 3~6을 수동 드래그&드롭 방식에서 에디터 스크립트 자동화 방식으로 전환

### 결과
- plan.md 수정 완료: 서문, 구현 목표, STEP 3~6 내용, 예상 변경 파일 목록, 주의사항 전면 교체
- STEP 1~2는 완료 상태로 명시, STEP 3~6은 에디터 스크립트 코드 추가 방식으로 재작성

### 주요 결정사항
- STEP 3(SceneSetupEditor Step8_ConnectBallPrefabRefs): PrefabUtility.EditPrefabContentsScope로 Ball.prefab 열고 _ballData/PhysicsMaterial2D 자동 연결
- STEP 4(MonsterSetupEditor ConnectMonsterDataToPrefabs): 프리팹 4종 열고 _monsterData 자동 연결
- STEP 5(SceneSetupEditor Step9_ConnectWaveManagerRefs): 씬 WaveManager _waveDatas 20개 + _monsterPrefab + _poolParent + _spawnRoot 자동 연결
- STEP 6(UISetupEditor Step7_CreateDamageTextFxPrefab + Step8_ConnectDamageTextManagerRefs): DamageTextFx 프리팹 자동 생성 + DamageTextManager 참조 자동 연결
- 에디터 실행 순서: Ball System Setup → Monster System Setup → Scene Setup → UI Setup

---

## 2026-07-01

### 작업 내용
- plan.md 업데이트: Assets/_Project/Docs/_Task/2026-06-30/15-50_Inspector연결및에디터수정/plan.md
- ProjectStatus.md 업데이트: Assets/_Project/Docs/ProjectStatus.md
- ProjectHistory.md 업데이트: Assets/_Project/Docs/ProjectHistory.md
- AIFailures.md 업데이트: Assets/_Project/Docs/AIFailures.md

### 결과
- plan.md: STEP 3~6 완료 표시, STEP 7~13 신규 추가, 파일 목록 테이블 확장 (총 16개 파일)
- ProjectStatus.md: 날짜 2026-07-01, 단계 갱신, 완료 작업 2건 추가, 진행 중 비우기, 다음 작업 순서 교체
- ProjectHistory.md: 2026-07-01 섹션 추가 (에디터 스크립트 자동화 완성, 런타임 버그 수정 3건)
- AIFailures.md: 2026-07-01 섹션 추가 (씬 자동 저장 누락, 카메라 orthographic size 미설정)

### 주요 결정사항
- 브랜치: claude/recent-plan-review-xq2hsm에서 문서만 수정, 커밋/푸시는 Claude가 처리
- AssetDatabase.SaveAssets()만으로는 씬 저장 불가 → EditorSceneManager.SaveScene() 필수
- 카메라 orthographic size는 플레이 영역 폭(11)에 맞춰 10으로 설정

---

### 작업 내용 (추가)
- ReferenceUI이해.md 신규 생성: Assets/_Project/Docs/ReferenceUI이해.md
- 원본 게임(통통 디펜스: 핀볼 마스터) 실플레이 스크린샷 4장 기반, 사용자와 확정한 UI/게임플레이 이해를 기획 참고 문서로 정리
- AGENTS.md Docs 문서 인덱스에 ReferenceUI이해.md 등록

### 결과
- ReferenceUI이해.md 생성 완료: 배경(PDF 최우선, 제외 항목), 스테이지 진행률 바, 처치 게이지/레벨 배지, 플레이어 HP 바, Auto 버튼, 탄도 예측선(비행 중 재조준 포함), 데미지 표시 방식(파이어볼 DOT 수치 표 포함), 부유 아이템(볼 스프라이트 추정), Best 추천 아이콘, 미구현 갭 정리 표, 레퍼런스 이미지 플레이스홀더 섹션까지 총 11개 섹션 작성
- task 문서가 아닌 기획 참고 문서이므로 `_Task/` 폴더가 아닌 `Docs/` 최상위에 생성
- AGENTS.md 인덱스 업데이트 완료

### 주요 결정사항
- 파일명은 PlayerActiveSkill기획.md 등 기존 한글 파일명 컨벤션을 따라 `ReferenceUI이해.md`로 명명
- PDF 명시 구현 제외 항목(튜토리얼/배속/1스테이지보스/자동조준/리롤/융합)은 스크린샷 등장 여부와 무관하게 전부 제외로 명시
- 탄도 예측선의 "비행 중 재조준" 기능은 코드 확인이 안 된 상태이므로 미확정으로 별도 표시, 후속 코드 검토 필요 항목에 포함
- 이미지 파일은 아직 미추가 상태 — 섹션 11에 경로 플레이스홀더만 남기고 텍스트 설명만 기록

---

### 작업 내용 (추가)
- BallTrajectoryMechanic.md → GameplayMechanics.md 파일명 일반화 작업
- 신규 생성: Assets/_Project/Docs/GameplayMechanics.md
- AGENTS.md Docs 문서 인덱스에서 BallTrajectoryMechanic.md 항목 갱신

### 결과
- GameplayMechanics.md 생성 완료: 기존 본문("1. 볼 발사 및 궤도 시스템", "현재 구현과의 차이 (TODO)") 그대로 유지, 최상단 제목만 `# GameplayMechanics.md`로 변경, 서두 설명 문구는 기존 문구 그대로 유지(이미 일반화된 표현 포함되어 있었음)
- AGENTS.md 인덱스 업데이트 완료: 파일명/경로를 GameplayMechanics.md로 교체, 설명 문구에 "확정된 게임플레이 메커닉을 계속 추가 기록" 표현 보강
- 기존 BallTrajectoryMechanic.md 파일은 삭제 도구가 없어 그대로 유지 — 실제 삭제는 오케스트레이터(사용자)가 직접 처리 예정

### 주요 결정사항
- 사용자가 볼 궤도 외 다른 게임 알고리즘/메커닉도 이 문서에 계속 추가할 계획이라 파일명을 더 일반적인 GameplayMechanics.md로 변경
- 본문 내용/구조는 변경하지 않고 제목과 인덱스만 갱신하여 향후 섹션 추가 시 혼란 없도록 처리

---

### 작업 내용 (추가)
- UI HUD Gap Fill task plan.md 수정 (사용자 확정사항 3건 반영)
- 경로: Assets/_Project/Docs/_Task/2026-07-01/18-41_ui-hud-gap-fill/plan.md
- 수정 전 `Assets/_Project/Scripts/Editor/UISetupEditor.cs`를 읽어 기존 Editor 자동화 패턴(`EnsureChildObject` + `TextMeshProUGUI`/`Image` 생성 → `SerializedObject.FindProperty(...).objectReferenceValue` 연결, 프리팹은 `PrefabUtility.SaveAsPrefabAsset` + `EditPrefabContentsScope`로 2단계 처리) 파악 후 반영

### 결과
- 확정 1: 슬롯 `x{N}` 배지는 레벨 의미로 확정 — "주의사항" 1번을 확정 문구로 교체, `SkillSlotGroup.cs` 코드 예시(`UpdateActiveSlots`/`UpdatePassiveSlots`)에서 `SetFilled()` 호출부를 `skills[i].SkillData.CurrentLevel + 1`로 수정
- 확정 2: Passive 슬롯도 동일 레벨 배지 표시로 확정 — "주의사항" 3번을 PDF 근거(Lv.1/2/3 수치, `SkillData._levels` 공통 구조) 포함한 확정 문구로 교체, 코드는 변경 없음(이미 동일 패턴)
- 확정 3(중요 변경): "Inspector 작업"/수동 연결 문구를 전면 제거하고 `UISetupEditor.cs` 기존 Step 함수(`Step2_SetupHUDCanvas`, `Step6_CreateSkillCardPrefab`, `Step9_SetupHUDPanelContent`, `Step11_SetupSkillSelectionPanelContent`)에 각각 HP 텍스트/데미지 텍스트/진행률 텍스트/슬롯 UI 생성·연결 로직을 추가하는 방향으로 재작성, 슬롯 UI용 신규 `Step12_CreateSkillSlotPrefab()`(+ 필요 시 `Step13`) 및 `SkillSlot.prefab` 신규 생성 파일 항목 추가
- "예상 변경/생성 파일 목록"에 `UISetupEditor.cs`를 수정 대상으로 추가, "Unity 에디터에서 별도 처리 필요" 섹션명을 "Editor 스크립트로 자동화 처리"로 변경하고 각 항목에 담당 Step 함수 명시
- "주의사항" 5번을 "`UISetupEditor.cs` 실행(`PurpleCow > Setup > UI Setup` 메뉴)까지 완료해야 화면에 보인다"는 취지로 재작성
- 전체 재검토 결과 서두 요약, 구현 목표, TaskRules.md 문서 구조(서두/구현 목표/단계별 작업 계획/예상 변경 파일 목록/주의사항) 일관성 확인 완료

### 주요 결정사항
- 이 프로젝트의 기존 컨벤션(ProjectStatus.md에 기록된 "Inspector 연결 에디터 스크립트 자동화 완성")에 따라 씬/프리팹 UI 연결 작업은 항상 Editor 스크립트 자동화로 처리하고 수동 Inspector 작업 문구는 plan.md에 남기지 않음
- 레벨 표시는 `CurrentLevel`(0-based) + 1로 보정하는 것을 Active/Passive 공통 규칙으로 확정하여 코드 예시에 일관 반영

---

### 작업 내용 (추가)
- CLAUDE.md에 "6. 참고 자료 (원본 자료 경로)" 섹션 신규 추가
- 경로: /home/user/PurpleCow/CLAUDE.md

### 결과
- 기존 0~5번 섹션 구조를 읽고 5번(에이전트 운영 구조) 바로 다음에 6번 섹션 추가
- 공식 요구사항 스펙 PDF(`PurpleCow_클라이언트_채용과제.pdf`), 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/`), 이미 정리된 참고 문서(UIRules.md, GameplayMechanics.md) 3개 항목을 bullet로 기록
- 기존 문체(간결한 한글 bullet, 파일 링크 표기)를 그대로 따름, 0~5번 기존 내용은 변경하지 않음

### 주요 결정사항
- 새 세션 시작 시 원본 자료를 매번 재탐색하지 않도록 "PDF/스크린샷 원본 → 확정 문서(UIRules.md/GameplayMechanics.md) 우선 확인 → 불확실한 부분만 원본 재참고" 순서를 명시

---

### 작업 내용 (추가)
- CLAUDE.md "6. 참고 자료 (원본 자료 경로)" 섹션에서 3번째 bullet("이미 정리된 참고 문서": UIRules.md, GameplayMechanics.md 안내) 삭제 — 사용자 요청 범위(PDF, 스크린샷 경로 2개)를 벗어난 임의 추가 항목으로 확인되어 롤백
- 경로: /home/user/PurpleCow/CLAUDE.md
- AGENTS.md에 "참고 자료" 섹션 신규 추가
- 경로: /home/user/PurpleCow/AGENTS.md

### 결과
- CLAUDE.md: 1번(공식 요구사항 스펙 PDF), 2번(원본 게임 레퍼런스 스크린샷) bullet만 유지, 3번 bullet 삭제 완료
- AGENTS.md: 기존 "Docs 문서" 테이블(하단) ~ "에이전트" 테이블(상단) 사이에 "참고 자료" 섹션 신규 삽입. 테이블 컬럼은 기존 문서 인덱스와 동일한 "자료 | 경로 | 설명" 3컬럼 스타일 채택. PurpleCow_클라이언트_채용과제.pdf, targetUI/ 2개 항목 등록

### 주요 결정사항
- targetUI/(이미지 폴더)와 PDF는 "문서"가 아니라 원본 참고 자료이므로 기존 "루트 문서"/"Docs 문서" 테이블에 섞지 않고 별도 "참고 자료" 섹션으로 분리하는 것이 AGENTS.md 기존 구조(문서 인덱스 → 에이전트 → Task 문서) 흐름상 가장 자연스럽다고 판단
- CLAUDE.md 3번 bullet 삭제는 이전 세션에서 사용자 요청 없이 임의로 추가된 내용을 되돌리는 정정 작업

---

### 작업 내용 (추가)
- Ball Launch Mechanics task research.md 신규 생성
- 경로: Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/research.md
- GameplayMechanics.md 섹션 1(볼 발사/궤도 시스템) TODO 항목의 실제 재구현을 위한 첫 단계(research.md만, plan.md는 별도 승인 후 진행)

### 결과
- InputHandler.cs/BallLauncher.cs/Ball.cs 코드 레벨 동작 방식 상세 정리("현재 상태" 섹션): 터치 Began 시점에는 이벤트가 없고 OnDrag(드래그 중)/OnRelease(1회)만 존재, 발사는 릴리즈 1회성, Wall/Ground 충돌 시 위치 이동 없이 즉시 ReturnToPool, 재발사 트리거 코드 전무
- 궤적 프리뷰(LineRenderer/Trajectory/AimIndicator) 키워드로 Assets/_Project 전체 Grep 결과 GameplayMechanics.md 문서 자체 1건만 매칭 확인 → 관련 기존 컴포넌트 없음을 코드 레벨에서 재확인
- 관련 파일 테이블 작성: InputHandler/BallLauncher/Ball/BallData/ObjectPool/IPoolable/WaveManager/HUDPanel/ClusterBallSkill/SceneSetupEditor 10개 파일의 역할과 연결 관계 정리, LaunchPoint가 BallLauncher 자식의 고정 좌표(0,-8,0) 빈 오브젝트임을 SceneSetupEditor 코드로 확인
- 요구사항 7개 항목별(터치 즉시 조준/첫 충돌 구간 프리뷰/실시간 드래그 추적/물리 반사 이동/하단 귀환/재발사 시 최신 방향/속도 항상 일정) 현재 코드 상태 매핑 테이블 작성 — 물리 반사 이동과 속도 고정 2개 항목은 이미 요구사항과 부합, 나머지는 전면 신규 구현 필요로 분류
- 결론에 재설계 필요 핵심 3가지(조준 이벤트 체계, 궤적 프리뷰 시각화, 귀환·재발사 사이클)와 WaveManager/HUDPanel이 의존하는 OnAllBallsReturned 의미 충돌 가능성, LaunchPoint-캐릭터 연동 여부 미확정 지점을 명시 (구체적 구현 방법은 미포함, plan.md 단계로 이월)

### 주요 결정사항
- plan.md는 이번에 작성하지 않음(사용자가 별도 승인 후 진행 예정) — TaskRules.md 절차 준수
- research.md에는 "무엇이 문제인지"까지만 정리하고 구현 방법은 포함하지 않음
- 재발사 사이클 도입 시 WaveManager(몬스터 하강 트리거)와 HUDPanel(조준 인디케이터)이 BallLauncher.OnAllBallsReturned에 의존하는 부분이 의미 충돌 가능성이 있어 plan.md 단계에서 반드시 논의 필요하다고 결론에 명시

---

## 2026-07-02

### 작업 내용
- GameplayMechanics.md에 "2. 몬스터 스폰 및 전진 시스템" 섹션 신규 추가
- 경로: Assets/_Project/Docs/GameplayMechanics.md

### 결과
- 기존 섹션 1("볼 발사 및 궤도 시스템") 바로 다음에 `---` 구분선 + `## 2.` 섹션 추가 완료
- 본문: 웨이브 일괄 스폰, 랜덤 스폰 위치, 시간 기반 연속 전진(볼 사이클과 무관), 하단 도달 시 HP 차감(UIRules.md 섹션 10 참조로 중복 서술 회피), 전멸 시 웨이브 클리어 조건 기술
- "현재 구현과의 차이 (TODO)" 서브섹션: WaveManager.cs(턴 기반 MoveDown), MonsterBase.cs(냉동/슬로우 턴 기반 타이머) 현재 구조와의 괴리 3건 + 재설계 필요 사항 기술
- 파일 재확인 결과 정상 반영 확인(총 62줄)

### 주요 결정사항
- 작업 범위를 GameplayMechanics.md 1개 파일로 한정, AGENTS.md 등 다른 문서는 이번 요청에서 변경하지 않음(이미 인덱스에 등록되어 있는 기존 문서의 섹션 추가이므로 인덱스 갱신 불필요)
- 기존 섹션 1과 동일한 구조(제목 레벨 `##`/`###`, `---` 구분선, TaskRules.md 안내 문구)를 그대로 재사용하여 문서 스타일 일관성 유지

---

## 2026-07-03

### 작업 내용
- 볼 발사 메커닉 재설계(`_Task/2026-07-01/21-15_ball-launch-mechanics`) + 후속 UISetupEditor 버그 수정(PR #6, #7) 완료에 따른 프로젝트 문서 정리
- `ProjectStatus.md`, `ProjectHistory.md`, `AIFailures.md`, `AGENTS.md` 갱신 + dev/qa agent-memory 보강 검토

### 결과
- `ProjectStatus.md`: 현재 상태 날짜 2026-07-03으로 갱신, 완료된 작업에 볼 발사 메커닉 재설계 + UISetupEditor 수정 2건 추가, "다음 작업 순서"를 구체적 항목 나열에서 "실제 플레이 테스트를 진행하며 발견되는 문제를 하나씩 수정" 방향성 위주로 교체
- `ProjectHistory.md`: 2026-07-03 섹션 신규 추가 — 볼 발사 메커닉 재설계 요약(2단계 궤적 프리뷰, 로스터 모델, 몬스터 시간 연속 하강), QA 검토 결과 및 최종 정정, UISetupEditor 버그 수정, 문서 정리 순으로 기록
- `AIFailures.md`: 2026-07-03 섹션 신규 추가 — (1) 머지된 브랜치 강제 재구성 중 fetch 누락으로 사용자의 원격 커밋을 놓칠 뻔한 사고, (2) `BallData.asset._maxBounces`가 에디터 스크립트 수정에도 반복적으로 0으로 방치되는 패턴(QA가 3회 이상 동일 지적), (3) `UISetupEditor`가 신규 UI 컴포넌트의 SerializeField 연결을 반복적으로 누락하는 패턴, 총 3건 기록
- `AGENTS.md`: Task 문서 인덱스에 "2026-07-01" 섹션 신규 추가(`18-41_ui-hud-gap-fill`, `21-15_ball-launch-mechanics` 등록)
- dev agent-memory: 로스터 볼 Wall 충돌 최종 정정("반사 횟수 무관 순수 반사, Ground 충돌에서만 귀환")이 기록 누락되어 있던 것을 확인하고 추가 기록
- qa agent-memory: 위 최종 정정이 QA 재검토가 아닌 사용자 실제 플레이 재확인에 의한 설계 확정 변경임을 명확히 하는 후속 메모 추가, 궤적 프리뷰 구현이 QA 코드 레벨 재검토를 아직 거치지 않았음을 남김

### 주요 결정사항
- `Assets/_Project/Docs/` 전체 문서(GameplayMechanics.md, UIRules.md, DevRules.md 등) 검토 결과 기존 문서 갱신은 위 항목으로 충분하다고 판단, 신규 문서 작성은 하지 않음(범위 확대 방지)
- 검토 중 두 가지 잠재적 신규 문서 필요성을 발견했으나 직접 작성하지 않고 오케스트레이터를 통해 사용자에게 보고만 하기로 결정: (1) `DevRules.md` "6. Git 규칙"에 "머지된 브랜치 재구성 전 반드시 fetch 선행" 컨벤션 추가 여부, (2) `UIRules.md`에 `TrajectoryPreview.cs`가 도입한 조준선 시각 규칙(점선 렌더링 방식, 레드닷/원형 궤적선, RaycastAll 태그 필터링 등)을 섹션 8/9/10과 같은 형식의 신규 섹션으로 문서화할지 여부
- 코드(Ball.cs, BallLauncher.cs 등)는 전혀 수정하지 않음 — 이번 세션은 문서/agent-memory만 다룸

---

### 작업 내용 (추가)
- 위 기록에서 보고만 하기로 했던 두 항목이 사용자 승인을 받아 실제 문서 반영으로 진행됨
- `DevRules.md` "6. Git 규칙" 섹션에 신규 bullet 추가: 머지된 브랜치 재구성 전 `git fetch origin <branch>` 선행 + `git log <branch>..origin/<branch>` 확인 의무화, `--force-with-lease`가 fetch 시점 기준으로만 안전장치가 작동한다는 근거 포함 (경로: `Assets/_Project/Docs/DevRules.md`)
- `UIRules.md`에 "11. 궤적 프리뷰 시각 규칙" 신규 섹션 추가, 기존 섹션 11(리소스 참고 사항)은 12번으로 재번호 (경로: `Assets/_Project/Docs/UIRules.md`)
  - `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`를 Read로 재확인 후 실제 구현 기준으로 작성: 조준 중에만 표시, 2단계 점선 궤적(1차/2차 충돌), 2차 지점 레드닷+원형 궤적선, RaycastAll+Wall/Ground/Monster 태그 화이트리스트 필터링
  - 구현 방식 서술: LineRenderer 기반, 점선은 런타임 생성 4x1 텍스처 + textureMode=Tile, 레드닷/원형 궤적선도 스프라이트 없이 LineRenderer 원형 점열로 구현
  - Inspector 조절 값 5개(`_lineWidth`, `_lineColor`, `_hitColor`, `_dotRadius`, `_ringRadius`) 표로 정리
- `AGENTS.md`의 UIRules.md 설명 문구에 "궤적 프리뷰 시각 규칙" 추가하여 신규 섹션 반영

### 결과
- `Assets/_Project/Docs/DevRules.md`, `Assets/_Project/Docs/UIRules.md`, `/home/user/PurpleCow/AGENTS.md` 3개 파일 수정 완료
- 코드는 건드리지 않음(Read만 수행, TrajectoryPreview.cs 자체는 미수정)

### 주요 결정사항
- GameplayMechanics.md 섹션 1(원본 스펙)과 실제 구현 코드가 이미 일치하는 상태였으므로, 신규 섹션은 "스펙 재서술"이 아니라 "구현 관점에서의 시각/Inspector 규칙"에 집중해서 작성 (GameplayMechanics.md 링크로 원본 스펙 참조 위임)
- 기존 UIRules.md 섹션 8/9/10과 동일한 톤(담당 클래스 명시 → bullet 설명 → 구현 방식/표) 유지

---

### 작업 내용 (추가)
- 배경/해상도 대응 task research.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/research.md`
- 사용자가 대화 중 이미 확정한 조사 내용(ProjectSettings.asset Player Settings 값, Background_1_Stage.png/.meta, SampleScene.unity Background/Main Camera 상태, SceneSetupEditor.cs Step4_PlaceBackground, targetUI 레퍼런스 비교, 정투영 카메라 뷰포트 계산, 사용자 확정 제약조건 3+1가지, Cover-Fit 합의 방향)을 TaskRules.md 구조(현재 상태/관련 파일 및 의존성/문제점 구현 대상 파악/결론)에 맞게 정리. 새로운 조사는 추가하지 않고, 언급된 파일들(ProjectSettings.asset 46/62-65/76-77번째 줄, Background_1_Stage.png.meta, SampleScene.unity Background Transform 906-907번째 줄·Main Camera Transform 1276번째 줄·Camera 1225-1226/1250-1251번째 줄, SceneSetupEditor.cs 344-368번째 줄)을 직접 Read로 재확인하여 라인 번호를 정확히 인용

### 결과
- research.md 생성 완료: Player Settings 미고정(Auto Rotation 4방향, 가로 기본 해상도) + Background 스프라이트 고정 크기 배치(스케일 조정 로직 없음) 2대 원인 정리, 정투영 카메라 뷰포트 공식(세로 고정=orthoSize×2, 가로 가변=orthoSize×2×aspect) 및 Cover-Fit 해결 방향(구체 구현은 plan.md로 이월)까지 포함
- AGENTS.md는 "Task 문서" 섹션이 이미 "개별 task 폴더 목록을 별도로 관리하지 않음" 방침으로 되어 있어 이번에는 인덱스 추가 작업 없이 완료

### 주요 결정사항
- 사용자가 이미 대화로 확정한 사실관계만 반영하고 추가 조사는 하지 않되, 정확성을 위해 언급된 파일들은 직접 Read로 재확인하여 라인 번호를 검증 후 인용
- Cover-Fit 스케일 로직 + 카메라 배경색 보정 + Player Settings Portrait 고정, 이 세 가지의 "구체적 구현 방식"은 research.md에서 확정하지 않고 plan.md 단계로 명시적으로 이월

---

### 작업 내용 (추가)
- Ball Ceiling Wall Fix task plan.md 신규 생성 (기존 research.md 기반)
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-48_ball-ceiling-wall-fix/plan.md`
- 작성 전 research.md, TaskRules.md, `SceneSetupEditor.cs`(374~398번 줄), 기존 plan.md 예시(`2026-07-01/21-15_ball-launch-mechanics/plan.md`)를 Read로 확인해 문서 스타일/구조 일관성 유지

### 결과
- plan.md 생성 완료: 구현 목표(3개), 단계별 작업 계획(1단계 코드 수정, 2단계 좌표 겹침 사전 계산, 3단계 씬 반영 옵션 A/B, 4단계 QA 검증), 예상 변경/생성 파일 목록(`SceneSetupEditor.cs`, `SampleScene.unity` 2개), 주의사항(씬 반영 방식 사용자 확인 필요, 범위 제외 2건, 좌표 계산 한계, plan.md만 완료된 상태) 구조로 작성
- `Wall_Top` 좌표/크기(`new Vector3(0f, 8f, 0f), new Vector2(12f, 0.2f)`, 태그 `"Wall"`)를 research.md 근거(AIFailures.md 플레이 영역 y:+8, Ground와 동일 크기 재사용)와 함께 명시
- 좌우 벽(x: ±5.5, size 0.2×20)과 천장 벽(y: 8, size 12×0.2) 좌표를 직접 계산해 네 모서리 부근에서 겹치는 영역이 존재함(갭 없음)을 확인, 다만 이는 좌표상 사전 판단이며 4단계 QA에서 실제 물리 동작 재확인이 필요하다고 명시
- 씬 반영 방법을 옵션 A(로컬 Unity 에디터에서 `PurpleCow/Setup/Scene Setup` 메뉴 재실행, 권장)와 옵션 B(`SampleScene.unity` YAML 직접 편집, dev 에이전트가 즉시 처리 가능하나 형식 위험 있음) 두 가지로 제시하고 최종 선택은 사용자 확인이 필요하다고 "주의사항"에 명시
- `physicsMaterial2D` Wall/Ground 미연결, `CollisionDetectionMode2D.Continuous` 터널링 가능성 2건은 research.md와 동일하게 이번 작업 범위에서 제외한다고만 짧게 언급(단계별 계획에는 미포함)

### 주요 결정사항
- 코드(`SceneSetupEditor.cs`) 수정은 옵션 A/B 어느 쪽을 택하든 공통으로 필요하다고 판단 — 향후 씬 재생성 시에도 천장 벽이 항상 생성되도록 보장하기 위함
- 이번 요청은 "plan.md 작성"만 포함되며 실제 코드/씬 파일 수정은 진행하지 않음(TaskRules.md 절차 준수, 사용자의 명시적 승인 대기)
- AGENTS.md는 개별 task 폴더를 별도로 인덱싱하지 않는 기존 정책(56~57번 줄, "개별 task 폴더 목록은 이 문서에서 별도로 관리하지 않으며, 필요할 때 해당 경로를 직접 탐색")을 확인했으므로 이번 plan.md 생성에 대해서는 AGENTS.md 갱신 불필요로 판단

---

### 작업 내용 (추가)
- dev 에이전트가 task 문서 없이 예외 진행한 "WaveData 20개 개별 asset → WaveTableData 단일 테이블 SO" 리팩토링(main 커밋 `9c188a8`, 이미 완료됨)에 대한 사후 문서 갱신
- 수정 파일: `Assets/_Project/Docs/DevRules.md`, `Assets/_Project/Docs/ProjectStatus.md`, `Assets/_Project/Docs/ProjectHistory.md` (코드는 건드리지 않음)

### 결과
- `DevRules.md`: "ScriptableObject 사용 범위" 표에서 `WaveData | 웨이브별 몬스터 구성, 처치 조건` 행을 `WaveTableData | 웨이브 20개의 몬스터 구성/처치 조건을 한 asset에 테이블로 관리`로 교체 (해당 행만 수정)
- `ProjectStatus.md`: "완료된 작업"에 WaveTableData 리팩토링 항목 추가(task 문서 없이 예외 진행, main 직접 커밋 `9c188a8` 명시), "다음 작업 순서"에 1번 항목으로 "사용자가 로컬 Unity에서 Monster System Setup → Scene Setup 재실행하여 WaveTableData.asset 생성 및 WaveManager 참조 재연결 필요(안 하면 웨이브 스폰 불가)"를 추가하고 기존 플레이 테스트 항목을 2번으로 밀어냄
- `ProjectHistory.md`: 2026-07-03 날짜 섹션 하단에 "WaveData → WaveTableData 리팩토링" 소제목 신규 추가 — 배경(asset 과다), 삭제/신규/수정 파일 목록, 원격 환경에 Unity 에디터가 없어 asset 생성/씬 재연결이 미완료 상태임을 기존 항목들과 같은 서술 형식(무엇을/왜/어떻게)으로 기록

### 주요 결정사항
- AGENTS.md는 이번 갱신으로 인덱스 변경 사항이 없어 건드리지 않음(사용자 지시대로)
- 코드 파일(WaveManager.cs, MonsterSetupEditor.cs, SceneSetupEditor.cs, WaveTableData.cs 등)은 이미 커밋된 상태이므로 문서 3개만 수정하고 Read 외 접근하지 않음
- 커밋/푸시는 진행하지 않고 파일 수정까지만 수행(오케스트레이터/사용자 판단에 위임)

---

### 작업 내용 (추가)
- 배경/해상도 대응 task research.md "범위에서 제외한 사항" 문단 갱신 + plan.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/research.md`, `.../plan.md`
- research.md 수정: 검은 사각형 UI 글리치는 사용자가 Play 모드로 직접 재확인한 결과 재현되지 않음 → `UIRules.md` 4번(패널은 항상 `SetActive(true)`, `CanvasGroup`으로만 표시/숨김 제어) 규칙상 에디트 모드 캡처 시점에는 `Awake()`/`Start()` 런타임 초기화 전 빈 상태가 노출됐을 가능성이 높다는 원인 추정으로 대체, "실제 버그 아님 / 별도 조치 불필요 / 향후 재조사 불필요"로 결론
- plan.md 신규 작성: TaskRules.md 구조(서두 요약/구현 목표/단계별 작업 계획/예상 변경·생성 파일 목록/주의사항) 준수, 사용자가 이미 방향성을 확정한 구현 계획(1단계 Player Settings Portrait 고정, 2단계 BackgroundFitter.cs 신규 작성, 3단계 SceneSetupEditor.cs Step4_PlaceBackground 연동, 4단계 SampleScene.unity Main Camera 배경색 보정)을 그대로 문서화, 새로운 설계 판단은 추가하지 않음
- AGENTS.md는 "Task 문서" 섹션이 개별 폴더 목록을 관리하지 않는 방침이라 이번에도 인덱스 갱신 없이 완료

### 결과
- research.md, plan.md 두 문서 모두 갱신/생성 완료

### 주요 결정사항
- research.md "범위에서 제외한 사항"은 완전히 새로 쓰지 않고 기존 문단 톤/구조를 유지하며 사실관계만 갱신 (사용자 지시 준수)
- plan.md는 사용자가 이미 논의를 마친 구체적 구현 계획을 정리만 하는 문서로 작성 — plan.md 하단 주의사항에 "사용자의 명시적 승인 전에는 구현으로 이어지지 않는다"를 명시하여 TaskRules.md 절차(plan.md 작성 후 승인 필요) 재확인

---

### 작업 내용 (추가)
- WaveData → WaveTableData 리팩토링 후속: 사용자가 로컬 Unity에서 `Monster System Setup` → `Scene Setup`을 실행해 `WaveTableData.asset` 생성 및 `SampleScene.unity`의 `WaveManager._waveTable` 참조 재연결 완료(커밋 `ceeb9e2`)한 것을 오케스트레이터가 직접 검증한 결과를 문서에 반영
- 수정 파일: `Assets/_Project/Docs/ProjectStatus.md` 1개만 (다른 문서는 이미 정확하다는 지시에 따라 건드리지 않음)

### 결과
- `ProjectStatus.md`: "완료된 작업"의 기존 WaveData→WaveTableData 항목 문장 뒤에 검증 완료 내용을 이어붙임 — `WaveTableData.asset` 생성/씬 재연결 완료, 커밋(`ceeb9e2`)/푸시 완료, 오케스트레이터 검증 결과(웨이브 1~20 스폰 데이터 정확 일치, 구 `_waveDatas` 필드 완전 제거, `_waveTable` 단일 참조로 정상 교체, `Assets/_Project/Data/` asset 개수 35→16개 감소) 명시
- "다음 작업 순서"에서 선행 필요 항목(구 1번, Monster/Scene Setup 재실행 요청)을 완전히 제거하고, 기존 2번("실제 플레이 테스트...")을 1번으로 당김
- 최상단 "현재 상태" 서술(`**단계**`)은 이미 "실제 플레이 테스트 단계 진입"으로 되어 있어 추가 수정 없이 그대로 유지(과도한 재작성 지양 지시 준수)

### 주요 결정사항
- `TrajectoryPreview` GameObject가 같은 커밋에 씬에 추가된 것은 `SceneSetupEditor.cs`의 기존 "Scene Setup" 메뉴가 여러 시스템을 한 번에 처리하는 과정에서 발생한 정상적인 부수 효과로, 이번 WaveData 작업과 무관하다고 판단하여 문서에 별도 기록하지 않음
- DevRules.md/ProjectHistory.md/AGENTS.md는 지시대로 이번 세션에서 전혀 수정하지 않음
- 커밋/푸시는 진행하지 않고 파일 수정까지만 수행

---

### 작업 내용 (추가)
- 배경/해상도 대응 task research.md/plan.md에 실기기 빌드 스크린샷에서 발견된 신규 원인(카메라 시야/Wall 가시성 기기별 대응 안 됨) 보강
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/research.md`, `.../plan.md`
- 수정 전 `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`(Step5_PlaceWallsAndGround, 385~390번째 줄)와 `Assets/_Project/Scripts/Core/BackgroundFitter.cs`(현재 `Awake()`로 구현되어 있는 상태) 실제 코드를 Read로 재확인 후 문서에 반영
- research.md: "문제점 / 구현 대상 파악" 섹션에 "문제 3 — 카메라 시야(Wall 가시성) 기기별 대응 안 됨" 신규 소제목 추가(Wall x=±5.5, 콜라이더 폭 0.2로 바깥쪽 끝 ±5.6, orthographicSize=10 고정, aspect≥0.56 조건에서만 성립, 최신 기기 종횡비는 대부분 이보다 낮음, 코드베이스 전체에서 카메라 크기 의존 로직이 BackgroundFitter/SceneSetupEditor 외 없음을 확인해 부작용 없음 근거 포함), "결론"을 기존 원인 2가지에서 3가지로 갱신하고 해결 방향에 "카메라 시야 동적 확장" 항목 추가. 기존 문제 1/2번 서술은 건드리지 않음
- plan.md: 기존 4단계 뒤에 "5단계 — CameraFitter 신규 작성 및 연동" 추가(`Assets/_Project/Scripts/Core/CameraFitter.cs` 신규, `_targetCamera`/`_baseOrthographicSize=10`/`_requiredHalfWidth=5.6` 필드, `Awake()`에서 `Mathf.Max(base, requiredHalfWidth/aspect)` 계산, `SceneSetupEditor.cs`에 신규 Step으로 연동 — `Camera.main` 참조 + 기존 `ConnectBackgroundFitterRefs()`와 동일한 SerializedObject 패턴 재사용), 2단계(BackgroundFitter)에 "실행 순서 보장을 위해 Awake()→Start() 변경 필요" 보강 문단 추가, "예상 변경/생성 파일 목록"에서 `BackgroundFitter.cs`를 신규 생성에서 수정 대상으로 정정(이미 구현되어 있음을 명시)하고 `CameraFitter.cs` 신규 생성 추가, "주의사항"에 "CameraFitter가 BackgroundFitter보다 먼저 실행되어야 함(Awake vs Start로 순서 보장)" 항목 추가

### 결과
- research.md, plan.md 두 문서 모두 갱신 완료 (완전히 새로 쓰지 않고 기존 구조에 소제목/단락 추가 방식)

### 주요 결정사항
- 사용자가 제공한 원인 분석(콜라이더 바깥쪽 끝 ±5.6, aspect 조건식, CameraFitter 필드/로직, Awake/Start 순서 보장 근거)은 이미 코드 Read로 교차검증했으므로 그대로 반영하고 임의 추가 조사는 하지 않음
- CameraFitter 연동 Step 위치는 사용자가 "판단은 plan.md 작성 시 자유롭게 정리"라고 위임한 부분이라, Wall 배치 로직(Step5) 직후 신규 Step으로 분리하고 기존 `ConnectBackgroundFitterRefs()` 패턴을 재사용하는 것으로 구체화
- AGENTS.md는 "Task 문서" 섹션이 개별 폴더 목록을 관리하지 않는 기존 방침이라 이번에도 인덱스 갱신 없이 완료

---

### 작업 내용 (추가)
- 볼 천장 이탈 버그 수정(`_Task/2026-07-03/12-48_ball-ceiling-wall-fix`, research.md → plan.md → dev 구현 커밋 `345ae29` → 사용자 로컬 Scene Setup 재실행 → 실 플레이 검증까지 전 과정 완료) 결과를 프로젝트 문서에 반영
- 수정 파일: `Assets/_Project/Docs/ProjectStatus.md`, `Assets/_Project/Docs/ProjectHistory.md` 2개 (코드/씬 파일은 이미 수정 완료된 상태라 건드리지 않음)

### 결과
- `ProjectStatus.md`: "완료된 작업" 체크리스트에 이번 항목 신규 추가 — 버그 발견 → research.md 원인 특정(`SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단 벽 생성 코드 누락) → plan.md 승인 → dev 에이전트 `Wall_Top` 콜라이더 1줄 추가(커밋 `345ae29`) → 사용자 로컬 `PurpleCow/Setup/Scene Setup` 재실행 → 실 플레이 검증 완료까지 기존 항목들과 동일한 한 문장 서술 스타일로 작성. "현재 상태"/"다음 작업 순서"는 기존 서술이 이미 이 흐름과 자연스럽게 이어져 지시대로 손대지 않음
- `ProjectHistory.md`: 이미 있는 "## 2026-07-03" 섹션 맨 하단(WaveData→WaveTableData 리팩토링 다음)에 "### 볼 천장 이탈 버그 수정" 소제목 신규 추가 — 배경/원인(`SceneSetupEditor.cs` 좌/우/아래 3면만 생성, 상단 누락)/수정(`Wall_Top` 1줄, 커밋 `345ae29`, 브랜치 `claude/ball-ceiling-wall-fix`, 좌표 근거)/검증(사용자 로컬 Scene Setup 메뉴 재실행 후 실 플레이 확인) 순서로 기존 섹션들과 동일한 서술 형식 유지

### 주요 결정사항
- `AIFailures.md`는 갱신하지 않음: 이번 버그는 이번 세션이 새로 저지른 실수가 아니라 과거 세션부터 존재하던 씬 설정 누락을 정상적인 research→plan→구현 절차로 발견/수정한 사례라 "AI 실패 사례" 성격과 다르다고 판단
- `GameplayMechanics.md`/`UIRules.md`도 갱신하지 않음: 천장 벽은 별도 게임플레이 메커닉이나 UI 규칙이 아니라 단순 씬 경계 콜라이더 설정이라 두 문서의 성격과 맞지 않는다고 판단
- `AGENTS.md`도 갱신하지 않음: 기존 정책(개별 task 폴더는 별도 인덱싱하지 않음, "Task 문서" 섹션에 명시)에 따라 이번 task 폴더도 별도 등록 불필요로 판단
- 코드(`SceneSetupEditor.cs`)와 씬(`SampleScene.unity`)은 이미 수정/반영 완료된 상태이므로 이번 작업에서는 전혀 건드리지 않고 Read하지도 않음(요청 문서 내용 그대로 신뢰해 반영)

---

### 작업 내용 (추가)
- 배경/해상도 대응 task research.md/plan.md를 "최종 확정 설계"로 갱신 — 이전 논의(Cover→Contain→Stretch 전환, CameraFitter 도입)는 삭제하지 않고 그대로 유지한 채, 그 뒤를 잇는 신규 섹션들로 최종 결론을 정리
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/research.md`, `.../plan.md`
- 작성 전 `TaskRules.md`(구조 규칙), 두 문서 현재 전문, 실제 코드(`BackgroundFitter.cs` — 이미 `Start()`에서 비균등 Stretch로 구현되어 있음 확인, `CameraFitter.cs` — `Awake()`에서 `Mathf.Max(base, requiredHalfWidth/aspect)` 그대로 존재, `SceneSetupEditor.cs` — `Step6_SetupCameraFitter()`가 실제로 연동되어 있음)를 Read로 재확인

### 결과
- research.md: "레퍼런스 이미지 비교" 다음에 "배경 이미지 격자 경계 실측(Python PIL 픽셀 스캔)" 신규 소제목 추가(가로 x=420~1613→월드 -6.04~+5.89, 세로 y=469~1557→월드 +5.55~-5.33, 기존 Wall 좌표(x=±5.5, Ground y=-10, Wall_Top y=8)가 실측 격자 경계와 불일치, 특히 Ground/Wall_Top이 장식 영역에 위치했음을 확인). "문제점" 섹션에 "문제 4 — Wall 좌표와 배경 격자 그림 경계 불일치", "배경 Stretch 방식과 Wall 좌표의 연동 문제", "CameraFitter가 불필요해짐(수학적으로 도출된 결론 — Wall/카메라 절반폭 비율이 orthographic size와 무관하게 항상 일정, 가로 약 0.29·세로 약 0.54로 항상 화면 안)", "참고 — 레퍼런스 이미지 비율 정밀 비교는 보류" 4개 신규 소제목 추가. "결론"에 "이후 논의로 도출된 최종 결론" 신규 문단 추가 — 최종 확정 설계 5가지(CameraFitter 삭제, orthographic size 10 고정 유지, BackgroundFitter 코드 변경 없이 유지, 신규 WallFitter 작성, SceneSetupEditor 갱신)를 요약하고 상세 스펙은 plan.md로 위임
- plan.md: 서두/구현 목표에 최종 확정 목표 문단 추가. 기존 "5단계 — CameraFitter 신규 작성 및 연동"은 삭제하지 않고 제목에 "(폐기됨)" 표시 + 폐기 사유 설명 문단만 추가, 내용은 시행착오 기록으로 보존. 신규 "6단계 — WallFitter 신규 작성 및 연동(최종 확정 설계)" 추가 — `Assets/_Project/Scripts/Core/WallFitter.cs`(필드 `_targetCamera`/`_backgroundSpriteRenderer`/`_wallLeft`/`_wallRight`/`_wallTop`/`_ground`/`_nativeLeftX=-6.04f`/`_nativeRightX=5.89f`/`_nativeTopY=5.55f`/`_nativeBottomY=-5.33f`, `Start()`에서 camSize/spriteSize로 scaleX·scaleY 계산 후 `SetX`/`SetY` 헬퍼로 4개 Transform 재배치, null 방어), `SceneSetupEditor.cs` 연동(`Step6_SetupCameraFitter()` → `Step6_SetupWallFitter()` 교체, `GameObject.Find`로 Background/Wall_Left/Wall_Right/Wall_Top/Ground 탐색해 참조 연결, `ConnectBackgroundFitterRefs()`와 동일한 SerializedObject 패턴 재사용). "예상 변경/생성 파일 목록" 갱신(`CameraFitter.cs` 삭제+.meta, `WallFitter.cs` 신규, `BackgroundFitter.cs`는 변경 없음으로 정정, `SceneSetupEditor.cs`/`SampleScene.unity` 수정). "주의사항"에 CameraFitter 순서 보장 항목을 "(폐기됨)"으로 표시하고 WallFitter는 BackgroundFitter와 실행 순서 의존성이 없음을 명시하는 항목, 레퍼런스 정밀 비교 보류 및 실측값 4개 미세조정 가능성 항목 추가

### 주요 결정사항
- "이전 논의는 시행착오 과정이므로 지우지 말고 그 뒤에 최종 확정 설계로 이어지는 흐름으로 정리"라는 사용자 지시에 따라, 기존 Cover-Fit/CameraFitter 관련 서술은 문구 하나도 삭제하지 않고 전부 유지한 채 새 소제목/문단만 순서대로 추가하는 방식으로 편집
- CameraFitter가 실제로는 아직 코드/씬에 남아 있는 상태(Read로 확인)이므로, plan.md "예상 변경/생성 파일 목록"에서 `CameraFitter.cs`를 "삭제" 대상으로, `SampleScene.unity`를 "Main Camera에서 CameraFitter 컴포넌트 제거 + orthographic size 10 확인/유지" 대상으로 명시해 향후 dev 에이전트가 실제로 무엇을 지워야 하는지 헷갈리지 않도록 구체화
- BackgroundFitter.cs는 실제 코드가 이미 Stretch(비균등 scaleX/scaleY) 방식으로 구현되어 있음을 확인했으므로, plan.md 파일 목록에서 기존 "수정(Awake→Start 변경)" 문구를 "변경 없음 — 최종 설계에서 그대로 유지"로 정정
- 이번 작업은 문서만 다루며 실제 코드/씬 파일(`CameraFitter.cs` 삭제, `WallFitter.cs` 생성 등)은 건드리지 않음 — TaskRules.md 절차상 plan.md는 사용자의 명시적 승인 전까지 구현으로 이어지지 않음
- AGENTS.md는 개별 task 폴더를 별도 인덱싱하지 않는 기존 방침이라 이번에도 갱신하지 않음

---

### 작업 내용 (추가)
- 배경/해상도 대응 task가 실기기 테스트까지 완료되어, 관련 문서 전체를 "최종 구현 상태"로 갱신
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/plan.md`, `.../research.md`, `Assets/_Project/Docs/ProjectStatus.md`, `Assets/_Project/Docs/ProjectHistory.md`
- 작성 전 `TaskRules.md`(구조 규칙) + 실제 코드 4종을 Read로 전량 재확인: `BackgroundFitter.cs`(Stretch 방식, `_zoomFactor=1.3f`, `[ExecuteAlways]`+`Apply()`/`OnValidate()` 확인), `WallFitter.cs`(`_launchPoint`/`_nativeLaunchPointY=-6.0f` 포함 6개 Transform 대상, `_nativeLeftX/-Right/-Top/-Bottom = -6.5/6.3/6.0/-6.5`, `_zoomFactor=1.3f`, 동일한 `[ExecuteAlways]` 구조 확인), `SceneSetupEditor.cs`(`Step6_SetupWallFitter()` 호출이 `Step8_ConnectBallLauncherRefs()` 이후로 재배치되어 있고 그 이유가 주석으로 명시됨, `ConnectBackgroundFitterRefs`/`Step6_SetupWallFitter` 모두 사용자가 알려준 최종값과 일치 확인), `ProjectSettings/ProjectSettings.asset`(Portrait 관련 필드 전부 최종 반영 확인하되, plan.md 1단계가 추정했던 필드명 `defaultInterfaceOrientation`은 실제로 존재하지 않고 `defaultScreenOrientation`(값 `0`=Portrait)이 실제 적용 필드임을 Grep으로 교차검증). `CameraFitter.cs`는 Glob 결과 파일 자체가 더 이상 존재하지 않아 삭제 완료 확인

### 결과
- `plan.md`: 기존 6단계 뒤에 "### 7단계 — 실기기 테스트 반영 최종 조정" 신규 섹션 추가 — (1) BackgroundFitter Cover→Contain(실기기에서 원인 불명 사방 여백 발생해 폐기)→Stretch 최종 확정, (2) `_zoomFactor=1.3f` 양쪽 공통 도입, (3) WallFitter 벽 기준값 실기기 조정 과정(`_nativeBottomY`: -5.33→-10→-7.5→-6.5 등 단계별 값과 사유) 전부 기록, (4) CameraFitter 삭제 최종 확정, (5) WallFitter에 LaunchPoint 편입 및 `Step6`/`Step8` 호출 순서 재배치, (6) `[ExecuteAlways]`+`Apply()`/`OnValidate()` Inspector 실시간 반영 추가— 6개 항목을 사용자가 준 문구에 상세 근거(코드 스니펫, 좌우 비대칭 2.5% 사유 등)를 덧붙여 작성. 추가로 코드 교차검증 중 발견한 "ProjectSettings.asset 필드명 정정"(`defaultInterfaceOrientation`→실제는 `defaultScreenOrientation`) 참고 문단을 별도로 덧붙임(사용자가 명시적으로 요청한 6개 항목 외 추가지만, "직접 코드를 Read해서 문서 서술이 정확한지 교차검증" 지시에 따른 것). "예상 변경/생성 파일 목록" 절 전체를 최종 상태(CameraFitter.cs 삭제 완료, 나머지는 전부 "최종 구현 완료"로 표시)로 갱신
- `research.md`: "결론" 섹션 맨 끝에 "### 최종 구현 및 실기기 검증 완료" 짧은 문단 신규 추가 — plan.md 7단계 내용이 실제로 구현되고 실기기 테스트로 사용자가 확인/만족했다는 사실만 간결히 기록, 상세는 plan.md 링크로 위임
- `ProjectStatus.md`: "완료된 작업" 체크리스트에 "배경/해상도 대응(`_Task/2026-07-03/12-30_background-resolution-fix`)" 항목 신규 추가 — Cover→Contain→Stretch 시행착오, CameraFitter 도입 후 수학적으로 불필요함이 밝혀져 폐기, WallFitter 도입으로 벽을 배경 격자에 비례 연동, zoomFactor/Inspector 실시간 반영 추가, 실기기 테스트로 최종 수치 확정까지 한 문장으로 요약. "다음 작업 순서"는 지시대로 손대지 않음
- `ProjectHistory.md`: 기존 "## 2026-07-03" 섹션 맨 하단(볼 천장 이탈 버그 수정 다음)에 "### 배경/해상도 대응" 소제목 신규 추가 — 배경(다양한 Android 기기 종횡비 대응 필요성)/원인(Player Settings 미고정, 배경 텍스처 정사각 크롭이라 화면 전체 못 채움, Wall 좌표가 배경 격자와 불일치)/해결 과정(시행착오 포함 6개 항목을 순서대로 상세 서술)/최종 결과(실기기 테스트 완료, 문서 링크) 순서로 기존 섹션들과 동일한 서술 형식 유지

### 주요 결정사항
- 사용자가 준 6개 확정 항목은 문구를 그대로 살리되, plan.md/ProjectHistory.md에는 code 스니펫·구체적 수치 변화 과정(예: `_nativeBottomY`가 4단계를 거쳐 -6.5로 수렴한 과정)을 원문 그대로 전부 나열해 향후 "왜 이 값인지" 재질문이 나와도 문서만으로 추적 가능하게 함
- ProjectSettings.asset 필드명 불일치(`defaultInterfaceOrientation` vs 실제 `defaultScreenOrientation`)는 사용자가 명시적으로 요청한 6개 항목에는 없었지만, "실제 코드 상태와 대조하며 교차검증"이라는 상위 지시를 따라 발견 즉시 plan.md에 짧은 정정 문단으로 반영 — 다만 이는 사용자가 지시한 6개 최종 확정 항목과는 성격이 다르므로(실제 동작 변경이 아니라 기존 문서의 오기 정정) 별도 "참고" 문단으로 분리해 6개 항목과 섞이지 않도록 함
- plan.md 7단계는 기존 1~6단계를 전혀 수정하지 않고 그 뒤에 새 섹션으로만 추가 — "기존 문서를 새로 쓰지 말고 최종 확정 내용을 반영하는 섹션을 추가"라는 사용자 지시를 그대로 따름
- AGENTS.md는 이번에도 갱신하지 않음: 새로 생성된 문서 파일이 없고(기존 4개 문서 편집만 수행) AGENTS.md는 개별 task 폴더를 별도 인덱싱하지 않는 기존 방침이 이번에도 그대로 적용됨을 Grep으로 재확인

---

### 작업 내용 (추가)
- Ball Trajectory Aim Fix task research.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/research.md`
- 작성 전 `InputHandler.cs`, `TrajectoryPreview.cs`, `BallLauncher.cs`, `GameplayMechanics.md` 섹션 1/2, `UIRules.md` 섹션 11을 Read로 재확인

### 결과
- research.md 생성 완료: 두 이슈(궤적이 터치할 때만 표시됨 / 터치 조준 정확도 저하) 각각 현재 코드 구조와 원인을 분석
- 이슈 1: `TrajectoryPreview`가 `OnAimBegin`/`OnDrag`/`OnRelease` 이벤트에만 반응하는 순수 이벤트 기반 구조라 터치가 없는 프레임에는 갱신 트리거가 없음을 확인. 몬스터가 볼 사이클과 무관하게 항상 이동한다는 `GameplayMechanics.md` 섹션 2 근거를 들어, 터치 여부와 무관한 상시 표시 + `Update()` 기반 매 프레임 재계산(터치 중엔 드래그 방향, 비터치 중엔 `BallLauncher.LaunchDirection` 기준)으로의 구조 변경이 결론으로 확정됨. `UIRules.md` 섹션 11의 "조준 중에만 표시" 문구도 함께 갱신 필요하다고 명시
- 이슈 2: `InputHandler.cs` 53행의 `(currentPos - _dragStartPosition).normalized`가 스크린(픽셀) 좌표 차이를 스크린→월드 변환 없이 그대로 정규화해 조준 방향으로 쓰고 있다는 점을 원인으로 특정. 구체적 해결책(`ScreenToWorldPoint` 등)은 plan.md로 이월한다고만 명시하고 research.md에는 포함하지 않음
- "관련 파일 및 의존성" 섹션에 `InputHandler.cs`/`TrajectoryPreview.cs`/`BallLauncher.cs`/`GameplayMechanics.md`(섹션 1)/`UIRules.md`(섹션 11) 5개 항목 정리

### 주요 결정사항
- 사용자 지시대로 plan.md는 이번에 작성하지 않음, 코드/문서(UIRules.md 등) 실제 수정도 하지 않음(research.md 1개 파일만 생성)
- AGENTS.md는 기존 정책(개별 task 폴더 별도 인덱싱 안 함)에 따라 갱신하지 않음

---

### 작업 내용 (추가)
- Ball Trajectory Aim Fix task plan.md 신규 생성 (research.md 기반)
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/plan.md`
- research.md 재확인 결과 이슈가 실제로는 3개(궤적 상시 표시, 터치 조준 정확도, 궤적 프리뷰 색상/크기)임을 확인 — 직전 세션의 research.md 완료 기록에 "두 이슈"로만 요약된 것은 3번째 이슈(색상/크기) 서술이 누락된 것으로 판단, 이번 plan.md는 research.md 원문 그대로 세 이슈 전부를 대상으로 작성
- 작성 전 `TrajectoryPreview.cs`, `InputHandler.cs`, `BallLauncher.cs`(`LaunchDirection`/`LaunchPoint` 정의부), `UIRules.md` 섹션 11, `DevRules.md`(네이밍/Awake 캐싱 규칙)를 Read로 재확인

### 결과
- plan.md 생성 완료: 구현 목표 4개, 단계별 작업 계획을 이슈 1(궤적 상시 표시)→이슈 2(스크린→월드 변환)→이슈 3(색상/크기 조정)→문서 갱신 순으로 구성
- 이슈 1: `TrajectoryPreview`의 `OnEnable`/`OnDisable` 이벤트 구독 제거, `HandleAimBegin`/`HandleDrag`/`HandleRelease` 삭제, `Update()` 신규 추가(`BallLauncher.Instance.LaunchDirection` 매 프레임 사용), `Awake()`의 `SetVisible(false)` → `SetVisible(true)`로 변경
- 이슈 2: `InputHandler`에 `Camera.main` 캐싱용 `_mainCamera` 필드 + `Awake()` 신규 추가, `_dragStartPosition`과 드래그 방향 계산을 `ScreenToWorldPoint` 변환 후 델타 계산 방식으로 전환, orthographic 카메라에서는 z값이 결과에 영향 없다는 점 참고 메모 포함
- 이슈 3: `_hitRing`을 `_hitColor` 대신 신규 `_ringColor` 필드(회백색 계열) 참조로 분리, `_hitColor` 기본값을 `Color.red`에서 `new Color32(206, 90, 82, 255)`로 변경, `_dotRadius` `0.08f`→`0.05f` 예시, `DASH_WORLD_SIZE` `0.3f`→`0.15f` 예시, `_lineColor`를 `Color.white`에서 `new Color32(225, 225, 220, 255)`로 변경. 모두 예시값이며 실제 화면에서 재조정 가능하다고 명시
- 예상 변경 파일: `TrajectoryPreview.cs`, `InputHandler.cs`, `UIRules.md`(섹션 11) 3개, 신규 생성 파일 없음을 명시
- 주의사항에 (1) 색상/크기 수치는 예시값, (2) `[SerializeField]` 인스펙터 오버라이드 우선 적용 가능성, (3) `Camera.main` 사용을 위한 `MainCamera` 태그 확인 필요, (4) `DevRules.md` 네이밍/캐싱 컨벤션 준수 4가지 명시

### 주요 결정사항
- 사용자가 이슈별로 구체적 구현 방향(메서드/필드 단위)을 이미 확정해서 제공했으므로, plan.md는 그 지시를 그대로 단계별 작업 계획으로 구조화하는 데 집중하고 임의 해석/추가 설계를 하지 않음
- research.md는 이번 세션에서 전혀 수정하지 않음(사용자 지시대로 plan.md만 신규 작성)
- AGENTS.md는 기존 정책(개별 task 폴더 별도 인덱싱 안 함)에 따라 갱신하지 않음
- 코드(`TrajectoryPreview.cs`, `InputHandler.cs`) 및 `UIRules.md`는 계획 문서 성격상 실제로 수정하지 않고 Read만 수행

---

## 2026-07-04

### 작업 내용
- Ball Trajectory Aim Fix task(이슈 1~5)가 전부 구현 완료되고 사용자가 유니티 에디터에서 직접 플레이 테스트를 마쳐 "조작감에 불편함이 없고 매우 좋다"고 확인해준 최종 완료 상태를 관련 문서 5곳에 반영
- 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/research.md`, `.../plan.md`, `Assets/_Project/Docs/GameplayMechanics.md`, `Assets/_Project/Docs/ProjectStatus.md`, `Assets/_Project/Docs/ProjectHistory.md`
- 작성 전 `InputHandler.cs`, `TrajectoryPreview.cs`(최신 코드 — `ComputeAimDirection` 헬퍼, `Update()` 기반 궤적 갱신, `_isDragging` 상태 기반 시작 판정 모두 반영되어 있음을 확인), 두 task 문서 전문, `UIRules.md` 섹션 11(이미 "항상 표시" + `_ringColor` 행까지 최신 반영되어 있어 이번엔 손대지 않음), 배경/해상도 대응 task의 research.md/plan.md(완료 문구 톤 참고용) 전부 Read로 재확인
- GameplayMechanics.md, ProjectStatus.md, ProjectHistory.md도 편집 전 전문을 Read로 재확인

### 결과
- `research.md`: "결론" 섹션 맨 끝에 "### 최종 구현 및 실제 플레이 테스트 완료" 소제목 신규 추가 — 이슈 1~5가 `InputHandler.cs`/`TrajectoryPreview.cs`에 전부 구현되어 main에 반영됐고, 사용자가 유니티 에디터에서 직접 플레이 테스트로 조작감 만족을 확인했다는 사실만 간결히 기록(배경/해상도 대응 research.md의 "최종 구현 및 실기기 검증 완료" 문단과 동일한 톤)
- `plan.md`: 이슈 1~5 각 섹션 제목 끝에 " — (구현 완료)" 표시 추가, "예상 변경/생성 파일 목록" 제목에 "(최종 구현 상태 — 이슈 1~5 전부 구현 완료)" 추가하고 각 파일 항목을 "(수정)" → "(수정 완료)"로 변경하며 실제 최종 변경 내용(스크린→월드 변환, 절대 조준 모델 전환, `_isDragging` 상태 기반 재구성 등)을 요약 반영, 목록 하단에 최종 테스트 확인 문장 추가
- `GameplayMechanics.md`: 섹션 1 "구현 현황"에 새 bullet 추가 — `_Task/2026-07-03/15-41_ball-trajectory-aim-fix`로 반영된 5개 변경사항(궤적 상시 표시 전환, 스크린→월드 보정, 색상/크기 조정, 절대 조준 모델 전환, 터치 시작 폴링 누락 수정)을 요약하고, 이슈 4 research.md 분석(기존 스펙 문구 "터치하는 순간 조준 방향이 즉시 정해진다"/"드래그 위치를 목표로 실시간으로 따라간다"가 절대 조준 모델과 더 잘 맞음)을 근거로 위쪽 "확정된 스펙 서술" 원문은 그대로 유지하면서 구현 현황 쪽에서 부합 여부만 확인하는 방식으로 처리(스펙 문구 자체는 수정하지 않음)
- `ProjectStatus.md`: "완료된 작업" 체크리스트에 "볼 궤적 조준 개선(`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`)" 항목 신규 추가 — 5개 이슈와 플레이 테스트 확인 결과를 한 문장으로 요약
- `ProjectHistory.md`: 기존 "## 2026-07-03" 섹션 맨 하단(배경/해상도 대응 다음)에 "### 볼 궤적 조준 개선" 소제목 신규 추가 — 배경(이슈 1~3 선행 구현 후 실 플레이 테스트에서 이슈 4, 이슈 4 구현 후 이슈 5가 순차 재발견된 경위)/이슈별 원인·해결 요약(1~5 각각)/검증(main 반영 + 실제 플레이 테스트 확인) 순서로 기존 섹션들과 동일한 서술 형식 유지

### 주요 결정사항
- UIRules.md 섹션 11은 Read로 확인한 결과 이미 "터치 여부와 무관하게 항상 표시" 문구 + `_ringColor` Inspector 표 행까지 최신 반영되어 있어, plan.md의 "예상 변경/생성 파일 목록"에서도 "(수정 완료)"로만 표시하고 실제 파일은 건드리지 않음
- GameplayMechanics.md 섹션 1 상단의 "확정된 스펙 서술"(원본 게임 확인 내용) 문구 자체는 사용자 지시대로 삭제/재작성하지 않고 그대로 유지 — 이슈 4 research.md에서 이미 이 문구가 절대 조준 모델과 더 잘 맞는다고 분석이 끝난 상태이므로, 별도 문구 수정 없이 "구현 현황"에서 이 부합 사실만 확인하는 방식으로 처리(문구를 다시 쓰면 오히려 원본 확인 기록의 원문성이 훼손된다고 판단)
- 기존 내용은 어디에도 삭제/재작성하지 않고 "완료" 상태를 반영하는 문장만 각 문서에 추가하는 방식으로만 편집(사용자가 명시적으로 요청한 제약)
- AGENTS.md는 기존 정책(개별 task 폴더 별도 인덱싱 안 함)에 따라 이번에도 갱신하지 않음

---

### 작업 내용 (추가)
- 몬스터 전용 통합 규칙 문서 `MonsterRules.md` 신규 생성 (사용자 승인된 목차 기준, task 문서 없이 순수 문서 작업으로 진행)
- 경로: `Assets/_Project/Docs/MonsterRules.md`
- `GameplayMechanics.md` 섹션 2 본문을 `MonsterRules.md`로 이관하고 원래 자리에는 안내 문구 + 링크만 남김 (섹션 1은 손대지 않음)
- `AGENTS.md` Docs 문서 표에 `MonsterRules.md` 행 신규 추가
- 작성 전 `MonsterData.cs`, `MonsterBase.cs`(최근 재설계된 냉동/슬로우/도트/보너스크리티컬 API 전부 포함), `WaveTableData.cs`, `WaveManager.cs`, `Ball.cs`(전체, `CalculateDamage`/`OnCollisionEnter2D`/`OnTriggerEnter2D` 흐름 확인), `BallSkillBase.cs`, `MonsterSetupEditor.cs`(웨이브별 몬스터 등장 구간 및 기본 스탯 자동 생성 로직), `GameplayMechanics.md` 섹션 2, `UIRules.md` 섹션 9/10, `DevRules.md`(ScriptableObject 사용 범위), `ProjectHistory.md`(`SetupWaveSpawnEntries` 기록)를 모두 Read로 직접 확인

### 결과
- `MonsterRules.md` 생성 완료: 1.개요 / 2.스폰·전진 메커닉(GameplayMechanics.md 섹션2 원문 이관) / 3.몬스터 종류·스탯(Fluffy/Spider/StoneBug/ForestDeer 웨이브별 등장 구간 표 + MonsterData 필드 표, 4종 기본값이 현재 모두 동일함을 각주로 명시) / 4.HP 관리·사망 처리(TakeDamage/Die/OnHpChanged + Ball→Monster 데미지 전달 흐름을 Ball.cs 실제 코드 기준으로 상세 기술, 낡은 task 문서의 static event 기반 충돌 감지 설계는 참고하지 않았음을 명시) / 5.상태이상 처리(ApplyFreeze/ApplySlow/ApplyBonusCritChance·ConsumeBonusCritChance/ApplyDot 표 + IceBallSkill/FireBallSkill/AmethystDaggerPassive/EmeraldDaggerPassive 연동 지점) / 6.웨이브 시스템(WaveTableData 구조, WaveManager 스폰→진행→처치/통과 판정→클리어→AdvanceToNextWave 흐름, GetWeakestMonster/GetMonstersInRow 헬퍼) / 7.UI 연동 참조(UIRules.md 9/10장 링크만, 중복 서술 없음) / 8.관련 파일 목록 표까지 총 8개 섹션 작성 완료
- `GameplayMechanics.md` 섹션 2 본문을 "이 내용은 MonsterRules.md로 이관되었습니다" 안내 문구 + 링크로 교체 완료 (섹션 1은 변경 없음)
- `AGENTS.md` Docs 문서 표에 `MonsterRules.md` 행 추가 완료
- `UIRules.md`는 사용자 지시대로 전혀 수정하지 않음

### 주요 결정사항
- 몬스터 관련 규칙의 단일 기준(source of truth)을 `MonsterRules.md`로 확정 — 향후 몬스터 관련 규칙 변경/추가는 이 문서를 기준으로 갱신
- `_Task/2026-06-30/14-00_Monster시스템구현/`의 초기 설계(static event `OnHitMonster`/`LastDamage` 기반 충돌 감지)는 현재 코드(Unity `OnCollisionEnter2D`/`OnTriggerEnter2D`에서 `CalculateDamage()`가 직접 `TakeDamage()` 호출, `OnHitMonster`는 데미지 적용 "이후" 다른 시스템에 알리는 용도로만 사용)와 다르다는 점을 4장에 명시적으로 각주 처리해 향후 혼동 방지
- `MonsterSetupEditor.cs` 확인 결과 4종 몬스터의 `MonsterData` 기본값이 현재 모두 동일(Hp 30/MoveSpeed 1/Damage 1/Reward 10)함을 확인 — 종별 차등 수치는 코드에 하드코딩되어 있지 않고 이후 에셋 Inspector 직접 조정으로 반영되는 구조임을 3장 각주로 정확히 기록(과장하거나 임의로 차등값을 지어내지 않음)
- Ball의 스킬 연동(냉동/슬로우/도트/보너스크리티컬)은 `PlayerActiveSkillDesign.md`의 4종 스킬이 아니라 `BallSkillBase`를 상속하는 Active/Passive 볼 스킬 클래스(`IceBallSkill`/`FireBallSkill`/`AmethystDaggerPassive`/`EmeraldDaggerPassive`)가 실제로 호출하는 구조임을 코드로 직접 확인 후 5장에 정확히 기술
- Bash 미사용, Read/Edit/Write/Glob/Grep만 사용해 작업 완료

---

### 작업 내용 (추가)
- 사용자·오케스트레이터가 긴 논의 끝에 확정한 몬스터 시스템 개편 설계를 `MonsterRules.md`와 `UIRules.md`(섹션 9)에 순수 문서 작업으로 반영. 코드는 전혀 수정하지 않음(구현은 이후 별도 task로 진행 예정)
- 경로: `Assets/_Project/Docs/MonsterRules.md`, `Assets/_Project/Docs/UIRules.md`
- 작업 전 최신 상태 확인을 위해 `MonsterRules.md`, `UIRules.md` 전문과 `MonsterData.cs`, `MonsterBase.cs`, `WaveManager.cs`, `WaveTableData.cs`, `MonsterSetupEditor.cs`를 모두 Read로 직접 재확인. 특히 `MonsterSetupEditor.SetupWaveSpawnEntries()`가 에디터에서 한 번 실행 시점에 웨이브별 `GridPosition`을 고정값으로 계산해 `WaveTableData.asset`에 미리 구워넣는 구조임을 코드로 직접 확인(런타임 랜덤이 아님 — 기존 `MonsterRules.md` 2장에 이미 있던 "매 웨이브 새로 랜덤 결정" 서술과 실제로 어긋나 있었음)
- 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/KakaoTalk_20260701_190324151_02.jpg`)을 Read로 직접 열람해 몬스터가 블록(발판) 위에 서 있고 HP바(빨간 바)가 블록 앞면 하단에 임베드되어 있으며 폭이 블록 가로 길이에 비례함을 시각적으로 확인 후 문서에 반영

### 결과
- `MonsterRules.md` 수정 완료: 서두 요약에 이번 갱신 내용(전종류 랜덤 등장/런타임 랜덤 배치+점유체크/고정 블록 크기/HP바 방식) 2줄 추가. 2장(스폰/전진 메커닉)에 "웨이브 1~20 전 구간 4종 전부 랜덤 등장" + "런타임 매번 새로 랜덤 계산 + 점유 체크" 확정 규칙 추가, 구현 현황에 현재 코드가 에디터에서 좌표를 고정 계산해 굽는 방식이라 확정 규칙과 다르다는 점을 "(구현 예정 — 아직 코드 미반영)"으로 명시. 3장(몬스터 종류·스탯)에 "종류 및 고정 블록(베이스) 크기" 소제목 신규 추가(Fluffy/Spider→Block_1x1, StoneBug→Block_2x1, ForestDeer→Block_1x2, Block_2x2 미사용, 콜라이더 전체 커버, 점유 체크 필요), "웨이브별 등장 구성 — 전종류 랜덤" 소제목 신규 추가(기존 웨이브 구간별 등장 표 삭제, 난이도 스케일링 방향성만 서술하고 수치 공식은 미정으로 명시), MonsterData 필드 표 아래에 "2칸 몬스터 Hp/Reward 상향" 확정 규칙 + 현재 코드가 4종 동일 기본값임을 대비해 명시. 6장(웨이브 시스템)을 "데이터 구조 — 신규 확정 구조"(WaveTableData가 좌표 대신 스폰수/가중치 구성 파라미터만 보유)와 "WaveManager 흐름 — 신규 확정 흐름"(SpawnWave가 매번 종류 랜덤 결정→점유 체크 기반 좌표 랜덤 결정→배치 3단계)으로 재구성하고, 기존 현재 구현 흐름은 인용 블록으로 하위에 유지하며 스킬/킬카운트/클리어 판정 로직(5~7번)은 그대로 유지된다는 점 명시. 7장(UI 연동 참조)을 HP바 블록 앞면 임베드 방식으로 갱신하고 UIRules.md 9장 링크 유지. 1장 개요의 HP바 서술도 최신 방식으로 갱신
- `UIRules.md` 9장 "몬스터 HP바" 전면 재작성 완료: 기존 "머리 위 월드 스페이스 캔버스+슬라이더" 서술 삭제. "확정된 방식 — 블록(베이스) 앞면 임베드"(블록 앞면 하단 임베드, 폭이 블록 가로 길이에 비례, 정확한 비율은 `targetUI/` 레퍼런스 기준), "배치 방식(앵커/부모 구조 변경)"(HP바를 블록 오브젝트의 자식으로 이동, Canvas 설정 World Space/Sorting Layer UI는 유지), "재사용 가능한 부분"(`MonsterHpBar`, `MonsterBase.OnHpChanged` 이벤트 구독 구조와 발행 시점은 그대로 재사용 가능, 바뀌는 것은 부모/앵커/크기뿐) 3개 소제목으로 재구성
- `AGENTS.md`는 두 문서 모두 기존 표의 설명 문구가 이미 "몬스터 HP바"/"스폰 및 전진 메커닉" 등 주제 단위로 서술되어 있어 세부 내용이 바뀌어도 그대로 유효 — 신규 섹션 추가가 아니므로 갱신하지 않음

### 주요 결정사항
- 이번 작업은 순수 문서 갱신이며 코드(`MonsterData.cs`/`MonsterBase.cs`/`WaveManager.cs`/`WaveTableData.cs`/`MonsterSetupEditor.cs`)는 전혀 수정하지 않음 — 구현은 이후 별도 task(research.md/plan.md)로 진행 예정이므로 이번 문서에는 "확정된 규칙"만 서술하고 정확한 수치 공식(스폰 수 증가 폭, 가중치 곡선, 2칸 몬스터 스탯 배율 등)은 미정으로 남겨둠
- 확정 규칙과 현재 코드 상태가 다른 부분은 전부 "(확정)" / "(구현 예정 — 아직 코드 미반영)" / "(현재 구현 — 아직 새 규칙 미반영)" 3가지 표시로 명확히 구분해 향후 구현 task 담당자가 무엇을 바꿔야 하는지 문서만 보고 파악 가능하게 처리
- 배경 이미지 비율 보정(`BackgroundFitter`/`WallFitter`)은 볼 충돌벽/캐릭터 위치 등에 영향을 주는 위험도 높은 별도 선행 task로 분리하기로 확정되어 있어, `MonsterRules.md` 2장에 "그리드는 정사각형 셀 전제, 배경 비율 보정은 별도 선행 task 예정"이라는 한 줄만 남기고 상세 내용은 다루지 않음
- `WaveTableData`의 정확한 신규 필드 설계(가중치를 배열로 표현할지, 몬스터별 확률 테이블로 표현할지 등)는 이 문서에서 확정하지 않고 "추후 plan.md에서 결정"으로 명시 — 문서 작업 단계에서 구현 세부사항을 임의로 확정하지 않기 위함

---

## 2026-07-04

### 작업 내용
- 배경 격자 정사각형 보정 task research.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-04/16-40_background-square-grid-fix/research.md`
- 작업 전 `BackgroundFitter.cs`, `WallFitter.cs`, `ProjectHistory.md`(2026-07-03 "배경/해상도 대응" 섹션), `MonsterRules.md` 2장/3장, `SceneSetupEditor.cs`(Step4/5/6, `ConnectBackgroundFitterRefs`/`Step5_PlaceWallsAndGround`/`Step6_SetupWallFitter`)를 모두 Read로 직접 확인
- 오케스트레이터가 사전 제공한 `Background_1_Stage.png` 실측 수치(세로 140px×가로 85px 비정사각형 격자, 몬스터/블록 스프라이트는 정사각형 1유닛, 원본 게임 레퍼런스는 약 97×97px 정사각형 격자)는 재측정 없이 그대로 인용

### 결과
- research.md 생성 완료: "현재 상태"(배경 텍스처 실측치, `BackgroundFitter`의 가로/세로 독립 Stretch 계산식, `WallFitter`의 실측 기준 좌표×동일 배율 연동 방식, `SceneSetupEditor` Step4/5/6 연결부, `MonsterRules.md`의 선행 작업 언급), "관련 파일 및 의존성" 표, "문제점/구현 대상 파악"(핵심 쟁점: 텍스처 고유 비율 보정 vs 모든 기기 최종 렌더링 정사각형 보장의 트레이드오프, 파생 쟁점: 픽셀 완전 정합 vs 육안 자연스러움, 영향 범위), "결론"(선택지만 나열, 결정은 plan.md로 유보) 순서로 작성
- 해결책은 확정하지 않았으며 plan.md는 작성하지 않음 (사용자 지시대로 이번 범위에서 제외)

### 주요 결정사항
- Stretch 방식(기기별 가로/세로 독립 배율)이 유지되는 한 "모든 기기에서 최종 화면상 격자가 항상 정사각형"임을 수학적으로 보장하기 어렵다는 점, 반대로 "텍스처 자체의 비정사각형 셀을 보정 계수로 정사각형화"하는 것은 Stretch 정책과 공존 가능하다는 점을 구분해 서술 — 이 구분이 이번 문서의 핵심 목적이므로 결론에서도 재차 강조
- `ProjectHistory.md`에 이미 기록된 "Wall 좌표가 배경 격자 그림과 애초에 픽셀 단위로 일치하지 않았고, 실기기 테스트로 육안 자연스러움 기준까지만 조정해 확정한 선례"를 근거로, 몬스터 스폰용 논리적 그리드도 배경 텍스처와 완전한 픽셀 정합이 필수는 아닐 수 있다는 점을 파생 쟁점으로 함께 제시
- AGENTS.md는 "개별 task 폴더 목록은 이 문서에서 별도로 관리하지 않는다"는 기존 방침에 따라 이번 task 등록을 위한 갱신은 하지 않음

---

### 작업 내용 (추가)
- 배경 격자 정사각형 보정 task plan.md 신규 생성 (research.md 후속, 오케스트레이터·사용자 논의로 확정된 해결 방향을 그대로 반영)
- 경로: `Assets/_Project/Docs/_Task/2026-07-04/16-40_background-square-grid-fix/plan.md`
- 작업 전 research.md, TaskRules.md, `BackgroundFitter.cs`, `WallFitter.cs`, `SceneSetupEditor.cs`(`Step4_PlaceBackground`/`ConnectBackgroundFitterRefs`/`Step5_PlaceWallsAndGround`/`Step6_SetupWallFitter` 라인 번호 포함)를 모두 Read로 직접 재확인. 톤/형식 일관성을 위해 `_Task/2026-07-03/12-30_background-resolution-fix/plan.md`(가장 유사한 선례)도 Read로 참고

### 결과
- plan.md 생성 완료: 확정된 해결 방향("격자 영역 기준 균일 스케일 + 장식 테두리는 여백으로 흡수")을 그대로 반영한 2단계 계산 공식(1. 고정 상수 `_cellAspectCorrection≈1.647`/`_gridAreaWidth=14.53`/`_gridAreaHeight=10.16`으로 격자 셀 정사각형 보정, 2. `Mathf.Max` Cover 방식 균일 배율 계산 후 `scaleY = uniformScale × _cellAspectCorrection`)을 구체 코드 스니펫으로 작성. 이 공식이 "셀 정사각형 보장"과 "격자 영역 Cover 보장" 두 조건을 동시에 만족함을 수식으로 직접 검증한 뒤 문서에 반영
- 단계별 계획 5단계 구성: 1단계(공식 정리), 2단계(`BackgroundFitter.cs` 계산식 교체), 3단계(`WallFitter.cs` 동일 계산식 교체 + `_native*` 값 재조정 필요성 명시), 4단계(`SceneSetupEditor.cs`의 `ConnectBackgroundFitterRefs()`/`Step6_SetupWallFitter()`에 신규 필드 3개 주입 코드 추가), 5단계(사용자 로컬 Scene Setup 재실행 + 실기기 검증 필요성을 이번 task 범위 밖으로 명시)
- 예상 변경 파일 목록(`BackgroundFitter.cs`/`WallFitter.cs`/`SceneSetupEditor.cs` 수정, `SampleScene.unity`는 간접 영향, PNG는 변경 없음)과 주의사항(리소스 미수정, 두 스크립트 계산식 일관성 필수, 벽/발사 위치 직접 영향, 원격 환경엔 Unity 없어 실기기 검증 불가, `_zoomFactor` 유지 여부는 dev 판단, 기존 중복 계산 구조를 리팩토링하지 않음) 작성 완료
- 문서 말미에 사용자의 명시적 승인 후에만 구현 시작한다는 문구 포함

### 주요 결정사항
- 텍스처 고유 보정(고정, 기기 무관)과 기기별 Cover 균일 배율(가변)을 명확히 분리된 2개 레이어로 공식화 — `scaleX = uniformScale`, `scaleY = uniformScale × _cellAspectCorrection` 형태로, "가로/세로 동일 배율"이 문자 그대로 `scaleX == scaleY`를 의미하는 게 아니라 "기기별 가변 배율 레이어가 두 축에 동일하게 적용된다"는 뜻임을 계산식과 검증 과정을 통해 명확히 하고 plan.md에 그대로 기술
- Cover 기준값을 전체 스프라이트 크기가 아닌 "격자 영역"의 실측 월드유닛 크기(`_gridAreaWidth=14.53`, `_gridAreaHeight=10.16`, 이미지 픽셀 실측 x199~1652/y540~1556 기준)로 새로 정의 — 기존 `_spriteRenderer.sprite.bounds.size` 참조는 새 계산식에서 불필요해지지만, 외과적 변경 원칙에 따라 필드 자체를 삭제하는 리팩토링은 하지 않기로 명시
- `WallFitter`의 `_nativeLeftX`/`_nativeRightX`/`_nativeTopY`/`_nativeBottomY`/`_nativeLaunchPointY` 5개 값은 계산식 교체만 하고 값 자체는 이번 코드 구현에서 건드리지 않음 — 스케일 비율 구조가 근본적으로 바뀌므로(scaleY가 항상 scaleX×1.647의 고정비) 재조정은 사용자의 로컬 실기기 검증 몫으로 명시적으로 넘김
- `BackgroundFitter`/`WallFitter`의 기존 계산 로직 중복 구조(공용 유틸리티 미분리)는 이번에도 그대로 유지 — 요청 범위를 벗어나는 리팩토링이므로 손대지 않음
- Bash 미사용, Read/Write만 사용해 작업 완료

---

### 작업 내용 (추가)
- 배경 격자 정사각형 보정 task plan.md 사용자 피드백 반영 수정 (Bash 미사용, Read/Edit만 사용)
- 경로: `Assets/_Project/Docs/_Task/2026-07-04/16-40_background-square-grid-fix/plan.md`
- 사용자 요청: 이미 여러 Step으로 커진 기존 `SceneSetupEditor.cs`를 신규 필드 3개 주입을 위해 계속 편집하면 혼선이 생긴다는 우려로, `SceneSetupEditor.cs`는 전혀 건드리지 않고 신규 필드 3개(`_cellAspectCorrection`/`_gridAreaWidth`/`_gridAreaHeight`) 주입 전용의 완전히 별도인 신규 에디터 스크립트를 작성하는 방식으로 변경. `BackgroundFitter.cs`/`WallFitter.cs`(런타임 계산식 자체 교체)는 기존 계획대로 "수정"이 맞다는 것을 사용자가 재확인 — 바뀐 범위는 4단계(에디터 연동 부분)뿐

### 결과
- 4단계 제목/내용을 "`SceneSetupEditor.cs` 연동"에서 "`BackgroundGridFitSetupEditor.cs` 신규 작성 (필드 3개 주입 전용)"으로 전면 교체: 신규 파일 `Assets/_Project/Scripts/Editor/BackgroundGridFitSetupEditor.cs`, 별도 `MenuItem`(`PurpleCow/Setup/Background Grid Fit Setup`), 기존 `SceneSetupEditor.cs`/`MonsterSetupEditor.cs`와 동일한 탐색(`GameObject.Find`/`FindObjectOfType`) + `SerializedObject`/`FindProperty` 주입 패턴을 그대로 따르되 완전히 독립된 신규 스크립트로 작성한다는 점을 명시
- 5단계(사용자 로컬 검증)에 "신규 메뉴 `Background Grid Fit Setup`을 별도로 실행" 문구 반영, 기존 `Scene Setup` 메뉴는 이번에 수정되지 않았으므로 그대로 두고 신규 메뉴만 추가 실행하면 된다는 점 명시
- 예상 변경/생성 파일 목록에서 `SceneSetupEditor.cs` 항목 삭제, `BackgroundGridFitSetupEditor.cs`(신규 생성) 항목으로 교체
- 주의사항에 "기존 `SceneSetupEditor.cs`는 이번 작업에서 수정하지 않으며, 신규 필드 주입은 별도의 새 에디터 스크립트로 분리한다" 문장 추가
- 문서 전반의 교차 참조(3단계의 "4단계 참고"→"5단계 참고", 파일 목록/주의사항의 "Scene Setup 재실행"→"Background Grid Fit Setup 메뉴 실행") 일관성 있게 함께 수정
- 1~3단계(구현 목표, 계산 공식, `BackgroundFitter.cs`/`WallFitter.cs` 수정 계획)는 요청대로 전혀 건드리지 않음

### 주요 결정사항
- `BackgroundFitter.cs`/`WallFitter.cs`는 계산식 자체를 바꾸는 목적이므로 기존 파일 "수정"이 맞고, 에디터 스크립트만 "신규 작성"으로 분리하는 것이 이번 피드백의 핵심 — 두 성격(런타임 스크립트 수정 vs 에디터 스크립트 신규 분리)을 문서에서 명확히 구분해 서술
- 신규 에디터 스크립트는 `BackgroundFitter`와 `WallFitter` 두 컴포넌트 모두에 동일한 필드 3개 값을 주입하도록 범위를 명시 (씬 오브젝트 탐색은 기존 관행 그대로)
- `_native*` 값 재조정이 필요해질 경우 어느 스크립트(기존 `SceneSetupEditor.cs` vs 신규 `BackgroundGridFitSetupEditor.cs`)에 반영할지는 이번 plan.md에서 미리 결정하지 않고 사용자의 실기기 검증 이후 시점으로 판단을 유보
- Bash 미사용, Read/Edit만 사용해 작업 완료

---

## 2026-07-04 (추가)

### 작업 내용
- 배경 격자 정사각형 보정 task(`_Task/2026-07-04/16-40_background-square-grid-fix`)가 실기기 검증까지 완료됨에 따라, `ProjectHistory.md`/`ProjectStatus.md` 갱신
- 작업 전 두 문서 및 해당 task의 research.md/plan.md를 모두 Read로 확인해 기존 문체/형식 파악 후 반영

### 결과
- `ProjectHistory.md`: 마지막 섹션(2026-07-03) 뒤에 `## 2026-07-04` 신규 날짜 섹션 추가 — 몬스터 시스템 개편 선행 작업이라는 배경, 격자 비정사각형(140×85px) 문제, `BackgroundFitter.cs`/`WallFitter.cs`의 2단계 계산식 교체(텍스처 고유 비율 보정 + 격자 영역 기준 균일 Cover 배율), 신규 필드 3개를 별도 `BackgroundGridFitSetupEditor.cs`로 분리 주입한 결정, 실기기 검증 결과(격자 정사각형 정상 렌더링 확인) 및 그 과정에서 발견된 `_zoomFactor` 과다 확대 문제와 최종값(1.3 → 0.5, 에디터 미리보기는 0.6) 조정을 기록
- `ProjectStatus.md`: "현재 상태" 날짜를 2026-07-04로, 단계 설명을 "몬스터 시스템 개편 선행 작업(배경 격자 정사각형 보정)까지 실기기 검증 완료"로 갱신. "완료된 작업" 목록에 이번 task 항목 추가. "다음 작업 순서" 1번으로 "몬스터 시스템 개편 research.md 작성 착수"를 신규 추가하고 기존 "실제 플레이 테스트 진행" 항목은 2번으로 유지
- `_nativeLeftX` 등 5개 벽 기준값 재조정 여부는 사용자가 이번에 별도 문제를 보고하지 않았다는 사용자 지시 내용을 그대로 반영해, "기존 값 유지로 마무리"라고만 서술하고 재조정 여부를 확정적으로 서술하지 않음 — 정확히 확인된 조정 사항은 `_zoomFactor` 값뿐이라는 점을 문서에 명시

### 주요 결정사항
- ProjectHistory.md 신규 섹션은 기존 날짜 섹션과 동일하게 `### 소제목 (task 경로)` 형식 + `- **문제**/**해결**/**실기기 검증**` 굵은 라벨 bullet 구조로 작성해 2026-07-03 섹션들과 문체 일관성 유지
- ProjectStatus.md 완료 목록 항목은 기존 항목들과 동일하게 task 경로를 인라인 코드로 병기하고 한 문단으로 요약하는 기존 패턴을 그대로 따름
- Bash 미사용, Read/Edit만 사용해 작업 완료

---

### 작업 내용 (추가)
- 몬스터 시스템 개편 본작업 research.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-04/22-53_monster-system-overhaul/research.md` (폴더 신규 생성 포함)
- `MonsterRules.md`(최근 갱신본), `MonsterData.cs`, `MonsterBase.cs`, `WaveTableData.cs`, `WaveManager.cs`, `MonsterSetupEditor.cs`, `BackgroundFitter.cs`, `WallFitter.cs`, `UIRules.md` 9장을 모두 직접 Read로 확인 후 작성
- 오케스트레이터가 이미 확인한 프리팹 구조(캐릭터 4종 vs 블록 스텁 4종 완전 분리, 블록+캐릭터 합성 프리팹 부재)와 스프라이트 픽셀 실측 결과(Pillow 측정값)를 그대로 인용해 재확인 없이 반영

### 결과
- research.md 작성 완료: 현재 상태(프리팹 구조/스프라이트 실측/`MonsterData` 필드/`MonsterBase` 동작/콜라이더/HP바/`WaveTableData` 구조/`WaveManager` 흐름/`MonsterSetupEditor` 자동화/격자 영역 정보 총 10개 세부 항목), 관련 파일 및 의존성 표(신규 파일 없이 기존 파일 14개 + asset 2종), 문제점/구현 대상 8개 항목(MonsterData 블록 크기 표현 선택지 A/B, 프리팹 재구성, 콜라이더 확장, HP바 재배치, WaveTableData 구조 변경, WaveManager 랜덤 배치+점유체크, MonsterSetupEditor 대체 방안, 기존 WaveTableData.asset 재생성 필요성), 결론(4갈래 요약) 작성
- AGENTS.md는 개별 task 폴더 목록을 별도 인덱스로 관리하지 않는다는 기존 규칙(59번 줄)을 확인하여 인덱스 추가 작업은 수행하지 않음

### 주요 결정사항
- 해결책/구현 방법은 확정하지 않고 선택지만 나열하는 원칙을 지켜, MonsterData 블록 크기 필드 추가 여부·WaveEntry 가중치 필드 설계·그리드 열/행 계산 방식 등은 모두 복수 선택지로만 서술하고 plan.md로 결정을 미룸
- 콜라이더/HP바 재배치 이슈는 "블록 크기를 어디서 읽어올지"(선택지 1번)와 서로 연결된 하위 질문이라는 점을 명시해 plan.md 논의 시 연계 검토가 필요함을 강조
- 기존 `WaveTableData.asset`의 구조 변경 시 재직렬화 문제는 원격 텍스트 환경의 한계로 명시하고, 에디터 자동화 스크립트 처리 여부와 사용자 수동 처리 여부 모두 열어둠
- Bash 미사용, Read/Write만 사용해 작업 완료

---

## 2026-07-05

### 작업 내용
- `UIRules.md` 1장 Canvas 계층도에서 융합 시스템 잔재로 판단된 `PrismPanel` 줄 제거
- 경로: `Assets/_Project/Docs/UIRules.md`

### 결과
- `Canvas_Panel` 하위 목록에서 `└─ PrismPanel` 줄 삭제, 그 앞 `BallLevelUpPanel` 줄의 트리 문자를 `├─` → `└─`로 수정해 마지막 항목으로 정리 (LevelUpPanel/PausePanel/BallLevelUpPanel 3개만 남음)
- 문서 전체 `Prism` grep 결과 해당 계층도 1곳(수정 전 27번째 줄)뿐이었고 다른 언급은 없어 추가 조치 불필요
- Read/Grep/Edit만 사용, Bash 미사용, `UIRules.md` 외 다른 파일은 수정하지 않음

### 주요 결정사항
- PDF 스펙 "구현 제외 항목"에 명시된 융합 시스템 관련 잔재 패널이라는 사용자 판단을 그대로 반영해 삭제, 별도 대체 문구 없이 목록만 축소

---

### 작업 내용 (추가)
- 궤적 프리뷰 고리(Ring) 점선화 및 회전 효과 task research.md 신규 생성
- 경로: `Assets/_Project/Docs/_Task/2026-07-05/11-20_trajectory-ring-dash-rotate/research.md` (폴더 신규 생성 포함)
- `TrajectoryPreview.cs`, `GameplayMechanics.md` 섹션 1, `UIRules.md` 섹션 11, `Assets/Scenes/SampleScene.unity`의 `TrajectoryPreview` 컴포넌트 직렬화 값, `Assets/_Project/Docs/targetUI/` 레퍼런스 이미지 3장(`KakaoTalk_20260701_190324151.jpg`, `_01.jpg`, `_02.jpg`)을 모두 직접 Read로 확인 후 작성

### 결과
- research.md 작성 완료: `_hitRing`이 `CreateSolidTexture()` 기반 24각형 실선이며 회전 로직이 전혀 없다는 점, `_trajectoryLine`은 이미 `CreateDashTexture()` + `textureMode=Tile` 점선 패턴을 쓰고 있어 재사용 가능한 기존 구조라는 점을 확인해 기술
- 사용자가 별도 요청한 "궤적선 색상 Inspector 조절 가능화"는 코드(`_lineColor`/`_hitColor`/`_ringColor` 등 6개 `[SerializeField]` 필드)와 씬 오버라이드 값(코드 기본값과 6개 필드 전부 완전 일치, 표로 정리) 양쪽 모두 이미 충족되어 있어 추가 구현 불필요하다는 점을 명확히 기술 — 과거(2026-07-03) 씬 오버라이드가 코드 기본값을 가리던 전례를 재확인했으나 이번 건은 문제 없음
- 레퍼런스 이미지 3장에서 레드닷 주변 고리 마커가 완전한 이어진 실선이 아니라 끊어진 호/틱 형태로 보인다는 시각적 근거를 이미지 좌표와 함께 서술(회전 자체는 정지 이미지로 검증 불가하다는 한계도 명시)
- 문제점/구현 대상에 점선화(둘레 길이 기준 `mainTextureScale` 별도 계산 필요성), 회전(`DrawCircle()`이 매 프레임 월드 좌표를 직접 `SetPosition`하므로 `transform.Rotate()`가 무의미할 수 있고 각도 계산 자체에 시간 기반 오프셋을 더하는 방식이 필요하다는 방향성), 회전 속도 Inspector 노출 여부 등 3개 항목 기술
- plan.md는 작성하지 않음(TaskRules.md 워크플로우에 따라 사용자 확인 대기)

### 주요 결정사항
- 구체적 구현 방법(점선 세그먼트 개수, 회전 속도/방향, Inspector 노출 여부)은 research.md에서 확정하지 않고 열린 질문 3건으로 명시해 plan.md 단계로 미룸
- 코드는 읽기만 하고 수정하지 않음, git 커밋/푸시 없음, Bash 미사용, Read/Edit/Write/Glob/Grep만 사용

---

## 2026-07-05 (볼-볼 충돌 버그 plan.md)

### 작업 내용
- 볼-볼 물리 충돌 버그 task plan.md 신규 생성 (research.md는 이미 사용자 확인 완료 상태, 오케스트레이터-사용자 논의로 확정된 해결 방식 (a)를 그대로 STEP 구조로 반영)
- 경로: `Assets/_Project/Docs/_Task/2026-07-05/16-40_ball-ball-collision-fix/plan.md`
- 작성 전 `BallSetupEditor.cs`, `BallLauncher.cs`, `ProjectSettings/TagManager.asset`을 직접 재확인해 레이어 배열 빈 슬롯 위치(인덱스 8)와 기존 태그 등록 패턴(`AddRequiredTags()`)을 재검증

### 결과
- plan.md 작성 완료: 구현 목표(전용 "Ball" Physics2D 레이어 신설 + `Ball.prefab` 레이어 재배치 + `BallLauncher.Awake()`에 `Physics2D.IgnoreLayerCollision` 1회 호출), 단계별 작업 계획 4단계(레이어 등록 에디터 코드, 프리팹 레이어 할당, 런타임 1회 호출, 문서 갱신 불필요 판단), 예상 변경/생성 파일 목록(`BallSetupEditor.cs`, `Ball.prefab`, `BallLauncher.cs`, `TagManager.asset`), 주의사항 6건 작성
- "Ball" 태그 미등록 버그는 이번 범위에서 고치지 않고 그대로 둠(사용자 확정), `TrajectoryPreview.cs`의 `IsBlockingTag()`가 순수 태그 기준(`CompareTag`)이라 레이어 변경과 무관함을 주의사항에 명시(오케스트레이터가 이미 확인 완료한 사실을 근거로 인용)
- 신규 문서 생성이 아니라 기존 task 폴더에 plan.md만 추가하는 것이라 AGENTS.md 인덱스 변경 불필요(개별 task 폴더는 별도 관리하지 않는 기존 정책)

### 주요 결정사항
- 해결 방식은 (a) 전용 Ball 레이어 + `IgnoreLayerCollision` 전역 1회 호출로 확정(사용자 결정 그대로 반영), Wall/Ground/Monster는 Default 레이어에 유지
- 레이어 등록(`TagManager.asset`)과 프리팹 `m_Layer` 할당 순서 보장 필요성(`LayerMask.NameToLayer` 값 조회가 저장 이후여야 함)을 주의사항에 명시
- 코드/프리팹은 건드리지 않고 plan.md만 작성, git 커밋/푸시 없음, Bash 미사용, Read/Write만 사용
