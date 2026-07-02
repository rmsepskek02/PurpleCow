# Plan — Ball Launch Mechanics

research.md에서 파악한 5가지 갭(조준 시작 이벤트 부재, 궤적 프리뷰 부재, 귀환/재발사 사이클 부재, 볼 타입별 개별 로스터 모델 부재, 몬스터 하강과의 결합 부작용)을 해소하기 위한 구현 계획입니다. `InputHandler` → `BallLauncher`(로스터 관리) → `Ball`(귀환/재발사) → 신규 `TrajectoryPreview`(궤적 프리뷰) 순으로 단계를 나누고, `SkillManager`/`SkillSelectionPanel`이 로스터에 실제로 볼을 추가/레벨업하도록 연동하는 방식을 다룹니다. 마지막으로 `WaveManager`/`HUDPanel`이 더 이상 유효하지 않게 되는 `OnAllBallsReturned` 의존성을 정리하고, `MonsterBase`에 시간 기반 연속 하강 로직을 실제로 구현하는 작업까지 포함합니다.

## 구현 목표

- 터치 시작 즉시 조준이 시작되고, 드래그 중 방향이 실시간으로 갱신되며, 릴리즈는 더 이상 발사를 트리거하지 않도록 입력-발사 결합을 끊는다.
- 조준 중 1차/2차 충돌 지점을 계산해 점선 궤적과 2차 지점의 레드닷+원형 궤적선을 실시간으로 표시하는 궤적 프리뷰를 신규 구현한다.
- 볼이 화면 하단(바닥)에 닿으면 소멸하지 않고 캐릭터 위치(`LaunchPoint`)로 귀환한 뒤, 그 시점의 최신 조준 방향으로 자동 재발사되는 사이클을 구현한다.
- "노말볼 5개로 시작하고, 웨이브 클리어 후 3택지에서 신규 타입을 고르면 로스터에 볼이 1개 늘고, 기존 타입을 고르면 해당 볼의 레벨만 오른다"는 규칙을 실제로 뒷받침하는 볼 로스터 데이터 모델을 도입하고, 로스터의 각 항목이 독립적으로 조준→발사→이동→귀환→재발사 사이클을 반복하게 한다.
- 위 로스터 구조 도입으로 인해 사실상 발생하지 않게 되는 `BallLauncher.OnAllBallsReturned`에 의존하던 `WaveManager`/`HUDPanel`의 부작용을 정리한다.
- 몬스터가 볼 사이클과 무관하게 시간 기반으로 연속적으로 하강하도록 구현한다.

## 단계별 작업 계획

### 1단계 — 조준 이벤트 체계 (`InputHandler.cs`)

- `Action OnAimBegin`(또는 `Action<Vector2> OnAimBegin`, 최초 조준 방향/시작 위치 전달용) static 이벤트를 신설한다.
- `Update()` 내 `pressedPos.HasValue`로 `_dragStartPosition`을 기록하는 분기에서, 기존 `_isDragging = true` 처리와 함께 `OnAimBegin`을 1회 발행한다.
- 기존 `OnDrag`(드래그 중 매 프레임 방향 갱신), `OnRelease`(릴리즈 시점 1회 발행) 이벤트 자체는 유지한다. 다만 `OnRelease`의 "의미"가 바뀐다 — 더 이상 "발사 트리거"가 아니라 "조준 종료(궤적 프리뷰 숨김 등)를 알리는 신호"로만 소비되도록 후속 단계에서 구독부를 수정한다.
- `BallLauncher.HandleRelease()`에서 `LaunchBall()` 호출을 제거한다(2단계와 함께 진행). `InputHandler.OnRelease` 구독 자체는 궤적 프리뷰(4단계)가 필요로 하므로, `BallLauncher`는 더 이상 `OnRelease`를 구독하지 않아도 되는지 2단계에서 함께 정리한다.
- 게임 시작 즉시 기본값 `Vector2.up`(12시 방향)으로 자동 발사가 시작되며, 사용자 터치 여부와 무관하다(확정). `BallLauncher`(또는 로스터를 초기화하는 지점)에 `Vector2.up`을 기본값으로 하는 필드를 두고, `OnAimBegin`/`OnDrag`가 한 번도 발행되지 않은 상태에서도 게임 시작과 동시에 첫 자동 발사가 이루어지도록 한다.

