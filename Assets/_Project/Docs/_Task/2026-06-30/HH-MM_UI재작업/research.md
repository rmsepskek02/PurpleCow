# Research — UI 전체 재작업

현재 구현된 UI 스크립트 5종(UIManager, HUDPanel, ResultPanel, SkillSelectionPanel, SkillCardUI)이
UIRules.md에서 정의한 규칙을 위반하고 있으며, 다수의 핵심 UI 컴포넌트가 미구현 상태다.
이 문서는 각 파일의 구체적인 문제점과 미구현 항목, 의존성 누락 현황을 기록한다.

---

## 현재 상태

### UI 스크립트 파일 목록

`Assets/_Project/Scripts/UI/` 하위에 존재하는 파일:

- `UIManager.cs`
- `HUDPanel.cs`
- `ResultPanel.cs`
- `SkillSelectionPanel.cs`
- `SkillCardUI.cs`

---

## 관련 파일 및 의존성

### MonsterData.cs

`Assets/_Project/Scripts/Data/MonsterData.cs`

```
_hp, _moveSpeed, _damage, _reward 필드 존재
public int Damage => _damage;
public int Reward => _reward;
```

→ `CharacterManager`가 참조할 `Damage`, `Reward` 프로퍼티는 이미 존재함.

### MonsterBase.cs

`Assets/_Project/Scripts/Monster/MonsterBase.cs`

- `public static event Action<MonsterBase> OnMonsterDied` — 존재
- `public event Action<float, float> OnHpChanged` — **없음** (추가 필요)
- `TakeDamage(float damage)` — HP 차감 후 Die() 호출. OnHpChanged 미발행
- `OnSpawn()` — `_currentHp = _monsterData.Hp` 초기화. OnHpChanged 미발행

### WaveManager.cs

`Assets/_Project/Scripts/Wave/WaveManager.cs`

- `public static event Action OnWaveCleared` — 존재
- `public static event Action OnKillCountReached` — 존재
- `public static event Action<MonsterBase> OnMonsterReachedBottom` — **없음** (추가 필요)
- `CheckGameOver()` — 바닥 도달 몬스터 발견 시 `GameManager.Instance.EndGame(false)` 직접 호출. 이벤트 발행 없음

### DOTween

`Packages/manifest.json` 및 `Assets/` 전체를 확인한 결과, `com.demigiant.dotween` 패키지가 **존재하지 않음**.

→ UIRules.md 규칙 5(UI 애니메이션)와 규칙 6(버튼 피드백)은 DOTween 의존. 구현 전 DOTween 설치가 선행되어야 함.

---

## 문제점 / 구현 대상 파악

### UIManager.cs 문제

파일 위치: `Assets/_Project/Scripts/UI/UIManager.cs`

**위반 1 — SetActive 사용 (UIRules 규칙 4)**

```csharp
// Awake()
_hudPanel.gameObject.SetActive(false);           // 위반
_resultPanel.gameObject.SetActive(false);        // 위반
_skillSelectionPanel.gameObject.SetActive(false);// 위반
```

모든 패널은 씬 시작부터 `SetActive(true)` 상태를 유지하고 CanvasGroup으로만 제어해야 함.

**위반 2 — ShowHUD/Result/SkillSelection 메서드**

```csharp
private void ShowHUD(bool show)            => _hudPanel.gameObject.SetActive(show);     // 위반
private void ShowResult(bool show)         => _resultPanel.gameObject.SetActive(show);  // 위반
private void ShowSkillSelection(bool show) => _skillSelectionPanel.gameObject.SetActive(show); // 위반
```

**버그 — 이중 트리거**

```csharp
WaveManager.OnWaveCleared += HandleWaveCleared;  // 구독

private void HandleWaveCleared()
{
    ShowSkillSelection(true);  // 스킬 선택 패널 열기
}
```

`SkillSelectionPanel.cs`의 `OnEnable()`에서는 `WaveManager.OnKillCountReached`로 스킬 선택을 트리거하도록 되어 있음.  
UIManager도 `OnWaveCleared`로 스킬 선택을 여는 로직이 남아 있어 **이중 트리거** 가능성 존재.

실제 `WaveManager.OnWaveCleared`는 웨이브 전체 클리어 시 발행되고,
`OnKillCountReached`는 킬카운트 달성 시 발행됨 — 두 경로 모두 스킬 선택을 열 수 있어 충돌 가능.

---

### HUDPanel.cs 문제

파일 위치: `Assets/_Project/Scripts/UI/HUDPanel.cs`

**위반 1 — SetActive 사용**

```csharp
_launchReadyIndicator.SetActive(false);  // Start()에서 — 위반
_launchReadyIndicator.SetActive(true);   // HandleWaveStarted()에서 — 위반
_launchReadyIndicator.SetActive(false);  // HandleAllBallsReturned()에서 — 위반
```

