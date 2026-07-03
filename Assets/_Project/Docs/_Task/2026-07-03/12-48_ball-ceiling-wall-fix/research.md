# Research — Ball Ceiling Wall Fix

실제 플레이 테스트 중 발견된 버그(볼이 맵 외곽에서 튕기지 않고 맵 밖으로 나가버리는 현상)의 원인을 코드/씬 파일 기준으로 조사한 문서입니다. 씬에 배치되는 벽/바닥 콜라이더 구성과 `Ball.cs`의 충돌 처리 로직을 확인해 원인을 특정했습니다. 이번 문서는 조사와 문제점 매핑까지만 다루며, 구현 방법(plan.md)은 포함하지 않습니다.

---

## 현재 상태

### SceneSetupEditor.cs — 벽/바닥 콜라이더 생성 로직

- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step5_PlaceWallsAndGround()`(374~379번 줄)가 씬에 배치하는 콜라이더 오브젝트는 다음 3개뿐이다.

```csharp
private static void Step5_PlaceWallsAndGround()
{
    PlaceColliderObject("Wall_Left",  "Wall",   new Vector3(-5.5f, 0f, 0f),  new Vector2(0.2f, 20f));
    PlaceColliderObject("Wall_Right", "Wall",   new Vector3(5.5f,  0f, 0f),  new Vector2(0.2f, 20f));
    PlaceColliderObject("Ground",     "Ground", new Vector3(0f, -10f, 0f),   new Vector2(12f,  0.2f));
}
```

- `PlaceColliderObject`(381~398번 줄)는 `BoxCollider2D`를 붙인 빈 GameObject를 생성하고 태그(`Wall` 또는 `Ground`)를 부여하는 공용 헬퍼다.
- `Wall_Left`/`Wall_Right`는 x = ±5.5, size = (0.2, 20)으로 y축 기준 -10 ~ +10 범위를 커버한다. `Ground`는 y = -10에 위치한 얇은 바닥이다.
- **상단(천장) 벽을 생성하는 코드가 존재하지 않는다.** 좌/우/아래 3면만 정의되어 있고, 위쪽을 막는 콜라이더 생성 호출 자체가 없다.

### SampleScene.unity — 실제 커밋된 씬 확인

- `Assets/Scenes/SampleScene.unity`를 직접 grep한 결과도 코드와 동일하다. `Ground`(565~566번 줄), `Wall_Left`(2472번 줄), `Wall_Right`(3942번 줄) 3개 GameObject만 존재하며, `Wall_Top`이나 `Ceiling` 성격의 오브젝트는 씬에 전혀 없다.
- 즉 에디터 스크립트 누락이 그대로 실제 플레이 씬에 반영되어 있는 상태이며, 씬 파일만 별도로 수정되어 있거나 하는 불일치는 없다.

### 실제 플레이 영역과의 비교

- `Assets/_Project/Docs/AIFailures.md`의 "카메라 orthographic size 미설정" 실패 기록(49~53번 줄)에 문서화된 실제 플레이 영역은 `x: ±5.5, y: -10 ~ +8`이다.
- 좌우 벽은 y: -10 ~ +10까지 물리적으로 커버하므로 플레이 영역 상단(y = +8)보다 더 위쪽까지도 일단은 막아주는 편이지만, 애초에 **위쪽을 막는 콜라이더 자체가 없으므로** 좌우 벽의 상단 끝(y = +10)을 넘어서거나, 좌우 벽 사이의 빈 공간(몬스터가 없는 레인 등)을 그대로 위로 통과하는 볼은 아무것도 막지 못한다.

### Ball.cs — 충돌 처리 로직과 로스터 귀환 사이클

- `Assets/_Project/Scripts/Ball/Ball.cs`의 `OnCollisionEnter2D`(92~140번 줄)는 `Monster`/`Wall`/`Ground` 3개 태그만 분기 처리하는 구조다. 위로 빠져나가는 볼은 어떤 콜라이더와도 충돌 이벤트가 발생하지 않으므로 이 분기 자체가 실행되지 않는다.
- `FixedUpdate()`(72~90번 줄)는 `_isActive`인 동안 매 물리 프레임 속도 크기를 `_ballData.Speed`로 강제 유지하기만 할 뿐, 화면/플레이 영역을 벗어났는지 감지하는 별도의 경계 체크 로직은 없다. 따라서 위로 빠져나간 볼은 어떤 제약도 없이 등속으로 계속 위쪽을 향해 날아간다.
- `BallLauncher.cs`(`Assets/_Project/Scripts/Ball/BallLauncher.cs`)를 함께 확인한 결과, 로스터 소속 볼(`IsRosterMember(ball)`이 true인 볼 — 노말볼 5개 + 특수볼 획득분)의 귀환은 오직 `"Ground"` 태그와의 충돌(`Ball.cs` 131~139번 줄, `ReturnToLaunchPoint()` 호출 → `_isReturning = true` → `FixedUpdate()`가 `LaunchPoint` 도달을 감지해 `BallLauncher.RelaunchBall()` 호출)로만 트리거된다. `"Wall"` 충돌 시에는 로스터 볼의 경우 반사 카운트도 건드리지 않고 그대로 반사만 시킨다(122~125번 줄, `IsRosterMember`이면 `return`).
- 즉 볼이 위쪽으로 빠져나가 어떤 콜라이더와도 충돌하지 않게 되면, `OnCollisionEnter2D`도 `Ground` 분기도 전혀 호출되지 않으므로 `ReturnToLaunchPoint()`가 트리거될 방법이 없다. `BallLauncher._roster` 리스트에는 해당 볼의 엔트리(`BallRosterEntry`)가 그대로 남아있지만(`IsRosterMember` 조회 대상에서 제거되지 않음), 그 볼 개체는 다시는 `RelaunchBall()`을 통해 재발사되지 않는다 — 사실상 로스터에서 영구적으로 이탈한 채로 화면 밖을 계속 날아다니는 상태로 남는다.
- 노말볼이 아닌 서브볼(`LaunchSubBalls`로 생성된, 로스터에 속하지 않는 볼)의 경우도 동일하게, 위로 빠져나가면 `Wall` 충돌에 의한 반사 카운트 소진(`_remainingBounces--` → 0 이하 시 `ReturnToPool()`) 자체가 발생할 기회가 없어 풀로 반환되지 않는다.
- 이 부분(로스터 볼이 완전히 유실되어 다시는 발사되지 않는 상태로 남는지)은 코드 구조상으로는 위와 같이 확인되나, 실제 게임 진행에 미치는 영향(예: 남은 볼 개수 표시 UI, 게임오버 판정 등과의 상호작용)까지는 이번 조사에서 별도로 확인하지 않았다 — 필요 시 plan.md 또는 별도 확인 단계에서 재검토 가능하다.

---

## 관련 파일 및 의존성

| 파일 | 역할 | 비고 |
|---|---|---|
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 씬 초기 설정 에디터 스크립트. `Step5_PlaceWallsAndGround()`(374~379번 줄)에서 `Wall_Left`/`Wall_Right`/`Ground` 3개 콜라이더만 생성 | 상단(천장) 벽 생성 호출이 누락된 근본 원인 지점 |
| `Assets/Scenes/SampleScene.unity` | 실제 커밋된 플레이 씬 | grep 결과 `Wall_Top`/`Ceiling` 성격 오브젝트 없음, 코드와 동일한 3개 오브젝트만 존재함을 확인 |
| `Assets/_Project/Scripts/Ball/Ball.cs` | 볼의 이동/충돌/귀환 로직. `OnCollisionEnter2D`(92~140번 줄)가 `Monster`/`Wall`/`Ground` 태그만 처리 | 상단 경계 이탈 시 어떤 분기도 호출되지 않아 반사/귀환이 트리거되지 않음 |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 로스터(노말볼+특수볼) 관리, 귀환한 볼의 재발사(`RelaunchBall`) 처리 | `IsRosterMember` 조회 대상인 `_roster` 리스트에서 유실된 볼이 자동으로 제거되지는 않음(귀환 자체가 발생하지 않으므로 재발사도 되지 않음) |
| `Assets/_Project/Docs/AIFailures.md` | 카메라 orthographic size 관련 기존 실패 기록. 실제 플레이 영역(`x: ±5.5, y: -10 ~ +8`)이 문서화되어 있음 | 좌우 벽의 y축 커버 범위(-10~+10)와 실제 플레이 영역 상단(y=+8)을 비교하는 데 참조 |

---

## 문제점 / 구현 대상 파악

- **핵심 문제**: `SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단(천장) 벽을 생성하는 코드가 없어, 씬에도 좌/우/아래 3면만 존재하고 위쪽이 완전히 뚫려 있다. 몬스터가 없는 빈 레인 등으로 볼이 위쪽으로 계속 진행하는 경우 이를 막는 콜라이더가 전혀 없어 볼이 플레이 영역 밖으로 무한히 날아가 버린다.
- **연쇄 영향**: `Ball.OnCollisionEnter2D`는 `Monster`/`Wall`/`Ground` 태그가 있는 콜라이더와 충돌해야만 동작하는데, 위로 빠져나간 볼은 애초에 아무 콜라이더와도 충돌하지 않으므로 반사도, 로스터 귀환(재발사 사이클 복귀)도 발생하지 않는다. 결과적으로 유실된 볼은 게임에서 사실상 영구적으로 사라지는 것으로 코드상 확인된다.

