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
            PassiveSkillId.WarmTinHeart   => new WarmTinHeartPassive(data),
            PassiveSkillId.MagicMirror    => new MagicMirrorPassive(data),
            PassiveSkillId.AmethystDagger => new AmethystDaggerPassive(data),
            PassiveSkillId.EmeraldDagger  => new EmeraldDaggerPassive(data),
            PassiveSkillId.LastMatch      => new LastMatchPassive(data),
            _                             => throw new ArgumentOutOfRangeException(nameof(data.SkillId), data.SkillId, "Unknown passive skill id")
        };
    }
}
