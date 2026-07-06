# Plan — 몬스터 피격/상태이상 스프라이트 색상 효과

이 문서는 `research.md`에서 확인된 현재 `MonsterBase.cs` 상태(색상 관련 로직 전무, 화상 지속시간 미추적)를 바탕으로, 피격 시 흰색 히트 플래시와 얼음/화상 지속 틴트, 그리고 "나중에 걸린 효과 우선" 규칙을 어떻게 구현할지 구체적으로 계획한다. 변경 대상은 `MonsterBase.cs` 단 하나뿐이며, 프리팹 구조상 본체 `SpriteRenderer`가 `MonsterBase`와 같은 루트 GameObject에 있어 발판(`BlockVisual`)은 별도 처리 없이 자연스럽게 제외된다. 색상 값과 히트 플래시 지속시간은 모두 `[SerializeField]`로 노출해 인스펙터에서 조정 가능하게 하고, 색상 전환은 트윈 없이 매 프레임 즉시 대입하는 방식으로 단순하게 처리한다.

## 구현 목표

- 몬스터가 `TakeDamage()`로 실제 데미지를 받을 때마다 본체 스프라이트가 흰색으로 0.1초간 짧게 반짝이도록 한다.
- 냉동(`ApplyFreeze`)/슬로우(`ApplySlow`) 효과가 지속되는 동안 본체 스프라이트가 하늘색 계열로 물들도록 한다.
- 화상(`ApplyDot`) 효과가 지속되는 동안 본체 스프라이트가 붉은주황색 계열로 물들도록 한다.
- 얼음과 화상이 동시에 걸려 있는 경우, 둘 중 나중에 걸린 효과의 색을 우선 적용한다.
- 히트 플래시가 끝나면 "기본색"이 아니라 그 시점에 유효한 상태이상 색(또는 상태이상이 없다면 기본색)으로 복귀하도록 한다.
- 모든 지속효과가 끝나면 원래 스프라이트 색으로 돌아가며, 오브젝트 풀 재사용 시에도 이전 색이 남아있지 않도록 리셋한다.
- 발판(`BlockVisual`) 스프라이트는 이번 색상 효과의 적용 대상에서 제외한다.

## 단계별 작업 계획

### 1. 신규 필드 추가

기존 상태이상 필드(`_frozenSecondsRemaining`, `_slowSecondsRemaining`, `_slowPercent`) 아래에 색상 효과 전용 필드를 추가한다.

```csharp
private SpriteRenderer _spriteRenderer;
private Color _baseColor;

[SerializeField] private Color _hitFlashColor = Color.white;
[SerializeField] private float _hitFlashDuration = 0.1f;
[SerializeField] private Color _freezeTintColor = new Color(0.53f, 0.81f, 0.98f);
[SerializeField] private Color _burnTintColor = new Color(1f, 0.35f, 0.16f);

private float _flashSecondsRemaining;
private float _burnSecondsRemaining;

private enum StatusVisualType { None, Ice, Fire }
private StatusVisualType _lastStatusVisual;
```

- `_spriteRenderer`/`_baseColor`는 본체 스프라이트 참조와 원본 색을 캐싱하는 용도로, `Ball.cs`가 `Awake()`에서 `Rigidbody2D` 등을 캐싱하는 기존 패턴과 동일하게 처리한다.
- `_hitFlashColor`/`_hitFlashDuration`/`_freezeTintColor`/`_burnTintColor`는 사용자와 확정한 대로 전부 `[SerializeField]`로 노출해, 코드에 넣는 값은 어디까지나 적당한 시작값일 뿐이고 실제 톤 조정은 Unity 에디터 인스펙터에서 진행한다. `_hitFlashDuration`은 0.1초로 고정 확정.
- `_burnSecondsRemaining`은 기존 `CoDotTick()` 코루틴의 데미지 로직과는 완전히 별개로, 시각 효과 판단 전용으로 병행 추적하는 값이다.
- `_lastStatusVisual`은 얼음과 화상이 동시에 활성 상태일 수 있으므로, 지속시간이 남아있는지 여부만으로는 "나중에 걸린 효과"를 판정할 수 없어 마지막으로 걸린 효과 종류를 별도로 기록하기 위한 필드다.

### 2. `Awake()` 신규 추가

현재 `MonsterBase`에는 `Awake()`가 없으므로 새로 추가해, 본체 `SpriteRenderer`와 원본 색을 한 번만 캐싱한다.

```csharp
private void Awake()
{
    _spriteRenderer = GetComponent<SpriteRenderer>();
    _baseColor = _spriteRenderer.color;
}
```

