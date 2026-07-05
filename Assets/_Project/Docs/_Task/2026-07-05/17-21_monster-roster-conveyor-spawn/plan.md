# Plan — 몬스터 로스터+컨베이어 스폰 방식 전환

이 문서는 `research.md`에서 확인된 현재 코드와 `MonsterRules.md`(2장/6장) 확정 설계 간의 차이를 해소하기 위해 `WaveManager.cs`를 어떻게 재구성할지 구체적으로 계획한다. 핵심은 "웨이브 시작 시 전체 일괄 스폰"을 "웨이브 로스터 사전 계산 + 그리드 상단 2줄 컨베이어 벨트 방식 점진 스폰"으로 교체하는 것이며, 정적 점유 배열(`occupied`) 기반 판정을 실시간 위치 기반 판정으로 대체하고, 웨이브 클리어 조건 확장 및 `CheckGameOver()` 버그 수정도 함께 진행한다. 아래 설계는 이미 사용자와 논의를 마치고 확정된 내용이며, 이 문서는 그 확정 설계를 `WaveManager.cs` 구현 단위로 정리한다.

## 구현 목표

- `WaveManager.SpawnWave(int index)`가 몬스터를 한 프레임에 일괄 배치하지 않고, 이번 웨이브에 스폰할 몬스터 목록("로스터")만 미리 계산해두도록 변경한다.
- 그리드 상단 2줄(row 3, row 4)에 빈 칸이 생길 때마다 `spawnCheckInterval`(`_gridCellSize / MoveSpeed`) 주기로 로스터에서 무작위로 몬스터를 뽑아 채워 넣는 컨베이어 벨트 방식을 구현한다.
- 정적 `bool[,] occupied` 배열과 `GetFreeAnchors`/`MarkOccupied`를 제거하고, 실시간 `_activeMonsters` 위치를 기준으로 빈 칸 여부를 판정하는 방식으로 전환한다.
- 웨이브 클리어 조건에 "로스터 소진" 조건을 추가하고, `CheckGameOver()`가 `CheckWaveCleared()`를 호출하지 않는 기존 버그를 수정한다.
- 변경 대상은 `WaveManager.cs` 하나로 한정하며, `WaveTableData.cs`/`MonsterData.cs`/`MonsterBase.cs`는 수정하지 않는다.

## 단계별 작업 계획

### 1. 필드 정리

- 제거: `SpawnWave` 내부 지역 변수였던 `bool[,] occupied`, 그리고 이를 다루던 `GetFreeAnchors(bool[,], int, int)` / `MarkOccupied(bool[,], Vector2Int, int, int)` 메서드 전체를 삭제한다(웨이브 시작 시 일괄 배치를 전제로 한 방식이라 컨베이어 방식과 맞지 않음).
- 추가: `private List<MonsterData> _waveRoster = new List<MonsterData>();` — 이번 웨이브에 아직 스폰되지 않고 남은 몬스터 목록(순서 의미 없는 "bag").
- 추가: `private float _spawnCheckTimer;` — 컨베이어 벨트 빈 칸 체크용 누적 타이머(`Update()`에서 사용).
- 기존 `_currentWaveTotalCount`, `_activeMonsters`, `_currentWaveIndex`, `_totalKillCount` 등은 그대로 유지한다.

### 2. `SpawnWave(int index)` 재작성

기존과 동일하게 스폰 수/2칸 가중치 계산까지는 그대로 재사용하되, 그 결과를 즉시 배치하지 않고 로스터에 채워 넣는 구조로 바꾼다.

1. 기존과 동일한 공식으로 `spawnCount`(`BaseSpawnCount + SpawnCountPerWave * index`, 반올림)와 `twoCellWeight`(`BaseTwoCellWeight + TwoCellWeightPerWave * index`, 0~1 클램프)를 계산한다.
2. 기존과 동일하게 `capacityLimit = (_gridColumns * _gridRows) / 2`로 `spawnCount`를 상한 제한한다.
3. `_waveRoster.Clear()` 후, `twoCellTarget = Mathf.Min(Mathf.RoundToInt(spawnCount * twoCellWeight), spawnCount)`개만큼 `StoneBugData` 또는 `ForestDeerData`를 50:50 무작위로 뽑아 `_waveRoster`에 추가한다(기존 코드의 `horizontal` 랜덤 분기 로직을 좌표 계산 없이 데이터 선택 용도로만 재사용).
4. 나머지 `spawnCount - twoCellTarget`개만큼 `FluffyData` 또는 `SpiderData`를 50:50 무작위로 뽑아 `_waveRoster`에 추가한다.
5. `_currentWaveTotalCount = _waveRoster.Count;`로 설정한다(스폰 시작 시점 총합, UI 카운트 표시 로직은 변경 없음).
6. `OnWaveStarted?.Invoke(index + 1);`, `OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);`를 기존과 동일하게 발행한다.
7. 웨이브 시작과 동시에 첫 몬스터가 바로 나타나도록, 메서드 마지막에서 `TryDispenseRoster();`를 한 번 즉시 호출한다(타이머 첫 도달을 기다리지 않음).

### 3. `Update()` — 컨베이어 타이머 추가

```
private void Update()
{
    CheckGameOver();

    if (_waveRoster.Count == 0)
        return; // 로스터 소진 시 스캔 자체를 건너뛰어 성능 낭비 방지

    _spawnCheckTimer += Time.deltaTime;

    float referenceMoveSpeed = _waveTable.FluffyData.MoveSpeed;
    float spawnCheckInterval = _gridCellSize / referenceMoveSpeed;

    if (_spawnCheckTimer >= spawnCheckInterval)
    {
        _spawnCheckTimer -= spawnCheckInterval;
        TryDispenseRoster();
    }
}
```