### 2단계 — 볼 로스터 데이터 모델 도입 (`BallLauncher.cs`, `Ball.cs`, `SkillManager.cs`, `SkillSelectionPanel.cs`)

이번 재설계에서 아키텍처 변경 폭이 가장 큰 부분이므로 세부 단계로 나눈다.

1. **로스터 데이터 구조 설계**
   - `BallLauncher`가 소유하는 로스터 리스트(예: `List<BallRosterEntry> _roster`)를 신설한다. `BallRosterEntry`는 "타입(Normal/Fire/Ice/Ghost/Laser/Cluster)과 레벨을 영구 보유하며, 그 타입에 대응하는 `Ball` 인스턴스 1개와 1:1로 연결되는" 최소 데이터 단위로 정의한다(클래스로 만들지, `Ball` 자신이 타입/레벨 필드를 직접 들고 로스터는 `List<Ball>`만 관리할지는 "주의사항"에서 확인 필요 — DevRules.md의 "3줄로 해결되는 것을 클래스로 만들지 않는다" 원칙에 따라 가능한 한 단순한 구조를 우선 검토).
   - 볼 타입 구분을 위한 열거형이 필요하다. 기존 `ActiveSkillId`(Fire=1001, Ice=1002, Ghost=1003, Laser=1004, Cluster=1005)를 그대로 재사용할지, 별도로 `Normal`을 포함하는 `BallType` 열거형을 신설할지 결정한다(예: `BallType.Normal`, `BallType.Fire` ... 기존 `ActiveSkillId`와 값 매핑). DevRules.md 네이밍 컨벤션(`BallType`, PascalCase 값)을 따른다.
   - 각 로스터 항목은 자신의 "귀환 후 재적용해야 할 스킬/레벨"을 유지해야 한다 — 현재 `Ball._skills`가 `OnDespawn()`에서 매번 `Clear()`되는 구조(Ball.cs:39-46)를 그대로 두되, 재발사 시점(3단계)에 로스터가 해당 볼의 타입/레벨에 맞는 스킬을 다시 `AddSkill()`로 부착하는 방식으로 "정체성은 로스터가 기억하고, `Ball` 컴포넌트 자체는 매 사이클 재구성되는" 역할 분담을 검토한다.

2. **로스터 초기화 (게임 시작 시 노말볼 5개)**
   - `BallLauncher`가 `Awake()`/`Start()` 또는 `GameManager.GameState.Ready → Playing` 전환 시점에 로스터를 노말볼 5개로 초기화한다. 각 항목은 풀에서 `Ball` 인스턴스를 1개씩 미리 확보(또는 최초 발사 시점에 확보)해 자신에게 연결한다.
   - 웨이브 재시작(재도전) 시 로스터를 초기 상태(노말볼 5개, 레벨 초기화)로 되돌리는 처리가 필요한지 확인한다 — `SkillSelectionPanel.HandleGameStateChanged()`가 이미 `GameState.Ready`에서 `SkillData.ResetLevel()`을 호출하고 있으므로, 동일 시점에 로스터도 리셋하는 것을 고려한다.

3. **`BallLauncher`의 발사 트리거를 "릴리즈 1회성"에서 "로스터 상시 순환형"으로 변경**
   - 기존 `LaunchBall()`(단일 볼, 릴리즈 시 1회 호출)을 로스터 순환 구조에 맞게 재구성한다. 각 로스터 항목이 "현재 비행 중이 아니면(=대기/귀환 완료 상태) 최신 조준 방향으로 즉시 발사"하는 방식으로 동작해야 하므로, `LaunchBall()`은 특정 로스터 항목(볼)을 인자로 받아 발사하는 형태로 시그니처를 바꾸는 것을 검토한다.
   - `_activeBallCount`(현재 활성 볼 수)는 로스터 도입 후에도 유지 가능하나, 그 의미(0이 되는 시점)가 5단계에서 다뤄지듯 더 이상 "한 턴 종료"를 뜻하지 않게 된다는 점을 인지한다.

