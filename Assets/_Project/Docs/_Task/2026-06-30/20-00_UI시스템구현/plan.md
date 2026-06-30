# Plan — UI 시스템 구현

이 문서는 PurpleCow 프로젝트의 UI 시스템 구현 계획을 기술합니다.
`UIManager`, `HUDPanel`, `ResultPanel`, `SkillSelectionPanel`, `SkillCardUI` 5개 클래스를 신규 생성하며, 기존 시스템 이벤트를 구독하는 방식으로 최소한의 기존 코드 수정만 발생합니다.
`WaveManager`에 웨이브 단위 클리어 이벤트(`OnWaveCleared`) 1개를 추가하는 것이 유일한 기존 파일 수정입니다.

---

## 구현 목표

1. `UIManager` — 싱글톤. HUD/Result/SkillSelection 패널 참조 관리 및 GameState에 따른 패널 전환
2. `HUDPanel` — 플레이 중 표시. 현재 웨이브 번호, 누적 점수(처치 몬스터 수), 발사 대기 안내
3. `ResultPanel` — 게임 종료 후 표시. 성공/실패 메시지, 최종 점수, 재시작 버튼
4. `SkillSelectionPanel` — 웨이브 클리어 후 표시. 무작위 스킬 카드 3장 중 1장 선택
5. `SkillCardUI` — 스킬 카드 1장 단위 컴포넌트. 아이콘, 이름, 설명 표시 및 선택 콜백

---

## 단계별 작업 계획

### Step 1. WaveManager에 OnWaveCleared 이벤트 추가

**파일:** `Assets/_Project/Scripts/Wave/WaveManager.cs`

```csharp
// 추가할 static event
public static event Action OnWaveCleared;

// CheckWaveCleared() 수정
private void CheckWaveCleared()
{
    if (_activeMonsters.Count == 0)
    {
        OnWaveCleared?.Invoke();   // ← 추가
        AdvanceToNextWave();
    }
}
```

- `AdvanceToNextWave()` 호출 전에 발행 → SkillSelectionPanel이 받아 스킬 선택을 완료한 뒤 다음 웨이브 스폰이 이루어져야 하는 문제가 있음
- 따라서 `AdvanceToNextWave()`는 SkillSelectionPanel이 닫힐 때 UIManager가 호출하는 방식으로 제어 흐름을 분리

**수정 방향:**

```csharp
private void CheckWaveCleared()
{
    if (_activeMonsters.Count == 0)
    {
        // 마지막 웨이브인 경우 스킬 선택 없이 바로 종료
        if (_currentWaveIndex + 1 >= _waveDatas.Length)
        {
            OnAllWavesCleared?.Invoke();
        }
        else
        {
            OnWaveCleared?.Invoke();   // UIManager → SkillSelectionPanel 열기
            // AdvanceToNextWave()는 SkillSelectionPanel.OnSkillSelected 콜백 이후 호출
        }
    }
}

// 외부에서 호출 가능한 public 메서드로 공개
public void AdvanceToNextWave()  // private → public 변경
{
    _currentWaveIndex++;
    if (_currentWaveIndex < _waveDatas.Length)
    {
        SpawnWave(_currentWaveIndex);
    }
    else
    {
        OnAllWavesCleared?.Invoke();
    }
}
```

---

### Step 2. UIManager 구현

**파일:** `Assets/_Project/Scripts/UI/UIManager.cs`

