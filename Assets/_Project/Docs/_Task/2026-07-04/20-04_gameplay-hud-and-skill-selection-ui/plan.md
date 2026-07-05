# Plan — Gameplay HUD 진행도 개편 및 삼택지 스킬 선택 UI 개편

이 문서는 같은 task 폴더의 `research.md`에서 확정된 6가지 결정사항(및 이후 사용자 실플레이 확인으로 정정된 A-0 항목)을 바탕으로, HUD 진행도 표시(TopBar 스테이지 전체 누적 처치율 + `CharacterXpBar` 상단 재배치 2단 구조)와 삼택지 스킬 선택 카드(New! 라벨)를 실제로 구현하기 위한 단계별 계획입니다. **이 문서는 구현 전 계획 단계이며, 사용자의 명시적인 승인 없이는 dev 에이전트에 의한 실제 코드/에디터스크립트 수정을 시작하지 않습니다.** 승인 시 아래 단계를 그대로 dev 에이전트에게 위임할 예정입니다.

---

## 구현 목표

1. **HUD 진행도 2단 구조 도입**: 기존 텍스트 전용(`_waveText`/`_progressText`) HUD를, 레퍼런스(`targetUI/KakaoTalk_20260703_115951058_01/02/03.jpg`)와 동일한 TopBar(스테이지명 + 스테이지 전체 누적 처치율 % 진행바 + 장식용 보스 아이콘)/`CharacterXpBar`(기존 컴포넌트, 상단 재배치) 2단 구조로 개편한다.
2. **HUD 자체 추가 요소 제거**: 레퍼런스에 대응 요소가 없는 `_scoreText`(처치 수 텍스트)와 `_launchReadyIndicator`(발사 준비 인디케이터)를 `HUDPanel`/`UIManager`에서 완전히 제거한다.
3. **삼택지 카드 New! 라벨 추가**: 신규(미보유) 스킬 후보 카드에만 "New!" 라벨을 표시하도록 `SkillCardUI`/`SkillSelectionPanel`을 확장한다.
4. **에셋 없이 기능 우선 구현**: 진행바/배지/라벨은 전용 스프라이트 없이 Unity 기본 Image(Filled)/GameObject로 기능만 구현하고, 스타일링은 이번 범위에서 제외한다.

---

## 단계별 작업 계획

### A. HUD 진행도 파트

**A-0. 데이터 매핑 (정정 완료 — research.md "추가 정정" 참고)**

최초 계획은 "새 계산 로직 없이 기존 데이터 두 가지를 재사용"하는 것이었으나, 사용자의 실플레이 확인으로 다음과 같이 정정되었다.

- **TopBar % 진행바** = 스테이지 전체 누적 처치율. `(현재까지 누적 처치 수) / (스테이지 전체 몬스터 총수)`로 계산하며, 웨이브가 바뀌어도 리셋되지 않는다. 분자는 `WaveManager`의 기존 `_totalKillCount`를 재사용하되, 분모(스테이지 전체 몬스터 총수)와 "몬스터가 죽을 때마다 즉시 갱신되는 이벤트"는 신규로 추가해야 한다(아래 A-1b 참고). **이 부분은 새 계산 로직이 필요하다.**
- **상단 두 번째 바(경험치바)** = 별도의 "WaveBar" 개념은 없으며, 기존 `Assets/_Project/Scripts/UI/CharacterXpBar.cs`(`CharacterManager.OnXpChanged`/`OnLevelUp` 구독, 이미 완성되어 있음)를 그대로 재사용한다. 코드 수정은 필요 없고, 씬 하이어라키상 배치 위치만 기존 계획(하단)에서 TopBar 바로 아래(상단)로 옮긴다.

**A-1. `Assets/_Project/Scripts/UI/HUDPanel.cs` 수정**

