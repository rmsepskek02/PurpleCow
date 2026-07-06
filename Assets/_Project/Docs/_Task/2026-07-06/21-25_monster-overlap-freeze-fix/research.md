# Research — 몬스터 이동 겹침/관통 버그 수정

TODO.md 5번("아이스볼 및 몬스터 이동 겹침 방지")은 "구현 완료"로 기록되어 있었으나, 사용자가 실제 플레이에서 (1) 1×1 몬스터끼리 겹침, (2) 1×1과 2칸짜리 몬스터의 겹침, (3) 빙결된 몬스터를 뒤따르는 몬스터가 관통하는 현상 3가지를 발견했다. **이 문서는 최초 조사본을 사용자가 검토하고 명확히 반박한 뒤 재조사한 개정판이다.** 최초 조사에서 제시했던 "스프라이트가 Collider보다 세로로 조금(0.03~0.14 유닛) 더 크다"는 가설은 사용자가 실제로 목격한 "거의 완전히 하나처럼 겹침", "레인 중상단에서도 자주 발생하는 관통" 규모와 전혀 맞지 않아 **기각**되었다. 이번 개정판은 Unity 물리 엔진의 Transform-Collider 동기화 설정까지 포함해 프로젝트 설정을 다시 조사했고, 훨씬 규모가 크고 지속적인 원인 후보를 새로 확인했다.

## 현재 상태

- `MonsterBase.Update()`(274~305행)는 매 프레임 `transform.position += Vector3.down * safeDistance`로 몬스터를 **직접 Transform으로** 이동시킨다. `FixedUpdate()`가 아니라 `Update()`에서, `Time.deltaTime`(프레임 델타)을 사용한다.
- `WaveManager.GetSafeDownwardDistance()`는 매 프레임 `_activeMonsters`를 순회하며 각 몬스터의 `BoxCollider2D.bounds`(`TryGetColliderBounds()` 경유)를 읽어 앞 몬스터까지의 세로 간격을 계산하고, 그 간격 이하로 이번 프레임 이동 거리를 clamp한다.
- 모든 몬스터 프리팹에는 `Rigidbody2D`가 붙어있으며 4종 모두 `m_BodyType: 1`(Kinematic), `m_GravityScale: 0`, `m_Simulated: 1`이다. Kinematic Rigidbody2D를 붙여 이동시키는 오브젝트는 원래 Unity 권장 방식상 `Rigidbody2D.MovePosition()`으로 옮겨야 하지만, 이 코드는 `transform.position`을 직접 대입한다.
- **`ProjectSettings/Physics2DSettings.asset`을 확인한 결과 `m_AutoSyncTransforms: 0`(비활성화), `m_SimulationMode: 0`(FixedUpdate 기준 시뮬레이션)으로 설정되어 있다.** `ProjectSettings/TimeManager.asset`의 `Fixed Timestep: 0.02`(50Hz)이고, 코드 전체를 grep했으나 `Application.targetFrameRate`를 설정하는 곳이 없어 **프레임레이트는 사실상 무제한(디바이스 성능/디스플레이 주사율에 따름)**이다.
- Unity의 2D 물리 시스템은 `Collider2D.bounds`를 포함한 물리 질의(bounds, overlap, raycast 등)에 사용하는 내부 좌표를 `Physics2D.autoSyncTransforms`가 꺼져있으면 스크립트에서 `transform.position`을 바꿔도 **즉시 반영하지 않고, 다음 물리 스텝(FixedUpdate 동기화 시점) 때까지 그대로 유지**한다(Unity 공식 문서에 명시된 동작). 이 프로젝트는 정확히 이 설정을 비활성화한 상태이면서, 물리 동기화 없이 `Update()`에서 직접 `transform.position`을 옮기고, **같은 프레임 안에서** 그 값을 반영해야 할 `_bodyCollider.bounds`를 이용해 다른 몬스터의 다음 이동을 clamp하는 구조다.
- 즉 "몬스터가 실제로 얼마나 움직였는지"(Transform)와 "다른 몬스터가 장애물 판정에 사용하는 위치"(Collider2D.bounds)가 **물리 스텝(0.02초)마다 한 번씩만 동기화**되고, 그 사이 여러 번 실행되는 `Update()` 프레임에서는 이미 이동한 만큼이 Collider 판정에 반영되지 않는다. Fixed Timestep이 50Hz인데 프레임레이트 제한이 없으므로, 60/90/120Hz 등 50Hz를 넘는 환경(에디터 포함 대부분의 PC, 최근 모바일 고주사율 화면)에서는 **하나의 물리 동기화 구간 안에 `Update()`가 2회 이상 실행**되는 경우가 흔하다.
- 이전 조사(1차본)에서 검증했던 수치(그리드 셀 0.85, Collider 월드 크기 0.85, MoveSpeed 0.2 등)는 여전히 사실이며 알고리즘 자체(부호, clamp 범위)의 논리 오류는 이번에도 찾지 못했다. 다만 1차 조사는 "그 순간의 Collider 위치가 항상 정확하다"는 전제 하에 검증한 것이었고, **이번 조사에서 그 전제 자체가 깨질 수 있음(Collider 위치가 최대 한 물리 스텝만큼 낡을 수 있음)을 확인**했다.
- `_activeMonsters` 리스트 관리(`PlaceMonster()`의 `Add`, `HandleMonsterDied()`/`HandleBottomAttackImpact()`의 `Remove`)와 `WaveManager` 싱글톤 중복 여부(씬에 `WaveManager` MonoBehaviour가 정확히 1개만 존재함을 `SampleScene.unity`에서 재확인)도 다시 점검했으나 이상 없었다.
- `_bodyCollider`가 null이 되거나 예기치 않은 컴포넌트가 될 가능성도 4개 프리팹 전부에서 `MonsterBase`와 `BoxCollider2D`가 항상 같은 루트 GameObject에 있음을 재확인해 배제했다. `.enabled = false`/`SetActive(false)`/`Destroy(` 전체를 프로젝트 전역에서 grep한 결과, 몬스터 Collider를 끄는 곳은 여전히 `BeginBottomAttack()` 한 곳뿐이었다.
- 렌더링 정렬(Sorting Order) 문제 여부도 확인했다 — 4개 프리팹 모두 루트 `SpriteRenderer.sortingOrder`가 동일한 고정값 `1`이며, Y 위치 기반 동적 정렬 코드는 어디에도 없다. 이는 겹쳤을 때 "어느 쪽이 위에 그려질지"가 안정적이지 않다는 부수적 문제이긴 하지만, 위치 자체가 겹치는 근본 원인은 아니다(순수 렌더링 착시가 아니라 실제 Transform 위치가 겹친다는 사용자 설명과 일치).

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `Update()`(직접 Transform 이동), `TryGetColliderBounds()`
- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `GetSafeDownwardDistance()`, `CanPlaceMonster()`, `_activeMonsters` 관리
- `Assets/_Project/Prefabs/Monster/{Fluffy,Spider,StoneBug,ForestDeer}.prefab` — `Rigidbody2D`(`m_BodyType: 1` Kinematic, `m_GravityScale: 0`), `BoxCollider2D`
- **`ProjectSettings/Physics2DSettings.asset` — `m_AutoSyncTransforms: 0`, `m_SimulationMode: 0` (이번 개정에서 새로 확인한 핵심 설정)**
- **`ProjectSettings/TimeManager.asset` — `Fixed Timestep: 0.02`(50Hz)**
- 코드 전역에서 `Application.targetFrameRate`, `Physics2D.SyncTransforms()` 호출 여부 grep(둘 다 존재하지 않음을 확인)
- `Assets/Scenes/SampleScene.unity` — `WaveManager` 컴포넌트가 씬에 1개만 존재함을 재확인
- (1차 조사에서 확인, 여전히 유효하나 결론이 바뀐 파일들) `Assets/_Project/Data/MonsterData_*.asset`, 4개 몬스터 스프라이트 PNG/`.meta`

