# MonsterRules.md

이 문서는 몬스터 시스템(스폰, 전진, HP, 상태이상, 웨이브 진행)에 관한 규칙을 한곳에 모은 통합 문서입니다.
기존에 `GameplayMechanics.md`(스폰/전진 메커닉)와 `UIRules.md`(HP바, 캐릭터 HP/XP)에 흩어져 있던 몬스터 관련 서술과,
현재 코드와 어긋난 낡은 초기 설계 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)를 정리하기 위해 신규 작성되었습니다.
`MonsterBase.cs`/`WaveManager.cs` 실제 코드를 기준으로 작성되었으며, 이후 몬스터 관련 규칙이 추가/변경되면 이 문서를 기준으로 갱신합니다.
이번 갱신에서는 웨이브 구성(전종류 랜덤 등장), 스폰 위치 결정 방식(런타임 랜덤 배치 + 점유 체크), 몬스터별 고정 블록 크기, HP바 표시 방식이 새로 확정되어 반영되었습니다.
확정된 설계이지만 아직 코드에는 반영되지 않은 부분은 각 섹션에 "(구현 예정 — 아직 코드 미반영)" 또는 "(현재 구현 — 아직 새 규칙 미반영)"으로 표시했습니다.
**추가 갱신**: 이후 논의를 거쳐 "웨이브 시작 시 일괄 랜덤 배치 + 점유 체크" 방식은 "웨이브 로스터(수량+종류) 사전 결정 + 그리드 상단 2줄 컨베이어 벨트 방식 채움"으로 전면 대체되었습니다. 함께 웨이브 클리어 조건에 "로스터 전부 소진" 조건이 추가되었고, 바닥 도달 시 웨이브클리어 체크가 누락되던 버그의 수정 방향도 확정되었습니다(2장/6장 참고).

**2026-07-05 갱신**: 실제 `WaveManager.cs`/`MonsterBase.cs` 코드를 다시 읽어 확인한 결과, 위 "로스터 + 상단 2줄(row 3, row 4) 컨베이어 벨트" 방식과 "로스터 전부 소진" 웨이브 클리어 조건, 바닥 도달 시 `CheckWaveCleared()` 호출 누락 버그 수정까지는 이미 코드에 구현되어 커밋된 상태임을 확인했다(2장/6장의 관련 서술을 "(구현 완료)"로 갱신). 이번 라운드에서는 사용자 플레이테스트 결과 새로 발견/논의된 문제 4가지가 추가로 확정되었다(아직 코드 미반영, 2장/6장 참고): **(A)** 몬스터 종류별 프리팹/오브젝트 풀이 1개뿐이라 로스터상 종류와 무관하게 항상 같은 프리팹만 스폰되는 버그의 수정(종류별 프리팹 4개 + 풀 4개로 분리), **(B)** 스폰 트리거 조건을 상단 2줄(row 3, 4)에서 **상단 1행(row 4)** 만으로 단순화하고, 세로 2칸 몬스터(ForestDeer)는 스폰 직전 바로 아래 칸(row 3) 사전 확인 후 배치, **(C)** 컨베이어 체크 틱마다 전 칸을 채우던 방식을 **틱당 3~7마리 무작위 제한**으로 변경, **(D)** 게임 전체 최초 웨이브(웨이브 인덱스 0)에 한해 그리드 **전체 5행**에 즉시 무작위 배치하는 예외 도입(2웨이브부터는 B/C 방식 적용).

**2026-07-05 추가 갱신**: `WaveManager.cs` 실제 코드를 다시 확인한 결과, 바로 위 A(몬스터 종류별 프리팹 4개 + 풀 4개 분리)/B(스폰 트리거를 상단 1행으로 단순화 + 세로 2칸 사전확인)/C(틱당 3~7마리 제한)/D(웨이브 0 전체 그리드 즉시배치) 네 가지는 이미 전부 구현되어 커밋되어 있음을 확인했다(2장/6장의 관련 서술을 전부 "(구현 완료)"로 갱신). 이 구현 위에서 추가 플레이테스트를 거쳐 새로운 버그 2가지와 설계 변경 1가지가 확정되었다(아직 코드 미반영, 2장/6장 참고): **(E)** 가로 2칸(`TwoByOne`, StoneBug)/세로 2칸(`OneByTwo`, ForestDeer) 몬스터가 `PlaceMonster()`에서 앵커로 쓰인 한 칸의 중심 좌표로만 배치되어 스프라이트가 인접 칸에 걸쳐 보이거나 그리드 밖으로 튀어나가는 위치 버그의 수정(두 칸의 중간 지점을 실제 배치 좌표로 사용하도록 변경 + `IsCellFree()` 판정 여유 반경 확대), **(F)** `TryDispenseRoster()`가 매 틱 열을 0번부터 고정 순서로 스캔하다 이번 틱 배치 제한(3~7마리)에 걸려 우측 열이 스캔되지 못해 스폰이 좌측에 편중되는 버그의 수정(매 틱 열 스캔 순서를 무작위로 셔플), **(G)** 기존 웨이브 클리어 조건("활성 몬스터 0 AND 로스터 소진")이 다음 웨이브 스폰 자체를 막고 있던 문제를 해결하기 위해, 로스터가 소진되는 즉시(활성 몬스터 잔존과 무관하게) 다음 웨이브로 넘어가 이어서 스폰하는 오버랩 방식으로 설계 변경(단, 10웨이브는 예외적으로 기존처럼 활성 몬스터 전멸까지 대기하고, 11웨이브는 1웨이브처럼 그리드 전체 즉시배치 방식을 사용하며, 20웨이브(마지막 웨이브)는 기존과 동일하게 두 조건 모두 충족 시 진짜 클리어로 판정).

**2026-07-05 최종 갱신**: 사용자가 위 A~G 전체(프리팹/풀 분리, 스폰 트리거 단순화, 틱당 배치 수 제한, 웨이브 0 전체 그리드 즉시배치, 2칸 몬스터 배치 좌표 보정, 좌측 편중 스폰 수정, 웨이브 오버랩 진행 + 10/11웨이브 예외)를 실제 플레이테스트로 직접 검증했고, 관련 코드는 모두 `main` 브랜치에 머지 완료되었다. `WaveManager.cs`/`MonsterData.cs`/`MonsterBase.cs`와 4종 `MonsterData` 에셋을 다시 읽어 확인한 결과 E/F/G까지 포함한 모든 항목이 실제로 구현되어 있음을 확인했으며, 이 갱신에서 2장/3장/6장의 관련 서술을 전부 "(구현 완료)"로 정정했다. 추가로, 틱당 스폰 수(C)는 `Random.Range(3, 8)` 하드코딩이 아니라 `[SerializeField] private int _minSpawnPerTick = 3`/`_maxSpawnPerTick = 7` 필드로 Inspector에서 조절 가능하게 되어 있음을 확인했다(2장 C 관련 서술에 반영). 3장의 몬스터별 스탯도 4종 에셋(`Assets/_Project/Data/MonsterData_*.asset`)을 직접 읽어 실제 값으로 정정했다(Fluffy/Spider: Hp 30·Reward 10, StoneBug/ForestDeer: Hp 50·Reward 18, 4종 공통 MoveSpeed 0.2).

