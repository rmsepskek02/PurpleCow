# Plan — Skill 시스템 구현

이 문서는 research.md를 바탕으로 Skill 시스템 전체(SkillData SO, SkillManager, BallSkillBase/구체 5종, PassiveSkillBase/구체 7종, SkillSetupEditor)의 구체적인 구현 계획을 정리한 것입니다.
Core/Ball/Monster 시스템이 모두 완성된 상태에서 Active 스킬(볼 타입별 효과)과 Passive 스킬(전투 전반에 적용되는 보조 효과)을 이벤트 기반으로 연동하여 구현합니다.
Ball.cs 수정은 최소한으로 유지하며, 스킬 효과는 컴포넌트 부착 및 이벤트 훅 방식으로 주입합니다.

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**

---

## 구현 목표

- Active 스킬 5종(Fire/Ice/Ghost/Laser/Cluster)이 볼에 부착되어 충돌 시 고유 효과 발동
- Passive 스킬 7종이 전투 흐름(데미지 계산, 충돌, 턴 종료)의 이벤트에 자동으로 개입
- SkillManager가 현재 장착 스킬을 관리하고, 볼 발사 시점에 Active 스킬을 Ball에 주입
- SkillData ScriptableObject로 모든 스킬 수치 하드코딩 금지
- DevRules.md 네이밍/구조 규칙 완전 준수

---

## 단계별 작업 계획

### Step 1. SkillData (ScriptableObject)

**파일:** `Assets/_Project/Scripts/Data/SkillData.cs`

**역할:** 스킬 1개의 식별 정보와 수치 데이터를 읽기 전용으로 보관하는 ScriptableObject. 런타임에 직접 수정하지 않으며 각 스킬 클래스에서 참조만 한다.

**열거형 (같은 파일 내 정의):**
```csharp
public enum SkillType
{
    Active,
    Passive
}

public enum ActiveSkillId
{
    Fire    = 1001,
    Ice     = 1002,
    Ghost   = 1003,
    Laser   = 1004,
    Cluster = 1005
}

public enum PassiveSkillId
{
    DamageUp        = 3000,
    CritChanceUp    = 3002,
    CritDamageUp    = 3003,
    SpeedUp         = 3006,
    BounceUp        = 3007,
    KillShot        = 3013,
    LastHit         = 3014
}
```

**클래스 구조:**
```csharp
[CreateAssetMenu(fileName = "SkillData", menuName = "PurpleCow/SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] private int        _skillId;
    [SerializeField] private string     _skillName;
    [SerializeField] private Sprite     _icon;
    [SerializeField] private string     _description;
    [SerializeField] private SkillType  _skillType;

    // 수치 — 패시브/액티브 공통. 필요한 항목만 설정, 나머지 0으로 유지
    [SerializeField] private float _value1;   // 예: 데미지 증가량, 폭발 반경, 서브볼 개수
    [SerializeField] private float _value2;   // 예: 크리티컬 배율, 둔화 지속 시간
    [SerializeField] private float _value3;   // 예: 추가 데미지 배율

    public int       SkillId      => _skillId;
    public string    SkillName    => _skillName;
    public Sprite    Icon         => _icon;
    public string    Description  => _description;
    public SkillType SkillType    => _skillType;
    public float     Value1       => _value1;
    public float     Value2       => _value2;
    public float     Value3       => _value3;
}
```

**네이밍 규칙:**
- private 필드: `_camelCase` (SerializeField)
- 프로퍼티: `PascalCase` (읽기 전용)
- 열거형/값: `PascalCase`
- CreateAssetMenu 경로: `PurpleCow/SkillData`

---

### Step 2. BallSkillBase (abstract)

**파일:** `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs`

**역할:** 볼에 부착되는 Active 스킬의 추상 기반 클래스. Ball이 몬스터와 충돌할 때 `OnBallHit()`를 호출받아 스킬 고유 효과를 발동한다.

