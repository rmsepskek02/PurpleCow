# Plan — Ball Ceiling Wall Fix

research.md에서 확정한 루트 원인(`SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단(천장) 벽 콜라이더 생성 코드 자체가 없음)을 해소하기 위한 구현 계획입니다. `Wall_Top` 콜라이더를 좌/우/아래와 동일한 방식으로 추가하는 코드 변경과, 그 변경을 실제 커밋된 `Assets/Scenes/SampleScene.unity`에 반영하는 방법(에디터 재실행 vs 씬 파일 직접 편집)을 다룹니다. 이 문서는 계획 단계이며, TaskRules.md 규칙에 따라 사용자의 명시적 승인 전까지 실제 코드/씬 파일 수정은 진행하지 않습니다.

## 구현 목표

- `SceneSetupEditor.Step5_PlaceWallsAndGround()`에 `Wall_Top`(천장) 콜라이더 생성 로직을 추가해, 좌/우/아래와 마찬가지로 위쪽도 항상 막히도록 한다.
- 좌우 벽과 천장 벽 사이에 볼이 새어나갈 수 있는 틈(갭)이 없도록 좌표/크기를 정한다.
- 실제 플레이 씬(`Assets/Scenes/SampleScene.unity`)에도 `Wall_Top` 오브젝트가 반영되어, 코드 수정만으로 끝나지 않고 실제 플레이 테스트에서 효과를 확인할 수 있는 상태로 만든다.

## 단계별 작업 계획

### 1단계 — `SceneSetupEditor.cs` 코드 수정

- `Step5_PlaceWallsAndGround()`(`Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` 374~379번 줄)에 아래 한 줄을 추가한다.

```csharp
PlaceColliderObject("Wall_Top", "Wall", new Vector3(0f, 8f, 0f), new Vector2(12f, 0.2f));
```

- 좌표/크기 근거:
  - y = 8: `AIFailures.md`에 문서화된 실제 플레이 영역(`x: ±5.5, y: -10 ~ +8`)의 상단 값을 그대로 사용한다.
  - size = (12, 0.2): `Ground`(`new Vector2(12f, 0.2f)`)와 동일한 크기를 재사용해 좌우 벽과의 형태 일관성을 유지한다.
  - 태그 = `"Wall"`: 좌우 벽과 동일한 태그를 부여한다. `Ball.OnCollisionEnter2D`가 `"Wall"` 태그를 그대로 반사 처리하는 구조이므로, `Ball.cs`나 다른 스크립트를 별도로 수정할 필요가 없다.
- `PlaceColliderObject`는 이미 `GameObject.Find(objName) != null`이면 스킵하는 멱등성 로직을 갖고 있으므로, 기존 `Wall_Left`/`Wall_Right`/`Ground`는 재실행해도 영향을 받지 않고 신규 `Wall_Top`만 생성된다.

### 2단계 — 좌우 벽과 천장 벽 사이 갭(틈) 여부 사전 계산

좌표 기준으로 두 콜라이더가 실제로 겹치는지 미리 계산해 갭이 없는지 확인한다.

- `Wall_Left`: 위치 x=-5.5, size(0.2, 20) → x 범위 -5.6 ~ -5.4, y 범위 -10 ~ +10
- `Wall_Right`: 위치 x=5.5, size(0.2, 20) → x 범위 5.4 ~ 5.6, y 범위 -10 ~ +10
- `Wall_Top`(제안): 위치 y=8, size(12, 0.2) → x 범위 -6 ~ +6, y 범위 7.9 ~ 8.1

좌우 벽의 x 범위(-5.6~-5.4, 5.4~5.6)가 모두 `Wall_Top`의 x 범위(-6~6) 안에 포함되고, `Wall_Top`의 y 범위(7.9~8.1)도 좌우 벽의 y 범위(-10~10) 안에 포함되므로 좌표 계산상으로는 네 모서리 부근에서 두 콜라이더가 서로 겹치는 영역이 존재하며 물리적인 빈틈은 없는 것으로 판단된다. 다만 이는 좌표값 계산에 근거한 사전 판단이며, 3단계 반영 이후 4단계 QA에서 실제 물리 동작으로 재확인한다.

### 3단계 — 실제 씬(`SampleScene.unity`) 반영

1단계 코드 수정만으로는 이미 커밋되어 있는 `Assets/Scenes/SampleScene.unity`에 `Wall_Top`이 자동으로 추가되지 않는다. `SceneSetupEditor.cs`는 Unity 에디터 메뉴(`PurpleCow/Setup/Scene Setup`)를 실제로 실행해야만 씬 파일에 변경이 반영되는 구조이기 때문이다. 이를 반영하는 방법은 아래 두 옵션 중 하나를 선택해야 하며, 최종 선택은 사용자 확인이 필요하다(자세한 내용은 "주의사항" 참고).

- **옵션 A (권장) — 사용자가 로컬 Unity 에디터에서 메뉴 재실행**
  - 1단계 코드 수정을 커밋한 뒤, 사용자가 로컬 Unity 에디터를 열어 `PurpleCow/Setup/Scene Setup` 메뉴(또는 `Step5`에 해당하는 부분)를 재실행한다.
  - 이 메뉴는 이미 `EditorSceneManager.SaveScene()`까지 자동 호출하는 구조이므로, 메뉴 재실행만으로 `Wall_Top` 생성과 씬 저장이 한 번에 끝난다.
  - `PlaceColliderObject`의 멱등성 로직 덕분에 기존 `Wall_Left`/`Wall_Right`/`Ground`는 재실행해도 중복 생성되지 않는다.
- **옵션 B — dev 에이전트가 `SampleScene.unity` YAML을 직접 텍스트 편집**
  - Unity 에디터 없이 즉시 반영 가능하지만, 기존 `Wall_Left`/`Wall_Right` 블록과 동일한 구조(GameObject + Transform + BoxCollider2D, `Wall` 태그 참조 포함)로 `Wall_Top` 블록을 수동 작성해야 한다.
  - Unity YAML의 `fileID` 고유성, `m_Component` 참조 관계 등 형식을 정확히 지켜야 하는 위험이 있어 실수 시 씬 파일이 깨질 가능성이 있다.

### 4단계 — QA 검증

코드/씬 반영이 끝난 뒤, qa 에이전트(또는 실제 플레이 테스트)를 통해 아래 항목을 확인한다.

- 볼을 위쪽 방향으로 발사했을 때 상단(`Wall_Top`)에서 정상적으로 반사되는지, 화면 밖으로 유실되지 않는지.
- 좌우 벽(`Wall_Left`/`Wall_Right`, y: -10~+10)과 천장 벽(`Wall_Top`, y: 7.9~8.1) 경계 부근(2단계에서 계산한 겹침 구간)으로 볼을 여러 각도에서 발사해, 실제로 그 틈으로 새어나가는 사례가 없는지 반복 확인.
- 로스터 소속 볼이 상단 반사 이후에도 기존과 동일하게 정상적으로 비행을 이어가는지(귀환/재발사 사이클 자체는 `"Ground"` 충돌에서만 트리거되므로 이번 변경과 무관하나, 회귀 여부를 함께 확인).

## 예상 변경/생성 파일 목록

| 파일 경로 | 변경 내용 |
|---|---|
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | `Step5_PlaceWallsAndGround()`에 `Wall_Top` 콜라이더 생성 호출 1줄 추가 (`PlaceColliderObject("Wall_Top", "Wall", new Vector3(0f, 8f, 0f), new Vector2(12f, 0.2f))`) |
| `Assets/Scenes/SampleScene.unity` | `Wall_Top` GameObject(Transform + BoxCollider2D, `Wall` 태그) 신규 추가 — 옵션 A(에디터 메뉴 재실행) 또는 옵션 B(YAML 직접 편집) 중 선택된 방식으로 반영 |

## 주의사항

- **씬 반영 방식은 사용자 확인 필요**: 옵션 A(로컬 Unity 에디터에서 `PurpleCow/Setup/Scene Setup` 메뉴 재실행)와 옵션 B(`SampleScene.unity` YAML 직접 편집) 중 어느 쪽으로 진행할지 구현 전 사용자에게 확인한다. 옵션 A는 Unity 에디터가 이미 검증된 방식으로 씬을 생성/저장하므로 안전하지만 사용자의 로컬 작업이 필요하고, 옵션 B는 dev 에이전트가 즉시 처리할 수 있으나 YAML 형식을 손으로 정확히 맞춰야 하는 위험이 있다. 두 옵션 모두 `SceneSetupEditor.cs` 코드 수정(1단계)은 공통으로 필요하다 — 이는 향후 프로젝트를 처음부터 새로 세팅하거나 씬을 재생성할 때도 천장 벽이 항상 생성되도록 보장하기 위함이다.
- **이번 작업 범위 제외**: `BallBounce.physicsMaterial2D`가 Wall/Ground 콜라이더에 미연결인 문제, `CollisionDetectionMode2D.Continuous` 관련 터널링 가능성은 research.md에서 함께 발견되었으나 이번 task 범위에서는 다루지 않는다. 추후 별도 버그가 발견되면 그때 별도 task로 재점검한다.
- **좌표 계산은 이론상 검증**: 2단계의 좌표 겹침 계산은 콜라이더 크기/위치 수치에 근거한 사전 판단이며, Unity 2D 물리 엔진의 실제 충돌 판정(특히 모서리 부근 고속 이동 시)까지 보장하지는 않는다. 4단계 QA에서 실제 플레이로 재확인이 필요하다.
- **plan.md 작성만 완료된 상태**: TaskRules.md 규칙에 따라 이 plan.md는 계획 문서일 뿐이며, 사용자의 명시적인 승인 전까지 `SceneSetupEditor.cs`나 `SampleScene.unity`에 대한 실제 수정은 진행하지 않는다.