```csharp
public class UIManager : Singleton<UIManager>
{
    // Inspector 연결 패널
    [SerializeField] private HUDPanel            _hudPanel;
    [SerializeField] private ResultPanel         _resultPanel;
    [SerializeField] private SkillSelectionPanel _skillSelectionPanel;

    // 점수 (처치 몬스터 수)
    private int _score;
    public int Score => _score;

    // 이벤트
    public static event Action<int> OnScoreChanged;

    protected override void Awake()
    {
        base.Awake();
        // 초기 상태: 모든 패널 비활성
        _hudPanel.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
        _skillSelectionPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged  += HandleGameStateChanged;
        WaveManager.OnWaveCleared                += HandleWaveCleared;
        WaveManager.OnAllWavesCleared            += HandleAllWavesCleared;
        MonsterBase.OnMonsterDied                += HandleMonsterDied;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged  -= HandleGameStateChanged;
        WaveManager.OnWaveCleared                -= HandleWaveCleared;
        WaveManager.OnAllWavesCleared            -= HandleAllWavesCleared;
        MonsterBase.OnMonsterDied                -= HandleMonsterDied;
    }

    // 패널 전환
    private void HandleGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Ready:
                ShowHUD(false);
                ShowResult(false);
                ShowSkillSelection(false);
                _score = 0;
                break;
            case GameManager.GameState.Playing:
                ShowHUD(true);
                ShowResult(false);
                ShowSkillSelection(false);
                break;
            case GameManager.GameState.Result:
                ShowHUD(false);
                ShowResult(true);
                ShowSkillSelection(false);
                break;
        }
    }

    // 웨이브 클리어 → 스킬 선택 패널 열기
    private void HandleWaveCleared()
    {
        ShowSkillSelection(true);
    }

    // 모든 웨이브 클리어 → GameManager.EndGame(true) 호출
    private void HandleAllWavesCleared()
    {
        GameManager.Instance.EndGame(true);
    }

    // 점수 누적
    private void HandleMonsterDied(MonsterBase monster)
    {
        _score++;
        OnScoreChanged?.Invoke(_score);
    }

    // 스킬 선택 완료 콜백 (SkillSelectionPanel이 호출)
    public void OnSkillSelectionComplete()
    {
        ShowSkillSelection(false);
        WaveManager.Instance.AdvanceToNextWave();
    }

    // 패널 활성화 헬퍼
    private void ShowHUD(bool show)             => _hudPanel.gameObject.SetActive(show);
    private void ShowResult(bool show)          => _resultPanel.gameObject.SetActive(show);
    private void ShowSkillSelection(bool show)  => _skillSelectionPanel.gameObject.SetActive(show);
}
```

**설계 포인트:**
- `OnEnable` / `OnDisable` 에서 구독/해제 (DevRules 이벤트 규칙)
- `GameManager.Instance` 는 `Singleton<T>` 보장이므로 `OnEnable`에서 안전하게 참조 (Awake 순서 주의 — GameManager는 먼저 Awake 되어야 함)
- `OnScoreChanged` 이벤트로 HUDPanel에 점수 전달

---

### Step 3. HUDPanel 구현

**파일:** `Assets/_Project/Scripts/UI/HUDPanel.cs`

```csharp
public class HUDPanel : MonoBehaviour
{
    // Inspector 연결 UI 컴포넌트
    [SerializeField] private TMP_Text _waveText;    // "WAVE 3 / 5"
    [SerializeField] private TMP_Text _scoreText;   // "처치: 12"
    [SerializeField] private GameObject _launchReadyIndicator;  // "발사 준비" 안내 오브젝트

    private int _totalWaves;

    private void OnEnable()
    {
        WaveManager.OnWaveStarted     += HandleWaveStarted;
        BallLauncher.OnAllBallsReturned += HandleAllBallsReturned;
        UIManager.OnScoreChanged      += HandleScoreChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted       -= HandleWaveStarted;
        BallLauncher.OnAllBallsReturned -= HandleAllBallsReturned;
        UIManager.OnScoreChanged        -= HandleScoreChanged;
    }

    private void Start()
    {
        // WaveManager에서 총 웨이브 수 파악
        _totalWaves = WaveManager.Instance.TotalWaves;
        UpdateScore(0);
        _launchReadyIndicator.SetActive(false);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        _waveText.text = $"WAVE {waveNumber} / {_totalWaves}";
        _launchReadyIndicator.SetActive(true);
    }

    private void HandleAllBallsReturned()
    {
        _launchReadyIndicator.SetActive(false);
    }

    private void HandleScoreChanged(int score)
    {
        UpdateScore(score);
    }

    private void UpdateScore(int score)
    {
        _scoreText.text = $"처치: {score}";
    }
}
```

