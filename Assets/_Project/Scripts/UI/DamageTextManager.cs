using UnityEngine;

public class DamageTextManager : Singleton<DamageTextManager>
{
    [SerializeField] private DamageTextFx _prefab;
    [SerializeField] private Transform    _poolParent;
    [SerializeField] private int          _initialPoolSize = 10;

    private ObjectPool<DamageTextFx> _pool;

    protected override void Awake()
    {
        base.Awake();
        _pool = new ObjectPool<DamageTextFx>(_prefab, _poolParent, _initialPoolSize);
    }

    private void OnEnable()
    {
        Ball.OnHitMonster += HandleHitMonster;
    }

    private void OnDisable()
    {
        Ball.OnHitMonster -= HandleHitMonster;
    }

    private void HandleHitMonster(MonsterBase monster, float damage, bool isCritical)
    {
        ShowDamage(monster.transform.position, damage, isCritical);
    }

    public void ShowDamage(Vector3 worldPos, float damage, bool isCritical)
    {
        DamageTextFx fx = _pool.Get();
        fx.Play(worldPos, damage, isCritical);
    }

    public void Return(DamageTextFx fx)
    {
        _pool.Return(fx);
    }
}
