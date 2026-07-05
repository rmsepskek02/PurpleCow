using UnityEngine;

public enum PlayerActiveSkillType
{
    Berserk,
    Clone
}

[CreateAssetMenu(
    fileName = "PlayerActiveSkillData",
    menuName = "PurpleCow/Player Active Skill Data")]
public class PlayerActiveSkillData : ScriptableObject
{
    [SerializeField] private PlayerActiveSkillType _skillType;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _cooldown = 30f;
    [SerializeField] private float _duration = 6f;
    [SerializeField] private float _speedMultiplier = 1.5f;
    [SerializeField] private int _cloneReturnCount = 2;

    public PlayerActiveSkillType SkillType => _skillType;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public float Cooldown => _cooldown;
    public float Duration => _duration;
    public float SpeedMultiplier => _speedMultiplier;
    public int CloneReturnCount => _cloneReturnCount;
}
