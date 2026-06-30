public class CritChanceUpPassive : PassiveSkillBase
{
    public CritChanceUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddCritChanceBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveCritChanceBonus(_skillData.Value1);
    }
}
