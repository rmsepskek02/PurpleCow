# Research — Ball Trajectory Aim Fix

이 문서는 볼 궤도(트레젝토리) 프리뷰와 관련해 사용자가 지적한 두 가지 조작 문제, 즉 (1) 궤적이 터치할 때만 보이는 문제와 (2) 터치 조준 시 손가락 방향과 궤적 각도가 미묘하게 어긋나는 정확성 문제를 다룬다. 현재 구현(`InputHandler`, `TrajectoryPreview`, `BallLauncher`)을 코드 레벨에서 조사하고, 각 문제의 원인을 분석한 뒤 다음 단계(plan.md)에서 다룰 방향을 정리한다. 구체적인 코드 수정은 이 문서에서 다루지 않는다.

## 현재 상태

### 이벤트 기반 조준/궤적 갱신 구조

`InputHandler.cs`가 매 프레임(`Update()`) 터치/마우스 입력을 폴링하여 세 가지 static event를 발행한다.

- `OnAimBegin` — 터치/클릭이 시작되는 프레임에 1회 발행
- `OnDrag(Vector2 direction)` — 터치/클릭을 유지한 채 이동 중인 프레임마다 발행. 방향 벡터는 `(currentPos - _dragStartPosition).normalized`로, 터치 시작 지점과 현재 지점의 **스크린(픽셀) 좌표** 차이를 그대로 정규화한 값이다.
- `OnRelease` — 터치/클릭을 뗀 프레임에 1회 발행

`TrajectoryPreview.cs`는 `Awake()`에서 `SetVisible(false)`로 초기 비활성 상태를 만든 뒤, 위 세 이벤트를 구독해서만 궤적을 갱신한다.

- `HandleAimBegin()` → `SetVisible(true)`로 궤적을 켜고, `BallLauncher.Instance.LaunchDirection`(직전까지의 마지막 조준 방향, 최초에는 `Vector2.up`)으로 1회 궤적을 계산한다.
- `HandleDrag(direction)` → 그 프레임의 `direction`으로 `UpdateTrajectory()`를 다시 호출해 궤적을 갱신한다.
- `HandleRelease()` → `SetVisible(false)`로 궤적을 끈다.

즉 `TrajectoryPreview`에는 자체 `Update()`가 없으며, 순수하게 `InputHandler`가 보내는 이벤트에 반응해서만 궤적선/레드닷/링을 다시 그리는 구조다. 터치가 없는 동안에는 어떤 갱신도 일어나지 않는다.

`BallLauncher.cs`는 같은 `OnDrag` 이벤트를 구독해 `_launchDirection` 필드를 갱신하고(`HandleDrag`), 이 값을 `LaunchDirection` 프로퍼티로 노출한다. 이 값은 로스터 볼이 귀환했을 때(`RelaunchBall`) "그 시점의 최신 조준 방향"으로 재발사하는 데 쓰인다(`GameplayMechanics.md` 섹션 1 스펙대로 이미 구현 완료된 부분).

### 몬스터는 볼 사이클과 무관하게 항상 이동 중

`GameplayMechanics.md` 섹션 2에 따르면 몬스터는 `MonsterBase`가 매 프레임 스스로 하강하며, 볼이 몇 개 남아있든 귀환했든과 무관한 독립적 시간 흐름으로 전진한다(이미 구현 완료). 즉 필드 위 몬스터 위치는 터치 여부와 상관없이 계속 바뀌고 있으므로, `TrajectoryPreview`가 `Monster` 태그 콜라이더에 충돌 판정을 하는 현재 로직상 궤적의 충돌 지점(레드닷 위치 등)도 몬스터가 이동하면 실시간으로 달라져야 정확하다.

### UIRules.md 섹션 11의 현재 스펙 문구

`UIRules.md` 176~201행 "11. 궤적 프리뷰 시각 규칙"에는 다음과 같이 명시되어 있다.

> 조준 중(터치 시작~릴리즈)에만 표시되고, 조준하지 않을 때는 숨겨진다.

