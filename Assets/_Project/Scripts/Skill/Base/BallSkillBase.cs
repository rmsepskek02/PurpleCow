public abstract class BallSkillBase
{
    protected SkillData _skillData;
    protected Ball _ball;

    protected BallSkillBase(SkillData skillData)
    {
        _skillData = skillData;
    }

    public void Initialize(Ball ball)
    {
        _ball = ball;
    }

    // Ball.OnCollisionEnter2D에서 Monster 충돌 시 호출
    public abstract void OnBallHit(MonsterBase target);

    // Ball이 풀에서 꺼내질 때 (OnSpawn 시) 호출 — 상태 초기화
    public virtual void OnActivate() { }

    // Ball이 풀로 돌아갈 때 (OnDespawn 시) 호출 — 상태 정리
    public virtual void OnDeactivate() { }

    public SkillData SkillData => _skillData;

    protected SkillLevelData LevelData => _skillData.CurrentLevelData;
}
