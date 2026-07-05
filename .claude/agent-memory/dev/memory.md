# Dev Agent Memory

## 2026-06-29

### 작업: Core 시스템 구현 및 더미 파일 삭제

**작업 내용:**
- `Assets/_Project/Scripts/NewMonoBehaviourScript.cs` 및 `.meta` 파일 삭제
- `Assets/_Project/Scripts/Core/` 디렉토리 생성 후 5개 스크립트 생성

**생성 파일:**
- `Singleton.cs` - MonoBehaviour 상속 추상 제네릭 싱글톤 베이스 클래스
- `IPoolable.cs` - OnSpawn/OnDespawn 인터페이스
- `ObjectPool.cs` - 제네릭 오브젝트 풀 (T : MonoBehaviour, IPoolable)
- `GameManager.cs` - Singleton<GameManager> 상속, GameState enum, 이벤트 기반 상태 관리
- `InputHandler.cs` - Singleton<InputHandler> 상속, 마우스 드래그/릴리즈 이벤트

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Core system (Singleton, IPoolable, ObjectPool, GameManager, InputHandler) and remove dummy script`
- push 완료 (new branch)

**주요 결정사항:**
- DontDestroyOnLoad 미사용 (단일 씬 구조, DevRules.md 기준)
- InputHandler는 기본 Input 클래스 사용 (New Input System 미사용)
- GameManager는 WaveManager/UIManager와 직접 연결 없음 (이후 단계에서 연결)
- ObjectPool은 static이 아닌 인스턴스 기반으로 구현 (사용처에서 풀 인스턴스를 직접 관리)
- namespace 없이 작성 (DevRules.md에 명시 없으므로 생략)

---

## 2026-06-29

### 작업: Ball 시스템 구현 (BallData, Ball, BallLauncher, BallSetupEditor)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/10-00_Ball시스템구현/plan.md`
- 4개 파일 신규 생성 (Data, Ball, Editor 폴더 신규 생성 포함)

**생성 파일:**
- `Assets/_Project/Scripts/Data/BallData.cs` — ScriptableObject, damage/speed/criticalChance/criticalMultiplier 프로퍼티
- `Assets/_Project/Scripts/Ball/Ball.cs` — IPoolable 구현, OnHitMonster static event, Tag 분기 충돌 처리, CalculateDamage 치명타 계산
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — Singleton<BallLauncher> 상속, ObjectPool<Ball> 소유, InputHandler/GameManager 이벤트 구독, OnAllBallsReturned static event
- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` — #if UNITY_EDITOR 가드, MenuItem("PurpleCow/Setup/Ball System Setup"), 태그 등록/PhysicsMaterial2D 생성/BallData 에셋 생성 자동화

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Ball system (BallData, Ball, BallLauncher, BallSetupEditor)`
- push 완료 (기존 브랜치에 추가)

**주요 결정사항:**
- InputHandler.OnDrag/OnRelease, GameManager.OnGameStateChanged는 instance event이므로 OnEnable/OnDisable에서 Instance 통해 구독 (plan.md 표기는 축약이었음)
- FixedUpdate에서 velocity.normalized * speed로 속력 유지 (bounciness=1이어도 프레임 간 미세 감쇠 방지)
- BallSetupEditor는 #if UNITY_EDITOR 가드 사용 (Editor 폴더에 위치하므로 이중 보호)
- BallData 기본값은 SerializedObject를 통해 설정 (직접 필드 접근 불가 - private 필드이므로)
- OnEnable에서 Instance 접근 — Singleton Awake보다 OnEnable이 나중이므로 안전
- _activeBallCount 언더플로 방지는 미구현 (plan.md 범위 외)

---

## 2026-06-30

### 작업: Monster 시스템 구현 (MonsterData, MonsterBase, WaveData, WaveManager, MonsterSetupEditor)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/14-00_Monster시스템구현/plan.md`
- Ball.cs 기존 파일 수정 1건 + 신규 파일 5개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Ball/Ball.cs` — `LastDamage { get; private set; }` 프로퍼티 추가, `CalculateDamage()` 내부에서 `LastDamage` 캐싱 및 `OnHitMonster` 발행으로 이동 (OnCollisionEnter2D에서 중복 Invoke 제거)

**생성 파일:**
- `Assets/_Project/Scripts/Data/MonsterData.cs` — ScriptableObject, hp/moveSpeed/damage/reward 읽기 전용 프로퍼티
- `Assets/_Project/Scripts/Data/WaveData.cs` — ScriptableObject, waveNumber/spawnEntries, MonsterSpawnEntry Serializable 중첩 클래스
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — IPoolable 구현, OnMonsterDied static event, OnEnable/OnDisable에서 Ball.OnHitMonster 구독/해제, OnCollisionEnter2D에서 ball.LastDamage로 TakeDamage 호출
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — Singleton<WaveManager> 상속, ObjectPool<MonsterBase> 소유, BallLauncher.OnAllBallsReturned + MonsterBase.OnMonsterDied 구독, SpawnWave/HandleAllBallsReturned/CheckGameOver/CheckWaveCleared/AdvanceToNextWave 구현
- `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` — #if UNITY_EDITOR 가드, MenuItem("PurpleCow/Setup/Monster System Setup"), Monster 태그 확인(중복 방지), MonsterData 4종 에셋 생성(Fluffy/Spike/Blaze/Stone), WaveData_Wave1 에셋 생성

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Monster system (MonsterData, MonsterBase, WaveData, WaveManager, MonsterSetupEditor)`
- push 완료

**주요 결정사항:**
- Ball.cs의 OnHitMonster 발행을 CalculateDamage() 내부로 이동 — OnCollisionEnter2D에서 중복 발행되던 구조 수정
- HandleHitMonster는 plan.md 스펙 준수를 위해 구독 핸들러로 존재하나 실제 데미지 처리는 OnCollisionEnter2D에서 LastDamage 방식으로 처리
- Monster/Wave 디렉토리는 기존에 .meta 파일만 있고 실제 디렉토리가 없어 mkdir으로 생성
- MonsterSpawnEntry는 WaveData 파일 내 별도 클래스로 정의 (중첩 클래스 아님 — Unity Serializable 클래스는 파일 루트 레벨에서도 동작)

---

## 2026-06-30

### 작업: Skill 시스템 구현 (SkillData, BallSkillBase, PassiveSkillBase, Active 5종, Passive 7종, SkillManager, SkillSetupEditor)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/18-00_Skill시스템구현/plan.md`
- 기존 파일 5개 수정 + 신규 파일 17개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Data/BallData.cs` — `_maxBounces` 필드 및 `MaxBounces` 프로퍼티 추가 (BounceUpPassive 연동용)
- `Assets/_Project/Scripts/Ball/Ball.cs` — `_skill`, `_remainingBounces`, `_collider` 필드 추가; `LaunchDirection`/`OnBeforeReturn` 추가; `OnSpawn`/`OnDespawn`/`Launch`/`FixedUpdate`/`CalculateDamage`/`OnCollisionEnter2D` 수정; `SetSkill`/`SetGhostMode`/`ForceReturn`/`OnTriggerEnter2D` 추가
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — `LaunchBall()`에 `SkillManager.Instance.ApplySkillToBall(ball)` 추가; `LaunchSubBalls()` 메서드 추가
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `_frozenTurnsRemaining`/`IsFrozen` 추가; `OnSpawn()`에 초기화; `MoveDown()`에 freeze 체크; `ApplyFreeze()` 추가
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `GetWeakestMonster()` 메서드 추가

**생성 파일:**
- `Assets/_Project/Scripts/Data/SkillData.cs` — ScriptableObject, SkillType/ActiveSkillId/PassiveSkillId 열거형, Value1~3 수치 필드
- `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` — abstract MonoBehaviour, OnBallHit/OnActivate/OnDeactivate
- `Assets/_Project/Scripts/Skill/Base/PassiveSkillBase.cs` — abstract 순수 C# 클래스, Apply/Remove
- `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs` — OverlapCircleAll 폭발 데미지
- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs` — ApplyFreeze 호출
- `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs` — SetGhostMode(true/false) 전환
- `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs` — OnActivate에서 RaycastAll 후 ForceReturn
- `Assets/_Project/Scripts/Skill/Active/ClusterBallSkill.cs` — LaunchSubBalls 호출, _hasExploded 1회 제어
- `Assets/_Project/Scripts/Skill/Passive/DamageUpPassive.cs` — AddDamageMultiplier/Remove
- `Assets/_Project/Scripts/Skill/Passive/CritChanceUpPassive.cs` — AddCritChanceBonus/Remove
- `Assets/_Project/Scripts/Skill/Passive/CritDamageUpPassive.cs` — AddCritDamageBonus/Remove
- `Assets/_Project/Scripts/Skill/Passive/SpeedUpPassive.cs` — AddSpeedBonus/Remove
- `Assets/_Project/Scripts/Skill/Passive/BounceUpPassive.cs` — AddBounceBonus/Remove
- `Assets/_Project/Scripts/Skill/Passive/KillShotPassive.cs` — MonsterBase.OnMonsterDied 구독, LaunchSubBalls(position, 1)
- `Assets/_Project/Scripts/Skill/Passive/LastHitPassive.cs` — Ball.OnBeforeReturn 구독, GetWeakestMonster().TakeDamage
- `Assets/_Project/Scripts/Skill/SkillManager.cs` — Singleton<SkillManager>, 5종 보너스 누적, ApplySkillToBall, Add/Remove 메서드
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs` — MenuItem("PurpleCow/Setup/Skill System Setup"), Active 5종 + Passive 7종 SkillData 에셋 생성

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Skill system (SkillData, BallSkillBase, PassiveSkillBase, 5 active skills, 7 passive skills, SkillManager, SkillSetupEditor)`
- push 완료 (22 files changed, 736 insertions)

**주요 결정사항:**
- `BallData`에 `_maxBounces` 필드 추가 — plan.md 주의사항 5번 기준, BallData SO로 관리
- `Ball.OnCollisionEnter2D` Monster 분기에서 `TakeDamage` 직접 호출을 제거하지 않고 `_skill?.OnBallHit()` 추가 — 기존 `OnCollisionEnter2D → LastDamage → MonsterBase.OnCollisionEnter2D.TakeDamage` 흐름 유지, 충돌 데미지 중복 방지는 기존 구조에 의존
- GhostBallSkill의 OnTriggerEnter2D는 `_skill is GhostBallSkill` 조건으로 Ghost 모드 한정 처리
- SkillSetupEditor에서 `_skillType.enumValueIndex = (int)skillType` 방식으로 enum 값 설정
- PassiveSkillBase는 MonoBehaviour가 아닌 순수 C# 클래스 — OnEnable/OnDisable 없이 Apply/Remove 쌍으로 이벤트 관리

---

## 2026-06-30

### 작업: UI 시스템 구현 (UIManager, HUDPanel, ResultPanel, SkillSelectionPanel, SkillCardUI, SkillFactory)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/20-00_UI시스템구현/plan.md`
- 기존 파일 9개 수정 + 신규 파일 6개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Core/GameManager.cs` — `IsLastGameSuccess { get; private set; }` 프로퍼티 추가, `EndGame(bool)` 내부에서 `IsLastGameSuccess = isSuccess` 저장
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `OnWaveCleared` static event 추가, `TotalWaves` 읽기 전용 프로퍼티 추가, `CheckWaveCleared()` 분기 수정(마지막 웨이브면 OnAllWavesCleared, 아니면 OnWaveCleared), `AdvanceToNextWave()` private → public 전환
- `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` — MonoBehaviour 상속 제거, 순수 C# 클래스로 전환, `_skillData` 생성자 주입 방식으로 변경, `Initialize(Ball ball)` 메서드 추가
- `Assets/_Project/Scripts/Ball/Ball.cs` — `SetSkill()` 내부에서 `_skill?.Initialize(this)` 호출 추가
- `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs` — MonoBehaviour 상속 제거, 생성자 `(SkillData skillData)` 추가
- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs` — 동일
- `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs` — 동일
- `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs` — 동일
- `Assets/_Project/Scripts/Skill/Active/ClusterBallSkill.cs` — 동일

**생성 파일:**
- `Assets/_Project/Scripts/UI/UIManager.cs` — Singleton<UIManager>, HUD/Result/SkillSelection 패널 전환, 점수 관리, OnScoreChanged static event
- `Assets/_Project/Scripts/UI/HUDPanel.cs` — WaveManager.OnWaveStarted/BallLauncher.OnAllBallsReturned/UIManager.OnScoreChanged 구독, TMP_Text 사용
- `Assets/_Project/Scripts/UI/ResultPanel.cs` — GameManager.OnGameStateChanged 구독, IsLastGameSuccess로 성공/실패 표시, RestartGame 연결
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — OnEnable에서 중복 없는 무작위 3장 제시, SkillFactory로 스킬 생성, UIManager.OnSkillSelectionComplete 호출
- `Assets/_Project/Scripts/UI/SkillCardUI.cs` — Setup(SkillData, Action<SkillData>), _currentData 캐싱, TMP_Text/Image/Button 사용
- `Assets/_Project/Scripts/Skill/SkillFactory.cs` — 정적 클래스, CreateActiveSkill/CreatePassiveSkill, SkillId switch 패턴으로 인스턴스 생성

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement UI system (UIManager, HUDPanel, ResultPanel, SkillSelectionPanel, SkillCardUI, SkillFactory)`
- push 완료 (15 files changed, 338 insertions)

**주요 결정사항:**
- BallSkillBase를 순수 C# 클래스로 전환(옵션 A 채택) — SkillFactory에서 new로 직접 생성 가능
- Ball 참조는 생성자가 아닌 `Initialize(Ball)` 메서드로 주입 — 스킬 인스턴스 재사용 시 유연성 확보
- WaveManager.CheckWaveCleared: 마지막 웨이브 여부를 `_currentWaveIndex + 1 >= _waveDatas.Length`로 판단, AdvanceToNextWave는 UIManager가 스킬 선택 완료 후 호출
- UIManager의 _score는 Ready 상태 전환 시 0으로 초기화 (RestartGame 흐름 대응)

---

## 2026-06-30

### 작업: EditorSetup 개선 (SkillSetupEditor 오타 수정 + SceneSetupEditor 신규 생성)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/HH-MM_EditorSetup개선/plan.md`
- 기존 파일 1개 수정 + 신규 파일 1개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs` — 라인 55/67/79/91의 아이콘 경로 소문자 `ball` → 대문자 `Ball` 수정 4곳 (Ball_Ice_Ball, Ball_Ghost_Ball, Ball_Laser_Ball, Ball_Cluster_Ball)

**생성 파일:**
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — MenuItem("PurpleCow/Setup/Scene Setup"), 7단계 자동화 (Ball 프리팹, Monster 4종, Block 4종, Background, Wall/Ground, Manager 6종, BallLauncher 참조 연결)

**SceneSetupEditor 구현 상세:**
- Step 1: Ball.prefab (SpriteRenderer + Rigidbody2D GravityScale=0 Continuous + CircleCollider2D + Ball 스크립트, Tag="Ball")
- Step 2: Monster 4종 (Fluffy/Spider/StoneBug/ForestDeer) — SpriteRenderer + Rigidbody2D Kinematic + BoxCollider2D + MonsterBase, Tag="Monster"
- Step 3: Block 4종 (Block_1x1~2x2) — SpriteRenderer + BoxCollider2D + MonsterBase (Rigidbody2D 없음), Tag="Monster"
- Step 4: Background — SpriteRenderer + Position(0,0,1)
- Step 5: Wall_Left/Wall_Right/Ground — BoxCollider2D + Tag + Position 설정
- Step 6: Manager 6종 — PlaceManager<T> 제네릭 메서드, try-catch 예외 처리
- Step 7: BallLauncher SerializedObject — _ballPrefab/PoolRoot Transform 연결, _launchPoint는 수동 안내 LogWarning
- TrySetTag 유틸리티: try-catch로 미등록 태그 예외 처리

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: fix SkillSetupEditor icon paths and add SceneSetupEditor`
- push 완료 (2 files changed, 331 insertions)

