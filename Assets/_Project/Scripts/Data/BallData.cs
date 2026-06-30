using UnityEngine;

[CreateAssetMenu(fileName = "BallData", menuName = "PurpleCow/BallData")]
public class BallData : ScriptableObject
{
    [SerializeField] private float _damage;
    [SerializeField] private float _speed;
    [SerializeField] private float _criticalChance;       // 0 ~ 1
    [SerializeField] private float _criticalMultiplier;   // 예: 2.0 = 200%
    [SerializeField] private int   _maxBounces;           // 기본 최대 반사 횟수

    public float Damage             => _damage;
    public float Speed              => _speed;
    public float CriticalChance     => _criticalChance;
    public float CriticalMultiplier => _criticalMultiplier;
    public int   MaxBounces         => _maxBounces;
}
