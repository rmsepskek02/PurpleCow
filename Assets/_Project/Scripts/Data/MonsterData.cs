using UnityEngine;

public enum BlockSize
{
    OneByOne,
    TwoByOne,
    OneByTwo,
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "PurpleCow/MonsterData")]
public class MonsterData : ScriptableObject
{
    [SerializeField] private float _hp;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int _damage;
    [SerializeField] private int _reward;
    [SerializeField] private BlockSize _blockSize;

    public float Hp        => _hp;
    public float MoveSpeed => _moveSpeed;
    public int   Damage    => _damage;
    public int   Reward    => _reward;
    public BlockSize BlockSize => _blockSize;
}
