using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : Singleton<WaveManager>
{
    [SerializeField] private WaveTableData _waveTable;
    [SerializeField] private MonsterBase _fluffyPrefab;
    [SerializeField] private MonsterBase _spiderPrefab;
    [SerializeField] private MonsterBase _stoneBugPrefab;
    [SerializeField] private MonsterBase _forestDeerPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 20;
    [SerializeField] private Transform _spawnRoot;
    [SerializeField] private float _gridCellSize = 1.0f;
    [SerializeField] private int _gridColumns = 9;
    [SerializeField] private int _gridRows = 5;
    [SerializeField] private float _bottomBoundaryY;
    [SerializeField] private int _killCountForSkill = 5;

    private Dictionary<MonsterData, ObjectPool<MonsterBase>> _poolByData;
    private List<MonsterBase> _activeMonsters = new List<MonsterBase>();
    private List<MonsterData> _waveRoster = new List<MonsterData>();
    private float _spawnCheckTimer;
    private int _currentWaveIndex;
    private int _totalKillCount;
    private int _currentWaveTotalCount;

    public static event Action<int>         OnWaveStarted;
    public static event Action              OnWaveCleared;
    public static event Action              OnAllWavesCleared;
    public static event Action              OnKillCountReached;
    public static event Action<MonsterBase> OnMonsterReachedBottom;
    public static event Action<int, int>    OnMonsterCountChanged; // (남은 수, 전체 수)

    public int TotalWaves => _waveTable.TotalWaves;
    public float BottomBoundaryY => _bottomBoundaryY;

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

    private void Start()
    {
        SpawnWave(_currentWaveIndex);
    }

    private void OnEnable()
    {
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }

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

    private void SpawnWave(int index)
    {
        if (index < 0 || index >= _waveTable.TotalWaves)
            return;

        // 1. 웨이브 진행도 기반 스폰 수 계산
        int spawnCount = Mathf.RoundToInt(_waveTable.BaseSpawnCount + _waveTable.SpawnCountPerWave * index);

        // 2. 웨이브 진행도 기반 2칸 몬스터 등장 가중치 계산
        float twoCellWeight = Mathf.Clamp01(_waveTable.BaseTwoCellWeight + _waveTable.TwoCellWeightPerWave * index);

        // 3. 그리드 용량 이내로 스폰 수 상한 제한 (2칸 몬스터가 전부 나와도 45칸을 넘지 않도록)
        int capacityLimit = (_gridColumns * _gridRows) / 2;
        spawnCount = Mathf.Min(spawnCount, capacityLimit);

        // 4. 이번 웨이브 로스터 계산 (좌표 없이 종류/수량만 결정)
        _waveRoster.Clear();

        // 5. 2칸 몬스터(StoneBug/ForestDeer) 로스터 채우기
        int twoCellTarget = Mathf.Min(Mathf.RoundToInt(spawnCount * twoCellWeight), spawnCount);
        for (int i = 0; i < twoCellTarget; i++)
        {
            bool horizontal = UnityEngine.Random.value < 0.5f;
            MonsterData data = horizontal ? _waveTable.StoneBugData : _waveTable.ForestDeerData;
            _waveRoster.Add(data);
        }

        // 6. 나머지 스폰 수만큼 1칸 몬스터(Fluffy/Spider) 로스터 채우기
        int remaining = spawnCount - twoCellTarget;
        for (int i = 0; i < remaining; i++)
        {
            MonsterData data = UnityEngine.Random.value < 0.5f ? _waveTable.FluffyData : _waveTable.SpiderData;
            _waveRoster.Add(data);
        }

        // 7. 웨이브 시작/몬스터 수 변화 이벤트 발행
        _currentWaveTotalCount = _waveRoster.Count;

        OnWaveStarted?.Invoke(index + 1);
        OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);

        // 8. 웨이브 인덱스 0(게임 전체 최초 웨이브)은 그리드 전체에 즉시 배치, 그 외에는 컨베이어 디스펜스
        if (index == 0)
            SpawnRosterAcrossFullGrid();
        else
            TryDispenseRoster();
    }

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

    private Vector3 GridToWorldPosition(int col, int row)
    {
        return _spawnRoot.position + new Vector3(
            (col - (_gridColumns - 1) / 2f) * _gridCellSize,
            row * _gridCellSize,
            0f
        );
    }

    private void PlaceMonster(MonsterData data, int col, int row)
    {
        MonsterBase monster = _poolByData[data].Get();
        if (data != null)
            monster.ApplyData(data);

        monster.transform.position = GridToWorldPosition(col, row);
        _activeMonsters.Add(monster);

        OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);
    }

    private void CheckGameOver()
    {
        for (int i = _activeMonsters.Count - 1; i >= 0; i--)
        {
            MonsterBase monster = _activeMonsters[i];
            if (monster.transform.position.y <= _bottomBoundaryY)
            {
                _activeMonsters.RemoveAt(i);
                _poolByData[monster.Data].Return(monster);
                OnMonsterReachedBottom?.Invoke(monster);
                OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);

                CheckWaveCleared();
            }
        }
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        _activeMonsters.Remove(monster);
        _poolByData[monster.Data].Return(monster);

        _totalKillCount++;
        CheckSkillUnlock();
        OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);

        CheckWaveCleared();
    }

    private void CheckSkillUnlock()
    {
        if (_killCountForSkill > 0 && _totalKillCount % _killCountForSkill == 0)
            OnKillCountReached?.Invoke();
    }

    private void CheckWaveCleared()
    {
        if (_activeMonsters.Count == 0 && _waveRoster.Count == 0)
        {
            // 마지막 웨이브인 경우 스킬 선택 없이 바로 종료
            if (_currentWaveIndex + 1 >= _waveTable.TotalWaves)
            {
                OnAllWavesCleared?.Invoke();
            }
            else
            {
                // 스킬 선택(SkillSelectionPanel)은 웨이브 클리어가 아니라 OnKillCountReached(킬 카운트 누적)로
                // 별도 트리거되는 독립적인 시스템이라, 웨이브 클리어 시 그 콜백을 기다리지 않고 바로 다음 웨이브로 진행한다.
                OnWaveCleared?.Invoke();
                AdvanceToNextWave();
            }
        }
    }

    public void AdvanceToNextWave()
    {
        _currentWaveIndex++;

        if (_currentWaveIndex < _waveTable.TotalWaves)
        {
            SpawnWave(_currentWaveIndex);
        }
        else
        {
            OnAllWavesCleared?.Invoke();
        }
    }

    public MonsterBase GetWeakestMonster()
    {
        if (_activeMonsters.Count == 0) return null;

        MonsterBase weakest = _activeMonsters[0];
        foreach (MonsterBase monster in _activeMonsters)
        {
            if (monster.CurrentHp < weakest.CurrentHp)
                weakest = monster;
        }
        return weakest;
    }

    public List<MonsterBase> GetMonstersInRow(MonsterBase reference)
    {
        float targetY = reference.transform.position.y;
        var result = new List<MonsterBase>();
        foreach (var m in _activeMonsters)
        {
            if (m != null && Mathf.Abs(m.transform.position.y - targetY) < 0.1f)
                result.Add(m);
        }
        return result;
    }
}
