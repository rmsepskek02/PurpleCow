# Research — 고스트볼 벽/바닥 미작동 버그 수정

`TODO.md` 9번 항목에서 코드 검토만으로 조사된 "고스트볼이 벽/바닥에서 반응하지 않는다"는 추정 원인을 실제 소스 코드 재확인을 통해 검증하고, 수정 방식의 후보를 조사한 문서입니다. 아직 런타임(플레이 모드) 검증은 하지 않았으며, 코드 정적 분석 결과입니다.

## 현재 상태

### Ball.cs 충돌/트리거 처리 전문 (재확인)

`Assets/_Project/Scripts/Ball/Ball.cs`

- `OnCollisionEnter2D(Collision2D collision)` (107~148행): 일반(비고스트) 볼의 모든 물리 충돌을 처리한다.
  - `"Monster"` 태그: `CalculateDamage()` 호출 후 `_skills`를 순회하며 `skill.OnBallHit(monster)` 실행.
  - `"Wall"` 태그: `OnWallHit?.Invoke(this)` 이벤트 발행 → 귀환 중(`_isReturning`)이면 `ReturnToLaunchPoint()`만 재호출하고 종료 → 로스터 볼(`BallLauncher.Instance.IsRosterMember(this)`)이거나 분신(`_isClone`)이면 반사 횟수를 소모하지 않고 그대로 반사 → 그 외(서브볼 등)는 `_remainingBounces--` 후 0 이하가 되면 `ReturnToPool()`.
  - `"Ground"` 태그: 로스터 볼/분신이면 `ReturnToLaunchPoint()`, 그 외(서브볼)는 즉시 `ReturnToPool()`.
- `OnTriggerEnter2D(Collider2D other)` (150~169행): `_skills`에 `GhostBallSkill`이 포함되어 있는지 확인한 뒤, `other.CompareTag("Monster")`인 경우에만 `CalculateDamage()` + `skill.OnBallHit(monster)`를 실행한다. **`"Wall"`/`"Ground"` 태그에 대한 분기가 전혀 존재하지 않는다.** `OnWallHit` 이벤트 발행, `_remainingBounces` 차감, `ReturnToLaunchPoint()`, `ReturnToPool()` 호출이 모두 빠져 있다.
- `SetGhostMode(bool isGhost)` (263~266행): `_collider.isTrigger = isGhost` 한 줄. 볼 자신의 `Collider2D`(`CircleCollider2D`) 전체를 트리거로 전환하며, 몬스터/벽/바닥을 구분하지 않는다.

### GhostBallSkill.cs

`Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs`

- `OnActivate()` → `_ball.SetGhostMode(true)`
- `OnDeactivate()` → `_ball.SetGhostMode(false)`
- `OnBallHit(MonsterBase target)` → 관통이므로 추가 처리 없음(주석: "관통 — 추가 처리 없음, 볼은 계속 이동")

코드 상단 주석("Ghost 볼은 Monster 레이어와 물리 충돌하지 않음 / Collider를 Trigger로 전환 → OnTriggerEnter2D로 데미지 처리")은 몬스터 피어싱만 의도한 것으로 보이며, Wall/Ground와의 관계는 애초에 고려되지 않은 것으로 보인다.

### Unity 2D 물리 규칙 재확인

두 `Collider2D` 중 하나라도 `isTrigger = true`이면, 물리 엔진은 해당 쌍을 절대 `OnCollisionEnter2D`로 보고하지 않고 항상 `OnTriggerEnter2D`(양쪽 Rigidbody2D가 있는 경우 양쪽 모두)로만 보고한다. 이는 태그·레이어와 무관하게 Collider의 `isTrigger` 플래그만으로 결정되는 Unity 엔진의 고정 동작이다.

따라서 `SetGhostMode(true)`로 볼의 `_collider.isTrigger`가 `true`가 되는 순간, 그 볼과 Wall/Ground/Monster 모든 상대방과의 상호작용이 예외 없이 트리거 이벤트로 바뀐다. 그런데 `OnTriggerEnter2D`는 Monster만 처리하므로 Wall/Ground 접촉 시 아무 코드도 실행되지 않는다 — 벽에 닿아도 반사되지 않고(`OnWallHit` 미발행, `_remainingBounces` 미차감), 바닥에 닿아도 귀환/풀반환이 일어나지 않는다. 볼은 그대로 화면 밖으로 계속 날아가며, 다시 발사 지점으로 돌아오지 않는다.

