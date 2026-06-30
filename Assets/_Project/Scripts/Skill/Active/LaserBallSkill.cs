using UnityEngine;

public class LaserBallSkill : BallSkillBase
{
    public override void OnActivate()
    {
        // 발사 시점에 Raycast로 직선 관통 데미지 적용 후 볼 즉시 반납
        FireLaser();
        _ball.ForceReturn();   // 레이저는 발사 즉시 처리 완료
    }

    public override void OnBallHit(MonsterBase target, float baseDamage)
    {
        // Raycast에서 처리하므로 충돌 콜백 없음
    }

    private void FireLaser()
    {
        Vector2 origin    = _ball.transform.position;
        Vector2 direction = _ball.LaunchDirection;
        float   damage    = _skillData.Value1;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Infinity,
                                LayerMask.GetMask("Monster"));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                monster.TakeDamage(damage);
            }
        }
    }
}
