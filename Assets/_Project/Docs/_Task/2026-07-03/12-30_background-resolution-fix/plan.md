# Plan — 배경/해상도 대응

이 문서는 research.md에서 정리한 원인 분석("Player Settings가 Portrait로 고정되어 있지 않은 점", "Background 스프라이트가 카메라 뷰포트에 맞춰 스케일되지 않는 점", "카메라 시야가 표준 세로 비율(1080x1920)에만 맞춰 하드코딩되어 있어 더 좁고 긴 기기에서 Wall이 화면 밖으로 잘리는 점")을 해결하기 위한 구체적인 구현 계획을 다룬다. 사용자와 이미 방향성 논의가 끝난 내용을 정리한 것이며, 신규 아트 에셋 제작 없이 기존 보유 에셋(`Background_1_Stage.png`)만으로 여백 없이/눈에 띄는 크롭 없이 화면을 채우는 것과, 다양한 기기 종횡비에서 Wall/플레이 영역이 잘리지 않는 것을 목표로 한다. 이후 Wall 좌표가 배경 이미지 속 격자 그림과 애초에 일치하지 않았다는 사실이 추가로 밝혀지면서, 카메라 시야를 동적으로 조정하던 방식(`CameraFitter`)은 폐기하고 Wall 좌표를 배경 배율에 비례해 재배치하는 방식(`WallFitter`)으로 최종 설계가 갱신됐다. 이 문서는 이 최종 확정 설계까지 반영한 구현 계획을 다룬다.

## 구현 목표

research.md에서 도출한 "Cover-Fit 스케일 로직 + 카메라 배경색 보정 + Player Settings Portrait 고정 + 카메라 시야 동적 확장(CameraFitter)" 4가지를 실제로 구현하여, 기존 보유 에셋(`Background_1_Stage.png`)만으로 어떤 Android 기기 종횡비에서도 배경이 여백 없이, 눈에 띄는 크롭 없이 화면을 채우고, Wall을 비롯한 플레이 영역이 화면 좌우로 잘리지 않도록 한다. 신규 에셋 제작은 하지 않는다.

**최종 확정 목표 (갱신)** — 이후 research.md에서 추가로 정리한 "Wall 좌표가 배경 격자 그림과 애초에 일치하지 않는 문제"가 반영되면서, 카메라 시야 동적 확장(`CameraFitter`)은 폐기되고 대신 `WallFitter`가 Wall/Ground 좌표를 배경 배율에 비례해 재배치하는 방식으로 대체됐다. 최종 목표는 배경이 Stretch 방식으로 기기마다 다르게 늘어나는 동안 Wall/Ground가 항상 배경 그림 속 격자 경계와 일치하도록 함께 비례 이동하는 것이며, Main Camera의 `orthographic size`는 원래 설계값인 10으로 고정한다.

## 단계별 작업 계획

### 1단계 — Player Settings Portrait 고정

대상 파일: `ProjectSettings/ProjectSettings.asset`

- `defaultInterfaceOrientation` 필드를 `1`(Portrait)로 설정한다. 현재 이 필드 자체가 파일에 없으므로 새로 추가해야 한다. Unity의 UIOrientation 직렬화 값은 0=Auto Rotation, 1=Portrait, 2=Portrait Upside Down, 3=Landscape Right, 4=Landscape Left로 알려져 있다. dev 에이전트는 이 값을 적용한 뒤 반드시 Unity 에디터에서 Player Settings > Resolution and Presentation > Orientation이 실제로 "Portrait"로 표시되는지 사용자에게 확인을 요청해야 한다 (YAML 직접 편집이라 오기입 위험이 있기 때문).
- `allowedAutorotateToPortrait: 1`은 유지하고, `allowedAutorotateToPortraitUpsideDown`, `allowedAutorotateToLandscapeRight`, `allowedAutorotateToLandscapeLeft`는 전부 `0`으로 변경한다 (세로 단일 방향 고정).
- `defaultScreenWidth: 1080`, `defaultScreenHeight: 1920`으로 변경한다 (기존 1920x1080 가로값에서 세로값으로).
- `androidDefaultWindowWidth: 1080`, `androidDefaultWindowHeight: 1920`으로 변경한다.

### 2단계 — Background Cover-Fit 스케일 스크립트 신규 작성

신규 파일: `Assets/_Project/Scripts/Core/BackgroundFitter.cs`

