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

    public int TotalWaves => _waveTable.WaveCount;

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
        if (index < 0 || index >= _waveTable.WaveCount)
            return;

        WaveEntry waveEntry = _waveTable.Waves[index];

        foreach (MonsterSpawnEntry entry in waveEntry.SpawnEntries)
        {
            MonsterBase monster = _monsterPool.Get();
            if (entry.Data != null)
                monster.ApplyData(entry.Data);
            Vector3 worldPosition = _spawnRoot.position + new Vector3(
                entry.GridPosition.x * _gridCellSize,
                entry.GridPosition.y * _gridCellSize,
                0f
            );
            monster.transform.position = worldPosition;
            _activeMonsters.Add(monster);
        }

        _currentWaveTotalCount = waveEntry.SpawnEntries.Count;

        OnWaveStarted?.Invoke(waveEntry.WaveNumber);
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
            if (_currentWaveIndex + 1 >= _waveTable.WaveCount)
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

        if (_currentWaveIndex < _waveTable.WaveCount)
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
