# Plan — Ball Trajectory Aim Fix

이 문서는 `research.md`에서 파악한 다섯 가지 문제(궤적 상시 표시 미구현, 터치 조준 정확도 왜곡, 궤적 프리뷰 색상/크기 불일치, 이슈 2 구현 완료 후 실제 플레이 테스트에서 재발견된 조준 모델 자체의 괴리감, 그리고 이슈 4 구현 완료 후 실제 플레이 테스트에서 재발견된 터치 시작 단계 폴링 누락 문제)에 대한 구체적인 구현 계획을 다룬다. `TrajectoryPreview.cs`를 이벤트 전용 구조에서 `Update()` 기반 매 프레임 갱신 구조로 전환하고, `InputHandler.cs`의 방향 계산을 스크린 좌표 기반에서 월드 좌표 기반으로 바꾸며, `TrajectoryPreview.cs`의 색상/크기 관련 필드 값을 원본 게임에 맞게 조정한다. 이후 이슈 1~3이 구현 완료된 뒤 실제 플레이 테스트에서 재발견된 이슈 4에 대해서는 `InputHandler.cs`의 조준 모델을 상대 드래그 방식에서 절대 조준 방식으로 전환한다. 이슈 4까지 구현 완료된 뒤 실제 플레이 테스트에서 재발견된 이슈 5에 대해서는, `InputHandler.cs`의 터치/마우스 phase 분기 로직을 `TouchPhase.Began` 같은 특정 phase 값 의존에서 `_isDragging` 상태 기반 판정으로 재구성한다. 문서 갱신 대상인 `UIRules.md` 섹션 11도 함께 수정한다. 이 문서는 계획 문서이므로 실제 코드 수정은 진행하지 않는다.

## 구현 목표

- 궤적 프리뷰가 터치 여부와 무관하게 항상 화면에 표시되고, 몬스터 이동을 포함해 매 프레임 실시간으로 갱신되도록 한다.
- 터치 조준 시 손가락이 가리키는 실제 방향과 궤적 각도가 일치하도록 스크린→월드 변환을 적용한다.
- 궤적 프리뷰(레드닷/링/점선)의 색상과 크기를 원본 게임(통통 디펜스: 핀볼 마스터) 레퍼런스에 맞게 조정한다.
- 터치 시작 순간부터 궤적이 손가락 위치를 절대적으로(발사 지점 기준) 따라가도록 조준 모델을 상대 드래그 방식에서 절대 조준 방식으로 전환한다.
- 터치 시작(Began) 단계가 폴링 프레임에서 누락되더라도 조준 시작 처리(`OnAimBegin` 발행 + 즉시 조준 방향 계산)가 스킵되지 않도록, `InputHandler.cs`의 phase 분기 로직을 `_isDragging` 상태 기반 판정으로 재구성한다.
- 위 변경 사항 중 스펙 문서와 상충하는 부분(`UIRules.md` 섹션 11)을 함께 최신화한다.

## 단계별 작업 계획

### 이슈 1: 궤적 상시 표시 + 매 프레임 실시간 갱신 (`TrajectoryPreview.cs`) — (구현 완료)

1. `OnEnable()`/`OnDisable()`에서 `InputHandler.OnAimBegin`/`OnDrag`/`OnRelease`를 구독/해제하는 코드를 전부 제거한다. 더 이상 이벤트 기반으로 갱신하지 않기 때문이다.
2. `HandleAimBegin()`, `HandleDrag(Vector2 direction)`, `HandleRelease()` 세 메서드를 제거한다. 이 메서드들이 하던 일(방향 결정 + `UpdateTrajectory` 호출)을 새 `Update()`가 대체한다.
3. `Update()` 메서드를 새로 추가하고, 매 프레임 다음을 수행한다.
   - `UpdateTrajectory(BallLauncher.Instance.LaunchDirection)` 호출.
   - `BallLauncher.LaunchDirection`은 터치 중일 때는 그 프레임의 드래그 방향(`BallLauncher`가 `OnDrag`를 자체 구독해 갱신), 터치하지 않을 때는 마지막 조준 방향(기본값 `Vector2.up`)을 담고 있으므로, 별도의 상태 분기 없이 이 값 하나만 참조하면 두 상황이 자연스럽게 이어진다.
