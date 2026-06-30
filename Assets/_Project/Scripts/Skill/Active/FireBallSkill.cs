using UnityEngine;

public class FireBallSkill : BallSkillBase
{
    public FireBallSkill(SkillData skillData) : base(skillData) { }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        float radius   = _skillData.Value1;
        float bonusDmg = _skillData.Value2;

        Collider2D[] hits = Physics2D.OverlapCircleAll(target.transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                monster.TakeDamage(bonusDmg);
            }
        }
    }
}
