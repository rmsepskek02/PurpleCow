using UnityEngine;

public class ClusterBallSkill : BallSkillBase
{
    private bool _hasExploded;   // 1회만 폭발하도록 제어

    public override void OnActivate()
    {
        _hasExploded = false;
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        if (_hasExploded) return;
        _hasExploded = true;

        int subBallCount = Mathf.RoundToInt(_skillData.Value1);
        BallLauncher.Instance.LaunchSubBalls(_ball.transform.position, subBallCount);
    }
}