**추가 필요 사항 (WaveManager):**
- `WaveManager`에 `public int TotalWaves => _waveDatas.Length;` 프로퍼티 추가 (읽기 전용)

---

### Step 4. ResultPanel 구현

**파일:** `Assets/_Project/Scripts/UI/ResultPanel.cs`

```csharp
public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text  _resultTitleText;   // "SUCCESS" / "GAME OVER"
    [SerializeField] private TMP_Text  _finalScoreText;    // "최종 점수: 24"
    [SerializeField] private Button    _restartButton;

    private void Awake()
    {
        _restartButton.onClick.AddListener(HandleRestartClicked);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        _restartButton.onClick.RemoveListener(HandleRestartClicked);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Result)
            return;

        // GameManager.EndGame(isSuccess) 의 isSuccess 값이 필요 → GameManager에 IsLastGameSuccess 프로퍼티 추가 필요
        bool isSuccess = GameManager.Instance.IsLastGameSuccess;
        _resultTitleText.text = isSuccess ? "SUCCESS" : "GAME OVER";
        _finalScoreText.text  = $"최종 점수: {UIManager.Instance.Score}";
    }

    private void HandleRestartClicked()
    {
        GameManager.Instance.RestartGame();
    }
}
```

**추가 필요 사항 (GameManager):**
- `EndGame(bool isSuccess)` 호출 시 `_isLastGameSuccess` 저장
- `public bool IsLastGameSuccess { get; private set; }` 프로퍼티 추가

---

### Step 5. SkillSelectionPanel 구현

**파일:** `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`

```csharp
public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _cards;              // 카드 슬롯 3개
    [SerializeField] private SkillData[]   _allSkillDatas;      // Inspector에서 모든 SkillData SO 연결

    private void OnEnable()
    {
        ShowRandomSkills();
    }

    private void ShowRandomSkills()
    {
        // _allSkillDatas에서 중복 없이 3개 무작위 선택
        List<SkillData> pool = new List<SkillData>(_allSkillDatas);
        for (int i = 0; i < _cards.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            _cards[i].Setup(pool[randomIndex], OnCardSelected);
            pool.RemoveAt(randomIndex);
        }
    }

    private void OnCardSelected(SkillData selectedData)
    {
        // 스킬 타입에 따라 SkillManager에 장착
        ApplySkill(selectedData);
        UIManager.Instance.OnSkillSelectionComplete();
    }

    private void ApplySkill(SkillData data)
    {
        if (data.SkillType == SkillType.Active)
        {
            // BallSkillBase 인스턴스 생성은 별도 팩토리 또는 프리팹 방식 필요
            // → SkillFactory (별도 구현) 또는 Inspector 프리팹 연결 방식 선택
            // 본 plan에서는 SkillFactory 패턴을 채택
            BallSkillBase skill = SkillFactory.CreateActiveSkill(data);
            SkillManager.Instance.EquipActiveSkill(skill);
        }
        else
        {
            PassiveSkillBase skill = SkillFactory.CreatePassiveSkill(data);
            SkillManager.Instance.AddPassiveSkill(skill);
        }
    }
}
```

**설계 포인트:**
- `OnEnable` 에서 카드 갱신 — 패널이 열릴 때마다 새로운 무작위 3장 제시
- `SkillFactory` 는 `SkillId` 로 구체 스킬 클래스를 생성하는 정적 팩토리 (별도 Step에서 구현)
- `_allSkillDatas` 는 Inspector에서 연결 (SO 배열)

---

### Step 6. SkillCardUI 구현

**파일:** `Assets/_Project/Scripts/UI/SkillCardUI.cs`

