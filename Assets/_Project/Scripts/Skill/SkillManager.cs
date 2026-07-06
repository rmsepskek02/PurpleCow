using System;
using System.Collections.Generic;

public class SkillManager : Singleton<SkillManager>
{
    private List<BallSkillBase>    _activeSkills  = new List<BallSkillBase>();
    private List<PassiveSkillBase> _passiveSkills = new List<PassiveSkillBase>();

    // Passive 누적 보너스
    private float _normalBallDamageMultiplierBonus;
    private float _frontHitCriticalChanceBonus;
    private float _backHitCriticalChanceBonus;

    public float NormalBallDamageMultiplierBonus => _normalBallDamageMultiplierBonus;
    public float FrontHitCriticalChanceBonus => _frontHitCriticalChanceBonus;
    public float BackHitCriticalChanceBonus => _backHitCriticalChanceBonus;

    public static event Action<List<BallSkillBase>>    OnActiveSkillsChanged;
    public static event Action<List<PassiveSkillBase>> OnPassiveSkillsChanged;

    protected override void Awake()
    {
        base.Awake();
        _activeSkills  = new List<BallSkillBase>();
        _passiveSkills = new List<PassiveSkillBase>();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _passiveSkills.Count; i++)
            _passiveSkills[i].Remove();
    }

    // 반환값: 로스터에 새 볼을 추가해야 하는 "신규 장착"이면 true, 기존 타입의 "레벨업"이면 false.
    public bool EquipActiveSkill(SkillData data, out SkillRuntimeState state)
    {
        BallSkillBase existing = _activeSkills.Find(s => s.SkillData.SkillId == data.SkillId);
        if (existing != null)
        {
            existing.State.TryLevelUp();
            state = existing.State;
            OnActiveSkillsChanged?.Invoke(_activeSkills);
            return false;
        }
        if (_activeSkills.Count >= 4)
        {
            state = null;
            return false;
        }
        state = new SkillRuntimeState(data);
        BallSkillBase skill = SkillFactory.CreateActiveSkill(state);
        _activeSkills.Add(skill);
        OnActiveSkillsChanged?.Invoke(_activeSkills);
        return true;
    }

    public bool CanEquipActive => _activeSkills.Count < 4;

    public void AddPassiveSkill(SkillData data)
    {
        PassiveSkillBase existing = _passiveSkills.Find(s => s.SkillData.SkillId == data.SkillId);
        if (existing != null)
        {
            existing.Remove();
            existing.State.TryLevelUp();
            existing.Apply();
            OnPassiveSkillsChanged?.Invoke(_passiveSkills);
            return;
        }
        if (_passiveSkills.Count >= 2) return;
        SkillRuntimeState state = new SkillRuntimeState(data);
        PassiveSkillBase skill = SkillFactory.CreatePassiveSkill(state);
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

    public void AddNormalBallDamageMultiplier(float value)
        => _normalBallDamageMultiplierBonus += value;
    public void RemoveNormalBallDamageMultiplier(float value)
        => _normalBallDamageMultiplierBonus -= value;
    public void AddFrontHitCriticalChance(float value)
        => _frontHitCriticalChanceBonus += value;
    public void RemoveFrontHitCriticalChance(float value)
        => _frontHitCriticalChanceBonus -= value;
    public void AddBackHitCriticalChance(float value)
        => _backHitCriticalChanceBonus += value;
    public void RemoveBackHitCriticalChance(float value)
        => _backHitCriticalChanceBonus -= value;

    public IReadOnlyList<int> ActiveSkillIds  => _activeSkills.ConvertAll(s => s.SkillData.SkillId);
    public IReadOnlyList<int> PassiveSkillIds => _passiveSkills.ConvertAll(s => s.SkillData.SkillId);

    public IReadOnlyList<BallSkillBase>    EquippedActiveSkills  => _activeSkills;
    public IReadOnlyList<PassiveSkillBase> EquippedPassiveSkills => _passiveSkills;

    public SkillRuntimeState GetActiveSkillState(int skillId)
        => _activeSkills.Find(s => s.SkillData.SkillId == skillId)?.State;

    public SkillRuntimeState GetPassiveSkillState(int skillId)
        => _passiveSkills.Find(s => s.SkillData.SkillId == skillId)?.State;
}