## 참고 / 추후 확인 사항 (이번 작업 범위 제외)

아래 두 가지는 조사 과정에서 함께 발견했으나, 이번 task 범위에서는 다루지 않기로 확인되었다. 참고용으로만 남긴다.

- `BallBounce.physicsMaterial2D`(bounciness=1, friction=0)가 Ball의 `CircleCollider2D`에만 연결되어 있고, Wall/Ground의 `BoxCollider2D`에는 연결되어 있지 않다. Unity 2D 물리는 충돌하는 두 콜라이더 중 더 큰 반발값(bounciness)을 사용하므로 현재는 정상 동작하는 것으로 보이나, 벽/바닥 쪽에 명시적으로 연결되어 있지는 않은 상태다.
- Ball의 `Rigidbody2D`가 `CollisionDetectionMode2D.Continuous`로 설정되어 있어, 고속 이동 시 벽을 뚫고 지나가는 터널링 현상이 발생할 가능성은 낮아 보인다.

---

## 결론

- 이번 버그(볼이 맵 외곽에서 튕기지 않고 밖으로 나가버리는 현상)의 루트 원인은 **상단(천장) 벽 콜라이더 자체가 씬 설정 코드(`SceneSetupEditor.Step5_PlaceWallsAndGround`)에서 생성되지 않고 있다는 것**이다. 좌/우/아래 3면은 정상적으로 막혀 있으나, 위쪽만 완전히 뚫려 있는 구조적 누락 버그로 확인된다.
- 실제 커밋된 `Assets/Scenes/SampleScene.unity`도 이 코드의 결과물과 동일하게 3개 콜라이더만 갖고 있음을 grep으로 재확인했으므로, 씬 파일과 에디터 스크립트 사이의 불일치는 없다.
- `Ball.cs`의 충돌 처리 구조상, 위로 빠져나간 볼은 어떤 이벤트도 트리거하지 못해 반사/귀환 없이 그대로 유실되며, 로스터 소속 볼의 경우 재발사 사이클에서도 영구적으로 이탈하는 것으로 코드상 확인된다(단, 실제 게임 진행 지표에 미치는 구체적 영향까지는 이번 조사에서 재확인하지 않았다).
- `physicsMaterial2D` 연결 범위, `CollisionDetectionMode2D` 설정 등 부가적으로 발견된 사항은 이번 task 범위에서 제외하기로 확인되었으며, 별도 대응이 필요하면 추후 별도 task로 다룰 수 있다.
- 다음 단계(plan.md)에서는 `SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단 벽 콜라이더를 추가하는 구현 방법을 다룬다.