4. `Awake()`의 마지막 줄 `SetVisible(false)`를 `SetVisible(true)`로 변경해, 궤적이 처음부터 항상 보이는 상태로 시작하도록 한다.
5. `UpdateTrajectory()` 내부 로직(레이캐스트, 반사, 점 좌표 계산 등)은 수정하지 않는다. `Update()`가 매 프레임 이 메서드를 호출하는 것만으로, 몬스터가 이동해도 그 프레임의 최신 위치를 기준으로 레이캐스트가 다시 계산되어 레드닷/링이 몬스터를 따라가는 효과가 자연스럽게 나온다(부수 효과, 별도 구현 불필요).
6. `HandleRelease()` 삭제로 인해 더 이상 `SetVisible(false)`가 호출되는 지점이 없어지므로, 궤적은 항상 켜진 상태로 유지된다.

### 이슈 2: 터치 조준 정확도 — 스크린→월드 변환 적용 (`InputHandler.cs`) — (구현 완료)

1. `Camera.main`을 매 프레임 조회하지 않도록 `private Camera _mainCamera;` 필드를 추가하고, `Awake()`를 새로 만들어 `_mainCamera = Camera.main;`으로 캐싱한다(`DevRules.md`의 `Awake()`에서 컴포넌트 캐싱 규칙 준수).
2. `pressedPos.HasValue`로 `_dragStartPosition`을 저장하는 부분(현재 46행)을, 스크린 좌표를 바로 저장하는 대신 `_mainCamera.ScreenToWorldPoint(pressedPos.Value)`로 변환한 월드 좌표를 저장하도록 바꾼다. `_dragStartPosition`의 의미 자체가 "스크린 좌표"에서 "월드 좌표"로 바뀌므로 필드 주석/타입은 `Vector2`로 유지하되 저장 값의 의미가 달라짐을 코드 주석으로 명시한다.
3. `OnDrag` 발행 부분(현재 51~54행)의 `direction` 계산을, `currentPos.Value`를 스크린 좌표로 바로 빼는 대신 `_mainCamera.ScreenToWorldPoint(currentPos.Value)`로 변환한 뒤 `_dragStartPosition`(이미 월드 좌표)과의 차이를 정규화하는 방식으로 바꾼다.
   - `(월드 currentPos - 월드 dragStartPosition).normalized` 형태.
4. `ScreenToWorldPoint`에 넘기는 스크린 좌표는 `Vector3`가 필요하므로, z값은 카메라와 게임 평면 사이 거리(또는 기존에 카메라가 orthographic이면 임의의 고정값, 예: 0이나 `-_mainCamera.transform.position.z`)를 사용한다. 카메라가 orthographic이면 평행 투영이라 z값이 x, y 결과에 영향을 주지 않으므로 어떤 고정값을 넣어도 무방하다(참고용 메모, 실제 프로젝트 카메라가 orthographic인지 구현 단계에서 확인 필요).
5. `pressedPos`/`currentPos`를 읽어오는 `Touchscreen`/`Mouse` 폴링 로직 자체(20~42행)는 변경하지 않는다. 스크린 좌표를 읽어오는 지점은 그대로 두고, 그 값을 쓰는 시점(월드 변환 후 델타 계산)만 바꾼다.

### 이슈 3: 궤적 프리뷰 색상/크기 조정 (`TrajectoryPreview.cs`) — (구현 완료)

