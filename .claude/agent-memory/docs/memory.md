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
<<<<<<< HEAD
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