**주요 결정사항:**
- Background Position Z=1로 설정 (렌더 순서 뒤로 — plan.md 지시사항)
- PlaceManager는 제네릭 메서드로 구현하여 6개 Manager를 동일 패턴으로 처리
- BallLauncher._ballPrefab 연결 시 Step 1의 반환값(Ball 컴포넌트)을 직접 활용
- PoolRoot는 씬에서 Find 후 없으면 생성 (중복 실행 시 재사용)
- Block 프리팹에 Rigidbody2D 없음 (plan.md 지시사항 준수)

---

## 2026-06-30

### 작업: PDF 스펙 정합 (SkillData 레벨 시스템, 패시브 전면 교체, Ball/Monster 리팩토링)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/HH-MM_PDF스펙정합/plan.md`
- 수정 파일 25개, 삭제 파일 7개, 신규 파일 5개

**수정 파일:**
- `SkillData.cs` — `SkillLevelData` 구조체 추가, `_value1~3` 제거, `_levels[]` / `_currentLevel` 추가, `PassiveSkillId` 열거형 교체 (7종→5종)
- `BallSetupEditor.cs` — 기본값 변경: damage 10f→8f, criticalChance 0.1f→0f, criticalMultiplier 2f→1.5f
- `BallSkillBase.cs` — `OnBallHit(MonsterBase, float)` → `OnBallHit(MonsterBase)` 시그니처 변경, `LevelData` 편의 프로퍼티 추가
- `PassiveSkillBase.cs` — `LevelData` 편의 프로퍼티 추가
- `MonsterBase.cs` — `_slowTurnsRemaining`/`_slowPercent`/`_bonusCritChance` 필드 추가, `ApplySlow`/`ApplyBonusCritChance`/`ConsumeBonusCritChance`/`ApplyDot`/`ApplyFreeze(float)` 추가, `MoveDown`에 슬로우 반영, `OnCollisionEnter2D` 중복 데미지 처리 제거
- `Ball.cs` — `_skill` 단일→`_skills` 리스트 전환, `OnWallHit`/`OnHitMonsterFront`/`OnHitMonsterBack` static 이벤트 추가, `OnBeforeReturn` 제거, `SetSkill`→`AddSkill` 전환, `SetSubBallDamage` 추가, `CalculateDamage` 리팩토링(ConsumeBonusCritChance/ConsumeNextShotDamageBonus 통합), SpeedBonus/BounceBonus 참조 제거, 전면/후면 판정 이벤트 발행
- `BallLauncher.cs` — `LaunchSubBalls(Vector2, int)` → `(Vector2, int, float damage=0f)` 시그니처 확장
- `SkillManager.cs` — `_equippedActiveSkill` 단일→`_activeSkills` 리스트, 최대 4/2 제한 강화, `_critChanceBonus` 등 제거, `_nextShotDamageBonus` 추가, `ActiveSkillIds`/`PassiveSkillIds` 프로퍼티 추가
- `WaveManager.cs` — `_killCountForSkill`/`_totalKillCount` 추가, `CheckSkillUnlock`/`GetMonstersInRow` 추가, `OnKillCountReached` static 이벤트 추가
- `SkillSelectionPanel.cs` — `WaveManager.OnKillCountReached` 구독/해제, `OpenPanel` 추가, `BuildSkillCardPool` 로직(레벨업/신규 분기), LINQ 사용
- `SkillFactory.cs` — 패시브 switch 7종→5종 교체
- `SkillSetupEditor.cs` — `CreateSkillData` 헬퍼를 `SkillLevelData[]` 배열 기반으로 전면 수정, Active 5종 레벨 수치 반영, Passive 5종으로 교체
- `MonsterSetupEditor.cs` — 몬스터 이름 배열 수정 (Spike→Spider, Blaze→StoneBug, Stone→ForestDeer)
- Active 스킬 5종 — `OnBallHit(MonsterBase target)` 시그니처 적용 및 PDF 스펙 로직 재구현

**삭제 파일 (패시브 7종):**
- `DamageUpPassive.cs`, `CritChanceUpPassive.cs`, `CritDamageUpPassive.cs`, `SpeedUpPassive.cs`, `BounceUpPassive.cs`, `KillShotPassive.cs`, `LastHitPassive.cs`

**신규 파일 (패시브 5종):**
- `WarmTinHeartPassive.cs`, `MagicMirrorPassive.cs`, `AmethystDaggerPassive.cs`, `EmeraldDaggerPassive.cs`, `LastMatchPassive.cs`

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: PDF spec alignment - skill level system, new passives, ball/monster refactor`
- push 완료 (30 files changed, 537 insertions, 409 deletions)

**주요 결정사항:**
- Ball.cs의 `CalculateDamage`를 `target` 파라미터를 받도록 변경 — MonsterBase.ConsumeBonusCritChance 호출 및 target.TakeDamage 직접 처리
- MonsterBase.OnCollisionEnter2D 중복 데미지 제거 — Ball.CalculateDamage에서 이미 TakeDamage 호출하므로 MonsterBase의 충돌 핸들러는 제거
- BallLauncher.LaunchSubBalls에 `damage` 기본값 파라미터 추가 — 기존 KillShotPassive 방식 호출도 `damage=0f` 기본값으로 하위 호환
- Ball.OnSpawn에서 `_skills.Clear()` 처리 — 풀 재사용 시 이전 스킬 목록 초기화
- SkillSelectionPanel에서 `_cards` → `_skillCards` 필드명 변경 (plan.md 스펙 기준)

---

## 2026-06-30

### 작업: UI 전체 재작업 (CanvasGroup, DOTween, CharacterManager, DamageText, MonsterHpBar)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/HH-MM_UI재작업/plan.md`
- 기존 파일 6개 수정 + 신규 파일 9개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `Data => _monsterData` 프로퍼티 추가, `OnHpChanged` 인스턴스 이벤트 추가, `OnSpawn()`/`TakeDamage()`에 `OnHpChanged?.Invoke()` 발행
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `OnMonsterReachedBottom` static 이벤트 추가, `CheckGameOver()` 재작성 (역방향 순회, 몬스터 풀 반납 후 이벤트 발행, GameManager.EndGame 직접 호출 제거)
- `Assets/_Project/Scripts/UI/UIManager.cs` — Awake의 SetActive(false) 3개 → Hide() 호출로 교체, `WaveManager.OnWaveCleared` 구독/해제 및 `HandleWaveCleared()` 제거, ShowHUD/Result/SkillSelection을 Show()/Hide() 방식으로 교체
- `Assets/_Project/Scripts/UI/HUDPanel.cs` — CanvasGroup/slideDist/animDuration/ease 필드 추가, Awake() 추가(_originalPos 저장), `_launchReadyCanvasGroup` 추가(null 체크 fallback), Show()/Hide() DOTween 애니메이션 추가
- `Assets/_Project/Scripts/UI/ResultPanel.cs` — 동일 패턴으로 CanvasGroup + Show()/Hide() 추가
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — CanvasGroup + Show()/Hide() 추가, OnEnable에서 ShowRandomSkills() 직접 호출 제거, OpenPanel()에서 Show() 호출, OnSkillSelected에서 Hide() 호출, _skillCards[i].gameObject.SetActive() → SetVisible() 교체
- `Assets/_Project/Scripts/UI/SkillCardUI.cs` — `_canvasGroup` 필드 추가, `SetVisible(bool)` 메서드 추가

**생성 파일:**
- `Assets/_Project/Scripts/UI/UIButton.cs` — IPointerDownHandler/IPointerUpHandler, DOTween scale 애니메이션
- `Assets/_Project/Scripts/UI/SafeAreaFitter.cs` — Screen.safeArea 기반 RectTransform offsetMin/offsetMax 적용
- `Assets/_Project/Scripts/Core/CharacterManager.cs` — Singleton<CharacterManager>, OnHpChanged/OnXpChanged/OnLevelUp static 이벤트, WaveManager.OnMonsterReachedBottom/MonsterBase.OnMonsterDied 구독, TakeDamage/AddXp/레벨업 로직
- `Assets/_Project/Scripts/UI/MonsterHpBar.cs` — MonsterBase.OnHpChanged 인스턴스 이벤트 구독, Slider 제어
- `Assets/_Project/Scripts/UI/CharacterHpBar.cs` — CharacterManager.OnHpChanged 구독, Slider 제어
- `Assets/_Project/Scripts/UI/CharacterXpBar.cs` — CharacterManager.OnXpChanged/OnLevelUp 구독, Slider + TMP_Text 레벨 표시
- `Assets/_Project/Scripts/UI/DamageTextFx.cs` — IPoolable 구현, DOTween float + DOFade 애니메이션, Play(worldPos, damage, isCritical)
- `Assets/_Project/Scripts/UI/DamageTextManager.cs` — Singleton<DamageTextManager>, ObjectPool<DamageTextFx>, ShowDamage/Return 메서드

**주요 결정사항:**
- WaveManager.CheckGameOver() 역방향 for 순회: List 수정 중 순방향 foreach 사용 불가, RemoveAt(i)로 안전하게 처리
- OnMonsterReachedBottom 발행 후 GameManager.EndGame 호출 제거 — CharacterManager가 HP 차감 후 0이 되면 EndGame 호출하는 구조로 분리
- HUDPanel._launchReadyIndicator: null 체크 없이 기존 SetActive 방식과 새 CanvasGroup 방식을 SetLaunchIndicatorVisible() 헬퍼로 통합
- MonsterHpBar는 Start()에서 GetComponentInParent로 MonsterBase 참조 (OnEnable이 아닌 Start에서 처리 — 풀 스폰 시점 순서 고려)

---

## 2026-06-30

### 작업: QA 수정 10건 구현 (CRITICAL 2·3·4·5, WARNING 2·3·4·6, INFO 2·3)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/HH-MM_QA수정/plan.md`
- 기존 파일 11개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `ApplyData(MonsterData data)` 메서드 추가 (STEP1), `OnEnable`/`OnDisable`의 `Ball.OnHitMonster` 구독/해제 라인 제거, 빈 `HandleHitMonster()` 메서드 제거 (STEP5)
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `SpawnWave()` 내 `_monsterPool.Get()` 직후에 `entry.Data != null` 조건부 `monster.ApplyData(entry.Data)` 호출 추가 (STEP1)
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — `OnEnable`에 `GameManager.OnGameStateChanged += HandleGameStateChanged` 추가, `HandleGameStateChanged`에서 Ready 시 `_allSkillDatas` 전체 `ResetLevel()` (STEP3/STEP7), `OpenPanel()`에 `Time.timeScale = 0f` 추가 (STEP2), Show()/Hide() Sequence에 `.SetUpdate(true)` 추가 (STEP2), `OnSkillSelected()`에서 직접 `Hide()` 호출 제거 (STEP6)
- `Assets/_Project/Scripts/UI/UIManager.cs` — `OnEnable`/`OnDisable`에서 `GameManager.Instance.OnGameStateChanged` → `GameManager.OnGameStateChanged` static 이벤트 직접 참조로 교체 (STEP7), `OnSkillSelectionComplete()`에 `Time.timeScale = 1f` 추가 (STEP2)
- `Assets/_Project/Scripts/Data/SkillData.cs` — `ResetLevel()` 메서드 추가 (STEP3)
- `Assets/_Project/Scripts/Core/GameManager.cs` — `using UnityEngine.SceneManagement` 추가, `OnGameStateChanged`를 `public static event`로 변경 (STEP7), `RestartGame()`에 `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)` 추가 (STEP3)
- `Assets/_Project/Scripts/Skill/SkillManager.cs` — `AddPassiveSkill()` 레벨업 분기에 `existing.Remove()` 추가 (STEP4), `ApplySkillToBall()`에서 기존 인스턴스 재사용 → `SkillFactory.CreateActiveSkill(skill.SkillData)` 새 인스턴스 생성으로 변경 (STEP10)
- `Assets/_Project/Scripts/Core/InputHandler.cs` — `OnDrag`, `OnRelease`를 `public static event`로 변경 (STEP7)
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — `OnEnable`/`OnDisable`에서 `InputHandler.Instance.OnDrag` → `InputHandler.OnDrag`, `GameManager.Instance.OnGameStateChanged` → `GameManager.OnGameStateChanged` 교체 (STEP7)
- `Assets/_Project/Scripts/UI/ResultPanel.cs` — `OnEnable`/`OnDisable`에서 `GameManager.Instance.OnGameStateChanged` → `GameManager.OnGameStateChanged` 교체 (STEP7)
- `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` — `CreateWaveDataAsset()` → `CreateWaveDataAssets()`로 교체, Wave1~Wave20 에셋 생성 루프, `_waveNumber` SerializedObject로 설정 (STEP8)
- `Assets/_Project/Scripts/Ball/Ball.cs` — `OnHitMonster` 시그니처 `Action<float, bool>` → `Action<MonsterBase, float, bool>` 변경, `Invoke`에 `target` 추가 (STEP9)
- `Assets/_Project/Scripts/UI/DamageTextManager.cs` — `OnEnable`/`OnDisable` 추가, `HandleHitMonster(MonsterBase, float, bool)` 메서드 추가로 `ShowDamage(monster.transform.position, ...)` 호출 (STEP9)

**주요 결정사항:**
- `GameManager.OnGameStateChanged` / `InputHandler.OnDrag` / `InputHandler.OnRelease` 모두 static event로 전환 — Instance 접근 없이 직접 참조 가능해져 OnEnable NullReferenceException 근본 해결
- `SkillSelectionPanel.OnEnable()` 내에서 static 이벤트 구독 — SkillSelectionPanel이 항상 active 상태이므로 구독 유지됨 (UI재작업에서 CanvasGroup 방식으로 전환 완료)
- `ApplyData()` 호출 순서: `OnSpawn()` 이후 → `OnSpawn()`의 기본 MonsterData 초기화를 덮어쓰는 방식
- `ApplySkillToBall()`에서 각 Ball마다 새 인스턴스 생성 — GhostBallSkill 등 `_ball` 참조 충돌 완전 해소
- `CreateWaveDataAssets()`에서 이미 존재하는 에셋은 스킵 — 멱등성 보장

---

## 2026-06-30

### 작업: UI/Scene 에디터 자동화 (UISetupEditor 신규 생성, SceneSetupEditor 수정)

**작업 내용:**
- 신규 파일 1개 생성 + 기존 파일 1개 수정

