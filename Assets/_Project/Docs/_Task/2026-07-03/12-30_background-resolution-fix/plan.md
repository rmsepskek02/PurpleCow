# Plan — 배경/해상도 대응

이 문서는 research.md에서 정리한 원인 분석("Player Settings가 Portrait로 고정되어 있지 않은 점"과 "Background 스프라이트가 카메라 뷰포트에 맞춰 스케일되지 않는 점")을 해결하기 위한 구체적인 구현 계획을 다룬다. 사용자와 이미 방향성 논의가 끝난 내용을 정리한 것이며, 신규 아트 에셋 제작 없이 기존 보유 에셋(`Background_1_Stage.png`)만으로 여백 없이/눈에 띄는 크롭 없이 화면을 채우는 것을 목표로 한다.

## 구현 목표

research.md에서 도출한 "Cover-Fit 스케일 로직 + 카메라 배경색 보정 + Player Settings Portrait 고정" 3가지를 실제로 구현하여, 기존 보유 에셋(`Background_1_Stage.png`)만으로 어떤 Android 기기 종횡비에서도 배경이 여백 없이, 눈에 띄는 크롭 없이 화면을 채우도록 한다. 신규 에셋 제작은 하지 않는다.

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

### 3단계 — SceneSetupEditor.cs 연동

대상 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step4_PlaceBackground()` 메서드 (약 344~368번째 줄)

- Background GameObject 생성 로직은 유지하되, `BackgroundFitter` 컴포넌트를 추가로 부착한다.
- `_spriteRenderer`는 방금 생성한 SpriteRenderer로, `_targetCamera`는 `Camera.main`(씬의 Main Camera)으로 `SerializedObject`를 통해 자동 연결한다. 이는 이 파일의 다른 Step들이 참조 연결에 사용하는 기존 패턴과 동일한 방식(`SerializedObject`/`FindProperty`/`ApplyModifiedPropertiesWithoutUndo` 패턴)을 재사용하는 것이다.
- 이미 Background가 존재해서 스킵되는 기존 케이스(`if (GameObject.Find("Background") != null)`)에도 `BackgroundFitter`가 없으면 추가해주는 보완 로직이 필요하다 (기존 씬에 이미 배치된 Background에도 적용되도록).

### 4단계 — Main Camera 배경색 보정

대상 파일: `Assets/Scenes/SampleScene.unity`

- Main Camera(`fileID 519420031`)의 `m_BackGroundColor`를 현재 `{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}`에서 `{r: 0.05, g: 0.06, b: 0.06, a: 1}`로 직접 수정한다 (research.md에서 실측한 배경 이미지 가장자리 색상과 근접한 값). alpha는 기존 0에서 1로 변경한다 — Solid Color 클리어 플래그에서 알파가 0이면 완전 투명 검정으로 렌더링될 수 있어, 불투명하게 보이도록 1로 설정한다.

## 예상 변경/생성 파일 목록

- `ProjectSettings/ProjectSettings.asset` (수정)
- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` (신규 생성)
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` (수정)
- `Assets/Scenes/SampleScene.unity` (수정)

## 주의사항

- `defaultInterfaceOrientation` 값은 YAML 직접 편집이라 오기입 위험이 있으므로, 적용 후 사용자가 Unity 에디터에서 Player Settings를 열어 Orientation이 실제로 Portrait로 표시되는지 반드시 육안 확인해야 한다.
- Cover-Fit 스케일은 이미지 가장자리의 검정 비네트 영역만 잘려나가는 것을 전제로 하며(픽셀 샘플링 근거는 research.md 참고), 만약 실제 빌드에서 격자 무늬나 뿌리 장식 같은 식별 가능한 콘텐츠가 잘리는 게 육안으로 확인되면 이 방식 대신 다른 접근(예: 9-slice, 여백 색상 채우기)을 재검토해야 한다.
- 이번 계획은 코드/설정 변경만 다루며 신규 아트 에셋 제작은 포함하지 않는다 (사용자가 명시적으로 제외 요청함).
- 이번 plan.md는 사용자의 명시적 승인 전에는 구현(dev 에이전트 위임)으로 이어지지 않는다.