**2026-07-06 추가 갱신**: Android 실기기에서 스폰 직후 몬스터가 겹친 채 유지되는 현상이 확인되었다. 활성 몬스터는 실제 Collider Bounds를 사용했지만 후보는 오프셋 없는 셀 Bounds를 사용해 Y 중심이 약 `0.203646` 어긋난 것이 원인이었다. 셀 단위 `IsCellFree()`를 제거하고 `MonsterData`별 프리팹 Collider 크기·오프셋·루트 스케일과 풀 부모 스케일을 반영한 후보 전체 Bounds 검사로 교체했다. 일반 상단 스폰과 1·11웨이브 전체 그리드 배치가 같은 `CanPlaceMonster()` 검사를 사용하며, 현재 구현은 Android 실기기 재검증 대기 상태다.

---

## 1. 개요

- 이 문서는 **몬스터 전용 규칙의 단일 기준(source of truth)**입니다. 몬스터와 관련된 내용을 새로 정의하거나 수정할 때는 이 문서를 갱신합니다.
- `GameplayMechanics.md` 섹션 2(몬스터 스폰/전진 시스템)에 있던 본문은 이 문서로 이관되었습니다. 해당 문서에는 안내 문구와 링크만 남아 있습니다.
- 몬스터 HP바(블록 앞면 임베드 방식, 7장 참고) UI, 캐릭터 HP/XP/레벨 처리는 `UIRules.md` 섹션 9, 10에 이미 정의되어 있으므로 이 문서에서 중복 서술하지 않고 7장에서 링크만 겁니다.
- 초기 구현 단계의 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)는 static event 기반 `OnHitMonster`/`LastDamage` 충돌 감지라는 낡은 설계를 담고 있어 현재 코드(Unity 물리 충돌 콜백 + `MonsterBase.TakeDamage` 직접 호출 방식)와 다릅니다. 이 문서는 그 낡은 설계를 참고하지 않고 현재 코드를 기준으로 작성되었습니다.

---

## 2. 몬스터 스폰 및 전진 메커닉

원본 게임 실제 플레이어(사용자)가 확인해준 몬스터 스폰/전진 메커닉이며, 이후 논의를 거쳐 스폰 규칙 일부가 아래와 같이 새로 확정되었습니다. "로스터 + 컨베이어 벨트" 골격과 A(프리팹/풀 4개 분리)/B(상단 1행 트리거 + 세로 2칸 사전확인)/C(틱당 3~7마리 제한)/D(웨이브 0 전체 그리드 즉시배치)/E(2칸 몬스터 배치 좌표 보정)/F(좌측 편중 스폰 수정)/G(웨이브 오버랩 진행 + 10/11웨이브 예외) 전 항목이 `WaveManager.cs`에 실제로 구현되어 있으며, 사용자 플레이테스트로 검증을 마치고 `main` 브랜치에 머지 완료된 상태다(모두 구현 완료).