**클래스 구조:**
```csharp
public abstract class BallSkillBase : MonoBehaviour
{
    [SerializeField] protected SkillData _skillData;

    // Ball 컴포넌트 참조 (Awake에서 캐싱)
    protected Ball _ball;

    protected virtual void Awake()
    {
        _ball = GetComponent<Ball>();
    }

    // Ball.OnCollisionEnter2D에서 Monster 충돌 시 호출
    public abstract void OnBallHit(MonsterBase target, float baseDamage);

    // Ball이 풀에서 꺼내질 때 (OnSpawn 시) 호출 — 상태 초기화
    public virtual void OnActivate() { }

    // Ball이 풀로 돌아갈 때 (OnDespawn 시) 호출 — 상태 정리
    public virtual void OnDeactivate() { }

    public SkillData SkillData => _skillData;
}
```

**연동 방식 (Ball.cs 최소 수정):**
- `Ball.cs`에 `private BallSkillBase _skill` 필드와 `public void SetSkill(BallSkillBase skill)` 메서드 추가
- `Ball.OnCollisionEnter2D`의 Monster 분기에서 `_skill?.OnBallHit(monster, damage)` 호출 추가
- `Ball.OnSpawn()` / `Ball.OnDespawn()`에서 `_skill?.OnActivate()` / `_skill?.OnDeactivate()` 추가

---

### Step 3. Active 스킬 구체 클래스 (5종)

#### 3-1. FireBallSkill

**파일:** `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs`

**효과:** 몬스터 충돌 시 충돌 지점 주변 범위(반경 `_skillData.Value1`) 내 모든 몬스터에게 추가 폭발 데미지(`_skillData.Value2`) 적용.

**클래스 구조:**
```csharp
public class FireBallSkill : BallSkillBase
{
    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        float radius     = _skillData.Value1;
        float bonusDmg   = _skillData.Value2;

        Collider2D[] hits = Physics2D.OverlapCircleAll(target.transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                monster.TakeDamage(bonusDmg);
            }
        }
    }
}
```

**SkillData 수치 역할:**
| 필드 | 역할 |
|------|------|
| `Value1` | 폭발 반경 (예: 1.5f) |
| `Value2` | 폭발 추가 데미지 (예: 5f) |

---

#### 3-2. IceBallSkill

**파일:** `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`

**효과:** 충돌한 몬스터에게 이동 정지 상태 부여. `_skillData.Value1` 턴 동안 WaveManager의 MoveDown 호출 시 해당 몬스터 제외.

**클래스 구조:**
```csharp
public class IceBallSkill : BallSkillBase
{
    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        int freezeTurns = Mathf.RoundToInt(_skillData.Value1);
        target.ApplyFreeze(freezeTurns);
    }
}
```

**MonsterBase 추가 필요:**
- `public void ApplyFreeze(int turns)` — `_frozenTurnsRemaining = turns` 설정
- `public bool IsFrozen => _frozenTurnsRemaining > 0`
- `MoveDown(float distance)` 내부: `if (IsFrozen) { _frozenTurnsRemaining--; return; }` 처리

**SkillData 수치 역할:**
| 필드 | 역할 |
|------|------|
| `Value1` | 이동 정지 턴 수 (예: 1) |

---

#### 3-3. GhostBallSkill

**파일:** `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs`

**효과:** 볼이 몬스터를 관통함. 충돌 시 볼이 소멸하지 않고 이동을 계속. 물리 충돌 레이어를 동적으로 제어하여 관통 구현.

**클래스 구조:**
```csharp
public class GhostBallSkill : BallSkillBase
{
    // Ghost 볼은 Monster 레이어와 물리 충돌하지 않음
    // 대신 Physics2D.IgnoreLayerCollision 또는 Trigger로 전환

    public override void OnActivate()
    {
        // 볼의 Collider를 Trigger로 전환 → OnTriggerEnter2D로 데미지 처리
        _ball.SetGhostMode(true);
    }

    public override void OnDeactivate()
    {
        _ball.SetGhostMode(false);
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        // 관통 — 추가 처리 없음, 볼은 계속 이동
    }
}
```

**Ball.cs 추가 필요:**
- `public void SetGhostMode(bool isGhost)` — Collider2D의 `isTrigger` 전환
- `OnTriggerEnter2D(Collider2D)` — Ghost 모드일 때 몬스터 데미지 처리

---

#### 3-4. LaserBallSkill

**파일:** `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs`

**효과:** 발사 시 직선상의 모든 몬스터에게 즉시 데미지. 볼은 일반 물리 이동 대신 Raycast 기반으로 처리.

