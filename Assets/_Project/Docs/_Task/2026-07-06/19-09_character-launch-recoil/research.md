# Research — 캐릭터 볼 발사 반동 추가

`TODO.md` 4번 항목("캐릭터 볼 발사 반동 추가")의 구현 착수를 위해, 발사 시점을 캐릭터 쪽에 전달할 이벤트 설계와 반동 연출을 적용할 대상 Transform을 다시 코드/프리팹 기준으로 확인한다. 사용자가 확정한 목표는 "무기만이 아니라 `CharacterAimView`가 붙은 캐릭터 루트 오브젝트 전체가 발사 순간 살짝 밀렸다가 복귀하는 펀치성 반동"이다.

## 현재 상태

### `CharacterAimView.cs` (`Assets/_Project/Scripts/Character/CharacterAimView.cs`)
- `MonoBehaviour`이며 이벤트 구독이 전혀 없다. `Update()`에서 매 프레임 `BallLauncher.Instance.LaunchDirection`을 폴링해 `UpdateAim(Vector2 direction)`을 호출한다(파일 상단 주석에 따르면, `InputHandler.OnDrag`는 터치가 없을 때 발행되지 않아 이벤트 구독 방식으로는 회전이 멈추는 문제가 있어 의도적으로 폴링 방식을 택했다).
- `UpdateAim()`이 실제로 건드리는 Transform은 두 곳뿐이다.
  - `transform.localScale`: 컴포넌트가 붙은 **자기 자신(= 캐릭터 루트)**을 좌우 반전(`(-1,1,1)` / `(1,1,1)`)한다.
  - `_weaponPivot.localRotation`, `_headTransform.localRotation`: 무기 피벗과 머리만 회전시킨다.
- 발사 시점(펀치성 반동의 트리거)에 반응하는 로직은 코드 어디에도 없다. 즉 현재는 발사가 일어나도 캐릭터 쪽에서는 아무 일도 일어나지 않는다.
- `[SerializeField]`로 `_bodySpriteRenderer`, `_headSpriteRenderer`, `_headTransform`, `_weaponPivot`, `_headRotationRatio`, `_horizontalBiasDegrees`가 노출되어 있다. `_bodySpriteRenderer`는 현재 코드에서 실제로 읽거나 쓰지 않는다(참조만 캐싱된 상태, 향후 확장 여지로 보이나 현재 미사용).

### `BallLauncher.cs` (`Assets/_Project/Scripts/Ball/BallLauncher.cs`)
- 싱글톤(`Singleton<BallLauncher>`)이며 `LaunchDirection`(현재 조준 방향)과 `LaunchPoint`(발사 지점 `Transform`)를 공개 프로퍼티로 노출한다.
- 볼 발사가 실제로 일어나는 지점은 두 곳이다.
  - `LaunchRosterEntry(BallRosterEntry entry, Vector2 direction)` (104행): 최초 로스터 발사(`CoInitializeRoster`)와 신규 볼 로스터 합류(`AddBallToRoster`)에서 호출되며, 내부에서 `entry.Ball.Launch(direction)`을 호출한다.
  - `RelaunchQueuedBall(Ball ball)` (159행): 귀환한 볼을 큐에서 꺼내 재발사할 때(`CoRelaunchQueuedBalls`) 호출되며, 내부에서 `ball.Launch(_launchDirection)`을 호출한다.
  - (참고로 `CoLaunchRosterClones()`/`LaunchSubBalls()`도 각각 `clone.Launch(...)`, `ball.Launch(randomDir)`을 호출하지만, 이들은 분신볼/서브볼이며 캐릭터 본체가 직접 쏘는 발사가 아니다. 사용자가 말한 "캐릭터 전체 반동"이 이 두 경로까지 포함하는지는 범위 확인이 필요하다 — 아래 결론 참고.)
- 발사 시점을 외부에 알리는 공개 이벤트는 현재 하나도 없다. 기존에 정의된 유일한 static event는 `OnAllBallsReturned`(활성 볼이 0개가 됐을 때 발행, 247행)뿐이며, 발사(`Launch`) 관련 이벤트는 없다.
- 이벤트 구독/구독 해제 패턴은 이미 `OnEnable`/`OnDisable`에서 `InputHandler.OnDrag`, `GameManager.OnGameStateChanged`를 각각 `+=`/`-=`로 짝지어 처리하는 기존 관례가 있다(57~67행).