4. **`SkillManager`/`SkillSelectionPanel` 연동 — 신규 타입 선택 시 로스터에 볼 추가, 기존 타입 선택 시 레벨업**
   - `SkillSelectionPanel.ApplySkill(SkillData data)`(SkillSelectionPanel.cs:126)이 `SkillType.Active`일 때 현재는 `SkillManager.Instance.EquipActiveSkill(skill)`만 호출한다(SkillSelectionPanel.cs:131). `EquipActiveSkill()`(SkillManager.cs:25)은 "동일 `SkillId`가 이미 있으면 `LevelUp()`만 호출, 없으면 `_activeSkills`(최대 4종)에 `Add`"하는 로직을 그대로 유지하되, 이 결과를 `BallLauncher`의 로스터에도 반영해야 한다.
   - 구체적으로, `EquipActiveSkill()`이 "신규 추가"인지 "레벨업"인지 구분해서 알려주는 반환값(예: `bool isNewSkill`) 또는 별도 이벤트(`OnActiveSkillAdded(SkillData)` / `OnActiveSkillLeveledUp(SkillData)`)를 신설하는 것을 검토한다. `BallLauncher`(또는 로스터를 관리하는 주체)가 이를 구독해:
     - 신규 타입이면 로스터에 해당 타입의 볼 1개를 새로 추가하고(풀에서 `Ball` 확보 후 로스터 등록, 화면에는 다음 재발사 시점 혹은 즉시 발사되어 사이클에 합류),
     - 기존 타입이면 로스터에서 해당 타입 항목을 찾아 레벨 정보만 갱신(레벨 자체는 `SkillData.CurrentLevel`을 참조하므로 별도 저장이 필요 없을 수도 있음 — 로스터 항목이 `SkillData`를 직접 참조하는 방식이면 자동으로 최신 레벨이 반영됨)한다.
   - `SkillManager._activeSkills`(액티브 스킬 슬롯, 최대 4종)는 특수볼 타입 최대 4종과 정확히 대응한다(확정 — 원본 게임은 액티브 슬롯 4개 + 패시브 슬롯 2개 구조이며 특수볼은 액티브 슬롯에 포함됨). 즉 로스터 볼 개수 상한은 노말볼 5개(고정) + 특수볼 최대 4종(레벨업은 가능하나 개수는 늘어나지 않음) = 최대 9개로 확정한다.

5. **`Ball` 인스턴스와 로스터 항목의 연결 방식**
   - 오브젝트 풀링 규칙(DevRules.md "풀링 대상: 볼, 몬스터, 데미지 텍스트", "부족 시 자동 추가 생성, 최대 사이즈 제한 없음")을 그대로 준수한다. 로스터 항목이 늘어나도 `_ballPool`은 기존 `ObjectPool<Ball>` 그대로 사용하되, 로스터 항목마다 발사 시점에 풀에서 `Ball`을 꺼내 타입에 맞는 스킬을 부착하는 방식(현재 `ApplySkillToBall()`과 유사하나 "로스터 항목 1개당 1개 타입"으로 특정)으로 구현한다.
   - 볼이 동시에 여러 개(최대 5+N개) 화면에 존재하며 각자 독립적으로 비행/귀환/재발사되어야 하므로, `Ball`이 "자신이 어느 로스터 항목에 속하는지" 알 필요가 있는지, 아니면 `BallLauncher`가 `Ball` 인스턴스 참조로 로스터를 관리하고 `Ball`은 자신의 상태(비행 중/대기 중)만 노출하면 되는지를 구현 시점에 정한다.

### 3단계 — 귀환·재발사 사이클 (`Ball.cs`)