이는 지금까지의 구현(터치 시작~릴리즈 구간에만 `SetVisible(true)`)과 정확히 일치하는 스펙 문구이며, 이번에 논의된 "항상 표시" 방향으로 바뀌면 이 문서 내용도 함께 갱신되어야 한다.

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Core/InputHandler.cs` — 터치/마우스 입력을 폴링해 `OnAimBegin`/`OnDrag`/`OnRelease` static event를 발행. `OnDrag`의 방향 벡터가 스크린 픽셀 좌표 차이를 그대로 정규화한 값이라는 점이 이번 이슈 2의 핵심.
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` — 위 세 이벤트만 구독해 궤적선/레드닷/링을 갱신하는 현재 구조. 자체 `Update()` 없음. 이슈 1(항상 표시 + 실시간 갱신)의 직접적인 수정 대상.
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — `LaunchDirection` 프로퍼티(마지막 조준 방향)를 노출. 터치하지 않는 동안 궤적을 계속 그릴 때 기준이 되는 값이며, `OnDrag`를 직접 구독해 자체적으로 방향을 갱신하고 있어 `TrajectoryPreview`와 별개의 구독 구조를 갖고 있다는 점도 확인 필요.
- `Assets/_Project/Docs/GameplayMechanics.md` 섹션 1(볼 발사 및 궤도 시스템) — 터치 시작 시 조준 확정, 드래그 중 실시간 갱신, 재발사 시 최신 조준 방향 사용 등 원본 스펙 근거. 섹션 2(몬스터 스폰 및 전진 시스템) — 몬스터가 볼 사이클과 무관하게 항상 이동한다는 근거이며, 이슈 1에서 "터치하지 않을 때도 궤적 충돌 지점이 몬스터를 따라가야 하는" 이유가 된다.
- `Assets/_Project/Docs/UIRules.md` 섹션 11(궤적 프리뷰 시각 규칙) — "조준 중에만 표시" 문구가 이번 변경 방향과 상충하므로 함께 갱신이 필요한 문서.

## 문제점 / 구현 대상 파악

### 이슈 1: 궤적이 터치할 때만 보임

- 현재 `TrajectoryPreview`는 `OnAimBegin`에서 `SetVisible(true)`, `OnRelease`에서 `SetVisible(false)`를 호출하는 구조이기 때문에, 터치를 유지하고 있는 구간에서만 궤적이 보이고 손을 떼는 즉시 사라진다.
- 확정된 변경 방향은 터치 여부와 무관하게 궤적을 **항상 표시**하는 것이다. 이 경우 `SetVisible(false)`를 호출하는 시점 자체가 없어지거나 최소한 `Awake()`/`OnRelease()`의 비표시 로직을 제거하는 방향의 변경이 필요하다.
- 다만 항상 표시하는 것만으로는 부족하다. 몬스터가 볼 사이클과 무관하게 계속 이동하므로(`GameplayMechanics.md` 섹션 2), 터치하지 않는 동안에도 궤적의 충돌 지점(특히 `Monster` 태그와의 충돌로 생기는 레드닷/링 위치)이 실시간으로 갱신되어야 시각적으로 정확하다.
- 사용자가 선택한 갱신 방식은 "매 프레임 실시간 갱신(몬스터 이동 반영)"이다. 즉 현재의 순수 이벤트 기반(`OnAimBegin`/`OnDrag`/`OnRelease`로만 궤적을 다시 그리는) 구조로는 터치가 없는 프레임에 궤적을 갱신할 트리거가 전혀 없으므로, `TrajectoryPreview`에 `Update()`를 추가해 매 프레임 궤적을 재계산하는 구조로 바꿔야 한다는 것이 결론이다.
- 이 매 프레임 갱신에서 사용할 조준 방향 기준은, 터치 중일 때는 그 프레임의 드래그 방향, 터치하지 않을 때는 `BallLauncher.Instance.LaunchDirection`(다음 재발사에 쓰일 마지막 조준 방향)이 되어야 한다. 이는 `BallLauncher`가 이미 갖고 있는 값과 정확히 일치하므로 재사용이 가능해 보인다(구체적 연결 방식은 plan.md에서 다룸).
- 이 변경이 이루어지면 `UIRules.md` 섹션 11의 "조준 중(터치 시작~릴리즈)에만 표시되고, 조준하지 않을 때는 숨겨진다" 문구는 더 이상 사실과 맞지 않으므로 함께 갱신해야 한다.

