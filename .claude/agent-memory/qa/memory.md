# QA 에이전트 메모리

## 2026-07-06 - 패시브 스킬 5종 구현 점검

### 작업 내용
사용자 요청에 따라 패시브 스킬 5종(따뜻한 양철 심장, 마법 거울, 자수정 단검, 에메랄드 단검, 마지막 성냥)의 구현 상태를 PDF 공식 스펙(`PurpleCow_클라이언트_채용과제.pdf`), `ProjectHistory.md`(2026-07-06 `10-03_skill-effects-progression`), `_Task/2026-07-06/10-03_skill-effects-progression/plan.md`와 대조 검증함. 코드 범위: `Assets/_Project/Scripts/Skill/Passive/*.cs`, `Skill/Base/PassiveSkillBase.cs`, `Skill/SkillManager.cs`, `Skill/SkillFactory.cs`, `Data/SkillData.cs`, `Ball/Ball.cs`, `Monster/MonsterBase.cs`, `UI/SkillSelectionPanel.cs`, `UI/SkillCardUI.cs`, `UI/SkillSlotGroup.cs`, `UI/SkillSlotIcon.cs`, 데이터 에셋 `Data/SkillData_Passive_*.asset`.

### 결과
5종 모두 정상 구현으로 판단됨(코드 수정 없이 조사만 수행).
- 따뜻한 양철 심장(WarmTinHeartPassive): 노멀 볼 한정 20/30/40% 데미지 배율, `Ball.CalculateDamage()`에서 `!_isSpecialBall` 조건으로 정확히 게이팅됨.
- 마법 거울(MagicMirrorPassive): `Ball.OnWallHit` 이벤트 구독, 볼 개별 인스턴스에 `_nextHitDamageMultiplier` 저장 후 다음 타격에 20/40/60% 소비, 스폰/귀환 시 초기화됨.
- 자수정 단검(AmethystDaggerPassive)/에메랄드 단검(EmeraldDaggerPassive): 전면(velocity.y<0)/후면 판정 시 치명타 확률 +10/20/30%, +20/30/40%를 해당 타격 즉시 계산에 반영(과거 "다음 타격 몬스터 저장" 방식에서 교체됨, ProjectHistory 기록과 일치).
- 마지막 성냥(LastMatchPassive): `MonsterBase.OnMonsterDied` 구독, 사망 시점 위치 기준 반경(Value2=1.5, PDF 미명시 커스텀값) 내 생존 몬스터에게 10/20/30 피해.
- 레벨업 시 `SkillManager.AddPassiveSkill()`이 Remove→TryLevelUp→Apply 순서로 재적용해 이전 레벨 보너스 제거 후 새 레벨 보너스만 남도록 정확히 처리됨(중복/누락 없음).
- 데이터 에셋(SkillData_Passive_*.asset) 5종 모두 PDF 수치와 정확히 일치, 0으로 비어있는 값 없음.
- 보유 최대 2개 제한(`SkillManager.CanEquipPassive`), 동일 카드 중복 노출 방지, 만렙 시 후보 제외 로직 모두 스펙과 일치.

### 주요 발견/의심 사항(치명적 버그 아님, 참고용)
1. 전면/후면 판정이 볼의 수직 속도 부호(`vel.y < 0`)에만 의존 — 원본 게임 화면 기준 정확한 전후면 정의인지 시각적으로 재검증 필요(수평 이동 중 충돌 시 항상 "후면"으로 판정되는 엣지케이스 존재).
2. 삼택지 카드 UI(`SkillCardUI.cs`)는 액티브 스킬만 레벨별 수치(`BallDamage`)를 텍스트로 노출하고, 패시브는 정적 설명 문구만 표시해 레벨 1/2/3의 실제 수치(%) 차이를 카드에서 확인할 수 없음. 로직 자체는 정확히 레벨별로 다르게 동작하므로 기능 버그는 아니고 UX 완성도 이슈.
3. 마지막 성냥의 폭발 반경 1.5는 PDF에 명시되지 않은 자체 설계값(합리적이나 스펙 외 임의값).

### 결정사항
파일 수정 없음(QA 역할 규칙 준수, 순수 조사·보고만 수행). 위 발견사항은 오케스트레이터에게 자연어로 보고함.

---

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
