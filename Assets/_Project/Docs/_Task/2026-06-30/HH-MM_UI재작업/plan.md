# Plan — UI 전체 재작업

UIRules.md 위반 항목을 수정하고 미구현 UI 컴포넌트 8종을 신규 작성하는 계획이다.
의존성 이벤트(MonsterBase.OnHpChanged, WaveManager.OnMonsterReachedBottom) 추가와
DOTween 설치도 선행 작업으로 포함된다.
총 12개 STEP으로 구성되며, 각 STEP은 독립적으로 완료 가능하도록 설계한다.

---

## 구현 목표

1. 모든 패널 제어를 `SetActive` → `CanvasGroup` 방식으로 전환 (UIRules 규칙 4)
2. 모든 패널에 DOTween Show/Hide 애니메이션 적용 (UIRules 규칙 5)
3. 모든 버튼에 `UIButton` 컴포넌트 적용 (UIRules 규칙 6)
4. 미구현 UI 컴포넌트 8종 신규 생성
5. 의존성 이벤트 2종 추가

---

## 단계별 작업 계획

---

### STEP 0 — DOTween 설치 (사전 조건)

**작업 유형**: 수동 설치 (개발자 직접 수행)

manifest.json 확인 결과 `com.demigiant` 패키지가 없음. STEP 1 이후 모든 DOTween 코드는 이 패키지가 설치된 상태를 전제로 한다.

설치 방법:
- Unity Package Manager → Add package from git URL: `https://github.com/Demigiant/dotween.git`
- 또는 Asset Store에서 DOTween (HOTween v2) 다운로드 후 `Assets/Plugins/` 하위 import

설치 완료 후 DOTween Setup Utility를 실행하여 `using DG.Tweening`이 컴파일되는지 확인한다.

---

### STEP 1 — UIButton.cs 신규 생성

**파일**: `Assets/_Project/Scripts/UI/UIButton.cs` (신규)

`IPointerDownHandler`, `IPointerUpHandler`를 구현한 MonoBehaviour 컴포넌트.
Unity 기본 Button의 Transition은 None으로 설정하고 이 컴포넌트가 시각 피드백을 전담한다.

구현 내용:
- `[SerializeField] private float _pressedScale = 0.9f`
- `[SerializeField] private float _animDuration = 0.1f`
- `OnPointerDown(PointerEventData)`: `transform.DOScale(_pressedScale, _animDuration)`
- `OnPointerUp(PointerEventData)`: `transform.DOScale(1f, _animDuration)`
- `using UnityEngine.EventSystems`, `using DG.Tweening`

---

### STEP 2 — SafeAreaFitter.cs 신규 생성

**파일**: `Assets/_Project/Scripts/UI/SafeAreaFitter.cs` (신규)

`Canvas_HUD` 내 `SafeAreaPanel` 오브젝트에 부착하는 컴포넌트.
노치/펀치홀/홈 인디케이터 등 기기 침범 영역을 자동으로 회피한다.

구현 내용:
- `private RectTransform _rectTransform`
- `private Canvas _canvas`
- `Awake()`:
  - `_rectTransform = GetComponent<RectTransform>()`
  - `_canvas = GetComponentInParent<Canvas>()`
  - `ApplySafeArea(Screen.safeArea)` 호출
- `ApplySafeArea(Rect safeArea)`:
  - `safeArea`를 Canvas 픽셀 단위로 변환
  - `_rectTransform.offsetMin`, `_rectTransform.offsetMax` 설정

---

### STEP 3 — UIManager.cs 수정 (CanvasGroup 전환)

**파일**: `Assets/_Project/Scripts/UI/UIManager.cs` (수정)

SetActive 기반 제어를 CanvasGroup 기반으로 전환하고 이중 트리거 버그를 제거한다.

변경 내용:

1. `Awake()` 내 `SetActive(false)` 3회 제거
   - 대신 각 패널의 `CanvasGroup`을 숨김 상태로 초기화: `_hudPanel.Hide()`, `_resultPanel.Hide()`, `_skillSelectionPanel.Hide()` 호출

2. `OnEnable()` / `OnDisable()` 에서 `WaveManager.OnWaveCleared` 구독/해제 제거

3. `HandleWaveCleared()` 메서드 제거
   - 스킬 선택 트리거는 `SkillSelectionPanel`이 `OnKillCountReached`로 직접 처리

