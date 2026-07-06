# Research — 몬스터 피격/상태이상 스프라이트 색상 효과

이 문서는 `TODO.md` 7번 항목인 "몬스터 피격/상태이상 스프라이트 색상 효과" 구현을 위한 현재 상태 조사 문서다. 목표 자체는 이미 사용자와 논의를 마쳐 확정되어 있으며, 이 문서는 그 목표와 현재 코드 사이의 차이, 그리고 구현 시 고려해야 할 지점들을 정리해 다음 단계인 plan.md 작성의 기반을 마련한다. 구체적인 필드/메서드 설계와 색상 값 확정은 이 문서에서 다루지 않는다.

## 현재 상태

`TODO.md`에 확정된 목표는 다음과 같다.

1. 몬스터가 피격당할 때마다 본체 스프라이트가 흰색으로 아주 짧게 반짝인다(hit flash).
2. 아이스볼 효과(냉동/슬로우)가 지속되는 동안에는 본체 스프라이트가 하늘색 계열로 물든다.
3. 파이어볼 효과(화상 DOT)가 지속되는 동안에는 본체 스프라이트가 붉은주황색 계열로 물든다.
4. 얼음과 화상이 같은 몬스터에 동시에 걸리는 경우, 나중에 걸린 효과의 색을 적용한다(먼저 걸려 있던 효과의 색은 덮어쓴다).
5. 지속효과가 모두 끝나면 원래 색(기본 스프라이트 색)으로 돌아간다.
6. 몬스터가 밟고 있는 발판(돌판, `BlockVisual` 스프라이트)에는 이 색상 효과가 적용되지 않는다 — 몬스터 본체 스프라이트에만 적용한다.

반면 `MonsterBase.cs`에는 현재 `SpriteRenderer` 참조나 색상 변경 로직이 전혀 없다. 몬스터의 상태이상 관련 필드는 냉동(`_frozenSecondsRemaining`)과 슬로우(`_slowSecondsRemaining`, `_slowPercent`)뿐이며, 이 둘은 `ApplyFreeze(seconds)` / `ApplySlow(seconds, percent)`가 각각 `Mathf.Max(기존값, 새값)` 방식(슬로우는 단순 대입)으로 갱신하고 `Update()`에서 매 프레임 감소시키는 구조로 지속시간을 추적하고 있다. 반면 화상(DOT)은 `ApplyDot(damagePerSec, duration, maxStacks)`가 `StartCoroutine(CoDotTick(...))`을 호출할 뿐, 지속시간을 필드로 저장하지 않는다. `CoDotTick` 내부의 `elapsed`/`duration`은 코루틴 로컬 변수라서, 외부에서 "지금 화상 중인지"를 조회할 방법이 현재 코드에는 없다. 화상이 중첩 걸리면 `ApplyDot`이 호출될 때마다 독립된 코루틴이 새로 시작되어 여러 개가 동시에 돌아가는 구조인데, 이는 화상 중첩 스택 자체의 기존 의도된 동작이므로 이번 작업에서 손대지 않아야 한다.

피격 처리의 단일 진입점은 `TakeDamage(float damage)`다. `_isDead` 체크 → `_currentHp -= damage` → `OnHpChanged` 발행 → HP가 0 이하면 `Die()` 순서로 동작하며, 히트 플래시를 트리거하기에 가장 자연스러운 지점으로 보인다. 이 메서드를 호출하는 곳은 `Ball.CalculateDamage(MonsterBase target)`인데, 여기서는 `target.TakeDamage(damage)` 호출 뒤 정적 이벤트 `OnHitMonster`(`Action<MonsterBase, float, bool>`)를 발행한다. `DamageTextManager.cs`가 이미 이 이벤트를 구독해 데미지 텍스트를 띄우는 전례가 있지만, 히트 플래시는 굳이 이 정적 이벤트를 거치지 않고 `MonsterBase.TakeDamage()` 내부에서 인스턴스 자신에게 직접 트리거하는 편이 더 단순하다.

상태이상을 실제로 거는 쪽은 스킬 코드다. `FireBallSkill.OnBallHit()`는 `target.ApplyDot(LevelData.Value3, LevelData.Value1, (int)LevelData.Value2)`를 호출한다(Value1=지속시간, Value2=최대중첩, Value3=초당피해). `IceBallSkill.OnBallHit()`는 확률 판정 후 `target.ApplyFreeze(LevelData.Value2)`와 `target.ApplySlow(LevelData.Value2, LevelData.Value3)`를 함께 호출한다(Value1=확률, Value2=지속초, Value3=슬로우율).

