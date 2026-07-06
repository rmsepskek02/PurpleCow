# Research — 레이저볼/지속대미지(DoT) 대미지 텍스트 미표시 통합 수정

`TODO.md` 8번(레이저볼 가로 행 대미지 텍스트 미표시)과 10번(DoT 틱 대미지 텍스트 미표시)을 하나의 task로 묶어 조사한 문서다. 두 항목 모두 "`Ball` 인스턴스를 거치지 않고 몬스터에게 직접 `TakeDamage()`가 호출되는 피해 경로에서는 `DamageTextManager`가 구독하는 `Ball.OnHitMonster` 이벤트가 발행되지 않아 대미지 텍스트가 뜨지 않는다"는 동일한 근본 원인을 가진다. 실제 소스를 다시 읽어 TODO.md에 기록된 조사 내용을 재검증하고, 구현 방식 후보 두 가지를 비교한다.

## 현재 상태

### 8번 — 레이저볼 (`Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs`)

```csharp
public override void OnBallHit(MonsterBase target)
{
    // Value1=추가피해
    var row = WaveManager.Instance.GetMonstersInRow(target);
    foreach (var monster in row)
    {
        if (monster != target)
            monster.TakeDamage(LevelData.Value1);
    }
}
```

`target`(직접 피격 몬스터)을 제외한 같은 가로 행의 나머지 몬스터에게 `monster.TakeDamage(LevelData.Value1)`을 직접 호출한다. `Ball.CalculateDamage()`를 거치지 않으므로 `Ball.OnHitMonster` 이벤트가 전혀 발행되지 않는다.

### 10번 — DoT (`Assets/_Project/Scripts/Monster/MonsterBase.cs`)

```csharp
private void UpdateDot(float deltaTime)
{
    ...
    while (_dotTickTimer >= 1f && !_isDead)
    {
        _dotTickTimer -= 1f;
        float tickDamage = 0f;
        for (int i = 0; i < _dotStacks.Count; i++)
        {
            if (_dotStacks[i].RemainingSeconds >= 0f)
                tickDamage += _dotStacks[i].DamagePerSecond;
        }

        if (tickDamage > 0f)
            TakeDamage(tickDamage);
    }
    ...
}
```

매 초 틱마다 살아있는 DoT 스택의 `DamagePerSecond` 합을 몬스터 자신의 `TakeDamage(tickDamage)`로 직접 호출한다. 이 경로 역시 `Ball` 인스턴스와 무관하게 몬스터 쪽(`Update()` → `UpdateDot()`)에서 발생하므로 `Ball.OnHitMonster`가 발행되지 않는다.

### 공통 원인 — `Ball.cs`의 이벤트 발행 지점

```csharp
public static event Action<MonsterBase, float, bool> OnHitMonster;
...
private void CalculateDamage(MonsterBase target, bool isFrontHit)
{
    ...
    LastDamage = damage;
    target.TakeDamage(damage);
    OnHitMonster?.Invoke(target, damage, isCritical);
}
```

`OnHitMonster`는 `Action<MonsterBase, float, bool>`(피격 몬스터, 대미지, 치명타 여부) 시그니처의 **static event**다. `CalculateDamage()`는 `OnCollisionEnter2D`/`OnTriggerEnter2D`에서 직접 피격한 몬스터에 대해서만 호출되며, `target.TakeDamage(damage)` 호출 직후 단 한 번 이벤트를 발행한다. static event이므로 특정 `Ball` 인스턴스 없이도(`Ball.OnHitMonster?.Invoke(...)` 형태로) 어디서든 발행 가능한 구조다 — 레이저볼 부가 피해, DoT 틱처럼 볼 인스턴스가 없는 호출부에서도 그대로 재사용할 수 있다.

### `DamageTextManager.cs`

```csharp
private void OnEnable()
{
    Ball.OnHitMonster += HandleHitMonster;
}

private void OnDisable()
{
    Ball.OnHitMonster -= HandleHitMonster;
}

private void HandleHitMonster(MonsterBase monster, float damage, bool isCritical)
{
    ShowDamage(monster.transform.position, damage, isCritical);
}

public void ShowDamage(Vector3 worldPos, float damage, bool isCritical)
{
    DamageTextFx fx = _pool.Get();
    fx.Play(worldPos, damage, isCritical);
}
```

