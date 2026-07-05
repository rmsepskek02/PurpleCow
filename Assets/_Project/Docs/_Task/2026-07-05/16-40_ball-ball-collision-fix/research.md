# Research — 볼-볼 물리 충돌 버그 수정

플레이 테스트 중 "볼끼리 서로 충돌하면 안 되는데 충돌해서 튕겨나간다"는 버그가 보고되어 원인을 조사했다. 원인은 `Ball.cs`의 태그 분기 로직이 아니라, 모든 볼(및 Wall/Ground/Monster)이 전부 동일한 물리 레이어(Default, 0)에 있고 `Physics2DSettings`의 레이어 충돌 매트릭스가 Default-Default 쌍을 켜둔 상태이기 때문에, Unity 물리 엔진이 스크립트 콜백과 무관하게 볼-볼 간 실제 충돌 반응(속도 변경)을 적용하고 있는 것으로 확인됐다. 본 문서는 원인을 코드/설정 파일 기준으로 검증하고, plan.md에서 선택할 해결 방식 후보를 정리한다.

## 현재 상태

- `Assets/_Project/Prefabs/Ball/Ball.prefab`의 GameObject는 `m_Layer: 0`(Default), `m_TagString: Untagged`로 저장되어 있다(직접 파일 확인, line 16, 18).
- `ProjectSettings/Physics2DSettings.asset`의 `m_LayerCollisionMatrix` 값이 전 구간 `ffff...`(모든 비트 1)이다(line 56). 즉 Default 레이어끼리도 물리 충돌이 활성화되어 있다.
- `ProjectSettings/TagManager.asset`을 확인한 결과 현재 등록된 태그는 `Monster`, `Wall`, `Ground` 3개뿐이고(line 6-9), `Ball` 태그는 등록되어 있지 않다. 레이어 목록도 8번 슬롯(`Layer 8`)부터 전부 빈 문자열이라(line 17 이하) 커스텀 레이어를 새로 등록할 여유가 충분하다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step1_CreateBallPrefab()`(line 53-94)을 보면 실제로는 `TrySetTag(go, "Ball")` 코드가 존재해 "Ball" 태그를 붙이려 시도한다(line 87). 하지만 `Ball` 태그 자체가 `TagManager.asset`에 등록되어 있지 않으므로, `go.tag = "Ball"` 대입이 예외를 던지고 `TrySetTag`의 `catch` 블록에서 경고 로그만 남긴 채 조용히 무시된다(line 573-583). 이것이 Ball.prefab이 `Untagged`로 남아있는 실제 원인이다. `BallSetupEditor.AddRequiredTags()`(line 20-52)가 등록하는 태그 목록에도 `Monster`/`Wall`/`Ground`만 있고 `Ball`은 빠져 있어, 애초에 "Ball" 태그를 등록해주는 코드 경로가 프로젝트 어디에도 없다.
- 다만 `Ball.cs`나 `TrajectoryPreview.cs`의 주석 관점에서는 이 "태그 없음" 상태가 궤적 레이캐스트 필터링에 우연히도 유용하게 작용하고 있다(`TrajectoryPreview.cs` line 96: "로스터 사이클로 상시 비행 중인 다른 볼의 콜라이더(태그 없음)는 자연히 무시된다"). 즉 태그가 없다는 것 자체는 다른 기능(궤적 미리보기)이 의존하고 있는 상태이므로, 이 버그를 고치면서 태그 자체를 함부로 바꾸면 궤적 미리보기 로직에 영향을 줄 수 있다는 점에 유의해야 한다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `PlaceColliderObject()`(line 396-413, Wall/Ground 배치에 사용)와 `MonsterSetupEditor.cs`의 몬스터/블록 프리팹 생성 코드 어디에도 `go.layer = ...` 형태로 레이어를 명시적으로 설정하는 코드가 없다. 즉 Wall/Ground/Monster/Block 모두 GameObject 생성 시 기본값인 Default(0) 레이어를 그대로 사용한다.
- 이를 실제 프리팹/씬 파일에서 재확인한 결과, `Assets/_Project/Prefabs/Monster/*.prefab`(Fluffy, Spider, StoneBug, ForestDeer, Block_1x1/1x2/2x1/2x2) 전부 `m_Layer: 0`이고, 실제 게임 씬인 `Assets/Scenes/SampleScene.unity`(Wall_Left/Wall_Right/Ground 오브젝트가 존재하는 씬, `URP2DSceneTemplate.unity`는 미사용 템플릿으로 추정)에 있는 모든 GameObject도 전부 `m_Layer: 0`이다.

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Ball/Ball.cs` — `Awake()`(line 27-31)에서 `_rigidbody`, `_collider`를 캐싱하며 둘 다 `private` 필드로 외부 접근 불가. `OnCollisionEnter2D(Collision2D collision)`(line 92-140)는 `collision.gameObject.CompareTag(...)`로 "Monster"/"Wall"/"Ground" 3가지만 분기 처리하고, 그 외(예: 다른 Ball, 태그 없음)의 경우는 이 메서드 안에서 아무 분기도 타지 않아 실질적으로 무시된다. 하지만 **이 메서드는 Unity 물리 엔진이 이미 충돌 반응(속도 변화, 밀어내기)을 계산해 적용한 뒤에 호출되는 콜백**이므로, 여기서 태그를 걸러도 실제 물리적 튕김 자체는 막지 못한다. `SetGhostMode(bool)`(line 194-197)에서 `_collider.isTrigger`를 직접 제어하는 코드가 있어 `_collider`가 `CircleCollider2D`(구체적으로는 `Collider2D` 타입)임을 재확인했다.
- `Assets/_Project/Scripts/Core/ObjectPool.cs` — `Get()`(line 23-39)은 먼저 비활성 상태의 기존 풀 인스턴스를 순회 검색해 재사용하고, 없으면 `CreateNew()`(line 47-50, `Object.Instantiate(_prefab, _parent)`)로 새 인스턴스를 만들어 `_pool` 리스트에 추가한다. 즉 풀 크기가 `initialSize`로 고정되지 않고 필요에 따라 계속 늘어나는 구조다(캡 없음). 이는 아래 "구현 대상 파악"의 해결 방식 비교에서 중요한 제약이 된다.
- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` — `AddRequiredTags()`(line 20-52)에서 `Monster`/`Wall`/`Ground` 태그만 등록하고 `Ball` 태그는 등록하지 않는다. `CreatePhysicsMaterial()`(line 54-78)에서 `BallBounce.physicsMaterial2D`(bounciness 1, friction 0)를 생성해 벽 반사에 사용한다. 레이어 관련 설정 코드는 이 파일에 없다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — `Step1_CreateBallPrefab()`(line 53-94)에서 Ball 프리팹을 만들며 `TrySetTag(go, "Ball")`을 호출하지만 앞서 설명한 대로 태그 미등록으로 실패한다. `Step5_PlaceWallsAndGround()`→`PlaceColliderObject()`(line 388-413)에서 Wall/Ground 오브젝트를 배치하며 태그만 설정하고(`TrySetTag`) 레이어는 건드리지 않는다. `TrySetTag()`(line 573-583)는 태그 설정 실패 시 예외를 잡아 경고만 남기는 유틸리티로, 향후 "Ball" 레이어를 등록하는 에디터 스크립트를 작성할 때 이와 유사한 패턴(`SerializedObject`로 `TagManager.asset`을 수정)을 레이어 등록에도 참고할 수 있다.
- `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` — `EnsureMonsterTag()`(line 22-46)에서 "Monster" 태그만 등록. 레이어 설정 코드 없음. 몬스터/블록 프리팹 전부 Default 레이어로 생성됨을 확인.
- `ProjectSettings/TagManager.asset` — 태그 3개(Monster/Wall/Ground)만 등록, 레이어는 Default/TransparentFX/Ignore Raycast/Water/UI 및 Unity 기본 슬롯만 사용 중이고 8번 슬롯부터 전부 비어 있어 커스텀 레이어 등록 여지 있음.
- `ProjectSettings/Physics2DSettings.asset` — `m_LayerCollisionMatrix: ffff...`로 전 레이어 쌍 충돌이 켜져 있음.
- `Assets/_Project/Prefabs/Ball/Ball.prefab` — GameObject `m_Layer: 0`, `m_TagString: Untagged`, `Rigidbody2D`(gravityScale 0, Continuous 충돌 감지), `CircleCollider2D`(radius 0.17, PhysicsMaterial2D 연결됨) 확인.
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` — line 96 주석에서 "다른 볼의 콜라이더(태그 없음)는 자연히 무시된다"고 명시. 태그가 없다는 사실이 궤적 레이캐스트 필터링에 이용되고 있음을 확인(태그/레이어 변경 시 이 로직과의 상호작용을 plan.md에서 고려해야 함).

## 문제점 / 구현 대상 파악

**근본 원인**: Ball, Wall, Ground, Monster 오브젝트가 전부 물리 레이어 0(Default)에 있고, `Physics2DSettings.m_LayerCollisionMatrix`가 Default-Default 쌍의 충돌을 허용하고 있어, 볼 2개 이상이 동시에 화면에 존재할 때(로스터 다중 볼, 서브볼 스킬 등) 서로의 `Rigidbody2D`+`CircleCollider2D`가 물리적으로 충돌 반응을 일으킨다. `Ball.OnCollisionEnter2D`의 태그 분기는 이 물리 반응 자체를 막을 수 없다(스크립트 콜백은 물리 계산 이후에 실행됨).

**해결 방식 후보** (구체적 구현은 plan.md에서 확정):

- **(a) 전용 Physics2D 레이어("Ball") 신설 + 레이어 간 충돌 비활성화**
  - 방법: `TagManager.asset`에 커스텀 레이어(예: 8번 슬롯 "Ball")를 등록하고, `Ball.prefab`의 `m_Layer`를 그 값으로 설정한 뒤, 게임 시작 시 1회 `Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true)`를 호출해 Ball-Ball 레이어 쌍의 충돌을 전역적으로 끈다.
  - 장점: `ObjectPool`이 동적으로 볼 인스턴스를 계속 생성해도(3번 항목 참고) 새로 생성된 볼은 프리팹 상속으로 자동으로 "Ball" 레이어에 속하게 되므로, 별도로 매번 관계를 등록해줄 필요가 없다. 한 번 설정하면 끝.
  - 단점: `ProjectSettings/TagManager.asset`(Tags & Layers)을 건드려야 한다. 이 원격 환경에는 Unity 에디터가 없어 레이어 등록 자체를 `SerializedObject` 기반 에디터 스크립트로 처리해야 하며(`BallSetupEditor.cs`의 태그 등록 패턴과 유사), Unity 에디터에서 직접 GUI로 등록하는 것과 달리 검증이 번거롭다. 또한 "Ball" 태그가 미등록 상태인 것과 별개로 "Ball" 레이어를 새로 만드는 것이므로 기존 `TrajectoryPreview.cs`의 "태그 없음으로 다른 볼을 자연히 무시" 로직과 레이어 변경이 상호작용하지 않는지(레이캐스트가 태그가 아닌 레이어 마스크를 쓰는지) 확인이 필요하다.

- **(b) 볼 인스턴스 쌍마다 `Physics2D.IgnoreCollision(colliderA, colliderB, true)` 호출**
  - 방법: 볼이 스폰될 때마다 현재 활성 상태인 다른 모든 볼과의 콜라이더 쌍에 대해 `IgnoreCollision`을 호출한다.
  - 장점: 레이어나 `TagManager.asset`/`Physics2DSettings.asset` 같은 프로젝트 설정 파일을 전혀 건드릴 필요가 없다. 순수 런타임 코드(`Ball.cs` 또는 풀/런처 코드)만으로 해결 가능.
  - 단점: `ObjectPool.Get()`(2번 항목 참고)이 고정 크기가 아니라 필요 시 `CreateNew()`로 계속 늘어나는 구조이기 때문에, 새 볼이 생성될 때마다 그 순간 존재하는 모든 다른 볼과의 쌍에 대해 매번 다시 `IgnoreCollision`을 호출해줘야 한다. 스폰/디스폰 타이밍 관리가 번거롭고, 어느 한 쌍이라도 누락되면 그 두 볼 사이에서만 버그가 재현되어 디버깅이 어려워진다.

- **(c, 참고) `Collider2D.excludeLayers`/`includeLayers` 활용**
  - `CircleCollider2D`에 개별적으로 `excludeLayers`를 설정해 같은 레이어를 제외하는 방식도 있으나, 결국 "Ball"이라는 별도 레이어가 먼저 존재해야 의미가 있으므로 전제 조건이 (a)와 동일하다. 레이어 신설이 필요하다면 (a)의 `IgnoreLayerCollision` 방식이 전역 1회 설정으로 더 간단하다.

**추가로 확인이 필요한 상호작용**: "Ball" 태그가 현재 미등록 상태로 남아있는 것은 `SceneSetupEditor.cs`의 버그성 누락(요구 태그 목록에 "Ball"이 빠짐)으로 보이지만, 결과적으로 `TrajectoryPreview.cs`가 이 상태를 "다른 볼 무시" 필터로 활용하고 있다. plan.md에서 레이어 기반 해결책을 택할 경우, 이 궤적 필터링 로직이 태그 기준인지 레이어 기준인지 다시 확인해 레이어 변경이 궤적 미리보기를 깨뜨리지 않는지 별도로 검토해야 한다.

## 결론

원인은 `Ball.cs`의 로직 결함이 아니라 프로젝트 물리 설정(모든 오브젝트가 Default 레이어, 레이어 충돌 매트릭스 전체 활성화) 때문이며, 스크립트 콜백 레벨에서는 이미 늦은 시점이라 물리적 충돌 반응 자체를 막을 수 없다는 것이 핵심이다. 해결 방식은 (a) 전용 Ball 레이어 신설 + `IgnoreLayerCollision` 전역 설정, 또는 (b) 볼 쌍마다 런타임 `IgnoreCollision` 호출 두 가지로 좁혀지며, 각각 트레이드오프(프로젝트 설정 변경 필요 여부 vs. 풀 확장 시 유지보수 부담)가 명확히 갈린다. 어느 방식을 택할지, 그리고 (a)를 택할 경우 `TrajectoryPreview.cs`의 기존 "태그 없음" 필터링 로직과의 상호작용을 어떻게 처리할지는 plan.md 작성 전에 사용자 확인이 필요하다.
