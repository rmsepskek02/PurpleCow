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
