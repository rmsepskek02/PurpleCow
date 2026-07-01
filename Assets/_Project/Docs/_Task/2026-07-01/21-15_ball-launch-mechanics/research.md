# Research — Ball Launch Mechanics

`GameplayMechanics.md` 섹션 1(볼 발사 및 궤도 시스템)에 확정된 원본 게임의 조준/발사/충돌/귀환/재발사 사이클을 실제로 재구현하기 전, 현재 `InputHandler`/`BallLauncher`/`Ball` 및 관련 스크립트가 실제로 어떻게 동작하는지 코드 기준으로 조사한 문서입니다. 궤적 프리뷰 관련 기존 컴포넌트가 프로젝트에 정말 존재하지 않는지도 함께 확인했습니다. 이후 사용자와의 추가 논의를 통해 (1) 시작 볼 개수 및 특수볼 획득 시 볼 개수/로스터가 어떻게 변화하는지, (2) 이 로스터 개념이 현재 `SkillManager`의 액티브 스킬 슬롯 구조와 어떻게 다른지, (3) 몬스터 하강이 볼 발사/귀환 사이클과 실제로는 무관한 시간 기반 로직이라는 점까지 조사 범위를 확장했다. 이번 문서는 조사와 문제점 매핑까지만 다루며, 구현 방법(plan.md)은 포함하지 않는다.

---

## 현재 상태

### InputHandler.cs (`Assets/_Project/Scripts/Core/InputHandler.cs`)

- `Singleton<InputHandler>`로 구현된 입력 전담 클래스. `Update()`에서 매 프레임 터치스크린(`Touchscreen.current`) 또는 마우스(`Mouse.current`) 입력을 폴링한다.
- 터치의 경우 `TouchPhase.Began`이면 `pressedPos`, `Moved`/`Stationary`면 `currentPos`, `Ended`/`Canceled`면 `released = true`로 판정한다. 마우스도 `wasPressedThisFrame`/`isPressed`/`wasReleasedThisFrame`으로 동일하게 대응한다.
- `pressedPos`가 있으면 `_dragStartPosition`을 기록하고 `_isDragging = true`로 드래그 상태를 시작한다.
- `currentPos`가 있고 `_isDragging` 중이면, `(currentPos - _dragStartPosition).normalized`를 방향 벡터로 계산해 `OnDrag` 이벤트를 매 프레임 발행한다. 즉 방향은 "터치 시작 지점 → 현재 지점"의 상대 벡터이며, 화면 좌표를 그대로 사용해 정규화한 값이다.
- `released`이고 `_isDragging` 중이었으면 `_isDragging = false`로 초기화하고 `OnRelease` 이벤트를 1회 발행한다.
- 이 클래스는 `Action<Vector2> OnDrag`와 `Action OnRelease` 두 개의 static 이벤트만 외부에 노출한다. "터치 시작(Began) 시점" 자체를 알리는 이벤트는 없다 — `OnDrag`는 드래그(Moved/Stationary) 상태에서만 발행되고, `Began` 프레임에는 `_dragStartPosition`만 기록될 뿐 별도 이벤트가 나가지 않는다.

### BallLauncher.cs (`Assets/_Project/Scripts/Ball/BallLauncher.cs`)

- `Singleton<BallLauncher>`. `_ballPool`(`ObjectPool<Ball>`)을 `Awake()`에서 생성하고, `OnEnable`/`OnDisable`에서 `InputHandler.OnDrag`, `InputHandler.OnRelease`, `GameManager.OnGameStateChanged`를 구독/해제한다.
- `HandleDrag(Vector2 direction)`: `_launchDirection` 필드에 최신 방향을 그대로 저장만 한다. 궤적 미리보기나 시각적 피드백 처리는 전혀 없다.
- `HandleRelease()`: `_canLaunch`(게임 상태가 `Playing`일 때만 true)가 true이면 `LaunchBall()`을 호출한다. 즉 **손을 뗄 때(Release) 딱 1번** 발사가 일어나는 구조이며, 이 발사가 끝나면 다음 `OnRelease`(= 다음 드래그+릴리즈 사이클)가 오기 전까지 더 이상 볼이 나가지 않는다.
- `LaunchBall()`: 풀에서 `Ball`을 꺼내 `_launchPoint.position`으로 옮기고 `ball.Launch(_launchDirection)` 호출, `SkillManager.Instance.ApplySkillToBall(ball)`로 스킬 적용, `_activeBallCount++`.
- `LaunchSubBalls(origin, count, damage)`: 클러스터볼 스킬(`ClusterBallSkill`) 등에서 추가로 볼을 뿌릴 때 쓰는 별도 메서드. 랜덤 방향(단, y<0이면 뒤집어 위쪽으로만 나가게 보정)으로 발사하며, 조준 방향과는 무관하다. 이번 재설계 대상인 "메인 발사 사이클"과는 다른 보조 기능이다.
- `ReturnBall(Ball ball)`: `Ball` 쪽에서 호출되는 콜백. 풀에 반환하고 `_activeBallCount--`. 카운트가 0이 되면 `OnAllBallsReturned` 이벤트를 발행한다 (이 이벤트는 `WaveManager`가 몬스터를 한 칸 내리는 트리거로, `HUDPanel`이 조준 인디케이터를 끄는 트리거로 사용 중).
- **재발사 로직이 전혀 없다.** `ReturnBall()`이 호출돼도 볼을 다시 `Launch()`하는 코드가 없고, 단순히 풀에 반환되어 비활성화될 뿐이다. "캐릭터로 돌아온 볼이 최신 조준 방향으로 자동 재발사된다"는 요구사항에 대응하는 코드는 존재하지 않는다.
- `_canLaunch`는 게임 상태에 따라 발사 가능 여부만 제어할 뿐, 사이클 반복이나 "발사 중 여러 개 볼이 동시에 날아다니는" 상황에 대한 별도 상태 관리는 없다.