1. 제거: `_scoreText` 필드, `HandleScoreChanged`/`UpdateScore` 메서드, `UIManager.OnScoreChanged` 구독/해제(`OnEnable`/`OnDisable`), `Start()`의 `UpdateScore(0)` 호출
2. 제거: `_launchReadyIndicator`/`_launchReadyCanvasGroup` 필드, `SetLaunchIndicatorVisible` 메서드, `HandleGameStateChanged` 메서드, `GameManager.OnGameStateChanged` 구독/해제(`OnEnable`/`OnDisable`), `Start()`의 `SetLaunchIndicatorVisible(...)` 호출, `HandleWaveStarted` 내부의 `SetLaunchIndicatorVisible(true)` 호출
3. 추가: `[SerializeField] private string _stageName = "1. 깊은 숲";` 필드와 `[SerializeField] private TMP_Text _stageNameText;` 필드. `Start()`에서 `_stageNameText.text = _stageName;`으로 1회 설정(스테이지 1개만 구현하는 PDF 스펙상 `WaveTableData` 구조 변경 없이 `HUDPanel` 자체 Inspector 필드로 충분)
4. 추가: `[SerializeField] private Image _stageProgressFillImage;`(Image Type = Filled/Horizontal). 기존처럼 `WaveManager.OnMonsterCountChanged`를 구독하는 대신, 아래 A-1b에서 `WaveManager.cs`에 신규 추가하는 `OnStageKillProgressChanged(int killedSoFar, int totalInStage)` 이벤트를 구독하여 `_stageProgressFillImage.fillAmount = (float)killedSoFar / totalInStage;`와 `_progressText.text = $"{percent}%"`를 함께 갱신한다.
5. `_waveText` 필드 자체는 더 이상 사용되지 않으므로 제거
6. 보스 아이콘(TopBar 우측 장식) 등 완전히 정적인 장식 오브젝트는 코드 필드를 추가하지 않고 A-3(UISetupEditor) 단계에서 오브젝트만 생성

**A-1b. `Assets/_Project/Scripts/Wave/WaveManager.cs` 수정 (신규)**

1. 게임 시작 시(`Awake()` 또는 `Start()`, `SpawnWave(0)` 호출 이전) 웨이브 0~(`TotalWaves`-1)까지 기존 스폰 공식(`Mathf.RoundToInt(_waveTable.BaseSpawnCount + _waveTable.SpawnCountPerWave * index)` 후 그리드 용량 상한 `Mathf.Min(spawnCount, capacityLimit)` 적용)을 그대로 반복 계산해 합산한 `_totalMonstersInStage`(private int)를 구한다. `SpawnWave(int index)` 내부의 기존 스폰 수 계산 로직과 공식이 어긋나면 % 총합이 부정확해지므로, 가능하면 스폰 수 계산 부분을 별도 private 메서드(예: `CalculatePlannedSpawnCount(int index)`)로 추출해 `SpawnWave()`와 신규 총수 계산 양쪽에서 재사용하는 방식을 권장한다(포뮬러 중복 방지).
2. 신규 이벤트 추가: `public static event Action<int, int> OnStageKillProgressChanged;` (누적 처치 수, 스테이지 전체 몬스터 총수)
3. 기존 `HandleMonsterDied` 메서드(`_totalKillCount` 증가 및 `OnKillCountReached` 처리하는 부분)에서, 처치 수 증가 직후 `OnStageKillProgressChanged?.Invoke(_totalKillCount, _totalMonstersInStage);`를 함께 발행한다.
4. 기존 `OnMonsterCountChanged`, `OnKillCountReached`, `_currentWaveTotalCount` 등 웨이브 단위 로직은 전혀 건드리지 않는다(다른 시스템이 이미 사용 중이므로 그대로 유지, 이번 신규 이벤트만 추가).

**A-2. `Assets/_Project/Scripts/UI/UIManager.cs` 수정**

1. HUDPanel이 더 이상 `OnScoreChanged`를 구독하지 않으므로, `_score` 필드, `Score` 프로퍼티, `OnScoreChanged` 이벤트, `HandleMonsterDied` 메서드, `MonsterBase.OnMonsterDied` 구독/해제(`OnEnable`/`OnDisable`)를 제거한다.
2. `HandleGameStateChanged`의 `GameState.Ready` 분기 내 `_score = 0;` 줄도 함께 제거한다.
3. **주의**: `MonsterBase.OnMonsterDied` 이벤트가 `UIManager` 외 다른 스크립트에서도 구독되고 있는지 제거 전 반드시 grep으로 재확인한다(아래 "주의사항" 참고).

**A-3. `Assets/_Project/Scripts/Editor/UISetupEditor.cs` 수정 (`Step9_SetupHUDPanelContent` 등)**