**생성 파일:**
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs` — MenuItem("PurpleCow/Setup/UI Setup"), 6단계 자동화

**UISetupEditor 구현 상세:**
- Step 1: Canvas 3개 생성 (Canvas_HUD Sort=10 / Canvas_Panel Sort=20 / Canvas_Popup Sort=30), ScreenSpaceOverlay, CanvasScaler 1080x1920 ScaleWithScreenSize
- Step 2: Canvas_HUD 내부 구성 — SafeAreaPanel(SafeAreaFitter), HUDPanel, ResultPanel, SkillSelectionPanel, CharacterHP(Slider+CharacterHpBar), CharacterXP(Slider+CharacterXpBar+LevelText TMP)
- Step 3: Canvas_Panel 내부 구성 — LevelUpPanel/PausePanel/BallLevelUpPanel/PrismPanel (빈 패널 + CanvasGroup)
- Step 4: DamageTextManager(+DamageTextPool 자식) / CharacterManager 씬 배치
- Step 5: UIManager SerializedObject를 통해 _hudPanel/_resultPanel/_skillSelectionPanel 참조 자동 연결
- Step 6: SkillCard.prefab 생성 (Icon Image + NameText/DescText/TypeText TMP + SelectButton UIButton + SkillCardUI 컴포넌트)

**수정 파일:**
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `using UnityEngine.UI` 추가 (Slider/Canvas/GraphicRaycaster 사용)
  - `AddMonsterHpBar(GameObject go)` 메서드 추가 — World Space Canvas(HpBarCanvas) + Slider(HpSlider) + MonsterHpBar 컴포넌트, SerializedObject로 _slider 자동 연결
  - Step2_CreateMonsterPrefabs에서 `PrefabUtility.SaveAsPrefabAsset` 직전에 `AddMonsterHpBar(go)` 호출 삽입
  - `UpdateMonsterHpBars()` 메뉴 항목 추가 (`PurpleCow/Setup/Update Monster HpBar`) — 기존 프리팹에 HpBar 소급 적용, `PrefabUtility.EditPrefabContentsScope` 사용
  - Step6_PlaceManagers에 `PlaceManager<CharacterManager>("CharacterManager")` 및 `PlaceManager<DamageTextManager>("DamageTextManager")` 추가

**주요 결정사항:**
- UISetupEditor의 EnsureComponent<T>는 try-catch로 실패 방어 처리 (일부 UI 컴포넌트 AddComponent 순서 의존성 대응)
- AddMonsterHpBar는 `go.transform.Find("HpBarCanvas") != null` 중복 체크로 멱등성 보장
- UpdateMonsterHpBars는 EditPrefabContentsScope 패턴 사용 — 프리팹 파일 직접 수정 후 저장

---

## 2026-06-30

### 작업: BallSetupEditor _maxBounces 기본값 추가 + SceneSetupEditor DamageTextManager 제거

**작업 내용:**
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` — `CreateBallDataAsset()` 내 `_criticalMultiplier` 설정 바로 아래에 `so.FindProperty("_maxBounces").intValue = 10;` 1줄 추가
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step6_PlaceManagers()` 내 `PlaceManager<DamageTextManager>("DamageTextManager");` 라인 제거 (UISetupEditor가 전담)

**주요 결정사항:**
- DamageTextManager 배치 책임이 UISetupEditor로 이전되어 SceneSetupEditor에서 중복 생성 방지

---

## 2026-06-30

### 작업: 에디터 스크립트 참조 연결 자동화 (SceneSetupEditor Step8/9, MonsterSetupEditor ConnectMonsterDataToPrefabs, UISetupEditor Step7/8)

**작업 내용:**
- 기존 파일 3개 수정 (신규 파일 없음)
- Inspector 수동 드래그&드롭 4건을 에디터 스크립트로 자동화

**수정 파일:**

1. `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
   - 변경사항 A: SetupScene()에 `Step8_ConnectBallPrefabRefs()` / `Step9_ConnectWaveManagerRefs()` 호출 추가
   - 변경사항 B: Step1_CreateBallPrefab()의 PhysicsMaterial2D 수동 연결 경고 LogWarning 삭제 (Step8에서 자동 처리)
   - 변경사항 C: UpdateMonsterHpBars() 바로 뒤에 Step8/Step9 메서드 추가
     - Step8: Ball.prefab 열어 _ballData(BallData.asset) + CircleCollider2D m_Material(BallBounce.physicsMaterial2D) 연결
     - Step9: WaveManager 씬 오브젝트에 _waveDatas 배열(20개) + _monsterPrefab(Fluffy) + _poolParent/_spawnRoot(PoolRoot) 연결

2. `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`
   - 변경사항 A: SetupMonsterSystem()에 `ConnectMonsterDataToPrefabs()` 호출 추가
   - 변경사항 B: EnsureDataFolder() 뒤에 `ConnectMonsterDataToPrefabs()` 메서드 추가
     - Fluffy/Spider/StoneBug/ForestDeer 프리팹 각각 열어 MonsterBase._monsterData에 MonsterData 에셋 연결

3. `Assets/_Project/Scripts/Editor/UISetupEditor.cs`
   - 변경사항 A: SetupUI()에 `Step7_CreateDamageTextFxPrefab()` / `Step8_ConnectDamageTextManagerRefs()` 호출 추가
   - 변경사항 B: Step4_SetupManagers()의 DamageTextManager 생성 블록에서 _poolParent 즉시 연결 (SerializedObject 방식)
   - 변경사항 C: Step6_CreateSkillCardPrefab() 뒤에 Step7/Step8 메서드 추가
     - Step7: DamageTextFx.prefab 생성 (World Space TextMeshPro 자식 + DamageTextFx 컴포넌트 + _text 자동 연결)
     - Step8: DamageTextManager 씬 오브젝트에 _prefab(DamageTextFx.prefab) + _poolParent(DamageTextPool) 연결

**주요 결정사항:**
- Step8_ConnectBallPrefabRefs는 MenuItem으로도 노출 (단독 실행 가능)
- Step9_ConnectWaveManagerRefs는 씬 오브젝트 참조이므로 MenuItem 없이 SetupScene 플로우에만 포함
- UISetupEditor Step7은 TextMeshPro(3D World Space)를 사용 — 기존 `using TMPro;` 재사용, 별도 using 불필요
- DamageTextFx.prefab 생성 시 _text 참조를 SerializedObject로 즉시 연결하여 수동 작업 완전 제거

---

## 2026-07-01

### 작업: UI HUD Gap Fill (캐릭터 HP바 숫자, 스킬 카드 데미지, Active/Passive 슬롯 UI, 스테이지 진행률 %)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-01/18-41_ui-hud-gap-fill/plan.md` (사용자 승인 문서, 그대로 구현)
- 기존 파일 6개 수정 + 신규 파일 2개 생성 (SkillSlot.prefab은 UISetupEditor 실행 시 자동 생성 예정, 아직 미실행)

**수정 파일:**
- `Assets/_Project/Scripts/UI/CharacterHpBar.cs` — `using TMPro;` 추가, `[SerializeField] private TMP_Text _hpText;` 추가, `UpdateHp()`에서 `_hpText.text = $"{current} / {max}"` 갱신
- `Assets/_Project/Scripts/UI/SkillCardUI.cs` — `[SerializeField] private TMP_Text _damageText;` 추가, `Setup()` 마지막에 `SkillType.Active`일 때만 `_damageText` 활성화 + `data.CurrentLevelData.BallDamage.ToString("0")` 표시, Passive는 `SetActive(false)`
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — `_activeSlotGroup`/`_passiveSlotGroup`(SkillSlotGroup) 필드 추가, `OnEnable`/`OnDisable`에 `SkillManager.OnActiveSkillsChanged`/`OnPassiveSkillsChanged` 구독/해제 추가, `HandleActiveSkillsChanged`/`HandlePassiveSkillsChanged` 핸들러 추가, `OpenPanel()`에서 `RefreshSlotGroups()` 호출 추가(패널 열릴 때 `SkillManager.EquippedActiveSkills`/`EquippedPassiveSkills`로 최초 1회 슬롯 갱신)
- `Assets/_Project/Scripts/Skill/SkillManager.cs` — `public IReadOnlyList<BallSkillBase> EquippedActiveSkills => _activeSkills;`, `public IReadOnlyList<PassiveSkillBase> EquippedPassiveSkills => _passiveSkills;` 프로퍼티 추가 (plan.md 주의사항 2번 열린 이슈 해결 — 패널 최초 오픈 시 아이콘 포함 리스트 즉시 조회용)
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `private int _currentWaveTotalCount;` 필드 추가, `public static event Action<int, int> OnMonsterCountChanged;` 추가, `SpawnWave()`(스폰 직후 `_currentWaveTotalCount` 세팅 후 발행)/`HandleMonsterDied()`(`_totalKillCount++` 이후 발행)/`CheckGameOver()`(바닥 통과 제거 직후 발행) 3곳에서 `OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount)` 호출
- `Assets/_Project/Scripts/UI/HUDPanel.cs` — `[SerializeField] private TMP_Text _progressText;` 추가, `OnEnable`/`OnDisable`에 `WaveManager.OnMonsterCountChanged` 구독/해제 추가, `HandleMonsterCountChanged(int remaining, int total)` 추가해 `{(total-remaining)/total*100}%` 텍스트 갱신
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs` — `Step2_SetupHUDCanvas()`에 `CharacterHP` 하위 `HpText` TMP 생성/연결, `Step6_CreateSkillCardPrefab()`에 `DamageText` TMP 생성(신규+기존 프리팹 보정) 및 `_damageText` 연결, `Step9_SetupHUDPanelContent()`에 `ProgressText` TMP 생성/`_progressText` 연결, 신규 `Step12_CreateSkillSlotPrefab()`(SkillSlot.prefab 생성 — Filled/Icon+LevelText, Empty(검은 Image), `SkillSlotIcon` 연결) + 신규 `Step13_SetupSkillSlotGroups()`(SkillSelectionPanel 하위에 ActiveSkillGroup(4칸)/PassiveSkillGroup(2칸) + Label 생성, SkillSlot.prefab 인스턴스화, `_activeSlotGroup`/`_passiveSlotGroup` 연결) 추가, `SetupUI()` 실행 순서에 `Step12`/`Step13` 추가

**신규 생성 파일:**
- `Assets/_Project/Scripts/UI/SkillSlotIcon.cs` — 슬롯 1칸, `_iconImage`/`_levelText`/`_filledRoot`/`_emptyRoot` 필드, `SetFilled(Sprite, int level)`/`SetEmpty()` 메서드
- `Assets/_Project/Scripts/UI/SkillSlotGroup.cs` — `_slots` 배열, `UpdateActiveSlots(List<BallSkillBase>)`/`UpdatePassiveSlots(List<PassiveSkillBase>)` — 두 메서드 모두 `CurrentLevel + 1`로 1-based 레벨(`x1`/`x2`/`x3`) 배지 표시

**주요 결정사항:**
- plan.md 주의사항 2번(열린 이슈) 해결: `SkillManager`에 `EquippedActiveSkills`/`EquippedPassiveSkills`(`IReadOnlyList<T>`, 내부 리스트 그대로 노출) 프로퍼티를 신규 추가하고, `SkillSelectionPanel.OpenPanel()`에서 `RefreshSlotGroups()`를 호출해 패널이 열릴 때 이미 장착된 스킬을 아이콘까지 즉시 반영하도록 구현. 기존 이벤트 기반 갱신(`OnActiveSkillsChanged`/`OnPassiveSkillsChanged`)은 그대로 유지
- Passive 슬롯도 Active와 동일하게 `CurrentLevel + 1` 레벨 배지 표시 (plan.md 확정 사항 그대로 반영)
- `SkillCardUI._damageText`는 Passive일 때 `gameObject.SetActive(false)`로 완전히 숨김 (텍스트만 비우는 방식이 아님)
- `WaveManager.OnMonsterCountChanged`는 `HandleMonsterDied`/`CheckGameOver`/`SpawnWave` 3곳에서 발행 — 같은 프레임에 여러 번 발행될 수 있음(plan.md 주의사항 4번 인지, 별도 디바운스 처리는 하지 않음, 성능 규칙 위배 아님)
- `UISetupEditor.cs`는 코드만 수정 완료, `[MenuItem("PurpleCow/Setup/UI Setup")]`은 Unity 에디터 GUI 환경이 아니므로 실행하지 않음 — 사용자가 Unity 에디터에서 직접 실행 필요
- `UIRules.md`의 "몬스터 HP바" 문구 수정(TopBar 항목)은 문서 작업이므로 dev 에이전트 범위에서 제외(docs 에이전트 담당)

## 2026-07-02

### 작업: 몬스터 시간 기반 하강 구현 (plan.md 5단계, ball-launch-mechanics task)

**근거 문서:** `Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/plan.md` "5단계 — 몬스터 시간 기반 하강 구현" (1~4단계는 다른 에이전트가 동시 작업 중이라 범위 제외)

**변경 파일:**
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `OnEnable`/`OnDisable`에서 `BallLauncher.OnAllBallsReturned` 구독 완전 제거, `HandleAllBallsReturned()`/`MoveAllMonstersDown()` 메서드 삭제, 사용처가 사라진 `_monsterMoveDistance` 필드 삭제. 신규 `Update()`를 추가해 매 프레임 `CheckGameOver()` 호출(MonsterBase 이벤트 방식 대신 단순한 쪽 선택)
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — 턴 기반 `_frozenTurnsRemaining`(int)/`_slowTurnsRemaining`(int) → 시간 기반 `_frozenSecondsRemaining`(float)/`_slowSecondsRemaining`(float)로 전환. `IsFrozen`도 float 비교로 수정. `ApplyFreeze(int turns)` 오버로드 제거하고 `ApplyFreeze(float seconds)` 단일 시그니처로 통합, `ApplySlow(int turns, float percent)` → `ApplySlow(float seconds, float percent)`로 변경. 턴 호출 기반 `MoveDown(float distance)` 메서드를 제거하고 신규 `Update()`(물리 아님, 단순 위치 이동이라 FixedUpdate 대신 Update 선택)를 추가해 매 프레임 `_monsterData.MoveSpeed * Time.deltaTime`만큼 연속 하강, freeze 중엔 스킵, slow 중엔 속도에 `(1f - _slowPercent)` 적용
- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs` — `ApplySlow(Mathf.RoundToInt(LevelData.Value2), LevelData.Value3)`의 반올림 제거하고 `ApplySlow(LevelData.Value2, LevelData.Value3)`로 변경(초 단위 float 그대로 전달)
- `Assets/_Project/Scripts/UI/HUDPanel.cs` — `BallLauncher.OnAllBallsReturned` 구독을 `GameManager.OnGameStateChanged` 구독으로 교체, `HandleAllBallsReturned()` → `HandleGameStateChanged(GameManager.GameState state)`(Playing이면 표시, 그 외 숨김)로 교체. `Start()`의 하드코딩된 `SetLaunchIndicatorVisible(false)`도 `GameManager.Instance.CurrentState == Playing` 조건으로 교체(스크립트 실행 순서 무관하게 초기 상태 정확히 반영)

**주요 결정사항:**
- `CheckGameOver()` 매 프레임 호출 방식(`WaveManager.Update()`)을 선택. `MonsterBase`가 바닥 도달을 스스로 감지해 이벤트 발행하는 방식 대신 더 단순한 쪽으로 판단(DevRules.md 단순함 우선 원칙)
- `MonsterBase.Update()`는 물리(Rigidbody) 없이 순수 `transform.position` 이동이므로 DevRules.md의 "물리 관련 처리는 FixedUpdate() 사용" 규칙 대상이 아니라고 판단해 `Update()` 채택(Monster 스크립트에 Rigidbody2D 없음을 Grep으로 확인)
- `BallLauncher.OnAllBallsReturned` 이벤트 선언/발행부(`BallLauncher.cs`) 자체는 plan.md 지시대로 손대지 않음 — 구독부만 정리
- 사용처가 사라진 `_monsterMoveDistance`(WaveManager) 필드는 dead code이므로 함께 제거. `Assets/Scenes/SampleScene.unity`에는 해당 필드의 직렬화 값이 남아있으나 Unity가 알 수 없는 필드를 무시하므로 컴파일/런타임 오류 없음(별도 조치 안 함)
- `Ball.cs`/`BallLauncher.cs`/`InputHandler.cs`/`SkillManager.cs`/`SkillSelectionPanel.cs`는 다른 에이전트 작업 범위이므로 전혀 수정하지 않음

---

## 2026-07-02

### 작업: 볼 발사 메커닉 재설계 1~3단계 (조준 이벤트 체계 / 볼 로스터 데이터 모델 / 귀환·재발사 사이클)

**근거 문서:** `Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/plan.md` 1~3단계만 (4단계 궤적 프리뷰, 5단계 몬스터 하강은 이번 범위 제외, 5단계는 다른 에이전트가 동시 작업 완료함)