```csharp
public class SkillCardUI : MonoBehaviour
{
    [SerializeField] private Image     _iconImage;
    [SerializeField] private TMP_Text  _nameText;
    [SerializeField] private TMP_Text  _descriptionText;
    [SerializeField] private TMP_Text  _typeText;       // "액티브" / "패시브"
    [SerializeField] private Button    _selectButton;

    private Action<SkillData> _onSelected;

    private void Awake()
    {
        _selectButton.onClick.AddListener(HandleSelectClicked);
    }

    private void OnDestroy()
    {
        _selectButton.onClick.RemoveListener(HandleSelectClicked);
    }

    public void Setup(SkillData data, Action<SkillData> onSelected)
    {
        _onSelected          = onSelected;
        _iconImage.sprite    = data.Icon;
        _nameText.text       = data.SkillName;
        _descriptionText.text = data.Description;
        _typeText.text       = data.SkillType == SkillType.Active ? "액티브" : "패시브";
    }

    private void HandleSelectClicked()
    {
        _onSelected?.Invoke(/* SkillData 참조 필요 — _currentData 캐싱 */);
    }
}
```

**상세 필드:**

| 필드 | 타입 | SerializeField | 역할 |
|------|------|---------------|------|
| `_iconImage` | `Image` | O | 스킬 아이콘 스프라이트 표시 |
| `_nameText` | `TMP_Text` | O | 스킬 이름 |
| `_descriptionText` | `TMP_Text` | O | 스킬 설명 |
| `_typeText` | `TMP_Text` | O | 스킬 타입 레이블 |
| `_selectButton` | `Button` | O | 카드 선택 버튼 |
| `_currentData` | `SkillData` | X | Setup 시 캐싱, 콜백 전달용 |
| `_onSelected` | `Action<SkillData>` | X | 외부 콜백 |

---

### Step 7. SkillFactory 구현

**파일:** `Assets/_Project/Scripts/Skill/SkillFactory.cs`

```csharp
public static class SkillFactory
{
    public static BallSkillBase CreateActiveSkill(SkillData data)
    {
        return (ActiveSkillId)data.SkillId switch
        {
            ActiveSkillId.Fire    => new FireBallSkill(data),
            ActiveSkillId.Ice     => new IceBallSkill(data),
            ActiveSkillId.Ghost   => new GhostBallSkill(data),
            ActiveSkillId.Laser   => new LaserBallSkill(data),
            ActiveSkillId.Cluster => new ClusterBallSkill(data),
            _                     => throw new ArgumentOutOfRangeException()
        };
    }

    public static PassiveSkillBase CreatePassiveSkill(SkillData data)
    {
        return (PassiveSkillId)data.SkillId switch
        {
            PassiveSkillId.DamageUp     => new DamageUpPassive(data),
            PassiveSkillId.CritChanceUp => new CritChanceUpPassive(data),
            PassiveSkillId.CritDamageUp => new CritDamageUpPassive(data),
            PassiveSkillId.SpeedUp      => new SpeedUpPassive(data),
            PassiveSkillId.BounceUp     => new BounceUpPassive(data),
            PassiveSkillId.KillShot     => new KillShotPassive(data),
            PassiveSkillId.LastHit      => new LastHitPassive(data),
            _                           => throw new ArgumentOutOfRangeException()
        };
    }
}
```

**주의:** `BallSkillBase`는 현재 `MonoBehaviour`를 상속하므로 `new`로 직접 생성 불가. 이 구조적 문제를 해결하는 두 가지 옵션:

- **옵션 A (권장)**: `BallSkillBase`를 순수 C# 클래스로 변경, `Ball` 컴포넌트에서 직접 보유
- **옵션 B**: 액티브 스킬 프리팹을 Inspector에서 `SkillSelectionPanel`에 배열로 연결, `SkillId`로 매칭하여 `Instantiate`

본 plan에서는 **옵션 A**를 채택합니다. `BallSkillBase`를 MonoBehaviour에서 분리하는 리팩토링이 포함됩니다.

