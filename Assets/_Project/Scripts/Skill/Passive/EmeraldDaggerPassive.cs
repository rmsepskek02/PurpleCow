public class EmeraldDaggerPassive : PassiveSkillBase
{
    public EmeraldDaggerPassive(SkillRuntimeState state) : base(state) { }

    public override void Apply()
    {
        SkillManager.Instance.AddBackHitCriticalChance(LevelData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveBackHitCriticalChance(LevelData.Value1);
    }
}
