# Research — LaunchPoint Character Orbit

직전 task(`2026-07-04/01-38_character-visual-implementation`)를 main에 병합하는 과정에서, `WallFitter`가 런타임에 화면 비율에 맞춰 재배치하는 `LaunchPoint`와, 씬 설정 시점에 그 위치를 한 번만 복사해 고정되는 `Character` 오브젝트가 서로 어긋날 수 있다는 문제가 발견되었습니다. 이 문서는 `LaunchPoint`가 현재 코드베이스에서 겸하고 있는 역할들(발사 스폰 위치 / 귀환 목적지 / 궤적 프리뷰 원점 / WallFitter 재배치 대상)을 모두 확인하고, 이를 "발사 시작점(무기 끝, 캐릭터를 중심으로 도는 동적인 점)"과 "귀환 목적지(캐릭터 Body, 고정에 가까운 점)"로 분리하기 위한 재설계에 앞서 현재 상태를 조사한 문서입니다. 구체적인 구현 방법은 다루지 않고, plan.md 단계에서 결정할 열린 이슈로 남겨둡니다.

---

## 현재 상태

### `LaunchPoint`가 겸하고 있는 4가지 역할

현재 `BallLauncher._launchPoint` (`Assets/_Project/Scripts/Ball/BallLauncher.cs` 11행) 하나의 `Transform`이 다음 4가지 역할을 동시에 수행하고 있다.

1. **볼 발사 스폰 위치**: `BallLauncher.LaunchRosterEntry()` (95행) `entry.Ball.transform.position = _launchPoint.position;` — 로스터 볼이 발사될 때 이 위치로 순간이동한 뒤 발사된다.
2. **귀환(return) 목적지**: `Ball.cs` 79행(도착 판정) `Vector2 toLaunchPoint = (Vector2)BallLauncher.Instance.LaunchPoint.position - (Vector2)transform.position;`과 214행(`ReturnToLaunchPoint()` 내부) `Vector2 direction = ((Vector2)BallLauncher.Instance.LaunchPoint.position - (Vector2)transform.position).normalized;` — Ground에 닿아 귀환 중인 볼이 매 프레임 이 위치를 향해 방향을 재설정하고, `RETURN_ARRIVAL_DISTANCE`(0.3f) 이내로 접근하면 도착으로 판정해 재발사한다.
3. **궤적 프리뷰(TrajectoryPreview) 원점**: `TrajectoryPreview.UpdateTrajectory()` 70행 `Vector2 origin = BallLauncher.Instance.LaunchPoint.position;` — 조준 중 표시되는 점선 궤적이 이 위치에서 시작된다.
4. **WallFitter가 화면비에 맞춰 재배치하는 대상**: `WallFitter.cs` 12행 `[SerializeField] private Transform _launchPoint;`, 17행 `_nativeLaunchPointY = -6.0f`, 45행 `SetY(_launchPoint, _nativeLaunchPointY * scaleY);` — `[ExecuteAlways]`가 붙은 `WallFitter`가 `Start()`/`OnValidate()` 시점(20~28행)마다 `Apply()`(30~46행)를 호출해 카메라의 `orthographicSize`/`aspect`와 배경 스프라이트 크기 비율(`scaleY`)에 따라 `_launchPoint`의 월드 Y좌표를 동적으로 재계산한다. `_wallLeft`/`_wallRight`/`_wallTop`/`_ground`도 동일한 패턴으로 재배치되는 대상이다.

즉 `LaunchPoint`는 "볼이 어디서 나가고 어디로 돌아오는지"와 "화면 비율이 달라져도 항상 화면 하단의 같은 상대 위치에 있어야 하는 기준점"이라는 서로 다른 두 성격의 요구사항을 하나의 Transform으로 동시에 만족시키고 있었다.

### `Character` 오브젝트의 현재 배치 방식과 한계

`SceneSetupEditor.Step11_SetupCharacterVisual()` (`Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` 306~352행)에서 `Character` GameObject를 다음과 같이 생성한다.

- 315~320행: `BallLauncher`의 자식 `LaunchPoint` Transform을 `launcherObj.transform.Find("LaunchPoint")`로 찾는다.
- 322~335행: `Character`가 없으면 새로 생성해 `launcherObj.transform`(즉 `BallLauncher`)의 자식으로 붙이고, `characterObj.transform.localPosition = launchPoint.localPosition;`(329행)으로 **씬 설정 시점에 딱 한 번** `LaunchPoint`의 로컬 좌표를 복사한다. 이미 존재하면(332~335행) 아무 위치 조정도 하지 않는다.
- 337~339행: `Character`의 자식으로 `Body`/`Head`/`Weapon` 3개의 `SpriteRenderer`를 `CreateCharacterPart()`(354~376행)로 생성하고, 각각 로컬 오프셋(`Body`: `(0.42, -0.75, 0)`, `Head`: `(0.51, -0.23, 0)`, `Weapon`: `Vector3.zero`)을 부여한다.
- 341~349행: `CharacterAimController` 컴포넌트를 붙이고 `_bodyRenderer`/`_headRenderer`/`_weaponRenderer` 참조를 연결한다.

