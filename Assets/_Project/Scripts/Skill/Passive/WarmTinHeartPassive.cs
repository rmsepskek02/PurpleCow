public class WarmTinHeartPassive : PassiveSkillBase
{
    public WarmTinHeartPassive(SkillData data) : base(data) { }

    public override void Apply()
    {
        SkillManager.Instance.AddDamageMultiplier(LevelData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveDamageMultiplier(LevelData.Value1);
    }
}
