using UnityEngine;

public class IceBallSkill : BallSkillBase
{
    public IceBallSkill(SkillData skillData) : base(skillData) { }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        int freezeTurns = Mathf.RoundToInt(_skillData.Value1);
        target.ApplyFreeze(freezeTurns);
    }
}
