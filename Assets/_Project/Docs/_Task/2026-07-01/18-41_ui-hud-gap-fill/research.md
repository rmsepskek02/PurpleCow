# Research — UI HUD Gap Fill

`UIRules.md`를 정리하는 과정에서 규칙 문서에는 이미 명시되었지만 실제 코드에는 아직 구현되지 않은 UI 갭 4가지(캐릭터 HP바 숫자 텍스트, 스킬 카드 데미지 수치, Active/Passive 슬롯 시각 UI, 스테이지 진행률 % 표시)를 조사한 문서입니다. 각 항목에 대해 규칙 문서상 근거와 현재 스크립트 상태를 대조하여 정확한 차이(gap)를 확인했습니다. 이번 문서는 조사만을 목적으로 하며, 구현 계획(plan.md)은 포함하지 않습니다.

---

## 현재 상태

프로젝트의 UI 관련 스크립트는 `Assets/_Project/Scripts/UI/` 아래에 다음과 같이 존재합니다.

```
Assets/_Project/Scripts/UI/
  CharacterHpBar.cs
  CharacterXpBar.cs
  SkillSelectionPanel.cs
  SkillCardUI.cs
  MonsterHpBar.cs
  ResultPanel.cs
  UIButton.cs
  SafeAreaFitter.cs
  HUDPanel.cs
  DamageTextManager.cs
  DamageTextFx.cs
  UIManager.cs
```

이 중 아래 4개 항목은 `UIRules.md`에 규칙으로는 명시되어 있으나, 대응하는 스크립트를 실제로 열어본 결과 코드 구현이 없거나 일부만 되어 있음을 확인했습니다.

---

## 관련 파일 및 의존성

### 1. 캐릭터 HP바 숫자 텍스트

- 규칙 근거: `UIRules.md` Section 10 — "HUD: `CharacterHpBar` (MonoBehaviour, Slider + TMP_Text 현재/최대 HP 숫자 표시), `CharacterHP` 오브젝트에 부착"
- `Assets/_Project/Scripts/UI/CharacterHpBar.cs` (전체 15줄)
  - `[SerializeField] private Slider _slider;` 필드만 존재
  - `CharacterManager.OnHpChanged += UpdateHp` 구독, `UpdateHp(int current, int max)`에서 `_slider.value`만 갱신
  - TMP_Text 필드 및 "현재/최대" 형식의 텍스트 갱신 로직 없음
- `Assets/_Project/Scripts/Core/CharacterManager.cs`
  - `public static event Action<int, int> OnHpChanged;` (현재, 최대) — 이벤트 시그니처는 이미 규칙과 일치하며 `TakeDamage()`에서 `OnHpChanged?.Invoke(_currentHp, _maxHp)`로 발행됨
  - 즉 이벤트 페이로드(현재/최대 HP 정수값)는 이미 제공되고 있어, `CharacterHpBar` 쪽에서 이를 받아 텍스트로 표시하기만 하면 됨

### 2. 스킬 카드 데미지 수치 표시

- 규칙 근거: `UIRules.md`에 스킬 카드 데미지 텍스트에 대한 별도 section은 없으나, Section 11에서 "스킬 카드의 'Best!' 추천 아이콘"은 제외한다고 명시하는 등 스킬 카드 UI 구성 요소를 다루고 있어 데미지 수치 표시는 `SkillData.CurrentLevelData.BallDamage` 필드의 존재로 볼 때 의도된 구현 대상으로 판단됨 (사용자 확인 사항으로 결론에 별도 명시)
- `Assets/_Project/Scripts/UI/SkillCardUI.cs` (전체 51줄)
  - 필드: `_iconImage`, `_nameText`, `_descriptionText`, `_typeText`, `_selectButton`, `_canvasGroup`
  - `Setup(SkillData data, Action<SkillData> onSelected)`에서 아이콘/이름/설명/타입("액티브"/"패시브")만 세팅
  - 데미지 수치를 표시할 TMP_Text 필드 및 관련 세팅 로직 없음
