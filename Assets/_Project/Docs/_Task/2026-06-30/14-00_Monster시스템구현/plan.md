# Plan — Monster 시스템 구현

이 문서는 research.md를 바탕으로 Monster 시스템 5개 클래스(MonsterData, MonsterBase, WaveData, WaveManager, MonsterSetupEditor)의 구체적인 구현 계획을 정리한 것입니다.
Core 시스템과 Ball 시스템이 모두 완성된 상태에서 몬스터 HP 관리, 볼 충돌 데미지 수신, 웨이브 스폰 및 턴 진행 전체 흐름을 구현합니다.
스킬 시스템 및 UI 시스템은 이번 범위에서 제외합니다.

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**

---

## 구현 목표

- 몬스터가 그리드 위에 배치되고, Ball 충돌 시 HP 감소 → 0이 되면 사망(풀 반납)
- WaveManager가 BallLauncher.OnAllBallsReturned를 구독하여 볼 턴 종료 시 몬스터를 아래로 전진
- 몬스터가 바닥 경계에 도달하면 GameManager.EndGame(false) 호출
- MonsterData / WaveData ScriptableObject로 수치 하드코딩 금지

---

## 단계별 작업 계획

### Step 1. MonsterData (ScriptableObject)

**파일:** `Assets/_Project/Scripts/Data/MonsterData.cs`

**역할:** 몬스터 수치 데이터를 읽기 전용으로 보관하는 ScriptableObject. 런타임에 직접 수정하지 않으며 MonsterBase가 참조만 한다.

**클래스 구조:**
```csharp
[CreateAssetMenu(fileName = "MonsterData", menuName = "PurpleCow/MonsterData")]
public class MonsterData : ScriptableObject
{
    [SerializeField] private float _hp;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int   _damage;       // 바닥 도달 시 플레이어에게 주는 피해 (게임 오버 판정용)
    [SerializeField] private int   _reward;       // 처치 시 획득 점수

    public float Hp        => _hp;
    public float MoveSpeed => _moveSpeed;
    public int   Damage    => _damage;
    public int   Reward    => _reward;
}
```

**네이밍 규칙:**
- private 필드: `_camelCase` (SerializeField)
- 프로퍼티: `PascalCase` (읽기 전용)
- CreateAssetMenu 경로: `PurpleCow/MonsterData`

---

### Step 2. MonsterBase (MonoBehaviour + IPoolable)

**파일:** `Assets/_Project/Scripts/Monster/MonsterBase.cs`

**역할:** 개별 몬스터 1개의 HP 관리, 볼 충돌 감지, 사망 처리를 담당한다. IPoolable을 구현하여 ObjectPool에 의해 관리된다.

**필드:**
```
[SerializeField] private MonsterData _monsterData   // Inspector에서 SO 연결
private float _currentHp                            // 런타임 HP (MonsterData는 읽기 전용)
private bool _isDead
```

**프로퍼티:**
```
public float CurrentHp => _currentHp
public bool IsAlive    => !_isDead
```

**이벤트:**
```
public static event Action<MonsterBase> OnMonsterDied
// 파라미터: 사망한 MonsterBase 인스턴스
// WaveManager가 구독하여 생존 몬스터 수 추적 및 풀 반납 처리
```

**메서드:**

| 메서드 | 설명 |
|--------|------|
| `OnSpawn()` | `_currentHp = _monsterData.Hp`, `_isDead = false` 초기화 |
| `OnDespawn()` | `_isDead = true`, 상태 정리 |
| `OnEnable()` | `Ball.OnHitMonster += HandleHitMonster` 구독 |
| `OnDisable()` | `Ball.OnHitMonster -= HandleHitMonster` 해제 |
| `TakeDamage(float damage)` | `_currentHp -= damage`, 0 이하이면 Die() 호출 |
| `Die()` | `_isDead = true`, `OnMonsterDied?.Invoke(this)` 발행 |
| `MoveDown(float distance)` | `transform.position += Vector2.down * distance` (WaveManager 호출) |
| `OnCollisionEnter2D(Collision2D)` | Ball 태그 감지 시 별도 처리 없음 (static event로 데미지 수신) |
| `HandleHitMonster(float damage, bool isCritical)` | Ball.OnHitMonster 구독 핸들러 — 단, 현재 충돌 중인 Ball과 자신을 연관짓는 로직 필요 (하단 주의사항 참조) |

