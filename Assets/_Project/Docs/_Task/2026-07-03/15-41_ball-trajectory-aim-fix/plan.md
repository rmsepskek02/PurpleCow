# Plan — Ball Trajectory Aim Fix

이 문서는 `research.md`에서 파악한 세 가지 문제(궤적 상시 표시 미구현, 터치 조준 정확도 왜곡, 궤적 프리뷰 색상/크기 불일치)에 대한 구체적인 구현 계획을 다룬다. `TrajectoryPreview.cs`를 이벤트 전용 구조에서 `Update()` 기반 매 프레임 갱신 구조로 전환하고, `InputHandler.cs`의 방향 계산을 스크린 좌표 기반에서 월드 좌표 기반으로 바꾸며, `TrajectoryPreview.cs`의 색상/크기 관련 필드 값을 원본 게임에 맞게 조정한다. 문서 갱신 대상인 `UIRules.md` 섹션 11도 함께 수정한다. 이 문서는 계획 문서이므로 실제 코드 수정은 진행하지 않는다.

## 구현 목표

- 궤적 프리뷰가 터치 여부와 무관하게 항상 화면에 표시되고, 몬스터 이동을 포함해 매 프레임 실시간으로 갱신되도록 한다.
- 터치 조준 시 손가락이 가리키는 실제 방향과 궤적 각도가 일치하도록 스크린→월드 변환을 적용한다.
- 궤적 프리뷰(레드닷/링/점선)의 색상과 크기를 원본 게임(통통 디펜스: 핀볼 마스터) 레퍼런스에 맞게 조정한다.
- 위 변경 사항 중 스펙 문서와 상충하는 부분(`UIRules.md` 섹션 11)을 함께 최신화한다.

## 단계별 작업 계획

### 이슈 1: 궤적 상시 표시 + 매 프레임 실시간 갱신 (`TrajectoryPreview.cs`)

1. `OnEnable()`/`OnDisable()`에서 `InputHandler.OnAimBegin`/`OnDrag`/`OnRelease`를 구독/해제하는 코드를 전부 제거한다. 더 이상 이벤트 기반으로 갱신하지 않기 때문이다.
2. `HandleAimBegin()`, `HandleDrag(Vector2 direction)`, `HandleRelease()` 세 메서드를 제거한다. 이 메서드들이 하던 일(방향 결정 + `UpdateTrajectory` 호출)을 새 `Update()`가 대체한다.
3. `Update()` 메서드를 새로 추가하고, 매 프레임 다음을 수행한다.
   - `UpdateTrajectory(BallLauncher.Instance.LaunchDirection)` 호출.
   - `BallLauncher.LaunchDirection`은 터치 중일 때는 그 프레임의 드래그 방향(`BallLauncher`가 `OnDrag`를 자체 구독해 갱신), 터치하지 않을 때는 마지막 조준 방향(기본값 `Vector2.up`)을 담고 있으므로, 별도의 상태 분기 없이 이 값 하나만 참조하면 두 상황이 자연스럽게 이어진다.
4. `Awake()`의 마지막 줄 `SetVisible(false)`를 `SetVisible(true)`로 변경해, 궤적이 처음부터 항상 보이는 상태로 시작하도록 한다.
5. `UpdateTrajectory()` 내부 로직(레이캐스트, 반사, 점 좌표 계산 등)은 수정하지 않는다. `Update()`가 매 프레임 이 메서드를 호출하는 것만으로, 몬스터가 이동해도 그 프레임의 최신 위치를 기준으로 레이캐스트가 다시 계산되어 레드닷/링이 몬스터를 따라가는 효과가 자연스럽게 나온다(부수 효과, 별도 구현 불필요).
6. `HandleRelease()` 삭제로 인해 더 이상 `SetVisible(false)`가 호출되는 지점이 없어지므로, 궤적은 항상 켜진 상태로 유지된다.

### 이슈 2: 터치 조준 정확도 — 스크린→월드 변환 적용 (`InputHandler.cs`)

