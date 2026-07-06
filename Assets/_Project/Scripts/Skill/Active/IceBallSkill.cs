using UnityEngine;

public class IceBallSkill : BallSkillBase
{
    public IceBallSkill(SkillRuntimeState state) : base(state) { }

    public override void OnBallHit(MonsterBase target)
    {
        // Value1=확률, Value2=지속(초), Value3=슬로우율
        if (UnityEngine.Random.value < LevelData.Value1)
        {
            var rearMonsters = WaveManager.Instance.GetMonstersBehindInColumn(target);

            target.ApplyFreeze(LevelData.Value2);
            target.ApplySlow(LevelData.Value2, LevelData.Value3);
            target.TakeDamage(_ball.LastDamage * LevelData.Value3);

            foreach (MonsterBase monster in rearMonsters)
            {
                monster.ApplyFreeze(LevelData.Value2);
                monster.ApplySlow(LevelData.Value2, LevelData.Value3);
            }
        }
    }
}