`GetComponent<SpriteRenderer>()`는 자식 오브젝트를 검색하지 않으므로, 자식인 `BlockVisual`의 발판 스프라이트는 이 시점에 자연스럽게 제외된다.

### 3. `ApplyFreeze(seconds)` / `ApplySlow(seconds, percent)` 수정

기존 로직 끝에 "마지막으로 걸린 효과는 얼음"이라는 표시만 한 줄씩 추가한다.

```csharp
public void ApplyFreeze(float seconds)
{
    _frozenSecondsRemaining = Mathf.Max(_frozenSecondsRemaining, seconds);
    _lastStatusVisual = StatusVisualType.Ice;
}

public void ApplySlow(float seconds, float percent)
{
    _slowSecondsRemaining = seconds;
    _slowPercent = percent;
    _lastStatusVisual = StatusVisualType.Ice;
}
```

### 4. `ApplyDot(damagePerSec, duration, maxStacks)` 수정

기존 `StartCoroutine(CoDotTick(...))` 호출은 그대로 두고, 화상 지속시간을 시각 효과 판단용으로 별도 갱신하는 코드만 추가한다.

```csharp
public void ApplyDot(float damagePerSec, float duration, int maxStacks)
{
    StartCoroutine(CoDotTick(damagePerSec, duration, maxStacks));
    _burnSecondsRemaining = Mathf.Max(_burnSecondsRemaining, duration);
    _lastStatusVisual = StatusVisualType.Fire;
}
```

`CoDotTick()` 내부는 중첩 스택 데미지를 계산하는 기존 의도된 로직이므로 이번 작업에서 전혀 손대지 않는다.

### 5. `TakeDamage(float damage)` 수정

`_isDead` 체크를 통과해 실제로 데미지를 받는 시점(즉 아직 살아있는 몬스터가 피격당한 순간)에 히트 플래시 타이머를 세팅한다.

```csharp
public void TakeDamage(float damage)
{
    if (_isDead)
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

화상 DOT의 매 초 피해(`CoDotTick()` 내부의 `TakeDamage(dps * stacks)` 호출)도 결국 이 메서드를 거치므로, 화상 데미지가 들어올 때마다 흰색 플래시가 한 번씩 더 겹쳐 반짝이게 되는데 이는 자연스러운 부수효과이며 별도 예외처리는 하지 않는다.

### 6. `Update()` — 상태 시각 효과 갱신 로직 추가

기존 `Update()`는 "얼어있으면 이동 조기 종료, 아니면 슬로우 적용 후 하강"이라는 이동 처리 전용 흐름이다. 얼어있을 때의 조기 종료(`return`)는 그대로 유지하되, 색상 갱신은 그 이동 로직과 무관하게 매 프레임 항상 실행되어야 하므로, 이동 로직보다 앞서 항상 호출되는 신규 비공개 메서드 `UpdateStatusVisual(float deltaTime)`을 도입해 `Update()` 맨 앞에서 호출한다.

```csharp
private void Update()
{
    if (_isDead)
        return;

    float deltaTime = Time.deltaTime;

    UpdateStatusVisual(deltaTime);

    if (_frozenSecondsRemaining > 0f)
    {
        _frozenSecondsRemaining -= deltaTime;
        return;
    }

    float speed = _monsterData.MoveSpeed;

    if (_slowSecondsRemaining > 0f)
    {
        speed *= (1f - _slowPercent);
        _slowSecondsRemaining -= deltaTime;
    }

    transform.position += Vector3.down * speed * deltaTime;
}
```

신규 메서드 `UpdateStatusVisual(float deltaTime)`는 다음 순서로 동작한다.

```csharp
private void UpdateStatusVisual(float deltaTime)
{
    _flashSecondsRemaining = Mathf.Max(0f, _flashSecondsRemaining - deltaTime);
    _burnSecondsRemaining = Mathf.Max(0f, _burnSecondsRemaining - deltaTime);

    bool isIceActive = _frozenSecondsRemaining > 0f || _slowSecondsRemaining > 0f;
    bool isFireActive = _burnSecondsRemaining > 0f;

    Color statusColor;
    if (isIceActive && isFireActive)
        statusColor = (_lastStatusVisual == StatusVisualType.Fire) ? _burnTintColor : _freezeTintColor;
    else if (isIceActive)
        statusColor = _freezeTintColor;
    else if (isFireActive)
        statusColor = _burnTintColor;
    else
        statusColor = _baseColor;

    _spriteRenderer.color = (_flashSecondsRemaining > 0f) ? _hitFlashColor : statusColor;
}
```

1. 히트 플래시(`_flashSecondsRemaining`)와 화상 판정용 잔여시간(`_burnSecondsRemaining`)을 각각 0 이하로 내려가지 않게 감소시킨다.
2. 얼음 활성 여부는 냉동(`_frozenSecondsRemaining`)과 슬로우(`_slowSecondsRemaining`) 중 하나라도 남아있으면 활성으로 판정하고, 화상 활성 여부는 `_burnSecondsRemaining`으로 판정한다.
3. 둘 다 활성이면 `_lastStatusVisual`이 가리키는 쪽(나중에 걸린 효과)의 색을, 하나만 활성이면 그 색을, 둘 다 비활성이면 `_baseColor`를 "현재 상태색"으로 결정한다.
4. 히트 플래시가 남아있으면 위 결과와 무관하게 흰색(`_hitFlashColor`)으로 덮어쓰고, 아니면 상태색을 그대로 대입한다. 매 프레임 즉시 대입하는 방식이라 DOTween 같은 별도 트윈 라이브러리는 필요 없다.

### 7. `OnSpawn()` 리셋 범위 확장

오브젝트 풀에서 몬스터가 재사용될 때 이전 상태의 색이 남아있지 않도록, 기존 리셋 목록에 신규 필드와 스프라이트 색 리셋을 추가한다.

```csharp
public void OnSpawn()
{
    _currentHp              = _monsterData.Hp;
    _isDead                 = false;
    _frozenSecondsRemaining = 0f;
    _slowSecondsRemaining   = 0f;
    _slowPercent            = 0f;
    _bonusCritChance        = 0f;
    _burnSecondsRemaining   = 0f;
    _flashSecondsRemaining  = 0f;
    _lastStatusVisual       = StatusVisualType.None;
    _spriteRenderer.color   = _baseColor;
    ApplyBlockSize();
    OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
}
```

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Monster/MonsterBase.cs` (수정) — 이번 작업의 유일한 변경 파일. 신규 필드(`_spriteRenderer`, `_baseColor`, `_hitFlashColor`, `_hitFlashDuration`, `_freezeTintColor`, `_burnTintColor`, `_flashSecondsRemaining`, `_burnSecondsRemaining`, `StatusVisualType`, `_lastStatusVisual`) 추가, `Awake()` 신규 추가, `ApplyFreeze`/`ApplySlow`/`ApplyDot`/`TakeDamage`/`Update`/`OnSpawn` 수정, `UpdateStatusVisual(float)` 신규 메서드 추가.

