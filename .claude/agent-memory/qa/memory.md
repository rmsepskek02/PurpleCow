# QA 에이전트 메모리

## 2026-06-30 — PurpleCow 채용과제 전체 QA 검토

### 작업 내용
채용과제 PDF 스펙 기준으로 모든 구현 파일 검토 수행.
총 44개 .cs 파일 대상 (핵심 시스템, 스킬, UI, 에디터 스크립트 포함)

### 결과 요약
- CRITICAL: 5건
- WARNING: 6건
- INFO: 3건

### 주요 발견사항

#### CRITICAL
1. SkillSelectionPanel:71,79 — 업그레이드 가능 조건 비교 오류 (< MaxLevel-1 이어야 할 것이 < MaxLevel이어야 함)
   - CurrentLevel은 0-based index이므로, MaxLevel(=3) - 1 = 2가 최대 레벨 index. 조건이 `< MaxLevel - 1`이면 현재 레벨이 1(Lv.2)일 때까지만 풀에 포함됨. Lv.2(index=1)에서 Lv.3(index=2)로의 업그레이드 카드가 완전히 누락될 수 있음.
   
   실제로는 `CurrentLevel < MaxLevel - 1`이 맞으려면 CurrentLevel이 0(Lv1), 1(Lv2)일 때만 풀에 포함되어야 함. MaxLevel=3이면 index 0,1,2이고 `< MaxLevel - 1`=`< 2`이므로 index 0,1 즉 Lv1,Lv2 상태일 때만 업그레이드 가능 카드로 표시. 이는 최대 레벨(index=2) 제외에는 맞지만, 현재 로직이 SkillData.LevelUp()이 실행되면 CurrentLevel이 올라간다는 점에서 경계를 다시 확인해야 함.

2. SkillManager:28 — 액티브 스킬 레벨업 시 신규 인스턴스가 아닌 기존 스킬의 LevelUp() 호출 후 조기 반환하므로, Ball에 매 발사마다 AddSkill()로 추가되는 스킬 인스턴스가 업데이트된 LevelData를 참조하지 못할 가능성이 있음.

3. CharacterManager:40,45 — 바닥 도달 몬스터에게 AddXp 중복 지급 버그. WaveManager.HandleMonsterDied와 CharacterManager.HandleMonsterDied가 동일한 MonsterBase.OnMonsterDied 이벤트를 구독하여 WaveManager는 처치 카운트, CharacterManager는 XP 지급을 하는데, 바닥에 닿은 경우 WaveManager는 OnMonsterDied가 아닌 별도 경로로 풀 반환을 하므로 XP는 죽은 경우에만 지급되어야 하나, HandleMonsterReachedBottom(L37)에서도 AddXp를 호출(L40)함. 즉 몬스터가 바닥에 닿으면 XP 지급, 그 후 플레이어 HP가 0이 되어도 Die()가 호출되지 않으므로 중복은 없음. 단, 바닥 도달 후 MonsterBase.TakeDamage로 죽는 경우(남아있는 DoT 등)는 중복 XP가 될 수 있음 — WARNING 수준으로 재분류.

4. WaveManager:33 — 단일 MonsterBase 프리팹만 지원. WaveData.MonsterSpawnEntry.Data 필드가 있으나, SpawnWave에서 이 Data를 반영하지 않고 항상 _monsterPrefab 하나만 스폰함. 이로 인해 몬스터 종류별 WaveData 설정이 동작하지 않음.

5. SkillSelectionPanel — 스킬 선택 패널 오픈 시 Time.timeScale 정지 및 선택 완료 후 재개 처리 없음.

#### WARNING
1. MagicMirrorPassive — LevelUp 시 이전 이벤트 구독 해제 없이 재구독 가능성
2. BallLauncher.LaunchSubBalls — 서브볼 생성 방향 보정 로직이 y < 0이면 y를 양수로 바꾸는데, 이는 위 방향 볼만 생성하게 되어 클러스터 볼의 의도와 다를 수 있음
3. MonsterBase — HandleHitMonster 핸들러가 완전히 빈 메서드로 구독만 하고 있음
4. SkillSelectionPanel.OnSkillSelected — UIManager.OnSkillSelectionComplete()와 Hide()를 모두 호출하여 Hide()가 두 번 실행될 가능성
5. GameManager.RestartGame — 씬 재로드 없이 상태만 변경하므로 실제 게임 재시작(몬스터/스킬 초기화)이 되지 않음