- **(구현 완료)** 한 웨이브가 시작되면 "이번 웨이브에 스폰해야 할 몬스터 로스터"(총 수량 + 종류 구성)만 먼저 결정된다. 로스터에 담긴 몬스터를 한 번에 전부 배치하지 않고, 그리드 상단 컨베이어 벨트 자리에 빈 칸이 생길 때마다 로스터에서 남은 몬스터를 무작위로 하나 뽑아 그 칸에 채워 넣는 **"로스터 + 컨베이어 벨트" 방식**이 `WaveManager.TryDispenseRoster()`로 실제 구현되어 있다(과거 "웨이브 시작 시 일괄 랜덤 배치" 방식은 이 방식으로 완전히 대체 완료됨). 몬스터 종류/수량을 정하는 공식(스폰 수, 2칸 몬스터 등장 가중치) 자체는 바뀌지 않는다(자세한 알고리즘은 6장 참고).
- **(구현 완료)** 웨이브 1부터 20까지 전 구간에서 Fluffy/Spider/StoneBug/ForestDeer **4종 전부가 랜덤하게 섞여서 등장**한다. 특정 웨이브 구간에서만 종류를 점진적으로 늘리던 기존 방식은 폐지되었다(3장 참고). 웨이브 개수는 기존과 동일하게 20웨이브를 유지한다.
- **(구현 완료)** 스폰 트리거 칸은 **그리드 최상단 1행(row 4, `topRow`)뿐이다.** row 3(`belowRow`, 상단에서 두 번째 줄)은 더 이상 스폰 트리거/배치 대상이 아니며, 최상단에서 스폰된 몬스터가 시간이 지나며 자연스럽게 흘러 내려가면서 지나가는 경유 지점일 뿐이다. 2칸을 차지하는 몬스터(StoneBug/ForestDeer)가 다른 몬스터와 겹치지 않도록 **점유 체크(occupancy check)** 로직을 거쳐 좌표가 결정되는 것도 그대로 유지된다(자세한 내용은 6장 참고). 이전 웨이브의 점유 상태와는 무관하게 매 웨이브 새로 로스터가 계산되고 컨베이어가 새로 시작되는 것도 그대로다.
- **(구현 완료)** 세로 2칸 몬스터(ForestDeer, `OneByTwo`)는 예외로 취급한다. 스폰 대상 칸(앵커)은 최상단 행(row 4, `topRow`)이지만, **스폰하기 직전에 바로 아래 칸(row 3, `belowRow`, 같은 열)도 비어있는지 반드시 사전 확인**한 뒤에만 배치한다. 바로 아래 칸이 비어있지 않으면 이번 틱에는 그 칸에 배치를 시도하지 않고 건너뛰며, 다음 틱에 다시 시도한다.
- **(구현 완료)** 빈 칸 체크 주기는 "그리드 셀 하나를 몬스터가 내려가는 데 걸리는 시간"으로 계산한다: `spawnCheckInterval = _gridCellSize / MoveSpeed`. 이 값은 고정 상수로 하드코딩하지 않고 실제 `MonsterData.MoveSpeed`를 매번 참조해 계산하며, 이동속도가 바뀌면 체크 주기도 자동으로 함께 바뀐다. 현재 4종 몬스터의 `MoveSpeed`는 모두 동일 값(0.2, 최초 기본값 1에서 하향 조정됨 — 4종 에셋 직접 확인)이므로 코드에서는 임의의 한 몬스터 데이터(`WaveTableData.FluffyData`)를 기준으로 참조한다(실측 예: 그리드 셀 크기 0.85(`MonsterOverhaulSetupEditor.cs` 주석에서 확인), MoveSpeed 0.2 기준 → 체크 주기 4.25초).
- **(구현 완료)** 체크 틱마다 배치 가능한 빈 칸을 전부 채우지 않고, 이번 틱에 배치할 몬스터 수를 `[SerializeField] private int _minSpawnPerTick = 3`/`_maxSpawnPerTick = 7` 필드 범위 안에서 **무작위로 결정**(`Random.Range(_minSpawnPerTick, _maxSpawnPerTick + 1)`)하고, 그 수만큼만(또는 배치 가능한 빈 칸이 그보다 적으면 있는 만큼만) 채운다. 두 값 모두 Inspector에서 조절 가능한 필드로 노출되어 있어 하드코딩된 3~7 고정값이 아니다. 한 번에 너무 많은 몬스터가 몰려나와 난이도가 급격히 올라가던 문제의 수정이다.
- **(구현 완료)** 게임 전체에서 가장 첫 웨이브(웨이브 인덱스 0)에 한해서는 예외적으로, 위 컨베이어 방식이 아니라 로스터 전체를 그리드 **전체 5행**에 걸쳐 점유 체크를 거쳐 즉시 무작위로 배치한다(`SpawnRosterAcrossFullGrid()`, 겹침 불가). 화면이 시작부터 몬스터로 차 있는 느낌을 주기 위함이다. **2웨이브부터는** 위 "상단 1행 컨베이어 + 틱당 3~7마리 제한" 방식이 그대로 적용된다.
- **(구현 완료, E)** 가로 2칸(`TwoByOne`, StoneBug)/세로 2칸(`OneByTwo`, ForestDeer) 몬스터가 앵커로 쓰인 한 칸의 중심 좌표로만 배치되어 스프라이트가 인접 칸에 걸쳐 보이거나 그리드 밖으로 튀어나가던 위치 버그가 수정되었다. `PlaceMonster(MonsterData data, Vector3 worldPosition)`가 (col, row) 대신 **최종 월드 좌표(Vector3)**를 직접 받도록 시그니처가 변경되었고, 호출부가 `BlockSize`에 따라 한 칸 중심 또는 두 칸의 평균 위치를 계산한다. 점유 판정은 더 이상 셀 반경이나 `IsCellFree()`를 사용하지 않으며, 후보 프리팹의 BlockSize별 Collider 크기·오프셋·스케일을 최종 위치에 투영한 전체 Bounds로 수행한다.
- **(구현 완료, F)** `TryDispenseRoster()`가 매 틱마다 열(`col`)을 **항상 0번부터 8번까지 고정된 순서**로 스캔하고, 배치 개수가 `maxThisTick`(3~7)에 도달하면 그 자리에서 스캔을 즉시 종료하던, 왼쪽부터 빈 칸을 채우다가 개수 제한에 걸려 멈추기를 반복하면서 오른쪽 열(대략 5~8번)은 스캔 대상에 거의 도달하지 못해 스폰이 좌측에 심하게 편중되던 버그가 수정되었다. 매 틱마다 열 스캔 순서를 무작위로 섞도록 변경되었다: 0~8 열 인덱스로 `colOrder` 리스트를 만들고 Fisher-Yates 셔플(뒤에서부터 `j = Random.Range(0, i + 1)`로 스왑)을 적용한 뒤 그 순서로 스캔한다. 이 덕분에 어떤 열이 이번 틱의 배치 제한(3~7마리) 안에 포함될지가 매번 랜덤하게 정해져 좌우 편중이 사라졌다.
- 한 웨이브의 로스터 수량이 스폰 트리거 칸(상단 1행)의 수용 가능 칸 수보다 많을 수 있으며, 이 경우 일부만 먼저 채우고 나머지는 시간이 지나 자리가 나는 대로 순차적으로 채운다.
- 몬스터는 스폰 직후부터 **시간 경과에 따라 연속적으로(부드럽게)** 화면 하단을 향해 전진한다. 그리드 한 칸씩 딱딱 끊어지는 스텝 이동이 아니다. 상단에서 스폰된 뒤에도 계속 아래로 흘러가므로 상단 1행에 자리가 계속 나며, 컨베이어 벨트처럼 로스터가 소진될 때까지 계속 채워진다.
- 전진은 볼 발사/귀환 사이클과 **무관한 독립적인 시간 흐름**에 따라 진행된다. 볼이 몇 개 남아있든, 귀환했든과 상관없이 몬스터는 계속 전진한다.
- 몬스터가 화면 하단(캐릭터 주변 경계선)에 도달하면 캐릭터 HP를 깎고 소멸한다. 캐릭터 HP 차감 및 경험치(XP) 처리는 `UIRules.md` 섹션 10 "캐릭터 HP / 경험치 / 레벨 시스템"에 이미 정리되어 있으므로 이 문서에서는 중복 서술하지 않는다.
- **(구현 완료, G)** 웨이브 클리어 조건은 원래 **"활성 몬스터 수 == 0"** 그리고 **"이번 웨이브 로스터를 전부 소진(다 스폰 완료)"** 두 조건을 모두 만족해야 다음 웨이브로 넘어가는 것이었다. 그런데 이 조건이 다음 웨이브의 로스터 생성 자체를 막고 있어, 로스터를 다 배출해도 필드에 몬스터가 하나라도 남아있으면 다음 웨이브 스폰이 전혀 시작되지 않는 문제가 확인되었다. 이를 해결하기 위해 `CheckRosterDepleted()`가 로스터 소진 즉시(활성 몬스터 잔존 여부와 무관하게) `AdvanceToNextWave()`를 호출해 다음 웨이브로 넘어가 이어서 스폰하는 오버랩 방식으로 구현되었다. 위 "두 조건 모두 충족" 규칙은 이제 **마지막 웨이브(20웨이브, index 19)** 와 **10웨이브(index 9) → 11웨이브 전환** 두 경우에만(`CheckRosterDepleted()`의 `isLastWave`/`isOverlapExceptionWave` 분기) 실질적으로 적용되고, 그 외 웨이브는 로스터 소진 즉시 다음 웨이브로 넘어간다. 상세 규칙은 6장 참고.
- 게임의 목표는 몬스터 처치를 통한 생존이다.
- 그리드는 **정사각형 셀을 전제**로 한다. 배경 이미지 비율 보정(`BackgroundFitter`/`WallFitter`)은 볼 충돌벽/캐릭터 위치 등 여러 시스템에 영향을 주는 위험도 높은 작업이라 별도의 선행 task로 진행할 예정이며, 이번 문서 갱신 범위에는 포함하지 않는다.

### 구현 현황

관련 코드: `WaveManager.cs`, `MonsterBase.cs`