## 문제점 / 구현 대상 파악

### 기각된 가설 1 — "스프라이트가 Collider보다 세로로 조금 크다" (사용자 반박으로 기각)

1차 조사에서 Fluffy(스프라이트 0.99 vs Collider 0.96, 초과 약 0.027 월드 유닛), StoneBug(1.10 vs 0.96, 초과 약 0.124 유닛), ForestDeer(1.98 vs 1.92, 초과 약 0.053 유닛)의 세로 초과를 근거로 들었으나, 이는 그리드 한 칸(0.85 유닛) 대비 최대 15% 수준의 미세한 차이다. 사용자가 확인한 "거의 하나처럼 겹침", "완전히 겹침" 수준(사실상 두 몬스터가 같은 위치까지 파고든 것)과는 규모가 맞지 않는다. **이 원인은 기각한다.** (다만 이 스프라이트 초과분 자체는 사실이므로, 아래 핵심 원인이 수정된 뒤에도 아주 약간의 시각적 겹침 여지는 남을 수 있어 참고용으로만 남겨둔다.)

### 기각까지는 아니나 적용 범위가 좁아 "주된 원인"에서 제외 — 빙결 중 BeginBottomAttack

`WaveManager.CheckGameOver()`/`MonsterBase.BeginBottomAttack()`가 `IsFrozen`을 확인하지 않아 화면 맨 아래 경계(`_bottomBoundaryY = -5`) 부근에서 얼어있는 몬스터가 스스로 Collider를 끄고 돌진해버리는 코드 경로는 이번에도 재확인했고 실제로 존재한다. 그러나 씬 실측값을 계산해보면 스폰 행(row 0~4)은 월드 Y `1.0~4.4` 부근인 반면 `_bottomBoundaryY`는 `-5`로, 그 사이에 몬스터가 다른 몬스터의 방해 없이 최소 5~6 유닛을 더 내려가야 하는 구간이 있다. 사용자가 명시적으로 "하단 근처가 아닌 중상단에서도 자주 벌어졌다"고 확인했으므로, **이 메커니즘은 사용자가 보고한 현상의 주된 원인이 아니라고 판단**한다(다만 하단 부근에서 발생하는 별도의 부차적 버그로는 여전히 유효하며, 그 자체로도 수정 대상이 될 수 있다).