**변경 파일:**
- `Assets/_Project/Scripts/Core/InputHandler.cs` — `public static event Action OnAimBegin;` 신설(파라미터 없음 — 현재 스코프에서 소비하는 곳이 없어 단순함 우선 원칙에 따라 최소 형태로 결정), `Update()`의 `pressedPos.HasValue` 분기(터치/클릭 시작 프레임)에서 1회 발행
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — 전면 재작성. `private class BallRosterEntry { SkillData SkillData; Ball Ball; }`(SkillData가 null이면 노말볼) 도입, `List<BallRosterEntry> _roster` 신설. `Start()`에서 `InitializeRoster()`로 노말볼 5개(`_normalBallCount` 필드, 기본값 5)를 기본 방향 `Vector2.up`(필드 초기값)으로 게임 시작 즉시 자동 발사. `HandleRelease()`/`LaunchBall()`/`_canLaunch`/`GameManager.OnGameStateChanged` 구독 완전 제거(더 이상 릴리즈가 발사를 트리거하지 않으므로), `InputHandler.OnDrag` 구독만 유지. `LaunchRosterEntry()`(최초 합류 시 위치 세팅+발사+스킬 부착+`_activeBallCount++`), `AddBallToRoster(SkillData)`(신규 특수볼 타입 획득 시 로스터에 볼 1개 추가 후 즉시 발사), `RelaunchBall(Ball)`(귀환 후 재발사 — `PrepareForRelaunch()`+`Launch(_launchDirection)`+스킬 재부착, `_activeBallCount` 변경 없음), `IsRosterMember(Ball)`(로스터 소속 여부 조회, `Ball.cs`가 Ground 충돌 시 귀환 사이클 대상인지 판단하는 데 사용) 신규 공개. `LaunchPoint`/`LaunchDirection` 읽기 전용 프로퍼티 추가. `OnAllBallsReturned`/`ReturnBall()`/`LaunchSubBalls()`는 기존 그대로 유지(진짜 풀 반환이 필요한 경로용, 클러스터 서브볼 등)
- `Assets/_Project/Scripts/Ball/Ball.cs` — `OnCollisionEnter2D`의 `"Ground"` 분기를 `BallLauncher.Instance.IsRosterMember(this)`로 분기: 로스터 소속 볼만 신규 `ReturnToLaunchPoint()`(속도 방향을 `LaunchPoint` 쪽으로 강제 재설정, 위치는 그대로 — 순간이동 아님) 호출, 로스터 밖의 볼(클러스터 서브볼 등)은 기존과 동일하게 즉시 `ReturnToPool()`. `"Wall"` 분기는 plan.md 확정사항대로 전혀 변경하지 않음. `FixedUpdate()`에 `_isReturning` 상태일 때 `LaunchPoint`까지의 거리를 매 프레임 체크해 도달 시(`RETURN_ARRIVAL_DISTANCE = 0.3f`) `BallLauncher.Instance.RelaunchBall(this)` 호출하는 분기 추가(기존 speed-normalize 라인은 그대로 유지). 신규 `PrepareForRelaunch()`(반사 횟수/서브볼 데미지 초기화 + 장착 스킬 `OnDeactivate()` 후 `Clear()` — `OnSpawn()`과 달리 풀을 거치지 않는 재발사 경로 전용) 공개 메서드 추가, `OnSpawn()`/`OnDespawn()`에 `_isReturning = false` 리셋 추가
- `Assets/_Project/Scripts/Skill/SkillManager.cs` — `EquipActiveSkill(BallSkillBase)` 반환형을 `void`→`bool`로 변경(신규 장착이면 true, 레벨업/슬롯 초과면 false). 기존 레벨업 분기(`OnActiveSkillsChanged` 미발행)는 그대로 유지, 반환값만 추가
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — `ApplySkill()`에서 `EquipActiveSkill()`의 반환값을 받아 `true`(신규 타입)일 때만 `BallLauncher.Instance.AddBallToRoster(data)` 호출하도록 연동. 기존 타입 재선택(레벨업)은 로스터 변경 없음 — 로스터 항목이 동일 `SkillData` 에셋 참조를 그대로 들고 있어 재발사 시 `SkillFactory.CreateActiveSkill(entry.SkillData)`가 자동으로 최신 `CurrentLevel`을 반영

**주요 결정사항:**
- **볼 타입 열거형(`BallType`) 신설 안 함** — 로스터 항목의 "타입 정체성"을 별도 enum 대신 기존 `SkillData` 참조(특수볼은 해당 에셋, 노말볼은 `null`)로 표현. `BallType`↔`ActiveSkillId` 간 변환 코드를 추가로 만들 필요가 없고 기존 `SkillFactory.CreateActiveSkill(SkillData)` 구조를 그대로 재사용 가능해 DevRules.md "단순함 우선"에 더 부합한다고 판단
- **로스터 데이터 구조**: `BallLauncher` 내부 `private class BallRosterEntry`(중첩 클래스, 2필드)로 최소 구현. `Ball` 자체에는 타입/레벨 필드를 추가하지 않음(정체성은 로스터가, `Ball`은 상태만 담당하는 plan.md의 역할 분담 그대로 채택)
- **로스터 밖 볼(서브볼) 구분**: `Ball.OnCollisionEnter2D`의 `"Ground"` 분기에서 `BallLauncher.IsRosterMember(this)`로 분기해, `ClusterBallSkill.LaunchSubBalls()`로 생성된 서브볼은 기존과 동일하게 Ground 충돌 시 즉시 풀 반환되도록 유지 — 이번 재설계가 범위 밖인 서브볼 발사 경로의 동작을 바꾸지 않기 위한 선택
- **귀환 방향 재설정은 Ground 충돌 시점 1회만**: 매 프레임 방향을 강제로 재보정하지 않고, Ground 충돌 순간에만 속도 방향을 `LaunchPoint`로 재설정한 뒤 기존 FixedUpdate의 속력 유지 로직에 맡김(plan.md 문구 "그 시점에... 재설정한다"를 문자 그대로 1회성으로 해석). 이후 Wall 충돌이 재차 발생하면 방향이 다시 바뀔 수 있으나 plan.md가 Wall 분기를 "기존처럼" 그대로 유지하라고 확정했으므로 별도 방어 로직을 추가하지 않음(잠재적 엣지케이스로 보고만 함)
- **`_activeBallCount` 증감 규칙 재정의**: 로스터 볼은 최초 `LaunchRosterEntry()` 1회만 카운트 증가시키고, 귀환→재발사(`RelaunchBall`)는 볼이 계속 "활성" 상태였으므로 카운트를 건드리지 않음(풀 반환 없이 사이클 반복) — 이 때문에 `OnAllBallsReturned`는 로스터 도입 후 정상 플레이 중에는 사실상 발생하지 않게 됨(research.md에서 이미 예견된 부작용이며, 5단계 작업(다른 에이전트)이 이를 이미 반영해 `WaveManager`/`HUDPanel` 구독을 제거함)
- **`_canLaunch`/`GameManager.OnGameStateChanged` 구독 제거**: 발사가 더 이상 릴리즈에 종속되지 않고 로스터 초기화(`Start()`)/귀환 시점에 자동으로 일어나므로, 게임 상태 게이팅이 이번 스코프 요구사항에 없어 죽은 필드로 남기지 않고 제거
- **게임 시작 즉시 자동 발사**는 `GameManager`의 상태 이벤트에 의존하지 않고 `BallLauncher.Start()`에서 무조건 실행하도록 구현(스크립트 실행 순서와 무관하게 "터치 무관, 즉시" 요구사항을 보장하기 위함)
- `WaveManager.cs`/`MonsterBase.cs`/`HUDPanel.cs`는 지시사항대로 전혀 수정하지 않음(5단계 작업이 이미 동시 진행되어 `OnAllBallsReturned` 구독이 제거되어 있음을 확인)

---

## 2026-07-02

### 작업: 볼 발사 메커닉 재설계 QA 버그 4건 수정

**근거 문서:** `Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/plan.md` (신규 plan 없이 기존 plan.md 기준 QA 지적사항 직접 수정 지시)

**변경 파일:**
- `Assets/_Project/Scripts/Ball/Ball.cs` — `OnCollisionEnter2D`의 `"Wall"` 분기 수정. 기존에는 `_remainingBounces` 소진 시 무조건 `ReturnToPool()`을 호출해 로스터 볼이 영구 이탈하는 Critical 버그가 있었음. `"Ground"` 분기와 동일한 패턴으로 `BallLauncher.Instance.IsRosterMember(this)`를 분기해 로스터 소속 볼은 `ReturnToLaunchPoint()`(귀환 후 재발사), 로스터 밖 볼(서브볼 등)은 기존대로 `ReturnToPool()` 유지. 추가로 `_isReturning`(이미 귀환 중) 상태에서 벽에 부딪히는 경우, 반사 카운트를 건드리지 않고 `ReturnToLaunchPoint()`로 방향만 재계산하도록 분기 최상단에 가드 추가(가장 단순한 처리 — 방향이 벽 충돌로 흐트러져도 다시 LaunchPoint 쪽으로 재조준)
- `Assets/_Project/Data/BallData.asset` — `_maxBounces: 0` → `_maxBounces: 10`으로 직접 YAML 수정(ScriptableObject 에셋을 텍스트로 직접 편집). `BallSetupEditor.cs:104`가 신규 생성 시 10으로 설정하도록 되어 있었으나 기존 에셋 파일 자체는 0으로 저장되어 있던 데이터 불일치를 해소
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — 로스터 재설계 과정에서 제거됐던 `GameManager.OnGameStateChanged` 구독을 최소 형태로 재도입. `_currentGameState` 필드 추가, `OnEnable`/`OnDisable`에서 구독/해제, `HandleGameStateChanged(GameManager.GameState state)`로 캐싱. `RelaunchBall(Ball)` 최상단에 `if (_currentGameState != GameManager.GameState.Playing) return;` 가드 추가 — 게임 오버/클리어(Result) 이후 로스터 볼의 재발사만 차단(초기 로스터 발사/신규 볼 합류 경로는 건드리지 않음, plan.md 지시대로 최소 게이팅)
- `Assets/_Project/Scripts/Skill/SkillManager.cs` — 로스터 모델 도입 후 호출부가 없어진 죽은 코드 `ApplySkillToBall(Ball ball)` 메서드 삭제(Grep으로 호출부 없음을 재확인 후 제거)

**주요 결정사항:**
- Wall 분기의 `_isReturning` 처리는 "반사 카운트 재체크 불필요, 방향만 재계산"을 가장 단순하게 구현하기 위해 분기 최상단에서 조기 `return`으로 처리 — Ground 충돌로 이미 귀환 모드에 들어간 볼이 벽에 튕겨도 반사 카운트 소모/풀 반환 로직을 거치지 않고 항상 LaunchPoint로 재조준됨
- `RelaunchBall()` 가드만 추가하고 `LaunchRosterEntry()`/`InitializeRoster()`/`AddBallToRoster()`는 건드리지 않음 — 지시사항이 "RelaunchBall이 실제 재발사를 하지 않도록" 최소 게이팅만 요구했고, 게임 시작 시 최초 발사는 게이팅 대상이 아니므로 범위를 넓히지 않음(단순함 우선)
- 가드가 걸려 `RelaunchBall`이 조기 반환되면 볼은 `Ball.FixedUpdate()`에서 `_isReturning = false`로 전환된 채 직전 속도(LaunchPoint 방향) 그대로 계속 이동하게 됨 — "정지"가 아니라 "관성 유지"이나, plan.md 지시사항이 "정지 또는 재발사 안 함" 둘 다 허용했고 별도 정지 로직 추가는 과설계로 판단해 채택하지 않음

---

## 2026-07-02

### 작업: 궤적 프리뷰 신규 구현 (`TrajectoryPreview.cs`) — 볼 발사 메커닉 재설계 4단계

**근거 문서:** `Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/plan.md` "4단계 — 궤적 프리뷰 신규 컴포넌트" 섹션, `Assets/_Project/Docs/GameplayMechanics.md` 섹션 1

**생성 파일:**
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` — 신규 MonoBehaviour. `Awake()`에서 자식 GameObject 3개(`TrajectoryLine`/`HitDot`/`HitRing`)를 동적 생성해 각각 `LineRenderer`를 스스로 준비(씬 수작업 연결 불필요). `OnEnable`/`OnDisable`에서 `InputHandler.OnAimBegin`(즉시 표시+현재 `BallLauncher.LaunchDirection`으로 계산)/`OnDrag`(매 프레임 갱신)/`OnRelease`(전체 숨김) 구독. `UpdateTrajectory()`가 `LaunchPoint.position`→1차 충돌(`hit1`)→`Vector2.Reflect`로 반사 방향 계산→2차 충돌(`hit2`)까지 계산해 점선(단일 `LineRenderer`, 3점: origin/hit1/hit2)과 `hit2` 위치의 레드닷(`HitDot`)/원형 궤적선(`HitRing`)을 표시. 3차 충돌 이후는 계산하지 않음.

**수정 파일:**
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step6_PlaceManagers()`에 `PlaceManager<TrajectoryPreview>("TrajectoryPreview");` 1줄 추가(기존 Manager 배치 패턴과 동일하게 씬에 자동 생성).

**주요 결정사항:**
- **Raycast 방식 — `Physics2D.Raycast` 단발 대신 `Physics2D.RaycastAll` + 태그 필터링 채택**: 씬 확인 결과(`ProjectSettings/TagManager.asset`) `Wall`/`Ground`/`Monster` 태그 콜라이더가 모두 레이어 0(`Default`) 하나에만 존재해 레이어마스크로는 분리 불가. 또한 이 프로젝트는 이미 로스터 사이클 구조(노말볼 5개+특수볼)가 상시 화면에 비행 중이므로, 단순 `Physics2D.Raycast` 1회 호출 시 조준선 경로상의 다른 볼 콜라이더(태그 없음/`Untagged`, `Ball` 태그는 `TagManager.asset`에 미등록 상태로 확인됨)가 벽/바닥/몬스터보다 먼저 잡혀 궤적이 잘못 계산되는 문제가 있어, `RaycastAll` 결과를 `fraction` 기준 정렬 후 `Wall`/`Ground`/`Monster` 태그를 가진 첫 콜라이더만 유효한 충돌로 채택하는 방식으로 대체. 이는 지시문의 "볼 자신의 콜라이더 제외" 요구를 태그 필터링으로 자연스럽게 만족.
- **점선 렌더링 — 런타임 생성 텍스처 + `LineRenderer.textureMode = Tile`**: 커스텀 셰이더 없이 `Sprites/Default`(Built-in RP, 프로젝트에 URP 미적용 확인) 머티리얼에 4x1 픽셀 흑백 텍스처(앞 절반 불투명/뒤 절반 투명)를 입히고 `material.mainTextureScale.x = 1/DASH_WORLD_SIZE`로 대시 간격을 일정하게 유지. 시작점→1차 충돌→2차 충돌 3점을 하나의 `LineRenderer`로 이어 그려 반사 지점에서도 대시가 자연스럽게 이어지도록 함.
- **레드닷/원형 궤적선 — 별도 `LineRenderer` 2개로 원형 점열 생성**: `HitDot`은 반지름 대비 두꺼운 선 두께로 채워진 것처럼 보이게, `HitRing`은 얇은 선 두께의 고리로 구현(둘 다 `loop = true`). 스프라이트 에셋 신규 추가 없이 지시문에서 허용한 "LineRenderer로 그린 작은 도트" 방식 채택.
- **`OnRelease` 시 프리뷰 숨김 처리**: `GameplayMechanics.md`/plan.md 주의사항에는 릴리즈 후 유지 여부가 불명확하다고 되어 있었으나, 이번 작업 지시문 6번("조준하지 않을 때(터치 안 함)는 전부 숨긴다")이 명확히 규정하여 이를 그대로 따름.
- 색상/두께/반지름 등은 `[SerializeField]`로 노출해 Inspector에서 추후 시각적 튜닝 가능하도록 함(디자인 에이전트 조정 대상).
- `InputHandler.cs`/`BallLauncher.cs`는 기존 완성 코드를 그대로 활용했고 수정하지 않음.