4. `ShowHUD / ShowResult / ShowSkillSelection` 메서드 교체:
   ```
   // 기존: gameObject.SetActive(show)
   // 변경: show ? panel.Show() : panel.Hide()
   private void ShowHUD(bool show)            => { if (show) _hudPanel.Show(); else _hudPanel.Hide(); }
   private void ShowResult(bool show)         => { if (show) _resultPanel.Show(); else _resultPanel.Hide(); }
   private void ShowSkillSelection(bool show) => { if (show) _skillSelectionPanel.Show(); else _skillSelectionPanel.Hide(); }
   ```

5. 각 패널 클래스에 `public void Show()` / `public void Hide()` 메서드가 생성되는 것을 전제로 함 (STEP 4).

---

### STEP 4 — 패널 DOTween 애니메이션 추가 (HUDPanel, ResultPanel, SkillSelectionPanel)

**파일**: 3개 파일 수정

각 패널에 `CanvasGroup` 참조와 Show/Hide DOTween 애니메이션을 추가한다.

공통 추가 필드:
```csharp
[SerializeField] private CanvasGroup _canvasGroup;
[SerializeField] private float _slideDist   = 50f;
[SerializeField] private float _animDuration = 0.3f;
[SerializeField] private Ease  _ease        = Ease.OutCubic;
```

공통 Show() 패턴:
```csharp
public void Show()
{
    _canvasGroup.blocksRaycasts = false;
    _canvasGroup.interactable   = false;

    var startPos = _originalPos + Vector3.down * _slideDist;
    transform.localPosition = startPos;
    _canvasGroup.alpha = 0f;

    Sequence seq = DOTween.Sequence();
    seq.Append(transform.DOLocalMoveY(_originalPos.y, _animDuration).SetEase(_ease));
    seq.Join(_canvasGroup.DOFade(1f, _animDuration));
    seq.OnComplete(() =>
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable   = true;
    });
}
```

공통 Hide() 패턴:
```csharp
public void Hide()
{
    _canvasGroup.blocksRaycasts = false;
    _canvasGroup.interactable   = false;

    Sequence seq = DOTween.Sequence();
    seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
    seq.Join(_canvasGroup.DOFade(0f, _animDuration));
    seq.OnComplete(() =>
    {
        transform.localPosition = _originalPos;
    });
}
```

`_originalPos`는 `Awake()`에서 `transform.localPosition`으로 캐싱.

**HUDPanel.cs 추가 변경**:
- `_launchReadyIndicator.SetActive(false/true)` → `CanvasGroup` 또는 `alpha` 제어로 교체
  - `_launchReadyIndicator`에 `CanvasGroup`을 추가하고 alpha/interactable/blocksRaycasts로 ON/OFF 처리

---

### STEP 5 — UIButton 적용 (ResultPanel, SkillCardUI)

**파일**: `ResultPanel.cs`, `SkillCardUI.cs` (수정), 씬/프리팹 작업

코드 변경:
- `ResultPanel.cs`: `[SerializeField] private Button _restartButton` → `UIButton` 컴포넌트와 병행 보유
- `SkillCardUI.cs`: `[SerializeField] private Button _selectButton` → 동일

씬/프리팹 작업 (주의사항):
- `_restartButton` 오브젝트에 `UIButton` 컴포넌트 Add Component
- `_selectButton` 오브젝트에 `UIButton` 컴포넌트 Add Component
- 기존 Unity Button의 Transition을 **None**으로 설정
- 이 작업은 코드 수정이 아닌 Inspector/프리팹 작업이므로 별도 씬 작업 필요

---

### STEP 6 — MonsterBase OnHpChanged 이벤트 추가

**파일**: `Assets/_Project/Scripts/Monster/MonsterBase.cs` (수정)

추가 내용:
```csharp
public event Action<float, float> OnHpChanged;
```

`OnSpawn()` 수정:
```csharp
public void OnSpawn()
{
    _currentHp = _monsterData.Hp;
    // ... 기존 초기화 ...
    OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
}
```

`TakeDamage()` 수정:
```csharp
public void TakeDamage(float damage)
{
    if (_isDead) return;
    _currentHp -= damage;
    OnHpChanged?.Invoke(Mathf.Max(_currentHp, 0f), _monsterData.Hp);
    if (_currentHp <= 0f) Die();
}
```

주의: `_monsterData.Hp`에 접근하기 위해 `_monsterData` 필드가 private이므로 `MaxHp` 프로퍼티를 추가하거나 `MonsterData.Hp`를 직접 참조. 현재 `MonsterData.Hp` 프로퍼티가 public이므로 `_monsterData.Hp` 직접 사용 가능.

