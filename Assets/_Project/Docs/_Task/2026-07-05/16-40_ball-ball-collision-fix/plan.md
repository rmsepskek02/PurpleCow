# Plan — 볼-볼 물리 충돌 버그 수정

research.md에서 확인된 원인(모든 Ball이 Default 레이어에 있고 `Physics2DSettings`의 레이어 충돌 매트릭스가 Default-Default 충돌을 허용해 볼끼리 물리적으로 튕겨나감)을 해결하기 위해, 전용 "Ball" Physics2D 레이어를 신설하고 `Physics2D.IgnoreLayerCollision`로 볼-볼 충돌만 전역적으로 비활성화하는 방식(연구 문서의 해결 방식 (a))을 채택한다. Wall/Ground/Monster는 기존대로 Default 레이어에 남기며, "Ball" 태그 미등록 버그는 이번 범위에서 다루지 않는다.

## 구현 목표

- 전용 Physics2D 레이어("Ball") 신설
- `Ball.prefab`을 이 레이어로 배치
- 게임 시작 시 1회 `Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true)` 호출로 볼-볼 물리 충돌을 전역적으로 비활성화
- Wall/Ground/Monster는 계속 Default 레이어에 남아있어도 무방하다. Unity는 새 레이어를 추가해도 다른 기존 레이어와의 충돌 비트를 기본적으로 켠 채로 초기화하므로, Ball-Default(Wall/Ground/Monster) 충돌은 별도 조치 없이 그대로 유지되어 볼이 벽/바닥/몬스터와는 정상적으로 계속 충돌한다(주의사항에도 재명시)
- "Ball" 태그 미등록 문제는 이번 범위에서 고치지 않고 그대로 둔다(사용자 확정 사항)

## 단계별 작업 계획

1. **레이어 등록 — 에디터 스크립트로 `TagManager.asset` 수정**
   - 이 원격 환경에는 Unity 에디터가 없어 Project Settings > Tags & Layers GUI를 직접 쓸 수 없으므로, 기존 프로젝트가 태그 등록에 써온 패턴(`SerializedObject`로 `ProjectSettings/TagManager.asset`을 열어 `FindProperty`로 값을 채우는 방식 — `BallSetupEditor.AddRequiredTags()`, `MonsterSetupEditor.EnsureMonsterTag()` 참고)과 유사하게, 레이어 배열(`SerializedProperty` 이름은 `"layers"`, 인덱스 8~31이 커스텀 슬롯)에 `"Ball"`을 등록하는 에디터 코드를 신설한다.
   - 위치는 dev 에이전트 재량으로 정하되, 기존 `BallSetupEditor.cs`에 메서드를 추가하는 것이 자연스럽다(볼 관련 셋업이 이미 여기 모여있음). 신규 별도 파일로 분리해도 무방하다.
   - 현재 `TagManager.asset` 확인 결과 인덱스 8(파일 내 `layers:` 하위 9번째 항목)부터 전부 빈 문자열이므로, 비어있는 첫 슬롯을 사용한다. 단, 이미 값이 차있는 슬롯은 건드리지 않도록 방어 코드를 넣는다(멱등성 — 이미 "Ball"이 등록되어 있으면 스킵. `UnityEditorInternal.InternalEditorUtility.layers` 또는 배열 순회로 기존 등록 여부를 먼저 확인).
   - 기존 `BallSetupEditor.SetupBallSystem()`의 `[MenuItem("PurpleCow/Setup/Ball System Setup")]` 흐름에 이 레이어 등록 호출을 포함시켜, 사용자가 로컬 Unity에서 기존 메뉴를 한 번 더 실행하는 것만으로 처리되게 하는 방안을 권장한다(정확한 통합 방식은 dev 재량).

2. **`Ball.prefab`의 GameObject `m_Layer`를 새 레이어로 변경**
   - 위 1번에서 등록한 레이어 인덱스로 `Assets/_Project/Prefabs/Ball/Ball.prefab`(및 향후 씬에 배치되는 Ball 인스턴스 — 프리팹 기반이라 자동 상속됨)의 `m_Layer`를 설정하는 에디터 코드를 추가한다(`AssetDatabase.LoadAssetAtPath` + `GameObject.layer` 대입 후 `PrefabUtility`/`AssetDatabase.SaveAssets` 패턴, 기존 `MonsterSetupEditor.ConnectMonsterDataToPrefabs()` 류의 프리팹 편집 패턴 참고 가능).
   - 하드코딩된 레이어 인덱스(8) 대신 `LayerMask.NameToLayer("Ball")`로 조회해서 사용할 것을 권장한다(슬롯 번호가 바뀌어도 안전). 이 호출은 반드시 1번 단계에서 레이어가 `TagManager.asset`에 실제로 저장(`ApplyModifiedProperties` + `AssetDatabase.SaveAssets`)된 이후에 실행되어야 정상적으로 값을 반환한다(정확한 실행 순서 보장은 dev 재량, 예: 같은 메뉴 안에서 순차 호출하되 필요 시 `AssetDatabase.SaveAssets()`를 레이어 등록 직후 먼저 호출).

