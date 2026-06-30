using UnityEngine;

public class IceBallSkill : BallSkillBase
{
    public IceBallSkill(SkillData skillData) : base(skillData) { }

    public override void OnBallHit(MonsterBase target)
    {
        // Value1=확률, Value2=지속(초), Value3=슬로우율
        if (UnityEngine.Random.value < LevelData.Value1)
        {
            target.ApplyFreeze(LevelData.Value2);
            target.ApplySlow(Mathf.RoundToInt(LevelData.Value2), LevelData.Value3);
            target.TakeDamage(_ball.LastDamage * LevelData.Value3);
        }
    }
}