---

### STEP 7 — MonsterHpBar.cs 신규 생성

**파일**: `Assets/_Project/Scripts/UI/MonsterHpBar.cs` (신규)

각 몬스터 프리팹의 World Space Canvas 자식으로 배치하는 HP바 컴포넌트.

구현 내용:
- `[SerializeField] private Slider _slider`
- `private MonsterBase _monster`
- `Start()`: `_monster = GetComponentInParent<MonsterBase>()`, 이벤트 구독
- `OnEnable()`: `_monster`가 null이 아닐 때 `_monster.OnHpChanged += UpdateHp`
- `OnDisable()`: `_monster.OnHpChanged -= UpdateHp`
- `UpdateHp(float current, float max)`: `_slider.value = current / max`

---

### STEP 8 — WaveManager OnMonsterReachedBottom 이벤트 추가

**파일**: `Assets/_Project/Scripts/Wave/WaveManager.cs` (수정)

추가 내용:
```csharp
public static event Action<MonsterBase> OnMonsterReachedBottom;
```

`CheckGameOver()` 수정:
```csharp
private void CheckGameOver()
{
    var monstersAtBottom = new List<MonsterBase>();
    foreach (MonsterBase monster in _activeMonsters)
    {
        if (monster.transform.position.y <= _bottomBoundaryY)
            monstersAtBottom.Add(monster);
    }

    foreach (MonsterBase monster in monstersAtBottom)
    {
        OnMonsterReachedBottom?.Invoke(monster);
        _activeMonsters.Remove(monster);
        _monsterPool.Return(monster);
    }
}
```

주의: 기존 `CheckGameOver()`는 바닥 도달 즉시 `GameManager.EndGame(false)`를 호출함.
이벤트 방식으로 전환하면 HP 차감 로직은 `CharacterManager`가 담당하고,
HP가 0이 되었을 때 `CharacterManager`가 `GameManager.EndGame(false)`를 호출함.
기존 `CheckGameOver()`의 직접 EndGame 호출은 제거한다.

---

### STEP 9 — CharacterManager.cs 신규 생성

**파일**: `Assets/_Project/Scripts/Core/CharacterManager.cs` (신규)

HP/XP/레벨을 통합 관리하는 Singleton 컴포넌트.

구현 내용:

```csharp
public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private int   _maxHp = 10;
    [SerializeField] private int[] _xpPerLevel;  // 레벨별 필요 XP

    private int _currentHp;
    private int _currentXp;
    private int _currentLevel;

    public static event Action<int, int> OnHpChanged;   // (current, max)
    public static event Action<int, int> OnXpChanged;   // (current, required)
    public static event Action<int>      OnLevelUp;     // (newLevel)

    protected override void Awake()
    {
        base.Awake();
        _currentHp    = _maxHp;
        _currentLevel = 1;
        _currentXp    = 0;
    }

    private void OnEnable()
    {
        WaveManager.OnMonsterReachedBottom += HandleMonsterReachedBottom;
        MonsterBase.OnMonsterDied          += HandleMonsterDied;
    }

    private void OnDisable()
    {
        WaveManager.OnMonsterReachedBottom -= HandleMonsterReachedBottom;
        MonsterBase.OnMonsterDied          -= HandleMonsterDied;
    }

    private void HandleMonsterReachedBottom(MonsterBase monster)
    {
        TakeDamage(monster.Data.Damage);   // MonsterBase에 Data 프로퍼티 추가 필요 또는 이벤트 파라미터 변경
        AddXp(monster.Data.Reward);
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        AddXp(monster.Data.Reward);
    }

    private void TakeDamage(int damage)
    {
        _currentHp = Mathf.Max(0, _currentHp - damage);
        OnHpChanged?.Invoke(_currentHp, _maxHp);
        if (_currentHp <= 0) GameManager.Instance.EndGame(false);
    }

    private void AddXp(int amount)
    {
        if (_currentLevel - 1 >= _xpPerLevel.Length) return;

        _currentXp += amount;
        int required = _xpPerLevel[_currentLevel - 1];
        OnXpChanged?.Invoke(_currentXp, required);

        if (_currentXp >= required)
        {
            _currentXp -= required;
            _currentLevel++;
            OnLevelUp?.Invoke(_currentLevel);
        }
    }
}
```