- 현재 `OnCollisionEnter2D`의 `"Wall"` 분기(`_remainingBounces` 소진 시 `ReturnToPool()`, Ball.cs:83-91)와 `"Ground"` 분기(무조건 `ReturnToPool()`, Ball.cs:92-95) 중, 귀환 트리거는 기본적으로 `"Ground"` 충돌로 한정한다(확정). `"Wall"`(좌우/상단 벽) 충돌은 평상시 귀환을 트리거하지 않으며 순수 반사(반사 카운트 차감)만 유지한다.
- **예외(구현 중 QA로 확정된 안전장치)**: 이 게임은 중력이 없어 볼이 좌우 벽 사이에서만 반사를 거듭하며 바닥에 영원히 닿지 않을 가능성이 이론적으로 존재한다. 이를 방지하기 위해, 로스터에 속한 볼이 `"Wall"` 충돌로 `_remainingBounces`를 전부 소진한 경우에도(=계속 반사만 하다 바닥에 못 닿고 반사 횟수가 다 떨어진 경우) `ReturnToPool()`로 소멸시키지 않고 `"Ground"`와 동일하게 `LaunchPoint`로 강제 귀환시킨다. 로스터 밖의 볼(서브볼 등)은 이 예외 없이 기존대로 `ReturnToPool()`된다. 평상시(반사 횟수 내에 바닥에 도달하는 일반적인 경우)에는 이 예외가 발동하지 않으며, 위 "귀환은 Ground로 한정한다"는 원칙 그대로 동작한다.
- `"Ground"` 충돌 시 `ReturnToPool()` 대신, 다음을 수행하는 신규 메서드(예: `ReturnToLaunchPoint()`)를 호출한다(확정 — 순간이동(teleport)이 아니라, 방향을 강제로 재설정한 뒤 기존 물리 이동을 그대로 이어가는 방식):
  1. `"Ground"` 충돌 시점에 물리엔진이 계산하는 자연스러운 반사 방향을 그대로 따르지 않고, 볼의 이동 방향을 강제로 `BallLauncher`가 노출하는 `LaunchPoint.position`을 향하는 방향으로 재설정한다(위치 자체를 순간이동시키지 않고, `Rigidbody2D.velocity` 등 기존 이동 로직에 새 방향만 반영해 계속 날아가게 한다).
  2. 볼이 그 방향으로 계속 이동해 `LaunchPoint` 위치(또는 그 인근 판정 범위)에 도달하면, 그 시점의 최신 조준 방향(`BallLauncher`가 관리하는 `_launchDirection`)으로 즉시 재발사(`Launch(direction)` 재호출)한다.
  3. 재발사 시 이 볼(로스터 항목)의 타입에 대응하는 스킬을 다시 부착한다(2단계에서 정한 방식대로).
- `_remainingBounces`는 재발사 시점에 `_ballData.MaxBounces`로 다시 초기화되어야 한다(현재 `OnSpawn()`에서만 초기화되므로, 재발사 경로에서도 동일 초기화 로직을 호출하도록 정리).
- `ReturnToPool()`(풀 반환, `BallLauncher.ReturnBall()` 호출) 자체는 완전히 제거하지 않는다 — 게임 종료/웨이브 리셋 등 "정말로 비활성화해야 하는" 경우를 위해 남겨두되, 평상시 귀환 사이클에서는 호출되지 않도록 분리한다.

### 4단계 — 궤적 프리뷰 신규 컴포넌트 (`TrajectoryPreview.cs`)

- 신규 `MonoBehaviour` `TrajectoryPreview`를 작성한다(경로 제안: `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`, 기존 볼 관련 스크립트와 같은 폴더).
- `OnEnable`/`OnDisable`에서 `InputHandler.OnAimBegin`(1단계 신설), `InputHandler.OnDrag`, `InputHandler.OnRelease`를 구독/해제한다(DevRules.md 이벤트 규칙 준수).
- `OnAimBegin` 시 `LineRenderer`(또는 2개의 `LineRenderer`/렌더러 세트) 및 레드닷/원형 궤적선 오브젝트를 활성화한다.
- `OnDrag(direction)` 매 프레임:
  1. `LaunchPoint.position`에서 `direction`으로 `Physics2D.Raycast`를 쏴서 1차 충돌 지점(`hit1`)을 구한다.
  2. `hit1.normal`로 반사 방향(`Vector2.Reflect(direction, hit1.normal)`)을 계산하고, `hit1.point`에서 그 방향으로 다시 `Physics2D.Raycast`를 쏴서 2차 충돌 지점(`hit2`)을 구한다.
  3. `LaunchPoint→hit1`, `hit1→hit2` 두 구간을 점선(`LineRenderer`의 `material`/텍스처 타일링 또는 `Dashed` 셰이더 등, 구체 구현 방식은 "주의사항" 참고)으로 그린다.
  4. `hit2.point`에 레드닷 스프라이트와 원형 궤적선(고리) 오브젝트를 배치하고 표시한다.
  5. 3차 충돌 이후는 계산/표시하지 않는다.
