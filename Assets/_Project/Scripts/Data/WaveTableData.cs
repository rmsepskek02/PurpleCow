using UnityEngine;

[CreateAssetMenu(fileName = "WaveTableData", menuName = "PurpleCow/WaveTableData")]
public class WaveTableData : ScriptableObject
{
    [SerializeField] private int   _baseSpawnCount = 10;
    [SerializeField] private float _spawnCountPerWave = 0.5f;
    [SerializeField] private float _baseTwoCellWeight = 0.1f;
    [SerializeField] private float _twoCellWeightPerWave = 0.03f;
    [SerializeField] private int   _totalWaves = 20;
    [SerializeField] private MonsterData _fluffyData;
    [SerializeField] private MonsterData _spiderData;
    [SerializeField] private MonsterData _stoneBugData;
    [SerializeField] private MonsterData _forestDeerData;

    public int   BaseSpawnCount       => _baseSpawnCount;
    public float SpawnCountPerWave    => _spawnCountPerWave;
    public float BaseTwoCellWeight    => _baseTwoCellWeight;
    public float TwoCellWeightPerWave => _twoCellWeightPerWave;
    public int   TotalWaves           => _totalWaves;
    public MonsterData FluffyData     => _fluffyData;
    public MonsterData SpiderData     => _spiderData;
    public MonsterData StoneBugData   => _stoneBugData;
    public MonsterData ForestDeerData => _forestDeerData;
}
