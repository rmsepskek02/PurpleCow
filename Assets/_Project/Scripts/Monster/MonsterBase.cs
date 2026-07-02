using System;
using System.Collections;
using UnityEngine;

public class MonsterBase : MonoBehaviour, IPoolable
{
    [SerializeField] private MonsterData _monsterData;

    private float _currentHp;
    private bool  _isDead;
    private float _frozenSecondsRemaining;
    private float _slowSecondsRemaining;
    private float _slowPercent;
    private float _bonusCritChance;

    public float CurrentHp    => _currentHp;
    public bool  IsAlive      => !_isDead;
    public bool  IsFrozen     => _frozenSecondsRemaining > 0f;
    public MonsterData Data   => _monsterData;

    public static event Action<MonsterBase> OnMonsterDied;
    public event Action<float, float> OnHpChanged;

    public void OnSpawn()
    {
        _currentHp              = _monsterData.Hp;
        _isDead                 = false;
        _frozenSecondsRemaining = 0f;
        _slowSecondsRemaining   = 0f;
        _slowPercent            = 0f;
        _bonusCritChance        = 0f;
        OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
    }

    public void ApplyData(MonsterData data)
    {
        _monsterData = data;
        _currentHp   = _monsterData.Hp;
        OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
    }

    public void OnDespawn()
    {
        _isDead = true;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _currentHp -= damage;
        OnHpChanged?.Invoke(Mathf.Max(_currentHp, 0f), _monsterData.Hp);

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        OnMonsterDied?.Invoke(this);
    }

    public void ApplyFreeze(float seconds)
    {
        _frozenSecondsRemaining = Mathf.Max(_frozenSecondsRemaining, seconds);
    }

    public void ApplySlow(float seconds, float percent)
    {
        _slowSecondsRemaining = seconds;
        _slowPercent = percent;
    }

    public void ApplyBonusCritChance(float bonus)
    {
        _bonusCritChance += bonus;
    }

    public float ConsumeBonusCritChance()
    {
        float val = _bonusCritChance;
        _bonusCritChance = 0f;
        return val;
    }

    public void ApplyDot(float damagePerSec, float duration, int maxStacks)
    {
        StartCoroutine(CoDotTick(damagePerSec, duration, maxStacks));
    }

    private IEnumerator CoDotTick(float dps, float duration, int stacks)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            TakeDamage(dps * stacks);
        }
    }

    private void Update()
    {
        if (_isDead)
            return;

        float deltaTime = Time.deltaTime;

        if (_frozenSecondsRemaining > 0f)
        {
            _frozenSecondsRemaining -= deltaTime;
            return;
        }

        float speed = _monsterData.MoveSpeed;

        if (_slowSecondsRemaining > 0f)
        {
            speed *= (1f - _slowPercent);
            _slowSecondsRemaining -= deltaTime;
        }

        transform.position += Vector3.down * speed * deltaTime;
    }
}