`OnEnable/OnDisable` 쌍으로 `Ball.OnHitMonster`만 구독한다(DevRules.md의 이벤트 구독/해제 규칙 준수). 이 이벤트가 발행되지 않으면 `ShowDamage()`까지 이어질 방법이 코드상 없다.

### `DamageTextFx.cs` — 시각적 표현

```csharp
public void Play(Vector3 worldPos, float damage, bool isCritical)
{
    transform.position = worldPos;
    _text.text         = isCritical ? $"<b>{Mathf.RoundToInt(damage)}</b>" : Mathf.RoundToInt(damage).ToString();
    _text.color        = isCritical ? _criticalColor : _normalColor;
    transform.localScale = isCritical ? Vector3.one * _criticalScale : Vector3.one;
    ...
}
```

현재 `isCritical` 하나만으로 색상/굵기/크기를 분기한다. DoT 전용 시각 스타일(예: 별도 색상)을 구분하는 파라미터나 분기는 존재하지 않는다.

### `MonsterBase.TakeDamage()`

```csharp
public void TakeDamage(float damage)
{
    if (_isDead || _isBottomAttacking)
        return;

    _flashSecondsRemaining = _hitFlashDuration;

    _currentHp -= damage;
    OnHpChanged?.Invoke(Mathf.Max(_currentHp, 0f), _monsterData.Hp);

    if (_currentHp <= 0f)
    {
        Die();
    }
}
```

`TakeDamage()`는 HP 차감, 피격 플래시, `OnHpChanged` 발행, 사망 처리만 담당하며 `Ball.OnHitMonster`를 발행하지 않는다. `MonsterBase`는 `Ball`을 참조하지 않고, `Ball` 쪽에서 `TakeDamage()`를 호출한 뒤 별도로 이벤트를 발행하는 구조다.

## 관련 파일 및 의존성