**IPoolable 구현:**
- `OnSpawn()`: HP 초기화, `_isDead = false`
- `OnDespawn()`: `_isDead = true`, 게임오브젝트 비활성화 전 정리

**충돌 데미지 처리 방식:**

Ball.OnHitMonster는 static event이므로 어느 몬스터가 맞았는지 직접 특정할 수 없습니다.
MonsterBase는 `OnCollisionEnter2D`에서 Ball 태그를 직접 감지하여 해당 순간의 데미지를 가져옵니다.

```csharp
// MonsterBase.cs 충돌 처리 구조
private void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Ball"))
    {
        // Ball.OnHitMonster 이벤트는 Ball.OnCollisionEnter2D에서 발행됨
        // 동일 충돌 이벤트 프레임 내에 static 필드로 마지막 데미지를 캐싱하는 방식
        // 또는: Ball 컴포넌트에 LastDamage public 프로퍼티 추가 후 직접 참조
        TakeDamage(collision.gameObject.GetComponent<Ball>().LastDamage);
    }
}
```

Ball.cs에 `public float LastDamage { get; private set; }` 프로퍼티를 추가하고,
`CalculateDamage()` 호출 후 결과를 `LastDamage`에 캐싱하는 방식으로 연동합니다.
이 방식이 가장 단순하고 같은 프레임 보장도 명확합니다.

**네이밍 규칙:**
- private 필드: `_camelCase`
- 이벤트: `OnMonsterDied` (PascalCase, static event)
- 코루틴 없음 (이번 단계에서 불필요)
- 이벤트 구독/해제: `OnEnable / OnDisable` 쌍

---

### Step 3. WaveData (ScriptableObject)

**파일:** `Assets/_Project/Scripts/Data/WaveData.cs`

**역할:** 웨이브 1개의 몬스터 구성(어떤 MonsterData를 어떤 그리드 위치에 배치할지)을 정의하는 ScriptableObject.

**중첩 클래스 (WaveData 안에 정의):**
```csharp
[System.Serializable]
public class MonsterSpawnEntry
{
    public MonsterData Data;       // 어떤 종류의 몬스터인지
    public Vector2Int GridPosition; // 그리드 좌표 (열, 행)
}
```

**클래스 구조:**
```csharp
[CreateAssetMenu(fileName = "WaveData", menuName = "PurpleCow/WaveData")]
public class WaveData : ScriptableObject
{
    [SerializeField] private int _waveNumber;
    [SerializeField] private List<MonsterSpawnEntry> _spawnEntries;

    public int WaveNumber               => _waveNumber;
    public List<MonsterSpawnEntry> SpawnEntries => _spawnEntries;
}
```

**네이밍 규칙:**
- 중첩 Serializable 클래스: `PascalCase`
- public 필드(Serializable 클래스 내): `PascalCase`
- CreateAssetMenu 경로: `PurpleCow/WaveData`

---

### Step 4. WaveManager (Singleton)

**파일:** `Assets/_Project/Scripts/Wave/WaveManager.cs`

**역할:** WaveData 배열을 순서대로 처리하며 몬스터를 스폰하고, BallLauncher.OnAllBallsReturned를 구독하여 턴마다 몬스터를 전진시킨다. 게임 종료 조건(몬스터 바닥 도달)도 여기서 판단한다.

**필드:**
```
[SerializeField] private WaveData[] _waveDatas          // Inspector에서 WaveData 에셋 배열 연결
[SerializeField] private MonsterBase _monsterPrefab     // Inspector에서 프리팹 연결
[SerializeField] private Transform _poolParent          // 풀 오브젝트 부모 Transform
[SerializeField] private int _initialPoolSize           // 초기 풀 사이즈 (기본값 20)
[SerializeField] private Transform _spawnRoot           // 그리드 스폰 기준점 Transform
[SerializeField] private float _gridCellSize            // 셀 1칸 크기 (기본값 1.0f)
[SerializeField] private float _monsterMoveDistance     // 턴당 몬스터 전진 거리
[SerializeField] private float _bottomBoundaryY         // 게임 오버 판정 Y좌표

private ObjectPool<MonsterBase> _monsterPool
private List<MonsterBase> _activeMonsters               // 현재 살아있는 몬스터 목록
private int _currentWaveIndex
```

