# Plan — 캐릭터 볼 발사 반동 추가

`research.md`에서 파악한 구조를 바탕으로, `BallLauncher`에 발사 시점 이벤트를 추가하고 `CharacterAimView`가 이를 구독해 캐릭터 루트가 발사 순간 살짝 밀렸다가 원위치로 복귀하는 펀치성 반동을 재생하도록 구현할 계획이다. DOTween을 사용한다(프로젝트에 이미 도입되어 있음).

## 사용자 확정 사항 (2026-07-06)

1. **모든 볼 발사에 반동을 적용한다.** 로스터 노말/특수볼(`LaunchRosterEntry()`/`RelaunchQueuedBall()`)뿐 아니라 분신볼(`CoLaunchRosterClones()`)과 서브볼(`LaunchSubBalls()`)의 발사도 전부 `OnBallLaunched` 이벤트 발행 대상에 포함한다. 한 메서드 안에서 볼이 여러 개 발사되면(예: `LaunchSubBalls()`가 여러 발을 쏘는 경우) 발사마다 이벤트를 한 번씩 발행한다.
2. **반동 방향은 발사 방향의 반대쪽(총기 반동처럼)으로 확정한다.**
3. **반동 강도/지속시간은 적정 초기값을 넣고 Inspector(`[SerializeField]`)에서 조정 가능하게 한다.**

## 구현 목표

- 볼이 발사되는 순간(`BallLauncher`의 `Launch()` 호출 직후), `CharacterAimView`가 붙은 캐릭터 루트 오브젝트 전체가 짧게 밀렸다가 자동으로 원위치로 복귀하는 펀치성 반동 연출을 추가한다.
- 무기(`WeaponPivot`)만이 아니라 캐릭터 루트(`Character`, `CharacterAimView.transform`) 자체가 움직여야 한다.
- 기존 조준 회전/좌우 반전 로직(`Update()` 폴링 기반)은 그대로 유지하고, 발사 반동은 이벤트 구독 기반으로 별도 추가한다(두 로직이 서로 방해하지 않도록 분리).

## 단계별 작업 계획

### 1. `BallLauncher`에 발사 이벤트 추가
- `public static event Action OnBallLaunched;`를 신규 추가한다(기존 `OnAllBallsReturned`와 동일한 static event 패턴).
- 이벤트 발행 위치: 볼이 실제로 `Launch(...)`를 호출하는 4곳 전부에서, 각 `Launch(...)` 호출 직후 `OnBallLaunched?.Invoke();`를 호출한다.
  - `LaunchRosterEntry(BallRosterEntry entry, Vector2 direction)` — `entry.Ball.Launch(direction)` 직후
  - `RelaunchQueuedBall(Ball ball)` — `ball.Launch(_launchDirection)` 직후
  - `CoLaunchRosterClones(List<BallRosterEntry> originals, int returnCount)` — 반복문 안 `clone.Launch(_launchDirection)` 직후(분신볼 각각 발사마다 1회씩)
  - `LaunchSubBalls(Vector2 origin, int count, float damage, Sprite sprite)` — 반복문 안 `ball.Launch(randomDir)` 직후(서브볼 각각 발사마다 1회씩)
- 모든 볼 발사(로스터 노말/특수볼, 분신볼, 서브볼)에 반동을 적용하기로 확정했으므로 예외 없이 4곳 모두에서 발행한다.
- 이벤트는 매개변수 없이 발사가 일어났다는 사실만 알린다(방향은 이미 `BallLauncher.LaunchDirection` 프로퍼티로 조회 가능하므로 이벤트 인자로 중복 전달하지 않는다).