---

## 2026-07-02

### 작업: BallLauncher 로스터 초기 발사 시간차 적용

**작업 내용:**
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` 수정 (신규 파일 없음)
- 게임 시작 시 노말볼 5개가 한 프레임에 완전히 겹쳐 동시 발사되던 것을 `_rosterLaunchInterval`초 간격으로 순차 발사하도록 변경

**수정 파일:**
- `Assets/_Project/Scripts/Ball/BallLauncher.cs`
  - `[SerializeField] private float _rosterLaunchInterval = 0.1f;` 필드 추가 (Inspector 노출)
  - `private void InitializeRoster()` → `private IEnumerator CoInitializeRoster()`로 전환 (DevRules.md 코루틴 네이밍 `Co + PascalCase` 준수), 루프 내 `LaunchRosterEntry` 호출 직후 마지막 볼이 아니면 `yield return new WaitForSeconds(_rosterLaunchInterval)` 삽입
  - `Start()`에서 `InitializeRoster()` 직접 호출 → `StartCoroutine(CoInitializeRoster())`로 교체
  - `using System.Collections;` 추가 (`IEnumerator` 사용을 위함)
  - `AddBallToRoster(SkillData)`는 지시사항대로 건드리지 않음 (볼 1개만 추가하는 경로라 시간차 불필요)

**주요 결정사항:**
- 시간차는 마지막 볼 발사 후에는 대기하지 않도록 `i < _normalBallCount - 1` 조건으로 처리 — 불필요한 프레임 지연 방지
- Grep으로 `InitializeRoster` 참조를 전수 확인해 BallLauncher.cs 내부(Start 호출부, 메서드 정의)만 존재함을 검증 후 안전하게 변경

---

## 2026-07-03

### 작업: UISetupEditor HUD Canvas 참조 연결 누락 버그 수정 (CharacterHpBar/CharacterXpBar NullReferenceException)

**작업 내용:**
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs` 수정 (신규 파일 없음)
- `Step2_SetupHUDCanvas()`에서 `CharacterHpBar`/`CharacterXpBar` 컴포넌트와 자식 `Slider`/`TMP_Text`를 생성만 하고 `SerializedObject`로 필드 연결을 하지 않아, 몬스터 처치(경험치 획득)나 몬스터 바닥 도달(HP 차감) 시 `_slider.value = ...`에서 NullReferenceException이 발생하던 버그 수정

**수정 파일:**
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs`
  - `hpBarSo` 블록에 `hpBarSo.FindProperty("_slider").objectReferenceValue = charHpObj.GetComponent<Slider>();` 라인 추가 (기존 `_hpText` 연결 라인 바로 위)
  - `CharacterXpBar` 연결 코드 자체가 없어서 신규로 `SerializedObject xpBarSo` 블록 추가 — `_slider`(`charXpObj.GetComponent<Slider>()`)와 `_levelText`(`levelTextObj.GetComponent<TextMeshProUGUI>()`) 연결, `EnsureComponent<CharacterXpBar>(charXpObj)` 호출 직후에 배치

**주요 결정사항:**
- 기존 `_hpText` 연결 패턴(`SerializedObject` + `FindProperty(...).objectReferenceValue = ...` + `ApplyModifiedPropertiesWithoutUndo()`)을 그대로 따름
- 이 수정은 Editor 스크립트 코드만 변경한 것이라 이미 저장된 씬(SampleScene.unity)의 기존 오브젝트에는 자동 반영되지 않음 — 사용자가 Unity 에디터에서 `PurpleCow > Setup > UI Setup` 메뉴를 다시 실행해야 씬에 실제로 반영됨을 사용자에게 안내함

---

## 2026-07-02 (기록 누락분 추가 정리)

### 작업: 로스터 볼 Wall 충돌 최종 정정 — "반사 횟수 무관 순수 반사, Ground 충돌에서만 귀환"

**배경:** "볼 발사 메커닉 재설계 QA 버그 4건 수정"(위 항목)에서는 로스터 소속 볼의 Wall 분기를 `IsRosterMember(this)`면 `ReturnToLaunchPoint()`(귀환 후 재발사)로 처리했었음. 이후 사용자가 원본 게임 실제 플레이 기준으로 재확인한 결과 "로스터 볼은 벽에서 반사 횟수와 무관하게 항상 순수 반사만 하고, 귀환은 오직 Ground 충돌에서만 일어난다"로 최종 정정 지시가 있어 `Ball.cs`의 Wall 분기를 다시 수정함. 이 수정이 이번 세션 이전에 agent-memory에 기록되지 않아 뒤늦게 추가 기록.

**변경 파일:**
- `Assets/_Project/Scripts/Ball/Ball.cs` — `OnCollisionEnter2D`의 `"Wall"` 분기를 재수정. `_isReturning`(귀환 중) 볼은 기존대로 `ReturnToLaunchPoint()`로 방향 재조준. 그 다음 `BallLauncher.Instance.IsRosterMember(this)`이면 반사 카운트를 전혀 건드리지 않고 그냥 `return`(물리 반사만 자연스럽게 일어나도록 방치, `ReturnToLaunchPoint()` 호출 자체를 제거). 로스터 밖 볼(서브볼 등)만 기존처럼 `_remainingBounces--` 후 소진 시 `ReturnToPool()`. `"Ground"` 분기는 기존 그대로(`IsRosterMember`면 `ReturnToLaunchPoint()`, 아니면 `ReturnToPool()`) 유지

**주요 결정사항:**
- 로스터 볼에는 사실상 `_maxBounces`/`_remainingBounces` 개념이 완전히 무의미해짐(Wall 분기에서 아예 감소시키지 않음) — 다만 서브볼 등 로스터 밖 볼은 여전히 이 값을 사용하므로 필드 자체나 `BallData._maxBounces`는 제거하지 않고 그대로 둠
- 귀환 트리거는 오직 `"Ground"` 태그 충돌 1곳으로 확정 — Wall에서는 어떤 경우에도 로스터 볼을 귀환시키지 않음

---

## 2026-07-03

### 작업: WaveData 20개 → WaveTableData 1개 테이블 SO 통합 (asset 개수 축소, task 문서 없이 예외 승인)

**작업 내용:**
- 사용자와 이미 합의된 "웨이브 20개 개별 asset → 테이블 SO 1개" 리팩토링. 간단한 작업으로 판단되어 research.md/plan.md 없이 예외적으로 바로 구현 (사용자 명시적 예외 승인)
- 기존 파일 3개 수정 + 신규 파일 1개 생성 + 기존 파일 1개 삭제 + 구 asset/meta 40개 삭제

**삭제 파일:**
- `Assets/_Project/Scripts/Data/WaveData.cs`, `WaveData.cs.meta` — `git rm`
- `Assets/_Project/Data/WaveData_Wave1.asset` ~ `WaveData_Wave20.asset` 및 각 `.meta` (총 40개) — `git rm`

**신규 파일:**
- `Assets/_Project/Scripts/Data/WaveTableData.cs` — `WaveEntry`(WaveNumber, SpawnEntries) Serializable 클래스 + `WaveTableData`(ScriptableObject, `_waves` List, `Waves`/`WaveCount` 프로퍼티) + 기존 `MonsterSpawnEntry`(Data, GridPosition) 그대로 이전

**수정 파일:**
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `_waveDatas` (WaveData[]) → `_waveTable` (WaveTableData) 단일 필드로 교체. `TotalWaves`, `SpawnWave()`, `CheckWaveCleared()`, `AdvanceToNextWave()` 전부 `_waveTable.WaveCount`/`_waveTable.Waves[index]` 기준으로 수정. `WaveData waveData` 지역변수 → `WaveEntry waveEntry`로 교체
- `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`
  - `CreateWaveDataAssets()` — 20개 개별 asset 생성 로직을 `Assets/_Project/Data/WaveTableData.asset` 1개 생성 + `_waves` 배열 크기 20 설정 + `WaveNumber` 1~20 채우기로 교체 (이미 존재하면 스킵 로그 후 return)
  - `SetupWaveSpawnEntries()` — 몬스터 종류 수/스폰 개수/그리드 위치 계산 로직(그룹별 진행, `spawnCount = 3 + posInGroup + groupIdx * 2`, `startY = 8 - groupIdx * 2` 등)은 그대로 유지. 20개의 개별 `WaveData` asset을 로드하던 것을 `WaveTableData.asset` 1개를 로드해 `_waves[waveIdx].SpawnEntries`에 채우는 방식으로 교체. 웨이브별 "이미 스폰 데이터 있으면 스킵" 멱등성 체크 유지, `SerializedObject.ApplyModifiedPropertiesWithoutUndo()`는 루프 밖으로 이동(단일 오브젝트이므로 1회만 호출)
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step9_ConnectWaveManagerRefs()`의 `_waveDatas` 배열(20개 GUID 로드 후 `arraySize=20` 설정) 연결 로직을 `WaveTableData.asset` 1개 로드 후 `_waveTable` 단일 필드에 대입하는 방식으로 단순화

**환경 제약 및 후속 작업 필요:**
- 이 원격 환경에 Unity 에디터가 없어 `[MenuItem]` 실행 불가 — 새 `WaveTableData.asset` 파일은 생성하지 못함(코드만 수정)
- `Assets/Scenes/SampleScene.unity`의 `WaveManager` 컴포넌트 직렬화 데이터에 구 `_waveDatas` 배열(20개 GUID 참조)이 그대로 남아있음 — 필드명이 `_waveTable`로 바뀌었으므로 이 데이터는 Unity가 열면 고아 데이터로 처리되고 `_waveTable`은 비어있는 상태로 로드됨
- 사용자가 로컬 Unity에서 반드시 순서대로 재실행해야 함: `PurpleCow/Setup/Monster System Setup` (또는 `CreateWaveDataAssets()`+`SetupWaveSpawnEntries()`가 포함된 메뉴) → `WaveTableData.asset` 생성 및 스폰 데이터 채움, 이어서 `PurpleCow/Setup/Scene Setup`(또는 Step9 단독)을 재실행해 `WaveManager._waveTable`을 새 asset에 재연결해야 함
- `DevRules.md`의 하드코딩 금지 표 `WaveData` 언급은 이번 작업 범위에서 의도적으로 미수정 (별도 문서 갱신 예정)

**주요 결정사항:**
- `WaveTableData.CreateWaveDataAssets()`는 asset이 이미 있으면 스킵하는 기존 멱등성 관례를 유지 (20개 개별 스킵 로그 → 1개 스킵 로그로 단순화)
- `SetupWaveSpawnEntries()`에서 `SerializedObject.ApplyModifiedPropertiesWithoutUndo()` 호출을 매 웨이브 반복문 안에서가 아니라 루프 종료 후 1회만 호출하도록 변경 — 기존엔 20개 별도 asset이라 웨이브마다 각각의 SerializedObject를 Apply해야 했지만, 이제 하나의 SerializedObject(같은 asset)를 여러 웨이브에 걸쳐 수정하므로 마지막에 한 번만 Apply하는 것이 자연스러움
- 커밋/푸시는 수행하지 않음 (오케스트레이터가 사용자 확인 후 별도 처리)

---

## 2026-07-03

### 작업: 볼 천장 이탈 버그 수정 (Wall_Top 콜라이더 추가)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-48_ball-ceiling-wall-fix/plan.md` (research.md 포함, 사용자 승인 완료)
- 기존 파일 1개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step5_PlaceWallsAndGround()`에 `PlaceColliderObject("Wall_Top", "Wall", new Vector3(0f, 8f, 0f), new Vector2(12f, 0.2f));` 1줄 추가 (`Ground` 호출 바로 다음 줄). 좌표 y=8은 `AIFailures.md` 문서화된 실제 플레이 영역 상단값, size(12,0.2)는 `Ground`와 동일 재사용, 태그는 좌우 벽과 동일한 `"Wall"`.

**Ball.cs 재검증 결과:**
- `Ball.OnCollisionEnter2D`의 `else if (collision.gameObject.CompareTag("Wall"))` 분기(111~130번 줄)는 오브젝트 이름이 아닌 태그값만으로 분기하므로, `Wall_Top`이 `"Wall"` 태그로 생성되면 좌우 벽과 완전히 동일하게 처리됨을 코드로 재확인. `Ball.cs` 수정 불필요 — 실제로 수정하지 않음.

**범위 밖 항목(건드리지 않음):**
- `Assets/Scenes/SampleScene.unity` — 사용자가 옵션 A(로컬 Unity 에디터에서 `PurpleCow/Setup/Scene Setup` 메뉴 재실행)로 진행하기로 확정했으므로 씬 파일 직접 편집하지 않음. 사용자가 로컬에서 메뉴를 재실행해야 `Wall_Top`이 실제 씬에 반영됨.
- `BallBounce.physicsMaterial2D`의 Wall/Ground 미연결 문제, `CollisionDetectionMode2D.Continuous` 터널링 가능성 — research.md/plan.md에서 이미 이번 작업 범위 제외로 확정되어 손대지 않음.

**주요 결정사항:**
- 코드 변경은 정확히 plan.md에 명시된 1줄 추가로 한정, 다른 리팩토링/개선 없음
- 커밋/푸시는 수행하지 않음 (오케스트레이터가 처리)

---

## 2026-07-03

### 작업: 배경/해상도 대응 5단계 — CameraFitter 신규 작성 + BackgroundFitter 실행 순서 보강

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-03/12-30_background-resolution-fix/plan.md` (5단계) + 2단계 보강분(BackgroundFitter Awake→Start)
- 신규 파일 1개 생성 + 기존 파일 2개 수정

**생성 파일:**
- `Assets/_Project/Scripts/Core/CameraFitter.cs` — MonoBehaviour, `_targetCamera`/`_baseOrthographicSize(10f)`/`_requiredHalfWidth(5.6f)` SerializeField, `Awake()`에서 `requiredSize = _requiredHalfWidth / aspect`와 `_baseOrthographicSize` 중 큰 값을 `orthographicSize`에 1회 적용. null 방어 처리 포함.