**이벤트:**
```
public static event Action<int> OnWaveStarted
// 파라미터: 웨이브 번호
// UI/GameManager 연동 예정

public static event Action OnAllWavesCleared
// 모든 웨이브 처리 완료 시 발행 → GameManager.EndGame(true) 호출 예정
```

**메서드:**

| 메서드 | 설명 |
|--------|------|
| `Awake()` | Singleton 초기화, `_monsterPool = new ObjectPool<MonsterBase>(...)` |
| `Start()` | `SpawnWave(_currentWaveIndex)` 호출로 첫 웨이브 시작 |
| `OnEnable()` | `BallLauncher.OnAllBallsReturned += HandleAllBallsReturned`, `MonsterBase.OnMonsterDied += HandleMonsterDied` |
| `OnDisable()` | 위 이벤트 해제 |
| `SpawnWave(int index)` | `_waveDatas[index]`의 SpawnEntries를 순회하며 몬스터 풀에서 꺼내어 그리드 위치에 배치, `OnWaveStarted` 발행 |
| `HandleAllBallsReturned()` | `MoveAllMonstersDown()` 호출 → 바닥 도달 몬스터 확인 → `CheckGameOver()` |
| `MoveAllMonstersDown()` | `_activeMonsters`의 각 MonsterBase에 `MoveDown(_monsterMoveDistance)` 호출 |
| `CheckGameOver()` | `_activeMonsters` 중 `transform.position.y <= _bottomBoundaryY`인 것이 있으면 `GameManager.Instance.EndGame(false)` |
| `HandleMonsterDied(MonsterBase monster)` | `_activeMonsters.Remove(monster)`, `_monsterPool.Return(monster)`, `CheckWaveCleared()` |
| `CheckWaveCleared()` | `_activeMonsters.Count == 0`이면 다음 웨이브 스폰 또는 `OnAllWavesCleared` 발행 |
| `AdvanceToNextWave()` | `_currentWaveIndex++`, 범위 내면 `SpawnWave(_currentWaveIndex)`, 초과면 `OnAllWavesCleared` 발행 |

**Singleton 상속:**
```csharp
public class WaveManager : Singleton<WaveManager>
{
    protected override void Awake()
    {
        base.Awake();
        _monsterPool = new ObjectPool<MonsterBase>(_monsterPrefab, _poolParent, _initialPoolSize);
    }
}
```

**이벤트 구독/해제 규칙 (DevRules.md 준수):**
- `OnEnable()` 에서 구독
- `OnDisable()` 에서 해제

---

### Step 5. MonsterSetupEditor (Editor 전용 자동화 스크립트)

**파일:** `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`

**역할:** Monster 시스템 초기 세팅(태그 확인, MonsterData/WaveData 샘플 에셋 생성)을 Unity Editor 메뉴에서 한 번에 실행. BallSetupEditor.cs와 동일한 패턴 적용.

**MenuItem 경로:** `PurpleCow/Setup/Monster System Setup`

**수행 작업:**

1. **Tag 확인** (`"Monster"` 태그)
   - BallSetupEditor에서 이미 등록되었을 가능성이 높으므로 중복 등록 방지 로직 포함
   - 없는 경우에만 SerializedObject(TagManager)를 통해 추가

2. **MonsterData ScriptableObject 에셋 생성 (샘플 4종)**
   - 경로: `Assets/_Project/Data/MonsterData_Fluffy.asset` 등
   - 기본값: `hp=30`, `moveSpeed=1`, `damage=1`, `reward=10`
   - 이미 존재하면 스킵

