# Plan — 고스트볼 벽/바닥 미작동 버그 수정

research.md에서 확인한 원인(고스트 모드에서 볼의 `Collider2D`가 트리거로 전환되면서 Wall/Ground 접촉이 `OnTriggerEnter2D`로만 발생하는데, 이 콜백에 Wall/Ground 처리가 없음)을 해소하기 위한 구현 계획입니다. 몬스터 관통(피어싱)은 그대로 유지하고, Wall/Ground에서는 일반 볼과 동일하게 반사/귀환하도록 `Ball.cs`만 수정합니다.

## 구현 목표

- 고스트볼 활성 중에도 Wall에 닿으면 `OnWallHit` 이벤트 발행 및 `_remainingBounces` 차감/반사 로직이 일반 볼과 동일하게 동작한다.
- 고스트볼 활성 중에도 Ground에 닿으면 로스터 볼/분신은 `ReturnToLaunchPoint()`, 서브볼은 `ReturnToPool()`이 일반 볼과 동일하게 동작한다.
- 몬스터에 대해서는 기존과 동일하게 관통하며 데미지만 주고(반사/소멸 없음), 이 부분은 변경하지 않는다.
- research.md에서 조사한 후보 중, 신규 레이어/충돌 매트릭스/멀티 Collider 구조를 도입하는 후보 2 대신, 기존 태그 기반 처리 구조를 유지하면서 `OnTriggerEnter2D`에 Wall/Ground 분기를 추가하는 후보 1을 채택한다(DevRules.md "단순함 우선" 원칙에 부합, 씬/프리팹 설정 변경 불필요).

## 단계별 작업 계획

1. **Wall 처리 로직을 private 메서드로 추출**
   - `Ball.cs`의 `OnCollisionEnter2D` 안에 있는 `"Wall"` 태그 분기 코드(`OnWallHit` 발행, `_isReturning` 시 `ReturnToLaunchPoint()` 후 종료, 로스터/분신 예외 처리, `_remainingBounces--` 및 `ReturnToPool()` 호출)를 그대로 `private void HandleWallHit()` 메서드로 옮긴다. 로직 자체는 한 글자도 바꾸지 않고 위치만 옮기는 순수 추출이다.

2. **Ground 처리 로직을 private 메서드로 추출**
   - 같은 방식으로 `"Ground"` 태그 분기(로스터/분신이면 `ReturnToLaunchPoint()`, 그 외에는 `ReturnToPool()`)를 `private void HandleGroundHit()` 메서드로 옮긴다.

3. **`OnCollisionEnter2D`가 추출된 메서드를 호출하도록 변경**
   - 기존 `"Wall"`/`"Ground"` 분기 내용을 각각 `HandleWallHit()`/`HandleGroundHit()` 호출 한 줄로 교체한다. `"Monster"` 분기는 건드리지 않는다. 동작 변화 없음(순수 리팩토링).

4. **`OnTriggerEnter2D`에 Wall/Ground 분기 추가**
   - 기존 `other.CompareTag("Monster")` 분기 아래(또는 옆)에 `else if (other.CompareTag("Wall")) HandleWallHit();`와 `else if (other.CompareTag("Ground")) HandleGroundHit();`를 추가한다.
   - 이 두 분기는 `hasGhostSkill` 여부를 확인하지 않고 태그만으로 즉시 호출한다. 이유: `OnTriggerEnter2D`는 볼의 `Collider2D.isTrigger`가 `true`인 동안(즉 `SetGhostMode(true)`가 걸려 있는 동안)에만 발생하므로, Wall/Ground와의 트리거 접촉은 사실상 고스트 모드 중에만 일어난다. 다만 이 가정이 100% 안전한지는 "주의사항"에 별도로 남긴다.

5. **몬스터 분기는 이번 수정 범위에서 제외**
   - `OnCollisionEnter2D`와 `OnTriggerEnter2D`에 각각 존재하는 Monster 데미지 처리 코드(`CalculateDamage` + `skill.OnBallHit`)의 중복은 이번 버그와 직접 관련이 없으므로 건드리지 않는다. 이번 수정은 Wall/Ground 처리를 트리거 콜백에도 동일하게 연결하는 것에만 집중한다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Ball/Ball.cs` (수정) — `HandleWallHit()`, `HandleGroundHit()` private 메서드 신규 추출, `OnCollisionEnter2D`/`OnTriggerEnter2D`에서 해당 메서드 호출로 교체.
- 다른 파일은 변경하지 않는다(레이어/씬/프리팹 설정 변경 없음).

## 주의사항

- DevRules.md "단순함 우선"/"목표 중심 실행" 원칙에 따라, 이번 수정은 `Ball.cs`의 Wall/Ground 처리 경로 통합에만 집중하고 관련 없는 코드(Monster 분기 중복, 스킬 시스템 구조 등)는 리팩토링하지 않는다.
- 기존 코드 스타일(중괄호 없는 단문 `if`/`else if` 체인, 주석 스타일)을 그대로 유지한다.
- 구현 착수 전 아래 모호한 부분에 대한 추가 확인이 필요하다:
  1. **고스트 모드 중 벽 반사 횟수(`_remainingBounces`) 차감 여부**: 이번 계획은 일반 볼과 완전히 동일한 `HandleWallHit()`를 그대로 재사용하므로, 고스트 모드 중에도 벽에 부딪힐 때마다 `_remainingBounces`가 그대로 차감된다(로스터/분신 제외). 고스트볼이 "벽 반사 횟수 제한 없이 계속 튕겨야 한다"는 별도 요구사항이 있는지 확인이 필요하다(현재까지 논의된 요구사항에는 그런 언급이 없어, 우선 일반 볼과 동일하게 차감되는 것으로 가정하고 진행할 예정이다).
  2. **`OnTriggerEnter2D`의 Wall/Ground 분기에 `hasGhostSkill` 체크를 넣을지 여부**: 위 4단계에서 설명한 대로 체크를 생략하는 편이 더 단순하지만, 향후 고스트볼이 아닌 다른 스킬이 볼의 Collider를 트리거로 바꾸는 경우가 생기면 이 가정이 깨질 수 있다. 현재 코드베이스에는 `GhostBallSkill` 외에 `SetGhostMode()`를 호출하는 곳이 없으므로 문제 없다고 판단했으나, 구현 시점에 다시 한번 확인한다.
  3. **`OnWallHit` 이벤트 구독자에게 미치는 영향**: `OnWallHit` 이벤트를 구독하는 곳(예: 화면 흔들림, 이펙트 등)이 있다면 고스트 모드 중 벽 반사 시에도 동일하게 트리거되는 것이 의도인지 확인이 필요하다(현재 구독처는 이번 조사 범위에 포함하지 않았다).
- 이번 문서는 계획 단계이며, 사용자의 명시적인 승인 전에는 위 변경 사항을 실제 코드에 반영하지 않는다.