**수정 파일:**
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` — 계산 메서드를 `Awake()` → `Start()`로 이름만 변경 (내부 로직 동일). Unity가 모든 오브젝트의 Awake()를 끝낸 뒤 Start()를 호출하는 것을 이용해, `CameraFitter.Awake()`가 카메라 orthographicSize를 먼저 확정한 뒤 `BackgroundFitter.Start()`가 그 값을 읽도록 순서 보장.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `SetupScene()`에 `Step5_PlaceWallsAndGround();` 다음 줄로 `Step6_SetupCameraFitter();` 호출 추가
  - `Step5_PlaceWallsAndGround()`와 기존 `Step6_PlaceManagers()` 사이에 신규 `Step6_SetupCameraFitter()` 메서드 추가 — `Camera.main`으로 Main Camera를 찾아 `CameraFitter` 컴포넌트를 부착(이미 있으면 재부착 없이 참조만 갱신)하고, `SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo` 패턴(기존 `ConnectBackgroundFitterRefs()`와 동일)으로 `_targetCamera`=자기 자신, `_baseOrthographicSize`=10, `_requiredHalfWidth`=5.6을 연결.

**주요 결정사항:**
- 신규 메서드명은 요청받은 대로 `Step6_SetupCameraFitter`를 그대로 사용. 기존에 이미 `Step6_PlaceManagers`(및 뒤이은 Step7/8/9)가 존재해 호출 순서상 두 개의 "Step6"이 연달아 호출되고 이후 Step7~9의 번호가 실제 호출 순서(8~10번째)와 어긋나는 번호 불일치가 발생하지만, 이는 CLAUDE.md의 외과적 변경 원칙(요청된 파일/함수만 수정) 및 요청에 명시된 정확한 메서드명을 우선해 그대로 두었고 다른 Step들의 번호를 리네이밍하지 않음. 필요 시 별도 후속 정리 작업으로 진행 권장.
- CameraFitter는 plan.md 원안 그대로 구현 (Awake 1회 계산, Mathf.Max로 base와 required 중 큰 값 채택).
- 신규 아트 에셋 제작 없음. git 변경 명령어(add/commit) 실행하지 않음.

---

## 2026-07-03

### 작업: SceneSetupEditor.cs Step 번호 정리 (순수 리네이밍)

**작업 내용:**
- 위 항목(배경/해상도 대응 5단계)에서 예고했던 후속 정리 작업. `Step6_SetupCameraFitter()`가 `Step5_PlaceWallsAndGround()` 직후에 추가되며 발생한 `Step6` 중복 및 이후 번호 불일치를 실제 호출 순서(1~10)와 메서드 이름이 일치하도록 리네이밍만 수행 (로직 변경 없음)
- 기존 파일 1개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `Step6_PlaceManagers` → `Step7_PlaceManagers` (정의부 + `SetupScene()` 호출부 + 섹션 구분 주석 `// Step 6. Manager 오브젝트 씬 배치` → `// Step 7. ...`)
  - `Step7_ConnectBallLauncherRefs` → `Step8_ConnectBallLauncherRefs` (정의부 + 호출부 + 주석 `// Step 7. BallLauncher 참조 연결` → `// Step 8. ...`)
  - `Step8_ConnectBallPrefabRefs` → `Step9_ConnectBallPrefabRefs` (정의부 + 호출부 + 주석 `// Step 8. Ball.prefab 참조 연결` → `// Step 9. ...`, `[MenuItem("PurpleCow/Setup/Connect Ball Prefab Refs")]` 메뉴 문자열은 그대로 유지)
  - `Step9_ConnectWaveManagerRefs` → `Step10_ConnectWaveManagerRefs` (정의부 + 호출부 + 주석 `// Step 9. WaveManager 참조 연결` → `// Step 10. ...`)

**주요 결정사항:**
- Grep으로 `Assets/_Project/Scripts/Editor/` 전체를 확인해 이 4개 메서드를 참조하는 다른 파일이 없음을 사전 확인 후 진행 (SceneSetupEditor.cs 내부 참조만 존재)
- `[MenuItem]` 어트리뷰트 문자열(메뉴 표시명)은 변경 대상에서 제외 — C# 메서드 식별자만 리네이밍
- 리네이밍 후 `SetupScene()` 호출 순서와 메서드 번호가 Step1~Step10으로 완전히 일치함을 재확인
- 로직/동작 변경 없음, 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: BackgroundFitter.cs Contain → Stretch 방식 전환

**작업 내용:**
- 실기기 테스트에서 Contain 방식(`Mathf.Min` 기반 비율 유지 스케일)이 사방 여백 문제를 일으켜, 논의 끝에 Stretch 방식(가로/세로 각각 독립적으로 카메라 뷰포트에 맞춤, 비율 왜곡 감수)으로 전환
- 기존 파일 1개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` — `Start()` 내부에서 `float scale = Mathf.Min(camSize.x / spriteSize.x, camSize.y / spriteSize.y); transform.localScale = new Vector3(scale, scale, 1f);` 2줄을 `transform.localScale = new Vector3(camSize.x / spriteSize.x, camSize.y / spriteSize.y, 1f);` 1줄로 교체. `scale` 지역 변수 및 `Mathf.Min` 비교 로직 제거. `Start()` 진입부의 null 방어 처리 등 다른 로직은 변경하지 않음.

**주요 결정사항:**
- 요청 범위를 정확히 준수해 스케일 계산 로직 2줄만 교체, 그 외 파일 내용(주석 없음, null 체크 등) 일절 손대지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: WallFitter._nativeBottomY 기준값 수정 (-5.33f → -10f)

**작업 내용:**
- `Ground`(하단 벽)는 다른 벽들(`Wall_Left`/`Wall_Right`/`Wall_Top`)과 달리 배경 격자 경계가 아니라 그 아래 캐릭터/볼 발사 위치까지 포함하는 더 아래쪽 지점에 있어야 함이 확인됨
- 원래(변경 전) `Ground` 값이 `y=-10`이었고 배경 텍스처 맨 아래 끝 계산값(`y≈-10.24`)과 거의 일치하므로, 격자 경계값(-5.33) 대신 원래 값(-10)으로 되돌림
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/WallFitter.cs` — `_nativeBottomY` 필드 기본값 `-5.33f` → `-10f`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step6_SetupWallFitter()` 내 `so.FindProperty("_nativeBottomY").floatValue` `-5.33f` → `-10f`

**주요 결정사항:**
- `_nativeLeftX`(-6.04) / `_nativeRightX`(5.89) / `_nativeTopY`(5.55)는 요청 범위 밖이므로 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: 배경/벽 크기 계산에 확대 배율(zoomFactor) 추가

**작업 내용:**
- 실기기 테스트에서 격자가 화면에서 차지하는 비중이 작다는 피드백에 따라, 배경 전체를 1.3배 확대해 격자가 화면을 더 크게 채우고 바깥 테두리 장식은 화면 밖으로 잘려나가도록 함
- `BackgroundFitter`와 `WallFitter`가 동일한 배율(`_zoomFactor = 1.3f`)을 사용해야 벽이 계속 배경 격자와 맞음
- 기존 파일 3개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` — `[SerializeField] private float _zoomFactor = 1.3f;` 필드 추가, `Start()` 내 `transform.localScale` 계산 시 `camSize.x / spriteSize.x`, `camSize.y / spriteSize.y` 각각에 `_zoomFactor`를 곱하도록 수정
- `Assets/_Project/Scripts/Core/WallFitter.cs` — `[SerializeField] private float _zoomFactor = 1.3f;` 필드 추가, `Start()` 내 `scaleX`/`scaleY` 계산 시 각각 `_zoomFactor`를 곱하도록 수정
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `ConnectBackgroundFitterRefs()`에 `so.FindProperty("_zoomFactor").floatValue = 1.3f;` 추가, `Step6_SetupWallFitter()`에 동일하게 `so.FindProperty("_zoomFactor").floatValue = 1.3f;` 추가 (기존 SerializedObject/FindProperty/ApplyModifiedPropertiesWithoutUndo 패턴 재사용)

**주요 결정사항:**
- 다른 로직(native 좌표값, 스케일 계산 외 부분)은 일절 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: BackgroundFitter/WallFitter Inspector 실시간 반영 (ExecuteAlways + OnValidate)

**작업 내용:**
- `_zoomFactor` 등 값을 Inspector에서 조정할 때 Play 모드 진입 없이 씬에 즉시 반영되도록 개선
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` — 클래스에 `[ExecuteAlways]` 추가, 기존 `Start()` 본문을 `private void Apply()`로 이동, `Start()`는 `Apply()` 호출만 수행, `OnValidate()` 신규 추가해 `Apply()` 호출
- `Assets/_Project/Scripts/Core/WallFitter.cs` — 동일 패턴 적용: `[ExecuteAlways]` 추가, `Start()` 본문을 `Apply()`로 이동, `Start()`는 `Apply()` 호출만, `OnValidate()` 신규 추가해 `Apply()` 호출. `SetX`/`SetY` 헬퍼와 필드는 변경 없음

**주요 결정사항:**
- 이번 작업은 진행 중인 `2026-07-03/12-30_background-resolution-fix` task의 연장선상의 작은 에디터 편의 기능으로 판단해 별도 plan.md 문서 작성 없이 진행 (오케스트레이터 판단)
- 다른 필드/로직은 일절 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: WallFitter `_nativeBottomY` 기본값 수정 (-10f → -7.5f)

**작업 내용:**
- 사용자 명시적 승인에 따라 별도 plan.md 없이 바로 구현
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/WallFitter.cs` — `_nativeBottomY` 필드 기본값 `-10f` → `-7.5f`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step6_SetupWallFitter()` 내 `so.FindProperty("_nativeBottomY").floatValue` 설정값 `-10f` → `-7.5f`

**배경:**
- `_zoomFactor`(1.3)가 배경뿐 아니라 벽 위치에도 곱해지는 구조에서, 기존 `_nativeBottomY`(-10)가 카메라 시야 경계(-10)에 거의 딱 붙어 있어 `-10 × 1.3 ≈ -12.7`로 카메라 시야(±10) 밖으로 나가는 문제가 실기기 테스트에서 확인됨
- 격자 아래 덩쿨 장식 위치를 감안해 `-7.5`로 조정

**주요 결정사항:**
- `_nativeLeftX`/`_nativeRightX`/`_nativeTopY`/`_zoomFactor`는 요청 범위 외이므로 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: WallFitter 벽 기준값 4개 재조정 (좌/우/상단 바깥으로, 하단 안으로)

**작업 내용:**
- 사용자 명시적 승인에 따라 별도 plan.md 없이 바로 구현
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/WallFitter.cs`
  - `_nativeLeftX` 기본값 `-6.04f` → `-6.5f`
  - `_nativeRightX` 기본값 `5.89f` → `6.3f`
  - `_nativeTopY` 기본값 `5.55f` → `6.0f`
  - `_nativeBottomY` 기본값 `-7.5f` → `-6.5f`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step6_SetupWallFitter()` 내 동일 4개 `so.FindProperty(...).floatValue` 설정값을 위와 동일하게 변경

**배경:**
- 실기기 테스트 결과 좌우/상단 벽은 카메라 시야 기준 조금 더 바깥으로, 하단(Ground)은 조금 더 안쪽으로 배치되도록 조정 필요성이 확인됨

**주요 결정사항:**
- `_zoomFactor`는 요청 범위 외이므로 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

## 2026-07-03

### 작업: WallFitter에 LaunchPoint(볼 발사 지점) 위치 관리 추가

**작업 내용:**
- 사용자 명시적 승인에 따라 별도 plan.md 없이 바로 구현
- 기존 파일 2개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/WallFitter.cs`
  - `[SerializeField] private Transform _launchPoint;` 필드 추가 (`_ground` 다음)
  - `[SerializeField] private float _nativeLaunchPointY = -6.0f;` 필드 추가 (`_nativeBottomY` 다음)
  - `Apply()`에 `SetY(_launchPoint, _nativeLaunchPointY * scaleY);` 추가 (`SetY(_ground, ...)` 다음 줄)
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `Step6_SetupWallFitter()`에 `Transform launchPoint = FindTransformOrWarn("LaunchPoint");` 추가, `SerializedObject`로 `_launchPoint`(Transform) / `_nativeLaunchPointY`(-6.0f) 연결
  - `SetupScene()`의 호출 순서 변경: `Step6_SetupWallFitter()`를 `Step8_ConnectBallLauncherRefs(ballPrefab)` **다음**으로 이동 (기존에는 `Step5_PlaceWallsAndGround()` 직후였음)

**배경 및 실행 순서 문제 해결 방법:**
- `LaunchPoint`는 `BallLauncher`의 자식으로 `Step8_ConnectBallLauncherRefs()`에서 최초 생성됨. 기존 호출 순서(`Step6` → `Step7` → `Step8`)대로면 `Step6_SetupWallFitter()` 실행 시점에 `LaunchPoint`가 아직 씬에 없어 `_launchPoint` 참조 연결이 항상 실패(경고 로그 후 null)하는 문제가 있었음
- Step 함수 이름/번호는 그대로 유지하고 (요청 범위 외 리네이밍 방지), `SetupScene()` 내부의 **호출 순서만** `Step6_SetupWallFitter()`를 `Step8_ConnectBallLauncherRefs()` 뒤로 옮겨 해결. 코드에는 순서 변경 이유를 설명하는 주석 추가
- `Step6_SetupWallFitter()`는 기존 `FindTransformOrWarn()` 패턴을 그대로 재사용해 `GameObject.Find("LaunchPoint")` 방식으로 참조를 찾으므로, 씬 재실행(이미 `LaunchPoint`가 존재하는 케이스)에서도 정상 동작함

**주요 결정사항:**
- `SetY`/`SetX` 헬퍼가 이미 null 방어 처리되어 있어 재사용만 함 (신규 로직 없음)
- 요청받지 않은 다른 필드(`_nativeLeftX` 등)는 변경하지 않음
- 커밋/푸시 미수행 (요청에 따라 파일 수정만 진행)

---

### 작업: 볼 궤적 프리뷰 상시 표시 + 조준 정확도 + 색상/크기 수정

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/plan.md` (research.md 포함, 사용자 승인 완료)
- 기존 파일 4개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`
  - 이슈1: `OnEnable`/`OnDisable`의 `InputHandler.OnAimBegin`/`OnDrag`/`OnRelease` 구독 제거, `HandleAimBegin`/`HandleDrag`/`HandleRelease` 삭제, `Update()` 신규 추가(`UpdateTrajectory(BallLauncher.Instance.LaunchDirection)` 매 프레임 호출), `Awake()`의 `SetVisible(false)` → `SetVisible(true)`
  - 이슈3: `_hitRing`이 새 필드 `_ringColor`(기본값 `Color32(225,225,220,255)`)를 참조하도록 분리, `_hitColor` 기본값 `Color32(206,90,82,255)`로 톤다운, `_dotRadius` `0.08f`→`0.05f`, `DASH_WORLD_SIZE` `0.3f`→`0.15f`, `_lineColor` 기본값 `Color32(225,225,220,255)`로 변경
  - 클래스 상단 주석도 `OnAimBegin~OnRelease` 언급을 "터치 여부와 무관하게 매 프레임" 문구로 함께 수정 (plan.md에 직접 명시되지 않았으나 방금 제거한 로직을 그대로 서술하던 주석이라 부정확해져 함께 수정 — 관련 있는 범위로 판단)
- `Assets/_Project/Scripts/Core/InputHandler.cs`
  - 이슈2: `private Camera _mainCamera;` 필드 추가, `protected override void Awake() { base.Awake(); _mainCamera = Camera.main; }` 추가(단순 `private void Awake()`가 아니라 `Singleton<InputHandler>.Awake()`를 오버라이드+`base.Awake()` 호출하는 형태로 구현 — plan.md는 단순 `Awake()` 추가라고만 적었지만, `InputHandler`가 `Singleton<T>`를 상속하므로 `base.Awake()`를 호출하지 않으면 `Instance` 할당이 씹히는 문제를 방지하기 위한 필수 보정)
  - `_dragStartPosition` 저장을 `_mainCamera.ScreenToWorldPoint(pressedPos.Value)`로, `OnDrag` 방향 계산을 `(ScreenToWorldPoint(currentPos.Value) - _dragStartPosition).normalized`로 변경
- `Assets/_Project/Docs/UIRules.md` 섹션 11 — "조준 중에만 표시" 문구를 "터치 여부와 무관하게 항상 표시, 매 프레임 갱신" 취지로 수정, Inspector 조절 값 표에 `_ringColor` 행 추가(`_hitColor` 설명도 "레드닷/원형 궤적선 색상"→"레드닷 색상"으로 정정)
- `Assets/Scenes/SampleScene.unity` — **plan.md의 예상 변경 파일 목록에는 없었으나 "주의사항" 섹션에서 명시적으로 요구한 사항**: `TrajectoryPreview` 컴포넌트의 기존 인스펙터 오버라이드 값(`_lineColor` white, `_hitColor` red, `_dotRadius` 0.08)이 코드 기본값 변경을 그대로 덮어써 화면에 반영되지 않는 것을 확인하고, YAML을 새 기본값과 동일하게 직접 갱신(`_ringColor` 필드도 새로 추가). 코드 기본값과 씬의 실제 값을 일치시키지 않으면 이슈3 변경이 시각적으로 전혀 반영되지 않기 때문.