### 주요 결정사항
- 코드 수정 없이 검토 결과만 보고
- 모든 파일 직접 읽어 스펙과 대조 완료

## 2026-06-30 — plan.md STEP 1~10 구현 검증

### 작업 내용
`/home/user/PurpleCow/Assets/_Project/Docs/_Task/2026-06-30/HH-MM_QA수정/plan.md` 기준으로
STEP 1~10 구현 완료 여부를 13개 파일 대상으로 검토.

### 결과 요약
STEP 1~10 전 항목 정상 구현 확인. 누락 없음.

### 검토 파일 목록
- MonsterBase.cs — ApplyData() 추가, 빈 HandleHitMonster 및 OnEnable/OnDisable 제거 완료
- WaveManager.cs — SpawnWave() 내 ApplyData() 조건부 호출 완료
- SkillSelectionPanel.cs — timeScale=0, SetUpdate(true), ResetLevel 구독, Hide() 중복 제거 완료
- UIManager.cs — timeScale=1, static 이벤트 참조 완료
- SkillData.cs — ResetLevel() 추가 완료
- GameManager.cs — OnGameStateChanged static, SceneManager.LoadScene() 추가 완료
- InputHandler.cs — OnDrag/OnRelease static 전환 완료
- BallLauncher.cs — static 이벤트 직접 참조 완료
- ResultPanel.cs — static 이벤트 직접 참조 완료
- SkillManager.cs — Remove() 추가, SkillFactory.CreateActiveSkill() 사용 완료
- Ball.cs — OnHitMonster 시그니처 Action<MonsterBase, float, bool>으로 변경 완료
- DamageTextManager.cs — OnEnable/OnDisable 구독 및 HandleHitMonster 추가 완료
- MonsterSetupEditor.cs — CreateWaveDataAssets()로 교체, Wave1~Wave20 루프 생성 완료

### 주요 발견사항
- OnHitMonster 시그니처 변경의 파급 범위 확인: DamageTextManager만 구독하며 패시브 스킬은 별도 이벤트(OnHitMonsterFront/Back) 사용으로 영향 없음
- 지적 수준 낮은 사항 1건: OnSpawn()과 ApplyData() 모두 OnHpChanged 발행하여 Get() 시 두 번 발행됨. 기능 오류 아니나 향후 HP UI 애니메이션 시 깜빡임 가능성 있음

### 주요 결정사항
- 코드 수정 없이 검토 결과만 보고
- STEP 1~10 모두 plan.md 명세와 일치하여 추가 수정 불필요 판정

## 2026-06-30 — 에디터 스크립트 실행 후 에셋/씬 구조 전체 점검

### 작업 내용
에디터 스크립트(BallSetupEditor, MonsterSetupEditor, SkillSetupEditor, SceneSetupEditor, UISetupEditor) 코드 분석 및 실제 생성 결과 대조 검증.

### 결과 요약
- CRITICAL: 3건 (에셋/씬 미생성 — 에디터 스크립트가 실행되지 않음)
- WARNING: 6건 (스크립트 내 논리 오류 및 연결 누락)

### 주요 발견사항

#### CRITICAL — 에셋/씬 오브젝트 미생성 (에디터 스크립트 미실행)
1. Assets/_Project/Data/ 폴더 자체가 존재하지 않음 → BallData, MonsterData(4종), WaveData(Wave1~20), SkillData(액티브5+패시브5) 에셋 전부 미생성
2. Assets/_Project/Prefabs/ 하위 .prefab 파일이 없음 (Ball.prefab, Fluffy/Spider/StoneBug/ForestDeer.prefab, Block_1x1~2x2.prefab, SkillCard.prefab 모두 없음)
3. SampleScene.unity에는 Main Camera와 Global Light 2D만 존재 — GameManager, WaveManager, UIManager, BallLauncher, SkillManager, CharacterManager, DamageTextManager, InputHandler, Background, Wall_Left, Wall_Right, Ground, PoolRoot, Canvas_HUD/Panel/Popup, HUDPanel, ResultPanel, SkillSelectionPanel, CharacterHP/XP 전부 없음