- 코루틴 방식이 아니라 `Update()` 누적 타이머 방식을 채택한다(기존 `CheckGameOver()`도 `Update()` 기반이라 스타일 일관성을 유지하기 위함).
- `spawnCheckInterval`은 매번 실시간 계산하며 캐싱하지 않는다(하드코딩 상수 금지).
- `referenceMoveSpeed`는 `WaveTableData`에 연결된 4종 `MonsterData` 중 하나(`FluffyData.MoveSpeed`)를 참조한다. 4종의 `MoveSpeed`가 모두 동일 값이라는 전제 하에 임의의 하나를 참조해도 무방하다(주의사항 참고).

### 4. `IsCellFree(int col, int row)` — 실시간 위치 기반 점유 판정 (신규)

기존 정적 `occupied` 배열 방식은 "웨이브 시작 시 한 번에 배치"를 전제로 하며, 몬스터가 계속 아래로 흘러가는 컨베이어 구조와 맞지 않으므로 전면 대체한다.

```
private bool IsCellFree(int col, int row)
{
    Vector3 cellWorldPos = GridToWorldPosition(col, row);
    float halfCell = _gridCellSize / 2f;

    foreach (MonsterBase monster in _activeMonsters)
    {
        Vector3 pos = monster.transform.position;
        if (Mathf.Abs(pos.x - cellWorldPos.x) < halfCell && Mathf.Abs(pos.y - cellWorldPos.y) < halfCell)
            return false;
    }
    return true;
}
```

- 판정 대상 셀은 항상 상단 2줄(`row = _gridRows - 2` 또는 `row = _gridRows - 1`, 5행 그리드 기준 row 3/row 4)로 한정해서 호출된다.
- 셀의 월드 좌표 계산은 기존 `SpawnWave()`에 있던 `anchor → world` 변환식을 그대로 재사용하되, 중복을 피하기 위해 아래처럼 공용 헬퍼로 추출한다.

```
private Vector3 GridToWorldPosition(int col, int row)
{
    return _spawnRoot.position + new Vector3(
        (col - (_gridColumns - 1) / 2f) * _gridCellSize,
        row * _gridCellSize,
        0f
    );
}
```

### 5. `TryDispenseRoster()` — 컨베이어 벨트 배치 로직 (신규)

상단 2줄(row 3, row 4)을 스캔하면서 빈 칸에 로스터의 몬스터를 무작위로 뽑아 채운다. 2칸 몬스터는 필요한 2칸(가로 인접 또는 세로 인접, `BlockSize`에 따라)이 모두 비어있어야 배치 가능하다.

동작 절차:

1. `_waveRoster.Count == 0`이면 즉시 반환한다.
2. 상단 2줄(`midRow = _gridRows - 2`, `topRow = _gridRows - 1`)에 대해 열(`col`) 순서로 스캔한다. 이번 스캔 패스 동안 이미 배치를 확정한 셀은 계속 "비어있지 않음"으로 취급해 중복 배치를 막는다(패스 시작 시 `IsCellFree` 결과를 셀 단위로 캐싱해두고, 배치가 일어날 때마다 해당 캐시를 갱신하는 방식을 권장).
3. 각 빈 칸 후보에 대해 그 칸(들)에 실제로 들어갈 수 있는 몬스터 크기를 판별한다.
   - `OneByOne`(Fluffy/Spider): 대상 칸 하나만 비어있으면 배치 가능.
   - `TwoByOne`(StoneBug, 가로 2칸): 같은 행(`midRow` 또는 `topRow`)에서 인접한 두 열(`col`, `col+1`)이 모두 비어있어야 배치 가능.
   - `OneByTwo`(ForestDeer, 세로 2칸): 같은 열에서 `midRow`와 `topRow`가 모두 비어있어야 배치 가능. 배치되면 그 열의 상단 2줄 전체를 정확히 채우는 것이 의도된 정상 동작이다.
4. 현재 칸(들)에 배치 가능한 크기와 일치하는 `MonsterData`들을 `_waveRoster`에서 찾아, 그중 하나를 무작위로 뽑아 배치한다(`PlaceMonster` 호출) 후 로스터에서 제거하고, 스캔 캐시에서 해당 칸(들)을 "사용됨"으로 표시한다.
5. 더 이상 배치할 수 있는 빈 칸이 없거나 로스터가 소진되면 그 체크 틱을 종료한다.
6. 로스터가 상단 2줄 수용력보다 많으면 이번 틱엔 일부만 배치되고, 몬스터가 흘러 내려가 칸이 비면 다음 체크 틱에 나머지가 순차적으로 배치된다(매 틱마다 그 시점 빈 칸을 다시 스캔하므로 별도 로직 없이 자연히 해결됨).

### 6. `PlaceMonster(MonsterData data, int col, int row)` — 배치 헬퍼 (신규, 기존 스폰 로직 재사용)

기존 `SpawnWave()`의 "그리드 좌표 → 월드 좌표 변환 후 스폰" 블록을 그대로 헬퍼 메서드로 옮긴다.

```
private void PlaceMonster(MonsterData data, int col, int row)
{
    MonsterBase monster = _monsterPool.Get();
    if (data != null)
        monster.ApplyData(data);

    monster.transform.position = GridToWorldPosition(col, row);
    _activeMonsters.Add(monster);

    OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);
}
```

- 세로 2칸(ForestDeer) 배치 시 앵커 좌표를 `midRow`(row 3)와 `topRow`(row 4) 중 어느 쪽으로 둘지, 그리고 실제 스프라이트 중심이 두 칸의 중간에 오도록 오프셋을 줄지는 몬스터 프리팹의 피벗 설정에 따라 구현 시점에 확인이 필요하다(주의사항 참고).