추가 필요 사항: `MonsterBase`에 `public MonsterData Data => _monsterData` 프로퍼티 추가.
현재 `_monsterData`는 private이므로 외부에서 접근 불가. 외과적으로 프로퍼티 1개만 추가.

---

### STEP 10 — CharacterHpBar.cs / CharacterXpBar.cs 신규 생성

**파일**: `Assets/_Project/Scripts/UI/CharacterHpBar.cs` (신규)
**파일**: `Assets/_Project/Scripts/UI/CharacterXpBar.cs` (신규)

**CharacterHpBar.cs**:
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

**CharacterXpBar.cs**:
```csharp
public class CharacterXpBar : MonoBehaviour
{
    [SerializeField] private Slider   _slider;
    [SerializeField] private TMP_Text _levelText;

    private void OnEnable()
    {
        CharacterManager.OnXpChanged += UpdateXp;
        CharacterManager.OnLevelUp   += UpdateLevel;
    }

    private void OnDisable()
    {
        CharacterManager.OnXpChanged -= UpdateXp;
        CharacterManager.OnLevelUp   -= UpdateLevel;
    }

    private void UpdateXp(int current, int required)
    {
        _slider.value = required > 0 ? (float)current / required : 0f;
    }

    private void UpdateLevel(int level)
    {
        _levelText.text = $"Lv.{level}";
    }
}
```

---

### STEP 11 — DamageTextFx.cs / DamageTextManager.cs 신규 생성

**파일**: `Assets/_Project/Scripts/UI/DamageTextFx.cs` (신규)
**파일**: `Assets/_Project/Scripts/UI/DamageTextManager.cs` (신규)

World Space TMP 기반 데미지 텍스트 이펙트 시스템. Canvas 없이 월드 좌표에 직접 배치.

**DamageTextFx.cs**:
- `MonoBehaviour`, `IPoolable` 구현
- `[SerializeField] private TMP_Text _text`
- `[SerializeField] private float _floatDist = 1f`
- `[SerializeField] private float _duration = 0.8f`
- `[SerializeField] private float _criticalScale = 1.5f`
- `[SerializeField] private Color _normalColor = Color.white`
- `[SerializeField] private Color _criticalColor = Color.yellow`
- `public void Play(Vector3 worldPos, float damage, bool isCritical)`:
  - `transform.position = worldPos`
  - 크리티컬 시 색상/크기 변경
  - DOTween Sequence: 위로 `_floatDist` 이동 + alpha 0으로 FadeOut
  - `OnComplete`: `DamageTextManager.Instance.Return(this)`
- `OnSpawn()`: DOTween Kill, alpha 1 복원, 스케일 1 복원
- `OnDespawn()`: DOTween Kill

**DamageTextManager.cs**:
- `Singleton<DamageTextManager>` 상속
- `[SerializeField] private DamageTextFx _prefab`
- `[SerializeField] private Transform _poolParent`
- `[SerializeField] private int _initialPoolSize = 10`
- `private ObjectPool<DamageTextFx> _pool`
- `Awake()`: `_pool = new ObjectPool<DamageTextFx>(_prefab, _poolParent, _initialPoolSize)`
- `public void ShowDamage(Vector3 worldPos, float damage, bool isCritical)`: 풀에서 꺼내 `Play()` 호출
- `public void Return(DamageTextFx fx)`: `_pool.Return(fx)`

---

### STEP 12 — SkillSelectionPanel.cs OnEnable 정리 및 CanvasGroup 전환