3. **WaveData ScriptableObject 에셋 생성 (샘플 1개)**
   - 경로: `Assets/_Project/Data/WaveData_Wave1.asset`
   - 빈 SpawnEntries로 생성 (Inspector에서 수동 편집 예정)
   - 이미 존재하면 스킵

**마무리:** `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()` 호출 후 완료 로그 출력

---

## Ball.cs 수정 사항

MonsterBase의 충돌 데미지 처리를 위해 Ball.cs에 `LastDamage` 프로퍼티를 추가합니다.

**변경 내용 (외과적 변경):**
```csharp
// Ball.cs — CalculateDamage() 내부에 캐싱 추가
public float LastDamage { get; private set; }

private (float damage, bool isCritical) CalculateDamage()
{
    bool isCritical = UnityEngine.Random.value < _ballData.CriticalChance;
    float damage = isCritical
        ? _ballData.Damage * _ballData.CriticalMultiplier
        : _ballData.Damage;
    LastDamage = damage;           // 캐싱
    OnHitMonster?.Invoke(damage, isCritical);
    return (damage, isCritical);
}
```

수정 대상 파일: `Assets/_Project/Scripts/Ball/Ball.cs` (1개, 소규모 변경)

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 경로 |
|------|------|------|
| 신규 생성 | `MonsterData.cs` | `Assets/_Project/Scripts/Data/` |
| 신규 생성 | `MonsterBase.cs` | `Assets/_Project/Scripts/Monster/` |
| 신규 생성 | `WaveData.cs` | `Assets/_Project/Scripts/Data/` |
| 신규 생성 | `WaveManager.cs` | `Assets/_Project/Scripts/Wave/` |
| 신규 생성 | `MonsterSetupEditor.cs` | `Assets/_Project/Scripts/Editor/` |
| 기존 수정 | `Ball.cs` | `Assets/_Project/Scripts/Ball/` |

기존 Core 파일 및 BallLauncher.cs는 수정하지 않습니다.

---

## 주의사항

1. **MonsterSetupEditor 실행 필요**
   구현 후 Unity Editor 메뉴 `PurpleCow > Setup > Monster System Setup` 실행으로 태그/"Monster" 확인 및 샘플 에셋 자동 생성.
   MonsterBase 프리팹 생성 시 Collider2D 컴포넌트의 Tag를 `"Monster"`로 설정해야 Ball.OnCollisionEnter2D의 충돌 분기가 정상 동작합니다.

2. **Ball.cs LastDamage 캐싱 의존**
   MonsterBase.OnCollisionEnter2D → `ball.LastDamage`로 데미지를 가져오는 방식입니다.
   같은 프레임 내 Ball.CalculateDamage → LastDamage 캐싱 → MonsterBase.TakeDamage 순서가 보장됩니다.
   단, 복수의 Ball이 같은 프레임에 같은 몬스터에 동시 충돌하는 경우 LastDamage가 마지막 값으로 덮어씌워질 수 있습니다. 이번 구현에서는 허용 범위로 처리합니다.

3. **WaveData는 Inspector에서 직접 편집**
   MonsterSetupEditor는 빈 WaveData 에셋만 생성합니다.
   실제 SpawnEntries(어떤 몬스터를 어디에 배치할지)는 Unity Inspector에서 직접 설정해야 합니다.

4. **그리드 좌표 계산**
   `SpawnWave()`에서 그리드 위치 계산:
   ```
   worldPosition = _spawnRoot.position + new Vector3(entry.GridPosition.x * _gridCellSize,
                                                      entry.GridPosition.y * _gridCellSize, 0)
   ```
   `_spawnRoot`는 씬의 스폰 기준점 Transform이며 Inspector에서 연결합니다.

5. **DevRules.md 네이밍 규칙 준수**
   - private 필드: `_camelCase`
   - SerializeField: Inspector 노출 필요한 값만 (밸런스 수치는 MonsterData/WaveData SO로 분리)
   - 이벤트: C# static event
   - 이벤트 구독/해제: OnEnable / OnDisable 쌍
   - 코루틴 명명 시: `Co` 접두어 사용 (이번 단계에서 코루틴 불필요)

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