몬스터 프리팹 구조도 확인했다. `Fluffy`, `Spider`, `StoneBug`, `ForestDeer` 4종 프리팹 모두 동일한 구조를 갖는다 — 루트 GameObject(몬스터 이름, `MonsterBase` 부착)에 본체 `SpriteRenderer`가 직접 붙어 있고, 별도 자식 오브젝트 `BlockVisual`에 발판(돌판) 전용 `SpriteRenderer`가 따로 붙어 있다. `MonsterOverhaulSetupEditor.cs`(191번 줄, `SpriteRenderer characterRenderer = root.GetComponent<SpriteRenderer>();`)에서 이 구조가 만들어진 근거를 확인했으며, 캐릭터 스프라이트는 `sortingOrder=1`, 블록은 `sortingOrder=0`으로 캐릭터가 앞에 그려지도록 되어 있다. 이 구조 덕분에 본체 `SpriteRenderer`가 `MonsterBase`와 같은 GameObject(루트)에 있으므로, `MonsterBase.Awake()`에서 `GetComponent<SpriteRenderer>()`로 바로 가져올 수 있다. 별도의 `[SerializeField]` Inspector 참조 연결이나 `MonsterSetupEditor.cs` / `MonsterOverhaulSetupEditor.cs` 수정이 필요 없다. `BlockVisual`은 자식 오브젝트이고 `GetComponent`는 자식을 검색하지 않으므로, 요구사항 6번(발판 제외)은 구조적으로 이미 만족되는 상태다.

## 관련 파일 및 의존성

