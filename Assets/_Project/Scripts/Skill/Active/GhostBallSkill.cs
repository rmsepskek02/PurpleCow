public class GhostBallSkill : BallSkillBase
{
    public GhostBallSkill(SkillData skillData) : base(skillData) { }

    // Ghost 볼은 Monster 레이어와 물리 충돌하지 않음
    // Collider를 Trigger로 전환 → OnTriggerEnter2D로 데미지 처리

    public override void OnActivate()
    {
        _ball.SetGhostMode(true);
    }

    public override void OnDeactivate()
    {
        _ball.SetGhostMode(false);
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        // 관통 — 추가 처리 없음, 볼은 계속 이동
    }
}
