using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : Singleton<WaveManager>
{
    [SerializeField] private WaveTableData _waveTable;
    [SerializeField] private MonsterBase _monsterPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 20;
    [SerializeField] private Transform _spawnRoot;
    [SerializeField] private float _gridCellSize = 1.0f;
    [SerializeField] private int _gridColumns = 9;
    [SerializeField] private int _gridRows = 5;
    [SerializeField] private float _bottomBoundaryY;
    [SerializeField] private int _killCountForSkill = 5;

    private ObjectPool<MonsterBase> _monsterPool;
    private List<MonsterBase> _activeMonsters = new List<MonsterBase>();
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

    protected override void Awake()
    {
        base.Awake();
        _monsterPool = new ObjectPool<MonsterBase>(_monsterPrefab, _poolParent, _initialPoolSize);
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

        // 4. 웨이브 시작마다 점유 배열 새로 초기화
        bool[,] occupied = new bool[_gridColumns, _gridRows];
        var spawnList = new List<(MonsterData data, Vector2Int anchor)>();

        // 5. 2칸 몬스터(StoneBug/ForestDeer) 먼저 배치
        int twoCellTarget = Mathf.Min(Mathf.RoundToInt(spawnCount * twoCellWeight), spawnCount);
        for (int i = 0; i < twoCellTarget; i++)
        {
            bool horizontal = UnityEngine.Random.value < 0.5f;
            int width  = horizontal ? 2 : 1;
            int height = horizontal ? 1 : 2;
            MonsterData data = horizontal ? _waveTable.StoneBugData : _waveTable.ForestDeerData;

            List<Vector2Int> candidates = GetFreeAnchors(occupied, width, height);
            if (candidates.Count == 0)
                break;

            Vector2Int anchor = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            MarkOccupied(occupied, anchor, width, height);
            spawnList.Add((data, anchor));
        }

        // 6. 나머지 스폰 수만큼 1칸 몬스터(Fluffy/Spider)로 채움
        int remaining = spawnCount - spawnList.Count;
        for (int i = 0; i < remaining; i++)
        {
            MonsterData data = UnityEngine.Random.value < 0.5f ? _waveTable.FluffyData : _waveTable.SpiderData;

            List<Vector2Int> candidates = GetFreeAnchors(occupied, 1, 1);
            if (candidates.Count == 0)
                break;

            Vector2Int anchor = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            MarkOccupied(occupied, anchor, 1, 1);
            spawnList.Add((data, anchor));
        }

        // 7~8. 그리드 좌표 → 월드 좌표 변환 후 스폰
        foreach ((MonsterData data, Vector2Int anchor) in spawnList)
        {
            MonsterBase monster = _monsterPool.Get();
            if (data != null)
                monster.ApplyData(data);

            Vector3 worldPosition = _spawnRoot.position + new Vector3(
                anchor.x * _gridCellSize,
                anchor.y * _gridCellSize,
                0f
            );
            monster.transform.position = worldPosition;
            _activeMonsters.Add(monster);
        }

        // 9. 웨이브 시작/몬스터 수 변화 이벤트 발행
        _currentWaveTotalCount = spawnList.Count;

        OnWaveStarted?.Invoke(index + 1);
        OnMonsterCountChanged?.Invoke(_activeMonsters.Count, _currentWaveTotalCount);
    }

    private List<Vector2Int> GetFreeAnchors(bool[,] occupied, int width, int height)
    {
        var candidates = new List<Vector2Int>();

        for (int col = 0; col <= _gridColumns - width; col++)
        {
            for (int row = 0; row <= _gridRows - height; row++)
            {
                bool isFree = true;
                for (int dx = 0; dx < width && isFree; dx++)
                {
                    for (int dy = 0; dy < height && isFree; dy++)
                    {
                        if (occupied[col + dx, row + dy])
                            isFree = false;
                    }
                }

                if (isFree)
                    candidates.Add(new Vector2Int(col, row));
            }
        }

        return candidates;
    }

    private void MarkOccupied(bool[,] occupied, Vector2Int anchor, int width, int height)
    {
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                occupied[anchor.x + dx, anchor.y + dy] = true;
            }
        }
    }

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
            }
        }
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        _activeMonsters.Remove(monster);
        _monsterPool.Return(monster);

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
        if (_activeMonsters.Count == 0)
        {
            // 마지막 웨이브인 경우 스킬 선택 없이 바로 종료
            if (_currentWaveIndex + 1 >= _waveTable.TotalWaves)
            {
                OnAllWavesCleared?.Invoke();
            }
            else
            {
                OnWaveCleared?.Invoke();   // UIManager → SkillSelectionPanel 열기
                // AdvanceToNextWave()는 SkillSelectionPanel.OnSkillSelected 콜백 이후 UIManager가 호출
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
