# Plan — 배경 격자 정사각형 보정

이 문서는 research.md에서 정리한 "격자 셀 비정사각형(140×85px, 약 1.647:1) 문제"와 "`BackgroundFitter`/`WallFitter`가 채택 중인 기기별 Stretch(가로/세로 독립 배율) 방식과의 근본적 충돌 가능성"이라는 두 트레이드오프에 대해, 오케스트레이터와 사용자가 논의를 거쳐 확정한 해결 방향("격자 영역 기준 균일 스케일 + 장식 테두리는 여백으로 흡수")을 구체적인 구현 계획으로 정리한 문서다. 리소스(PNG)는 전혀 수정하지 않고, `BackgroundFitter.cs`/`WallFitter.cs`의 스케일 계산식만 교체하는 순수 코드 작업이며, 이번 계획으로 완전히 끝나지 않고 사용자의 로컬 실기기 검증이 후속으로 필요하다는 점까지 포함한다.

## 구현 목표

- 배경 텍스처(`Background_1_Stage.png`) 안의 격자 셀(140×85px, 비정사각형)이 어떤 기기 종횡비에서도 항상 정사각형으로 렌더링되도록, `BackgroundFitter`가 배경 전체에 적용하는 스케일 계산식을 "텍스처 고유 종횡비 보정(고정값) + 격자 영역 기준 균일 배율(기기별 Cover)"의 2단계 계산으로 교체한다.
- `WallFitter`는 `BackgroundFitter`와 반드시 동일한 스케일 계산식을 사용해야 벽-격자 정렬이 유지되므로, 동일한 2단계 계산식을 그대로 이식한다. 단, 계산식이 바뀌면서 벽/발사 위치의 실측 기준값(`_nativeLeftX` 등)이 기존 Stretch 기준으로는 더 이상 맞지 않을 수 있음을 감안해, 재조정 필요 가능성을 명시적으로 남긴다.
- 격자 바깥의 장식 여백(덩쿨/바위 무늬)은 기기 종횡비에 따라 화면 밖으로 잘리거나 남을 수 있음을 허용한다(플레이에 영향 없는 영역이므로 문제 없음).
- 이번 task는 코드 변경까지만 다루며, 실기기에서 최종 수치를 확정하는 검증 과정은 사용자의 후속 작업으로 남긴다.

## 단계별 작업 계획

### 1단계 — 확정된 계산식 정리 (구현에 그대로 반영할 공식)

이번 계산식은 오케스트레이터가 이미 설계를 마쳤고, dev 에이전트는 아래 공식을 그대로 구현하면 된다. 별도의 추가 설계 판단은 필요하지 않다.

**고정 상수(텍스처 실측값, 기기 무관)**

- `_cellAspectCorrection = 140f / 85f ≈ 1.647f` — 텍스처 안 격자 셀의 가로/세로 비율(연구 문서 실측값)
- `_gridAreaWidth = 14.53f` — 격자 영역의 가로 폭(실측 x 범위 약 199~1652px → 1453px ÷ PPU 100)
- `_gridAreaHeight = 10.16f` — 격자 영역의 세로 폭(실측 y 범위 약 540~1556px → 1016px ÷ PPU 100)

이 세 값은 `Background_1_Stage.png` 이 특정 텍스처에 종속된 고정 상수로, `WallFitter`의 `_nativeLeftX` 등과 동일한 성격(실측 기반 하드코딩 값)이다. 기존 관행대로 `[SerializeField]`로 노출해 Inspector에서 미세 조정이 가능하도록 한다.

**계산 공식 (`BackgroundFitter.Apply()` / `WallFitter.Apply()` 공통)**

```csharp
Vector2 camSize = new Vector2(
    _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
    _targetCamera.orthographicSize * 2f);

// 1. 격자 영역이 카메라 뷰포트를 완전히 덮도록 필요한 배율을 각 축별로 계산한다.
//    세로 쪽은 _cellAspectCorrection을 미리 곱해 "정사각형 셀 보정"을 반영한 뒤 계산한다.
float scaleXNeeded = camSize.x / _gridAreaWidth;
float scaleYNeeded = camSize.y / (_gridAreaHeight * _cellAspectCorrection);

// 2. 두 축 중 더 큰 배율(Cover)을 취해 균일 배율(uniformScale)을 정한다 — 이 값 자체는 기기별로 변하지만
//    가로/세로에 동일하게 적용되는 "하나의" 배율이라는 뜻에서 균일하다.
float uniformScale = Mathf.Max(scaleXNeeded, scaleYNeeded) * _zoomFactor;

// 3. 최종 스케일: X축은 uniformScale 그대로, Y축은 여기에 _cellAspectCorrection을 추가로 곱한다.
//    이 고정 배수 차이가 텍스처 자체의 비정사각형 셀을 정사각형으로 보정하는 부분이며, 기기가 바뀌어도 항상 동일한 비율로 유지된다.
float scaleX = uniformScale;
float scaleY = uniformScale * _cellAspectCorrection;
```

