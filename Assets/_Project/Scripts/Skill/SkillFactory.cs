using System;

public static class SkillFactory
{
    public static BallSkillBase CreateActiveSkill(SkillData data)
    {
        return (ActiveSkillId)data.SkillId switch
        {
            ActiveSkillId.Fire    => new FireBallSkill(data),
            ActiveSkillId.Ice     => new IceBallSkill(data),
            ActiveSkillId.Ghost   => new GhostBallSkill(data),
            ActiveSkillId.Laser   => new LaserBallSkill(data),
            ActiveSkillId.Cluster => new ClusterBallSkill(data),
            _                     => throw new ArgumentOutOfRangeException(nameof(data.SkillId), data.SkillId, "Unknown active skill id")
        };
    }

    public static PassiveSkillBase CreatePassiveSkill(SkillData data)
    {
        return (PassiveSkillId)data.SkillId switch
        {
            PassiveSkillId.DamageUp     => new DamageUpPassive(data),
            PassiveSkillId.CritChanceUp => new CritChanceUpPassive(data),
            PassiveSkillId.CritDamageUp => new CritDamageUpPassive(data),
            PassiveSkillId.SpeedUp      => new SpeedUpPassive(data),
            PassiveSkillId.BounceUp     => new BounceUpPassive(data),
            PassiveSkillId.KillShot     => new KillShotPassive(data),
            PassiveSkillId.LastHit      => new LastHitPassive(data),
            _                           => throw new ArgumentOutOfRangeException(nameof(data.SkillId), data.SkillId, "Unknown passive skill id")
        };
    }
}
