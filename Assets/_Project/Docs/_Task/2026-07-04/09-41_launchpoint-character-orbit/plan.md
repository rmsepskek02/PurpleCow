# Plan — LaunchPoint Character Orbit

research.md에서 정리한 `LaunchPoint`의 4가지 역할(발사 스폰 / 귀환 목적지 / 궤적 프리뷰 원점 / WallFitter 재배치 대상)을 "발사 시작점(무기 끝, 캐릭터를 중심으로 도는 동적 계산값)"과 "귀환 목적지(Character의 Body 위치)"로 분리하는 구현 계획입니다. research.md가 열어뒀던 5가지 이슈(노출 주체, 계산 방식, WallFitter 재배치 대상, 무기 길이 소유 클래스, 씬 배선 스크립트 분리 여부)는 사용자와의 논의를 거쳐 모두 아래와 같이 확정되었으며, 이 문서는 확정된 설계를 바탕으로 한 구체적인 변경 단계만 다룹니다.

---

## 구현 목표

- `Character`를 화면비에 맞춰 `WallFitter`가 한 번 재배치하는 고정 기준점으로 삼고, 발사 시작점과 귀환 목적지를 `Character`를 중심으로 계산되는 두 개의 별도 값으로 분리한다.
- **[확정 1] 발사/귀환 지점 노출 주체는 `BallLauncher`가 창구**: `Ball.cs`, `TrajectoryPreview.cs` 등 기존 게임플레이 소비 코드는 계속 `BallLauncher`에만 값을 물어보고, `BallLauncher`가 내부적으로 `CharacterAimController`(캐릭터 시각 레이어)를 참조해 값을 계산/전달한다. 게임플레이 로직이 시각 레이어를 직접 알 필요가 없도록 유지한다.
- **[확정 2] Transform 오브젝트 방식이 아닌 계산 프로퍼티 방식 채택**: 별도 GameObject(Transform)를 매 프레임 갱신하는 방식은 채택하지 않는다.
  - 발사 시작점 = `Character 위치 + LaunchDirection(정규화) × 무기 길이`로 즉시 계산 (무기 회전의 `-90f` 오프셋이 조준 방향과 항상 일치하도록 설계되어 있으므로 무기 Transform을 다시 읽을 필요가 없음)
  - 귀환 목적지 = `CharacterAimController._bodyRenderer.transform.position`을 그대로 반환 (이미 실시간 Transform 값이므로 캐싱 불필요)
  - Transform 방식이 갖는 "누군가 매 프레임 먼저 갱신해야 값이 맞다"는 실행 순서 의존성을 제거해, 이번 세션에서 이미 두 차례 겪은 "복사된 값이 원본과 어긋나는" 버그 유형을 재도입하지 않기 위함.
- **[확정 3] `WallFitter`의 재배치 대상을 `LaunchPoint`에서 `Character`로 변경 + 필드명 리네이밍**: `_launchPoint`(Transform)→`_character`, `_nativeLaunchPointY`(-6.0f)→`_nativeCharacterY`(동일 기본값 -6.0f 유지). 필드명 변경으로 인해 로컬에서 이미 튜닝된 값이 있다면 초기화될 수 있음을 감수한다(가독성 우선).
- **[확정 4] 무기 길이는 `CharacterAimController`가 `SerializeField`로 소유**: `_weaponLength`(float, 기본값 `0.6612f` — 그립 Pivot y=0.43, rect height=116, spritePixelsToUnits=100 기준 역산값)를 추가한다. 스프라이트 자체는 변경 예정이 없으므로 매 프레임 자동 계산 대신 고정값으로 둔다.
- **[확정 5] `SceneSetupEditor.cs`는 죽은 코드만 삭제, 새 배선은 신규 에디터 스크립트로 분리**: 기존 `SceneSetupEditor.cs`는 이미 여러 task를 거쳐 안정화된 공용 자동화 스크립트이므로 새 로직을 얹지 않고, 이번 재설계로 무의미해진 코드만 삭제한다. `WallFitter`↔`Character` 연결, `Character` 기본 위치 지정 등 신규 배선은 별도 신규 파일 `CharacterLaunchOrbitSetupEditor.cs`(자체 메뉴 아이템)에서 처리한다. 이는 `SceneSetupEditor.cs`/`UISetupEditor.cs`/`MonsterSetupEditor.cs`처럼 관심사별로 여러 `*SetupEditor.cs`를 두는 기존 프로젝트 관례와 일치한다.
- `CharacterManager.cs`(HP/XP 로직)와 볼의 물리/충돌/데미지 로직 자체는 이번 작업과 무관하며 건드리지 않는다 — 발사/귀환 "위치"만 재설계 대상이다.