1. `Camera.main`을 매 프레임 조회하지 않도록 `private Camera _mainCamera;` 필드를 추가하고, `Awake()`를 새로 만들어 `_mainCamera = Camera.main;`으로 캐싱한다(`DevRules.md`의 `Awake()`에서 컴포넌트 캐싱 규칙 준수).
2. `pressedPos.HasValue`로 `_dragStartPosition`을 저장하는 부분(현재 46행)을, 스크린 좌표를 바로 저장하는 대신 `_mainCamera.ScreenToWorldPoint(pressedPos.Value)`로 변환한 월드 좌표를 저장하도록 바꾼다. `_dragStartPosition`의 의미 자체가 "스크린 좌표"에서 "월드 좌표"로 바뀌므로 필드 주석/타입은 `Vector2`로 유지하되 저장 값의 의미가 달라짐을 코드 주석으로 명시한다.
3. `OnDrag` 발행 부분(현재 51~54행)의 `direction` 계산을, `currentPos.Value`를 스크린 좌표로 바로 빼는 대신 `_mainCamera.ScreenToWorldPoint(currentPos.Value)`로 변환한 뒤 `_dragStartPosition`(이미 월드 좌표)과의 차이를 정규화하는 방식으로 바꾼다.
   - `(월드 currentPos - 월드 dragStartPosition).normalized` 형태.
4. `ScreenToWorldPoint`에 넘기는 스크린 좌표는 `Vector3`가 필요하므로, z값은 카메라와 게임 평면 사이 거리(또는 기존에 카메라가 orthographic이면 임의의 고정값, 예: 0이나 `-_mainCamera.transform.position.z`)를 사용한다. 카메라가 orthographic이면 평행 투영이라 z값이 x, y 결과에 영향을 주지 않으므로 어떤 고정값을 넣어도 무방하다(참고용 메모, 실제 프로젝트 카메라가 orthographic인지 구현 단계에서 확인 필요).
5. `pressedPos`/`currentPos`를 읽어오는 `Touchscreen`/`Mouse` 폴링 로직 자체(20~42행)는 변경하지 않는다. 스크린 좌표를 읽어오는 지점은 그대로 두고, 그 값을 쓰는 시점(월드 변환 후 델타 계산)만 바꾼다.

### 이슈 3: 궤적 프리뷰 색상/크기 조정 (`TrajectoryPreview.cs`)

1. `_hitRing`이 `_hitColor`를 참조하는 부분(현재 `Awake()` 28행)을 새 필드로 분리한다.
   - `[SerializeField] private Color _ringColor = new Color32(225, 225, 220, 255);` 형태의 흰색 계열 필드를 새로 추가한다(아래 `_lineColor` 조정값과 동일 계열로 통일).
   - `_hitRing = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateSolidTexture());`로 변경해 링이 더 이상 `_hitColor`(빨강 계열)를 참조하지 않도록 한다.
   - `_hitDot`은 계속 `_hitColor`를 참조하되, 기본값을 `Color.red`에서 톤 다운된 브릭레드로 변경한다. 예: `[SerializeField] private Color _hitColor = new Color32(206, 90, 82, 255);` (research.md 실측 범위 RGB (200~220, 90~110, 90~100)의 중간값 근사).
