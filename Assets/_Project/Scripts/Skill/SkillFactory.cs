using System;

public static class SkillFactory
{
    public static BallSkillBase CreateActiveSkill(SkillRuntimeState state)
    {
        SkillData data = state.Data;
        return (ActiveSkillId)data.SkillId switch
        {
            ActiveSkillId.Fire    => new FireBallSkill(state),
            ActiveSkillId.Ice     => new IceBallSkill(state),
            ActiveSkillId.Ghost   => new GhostBallSkill(state),
            ActiveSkillId.Laser   => new LaserBallSkill(state),
            ActiveSkillId.Cluster => new ClusterBallSkill(state),
            _                     => throw new ArgumentOutOfRangeException(nameof(data.SkillId), data.SkillId, "Unknown active skill id")
        };
    }

    public static PassiveSkillBase CreatePassiveSkill(SkillRuntimeState state)
    {
        SkillData data = state.Data;
        return (PassiveSkillId)data.SkillId switch
        {
            PassiveSkillId.WarmTinHeart   => new WarmTinHeartPassive(state),
            PassiveSkillId.MagicMirror    => new MagicMirrorPassive(state),
            PassiveSkillId.AmethystDagger => new AmethystDaggerPassive(state),
            PassiveSkillId.EmeraldDagger  => new EmeraldDaggerPassive(state),
            PassiveSkillId.LastMatch      => new LastMatchPassive(state),
            _                             => throw new ArgumentOutOfRangeException(nameof(data.SkillId), data.SkillId, "Unknown passive skill id")
        };
    }
}
