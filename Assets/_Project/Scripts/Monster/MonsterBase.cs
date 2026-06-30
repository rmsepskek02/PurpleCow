using System;
using System.Collections;
using UnityEngine;

public class MonsterBase : MonoBehaviour, IPoolable
{
    [SerializeField] private MonsterData _monsterData;

    private float _currentHp;
    private bool  _isDead;
    private int   _frozenTurnsRemaining;
    private int   _slowTurnsRemaining;
    private float _slowPercent;
    private float _bonusCritChance;

    public float CurrentHp    => _currentHp;
    public bool  IsAlive      => !_isDead;
    public bool  IsFrozen     => _frozenTurnsRemaining > 0;
    public MonsterData Data   => _monsterData;

    public static event Action<MonsterBase> OnMonsterDied;
    public event Action<float, float> OnHpChanged;

    public void OnSpawn()
    {
        _currentHp             = _monsterData.Hp;
        _isDead                = false;
        _frozenTurnsRemaining  = 0;
        _slowTurnsRemaining    = 0;
        _slowPercent           = 0f;
        _bonusCritChance       = 0f;
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

    public void ApplyFreeze(int turns)
    {
        _frozenTurnsRemaining = Mathf.Max(_frozenTurnsRemaining, turns);
    }

    public void ApplyFreeze(float seconds)
    {
        // 초 기반은 turns 기반으로 근사 변환 (1턴=1초 가정)
        ApplyFreeze(Mathf.RoundToInt(seconds));
    }

    public void ApplySlow(int turns, float percent)
    {
        _slowTurnsRemaining = turns;
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

    public void MoveDown(float distance)
    {
        if (IsFrozen)
        {
            _frozenTurnsRemaining--;
            return;
        }

        if (_slowTurnsRemaining > 0)
        {
            distance *= (1f - _slowPercent);
            _slowTurnsRemaining--;
        }

        transform.position += (Vector3)(Vector2.down * distance);
    }


}