`Character`는 `LaunchPoint`와 마찬가지로 `BallLauncher`의 자식이 아니라 **형제(sibling)** 오브젝트이며(322행 주석에서도 "동일한 부모(BallLauncher)의 형제 오브젝트"라고 명시), 씬 설정 시점의 위치 복사 외에는 `LaunchPoint`와 아무런 런타임 연결이 없다. 따라서 `WallFitter.Apply()`가 런타임에 `_launchPoint`의 Y좌표를 재계산해도(예: 세로로 더 긴 화면비의 기기에서 재배치되는 경우), `Character`는 씬 설정 시점에 복사된 원래 좌표에 그대로 머물러 있어 실제 발사/귀환 지점과 시각적으로 어긋나게 된다. 이것이 직전 병합에서 발견된 문제의 원인이다.

### `CharacterAimController`의 현재 회전/반전 로직

`Assets/_Project/Scripts/Character/CharacterAimController.cs`는 `Character`의 자식인 Body/Head/Weapon 3개 `SpriteRenderer`의 반전과 회전을 매 프레임(`Update()`, 27~57행) 계산한다.

- 8~12행: `_bodyRenderer`/`_headRenderer`/`_weaponRenderer` 참조와 `_headDampFactor = 0.25f`, `_flipDeadzone = 0.05f` 튜닝값을 `SerializeField`로 보유.
- 20~25행(`Start()`): 세 파츠의 `localPosition`을 각각 `_headBasePosition`/`_bodyBasePosition`/`_weaponBasePosition`으로 캐싱한다.
- 29행: `BallLauncher.Instance.LaunchDirection`을 매 프레임 읽어 조준 방향을 얻는다.
- 33행: `aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;` — 스프라이트 기본 정면이 위쪽을 향한다고 가정한 오프셋.
- 36~41행: `direction.x`가 데드존을 넘으면 `_facingRight`를 갱신하고, 세 파츠 모두 `flipX`로 좌우 반전(회전 방식이 아닌 `flipX`만 사용, 4~5행 주석에 "확정된 설계"로 명시됨).
- 45~48행: 반전 시 `localPosition.x` 부호도 함께 뒤집어 파츠 위치가 좌우 대칭이 되도록 보정.
- 51행: `_weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle);` — 무기는 조준 방향을 감쇠 없이 그대로 따라간다.
- 54행: `_headRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle * _headDampFactor);` — 머리는 감쇠(0.25배)된 각도로 약하게 회전.
- Body는 회전을 전혀 적용하지 않고(56행 주석) `flipX`+좌우 위치 미러링만 적용된다.

이 스크립트는 현재 위치(발사/귀환 지점 계산)에는 전혀 관여하지 않고, 오직 파츠의 회전/반전 시각 효과만 담당한다.

### 무기 스프라이트 Pivot(그립 위치) 재확인

`Assets/_Project/Sprites/Character/Character_main_weapon.png.meta`를 다시 확인한 결과, `spriteSheet.sprites[0]` 항목(115~132행) 기준 다음과 같다.

- `rect: { x: 0, y: 0, width: 59, height: 116 }` (121~122행) — 실제 유효 스프라이트 사각형은 59x116px.
- `pivot: { x: 0.39, y: 0.43 }` (124행), 최상단 `spritePivot`도 동일하게 `{ x: 0.39, y: 0.43 }` (53행) — 그립(손잡이) 위치로 이미 설정되어 있다(직전 task에서 커스텀 Pivot으로 조정 완료된 상태).
- `spritePixelsToUnits: 100` (54행).

배경에서 언급된 대로, 그립(Pivot, y=0.43)에서 갈고리 쪽 끝(스파이크 반대쪽, 스프라이트 상단 y=1.0 지점)까지의 거리를 픽셀 치수로 역산하면 `(1 - 0.43) * 116 / 100 = 0.6612` 유닛(약 0.66)이 된다. 이 값 자체는 배경에서 이미 확정된 수치이며, 이번 조사에서 메타 파일 원본 값(`pivot.y = 0.43`, `height = 116`, `pixelsToUnits = 100`)으로 재검증되었다.

---

## 관련 파일 및 의존성