---

## 예상 변경 / 생성 파일 목록

### 신규 생성

| 파일 경로 | 클래스 | 역할 |
|-----------|--------|------|
| `Assets/_Project/Scripts/UI/UIManager.cs` | `UIManager` | 패널 전환, 점수 관리 |
| `Assets/_Project/Scripts/UI/HUDPanel.cs` | `HUDPanel` | 웨이브·점수 HUD |
| `Assets/_Project/Scripts/UI/ResultPanel.cs` | `ResultPanel` | 결과 화면 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | `SkillSelectionPanel` | 스킬 선택 UI |
| `Assets/_Project/Scripts/UI/SkillCardUI.cs` | `SkillCardUI` | 스킬 카드 1장 컴포넌트 |
| `Assets/_Project/Scripts/Skill/SkillFactory.cs` | `SkillFactory` | 스킬 인스턴스 생성 팩토리 |

### 기존 파일 수정

| 파일 경로 | 수정 내용 |
|-----------|----------|
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | `OnWaveCleared` static event 추가, `AdvanceToNextWave()` private → public, `TotalWaves` 프로퍼티 추가, `CheckWaveCleared()` 로직 수정 |
| `Assets/_Project/Scripts/Core/GameManager.cs` | `_isLastGameSuccess` 필드·`IsLastGameSuccess` 프로퍼티 추가, `EndGame(bool)` 에서 저장 |
| `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` | MonoBehaviour 상속 제거, 순수 C# 클래스로 변환 (옵션 A 채택 시) |
| `Assets/_Project/Scripts/Ball/Ball.cs` | `BallSkillBase`가 MonoBehaviour 아닌 경우 `_skill` 필드 및 Awake 캐싱 방식 조정 |
| `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs` 외 액티브 스킬 5종 | MonoBehaviour 상속 제거에 따른 생성자 방식 변경 |

---

## 각 클래스 필드·메서드·이벤트 요약

### UIManager

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| field | `_hudPanel` | `HUDPanel` | SerializeField, HUD 패널 참조 |
| field | `_resultPanel` | `ResultPanel` | SerializeField, 결과 패널 참조 |
| field | `_skillSelectionPanel` | `SkillSelectionPanel` | SerializeField, 스킬 선택 패널 참조 |
| field | `_score` | `int` | 누적 처치 수 |
| property | `Score` | `int` | `_score` 읽기 전용 |
| event | `OnScoreChanged` | `static Action<int>` | 점수 변경 시 발행 |
| method | `OnSkillSelectionComplete()` | `void` | 패널 닫기 + `WaveManager.AdvanceToNextWave()` |
| method | `HandleGameStateChanged(GameState)` | `void` | private, 패널 전환 |
| method | `HandleWaveCleared()` | `void` | private, SkillSelectionPanel 열기 |
| method | `HandleAllWavesCleared()` | `void` | private, `GameManager.EndGame(true)` |
| method | `HandleMonsterDied(MonsterBase)` | `void` | private, 점수 누적 |

### HUDPanel

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| field | `_waveText` | `TMP_Text` | SerializeField, 웨이브 텍스트 |
| field | `_scoreText` | `TMP_Text` | SerializeField, 점수 텍스트 |
| field | `_launchReadyIndicator` | `GameObject` | SerializeField, 발사 대기 표시 |
| field | `_totalWaves` | `int` | 전체 웨이브 수 캐싱 |
| method | `HandleWaveStarted(int)` | `void` | private, 웨이브 텍스트 갱신 |
| method | `HandleAllBallsReturned()` | `void` | private, 발사 대기 표시 비활성 |
| method | `HandleScoreChanged(int)` | `void` | private, 점수 텍스트 갱신 |
| method | `UpdateScore(int)` | `void` | private, 텍스트 포맷팅 |

