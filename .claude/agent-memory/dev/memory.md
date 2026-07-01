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