1. `_hitRing`이 `_hitColor`를 참조하는 부분(현재 `Awake()` 28행)을 새 필드로 분리한다.
   - `[SerializeField] private Color _ringColor = new Color32(225, 225, 220, 255);` 형태의 흰색 계열 필드를 새로 추가한다(아래 `_lineColor` 조정값과 동일 계열로 통일).
   - `_hitRing = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateSolidTexture());`로 변경해 링이 더 이상 `_hitColor`(빨강 계열)를 참조하지 않도록 한다.
   - `_hitDot`은 계속 `_hitColor`를 참조하되, 기본값을 `Color.red`에서 톤 다운된 브릭레드로 변경한다. 예: `[SerializeField] private Color _hitColor = new Color32(206, 90, 82, 255);` (research.md 실측 범위 RGB (200~220, 90~110, 90~100)의 중간값 근사).
2. `_dotRadius` 기본값을 `0.08f`에서 낮춘다. 예: `[SerializeField] private float _dotRadius = 0.05f;`. `_ringRadius`(`0.3f`)는 그대로 유지한다. 이 조정으로 닷/링 반지름 비율이 기존 27%에서 약 16~17% 수준으로 낮아진다. 정확한 값은 예시이며, 실제 게임 화면에서 시각 비교 후 인스펙터에서 재조정 가능하다(`[SerializeField]`이므로).
3. `DASH_WORLD_SIZE` 상수(`0.3f`)를 줄여 점선 간격을 촘촘하게 한다. 예: `0.15f`. 이 값은 `const`이므로 인스펙터 노출 없이 코드 수정으로만 반영되며, 실제 화면에서 시각 확인 후 추가 조정이 필요할 수 있다.
4. `_lineColor` 기본값을 `Color.white`에서 살짝 톤 다운된 회백색으로 변경한다. 예: `[SerializeField] private Color _lineColor = new Color32(225, 225, 220, 255);`.
5. 위 필드는 모두(또는 `DASH_WORLD_SIZE` 제외) `[SerializeField]`이므로, 코드의 기본값만으로는 화면에 반영되지 않을 수 있다. 씬/프리팹 인스펙터에 이미 개별 오버라이드 값이 저장되어 있는 경우 그 값이 우선 적용되므로, 구현 단계에서 실제 씬/프리팹의 인스펙터 값도 함께 확인하고 필요 시 리셋 또는 재적용해야 한다.

### 이슈 4: 조준 모델 전환 — 상대 드래그(relative drag) → 절대 조준(absolute aim) (`InputHandler.cs`) — (구현 완료)

이슈 2 수정(스크린→월드 변환)이 구현 완료되어 main에 반영된 이후 실제 플레이 테스트에서 재발견된 문제다(research.md 이슈 4 참고). 터치 시작 지점을 고정 기준점으로 삼아 그로부터의 상대적 이동량을 방향으로 쓰는 현재 모델 대신, 발사 지점(`BallLauncher.Instance.LaunchPoint`)에서 현재 손가락 위치를 향하는 절대 방향을 매 순간 계산하는 모델로 조준 방식 자체를 전환한다.

1. `_dragStartPosition` 필드를 제거한다. 절대 조준 모델에서는 "터치 시작 지점"이라는 기준점 개념 자체가 더 이상 필요 없기 때문이다.
2. 중복 계산을 피하기 위해 `private Vector2 ComputeAimDirection(Vector2 screenPos)` private 헬퍼 메서드를 새로 추가한다. 이 메서드는 `_mainCamera.ScreenToWorldPoint(screenPos)`로 구한 월드 좌표에서 `BallLauncher.Instance.LaunchPoint.position`을 뺀 뒤 `.normalized`로 정규화한 `Vector2`를 반환한다.
3. `pressedPos.HasValue` 분기(터치 시작 프레임, 현재 53~58행)에서, 기존에 `_dragStartPosition`을 저장만 하던 로직을 제거하고, `_isDragging = true; OnAimBegin?.Invoke();`에 이어서 곧바로 `ComputeAimDirection(pressedPos.Value)`로 방향을 계산해 `OnDrag?.Invoke(direction)`을 발행한다. 이로써 터치를 시작하는 바로 그 프레임부터 조준 방향이 확정되어 `OnDrag`가 즉시 발행된다.
4. `currentPos.HasValue && _isDragging` 분기(드래그 중, 현재 60~65행)에서도 동일하게 `ComputeAimDirection(currentPos.Value)`로 방향을 계산해 `OnDrag?.Invoke(direction)`을 발행하도록 바꾼다. 기존의 "월드 currentPos - 월드 dragStartPosition" 델타 계산 대신, 매 프레임 "발사 지점 → 현재 손가락 위치"를 새로 계산하는 방식이므로 손가락의 실제 화면 위치를 항상 따라가게 된다.
5. `released && _isDragging` 분기(릴리즈)는 `_dragStartPosition`을 참조하지 않으므로 별도 수정이 필요 없다.
6. `Camera.main` 캐싱(`_mainCamera` 필드, `Awake()`)은 이슈 2에서 이미 구현되어 있으므로 그대로 재사용한다. 이번 변경으로 새로 추가되는 의존성은 `BallLauncher.Instance.LaunchPoint` 하나뿐이다.