- `OnRelease` 시에도 궤적 프리뷰 자체는 계속 표시할지(조준이 릴리즈 후에도 유지되는 최신 방향을 계속 보여주는지), 아니면 손을 뗀 순간 숨길지는 GameplayMechanics.md에 명시가 없어 "주의사항"에 남긴다 — 다만 재발사 사이클이 "최신 조준 방향"을 계속 참조해야 하므로 최소한 `_launchDirection` 값 자체는 릴리즈 후에도 보존되어야 한다(이는 `BallLauncher`가 이미 그렇게 동작 중).
- Raycast에 사용할 레이어마스크(벽/몬스터/바닥 등 어떤 콜라이더를 충돌 대상으로 볼지)는 기존 씬의 `"Wall"`/`"Ground"`/`"Monster"` 태그/레이어 구성을 그대로 활용하되, 구체적인 레이어마스크 값은 구현 시점에 씬을 확인하며 정한다.

### 5단계 — 몬스터 시간 기반 하강 구현 (`WaveManager.cs`, `MonsterBase.cs`, `HUDPanel.cs`)

사용자가 최종 확인해준 대로, 몬스터 하강은 더 이상 볼 사이클(`OnAllBallsReturned`)에 결합되지 않고 시간 기반 연속 하강으로 실제 구현까지 이번 task에 포함한다.

- **`WaveManager.cs` — 볼 이벤트 결합 완전 제거**
  - `OnEnable`/`OnDisable`의 `BallLauncher.OnAllBallsReturned += HandleAllBallsReturned` / `-=` 구독을 완전히 제거한다.
  - `HandleAllBallsReturned()` → `MoveAllMonstersDown()` 호출 경로 및 `MoveAllMonstersDown()` 메서드 자체를 제거한다. 몬스터 하강은 이제 `MonsterBase` 스스로가 매 프레임 수행하므로 `WaveManager`가 몬스터를 직접 이동시킬 필요가 없다.
  - `CheckGameOver()`(몬스터가 `_bottomBoundaryY` 이하로 내려왔는지 판정)는 기존에는 `MoveAllMonstersDown()` 직후(=볼 사이클이 끝날 때마다)에만 호출됐으나, 이제 몬스터가 매 프레임 스스로 이동하므로 이 판정도 매 프레임 이뤄져야 한다. `WaveManager`에 `Update()`를 신설해 매 프레임 `CheckGameOver()`를 호출하는 방식과, `MonsterBase`가 스스로 바닥 도달을 감지해 이벤트를 발행하고 `WaveManager`는 그 이벤트만 구독하는 방식 중 하나를 구현 시점에 결정한다(둘 다 가능한 방식 — "주의사항" 참고).

- **`MonsterBase.cs` — 시간 기반 연속 하강**
  - 신규 `Update()`(또는 물리 이동이면 `FixedUpdate()`) 메서드를 추가해, 매 프레임 자기 자신을 `_monsterData.MoveSpeed * Time.deltaTime`만큼 아래 방향으로 연속 이동시킨다.
  - `IsFrozen`이면 이동을 건너뛴다.
  - `_slowTurnsRemaining > 0`이면(아래 이름 변경 참고) 이동 속도에 `(1f - _slowPercent)`를 곱한다.
  - **턴 기반 → 시간 기반 카운트다운 전환**: 기존 `MoveDown(float distance)`는 "한 번 호출 = 한 턴 경과"를 전제로 `_frozenTurnsRemaining--`, `_slowTurnsRemaining--`처럼 호출 횟수 기반으로 감소시키는 구조였다(Ball 사이클에 종속된 방식). 이제는 매 프레임 호출되는 연속 이동 구조로 바뀌므로, 이 카운트다운 로직을 시간(초) 기반(`Time.deltaTime`만큼 매 프레임 차감)으로 전환한다. `ApplyFreeze(float seconds)` 오버로드가 이미 존재하는 것으로 볼 때 원래도 "1턴=1초" 가정이 있었던 것으로 보이므로, 이참에 `_frozenTurnsRemaining`/`_slowTurnsRemaining` 필드 자체를 초 단위(예: `_frozenSecondsRemaining`, `_slowSecondsRemaining`)로 이름과 타입(`float`)을 바꾸는 것을 제안한다. 이 필드명 변경은 확정이 아니며 "주의사항" 참고.
  - `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`가 `target.ApplyFreeze(LevelData.Value2)`(int 오버로드)와 `target.ApplySlow(Mathf.RoundToInt(LevelData.Value2), LevelData.Value3)`(턴 수를 int로 반올림해 전달)를 호출하고 있음을 Grep으로 확인했다. 필드를 초 단위로 전환할 경우 이 호출부의 시그니처/단위 일치 여부를 함께 재검토해야 한다("주의사항" 참고).