이 공식은 다음 두 가지를 동시에 만족하도록 수학적으로 검증됐다.

- **셀 정사각형 보장**: 텍스처 안 셀(1.40×0.85 월드유닛)에 위 `scaleX`/`scaleY`를 각각 적용하면 화면상 셀 크기가 `1.40 × scaleX = 1.40 × uniformScale`, `0.85 × scaleY = 0.85 × uniformScale × 1.647 ≈ 1.40 × uniformScale`로 서로 같아진다. `uniformScale` 값(기기별로 달라짐)과 무관하게 항상 성립하므로, 어떤 기기에서도 셀은 정사각형으로 보인다.
- **격자 영역 Cover 보장**: `uniformScale`이 `scaleXNeeded`/`scaleYNeeded` 중 더 큰 값 이상이므로, 격자 영역의 화면상 가로 폭(`_gridAreaWidth × scaleX`)과 세로 폭(`_gridAreaHeight × scaleY`)이 항상 카메라 뷰포트 크기 이상이 되어(Cover), 격자 영역이 화면 밖으로 잘리는 일이 없다. 그 대신 격자 바깥의 장식 여백은 기기 종횡비에 따라 초과분만큼 잘리거나 남을 수 있다.

### 2단계 — `BackgroundFitter.cs` 계산식 교체

대상 파일: `Assets/_Project/Scripts/Core/BackgroundFitter.cs`

- 기존 `Apply()`의 `scaleX`/`scaleY` 독립 계산 로직(가로/세로 각각 카메라 크기 ÷ 스프라이트 크기 × `_zoomFactor`)을 1단계 공식으로 교체한다.
- 신규 필드 3개(`_cellAspectCorrection = 1.647f`, `_gridAreaWidth = 14.53f`, `_gridAreaHeight = 10.16f`)를 기존 `_spriteRenderer`/`_targetCamera`/`_zoomFactor` 필드와 같은 방식(`[SerializeField] private float`)으로 추가한다.
- 기존 `_spriteRenderer.sprite.bounds.size`(전체 스프라이트 크기) 참조는 새 계산식에서는 더 이상 필요하지 않다. 다만 `_spriteRenderer` 필드 자체는 null 체크 등 다른 용도로 계속 쓰이므로 필드 삭제 등 불필요한 리팩토링은 하지 않는다(외과적 변경 원칙).
- `[ExecuteAlways]`, `Start()`/`OnValidate()` → `Apply()` 위임 구조는 기존 그대로 유지한다(Inspector 실시간 반영 기능은 그대로 보존).

### 3단계 — `WallFitter.cs` 계산식 교체

대상 파일: `Assets/_Project/Scripts/Core/WallFitter.cs`

