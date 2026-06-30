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
    [SerializeField] private int _killCountForSkill = 5;

    private ObjectPool<MonsterBase> _monsterPool;
    private List<MonsterBase> _activeMonsters = new List<MonsterBase>();
    private int _currentWaveIndex;
    private int _totalKillCount;

    public static event Action<int> OnWaveStarted;
    public static event Action OnWaveCleared;
    public static event Action OnAllWavesCleared;
    public static event Action OnKillCountReached;

    public int TotalWaves => _waveDatas.Length;

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

        _totalKillCount++;
        CheckSkillUnlock();

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
            if (_currentWaveIndex + 1 >= _waveDatas.Length)
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