**클래스 구조:**
```csharp
public class LaserBallSkill : BallSkillBase
{
    public override void OnActivate()
    {
        // 발사 시점에 Raycast로 직선 관통 데미지 적용 후 볼 즉시 반납
        FireLaser();
        _ball.ForceReturn();   // 레이저는 발사 즉시 처리 완료
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        // Raycast에서 처리하므로 충돌 콜백 없음
    }

    private void FireLaser()
    {
        Vector2 origin    = _ball.transform.position;
        Vector2 direction = _ball.LaunchDirection;
        float   damage    = _skillData.Value1;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Infinity,
                                LayerMask.GetMask("Monster"));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                monster.TakeDamage(damage);
            }
        }
    }
}
```

**Ball.cs 추가 필요:**
- `public Vector2 LaunchDirection { get; private set; }` — Launch() 호출 시 저장
- `public void ForceReturn()` — ReturnToPool() 래핑 (public 노출)

**SkillData 수치 역할:**
| 필드 | 역할 |
|------|------|
| `Value1` | 레이저 1회 데미지 (예: 20f) |

---

#### 3-5. ClusterBallSkill

**파일:** `Assets/_Project/Scripts/Skill/Active/ClusterBallSkill.cs`

**효과:** 볼이 몬스터 충돌 시 서브볼 `_skillData.Value1`개를 무작위 방향으로 추가 발사. 서브볼은 Normal 볼처럼 동작 후 풀 반납.

**클래스 구조:**
```csharp
public class ClusterBallSkill : BallSkillBase
{
    private bool _hasExploded;   // 1회만 폭발하도록 제어

    public override void OnActivate()
    {
        _hasExploded = false;
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        if (_hasExploded) return;
        _hasExploded = true;

        int subBallCount = Mathf.RoundToInt(_skillData.Value1);
        BallLauncher.Instance.LaunchSubBalls(_ball.transform.position, subBallCount);
    }
}
```

**BallLauncher.cs 추가 필요:**
- `public void LaunchSubBalls(Vector2 origin, int count)` — 지정 위치에서 count개의 볼을 랜덤 방향으로 발사 (서브볼이므로 BallLauncher의 발사 횟수 카운트에 포함)

**SkillData 수치 역할:**
| 필드 | 역할 |
|------|------|
| `Value1` | 서브볼 개수 (예: 3) |

---

### Step 4. PassiveSkillBase (abstract)

**파일:** `Assets/_Project/Scripts/Skill/Base/PassiveSkillBase.cs`

**역할:** 패시브 스킬의 추상 기반 클래스. `Apply()`/`Remove()` 호출로 이벤트 구독/해제를 통해 전투 흐름에 개입한다.

**클래스 구조:**
```csharp
public abstract class PassiveSkillBase
{
    protected SkillData _skillData;

    protected PassiveSkillBase(SkillData skillData)
    {
        _skillData = skillData;
    }

    // SkillManager가 장착 시 호출 → 이벤트 구독
    public abstract void Apply();

    // SkillManager가 해제 시 호출 → 이벤트 해제
    public abstract void Remove();

    public SkillData SkillData => _skillData;
}
```

> PassiveSkillBase는 MonoBehaviour가 아닌 순수 C# 클래스로 구현.
> 이벤트 구독 대상: `Ball.OnHitMonster`, `MonsterBase.OnMonsterDied`, `BallLauncher.OnAllBallsReturned`

---

### Step 5. Passive 스킬 구체 클래스 (7종)

#### 5-1. DamageUpPassive (ID: 3000)

**파일:** `Assets/_Project/Scripts/Skill/Passive/DamageUpPassive.cs`

**효과:** 볼의 기본 데미지를 `_skillData.Value1` % 증가. Ball.OnHitMonster 발행 전 BallData 수치에 배율 적용.

```csharp
public class DamageUpPassive : PassiveSkillBase
{
    public DamageUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddDamageMultiplier(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveDamageMultiplier(_skillData.Value1);
    }
}
```

---

#### 5-2. CritChanceUpPassive (ID: 3002)

**파일:** `Assets/_Project/Scripts/Skill/Passive/CritChanceUpPassive.cs`

**효과:** 크리티컬 확률을 `_skillData.Value1` 만큼 증가. SkillManager의 CritChanceBonus 프로퍼티를 통해 Ball.CalculateDamage에 반영.