---

## 단계별 작업 계획

1. **[dev] `CharacterAimController.cs` 수정**
   - `_weaponLength`(float, `SerializeField`, 기본값 `0.6612f`) 필드 추가.
   - `BodyPosition`(Vector2) 프로퍼티 추가 — `_bodyRenderer.transform.position` 반환.
   - `WeaponLength`(float) 프로퍼티 추가 — `_weaponLength` 반환.
   - 클래스 선언을 `MonoBehaviour` 상속에서 `Singleton<CharacterAimController>` 상속으로 변경한다(기존 `CharacterManager`/`BallLauncher` 등과 동일한 싱글톤 패턴 재사용). `Update()` 등 기존 로직은 그대로 유지한다.

2. **[dev] `BallLauncher.cs` 수정**
   - 11행 `_launchPoint`(Transform) 필드와 24행 `LaunchPoint`(Transform) 프로퍼티를 삭제한다.
   - 새 계산 프로퍼티 2개를 추가한다.
     - `LaunchOrigin`(Vector2): `(Vector2)CharacterAimController.Instance.transform.position + LaunchDirection.normalized * CharacterAimController.Instance.WeaponLength`
     - `ReturnPoint`(Vector2): `CharacterAimController.Instance.BodyPosition`
   - `LaunchRosterEntry()`(95행) 내 `entry.Ball.transform.position = _launchPoint.position;`을 `entry.Ball.transform.position = LaunchOrigin;`으로 변경한다.

3. **[dev] `Ball.cs` 수정**
   - 79행 도착 판정: `(Vector2)BallLauncher.Instance.LaunchPoint.position`을 `BallLauncher.Instance.ReturnPoint`로 변경한다.
   - 214행 `ReturnToLaunchPoint()` 내부: `(Vector2)BallLauncher.Instance.LaunchPoint.position`을 `BallLauncher.Instance.ReturnPoint`로 변경한다.

4. **[dev] `TrajectoryPreview.cs` 수정**
   - 70행 `Vector2 origin = BallLauncher.Instance.LaunchPoint.position;`을 `Vector2 origin = BallLauncher.Instance.LaunchOrigin;`으로 변경한다.

5. **[dev] `WallFitter.cs` 수정**
   - 12행 `_launchPoint`(Transform) 필드를 `_character`(Transform)로 이름 변경.
   - 17행 `_nativeLaunchPointY`(기본값 `-6.0f`) 필드를 `_nativeCharacterY`(기본값 `-6.0f` 동일 유지)로 이름 변경.
   - `Apply()`(45행) 내 `SetY(_launchPoint, _nativeLaunchPointY * scaleY);`를 `SetY(_character, _nativeCharacterY * scaleY);`로 변경.

6. **[dev] `SceneSetupEditor.cs`에서 죽은 코드 삭제 (그 외 로직은 그대로 유지)**
   - `Step8_ConnectBallLauncherRefs()`(592~644행) 중 629~639행: `LaunchPoint` GameObject 생성 및 `BallLauncher._launchPoint` 연결 코드 삭제.
   - `Step6_SetupWallFitter()`(496~539행) 중 520행 `Transform launchPoint = FindTransformOrWarn("LaunchPoint");`, 529행 `so.FindProperty("_launchPoint").objectReferenceValue = launchPoint;`, 534행 `so.FindProperty("_nativeLaunchPointY").floatValue = -6.0f;` 삭제. (`_wallLeft`/`_wallRight`/`_wallTop`/`_ground`/카메라/배경 연결 등 나머지 로직은 그대로 유지)
   - `Step11_SetupCharacterVisual()`(306~352행) 중 315~320행(`LaunchPoint` Transform을 찾아 없으면 경고 후 리턴하는 코드)과 329행 `characterObj.transform.localPosition = launchPoint.localPosition;` 삭제. `Character`가 없을 때는 위치 대입 없이 그대로 생성만 하도록 하고(초기 위치는 7단계 신규 에디터 스크립트가 담당), Body/Head/Weapon 파츠 생성 및 `CharacterAimController` 참조 연결 로직(337~349행)은 그대로 유지한다.