**미구현 — DOTween 애니메이션 없음**

Show/Hide 시 즉시 전환. UIRules 규칙 5 미준수.

**미구현 — 캐릭터 HP바 / XP바 없음**

UIRules.md 규칙 10에서 정의한 CharacterHpBar, CharacterXpBar가 HUDPanel에 포함되어야 하나 현재 없음.  
`_waveText`, `_scoreText`, `_launchReadyIndicator`만 존재.

---

### ResultPanel.cs 문제

파일 위치: `Assets/_Project/Scripts/UI/ResultPanel.cs`

**위반 1 — Unity 기본 Button 사용**

```csharp
[SerializeField] private Button _restartButton;
```

UIRules 규칙 6: 모든 버튼에 `UIButton` 컴포넌트를 부착해야 함. `UIButton.cs`가 존재하지 않음.

**미구현 — DOTween 애니메이션 없음**

Show 시 즉시 표시. UIRules 규칙 5 미준수.

**구조 문제 — CanvasGroup 없음**

CanvasGroup 필드 미존재. UIManager의 `SetActive` 기반 제어를 수동으로 처리함.

---

### SkillSelectionPanel.cs 문제

파일 위치: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`

**위반 1 — SetActive 사용**

```csharp
private void OpenPanel()
{
    gameObject.SetActive(true);  // 위반
    ShowRandomSkills();
}

private void OnSkillSelected(SkillData selectedData)
{
    ApplySkill(selectedData);
    UIManager.Instance.OnSkillSelectionComplete();
    gameObject.SetActive(false);  // 위반
}
```

**위반 2 — 카드 SetActive 사용**

```csharp
_skillCards[i].gameObject.SetActive(true);   // 위반
_skillCards[i].gameObject.SetActive(false);  // 위반
```

**타이밍 버그 — OnEnable에서 ShowRandomSkills 직접 호출**

```csharp
private void OnEnable()
{
    WaveManager.OnKillCountReached += OpenPanel;
    ShowRandomSkills();  // 문제: CanvasGroup 방식으로 전환하면 OnEnable은 씬 시작 시 한 번만 호출됨
}
```

CanvasGroup 방식에서 패널은 항상 활성화 상태이므로 `OnEnable`은 씬 시작 시에만 호출됨.
패널을 실제로 열 때마다 `ShowRandomSkills()`가 호출되지 않는 버그 발생 예정.

---

### SkillCardUI.cs 문제

파일 위치: `Assets/_Project/Scripts/UI/SkillCardUI.cs`

**위반 1 — Unity 기본 Button 사용**

```csharp
[SerializeField] private Button _selectButton;
```

UIRules 규칙 6 미준수. `UIButton` 컴포넌트 미사용.

---

## 미구현 항목

| 파일 | 경로 | 상태 |
|------|------|------|
| `UIButton.cs` | `Scripts/UI/UIButton.cs` | 없음 |
| `SafeAreaFitter.cs` | `Scripts/UI/SafeAreaFitter.cs` | 없음 |
| `MonsterHpBar.cs` | `Scripts/UI/MonsterHpBar.cs` | 없음 |
| `CharacterHpBar.cs` | `Scripts/UI/CharacterHpBar.cs` | 없음 |
| `CharacterXpBar.cs` | `Scripts/UI/CharacterXpBar.cs` | 없음 |
| `DamageTextFx.cs` | `Scripts/UI/DamageTextFx.cs` | 없음 |
| `DamageTextManager.cs` | `Scripts/UI/DamageTextManager.cs` | 없음 |
| `CharacterManager.cs` | `Scripts/Core/CharacterManager.cs` | 없음 |

---

## 결론

현재 UI 시스템은 아래 두 가지 범주의 문제를 가진다.

**규칙 위반 (5개 파일)**
- UIManager, HUDPanel, SkillSelectionPanel: `SetActive` 기반 패널 제어 (UIRules 규칙 4 위반)
- ResultPanel, SkillCardUI: UIButton 미사용 (UIRules 규칙 6 위반)
- HUDPanel, ResultPanel, SkillSelectionPanel: DOTween 애니메이션 없음 (UIRules 규칙 5 위반)
- UIManager: `OnWaveCleared` 구독으로 인한 스킬 선택 이중 트리거 버그

**미구현 항목 (8개 파일)**
- UIButton, SafeAreaFitter, MonsterHpBar, CharacterHpBar, CharacterXpBar, DamageTextFx, DamageTextManager, CharacterManager

**의존성 누락 (2개 이벤트 + 1개 패키지)**
- `MonsterBase.OnHpChanged` 이벤트 없음
- `WaveManager.OnMonsterReachedBottom` 이벤트 없음
- DOTween 패키지 미설치 — 구현 전 Asset Store 또는 수동 설치 필요