```csharp
public class CritChanceUpPassive : PassiveSkillBase
{
    public CritChanceUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddCritChanceBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveCritChanceBonus(_skillData.Value1);
    }
}
```

---

#### 5-3. CritDamageUpPassive (ID: 3003)

**파일:** `Assets/_Project/Scripts/Skill/Passive/CritDamageUpPassive.cs`

**효과:** 크리티컬 데미지 배율을 `_skillData.Value1` 만큼 증가. SkillManager의 CritDamageBonus 프로퍼티를 통해 Ball.CalculateDamage에 반영.

```csharp
public class CritDamageUpPassive : PassiveSkillBase
{
    public CritDamageUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddCritDamageBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveCritDamageBonus(_skillData.Value1);
    }
}
```

---

#### 5-4. SpeedUpPassive (ID: 3006)

**파일:** `Assets/_Project/Scripts/Skill/Passive/SpeedUpPassive.cs`

**효과:** 볼 이동 속도를 `_skillData.Value1` 만큼 증가. SkillManager의 SpeedBonus 프로퍼티를 통해 Ball.Launch에 반영.

```csharp
public class SpeedUpPassive : PassiveSkillBase
{
    public SpeedUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddSpeedBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveSpeedBonus(_skillData.Value1);
    }
}
```

---

#### 5-5. BounceUpPassive (ID: 3007)

**파일:** `Assets/_Project/Scripts/Skill/Passive/BounceUpPassive.cs`

**효과:** 볼의 최대 반사 횟수를 `_skillData.Value1` 만큼 증가. Ball.cs에 `_remainingBounces` 카운터를 추가하고 SkillManager의 BounceBonus로 초기값 증가.

```csharp
public class BounceUpPassive : PassiveSkillBase
{
    public BounceUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddBounceBonus(Mathf.RoundToInt(_skillData.Value1));
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveBounceBonus(Mathf.RoundToInt(_skillData.Value1));
    }
}
```

---

#### 5-6. KillShotPassive (ID: 3013)

**파일:** `Assets/_Project/Scripts/Skill/Passive/KillShotPassive.cs`

**효과:** 몬스터 처치 시 처치 위치에서 추가 볼 1개를 무작위 방향으로 발사.

```csharp
public class KillShotPassive : PassiveSkillBase
{
    public KillShotPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    public override void Remove()
    {
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        BallLauncher.Instance.LaunchSubBalls(monster.transform.position, 1);
    }
}
```

---

#### 5-7. LastHitPassive (ID: 3014)

**파일:** `Assets/_Project/Scripts/Skill/Passive/LastHitPassive.cs`

**효과:** 볼이 바닥에 도달하기 직전(ReturnToPool 직전) HP가 가장 낮은 몬스터에게 추가 타격을 가함.

```csharp
public class LastHitPassive : PassiveSkillBase
{
    public LastHitPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        Ball.OnBeforeReturn += HandleBeforeReturn;
    }

    public override void Remove()
    {
        Ball.OnBeforeReturn -= HandleBeforeReturn;
    }

    private void HandleBeforeReturn(Ball ball)
    {
        // WaveManager에서 현재 살아있는 몬스터 목록을 받아
        // HP가 가장 낮은 몬스터를 찾아 추가 데미지 적용
        MonsterBase weakest = WaveManager.Instance.GetWeakestMonster();
        if (weakest != null)
        {
            weakest.TakeDamage(_skillData.Value1);
        }
    }
}
```

**Ball.cs / WaveManager.cs 추가 필요:**
- `Ball.cs`: `public static event Action<Ball> OnBeforeReturn` — ReturnToPool() 직전 발행
- `WaveManager.cs`: `public MonsterBase GetWeakestMonster()` — `_activeMonsters` 중 CurrentHp 최솟값 반환

---

### Step 6. SkillManager (Singleton)

**파일:** `Assets/_Project/Scripts/Skill/SkillManager.cs`

**역할:** 현재 장착된 Active 스킬 1개와 Passive 스킬 N개를 관리한다. 볼 발사 시 Active 스킬을 Ball에 주입하고, Passive 스킬의 누적 보너스를 외부(Ball, BallLauncher)에 제공한다.