| 파일 | 역할 | 현재 `LaunchPoint`와의 관계 |
|---|---|---|
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 로스터 볼 발사/재발사 관리 (`Singleton<BallLauncher>`) | 11행 `_launchPoint` 필드 보유, 24행 `LaunchPoint` 프로퍼티로 외부 노출, 95행에서 발사 스폰 위치로 직접 사용 |
| `Assets/_Project/Scripts/Ball/Ball.cs` | 개별 볼의 이동/충돌/귀환 상태 관리 | 79행(도착 거리 판정), 214행(`ReturnToLaunchPoint()` 내 방향 계산)에서 `BallLauncher.Instance.LaunchPoint.position`을 귀환 목적지로 직접 참조 |
| `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` | 조준 중 점선 궤적/2차 충돌 지점 표시 | 70행 `UpdateTrajectory()`에서 `BallLauncher.Instance.LaunchPoint.position`을 궤적 시작 원점으로 사용 |
| `Assets/_Project/Scripts/Core/WallFitter.cs` | 카메라 화면비에 맞춰 벽/바닥/발사점 재배치 (`[ExecuteAlways]`) | 12행 `_launchPoint` 필드 보유, 45행에서 `Start()`/`OnValidate()`마다 Y좌표를 동적으로 재계산해 재배치. `Character`에 대한 참조는 전혀 없음 |
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 씬 자동 설정 에디터 스크립트 | `Step8_ConnectBallLauncherRefs()`(630~639행)에서 `LaunchPoint` GameObject 최초 생성(`localPosition = (0, -8, 0)`) 및 `BallLauncher._launchPoint` 연결. `Step6_SetupWallFitter()`(520행, 529행)에서 `WallFitter._launchPoint`에 씬의 `LaunchPoint` 오브젝트 연결. `Step11_SetupCharacterVisual()`(306~352행)에서 `Character` 생성 시 `LaunchPoint.localPosition`을 1회 복사(329행) |
| `Assets/_Project/Scripts/Character/CharacterAimController.cs` | Body/Head/Weapon 파츠의 좌우 반전 및 회전(무기 강하게, 머리 약하게, 몸통 없음) 계산 | `LaunchPoint`를 직접 참조하지 않음. `BallLauncher.LaunchDirection`(조준 방향)만 읽어 회전/반전 계산 |
| `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta` | 무기 스프라이트 Import 설정 | 그립 Pivot이 `(0.39, 0.43)`으로 이미 설정되어 있음. rect `height: 116`, `spritePixelsToUnits: 100` 기준 그립→갈고리 끝 거리는 약 0.66 유닛으로 역산됨 |

---

## 문제점 / 구현 대상 파악

- **`LaunchPoint`의 4가지 역할을 두 갈래로 분리해야 함**: 배경에서 확정된 재설계 방향에 따라 "발사 시작점(무기 끝, 캐릭터 위치 + 무기 길이 × `LaunchDirection`으로 매 프레임 계산되는 동적인 점)"과 "귀환 목적지(`Character`의 `Body` 자식 오브젝트 월드 위치, 고정에 가까움)"로 나뉘어야 한다. 그러나 현재 `Ball.cs`(79행, 214행)와 `TrajectoryPreview.cs`(70행)는 모두 `BallLauncher.LaunchPoint` 단일 프로퍼티만 참조하고 있어, 각 참조 지점이 발사/귀환/궤적 프리뷰 중 어느 역할에 해당하는지에 따라 어떤 새 프로퍼티(예: 발사 시작점용, 귀환 목적지용)를 참조하도록 나눠야 할지 구조 설계가 아직 결정되지 않은 열린 이슈다.
  - `BallLauncher.LaunchRosterEntry()`(95행)의 스폰 위치와 `TrajectoryPreview.UpdateTrajectory()`(70행)의 궤적 원점은 "발사 시작점(무기 끝)" 역할에 해당하는 것으로 보이나, 이를 `BallLauncher`에 새 프로퍼티로 추가할지, `CharacterAimController`가 직접 노출할지는 미결정.
  - `Ball.cs`의 79행(도착 판정)과 214행(귀환 방향 계산)은 "귀환 목적지(Body)" 역할에 해당하는 것으로 보이나, 마찬가지로 어떤 클래스가 이 값을 노출할지(예: `BallLauncher`에 `ReturnPoint` 프로퍼티 추가 후 내부적으로 `Character`의 `Body` Transform 참조 등) 미결정.
