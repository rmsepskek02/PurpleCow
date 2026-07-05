# Research — 조준 방향 Y좌표 하한 제한 (격자 밑변 기준)

볼 조준(터치 드래그) 시 목표 지점의 월드 Y좌표가 배경 격자(그리드)의 밑변보다 아래로 내려가지 못하도록 제한하는 작업을 위한 조사 문서다. 현재 `InputHandler.ComputeAimDirection`은 터치 좌표를 아무 제한 없이 그대로 사용해 조준 방향을 계산하므로, 손가락이 발사 지점보다 화면 아래쪽을 터치하면 아래/뒤쪽을 향하는 방향까지 그대로 허용된다. 격자 밑변에 해당하는 실제 기준점은 `WallFitter`가 디바이스별로 동적으로 재배치하는 `Ground` Transform의 런타임 Y좌표이며, 이를 어떻게 `InputHandler`에 연결할지가 이번 작업의 핵심 이슈다.

## 현재 상태

- `Assets/_Project/Scripts/Core/InputHandler.cs`의 `ComputeAimDirection(Vector2 screenPos)`(22~27행):
  ```csharp
  private Vector2 ComputeAimDirection(Vector2 screenPos)
  {
      Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
      Vector2 launchPointPos = BallLauncher.Instance.LaunchPoint.position;
      return (worldPos - launchPointPos).normalized;
  }
  ```
  Y좌표에 대한 clamp가 전혀 없다. 이 메서드는 `Update()`에서 터치/마우스 입력이 감지될 때마다(터치 시작 프레임 포함, 매 드래그 프레임 포함) 호출되어 `OnDrag` 이벤트로 방향 벡터를 전파한다(62행 `OnDrag?.Invoke(ComputeAimDirection(touchPos.Value));`). 호출 지점은 이 한 곳뿐이며, 터치 시작과 드래그가 동일 코드 경로를 공유한다(`GameplayMechanics.md` 1번 항목에서 언급한 "터치 시작 시 즉시 조준 확정" 스펙과 "드래그 중 실시간 갱신" 스펙 둘 다 이 한 줄에서 처리됨).
- `InputHandler`는 `_mainCamera`, `BallLauncher.Instance.LaunchPoint`만 참조하며, `Ground`나 `WallFitter`에 대한 참조가 전혀 없다.
- `Assets/_Project/Scripts/Core/WallFitter.cs`는 `[ExecuteAlways]` `MonoBehaviour`(싱글톤 아님)로, 기기별 화면 비율에 맞춰 `Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`/`LaunchPoint`의 위치를 매 프레임이 아니라 `Start()`/`OnValidate()` 시점에 재계산해 적용한다(`Apply()`, 33~54행).
  - `_ground`는 `[SerializeField] private Transform`(11행)으로 **private**이다. 외부 스크립트가 `WallFitter` 인스턴스를 참조하더라도 `_ground` 필드 자체에는 직접 접근할 수 없다. 현재 이 값을 외부에 노출하는 프로퍼티가 전혀 없다(`BallLauncher.cs`의 `public Transform LaunchPoint => _launchPoint;` 같은 패턴이 `WallFitter`에는 없음).
  - `SetY(_ground, _nativeBottomY * scaleY);`(52행)로 `Ground`의 Y좌표가 매번 갱신된다. `_nativeBottomY = -6.5f`(고정), `scaleY = uniformScale * _cellAspectCorrection`(디바이스별 화면 크기에 따라 달라짐)이므로, `Ground`의 실제 월드 Y좌표는 하드코딩된 상수가 아니라 런타임에만 확정된다.
  - 참고로 `_nativeLaunchPointY = -6.0f`이 `_nativeBottomY = -6.5f`보다 항상 크므로(둘 다 같은 `scaleY`를 곱하므로 부호가 유지됨), `LaunchPoint`의 Y좌표는 `Ground`의 Y좌표보다 항상 위(덜 음수)에 위치한다. 이는 씬 파일 실측치로도 확인된다(아래 "관련 파일 및 의존성" 참고).