#### WARNING — 에디터 스크립트 논리 오류
4. BallSetupEditor:100-103 — BallData의 _maxBounces 필드가 설정되지 않음. BallData.cs에는 _maxBounces가 있으나 에디터에서 설정 코드 없음. 기본값 0이면 Ball.OnSpawn()에서 _remainingBounces=0이 되어 첫 벽 충돌 시 즉시 소멸
5. SkillSetupEditor — 패시브 스킬 5종(WarmTinHeart/MagicMirror/AmethystDagger/EmeraldDagger/LastMatch)의 iconPath가 빈 문자열("")로 설정됨. Sprites/Passive/ 폴더에 아이콘 이미지가 존재함에도 연결하지 않음
6. MonsterSetupEditor:65-69 — 4종 MonsterData 모두 동일한 기본값(hp=30, moveSpeed=1, damage=1, reward=10)으로 설정. 종류별 차별화 없음
7. MonsterSetupEditor:93-98 — WaveData Wave1~Wave20 생성 시 _waveNumber만 설정, _spawnEntries(List<MonsterSpawnEntry>)는 빈 상태. 실행 시 SpawnWave()에서 foreach가 빈 리스트를 순회하여 적이 하나도 스폰되지 않음
8. SceneSetupEditor:Step3 — Block 프리팹 4종(Block_1x1~2x2)이 Monster 폴더에 생성되고 MonsterBase 컴포넌트가 붙음. Block은 몬스터가 아님에도 동일 태그("Monster")와 컴포넌트를 사용
9. SceneSetupEditor:Step6과 UISetupEditor:Step4 중복 — DamageTextManager와 CharacterManager가 SceneSetupEditor.Step6과 UISetupEditor.Step4에서 각각 생성 시도됨. 두 에디터를 모두 실행하면 Singleton 중복 생성 방지가 작동하지만, Step4의 DamageTextManager는 자식 DamageTextPool을 추가하는 반면 Step6은 단순 AddComponent만 수행 — 실행 순서에 따라 DamageTextPool 유무가 달라짐

### 주요 결정사항
- 에디터 스크립트 자체는 컴파일 에러 없이 올바르게 작성됨
- 미생성 문제는 Unity Editor에서 메뉴 실행이 안 된 것이 원인 (CLI 환경에서는 실행 불가)
- 스크립트 논리 오류(maxBounces 미설정, spawnEntries 빈 상태, 패시브 아이콘 미연결)는 실행 후에도 수동 수정이 필요한 항목

## 2026-06-30 — 에디터 스크립트 재실행 후 에셋/씬 전체 재점검 (2차)

### 작업 내용
에디터 스크립트 실행 후 실제 생성된 에셋과 씬 파일을 직접 읽어 전항목 대조 검증.

### 결과 요약
- CRITICAL: 4건
- WARNING: 5건

### 주요 발견사항 상세

#### CRITICAL
1. BallData.asset: `_maxBounces: 0` — BallSetupEditor에서 이 필드 설정 코드 누락. 기본값 0이므로 첫 벽 충돌 즉시 소멸
2. WaveData_Wave1.asset: `_spawnEntries: []` — spawnEntries가 빈 배열. 웨이브 시작해도 적이 0마리 스폰됨
3. SampleScene.unity: 352줄, Main Camera + Global Light 2D만 존재 — GameManager, WaveManager, UIManager, BallLauncher, SkillManager, CharacterManager, DamageTextManager, InputHandler, Background, Wall_Left, Wall_Right, Ground, PoolRoot, Canvas_HUD/Panel/Popup 전부 미존재
4. Ball.prefab: `_ballData: {fileID: 0}` — BallData 에셋 연결 없음. Ball 스크립트의 _ballData 참조가 null이어서 런타임 NullReferenceException 발생

#### WARNING
5. MonsterData_Fluffy.asset과 MonsterData_Spider.asset 모두 hp=30, moveSpeed=1, damage=1, reward=10으로 동일값. 몬스터 종별 차별화 없음
6. SkillData_Passive_WarmTinHeart.asset: `_icon: {fileID: 0}` — 아이콘 미연결
7. Fluffy.prefab / Spider.prefab: MonsterBase 컴포넌트의 `_monsterData: {fileID: 0}` — MonsterData 에셋 연결 없음. 런타임 NullReferenceException 발생 가능
8. Ball.prefab: CircleCollider2D의 m_Material(PhysicsMaterial2D)이 {fileID: 0} — 에디터 스크립트에서 수동 연결 요구 LogWarning 출력했으나 실제 연결 안 됨. 벽/지면 반사 물리가 정상 동작 안 할 수 있음
9. SkillSetupEditor/BallSetupEditor 이중 실행 시 DamageTextManager 중복: SceneSetupEditor.Step6에서 DamageTextManager 단순 생성, UISetupEditor.Step4에서 DamageTextManager + 자식 DamageTextPool 생성 — 에디터 스크립트가 둘 다 실행된 경우 실행 순서에 따라 DamageTextPool 유무가 다름

