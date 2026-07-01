public class MagicMirrorPassive : PassiveSkillBase
{
    public MagicMirrorPassive(SkillData data) : base(data) { }

    public override void Apply()
    {
        Ball.OnWallHit += HandleWallHit;
    }

    public override void Remove()
    {
        Ball.OnWallHit -= HandleWallHit;
    }

    private void HandleWallHit()
    {
        SkillManager.Instance.AddNextShotDamageBonus(LevelData.Value1);
    }
}