### 7. `CheckWaveCleared()` 조건 확장

```
private void CheckWaveCleared()
{
    if (_activeMonsters.Count == 0 && _waveRoster.Count == 0)
    {
        // 이하 기존 로직(마지막 웨이브 판정, OnAllWavesCleared/OnWaveCleared 발행, AdvanceToNextWave 호출) 그대로 유지
    }
}
```

- 판정 조건만 `_activeMonsters.Count == 0` 단일 조건에서 `_activeMonsters.Count == 0 && _waveRoster.Count == 0` 두 조건 모두로 확장하고, 조건 충족 후의 내부 로직(마지막 웨이브 여부에 따른 `OnAllWavesCleared`/`OnWaveCleared` 분기, `AdvanceToNextWave()` 호출)은 변경하지 않는다.

### 8. `CheckGameOver()` 버그 수정

기존 코드는 하단 도달 몬스터 제거(`_activeMonsters`에서 제거 + 풀 반납 + `OnMonsterReachedBottom` 발행 + `OnMonsterCountChanged` 발행)까지만 하고 `CheckWaveCleared()`를 호출하지 않는다. 몬스터 제거 로직 마지막에 `CheckWaveCleared();` 호출을 추가한다(기존 `HandleMonsterDied()`가 이미 하는 방식과 동일).

```
private void CheckGameOver()
{
    for (int i = _activeMonsters.Count - 1; i >= 0; i--)
    {
        MonsterBase monster = _activeMonsters[i];
        if (monster.transform.position.y <= _bottomBoundaryY)
        {
            _activeMonsters.RemoveAt(i);
            _monsterPool.Return(monster);
            OnMonsterReachedBottom?.Invoke(monster);
            OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);

            CheckWaveCleared();
        }
    }
}
```

### 9. 영향받지 않는 부분 확인

- `GetWeakestMonster()`, `GetMonstersInRow()`, `HandleMonsterDied()`, `CheckSkillUnlock()`, `AdvanceToNextWave()`, 이벤트 목록(`OnWaveStarted`/`OnWaveCleared`/`OnAllWavesCleared`/`OnKillCountReached`/`OnMonsterReachedBottom`/`OnMonsterCountChanged`)은 이번 변경으로 시그니처/동작이 바뀌지 않는다.
- `MonsterBase.Update()`의 연속 하강 이동 로직, `Ball`/`SkillManager` 관련 로직은 이번 작업 범위에 포함되지 않는다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Wave/WaveManager.cs` (수정)
  - 필드 추가: `_waveRoster`(`List<MonsterData>`), `_spawnCheckTimer`(`float`)
  - `SpawnWave(int index)` 재작성 — 로스터 계산만 수행, 배치는 `TryDispenseRoster()`에 위임
  - `Update()`에 컨베이어 타이머 로직 추가(로스터 소진 시 스캔 건너뛰기 포함)
  - `IsCellFree(int col, int row)` 신규 메서드 추가 — 실시간 위치 기반 점유 판정
  - `GridToWorldPosition(int col, int row)` 신규 헬퍼 추가 — 기존 좌표 변환식 재사용/공용화
  - `TryDispenseRoster()` 신규 메서드 추가 — 상단 2줄 컨베이어 벨트 배치 로직
  - `PlaceMonster(MonsterData data, int col, int row)` 신규 헬퍼 추가 — 기존 스폰 배치 블록 이관
  - `GetFreeAnchors(bool[,], int, int)` / `MarkOccupied(bool[,], Vector2Int, int, int)` 제거(정적 점유 배열 방식 폐기)
  - `CheckWaveCleared()` 조건을 로스터 소진 여부까지 확장
  - `CheckGameOver()`에 `CheckWaveCleared()` 호출 추가(버그 수정)
- 그 외 파일(`WaveTableData.cs`, `MonsterData.cs`, `MonsterBase.cs`)은 변경하지 않는다.

## 주의사항

- 4종 몬스터(`Fluffy`/`Spider`/`StoneBug`/`ForestDeer`)의 `MoveSpeed`가 모두 동일하다는 전제 하에 `spawnCheckInterval` 계산 시 `FluffyData.MoveSpeed` 하나만 참조한다. 향후 몬스터별로 이동속도가 달라지면 이 참조 방식(어떤 몬스터 기준으로 체크 주기를 계산할지)을 재검토해야 한다.
- 실시간 위치 기반 점유 판정(`IsCellFree`)은 몬스터의 실제 콜라이더/스프라이트 크기가 아니라 그리드 셀 크기(`_gridCellSize`) 기준 근접도로 판정한다. 블록 시각 크기와 판정 범위가 어긋나지 않는지는 실제 플레이 테스트로 확인이 필요하다(이 원격 환경에는 Unity 에디터가 없으므로, 에디터에서 직접 실행/확인하는 작업은 사용자 로컬 환경에서 진행해야 한다).
- 세로 2칸(ForestDeer) 배치 시 앵커를 `midRow`/`topRow` 중 어디로 둘지, 프리팹 피벗 기준으로 두 칸의 정중앙에 스프라이트가 오는지는 실제 프리팹을 띄워보며 조정이 필요할 수 있다.
- 로스터 소진 후 `_activeMonsters.Count == 0`이 되는 마지막 구간에서 `CheckGameOver()`가 정상적으로 `CheckWaveCleared()`를 호출하는지, 마지막 웨이브(`_totalWaves`)의 `OnAllWavesCleared` 발행 타이밍이 기존과 동일하게 유지되는지 플레이 테스트로 확인이 필요하다.
- `TryDispenseRoster()`의 한 스캔 패스 내에서 이미 이번 틱에 배치를 확정한 셀을 "사용됨"으로 표시해 중복 배치를 막아야 한다(패스 도중 `IsCellFree`를 매번 다시 호출하면, 방금 배치한 몬스터가 아직 실제로 이동하지 않은 상태이므로 최신 `_activeMonsters` 리스트를 즉시 반영해 정상적으로 "비어있지 않음"으로 판정되긴 하지만, 구현 시 이 순서 의존성을 명확히 인지하고 작성해야 한다).
- 이 문서는 계획 문서 작성 단계이며, 실제 `WaveManager.cs` 코드 수정은 사용자의 명시적 승인 후 별도로 진행한다.

---

## 추가 확정 사항 반영 (2026-07-05 플레이테스트 피드백: A/B/C/D)

플레이테스트 결과 4가지 문제/개선사항이 추가로 확정되어 `MonsterRules.md`(2장/6장)에 이미 반영되었다. 이 섹션은 그 확정 사항을 `WaveManager.cs` 구현 단위로 구체화한다. 아래 설계는 이미 사용자와 논의를 마치고 확정된 내용이므로 그대로 반영하며, 임의로 다른 대안을 제안하지 않는다. 위 1~9번 계획(로스터/컨베이어 골격, `IsCellFree`, `GridToWorldPosition`, `CheckWaveCleared`, `CheckGameOver` 버그 수정 등)은 이미 구현 완료된 것으로 간주하고, 이 섹션에서는 그 위에 추가/변경되는 부분만 기술한다.

### A. 몬스터 종류별 프리팹 풀 분리

**원인**: 현재 `WaveManager`는 `_monsterPrefab` 필드 1개 + `_monsterPool`(`ObjectPool<MonsterBase>`) 1개만 가지므로, 로스터에서 어떤 `MonsterData`를 뽑든 항상 같은 프리팹 인스턴스가 스폰된다. `MonsterBase.ApplyData()`는 스탯/콜라이더/HP바만 갱신하고 캐릭터 스프라이트는 바꾸지 않기 때문이다.

**수정 내용**:

1. 필드 교체: `_monsterPrefab`(`MonsterBase`)과 `_monsterPool`(`ObjectPool<MonsterBase>`)을 제거하고, 아래로 대체한다.

```csharp
[SerializeField] private MonsterBase _fluffyPrefab;
[SerializeField] private MonsterBase _spiderPrefab;
[SerializeField] private MonsterBase _stoneBugPrefab;
[SerializeField] private MonsterBase _forestDeerPrefab;

