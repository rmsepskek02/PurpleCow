using System;
using UnityEngine;

public class Ball : MonoBehaviour, IPoolable
{
    [SerializeField] private BallData _ballData;

    private Rigidbody2D   _rigidbody;
    private Collider2D    _collider;
    private bool          _isActive;
    private BallSkillBase _skill;
    private int           _remainingBounces;

    public Vector2 LaunchDirection { get; private set; }
    public float   LastDamage      { get; private set; }

    public static event Action<float, bool> OnHitMonster;
    public static event Action<Ball>        OnBeforeReturn;  // LastHitPassive 연동용

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider  = GetComponent<Collider2D>();
    }

    public void OnSpawn()
    {
        _isActive         = true;
        _remainingBounces = _ballData.MaxBounces + SkillManager.Instance.BounceBonus;
        _rigidbody.linearVelocity = Vector2.zero;
        _skill?.OnActivate();
    }

    public void OnDespawn()
    {
        _isActive = false;
        _rigidbody.linearVelocity = Vector2.zero;
        _skill?.OnDeactivate();
    }

    public void Launch(Vector2 direction)
    {
        LaunchDirection = direction;
        float speed = _ballData.Speed + SkillManager.Instance.SpeedBonus;
        _rigidbody.linearVelocity = direction * speed;
    }

    private void FixedUpdate()
    {
        if (!_isActive)
            return;

        float speed = _ballData.Speed + SkillManager.Instance.SpeedBonus;
        _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            if (collision.gameObject.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                (float damage, bool isCritical) = CalculateDamage();
                _skill?.OnBallHit(monster, damage);
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            _remainingBounces--;
            if (_remainingBounces <= 0)
            {
                ReturnToPool();
            }
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ghost 모드(isTrigger=true)에서 몬스터 데미지 처리
        if (_skill is GhostBallSkill && other.CompareTag("Monster"))
        {
            if (other.TryGetComponent<MonsterBase>(out MonsterBase monster))
            {
                (float damage, bool isCritical) = CalculateDamage();
                _skill.OnBallHit(monster, damage);
            }
        }
    }

    private (float damage, bool isCritical) CalculateDamage()
    {
        float critChance = _ballData.CriticalChance + SkillManager.Instance.CritChanceBonus;
        bool  isCritical = UnityEngine.Random.value < critChance;

        float critMultiplier = _ballData.CriticalMultiplier + SkillManager.Instance.CritDamageBonus;
        float damage = isCritical
            ? _ballData.Damage * critMultiplier
            : _ballData.Damage;

        damage *= (1f + SkillManager.Instance.DamageMultiplierBonus);

        LastDamage = damage;
        OnHitMonster?.Invoke(damage, isCritical);
        return (damage, isCritical);
    }

    public void SetSkill(BallSkillBase skill)
    {
        _skill = skill;
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
        OnBeforeReturn?.Invoke(this);
        BallLauncher.Instance.ReturnBall(this);
    }
}
