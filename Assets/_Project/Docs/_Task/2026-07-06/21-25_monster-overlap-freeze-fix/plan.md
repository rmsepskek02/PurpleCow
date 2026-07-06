# Plan — 몬스터 이동 겹침/관통 버그 수정

research.md(개정판)에서 확인한 단일 핵심 원인 — `Physics2D.autoSyncTransforms` 비활성화로 인해 `MonsterBase.Update()`가 직접 이동시킨 `transform.position`이 같은 프레임의 `Collider2D.bounds`(=`WaveManager.GetSafeDownwardDistance()`가 장애물 판정에 쓰는 값)에 즉시 반영되지 않는 문제 — 만을 수정한다. 빙결 중 `BeginBottomAttack()` 관련 이슈는 사용자가 실제로 겪은 적이 없다고 확인해 이번 작업 범위에서 완전히 제외한다.

## 구현 목표

- 모든 몬스터 쌍(1×1-1×1, 1×1-2칸짜리, 빙결 여부 무관)에서 `GetSafeDownwardDistance()`가 항상 그 프레임의 **실제 최신 위치**를 반영한 `Collider2D.bounds`를 참조하도록 만들어, 겹침·관통 현상을 제거한다.
- 코드 변경 지점을 최소화하고(DevRules.md "단순함 우선"), 기존 이동 로직(`Update()` 기반 직접 Transform 이동, DoT/상태이상 타이머 등)은 그대로 유지한다.

## 수정 방식 검토 및 선택

research.md에서 제시된 세 후보를 비교했다.

1. **`Physics2D.autoSyncTransforms = true`를 한 곳에서 전역 설정** — Unity의 2D 물리 엔진이 Transform 변경을 물리 질의 시점에 자동으로 동기화하도록 하는 공식 옵션. 한 줄만 추가하면 되고, 프로젝트의 모든 물리 질의(현재의 몬스터-몬스터 Bounds 비교뿐 아니라 향후 추가될 수 있는 다른 Collider 기반 판정도)가 항상 최신 Transform을 반영하게 된다.
2. **`MonsterBase.Update()`를 `FixedUpdate()` + `Rigidbody2D.MovePosition()`으로 전환** — Kinematic Rigidbody2D의 Unity 권장 이동 방식과 정확히 일치하지만, DoT 틱·상태이상 타이머·`UpdateStatusVisual()` 등 `Update()`에 있는 다른 로직과의 실행 순서·프레임 델타 처리 방식을 전부 재검토해야 해서 변경 범위와 리스크가 크다.
3. **몬스터 이동 직후 `Physics2D.SyncTransforms()`를 매 프레임 명시적으로 호출** — 변경 범위는 가장 작지만(`MonsterBase.Update()`에 한 줄 추가), `Physics2D.SyncTransforms()`는 **씬 전체의 물리 대상 Transform을 동기화하는 전역 호출**이라, 활성 몬스터 수만큼(최대 수십 마리) **매 프레임 반복 호출**하게 되어 동일한 전역 동기화 작업이 몬스터 수에 비례해 중복 실행된다. 결과적으로 옵션 1과 동일한 효과를 훨씬 비효율적으로 얻는 셈이라 선택하지 않는다.

**git 히스토리 확인**: `git log -p --follow -- ProjectSettings/Physics2DSettings.asset`로 이 파일이 등장한 모든 커밋을 확인한 결과, `m_AutoSyncTransforms: 0`은 프로젝트 최초 커밋부터 지금까지 한 번도 값이 바뀐 적이 없다. 즉 누군가 의도적으로 끈 설정이 아니라 Unity가 프로젝트 생성 시 부여한 기본값이 그대로 남아있는 것이며, 이 값을 켜도 되돌려야 할 "의도된 동작"은 없다고 판단했다.

**선택: 옵션 1 (`Physics2D.autoSyncTransforms = true` 전역 설정)**. 근거:
- 변경 지점이 한 곳뿐이라 DevRules.md의 "단순함 우선"/"외과적 변경" 원칙에 가장 부합한다.
- 옵션 3처럼 매 프레임·몬스터마다 반복 호출하지 않고 한 번만 설정하면 되므로 성능상으로도 더 낫다.
- 이 프로젝트의 다른 물리 질의(`Ball.cs`의 `OnCollisionEnter2D`/`OnTriggerEnter2D`, `LastMatchPassive.cs`의 `Physics2D.OverlapCircleAll`, `TrajectoryPreview.cs`의 `Physics2D.RaycastAll`)는 모두 Unity 자체의 물리 시뮬레이션(속도 기반 이동, Rigidbody2D 충돌 콜백)에 의해 구동되며 스크립트가 `transform.position`을 직접 대입한 뒤 같은 프레임에 그 결과를 물리 질의로 읽는 패턴이 아니다. 따라서 `autoSyncTransforms`를 켜도 이들의 동작이 달라지지 않고, 부작용 위험은 낮다고 판단했다.
- **설정 위치**: `WaveManager.Awake()`. 이 버그의 근본 원인(몬스터 이동·Collider Bounds 판정)을 소유하는 클래스가 `WaveManager`이므로, 물리 전역 설정이지만 이 서브시스템과 가장 밀접한 곳에 두는 것이 향후 코드를 읽는 사람이 "왜 여기서 이 설정을 켜는지" 맥락을 찾기 쉽다고 판단했다(`GameManager`는 게임 상태/씬 전환만 다루는 클래스라 물리 설정을 두기에 맥락이 맞지 않는다).

## 단계별 작업 계획

1. `Assets/_Project/Scripts/Wave/WaveManager.cs`의 `Awake()`(현재 `protected override void Awake() { base.Awake(); ... 풀 초기화 ... }`) 맨 앞부분에 `Physics2D.autoSyncTransforms = true;` 한 줄을 추가하고, 이유를 설명하는 주석을 짧게 남긴다.
2. 수정 후 파일 전체를 다시 읽어 문법(세미콜론, `using UnityEngine;` 이미 존재하는지 등)과 기존 로직에 영향이 없는지 확인한다.
3. C# 문법·타입 관점에서 직접 검토한다(이 환경에는 Unity 에디터가 없어 실제 컴파일/플레이 검증은 불가능하므로, 문법적 정확성만 최대한 꼼꼼히 확인한다).

## 예상 변경/생성 파일 목록

- 변경: `Assets/_Project/Scripts/Wave/WaveManager.cs` (1개 파일, `Awake()` 내부 1줄 추가 + 주석)
- 생성: 없음

## 주의사항

- 빙결 중 `BeginBottomAttack()` 관련 이슈는 사용자가 겪지 않았다고 확인했으므로 이번 작업에서 전혀 손대지 않는다(`CheckGameOver()`, `BeginBottomAttack()`, `ApplyFreeze()` 등은 모두 그대로 둔다).
- research.md에 남겨둔 "스프라이트가 Collider보다 세로로 조금 크다"는 기각된 가설이므로 `ColliderSizeMap`, 프리팹, 스프라이트 등도 이번 작업에서 건드리지 않는다.
- 이 환경에는 Unity 에디터가 없어 실제 Play Mode 실행으로 겹침이 사라졌는지 실측 검증은 불가능하다. 코드 수정 후 사용자가 에디터/실기기에서 직접 재생해 겹침·관통 재현 여부를 확인해야 한다.
- git commit/push는 이번 작업에서 실행하지 않는다(오케스트레이터가 별도 처리).
