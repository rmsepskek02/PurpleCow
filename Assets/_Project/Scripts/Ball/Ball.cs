using System;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour, IPoolable
{
    private const float RETURN_ARRIVAL_DISTANCE = 0.3f;

    [SerializeField] private BallData _ballData;

    private Rigidbody2D          _rigidbody;
    private Collider2D           _collider;
    private SpriteRenderer       _spriteRenderer;
    private Sprite               _normalBallSprite;
    private bool                 _isActive;
    private bool                 _isReturning;
    private List<BallSkillBase>  _skills = new List<BallSkillBase>();
    private SkillRuntimeState    _skillState;
    private bool                 _isSpecialBall;
    private int                  _remainingBounces;
    private float                _subBallDamageOverride;
    private float                _nextHitDamageMultiplier;
    private float                _speedMultiplier = 1f;
    private bool                 _isClone;
    private int                  _remainingCloneReturns;

    public Vector2 LaunchDirection { get; private set; }
    public float   LastDamage      { get; private set; }
    public bool    IsClone         => _isClone;

    public static event Action<MonsterBase, float, bool> OnHitMonster;
    public static event Action<Ball> OnWallHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider  = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _normalBallSprite = _spriteRenderer.sprite;
    }

    public void OnSpawn()
    {
        _isActive              = true;
        _isReturning            = false;
        _remainingBounces      = _ballData.MaxBounces;
        _subBallDamageOverride = 0f;
        _nextHitDamageMultiplier = 0f;
        _skillState            = null;
        _isSpecialBall         = false;
        _speedMultiplier       = 1f;
        _isClone               = false;
        _remainingCloneReturns = 0;
        _skills.Clear();
        _collider.isTrigger = false;
        _spriteRenderer.sprite = _normalBallSprite;
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
        _rigidbody.linearVelocity = direction.normalized * CurrentSpeed;
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
                BallLauncher.Instance.HandleBallRecovered(this);
                return;
            }
        }

        _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * CurrentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            if (collision.gameObject.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                Vector2 vel = _rigidbody.linearVelocity.normalized;
                CalculateDamage(monster, vel.y < 0f);

                // 전면/후면 판정 — 볼 이동 방향이 아래(velocity.y < 0)면 전면 타격
                foreach (var skill in _skills)
                    skill.OnBallHit(monster);
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            HandleWallHit();
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            HandleGroundHit();
        }
    }

    private void HandleWallHit()
    {
        OnWallHit?.Invoke(this);

        // 이미 귀환 중인 볼은 반사 카운트를 건드리지 않고 LaunchPoint 방향으로 재조준만 한다.
        if (_isReturning)
        {
            ReturnToLaunchPoint();
            return;
        }

        // 원본 로스터 볼과 분신은 벽 반사 횟수를 소모하지 않고 Ground에서만 귀환한다.
        if (BallLauncher.Instance.IsRosterMember(this) || _isClone)
            return;

        _remainingBounces--;
        if (_remainingBounces <= 0)
            ReturnToPool();
    }

    // Wall_Left/Wall_Right는 좁고 긴 세로 벽, Wall_Top은 넓고 얇은 가로 벽이라는
    // 씬 구성(SceneSetupEditor)을 근거로, Collider Bounds의 가로/세로 비율만으로
    // 반사축을 판별한다(별도 좌/우/상단 태그 구분이 없어도 동작한다).
    private void ReflectOffTriggerWall(Collider2D wallCollider)
    {
        Vector2 extents = wallCollider.bounds.extents;
        Vector2 velocity = _rigidbody.linearVelocity;
        if (extents.x < extents.y)
            velocity.x = -velocity.x;
        else
            velocity.y = -velocity.y;
        _rigidbody.linearVelocity = velocity;
    }

    private void HandleGroundHit()
    {
        // 원본 로스터 볼과 분신은 캐릭터 위치로 귀환하고, 서브볼은 즉시 풀로 반환한다.
        if (BallLauncher.Instance.IsRosterMember(this) || _isClone)
            ReturnToLaunchPoint();
        else
            ReturnToPool();
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
                Vector2 vel = _rigidbody.linearVelocity.normalized;
                CalculateDamage(monster, vel.y < 0f);
                foreach (var skill in _skills)
                    skill.OnBallHit(monster);
            }
        }
        else if (other.CompareTag("Wall"))
        {
            // 트리거 콜라이더는 물리 반사가 자동으로 일어나지 않으므로,
            // OnCollisionEnter2D와 달리 여기서는 직접 속도를 반사시켜야 한다.
            ReflectOffTriggerWall(other);
            HandleWallHit();
        }
        else if (other.CompareTag("Ground"))
        {
            HandleGroundHit();
        }
    }

    private void CalculateDamage(MonsterBase target, bool isFrontHit)
    {
        float baseDamage = _subBallDamageOverride > 0f
            ? _subBallDamageOverride
            : _skillState != null
                ? _skillState.CurrentLevelData.BallDamage
                : _ballData.Damage;

        float directionalCritBonus = isFrontHit
            ? SkillManager.Instance.FrontHitCriticalChanceBonus
            : SkillManager.Instance.BackHitCriticalChanceBonus;
        float critChance = Mathf.Clamp01(_ballData.CriticalChance + directionalCritBonus);
        bool  isCritical = UnityEngine.Random.value < critChance;

        float critMultiplier = _ballData.CriticalMultiplier;
        float damage = isCritical
            ? baseDamage * critMultiplier
            : baseDamage;

        if (!_isSpecialBall)
            damage *= 1f + SkillManager.Instance.NormalBallDamageMultiplierBonus;

        if (_nextHitDamageMultiplier > 0f)
        {
            damage *= 1f + _nextHitDamageMultiplier;
            _nextHitDamageMultiplier = 0f;
        }

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

    public void ConfigureSkillBall(SkillRuntimeState state)
    {
        _skillState = state;
        _isSpecialBall = state != null;
        _spriteRenderer.sprite = state?.Data.BallSprite != null
            ? state.Data.BallSprite
            : _normalBallSprite;
    }

    public void ConfigureSubBall(float damage, Sprite sprite)
    {
        _skillState = null;
        _isSpecialBall = true;
        _subBallDamageOverride = damage;
        _spriteRenderer.sprite = sprite != null ? sprite : _normalBallSprite;
    }

    public void AddNextHitDamageMultiplier(float value)
    {
        _nextHitDamageMultiplier += Mathf.Max(0f, value);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);

        if (_isActive && _rigidbody.linearVelocity.sqrMagnitude > 0f)
            _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * CurrentSpeed;
    }

    public void ConfigureClone(int returnCount)
    {
        _isClone = true;
        _remainingCloneReturns = Mathf.Max(1, returnCount);
    }

    public bool ConsumeCloneReturn()
    {
        if (!_isClone)
            return false;

        _remainingCloneReturns--;
        return _remainingCloneReturns <= 0;
    }

    public void ParkAtLaunchPoint(Vector3 position)
    {
        transform.position = position;
        _rigidbody.linearVelocity = Vector2.zero;
        _isReturning = false;
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
    // LaunchPoint에 도달하면 FixedUpdate()에서 FIFO 재발사 큐 등록을 트리거한다.
    private void ReturnToLaunchPoint()
    {
        Vector2 direction = ((Vector2)BallLauncher.Instance.LaunchPoint.position - (Vector2)transform.position).normalized;
        _rigidbody.linearVelocity = direction * CurrentSpeed;
        _isReturning = true;
    }

    private float CurrentSpeed => _ballData.Speed * _speedMultiplier;
}