### Ball.cs (`Assets/_Project/Scripts/Ball/Ball.cs`)

- `IPoolable` 구현체. `OnSpawn()`에서 `_remainingBounces = _ballData.MaxBounces`로 초기화하고 속도를 0으로 리셋, `OnDespawn()`에서도 속도 0 및 스킬 리스트 정리.
- `Launch(Vector2 direction)`: `LaunchDirection`을 기록하고 `_rigidbody.linearVelocity = direction * _ballData.Speed`로 즉시 속도를 부여한다. 이후 발사 방향/속도를 바꾸는 다른 공개 메서드는 없다.
- `FixedUpdate()`: 매 물리 프레임 `_rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * speed`로 **속도 크기를 강제로 항상 일정하게 유지**한다. 이는 GameplayMechanics.md의 "볼의 이동 속도는 조준/조작과 무관하게 항상 일정하다" 요구사항과 이미 일치하는 부분이다. 물리 엔진(Rigidbody2D)의 충돌 반사에 의한 방향 전환은 그대로 두고 크기만 보정하는 방식이라, 벽/몬스터 충돌 시 Unity 2D 물리엔진의 기본 반사(`OnCollisionEnter2D` 자체는 막지 않음)에 의존해 방향이 바뀐다.
- `OnCollisionEnter2D`: 태그별로 분기.
  - `"Monster"`: `CalculateDamage`로 데미지 계산 후 `OnHitMonster` 발행, 볼의 현재 속도 y부호로 전면/후면 판정(`OnHitMonsterFront`/`OnHitMonsterBack`), 장착된 `BallSkillBase` 목록에 `OnBallHit` 통지. **볼이 몬스터에 맞아도 사라지거나 멈추지 않고, 물리 반사된 방향으로 계속 날아간다** (반환 로직 없음).
  - `"Wall"`: `OnWallHit` 발행 후 `_remainingBounces--`. 남은 반사 횟수가 0 이하가 되면 `ReturnToPool()` 호출. 즉 현재 코드는 "벽에 정해진 횟수만큼 튕기면 풀로 반환"되는 구조이며, GameplayMechanics.md가 요구하는 "화면 하단에서 튕겨 캐릭터 위치로 귀환" 동작과는 다르다 — 현재는 귀환 이동 연출이나 위치 이동 없이 그냥 비활성화될 뿐이다.
  - `"Ground"`: 무조건 `ReturnToPool()`. 이 태그가 "화면 하단"에 대응하는 것으로 보이나, 여기서도 캐릭터 위치로 돌아가는 이동 로직 없이 즉시 반환된다.
- `OnTriggerEnter2D`: Ghost 스킬(`GhostBallSkill`) 장착 시(`_collider.isTrigger = true`) 몬스터를 트리거로 통과하며 데미지만 주는 특수 케이스. 이번 재설계와는 직접 관련 없음(참고 사항).
- `ReturnToPool()`: `BallLauncher.Instance.ReturnBall(this)` 호출 후 끝. 재발사를 트리거하는 코드는 없다.

### SkillManager.cs (`Assets/_Project/Scripts/Skill/SkillManager.cs`) — 볼과 스킬의 결합 방식

(사용자 확인: 시작 볼 개수 및 특수볼 획득 방식 관련 추가 조사)

