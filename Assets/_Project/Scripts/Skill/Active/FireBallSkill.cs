public class FireBallSkill : BallSkillBase
{
    public FireBallSkill(SkillData skillData) : base(skillData) { }

    public override void OnBallHit(MonsterBase target)
    {
        // Value1=지속시간, Value2=최대중첩, Value3=초당피해
        target.ApplyDot(LevelData.Value3, LevelData.Value1, (int)LevelData.Value2);
    }
}