이는 사용자가 실제 플레이에서 확인한 "고스트볼이 벽을 뚫고 밖으로 나가는 현상"과 정확히 일치한다. 코드 정적 분석과 사용자의 런타임 관찰이 서로 부합하므로, 이 원인 추정은 신뢰도가 높다.

### 씬 오브젝트의 Collider/레이어 구조

`Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `PlaceColliderObject()`(`Step5_PlaceWallsAndGround()`에서 Wall_Left/Wall_Right/Ground/Wall_Top 생성)를 보면, Wall/Ground GameObject는 `BoxCollider2D`만 추가하고 `isTrigger`를 별도로 설정하지 않는다(기본값 `false`). 태그만 `"Wall"`/`"Ground"`로 설정되며, 레이어는 별도로 지정하지 않아 기본(`Default`) 레이어에 남는다.

`Assets/_Project/Scripts/Editor/BallSetupEditor.cs`를 보면, 이 프로젝트에서 유일하게 커스텀 Physics2D 레이어를 사용하는 사례는 `"Ball"` 레이어 하나뿐이며, 목적은 "볼끼리 물리적으로 충돌(튕겨나감)하는 것을 막기 위함"(주석 원문)이다. `AddBallLayer()`가 커스텀 슬롯(8~31) 중 빈 슬롯에 `"Ball"`을 등록하고, `AssignBallPrefabLayer()`가 `Ball.prefab`의 레이어를 그렇게 등록된 `"Ball"`로 변경한다. Monster/Wall/Ground 전용 레이어는 코드 어디에도 존재하지 않으며, 이들은 전부 태그로만 구분되고 레이어는 `Default` 그대로다. Physics2D 충돌 매트릭스(Project Settings > Physics 2D)에서 레이어별 상호작용을 세밀하게 끄고 켜는 설정도 코드로 자동화되어 있지 않다(수동 설정 여부는 확인 불가 — 코드 기준으로는 Ball 레이어 하나만 등록되어 있음).

### 데미지 처리 로직 중복

`OnCollisionEnter2D`의 Monster 분기와 `OnTriggerEnter2D`의 Monster 분기는 다음 3줄이 그대로 중복되어 있다.

```csharp
Vector2 vel = _rigidbody.linearVelocity.normalized;
CalculateDamage(monster, vel.y < 0f);
foreach (var skill in _skills)
    skill.OnBallHit(monster);
