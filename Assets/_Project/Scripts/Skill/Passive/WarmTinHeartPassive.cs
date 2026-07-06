public class WarmTinHeartPassive : PassiveSkillBase
{
    public WarmTinHeartPassive(SkillRuntimeState state) : base(state) { }

    public override void Apply()
    {
        SkillManager.Instance.AddNormalBallDamageMultiplier(LevelData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveNormalBallDamageMultiplier(LevelData.Value1);
    }
}
