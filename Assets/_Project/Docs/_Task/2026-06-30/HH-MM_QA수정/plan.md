# Plan — QA 수정

이 문서는 QA 검토 결과 중 논의가 완료된 항목에 대한 구현 계획을 기술합니다.
현재는 CRITICAL 2(WaveData MonsterData 미반영), CRITICAL 3(스킬 선택 중 게임 일시정지 처리), CRITICAL 4(재시작 초기화 미구현), WARNING 2(패시브 스킬 레벨업 시 이벤트 이중 구독), WARNING 3(MonsterBase 빈 이벤트 핸들러 제거), WARNING 4(SkillSelectionPanel Hide() 이중 호출 수정), WARNING 6(OnEnable Singleton Instance 접근 → static 이벤트 전환) 일곱 건의 수정 계획이 확정되었으며, 나머지 항목은 논의 완료 후 순차적으로 STEP으로 추가될 예정입니다.

---

## 구현 목표

WaveManager.SpawnWave()에서 몬스터 스폰 직후 WaveData에 설정된 MonsterData를 실제로 주입하여, 20웨이브에 걸쳐 다양한 몬스터 스탯과 보상이 정상 반영되도록 한다.

---

## 단계별 작업 계획

### STEP 1 — CRITICAL 2: WaveData MonsterData 미반영 수정

**배경**

ObjectPool.Get()은 내부적으로 OnSpawn()을 호출하여 프리팹에 기본 설정된 MonsterData로 몬스터를 초기화한다. 이후 별도로 올바른 MonsterData를 덮어쓰지 않으면 WaveData에 지정된 스탯/보상/종류가 무시된다.

**수정 파일 1: `Assets/_Project/Scripts/Monster/MonsterBase.cs`**

- `public void ApplyData(MonsterData data)` 메서드를 추가한다.
- 메서드 내부 처리 순서:
  1. `_monsterData = data` — 내부 데이터 교체
  2. `_currentHp = _monsterData.Hp` — HP 재초기화
  3. `OnHpChanged?.Invoke(_currentHp, _monsterData.Hp)` — HP 변경 이벤트 발행

**수정 파일 2: `Assets/_Project/Scripts/Wave/WaveManager.cs`**

- `SpawnWave()` 메서드에서 `_monsterPool.Get()` 직후에 아래 조건부 호출을 추가한다.

```
if (entry.Data != null)
    monster.ApplyData(entry.Data);
```

- 기존 Get() 이후 코드 흐름은 변경하지 않는다.

---

### STEP 2 — CRITICAL 3: 스킬 선택 중 게임 일시정지 처리

**배경**

SkillSelectionPanel은 인게임 도중 열리기 때문에 패널이 열려 있는 동안 게임 로직(몬스터 이동, 공격 등)이 계속 진행되는 문제가 있다. ResultPanel과 HUDPanel은 게임이 이미 멈춘 상태(Result/Ready)에서 열리므로 해당 처리가 불필요하지만, SkillSelectionPanel만 이 처리가 필요하다.

**수정 파일 1: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`**

- `OpenPanel()` 메서드에 `Time.timeScale = 0f` 를 추가하여 패널 열림과 동시에 게임을 일시정지한다.
- `Show()` 및 `Hide()` 메서드의 DOTween Sequence에 `.SetUpdate(true)` 를 추가한다.
  - `Time.timeScale = 0`일 때 DOTween 기본 설정은 scaled time을 따르므로 애니메이션이 멈춘다. `.SetUpdate(true)`를 적용하면 unscaled time 기준으로 재생되어 timeScale에 무관하게 UI 애니메이션이 정상 동작한다.

**수정 파일 2: `Assets/_Project/Scripts/UI/UIManager.cs`**

- `OnSkillSelectionComplete()` 메서드에 `Time.timeScale = 1f` 를 추가하여 스킬 선택 완료 시 게임을 재개한다.

---

### STEP 3 — CRITICAL 4: 재시작 초기화 구현

**배경**

RestartGame() 호출 시 MonoBehaviour 기반 시스템(WaveManager, CharacterManager, SkillManager 등)은 씬 재로드로 전부 초기화된다. 단, SkillData는 ScriptableObject 에셋이라 씬 재로드로 리셋되지 않으므로, 씬 재로드 전에 SkillSelectionPanel이 명시적으로 레벨을 리셋해야 한다.

**수정 파일 1: `Assets/_Project/Scripts/Data/SkillData.cs`**

- `public void ResetLevel() { _currentLevel = 0; }` 메서드를 추가한다.

**수정 파일 2: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`**

- `GameManager.Instance.OnGameStateChanged`를 구독하여 `GameState.Ready` 상태 수신 시 `_allSkillDatas` 배열을 순회하며 `data.ResetLevel()`을 호출한다.

