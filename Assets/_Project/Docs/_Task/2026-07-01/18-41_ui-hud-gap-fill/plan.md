# Plan — UI HUD Gap Fill

이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.

이 문서는 `research.md`에서 조사한 4가지 UI 구현 갭(캐릭터 HP바 숫자, 스킬 카드 데미지 텍스트, Active/Passive 슬롯 시각 UI, 스테이지 진행률 % 표시)을 실제로 채우기 위한 구현 계획입니다. research.md 작성 이후 사용자와의 논의를 거쳐 확정된 3가지 결정사항(스킬 카드 데미지는 Active 전용 표시, `UIRules.md`의 "몬스터 HP바" 오기 수정, Active/Passive 슬롯은 신규 HUD가 아닌 기존 `SkillSelectionPanel` 내부에 구현)을 모두 반영했습니다. 각 항목마다 정확한 파일 경로, 추가할 필드/메서드/이벤트 시그니처, 기존 코드와의 연결 지점을 명시합니다.

---

## 구현 목표

1. `CharacterHpBar`에 TMP_Text를 추가해 `현재 / 최대` 형식의 HP 숫자를 표시한다.
2. `SkillCardUI`에 데미지 텍스트를 추가하되, `SkillType.Active`일 때만 표시하고 Passive는 데미지 텍스트를 숨긴다.
3. 새 HUD 컴포넌트를 만들지 않고, 기존 `SkillSelectionPanel`(레벨업 팝업) 내부에 Active 4칸 / Passive 2칸 슬롯 UI를 추가해 현재 장착된 스킬 아이콘과 `x{레벨}` 배지를, 빈 슬롯은 검은 사각형으로 표시한다.
4. `WaveManager`에 진행률 계산용 신규 이벤트를 추가하고, `HUDPanel`에 `%` 텍스트/바 표시 로직을 추가한다.
5. (문서 정리) `UIRules.md` Section 1의 TopBar 항목에서 "몬스터 HP바" 문구를 제거한다.

---

## 단계별 작업 계획

### 1. 캐릭터 HP바 숫자 텍스트 — `CharacterHpBar.cs`

- 대상 파일: `Assets/_Project/Scripts/UI/CharacterHpBar.cs`
- 현재 코드(전체 15줄):
  ```csharp
  public class CharacterHpBar : MonoBehaviour
  {
      [SerializeField] private Slider _slider;

      private void OnEnable()  => CharacterManager.OnHpChanged += UpdateHp;
      private void OnDisable() => CharacterManager.OnHpChanged -= UpdateHp;

      private void UpdateHp(int current, int max)
      {
          _slider.value = max > 0 ? (float)current / max : 0f;
      }
  }
  ```
- 변경 내용:
  - `using TMPro;` 추가
  - 필드 추가: `[SerializeField] private TMP_Text _hpText;`
  - `UpdateHp(int current, int max)` 내부에 `_hpText.text = $"{current} / {max}";` 라인 추가 (기존 `_slider.value` 갱신 로직 아래에 추가)
- 이벤트 연결: `CharacterManager.OnHpChanged` (기존 그대로 사용, `public static event Action<int, int> OnHpChanged`, 시그니처 변경 없음)
- Inspector 작업: `CharacterHP` 오브젝트 하위에 TMP_Text 오브젝트를 새로 만들거나 기존 자식 오브젝트를 연결해 `_hpText` 필드에 할당해야 함 (씬/프리팹 수정은 코드 작업과 별개로 Unity 에디터에서 수동 연결 필요 — 이 문서에서는 스크립트 변경만 다룸)

### 2. 스킬 카드 데미지 텍스트 — `SkillCardUI.cs` (Active 전용, 결정 1 반영)

- 대상 파일: `Assets/_Project/Scripts/UI/SkillCardUI.cs`
- 현재 `Setup()`:
  ```csharp
  public void Setup(SkillData data, Action<SkillData> onSelected)
  {
      _currentData          = data;
      _onSelected           = onSelected;
      _iconImage.sprite     = data.Icon;
      _nameText.text        = data.SkillName;
      _descriptionText.text = data.Description;
      _typeText.text        = data.SkillType == SkillType.Active ? "액티브" : "패시브";
  }
  ```