### 캐릭터 프리팹/씬 구조 (`CharacterSetupEditor.cs`, `Assets/_Project/Prefabs/Character/Character.prefab`)
- `Character.prefab`의 계층 구조는 다음과 같다.
  ```
  Character (root)              ← CharacterAimView 부착, transform.localScale로 좌우 반전
  ├─ Body (SpriteRenderer)
  ├─ Head (SpriteRenderer)      ← _headTransform
  └─ WeaponPivot                ← _weaponPivot
     └─ Weapon (SpriteRenderer)
  ```
- `CharacterAimView`는 `Character` 루트 GameObject에 부착되고, `_bodySpriteRenderer`/`_headSpriteRenderer`/`_headTransform`/`_weaponPivot` 참조가 모두 이 프리팹 생성 시점에 `SerializedObject`로 연결된다(`CharacterSetupEditor.CreateCharacterPrefab()`, 84~90행).
- 씬 배치: `Character.prefab` 인스턴스는 `LaunchPoint` GameObject의 자식으로 배치되며 `localPosition = (0, -0.4, 0)`이다(`PlaceCharacterInScene()`, 112~141행). 즉 `BallLauncher._launchPoint`(발사 지점 Transform) 자신이 아니라 그 자식이 캐릭터 루트다.
- 결론적으로 "캐릭터 루트"는 곧 `CharacterAimView`가 붙어 있는 `Character` GameObject의 `transform` 그 자체이며, `CharacterAimView.transform`(= `this.transform`)을 조작하면 된다. 이미 `UpdateAim()`이 `transform.localScale`을 좌우 반전에 쓰고 있으므로, 반동에 `transform.localPosition`(또는 `DOPunchPosition`/`DOPunchScale`)을 추가로 사용해도 좌우 반전 로직과 직접 충돌하지 않는다(반전은 `localScale`, 반동은 `localPosition` 또는 별도 스케일 펀치로 분리 가능).

### DOTween 사용 현황
- 프로젝트에 DOTween(`DG.Tweening`)이 이미 도입되어 있고 여러 곳에서 사용 중이다: `Assets/_Project/Scripts/UI/DamageTextFx.cs`(`DOMoveY`, `DOFade`, `Sequence`), `Assets/_Project/Scripts/Monster/MonsterBase.cs`(바닥 도달 돌진 연출에서 `DOTween.Sequence()`, `transform.DOMove(...)`), `ResultPanel.cs`, `SkillSelectionPanel.cs`, `HUDPanel.cs`에서도 사용.
- 별도의 수동 코루틴 기반 트윈 유틸은 없으며, DOTween의 `DOPunchPosition`/`DOPunchScale`처럼 "밀렸다가 복귀"하는 펀치 계열 API를 그대로 쓰는 것이 기존 컨벤션과 가장 잘 맞는다.
- `DamageTextFx.OnSpawn()`/`OnDespawn()`에서 `DOTween.Kill(transform)`으로 진행 중이던 트윈을 정리하는 패턴이 이미 존재한다(오브젝트 풀링 대상이 아닌 캐릭터에는 직접 해당되지 않지만, 연속 발사 시 트윈이 겹치는 문제를 다룰 때 참고할 수 있는 기존 관례다).

## 관련 파일 및 의존성

| 파일 | 역할 |
|---|---|
| `Assets/_Project/Scripts/Character/CharacterAimView.cs` | 캐릭터 루트 좌우 반전 + 무기/머리 회전. 반동 로직 추가 대상. |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 볼 발사 트리거 지점(`LaunchRosterEntry`, `RelaunchQueuedBall`, `CoLaunchRosterClones`, `LaunchSubBalls`). 발사 이벤트 발행 위치 후보. |
| `Assets/_Project/Prefabs/Character/Character.prefab` | 캐릭터 루트/자식 계층 구조. 반동 대상 Transform이 정확히 무엇인지 결정하는 근거. |
| `Assets/_Project/Scripts/Editor/CharacterSetupEditor.cs` | 프리팹 생성 로직 원본(계층 구조 근거, 직접 수정 대상은 아님). |
| DOTween(`DG.Tweening`) | 이미 프로젝트에 도입된 트윈 라이브러리. 반동 구현에 재사용. |

## 문제점 / 구현 대상 파악