- 사용자 확인 사항: 게임은 **노말볼 5개**로 시작하며, 웨이브 클리어 후 3택지(로그라이크 스킬 선택 화면, `SkillSelectionPanel.cs`)에서 **신규 특수볼 타입을 선택하면 볼 개수가 1개 늘어난다**(예: 노말볼 5 + 파이어볼 1 = 총 6개가 동시에 독립적으로 순환). 이미 보유한 타입을 다시 선택하면 개수가 아니라 **해당 특수볼의 레벨이 상승**한다.
- 그런데 현재 코드의 `BallLauncher.LaunchBall()`(BallLauncher.cs:56)은 풀에서 `Ball` 인스턴스를 하나 꺼낸 뒤 `SkillManager.Instance.ApplySkillToBall(ball)`을 호출하고, `ApplySkillToBall(Ball ball)`(SkillManager.cs:55)은 현재 장착된 액티브 스킬 전체(`_activeSkills`, 최대 4종)를 그 볼 1개에 매번 새로 적용한다. 즉 **볼 자체에는 타입 정체성이 없고**, "파이어볼"이라는 것은 실체가 아니라 "볼에 파이어 스킬이 붙어 있는 상태"에 불과하다.
- `SkillManager.EquipActiveSkill(BallSkillBase skill)`(SkillManager.cs:25)에는 이미 "`_activeSkills`에 같은 `SkillId`를 가진 스킬이 있으면 `SkillData.LevelUp()`만 호출하고 반환, 없으면 신규로 `Add`(단, 최대 4종 제한)"라는 로직이 존재한다. 이는 사용자가 설명한 "신규 타입 = 개수 증가 / 기존 타입 = 레벨업" 규칙과 표면적으로 유사해 보이지만, 이는 **"장착 가능한 액티브 스킬 슬롯"을 최대 4개까지 관리하는 로직**이며, "N개의 볼 각각이 영구적으로 자신의 타입/레벨을 유지하는 로스터"를 관리하는 로직이 아니다. 스킬 슬롯 수(최대 4)와 볼 개수(시작 5개 + 특수볼 획득분)도 서로 다른 숫자 체계다.
- `Ball.OnSpawn()`/`OnDespawn()`(Ball.cs)은 스킬 리스트를 매번 초기화한다. 즉 지금 구조로는 특정 `Ball` 인스턴스가 "나는 영구히 파이어볼이다"라는 정체성을 유지할 방법이 없다 — 매 발사마다 그 시점의 `_activeSkills` 전체가 다시 덧씌워질 뿐이다.
- 결론적으로 "볼 개수/타입별 로스터"는 현재 `SkillManager`/`BallLauncher`/`Ball` 어디에도 대응하는 데이터 모델이 없으며, 이번 발사 메커닉 재설계가 반드시 포함해야 하는 별도의 구조적 축임을 확인했다.

### WaveManager.cs / MonsterData.cs — 몬스터 하강 방식

(사용자 확인: 몬스터 하강이 볼 사이클과 무관한 시간 기반 로직이라는 점 관련 추가 조사)

- 사용자 확인 사항: 몬스터는 "한 칸씩 내려오는" 개념이 아니라 **볼 발사/귀환 사이클과 완전히 무관하게, 시간 기반으로 서서히(continuous) 내려오는 형태**다.
- 그러나 현재 `WaveManager.cs`는 `OnEnable`에서 `BallLauncher.OnAllBallsReturned += HandleAllBallsReturned`(WaveManager.cs:45)를 구독하고, `HandleAllBallsReturned()`(WaveManager.cs:82)가 곧바로 `MoveAllMonstersDown()`(WaveManager.cs:88)을 호출한다. 즉 지금은 "활성 볼 카운트가 0이 되는 시점(=한 발이 나갔다가 완전히 반환된 시점)"을 트리거로 몬스터를 한 칸 내리는 구조이며, 사용자가 확인해준 실제 메커닉(시간 기반 연속 하강)과는 전혀 다르다.
- `MonsterData.cs`에는 `_moveSpeed` 필드와 `MoveSpeed` 프로퍼티(MonsterData.cs:7, 12)가 이미 존재하고 에디터 스크립트(`MonsterSetupEditor.cs:68`)에서 기본값 `1f`로 세팅되지만, 실제로 이 값을 읽어 몬스터를 이동시키는 코드는 프로젝트 어디에도 없다 — 시간 기반 하강 구현을 염두에 두고 미리 만들어졌으나 아직 쓰이지 않는 죽은 데이터로 확인된다.

### 정리 — 현재 사이클