7. **[dev] 신규 `Assets/_Project/Scripts/Editor/CharacterLaunchOrbitSetupEditor.cs` 작성**
   - `[MenuItem("PurpleCow/Setup/Character LaunchPoint Orbit Setup")]`로 진입점 하나를 추가한다.
   - (a) `BallLauncher` 오브젝트의 자식 `Character`를 찾아(없으면 경고 후 스킵), 기존 `LaunchPoint`가 갖던 기본 로컬 좌표와 동일한 `(0, -8, 0)`(기존 `Step8_ConnectBallLauncherRefs()`의 635행 값 재사용)으로 `localPosition`을 설정한다.
   - (b) `Camera.main`에서 `WallFitter` 컴포넌트를 찾아(없으면 경고 후 스킵), `SerializedObject` 패턴(기존 `Step6_SetupWallFitter()`와 동일한 방식)으로 신규 `_character` 필드에 `Character` Transform을 연결한다.
   - 기존 `SceneSetupEditor.cs`의 `FindTransformOrWarn()` 같은 헬퍼는 재사용하지 않고(다른 클래스의 `private static` 멤버이므로 접근 불가) 필요한 최소한의 탐색 로직만 이 신규 파일 안에 자체적으로 둔다.

8. **[qa] 코드 리뷰 및 로직 검증**
   - 위 1~7단계 변경 사항이 research.md에서 정리한 4가지 역할(발사 스폰/귀환 목적지/궤적 프리뷰 원점/WallFitter 재배치 대상)을 빠짐없이 대체했는지 확인한다.
   - `BallLauncher`, `Ball`, `TrajectoryPreview` 어디에도 `LaunchPoint`(구 프로퍼티/필드)에 대한 참조가 남아있지 않은지 검증한다.
   - `CharacterAimController`를 `Singleton<T>`로 변경한 것이 기존 `Awake()` 오버라이드(있다면)와 충돌하지 않는지, `base.Awake()` 호출 누락이 없는지 확인한다.
   - `SceneSetupEditor.cs`에서 삭제된 코드 범위가 계획과 정확히 일치하는지(그 외 로직이 실수로 삭제되지 않았는지) 확인한다.

---

## 예상 변경/생성 파일 목록

- **신규**: `Assets/_Project/Scripts/Editor/CharacterLaunchOrbitSetupEditor.cs`
- **수정**:
  - `Assets/_Project/Scripts/Character/CharacterAimController.cs` (`_weaponLength`/`BodyPosition`/`WeaponLength` 추가, `Singleton<CharacterAimController>` 상속으로 변경)
  - `Assets/_Project/Scripts/Ball/BallLauncher.cs` (`_launchPoint`/`LaunchPoint` 삭제, `LaunchOrigin`/`ReturnPoint` 추가, `LaunchRosterEntry()` 참조 변경)
  - `Assets/_Project/Scripts/Ball/Ball.cs` (79행, 214행 참조 변경)
  - `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` (70행 참조 변경)
  - `Assets/_Project/Scripts/Core/WallFitter.cs` (`_launchPoint`→`_character`, `_nativeLaunchPointY`→`_nativeCharacterY` 리네이밍)
  - `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` (죽은 코드 삭제만, 그 외 로직 유지)