- 변경 내용:
  - 필드 추가: `[SerializeField] private TMP_Text _damageText;`
  - `Setup()` 마지막에 아래 분기 추가:
    ```csharp
    bool isActive = data.SkillType == SkillType.Active;
    if (_damageText != null)
    {
        _damageText.gameObject.SetActive(isActive);
        if (isActive)
            _damageText.text = data.CurrentLevelData.BallDamage.ToString("0");
    }
    ```
  - 데미지 값 포맷(정수 표시 `"0"`)은 레퍼런스 스크린샷의 "💥27" 형태(정수 배지)를 기준으로 함. 소수점 표시가 필요하면 추후 포맷 문자열만 조정하면 됨.
- 데이터 소스: `SkillData.CurrentLevelData.BallDamage` (이미 존재, `Assets/_Project/Scripts/Data/SkillData.cs`) — 별도 이벤트 불필요, `Setup()` 호출 시점에 즉시 읽어 표시
- Inspector 작업: `SkillCardUI` 프리팹에 데미지 표시용 TMP_Text 오브젝트(배지 형태, 아이콘 등)를 추가하고 `_damageText`에 연결 필요 (프리팹 수정은 Unity 에디터 작업, 이 문서는 스크립트 변경만 다룸)

### 3. Active 4 / Passive 2 슬롯 UI — `SkillSelectionPanel.cs` 내부 구현 (결정 3 반영)

research.md에서는 신규 HUD 컴포넌트를 제안했으나, 사용자 확인 결과 레퍼런스 스크린샷상 슬롯은 레벨업 팝업(`SkillSelectionPanel`) 안에 "Active Skill" / "Passive Skill" 라벨과 함께 표시됨. 따라서 새 HUD를 만들지 않고 `SkillSelectionPanel`이 관리하는 하위 슬롯 표시용 컴포넌트를 신규로 추가한다.

- 신규 파일: `Assets/_Project/Scripts/UI/SkillSlotIcon.cs`
  - 슬롯 1칸을 표현하는 작은 컴포넌트. 필드:
    ```csharp
    [SerializeField] private Image    _iconImage;   // 채워졌을 때 스킬 아이콘
    [SerializeField] private TMP_Text _levelText;   // "x{N}" 배지
    [SerializeField] private GameObject _filledRoot; // 아이콘+배지 묶음 표시 여부
    [SerializeField] private GameObject _emptyRoot;  // 빈 슬롯(검은 사각형) 표시 여부
    ```
  - 메서드:
    - `public void SetFilled(Sprite icon, int level)` — `_filledRoot.SetActive(true)`, `_emptyRoot.SetActive(false)`, `_iconImage.sprite = icon`, `_levelText.text = $"x{level}"`
    - `public void SetEmpty()` — `_filledRoot.SetActive(false)`, `_emptyRoot.SetActive(true)`
  - `x{N}` 값은 `SkillData.CurrentLevel`을 기준으로 하되, Inspector에서 표시 시 사람이 읽기 좋은 1-based 값으로 보정할지(`CurrentLevel + 1`) 여부는 "주의사항"에 재확인 필요 항목으로 남김.

- 신규 파일: `Assets/_Project/Scripts/UI/SkillSlotGroup.cs`
  - Active 슬롯 4개 또는 Passive 슬롯 2개를 하나의 그룹으로 묶어 갱신하는 컴포넌트. `SkillSelectionPanel`이 두 개(Active용/Passive용)를 각각 참조.
  - 필드:
    ```csharp
    [SerializeField] private SkillSlotIcon[] _slots;
    ```
  - 메서드:
    ```csharp
    public void UpdateActiveSlots(List<BallSkillBase> skills)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < skills.Count)
                _slots[i].SetFilled(skills[i].SkillData.Icon, skills[i].SkillData.CurrentLevel);
            else
                _slots[i].SetEmpty();
        }
    }

    public void UpdatePassiveSlots(List<PassiveSkillBase> skills)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < skills.Count)
                _slots[i].SetFilled(skills[i].SkillData.Icon, skills[i].SkillData.CurrentLevel);
            else
                _slots[i].SetEmpty();
        }
    }
    ```
    (Active/Passive용으로 별도 메서드를 두되 내부 로직은 동일 패턴이며, 리스트 타입만 다름. 두 메서드 중 실제로 어느 그룹 인스턴스가 어느 메서드를 쓰는지는 `SkillSelectionPanel`이 결정)