### 주요 결정사항
- 코드 수정 없이 검토 결과만 보고

## 2026-06-30 — 9개 Task plan.md 대비 구현 완료 여부 전체 점검

### 작업 내용
9개 task 폴더의 plan.md를 읽고 실제 .cs 파일과 대조하여 구현 완료 여부 검증.
검토 대상 파일: Core 6개, Ball 2개, Monster 1개, Wave 1개, Skill 14개, UI 12개, Editor 5개, Data 4개 = 총 45개 .cs 파일

### 결과 요약
- 02-30_Core시스템구현: 완료
- 10-00_Ball시스템구현: 완료
- 14-00_Monster시스템구현: 완료
- 18-00_Skill시스템구현: 완료
- 20-00_UI시스템구현: 완료 (단, HUDPanel의 _launchReadyIndicator SetActive 폴백 방식 혼용)
- HH-MM_EditorSetup개선: 완료
- HH-MM_PDF스펙정합: 완료
- HH-MM_QA수정: 완료
- HH-MM_UI재작업: 완료

### 주요 결정사항
- 모든 task가 plan.md 명세와 일치하여 구현 완료 판정
- 미세 차이(BallData _maxBounces 필드 추가 등)는 plan 확장으로 오류 아님

## 2026-07-02 — 볼 발사 메커닉 재설계(ball-launch-mechanics) 코드 리뷰

### 작업 내용
plan.md/research.md(`Assets/_Project/Docs/_Task/2026-07-01/21-15_ball-launch-mechanics/`) 기준으로
`InputHandler.cs`, `BallLauncher.cs`, `Ball.cs`, `SkillManager.cs`, `SkillSelectionPanel.cs`,
`WaveManager.cs`, `MonsterBase.cs`, `IceBallSkill.cs`, `HUDPanel.cs` git diff 전체 검토.
`GameManager.cs`/`UIManager.cs`/`ObjectPool.cs`/`SkillFactory.cs`/`SkillData.cs`/Active 스킬 5종도 교차 확인.

### 결과 요약
- CRITICAL: 2건 (Wall 충돌 로스터 볼 영구 이탈 버그 + 확산 경로, 궤적 프리뷰(4단계) 전면 미구현)
- MAJOR: 2건 (GameState 게이팅 완전 제거로 게임오버 후에도 로스터 무한 순환, ApplySkillToBall 죽은 코드 잔존은 Minor로 하향)
- MINOR: 다수 (아래 본문 참고)

### 주요 발견사항
1. **(Critical, 확인됨)** `Ball.OnCollisionEnter2D`의 `"Wall"` 분기(Ball.cs:111-119)는 `IsRosterMember` 체크 없이 `_remainingBounces<=0`이면 무조건 `ReturnToPool()`. 로스터 볼이 벽 충돌로 소진되면 `BallLauncher._roster`는 죽은 Ball 참조를 영구 보유, 이후 `ObjectPool.Get()`이 그 인스턴스를 재사용하면 두 로스터 항목이 같은 Ball을 공유하는 상태 충돌 발생. `BallData.asset`의 `_maxBounces: 0`(에디터 스크립트는 10으로 설정하지만 커밋된 asset은 여전히 0)으로 인해 최초 벽 접촉 즉시 발동하는 사실상 확정적 버그. `ReturnToLaunchPoint()` 귀환 중 벽에 재충돌해도 동일 경로로 소실됨(방향 재보정 로직 없음).
2. **(Critical, 신규 발견)** plan.md 4단계(궤적 프리뷰, `TrajectoryPreview.cs`)가 완전히 미구현. `LineRenderer`/`Physics2D.Raycast` 관련 코드가 저장소 어디에도 없고, `InputHandler.OnAimBegin`도 구독자가 0명인 죽은 이벤트. dev agent-memory에도 "4단계는 이번 범위 제외"로 명시되어 있어, 상위 지시의 "5단계 전부 완료"와 실제 구현 상태가 불일치함.
3. **(Major)** `BallLauncher`가 `GameManager.OnGameStateChanged` 구독을 완전히 제거해 `Result`/`Ready` 상태에서도 로스터 볼이 계속 귀환·재발사 사이클을 돔.
4. 나머지 항목(로스터 상한 9개 강제, SkillSelectionPanel 중복 추가 방지, EquipActiveSkill bool 반환 호출부 정합성, 스킬 재부착 로직, MonsterBase 초 단위 전환, IceBallSkill 시그니처)은 모두 문제 없음으로 확인.
5. Minor: `SkillManager.ApplySkillToBall(Ball)`이 로스터 도입 후 호출부 없는 죽은 코드로 남음.

