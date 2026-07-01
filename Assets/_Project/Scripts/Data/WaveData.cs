using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "PurpleCow/WaveData")]
public class WaveData : ScriptableObject
{
    [SerializeField] private int _waveNumber;
    [SerializeField] private List<MonsterSpawnEntry> _spawnEntries;

    public int WaveNumber                        => _waveNumber;
    public List<MonsterSpawnEntry> SpawnEntries  => _spawnEntries;
}

[System.Serializable]
public class MonsterSpawnEntry
{
    public MonsterData Data;
    public Vector2Int GridPosition;
}