**수정 파일 3: `Assets/_Project/Scripts/Core/GameManager.cs`**

- `RestartGame()` 메서드에서 기존 GameState 변경 후 `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)` 호출을 추가한다.
- 파일 상단에 `using UnityEngine.SceneManagement;`를 추가한다.

---

### STEP 4 — WARNING 2: 패시브 스킬 레벨업 시 이벤트 이중 구독 수정

**배경**

`AddPassiveSkill()`의 레벨업 분기에서 `existing.Apply()`를 그대로 호출하면, 이벤트 구독 방식 패시브(MagicMirror, AmethystDagger, EmeraldDagger, LastMatch)는 같은 핸들러가 이벤트에 누적 등록되어 효과가 배수로 적용된다. WarmTinHeart는 `AddDamageMultiplier()`로 배율을 직접 더하는 방식이므로, `Remove()`로 기존 배율을 먼저 제거하지 않으면 레벨업할수록 배율이 누적된다. `Remove()` → `LevelUp()` → `Apply()` 순서로 처리해야 레벨 기준이 올바른 상태에서 새로 등록된다.

**수정 파일: `Assets/_Project/Scripts/Skill/SkillManager.cs`**

- `AddPassiveSkill()`의 레벨업 분기에서 `existing.Apply()` 호출 전에 `existing.Remove()`를 먼저 호출한다.

현재:
```
existing.SkillData.LevelUp(); existing.Apply(); return;
```

수정 후:
```
existing.Remove(); existing.SkillData.LevelUp(); existing.Apply(); return;
```

---

### STEP 5 — WARNING 3: MonsterBase 빈 이벤트 핸들러 제거

**배경**

원래 Ball이 OnHitMonster 이벤트를 발행하면 MonsterBase가 구독해서 TakeDamage()를 처리하는 설계였으나, Ball.CalculateDamage()에서 target.TakeDamage()를 직접 호출하는 방식으로 바뀌면서 핸들러 내부만 비워진 잔재 코드다. 불필요한 구독을 제거한다.

**수정 파일: `Assets/_Project/Scripts/Monster/MonsterBase.cs`**

- `OnEnable()`에서 `Ball.OnHitMonster += HandleHitMonster` 라인을 제거한다.
- `OnDisable()`에서 `Ball.OnHitMonster -= HandleHitMonster` 라인을 제거한다.
- 빈 `HandleHitMonster(float damage, bool isCritical)` 메서드 전체를 제거한다.

---

### STEP 6 — WARNING 4: SkillSelectionPanel Hide() 이중 호출 수정

**배경**

현재 OnSkillSelected()가 UIManager.OnSkillSelectionComplete()와 Hide()를 모두 호출해 DOTween Sequence가 두 번 실행된다. UIManager.OnSkillSelectionComplete() 내부에서 이미 ShowSkillSelection(false) → Hide()가 호출되므로 중복이다. 패널 닫기는 UIManager 한 곳에서만 처리하도록 통일한다.

