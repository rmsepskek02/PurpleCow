using System;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour, IPoolable
{
    private const float RETURN_ARRIVAL_DISTANCE = 0.3f;

    [SerializeField] private BallData _ballData;

    private Rigidbody2D          _rigidbody;
    private Collider2D           _collider;
    private bool                 _isActive;
    private bool                 _isReturning;
    private List<BallSkillBase>  _skills = new List<BallSkillBase>();
    private int                  _remainingBounces;
    private float                _subBallDamageOverride;

    public Vector2 LaunchDirection { get; private set; }
    public float   LastDamage      { get; private set; }

    public static event Action<MonsterBase, float, bool> OnHitMonster;
    public static event Action                    OnWallHit;
    public static event Action<MonsterBase>       OnHitMonsterFront;
    public static event Action<MonsterBase>       OnHitMonsterBack;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider  = GetComponent<Collider2D>();
    }

    public void OnSpawn()
    {
        _isActive              = true;
        _isReturning            = false;
        _remainingBounces      = _ballData.MaxBounces;
        _subBallDamageOverride = 0f;
        _skills.Clear();
        _rigidbody.linearVelocity = Vector2.zero;
    }

    public void OnDespawn()
    {
        _isActive    = false;
        _isReturning = false;
        _rigidbody.linearVelocity = Vector2.zero;
        foreach (var skill in _skills)
            skill.OnDeactivate();
        _skills.Clear();
    }

    public void Launch(Vector2 direction)
    {
        LaunchDirection = direction;
        float speed = _ballData.Speed;
        _rigidbody.linearVelocity = direction * speed;
    }

    // 귀환 후 재발사 직전 호출 — 반사 횟수/서브볼 데미지/장착 스킬을 초기화한다.
    // (OnSpawn()과 달리 풀을 거치지 않으므로 별도 메서드로 분리)
    public void PrepareForRelaunch()
    {
        _remainingBounces      = _ballData.MaxBounces;
        _subBallDamageOverride = 0f;

        foreach (var skill in _skills)
            skill.OnDeactivate();
        _skills.Clear();
    }

    private void FixedUpdate()
    {
        if (!_isActive)
            return;

        if (_isReturning)
        {
            Vector2 toLaunchPoint = (Vector2)BallLauncher.Instance.LaunchPoint.position - (Vector2)transform.position;
            if (toLaunchPoint.magnitude <= RETURN_ARRIVAL_DISTANCE)
            {
                _isReturning = false;
                BallLauncher.Instance.RelaunchBall(this);
                return;
            }
        }

        float speed = _ballData.Speed;
        _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            if (collision.gameObject.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                CalculateDamage(monster);

                // 전면/후면 판정 — 볼 이동 방향이 아래(velocity.y < 0)면 전면 타격
                Vector2 vel = _rigidbody.linearVelocity.normalized;
                if (vel.y < 0f)
                    OnHitMonsterFront?.Invoke(monster);
                else
                    OnHitMonsterBack?.Invoke(monster);

                foreach (var skill in _skills)
                    skill.OnBallHit(monster);
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            OnWallHit?.Invoke();

            // 이미 귀환 중인 볼은 반사 카운트를 건드리지 않고 LaunchPoint 방향으로 재조준만 한다.
            if (_isReturning)
            {
                ReturnToLaunchPoint();
                return;
            }

            _remainingBounces--;
            if (_remainingBounces <= 0)
            {
                // 로스터 소속 볼만 캐릭터 위치로 귀환 후 재발사한다.
                // 로스터 밖의 볼(서브볼 등)은 기존과 동일하게 즉시 풀로 반환한다.
                if (BallLauncher.Instance.IsRosterMember(this))
                    ReturnToLaunchPoint();
                else
                    ReturnToPool();
            }
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            // 로스터 소속 볼만 캐릭터 위치로 귀환 후 재발사한다.
            // 로스터 밖의 볼(서브볼 등)은 기존과 동일하게 즉시 풀로 반환한다.
            if (BallLauncher.Instance.IsRosterMember(this))
                ReturnToLaunchPoint();
            else
                ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ghost 모드(isTrigger=true)에서 몬스터 데미지 처리
        bool hasGhostSkill = false;
        foreach (var skill in _skills)
        {
            if (skill is GhostBallSkill) { hasGhostSkill = true; break; }
        }

        if (hasGhostSkill && other.CompareTag("Monster"))
        {
            if (other.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                CalculateDamage(monster);
                foreach (var skill in _skills)
                    skill.OnBallHit(monster);
            }
        }
    }

    private void CalculateDamage(MonsterBase target)
    {
        float baseDamage = _subBallDamageOverride > 0f ? _subBallDamageOverride : _ballData.Damage;

        float critChance = _ballData.CriticalChance + target.ConsumeBonusCritChance();
        bool  isCritical = UnityEngine.Random.value < critChance;

        float critMultiplier = _ballData.CriticalMultiplier;
        float damage = isCritical
            ? baseDamage * critMultiplier
            : baseDamage;

        damage *= (1f + SkillManager.Instance.DamageMultiplierBonus);
        damage += SkillManager.Instance.ConsumeNextShotDamageBonus();

        LastDamage = damage;
        target.TakeDamage(damage);
        OnHitMonster?.Invoke(target, damage, isCritical);
    }

    public void AddSkill(BallSkillBase skill)
    {
        _skills.Add(skill);
        skill.Initialize(this);
        skill.OnActivate();
    }

    public void SetSubBallDamage(float damage)
    {
        _subBallDamageOverride = damage;
    }

    public void SetGhostMode(bool isGhost)
    {
        _collider.isTrigger = isGhost;
    }

    public void ForceReturn()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        BallLauncher.Instance.ReturnBall(this);
    }

    // Ground 충돌 시점의 자연 반사 방향을 무시하고, 위치는 그대로 둔 채
    // 이동 방향만 LaunchPoint 쪽으로 강제 재설정해 계속 날아가게 한다(순간이동 아님).
    // LaunchPoint에 도달하면 FixedUpdate()에서 재발사(BallLauncher.RelaunchBall)를 트리거한다.
    private void ReturnToLaunchPoint()
    {
        Vector2 direction = ((Vector2)BallLauncher.Instance.LaunchPoint.position - (Vector2)transform.position).normalized;
        _rigidbody.linearVelocity = direction * _ballData.Speed;
        _isReturning = true;
    }
}