- 기존 `Apply()`의 `scaleX`/`scaleY` 독립 계산 로직을 `BackgroundFitter`와 완전히 동일한 1단계 공식으로 교체한다(두 스크립트가 계산 로직을 중복 구현하는 기존 패턴 그대로 유지 — 이번에 공용 유틸리티로 리팩토링하지 않는다. 요청 범위를 벗어나는 리팩토링이기 때문).
- `BackgroundFitter`와 동일한 신규 필드 3개(`_cellAspectCorrection`, `_gridAreaWidth`, `_gridAreaHeight`)를 동일한 값으로 추가한다.
- 계산된 `scaleX`/`scaleY`로 기존과 동일하게 `SetX(_wallLeft, ...)`/`SetX(_wallRight, ...)`/`SetY(_wallTop, ...)`/`SetY(_ground, ...)`/`SetY(_launchPoint, ...)`를 호출하는 구조는 그대로 유지한다.
- **중요 — 실측 기준값 재조정 가능성**: 기존 `_nativeLeftX(-6.5)`/`_nativeRightX(6.3)`/`_nativeTopY(6.0)`/`_nativeBottomY(-6.5)`/`_nativeLaunchPointY(-6.0)`는 기존 Stretch 방식(scaleX와 scaleY가 기기별로 서로 다른 임의의 비율)을 전제로 실기기 테스트를 거쳐 확정된 값이다. 새 계산식에서는 `scaleY`가 항상 `scaleX × 1.647`이라는 고정 비율 관계를 갖게 되어 이전과 근본적으로 다른 스케일 패턴이 되므로, 이 5개 값이 새 계산식 아래에서도 그대로 유효하다는 보장이 없다. 이번 task에서는 dev 에이전트가 기존 값을 코드에 그대로 유지한 채 계산식만 교체하고, 실제 값 재조정은 사용자의 로컬 실기기 검증(5단계 참고) 결과에 맡긴다.
- `_backgroundSpriteRenderer` 필드는 새 계산식에서 스케일 계산에 직접 쓰이지 않게 되지만, 이번에도 필드 삭제 등 불필요한 리팩토링은 하지 않는다.
- `[ExecuteAlways]`, `Start()`/`OnValidate()` → `Apply()` 위임 구조는 그대로 유지한다.

### 4단계 — `BackgroundGridFitSetupEditor.cs` 신규 작성 (필드 3개 주입 전용)

대상 파일: `Assets/_Project/Scripts/Editor/BackgroundGridFitSetupEditor.cs` (신규 생성)

- 기존 `SceneSetupEditor.cs`는 이미 여러 Step으로 커져 있어, 이번 신규 필드 3개 주입을 위해 계속 편집하면 작업에 혼선이 생길 수 있다는 사용자 판단에 따라, 기존 `ConnectBackgroundFitterRefs()`/`Step6_SetupWallFitter()`를 수정하지 않고 **완전히 별도의 신규 에디터 스크립트**를 작성한다.
- 이 신규 스크립트는 별도의 `MenuItem`(예: `PurpleCow/Setup/Background Grid Fit Setup`)을 가지며, `SceneSetupEditor.cs`/`MonsterSetupEditor.cs`와 동일한 기존 관행(`GameObject.Find` 또는 `FindObjectOfType` 등으로 씬 오브젝트 탐색 → `SerializedObject`/`FindProperty` 패턴으로 필드 주입 → `ApplyModifiedProperties()`)을 그대로 따른다.
- 씬에서 기존 `BackgroundFitter` 컴포넌트와 `WallFitter` 컴포넌트를 각각 찾아, 신규 필드 3개(`_cellAspectCorrection = 1.647f`, `_gridAreaWidth = 14.53f`, `_gridAreaHeight = 10.16f`)를 두 컴포넌트 모두에 동일한 값으로 주입한다.
- `SceneSetupEditor.cs`는 이번 task에서 **전혀 수정하지 않는다**. 신규 스크립트의 메뉴는 기존 `PurpleCow/Setup/Scene Setup` 메뉴 흐름과 완전히 독립적으로 동작하며, 사용자가 필요할 때 별도로 실행하는 구조다.
- `Step4_PlaceBackground()`/`Step5_PlaceWallsAndGround()` 등 `SceneSetupEditor.cs` 내부의 오브젝트 생성/배치 로직은 이번 계산식 교체와 직접 관련이 없고 이번 task에서 손대지 않으므로 언급하지 않는다. 다만 3단계에서 확인된 대로 `_native*` 값 자체의 재조정이 필요해지면, 그 재조정은 사용자의 로컬 실기기 검증(5단계 참고) 완료 후 별도로 반영해야 한다(어느 스크립트에 반영할지는 그 시점에 별도 판단).

### 5단계 — 사용자 로컬 검증 (이번 task 범위 밖, 후속 필요)

이 저장소가 실행되는 원격 환경에는 Unity 에디터가 없어 아래 항목은 이번 구현 task로 완결되지 않는다. `_Task/2026-07-03/12-30_background-resolution-fix`, `_Task/2026-07-03/12-48_ball-ceiling-wall-fix`의 전례와 동일하게, 사용자가 로컬 Unity 에디터에서 아래를 직접 진행해야 한다.