3. **런타임에 `Physics2D.IgnoreLayerCollision` 1회 호출**
   - `Assets/_Project/Scripts/Ball/BallLauncher.cs`(볼 시스템의 진입점 성격, `Awake()`에서 오브젝트 풀도 생성하는 곳)의 `Awake()`에 다음과 같은 형태의 코드를 추가한다(정확한 구현은 dev 재량):
     ```csharp
     int ballLayer = LayerMask.NameToLayer("Ball");
     Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true);
     ```
   - 정확한 위치(`BallLauncher.Awake()` vs 다른 매니저)는 dev 재량이나, 볼 시스템 초기화와 직접 관련된 곳에 두는 것을 권장한다.
   - 이 호출은 게임 시작 시 딱 1회만 필요하다(레이어 간 충돌 매트릭스 자체를 런타임에 전역으로 바꾸는 것이므로 매 프레임/매 스폰마다 반복 호출할 필요가 없다). `ObjectPool<Ball>`이 이후 동적으로 새 볼 인스턴스를 생성하더라도, 프리팹의 `m_Layer`(2번 단계에서 "Ball"로 설정됨)를 그대로 상속하므로 별도 처리가 필요 없다.

4. **`GameplayMechanics.md`/`UIRules.md`/`MonsterRules.md` 등 문서 갱신 여부**
   - 이번 변경은 순수 물리/버그 수정이며 게임플레이 스펙 자체(볼 발사/궤도, 몬스터, UI)에 새로운 규칙을 추가하는 것이 아니므로, 기존 스펙 문서들을 갱신할 필요는 없다고 판단된다. 만약 dev 에이전트가 구현 중 특정 문서의 서술이 이번 변경과 명백히 모순된다고 판단하면, 문서를 임의로 수정하지 말고 별도로 보고한다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` (수정, 또는 신규 파일 — dev 재량) — "Ball" 레이어 등록 + `Ball.prefab` 레이어 할당 로직 추가
- `Assets/_Project/Prefabs/Ball/Ball.prefab` (수정) — `m_Layer` 값을 신설된 "Ball" 레이어 인덱스로 변경
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` (수정) — `Awake()`에 `Physics2D.IgnoreLayerCollision` 1회 호출 추가
- `ProjectSettings/TagManager.asset` — 에디터 스크립트가 실제로 실행되어야(사용자가 로컬 Unity에서 해당 메뉴 실행) 갱신된다. 이 원격 환경에는 Unity가 없어 코드까지만 완결되고, 실제 레이어 등록/직렬화는 사용자의 로컬 Unity 에디터 실행이 필요하다(기존 프로젝트의 다른 SetupEditor들과 동일한 제약).

## 주의사항

- Wall/Ground/Monster는 그대로 Default 레이어에 둔다(이번 범위에서 레이어를 옮기지 않는다). 새 레이어("Ball")를 추가해도 Unity 기본 동작상 다른 기존 레이어와의 충돌 비트는 모두 켜진 채로 초기화되므로, Ball-Default(Wall/Ground/Monster) 충돌은 별도 조치 없이 그대로 유지된다 — 오직 Ball-Ball(같은 레이어끼리)만 명시적으로 꺼야 한다.
- "Ball" 태그 미등록 문제는 이번 범위에서 고치지 않는다(사용자 확정 — 그대로 유지).
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`의 궤적 레이캐스트 필터링(`IsBlockingTag()`, `Wall`/`Ground`/`Monster` 태그 화이트리스트만 통과)은 물리 레이어를 전혀 참조하지 않고 순수 태그(`CompareTag`) 기준으로만 동작한다. 따라서 Ball의 레이어를 "Ball"로 바꾸거나 Ball의 태그가 계속 미등록(Untagged) 상태로 남아있어도 이 화이트리스트 로직과는 전혀 상호작용하지 않는다(오케스트레이터가 이미 확인 완료 — 별도 검증/수정 불필요). 즉 이번 레이어 변경은 궤적 프리뷰를 깨뜨리지 않는다.
- 이 원격 환경에는 Unity 에디터가 없어 실제 레이어 등록/프리팹 저장/물리 동작을 직접 눈으로 검증할 수 없다. 문법/로직 검토까지만 가능하며, 최종 검증은 사용자가 로컬 Unity에서 `BallSetupEditor`의 메뉴를 실행하고 플레이 테스트하는 것으로 진행한다.
- 기존 `ObjectPool<T>`(`Assets/_Project/Scripts/Core/ObjectPool.cs`)는 수정하지 않는다. 레이어 기반 해결책을 택했기 때문에 풀 크기가 동적으로 늘어나도 문제가 없으며, 이는 (b) 방식(볼 쌍마다 런타임 `IgnoreCollision` 호출) 대비 이 방식을 택한 핵심 이유 중 하나다.
- `TagManager.asset`에 레이어를 추가하는 순서와 `Ball.prefab`의 `m_Layer`를 설정하는 순서가 뒤바뀌면 `LayerMask.NameToLayer("Ball")`이 `-1`을 반환할 수 있으므로, 반드시 레이어 등록이 먼저 저장된 뒤에 프리팹 레이어를 할당하도록 순서를 보장한다.
