# Plan — QA 수정

이 문서는 QA 검토 결과 중 논의가 완료된 항목에 대한 구현 계획을 기술합니다.
CRITICAL 2·3·4·5, WARNING 2·3·4·6, INFO 2·3 총 10건의 수정 계획이 STEP 1~10으로 확정되었습니다. WARNING 5는 의도된 구현으로 수정 불필요, INFO 1은 PDF스펙정합 작업에서 이미 처리된 중복 항목입니다.

---

## 구현 목표

QA 검토에서 확인된 버그·누락 기능 10건을 수정한다.
- **CRITICAL 2·3·4·5**: 몬스터 데이터 미주입, 스킬 선택 중 게임 진행, 재시작 미초기화, Ball 스킬 인스턴스 공유
- **WARNING 2·3·4·6**: 패시브 이벤트 이중 구독, 빈 핸들러 잔재, Hide() 이중 호출, OnEnable Instance 접근
- **INFO 2·3**: WaveData 20개 미생성, DamageTextManager 미연결

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

- `GameManager.OnGameStateChanged`를 구독하여 `GameState.Ready` 상태 수신 시 `_allSkillDatas` 배열을 순회하며 `data.ResetLevel()`을 호출한다. (STEP 7에서 static으로 전환되므로 Instance 접근 없이 직접 참조)

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

### STEP 8 — INFO 2: WaveData 20개 생성

**배경**

`MonsterSetupEditor.CreateWaveDataAsset()`이 `WaveData_Wave1.asset` 1개만 생성한다. `WaveManager._waveDatas[]`에 할당된 WaveData가 1개뿐이므로 1웨이브 후 즉시 스테이지가 클리어된다. 각 웨이브별 몬스터 구성(종류, 수, 배치)은 이후 Inspector에서 편집할 수 있도록 에셋만 먼저 생성한다.

**수정 파일: `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`**

- `CreateWaveDataAsset()` 메서드를 `CreateWaveDataAssets()`로 교체한다.
- Wave1~Wave20 총 20개의 에셋을 생성한다. 각 에셋은:
  - 파일 경로: `Assets/_Project/Data/WaveData_Wave{N}.asset`
  - `_waveNumber` 필드를 N(1~20)으로 설정
  - `_spawnEntries`는 빈 상태로 둠 — Inspector에서 직접 편집
- 이미 존재하는 에셋은 스킵한다.
- 메서드 호출부(`SetupMonsterSystem()`에서 `CreateWaveDataAsset()` → `CreateWaveDataAssets()`)도 함께 변경한다.

**Inspector 편집 방법**

생성된 WaveData_Wave1 ~ WaveData_Wave20 에셋을 열면 `_spawnEntries` 리스트를 통해 각 웨이브에 몬스터를 추가할 수 있다. 각 항목에 `MonsterData` 에셋을 연결하고 `GridPosition`(x, y)을 설정하면 해당 위치에 몬스터가 스폰된다.

---

### STEP 9 — INFO 3: DamageTextManager Ball.OnHitMonster 연결

**배경**

`Ball.OnHitMonster`의 시그니처가 `Action<float, bool>`(damage, isCritical)이라 위치 정보가 없다. `DamageTextManager.ShowDamage()`는 `Vector3 worldPos`를 필요로 하므로 현재 상태에서는 연결이 불가능하다. 게임 중 데미지 숫자 텍스트가 전혀 표시되지 않는다.

**수정 파일 1: `Assets/_Project/Scripts/Ball/Ball.cs`**

- `OnHitMonster` 이벤트 시그니처를 `Action<float, bool>` → `Action<MonsterBase, float, bool>`로 변경한다.
- `CalculateDamage()` 마지막 줄의 `OnHitMonster?.Invoke(damage, isCritical)`를 `OnHitMonster?.Invoke(target, damage, isCritical)`로 수정한다.

**수정 파일 2: `Assets/_Project/Scripts/UI/DamageTextManager.cs`**

