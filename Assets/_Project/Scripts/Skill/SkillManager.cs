using System;
using System.Collections.Generic;

public class SkillManager : Singleton<SkillManager>
{
    private BallSkillBase          _equippedActiveSkill;
    private List<PassiveSkillBase> _passiveSkills;

    // Passive 누적 보너스
    private float _damageMultiplierBonus;
    private float _critChanceBonus;
    private float _critDamageBonus;
    private float _speedBonus;
    private int   _bounceBonus;

    public float DamageMultiplierBonus => _damageMultiplierBonus;
    public float CritChanceBonus       => _critChanceBonus;
    public float CritDamageBonus       => _critDamageBonus;
    public float SpeedBonus            => _speedBonus;
    public int   BounceBonus           => _bounceBonus;

    public static event Action<BallSkillBase>          OnActiveSkillChanged;
    public static event Action<List<PassiveSkillBase>> OnPassiveSkillsChanged;

    protected override void Awake()
    {
        base.Awake();
        _passiveSkills = new List<PassiveSkillBase>();
    }

    public void EquipActiveSkill(BallSkillBase skill)
    {
        _equippedActiveSkill = skill;
        OnActiveSkillChanged?.Invoke(_equippedActiveSkill);
    }

    public void AddPassiveSkill(PassiveSkillBase skill)
    {
        _passiveSkills.Add(skill);
        skill.Apply();
        OnPassiveSkillsChanged?.Invoke(_passiveSkills);
    }

    public void RemovePassiveSkill(PassiveSkillBase skill)
    {
        skill.Remove();
        _passiveSkills.Remove(skill);
        OnPassiveSkillsChanged?.Invoke(_passiveSkills);
    }

    public void ApplySkillToBall(Ball ball)
    {
        if (_equippedActiveSkill != null)
        {
            ball.SetSkill(_equippedActiveSkill);
        }
    }

    public void AddDamageMultiplier(float value)    => _damageMultiplierBonus += value;
    public void RemoveDamageMultiplier(float value) => _damageMultiplierBonus -= value;

    public void AddCritChanceBonus(float value)    => _critChanceBonus += value;
    public void RemoveCritChanceBonus(float value) => _critChanceBonus -= value;

    public void AddCritDamageBonus(float value)    => _critDamageBonus += value;
    public void RemoveCritDamageBonus(float value) => _critDamageBonus -= value;

    public void AddSpeedBonus(float value)    => _speedBonus += value;
    public void RemoveSpeedBonus(float value) => _speedBonus -= value;

    public void AddBounceBonus(int value)    => _bounceBonus += value;
    public void RemoveBounceBonus(int value) => _bounceBonus -= value;
}