2. `_dotRadius` 기본값을 `0.08f`에서 낮춘다. 예: `[SerializeField] private float _dotRadius = 0.05f;`. `_ringRadius`(`0.3f`)는 그대로 유지한다. 이 조정으로 닷/링 반지름 비율이 기존 27%에서 약 16~17% 수준으로 낮아진다. 정확한 값은 예시이며, 실제 게임 화면에서 시각 비교 후 인스펙터에서 재조정 가능하다(`[SerializeField]`이므로).
3. `DASH_WORLD_SIZE` 상수(`0.3f`)를 줄여 점선 간격을 촘촘하게 한다. 예: `0.15f`. 이 값은 `const`이므로 인스펙터 노출 없이 코드 수정으로만 반영되며, 실제 화면에서 시각 확인 후 추가 조정이 필요할 수 있다.
4. `_lineColor` 기본값을 `Color.white`에서 살짝 톤 다운된 회백색으로 변경한다. 예: `[SerializeField] private Color _lineColor = new Color32(225, 225, 220, 255);`.
5. 위 필드는 모두(또는 `DASH_WORLD_SIZE` 제외) `[SerializeField]`이므로, 코드의 기본값만으로는 화면에 반영되지 않을 수 있다. 씬/프리팹 인스펙터에 이미 개별 오버라이드 값이 저장되어 있는 경우 그 값이 우선 적용되므로, 구현 단계에서 실제 씬/프리팹의 인스펙터 값도 함께 확인하고 필요 시 리셋 또는 재적용해야 한다.

### 문서 갱신: `UIRules.md` 섹션 11

1. "조준 중(터치 시작~릴리즈)에만 표시되고, 조준하지 않을 때는 숨겨진다." 문구를, "터치 여부와 무관하게 항상 표시되며, 매 프레임 실시간으로 갱신된다(터치 중에는 드래그 방향을, 터치하지 않을 때는 마지막 조준 방향을 기준으로 갱신)."는 취지로 수정한다.
2. Inspector 조절 값 표에 이슈 3에서 새로 추가되는 `_ringColor` 필드를 행으로 추가한다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` (수정) — 이벤트 구독 제거, `Update()` 추가, `Awake()`의 `SetVisible(true)` 변경, `_hitColor`/`_ringColor`/`_dotRadius`/`_lineColor`/`DASH_WORLD_SIZE` 값 조정.
- `Assets/_Project/Scripts/Core/InputHandler.cs` (수정) — `Camera.main` 캐싱용 `Awake()` 추가, `_dragStartPosition`/드래그 방향 계산을 스크린→월드 변환 기반으로 변경.
- `Assets/_Project/Docs/UIRules.md` (수정) — 섹션 11 "궤적 프리뷰 시각 규칙" 문구 및 Inspector 조절 값 표 갱신.
- 새로 생성되는 파일은 없다.

## 주의사항

- 색상(`_hitColor`, `_ringColor`, `_lineColor`)과 크기(`_dotRadius`, `DASH_WORLD_SIZE`) 수치는 모두 research.md의 픽셀 추출값을 근사한 예시값이며, 실제 게임 실행 화면에서 원본 레퍼런스(`Assets/_Project/Docs/targetUI/`)와 시각적으로 비교한 뒤 미세 조정이 필요할 수 있다.
- `_hitColor`, `_ringColor`, `_dotRadius`, `_lineColor`, `_ringRadius`, `_lineWidth`는 모두 `[SerializeField]`이므로, 코드의 기본값을 바꿔도 이미 씬/프리팹 인스펙터에 저장된 오버라이드 값이 있으면 그 값이 우선 적용된다. 구현 단계에서 씬/프리팹의 실제 인스펙터 값도 함께 확인해야 한다.
- `InputHandler.cs`에서 `Camera.main`을 `Awake()`에서 캐싱하므로, 씬에 `MainCamera` 태그가 정확히 붙은 카메라가 존재하는지 사전에 확인해야 한다(태그가 없거나 다른 카메라에 붙어 있으면 `Camera.main`이 `null`을 반환해 예외가 발생할 수 있음).
- 세 이슈 모두 `DevRules.md`의 네이밍/코딩 컨벤션(private 필드 `_camelCase`, `[SerializeField]` 필드도 `_camelCase`, `Awake()`에서 컴포넌트/참조 캐싱 등)을 따라야 한다.
- 이슈 1 변경(궤적 상시 표시)과 함께 `UIRules.md` 섹션 11 문서 갱신이 누락되지 않도록 코드 변경과 문서 변경을 같은 작업 범위로 처리해야 한다.
