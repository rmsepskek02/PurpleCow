# Plan — Ball 시스템 구현

이 문서는 research.md를 바탕으로 Ball 시스템 3개 클래스(BallData, Ball, BallLauncher)의 구체적인 구현 계획을 정리한 것입니다.
Core 시스템(Singleton, ObjectPool, IPoolable, GameManager, InputHandler)이 이미 완성된 상태에서
Normal 볼의 발사·이동·반사·충돌·데미지·풀링 전체 흐름을 구현합니다.
특수 볼 타입(Fire, Ice, Ghost, Laser, Cluster)은 이번 범위에서 제외하며 스킬 시스템 task에서 확장합니다.

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**

---

## 구현 목표

- Normal 볼 1개가 발사 → 이동 → 벽 반사 → 몬스터 충돌 시 데미지 → 바닥 도달 시 풀 반납까지 동작
- BallLauncher가 InputHandler/GameManager와 연동하여 올바른 타이밍에만 발사
- BallData ScriptableObject로 수치 하드코딩 금지

---

## 생성 파일 목록

| 파일 | 경로 |
|------|------|
| `BallData.cs` | `Assets/_Project/Scripts/Data/BallData.cs` |
| `Ball.cs` | `Assets/_Project/Scripts/Ball/Ball.cs` |
| `BallLauncher.cs` | `Assets/_Project/Scripts/Ball/BallLauncher.cs` |
| `BallSetupEditor.cs` | `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` |

---

## 단계별 구현 계획

### Step 1. BallData (ScriptableObject)

**파일:** `Assets/_Project/Scripts/Data/BallData.cs`

**역할:** Ball의 기본 수치 데이터를 읽기 전용으로 보관하는 ScriptableObject.
런타임에 직접 수정하지 않으며, Ball.cs에서 참조만 한다.

**클래스 구조:**
```csharp
[CreateAssetMenu(fileName = "BallData", menuName = "PurpleCow/BallData")]
public class BallData : ScriptableObject
{
    [SerializeField] private float _damage;
    [SerializeField] private float _speed;
    [SerializeField] private float _criticalChance;       // 0 ~ 1
    [SerializeField] private float _criticalMultiplier;   // 예: 2.0 = 200%

    public float Damage           => _damage;
    public float Speed            => _speed;
    public float CriticalChance   => _criticalChance;
    public float CriticalMultiplier => _criticalMultiplier;
}
```

**네이밍 규칙 적용:**
- private 필드: `_camelCase` (SerializeField)
- 프로퍼티: `PascalCase` (읽기 전용)
- CreateAssetMenu 경로: `PurpleCow/BallData`

---

### Step 2. Ball (MonoBehaviour + IPoolable)

**파일:** `Assets/_Project/Scripts/Ball/Ball.cs`

**역할:** 볼 1개의 이동·반사·충돌·데미지를 처리한다. IPoolable을 구현하여 ObjectPool에 의해 관리된다.

**필드:**
```
[SerializeField] private BallData _ballData      // Inspector에서 SO 연결
private Rigidbody2D _rigidbody
private bool _isActive                           // 이동 활성화 여부
```

**이벤트:**
```
public static event Action<float, bool> OnHitMonster
// 파라미터: (데미지량, 치명타여부)
// 몬스터 충돌 시 발행 — MonsterHP가 구독 예정
```

**메서드:**

| 메서드 | 설명 |
|--------|------|
| `Awake()` | `_rigidbody = GetComponent<Rigidbody2D>()` 캐싱 |
| `OnSpawn()` | `_isActive = true`, velocity 초기화 |
| `OnDespawn()` | `_isActive = false`, velocity = Vector2.zero |
| `Launch(Vector2 direction)` | `_rigidbody.linearVelocity = direction * _ballData.Speed` |
| `FixedUpdate()` | `_isActive`일 때 velocity 유지 (물리 감쇠 방지용 선택적) |
| `OnCollisionEnter2D(Collision2D)` | 충돌 태그 분기 처리 |
| `CalculateDamage()` | 치명타 여부 계산 후 최종 데미지 반환 |
| `ReturnToPool()` | `BallLauncher.Instance`를 통해 풀 반납 |

