public class CritDamageUpPassive : PassiveSkillBase
{
    public CritDamageUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddCritDamageBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveCritDamageBonus(_skillData.Value1);
    }
}