- 신규 메뉴 `PurpleCow/Setup/Background Grid Fit Setup`(`BackgroundGridFitSetupEditor.cs`)을 별도로 실행해, 새 필드 3개 값이 기존 `SampleScene.unity`의 `Background`/`Main Camera`(WallFitter) 컴포넌트에 반영되도록 한다(코드 변경만으로는 기존 씬에 자동 반영되지 않음). 기존 `PurpleCow/Setup/Scene Setup` 메뉴는 이번에 수정되지 않았으므로, 씬을 새로 구성해야 하는 경우가 아니라면 그대로 두고 이 신규 메뉴만 추가로 실행하면 된다.
- `BackgroundFitter`/`WallFitter`에 이미 있는 `[ExecuteAlways]`/`OnValidate` 실시간 반영 기능을 활용해, 실기기(또는 다양한 해상도 시뮬레이터) 테스트를 반복하며 격자가 실제로 정사각형으로 보이는지, 벽/발사 위치가 격자와 잘 맞는지 육안으로 확인하고 필요 시 `_nativeLeftX` 등 5개 값과 `_zoomFactor`를 재조정한다.
- 최종 확정된 수치는 `ProjectHistory.md` 관행대로 이후 문서에 기록하는 것을 권장한다(이번 plan.md의 범위는 아님).

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Core/BackgroundFitter.cs` (수정 — 계산식 교체, 신규 필드 3개 추가)
- `Assets/_Project/Scripts/Core/WallFitter.cs` (수정 — 계산식 교체, 신규 필드 3개 추가, 기존 `_native*`/`_zoomFactor` 값은 코드상 유지하되 재조정 필요 가능성 주석 등으로 남길 수 있음)
- `Assets/_Project/Scripts/Editor/BackgroundGridFitSetupEditor.cs` (신규 생성 — 신규 필드 3개(`_cellAspectCorrection`, `_gridAreaWidth`, `_gridAreaHeight`) 주입 전용 별도 메뉴 에디터 스크립트)
- `Assets/Scenes/SampleScene.unity` (직접 수정 대상 아님 — 사용자가 로컬에서 신규 `Background Grid Fit Setup` 메뉴 실행 시 간접 반영됨)
- `Assets/_Project/Sprites/Background/Background_1_Stage.png` (변경 없음 — 리소스 자체는 건드리지 않는다는 것이 이번 계획의 전제)

## 주의사항

- 이번 작업은 리소스(PNG) 자체를 수정하지 않는다. 순수 코드(스케일 계산식) 변경이다.
- 기존 `SceneSetupEditor.cs`는 이번 작업에서 수정하지 않으며, 신규 필드 주입은 별도의 새 에디터 스크립트(`BackgroundGridFitSetupEditor.cs`)로 분리한다.
- `BackgroundFitter`/`WallFitter` 두 스크립트가 계산식을 반드시 동일하게(신규 필드 3개 값 포함) 유지해야 벽-배경 정렬이 무너지지 않는다. 값을 나중에 조정할 때도 두 스크립트를 항상 함께 맞춰야 한다.
- 이 작업은 볼 충돌벽(`Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`)과 캐릭터 발사 위치(`LaunchPoint`)의 실제 월드 좌표에 직접 영향을 준다. 계산식이 바뀌면서 `scaleY`가 항상 `scaleX × 1.647`의 고정 비율을 갖게 되므로, 기존 Stretch 기준으로 확정됐던 `_nativeLeftX` 등 5개 값은 새 계산식 아래에서 그대로 맞지 않을 가능성이 높다. 이번 코드 구현 자체는 계산식 교체까지만 다루며, 값 재조정은 사용자의 로컬 실기기 검증을 거쳐 후속으로 확정해야 한다.
- 이 원격 환경에는 Unity 에디터가 없어 신규 `Background Grid Fit Setup` 메뉴 실행, 실기기 빌드 테스트, 육안 확인 등의 검증 자체는 이번 구현 task로 완결되지 않는다. 사용자의 후속 검증이 반드시 필요하다.
- `_zoomFactor`(현재 두 스크립트 모두 1.3)를 새 계산식에서 그대로 유지할지, 다른 방식으로 통합할지는 dev 에이전트가 구현 시 판단하되, 두 스크립트 간 값 일관성은 반드시 유지되어야 한다.
- `BackgroundFitter`/`WallFitter` 두 스크립트 모두 계산 로직이 중복 구현되어 있는 기존 구조를 그대로 유지하며, 이번 작업 범위에서 공용 유틸리티로 리팩토링하지 않는다(외과적 변경/단순함 우선 원칙).
- 이번 plan.md는 사용자의 명시적 승인 후에만 구현(dev 에이전트 위임)을 시작한다.