```
터치 시작 → (조준 이벤트 없음, 내부 상태만 기록)
  → 드래그 중 매 프레임 OnDrag(방향) 발행 → BallLauncher._launchDirection 갱신
  → 손을 뗌 → OnRelease 1회 발행 → BallLauncher.LaunchBall() 1회 실행
    → Ball.Launch(방향) → 물리 충돌로 이동/반사
      → Wall N회 반사 소진 또는 Ground 충돌 → ReturnToPool() (그대로 종료, 재발사 없음)
```
즉 현재는 "한 번의 드래그+릴리즈 = 한 발의 볼이 나가고 끝나는" 단발 구조이며, 문서가 요구하는 "터치 즉시 조준 시작 → 궤적 프리뷰 → 발사 → 충돌 이동 → 귀환 → 최신 방향으로 재발사"의 반복 사이클과는 근본적으로 다르다. 위 다이어그램은 또한 암묵적으로 "풀에서 아무 볼이나 한 개 꺼내 쓰는" 상황을 전제로 하고 있어, 사용자가 확인해준 "노말볼 5개 + 특수볼 N개가 각자 타입/레벨을 유지한 채 동시에 순환"하는 로스터 개념과는 처음부터 전제 자체가 다르다는 점도 재설계 시 함께 고려해야 한다.

---

## 관련 파일 및 의존성

| 파일 | 역할 | 비고 |
|---|---|---|
| `Assets/_Project/Scripts/Core/InputHandler.cs` | 터치/마우스 입력을 읽어 `OnDrag`/`OnRelease` static 이벤트로 변환 | `Singleton<InputHandler>` |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 조준 방향 저장, 발사 트리거, 볼 오브젝트 풀 관리, 활성 볼 수 카운트 | `Singleton<BallLauncher>`, `InputHandler`/`GameManager` 이벤트 구독 |
| `Assets/_Project/Scripts/Ball/Ball.cs` | 개별 볼의 이동/충돌/데미지 계산/풀 반환 | `IPoolable` 구현, `BallData`(SO) 참조, `BallSkillBase` 리스트 보유 |
| `Assets/_Project/Scripts/Data/BallData.cs` | 볼 기본 스탯(데미지, 속도, 치명타 확률/배율, 최대 반사 횟수)을 담는 ScriptableObject | DevRules.md의 "볼 기본 스탯은 SO로 분리" 규칙 준수 상태 |
| `Assets/_Project/Scripts/Core/ObjectPool.cs` | 제네릭 오브젝트 풀 (`IPoolable` 제약) | `BallLauncher`가 `Ball` 전용으로 인스턴스화하여 사용 |
| `Assets/_Project/Scripts/Core/IPoolable.cs` | `OnSpawn`/`OnDespawn` 인터페이스 | `Ball`이 구현 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | `BallLauncher.OnAllBallsReturned`를 구독해 `HandleAllBallsReturned()`(WaveManager.cs:82) → `MoveAllMonstersDown()`(WaveManager.cs:88)으로 몬스터를 한 칸 내리고 게임오버 체크 | 재발사 로직이 생기면 "모든 볼이 반환된 시점"의 의미(사이클 종료 시점 등)가 바뀔 수 있어 영향 범위에 포함. 사용자 확인 사항(몬스터는 시간 기반으로 서서히 내려와야 함)과도 다른 방식이나, 시간 기반 하강 자체는 별도 task이며 이번 task에서는 로스터 도입 시 `OnAllBallsReturned`가 사실상 발생하지 않게 되는 부작용만 다룸 |
| `Assets/_Project/Scripts/UI/HUDPanel.cs` | `BallLauncher.OnAllBallsReturned`를 구독해 조준 인디케이터(`SetLaunchIndicatorVisible(false)`) 표시 여부 제어 | 마찬가지로 재발사 사이클 도입 시 인디케이터 on/off 타이밍 재검토 필요 |
| `Assets/_Project/Scripts/Skill/Active/ClusterBallSkill.cs` | `BallLauncher.Instance.LaunchSubBalls(...)` 호출 | 메인 발사(조준 방향) 사이클과는 별개의 보조 발사 경로 — 이번 재설계 범위 밖으로 보임(참고만) |
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 에디터 스크립트. `BallLauncher` 하위에 `LaunchPoint`(자식 Transform, `localPosition = (0, -8, 0)`)를 자동 생성/연결 | `_launchPoint`가 곧 "캐릭터 위치"로 추정되는 고정 지점임을 확인. 씬에 `LaunchPoint`가 캐릭터 오브젝트 자체가 아니라 `BallLauncher`의 자식 빈 오브젝트로 존재. 사용자 확인: 캐릭터는 화면 하단 중앙에 고정된 위치에 배치되어 이동하지 않으며(좌우 반전, 발사각에 따른 스태프 회전 등 시각적 연출만 있음), 따라서 `LaunchPoint`의 고정 좌표를 귀환 목표 좌표로 그대로 사용해도 무방함 |
| `Assets/_Project/Scripts/Skill/SkillManager.cs` | `ApplySkillToBall(Ball)`로 발사되는 볼 1개에 액티브 스킬(최대 4종) 전체를 매번 적용, `EquipActiveSkill()`로 신규 타입 추가/기존 타입 레벨업 판정 | 이번 재설계의 "N개 타입별 개별 볼 로스터" 요구사항과 정면으로 다른 "스킬 슬롯" 모델을 사용 중 — 아키텍처 재검토 대상 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | 웨이브 클리어 후 3택지 로그라이크 스킬 선택 UI. 선택 시 `SkillManager.EquipActiveSkill()` 등을 호출하는 것으로 추정 | 신규 특수볼 타입 선택 시 "볼 로스터에 새 볼 개체 추가"까지 실제로 연동되어야 하는 지점 — 이번 조사에서 전체 정독은 하지 않았으며 plan.md 단계에서 상세 확인 필요 |
| `Assets/_Project/Scripts/Data/MonsterData.cs` | `_moveSpeed`/`MoveSpeed` 필드 보유 | 현재 어디서도 참조되지 않는 죽은 데이터. 시간 기반 하강(별도 task) 구현 시 활용될 필드로 추정되나, 이번 task에서는 사용하지 않음 |