| 파일 | 경로 | 역할 |
|---|---|---|
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 몬스터 HP/상태이상/사망 처리의 핵심 클래스. `TakeDamage`, `ApplyFreeze`, `ApplySlow`, `ApplyDot`, `OnSpawn`, `Update` 등이 이번 작업의 직접 수정 대상 |
| Ball.cs | `Assets/_Project/Scripts/Ball/Ball.cs` | `CalculateDamage(MonsterBase target)`에서 `target.TakeDamage(damage)` 호출 후 정적 이벤트 `OnHitMonster`를 발행. 히트 플래시 트리거 지점 판단에 참고 |
| FireBallSkill.cs | `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs` | `OnBallHit()`에서 `target.ApplyDot(...)` 호출. 화상(붉은주황) 상태를 거는 지점 |
| IceBallSkill.cs | `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs` | `OnBallHit()`에서 `target.ApplyFreeze(...)` + `target.ApplySlow(...)` 호출. 냉동/슬로우(하늘색) 상태를 거는 지점 |
| DamageTextManager.cs | `Assets/_Project/Scripts/UI/DamageTextManager.cs` | `Ball.OnHitMonster` 정적 이벤트를 구독하는 기존 전례. 히트 플래시를 이 이벤트로 처리할지, `TakeDamage` 내부 직접 처리할지 판단 시 참고용 |
| Fluffy.prefab / Spider.prefab / StoneBug.prefab / ForestDeer.prefab | `Assets/_Project/Prefabs/Monster/` | 4종 몬스터 프리팹. 루트에 본체 `SpriteRenderer`, 자식 `BlockVisual`에 발판 `SpriteRenderer`가 있는 동일 구조 확인 |
| MonsterOverhaulSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` (191번 줄) | 위 프리팹 구조(루트=본체 스프라이트, 자식 BlockVisual=발판 스프라이트, sortingOrder 분리)를 생성한 에디터 스크립트. 구조 근거 확인용이며 이번 작업에서 수정 대상은 아님 |
| TODO.md | `Assets/_Project/Docs/TODO.md` (7번 항목) | 이번 작업의 확정된 목표와 비고가 기록된 단일 기준 문서 |

## 문제점 / 구현 대상 파악

- **화상 상태를 조회 가능한 필드로 신규 추적해야 함**: 현재 화상은 코루틴 로컬 변수로만 지속시간을 관리해 외부에서 "지금 화상 중인지" 알 수 없다. 얼음(`_frozenSecondsRemaining`)과 동일한 패턴으로 `_burnSecondsRemaining` 같은 필드를 추가하고, `ApplyDot()` 호출 시마다 `_burnSecondsRemaining = Mathf.Max(_burnSecondsRemaining, duration)`처럼 갱신하는 방향이 자연스럽다. 다만 이 필드는 어디까지나 시각 효과 판단용으로만 쓰여야 하며, 기존 `CoDotTick()` 코루틴이 담당하는 중첩 스택 데미지 로직 자체는 변경하지 않아야 한다.
- **"나중에 걸린 효과 우선" 규칙 구현 방법**: 얼음과 화상 지속시간이 동시에 남아있는 상황이 충분히 발생할 수 있으므로, 단순히 두 지속시간 필드의 존재 여부만으로는 어느 색을 표시해야 할지 판단할 수 없다. 어느 효과가 "마지막으로 걸렸는지"를 별도로 기록하는 상태(예: enum 또는 마지막 적용 효과 종류를 나타내는 필드)가 추가로 필요하다.
- **히트 플래시와 지속 틴트가 겹치는 순간의 처리**: 예를 들어 화상 상태로 붉은주황색이 유지되고 있는 몬스터가 다시 피격당하면, 짧게 흰색으로 반짝인 뒤 다시 화상 색으로 복귀해야 한다. 두 색상 효과가 서로 다른 트리거(피격 시점 vs 상태 지속 여부)를 갖고 있어, 흰색 플래시가 끝난 뒤 원래 색이 "기본색"이 아니라 "현재 유효한 상태이상 색"으로 복귀하도록 처리하는 로직이 필요하다.
- **색상 적용/복원 방식(즉시 대입 vs 트윈) 결정 필요**: 프로젝트는 `DamageTextFx.cs`, `SkillSelectionPanel.cs` 등에서 DOTween을 이미 광범위하게 사용하고 있어, 히트 플래시나 색상 전환에도 DOTween을 쓸 수 있는 환경이 갖춰져 있다. 즉시 대입으로 처리할지 트윈으로 부드럽게 처리할지는 plan.md 단계에서 결정할 사항이다.
- **오브젝트 풀 재사용 시 리셋 처리 필요**: `OnSpawn()`은 현재 `_frozenSecondsRemaining`, `_slowSecondsRemaining`, `_slowPercent`, `_bonusCritChance`를 0으로 리셋하는데, 이번 작업으로 추가되는 신규 상태 필드(화상 지속시간, 마지막 적용 효과 종류 등)도 반드시 이 목록에 추가해 리셋해야 한다. 만약 색상 전환을 DOTween으로 구현한다면, 재사용 시점에 진행 중이던 트윈이 남아있지 않도록 `DOTween.Kill()` 등으로 정리하는 처리도 함께 필요하다.
- **정확한 색상 값과 플래시 지속시간 미확정**: `TODO.md` 비고에 명시된 대로, 하늘색/붉은주황색의 정확한 RGB 값과 흰색 히트 플래시의 지속시간(예: 0.1초 등)은 아직 결정되지 않았다. plan.md 작성 단계 또는 구현 착수 직전에 사용자에게 재확인이 필요하다.

## 결론

`MonsterBase.cs`에는 현재 스프라이트 색상 관련 로직이 전혀 없고, 얼음/슬로우는 지속시간 필드로 조회 가능한 반면 화상은 코루틴 내부에만 상태가 존재해 외부에서 조회할 수 없다는 차이가 확인되었다. 프리팹 구조상 본체 `SpriteRenderer`가 `MonsterBase`와 같은 루트 GameObject에 있어 `GetComponent<SpriteRenderer>()`만으로 발판(`BlockVisual`)을 자연스럽게 제외할 수 있다는 점도 확인했다. 화상 지속시간 추적용 신규 필드 추가, 얼음/화상 중 "나중에 걸린 효과 우선" 판정을 위한 상태 기록, 히트 플래시와 지속 틴트가 겹치는 경우의 처리, 색상 적용 방식(즉시 대입 vs DOTween), 오브젝트 풀 재사용 시 리셋 범위 확장이라는 구현 대상이 정리되었으며, 정확한 색상 값과 플래시 지속시간은 아직 미확정 상태다. 이 정도면 구현 가능한 상태이며, 구체적인 신규 필드/메서드 설계와 색상 값 확정은 plan.md에서 다룰 것이다. 이 research.md 자체는 조사 문서이며, 아직 코드 구현은 진행하지 않았다.