- **(구현 완료)** `WaveManager.SpawnWave()`는 로스터(종류/수량)만 미리 계산하고, `Update()`가 `spawnCheckInterval`마다 `TryDispenseRoster()`를 호출해 로스터를 점진적으로 소진시킨다. 좌표는 고정값이 아니라 각 `MonsterData` 후보의 최종 월드 위치를 계산한 뒤 `CanPlaceMonster()`가 예상 Collider 전체 Bounds와 활성 몬스터의 실제 Bounds를 비교해 런타임에 결정한다.
- **(구현 완료, B)** `TryDispenseRoster()`는 `topRow = _gridRows - 1`(row 4) 한 줄만 스폰 트리거로 스캔한다(`belowRow = _gridRows - 2`, row 3은 트리거가 아님). 세로 2칸(`OneByTwo`)은 `topRow`/`belowRow`의 평균을 최종 루트 위치로 사용하고, 두 행 높이와 Collider Y 오프셋을 모두 반영한 전체 후보 Bounds가 비어 있을 때만 배치한다.
- **(구현 완료, C)** `TryDispenseRoster()`는 호출마다 `maxThisTick = Random.Range(_minSpawnPerTick, _maxSpawnPerTick + 1)`(기본값 3~7, `[SerializeField]`로 Inspector 조절 가능)으로 이번 틱에 배치할 최대 수를 무작위로 정하고, `placedThisTick`이 그 값에 도달하거나 로스터가 소진되면 즉시 스캔을 중단한다.
- **(구현 완료, D)** `SpawnWave(index)`는 `index == 0`이면 `SpawnRosterAcrossFullGrid()`(그리드 전체 5행에 대해 (rosterIndex, col, row) 후보 조합을 전부 나열한 뒤 무작위로 하나씩 뽑아 배치, 점유 상태는 로컬 `bool[,] free` 배열로 관리)를 호출하고, 그 외에는 기존과 동일하게 `TryDispenseRoster()`를 호출한다.
- **(구현 완료, A)** `Awake()`가 `_fluffyPrefab`/`_spiderPrefab`/`_stoneBugPrefab`/`_forestDeerPrefab` 4개 프리팹 필드로 `MonsterData`를 키로 하는 `Dictionary<MonsterData, ObjectPool<MonsterBase>> _poolByData`를 구성해 종류별 풀 4개를 분리했다. `PlaceMonster()`는 `_poolByData[data].Get()`으로 로스터에서 뽑힌 `MonsterData`에 대응하는 풀에서 인스턴스를 꺼내며, `CheckGameOver()`/`HandleMonsterDied()`의 반납도 `_poolByData[monster.Data].Return(monster)`로 그 몬스터의 현재 `Data` 기준 올바른 풀에 반납한다.
- **(구현 완료, E)** `PlaceMonster(MonsterData data, Vector3 worldPosition)`로 시그니처가 변경되어 2칸 몬스터의 배치 위치가 더 이상 앵커 칸 중심으로 고정되지 않는다. `MonsterBase.TryGetProjectedColliderBounds()`와 `WaveManager.CanPlaceMonster()`가 후보의 실제 Collider 구성을 배치 전에 투영하며, 비교 양쪽 모두 Collider Bounds를 사용한다.
- **(구현 완료, F)** `TryDispenseRoster()`는 더 이상 `for (int col = 0; col < _gridColumns; col++)`로 열을 고정 순서로 스캔하지 않고, Fisher-Yates로 셔플된 `colOrder` 리스트 순서로 스캔해 좌측 편중 버그가 해소되었다(상세는 위 본문 참고).
- **(구현 완료)** `MonsterBase.Update()`가 매 프레임 `MonsterData.MoveSpeed` 기반으로 `Vector3.down` 방향 이동을 수행한다(시간 연속 하강). 볼 발사/귀환 사이클(`BallLauncher`/`Ball`)과는 완전히 독립적으로 동작한다. 이 값이 그대로 `spawnCheckInterval = _gridCellSize / MoveSpeed` 계산의 기준이 된다.
- **(구현 완료)** `WaveManager.CheckGameOver()`가 `Update()`에서 매 프레임 활성 몬스터의 `transform.position.y`가 `_bottomBoundaryY` 이하인지 체크해 하단 도달을 판정하고, 도달 시 리스트 제거 + 풀 반납 + `OnMonsterReachedBottom` 이벤트 발행 + `CheckWaveCleared()` 호출까지 수행한다. 과거 "`CheckGameOver()`가 `CheckWaveCleared()`를 호출하지 않아 몬스터가 전부 바닥으로 빠져나가면 웨이브 클리어 판정이 누락되는" 버그는 이미 수정되었다.
- 냉동(`ApplyFreeze`)/슬로우(`ApplySlow`)도 턴 기반이 아니라 초 단위 타이머로 동작한다(4장/5장 참고).

---

## 3. 몬스터 종류 및 스탯

### 종류 및 고정 블록(베이스) 크기 — 신규 확정

