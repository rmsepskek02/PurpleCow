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
