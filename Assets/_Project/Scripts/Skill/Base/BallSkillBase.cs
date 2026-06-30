using UnityEngine;

public abstract class BallSkillBase : MonoBehaviour
{
    [SerializeField] protected SkillData _skillData;

    // Ball 컴포넌트 참조 (Awake에서 캐싱)
    protected Ball _ball;

    protected virtual void Awake()
    {
        _ball = GetComponent<Ball>();
    }

    // Ball.OnCollisionEnter2D에서 Monster 충돌 시 호출
    public abstract void OnBallHit(MonsterBase target, float baseDamage);

    // Ball이 풀에서 꺼내질 때 (OnSpawn 시) 호출 — 상태 초기화
    public virtual void OnActivate() { }

    // Ball이 풀로 돌아갈 때 (OnDespawn 시) 호출 — 상태 정리
    public virtual void OnDeactivate() { }

    public SkillData SkillData => _skillData;
}