### ResultPanel

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| field | `_resultTitleText` | `TMP_Text` | SerializeField, SUCCESS/GAME OVER |
| field | `_finalScoreText` | `TMP_Text` | SerializeField, 최종 점수 |
| field | `_restartButton` | `Button` | SerializeField, 재시작 버튼 |
| method | `HandleGameStateChanged(GameState)` | `void` | private, 결과 표시 갱신 |
| method | `HandleRestartClicked()` | `void` | private, `GameManager.RestartGame()` |

### SkillSelectionPanel

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| field | `_cards` | `SkillCardUI[3]` | SerializeField, 카드 슬롯 3개 |
| field | `_allSkillDatas` | `SkillData[]` | SerializeField, 전체 스킬 SO 배열 |
| method | `ShowRandomSkills()` | `void` | private, 중복 없이 3장 무작위 제시 |
| method | `OnCardSelected(SkillData)` | `void` | private, 스킬 장착 + UIManager 콜백 |
| method | `ApplySkill(SkillData)` | `void` | private, SkillFactory 통해 장착 |

### SkillCardUI

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| field | `_iconImage` | `Image` | SerializeField |
| field | `_nameText` | `TMP_Text` | SerializeField |
| field | `_descriptionText` | `TMP_Text` | SerializeField |
| field | `_typeText` | `TMP_Text` | SerializeField |
| field | `_selectButton` | `Button` | SerializeField |
| field | `_currentData` | `SkillData` | private, Setup 시 캐싱 |
| field | `_onSelected` | `Action<SkillData>` | private, 선택 콜백 |
| method | `Setup(SkillData, Action<SkillData>)` | `void` | public, 카드 데이터 초기화 |
| method | `HandleSelectClicked()` | `void` | private, 콜백 호출 |

### SkillFactory

| 구분 | 이름 | 타입 | 설명 |
|------|------|------|------|
| method | `CreateActiveSkill(SkillData)` | `static BallSkillBase` | SkillId 기반 액티브 스킬 생성 |
| method | `CreatePassiveSkill(SkillData)` | `static PassiveSkillBase` | SkillId 기반 패시브 스킬 생성 |

---

## 주의사항

1. **BallSkillBase MonoBehaviour 분리 (옵션 A)**: 액티브 스킬 5종 모두 수정 필요. `Awake()`에서 `GetComponent<Ball>()` 캐싱 로직을 생성자 또는 `Initialize(Ball ball)` 메서드로 전환해야 함.

2. **Singleton 초기화 순서**: `UIManager.OnEnable()`에서 `GameManager.Instance`를 참조하므로, 씬 내 GameObject 활성화 순서에 따라 null 참조가 발생할 수 있음. GameManager GameObject를 UIManager보다 먼저 Awake 되도록 Script Execution Order 또는 GameObject 배치 순서로 보장.

3. **TextMeshPro 의존성**: `TMP_Text` 사용을 위해 TextMeshPro 패키지가 프로젝트에 포함되어 있어야 함. 현재 의존성 확인 필요.

4. **스킬 선택 중 입력 차단**: `SkillSelectionPanel`이 활성화된 동안 `BallLauncher`의 발사가 불가해야 함. `GameManager` 상태를 `Playing`으로 유지하되 `BallLauncher`는 `_canLaunch` 플래그로 제어 중이므로, UIManager가 SkillSelectionPanel 활성화 시 `BallLauncher`에 별도 신호를 보내거나 `GameState`를 임시 변경하는 방안 중 하나를 선택해야 함. 단순성 원칙에 따라 `UIManager`에서 `BallLauncher.Instance.SetLaunchEnabled(false/true)` 메서드를 추가하는 방식을 권장.

5. **ScoreManager 분리 미채택**: 단순성 원칙에 따라 점수를 `UIManager` 내부에서 관리. 추후 확장 필요 시 분리.

6. **씬 구성**: UIManager, HUDPanel, ResultPanel, SkillSelectionPanel은 씬 내 UI Canvas 하위에 배치. 구체적인 씬 구성은 구현 단계에서 결정.

---

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
