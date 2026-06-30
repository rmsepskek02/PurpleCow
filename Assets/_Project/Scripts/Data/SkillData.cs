using UnityEngine;

[System.Serializable]
public struct SkillLevelData
{
    public float BallDamage;
    public float Value1;
    public float Value2;
    public float Value3;
}

public enum SkillType
{
    Active,
    Passive
}

public enum ActiveSkillId
{
    Fire    = 1001,
    Ice     = 1002,
    Ghost   = 1003,
    Laser   = 1004,
    Cluster = 1005
}

public enum PassiveSkillId
{
    WarmTinHeart   = 3001,
    MagicMirror    = 3002,
    AmethystDagger = 3003,
    EmeraldDagger  = 3004,
    LastMatch      = 3005
}

[CreateAssetMenu(fileName = "SkillData", menuName = "PurpleCow/SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] private int       _skillId;
    [SerializeField] private string    _skillName;
    [SerializeField] private Sprite    _icon;
    [SerializeField] private string    _description;
    [SerializeField] private SkillType _skillType;

    [SerializeField] private SkillLevelData[] _levels = new SkillLevelData[3];
    [SerializeField] private int _currentLevel;

    public int       SkillId     => _skillId;
    public string    SkillName   => _skillName;
    public Sprite    Icon        => _icon;
    public string    Description => _description;
    public SkillType SkillType   => _skillType;

    public int CurrentLevel => _currentLevel;
    public int MaxLevel => _levels.Length;
    public SkillLevelData CurrentLevelData => GetLevelData(_currentLevel);

    public SkillLevelData GetLevelData(int level)
    {
        level = Mathf.Clamp(level, 0, _levels.Length - 1);
        return _levels[level];
    }

    public void LevelUp()
    {
        if (_currentLevel < MaxLevel - 1) _currentLevel++;
    }

    public void ResetLevel()
    {
        _currentLevel = 0;
    }
}
