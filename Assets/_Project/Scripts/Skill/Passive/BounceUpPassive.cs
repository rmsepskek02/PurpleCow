using UnityEngine;

public class BounceUpPassive : PassiveSkillBase
{
    public BounceUpPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        SkillManager.Instance.AddBounceBonus(Mathf.RoundToInt(_skillData.Value1));
    }

    public override void Remove()
    {
        SkillManager.Instance.RemoveBounceBonus(Mathf.RoundToInt(_skillData.Value1));
    }
}
