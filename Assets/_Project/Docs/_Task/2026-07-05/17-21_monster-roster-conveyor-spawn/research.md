# Research — 몬스터 로스터+컨베이어 스폰 방식 전환

이 문서는 몬스터 스폰 방식을 "웨이브 시작 시 전체 일괄 스폰"에서 "웨이브 로스터 사전 계산 + 그리드 상단 2줄(row 3, row 4) 컨베이어 벨트 방식 점진 스폰"으로 전환하기 위한 현재 상태 조사 문서다. 설계 자체는 이미 사용자와 논의를 마치고 `MonsterRules.md` 2장/6장에 확정 반영되어 있으며, 이 문서는 그 확정 설계와 현재 코드 사이의 차이를 정리해 다음 단계인 plan.md 작성의 기반을 마련한다. 구체적인 클래스/메서드 설계는 이 문서에서 다루지 않고 plan.md에서 결정한다.

## 현재 상태

`WaveManager.SpawnWave(int index)`는 웨이브가 시작될 때마다 다음과 같이 동작한다.

1. `WaveTableData`의 `_baseSpawnCount`(10) + `_spawnCountPerWave`(0.5) × waveIndex 로 `spawnCount`를, `_baseTwoCellWeight`(0.1) + `_twoCellWeightPerWave`(0.03) × waveIndex 로 `twoCellWeight`를 계산한다(둘 다 그리드 용량 이내로 클램프).
2. 웨이브 시작마다 `bool[_gridColumns, _gridRows]`(9×5) 크기의 `occupied` 배열을 새로 초기화하고, 그리드 **전체**(row 0~4)를 대상으로 2칸 몬스터(가로 `TwoByOne`=StoneBug, 세로 `OneByTwo`=ForestDeer)를 `twoCellWeight` 비율만큼 먼저 배치한다. `GetFreeAnchors`/`MarkOccupied` 헬퍼로 점유 체크를 수행한다.
3. 남은 스폰 수만큼 1칸 몬스터(Fluffy/Spider)로 채운다.
4. 완성된 스폰 리스트(`(MonsterData, Vector2Int anchor)`)를 **한 프레임에 전부** 순회하며 풀에서 꺼내 `ApplyData()` 호출 후 좌표를 계산해 배치한다. 좌표는 `_spawnRoot.position + new Vector3((anchor.x - (_gridColumns-1)/2f) * _gridCellSize, anchor.y * _gridCellSize, 0f)`로, 그리드 중앙 정렬 보정이 포함된 계산식이다.
5. 이후 `_currentWaveTotalCount`를 스폰된 수로 설정하고 `OnWaveStarted`/`OnMonsterCountChanged`를 발행한다.

웨이브 클리어 판정과 하단 도달 처리는 두 갈래로 나뉘어 있다.

- `Update()`에서 매 프레임 실행되는 `CheckGameOver()`는 활성 몬스터 중 `transform.position.y <= _bottomBoundaryY`인 것을 찾아 리스트 제거 + 풀 반납 + `OnMonsterReachedBottom` 발행 + `OnMonsterCountChanged` 발행까지 수행하지만, **`CheckWaveCleared()`를 호출하지 않는 버그가 있다.**
- `MonsterBase.OnMonsterDied` 이벤트를 구독하는 `HandleMonsterDied()`(공격으로 몬스터가 죽었을 때만 호출)는 리스트 제거 + 풀 반납 + 킬카운트 증가 + `CheckSkillUnlock()` + `OnMonsterCountChanged` 발행 후 `CheckWaveCleared()`를 호출한다.
- `CheckWaveCleared()`는 현재 "활성 몬스터 수 == 0"만으로 판정한다. 마지막 웨이브면 `OnAllWavesCleared`를 발행하고, 아니면 `OnWaveCleared`를 발행한 뒤 `AdvanceToNextWave()`를 직접 호출한다.
- `AdvanceToNextWave()`는 웨이브 인덱스를 증가시킨 뒤 다음 웨이브를 스폰하거나(모두 소진 시) `OnAllWavesCleared`를 발행한다.

몬스터 이동은 `MonsterBase.Update()`가 매 프레임 `MonsterData.MoveSpeed` 기반으로 `Vector3.down` 방향 연속 이동을 수행하며, 이는 볼 발사/귀환 사이클과 완전히 독립적으로 동작한다(구현 완료 상태, 그대로 유지). 현재 4종 몬스터의 `MoveSpeed`는 모두 동일 값(1)이다.

