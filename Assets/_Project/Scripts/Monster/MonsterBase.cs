using System;
using UnityEngine;

public class MonsterBase : MonoBehaviour, IPoolable
{
    [SerializeField] private MonsterData _monsterData;

    private float _currentHp;
    private bool _isDead;

    public float CurrentHp => _currentHp;
    public bool IsAlive    => !_isDead;

    public static event Action<MonsterBase> OnMonsterDied;

    private void OnEnable()
    {
        Ball.OnHitMonster += HandleHitMonster;
    }

    private void OnDisable()
    {
        Ball.OnHitMonster -= HandleHitMonster;
    }

    public void OnSpawn()
    {
        _currentHp = _monsterData.Hp;
        _isDead = false;
    }

    public void OnDespawn()
    {
        _isDead = true;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _currentHp -= damage;

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        OnMonsterDied?.Invoke(this);
    }

    public void MoveDown(float distance)
    {
        transform.position += (Vector3)(Vector2.down * distance);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            TakeDamage(collision.gameObject.GetComponent<Ball>().LastDamage);
        }
    }

    private void HandleHitMonster(float damage, bool isCritical)
    {
        // Ball.OnHitMonster 구독 핸들러
        // 실제 데미지 처리는 OnCollisionEnter2D에서 LastDamage를 통해 수행
    }
}