- 수정 파일: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`
  - 필드 추가:
    ```csharp
    [SerializeField] private SkillSlotGroup _activeSlotGroup;
    [SerializeField] private SkillSlotGroup _passiveSlotGroup;
    ```
  - `OnEnable()`/`OnDisable()`에 아래 구독/해제 추가:
    ```csharp
    SkillManager.OnActiveSkillsChanged  += HandleActiveSkillsChanged;
    SkillManager.OnPassiveSkillsChanged += HandlePassiveSkillsChanged;
    ```
  - 신규 핸들러 메서드:
    ```csharp
    private void HandleActiveSkillsChanged(List<BallSkillBase> skills)
        => _activeSlotGroup.UpdateActiveSlots(skills);

    private void HandlePassiveSkillsChanged(List<PassiveSkillBase> skills)
        => _passiveSlotGroup.UpdatePassiveSlots(skills);
    ```
  - 패널이 열릴 때(`OpenPanel()` 또는 `Show()`) 최초 1회 현재 상태로 슬롯을 갱신할 필요가 있음 — `SkillManager.Instance`에 현재 장착 리스트를 직접 얻어올 수 있는 프로퍼티가 `ActiveSkillIds`/`PassiveSkillIds`(`IReadOnlyList<int>`, ID만 제공)뿐이므로, 아이콘까지 표시하려면 `SkillManager`에 `List<BallSkillBase>`/`List<PassiveSkillBase>`를 그대로 반환하는 프로퍼티가 추가로 필요할 수 있음. 이 부분은 "주의사항"에 별도 명시.
- 이벤트 연결: `SkillManager.OnActiveSkillsChanged` (`Action<List<BallSkillBase>>`), `SkillManager.OnPassiveSkillsChanged` (`Action<List<PassiveSkillBase>>`) — 기존 이벤트 그대로 사용, 신규 이벤트 불필요
- Inspector 작업: `SkillSelectionPanel` 프리팹에 "Active Skill" 라벨 + 4개 슬롯, "Passive Skill" 라벨 + 2개 슬롯 UI 배치 및 `_activeSlotGroup`/`_passiveSlotGroup` 필드 연결 필요 (Unity 에디터 작업)

### 4. 스테이지 진행률(%) 표시 — `WaveManager.cs` + `HUDPanel.cs`

- 대상 파일 1: `Assets/_Project/Scripts/Wave/WaveManager.cs`
  - 이벤트 추가:
    ```csharp
    public static event Action<int, int> OnMonsterCountChanged; // (남은 수, 전체 수)
    ```
  - 발행 지점:
    - `SpawnWave(int index)` 마지막, `OnWaveStarted?.Invoke(waveData.WaveNumber);` 직전 또는 직후에 `OnMonsterCountChanged?.Invoke(_activeMonsters.Count, waveData.SpawnEntries.Count);` 추가 (웨이브 시작 시 100% 잔여 상태를 알림)
    - `HandleMonsterDied(MonsterBase monster)` 내, `_totalKillCount++;` 이후에 현재 웨이브의 전체 스폰 수를 알아야 하므로, 전체 수를 저장해 둘 private 필드 `_currentWaveTotalCount`를 추가하고 `SpawnWave()`에서 `_currentWaveTotalCount = waveData.SpawnEntries.Count;`로 세팅. `HandleMonsterDied()`에서 `OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);` 호출
    - `CheckGameOver()` 내 `_activeMonsters.RemoveAt(i);` 이후에도 동일하게 `OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);` 호출 (몬스터가 바닥에 닿아 사라지는 경우도 잔여 수 갱신 대상)
  - 신규 private 필드: `private int _currentWaveTotalCount;`
- 대상 파일 2: `Assets/_Project/Scripts/UI/HUDPanel.cs`
  - 필드 추가: `[SerializeField] private TMP_Text _progressText;` (또는 `Image _progressFillImage;` 병행 가능, 우선 텍스트 기준으로 계획)
  - `OnEnable()`/`OnDisable()`에 구독/해제 추가:
    ```csharp
    WaveManager.OnMonsterCountChanged += HandleMonsterCountChanged;
    ```
  - 신규 메서드:
    ```csharp
    private void HandleMonsterCountChanged(int remaining, int total)
    {
        int percent = total > 0 ? Mathf.RoundToInt((float)(total - remaining) / total * 100f) : 0;
        _progressText.text = $"{percent}%";
    }
    ```
  - `HandleWaveStarted(int waveNumber)`는 웨이브 번호(`_waveText`)만 갱신하는 기존 로직 그대로 두고, 진행률(%)은 별도의 `HandleMonsterCountChanged`로 분리 처리 (research.md에서 확인된 대로 `_waveText`는 WaveBar의 웨이브 번호 배지 역할이고, 진행률(%)은 TopBar 역할이므로 서로 다른 텍스트 필드로 관리)
  - Inspector 작업: `TopBar` 오브젝트 하위에 `%` 표시용 TMP_Text(및 필요 시 진행바 Image) 배치, `_progressText` 필드 연결 필요

### 5. 문서 정리 — `UIRules.md` TopBar 문구 수정 (결정 2 반영)

- 대상 파일: `Assets/_Project/Docs/UIRules.md`
- 현재 문구 (Section 1):
  ```
  ├─ TopBar   (스테이지명, 몬스터 HP바, %, 아이콘*)
  ```
- 수정 후:
  ```
  ├─ TopBar   (스테이지명, %, 아이콘*)
  ```
- 사유: 레퍼런스 스크린샷 확인 결과 TopBar에는 "몬스터 HP바" 요소가 존재하지 않으며, 개별 몬스터 HP바는 이미 `MonsterHpBar.cs`(World Space, 몬스터 프리팹 부착)로 별도 구현되어 TopBar와 무관함. 오기로 판단해 제거함.
- 이 스텝은 코드 작업이 아닌 간단한 문서 수정이며, docs 에이전트가 이번 task 정리 항목으로 함께 처리한다.

---

## 예상 변경/생성 파일 목록

**수정 파일**
- `Assets/_Project/Scripts/UI/CharacterHpBar.cs` — TMP_Text 필드 및 HP 숫자 텍스트 갱신 로직 추가
- `Assets/_Project/Scripts/UI/SkillCardUI.cs` — 데미지 텍스트 필드 및 Active 전용 표시 로직 추가
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` — 슬롯 그룹 필드, `OnActiveSkillsChanged`/`OnPassiveSkillsChanged` 구독 및 핸들러 추가
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `OnMonsterCountChanged` 이벤트 및 `_currentWaveTotalCount` 필드 추가, 발행 지점 3곳 추가
- `Assets/_Project/Scripts/UI/HUDPanel.cs` — 진행률(%) 텍스트 필드 및 `HandleMonsterCountChanged` 핸들러 추가
- `Assets/_Project/Docs/UIRules.md` — Section 1 TopBar 문구에서 "몬스터 HP바" 제거