private Dictionary<MonsterData, ObjectPool<MonsterBase>> _poolByData;
```

2. `Awake()`에서 딕셔너리 키를 `_waveTable`이 참조하는 실제 4종 `MonsterData` 에셋으로 채운다.

```csharp
protected override void Awake()
{
    base.Awake();

    _poolByData = new Dictionary<MonsterData, ObjectPool<MonsterBase>>
    {
        { _waveTable.FluffyData, new ObjectPool<MonsterBase>(_fluffyPrefab, _poolParent, _initialPoolSize) },
        { _waveTable.SpiderData, new ObjectPool<MonsterBase>(_spiderPrefab, _poolParent, _initialPoolSize) },
        { _waveTable.StoneBugData, new ObjectPool<MonsterBase>(_stoneBugPrefab, _poolParent, _initialPoolSize) },
        { _waveTable.ForestDeerData, new ObjectPool<MonsterBase>(_forestDeerPrefab, _poolParent, _initialPoolSize) },
    };
}
```

3. `PlaceMonster(MonsterData data, int col, int row)`에서 풀 참조를 교체한다.

```csharp
private void PlaceMonster(MonsterData data, int col, int row)
{
    MonsterBase monster = _poolByData[data].Get();
    if (data != null)
        monster.ApplyData(data);

    monster.transform.position = GridToWorldPosition(col, row);
    _activeMonsters.Add(monster);

    OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);
}
```

4. `CheckGameOver()`와 `HandleMonsterDied()`에서 반납 시 `monster.Data`(반납 시점의 실제 데이터)로 올바른 풀을 찾아 반납한다.

```csharp
// CheckGameOver() 내부
_activeMonsters.RemoveAt(i);
_poolByData[monster.Data].Return(monster);
OnMonsterReachedBottom?.Invoke(monster);
...