- **사용자가 로컬에서 처리해야 하는 사항**: `SampleScene.unity`는 코드 수정만으로 자동 반영되지 않는다. 사용자가 로컬 Unity에서 (1) `PurpleCow/Setup/Scene Setup`을 먼저 재실행하고, (2) 씬에 이미 남아있는 `LaunchPoint` GameObject를 필요 시 수동으로 정리한 뒤, (3) 신규 `PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴를 실행해야 `WallFitter`가 `Character`를 재배치하도록 완전히 반영된다.

---

## 주의사항

- 원격 환경에 Unity 에디터가 없어 코드 수정만으로는 이미 커밋된 `SampleScene.unity`에 자동 반영되지 않는다(기존 WaveTableData/Wall_Top 사례와 동일한 제약).
- 기존 씬에 이미 생성되어 있던 `LaunchPoint` GameObject는 `SceneSetupEditor.cs`에서 생성 로직을 삭제해도 씬 파일 자체에서 자동으로 사라지지 않으므로, 사용자가 로컬에서 필요 시 수동으로 정리해야 할 수 있다.
- `WallFitter`의 필드명 변경(`_launchPoint`→`_character`, `_nativeLaunchPointY`→`_nativeCharacterY`)으로 인해, 사용자가 로컬 Inspector에서 이미 튜닝해둔 값이 있었다면 초기화되어 기본값(`-6.0f` 등)으로 돌아갈 수 있다 — 재설정이 필요할 수 있음을 인지해야 한다.
- `CharacterManager.cs`, 볼의 물리/충돌/데미지 판정 로직은 이번 작업에서 전혀 수정하지 않는다.
- `CharacterAimController`를 `Singleton<CharacterAimController>`로 변경하는 것은 `DevRules.md`가 명시한 싱글톤 대상 목록(`GameManager`/`WaveManager`/`SkillManager`/`ObjectPool`/`UIManager`)에 원래 포함되지 않은 클래스다. 다만 이미 `CharacterManager`도 동일한 방식으로 싱글톤화되어 있는 선례가 있어, 이번 변경은 그 선례를 일관성 있게 확장하는 것임을 명시한다.

---

## 수정 — LaunchOrigin 공식 단순화

research.md의 "재검토 — 무기 길이 기반 발사 위치 가정 오류 발견" 섹션에서 정리된 대로, 실제 게임 레퍼런스 스크린샷 전체를 재확인한 결과 무기 스프라이트는 장식적 요소일 뿐 볼은 항상 캐릭터의 고정된 중심 위치에서 발사된다는 사실이 확인되어, 위 "구현 목표"의 [확정 2]에서 명시했던 `LaunchOrigin` 공식을 아래와 같이 단순화한다.

- 기존: `LaunchOrigin` = `Character 위치 + LaunchDirection(정규화) × 무기 길이`
- 수정: `LaunchOrigin` = **`Character 위치` 그대로** (무기 길이 오프셋 제거)

### 실제 코드 변경 필요 사항 (아직 미반영)

- `Assets/_Project/Scripts/Ball/BallLauncher.cs`의 `LaunchOrigin` 프로퍼티에서 `LaunchDirection.normalized * CharacterAimController.Instance.WeaponLength` 부분을 제거하고, `CharacterAimController.Instance.transform.position`만 반환하도록 수정이 필요하다.
- 이 문서 수정 시점에는 코드에 아직 반영되지 않았으며, 이번 문서 수정 다음 별도로 dev 에이전트에게 위임해 진행할 예정이다.

### 열린 사항

- `CharacterAimController._weaponLength`/`WeaponLength` 프로퍼티를 삭제하지 않고 그대로 둘지, 아니면 완전히 제거할지는 아직 결정되지 않았다. 코드 수정 단계에서 결정한다.

---

## 수정 2 — 반전 시 위치 이동 로직 제거

research.md의 "재검토 2 — 좌우 반전 시 파츠 위치 이동 버그 발견" 섹션에서 정리된 대로, `CharacterAimController.cs`가 좌우 반전(`flipX`) 시 Head/Body/Weapon의 로컬 X 좌표 부호까지 함께 뒤집어 파츠 위치를 이동시키는 버그가 확인되어, 이를 제거하기 위한 구현 지침을 아래와 같이 추가한다.

### 구현 지침 (확정된 수정 사항)

- `CharacterAimController.cs`에서 반전 시 X 좌표 부호를 뒤집어 위치를 이동시키는 로직(`sign` 변수 계산 및 `_headRenderer`/`_bodyRenderer`/`_weaponRenderer` 3개의 `localPosition` 재대입 줄)을 **삭제**한다.
- Head/Body/Weapon은 각자의 기존 로컬 위치(`_headBasePosition`/`_bodyBasePosition`/`_weaponBasePosition`, `Start()`에서 캐싱된 값)를 반전 여부와 무관하게 **그대로 유지**한다.
- 좌우 반전은 오직 `SpriteRenderer.flipX`만으로 처리한다(기존 확정 사항 그대로 유지, 이번에 바뀌는 건 "위치까지 같이 옮기던 부분"을 없애는 것뿐이다).
- 회전 로직(Weapon은 조준 방향을 그대로 따라가는 회전, Head는 감쇠된 회전, Body는 회전 없음)은 이번 수정과 무관하며 그대로 유지한다.

### 실제 코드 변경 필요 사항 (아직 미반영)

- `Assets/_Project/Scripts/Character/CharacterAimController.cs`의 `Update()`에서 `sign` 계산 및 3개 `localPosition` 재대입 줄(Head/Body/Weapon)을 삭제하는 수정이 필요하다.
- 이 문서 수정 시점에는 코드에 아직 반영되지 않았으며, 이번 문서 수정 다음 별도로 dev 에이전트에게 위임해 진행할 예정이다.

---

## 수정 3 — flipX 부호 반전 버그 수정

research.md의 "재검토 3 — flipX 부호 반전 버그 발견" 섹션에서 정리된 대로, `CharacterAimController.cs`의 좌우 반전(`flipX`) 판정 부호가 정확히 반대로 걸려 있는 버그가 확인되어, 이를 바로잡기 위한 구현 지침을 아래와 같이 추가한다.

### 구현 지침 (확정된 수정 사항)

- `CharacterAimController.cs`의 다음 줄

  ```csharp
  _bodyRenderer.flipX = _headRenderer.flipX = _weaponRenderer.flipX = !_facingRight;
  ```

  을 아래와 같이 부호만 단순화한다.

  ```csharp
  _bodyRenderer.flipX = _headRenderer.flipX = _weaponRenderer.flipX = _facingRight;
  ```

- 회전 각도(`aimAngle`) 계산 로직은 이번 수정과 무관하며 그대로 유지한다(벡터 대수 검증 결과 문제 없음이 research.md에서 확인됨).
- Body/Head/Weapon 세 파츠 모두 동일한 값을 대입하는 기존 구조도 그대로 유지하며, 이번 수정은 우변 부호(`!_facingRight` → `_facingRight`)만 바꾸는 것이다.

### 실제 코드 변경 필요 사항 (아직 미반영)

- `Assets/_Project/Scripts/Character/CharacterAimController.cs`의 `Update()`에서 `flipX` 대입 줄의 `!_facingRight`를 `_facingRight`로 바꾸는 수정이 필요하다.
- 이 문서 수정 시점에는 코드에 아직 반영되지 않았으며, 이번 문서 수정 다음 별도로 dev 에이전트에게 위임해 진행할 예정이다.

---

## 수정 4 — 무기 스프라이트 피벗 재조정 및 Weapon local position 보정

research.md의 "재검토 4 — 무기 회전 피벗이 캐릭터 중심이 아닌 스태프 자체 중심이었던 문제" 섹션에서 정리된 대로, 무기 스프라이트의 피벗이 스태프 밑동이 아니라 그래픽 중간 지점에 위치해 있어 회전 시 캐릭터 중심이 아닌 스태프 자체를 축으로 도는 것처럼 보이는 문제가 확인되어, 이를 바로잡기 위한 구현 지침을 아래와 같이 추가한다.

### 구현 지침 (확정된 수정 사항)

- `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta`의 스프라이트 피벗(`spritePivot`, `spriteSheet.sprites[0].pivot`)을 기존 `{x: 0.39, y: 0.43}`에서 `{x: 0.09, y: 0.12}`(스태프 밑동/손잡이 쪽 좌표, 픽셀 스캔 기반 측정값)로 변경한다.
- 피벗 변경으로 rest-pose(회전 0도) 렌더링 위치가 달라지므로, 기존 시각적 정렬을 유지하기 위해 Weapon GameObject의 local position을 보정한다. 계산식: `보정 position = spriteSize(unit) × (신규피벗 - 기존피벗)`, `spriteSize = (0.64, 1.16)`(`spritePixelsToUnits = 100` 기준), 결과 ≈ `(-0.192, -0.36, 0)`.
- `Assets/_Project/Scripts/Editor/CharacterLaunchOrbitSetupEditor.cs`에서 Weapon 생성 시 부여하는 local position을 `Vector3.zero`에서 `(-0.192, -0.36, 0)`으로 변경한다.

### 주의

- 피벗 `{0.09, 0.12}`와 보정 position `(-0.192, -0.36, 0)`은 픽셀 분석 기반 추정치이며, 실제 Unity Editor에서 사용자가 시각적으로 확인 후 미세 조정이 필요할 수 있다.

### 정정 — 스프라이트 폭 계산 오류 수정

위 "구현 지침" 계산식에서 스프라이트 폭을 64px로 가정했으나, 실제 sprite rect 폭은 59px(우측 5px는 투명 트리밍됨)로 확인되어 보정 position 값에 오차가 있었다. 정정된 계산은 다음과 같다.

- `size = (59/100, 116/100) = (0.59, 1.16)` (기존 문서엔 `(0.64, 1.16)`로 잘못 기재되어 있었음)
- `deltaPivot = (0.09-0.39, 0.12-0.43) = (-0.30, -0.31)`
- `보정 localPosition = (0.59 × -0.30, 1.16 × -0.31) = (-0.177, -0.3596)` ≈ **`(-0.177, -0.36, 0)`**

따라서 위 "구현 지침"과 "실제 코드/에셋 변경 필요 사항"에 등장하는 보정 position 값 `(-0.192, -0.36, 0)`은 `(-0.177, -0.36, 0)`으로 대체되어야 한다. 피벗 값 `{0.09, 0.12}` 자체는 변경 없이 그대로 유지한다.

### 실제 코드/에셋 변경 필요 사항 (아직 미반영)

- `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta`의 피벗 값 변경.
- `Assets/_Project/Scripts/Editor/CharacterLaunchOrbitSetupEditor.cs`의 Weapon local position 값 변경.
- 이 문서 수정 시점에는 코드/에셋에 아직 반영되지 않았으며, 이번 문서 수정 다음 별도로 dev 에이전트에게 위임해 진행할 예정이다.

---

## 수정 5 — `_facingRight` 초기값 정정 및 Weapon 위치 좌우 반전 추가

research.md의 "재검토 5 — flipX 부호 수정 반영 후 추가로 발견된 두 가지 문제" 섹션에서 정리된 대로, `flipX` 부호 수정(수정 3) 반영 과정에서 짝을 맞추지 못한 `_facingRight` 초기값 문제와, Weapon만 반전 시 부착 위치(로컬 X)도 함께 좌우로 이동해야 한다는 문제가 확인되어, 이를 반영하기 위한 구현 지침을 아래와 같이 추가한다.

### 구현 지침 1 — `_facingRight` 초기값 수정 (확정된 수정 사항)

- `CharacterAimController.cs`의 `_facingRight` 필드 초기값을 `true`에서 `false`로 변경한다.
- 이는 "수정 3"에서 확정한 `flipX` 판정 부호 변경(`!_facingRight` → `_facingRight`)과 짝을 맞추기 위한 것으로, 게임 시작 직후 조준 입력이 들어오기 전 첫 프레임에 캐릭터가 뒤집힌 포즈로 보이는 현상을 막기 위함이다.
- 반전 판정 로직 자체(데드존을 넘는 조준 입력이 있을 때만 `_facingRight`를 갱신하는 부분)는 이번 수정과 무관하며 그대로 유지한다.

### 구현 지침 2 — Weapon만 예외적으로 위치 좌우 반전 추가 (확정된 수정 사항)

- "수정 2"에서 확정했던 "Head/Body/Weapon 모두 반전 시 위치를 이동시키지 않고 `flipX`만 사용한다"는 원칙은 **Body/Head에는 그대로 유지**하되, **Weapon에는 예외를 둔다.**
- Weapon의 `localPosition`은 `_facingRight` 상태에 따라 X좌표의 부호만 반전시키고(예: `_weaponBasePosition.x * (부호)`, 부호는 `_facingRight`가 `true`/`false`일 때 각각 `+1`/`-1` 또는 그 반대 — 실제 원본 게임 기준(왼쪽 조준 시 왼쪽 어깨, 오른쪽 조준 시 오른쪽 어깨)과 맞는 방향으로 dev 에이전트가 구현 시 시각적으로 확인해 확정), Y좌표는 고정값(`_weaponBasePosition.y`)을 그대로 유지한다.
- Body/Head의 `localPosition`은 반전 여부와 무관하게 계속 고정값(각각 `_bodyBasePosition`/`_headBasePosition`)을 그대로 사용한다(수정 2에서 확정된 내용 변경 없음).
- 회전 로직(Weapon은 조준 방향을 그대로 따라가는 회전, Head는 감쇠된 회전, Body는 회전 없음)과 `flipX` 적용(수정 3에서 확정된 부호) 자체는 이번 수정과 무관하며 그대로 유지한다.
- 정확한 부호/수치는 dev 에이전트가 실제 코드 반영 시 원본 게임 참고 이미지와 대조해 시각적으로 확정한다.

### 실제 코드 변경 필요 사항 (아직 미반영)

- `Assets/_Project/Scripts/Character/CharacterAimController.cs`의 `_facingRight` 필드 초기값을 `false`로 변경.
- 같은 파일의 `Update()`에서 Weapon `localPosition`에 한해 `_facingRight`에 따른 X좌표 부호 반전 로직을 추가(Body/Head는 변경 없음).
- 이 문서 수정 시점에는 코드에 아직 반영되지 않았으며, 이번 문서 수정 다음 별도로 dev 에이전트에게 위임해 진행할 예정이다.
