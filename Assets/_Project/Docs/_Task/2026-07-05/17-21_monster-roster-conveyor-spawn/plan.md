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
