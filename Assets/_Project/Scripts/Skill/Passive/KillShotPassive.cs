public class KillShotPassive : PassiveSkillBase
{
    public KillShotPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    public override void Remove()
    {
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        BallLauncher.Instance.LaunchSubBalls(monster.transform.position, 1);
    }
}