- `OnEnable()`을 추가하여 `Ball.OnHitMonster += HandleHitMonster`를 구독한다.
- `OnDisable()`을 추가하여 `Ball.OnHitMonster -= HandleHitMonster`를 해제한다.
- `HandleHitMonster(MonsterBase monster, float damage, bool isCritical)` 메서드를 추가하여 `ShowDamage(monster.transform.position, damage, isCritical)`를 호출한다.

---

### STEP 10 — CRITICAL 5: 스킬 레벨업 시 Ball 인스턴스 공유 문제 수정

**배경**

`BallSkillBase`는 `protected Ball _ball` 필드를 가지며 `Initialize(Ball ball)`에서 설정된다. `SkillManager.ApplySkillToBall()`이 같은 `BallSkillBase` 인스턴스를 여러 Ball에 전달하므로, Ball마다 `Initialize(this)`가 호출될 때마다 `_ball` 참조가 마지막 Ball로 덮어씌워진다. GhostBallSkill처럼 `_ball`을 직접 조작하는 스킬의 경우 한 Ball의 OnActivate/OnDeactivate가 다른 Ball의 상태에 영향을 준다.

**수정 파일: `Assets/_Project/Scripts/Skill/SkillManager.cs`**

- `ApplySkillToBall()` 내부에서 `ball.AddSkill(skill)` 대신 `ball.AddSkill(SkillFactory.CreateActiveSkill(skill.SkillData))`를 호출하도록 변경한다.
- 각 Ball이 독립된 인스턴스를 가지므로 `_ball` 참조 충돌이 없어진다.
- `SkillData`는 공유 참조이므로 레벨업 후 생성된 인스턴스도 자동으로 새 레벨 데이터를 반영한다.

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
| 수정 | `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` | `CreateWaveDataAsset()` → `CreateWaveDataAssets()`로 교체, Wave1~Wave20 에셋 생성 |
| 수정 | `Assets/_Project/Scripts/Ball/Ball.cs` | `OnHitMonster` 시그니처 `Action<float, bool>` → `Action<MonsterBase, float, bool>` 변경, `Invoke` 호출에 `target` 추가 |
| 수정 | `Assets/_Project/Scripts/UI/DamageTextManager.cs` | `OnEnable`/`OnDisable` 추가, `HandleHitMonster` 메서드 추가 |
| 수정 | `Assets/_Project/Scripts/Skill/SkillManager.cs` | `ApplySkillToBall()`에서 기존 인스턴스 재사용 → `SkillFactory.CreateActiveSkill()` 새 인스턴스 생성으로 변경 |

---

## 주의사항

- `ApplyData()`는 `OnSpawn()` 이후에 호출되므로, `OnSpawn()` 내부의 초기화 로직과 충돌하지 않는지 확인한다.
- `entry.Data != null` 조건을 반드시 포함하여 MonsterData가 지정되지 않은 항목은 프리팹 기본값을 유지하도록 한다.
- HP 재초기화 후 `OnHpChanged` 이벤트를 발행해야 HP UI(체력바 등)가 즉시 갱신된다.

---

## 논의 예정 항목

아래 항목들은 수정 방향이 아직 확정되지 않았습니다. 논의 완료 후 각 항목이 STEP으로 이 plan.md에 추가될 예정입니다.

- ~~**WARNING 6:** OnEnable에서 Singleton Instance 직접 접근~~ → STEP 7로 확정
- ~~**INFO 2:** WaveData 20개 미생성~~ → STEP 8로 확정
- ~~**INFO 3:** DamageTextManager Ball.OnHitMonster 미연결~~ → STEP 9로 확정
- ~~**CRITICAL 5:** 스킬 레벨업 시 Ball 인스턴스 공유 문제~~ → STEP 10으로 확정
- **INFO 1:** BallData 기본값 → PDF스펙정합 STEP 2에서 이미 처리됨, 중복
- **WARNING 5:** ClusterBall 서브볼 발사 방향 → 몬스터가 위쪽에 위치하는 게임 구조상 의도된 구현, 수정 불필요
