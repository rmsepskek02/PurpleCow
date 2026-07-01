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

## 2026-07-01 원본 게임 스크린샷 vs 코드베이스 대조 검증 (7항목)

### 작업 내용
원본 게임(통통 디펜스: 핀볼 마스터) 플레이 스크린샷 4장에서 파악된 UI/게임플레이 요소 7가지를 현재 코드베이스와 대조 검증. PDF 명시 제외 항목(튜토리얼/배속/1스테이지 보스/자동조준/선택지 다시뽑기/융합 시스템)은 검증 대상에서 제외.

### 결과 요약
1. 킬카운트 배지(카운트다운): 미구현. `WaveManager.cs`는 `_totalKillCount`(누적) 증가 후 `_totalKillCount % _killCountForSkill == 0`(L118)일 때 이벤트만 발생시킴 — "남은 수" 계산 로직 없음. `HUDPanel.cs` L64는 `_scoreText.text = "처치: {score}"`로 누적 처치 수 표시만 함. 카운트다운 배지 UI 자체가 존재하지 않음.
2. 플레이어 HP 시스템: 구현되어 있고 스크린샷과 구조적으로 일치. `WaveManager.OnMonsterReachedBottom` → `CharacterManager.cs` L37 `HandleMonsterReachedBottom` → L39 `TakeDamage` → L51 `OnHpChanged` 이벤트 → `CharacterHpBar.cs` L8/L11 `UpdateHp`가 Slider.value 갱신. 단, HP 숫자 텍스트(230/258/300 형태) 표시 필드는 `CharacterHpBar.cs`에 없음(Slider만 존재) — 부분 불일치.
3. 스테이지 진행률 %/스테이지 테마명: 미구현. 프로젝트 전체 검색 결과 "진행률/Progress/StageName" 관련 코드 없음. `HUDPanel.cs` L48은 "WAVE n / total" 텍스트만 존재.
4. 데미지 숫자 팝업: 구현되어 있고 일치. `Ball.cs` L135 `OnHitMonster?.Invoke` → `DamageTextManager.cs` L19/L27 구독 → `ShowDamage` → `DamageTextFx.cs` L32-37 `Play()`에서 흰색(`_normalColor = Color.white`) 텍스트로 표시. 크리티컬 시 노란색/확대.
5. 스킬 카드 데미지 수치: 미구현. `SkillCardUI.cs` L28-36 `Setup()`은 아이콘/이름/설명/타입만 표시, 데미지 텍스트 필드 없음. `SkillData.cs` L56 `CurrentLevelData.BallDamage` 필드가 존재하므로 연결 가능.
6. Active 4슬롯/Passive 2슬롯 시각 UI: 미구현. `SkillManager.cs` L29/L34/L40/L46에 내부 제한 로직(`CanEquipActive`, `CanEquipPassive`, Count>=4/2)은 존재하나, `Assets/_Project/Scripts/UI` 내 슬롯 아이콘 표시 UI 파일 자체가 없음(Slot 관련 파일 검색 결과 없음).
7. 투사체 탄도 예측선: 미구현. `InputHandler.cs`(드래그 방향 계산만, L51), `BallLauncher.cs`(방향 저장만, L38-41)에 LineRenderer/궤적 미리보기 로직 전혀 없음. 자동조준과는 무관한 별개의 "드래그 중 궤적 프리뷰" 기능이며 PDF 제외 항목에 해당하지 않으므로 구현 검토 대상.

### 주요 결정사항
- 파일 수정 없이 자연어 보고만 수행함(QA 에이전트 책임 범위 준수).
- PDF 제외 항목(튜토리얼/배속/1스테이지 보스/자동조준/선택지 다시뽑기/융합)은 코드 확인도 하지 않음.