```

(단, `OnCollisionEnter2D`는 `collision.gameObject.TryGetComponent<MonsterBase>`을, `OnTriggerEnter2D`는 `other.TryGetComponent<MonsterBase>`을 사용하는 차이만 있음.)

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Ball/Ball.cs` — `OnCollisionEnter2D`, `OnTriggerEnter2D`, `SetGhostMode`, `_remainingBounces`, `OnWallHit` 이벤트, `ReturnToLaunchPoint()`, `ReturnToPool()` 정의.
- `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs` — `SetGhostMode(true/false)` 호출 지점, `OnBallHit()` 관통 처리(수정 불필요 예상).
- `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` — `OnBallHit`/`OnActivate`/`OnDeactivate` 추상 정의. `Ball`과의 결합 방식 확인용.
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — `IsRosterMember()`, `HandleBallRecovered()`, `RelaunchQueuedBall()` 등 귀환/재발사 흐름. Wall/Ground 분기가 참조하는 외부 의존.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — Wall/Ground GameObject 생성 로직(`PlaceColliderObject`), Collider/태그 설정 확인.
- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` — 프로젝트에서 유일하게 존재하는 커스텀 Physics2D 레이어(`"Ball"`) 등록/할당 로직. Monster/Wall/Ground 전용 레이어가 없음을 확인하는 근거.
- `Assets/_Project/Docs/TODO.md` 9번 항목 — 이번 조사의 배경이 된 기존 추정 내용.

## 문제점 / 구현 대상 파악

**요구사항**: 고스트볼 활성 중에는 몬스터를 관통(피어싱)하며 데미지를 주되, Wall/Ground와는 일반 볼과 동일하게 반사/귀환해야 한다.

**문제**: 볼의 `Collider2D` 전체를 트리거로 전환하는 현재 `SetGhostMode()` 구현 때문에, 몬스터뿐 아니라 Wall/Ground와의 상호작용도 함께 트리거 이벤트로 바뀌어 버린다. `OnTriggerEnter2D`가 Monster만 처리하므로 Wall/Ground 충돌 시 아무 반응이 없다.

**수정 방식 후보**:

1. **`OnTriggerEnter2D`에 Wall/Ground 태그 처리 추가** — `OnCollisionEnter2D`의 Wall/Ground 로직과 동일한 동작을 트리거 버전에도 추가한다. `OnCollisionEnter2D`와 `OnTriggerEnter2D`가 `Wall`/`Ground`/`Monster` 각 태그에 대해 사실상 동일한 로직을 실행해야 하므로, 태그별 처리를 `HandleWallHit()`/`HandleGroundHit()`/`HandleMonsterHit(GameObject target)` 같은 private 메서드로 추출해 두 콜백에서 공통 호출하는 방식이 자연스럽다.
   - 장점: 기존 구조(태그 기반 처리, Collider 전체 트리거 전환)를 그대로 유지하며 최소 변경으로 해결 가능. 새로운 씬 설정(레이어 추가, 충돌 매트릭스 조정)이 필요 없다.
   - 단점: `OnCollisionEnter2D`/`OnTriggerEnter2D` 두 콜백이 계속 공존하며 코드가 두 군데로 나뉜다(다만 공통 메서드 추출로 중복 자체는 없앨 수 있음).

2. **레이어 기반 충돌 매트릭스로 몬스터와의 상호작용만 트리거로 만들고 Wall/Ground와는 여전히 물리 충돌로 남기는 방법** — 예를 들어 Monster 전용 레이어를 신설하고, 고스트 모드에서는 볼의 Collider를 트리거로 바꾸는 대신 `Physics2D.IgnoreLayerCollision()`이나 별도의 트리거 전용 Collider(예: 몬스터 감지용 별도 트리거 Collider2D를 볼에 추가)를 두어 몬스터만 트리거로 감지하고, 기존 메인 Collider는 계속 non-trigger로 유지해 Wall/Ground와는 그대로 `OnCollisionEnter2D`가 발생하게 하는 방법.
   - 조사 결과: 현재 프로젝트에는 Monster/Wall/Ground 전용 레이어가 전혀 없다(`SceneSetupEditor.cs` 기준 전부 `Default` 레이어, 태그로만 구분). 유일한 커스텀 레이어는 볼-볼 충돌 방지용 `"Ball"` 레이어(`BallSetupEditor.cs`)뿐이다.
   - 이 방식을 적용하려면 (a) Monster 전용 레이어 신설, (b) `SceneSetupEditor.cs`/`BallSetupEditor.cs`에 레이어 등록·할당 로직 추가, (c) Physics2D 충돌 매트릭스 조정, (d) 볼에 몬스터 감지 전용 트리거 Collider를 별도로 추가하고 그 Collider만의 `OnTriggerEnter2D`를 구분해서 받는 구조 변경이 필요하다 — 기존 "Collider 하나 + isTrigger 토글"이라는 단순 구조를 다중 Collider/레이어 구조로 바꾸는 상당히 큰 리팩토링이다.
   - 장점: `OnCollisionEnter2D`/`OnTriggerEnter2D`가 태그별로 명확히 역할 분리되어(물리 충돌은 Wall/Ground, 트리거는 Monster) 개념적으로 더 깔끔할 수 있다.
   - 단점: 새 레이어 신설, 충돌 매트릭스 조정, 볼 프리팹에 자식 Collider 추가 등 씬/프리팹 설정 변경이 다수 필요하며, 변경 범위와 회귀 리스크가 후보 1보다 훨씬 크다. DevRules.md의 "단순함 우선" 원칙과 배치된다.

## 결론

- 원인은 `TODO.md` 9번 항목의 추정과 일치함을 코드 재확인으로 검증했다: `SetGhostMode(true)`가 볼의 Collider 전체를 트리거로 바꾸는데 `OnTriggerEnter2D`는 Monster만 처리하고 Wall/Ground 분기가 없어, 고스트 모드에서는 벽/바닥 접촉이 트리거 이벤트로 발생하지만 아무 처리도 되지 않는다.
- 두 가지 수정 후보를 조사했다. 후보 1(`OnTriggerEnter2D`에 Wall/Ground 처리 추가, 공통 로직은 private 메서드로 추출)은 기존 구조를 그대로 유지하며 최소 변경으로 해결 가능하다. 후보 2(레이어/다중 Collider 기반 분리)는 개념적으로는 더 명확할 수 있으나 신규 레이어 신설, 충돌 매트릭스 조정, 프리팹 구조 변경 등 변경 범위가 크고 현재 프로젝트에 그런 인프라가 전혀 없어 새로 구축해야 한다.
- 다음 plan.md에서는 DevRules.md "단순함 우선" 원칙에 따라 후보 1을 기반으로 구체적인 구현 단계를 제시한다.
