public class AmethystDaggerPassive : PassiveSkillBase
{
    public AmethystDaggerPassive(SkillRuntimeState state) : base(state) { }

    public override void Apply()
    {
        SkillManager.Instance.AddFrontHitCriticalChance(LevelData.Value1);
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveFrontHitCriticalChance(LevelData.Value1);
    }
}