`MonsterData`는 `BlockSize` enum(`OneByOne`/`TwoByOne`/`OneByTwo`)과 `Hp`/`MoveSpeed`/`Damage`/`Reward` 필드를 갖는다. `WaveTableData`는 `_baseSpawnCount`/`_spawnCountPerWave`/`_baseTwoCellWeight`/`_twoCellWeightPerWave`/`_totalWaves`(20) 파라미터와 4종 `MonsterData` 참조만 갖고 있으며, 정확한 스폰 좌표는 더 이상 저장하지 않는다(이미 파라미터화된 구조).

Scene에 설정된 주요 값: `_gridCellSize = 0.85`, `_gridColumns = 9`, `_gridRows = 5`, `_bottomBoundaryY = -5`, `PoolRoot` 위치 `(0, 1, 0)`.

### 새로 확정된 목표 설계 (`MonsterRules.md` 2장/6장 반영)

`MonsterRules.md`는 이번 갱신에서 "웨이브 시작 시 일괄 랜덤 배치 + 점유 체크" 방식을 "웨이브 로스터(수량+종류) 사전 결정 + 그리드 상단 2줄 컨베이어 벨트 방식 채움"으로 전면 대체했다고 명시한다(문서 서두, 2장, 6장). 확정된 흐름은 다음과 같다.

1. 웨이브 시작 시 "이번 웨이브에 스폰해야 할 몬스터 로스터"(총 수량 + 종류 구성)만 먼저 계산한다. 스폰 수/2칸 가중치를 정하는 공식(`BaseSpawnCount`/`SpawnCountPerWave`/`BaseTwoCellWeight`/`TwoCellWeightPerWave` 기반) 자체는 바뀌지 않는다 — 바뀌는 것은 "한 번에 배치하느냐 vs 점진적으로 배치하느냐"뿐이다.
2. 로스터를 한 번에 배치하지 않고, 그리드 **상단 2줄(row 3, row 4)**에 빈 칸이 생길 때마다 로스터에서 남은 몬스터를 무작위로 하나 뽑아 그 칸에 채워 넣는다. 2칸 몬스터(ForestDeer, `OneByTwo`)가 다른 몬스터와 겹치지 않도록 점유 체크 로직은 그대로 유지하되, 대상 범위를 상단 2줄로 한정한다. ForestDeer가 뽑혀 그 열의 상단 2줄 전체를 정확히 차지하는 것은 의도된 정상 동작이다.
3. 빈 칸 체크 주기는 `spawnCheckInterval = _gridCellSize / MoveSpeed`로 계산하며, 하드코딩 상수가 아니라 `MonsterData.MoveSpeed`를 매번 참조해 계산해야 한다(이동속도가 바뀌면 체크 주기도 자동으로 바뀌어야 함).
4. 로스터 수량이 상단 2줄 수용 칸 수보다 많으면 일부만 먼저 채우고, 몬스터가 아래로 흘러가 자리가 나는 대로 나머지를 순차적으로 채운다. 이전 웨이브의 점유 상태와 무관하게 매 웨이브 새로 로스터가 계산되고 컨베이어가 새로 시작된다.
5. 웨이브 클리어 조건은 "활성 몬스터 수 == 0" AND "이번 웨이브 로스터를 전부 소진(다 스폰 완료)" 두 조건을 모두 만족해야 한다. 로스터가 아직 남아있는데 상단 2줄 배치를 기다리는 짧은 순간 활성 몬스터가 일시적으로 0명이 되는 경우를 웨이브 클리어로 오판하지 않기 위함이다.
6. `CheckGameOver()`가 `CheckWaveCleared()`를 호출하지 않는 버그를 로스터+컨베이어 도입과 함께 수정한다. 몬스터 제거 후 `CheckWaveCleared()`를 호출하도록 추가해야 한다.

## 관련 파일 및 의존성

| 파일 | 경로 | 역할 |
|---|---|---|
| WaveManager.cs | `Assets/_Project/Scripts/Wave/WaveManager.cs` | 웨이브 스폰/진행/클리어 판정을 담당하는 핵심 클래스. `SpawnWave`, `CheckGameOver`, `CheckWaveCleared` 등이 이번 작업의 직접 수정 대상 |
| WaveTableData.cs | `Assets/_Project/Scripts/Data/WaveTableData.cs` | 웨이브 20개 구성 파라미터(스폰 수/2칸 가중치 공식, 4종 `MonsterData` 참조)를 갖는 ScriptableObject. 로스터 계산 공식의 데이터 소스 |
| MonsterData.cs | `Assets/_Project/Scripts/Data/MonsterData.cs` | 몬스터 스탯(`Hp`/`MoveSpeed`/`Damage`/`Reward`) + `BlockSize` ScriptableObject. `MoveSpeed`가 `spawnCheckInterval` 계산의 기준 |
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 몬스터 이동(`Update()`의 연속 하강)/HP/상태이상/사망 처리. 이동 로직은 이번 작업으로 변경되지 않지만 `MoveSpeed` 참조 관계가 있어 의존성으로 포함 |
| MonsterRules.md | `Assets/_Project/Docs/MonsterRules.md` (2장, 6장) | 로스터+컨베이어 방식의 단일 기준 문서. 이번 작업이 반영해야 할 확정 설계의 근거 |
| WaveTableData.asset | `Assets/_Project/Data/WaveTableData.asset` | 위 `WaveTableData.cs`의 실제 데이터 에셋 (20웨이브 파라미터 값이 저장된 인스턴스) |

