using System;
using UnityEngine;

public class Ball : MonoBehaviour, IPoolable
{
    [SerializeField] private BallData _ballData;

    private Rigidbody2D _rigidbody;
    private bool _isActive;

    public static event Action<float, bool> OnHitMonster;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void OnSpawn()
    {
        _isActive = true;
        _rigidbody.linearVelocity = Vector2.zero;
    }

    public void OnDespawn()
    {
        _isActive = false;
        _rigidbody.linearVelocity = Vector2.zero;
    }

    public void Launch(Vector2 direction)
    {
        _rigidbody.linearVelocity = direction * _ballData.Speed;
    }

    private void FixedUpdate()
    {
        if (!_isActive)
            return;

        // velocity 유지 (물리 감쇠 방지용 선택적 처리)
        _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _ballData.Speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            var (damage, isCritical) = CalculateDamage();
            OnHitMonster?.Invoke(damage, isCritical);
            // 볼은 반사 지속 (기본 물리 반사, PhysicsMaterial2D bounciness=1)
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            // 물리 반사 — PhysicsMaterial2D bounciness=1 로 처리, 별도 코드 불필요
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            ReturnToPool();
        }
    }

    private (float damage, bool isCritical) CalculateDamage()
    {
        bool isCritical = UnityEngine.Random.value < _ballData.CriticalChance;
        float damage = isCritical
            ? _ballData.Damage * _ballData.CriticalMultiplier
            : _ballData.Damage;
        return (damage, isCritical);
    }

    private void ReturnToPool()
    {
        BallLauncher.Instance.ReturnBall(this);
    }
}