### 2. `CharacterAimView`가 이벤트를 구독해 반동 재생
- `OnEnable()`/`OnDisable()`을 신규 추가해 `BallLauncher.OnBallLaunched += HandleBallLaunched;` / `-= HandleBallLaunched;`로 구독/해제한다(DevRules.md 이벤트 규칙 준수).
- `HandleBallLaunched()`에서 DOTween 펀치 트윈을 재생해 캐릭터 루트(`transform`)의 `localPosition`을 짧게 밀었다가 복귀시킨다(`DOPunchPosition` 사용 예정 — 별도 원위치 저장/복귀 코드 없이 자동으로 원위치로 돌아옴).
- 연속 발사 시(로스터 발사 간격 `0.1초`) 트윈이 겹칠 수 있으므로, 트윈 시작 전에 `DOTween.Kill(transform)`으로 이전 반동 트윈을 정리한 뒤 새로 재생한다(`DamageTextFx.OnSpawn()`의 기존 관례와 동일한 방식).
- 반동 방향은 `BallLauncher.Instance.LaunchDirection`의 반대 방향(뒤로 밀리는 반동)을 기준으로 하되, 캐릭터가 `transform.localScale.x`로 좌우 반전되어 있을 때는 `UpdateAim()`이 이미 하고 있는 것과 동일한 방식으로 로컬 좌표계 기준 부호를 보정한다.
- 반동 강도(밀리는 거리)와 지속시간은 `[SerializeField] private float _recoilPunchStrength`, `[SerializeField] private float _recoilDuration` 등으로 Inspector에서 조정 가능하게 노출한다(DevRules.md의 SerializeField 규칙 — Inspector 조정이 필요한 값은 `[SerializeField] private`).

### 3. 반동 방향/강도/지속시간 확정
- 방향: 발사 방향의 반대쪽(총기 반동처럼)으로 확정.
- 강도/지속시간: 구체적 수치는 착수 시 임의의 초기값(`_recoilPunchStrength` 0.15f 내외, `_recoilDuration` 0.2f 내외)으로 시작하고, Inspector 노출 필드이므로 이후 플레이 확인하며 값만 조정한다.

## 예상 변경/생성 파일 목록

| 파일 | 변경 내용 |
|---|---|
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | `OnBallLaunched` static event 추가, `LaunchRosterEntry()`/`RelaunchQueuedBall()`/`CoLaunchRosterClones()`/`LaunchSubBalls()` 4곳 모두에서 발행 |
| `Assets/_Project/Scripts/Character/CharacterAimView.cs` | `OnEnable()`/`OnDisable()` 추가(이벤트 구독/해제), 반동 재생 메서드 및 관련 `[SerializeField]` 필드 추가, `using DG.Tweening;` 추가 |

프리팹(`Character.prefab`)이나 씬 파일은 코드 변경만으로 충분하며(신규 필드는 기본값으로 직렬화되고 Inspector에서 나중에 조정 가능), 별도 수정이 필요하지 않을 것으로 예상한다. 단, Unity 에디터에서 기존 `Character.prefab` 인스턴스에 새 `[SerializeField]` 필드의 기본값을 확인/조정하는 작업은 필요할 수 있다.

## 주의사항

- DevRules.md 네이밍 컨벤션 준수: private 필드는 `_camelCase`, `[SerializeField]`도 `_camelCase`, 메서드는 PascalCase.
- DevRules.md "단순함 우선" 원칙에 따라 별도의 반동 전용 컴포넌트/추상 클래스를 새로 만들지 않고, 기존 `CharacterAimView`에 메서드 하나와 필드 몇 개만 추가하는 최소 범위로 구현한다.
- DevRules.md 이벤트 규칙에 따라 구독/해제는 반드시 `OnEnable`/`OnDisable` 쌍으로 관리한다.
- 아래 3개 항목은 모두 사용자 확정 완료(2026-07-06):
  1. **반동 강도/지속시간의 구체적 수치** — `_recoilPunchStrength`(0.15f 내외), `_recoilDuration`(0.2f 내외)로 Inspector 조정 가능하게 노출하는 것으로 확정. 이후 플레이 확인하며 값만 조정하면 된다.
  2. **분신볼/서브볼도 반동 대상에 포함할지** — 포함 확정. 로스터 노말/특수볼뿐 아니라 분신볼(`CoLaunchRosterClones`)·서브볼(`LaunchSubBalls`)도 모두 반동 트리거에 포함한다.
  3. **반동 방향** — 발사 방향의 반대쪽(뒤로 밀림, 총기 반동처럼)으로 확정.
- 빌드 오류 예상: 없음. `BallLauncher`에 이벤트만 추가하고 기존 메서드 시그니처는 변경하지 않으므로 다른 스크립트에 미치는 영향은 없을 것으로 예상한다. `CharacterAimView`에도 필드/메서드 추가만 있고 기존 필드·동작은 그대로 유지한다.

**사용자 확정 완료(2026-07-06). 본 plan.md 기준으로 실제 C# 코드 구현을 진행한다.**