**주요 결정사항:**
- `InputHandler.Awake()`는 `private`이 아닌 `protected override` + `base.Awake()`로 구현 — `Singleton<T>` 상속 클래스에서 `Awake()`를 새로 정의할 때 `base.Awake()` 누락 시 `Instance`가 설정되지 않는 문제를 다른 클래스(`BallLauncher` 등)와 동일한 패턴으로 방지
- 씬 파일(`SampleScene.unity`)의 `TrajectoryPreview` 인스펙터 오버라이드 값을 코드 기본값과 동일하게 직접 갱신 — plan.md 예상 파일 목록에는 없었지만 "주의사항" 섹션의 명시적 지시(오버라이드 확인 후 필요 시 리셋/재적용)에 따른 조치. 이전 작업(볼 천장 이탈 버그 수정)과 달리 이번엔 plan.md가 씬 값 확인/재적용을 직접 요구했으므로 직접 편집함
- `TrajectoryPreview.cs` 클래스 상단 주석 수정은 plan.md에 명시되지 않았지만, 방금 제거한 이벤트 기반 로직을 그대로 서술하던 부정확한 주석이라 같은 변경 범위로 판단해 함께 수정
- git 커밋/푸시는 수행하지 않음 (사용자가 별도 처리 예정)

---

## 2026-07-04

