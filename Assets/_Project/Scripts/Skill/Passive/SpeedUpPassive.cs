public class SpeedUpPassive : PassiveSkillBase
{
    public SpeedUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddSpeedBonus(_skillData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveSpeedBonus(_skillData.Value1);
    }
}