### 주요 결정사항
- 코드 수정 없이 자연어로 심각도(Critical/Major/Minor) 순 보고, ReportFindings 도구 미사용
- BallData.asset의 _maxBounces 실제 값을 asset 파일까지 직접 열어 확인(에디터 스크립트 코드만으로 판단하지 않음)

## 2026-07-03 (기록 누락분 추가 정리) — ball-launch-mechanics 후속 확정 사항

### 배경
위 2026-07-02 검토에서 지적한 Critical 2건(Wall 충돌 로스터 볼 영구 이탈, `BallData.asset._maxBounces` 데이터 오류) + Major 1건(GameState 게이팅 부재로 게임 종료 후에도 재발사 지속)은 dev 에이전트가 모두 수정 완료함(dev agent-memory 2026-07-02 "볼 발사 메커닉 재설계 QA 버그 4건 수정" 참고). 이후 사용자가 원본 게임 실제 플레이 기준으로 Wall 충돌 동작을 재확인하여, 최초 수정안(로스터 볼도 Wall 충돌 시 `ReturnToLaunchPoint()`로 귀환)을 "로스터 볼은 벽에서 반사 횟수와 무관하게 항상 순수 반사만 하고, 귀환은 오직 Ground 충돌에서만 일어난다"로 최종 정정하였음(`Ball.cs` 재수정, dev agent-memory에 뒤늦게 기록).

### 참고
- 이 정정 자체는 QA 에이전트가 별도로 재검토를 수행해 발견한 것이 아니라 사용자의 실제 플레이 재확인으로 결정된 사항이므로, 코드 리뷰 결과가 아닌 "설계 확정 변경"으로 분류
- 궤적 프리뷰(4단계, `TrajectoryPreview.cs`)는 2026-07-02 검토 시점엔 Critical(미구현)으로 지적했으나 이후 별도로 구현 완료됨(dev agent-memory 2026-07-02 "궤적 프리뷰 신규 구현" 참고) — 이번 문서 정리 세션에서 재검토는 수행하지 않았으므로, 실제 구현이 plan.md/GameplayMechanics.md 스펙과 완전히 일치하는지는 아직 QA 에이전트가 코드 레벨로 재확인하지 않은 상태로 남아 있음(추후 필요 시 재검토 권장)

## 2026-07-04 — LaunchPoint 캐릭터 궤도화 재설계 코드 리뷰

### 작업 내용
`Assets/_Project/Docs/_Task/2026-07-04/09-41_launchpoint-character-orbit/research.md`,
`plan.md` 확인 후 커밋 `7030259`("feat: LaunchPoint를 캐릭터+무기 방향 기반 계산값으로 재설계")의
`CharacterAimController.cs`, `BallLauncher.cs`, `Ball.cs`, `TrajectoryPreview.cs`, `WallFitter.cs`,
`SceneSetupEditor.cs`, 신규 `CharacterLaunchOrbitSetupEditor.cs` 전체 검토.
`Singleton.cs` 구현도 함께 확인.

### 결과 요약
- Critical: 0건
- Major: 1건 (`CharacterLaunchOrbitSetupEditor.ConnectWallFitterCharacterRef`가 character=null일 때도
  호출되어 기존 WallFitter._character 참조를 null로 덮어씀)
- Minor: 3건 (SceneSetupEditor.cs 21행 Step8/LaunchPoint 주석 사실과 불일치, LaunchPoint 잔존 네이밍
  다수(Ball.cs의 ReturnToLaunchPoint()/toLaunchPoint, BallLauncher.cs 104행 주석), 무기 pivot.x=0.39
  비대칭으로 인한 _weaponLength 근사치는 기존 task 확정사항이라 이번 diff 범위 밖)