### 작업: 이슈 4 — 조준 모델 전환 (상대 드래그 → 절대 조준, `InputHandler.cs`)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/plan.md` "이슈 4" 섹션 (research.md 이슈 4 포함, 사용자 승인 완료). 이슈 1~3은 이미 구현 완료 상태였고 이번엔 이슈 4만 추가 구현.
- 기존 파일 1개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/InputHandler.cs`
  - `_dragStartPosition` 필드 제거 (상대 드래그 기준점 개념 삭제)
  - `private Vector2 ComputeAimDirection(Vector2 screenPos)` 헬퍼 메서드 추가 — `_mainCamera.ScreenToWorldPoint(screenPos)`로 구한 월드 좌표에서 `BallLauncher.Instance.LaunchPoint.position`을 뺀 뒤 `.normalized` 반환
  - `pressedPos.HasValue` 분기: `_isDragging = true; OnAimBegin?.Invoke();`에 이어 `OnDrag?.Invoke(ComputeAimDirection(pressedPos.Value));`를 즉시 발행하도록 추가 (터치 시작 프레임부터 궤적이 반응하도록 수정)
  - `currentPos.HasValue && _isDragging` 분기: 기존 "월드 currentPos - 월드 dragStartPosition" 델타 계산을 제거하고 `OnDrag?.Invoke(ComputeAimDirection(currentPos.Value));`로 교체 (발사 지점 → 현재 손가락 위치의 절대 방향을 매 프레임 재계산)
  - `released && _isDragging` 분기는 `_dragStartPosition`을 참조하지 않아 수정 불필요 (plan.md 예상대로)

**사전 확인 사항 (주의사항 대응):**
- `BallLauncher`도 `Singleton<BallLauncher>` 상속이며 `Awake()`에서 `base.Awake()`로 `Instance` 할당. `InputHandler`는 `BallLauncher.Instance`를 `Awake()`/`Start()`가 아닌 `Update()`(그것도 `ComputeAimDirection` 호출 시점, 즉 터치 발생 프레임)에서만 참조하므로, 모든 오브젝트의 `Awake()`가 끝난 뒤 `Update()`가 도는 Unity 생명주기상 스크립트 실행 순서와 무관하게 안전함을 코드 레벨에서 확인
- `Assets/Scenes/SampleScene.unity`에서 `BallLauncher` 컴포넌트의 `_launchPoint: {fileID: 859741423}`가 실제로 연결되어 있어 `LaunchPoint`가 null이 아님을 씬 YAML 직접 확인

**주요 결정사항:**
- plan.md에 기술된 구현 순서(헬퍼 메서드 추가 위치, 각 분기 수정 내용)를 그대로 따름, 별도 해석/추가 변경 없음
- `UIRules.md`, `TrajectoryPreview.cs` 등 이슈 1~3 관련 파일은 이번 범위에 포함되지 않으므로 손대지 않음
- git 커밋/푸시는 수행하지 않음 (사용자가 별도 처리 예정)

---

## 2026-07-04

### 작업: 이슈 5 — 터치 시작(Began) 단계 폴링 누락 대응, `_isDragging` 상태 기반 판정으로 재구성 (`InputHandler.cs`)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/plan.md` "이슈 5" 섹션 (research.md 이슈 5 포함, 사용자 승인 완료). 이슈 1~4는 이미 구현 완료 상태였고 이번엔 이슈 5만 추가 구현.
- 기존 파일 1개 수정 (신규 파일 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Core/InputHandler.cs`
  - `Update()` 내부 `Vector2? pressedPos`/`Vector2? currentPos` 두 지역 변수를 `Vector2? touchPos` 하나로 통합
  - 터치 phase 분기: `Began`/`Moved`/`Stationary` 세 경우를 하나의 조건으로 묶어 `touchPos = touch.position.ReadValue();`로 채우도록 변경. `Ended`/`Canceled`일 때만 `released = true` 유지
  - 마우스 분기: `wasPressedThisFrame` 기반으로 별도 `pressedPos`를 채우던 코드 제거, `Mouse.current.leftButton.isPressed`일 때 `touchPos = Mouse.current.position.ReadValue();`로 통일. `wasReleasedThisFrame`일 때 `released = true`는 그대로 유지
  - `pressedPos.HasValue` 블록과 `currentPos.HasValue && _isDragging` 블록을 `if (touchPos.HasValue) { if (!_isDragging) { _isDragging = true; OnAimBegin?.Invoke(); } OnDrag?.Invoke(ComputeAimDirection(touchPos.Value)); }` 하나로 통합 (plan.md 4번 코드 그대로)
  - `released && _isDragging` 블록은 변경 없음
  - `ComputeAimDirection(Vector2 screenPos)` 헬퍼(이슈 4에서 추가)는 수정 없이 그대로 재사용

**주의사항 확인 결과:**
- 일반적인 터치 시나리오(Began이 정상 관측되는 경우)의 동작 불변 확인: `!_isDragging` 조건이 여전히 시작 프레임에서만 참이 되므로 `OnAimBegin`은 정확히 1회, 그 직후 `OnDrag`도 즉시 1회 발행됨 — 기존 동작과 로직상 동일하며, Began이 누락되는 예외 프레임만 추가로 안전하게 처리됨
- 마우스 분기 통일로 사라진 클릭 첫 프레임의 `OnDrag` 중복 발행에 의존하는 코드가 없는지 확인: 코드 전체에서 `InputHandler.OnDrag`를 구독하는 곳은 `BallLauncher.HandleDrag(Vector2 direction) { _launchDirection = direction; }` 한 곳뿐이며(`TrajectoryPreview`는 이슈 1에서 이미 `Update()` 기반으로 전환되어 `OnDrag` 구독 안 함), 단순 필드 대입으로 완전히 멱등(idempotent)이라 중복 호출 제거로 인한 영향 없음을 grep으로 확인
- `pressedPos`/`currentPos` 변수명/주석 잔존 여부 확인: `Assets/_Project` 전체 grep 결과 두 이름 모두 InputHandler.cs를 포함한 코드에서 완전히 제거되었고, 문서(plan.md/research.md 등 task 문서)에만 과거 기록으로 남아있음을 확인(문서는 수정 대상 아님)

**주요 결정사항:**
- plan.md에 기술된 코드 스니펫(4번)을 그대로 적용, 별도 해석/추가 변경 없음
- git 커밋/푸시는 수행하지 않음 (사용자가 별도 처리 예정)

---

### 작업: 배경 격자 정사각형 보정 — `BackgroundFitter`/`WallFitter` 계산식 교체 + 신규 필드 주입 에디터

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-04/16-40_background-square-grid-fix/plan.md` (사용자 승인 완료)
- 기존 파일 2개 수정 + 신규 파일 1개 생성

**수정 파일:**
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs`
  - 신규 필드 3개 추가: `_cellAspectCorrection = 1.647f`, `_gridAreaWidth = 14.53f`, `_gridAreaHeight = 10.16f` (`[SerializeField] private float`, 기존 `_zoomFactor` 뒤에 배치)
  - `Apply()`의 기존 "camSize / spriteSize × zoomFactor" 독립 축 계산을 plan.md 1단계 공식(scaleXNeeded/scaleYNeeded → uniformScale(Cover, ×zoomFactor) → scaleX=uniformScale, scaleY=uniformScale×cellAspectCorrection)으로 교체
  - `_spriteRenderer.sprite.bounds.size`(spriteSize) 참조 제거, `_spriteRenderer` 필드 자체와 null 체크는 유지
  - `[ExecuteAlways]`/`Start()`/`OnValidate()` → `Apply()` 위임 구조 변경 없음
- `Assets/_Project/Scripts/Core/WallFitter.cs`
  - `BackgroundFitter`와 동일한 신규 필드 3개(`_cellAspectCorrection`/`_gridAreaWidth`/`_gridAreaHeight`, 동일 값) 추가, 동일한 공식으로 `Apply()` 내 scaleX/scaleY 계산 교체
  - `_nativeLeftX(-6.5)`/`_nativeRightX(6.3)`/`_nativeTopY(6.0)`/`_nativeBottomY(-6.5)`/`_nativeLaunchPointY(-6.0)`/`_zoomFactor(1.3)` 값은 plan.md 지시대로 코드에 그대로 유지(재조정 안 함) — 새 계산식 아래에서 이 값들이 여전히 유효한지는 사용자의 로컬 실기기 검증 대상
  - `_backgroundSpriteRenderer` 필드는 스케일 계산에 더 이상 쓰이지 않지만 null 체크 용도로 필드 자체는 유지
  - `SetX(_wallLeft, ...)` 등 호출부/`[ExecuteAlways]` 구조 변경 없음

**생성 파일:**
- `Assets/_Project/Scripts/Editor/BackgroundGridFitSetupEditor.cs` (신규)
  - `[MenuItem("PurpleCow/Setup/Background Grid Fit Setup")]`, 기존 `SceneSetupEditor.cs`/`MonsterSetupEditor.cs`와 동일한 패턴(`SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo`)
  - `Object.FindFirstObjectByType<BackgroundFitter>()`/`FindFirstObjectByType<WallFitter>()`로 씬에서 컴포넌트 탐색 후, 신규 필드 3개(`_cellAspectCorrection=1.647f`, `_gridAreaWidth=14.53f`, `_gridAreaHeight=10.16f`)를 두 컴포넌트 모두에 주입
  - `SceneSetupEditor.cs`는 이번 작업에서 전혀 수정하지 않음(읽기만 함, 패턴 참고 목적)

**주요 결정사항:**
- Unity 6000.3의 `Object.FindObjectOfType`은 obsolete 경고 대상이라, plan.md에 API명이 명시되지 않았으므로 경고 없는 `FindFirstObjectByType`으로 선택(외과적 변경 범위 내의 합리적 API 선택으로 판단, 별도 리팩토링 아님)
- plan.md 지시대로 두 스크립트의 계산 로직 중복 구현 구조를 그대로 유지, 공용 유틸리티로 리팩토링하지 않음
- `SceneSetupEditor.cs`는 절대 수정 금지 지시를 준수, 리소스(PNG)도 수정하지 않음
- 이 원격 환경에는 Unity 에디터가 없어 컴파일 검증 불가 — 문법/네이밍 컨벤션만 신중히 확인
- git 커밋/푸시는 수행하지 않음 (오케스트레이터가 사용자 확인 후 처리 예정)

---

## 2026-07-04

### 작업: 몬스터 시스템 개편 (BlockSize 데이터화, 콜라이더/HP바 자동 적용, 프리팹 BlockVisual 합성, WaveTableData/WaveManager 파라미터화 + 그리드 랜덤 배치)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-04/22-53_monster-system-overhaul/plan.md` (사용자 승인 문서, STEP1~6 그대로 구현)
- 기존 파일 4개 수정 + 신규 파일 1개 생성 (프리팹/asset 실물 반영은 `PurpleCow/Setup/Monster Overhaul Setup` 메뉴 실행 필요, 원격 환경엔 Unity 에디터 없음)

**수정 파일:**
- `Assets/_Project/Scripts/Data/MonsterData.cs` — `public enum BlockSize { OneByOne, TwoByOne, OneByTwo }` 추가(클래스 밖), `_blockSize` 필드 + `BlockSize` 프로퍼티 추가. 기존 4개 필드/프로퍼티는 그대로 유지.
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `ColliderSizeMap`(BlockSize→Vector2)/`HpBarWidthMap`(BlockSize→float) static readonly Dictionary 추가, `ApplyBlockSize()` private 메서드 신규 추가 후 `OnSpawn()`/`ApplyData()` 양쪽에서 호출. `GetComponentInChildren<RectTransform>()`로 HP바 RectTransform을 찾아 `sizeDelta.x`만 갱신(y는 유지).
- `Assets/_Project/Scripts/Data/WaveTableData.cs` — 좌표 포함 `WaveEntry`/`MonsterSpawnEntry` 구조 완전 삭제, plan.md 스니펫 그대로 파라미터 구조(`_baseSpawnCount`/`_spawnCountPerWave`/`_baseTwoCellWeight`/`_twoCellWeightPerWave`/`_totalWaves` + MonsterData 4종 참조)로 전면 재작성.
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `_gridColumns=9`/`_gridRows=5` 필드 추가, `TotalWaves`를 `_waveTable.TotalWaves` 참조로 변경, `SpawnWave(int index)`를 9×5 그리드 랜덤 배치 + `bool[,] occupied` 점유 체크로 전면 재작성(신규 헬퍼 `GetFreeAnchors`/`MarkOccupied` 추가). **주의**: plan.md가 "수정 금지"로 지정한 `CheckWaveCleared()`/`AdvanceToNextWave()` 내부의 `_waveTable.WaveCount` 참조 2곳을 `_waveTable.TotalWaves`로 최소 치환함 — `WaveTableData.WaveCount` 프로퍼티가 STEP4에서 완전히 삭제되어 그대로 두면 컴파일 오류가 나기 때문(불가피한 최소 변경, 로직/구조는 미변경).

**신규 파일:**
- `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` — `MenuItem("PurpleCow/Setup/Monster Overhaul Setup")`. (1) MonsterData_Fluffy/Spider/StoneBug/ForestDeer.asset에 `_blockSize` 설정(Fluffy/Spider→OneByOne, StoneBug→TwoByOne, ForestDeer→OneByTwo), 2칸 몬스터는 `_hp`/`_reward` 상향. (2) WaveTableData.asset을 새 파라미터 구조로 생성/갱신(없으면 CreateAsset, 있으면 필드만 갱신). (3) 프리팹 4종에 `PrefabUtility.EditPrefabContentsScope`로 `BlockVisual` 자식(SpriteRenderer만, 콜라이더 없음, sortingOrder=0) 추가 + 캐릭터 SpriteRenderer sortingOrder=1로 조정 + 프리팹 BoxCollider2D 크기 사전 갱신 + `HpBarCanvas`를 `HpBarCanvas`가 이미 존재하는 위치에서 `BlockVisual`의 자식으로 재배치(로컬 Y 오프셋을 블록 정면 하단 근사값으로 조정). `MonsterSetupEditor.cs`는 전혀 수정하지 않음(읽기만 함).

**임의로 결정한 수치 (plan.md에 정확한 값 미명시 부분):**
- 2칸 몬스터(StoneBug/ForestDeer) 상향 스탯: `Hp=50`(1칸 기본값 30 대비), `Reward=18`(1칸 기본값 10 대비) — plan.md 제시 범위(Hp45~60/Reward15~20) 내에서 중간값 선택.
- HP바 폭 기준값: `MonsterBase.HpBarWidthMap` — OneByOne=1f, TwoByOne=2f(가로 2배), OneByTwo=1f(세로 2칸이지만 폭은 1칸 그대로) — 기존 `sizeDelta.x=1` 기준값 유지.
- 그리드 스폰 수 상한(`capacityLimit`): `(_gridColumns * _gridRows) / 2` = (9*5)/2 = 22 — plan.md가 예시로 제시한 값 그대로 채택.
- HP바 프리팹 초기 Y 오프셋(근사치, 런타임에 STEP2가 x폭만 재조정하고 y는 그대로 사용): 1칸/가로2칸(높이 0.96) 블록은 `-0.33`, 세로 2칸(ForestDeer, 높이 1.92) 블록은 `-0.81` — 블록 하단에서 살짝 띄운 근사값.
- 2칸 몬스터 중 가로(TwoByOne)/세로(OneByTwo) 선택 및 1칸 몬스터 중 Fluffy/Spider 선택은 각각 50:50 랜덤(`UnityEngine.Random.value < 0.5f`)으로 구현 — plan.md에 정확한 분배 비율 명시 없어 균등 분배로 결정.

**주요 결정사항:**
- `WaveManager.SpawnWave()`의 그리드 좌표→월드좌표 변환은 앵커(블록의 좌하단 셀) 좌표를 그대로 사용, 별도 중앙정렬 보정 없음 — plan.md가 명시한 `_spawnRoot.position + new Vector3(col*cellSize, row*cellSize, 0)` 형태를 그대로 따름.
- `Ball.cs`/`MonsterHpBar.cs`는 전혀 수정하지 않음 — 콜라이더가 여전히 루트에 있고 `MonsterHpBar.Start()`가 `GetComponentInParent<MonsterBase>()`를 쓰므로 `HpBarCanvas` 부모가 `BlockVisual`로 바뀌어도 문제없음을 확인.
- 기존 블록 스텁 4종(`Block_1x1/1x2/2x1/2x2.prefab`)은 삭제하지 않고 그대로 방치 — 스프라이트 에셋(`Block_*.png`)만 새 `BlockVisual` 자식에서 재사용.
- 블록 스프라이트 로드는 `AssetDatabase.LoadAssetAtPath<Sprite>` 대신 `AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()` 사용 — Multiple 모드 스프라이트(서브에셋 이름이 파일명과 다름, 예: `Block_1x1_0`)를 안전하게 로드하기 위함.
- 이 원격 환경엔 Unity 에디터가 없어 실제 컴파일/프리팹 GUI 편집/asset 재직렬화 검증 불가 — 문법/네이밍/직렬화 필드명(SerializedObject.FindProperty 대상 문자열) 일치 여부만 신중히 재확인함.
- git 커밋/푸시는 수행하지 않음 (오케스트레이터가 사용자 확인 후 처리 예정)

---

## 2026-07-05

### 작업: PrismPanel(융합 시스템 잔재) 제거 — UISetupEditor.cs + SampleScene.unity

**작업 내용:**
- task 문서 없이 사용자 명시적 승인으로 즉시 진행 (오케스트레이터 지시에 명시됨)
- 공식 스펙 PDF 구현 제외 항목(융합 시스템) 관련 빈 스텁 패널 `PrismPanel` 제거

**수정 파일:**
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs` — `Step3_SetupPanelCanvas()` 내 `panelNames` 배열에서 `"PrismPanel"` 항목 제거 (`{ "LevelUpPanel", "PausePanel", "BallLevelUpPanel", "PrismPanel" }` → `{ "LevelUpPanel", "PausePanel", "BallLevelUpPanel" }`). 나머지 3개 패널은 그대로 유지.
- `Assets/Scenes/SampleScene.unity` — YAML 텍스트 직접 편집으로 PrismPanel 관련 블록 3개 삭제:
  - `!u!1 &1731188296` GameObject 블록 (`m_Name: PrismPanel`)
  - `!u!224 &1731188297` RectTransform 블록 (m_Father: 849164776 = Canvas_Panel)
  - `!u!225 &1731188298` CanvasGroup 블록
  - 부모 `Canvas_Panel`의 RectTransform(`!u!224 &849164776`) `m_Children` 리스트에서 `- {fileID: 1731188297}` 참조 라인 1줄 제거 (나머지 3개 자식 LevelUpPanel/PausePanel/BallLevelUpPanel 참조는 유지)

**검증:**
- `grep -c "PrismPanel" SampleScene.unity` → 0
- fileID 1731188296/1731188297/1731188298 참조 → 파일 전체에서 매치 없음 (완전 제거 확인)
- `!u!1660057539 &9223372036854775807 SceneRoots`의 `- {fileID: 849164776}`는 Canvas_Panel 자체를 가리키는 무관한 참조이므로 건드리지 않음

**주요 결정사항:**
- Unity 에디터 없는 원격 환경이라 씬을 열어 직접 검증 불가 — YAML 텍스트 편집만 수행, fileID 참조 관계(부모 m_Children ↔ 자식 m_GameObject/m_Father)를 grep으로 교차 확인 후 진행
- LevelUpPanel/PausePanel/BallLevelUpPanel 및 다른 모든 GameObject/컴포넌트는 전혀 건드리지 않음
- git 커밋/푸시는 수행하지 않음 (오케스트레이터가 처리 예정)

---

## 2026-07-05

### 작업: 궤적 프리뷰 _hitRing 점선화 + 시계방향 회전 효과

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-07-05/11-20_trajectory-ring-dash-rotate/plan.md` (사용자 승인 완료)
- `TrajectoryPreview.cs` 단독 수정, `_hitRing`(2차 충돌 지점 레드닷을 감싸는 고리)을 실선 → 점선으로 교체하고 조준 여부와 무관하게 항상 시계방향으로 회전하는 효과 추가

**수정 파일:**
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`
  - `[SerializeField] private float _ringRotationSpeed = 90f;` 필드 추가 (deg/sec, Inspector 노출) — 4초에 한 바퀴 도는 속도로, 너무 느려 밋밋하지도 너무 빨라 어지럽지도 않은 값으로 임의 결정(plan.md의 "60f 전후 참고" 문구보다 살짝 빠르게 설정해 애니메이션이 눈에 띄도록 함)
  - `RING_DASH_COUNT = 10` 상수 추가 (고리 둘레에 배치할 점선 호 개수, plan.md가 제시한 8~12 범위의 중간값)
  - `CreateRingDashTexture()` 신설 — 궤적선의 `CreateDashTexture()`(4px, 50:50 비율)와 별도로 5px 텍스처(2px 불투명/3px 투명 = 40% 비율)를 사용해 궤적선보다 간격이 넓은 "짧은 호 + 넓은 갭" 형태로 차별화. `CreateDashTexture()` 재사용이 아닌 신규 메서드를 선택한 이유: research.md/plan.md가 궤적선과 고리의 점선 비율이 다를 때 더 자연스럽다고 명시했고, 레퍼런스 이미지(`targetUI/KakaoTalk_20260701_190324151_02.jpg`)의 고리가 궤적선보다 끊김이 더 뚜렷했기 때문
  - `CreateLineRenderer()` 시그니처에 `Vector2? textureScaleOverride = null` 선택적 파라미터 추가 — plan.md가 제시한 두 옵션(공용 헬퍼 파라미터 확장 vs `Awake()`에서 별도 재설정) 중 헬퍼 파라미터 확장을 선택(호출부 한 줄로 의도가 드러나고 `_trajectoryLine`/`_hitDot` 호출부는 그대로 유지되어 변경 범위가 더 작음). `Awake()`에서 `ringCircumference = 2π × _ringRadius`, `ringTextureScale = (RING_DASH_COUNT / ringCircumference, 1f)`로 계산해 전달 — 고리 둘레에 정확히 10개 점선이 이음새 없이 반복되도록 함(궤적선처럼 `DASH_WORLD_SIZE` 고정 스케일이 아닌, 고정 둘레 기준 정수 배 스케일 필요)
  - `DrawCircle()` 시그니처에 `float rotationOffsetDeg = 0f` 파라미터 추가(plan.md 옵션 a 채택, 별도 메서드 분리보다 중복 코드가 적음). `angle = (i / CIRCLE_SEGMENTS) * 2π - rotationOffsetDeg(rad)`로 각도에서 오프셋을 빼는 방식 채택 — Unity 2D 좌표계(Y+ 위쪽)에서 `(cos(angle), sin(angle))`은 angle 증가 시 반시계 방향으로 이동하므로(예: angle=0의 (r,0) 지점에서 angle이 조금 커지면 y가 양수가 되어 위쪽, 즉 반시계 방향), 시계방향으로 보이려면 시간에 따라 angle을 감소시켜야 함. 반대로 검증하면 angle을 시간에 따라 "빼면"(offsetRad가 시간에 비례해 증가) (r,0) 지점이 y가 음수 쪽(아래)으로 이동 → 시계 방향(3시 방향에서 6시 방향으로 이동)과 일치함을 확인
  - `UpdateTrajectory()`의 2차 충돌 분기에서 `float ringRotationOffsetDeg = Time.time * _ringRotationSpeed;` 계산 후 `DrawCircle(_hitRing, hit2.point, _ringRadius, ringRotationOffsetDeg)` 호출로 변경. `DrawCircle(_hitDot, hit2.point, _dotRadius)` 호출부는 그대로(기본값 0 사용)라 레드닷은 회전 미적용 유지
  - `Update()`가 이미 터치 여부와 무관하게 매 프레임 실행되는 기존 구조(2026-07-03 수정)를 그대로 활용 — 회전 관련 로직에 터치 상태 체크를 별도로 추가하지 않음
  - `CreateSolidTexture()` 위 주석을 "레드닷/원형 궤적선용" → "레드닷(_hitDot) 전용, 고리는 CreateRingDashTexture()로 분리됨"으로 정정(주석 정확성 유지 목적, 로직 변경 아님)

**주요 결정사항:**
- 텍스처 생성 방식: 신규 메서드(`CreateRingDashTexture()`) 신설 — `CreateDashTexture()` 재사용 대신 채택(이유는 위 수정 파일 항목 참고)
- `DrawCircle()` 확장 vs 분리: 시그니처 확장(옵션 a) 채택 — `_hitDot` 호출부는 그대로 유지되어 하위 호환, 코드 중복 없음
- `_ringRotationSpeed` 기본값: 90f(deg/sec, 약 4초/바퀴)
- 시계방향 부호 처리: `angle - Mathf.Deg2Rad * Time.time * _ringRotationSpeed` (research.md 예시와 동일한 부호)
- 점선 세그먼트 개수: `RING_DASH_COUNT = 10`, 비율은 40%(2px/5px) — 궤적선(50%)보다 간격이 넓어 시각적으로 구분됨
- `GameplayMechanics.md`/`UIRules.md` 문서 갱신은 이번 작업 범위 밖(별도 docs 에이전트 담당)이라 손대지 않음
- `Assets/Scenes/SampleScene.unity`는 수정하지 않음(신규 필드는 사용자 로컬에서 씬을 열 때 자동 직렬화됨)
- 이 원격 환경엔 Unity 에디터가 없어 실제 컴파일/시각 결과(점선 간격/회전 방향/속도 체감)는 검증 불가 — C# 문법과 좌표 기하학적 논리만 재확인함, 최종 시각 검증은 사용자 로컬 플레이 테스트 필요
- git 커밋/푸시는 수행하지 않음

---

## 2026-07-05

### 작업: TrajectoryPreview 고리(_hitRing) 점선 방식을 텍스처 타일링 → colorGradient로 교체 (QA 피드백 수정)

**배경:** 2026-07-05 11-20 trajectory-ring-dash-rotate task의 텍스처 타일링 기반 구현(`CreateRingDashTexture()` + `mainTextureScale.x = RING_DASH_COUNT / 둘레길이`)을 사용자가 로컬 Unity에서 실제 테스트한 결과 의도한 10개가 아니라 호가 2개만 보이는 것으로 확인됨. 텍스처 타일링 계산과 `LineRenderer.textureMode = Tile`의 실제 렌더링 결과가 어긋난 것으로 추정. 정확한 개수를 보장하는 `LineRenderer.colorGradient` 방식으로 교체.

**수정 파일 (`Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` 1개만):**
- `RING_DASH_COUNT` 상수 제거
- `CreateRingDashTexture()` 메서드 완전 제거(죽은 코드 방지)
- `Awake()`에서 `_hitRing` 생성 시 `CreateRingDashTexture()` + `ringTextureScale` 계산 로직 제거 → `CreateSolidTexture()`로 되돌림(레드닷과 동일), 생성 직후 `_hitRing.colorGradient = BuildRingDashGradient(_ringColor);` 할당
- `CreateLineRenderer()` 시그니처에서 `Vector2? textureScaleOverride = null` 파라미터 제거(호출부 3곳 모두 더 이상 필요 없어져 원래 시그니처로 복귀), `material.mainTextureScale`은 항상 `(1f / DASH_WORLD_SIZE, 1f)` 고정값 사용(이 부분은 기존 로직 그대로 유지, 이번 변경과 무관)
- `BuildRingDashGradient(Color ringColor)` static 메서드 신설 — `GradientColorKey` 2개(t=0/t=1, RGB 둘 다 ringColor), `GradientAlphaKey` 8개(피크 t=0/0.25/0.5/0.75는 alpha 1, 골 t=0.125/0.375/0.625/0.875는 alpha 0, t 오름차순 배치)로 `gradient.SetKeys()` 호출 후 반환. 정확히 8개 키(Gradient alphaKeys 최대치)로 4등분+4중간점이 딱 맞아떨어져 텍스처 타일링 계산 없이 항상 정확히 4개 호 보장
- `CreateSolidTexture()` 위 주석을 "레드닷/고리 공용, 고리의 점선 형태는 colorGradient로 표현"으로 갱신

**회전 로직과의 호환성 확인:**
- `DrawCircle()`의 `rotationOffsetDeg` 파라미터와 `Time.time * _ringRotationSpeed` 오프셋 로직은 변경하지 않음(요청사항대로) — `DrawCircle()`은 정점의 "월드 좌표 위치"를 회전시키는 반면, `colorGradient`는 LineRenderer 정점 "인덱스(순서, t=0~1)"를 기준으로 밝기를 매기므로 서로 독립적인 축. 정점이 회전해도 인덱스별 밝기 값은 인덱스에 고정된 채로 함께 회전하므로, 4개의 밝은 호가 고리 전체와 함께 계속 회전하는 것으로 동작할 것으로 판단(코드 변경 불필요, 그대로 유지)

**주요 결정사항:**
- `_ringColor`는 `[SerializeField]`라 런타임에 Inspector에서 바뀔 수 있지만, 기존 `startColor`/`endColor` 방식도 `Awake()` 시점 고정이었으므로 `BuildRingDashGradient()`도 동일한 수준(Awake 1회 계산)으로 유지 — 런타임 실시간 색상 반영은 범위 밖
- `_hitDot`(레드닷)은 이번 변경 대상에서 완전히 제외 — 계속 `CreateSolidTexture()` 사용, colorGradient 미적용
- `GameplayMechanics.md`/`UIRules.md`/`Assets/Scenes/SampleScene.unity`는 건드리지 않음(요청사항 명시)
- 이 원격 환경엔 Unity가 없어 실제 시각 결과(4개 호가 정확히 보이는지) 검증 불가 — C# 문법(`Gradient`/`GradientColorKey`/`GradientAlphaKey`는 `UnityEngine` 네임스페이스 포함, 별도 using 불필요)과 로직만 재확인함, 최종 시각 검증은 사용자 로컬 플레이 테스트 필요
- git 커밋/푸시는 수행하지 않음