// HandleMonsterDied() 내부
_activeMonsters.Remove(monster);
_poolByData[monster.Data].Return(monster);
...
```

`MonsterBase.Data`(`public MonsterData Data => _monsterData;`)는 이미 존재하는 프로퍼티이므로 추가 변경이 필요 없다.

### B. 스폰 트리거를 상단 1행(topRow)으로 단순화 + 세로 2칸 사전 확인

기존 계획 5번 항목(`TryDispenseRoster()`)을 다음과 같이 수정한다. `midRow`/`topRow` 두 줄을 모두 스캔하던 방식을 폐기하고, `topRow = _gridRows - 1` 한 줄만 스캔한다. `belowRow = _gridRows - 2`(기존 `midRow`와 같은 행 번호)는 더 이상 스폰 트리거가 아니며, 세로 2칸(`OneByTwo`) 배치 시 사전 확인 용도로만 사용한다.

- 모든 배치(`OneByOne`/`TwoByOne`/`OneByTwo`)의 앵커 좌표는 항상 `topRow`다.
- 열 `col`에 대해 `topRow`가 비어있을 때의 후보 판별:
  - `OneByOne`: 그 칸(`col`, `topRow`)만 비어있으면 배치 가능.
  - `TwoByOne`(StoneBug, 가로 2칸): 같은 행에서 인접한 두 열(`col`, `col+1`)이 모두 비어있어야 배치 가능(기존과 동일, `topRow` 기준으로만 확인).
  - `OneByTwo`(ForestDeer, 세로 2칸): `topRow`의 그 칸이 비어있는 것에 더해, **배치 직전에 `IsCellFree(col, belowRow)`를 호출해 바로 아래 칸(row 3, 같은 열)도 비어있는지 사전 확인**한다. 비어있지 않으면 이번 틱엔 그 칸에 `OneByTwo`를 배치하지 않는다. `oneByOneFits`/`twoByOneFits`는 `belowRow`와 무관하게 그대로 `topRow` 기준으로 판정한다.

C(틱당 3~7 제한)도 같은 메서드에 함께 반영되므로, `TryDispenseRoster()` 전체를 아래처럼 재작성한다.

```csharp
private void TryDispenseRoster()
{
    if (_waveRoster.Count == 0)
        return;

    int belowRow = _gridRows - 2;
    int topRow = _gridRows - 1;

    // C. 이번 틱에 배치할 최대 수를 3~7 사이 무작위로 결정 (Random.Range(int,int)는 min 포함, max 미포함)
    int maxThisTick = UnityEngine.Random.Range(3, 8);
    int placedThisTick = 0;

    bool[] topRowFree = new bool[_gridColumns];
    for (int col = 0; col < _gridColumns; col++)
        topRowFree[col] = IsCellFree(col, topRow);

    for (int col = 0; col < _gridColumns; col++)
    {
        if (_waveRoster.Count == 0 || placedThisTick >= maxThisTick)
            return;

        if (!topRowFree[col])
            continue;

        bool oneByOneFits = true;
        bool twoByOneFits = col + 1 < _gridColumns && topRowFree[col + 1];

        List<int> fittingIndices = new List<int>();
        for (int i = 0; i < _waveRoster.Count; i++)
        {
            BlockSize size = _waveRoster[i].BlockSize;
            if (size == BlockSize.OneByOne && oneByOneFits)
                fittingIndices.Add(i);
            else if (size == BlockSize.TwoByOne && twoByOneFits)
                fittingIndices.Add(i);
            else if (size == BlockSize.OneByTwo)
            {
                // B. 세로 2칸은 배치 직전 바로 아래 칸(belowRow)을 사전 확인
                if (IsCellFree(col, belowRow))
                    fittingIndices.Add(i);
            }
        }

        if (fittingIndices.Count == 0)
            continue;

        int rosterIndex = fittingIndices[UnityEngine.Random.Range(0, fittingIndices.Count)];
        MonsterData data = _waveRoster[rosterIndex];

        PlaceMonster(data, col, topRow);
        _waveRoster.RemoveAt(rosterIndex);
        placedThisTick++;

        switch (data.BlockSize)
        {
            case BlockSize.OneByOne:
                topRowFree[col] = false;
                break;
            case BlockSize.TwoByOne:
                topRowFree[col] = false;
                topRowFree[col + 1] = false;
                break;
            case BlockSize.OneByTwo:
                topRowFree[col] = false;
                break;
        }
    }
}
```

- `OneByTwo`가 배치되어도 `belowRow`는 스캔 대상이 아니므로 `topRowFree` 캐시만 갱신하면 된다(같은 스캔 패스 내에서 `belowRow`를 다시 조회할 일이 없음).
- 배치가 `maxThisTick`에 도달하면 그 틱의 스캔을 즉시 종료한다(로스터가 남아있고 빈 칸이 더 있어도 다음 틱까지 대기).

### C. 틱당 스폰 수 3~7 무작위 제한

B 항목의 `TryDispenseRoster()` 재작성 코드에 이미 통합 반영했다(`maxThisTick = UnityEngine.Random.Range(3, 8)`, `placedThisTick` 카운터). `Random.Range(int, int)`는 min 포함, max 미포함이므로 `Range(3, 8)`이 3~7 사이 정수를 반환함에 유의한다. 별도의 추가 메서드는 없다.

### D. 웨이브 인덱스 0(게임 전체 최초 웨이브) 전체 그리드 즉시 배치

기존 계획 2번 항목(`SpawnWave(int index)`)의 마지막 단계("메서드 마지막에서 `TryDispenseRoster();`를 한 번 즉시 호출")를 다음과 같이 분기 처리한다.

```csharp
// SpawnWave(int index) 마지막 단계 수정
if (index == 0)
    SpawnRosterAcrossFullGrid();
else
    TryDispenseRoster();