### 핵심 원인(신규) — `Physics2D.autoSyncTransforms` 비활성화로 인한 Collider Bounds 시점 지연(Stale Bounds)

`GetSafeDownwardDistance()`는 매 프레임 `_bodyCollider.bounds`를 읽어 장애물 위치를 판단한다. 이 값은 Unity 2D 물리 엔진(Box2D 기반)이 내부적으로 관리하는 값으로, **스크립트가 `transform.position`을 직접 바꿔도 물리 엔진 쪽 좌표에는 자동으로 반영되지 않는다** — 단, `Physics2D.autoSyncTransforms`가 켜져 있으면 물리 질의 시점에 자동으로 동기화되고, 꺼져 있으면 다음 물리 스텝(`FixedUpdate` 동기화 시점) 전까지 그대로 남아있는다(Unity의 공식 동작). 이 프로젝트는 `ProjectSettings/Physics2DSettings.asset`에서 `m_AutoSyncTransforms: 0`으로 **명시적으로 꺼져 있다.**

몬스터 이동은 `MonsterBase.Update()`(프레임마다 실행, 프레임레이트에 비례해 여러 번 실행될 수 있음)에서 `transform.position`을 직접 옮기는 방식이고, `Fixed Timestep`은 `0.02`초(50Hz)로 고정되어 있으며 프로젝트 어디에도 `Application.targetFrameRate` 제한이 없다. 즉:

- 50Hz보다 높은 프레임레이트(에디터 플레이, 대부분의 PC, 고주사율 모바일 디스플레이 등)에서는 **하나의 물리 동기화 구간(0.02초) 안에 `Update()`가 2회 이상 실행**된다.
- 그 구간 안의 두 번째, 세 번째 `Update()` 호출에서 `GetSafeDownwardDistance()`가 참조하는 앞 몬스터의 `_bodyCollider.bounds`는 **그 구간이 시작될 때의(즉, 이미 지나간) 위치**를 그대로 반환한다 — 앞 몬스터가 그 구간 안에서 실제로 이미 더 내려갔더라도, 또는 자기 자신이 이미 움직인 만큼도 반영되지 않는다.
- 결과적으로 같은 물리 동기화 구간 안에서 실행되는 각 `Update()` 호출은 "이미 소진한 안전 거리"를 매번 다시 허용해버릴 수 있고, 이 오차가 물리 스텝마다(초당 최대 50회) 반복적으로 누적된다. `MoveSpeed`가 0.2유닛/초로 느려 한 프레임의 오차 자체는 작아 보여도(예: 120fps 환경에서 구간당 여분 이동 약 0.006~0.012유닛), 이 누적이 계속되면 단 몇 초 안에 그리드 한 칸(0.85유닛)을 넘어 완전히 겹치거나 통과하는 수준까지 커질 수 있다.
- 이 메커니즘은 몬스터 종류(1×1/2×1/1×2)나 빙결 여부와 무관하게 **모든 몬스터 쌍, 레인의 모든 위치에서 동일하게 작동**하므로, "Fluffy와 Spider가 거의 하나처럼 겹침", "1×1과 2칸짜리가 완전히 겹침", "얼어있는 Spider를 Fluffy가 레인 중상단에서 관통" 세 증상을 **하나의 원인으로 통합해서 설명**할 수 있다. 얼어있는 몬스터는 스스로는 멈춰 있지만(`_frozenSecondsRemaining` 체크로 이동은 정상적으로 막힘), 뒤따르는 몬스터 입장에서 그 몬스터의 `_bodyCollider.bounds`를 읽어 안전 거리를 계산하는 매커니즘 자체가 이 지연 문제의 영향을 그대로 받기 때문에, 얼려서 "정지"시킨 것이 오히려 오차가 드러나기 쉬운 상황(오랫동안 같은 상대 앞에서 반복적으로 안전 거리를 재계산)을 만들어 더 자주 목격됐을 가능성이 높다.
- `MonsterBase`의 `Rigidbody2D`가 전부 `Kinematic`인데도 `Rigidbody2D.MovePosition()`이 아니라 `transform.position` 직접 대입을 쓰는 점도 이 문제와 같은 방향의 안티패턴이다(Unity 공식 가이드는 Kinematic Rigidbody는 반드시 `MovePosition()`으로 옮기라고 명시한다). 두 요인(관행에 어긋난 이동 방식 + `autoSyncTransforms` 비활성화)이 결합되어 물리 질의 결과가 실제 위치보다 지연되는 문제를 만들고 있다고 판단된다.

