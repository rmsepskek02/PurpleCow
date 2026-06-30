using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : Singleton<WaveManager>
{
    [SerializeField] private WaveData[] _waveDatas;
    [SerializeField] private MonsterBase _monsterPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 20;
    [SerializeField] private Transform _spawnRoot;
    [SerializeField] private float _gridCellSize = 1.0f;
    [SerializeField] private float _monsterMoveDistance;
    [SerializeField] private float _bottomBoundaryY;

    private ObjectPool<MonsterBase> _monsterPool;
    private List<MonsterBase> _activeMonsters = new List<MonsterBase>();
    private int _currentWaveIndex;

    public static event Action<int> OnWaveStarted;
    public static event Action OnAllWavesCleared;

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
        BallLauncher.OnAllBallsReturned += HandleAllBallsReturned;
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        BallLauncher.OnAllBallsReturned -= HandleAllBallsReturned;
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }

    private void SpawnWave(int index)
    {
        if (index < 0 || index >= _waveDatas.Length)
            return;

        WaveData waveData = _waveDatas[index];

        foreach (MonsterSpawnEntry entry in waveData.SpawnEntries)
        {
            MonsterBase monster = _monsterPool.Get();
            Vector3 worldPosition = _spawnRoot.position + new Vector3(
                entry.GridPosition.x * _gridCellSize,
                entry.GridPosition.y * _gridCellSize,
                0f
            );
            monster.transform.position = worldPosition;
            _activeMonsters.Add(monster);
        }

        OnWaveStarted?.Invoke(waveData.WaveNumber);
    }

    private void HandleAllBallsReturned()
    {
        MoveAllMonstersDown();
        CheckGameOver();
    }

    private void MoveAllMonstersDown()
    {
        foreach (MonsterBase monster in _activeMonsters)
        {
            monster.MoveDown(_monsterMoveDistance);
        }
    }

    private void CheckGameOver()
    {
        foreach (MonsterBase monster in _activeMonsters)
        {
            if (monster.transform.position.y <= _bottomBoundaryY)
            {
                GameManager.Instance.EndGame(false);
                return;
            }
        }
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        _activeMonsters.Remove(monster);
        _monsterPool.Return(monster);
        CheckWaveCleared();
    }

    private void CheckWaveCleared()
    {
        if (_activeMonsters.Count == 0)
        {
            AdvanceToNextWave();
        }
    }

    private void AdvanceToNextWave()
    {
        _currentWaveIndex++;

        if (_currentWaveIndex < _waveDatas.Length)
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
}