**신규 생성 파일**
- `Assets/_Project/Scripts/UI/SkillSlotIcon.cs` — 슬롯 1칸(채움/빈칸) 표현 컴포넌트
- `Assets/_Project/Scripts/UI/SkillSlotGroup.cs` — 슬롯 4칸/2칸 그룹 갱신 컴포넌트

**Unity 에디터에서 별도 처리 필요 (스크립트 외 작업, 이 plan.md 범위 밖이나 구현 시 함께 안내 필요)**
- `CharacterHpBar`가 붙은 `CharacterHP` 오브젝트에 TMP_Text 자식 추가 및 필드 연결
- `SkillCardUI` 프리팹에 데미지 텍스트 오브젝트 추가 및 필드 연결
- `SkillSelectionPanel` 프리팹에 "Active Skill"/"Passive Skill" 라벨 + 슬롯 오브젝트(4+2) 배치, `SkillSlotIcon`/`SkillSlotGroup` 프리팹 구성 및 필드 연결
- `HUDPanel`의 `TopBar` 오브젝트에 진행률(%) 텍스트 배치 및 필드 연결

---

## 주의사항

1. **`x{N}` 배지 값의 정확한 의미 재확인 필요**: 레퍼런스 스크린샷에서 확인된 "x1" 배지가 스킬 레벨을 의미하는지, 보유 개수(스택)를 의미하는지 명확하지 않음. 현재 프로젝트 구조상 액티브/패시브 스킬은 각각 1개씩만 장착되며(`EquipActiveSkill`에서 이미 보유한 스킬은 새로 추가하지 않고 `LevelUp()`만 호출), "보유 개수"라는 개념 자체가 없음. 따라서 이 계획에서는 `SkillData.CurrentLevel`을 사용하는 것으로 가정했으며, `CurrentLevel`이 0-based(0, 1, 2)이므로 화면에는 `CurrentLevel + 1`로 보정해 `x1`, `x2`, `x3`로 표시할지 여부를 구현 전 재확인해야 한다.
2. **`SkillSelectionPanel`이 슬롯 최초 갱신에 필요한 아이콘 포함 리스트를 얻는 방법**: 현재 `SkillManager`는 ID만 제공하는 `ActiveSkillIds`/`PassiveSkillIds`(`IReadOnlyList<int>`)만 외부에 노출하고 있어, 패널이 열릴 때(최초 진입 시) 아이콘까지 포함한 전체 리스트를 즉시 그려야 한다면 `SkillManager`에 `IReadOnlyList<BallSkillBase>`/`IReadOnlyList<PassiveSkillBase>`를 반환하는 프로퍼티를 추가로 노출해야 할 수 있다. 이는 이벤트 기반 갱신만으로는 "패널을 열자마자 이미 장착된 스킬이 바로 보이는 상태"를 보장하기 어렵기 때문이며, 구현 단계에서 `SkillManager` 수정 범위를 추가로 논의해야 한다.
3. **Passive 슬롯도 `x{N}` 배지를 그대로 쓸지 여부**: 결정사항은 Active 카드의 데미지 배지(💥27)에 관한 것이었고, 슬롯의 `x1` 배지는 Active/Passive 공통으로 표시되는 것으로 레퍼런스에서 확인되었다는 전제이나, Passive 슬롯에서도 동일하게 레벨 배지를 그대로 노출할지는 스크린샷 재확인이 필요하다.
4. **`WaveManager.OnMonsterCountChanged` 발행 시점 중복 가능성**: `HandleMonsterDied()`와 `CheckGameOver()`(바닥 통과) 양쪽에서 이벤트를 발행하도록 계획했으나, 두 메서드가 같은 프레임에서 연속 호출되는 경우(`HandleAllBallsReturned()` → `MoveAllMonstersDown()` → `CheckGameOver()`) 여러 번 이벤트가 발행될 수 있다. 성능 규칙(`UIRules.md` §7 "HP바/진행바 갱신, 값 변경 시 이벤트로만 갱신, 매 프레임 갱신 금지")에는 위배되지 않으나(프레임마다가 아닌 값 변경 시에만 발행), UI 쪽에서 텍스트 갱신 빈도가 다소 잦아질 수 있음을 고려해야 한다.
5. **씬/프리팹 수정은 코드 리뷰만으로 완결되지 않음**: 이 계획은 스크립트(C#) 변경만 다루며, 각 항목에 명시된 "Inspector 작업"(TMP_Text 오브젝트 배치, 필드 연결, 슬롯 프리팹 구성 등)은 Unity 에디터에서 별도로 수행해야 한다. 코드만 작성해서는 화면에 아무 것도 보이지 않을 수 있으므로, 구현 완료 후 반드시 씬/프리팹 연결 여부를 함께 확인해야 한다.
6. **`UIRules.md` 수정은 문서 작업이므로 task 문서 워크플로우상 코드 작업과 함께 진행하되, 실제 반영은 docs 에이전트가 담당**: `CLAUDE.md` 5장의 에이전트 분배 기준에 따라 `UIRules.md` 수정 자체는 문서 작업이므로 dev 에이전트가 아닌 docs 에이전트가 처리해야 한다.