궤적 프리뷰(LineRenderer, Trajectory, AimIndicator 등) 관련 키워드로 `Assets/_Project` 전체를 검색한 결과, 일치하는 스크립트는 `GameplayMechanics.md` 문서 자체(요구사항 서술) 1건뿐이었다. 즉 **궤적 프리뷰를 그리는 기존 컴포넌트는 프로젝트에 전혀 존재하지 않는다** — 신규로 작성해야 하는 항목임을 코드 레벨에서 재확인했다.

`HUDPanel.cs`에는 `_launchReadyIndicator`/`_launchReadyCanvasGroup`(발사 준비 인디케이터로 추정)이 있으나, 이는 `SetLaunchIndicatorVisible(bool)`로 단순 표시/숨김만 하는 단일 아이콘/그룹으로 보이며 점선 궤적을 그리는 기능은 없다(이번 조사에서 `HUDPanel.cs` 전체 내용까지 라인 단위로 정독하지는 않았으므로, 표시 대상이 정확히 무엇인지는 plan.md 단계에서 필요 시 재확인 가능).

`GameManager.cs`는 이번 조사에서 `BallLauncher`/`Ball`이 참조하는 지점(`OnGameStateChanged`, `DamageMultiplierBonus`, `ConsumeNextShotDamageBonus`)만 확인했으며, 전체 구현은 이번 재설계의 핵심 대상이 아니므로 상세 정독은 생략했다. `SkillManager.cs`는 최초 조사 시에는 `ApplySkillToBall` 호출부만 확인했으나, 이후 사용자 확인 사항(볼 개수/로스터 모델)을 검증하는 과정에서 `EquipActiveSkill()`/`LevelUp()` 로직까지 추가로 정독했다(위 "SkillManager.cs — 볼과 스킬의 결합 방식" 절 참고).

---

## 문제점 / 구현 대상 파악

GameplayMechanics.md 섹션 1에 명시된 요구사항 항목별로 현재 코드 상태를 매핑하면 다음과 같다.

