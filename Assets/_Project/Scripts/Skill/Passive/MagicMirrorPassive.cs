public class MagicMirrorPassive : PassiveSkillBase
{
    public MagicMirrorPassive(SkillRuntimeState state) : base(state) { }

    public override void Apply()
    {
        Ball.OnWallHit += HandleWallHit;
    }

    public override void Remove()
    {
        Ball.OnWallHit -= HandleWallHit;
    }

    private void HandleWallHit(Ball ball)
    {
        ball.AddNextHitDamageMultiplier(LevelData.Value1);
    }
}