- `Assets/_Project/Scripts/Data/SkillData.cs`
  - `SkillLevelData` 구조체에 `public float BallDamage;` 필드 존재
  - `SkillData.CurrentLevelData` 프로퍼티로 `GetLevelData(_currentLevel)` 결과 접근 가능 → `CurrentLevelData.BallDamage`로 현재 레벨의 데미지 값을 바로 읽을 수 있음
  - 즉 데이터 소스는 이미 준비되어 있고, `SkillCardUI` 쪽에 텍스트 필드와 `Setup()` 내 세팅 로직만 추가하면 되는 상태

### 3. Active 4 / Passive 2 슬롯 시각 UI

- 규칙 근거: `UIRules.md`에 슬롯 UI 자체를 다루는 전용 section은 없으나, Canvas 구조(Section 1)의 HUD 구성 요소들과 연계되는 항목이며, 개수 제한 로직이 이미 존재하는 만큼 사용자와의 논의에서 "화면에 슬롯이 몇 칸 찼는지 보여주는 UI"가 필요하다고 확인된 항목
- `Assets/_Project/Scripts/Skill/SkillManager.cs`
  - `public bool CanEquipActive => _activeSkills.Count < 4;`
  - `public bool CanEquipPassive => _passiveSkills.Count < 2;`
  - `public IReadOnlyList<int> ActiveSkillIds => _activeSkills.ConvertAll(s => s.SkillData.SkillId);`
  - `public IReadOnlyList<int> PassiveSkillIds => _passiveSkills.ConvertAll(s => s.SkillData.SkillId);`
  - `public static event Action<List<BallSkillBase>> OnActiveSkillsChanged;`
  - `public static event Action<List<PassiveSkillBase>> OnPassiveSkillsChanged;`
  - 개수 제한(4/2) 로직과 변경 시 이벤트 발행은 이미 구현되어 있음
- `Assets/_Project/Scripts/UI/` 전체 Glob/Grep 결과, "Slot"이라는 이름을 가진 스크립트나 슬롯 아이콘을 나열/표시하는 컴포넌트는 존재하지 않음 (`CharacterHpBar`, `CharacterXpBar`, `SkillSelectionPanel`, `SkillCardUI`, `MonsterHpBar`, `ResultPanel`, `UIButton`, `SafeAreaFitter`, `HUDPanel`, `DamageTextManager`, `DamageTextFx`, `UIManager` 중 슬롯 관련 항목 없음)
  - `OnActiveSkillsChanged` / `OnPassiveSkillsChanged` 이벤트를 구독하는 UI 컴포넌트도 코드베이스에 없음 (구독자 없이 이벤트만 발행되는 상태)
  - 즉 내부 로직(개수 제한, 장착 목록)은 완비되어 있으나, 이를 화면에 "칸" 형태로 시각화하는 UI 컴포넌트/프리팹은 전혀 존재하지 않음

### 4. 스테이지 진행률(%) 표시 UI

- 규칙 근거: `UIRules.md` Section 1 — `TopBar (스테이지명, 몬스터 HP바, %, 아이콘*)`. 각주에서 "진행률 바(%) 자체는 정상 구현 대상"이라고 명시
- `Assets/_Project/Scripts/UI/HUDPanel.cs` (전체 99줄)
  - 필드: `_waveText`, `_scoreText`, `_launchReadyIndicator`, `_launchReadyCanvasGroup`, `_canvasGroup` 등
  - `HandleWaveStarted(int waveNumber)`에서 `_waveText.text = $"WAVE {waveNumber} / {_totalWaves}"`로 웨이브 번호만 텍스트로 표시 (이는 `WaveBar`의 "웨이브 번호 배지"에 해당하는 것으로 보이며, `TopBar`의 %와는 다른 요소)
  - `TopBar`에 해당하는 "%" 진행률을 계산하거나 표시하는 필드/로직 전혀 없음
