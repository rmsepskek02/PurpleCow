public class AmethystDaggerPassive : PassiveSkillBase
{
    public AmethystDaggerPassive(SkillData data) : base(data) { }

    public override void Apply()
    {
        Ball.OnHitMonsterFront += HandleFrontHit;
    }

    public override void Remove()
    {
        Ball.OnHitMonsterFront -= HandleFrontHit;
    }

    private void HandleFrontHit(MonsterBase target)
    {
        target.ApplyBonusCritChance(LevelData.Value1);
    }
}