**충돌 처리 상세 (OnCollisionEnter2D):**
```
충돌 대상 Tag == "Monster"
    → CalculateDamage() 호출
    → OnHitMonster 발행 (데미지, 치명타여부)
    → 볼은 반사 지속 (관통 없음, 기본 물리 반사)

충돌 대상 Tag == "Wall"
    → 물리 반사 (Rigidbody2D PhysicsMaterial2D bounciness=1 로 처리)
    → 별도 코드 불필요 (Physics Material로 해결)

충돌 대상 Tag == "Ground" (바닥)
    → ReturnToPool() 호출
```

**데미지 계산 (CalculateDamage):**
```csharp
private (float damage, bool isCritical) CalculateDamage()
{
    bool isCritical = Random.value < _ballData.CriticalChance;
    float damage = isCritical
        ? _ballData.Damage * _ballData.CriticalMultiplier
        : _ballData.Damage;
    return (damage, isCritical);
}
```

**IPoolable 구현:**
- `OnSpawn()`: 활성화 시 상태 초기화
- `OnDespawn()`: 비활성화 시 velocity 정지, `_isActive = false`

**네이밍 규칙 적용:**
- private 필드: `_camelCase`
- 이벤트: `OnHitMonster` (PascalCase, static event)
- 메서드: `PascalCase`
- 코루틴 없음 (이번 단계에서 불필요)

---

### Step 3. BallLauncher (Singleton)

**파일:** `Assets/_Project/Scripts/Ball/BallLauncher.cs`

**역할:** ObjectPool<Ball>을 소유하고, InputHandler 이벤트를 구독하여 볼을 발사한다.
GameState가 Playing일 때만 발사를 허용한다.

**필드:**
```
[SerializeField] private Ball _ballPrefab        // Inspector에서 프리팹 연결
[SerializeField] private Transform _poolParent   // 풀 오브젝트 부모 Transform
[SerializeField] private int _initialPoolSize    // 초기 풀 사이즈 (기본값 10)
[SerializeField] private Transform _launchPoint  // 발사 위치 Transform

private ObjectPool<Ball> _ballPool
private Vector2 _launchDirection
private bool _canLaunch                          // 발사 가능 여부 (GameState 연동)
```

**이벤트:**
```
public static event Action OnAllBallsReturned
// 모든 볼이 풀에 반납된 후 발행 — WaveManager / TurnManager가 구독 예정

private int _activeBallCount                     // 현재 활성 볼 수 추적용
```

**메서드:**

| 메서드 | 설명 |
|--------|------|
| `Awake()` | `_ballPool = new ObjectPool<Ball>(...)` 풀 초기화 |
| `OnEnable()` | `InputHandler.OnDrag += HandleDrag`, `InputHandler.OnRelease += HandleRelease`, `GameManager.OnGameStateChanged += HandleGameState` |
| `OnDisable()` | 위 이벤트 해제 |
| `HandleDrag(Vector2 direction)` | `_launchDirection = direction` 갱신 (가이드라인 업데이트 연동 가능) |
| `HandleRelease()` | `_canLaunch`가 true이면 `LaunchBall()` 호출 |
| `LaunchBall()` | 풀에서 Ball 꺼내어 `_launchPoint` 위치에 배치 후 `ball.Launch(_launchDirection)` 호출, `_activeBallCount` 증가 |
| `HandleGameState(GameState)` | Playing → `_canLaunch = true`, 그 외 → `_canLaunch = false` |
| `ReturnBall(Ball ball)` | `_ballPool.Return(ball)`, `_activeBallCount` 감소, 0이 되면 `OnAllBallsReturned` 발행 |

**Singleton 상속:**
```csharp
public class BallLauncher : Singleton<BallLauncher>
```