```

신규 메서드 `SpawnRosterAcrossFullGrid()`를 추가한다. 그리드 전체(열 0~`_gridColumns-1`, 행 0~`_gridRows-1`)를 대상으로 하는 로컬 `bool[,] free` 배열을 전부 `true`로 초기화(웨이브 0 시점엔 활성 몬스터가 없으므로 안전)한 뒤, 로스터가 빌 때까지 반복한다.

```csharp
private void SpawnRosterAcrossFullGrid()
{
    bool[,] free = new bool[_gridColumns, _gridRows];
    for (int col = 0; col < _gridColumns; col++)
        for (int row = 0; row < _gridRows; row++)
            free[col, row] = true;

    while (_waveRoster.Count > 0)
    {
        // (rosterIndex, col, row) 후보 조합을 전부 나열
        List<(int rosterIndex, int col, int row)> candidates = new List<(int, int, int)>();

        for (int i = 0; i < _waveRoster.Count; i++)
        {
            BlockSize size = _waveRoster[i].BlockSize;

            for (int col = 0; col < _gridColumns; col++)
            {
                for (int row = 0; row < _gridRows; row++)
                {
                    bool fits = size switch
                    {
                        BlockSize.OneByOne => free[col, row],
                        BlockSize.TwoByOne => col + 1 < _gridColumns && free[col, row] && free[col + 1, row],
                        BlockSize.OneByTwo => row + 1 < _gridRows && free[col, row] && free[col, row + 1],
                        _ => false,
                    };

                    if (fits)
                        candidates.Add((i, col, row));
                }
            }
        }

        if (candidates.Count == 0)
            break; // 이론상 그리드 용량 클램프 덕분에 발생하지 않아야 함

        var (rosterIndex, chosenCol, chosenRow) = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        MonsterData data = _waveRoster[rosterIndex];

        PlaceMonster(data, chosenCol, chosenRow);
        _waveRoster.RemoveAt(rosterIndex);

        switch (data.BlockSize)
        {
            case BlockSize.OneByOne:
                free[chosenCol, chosenRow] = false;
                break;
            case BlockSize.TwoByOne:
                free[chosenCol, chosenRow] = false;
                free[chosenCol + 1, chosenRow] = false;
                break;
            case BlockSize.OneByTwo:
                free[chosenCol, chosenRow] = false;
                free[chosenCol, chosenRow + 1] = false;
                break;
        }
    }
}
```

이 메서드는 시간 지연 없이(같은 프레임 내에서) 로스터 전체를 소진시킨다. 실행 후 `_waveRoster.Count`는 0이 되므로, 이후 `Update()`의 컨베이어 스캔은 자연히 스킵된다(추가 처리 불필요).

## 예상 변경 파일 목록 (이번 추가분 반영)

- `Assets/_Project/Scripts/Wave/WaveManager.cs` (수정, 위 1~9번 계획에 이어 동일 파일에 추가 변경)
  - 필드 제거: `_monsterPrefab`(`MonsterBase`), `_monsterPool`(`ObjectPool<MonsterBase>`)
  - 필드 추가: `_fluffyPrefab`/`_spiderPrefab`/`_stoneBugPrefab`/`_forestDeerPrefab`(각 `MonsterBase`), `_poolByData`(`Dictionary<MonsterData, ObjectPool<MonsterBase>>`)
  - `Awake()` 수정 — 4개 프리팹으로 `_poolByData` 채우기(A)
  - `PlaceMonster(MonsterData, int, int)` 수정 — `_poolByData[data].Get()` 사용(A)
  - `CheckGameOver()`, `HandleMonsterDied()` 수정 — `_poolByData[monster.Data].Return(monster)` 사용(A)
  - `TryDispenseRoster()` 재작성 — `topRow` 한 줄만 스캔, `OneByTwo` 사전 확인(`belowRow`), 틱당 3~7 무작위 제한(B, C)
  - `SpawnWave(int index)` 수정 — 마지막 단계를 `index == 0` 분기로 교체(D)
  - `SpawnRosterAcrossFullGrid()` 신규 메서드 추가 — 웨이브 0 전용 그리드 전체 즉시 배치(D)

## 주의사항 (이번 추가분)

- 4개 풀 생성 시 각 프리팹 필드(`_fluffyPrefab`/`_spiderPrefab`/`_stoneBugPrefab`/`_forestDeerPrefab`)가 Inspector에서 실제 올바른 프리팹(Fluffy/Spider/StoneBug/ForestDeer)으로 연결되어야 하며, 이는 Unity 에디터에서 사용자가 직접 확인/연결해야 한다(이 원격 환경엔 Unity 에디터가 없음).
- `SpawnRosterAcrossFullGrid()`는 웨이브 인덱스 0에서만 호출되므로 매 프레임 반복 실행되는 로직이 아니며, 성능 우려는 없다.
- 틱당 3~7 제한(C)과 상단 1행 트리거 변경(B)으로 인해, 로스터가 큰 웨이브에서 로스터 전체가 소진되기까지 걸리는 시간이 기존보다 길어질 수 있다(의도된 동작이며 버그가 아님).

---

## 추가 확정 사항 반영 2 (2026-07-05 플레이테스트 피드백: E/F/G)

A/B/C/D는 이미 구현·커밋 완료되었다. 이후 추가 플레이테스트에서 버그 2가지(E, F)와 설계 변경 1가지(G, 사용자 명시적 요청)가 확정되어 `MonsterRules.md`에 반영되었다. 이 섹션은 그 확정 사항을 `WaveManager.cs` 구현 단위로 구체화한다. 아래 설계는 이미 논의를 마치고 확정된 내용이므로 그대로 반영하며, 임의로 다른 대안을 제안하지 않는다.

### E. 2칸 몬스터 배치 좌표 버그 수정

**원인**: 현재 `PlaceMonster(MonsterData data, int col, int row)`는 `monster.transform.position = GridToWorldPosition(col, row)`를 그대로 사용해, 가로 2칸(`TwoByOne`, StoneBug)/세로 2칸(`OneByTwo`, ForestDeer) 몬스터도 항상 앵커로 쓰인 한 칸의 중심에 배치된다. 그 결과 스프라이트가 인접 칸에 걸쳐 보이거나(맨 좌측 열 `col=0`이면 그리드 밖으로 튀어나옴) 실제 점유 범위와 시각적 위치가 어긋난다.

**수정 내용**:

1. `PlaceMonster` 시그니처를 `PlaceMonster(MonsterData data, int col, int row)`에서 `PlaceMonster(MonsterData data, Vector3 worldPosition)`으로 변경한다.

```csharp
private void PlaceMonster(MonsterData data, Vector3 worldPosition)
{
    MonsterBase monster = _poolByData[data].Get();
    if (data != null)
        monster.ApplyData(data);

    monster.transform.position = worldPosition;
    _activeMonsters.Add(monster);

    OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);
}
```

2. `TryDispenseRoster()`의 배치 호출부(`PlaceMonster(data, col, topRow);`)를 아래처럼 `BlockSize`에 따라 위치를 계산해 전달하도록 변경한다.

```csharp
Vector3 worldPos = data.BlockSize switch
{
    BlockSize.TwoByOne => (GridToWorldPosition(col, topRow) + GridToWorldPosition(col + 1, topRow)) / 2f,
    BlockSize.OneByTwo => (GridToWorldPosition(col, topRow) + GridToWorldPosition(col, belowRow)) / 2f,
    _ => GridToWorldPosition(col, topRow),
};
PlaceMonster(data, worldPos);
```

3. `SpawnRosterAcrossFullGrid()`의 배치 호출부(`PlaceMonster(data, chosenCol, chosenRow);`)도 아래처럼 변경한다. **주의**: 이 메서드에서는 `OneByTwo`가 `row`/`row+1` 두 행을 차지하므로, 평균 대상 행이 `TryDispenseRoster()`(`topRow`/`belowRow`)와 다르다.

```csharp
Vector3 worldPos = data.BlockSize switch
{
    BlockSize.TwoByOne => (GridToWorldPosition(chosenCol, chosenRow) + GridToWorldPosition(chosenCol + 1, chosenRow)) / 2f,
    BlockSize.OneByTwo => (GridToWorldPosition(chosenCol, chosenRow) + GridToWorldPosition(chosenCol, chosenRow + 1)) / 2f,
    _ => GridToWorldPosition(chosenCol, chosenRow),
};
PlaceMonster(data, worldPos);
```

4. `IsCellFree(int col, int row)`의 판정 여유 반경을 `_gridCellSize / 2f`에서 `_gridCellSize * 0.55f`로 늘린다(2칸 몬스터가 정확히 두 칸의 중간 지점에 위치했을 때 각 칸 중심에서의 거리가 정확히 반 칸이 되어 "비어있음"으로 오판되는 경계 케이스를 방지하기 위함).

```csharp
private bool IsCellFree(int col, int row)
{
    Vector3 cellWorldPos = GridToWorldPosition(col, row);
    float checkRadius = _gridCellSize * 0.55f;

    foreach (MonsterBase monster in _activeMonsters)
    {
        Vector3 pos = monster.transform.position;
        if (Mathf.Abs(pos.x - cellWorldPos.x) < checkRadius && Mathf.Abs(pos.y - cellWorldPos.y) < checkRadius)
            return false;
    }
    return true;
}
```

### F. 좌측 편중 스폰 버그 수정 — 열 스캔 순서 랜덤화

**원인**: `TryDispenseRoster()`가 매 틱마다 열(`col`)을 항상 `0`번부터 고정된 순서로 스캔하고, 이번 틱 배치 제한(`maxThisTick`, 3~7마리)에 도달하면 그 자리에서 즉시 스캔을 종료한다. 왼쪽부터 빈 칸을 채우다 제한에 걸려 멈추기를 반복하면서, 오른쪽 열(대략 5~8번)은 스캔 대상에 거의 도달하지 못해 스폰이 좌측에 심하게 편중된다.

**수정 내용**: `TryDispenseRoster()`의 열 스캔부(`for (int col = 0; col < _gridColumns; col++) { ... }`, `topRowFree` 계산 이후의 배치 루프)를 고정 순서 대신 매 틱 셔플된 순서로 스캔하도록 변경한다. `topRowFree` 배열 자체는 열 인덱스 기준 그대로 유지하고(값 캐싱은 기존과 동일하게 `col = 0`부터 순서대로 계산), 배치 루프만 셔플된 순서로 순회한다.

```csharp
List<int> colOrder = new List<int>(_gridColumns);
for (int c = 0; c < _gridColumns; c++)
    colOrder.Add(c);