**필드:**
```
private BallSkillBase _equippedActiveSkill     // 현재 장착된 Active 스킬 컴포넌트
private List<PassiveSkillBase> _passiveSkills  // 현재 장착된 Passive 스킬 목록

// Passive 누적 보너스 (읽기 전용 프로퍼티로 외부 제공)
private float _damageMultiplierBonus   // 합산 데미지 배율 (기본 0)
private float _critChanceBonus         // 합산 크리티컬 확률 보너스
private float _critDamageBonus         // 합산 크리티컬 배율 보너스
private float _speedBonus              // 합산 속도 보너스
private int   _bounceBonus             // 합산 추가 반사 횟수
```

**프로퍼티:**
```
public float DamageMultiplierBonus => _damageMultiplierBonus
public float CritChanceBonus       => _critChanceBonus
public float CritDamageBonus       => _critDamageBonus
public float SpeedBonus            => _speedBonus
public int   BounceBonus           => _bounceBonus
```

**이벤트:**
```
public static event Action<BallSkillBase> OnActiveSkillChanged
// Active 스킬 변경 시 발행 — UI 연동 예정

public static event Action<List<PassiveSkillBase>> OnPassiveSkillsChanged
// Passive 스킬 변경 시 발행 — UI 연동 예정
```

**메서드:**

| 메서드 | 설명 |
|--------|------|
| `Awake()` | `_passiveSkills = new List<PassiveSkillBase>()` 초기화 |
| `EquipActiveSkill(BallSkillBase skill)` | `_equippedActiveSkill = skill`, `OnActiveSkillChanged` 발행 |
| `AddPassiveSkill(PassiveSkillBase skill)` | 목록 추가 후 `skill.Apply()`, `OnPassiveSkillsChanged` 발행 |
| `RemovePassiveSkill(PassiveSkillBase skill)` | `skill.Remove()` 후 목록 제거, `OnPassiveSkillsChanged` 발행 |
| `ApplySkillToBall(Ball ball)` | `_equippedActiveSkill`이 있으면 `ball.SetSkill(_equippedActiveSkill)` |
| `AddDamageMultiplier(float value)` | `_damageMultiplierBonus += value` |
| `RemoveDamageMultiplier(float value)` | `_damageMultiplierBonus -= value` |
| `AddCritChanceBonus(float value)` | `_critChanceBonus += value` |
| `RemoveCritChanceBonus(float value)` | `_critChanceBonus -= value` |
| `AddCritDamageBonus(float value)` | `_critDamageBonus += value` |
| `RemoveCritDamageBonus(float value)` | `_critDamageBonus -= value` |
| `AddSpeedBonus(float value)` | `_speedBonus += value` |
| `RemoveSpeedBonus(float value)` | `_speedBonus -= value` |
| `AddBounceBonus(int value)` | `_bounceBonus += value` |
| `RemoveBounceBonus(int value)` | `_bounceBonus -= value` |

**Singleton 상속:**
```csharp
public class SkillManager : Singleton<SkillManager>
{
    protected override void Awake()
    {
        base.Awake();
        _passiveSkills = new List<PassiveSkillBase>();
    }
}
```

---

### Step 7. Ball.cs 수정 (외과적 변경)

**파일:** `Assets/_Project/Scripts/Ball/Ball.cs`

**추가 필드:**
```csharp
private BallSkillBase _skill;
private int _remainingBounces;         // BounceBonus 연동용
public Vector2 LaunchDirection { get; private set; }

public static event Action<Ball> OnBeforeReturn;  // LastHitPassive 연동용
```

**수정 메서드:**

| 수정 대상 | 변경 내용 |
|----------|----------|
| `Launch(Vector2)` | `LaunchDirection = direction` 저장 추가. speed에 `SkillManager.Instance.SpeedBonus` 합산 |
| `OnSpawn()` | `_remainingBounces = BallData의 기본값 + SkillManager.Instance.BounceBonus`, `_skill?.OnActivate()` 추가 |
| `OnDespawn()` | `_skill?.OnDeactivate()` 추가 |
| `CalculateDamage()` | 크리티컬 확률에 `SkillManager.Instance.CritChanceBonus` 합산, 크리티컬 배율에 `SkillManager.Instance.CritDamageBonus` 합산, 최종 데미지에 `(1 + SkillManager.Instance.DamageMultiplierBonus)` 곱연산 |
| `OnCollisionEnter2D` (Monster 분기) | `_skill?.OnBallHit(monster, damage)` 추가 호출 |
| `OnCollisionEnter2D` (Wall 분기) | `_remainingBounces` 감소, 0 이하이면 `ReturnToPool()` |
| `ReturnToPool()` | `OnBeforeReturn?.Invoke(this)` 호출 후 반납 |