## 주의사항

- `CoDotTick()` 코루틴이 담당하는 화상 중첩 데미지 계산 로직 자체는 절대 변경하지 않는다. `_burnSecondsRemaining`은 순수 시각 효과 판단용으로 병행 추적되는 필드일 뿐이다.
- 얼어있을 때 `Update()`가 이동을 조기 종료(`return`)하는 기존 동작은 그대로 유지하되, 시각 효과 갱신(`UpdateStatusVisual`)은 그 조기 종료보다 앞서 호출되므로 얼음 상태에서도 매 프레임 정상적으로 갱신된다.
- 색상 값(`_hitFlashColor`, `_freezeTintColor`, `_burnTintColor`)과 히트 플래시 지속시간(`_hitFlashDuration`)은 전부 `[SerializeField]`로 노출되므로, 코드에 넣는 기본값은 적당한 시작값일 뿐이며 실제 톤 조정은 Unity 에디터에서 사용자가 인스펙터로 진행한다.
- `BlockVisual`(발판) 스프라이트는 건드리지 않는다. `GetComponent<SpriteRenderer>()`가 자식을 검색하지 않으므로 별도 예외처리 없이 자연스럽게 제외된다.
- 이 작업은 PDF 공식 요구사항 스펙에 없는 "가산점/다듬기" 성격의 자유 구현이므로, 요청받은 범위(피격 플래시, 얼음/화상 지속 틴트, 우선순위, 리셋)를 넘어서는 추가 이펙트(파티클 등)는 구현하지 않는다.
- 화상 DOT의 매 초 피해도 결국 `TakeDamage()`를 거쳐 흰색 플래시를 다시 트리거하므로, 화상 상태에서는 매초 짧게 흰색이 겹쳐 반짝인 뒤 다시 붉은주황색으로 돌아가는 것이 정상 동작이다.
- 이 문서는 계획 문서 작성 단계이며, 실제 `MonsterBase.cs` 코드 수정은 사용자의 명시적 승인 후 별도로 진행한다.