for (int i = colOrder.Count - 1; i > 0; i--)
{
    int j = UnityEngine.Random.Range(0, i + 1);
    (colOrder[i], colOrder[j]) = (colOrder[j], colOrder[i]);
}

foreach (int col in colOrder)
{
    if (_waveRoster.Count == 0 || placedThisTick >= maxThisTick)
        return;

    if (!topRowFree[col])
        continue;

    // 이하 기존 로직 동일 (twoByOneFits은 col+1 < _gridColumns && topRowFree[col+1] 그대로 사용 —
    // 셔플된 순서로 순회해도 각 col의 인접 열 검사(col+1) 자체는 열 인덱스 기준이라 영향 없음)
    ...
}
```

기존 `for (int col = 0; col < _gridColumns; col++)`를 `foreach (int col in colOrder)`로 바꾸는 것 외 루프 내부 로직(`oneByOneFits`/`twoByOneFits` 판정, 후보 수집, 무작위 선택, 배치, 캐시 갱신)은 변경하지 않는다.

### G. 웨이브 오버랩 스폰 + 10/11웨이브 예외

**배경**: 기존 웨이브 클리어 조건("활성 몬스터 0 AND 로스터 소진")이 다음 웨이브 로스터 생성 자체를 막고 있어, 로스터를 다 배출해도 필드에 몬스터가 하나라도 남아있으면 다음 웨이브 스폰이 전혀 시작되지 않는 문제가 확인되었다(사용자 명시적 요청으로 오버랩 방식 도입 확정, `MonsterRules.md` 2장/6장 참고).

**수정 내용**:

1. `SpawnWave(int index)`의 마지막 분기(`if (index == 0) SpawnRosterAcrossFullGrid(); else TryDispenseRoster();`)를 `index == 10`(11웨이브)도 전체 그리드 즉시배치 대상에 포함하도록 확장한다.

```csharp
// index 0 = 1웨이브(게임 전체 최초), index 10 = 11웨이브(10웨이브 예외 이후 재시작) — 둘 다 그리드 전체 즉시배치
if (index == 0 || index == 10)
    SpawnRosterAcrossFullGrid();
