public class LaserBallSkill : BallSkillBase
{
    public LaserBallSkill(SkillRuntimeState state) : base(state) { }

    public override void OnBallHit(MonsterBase target)
    {
        // Value1=추가피해
        var row = WaveManager.Instance.GetMonstersInRow(target);
        foreach (var monster in row)
        {
            if (monster != target)
                monster.TakeDamage(LevelData.Value1);
        }
    }
}