- `Assets/_Project/Scripts/Wave/WaveManager.cs`의 `_bottomBoundaryY`(15행, `[SerializeField] private float`)는 `Assets/Scenes/SampleScene.unity`에 `-5`로 하드코딩 저장되어 있다(720행 `_bottomBoundaryY: -5`). 이는 몬스터가 바닥에 도달했는지 판정하는 게임오버 기준선으로, `WallFitter`가 디바이스별로 재계산하는 `Ground`의 실제 위치와는 완전히 별개의(동기화되지 않는) 값이다. 이번 조준 제한의 기준점으로 이 값을 재사용하는 것은 부적절하다 — 화면 비율이 바뀌면 `Ground`의 실제 위치는 변하지만 `_bottomBoundaryY`는 그대로 `-5`로 고정돼 있어 기기별로 조준 제한선과 실제 격자 그림이 어긋나게 된다.
- `Assets/Scenes/SampleScene.unity`를 직접 확인한 결과, `Ground` GameObject(`m_Name: Ground`, `m_TagString: Ground`, 605~682행)의 `m_LocalPosition`은 `{x: 0, y: -6.397638, z: 0}`(677행)로 저장되어 있다. 이는 에디터에서 `WallFitter.OnValidate()`가 마지막으로 저장 시점의 화면 비율 기준으로 계산해 넣은 값이며, 실제 빌드/디바이스에서는 `WallFitter.Apply()`가 다시 계산해 덮어쓴다. `WaveManager._bottomBoundaryY: -5`(720행)와 비교하면 이미 값이 다르다는 것이 실측으로 확인된다.

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Core/InputHandler.cs` — 수정 대상. `ComputeAimDirection`에 Y좌표 clamp 로직을 추가해야 하며, 이를 위해 `Ground`(또는 `WallFitter`)에 대한 참조를 새로 확보해야 한다. 현재 `Awake()`(15~19행)에서 `_mainCamera = Camera.main;`만 캐싱한다.
- `Assets/_Project/Scripts/Core/WallFitter.cs` — 참조 후보. `_ground` 필드가 private이라 외부 노출 프로퍼티(`public float GroundY => _ground.position.y;` 등)를 추가하지 않으면 `InputHandler`가 이 값을 읽을 방법이 없다. `MonoBehaviour`이며 싱글톤이 아니므로, `InputHandler`가 참조하려면 (1) `[SerializeField]`로 씬에서 직접 연결하거나 (2) `FindFirstObjectByType<WallFitter>()`로 런타임에 찾아야 한다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `Step5_PlaceWallsAndGround()`(388~394행)에서 `PlaceColliderObject("Ground", "Ground", new Vector3(0f, -10f, 0f), new Vector2(12f, 0.2f));`로 `Ground` GameObject를 생성한다. 이름은 정확히 `"Ground"`(따옴표 그대로, 대소문자 구분)이고, 태그도 `"Ground"`로 설정된다(`TrySetTag(go, tag)`). 즉 `GameObject.Find("Ground")`로도, `GameObject.FindWithTag("Ground")`로도 둘 다 찾을 수 있는 상태다.
  - `Step6_SetupWallFitter()`(419~462행)에서 `Transform ground = FindTransformOrWarn("Ground");`(442행)로 찾은 뒤 `so.FindProperty("_ground").objectReferenceValue = ground;`(451행)로 `WallFitter._ground`에 연결한다. 이 Step은 `Main Camera` GameObject에 `WallFitter` 컴포넌트를 추가/연동하는 역할만 하며, `InputHandler`에는 어떤 참조도 연결하지 않는다.
  - `Step7_PlaceManagers()`(479~489행)에서 `PlaceManager<InputHandler>("InputHandler")`로 `InputHandler` GameObject를 생성하지만, 이후 `InputHandler`의 필드를 채워주는 `Step*_ConnectInputHandlerRefs` 같은 함수는 존재하지 않는다(`BallLauncher`는 `Step8_ConnectBallLauncherRefs`가 있지만 `InputHandler`는 대응하는 Step이 없음). 만약 `InputHandler`에 `[SerializeField] private Transform _ground;`를 새로 추가한다면, 이 필드를 자동으로 채워주는 에디터 코드가 없으므로 씬에서 수동으로 드래그해 연결하거나, `Step6_SetupWallFitter()` 부근에 유사한 연결 코드를 새로 추가해야 한다(plan.md에서 결정).
- `Assets/_Project/Docs/GameplayMechanics.md` 섹션 1(볼 발사 및 궤도 시스템, 10~32행) — "조준 방향이 손가락 위치를 그대로 따라간다"는 기존 서술(14, 20행)과 이번에 추가할 "Y 하한 제한"이 상충하지 않는지 검토했다. 기존 서술은 손 위치를 향한다는 방향성만 규정할 뿐 좌표 범위 제한을 언급하지 않으므로 직접적인 모순은 없다. 다만 사용자가 명시적으로 요청한 새 규칙(격자 밑변 아래로는 조준 불가)이므로, 구현 완료 후에는 이 섹션 문서에 새 항목을 추가해 규칙을 명문화해야 한다(plan.md 및 구현 단계에서 반영 필요 — 이번 research.md 단계에서는 문서 자체를 수정하지 않음).

## 문제점 / 구현 대상 파악

**핵심 문제**: `ComputeAimDirection`이 터치 좌표를 변환한 `worldPos`를 그대로 `LaunchPoint` 기준 방향 계산에 사용하기 때문에, `worldPos.y`가 격자 밑변(`Ground`의 Y좌표)보다 낮아지는 경우에 대한 제약이 없다.

**Ground 참조 확보 방식 후보** (구체적 결정은 plan.md):
- (A) `InputHandler`에 `[SerializeField] private Transform _ground;`를 신설하고 씬(`SampleScene.unity`)에서 `Ground` GameObject를 직접 드래그해 연결. 가장 단순하고 `WallFitter` 내부 구현에 의존하지 않는다는 장점이 있으나, `SceneSetupEditor.cs`에도 연결 코드를 추가해야(또는 수동 연결 필요) 씬 재생성 시 누락되지 않는다.
- (B) `WallFitter`에 `public float GroundY => _ground != null ? _ground.position.y : float.NegativeInfinity;` 같은 프로퍼티를 추가하고, `InputHandler`가 `[SerializeField] private WallFitter _wallFitter;`(씬에서 `Main Camera` 오브젝트 연결) 또는 `FindFirstObjectByType<WallFitter>()`로 참조. `BallLauncher.LaunchPoint` 프로퍼티(`Transform LaunchPoint => _launchPoint;`)와 동일한 기존 코드 관례를 따르는 방식이라 스타일 일관성이 있다.
- (C) `InputHandler`가 `Awake()`에서 `FindFirstObjectByType<WallFitter>()`(또는 `GameObject.Find("Ground"))`로 런타임에 자동 탐색해 별도의 씬 수동 연결 없이 해결. 씬 설정 누락 위험은 줄지만, 프로젝트의 다른 매니저들(`BallLauncher`, `WaveManager` 등)이 대체로 `[SerializeField]` 명시적 연결 + `SceneSetupEditor.cs`의 `Step*_Connect*Refs` 패턴을 따르고 있어 기존 관례와는 다소 어긋난다.

방향 (A)/(B) 모두 `[SerializeField]` 명시적 연결이 필요하다는 공통점이 있고, (C)는 자동 탐색이라는 점에서 다르다. 어느 쪽이든 `SceneSetupEditor.cs`에 연결 코드를 추가할지, 씬 파일을 직접 수동으로 편집(연결)할지도 함께 결정해야 한다.

**Clamp 적용 위치 후보**:
- (a) `ComputeAimDirection` 내부에서 `worldPos.y`를 `Ground` Y좌표 이상으로 clamp한 뒤(`worldPos.y = Mathf.Max(worldPos.y, groundY);`) 그 다음 `LaunchPoint` 기준으로 방향 벡터를 계산하는 방식. 터치 좌표 자체(목표 지점)를 보정하는 접근이라 구현이 단순하고, 궤적 프리뷰(`TrajectoryPreview.cs`)를 포함해 이후 파이프라인 전체가 항상 "clamp된 목표점 기준 방향"을 그대로 사용하게 되어 부작용이 적어 보인다.
- (b) 방향 벡터(`normalized` 결과)를 먼저 계산한 뒤, 그 벡터가 격자 밑변을 향하는지 각도로 판정해서 사후에 보정하는 방식. 각도 계산과 예외 케이스(예: 완전히 수평/수직 방향, `LaunchPoint`와 `worldPos`의 Y좌표가 같은 경우 등) 처리가 (a)보다 복잡해질 수 있다.

(a)가 더 간단하고 직관적이라는 점만 여기서는 짚어두고, 최종 채택 여부와 세부 clamp 공식(예: `Ground` Y좌표를 그대로 쓸지, 약간의 여유값을 더할지)은 plan.md 단계에서 사용자 확인 후 확정한다.

**기하학적 영향 범위**: `LaunchPoint`가 항상 `Ground`보다 위에 위치한다는 점(현재 상태 항목 참고, `_nativeLaunchPointY = -6.0f` vs `_nativeBottomY = -6.5f`, 둘 다 동일한 `scaleY`가 곱해짐)을 고려하면, "격자 밑변(`Ground`의 Y좌표) 아래로 조준 목표점이 내려가지 못하게" 제한하는 것은 조준 가능 범위를 아주 크게 줄이지는 않는다. 구체적으로:
- `LaunchPoint`에서 수평(좌우, Y좌표 변화 없음) 방향으로는 항상 조준 가능하다 — 수평 방향의 목표점 Y좌표는 `LaunchPoint`와 같은 높이이고, `Ground`보다 항상 위에 있기 때문에 clamp에 걸리지 않는다.
- clamp가 실제로 작동하는 범위는 목표점의 Y좌표가 `Ground`의 Y좌표보다 낮아지려는 경우, 즉 `LaunchPoint`보다 "아래쪽"(화면 하단 방향, 뒤쪽)을 향하려는 조준만 막힌다. `LaunchPoint`와 `Ground`의 Y좌표 차이가 크지 않으므로(`_nativeLaunchPointY - _nativeBottomY = 0.5`, 스케일 적용 후에도 비율 유지), 완전히 수평보다 살짝 아래(거의 수평에 가까운 하향 각도)까지는 여전히 허용되고, 그보다 더 아래(발사 지점 자체보다 확연히 낮은 지점, 즉 사실상 뒤쪽/바닥 쪽)만 차단되는 정도로 제한 폭이 상대적으로 좁다. 다만 좌우 위치에 따라(터치 X좌표가 `LaunchPoint`의 X좌표에서 멀수록) 동일한 Y좌표 제한이라도 각도로 환산했을 때 체감되는 제한 정도는 달라질 수 있다는 점은 참고 사항으로 남긴다(각도 자체를 clamp하는 것이 아니라 Y좌표를 clamp하는 것이므로 좌우 위치에 따라 결과 각도가 달라짐).

## 결론

`InputHandler.ComputeAimDirection`에 Y좌표 하한 제한을 추가하려면, 먼저 `WallFitter._ground`가 private이라 외부에서 직접 접근할 수 없다는 구조적 제약을 해결해야 한다(프로퍼티 추가, 또는 `InputHandler`에 별도 `Ground` Transform 참조 신설 중 택일). `WaveManager._bottomBoundaryY`(하드코딩 `-5`)는 디바이스별로 동기화되지 않으므로 기준점으로 사용하기 부적절하며, `WallFitter`가 런타임에 재계산하는 `Ground`의 실제 Transform 위치를 참조하는 것이 유일하게 타당한 방식이다. clamp는 `ComputeAimDirection` 내부에서 `worldPos.y`를 `Ground` Y좌표 이상으로 고정한 뒤 방향을 계산하는 방식(후보 a)이 방향 벡터 사후 보정 방식(후보 b)보다 단순해 보인다. `LaunchPoint`가 `Ground`보다 항상 위에 있는 구조상, 이 제한은 수평 조준까지는 전혀 방해하지 않고 그보다 더 아래(뒤쪽/바닥 방향)만 차단하는 비교적 좁은 범위의 제약이 될 것으로 예상된다. plan.md 작성 전 사용자 확인이 필요한 열린 질문은 (1) `Ground` 참조를 `InputHandler`에 직접 연결할지, `WallFitter` 프로퍼티를 경유할지, 자동 탐색(`FindFirstObjectByType`)을 쓸지, (2) `SceneSetupEditor.cs`에 연결 코드를 추가할지 여부, (3) `GameplayMechanics.md` 섹션 1에 이번 규칙을 문서화할 시점(구현과 동시에 vs 별도 후속 작업)이다.