- `Assets/_Project/Scripts/Wave/WaveManager.cs`
  - `private List<MonsterBase> _activeMonsters = new List<MonsterBase>();` — 현재 웨이브에 남아있는 몬스터 목록 보유 (private, 외부 노출 프로퍼티 없음)
  - `SpawnWave(index)` 시점에 `waveData.SpawnEntries.Count`만큼 몬스터가 추가되므로, 해당 웨이브의 "초기 스폰 수"는 `WaveData.SpawnEntries.Count`(`Assets/_Project/Scripts/Data/WaveData.cs`)로 구할 수 있음
  - 몬스터가 죽거나(`HandleMonsterDied`) 바닥에 닿으면(`CheckGameOver` 내 `OnMonsterReachedBottom`) `_activeMonsters`에서 제거됨 → "처치+통과된 수" 또는 "남은 수"를 매 변화 시점에 알 수 있는 지점은 이미 존재하나, 이를 외부에 이벤트로 알리는 기능은 없음
  - 진행률(%) 계산에 필요한 이벤트(`OnMonsterCountChanged` 등 (현재 남은 수, 초기 수) 형태)가 현재 정의되어 있지 않음 — `OnWaveStarted`, `OnWaveCleared`, `OnAllWavesCleared`, `OnKillCountReached`, `OnMonsterReachedBottom`만 존재하며, 어느 것도 진행률 계산에 바로 쓸 수 있는 페이로드(남은 수/전체 수)를 제공하지 않음

---

## 문제점 / 구현 대상 파악

| 항목 | 규칙 문서 근거 | 코드 상태 | Gap |
|---|---|---|---|
| 1. 캐릭터 HP바 숫자 | UIRules.md §10 | `CharacterHpBar`에 Slider만 있고 TMP_Text 없음, `OnHpChanged` 이벤트는 이미 (현재,최대) 페이로드 제공 | TMP_Text 필드 + 텍스트 갱신 로직 추가 필요 |
| 2. 스킬 카드 데미지 수치 | (스킬 카드 관련 규칙, `SkillData.BallDamage` 필드로 확인) | `SkillCardUI.Setup()`에 데미지 텍스트 필드/세팅 없음, `SkillData.CurrentLevelData.BallDamage`는 이미 존재 | TMP_Text 필드 + `Setup()` 내 데미지 텍스트 세팅 로직 추가 필요 |
| 3. Active 4 / Passive 2 슬롯 시각 UI | (Canvas 구조 및 논의 확인 사항) | `SkillManager`에 개수 제한/이벤트는 있으나 구독하는 UI 컴포넌트가 전무 | 신규 UI 컴포넌트(슬롯 아이콘 나열) + `OnActiveSkillsChanged`/`OnPassiveSkillsChanged` 구독 로직 신규 작성 필요 |
| 4. 스테이지 진행률(%) 표시 | UIRules.md §1 TopBar 각주 | `HUDPanel`에 % 표시 로직 없음, `WaveManager._activeMonsters`는 private이며 진행률 계산용 이벤트 없음 | `WaveManager`에 진행률 계산 가능한 이벤트(남은 수/전체 수) 신규 추가 + `HUDPanel`(또는 별도 컴포넌트)에 % 텍스트/바 표시 로직 추가 필요 |

---

## 결론

### 항목별 수정 대상 정리

**1. 캐릭터 HP바 숫자 텍스트**
- 수정 대상 파일: `Assets/_Project/Scripts/UI/CharacterHpBar.cs`
- 필요한 필드/메서드: `[SerializeField] private TMP_Text _hpText;` 추가, `UpdateHp(int current, int max)` 내부에서 `_hpText.text = $"{current} / {max}"` 형태로 갱신
- 관련 이벤트 연결 지점: `CharacterManager.OnHpChanged` (기존 그대로 사용, 시그니처 변경 불필요)

**2. 스킬 카드 데미지 수치 표시**
- 수정 대상 파일: `Assets/_Project/Scripts/UI/SkillCardUI.cs`
- 필요한 필드/메서드: `[SerializeField] private TMP_Text _damageText;` 추가, `Setup(SkillData data, ...)` 내부에서 `_damageText.text = data.CurrentLevelData.BallDamage.ToString(...)` 형태로 세팅
- 관련 이벤트 연결 지점: 별도 이벤트 불필요 (`Setup()` 호출 시점에 `SkillData.CurrentLevelData.BallDamage`를 즉시 읽어 표시)
- 참고: Passive 스킬은 데미지 개념이 없을 수 있으므로(`SkillType.Passive`), 데미지 텍스트를 Active/Passive에 따라 표시 여부를 분기할지 여부는 plan.md 단계에서 결정 필요