몬스터 프리팹은 **블록(베이스, 발판) + 캐릭터 스프라이트가 합쳐진 하나의 프리팹**이다. 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/`)에서도 몬스터가 블록 발판 위에 서 있는 형태로 확인된다. 4종 모두 스프라이트 픽셀 실측을 기준으로 아래와 같이 종류별 고정 블록 크기가 매칭된다.

| 몬스터 | 블록 크기 | 비고 |
|---|---|---|
| Fluffy | `Block_1x1` | 정사각 1칸 |
| Spider | `Block_1x1` | 정사각 1칸 |
| StoneBug | `Block_2x1` | 가로로 2칸 |
| ForestDeer | `Block_1x2` | 세로로 2칸 |

- `Block_2x2`(정사각 2칸)는 이번 4종 매칭에는 사용하지 않는다.
- 콜라이더는 몬스터가 차지하는 블록 전체 크기를 커버해야 한다(2칸짜리 몬스터는 콜라이더도 2칸만큼 커버).
- 2칸을 차지하는 몬스터(StoneBug/ForestDeer)가 스폰될 때 인접한 다른 몬스터와 겹치지 않도록 **점유 체크(occupancy check)** 로직이 반드시 필요하다(겹침 절대 불가, 랜덤 스폰 — 6장 참고).
- **(구현 완료)** 위 블록 크기 매핑은 `MonsterData._blockSize`(`BlockSize` enum: `OneByOne`/`TwoByOne`/`OneByTwo`) 필드로 실제 코드에 반영되어 있다. `MonsterBase.ApplyBlockSize()`가 `ColliderSizeMap`/`HpBarWidthMap`을 참조해 콜라이더 크기(`OneByOne` 0.96x0.96, `TwoByOne` 1.92x0.96, `OneByTwo` 0.96x1.92)와 HP바 폭을 적용한다. 스폰 점유 체크는 `MonsterBase.TryGetProjectedColliderBounds()`와 `WaveManager.CanPlaceMonster()`가 같은 크기 매핑과 프리팹 오프셋·스케일을 사용한다.

### 웨이브별 등장 구성 — 전종류 랜덤 (신규 확정)

- **(확정)** 웨이브 1~20 전 구간에서 4종(Fluffy/Spider/StoneBug/ForestDeer) 전부가 랜덤하게 섞여서 등장한다. 특정 웨이브 구간에서만 종류가 점진적으로 늘어나던 기존 방식은 폐지되었다(2장 참고).
- **(구현 완료)** 난이도 스케일링: 웨이브가 진행될수록 (a) 웨이브당 스폰 수가 증가하고, (b) 2칸 몬스터(StoneBug/ForestDeer)의 등장 가중치가 증가한다. 정확한 수치 공식은 `WaveTableData`에 파라미터로 구현되어 있다: 스폰 수 = `BaseSpawnCount(10) + SpawnCountPerWave(0.5) * 웨이브 인덱스`(반올림), 2칸 몬스터 등장 가중치 = `Clamp01(BaseTwoCellWeight(0.1) + TwoCellWeightPerWave(0.03) * 웨이브 인덱스)`이며, `WaveManager.SpawnWave()`가 이 두 값을 이용해 매 웨이브 로스터를 계산한다(그리드 용량(`gridColumns * gridRows / 2`)을 넘지 않도록 상한도 함께 건다).

> **(폐기 완료)** `MonsterSetupEditor.SetupWaveSpawnEntries()`가 쓰던 "웨이브 구간별 종류 점진 추가 + 좌표 고정값 굽기" 방식은 로스터+컨베이어 방식으로 완전히 대체되어 더 이상 코드에 존재하지 않는다.

### `MonsterData` (ScriptableObject) 필드

`Assets/_Project/Scripts/Data/MonsterData.cs`

| 필드 | 타입 | 설명 |
|---|---|---|
| `Hp` | float | 최대 체력 |
| `MoveSpeed` | float | 초당 하강 속도 |
| `Damage` | int | 하단 도달 시 캐릭터에게 주는 피해량 |
| `Reward` | int | 처치/통과 시 획득 XP (`UIRules.md` 섹션 10 참고) |

- **(구현 완료)** 2칸 몬스터(StoneBug/ForestDeer)는 1칸 몬스터(Fluffy/Spider)보다 `Hp`/`Reward`가 더 크게 차등을 두어 실제 에셋 값으로 확정·반영되었다: Fluffy/Spider는 `Hp 30` / `Reward 10`, StoneBug/ForestDeer는 `Hp 50` / `Reward 18`이다(아래 실측값 표 참고).
- **(구현 완료)** 4종 `MonsterData` 에셋(`Assets/_Project/Data/MonsterData_*.asset`)의 실제 값을 직접 확인한 결과는 다음과 같다(2026-07-05 기준):

  | 몬스터 | Hp | MoveSpeed | Damage | Reward | BlockSize |
  |---|---|---|---|---|---|
  | Fluffy | 30 | 0.2 | 1 | 10 | `OneByOne`(0) |
  | Spider | 30 | 0.2 | 1 | 10 | `OneByOne`(0) |
  | StoneBug | 50 | 0.2 | 1 | 18 | `TwoByOne`(1) |
  | ForestDeer | 50 | 0.2 | 1 | 18 | `OneByTwo`(2) |

  `MonsterSetupEditor.CreateMonsterDataAssets()`가 생성하던 "4종 동일 기본값(Hp 30 / MoveSpeed 1 / Damage 1 / Reward 10)"은 이후 각 에셋이 개별 조정되어 더 이상 현재 값과 일치하지 않는다. 특히 `MoveSpeed`는 최초 기본값 1에서 4종 전체 0.2로 하향 조정되어 하강 속도가 느려졌다(2장의 `spawnCheckInterval = _gridCellSize / MoveSpeed` 계산도 이 값을 그대로 참조하므로 함께 영향을 받는다).

---

## 4. HP 관리 및 사망 처리

관련 코드: `MonsterBase.cs`, `Ball.cs`

### 풀링 초기화

- `OnSpawn()`: 풀에서 꺼내질 때 호출. `_currentHp`를 `MonsterData.Hp`로 리셋하고 냉동/슬로우/보너스 크리티컬 상태를 모두 초기화한 뒤 `OnHpChanged` 이벤트를 발행한다.
- `ApplyData(MonsterData data)`: `WaveManager.SpawnWave()`가 스폰 직후 호출해 이번에 사용할 `MonsterData`를 주입하고 `_currentHp`를 갱신한다.
- `OnDespawn()`: 풀 반납 시 `_isDead = true`로 설정.

### 데미지 처리 및 사망

- `TakeDamage(float damage)`: `_isDead`면 무시. HP를 차감하고 `OnHpChanged(currentHp, maxHp)` 이벤트를 발행. HP가 0 이하가 되면 `Die()` 호출.
- `Die()`: `_isDead = true` 설정 후 static event `OnMonsterDied` 발행. `WaveManager`가 이를 구독해 풀 반납/킬카운트 증가/웨이브 클리어 판정을 처리한다(6장 참고).
- `public event Action<float, float> OnHpChanged`: 몬스터별 HP바(`MonsterHpBar`, `UIRules.md` 섹션 9)가 구독하는 이벤트.
- `public static event Action<MonsterBase> OnMonsterDied`: `WaveManager`가 구독.

### Ball → Monster 데미지 전달 흐름 (실제 구현 기준)

몬스터에 대한 충돌 감지는 별도의 수동 레이캐스트가 아니라 **Unity 물리 콜백**을 통해 이루어진다.

1. `Ball.OnCollisionEnter2D`에서 상대 오브젝트 태그가 `"Monster"`면 `MonsterBase` 컴포넌트를 가져와 `CalculateDamage(monster)`를 호출한다.
2. `CalculateDamage()`는 `BallData.Damage`(또는 서브볼 데미지 오버라이드)와 `BallData.CriticalChance` + `monster.ConsumeBonusCritChance()`(상태이상으로 누적된 보너스 크리티컬 확률, 5장 참고)를 합산해 크리티컬 여부/최종 데미지를 계산하고, `SkillManager`의 데미지 배율/다음 발사 보너스 데미지를 추가로 적용한다.
3. 계산된 데미지로 `target.TakeDamage(damage)`를 호출해 실제 HP를 차감하고, `LastDamage`(볼의 마지막 적용 데미지, 일부 액티브 스킬이 참조)를 갱신한다.
4. 데미지 적용 후 static event `Ball.OnHitMonster(target, damage, isCritical)`를 발행한다(데미지 텍스트 등 다른 시스템이 소비).
5. 볼의 이동 방향(`velocity.y`)이 아래쪽이면 `OnHitMonsterFront`, 위쪽(귀환 중 튕겨나가는 방향)이면 `OnHitMonsterBack`을 발행한다. 이 두 이벤트는 전면/후면 히트 조건부 패시브 스킬(`AmethystDaggerPassive`/`EmeraldDaggerPassive` 등)이 구독한다.
6. Ball에 장착된 `BallSkillBase` 스킬들의 `OnBallHit(monster)`도 순서대로 호출되어 각 스킬 고유 효과(냉동/슬로우/도트 등, 5장 참고)를 적용한다.
7. Ghost 스킬(`GhostBallSkill`)이 장착되어 콜라이더가 트리거로 전환된 경우에는 `OnCollisionEnter2D` 대신 `OnTriggerEnter2D`에서 동일하게 `CalculateDamage()` + `OnBallHit()`가 호출된다.

> 참고: 초기 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)에 기술된 static event 기반 충돌 감지 설계는 현재 코드와 다르므로 참고하지 않는다. 실제로는 Unity `OnCollisionEnter2D`/`OnTriggerEnter2D` 콜백에서 직접 `TakeDamage()`를 호출하는 방식이며, `OnHitMonster` 등의 static event는 데미지 적용 "이후" 다른 시스템에 알리는 용도로만 쓰인다.

---

## 5. 상태이상 처리

관련 코드: `MonsterBase.cs`

| API | 파라미터 | 동작 |
|---|---|---|
| `ApplyFreeze(float seconds)` | 지속시간(초) | `_frozenSecondsRemaining`을 기존 값과 새 값 중 큰 값으로 갱신(중첩 시 갱신, 값이 줄어들지 않음). `Update()`에서 남은 시간이 0보다 크면 이동을 완전히 멈추고(조기 `return`) 슬로우 타이머도 함께 멈춘다. |
| `ApplySlow(float seconds, float percent)` | 지속시간(초), 감속 비율(0~1) | `_slowSecondsRemaining`/`_slowPercent`를 새 값으로 덮어쓴다(중첩 누적 아님, 마지막 호출 값으로 대체). `Update()`에서 냉동 상태가 아닐 때 `speed *= (1 - percent)`로 이동속도를 감소시키며 타이머를 감소시킨다. |
| `ApplyBonusCritChance(float bonus)` | 크리티컬 확률 보너스 | `_bonusCritChance`에 누적(`+=`) 저장. `Ball.CalculateDamage()`가 다음 피격 시 `ConsumeBonusCritChance()`로 값을 읽고 0으로 리셋한다(1회성 소비). |
| `ApplyDot(float damagePerSec, float duration, int maxStacks)` | 초당 피해, 지속시간(초), 최대 중첩 | 코루틴(`CoDotTick`)을 새로 시작해 1초 간격으로 `damagePerSec * maxStacks`만큼 `TakeDamage()`를 호출한다(`duration`초 동안 반복). |

### 스킬 시스템과의 연동

이 상태이상 API들은 `BallSkillBase`를 상속하는 볼 스킬 클래스들이 `OnBallHit(MonsterBase target)` 또는 전면/후면 히트 이벤트 콜백에서 호출한다.

- `IceBallSkill`(Active): 일정 확률로 `ApplyFreeze` + `ApplySlow`를 동시에 적용하고 추가 데미지도 `TakeDamage`로 직접 부여.
- `FireBallSkill`(Active): 피격 대상에 `ApplyDot`(지속시간/최대중첩/초당피해)을 적용.
- `AmethystDaggerPassive`(Passive, `Ball.OnHitMonsterFront` 구독): 전면 피격 시 `ApplyBonusCritChance` 적용.
- `EmeraldDaggerPassive`(Passive, `Ball.OnHitMonsterBack` 구독): 후면 피격 시 `ApplyBonusCritChance` 적용.

각 스킬의 수치는 `SkillData`/`SkillLevelData`(레벨별 `Value1`/`Value2`/`Value3`)에서 읽어온다.

---

## 6. 웨이브 시스템

관련 코드: `WaveTableData.cs`, `WaveManager.cs`

### 데이터 구조 — 신규 확정 구조

**(구현 완료)** `WaveTableData`는 더 이상 몬스터의 정확한 스폰 좌표를 저장하지 않는다. 웨이브당 "스폰 수"와 "몬스터 종류별 등장 가중치" 같은 **구성 파라미터만** 가지며, `WaveManager`는 이 파라미터로 웨이브 시작 시 "로스터"(스폰할 몬스터 수량+종류 리스트)를 미리 계산해두고, 실제 좌표는 스폰 트리거 칸에 빈 칸이 생길 때마다 점유 체크 로직을 이용해 그때그때 계산한다(2장/3장 참고).

- `WaveTableData`(ScriptableObject, `Assets/_Project/Scripts/Data/WaveTableData.cs`): 실제 필드는 `_baseSpawnCount`/`_spawnCountPerWave`/`_baseTwoCellWeight`/`_twoCellWeightPerWave`/`_totalWaves`와 4종 `MonsterData` 참조(`FluffyData`/`SpiderData`/`StoneBugData`/`ForestDeerData`)뿐이다. 과거 문서에 있던 `WaveEntry`/`MonsterSpawnEntry`(웨이브별 좌표를 미리 구워넣은 리스트 구조)는 이미 삭제되었고, 좌표는 어디에도 저장되지 않는다 — 예정되어 있던 "구성 파라미터만 저장" 구조가 이미 실제 구현이다.
- 로스터 수량/종류를 정하는 공식은 `WaveManager.SpawnWave()`가 `BaseSpawnCount`/`SpawnCountPerWave`/`BaseTwoCellWeight`/`TwoCellWeightPerWave`를 웨이브 인덱스에 곱/가산해 매 웨이브 런타임에 직접 계산한다(`MonsterSetupEditor` 등 에디터 사전 계산 스크립트에 의존하지 않는다).

### `WaveManager` 흐름 — 로스터 + 컨베이어 방식 (A~G 전체 구현 완료)

**(구현 완료)** `WaveManager.SpawnWave()`는 `WaveTableData`에서 이번 웨이브의 스폰 수/가중치 구성만 읽어와 다음 순서로 동작한다(실제 코드 기준).

1. `BaseSpawnCount + SpawnCountPerWave * index`로 스폰 수를, `BaseTwoCellWeight + TwoCellWeightPerWave * index`(0~1 클램프)로 2칸 몬스터 비율을 계산하고, 그리드 용량(`gridColumns * gridRows / 2`)을 넘지 않도록 상한을 건다.
2. 2칸 몬스터 목표 수만큼 StoneBug/ForestDeer를 절반씩 무작위로, 나머지는 Fluffy/Spider를 절반씩 무작위로 뽑아 `_waveRoster` 리스트(종류만, 좌표 없음)를 채운다.
3. `OnWaveStarted`/`OnMonsterCountChanged` 이벤트를 발행한다.
4. **(구현 완료, D)** `index == 0`(게임 전체 최초 웨이브)이면 `SpawnRosterAcrossFullGrid()`를 호출해 로스터 전체를 그리드 전체 5행에 대해 점유 체크를 거쳐 즉시 무작위로 배치한다(겹침 불가). `index >= 1`이면 `TryDispenseRoster()`를 1회 즉시 호출해 웨이브 시작과 동시에 첫 몬스터가 바로 보이도록 한다.
5. 이후 `Update()`가 `spawnCheckInterval = _gridCellSize / MoveSpeed`(2장 참고, `WaveTableData.FluffyData.MoveSpeed` 기준) 주기로 `TryDispenseRoster()`를 반복 호출해 로스터가 소진될 때까지 점진적으로 배치한다(웨이브 0은 4번 단계에서 이미 로스터가 전부 소진되므로 이 반복 호출은 매번 즉시 스킵된다). 이전 웨이브의 점유 상태와는 무관하게 매 웨이브 새로 로스터가 계산되고 컨베이어가 새로 시작된다.

`TryDispenseRoster()`(실제 구현)는 `topRow = _gridRows - 1`(row 4) **한 줄만** 스폰 트리거로 스캔한다(`belowRow = _gridRows - 2`, row 3은 트리거가 아님). **(구현 완료, C)** 호출마다 `maxThisTick = Random.Range(_minSpawnPerTick, _maxSpawnPerTick + 1)`(기본값 3~7, `[SerializeField]`로 Inspector 조절 가능)으로 이번 틱에 배치할 최대 수를 먼저 정하고, `placedThisTick`이 그 값에 도달하거나 로스터가 소진되면 스캔을 즉시 중단한다. 셔플된 각 열에서 로스터의 `MonsterData`별 최종 위치를 먼저 계산하고, `CanPlaceMonster()`가 해당 프리팹의 Collider 크기·오프셋·스케일을 반영한 예상 Bounds와 모든 활성 몬스터의 실제 Bounds를 비교한다. 1×1·2×1·1×2 후보 중 전체 Bounds에 양수 교집합이 없는 후보만 모아 무작위로 하나 배치한다.

**(구현 완료, F)** 과거에는 열 스캔이 항상 `col = 0`부터 8번까지 고정된 순서로 진행되고 이번 틱 배치 제한(3~7마리)에 도달하면 그 자리에서 즉시 종료돼, 왼쪽부터 빈 칸을 채우다 제한에 걸려 멈추기를 반복하면서 오른쪽 열(대략 5~8번)은 스캔 대상에 거의 도달하지 못해 스폰이 좌측에 심하게 편중되는 버그가 있었다. 매 틱마다 열 스캔 순서를 무작위로 셔플하도록 수정되었다(0~8 열 인덱스로 `colOrder` 리스트를 만들어 Fisher-Yates 셔플 후 그 순서로 스캔). 이 덕분에 어떤 열이 이번 틱의 배치 제한 안에 포함될지가 매번 랜덤하게 정해져 좌우 편중이 사라졌다(2장 F 참고).

A~G 전체가 위와 같이 `WaveManager.cs`에 구현되어 있다. 아래 E는 `PlaceMonster()`와 후보 전체 Collider Bounds 검사에 반영된 좌표·점유 보정 내용이다(구현 완료, 상세 배경은 2장 참고).

- **(구현 완료, E)** 과거에는 `PlaceMonster(MonsterData data, int col, int row)`가 한 칸 중심을 그대로 사용해 2칸 몬스터의 실제 점유 중심과 시각 위치가 어긋났다. 현재는 `PlaceMonster(MonsterData data, Vector3 worldPosition)`가 최종 월드 좌표를 받으며, `GetPlacementWorldPosition()`이 1×1은 한 칸 중심, 2×1은 좌우 두 칸 평균, 1×2는 상하 두 칸 평균을 반환한다. 이후 `CanPlaceMonster()`가 후보 전체 Collider Bounds를 검사하므로 셀 중심·반경 근사로 인한 오판은 사용하지 않는다.

### 웨이브 오버랩 스폰 + 10/11웨이브 예외 — G, 구현 완료

- **배경**: 기존 웨이브 클리어 조건("활성 몬스터 0 AND 로스터 전부 소진")은 다음 웨이브 로스터 생성 자체를 막고 있어서, 로스터를 다 배출해도 필드에 몬스터가 하나라도 남아있으면 다음 웨이브 스폰이 전혀 시작되지 않는다. 이 게임은 "빠르게 소환되고 빠르게 렙업해서 볼을 추가/강화하며 클리어하는" 컨셉이라, 로스터가 소진되는 즉시(활성 몬스터 잔존 여부와 무관하게) 다음 웨이브로 넘어가 이어서 스폰하는 것이 올바른 설계로 확정되었다.
- **(구현 완료)** `CheckRosterDepleted()`가 로스터 소진 즉시 호출되어, 그 웨이브가 **마지막 웨이브(20웨이브, index 19, `isLastWave`)도 아니고 10웨이브(index 9, `isOverlapExceptionWave`, 아래 예외 1)도 아니라면**, 활성 몬스터 잔존 여부와 무관하게 `AdvanceToNextWave()`를 호출해 즉시 다음 웨이브 인덱스로 넘어가 새 로스터를 생성하고 이어서 스폰을 시작한다(웨이브 사이 끊김 없음).
- **(구현 완료, 예외 1)** **10웨이브(index 9)** 는 예외로, 로스터가 소진되어도 오버랩하지 않는다(`CheckRosterDepleted()`의 `isOverlapExceptionWave = _currentWaveIndex == 9`). 10웨이브에 한해서는 기존 방식대로 `CheckWaveCleared()`가 "활성 몬스터 0 AND 로스터 소진" 두 조건을 모두 만족해야 **활성 몬스터가 전부 제거될 때까지 대기**한 뒤에야 11웨이브로 넘어간다.
- **(구현 완료, 예외 2)** **11웨이브(index 10)** 는 1웨이브(index 0)와 동일하게, `SpawnWave()`의 `if (index == 0 || index == 10)` 분기에 의해 컨베이어 방식이 아니라 **그리드 전체 5행에 로스터 전체를 즉시 무작위 배치**하는 방식(`SpawnRosterAcrossFullGrid()`)을 사용한다. 12웨이브부터는 다시 평소의 오버랩 컨베이어 방식으로 돌아간다.
- **(구현 완료)** 마지막 웨이브(20웨이브, index 19)는 기존과 동일하게, `CheckWaveCleared()`에서 로스터 소진 + 활성 몬스터 0이 모두 만족되어야 진짜 게임 클리어(`OnAllWavesCleared`)로 판정한다.
- **참고**: 이 설계로 인해 초반 웨이브들(로스터 수량이 적음)은 오버랩이 연쇄적으로 일어나 "웨이브 인덱스"(난이도 계산에 쓰이는 스폰 수/2칸 가중치 공식의 기준)가 실제 체감 진행 속도보다 빠르게 올라갈 수 있음을 알고 진행하기로 했다(의도된 트레이드오프).

**(구현 완료)** `Update() → CheckGameOver()`는 매 프레임 활성 몬스터의 `position.y`가 `_bottomBoundaryY` 이하인지 체크하고(전진은 `MonsterBase.Update()`가 자체적으로 수행), 도달 시 리스트에서 제거 + 풀 반납 + `OnMonsterReachedBottom` 발행 + `OnMonsterCountChanged` 발행에 이어 **`CheckWaveCleared()`까지 호출한다.** 과거 이 호출이 누락되어 몬스터가 죽지 않고 전부 바닥으로 빠져나가면 웨이브 클리어 판정이 영원히 실행되지 않던 버그는 이미 수정 완료되었다.

**(구현 완료)** `MonsterBase.OnMonsterDied` 구독(`OnEnable`/`OnDisable`) → `HandleMonsterDied()`: 리스트 제거, 풀 반납, `_totalKillCount` 증가 → `CheckSkillUnlock()`(`_killCountForSkill`마다 `OnKillCountReached` 발행) → `OnMonsterCountChanged` 발행 → `CheckWaveCleared()`. → `AdvanceToNextWave()`: SkillSelectionPanel에서 스킬 선택 완료 콜백 이후 UIManager가 호출. 웨이브 인덱스를 증가시키고 다음 웨이브를 스폰하거나(모두 소진 시) `OnAllWavesCleared`를 발행.

킬카운트/스킬 언락 로직은 이번 라운드 변경과 무관하게 그대로 유지된다. A(몬스터별 프리팹/풀 분리)/B(트리거 행 범위 단순화)/C(틱당 배치 수 제한)/D(`SpawnWave(0)`/`SpawnWave(10)` 전용 분기)/E(`PlaceMonster()`의 배치 좌표 계산)/F(`TryDispenseRoster()`의 열 스캔 순서)/G(웨이브 오버랩 진행 + 10/11웨이브 예외) 전 항목이 이미 구현 완료되어 `main` 브랜치에 머지되어 있다.

### 웨이브 클리어 조건 — G 구현 완료 이후 적용 범위 축소

- **(구현 완료 — 현재 코드 상태)** 현재 `CheckWaveCleared()`는 웨이브 인덱스와 무관하게 항상 **"활성 몬스터 수 == 0"** AND **"이번 웨이브 로스터를 전부 소진(다 스폰 완료, `_waveRoster.Count == 0`)"** 두 조건을 모두 만족해야 다음 웨이브로 넘어간다. 로스터가 아직 남아있는데 상단 스폰 트리거 칸 배치를 기다리는 짧은 순간 활성 몬스터가 일시적으로 0명이 되는 경우를 웨이브 클리어로 오판하지 않기 위함이다(2장 참고).
- **(구현 완료, G)** 위 "두 조건 모두 충족" 규칙은 이제 **마지막 웨이브(20웨이브, index 19)** 와 **10웨이브(index 9) → 11웨이브 전환** 두 경우에만 실질적으로 적용되도록 범위가 좁혀졌다. 그 외 웨이브(1~9웨이브, 11~19웨이브)는 활성 몬스터 잔존 여부와 무관하게 **"로스터 소진"만으로 즉시 다음 웨이브로 오버랩 진행**한다(`CheckRosterDepleted()`). 상세 규칙은 바로 위 "웨이브 오버랩 스폰 + 10/11웨이브 예외" 단락을 참고.
- **(구현 완료)** `CheckGameOver()`(바닥 도달 몬스터 제거, `Update()`에서 매 프레임 실행)와 `HandleMonsterDied()`(공에 맞아 죽었을 때) 양쪽 모두 몬스터 제거 후 `CheckWaveCleared()`를 호출한다. 과거 `CheckGameOver()`에서 이 호출이 누락되어 몬스터가 죽지 않고 전부 바닥으로 빠져나가는 경우 웨이브 클리어 판정이 누락되던 버그는 수정 완료되었다.

### 스킬 시스템이 참조하는 헬퍼

- `GetWeakestMonster()`: 활성 몬스터 중 `CurrentHp`가 가장 낮은 대상을 반환.
- `GetMonstersInRow(MonsterBase reference)`: 기준 몬스터와 같은 y좌표(±0.1 오차)에 있는 몬스터들을 반환.

이벤트 목록: `OnWaveStarted(int)`, `OnWaveCleared`, `OnAllWavesCleared`, `OnKillCountReached`, `OnMonsterReachedBottom(MonsterBase)`, `OnMonsterCountChanged(int, int)` (모두 static event).

---

## 7. UI 연동 참조

- **(확정)** 몬스터 HP바는 몬스터 머리 위가 아니라 **블록(베이스)의 앞면(정면 하단)에 임베드된 형태**로 표시된다. 폭은 블록의 가로 길이에 비례한다(2칸 블록은 HP바도 그만큼 넓다). 기존 "머리 위 월드 스페이스 캔버스 + 슬라이더" 방식은 폐기되었다. 상세 배치/비율/구현 방식은 `UIRules.md` 섹션 9를 참고한다.
- 캐릭터 HP / 경험치 / 레벨 시스템(하단 도달 시 HP 차감, XP 획득): `UIRules.md` 섹션 10 참고.

이 문서에서는 위 내용을 중복 서술하지 않습니다.

---

## 8. 관련 파일 목록

| 파일 | 경로 | 설명 |
|---|---|---|
| MonsterData.cs | `Assets/_Project/Scripts/Data/MonsterData.cs` | 몬스터 스탯 ScriptableObject (Hp/MoveSpeed/Damage/Reward) |
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 몬스터 HP/이동/상태이상/사망 처리, 풀링(IPoolable) |
| WaveTableData.cs | `Assets/_Project/Scripts/Data/WaveTableData.cs` | 웨이브 스폰 파라미터 ScriptableObject (`BaseSpawnCount`/`SpawnCountPerWave`/`BaseTwoCellWeight`/`TwoCellWeightPerWave`/`TotalWaves` + 4종 `MonsterData` 참조. 과거 `WaveEntry`/`MonsterSpawnEntry` 좌표 리스트 구조는 삭제됨) |
| WaveManager.cs | `Assets/_Project/Scripts/Wave/WaveManager.cs` | 웨이브 스폰/진행/클리어 판정, 킬카운트 기반 스킬 선택 트리거 |
| Ball.cs | `Assets/_Project/Scripts/Ball/Ball.cs` | 몬스터와의 충돌 감지 및 데미지 계산/적용 |
| BallSkillBase.cs | `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` | 볼 스킬 공통 베이스, `OnBallHit(MonsterBase)`에서 몬스터 상태이상 API 호출 |
| MonsterSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` | 몬스터/웨이브 SO 에셋 자동 생성 및 웨이브별 스폰 데이터 자동 설정 에디터 스크립트 |
| MonsterOverhaulSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` | 몬스터 시스템 개편(블록 크기 데이터화 + 웨이브 파라미터화 + 블록 비주얼 합성) 전용 세팅 에디터. `MonsterSetupEditor.cs`는 이 개편에서 수정하지 않으며 새 데이터 구조 세팅은 이 스크립트가 전담 |