**파일**: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` (수정)

변경 내용:

1. `OnEnable`에서 `ShowRandomSkills()` 직접 호출 제거
   ```csharp
   private void OnEnable()
   {
       WaveManager.OnKillCountReached += OpenPanel;
       // ShowRandomSkills() 제거
   }
   ```

2. `OpenPanel()` 내에서만 `ShowRandomSkills()` 호출 (기존 동일)

3. `gameObject.SetActive(true/false)` → CanvasGroup 기반으로 교체
   ```csharp
   private void OpenPanel()
   {
       Show();               // CanvasGroup Show (STEP 4에서 추가)
       ShowRandomSkills();
   }

   private void OnSkillSelected(SkillData selectedData)
   {
       ApplySkill(selectedData);
       UIManager.Instance.OnSkillSelectionComplete();
       Hide();               // CanvasGroup Hide
   }
   ```

4. `_skillCards[i].gameObject.SetActive()` → CanvasGroup alpha 제어로 교체
   ```csharp
   // SkillCardUI에 CanvasGroup 추가 후:
   _skillCards[i].SetVisible(true);   // alpha=1, interactable=true
   _skillCards[i].SetVisible(false);  // alpha=0, interactable=false
   ```
   → `SkillCardUI`에 `public void SetVisible(bool visible)` 메서드 추가 필요

---

## 예상 변경/생성 파일 목록

### 신규 생성 (8개)

| 파일 | 경로 | 역할 |
|------|------|------|
| `UIButton.cs` | `Scripts/UI/UIButton.cs` | 버튼 Scale 피드백 (IPointerDown/Up) |
| `SafeAreaFitter.cs` | `Scripts/UI/SafeAreaFitter.cs` | Safe Area 자동 적용 |
| `MonsterHpBar.cs` | `Scripts/UI/MonsterHpBar.cs` | 몬스터 HP Slider, OnHpChanged 구독 |
| `CharacterHpBar.cs` | `Scripts/UI/CharacterHpBar.cs` | 캐릭터 HP Slider |
| `CharacterXpBar.cs` | `Scripts/UI/CharacterXpBar.cs` | 캐릭터 XP Slider + 레벨 텍스트 |
| `DamageTextFx.cs` | `Scripts/UI/DamageTextFx.cs` | 개별 데미지 텍스트 이펙트 (IPoolable) |
| `DamageTextManager.cs` | `Scripts/UI/DamageTextManager.cs` | 데미지 텍스트 ObjectPool 관리 |
| `CharacterManager.cs` | `Scripts/Core/CharacterManager.cs` | HP/XP/레벨 통합 관리 Singleton |

### 수정 파일 (7개)

| 파일 | 주요 변경 내용 |
|------|--------------|
| `UI/UIManager.cs` | SetActive 제거, OnWaveCleared 구독 제거, HandleWaveCleared 제거, Show/Hide 호출 방식 전환 |
| `UI/HUDPanel.cs` | SetActive 제거, CanvasGroup 추가, DOTween Show/Hide 추가 |
| `UI/ResultPanel.cs` | CanvasGroup 추가, DOTween Show/Hide 추가 |
| `UI/SkillSelectionPanel.cs` | OnEnable ShowRandomSkills 제거, SetActive → CanvasGroup, 카드 SetActive → SetVisible |
| `UI/SkillCardUI.cs` | SetVisible(bool) 메서드 추가 |
| `Monster/MonsterBase.cs` | OnHpChanged 이벤트 추가, TakeDamage/OnSpawn에서 발행, Data 프로퍼티 추가 |
| `Wave/WaveManager.cs` | OnMonsterReachedBottom 이벤트 추가, CheckGameOver 로직 변경 |

---

## 주의사항

1. **DOTween 미설치**: STEP 0 수동 설치 완료 전에 STEP 1~12 구현 불가. DOTween Setup Utility 실행 필수.

2. **씬/프리팹 작업**: STEP 5의 UIButton 부착, 각 패널의 CanvasGroup 컴포넌트 추가는 코드 외 Inspector 작업이 필요하다. 코드 작성 완료 후 씬 작업을 별도로 진행해야 한다.

3. **CheckGameOver 변경 영향**: STEP 8에서 `WaveManager.CheckGameOver()`가 `GameManager.EndGame(false)` 직접 호출을 제거하고 이벤트를 발행하는 방식으로 바뀐다. `CharacterManager`(STEP 9)가 HP 0일 때 EndGame을 호출하므로, STEP 8과 STEP 9는 반드시 함께 구현해야 한다.

4. **MonsterBase.Data 프로퍼티**: `CharacterManager`가 `monster.Data.Damage`, `monster.Data.Reward`에 접근하기 위해 `MonsterBase`에 `public MonsterData Data => _monsterData` 프로퍼티가 필요하다. STEP 6과 함께 추가한다.

5. **SkillSelectionPanel 트리거 정리**: UIManager의 `OnWaveCleared` 구독 제거(STEP 3) 후에는 스킬 선택 트리거가 `OnKillCountReached`만 남는다. 웨이브 클리어 시 스킬 선택을 열어야 한다면 별도로 논의가 필요하다.

6. **구현 순서**: STEP 6(MonsterBase) → STEP 8(WaveManager) → STEP 9(CharacterManager) 순서를 지켜야 컴파일 에러 없이 진행된다. 나머지 STEP은 순서 무관.

---

이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.