| 요구사항 | 현재 코드 상태 | Gap |
|---|---|---|
| 터치하는 **순간** 조준 방향이 즉시 정해진다 (드래그 후 릴리즈 시점이 아님) | `InputHandler`는 `Began`(터치 시작) 프레임에 `_dragStartPosition`만 내부에 기록하고 별도 이벤트를 발행하지 않는다. 조준 방향(`_launchDirection`)은 `OnDrag`가 최소 1프레임 이상 발행된 이후에야 `BallLauncher`에 반영된다 | "터치 시작" 자체를 알리는 이벤트/훅이 없다. 조준 시작 시점과 방향 결정 시점의 정의가 요구사항과 다르다 |
| 조준 중 점선 형태 궤적 프리뷰가 **1차 충돌 지점까지, 그리고 그 지점에서 반사된 방향으로 이어진 2차 충돌 지점까지** 2단계로 표시되고(3차 충돌 이후는 미표시), 2차 충돌 지점에는 빨간 점(레드닷)과 이를 감싸는 원형 궤적선(고리)이 표시됨 | 궤적을 그리는 컴포넌트 자체가 프로젝트에 없음(위 Grep 결과로 확인) | 전면 신규 구현 필요. Physics2D Raycast를 최소 2회 계산해야 한다 — ① 조준 방향으로 1차 충돌 지점을 구하고, ② 그 지점에서 반사된 방향으로 다시 Raycast하여 2차 충돌 지점을 구해야 한다. 또한 2차 충돌 지점에 마커 UI(빨간 점 + 원형 궤적선)를 그리는 로직도 별도로 필요함 |
| 드래그 시 궤적이 실시간으로 드래그 위치를 목표로 따라감 | `OnDrag`로 방향 자체는 매 프레임 갱신되고 있으나(`_launchDirection`), 이를 시각적으로 그리는 부분이 없으므로 "실시간 추적"이 화면에 나타나지 않음 | 방향 데이터 갱신은 있으나 시각화가 전무 |
| 발사된 볼은 플레이어가 더 이상 제어할 수 없고, 충돌에 따라 물리적으로 방향이 바뀌며 이동 | `Ball.Launch()` 이후 방향을 바꾸는 별도 입력 처리는 없고, `FixedUpdate()`에서 속도 크기만 유지하며 방향은 Rigidbody2D의 충돌 반사에 맡김 | 이 부분은 요구사항과 이미 부합. 재설계 시 유지해야 할 기존 동작으로 분류 가능 |
| 볼은 최종적으로 화면 하단에서 튕겨 **캐릭터 위치로 돌아온다** | 현재는 `"Wall"` 태그와의 충돌 횟수(`_remainingBounces`) 소진, 또는 `"Ground"` 태그 충돌 시 곧바로 `ReturnToPool()`이 호출되어 **위치 이동 없이 즉시 비활성화**됨. 캐릭터 위치로 이동하는 연출/로직이 없음 | "귀환" 개념 자체가 코드에 없음 — 현재는 "소멸"에 가까움 |
| 귀환한 볼은 **그 시점의 최신 조준 방향**으로 자동 재발사 | `ReturnBall()`(`BallLauncher`) 및 `ReturnToPool()`(`Ball`) 어디에도 재발사를 트리거하는 코드가 없음. `_launchDirection`은 계속 최신값으로 갱신되고 있어 값 자체는 활용 가능하나, 이를 참조해 재발사하는 로직 자체가 없음 | 재발사 트리거 로직 완전히 부재 |
| 발사 → 충돌 이동 → 귀환 → 재발사 사이클이 반복되며, 그 사이 플레이어는 "다음 재발사에 쓸 방향"을 계속 조준 가능 | 현재 구조는 "드래그+릴리즈 1회 = 발사 1회, 이후 끝"으로, 사이클이라는 개념 자체가 없다. `OnRelease`가 재차 발생해야만(즉 사용자가 또 드래그+릴리즈해야만) 다음 발사가 일어남 | 발사가 릴리즈에 종속된 1회성 이벤트 구조 → "터치 유지 중 조준을 계속 갱신하고, 볼은 자동으로 사이클을 반복"하는 구조로 근본적 재설계 필요 |
| 볼의 이동 속도는 조준/조작과 무관하게 항상 일정 | `Ball.FixedUpdate()`에서 `linearVelocity.normalized * speed`로 매 물리 프레임 크기를 강제 고정 | 이미 요구사항과 일치. 재설계 시 그대로 유지 가능한 부분 |

추가로 확인된 구조적 제약:
- `BallLauncher._canLaunch`는 `GameManager.GameState.Playing` 여부만 반영하며, "현재 볼이 날아가는 중인지" 여부는 전혀 추적하지 않는다. 재설계 시 여러 볼이 동시에 존재할 수 있는지(원본처럼 재발사가 반복되며 화면에 여러 개의 볼이 동시에 날아다닐 수 있는지), 아니면 항상 1개만 존재하는지에 대한 규칙이 현재 코드/문서 어디에도 명시되어 있지 않다.
- `WaveManager`와 `HUDPanel`은 `BallLauncher.OnAllBallsReturned`(활성 볼 카운트가 0이 될 때)를 "한 턴이 끝난 시점"으로 간주해 몬스터를 내리거나 인디케이터를 끄는 트리거로 쓰고 있다. 재발사가 도입되면 "볼이 캐릭터로 돌아왔지만 즉시 재발사되어 활성 카운트가 다시 올라가는" 흐름이 생기므로, 이 두 클래스가 기대하는 "모든 볼이 반환된 시점"의 의미가 그대로 유지될지, 아니면 재정의가 필요한지는 재설계 시 반드시 짚어야 할 지점이다.
- `LaunchPoint`는 `BallLauncher`의 자식 빈 오브젝트로 고정 좌표(`localPosition = (0, -8, 0)`)에 위치하며, 캐릭터 오브젝트 자체와 연동되어 위치가 갱신되는 구조는 아닌 것으로 보인다(에디터 스크립트 기준). "캐릭터 위치로 귀환"을 구현할 때 이 지점을 목표 좌표로 그대로 쓸 수 있는지, 혹은 실제 캐릭터 Transform을 참조해야 하는지 확인이 필요하다.