**수정 파일: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`**

- `OnSkillSelected()`에서 직접 호출하는 `Hide()` 라인을 제거한다.

---

### STEP 7 — WARNING 6: OnEnable Singleton Instance 접근 → static 이벤트 전환

**배경**

현재 `GameManager.OnGameStateChanged`는 instance 이벤트이고, `InputHandler`의 `OnDrag`/`OnRelease`도 instance 이벤트다. `BallLauncher`, `ResultPanel`, `UIManager`는 이를 `OnEnable()`에서 구독하기 위해 `GameManager.Instance`, `InputHandler.Instance`에 접근한다. Unity는 오브젝트 활성화 순서를 보장하지 않으므로 Singleton의 `Awake()`보다 구독자의 `OnEnable()`이 먼저 실행되면 NullReferenceException이 발생할 수 있다.

프로젝트 전반(`WaveManager`, `MonsterBase`, `Ball` 등)은 이미 static 이벤트 패턴을 사용한다. `GameManager`와 `InputHandler`만 instance 이벤트를 사용해 아키텍처 일관성이 깨진 상태이며, `Start()`로 이동하는 방법은 `UIManager`의 동일 패턴을 해결하지 못해 불완전하다. static 이벤트로 전환하면 Instance 접근 자체가 불필요해져 근본 해결된다.

**수정 파일 1: `Assets/_Project/Scripts/Core/GameManager.cs`**

- `public event Action<GameState> OnGameStateChanged`를 `public static event Action<GameState> OnGameStateChanged`로 변경한다.
- 이벤트 발행 코드(`OnGameStateChanged?.Invoke(...)`)는 static이 되어도 동일하게 사용 가능하므로 수정 불필요.

**수정 파일 2: `Assets/_Project/Scripts/Core/InputHandler.cs`**

- `OnDrag`, `OnRelease` 이벤트를 `public static event`로 변경한다.
- 이벤트 발행 코드는 수정 불필요.

**수정 파일 3: `Assets/_Project/Scripts/Ball/BallLauncher.cs`**

- `OnEnable()`에서 `InputHandler.Instance.OnDrag +=`, `InputHandler.Instance.OnRelease +=`, `GameManager.Instance.OnGameStateChanged +=`를 각각 `InputHandler.OnDrag +=`, `InputHandler.OnRelease +=`, `GameManager.OnGameStateChanged +=`로 교체한다.
- `OnDisable()`도 동일하게 교체한다.

**수정 파일 4: `Assets/_Project/Scripts/UI/ResultPanel.cs`**

- `OnEnable()`에서 `GameManager.Instance.OnGameStateChanged +=`를 `GameManager.OnGameStateChanged +=`로 교체한다.
- `OnDisable()`도 동일하게 교체한다.

**수정 파일 5: `Assets/_Project/Scripts/UI/UIManager.cs`**

- `OnEnable()`에서 `GameManager.Instance.OnGameStateChanged +=`를 `GameManager.OnGameStateChanged +=`로 교체한다.
- `OnDisable()`도 동일하게 교체한다.

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 변경 내용 |
|------|------|-----------|
| 수정 | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `ApplyData(MonsterData data)` 메서드 추가 |
| 수정 | `Assets/_Project/Scripts/Wave/WaveManager.cs` | `SpawnWave()` 내 `ApplyData()` 호출 추가 |
| 수정 | `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | `OpenPanel()`에 `Time.timeScale = 0f` 추가, `Show()`/`Hide()` Sequence에 `.SetUpdate(true)` 추가 |
| 수정 | `Assets/_Project/Scripts/UI/UIManager.cs` | `OnSkillSelectionComplete()`에 `Time.timeScale = 1f` 추가 |
| 수정 | `Assets/_Project/Scripts/Data/SkillData.cs` | `ResetLevel()` 메서드 추가 |
| 수정 | `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | GameState.Ready 수신 시 전체 스킬 레벨 리셋 로직 추가 |
| 수정 | `Assets/_Project/Scripts/Core/GameManager.cs` | `RestartGame()`에 `SceneManager.LoadScene()` 추가, `using UnityEngine.SceneManagement` 추가 |
| 수정 | `Assets/_Project/Scripts/Skill/SkillManager.cs` | `AddPassiveSkill()` 레벨업 분기에 `existing.Remove()` 호출 추가 |
| 수정 | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `OnEnable()`/`OnDisable()` 내 `OnHitMonster` 구독/해제 라인 제거, 빈 `HandleHitMonster()` 메서드 제거 |
| 수정 | `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | `OnSkillSelected()` 내 직접 `Hide()` 호출 라인 제거 |
| 수정 | `Assets/_Project/Scripts/Core/GameManager.cs` | `OnGameStateChanged`를 `static event`로 변경 |
| 수정 | `Assets/_Project/Scripts/Core/InputHandler.cs` | `OnDrag`, `OnRelease`를 `static event`로 변경 |
| 수정 | `Assets/_Project/Scripts/Ball/BallLauncher.cs` | `OnEnable`/`OnDisable`에서 Instance 접근 제거, static 이벤트 직접 참조로 교체 |
| 수정 | `Assets/_Project/Scripts/UI/ResultPanel.cs` | `OnEnable`/`OnDisable`에서 Instance 접근 제거, static 이벤트 직접 참조로 교체 |
| 수정 | `Assets/_Project/Scripts/UI/UIManager.cs` | `OnEnable`/`OnDisable`에서 Instance 접근 제거, static 이벤트 직접 참조로 교체 |

---

## 주의사항

- `ApplyData()`는 `OnSpawn()` 이후에 호출되므로, `OnSpawn()` 내부의 초기화 로직과 충돌하지 않는지 확인한다.
- `entry.Data != null` 조건을 반드시 포함하여 MonsterData가 지정되지 않은 항목은 프리팹 기본값을 유지하도록 한다.
- HP 재초기화 후 `OnHpChanged` 이벤트를 발행해야 HP UI(체력바 등)가 즉시 갱신된다.

---

## 논의 예정 항목

아래 항목들은 수정 방향이 아직 확정되지 않았습니다. 논의 완료 후 각 항목이 STEP으로 이 plan.md에 추가될 예정입니다.

- ~~**WARNING 6:** OnEnable에서 Singleton Instance 직접 접근~~ → STEP 7로 확정
- **INFO 2:** WaveData 20개 미생성
- **INFO 3:** DamageTextManager Ball.OnHitMonster 미연결 (시그니처 변경 필요)
