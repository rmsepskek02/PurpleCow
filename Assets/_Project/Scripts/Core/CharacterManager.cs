using System;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private int   _maxHp = 10;
    [SerializeField] private int[] _xpPerLevel = { 10, 20, 30 };

    private int _currentHp;
    private int _currentXp;
    private int _currentLevel = 1;

    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;
    public int CurrentXp => _currentXp;
    public int CurrentLevel => _currentLevel;
    public int RequiredXp =>
        _currentLevel - 1 < _xpPerLevel.Length ? _xpPerLevel[_currentLevel - 1] : 0;

    public static event Action<int, int> OnHpChanged;
    public static event Action<int, int> OnXpChanged;
    public static event Action<int>      OnLevelUp;

    protected override void Awake()
    {
        base.Awake();
        _currentHp    = _maxHp;
        _currentLevel = 1;
        _currentXp    = 0;
    }

    private void OnEnable()
    {
        WaveManager.OnMonsterReachedBottom += HandleMonsterReachedBottom;
        MonsterBase.OnMonsterDied          += HandleMonsterDied;
    }

    private void OnDisable()
    {
        WaveManager.OnMonsterReachedBottom -= HandleMonsterReachedBottom;
        MonsterBase.OnMonsterDied          -= HandleMonsterDied;
    }

    private void HandleMonsterReachedBottom(MonsterBase monster)
    {
        TakeDamage(monster.Data.Damage);
        AddXp(monster.Data.Reward);
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        AddXp(monster.Data.Reward);
    }

    private void TakeDamage(int damage)
    {
        _currentHp = Mathf.Max(0, _currentHp - damage);
        OnHpChanged?.Invoke(_currentHp, _maxHp);
        if (_currentHp <= 0) GameManager.Instance.EndGame(false);
    }

    private void AddXp(int amount)
    {
        if (RequiredXp <= 0) return;
        _currentXp += amount;
        while (RequiredXp > 0 && _currentXp >= RequiredXp)
        {
            _currentXp -= RequiredXp;
            _currentLevel++;
            OnLevelUp?.Invoke(_currentLevel);
        }
        OnXpChanged?.Invoke(_currentXp, RequiredXp);
    }
}
