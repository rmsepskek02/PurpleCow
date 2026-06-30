public class EmeraldDaggerPassive : PassiveSkillBase
{
    public EmeraldDaggerPassive(SkillData data) : base(data) { }

    public override void Apply()
    {
        Ball.OnHitMonsterBack += HandleBackHit;
    }

    public override void Remove()
    {
        Ball.OnHitMonsterBack -= HandleBackHit;
    }

    private void HandleBackHit(MonsterBase target)
    {
        target.ApplyBonusCritChance(LevelData.Value1);
    }
}
