public class DamageUpPassive : PassiveSkillBase
{
    public DamageUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddDamageMultiplier(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveDamageMultiplier(_skillData.Value1);
    }
}
