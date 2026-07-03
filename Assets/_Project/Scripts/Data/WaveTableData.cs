using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEntry
{
    public int WaveNumber;
    public List<MonsterSpawnEntry> SpawnEntries;
}

[CreateAssetMenu(fileName = "WaveTableData", menuName = "PurpleCow/WaveTableData")]
public class WaveTableData : ScriptableObject
{
    [SerializeField] private List<WaveEntry> _waves;

    public List<WaveEntry> Waves => _waves;
    public int WaveCount => _waves.Count;
}

[System.Serializable]
public class MonsterSpawnEntry
{
    public MonsterData Data;
    public Vector2Int GridPosition;
}