1. **발사 시점을 알릴 이벤트가 없다.** `BallLauncher`에 `public static event Action OnBallLaunched`(또는 유사한 이름)를 신규 추가하고, `LaunchRosterEntry()`/`RelaunchQueuedBall()` 내부에서 `ball.Launch(direction)` 직후 발행해야 한다. `CoLaunchRosterClones()`(분신볼)와 `LaunchSubBalls()`(서브볼)까지 같은 이벤트로 묶을지는 "캐릭터가 직접 쏘는 발사"의 범위를 어디까지로 볼지에 달려 있었으나, 사용자가 "모든 볼 발사에 반동을 준다"고 확정했으므로(2026-07-06) 분신볼/서브볼도 포함해 4곳 모두에서 이벤트를 발행하는 것으로 확정됐다(plan.md 참고).
2. **반동을 실행할 구독 주체가 없다.** `CharacterAimView`는 현재 이벤트를 구독하지 않고 폴링만 한다. DevRules.md의 이벤트 규칙(구독/해제는 `OnEnable`/`OnDisable` 쌍)에 따라, `CharacterAimView`에 `OnEnable()`/`OnDisable()`을 새로 추가해 `BallLauncher.OnBallLaunched`를 구독/해제하고, 콜백에서 반동 트윈을 재생하는 방식이 기존 `Update()` 폴링 로직과 공존 가능하다(폴링은 조준 회전을 계속 담당하고, 이벤트 구독은 발사 순간의 1회성 펀치 연출만 담당하므로 책임이 겹치지 않는다).
3. **반동 대상 Transform과 방식.** `CharacterAimView.transform`(= `Character` 루트)의 `localPosition`을 짧게 밀었다가 복귀시키는 것이 사용자가 말한 "캐릭터 전체 반동"과 가장 부합한다. DOTween의 `DOPunchPosition`(밀림 후 감쇠하며 원위치 복귀) 또는 `DOPunchScale`을 사용하면 별도 원위치 저장/복귀 로직 없이 한 줄로 처리 가능하다. 다만 `transform.localScale`이 이미 좌우 반전(`-1/1`)에 쓰이고 있으므로, `DOPunchScale`을 쓸 경우 반전 부호와 곱연산이 겹치지 않도록 주의가 필요하다(반전이 뒤집혀 있을 때 펀치 스케일 값의 부호 처리 확인 필요) — 이 문제를 피하려면 `DOPunchPosition`(로컬 좌표 이동)이 더 안전한 선택으로 보인다.
4. **반동 방향.** 사용자가 아직 구체적 방향을 확정하지 않았다. 발사 방향(`LaunchDirection`)의 반대쪽(뒤로 밀리는 반동, 총기 반동과 유사)이 물리적으로 가장 자연스러워 보이나, 캐릭터가 좌우 반전되는 아트 특성상 반전 상태에서 반동 벡터의 부호를 어떻게 보정할지 결정이 필요하다(`UpdateAim()`의 `mirrored` 처리와 동일한 보정 방식을 재사용할 수 있음).

## 결론

- 반동 대상은 `CharacterAimView`가 부착된 `Character` 프리팹 루트(`CharacterAimView.transform`) 그 자체이며, `localPosition`을 짧게 밀었다가 복귀시키는 DOTween 펀치 트윈(`DOPunchPosition` 등)으로 구현 가능하다.
- `BallLauncher`에 발사 시점 이벤트(`OnBallLaunched` 등, 정확한 이름/시그니처는 plan.md에서 결정)를 신규 추가하고, `LaunchRosterEntry()`/`RelaunchQueuedBall()`(최소 범위) 직후 발행하는 방식이 DevRules.md의 이벤트 규칙 및 기존 코드 패턴과 가장 잘 맞는다.
- `CharacterAimView`는 기존 `Update()` 폴링 로직(조준 회전)을 그대로 유지한 채, `OnEnable`/`OnDisable`에서 새 이벤트를 구독/해제해 반동 트윈만 추가로 재생하는 방식으로 확장하면 기존 구조를 해치지 않고 구현할 수 있다.
- 확정이 필요한 모호한 지점은 plan.md의 주의사항에 정리한다: (1) 분신볼/서브볼 발사도 반동 이벤트에 포함할지, (2) 반동 강도/지속시간의 구체적 수치, (3) 반동 방향(발사 반대 방향 여부와 좌우 반전 보정 방식).