## 문제점 / 구현 대상 파악

- **일괄 스폰 → 로스터+컨베이어 구조 전환에 필요한 신규 상태**: 현재 `SpawnWave()`는 로컬 변수(`spawnList`)로 계산과 스폰을 한 번에 끝내는 구조라 별도 상태를 갖지 않는다. 점진적 스폰으로 바뀌면 다음과 같은 상태를 새로 관리해야 한다.
  - 로스터 큐/리스트: 이번 웨이브에 아직 스폰되지 않은 몬스터 종류 목록 (순서는 무작위 추출이므로 큐라기보다 "남은 항목 집합"에 가까움)
  - 컨베이어 타이머: `spawnCheckInterval` 주기로 상단 2줄 빈 칸을 체크하기 위한 경과 시간 누적 또는 스케줄링 상태
  - "로스터 소진 여부" 플래그(또는 로스터 남은 개수를 0과 비교): 웨이브 클리어 판정의 두 번째 조건에 사용
- **점유 체크 범위 축소**: 기존 `GetFreeAnchors`/`MarkOccupied`는 `_gridRows` 전체(0~4행)를 대상으로 하는 범용 헬퍼다. 새 방식에서는 이 로직을 그대로 재사용하되 대상 범위를 상단 2줄(row 3, row 4)로 한정해야 한다. 헬퍼 시그니처에 row 범위를 파라미터로 추가할지, 별도 오버로드를 만들지는 plan.md에서 결정할 사항이다.
- **`spawnCheckInterval` 구현 방식 미정**: 코루틴(`WaitForSeconds`)으로 구현할지, `Update()` 내 누적 타이머 방식으로 구현할지는 아직 결정되지 않았다. 이는 이 research.md의 범위를 벗어나며 plan.md에서 구체적으로 확정할 사항이다.
- **`CheckGameOver()` 버그 수정 필요**: 현재 `CheckGameOver()`는 하단 도달 몬스터를 제거하지만 `CheckWaveCleared()`를 호출하지 않아, 몬스터가 죽지 않고 전부 바닥으로 빠져나가는 경우 웨이브 클리어 판정이 영구히 누락되는 버그가 있다. 로스터+컨베이어 도입과 함께 이 호출을 추가해야 한다.
- **`CheckWaveCleared()` 조건 확장 필요**: 현재는 "활성 몬스터 수 == 0"만으로 판정하므로, 로스터 소진 여부를 함께 검사하도록 조건을 확장해야 한다.
- **좌표 계산 로직은 재사용 가능**: 그리드 좌표 → 월드 좌표 변환식(`_spawnRoot.position + new Vector3((anchor.x-(_gridColumns-1)/2f)*_gridCellSize, anchor.y*_gridCellSize, 0f)`) 자체는 이번 변경으로 바뀌지 않으며, 스폰 시점/빈도만 바뀐다.

## 결론

현재 코드(`WaveManager.SpawnWave`, `CheckGameOver`, `CheckWaveCleared`)는 `MonsterRules.md`에 이미 확정된 로스터+컨베이어 설계와 명확히 어긋나 있으며, 특히 (1) 웨이브 전체 일괄 스폰 구조, (2) 점유 체크 대상이 그리드 전체(row 0~4)로 되어 있는 점, (3) `CheckGameOver()`가 `CheckWaveCleared()`를 호출하지 않는 버그, (4) 웨이브 클리어 조건에 로스터 소진 여부가 빠져 있는 점이 확인되었다. 로스터 상태 관리 방식, 컨베이어 타이머 구현 방식(코루틴 vs `Update()` 누적 타이머), 점유 체크 헬퍼의 범위 파라미터화 방식 등 구체적인 클래스/메서드 설계는 다음 단계인 plan.md에서 확정한다.
