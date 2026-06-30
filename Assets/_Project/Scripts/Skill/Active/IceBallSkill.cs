using UnityEngine;

public class IceBallSkill : BallSkillBase
{
    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        int freezeTurns = Mathf.RoundToInt(_skillData.Value1);
        target.ApplyFreeze(freezeTurns);
    }
}