### 이슈 5: 터치 시작(Began) 단계 폴링 누락 — phase 값 대신 `_isDragging` 상태 기반으로 시작 판정 재구성 (`InputHandler.cs`) — (구현 완료)

이슈 4(절대 조준 모델 전환) 구현이 완료되어 main에 반영된 이후 실제 플레이 테스트에서 재발견된 문제다(research.md 이슈 5 참고). `TouchPhase.Began`이라는 특정 phase 값이 관측되어야만 조준 시작으로 인식하는 현재 구조 대신, "아직 드래그 중이 아닌 상태에서 터치/클릭이 감지되면 그 자체를 시작으로 인식"하는 상태 기반 판정으로 재구성한다. 터치와 마우스를 동일한 구조로 통일한다(아래 근거 참고).

1. `Update()` 내부에서 `pressedPos`/`currentPos`로 분리되어 있던 두 개의 `Vector2?` 지역 변수(현재 31~32행)를 `touchPos` 하나로 통합한다. "터치 시작 프레임"과 "드래그 중 프레임"을 코드 레벨에서 미리 구분해 담아두지 않고, "이번 프레임에 터치/클릭이 감지되었는가"라는 사실 하나만 담도록 단순화한다.
2. 터치 phase 분기(현재 35~48행)를 다음과 같이 재구성한다.
   - `phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary` 세 경우를 하나의 조건으로 묶어 `touchPos = touch.position.ReadValue();`를 채운다. `Began`인지 `Moved`인지를 더 이상 구분하지 않고, "터치가 유지되고 있는 상태(막 시작했든 계속 눌려있든)"라는 의미로 통합해서 읽는다.
   - `phase == TouchPhase.Ended || phase == TouchPhase.Canceled`일 때만 기존과 동일하게 `released = true;`로 처리한다.
3. 마우스 분기(현재 49~57행)도 같은 구조로 통일한다.
   - `Mouse.current.leftButton.isPressed`일 때 `touchPos = Mouse.current.position.ReadValue();`로 채운다. 기존에 `wasPressedThisFrame`으로 별도 `pressedPos`를 채우던 코드는 제거한다.
   - `Mouse.current.leftButton.wasReleasedThisFrame`일 때 `released = true;`는 그대로 유지한다.
   - **통일 근거**: research.md 이슈 5에서 확인된 대로, 기존 마우스 분기는 클릭 첫 프레임에 `wasPressedThisFrame`과 `isPressed`가 동시에 참이 되어 `pressedPos`와 `currentPos`가 모두 채워지고, 그 결과 아래 4번의 두 블록이 같은 프레임에 모두 실행되어 `OnDrag`가 동일한 값으로 두 번 중복 발행되고 있었다. 반면 마우스는 `wasPressedThisFrame` 덕분에 터치처럼 시작 단계 자체가 통째로 누락될 위험은 원천적으로 없다. 즉 마우스 쪽은 이슈 5의 "시작이 스킵되는" 버그 자체는 없지만, 터치와 다른 구조를 유지할 이유도 없고 오히려 위 중복 발행이라는 별개의 문제가 있으므로, 터치 수정과 동일한 구조로 통일하는 쪽을 택한다. 통일 후에는 클릭 첫 프레임에 `touchPos`가 한 번만 채워지고 `_isDragging`도 그 프레임에 한 번만 `false → true`로 전환되므로, `OnAimBegin`/`OnDrag` 모두 정확히 한 번씩만 발행된다.
