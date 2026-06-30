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