**추가 메서드:**
```csharp
public void SetSkill(BallSkillBase skill)
{
    _skill = skill;
}

public void SetGhostMode(bool isGhost)
{
    GetComponent<Collider2D>().isTrigger = isGhost;
}

public void ForceReturn()
{
    ReturnToPool();
}
```

---

### Step 8. MonsterBase.cs 수정 (외과적 변경)

**파일:** `Assets/_Project/Scripts/Monster/MonsterBase.cs`

**추가 필드:**
```csharp
private int _frozenTurnsRemaining;
public bool IsFrozen => _frozenTurnsRemaining > 0;
```

**수정 메서드:**

| 수정 대상 | 변경 내용 |
|----------|----------|
| `OnSpawn()` | `_frozenTurnsRemaining = 0` 초기화 추가 |
| `MoveDown(float)` | `if (IsFrozen) { _frozenTurnsRemaining--; return; }` 앞에 추가 |

**추가 메서드:**
```csharp
public void ApplyFreeze(int turns)
{
    _frozenTurnsRemaining = Mathf.Max(_frozenTurnsRemaining, turns);
}
```

---

### Step 9. BallLauncher.cs 수정 (외과적 변경)

**파일:** `Assets/_Project/Scripts/Ball/BallLauncher.cs`

**수정 메서드:**

| 수정 대상 | 변경 내용 |
|----------|----------|
| `LaunchBall()` | 발사 후 `SkillManager.Instance.ApplySkillToBall(ball)` 호출 추가 |

**추가 메서드:**
```csharp
public void LaunchSubBalls(Vector2 origin, int count)
{
    for (int i = 0; i < count; i++)
    {
        Ball ball = _ballPool.Get();
        ball.transform.position = origin;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        // 아래 방향 제외 (y > 0 보정)
        if (randomDir.y < 0) randomDir.y = -randomDir.y;
        ball.Launch(randomDir);
        _activeBallCount++;
    }
}
```

---

### Step 10. WaveManager.cs 수정 (외과적 변경)

**파일:** `Assets/_Project/Scripts/Wave/WaveManager.cs`

**추가 메서드:**
```csharp
public MonsterBase GetWeakestMonster()
{
    if (_activeMonsters.Count == 0) return null;

    MonsterBase weakest = _activeMonsters[0];
    foreach (MonsterBase monster in _activeMonsters)
    {
        if (monster.CurrentHp < weakest.CurrentHp)
            weakest = monster;
    }
    return weakest;
}
```

---

### Step 11. SkillSetupEditor (Editor 전용 자동화 스크립트)

**파일:** `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`

**역할:** Skill 시스템 초기 세팅(SkillData 에셋 생성, 아이콘 자동 연결)을 Unity Editor 메뉴에서 한 번에 실행. BallSetupEditor / MonsterSetupEditor와 동일한 패턴 적용.

**MenuItem 경로:** `PurpleCow/Setup/Skill System Setup`

**수행 작업:**

1. **Active SkillData 에셋 생성 (5종)**
   - 경로: `Assets/_Project/Data/SkillData_Fire.asset` 등
   - 기본값 설정 (Fire: Value1=1.5, Value2=5 / Ice: Value1=1 / Cluster: Value1=3 등)
   - 아이콘 자동 연결: `Assets/_Project/Sprites/BallSkillIcon/Ball_Fire_ball.png` 등
   - 이미 존재하면 스킵

2. **Passive SkillData 에셋 생성 (7종)**
   - 경로: `Assets/_Project/Data/SkillData_Passive_3000.asset` 등
   - 기본값 설정 (DamageUp: Value1=0.1 / CritChance: Value1=0.05 등)
   - 아이콘 자동 연결: `Assets/_Project/Sprites/Passive/icon_passive_3000.png` 등
   - 이미 존재하면 스킵