### 볼 개수/로스터 모델 (사용자 확인 사항 반영)

- 사용자 확인: 게임은 노말볼 5개로 시작하고, 웨이브 클리어 후 3택지에서 신규 특수볼 타입을 고르면 볼 개수가 1개 늘어나며(예: 노말볼 5 + 파이어볼 1 = 총 6개), 이미 보유한 타입을 다시 고르면 개수가 아니라 해당 특수볼의 레벨이 오른다. 즉 볼은 "재사용되는 동일한 부품 1개"가 아니라, **각자 타입(Normal/Fire/Ice/Ghost/Laser/Cluster)과 레벨을 영구적으로 유지하는 개별 개체들의 로스터**로 존재해야 하며, 로스터에 속한 모든 볼이 동시에 각자 독립적으로 조준→발사→충돌 이동→귀환→최신 방향 재발사 사이클을 반복해야 한다.
- 위 "SkillManager.cs" 절에서 확인했듯, 현재 코드에는 이 로스터 개념에 대응하는 데이터 모델이 없다. `BallLauncher`는 풀에서 `Ball` 인스턴스를 무작위로(타입 구분 없이) 하나 꺼내 쓰고, 볼의 "타입"은 발사 시점에 그때그때 적용되는 스킬 리스트로만 표현되며 귀환 시 초기화된다. `SkillManager.EquipActiveSkill()`의 "신규=추가/기존=레벨업" 판정 로직은 액티브 스킬 슬롯(최대 4종) 관리용이라는 점에서 사용자가 설명한 규칙과 개념적으로는 닮았지만, 관리 대상(스킬 슬롯 vs 볼 개체 로스터)과 개수 상한(4 vs 시작 5+획득분)이 다른 별개의 체계다.
- 따라서 이번 볼 발사 메커닉 재설계는 조준/궤적/귀환·재발사 사이클뿐 아니라, **"N개의 타입별 개별 정체성을 유지하는 볼 로스터" 구조**까지 함께 다뤄야 한다. 이는 `BallLauncher`(볼을 매번 풀에서 무작위로 꺼내 쓰는 대신 로스터를 유지하며 로스터 전원을 동시에 순환시켜야 함), `Ball`(귀환 후에도 타입/레벨을 유지해야 함), `SkillManager`/`SkillSelectionPanel`(신규 타입 선택 시 실제로 로스터에 새 볼 개체를 추가하는 연동이 필요함)에 걸쳐 영향을 미치는 구조적 변경이며, 구체적인 데이터 모델과 구현 방식은 plan.md 단계에서 다뤄야 한다.

### 몬스터 하강 결합 문제 (사용자 확인 사항 반영, 범위 제외 안건 포함)

- 사용자 확인: 몬스터 하강은 볼 발사/귀환 사이클과 무관하게 시간 기반으로 서서히(continuous) 진행되는 별개의 로직이며, 현재 코드처럼 "모든 볼이 반환되는 시점마다 한 칸씩" 내려오는 방식이 아니다.
- 다만 이 "서서히 내려오는" 신규 로직 자체를 구현하는 것은 **이번 task 범위가 아니며 별도 task로 분리**하기로 확인했다. 이번 task에서 다룰 지점은 어디까지나 그 부작용이다: 이번 재설계로 볼이 로스터 전체(N개)가 동시에 영구 순환하는 구조가 되면, 발사→귀환이 끊임없이 겹쳐서 일어나 활성 볼 카운트(`_activeBallCount`)가 사실상 0에 도달하는 일이 거의 없어지고, 그 결과 현재 `BallLauncher.OnAllBallsReturned` 이벤트 자체가 (의도한 대로는) 더 이상 발생하지 않게 된다.
- 이 이벤트에 의존하는 두 지점이 함께 영향을 받는다: `WaveManager.HandleAllBallsReturned()` → `MoveAllMonstersDown()`(몬스터를 한 칸 내리는 트리거)과, `HUDPanel`의 조준 인디케이터 on/off 트리거. 로스터 도입만으로 두 기능이 사실상 멈추는 부작용이 발생한다.
- 이 부작용을 이번 task에서 어떻게 처리할지(예: `OnAllBallsReturned` → `MoveAllMonstersDown` 연결고리만 우선 제거해 몬스터를 일단 정지 상태로 두는지, 아니면 이벤트 연결은 그대로 두고 후속의 "시간 기반 하강" task에서 한꺼번에 정리할지)는 이번 research.md에서 결론을 내리지 않으며, **plan.md 작성 시 명시적으로 다뤄야 할 안건으로 남겨둔다.**