4. `pressedPos.HasValue` 블록과 `currentPos.HasValue && _isDragging` 블록(현재 59~69행)을 다음과 같이 하나로 합친다.
   ```csharp
   if (touchPos.HasValue)
   {
       if (!_isDragging)
       {
           _isDragging = true;
           OnAimBegin?.Invoke();
       }
       OnDrag?.Invoke(ComputeAimDirection(touchPos.Value));
   }
   ```
   - `!_isDragging`(아직 드래그 중이 아님)일 때만 `OnAimBegin`을 발행하고 `_isDragging`을 `true`로 전환한다. phase가 `Began`이었는지 `Moved`였는지는 더 이상 판단 근거로 쓰지 않으므로, 같은 프레임에 `Began`이 스킵되고 `Moved`로 읽히더라도 `_isDragging`이 아직 `false`인 이상 이 프레임이 "시작"으로 정확히 인식된다.
   - 그 다음 줄의 `OnDrag?.Invoke(ComputeAimDirection(touchPos.Value))`는 시작 프레임이든 이후 드래그 프레임이든 매번 동일하게 실행되어, 이슈 4에서 확정한 "터치 시작 프레임부터 즉시 조준 방향 계산" 동작을 그대로 유지한다.
5. `released && _isDragging` 블록(현재 71~75행)은 변경하지 않는다.
6. `ComputeAimDirection(Vector2 screenPos)` 헬퍼(이슈 4에서 추가됨)는 그대로 재사용하며 수정하지 않는다.
7. 위 변경으로 `pressedPos`라는 개념 자체가 코드에서 사라지고, 터치/마우스 두 입력 방식이 "이번 프레임 입력 위치를 `touchPos`에 채운다" → "`touchPos`/`_isDragging`/`released` 세 값만으로 시작·유지·종료를 판정한다"는 동일한 구조로 통일된다.

### 문서 갱신: `UIRules.md` 섹션 11

1. "조준 중(터치 시작~릴리즈)에만 표시되고, 조준하지 않을 때는 숨겨진다." 문구를, "터치 여부와 무관하게 항상 표시되며, 매 프레임 실시간으로 갱신된다(터치 중에는 드래그 방향을, 터치하지 않을 때는 마지막 조준 방향을 기준으로 갱신)."는 취지로 수정한다.
2. Inspector 조절 값 표에 이슈 3에서 새로 추가되는 `_ringColor` 필드를 행으로 추가한다.

## 예상 변경/생성 파일 목록 (최종 구현 상태 — 이슈 1~5 전부 구현 완료)

- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` (수정 완료) — 이벤트 구독 제거, `Update()` 추가, `Awake()`의 `SetVisible(true)` 변경, `_hitColor`/`_ringColor`/`_dotRadius`/`_lineColor`/`DASH_WORLD_SIZE` 값 조정.
- `Assets/_Project/Scripts/Core/InputHandler.cs` (수정 완료) — `Camera.main` 캐싱용 `Awake()` 추가, 스크린→월드 변환 기반 조준 방향 계산(`ComputeAimDirection`), 상대 드래그 → 절대 조준 모델 전환(`_dragStartPosition` 제거), `_isDragging` 상태 기반 터치 시작 판정으로 재구성(이슈 5).
- `Assets/_Project/Docs/UIRules.md` (수정 완료) — 섹션 11 "궤적 프리뷰 시각 규칙" 문구 및 Inspector 조절 값 표(`_ringColor` 포함) 갱신.
- 새로 생성된 파일은 없다.

이슈 1~5 모두 구현이 완료되어 main에 반영되었으며, 이후 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인했다. 상세 확인 내용은 `research.md`의 "최종 구현 및 실제 플레이 테스트 완료" 참고.

## 주의사항

- 색상(`_hitColor`, `_ringColor`, `_lineColor`)과 크기(`_dotRadius`, `DASH_WORLD_SIZE`) 수치는 모두 research.md의 픽셀 추출값을 근사한 예시값이며, 실제 게임 실행 화면에서 원본 레퍼런스(`Assets/_Project/Docs/targetUI/`)와 시각적으로 비교한 뒤 미세 조정이 필요할 수 있다.
- `_hitColor`, `_ringColor`, `_dotRadius`, `_lineColor`, `_ringRadius`, `_lineWidth`는 모두 `[SerializeField]`이므로, 코드의 기본값을 바꿔도 이미 씬/프리팹 인스펙터에 저장된 오버라이드 값이 있으면 그 값이 우선 적용된다. 구현 단계에서 씬/프리팹의 실제 인스펙터 값도 함께 확인해야 한다.
- `InputHandler.cs`에서 `Camera.main`을 `Awake()`에서 캐싱하므로, 씬에 `MainCamera` 태그가 정확히 붙은 카메라가 존재하는지 사전에 확인해야 한다(태그가 없거나 다른 카메라에 붙어 있으면 `Camera.main`이 `null`을 반환해 예외가 발생할 수 있음).
- 이슈 5 변경(`touchPos.HasValue` + `!_isDragging` 통합 판정)은 `Began`이 누락되는 예외적인 프레임을 안전하게 처리하기 위한 것이지, `Began`이 정상적으로 관측되는 대다수 프레임의 동작 자체를 바꾸는 것은 아니다. 즉 일반적인 터치 시나리오에서도 시작 프레임에 `OnAimBegin` + 즉시 `OnDrag`가 정확히 한 번씩 발행되는 기존 동작이 그대로 유지되는지 구현 후 회귀 확인이 필요하다.
- 이슈 5의 마우스 분기 통합으로 기존에 있던 클릭 첫 프레임의 `OnDrag` 중복 발행(같은 값으로 두 번 호출)이 사라진다. 이 중복 호출에 의존하는 다른 코드가 없는지 확인이 필요하다. 현재 확인된 구독자(`TrajectoryPreview.UpdateTrajectory` 호출, `BallLauncher._launchDirection` 갱신)는 모두 같은 값을 다시 대입하는 멱등(idempotent) 연산이라 중복 호출이 있어도 결과에 차이가 없어 보이지만, 최종 확인은 구현 단계에서 진행한다.
- `pressedPos`/`currentPos` 두 지역 변수가 `touchPos` 하나로 합쳐지므로, 변수명 변경에 따라 관련 주석(현재 21행의 `ComputeAimDirection` 설명 주석 등)이나 남아있는 다른 참조가 없는지 함께 확인해야 한다.
- 세 이슈 모두 `DevRules.md`의 네이밍/코딩 컨벤션(private 필드 `_camelCase`, `[SerializeField]` 필드도 `_camelCase`, `Awake()`에서 컴포넌트/참조 캐싱 등)을 따라야 한다.
- 이슈 1 변경(궤적 상시 표시)과 함께 `UIRules.md` 섹션 11 문서 갱신이 누락되지 않도록 코드 변경과 문서 변경을 같은 작업 범위로 처리해야 한다.
- 이슈 4 변경으로 `InputHandler.cs`가 `BallLauncher.Instance.LaunchPoint`를 참조하는 새로운 의존성이 생긴다. `InputHandler`와 `BallLauncher` 둘 다 씬에 항상 존재하는 Singleton이므로 참조 자체는 안전하다고 판단되지만, `Awake()`/`Start()` 실행 순서상 `InputHandler`가 `BallLauncher.Instance`에 접근하는 시점에 `BallLauncher`의 `Awake()`(Instance 할당)가 이미 끝나 있는지, 그리고 `_launchPoint` 필드가 인스펙터에서 실제로 할당되어 `LaunchPoint`가 null이 아닌지 구현 단계에서 함께 확인해야 한다.