**마무리:** `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()` 호출 후 완료 로그 출력

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 경로 |
|------|------|------|
| 신규 생성 | `SkillData.cs` | `Assets/_Project/Scripts/Data/` |
| 신규 생성 | `BallSkillBase.cs` | `Assets/_Project/Scripts/Skill/Base/` |
| 신규 생성 | `PassiveSkillBase.cs` | `Assets/_Project/Scripts/Skill/Base/` |
| 신규 생성 | `FireBallSkill.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 생성 | `IceBallSkill.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 생성 | `GhostBallSkill.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 생성 | `LaserBallSkill.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 생성 | `ClusterBallSkill.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 생성 | `DamageUpPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `CritChanceUpPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `CritDamageUpPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `SpeedUpPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `BounceUpPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `KillShotPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `LastHitPassive.cs` | `Assets/_Project/Scripts/Skill/Passive/` |
| 신규 생성 | `SkillManager.cs` | `Assets/_Project/Scripts/Skill/` |
| 신규 생성 | `SkillSetupEditor.cs` | `Assets/_Project/Scripts/Editor/` |
| 기존 수정 | `Ball.cs` | `Assets/_Project/Scripts/Ball/` |
| 기존 수정 | `BallLauncher.cs` | `Assets/_Project/Scripts/Ball/` |
| 기존 수정 | `MonsterBase.cs` | `Assets/_Project/Scripts/Monster/` |
| 기존 수정 | `WaveManager.cs` | `Assets/_Project/Scripts/Wave/` |

---

## 주의사항

1. **PDF 스킬 수치 확인 필요**
   Passive 스킬 7종의 구체적 수치(ID 3002, 3003, 3006, 3007, 3013, 3014)는 PDF 원문에서 확인하여 SkillData 에셋 기본값에 반영해야 합니다. 현재 plan.md의 수치는 추정값입니다.

2. **Ball.cs CalculateDamage → SkillManager 참조 순환 주의**
   SkillManager.Instance가 Awake에서 초기화되므로, Ball의 OnSpawn()/Launch()보다 반드시 먼저 Awake가 실행되어야 합니다. Scene 내 SkillManager GameObject의 실행 순서를 Project Settings > Script Execution Order에서 Ball보다 앞으로 지정합니다.

3. **GhostBallSkill의 Trigger 전환 시 충돌 감지 방식 변경**
   `isTrigger = true`로 전환하면 `OnCollisionEnter2D`가 아닌 `OnTriggerEnter2D`에서만 이벤트가 발생합니다. Ball.cs에 `OnTriggerEnter2D` 핸들러를 별도 추가해야 하며, Ghost 모드에서만 실행되도록 `_skill is GhostBallSkill` 조건을 추가합니다.

4. **LaserBallSkill의 볼 즉시 반납**
   Laser 볼은 `OnActivate()` 내부에서 `ForceReturn()`을 호출합니다. 이 시점은 `BallLauncher.LaunchBall()`이 `_activeBallCount++`를 한 직후이므로 `ReturnToPool()` 내 `_activeBallCount--` 및 `OnAllBallsReturned` 발행 흐름이 정상 동작합니다.

5. **BounceUpPassive의 기본 반사 횟수 설계**
   Ball.cs에 기본 반사 횟수(`int _defaultMaxBounces`)를 BallData에 추가하거나 상수로 정의해야 합니다. BounceBonus는 이 기본값에 합산됩니다. 기본값은 Inspector에서 BallData SO로 관리하는 것을 권장합니다.

6. **SkillSetupEditor 실행 필요**
   구현 후 Unity Editor 메뉴 `PurpleCow > Setup > Skill System Setup` 실행으로 SkillData 에셋 자동 생성 및 아이콘 연결.

7. **DevRules.md 네이밍 규칙 준수**
   - private 필드: `_camelCase`
   - SerializeField: Inspector 노출 필요한 값만 (수치는 SkillData SO로 분리)
   - 이벤트: C# static event
   - 이벤트 구독/해제: `OnEnable / OnDisable` 쌍 (MonoBehaviour) 또는 `Apply() / Remove()` 쌍 (PassiveSkillBase)
   - 코루틴 명명 시: `Co` 접두어 (이번 단계에서 코루틴 미사용)

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
