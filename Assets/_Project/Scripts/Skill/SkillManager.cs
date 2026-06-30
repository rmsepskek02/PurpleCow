using System;
using System.Collections.Generic;

public class SkillManager : Singleton<SkillManager>
{
    private List<BallSkillBase>    _activeSkills  = new List<BallSkillBase>();
    private List<PassiveSkillBase> _passiveSkills = new List<PassiveSkillBase>();

    // Passive 누적 보너스
    private float _damageMultiplierBonus;
    private float _nextShotDamageBonus;

    public float DamageMultiplierBonus => _damageMultiplierBonus;

    public static event Action<List<BallSkillBase>>    OnActiveSkillsChanged;
    public static event Action<List<PassiveSkillBase>> OnPassiveSkillsChanged;

    protected override void Awake()
    {
        base.Awake();
        _activeSkills  = new List<BallSkillBase>();
        _passiveSkills = new List<PassiveSkillBase>();
    }

    public void EquipActiveSkill(BallSkillBase skill)
    {
        var existing = _activeSkills.Find(s => s.SkillData.SkillId == skill.SkillData.SkillId);
        if (existing != null) { existing.SkillData.LevelUp(); return; }
        if (_activeSkills.Count >= 4) return;
        _activeSkills.Add(skill);
        OnActiveSkillsChanged?.Invoke(_activeSkills);
    }

    public bool CanEquipActive => _activeSkills.Count < 4;

    public void AddPassiveSkill(PassiveSkillBase skill)
    {
        var existing = _passiveSkills.Find(s => s.SkillData.SkillId == skill.SkillData.SkillId);
        if (existing != null) { existing.Remove(); existing.SkillData.LevelUp(); existing.Apply(); return; }
        if (_passiveSkills.Count >= 2) return;
        _passiveSkills.Add(skill);
        skill.Apply();
        OnPassiveSkillsChanged?.Invoke(_passiveSkills);
    }

    public bool CanEquipPassive => _passiveSkills.Count < 2;

    public void RemovePassiveSkill(PassiveSkillBase skill)
    {
        skill.Remove();
        _passiveSkills.Remove(skill);
        OnPassiveSkillsChanged?.Invoke(_passiveSkills);
    }

    public void ApplySkillToBall(Ball ball)
    {
        foreach (var skill in _activeSkills)
            ball.AddSkill(SkillFactory.CreateActiveSkill(skill.SkillData));
    }

    public void AddDamageMultiplier(float value)    => _damageMultiplierBonus += value;
    public void RemoveDamageMultiplier(float value) => _damageMultiplierBonus -= value;

    public void AddNextShotDamageBonus(float bonus) { _nextShotDamageBonus += bonus; }
    public float ConsumeNextShotDamageBonus() { float v = _nextShotDamageBonus; _nextShotDamageBonus = 0f; return v; }

    public IReadOnlyList<int> ActiveSkillIds  => _activeSkills.ConvertAll(s => s.SkillData.SkillId);
    public IReadOnlyList<int> PassiveSkillIds => _passiveSkills.ConvertAll(s => s.SkillData.SkillId);
}