**확신도**: 높음(프로젝트 설정 파일에서 `m_AutoSyncTransforms: 0`을 직접 확인했고, 이 설정이 정확히 "Transform 직접 이동 후 같은 프레임에 Collider bounds를 읽는" 이 코드 패턴에서 문제를 일으킨다는 것은 Unity의 공식 문서화된 동작이다). **다만 이 환경에는 Unity 에디터/Play Mode를 직접 실행할 수 없어, 실제 런타임에서 `transform.position.y`와 `_bodyCollider.bounds.center.y`가 프레임별로 얼마나 벌어지는지 로그로 직접 재현·측정하지는 못했다.** plan.md 단계에서 수정(예: `Physics2D.autoSyncTransforms = true` 활성화, 또는 이동을 `FixedUpdate`+`Rigidbody2D.MovePosition()`으로 전환, 또는 매 이동 후 `Physics2D.SyncTransforms()` 명시적 호출)을 제안하기 전에, 가능하다면 사용자가 에디터에서 간단히 로그를 찍어 이 가설을 실측으로 한 번 더 확인해주면 좋겠다.

## 결론

1차 조사에서 제시했던 두 원인(스프라이트-Collider 세로 크기 불일치, 빙결 중 BeginBottomAttack 시작)은 사용자가 실제 목격한 겹침·관통의 규모(거의 완전한 겹침) 및 발생 위치(레인 중상단 포함)와 맞지 않아 **주된 원인에서 제외**한다. 재조사 결과, 새로 확인한 핵심 원인은 다음과 같다.

- **핵심 원인(확신도 높음, 세 증상 모두를 통합 설명)**: `ProjectSettings/Physics2DSettings.asset`의 `m_AutoSyncTransforms: 0`(비활성화)과, `MonsterBase.Update()`가 `FixedUpdate`가 아닌 `Update()`에서 `transform.position`을 직접 이동시키는 방식이 결합되어, `WaveManager.GetSafeDownwardDistance()`가 매 프레임 참조하는 `BoxCollider2D.bounds`가 실제 Transform 위치보다 최대 한 물리 스텝(0.02초)만큼 지연된 값을 반환할 수 있다. 프로젝트에 프레임레이트 상한이 없어 50Hz보다 높은 환경에서는 이 지연 구간 안에 `Update()`가 여러 번 실행되며, 그때마다 "이미 소진한 안전 거리"가 반복 허용되어 오차가 누적된다. 이 메커니즘은 몬스터 타입이나 빙결 여부와 무관하게 작동하므로 1×1끼리 겹침, 1×1과 2칸짜리 겹침, 얼어있는 몬스터를 레인 중상단에서 관통하는 현상을 모두 하나의 원인으로 설명할 수 있다.
- **부차적으로 남아있는 별도 이슈(주된 원인은 아니지만 여전히 유효)**: `CheckGameOver()`/`BeginBottomAttack()`이 `IsFrozen`을 확인하지 않아, 화면 맨 아래 경계 부근에서 얼어있는 몬스터가 스스로 돌진을 시작해버리는 경로는 여전히 존재한다. 이는 사용자가 보고한 "레인 중상단 관통"의 주 원인은 아니지만, 하단 경계 부근에서 발생하는 별개의 버그로서 plan.md 단계에서 함께 다룰지 여부를 사용자와 상의해야 한다.
- **기각됨**: 스프라이트가 Collider보다 세로로 커서 생기는 미세한(0.03~0.14유닛) 시각적 겹침 — 규모가 맞지 않아 주된 원인에서 제외. 다만 핵심 원인 수정 후에도 이 미세한 차이 자체는 남아있으므로, 완전히 정확한 시각 정합을 원한다면 별도로 손볼 여지는 있다(이번 버그의 원인은 아님을 명확히 구분해야 한다).
- **에디터 미실행으로 인한 한계**: 이번 조사는 프로젝트 설정 파일과 코드 정적 분석만으로 결론을 냈다. Unity Play Mode를 직접 실행해 프레임별 `transform.position`과 `Collider2D.bounds`의 실측 차이를 로그로 확인하지 못했으므로, "확신도 높음"이라 표기했지만 100% 확정은 아니다. plan.md 진행 전 실측 검증(또는 수정 후 실기기 재검증)이 필요하다.