| 파일 | 역할 |
|---|---|
| `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs` | 레이저볼 가로 행 부가 피해 — `TakeDamage()` 직접 호출 |
| `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `TakeDamage()`, `UpdateDot()` — DoT 틱 피해도 `TakeDamage()` 직접 호출 |
| `Assets/_Project/Scripts/Ball/Ball.cs` | `OnHitMonster` static event 선언, `CalculateDamage()`에서 유일하게 발행 |
| `Assets/_Project/Scripts/UI/DamageTextManager.cs` | `Ball.OnHitMonster` 구독 → `ShowDamage()` 스폰 |
| `Assets/_Project/Scripts/UI/DamageTextFx.cs` | 실제 텍스트 표시(색상/크기는 `isCritical`로만 분기) |

의존 관계: `LaserBallSkill`/`MonsterBase.UpdateDot()` → `MonsterBase.TakeDamage()` (이벤트 미발행) / `Ball.CalculateDamage()` → `MonsterBase.TakeDamage()` + `Ball.OnHitMonster` 발행 → `DamageTextManager.HandleHitMonster()` → `ShowDamage()`.

## 문제점 / 구현 대상 파악

두 가지 수정 방식을 비교한다.

### 방식 A — 각 호출부(`LaserBallSkill`, `UpdateDot`)에서 개별적으로 `Ball.OnHitMonster` 직접 invoke

- `LaserBallSkill.OnBallHit()`의 `monster.TakeDamage(LevelData.Value1)` 직후, `MonsterBase.UpdateDot()`의 `TakeDamage(tickDamage)` 직후 각각 `Ball.OnHitMonster?.Invoke(monster, damage, false)`를 추가로 호출한다.
- 장점: `Ball.CalculateDamage()`의 기존 발행 로직을 전혀 건드리지 않으므로 기존 정상 경로(볼 충돌)에 영향이 없고 중복 발행 위험이 원천적으로 없다.
- 단점: `MonsterBase`가 `Ball`의 static event를 알아야 하므로 `MonsterBase`(몬스터 도메인)가 `Ball`(볼 도메인)을 참조하는 의존 방향이 새로 생긴다. 또한 향후 몬스터에게 직접 피해를 주는 새 경로가 추가될 때마다 매번 이벤트 발행 코드를 개별적으로 다시 추가해야 하므로 동일 종류의 버그가 재발할 여지가 남는다.

### 방식 B — `MonsterBase.TakeDamage()` 자체에서 공통으로 이벤트 발행

- `TakeDamage()` 내부에서 `Ball.OnHitMonster?.Invoke(this, damage, false)`를 발행하도록 만들면, `TakeDamage()`를 호출하는 모든 경로(레이저볼, DoT, 그리고 향후 추가될 새 경로 포함)가 자동으로 텍스트 표시 대상이 된다 — 근본적으로 재발을 막는 방식이다.
- 하지만 `Ball.CalculateDamage()`는 이미 `target.TakeDamage(damage)` 호출 직후 자신도 `OnHitMonster?.Invoke(target, damage, isCritical)`를 호출한다. `TakeDamage()` 내부에서도 이벤트를 발행하게 만들면 **볼 충돌(정상 경로)에서 이벤트가 두 번 발행되어 대미지 텍스트가 이중으로 뜨는 회귀**가 발생한다. 이를 막으려면 `CalculateDamage()`의 기존 `OnHitMonster?.Invoke(...)` 호출을 제거하고 치명타 여부(`isCritical`)를 `TakeDamage()`에 전달할 방법을 새로 만들어야 하는데, 현재 `TakeDamage(float damage)` 시그니처에는 `isCritical` 파라미터가 없다. 시그니처를 `TakeDamage(float damage, bool isCritical = false)`로 변경하면 `TakeDamage()`를 호출하는 모든 다른 지점(레이저볼, DoT, 그리고 `MonsterBase` 내부에서 향후 추가될 수 있는 호출)도 영향을 받고, `Ball.CalculateDamage()` 쪽 호출도 함께 수정해야 하므로 변경 범위가 방식 A보다 넓어진다.
- 즉 방식 B는 "근본적 차단"이라는 장점은 있으나, 기존에 정상 동작하던 `CalculateDamage()` 경로의 이벤트 발행 지점을 함께 옮겨야 하는 리스크와 변경 범위 증가를 동반한다.

### 중복 발행 여부 확정 확인

`Ball.CalculateDamage()`를 다시 확인한 결과, `target.TakeDamage(damage)` 호출과 `OnHitMonster?.Invoke(target, damage, isCritical)` 호출은 순서상 분리되어 있고, `TakeDamage()` 자체는 이벤트를 발행하지 않으므로 **현재 코드에서는 중복 발행이 없다**. 방식 B를 채택할 경우에만 이 중복이 새로 생기므로 반드시 `CalculateDamage()`의 기존 invoke 라인을 제거해야 한다.

## 결론

- 두 버그(8번 레이저볼, 10번 DoT)는 "`Ball`을 거치지 않고 `MonsterBase.TakeDamage()`를 직접 호출하는 경로는 `Ball.OnHitMonster`를 발행하지 않는다"는 동일한 원인을 공유하며, 실제 소스 확인 결과 TODO.md의 기존 조사 내용과 일치한다.
- `Ball.OnHitMonster`는 `Action<MonsterBase, float, bool>` static event로, 볼 인스턴스 없이도 어디서든 발행 가능하다.
- 방식 A(개별 호출부에서 직접 invoke)는 변경 범위가 가장 작고 기존 정상 경로에 회귀 위험이 없다. 방식 B(`TakeDamage()` 공통화)는 더 근본적이지만 `CalculateDamage()`의 기존 이벤트 발행 로직을 함께 옮겨야 해서 변경 범위와 회귀 위험이 더 크다.
- plan.md에서는 DevRules.md "단순함 우선" 원칙에 따라 방식 A를 채택하는 구체적 구현 단계를 제시한다.