**이벤트 구독/해제 규칙 (DevRules.md 준수):**
- `OnEnable()` 에서 구독
- `OnDisable()` 에서 해제

**네이밍 규칙 적용:**
- private 필드: `_camelCase`
- 이벤트: `OnAllBallsReturned` (PascalCase, static event)
- 메서드: `PascalCase`, `Handle` 접두어로 이벤트 핸들러 명명

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 경로 |
|------|------|------|
| 신규 생성 | `BallData.cs` | `Assets/_Project/Scripts/Data/` |
| 신규 생성 | `Ball.cs` | `Assets/_Project/Scripts/Ball/` |
| 신규 생성 | `BallLauncher.cs` | `Assets/_Project/Scripts/Ball/` |
| 신규 생성 | `BallSetupEditor.cs` | `Assets/_Project/Scripts/Editor/` |

기존 Core 파일은 수정하지 않습니다.

---

### Step 4. BallSetupEditor (Editor 전용 자동화 스크립트)

**파일:** `Assets/_Project/Scripts/Editor/BallSetupEditor.cs`

**역할:** Ball 시스템 초기 세팅(태그 등록, PhysicsMaterial2D 생성, BallData 에셋 생성)을 Unity Editor 메뉴에서 한 번에 실행할 수 있도록 자동화하는 Editor 전용 스크립트.

**MenuItem 경로:** `PurpleCow/Setup/Ball System Setup`

**수행 작업:**

1. **Tag 등록** (`"Monster"`, `"Wall"`, `"Ground"`)
   - `UnityEditorInternal.InternalEditorUtility.tags`로 현재 태그 목록 확인
   - 없는 태그만 `SerializedObject`(TagManager)를 통해 추가

2. **PhysicsMaterial2D 생성**
   - 경로: `Assets/_Project/Physics/BallBounce.physicsMaterial2D`
   - `bounciness = 1f`, `friction = 0f`
   - `AssetDatabase.CreateAsset`으로 생성 (이미 존재하면 스킵)

3. **BallData ScriptableObject 에셋 생성**
   - 경로: `Assets/_Project/Data/BallData.asset`
   - 기본값: `damage=10`, `speed=10`, `criticalChance=0.1f`, `criticalMultiplier=2f`
   - `ScriptableObject.CreateInstance<BallData>`로 생성 (이미 존재하면 스킵)

**마무리:** `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()` 호출 후 완료 로그 출력

---

## 주의사항

1. **BallSetupEditor 실행 필요**
   구현 후 Unity Editor 메뉴 `PurpleCow > Setup > Ball System Setup` 실행으로 태그/PhysicsMaterial2D/BallData 에셋 자동 생성.
   Ball 프리팹 생성 시 Rigidbody2D에 `Collision Detection: Continuous`, `Gravity Scale: 0` 설정은 여전히 프리팹에서 직접 지정해야 합니다.

2. **OnHitMonster 이벤트는 현재 구독자 없음**
   Monster 시스템이 미구현 상태이므로 `OnHitMonster`는 발행만 하고 구독자 없이 컴파일됩니다.
   Monster 시스템 구현 시 구독 로직 추가 예정입니다.

3. **OnAllBallsReturned 이벤트는 현재 구독자 없음**
   WaveManager / TurnManager 미구현 상태이므로 마찬가지로 발행만 합니다.

4. **발사 방향 보정 미포함**
   이번 구현에서는 InputHandler가 제공하는 방향 벡터를 그대로 사용합니다.
   화면 좌표 → 월드 좌표 변환이 필요한 경우 BallLauncher.HandleDrag에서 Camera.main.ScreenToWorldPoint 처리를 추가해야 할 수 있습니다. (InputHandler 현 구현 확인 필요)

5. **DevRules.md 네이밍 규칙 준수**
   - private 필드: `_camelCase`
   - SerializeField: Inspector 노출 필요한 값만 (밸런스 수치는 BallData SO로 분리)
   - 이벤트: C# event, static event 활용
   - 이벤트 구독/해제: OnEnable/OnDisable 쌍

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