### 이슈 2: 터치 조준 시 정확성이 떨어짐

- `InputHandler.cs` 53행 부근에서 조준 방향을 다음과 같이 계산한다.

```csharp
Vector2 direction = (currentPos.Value - _dragStartPosition).normalized;
OnDrag?.Invoke(direction);
```

- `_dragStartPosition`과 `currentPos`는 둘 다 `Touchscreen`/`Mouse`에서 읽어온 **스크린(픽셀) 좌표**다. 이 픽셀 좌표 차이 벡터를 정규화한 값이 별도의 스크린→월드 변환 없이 그대로 `OnDrag` 이벤트를 통해 `TrajectoryPreview.UpdateTrajectory()`와 `BallLauncher._launchDirection`(월드 스페이스에서 볼을 발사하는 실제 방향)에 쓰이고 있다.
- 사용자가 실제로 확인해준 증상은 "손가락 방향과 궤적 각도가 미묘하게 안 맞음"이다. 이는 스크린 픽셀 좌표계와 월드 좌표계의 축별 스케일이 일치하지 않을 때 나타나는 전형적인 왜곡 증상과 부합한다. 모바일 세로 화면(폭과 높이의 픽셀 비율이 1:1이 아님)에서 카메라의 투영 배율(orthographic size 등)에 의해 스크린 X축 1픽셀과 Y축 1픽셀이 나타내는 월드 단위 거리가 서로 다를 수 있는데, 이 상태에서 스크린 픽셀 벡터를 그대로 정규화해 방향으로 쓰면 실제 손가락이 가리키는 월드 방향과 각도가 어긋난다.
- 즉 근본 원인은 스크린 좌표 벡터를 스크린→월드 변환(예: 카메라 기준으로 두 스크린 포인트를 각각 월드 좌표로 변환한 뒤 그 차이로 방향을 계산하는 방식 등) 없이 그대로 정규화해서 사용하고 있다는 점으로 분석된다.
- 구체적인 해결 방식(예: `Camera.main.ScreenToWorldPoint` 활용 등)은 이 문서에서 확정하지 않으며, 다음 단계(plan.md)에서 다룬다.

## 결론

- 이슈 1(궤적이 터치할 때만 보임)의 해결 방향은 확정되었다. `TrajectoryPreview`를 현재의 이벤트 전용 구조에서 `Update()` 기반 매 프레임 재계산 구조로 변경하여, 터치 여부와 무관하게 궤적을 항상 표시하고 몬스터 이동을 실시간 반영한다. 터치 중에는 드래그 방향을, 터치하지 않을 때는 `BallLauncher.Instance.LaunchDirection`을 기준으로 궤적을 그린다. 이 변경과 함께 `UIRules.md` 섹션 11의 "조준 중에만 표시" 문구도 함께 갱신이 필요하다.
- 이슈 2(조준 정확도 문제)의 원인은 `InputHandler.cs`가 스크린 픽셀 좌표 차이를 스크린→월드 변환 없이 그대로 정규화해 조준 방향으로 사용하고 있기 때문으로 분석된다. 구체적인 해결 방식은 다음 단계(plan.md)에서 다룬다.
- 두 이슈 모두 구현이 필요한 파일은 `InputHandler.cs`, `TrajectoryPreview.cs`이며, `BallLauncher.cs`는 `LaunchDirection` 값을 참조 대상으로 다시 확인이 필요하다. 문서 갱신 대상은 `UIRules.md` 섹션 11이다.