- **동적 계산 방식이 미결정**: 발사 시작점이 더 이상 고정 좌표가 아니라 매 프레임 `Character 위치 + 무기 길이 × LaunchDirection`으로 계산되어야 하는데, 현재 참조 지점들(`BallLauncher.cs` 95행, `TrajectoryPreview.cs` 70행)은 모두 `Transform.position`을 직접 읽는 방식이다. 이를 (a) 실제 `Transform`(예: 무기 끝에 붙는 자식 오브젝트)을 매 프레임 갱신하는 스크립트를 새로 두는 방식과, (b) `Vector3`/`Vector2`를 반환하는 계산 프로퍼티(예: `BallLauncher.LaunchPoint`를 Transform 대신 계산된 값을 반환하도록 변경)로 바꾸는 방식 중 무엇을 택할지 결정되지 않았다.
- **`WallFitter`에 `Character` 참조가 없음**: `WallFitter.cs`는 현재 `_launchPoint`(12행)를 재배치 대상으로 갖고 있을 뿐 `Character`에 대한 필드/참조가 전혀 없다. 배경에서 확정된 대로 `WallFitter`가 게임 시작 시 `Character`를 (기존에 `LaunchPoint`를 재배치하던 것과 동일한 Y좌표 계산 방식으로) 재배치하려면 `_character` 같은 신규 `Transform` 필드 추가와 `Apply()`(30~46행) 로직 수정, 그리고 `SceneSetupEditor.Step6_SetupWallFitter()`(496~539행)에서의 참조 연결 갱신이 필요하다. 다만 구체적으로 어떤 필드명/코드 구조로 추가할지는 plan.md에서 결정할 사안이다.
- **무기 길이(약 0.66 유닛) 하드코딩 위치 미결정**: 그립→갈고리 끝 거리(약 0.66 유닛)를 어느 클래스의 `SerializeField`로 노출할지(예: `CharacterAimController`에 `_weaponLength` 필드 추가, 혹은 발사 시작점을 계산하는 별도 신규 스크립트에 필드 추가 등)가 아직 결정되지 않았다.
- **`Character` 배치 코드(`SceneSetupEditor.Step11_SetupCharacterVisual()`)의 씬 설정 시점 위치 복사 로직도 재검토 필요**: 현재 329행 `characterObj.transform.localPosition = launchPoint.localPosition;`은 여전히 "LaunchPoint 위치를 Character가 따라간다"는 기존 방향을 전제로 한 코드다. 재설계 방향(Character가 기준점, LaunchPoint가 Character를 따라감)에 맞춰 이 로직을 어떻게 바꿀지(혹은 애초에 `Character`의 초기 위치를 어떤 값으로 둘지)는 plan.md 단계의 구현 대상이다.

---

## 결론

`LaunchPoint`(`BallLauncher._launchPoint`)는 현재 발사 스폰 위치(`BallLauncher.cs` 95행), 귀환 목적지(`Ball.cs` 79행/214행), 궤적 프리뷰 원점(`TrajectoryPreview.cs` 70행), WallFitter의 화면비 재배치 대상(`WallFitter.cs` 45행)이라는 4가지 역할을 단일 Transform으로 겸하고 있으며, `Character`(`SceneSetupEditor.cs` 306~352행)는 씬 설정 시점에 이 `LaunchPoint`의 위치를 한 번만 복사하는 별개의 형제 오브젝트라 `WallFitter`의 런타임 재배치를 따라가지 못하는 것이 현재 확인된 문제다.

배경에서 이미 확정된 재설계 방향(Character를 화면비에 맞춰 한 번 재배치되는 고정 기준점으로 삼고, LaunchPoint는 Character를 중심으로 무기 길이만큼 조준 방향으로 궤도를 도는 동적인 점으로, 귀환 목적지는 Character의 Body 위치로 분리)은 그대로 plan.md의 목표로 이어간다. 다만 이 문서에서 정리한 대로 다음 세부 설계는 아직 열린 이슈이며 plan.md에서 dev 에이전트와 함께 확정해야 한다.

- 발사 시작점(무기 끝)과 귀환 목적지(Body)를 각각 어떤 프로퍼티/필드로 어느 클래스에 노출할지
- 발사 시작점을 실제 Transform으로 매 프레임 갱신할지, 계산 프로퍼티로 대체할지
- `WallFitter`에 `Character` 참조를 어떻게 추가하고 `Apply()` 로직을 어떻게 수정할지
- 무기 길이(약 0.66 유닛) 값을 어느 클래스의 `SerializeField`로 노출할지
- `SceneSetupEditor.Step11_SetupCharacterVisual()`의 위치 복사 로직(329행)을 재설계 방향에 맞춰 어떻게 바꿀지

이 문서 자체에서는 위 이슈들에 대한 결정을 내리지 않았으며, 구체적인 해결 구조는 plan.md 단계에서 다룬다.