- **`HUDPanel.cs` — 조준 인디케이터 트리거 변경**
  - 조준 인디케이터(`SetLaunchIndicatorVisible`)가 `BallLauncher.OnAllBallsReturned`를 켜짐/꺼짐 트리거로 쓰는 부분을, `GameManager.GameState`(예: `Playing` 상태면 표시, 그 외 상태면 숨김) 기준으로 교체한다.

- `BallLauncher.OnAllBallsReturned` 이벤트 자체(및 `_activeBallCount` 카운트)는 로스터 구조 도입 후에도 완전히 무의미해지는 것은 아니므로(예: 디버깅, 향후 다른 트리거로 재활용될 가능성) 이벤트 선언 자체는 남겨두고, `WaveManager`/`HUDPanel`의 구독부만 정리한다.

## 예상 변경/생성 파일 목록

| 파일 경로 | 변경 내용 |
|---|---|
| `Assets/_Project/Scripts/Core/InputHandler.cs` | `OnAimBegin` static 이벤트 신설, `Began`/`wasPressedThisFrame` 프레임에 발행 |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 볼 로스터(`List<BallRosterEntry>` 또는 유사 구조) 신설 및 관리, 노말볼 5개 초기화 로직 추가, `HandleRelease()`에서 `LaunchBall()` 호출 제거, `LaunchBall()` 시그니처를 로스터 항목 기준으로 재구성, `SkillManager`의 신규/레벨업 알림 구독 및 로스터 반영 로직 추가, 기본 조준 방향(`Vector2.up`) 필드 추가 |
| `Assets/_Project/Scripts/Ball/Ball.cs` | `"Ground"`(및 필요 시 `"Wall"`) 충돌 처리를 `ReturnToPool()`에서 `ReturnToLaunchPoint()`(귀환 이동 + 재발사)로 변경, `_remainingBounces` 재초기화 위치 정리 |
| `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` (신규) | 조준 이벤트 구독, 1차/2차 Raycast 계산, 점선 궤적 + 레드닷/원형 궤적선 표시 |
| `Assets/_Project/Scripts/Skill/SkillManager.cs` | `EquipActiveSkill()`이 신규 추가/레벨업 여부를 구분해 알리도록 반환값 또는 이벤트 추가 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | `ApplySkill()`에서 신규 타입 선택 시 로스터에 볼 추가가 실제로 트리거되도록 연동 확인/수정 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | `BallLauncher.OnAllBallsReturned` 구독 및 `MoveAllMonstersDown()` 완전 제거, `CheckGameOver()`를 매 프레임 체크로 전환(`Update()` 신설 또는 `MonsterBase` 이벤트 구독 방식 중 결정) |
| `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 매 프레임(`Update()`/`FixedUpdate()`) `_monsterData.MoveSpeed * Time.deltaTime`만큼 연속 하강하는 로직 신설, `IsFrozen`/`_slowTurnsRemaining` 반영, 턴 기반 카운트다운(`_frozenTurnsRemaining`/`_slowTurnsRemaining`)을 시간(초) 기반으로 전환 |
| `Assets/_Project/Scripts/UI/HUDPanel.cs` | 조준 인디케이터 표시 트리거를 `OnAllBallsReturned` 대신 `GameManager.GameState` 기준으로 변경 |
| (신규, 명칭 미확정) 볼 타입 열거형 (`BallType.cs` 등) | 로스터 항목의 타입(Normal 포함)을 표현하는 열거형 — 기존 `ActiveSkillId` 재사용 여부에 따라 생성 여부 결정 |

## 주의사항

- **로스터 데이터 구조의 구체 형태**: 별도 클래스(`BallRosterEntry`)로 만들지, `Ball`에 타입/레벨 필드를 직접 추가해 `BallLauncher`가 `List<Ball>`만 관리할지 구현 전 결정 필요. DevRules.md의 단순함 우선 원칙에 따라 과도한 추상화를 피해야 한다.
- **볼 타입 열거형 신설 여부**: 기존 `ActiveSkillId`(SkillData.cs)를 그대로 볼 타입으로 재사용할지, `BallType.Normal`을 포함한 별도 열거형을 만들지 확인 필요.
- **액티브 스킬 슬롯(최대 4종) vs 로스터 볼 개수의 관계(확정됨)**: 액티브 슬롯 4종은 특수볼 타입 최대 4종과 정확히 대응하며, 로스터 볼 개수 상한은 노말볼 5개(고정) + 특수볼 최대 4종(레벨업 가능, 개수는 늘어나지 않음) = 최대 9개로 확정됨(2단계 4번째 세부 단계 참고).
- **귀환 이동 연출 방식 및 `"Wall"` 충돌과 귀환의 관계(확정됨)**: `"Ground"` 충돌 시 순간이동이 아니라 이동 방향을 강제로 `LaunchPoint` 쪽으로 재설정해 기존 물리 이동을 이어가며, `LaunchPoint`에 도달하면 재발사되는 방식으로 확정됨. `"Wall"` 충돌은 순수 반사만 유지하며 귀환 트리거가 아님(3단계 참고).
- **최초 게임 시작 시 자동 발사 시점(확정됨)**: 사용자 터치 여부와 무관하게 게임 시작 즉시 기본 조준 방향 `Vector2.up`(12시 방향)으로 로스터의 볼들이 자동 발사되어 사이클이 시작됨(1단계 참고).
- **동시에 여러 볼이 화면에 존재할 때의 물리/성능 영향**: 노말볼 5개+특수볼 N개가 동시에 독립적으로 비행/충돌/귀환하는 구조가 되므로, 기존에 암묵적으로 "볼 1개만 존재"를 가정했을 수 있는 다른 코드(예: 데미지 텍스트, 카메라 흔들림 등 UI 연출)에 부가 영향이 없는지는 이번 조사 범위 밖이며 구현 중 발견 시 별도 보고가 필요할 수 있음.
- **몬스터 하강 결합 해소 방식(확정됨)**: 시간 기반 연속 하강을 이번 task에 포함해 구현한다. `WaveManager`의 `OnAllBallsReturned` 구독 및 `MoveAllMonstersDown()`은 완전히 제거하고, `MonsterBase`가 매 프레임 스스로 하강한다. 다만 다음 세부 사항은 구현 시점에 확인이 필요하다.
  - **하강 속도 튜닝 여부**: `MonsterData.MoveSpeed`의 현재 기본값(`1f`, `MonsterSetupEditor.cs:68`)이 실제 원본 게임 체감 속도에 맞는지, 밸런스 조정이 필요한지는 구현 중 재확인한다.
  - **Freeze/Slow 턴 → 시간 기반 전환의 영향 범위**: `_frozenTurnsRemaining`/`_slowTurnsRemaining` 필드를 초 단위(`_frozenSecondsRemaining`/`_slowSecondsRemaining`, `float`)로 바꿀 경우, `ApplyFreeze(int turns)`/`ApplySlow(int turns, float percent)`를 호출하는 `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`(`target.ApplyFreeze(LevelData.Value2)`, `target.ApplySlow(Mathf.RoundToInt(LevelData.Value2), LevelData.Value3)`)의 시그니처/단위를 함께 초 단위로 통일할지, 아니면 기존 int 오버로드를 유지하며 내부에서만 초로 환산할지는 구현 시점에 결정한다.
  - **바닥 도달 판정 위치**: `CheckGameOver()`를 `WaveManager.Update()`에서 매 프레임 호출할지, `MonsterBase`가 스스로 바닥 도달을 감지해 이벤트를 발행하고 `WaveManager`가 이를 구독하는 방식으로 할지는 구현 시점에 결정한다.
- **궤적 프리뷰의 점선 렌더링 구체 구현**: `LineRenderer`의 텍스처 타일링 방식, 셰이더, 또는 별도 점 오브젝트 배열 방식 중 무엇을 쓸지는 구현 단계에서 결정한다. 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/`)을 참고해 시각적 스타일을 맞출 필요가 있다.
- **Raycast 레이어마스크**: 궤적 프리뷰 Raycast가 어떤 레이어/태그(Wall/Ground/Monster 등)를 충돌 대상으로 포함할지 씬 구성을 구현 시점에 재확인해야 한다.