---

## 결론

- 현재 `Ball.FixedUpdate()`의 속도 고정 로직과, 충돌 후 플레이어가 개입하지 않고 물리 반사에 맡기는 이동 방식은 GameplayMechanics.md 요구사항과 이미 부합하므로 재설계 시 그대로 유지 가능한 부분으로 분류된다.
- 사용자와의 추가 논의를 거쳐, 이번 task(볼 발사 메커닉 재설계)의 범위는 다음 네 가지로 최종 확정한다.
  1. **조준 시작/갱신 이벤트 체계** — "터치 시작 즉시 조준 시작"을 표현할 이벤트가 `InputHandler`에 없고, 현재는 오직 "드래그 중" 방향과 "릴리즈" 시점만 알 수 있다. 터치 시작(Began) 프레임 자체를 알리는 이벤트/훅을 신설해야 한다.
  2. **궤적 프리뷰** — LineRenderer 등으로 1차 충돌 지점까지, 그리고 그 지점에서 반사된 방향으로 이어지는 2차 충돌 지점까지 2단계 점선 경로를 그리는 컴포넌트가 프로젝트에 전무하며, 이를 계산할 Raycast 로직(1차 충돌 지점 계산 + 그 지점에서 반사 방향으로 2차 충돌 지점 계산, 총 최소 2회)도 없다. 2차 충돌 지점에 표시되는 빨간 점/원형 궤적선 마커 UI 구현도 포함하며, 3차 충돌 이후는 표시하지 않는다. 조준 중 실시간으로 드래그 방향을 따라가는 시각화까지 포함한다.
  3. **귀환·재발사 사이클** — 볼이 벽/바닥에 닿으면 즉시 소멸(풀 반환)하는 현재 구조를, "캐릭터 위치로 귀환 이동 → 그 시점 최신 조준 방향으로 자동 재발사"하는 반복 사이클로 바꿔야 하며, 이는 `BallLauncher`의 발사 트리거 방식(릴리즈 1회성 → 사이클 반복형)을 근본적으로 바꾸는 작업이다.
  4. **N개 타입별 개별 볼 로스터 구조** — 노말볼 5개로 시작해 웨이브 클리어 후 3택지에서 신규 특수볼 타입을 고르면 볼 개수가 1개 늘고, 기존 타입을 고르면 해당 볼의 레벨만 오르는 규칙을 실제로 뒷받침하려면, 볼이 매 발사마다 스킬 리스트를 새로 뒤집어쓰는 현재의 "스킬 슬롯" 모델(`SkillManager.ApplySkillToBall`/`EquipActiveSkill`)과는 별개로, 각 볼 개체가 자신의 타입과 레벨을 영구적으로 유지한 채 로스터 전체가 동시에 독립적으로 사이클을 도는 구조가 필요하다. 이는 `BallLauncher`/`Ball`뿐 아니라 `SkillManager`/`SkillSelectionPanel`의 신규 타입 선택 흐름까지 함께 재검토해야 하는 범위다.
  이 4가지가 이번 재설계의 핵심 대상이다.
- 반면 **몬스터가 시간 기반으로 서서히(continuous) 내려오는 실제 로직 구현은 이번 task 범위에서 명시적으로 제외**하고 별도 task로 분리한다. 다만 로스터 구조(위 4번) 도입의 부작용으로 `BallLauncher.OnAllBallsReturned`가 사실상 발생하지 않게 되어, 이 이벤트에 의존하는 `WaveManager`의 몬스터 하강 트리거와 `HUDPanel`의 조준 인디케이터 on/off 트리거가 함께 멈추게 된다는 점은 이번 task에서 반드시 인지하고 있어야 하며, 이 부작용을 어떤 방식으로 임시 처리할지(연결고리 제거 후 몬스터 정지 상태 유지 vs 후속 task까지 방치)는 plan.md 작성 시 명시적으로 결정해야 할 안건이다.
- `LaunchPoint`가 캐릭터 위치와 연동되는지 여부는 사용자 확인으로 해소되었다. 캐릭터는 화면 하단 중앙에 고정된 위치에서 이동하지 않으므로, `LaunchPoint`의 고정 좌표(`localPosition = (0, -8, 0)`)를 귀환 목표 좌표로 그대로 사용해도 무방하다. 다만 좌우 반전이나 발사각에 따라 캐릭터가 들고 있는 스태프의 각도가 회전하는 것과 같은 시각적 연출은 `GameplayMechanics.md`가 다루는 "알고리즘/메커닉" 범위가 아니라 UI/연출 영역이므로(문서 서두에 명시된 대로), 이번 조사·재설계 대상에서 제외한다.