**3. Active 4 / Passive 2 슬롯 시각 UI**
- 수정 대상 파일(신규): `Assets/_Project/Scripts/UI/` 아래 신규 컴포넌트 (예: `SkillSlotPanel.cs` 등, 정확한 명칭은 plan.md에서 결정)
- 필요한 필드/메서드: 슬롯 4칸(Active)/2칸(Passive)을 표현할 아이콘 UI 요소 배열, `SkillManager.OnActiveSkillsChanged` / `OnPassiveSkillsChanged` 구독 후 `ActiveSkillIds` / `PassiveSkillIds`(또는 이벤트로 전달되는 리스트)를 기반으로 슬롯 채움 상태 갱신
- 관련 이벤트 연결 지점: `SkillManager.OnActiveSkillsChanged`, `SkillManager.OnPassiveSkillsChanged` (기존 이벤트 그대로 사용 가능, 신규 이벤트 불필요)
- 참고: 이 UI를 씬 어디에 배치할지(Canvas_HUD 내 위치)는 UIRules.md Section 1의 Canvas 구조에 별도 언급이 없어 plan.md 단계에서 배치 위치를 논의 필요

**4. 스테이지 진행률(%) 표시 UI**
- 수정 대상 파일: `Assets/_Project/Scripts/Wave/WaveManager.cs`, `Assets/_Project/Scripts/UI/HUDPanel.cs` (또는 TopBar 전용 신규 컴포넌트)
- 필요한 필드/메서드:
  - `WaveManager`: 현재 웨이브의 초기 스폰 수(`WaveData.SpawnEntries.Count`)와 `_activeMonsters.Count`(잔여 수)를 기반으로 진행률을 계산할 수 있는 신규 이벤트(예: `public static event Action<int, int> OnMonsterCountChanged` — (남은 수, 전체 수)) 추가 필요. `SpawnWave()`, `HandleMonsterDied()`, `CheckGameOver()` 내 몬스터 수 변화 지점에서 발행
  - `HUDPanel`(또는 TopBar 전용 신규 스크립트): % 텍스트/이미지(Fill Amount 등) 필드 추가, 위 이벤트를 구독해 `(전체 - 남은) / 전체 * 100` 형태로 갱신
- 관련 이벤트 연결 지점: `WaveManager`에 신규 이벤트 추가 필요 (기존 `OnWaveStarted`, `OnMonsterReachedBottom` 등만으로는 진행률 계산에 필요한 실시간 잔여 수 정보 부족)
- 참고: `TopBar`의 "몬스터 HP바"는 이미 `MonsterHpBar.cs`(개별 몬스터 World Space)로 구현되어 있으나, `TopBar`에 표시되는 것이 개별 몬스터 HP바인지 웨이브 전체 진행률인지는 UIRules.md 문구만으로는 다소 모호하여 plan.md 단계에서 사용자와 재확인 필요

### 항목 간 의존성

- 4개 항목은 기본적으로 서로 독립적이다. 각각 다른 파일(`CharacterHpBar`, `SkillCardUI`, 신규 슬롯 UI, `WaveManager`+`HUDPanel`)을 다루며 서로를 참조하지 않으므로 병렬로 진행 가능하다.
- 다만 항목 4(진행률 %)는 `WaveManager`에 신규 이벤트를 추가하는 작업이 선행되어야 UI 쪽 구현이 가능하므로, "이벤트 추가 → UI 구독" 순서 의존성이 항목 내부에 존재한다(다른 항목과는 무관).
- 항목 3(슬롯 UI)은 신규 컴포넌트 작성이 필요해 나머지 항목보다 설계 논의(배치 위치, 슬롯 개수 표현 방식)가 더 필요할 수 있다.
