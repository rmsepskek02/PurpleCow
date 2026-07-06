using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MonsterBase : MonoBehaviour, IPoolable
{
    private struct DotStack
    {
        public float DamagePerSecond;
        public float RemainingSeconds;
    }

    private static readonly Dictionary<BlockSize, Vector2> ColliderSizeMap = new Dictionary<BlockSize, Vector2>
    {
        { BlockSize.OneByOne, new Vector2(0.96f, 0.96f) },
        { BlockSize.TwoByOne, new Vector2(1.92f, 0.96f) },
        { BlockSize.OneByTwo, new Vector2(0.96f, 1.92f) },
    };

    // 1칸 폭 기준값 1f (기존 sizeDelta.x = 1 참고), 가로 2칸(TwoByOne)은 2배 폭
    private static readonly Dictionary<BlockSize, float> HpBarWidthMap = new Dictionary<BlockSize, float>
    {
        { BlockSize.OneByOne, 1f },
        { BlockSize.TwoByOne, 2f },
        { BlockSize.OneByTwo, 1f },
    };

    [SerializeField] private MonsterData _monsterData;

    private float _currentHp;
    private bool  _isDead;
    private float _frozenSecondsRemaining;
    private float _slowSecondsRemaining;
    private float _slowPercent;
    private readonly List<DotStack> _dotStacks = new List<DotStack>();
    private float _dotTickTimer;
    private BoxCollider2D _bodyCollider;
    private Sequence _bottomAttackSequence;
    private bool _isBottomAttacking;

    private SpriteRenderer _spriteRenderer;
    private Color _baseColor;

    [SerializeField] private Color _hitFlashColor = Color.white;
    [SerializeField] private float _hitFlashDuration = 0.1f;
    [SerializeField] private Color _freezeTintColor = new Color(0.53f, 0.81f, 0.98f);
    [SerializeField] private Color _burnTintColor = new Color(1f, 0.35f, 0.16f);

    private float _flashSecondsRemaining;

    private enum StatusVisualType { None, Ice, Fire }
    private StatusVisualType _lastStatusVisual;

    public float CurrentHp    => _currentHp;
    public bool  IsAlive      => !_isDead;
    public bool  IsFrozen     => _frozenSecondsRemaining > 0f;
    public bool  IsBottomAttacking => _isBottomAttacking;
    public MonsterData Data   => _monsterData;

    public static event Action<MonsterBase> OnMonsterDied;
    public event Action<float, float> OnHpChanged;

    private void Awake()
    {
        _bodyCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseColor = _spriteRenderer.color;
    }

    public void OnSpawn()
    {
        KillBottomAttackSequence();
        _currentHp              = _monsterData.Hp;
        _isDead                 = false;
        _isBottomAttacking      = false;
        _frozenSecondsRemaining = 0f;
        _slowSecondsRemaining   = 0f;
        _slowPercent            = 0f;
        _dotStacks.Clear();
        _dotTickTimer           = 0f;
        if (_bodyCollider != null)
            _bodyCollider.enabled = true;
        _flashSecondsRemaining  = 0f;
        _lastStatusVisual       = StatusVisualType.None;
        _spriteRenderer.color   = _baseColor;
        ApplyBlockSize();
        OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
    }

    public void ApplyData(MonsterData data)
    {
        _monsterData = data;
        _currentHp   = _monsterData.Hp;
        ApplyBlockSize();
        OnHpChanged?.Invoke(_currentHp, _monsterData.Hp);
    }

    private void ApplyBlockSize()
    {
        if (_monsterData == null)
            return;

        if (_bodyCollider != null &&
            ColliderSizeMap.TryGetValue(_monsterData.BlockSize, out Vector2 colliderSize))
        {
            _bodyCollider.size = colliderSize;
        }

        RectTransform hpBarRect = GetComponentInChildren<RectTransform>();
        if (hpBarRect != null && HpBarWidthMap.TryGetValue(_monsterData.BlockSize, out float hpBarWidth))
        {
            Vector2 sizeDelta = hpBarRect.sizeDelta;
            sizeDelta.x = hpBarWidth;
            hpBarRect.sizeDelta = sizeDelta;
        }
    }

    public void OnDespawn()
    {
        KillBottomAttackSequence();
        _isDead = true;
        _isBottomAttacking = false;
        _dotStacks.Clear();
        _dotTickTimer = 0f;
        if (_bodyCollider != null)
            _bodyCollider.enabled = true;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead || _isBottomAttacking)
            return;

        _flashSecondsRemaining = _hitFlashDuration;

        _currentHp -= damage;
        OnHpChanged?.Invoke(Mathf.Max(_currentHp, 0f), _monsterData.Hp);

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

    public void ApplyFreeze(float seconds)
    {
        if (_isDead || _isBottomAttacking)
            return;

        _frozenSecondsRemaining = Mathf.Max(_frozenSecondsRemaining, seconds);
        _lastStatusVisual = StatusVisualType.Ice;
    }

    public void ApplySlow(float seconds, float percent)
    {
        if (_isDead || _isBottomAttacking)
            return;

        _slowSecondsRemaining = seconds;
        _slowPercent = percent;
        _lastStatusVisual = StatusVisualType.Ice;
    }

    public void ApplyDot(float damagePerSec, float duration, int maxStacks)
    {
        if (_isDead || _isBottomAttacking || damagePerSec <= 0f || duration <= 0f || maxStacks <= 0)
            return;

        if (_dotStacks.Count >= maxStacks)
            _dotStacks.RemoveAt(0);

        _dotStacks.Add(new DotStack
        {
            DamagePerSecond = damagePerSec,
            RemainingSeconds = duration,
        });

        _lastStatusVisual = StatusVisualType.Fire;
    }

    private void UpdateDot(float deltaTime)
    {
        if (_dotStacks.Count == 0)
            return;

        _dotTickTimer += deltaTime;
        for (int i = 0; i < _dotStacks.Count; i++)
        {
            DotStack stack = _dotStacks[i];
            stack.RemainingSeconds -= deltaTime;
            _dotStacks[i] = stack;
        }

        while (_dotTickTimer >= 1f && !_isDead)
        {
            _dotTickTimer -= 1f;
            float tickDamage = 0f;
            for (int i = 0; i < _dotStacks.Count; i++)
            {
                if (_dotStacks[i].RemainingSeconds >= 0f)
                    tickDamage += _dotStacks[i].DamagePerSecond;
            }

            if (tickDamage > 0f)
                TakeDamage(tickDamage);
        }

        _dotStacks.RemoveAll(stack => stack.RemainingSeconds < 0f);
        if (_dotStacks.Count == 0)
            _dotTickTimer = 0f;
    }

    private void Update()
    {
        if (_isDead || _isBottomAttacking)
            return;

        float deltaTime = Time.deltaTime;
        UpdateDot(deltaTime);

        UpdateStatusVisual(deltaTime);

        if (_slowSecondsRemaining > 0f)
            _slowSecondsRemaining -= deltaTime;

        if (_frozenSecondsRemaining > 0f)
        {
            _frozenSecondsRemaining -= deltaTime;
            return;
        }

        if (WaveManager.Instance != null && WaveManager.Instance.HasFrozenMonsterAhead(this))
            return;

        float speed = _monsterData.MoveSpeed;

        if (_slowSecondsRemaining > 0f)
        {
            speed *= (1f - _slowPercent);
        }

        transform.position += Vector3.down * speed * deltaTime;
    }

    public bool TryGetHorizontalBounds(out float minX, out float maxX)
    {
        if (!TryGetColliderBounds(out Bounds bounds))
        {
            minX = 0f;
            maxX = 0f;
            return false;
        }

        minX = bounds.min.x;
        maxX = bounds.max.x;
        return true;
    }

    public bool TryGetColliderBounds(out Bounds bounds)
    {
        if (_bodyCollider == null || !_bodyCollider.enabled)
        {
            bounds = default;
            return false;
        }

        bounds = _bodyCollider.bounds;
        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    public bool BeginBottomAttack(
        Transform target,
        float shakeDuration,
        float shakeStrength,
        float dashDuration,
        Action<MonsterBase> onImpact)
    {
        if (_isDead || _isBottomAttacking || target == null)
            return false;

        _isBottomAttacking = true;
        _dotStacks.Clear();
        _dotTickTimer = 0f;

        if (_bodyCollider != null)
            _bodyCollider.enabled = false;

        _bottomAttackSequence = DOTween.Sequence();
        _bottomAttackSequence.Append(
            transform.DOShakePosition(
                shakeDuration,
                shakeStrength,
                vibrato: 12,
                randomness: 60f,
                snapping: false,
                fadeOut: true));
        _bottomAttackSequence.Append(
            transform.DOMove(target.position, dashDuration).SetEase(Ease.InQuad));
        _bottomAttackSequence.OnComplete(() =>
        {
            _bottomAttackSequence = null;
            onImpact?.Invoke(this);
        });
        return true;
    }

    private void KillBottomAttackSequence()
    {
        if (_bottomAttackSequence == null)
            return;

        _bottomAttackSequence.Kill(false);
        _bottomAttackSequence = null;
    }

    private void UpdateStatusVisual(float deltaTime)
    {
        _flashSecondsRemaining = Mathf.Max(0f, _flashSecondsRemaining - deltaTime);

        bool isIceActive = _frozenSecondsRemaining > 0f || _slowSecondsRemaining > 0f;
        bool isFireActive = _dotStacks.Count > 0;

        Color statusColor;
        if (isIceActive && isFireActive)
            statusColor = (_lastStatusVisual == StatusVisualType.Fire) ? _burnTintColor : _freezeTintColor;
        else if (isIceActive)
            statusColor = _freezeTintColor;
        else if (isFireActive)
            statusColor = _burnTintColor;
        else
            statusColor = _baseColor;

        _spriteRenderer.color = (_flashSecondsRemaining > 0f) ? _hitFlashColor : statusColor;
    }

    private void OnDestroy()
    {
        KillBottomAttackSequence();
    }
}