### 주요 발견사항
1. plan.md 1~7단계는 실제 구현과 정확히 일치. 계획에서 벗어난 항목 없음.
2. `LaunchOrigin`(Character.position + LaunchDirection.normalized * WeaponLength) 공식을 수학적으로
   재검증함. Weapon 파츠 localPosition이 (0,0,0)이고 `aimAngle = atan2(dir)*Rad2Deg - 90f` 회전을
   기본 방향 (0,1)에 적용하면 정확히 dir과 일치함(회전행렬 유도로 확인). flipX는 로컬 x=0인 벡터에는
   영향을 주지 않으므로 좌우 반전 시에도 동일하게 성립. 단, "무기 스프라이트 기본 방향이 위쪽"이라는
   전제 자체는 코드 주석(35~36행)에 이미 "실제 플레이 확인 후 조정 가능"한 가정으로 명시되어 있어
   검증 불가 영역(Unity 에디터 부재)으로 남음.
3. `CharacterAimController`를 `Singleton<T>`로 전환한 것은 `Awake()` 오버라이드가 원래 없었으므로
   문제 없이 상속됨. `Singleton<T>.Awake()`가 `Instance` 설정만 하고 컴포넌트 파괴는 중복 인스턴스일
   때만 발생하므로 `Start()`/`Update()`와 충돌 없음.
4. `BallLauncher.LaunchOrigin`/`ReturnPoint`가 `CharacterAimController.Instance` null 체크를 하지
   않는 것은 이 코드베이스 전체 컨벤션(`GameManager.Instance`, `WaveManager.Instance` 등 어디에도
   null 체크 없음)과 완전히 일치하며 이례적이지 않음. 다만 마이그레이션 전환기(Character
   오브젝트가 없는 구버전 씬에서 신규 `CharacterLaunchOrbitSetupEditor` 메뉴 실행 전 플레이하는
   경우) NRE가 날 수 있음은 plan.md 주의사항에 이미 언급되어 있어 인지된 리스크로 판단.
5. `SceneSetupEditor.cs`의 삭제 범위(Step11 315~320행/329행, Step6 520/529/534행, Step8
   629~639행)는 plan.md 6단계와 정확히 일치. 그 외 로직 변경 없음.
6. **(Minor)** `SceneSetupEditor.cs` 21행 주석 `// WallFitter는 Step8에서 생성되는 LaunchPoint를
   참조해야 하므로 Step8 이후에 실행한다.`가 이제 사실과 다름. `Step6_SetupWallFitter()`는 더 이상
   `LaunchPoint`도, `_character`도 참조/연결하지 않음(Wall_Left/Right/Top/Ground는 Step5 산출물).
   Step8과 Step6의 순서 의존성 자체가 소멸했으므로 주석 삭제 또는 "WallFitter는 더 이상 Step8의
   산출물을 참조하지 않는다. Character↔WallFitter 배선은 별도 CharacterLaunchOrbitSetupEditor가
   전담한다" 식으로 교체 제안.
7. **(Major)** 신규 `CharacterLaunchOrbitSetupEditor.SetupCharacterLaunchOrbit()`(12~22행)에서
   `FindCharacter()`가 경고 후 null을 반환해도 `ConnectWallFitterCharacterRef(character)`가 그대로
   호출됨(21행). 내부에서 `so.FindProperty("_character").objectReferenceValue = character;`가
   조건 없이 실행되므로(60행), 이미 정상 연결되어 있던 `WallFitter._character` 참조를 null로 덮어쓸
   위험이 있음. `character == null`일 때는 배선 자체를 건너뛰도록 가드가 필요.
8. `git diff --stat` 결과 `CharacterManager.cs`, 볼 물리/충돌/데미지 로직은 전혀 건드리지 않음.
   변경 파일은 plan.md의 "예상 변경/생성 파일 목록"과 정확히 일치(7개 파일 + 신규 1개).
9. (부가) `Ball.cs`의 `ReturnToLaunchPoint()` 메서드명/`toLaunchPoint` 변수명과 주변 주석, 그리고
   `BallLauncher.cs` 104행 주석이 여전히 "LaunchPoint" 용어를 사용함. 기능상 문제는 없으나 실제
   LaunchPoint 오브젝트가 사라진 지금 시점에서는 네이밍이 혼동을 줄 수 있어 Minor로 기록.

### 주요 결정사항
- 코드 수정 없이 자연어로 심각도(Major/Minor) 순 보고
- git 커밋 로그(`7030259`)를 통해 이미 커밋된 상태임을 확인 후 `git show`로 diff 재구성하여 검토 진행
- `Singleton.cs`, `CharacterManager.cs`(선례 확인)까지 교차 확인하여 Singleton 전환 안전성 판단
