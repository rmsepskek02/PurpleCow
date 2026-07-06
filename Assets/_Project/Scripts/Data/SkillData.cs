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
    [SerializeField] private Sprite    _ballSprite;
    [SerializeField] private string    _description;
    [SerializeField] private SkillType _skillType;

    [SerializeField] private SkillLevelData[] _levels = new SkillLevelData[3];

    public int       SkillId     => _skillId;
    public string    SkillName   => _skillName;
    public Sprite    Icon        => _icon;
    public Sprite    BallSprite  => _ballSprite;
    public string    Description => _description;
    public SkillType SkillType   => _skillType;

    public int MaxLevel => _levels.Length;

    public SkillLevelData GetLevelData(int level)
    {
        level = Mathf.Clamp(level, 0, _levels.Length - 1);
        return _levels[level];
    }

}

public sealed class SkillRuntimeState
{
    private int _levelIndex;

    public SkillRuntimeState(SkillData data)
    {
        Data = data;
        _levelIndex = 0;
    }

    public SkillData Data { get; }
    public int LevelIndex => _levelIndex;
    public int DisplayLevel => _levelIndex + 1;
    public bool IsMaxLevel => _levelIndex >= Data.MaxLevel - 1;
    public SkillLevelData CurrentLevelData => Data.GetLevelData(_levelIndex);

    public bool TryLevelUp()
    {
        if (IsMaxLevel)
            return false;

        _levelIndex++;
        return true;
    }
}