1. 기존 `ScoreText`, `LaunchReadyIndicator` 자식 오브젝트 생성 코드(`EnsureChildObject(hudPanelObj.transform, "ScoreText")`, `"LaunchReadyIndicator"` 관련 블록) 및 이에 대응하는 `SerializedObject` 참조 연결 코드(`so.FindProperty("_scoreText")` 등) 제거
2. `WaveText` 자식 오브젝트 생성 코드 및 `so.FindProperty("_waveText")` 참조 연결 코드 제거
3. 다음 신규 자식 오브젝트 생성 코드 추가: `StageNameText`(TMP_Text), `StageProgressFillImage`(Image, Filled/Horizontal, TopBar 하위), 보스 아이콘 장식용 오브젝트(정적 Image, TopBar 우측)
4. 위에서 생성한 각 오브젝트를 **생성하는 자리에서 곧바로** `HUDPanel`의 `SerializedObject`를 통해 `_stageNameText`, `_stageProgressFillImage` 필드에 참조 연결하는 코드까지 한 세트로 작성한다(AIFailures.md에 기록된 "자식 오브젝트 생성 후 SerializeField 참조 연결 누락" 실수 재발 방지, 아래 "주의사항" 참고)
5. `ApplyModifiedProperties()` 및 마지막 `EditorSceneManager.SaveScene()` 호출 유지(기존 관례)
6. 기존 `CharacterXP`(`CharacterXpBar` 부착된 오브젝트) 생성/배치 코드가 있다면 그 배치 위치를 하단이 아닌 TopBar 하위(바로 아래)로 수정한다. `CharacterXpBar.cs` 자체의 `[SerializeField] _slider`/`_levelText` 참조 연결 코드는 이미 있다면 그대로 유지하고 위치(부모 Transform, anchoredPosition)만 조정한다.

### B. 삼택지 카드 파트

**B-1. `Assets/_Project/Scripts/UI/SkillCardUI.cs` 수정**

1. 추가: `[SerializeField] private GameObject _newLabelObject;` 필드
2. `Setup(SkillData data, Action<SkillData> onSelected)` 시그니처를 `Setup(SkillData data, Action<SkillData> onSelected, bool isNew)`로 확장
3. `Setup` 내부에 `_newLabelObject.SetActive(isNew);` 호출 추가

**B-2. `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` 수정**

1. `BuildSkillCardPool()`의 반환 타입을 `List<SkillData>`에서 `(SkillData Data, bool IsNew)` 튜플(또는 이에 준하는 작은 구조체) 리스트로 변경
2. `IsNew` 판별 기준: 액티브면 `!activeSkillIds.Contains(data.SkillId)`, 패시브면 `!passiveSkillIds.Contains(data.SkillId)`(미보유 상태에서 후보에 오른 경우만 New, 업그레이드 후보는 New 아님) — 기존 `owned` 판별 변수를 그대로 재사용 가능
3. `ShowRandomSkills()`에서 `candidates.OrderBy(...).Take(3)` 이후 `_skillCards[i].Setup(candidates[i].Data, OnSkillSelected, candidates[i].IsNew)` 형태로 호출부 수정

**B-3. `Assets/_Project/Scripts/Editor/UISetupEditor.cs` 수정 (`Step6_CreateSkillCardPrefab` 등)**

1. `SkillCard.prefab` 내부에 New! 라벨 자식 오브젝트(TMP_Text, 예: "New!" 텍스트) 생성 코드 추가
2. 생성 직후 `SkillCardUI._newLabelObject`에 `SerializedObject`로 참조 연결하는 코드까지 한 세트로 작성(A-3과 동일한 원칙 적용)

---

## 예상 변경/생성 파일 목록