- MonoBehaviour로 작성하며, DevRules.md 네이밍 컨벤션을 준수한다 (`_camelCase` private 필드, `[SerializeField]` 사용).
- 필드: `[SerializeField] private SpriteRenderer _spriteRenderer`, `[SerializeField] private Camera _targetCamera`.
- `Awake()` 또는 `Start()`에서 1회만 계산한다. DevRules 성능 규칙(매 프레임 갱신 금지 원칙)에 따라 반복 계산은 불필요하며, Portrait 고정 후에는 런타임 중 화면 비율이 바뀌지 않으므로 1회 계산으로 충분하다.
- 계산 로직 (research.md 결론 인용):

  ```csharp
  Vector2 camSize = new Vector2(
      _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
      _targetCamera.orthographicSize * 2f);
  Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
  float scale = Mathf.Max(camSize.x / spriteSize.x, camSize.y / spriteSize.y);
  transform.localScale = new Vector3(scale, scale, 1f);
  ```

- `_spriteRenderer`, `_targetCamera`가 null이면 아무 동작도 하지 않도록 Null 방어 처리를 한다.

**추가 보강 (5단계 CameraFitter 도입에 따른 실행 순서 조정 필요)** — `BackgroundFitter.cs`는 이미 위 로직대로 구현되어 있으나, 현재 계산 시점이 `Awake()`로 되어 있다. 5단계에서 신규 도입하는 `CameraFitter`가 카메라의 `orthographicSize`를 먼저 확정한 뒤에 `BackgroundFitter`가 그 최종 크기를 읽어서 배경을 스케일해야 하므로, `BackgroundFitter`의 계산 시점을 `Awake()`에서 `Start()`로 변경해야 한다. Unity는 씬의 모든 오브젝트의 `Awake()`가 전부 끝난 뒤에 `Start()`가 호출되는 것을 보장하므로, `CameraFitter`는 `Awake()`를 유지하고 `BackgroundFitter`만 `Start()`로 옮기면 별도의 Script Execution Order 프로젝트 설정 없이 순서가 보장된다.

### 3단계 — SceneSetupEditor.cs 연동

