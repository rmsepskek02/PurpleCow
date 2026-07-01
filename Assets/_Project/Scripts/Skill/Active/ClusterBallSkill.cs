public class ClusterBallSkill : BallSkillBase
{
    public ClusterBallSkill(SkillData skillData) : base(skillData) { }

    public override void OnBallHit(MonsterBase target)
    {
        // Value1=확률, Value2=서브볼피해
        if (UnityEngine.Random.value < LevelData.Value1)
        {
            BallLauncher.Instance.LaunchSubBalls(_ball.transform.position, 1, LevelData.Value2);
        }
    }
}