| 파일 | 종류 | 변경 내용 요약 |
|------|------|----------------|
| `Assets/_Project/Scripts/UI/HUDPanel.cs` | 수정 | `_scoreText`/`_launchReadyIndicator`/`_waveText` 관련 필드·메서드·구독 제거, `_stageName`/`_stageNameText`/`_stageProgressFillImage` 추가(`OnStageKillProgressChanged` 구독으로 갱신) |
| `Assets/_Project/Scripts/UI/UIManager.cs` | 수정 | `_score`/`Score`/`OnScoreChanged`/`HandleMonsterDied`/`MonsterBase.OnMonsterDied` 구독 제거 |
| `Assets/_Project/Scripts/UI/SkillCardUI.cs` | 수정 | `_newLabelObject` 필드 추가, `Setup()`에 `isNew` 매개변수 추가 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | 수정 | `BuildSkillCardPool()` 반환 타입을 `(SkillData, bool IsNew)` 리스트로 변경, `ShowRandomSkills()` 호출부 수정 |
| `Assets/_Project/Scripts/Editor/UISetupEditor.cs` | 수정 | HUD 관련 Step(`Step9_SetupHUDPanelContent` 등)에서 ScoreText/LaunchReadyIndicator/WaveText 생성 코드 제거, StageNameText/StageProgressFillImage/보스 아이콘 생성 및 참조 연결 코드 추가, `CharacterXP` 오브젝트 배치 위치를 TopBar 하위로 수정; SkillCard 관련 Step(`Step6_CreateSkillCardPrefab` 등)에서 New! 라벨 생성 및 참조 연결 코드 추가 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | 수정 | 스테이지 전체 몬스터 총수(`_totalMonstersInStage`) 사전 계산 로직 신규 추가, `OnStageKillProgressChanged(int, int)` 이벤트 신규 추가 및 `HandleMonsterDied`에서 발행 |

이번 계획에서는 신규 파일 생성이 없으며, 기존 6개 파일 수정만으로 범위가 한정됩니다.

---

## 주의사항

- **에셋 범위 제외**: 진행바 스프라이트, 배지 프레임, New! 라벨 디자인 등 전용 이미지 에셋은 이번 범위에 포함하지 않는다. Unity 기본 Image(Filled)/GameObject + 단색으로 기능만 구현하며, 스타일링은 이후 별도 task로 design 에이전트에게 위임한다.
- **명확히 제외된 항목**: 희귀도 다이아몬드, 다시 뽑기, 융합, Best! 추천 아이콘, 배속/Auto 버튼, 보스 기능은 research.md에서 이미 확정된 대로 이번 범위에서 완전히 제외한다.
- **스폰 공식 중복 위험**: `WaveManager.cs`의 스테이지 전체 몬스터 총수 계산이 `SpawnWave()`의 실제 스폰 로직과 공식이 어긋나면 TopBar %가 100%에 정확히 도달하지 못하거나 넘칠 수 있다. 가능하면 공식을 공용 메서드로 추출해 두 곳에서 재사용할 것을 권장하되, 기존 `SpawnWave()` 로직 자체의 동작 방식은 바꾸지 않는다(외과적 변경 원칙).
- **`CharacterXpBar`는 코드 수정 없음**: 이 컴포넌트는 이미 완성되어 있으므로 스크립트 변경 없이 씬 배치(부모/위치)만 조정한다. 로컬 Unity에서 `UISetupEditor` Setup 메뉴 재실행 시 이 재배치도 함께 반영되는지 확인이 필요하다.
- **SerializeField 참조 연결 누락 재발 방지**: `AIFailures.md`에 "자식 오브젝트는 생성하지만 SerializeField 참조 연결을 누락하는 패턴"이 반복 기록되어 있다. `UISetupEditor.cs` 수정 시 새 자식 오브젝트를 생성하는 코드와 해당 컴포넌트의 `[SerializeField]` 참조를 연결하는 `SerializedObject` 코드를 반드시 같은 자리에서 한 세트로 작성하고, 작업 완료 후 대상 컴포넌트의 전체 필드 목록과 대조 확인한다.
- **`MonsterBase.OnMonsterDied` 재확인**: `UIManager.cs`에서 이 이벤트 구독을 제거하기 전, 다른 스크립트에서 동일 이벤트를 구독하고 있지 않은지 dev 에이전트가 구현 단계에서 grep으로 한 번 더 확인한다. 스코어 집계 외 다른 용도로 쓰이고 있다면 제거 범위를 재조정해야 한다.
- **씬 반영은 로컬 Unity 필요**: 이 프로젝트는 원격 개발 환경에 Unity 에디터가 없어, 코드/에디터스크립트 수정만으로는 `SampleScene.unity`에 자동으로 반영되지 않는다. 사용자가 로컬 Unity에서 `UISetupEditor`의 해당 Setup 메뉴를 재실행해야 씬에 완전히 반영된다.
- **구현 승인 절차**: 이 plan.md는 계획 문서이며, 사용자의 명시적인 승인이 있어야 dev 에이전트가 위 단계별 작업을 실제로 시작한다.