대상 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step4_PlaceBackground()` 메서드 (약 344~368번째 줄)

- Background GameObject 생성 로직은 유지하되, `BackgroundFitter` 컴포넌트를 추가로 부착한다.
- `_spriteRenderer`는 방금 생성한 SpriteRenderer로, `_targetCamera`는 `Camera.main`(씬의 Main Camera)으로 `SerializedObject`를 통해 자동 연결한다. 이는 이 파일의 다른 Step들이 참조 연결에 사용하는 기존 패턴과 동일한 방식(`SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo` 패턴)을 재사용하는 것이다.
- 이미 Background가 존재해서 스킵되는 기존 케이스(`if (GameObject.Find("Background") != null)`)에도 `BackgroundFitter`가 없으면 추가해주는 보완 로직이 필요하다 (기존 씬에 이미 배치된 Background에도 적용되도록).

### 4단계 — Main Camera 배경색 보정

대상 파일: `Assets/Scenes/SampleScene.unity`

- Main Camera(`fileID 519420031`)의 `m_BackGroundColor`를 현재 `{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}`에서 `{r: 0.05, g: 0.06, b: 0.06, a: 1}`로 직접 수정한다 (research.md에서 실측한 배경 이미지 가장자리 색상과 근접한 값). alpha는 기존 0에서 1로 변경한다 — Solid Color 클리어 플래그에서 알파가 0이면 완전 투명 검정으로 렌더링될 수 있어, 불투명하게 보이도록 1로 설정한다.

### 5단계 — CameraFitter 신규 작성 및 연동 (폐기됨)

**이 단계는 폐기됐다.** research.md "이후 논의로 도출된 최종 결론"에서 정리한 대로, Wall 좌표를 "실측값 × 배경 배율"로 계산하면 Wall이 화면에서 차지하는 상대적 비율이 `orthographic size`와 무관하게 항상 일정함이 수학적으로 도출됐다(가로 약 0.29, 세로 약 0.54, 항상 화면 안). 따라서 카메라 시야를 기기별로 동적으로 넓히는 이 스크립트는 더 이상 필요 없으며, `orthographic size`는 원래 설계값 10으로 고정 유지한다. 아래 내용은 폐기 전 시행착오 기록으로 남겨두고, 실제 구현은 이 단계 대신 아래 "6단계 — WallFitter 신규 작성 및 연동"을 따른다.

신규 파일(폐기): `Assets/_Project/Scripts/Core/CameraFitter.cs`

- MonoBehaviour로 작성하며, DevRules.md 네이밍 컨벤션을 준수한다 (`_camelCase` private 필드, `[SerializeField]` 사용).
- 필드: `[SerializeField] private Camera _targetCamera`, `[SerializeField] private float _baseOrthographicSize = 10f`(기존 세로 기준값), `[SerializeField] private float _requiredHalfWidth = 5.6f`(Wall 바깥쪽 끝 기준 필요 가로 반폭).
- `Awake()`에서 1회만 계산한다.

  ```csharp
  float requiredSize = _requiredHalfWidth / _targetCamera.aspect;
  _targetCamera.orthographicSize = Mathf.Max(_baseOrthographicSize, requiredSize);
  ```

- 표준 비율(aspect ≥ 0.56)에서는 기존과 동일하게 10을 유지하고, 더 좁고 긴 기기에서는 자동으로 더 큰 값(줌아웃)으로 조정되어 Wall까지 항상 화면에 들어오도록 보장한다.
- 세로로 추가 확보되는 여유 공간은 배경(BackgroundFitter의 Cover-Fit)이 함께 커버하므로 별도 부작용이 없다.
- `_targetCamera`가 null이면 아무 동작도 하지 않도록 Null 방어 처리를 한다.

대상 파일(연동): `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`

- Main Camera에 `CameraFitter` 컴포넌트를 부착하고 `_targetCamera`(자기 자신), `_baseOrthographicSize=10`, `_requiredHalfWidth=5.6` 참조/값을 연결하는 로직을 추가한다. Main Camera GameObject 자체는 SceneSetupEditor가 생성하는 것이 아니라 씬에 이미 있는 기본 오브젝트이므로, `Camera.main`으로 찾아서 연결하는 방식을 사용한다.
- 연결 방식은 `ConnectBackgroundFitterRefs()`(364~379번째 줄 근방)와 동일한 패턴(`SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo`)을 재사용한다.
- `Step5_PlaceWallsAndGround()`(385~390번째 줄) 직후에 호출되는 신규 Step(예: `Step6_SetupCameraFitter()`)으로 분리한다. Wall 배치 이후 위치이므로 `_requiredHalfWidth` 값과 실제 Wall 배치 좌표가 맞물려 있음을 코드 흐름상으로도 자연스럽게 표현할 수 있다.
- 이미 `CameraFitter`가 부착되어 있으면 재부착하지 않고 참조만 갱신하는 방어 로직을 둔다 (기존 Step들의 스킵 패턴과 동일).

### 6단계 — WallFitter 신규 작성 및 연동 (최종 확정 설계)

research.md "이후 논의로 도출된 최종 결론"에서 정리한 최종 설계다. 5단계(CameraFitter)를 대체한다.

신규 파일: `Assets/_Project/Scripts/Core/WallFitter.cs`

- MonoBehaviour로 작성하며, DevRules.md 네이밍 컨벤션을 준수한다 (`_camelCase` private 필드, `[SerializeField]` 사용).
- 필드:
  - `[SerializeField] private Camera _targetCamera`
  - `[SerializeField] private SpriteRenderer _backgroundSpriteRenderer`
  - `[SerializeField] private Transform _wallLeft`
  - `[SerializeField] private Transform _wallRight`
  - `[SerializeField] private Transform _wallTop`
  - `[SerializeField] private Transform _ground`
  - `[SerializeField] private float _nativeLeftX = -6.04f`
  - `[SerializeField] private float _nativeRightX = 5.89f`
  - `[SerializeField] private float _nativeTopY = 5.55f`
  - `[SerializeField] private float _nativeBottomY = -5.33f`
  - `_native*` 4개 값은 research.md "배경 이미지 격자 경계 실측" 결과(배경 배율 1배 기준 격자 그림 경계)를 그대로 사용한다.
- `Start()`에서 1회만 계산한다. `BackgroundFitter`도 `Start()`에서 계산하지만, `WallFitter`는 `BackgroundFitter`의 계산 결과(예: `transform.localScale`)를 읽지 않고 카메라/스프라이트 원본 크기로부터 스케일을 자체적으로 다시 계산하므로 두 스크립트 간 실행 순서에 의존하지 않는다.

  ```csharp
  if (_targetCamera == null || _backgroundSpriteRenderer == null) return;

  Vector2 camSize = new Vector2(
      _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
      _targetCamera.orthographicSize * 2f);
  Vector2 spriteSize = _backgroundSpriteRenderer.sprite.bounds.size;
  float scaleX = camSize.x / spriteSize.x;
  float scaleY = camSize.y / spriteSize.y;

  SetX(_wallLeft, _nativeLeftX * scaleX);
  SetX(_wallRight, _nativeRightX * scaleX);
  SetY(_wallTop, _nativeTopY * scaleY);
  SetY(_ground, _nativeBottomY * scaleY);
  ```

  `SetX`/`SetY`는 각 Transform의 position.x 또는 position.y만 바꾸고 나머지 축(및 z)은 그대로 유지하는 private 헬퍼 메서드로 구현한다. 예:

  ```csharp
  private static void SetX(Transform t, float x)
  {
      if (t == null) return;
      Vector3 p = t.position;
      p.x = x;
      t.position = p;
  }

  private static void SetY(Transform t, float y)
  {
      if (t == null) return;
      Vector3 p = t.position;
      p.y = y;
      t.position = p;
  }
  ```

- `_wallLeft`/`_wallRight`/`_wallTop`/`_ground` 중 일부가 null이어도 나머지 축은 정상 계산되도록, `SetX`/`SetY` 내부에서 각 Transform이 null이면 해당 호출만 조용히 스킵한다 (위 헬퍼 구현에 이미 반영됨).

대상 파일(연동): `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`

- 기존 `Step6_SetupCameraFitter()`(CameraFitter 부착 로직, 417~437번째 줄 근방)를 제거하고, 같은 호출 위치(`Step5_PlaceWallsAndGround()` 직후)에 `Step6_SetupWallFitter()`로 대체한다.
- Main Camera에 `WallFitter` 컴포넌트를 부착한다 (이미 있으면 재부착하지 않고 참조만 갱신 — 기존 Step들의 스킵 패턴과 동일).
- 참조 연결:
  - `_targetCamera` = `Camera.main` (자기 자신)
  - `_backgroundSpriteRenderer` = `GameObject.Find("Background")`로 찾은 오브젝트의 `SpriteRenderer`
  - `_wallLeft` = `GameObject.Find("Wall_Left")`의 Transform
  - `_wallRight` = `GameObject.Find("Wall_Right")`의 Transform
  - `_wallTop` = `GameObject.Find("Wall_Top")`의 Transform
  - `_ground` = `GameObject.Find("Ground")`의 Transform
  - `_nativeLeftX = -6.04f`, `_nativeRightX = 5.89f`, `_nativeTopY = 5.55f`, `_nativeBottomY = -5.33f`
- 연결 방식은 `ConnectBackgroundFitterRefs()`(370~380번째 줄 근방)와 동일한 패턴(`SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo`)을 재사용한다.

## 예상 변경/생성 파일 목록

- `ProjectSettings/ProjectSettings.asset` (수정)
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` (변경 없음 — 최종 확정 설계에서 코드 변경 없이 그대로 유지하기로 함. Stretch 방식으로 이미 구현되어 있음)
- `Assets/_Project/Scripts/Core/CameraFitter.cs` (삭제 — 및 `.meta`도 함께 삭제. 최종 확정 설계에서 수학적으로 불필요함이 밝혀짐)
- `Assets/_Project/Scripts/Core/WallFitter.cs` (신규 생성)
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` (수정 — `CameraFitter` 연동 코드 제거, `WallFitter` 연동 코드 추가)
- `Assets/Scenes/SampleScene.unity` (수정 — Main Camera에서 `CameraFitter` 컴포넌트 제거, `orthographic size` 10 확인/유지)

## 주의사항

- `defaultInterfaceOrientation` 값은 YAML 직접 편집이라 오기입 위험이 있으므로, 적용 후 사용자가 Unity 에디터에서 Player Settings를 열어 Orientation이 실제로 Portrait로 표시되는지 반드시 육안 확인해야 한다.
- Cover-Fit 스케일은 이미지 가장자리의 검정 비네트 영역만 잘려나가는 것을 전제로 하며(픽셀 샘플링 근거는 research.md 참고), 만약 실제 빌드에서 격자 무늬나 뿌리 장식 같은 식별 가능한 콘텐츠가 잘리는 게 육안으로 확인되면 이 방식 대신 다른 접근(예: 9-slice, 여백 색상 채우기)을 재검토해야 한다.
- (폐기됨) CameraFitter가 BackgroundFitter보다 먼저 실행되어야 한다는 주의사항은 CameraFitter 자체가 폐기되면서 더 이상 적용되지 않는다. 최종 확정 설계에서는 `WallFitter`가 `BackgroundFitter`와 실행 순서 의존성이 전혀 없다 — 두 스크립트가 각자 독립적으로 동일한 원천 데이터(카메라 크기, 배경 스프라이트 원본 크기)에서 스케일을 다시 계산하기 때문이다. 이 점을 명시해서 향후 실행 순서 관련 혼동을 방지한다.
- 레퍼런스 이미지와의 정밀 비율 비교는 보류했으므로(research.md "참고 — 레퍼런스 이미지 비율과의 정밀 비교는 보류" 참고), 구현 후 실제 빌드에서 육안으로 레퍼런스와 비교하며 `WallFitter`의 `_nativeLeftX`/`_nativeRightX`/`_nativeTopY`/`_nativeBottomY` 4개 실측값을 미세 조정할 수 있다.
- 이번 계획은 코드/설정 변경만 다루며 신규 아트 에셋 제작은 포함하지 않는다 (사용자가 명시적으로 제외 요청함).
- 이번 plan.md는 사용자의 명시적 승인 전에는 구현(dev 에이전트 위임)으로 이어지지 않는다.