else
    TryDispenseRoster();
```

2. 신규 메서드 `CheckRosterDepleted()`를 추가하고, `TryDispenseRoster()`의 배치 루프 종료 직후와 `SpawnRosterAcrossFullGrid()`의 `while (_waveRoster.Count > 0) { ... }` 루프 종료 직후에 각각 호출한다.

```csharp
private void CheckRosterDepleted()
{
    if (_waveRoster.Count != 0)
        return;

    bool isLastWave = _currentWaveIndex + 1 >= _waveTable.TotalWaves;
    bool isOverlapExceptionWave = _currentWaveIndex == 9; // 10웨이브(index 9)는 전멸 전까지 다음 웨이브로 넘어가지 않는 예외

    if (!isLastWave && !isOverlapExceptionWave)
    {
        AdvanceToNextWave();
    }
    // 마지막 웨이브 또는 10웨이브(예외)라면 여기서는 아무것도 하지 않고,
    // CheckWaveCleared()가 활성 몬스터까지 모두 사라졌을 때 다음 단계로 진행하도록 둔다.
}
```

3. `CheckWaveCleared()`는 **수정하지 않는다.** 기존 로직("활성 몬스터 0 AND 로스터 소진" 두 조건 모두 만족 시 마지막 웨이브면 `OnAllWavesCleared`, 아니면 `OnWaveCleared` + `AdvanceToNextWave()`)이 그대로 유지되며, `CheckRosterDepleted()`가 비-예외 웨이브를 미리 처리해주기 때문에 이 메서드는 실질적으로 "마지막 웨이브(20웨이브, index 19)"와 "10웨이브(index 9) → 11웨이브(index 10) 전환" 두 경우에만 작동하게 된다(자연스럽게, 코드 변경 없이).
4. `AdvanceToNextWave()`도 **수정하지 않는다**(기존 그대로: `_currentWaveIndex++` 후 `SpawnWave(_currentWaveIndex)` 또는 `OnAllWavesCleared`).

## 예상 변경 파일 목록 (이번 추가분 반영)

- `Assets/_Project/Scripts/Wave/WaveManager.cs` (수정, 위 1~9번 계획 + A/B/C/D 추가분에 이어 동일 파일에 추가 변경)
  - `PlaceMonster(MonsterData, int, int)` → `PlaceMonster(MonsterData, Vector3)`로 시그니처 변경(E)
  - `TryDispenseRoster()` 배치 호출부 수정 — `BlockSize`별 월드 좌표 계산 후 `PlaceMonster(data, worldPos)` 호출(E)
  - `SpawnRosterAcrossFullGrid()` 배치 호출부 수정 — `BlockSize`별 월드 좌표 계산 후 `PlaceMonster(data, worldPos)` 호출(E, `row`/`row+1` 평균 기준이 `TryDispenseRoster()`와 다름)
  - `IsCellFree(int, int)` 수정 — 판정 여유 반경을 `_gridCellSize / 2f`에서 `_gridCellSize * 0.55f`로 확대(E)
  - `TryDispenseRoster()` 열 스캔 루프 수정 — 고정 순서(`for`) 대신 매 틱 셔플된 `colOrder`(`foreach`)로 스캔(F)
  - `SpawnWave(int index)` 마지막 분기 수정 — `index == 0 || index == 10`으로 확장(G)
  - `CheckRosterDepleted()` 신규 메서드 추가 — `TryDispenseRoster()`/`SpawnRosterAcrossFullGrid()` 종료 직후 호출, 10웨이브(index 9)/마지막 웨이브 예외를 제외하고 즉시 `AdvanceToNextWave()` 호출(G)
  - `CheckWaveCleared()`, `AdvanceToNextWave()`는 수정하지 않음(G)

## 주의사항 (이번 추가분)

- E 수정 시 `TryDispenseRoster()`와 `SpawnRosterAcrossFullGrid()` 양쪽 모두에서 `PlaceMonster` 호출부를 빠짐없이 고쳐야 하며, 두 메서드의 `OneByTwo` 평균 대상 행이 다르다는 점에 유의한다(`TryDispenseRoster()`는 `topRow`/`belowRow`, `SpawnRosterAcrossFullGrid()`는 `row`/`row+1` — 위 본문 참고).
- G 반영 후 초반 웨이브들(로스터 수량이 적음)은 오버랩이 연쇄적으로 일어나 웨이브 인덱스가 실제 체감 진행 속도보다 빠르게 올라갈 수 있다(의도된 트레이드오프이며 버그가 아님, 이미 사용자 확인됨).
- 10웨이브(index 9)/11웨이브(index 10)는 매직 넘버로 하드코딩되므로, 코드에 그 의미(10웨이브 예외/11웨이브 전체배치)를 주석으로 명확히 남길 것(`SpawnWave()`의 `index == 10` 분기, `CheckRosterDepleted()`의 `_currentWaveIndex == 9` 분기 각각).
- 이 변경들(E/F/G)은 Unity 에디터에서 실제 플레이 테스트로 검증이 필요하다(이 원격 환경엔 Unity 에디터가 없으므로, 에디터에서 직접 실행/확인하는 작업은 사용자 로컬 환경에서 진행해야 한다).
