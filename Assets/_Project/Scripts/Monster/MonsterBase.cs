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
    [SerializeField] private float _hitFlashDuration = 0.4f;
    [SerializeField] private Color _freezeTintColor = new Color(0.53f, 0.81f, 0.98f);
    [SerializeField] private Color _burnTintColor = new Color(1f, 0.35f, 0.16f);

    private const int FlashOverlaySortingOffset = 100;

    private float _flashSecondsRemaining;
    private static Material _flashOverlayMaterial;
    private SpriteRenderer _flashOverlayRenderer;
    private SpriteRenderer _blockSpriteRenderer;
    private SpriteRenderer _blockFlashOverlayRenderer;

    private enum StatusVisualType { None, Ice, Fire }
    private StatusVisualType _lastStatusVisual;

    public float CurrentHp    => _currentHp;
    public bool  IsAlive      => !_isDead;
    public bool  IsBottomAttacking => _isBottomAttacking;
    public MonsterData Data   => _monsterData;

    public static event Action<MonsterBase> OnMonsterDied;
    public event Action<float, float> OnHpChanged;

    private void Awake()
    {
        _bodyCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseColor = _spriteRenderer.color;

        Transform blockVisual = transform.Find("BlockVisual");
        if (blockVisual != null)
            _blockSpriteRenderer = blockVisual.GetComponent<SpriteRenderer>();

        CreateFlashOverlay();
    }

    private void CreateFlashOverlay()
    {
        _flashOverlayRenderer = CreateOverlayFor(_spriteRenderer, transform);

        if (_blockSpriteRenderer != null)
            _blockFlashOverlayRenderer = CreateOverlayFor(_blockSpriteRenderer, _blockSpriteRenderer.transform);
    }

    private SpriteRenderer CreateOverlayFor(SpriteRenderer source, Transform parent)
    {
        var overlayObject = new GameObject(source.gameObject.name + "_FlashOverlay");
        overlayObject.transform.SetParent(parent, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = source.sprite;
        overlayRenderer.sortingLayerID = source.sortingLayerID;
        overlayRenderer.sortingOrder = source.sortingOrder + FlashOverlaySortingOffset;

        if (_flashOverlayMaterial == null)
        {
            Shader shader = Shader.Find("PurpleCow/SpriteFlashOverlay");
            if (shader != null)
                _flashOverlayMaterial = new Material(shader);
        }

        if (_flashOverlayMaterial != null)
            overlayRenderer.sharedMaterial = _flashOverlayMaterial;

        overlayRenderer.color = new Color(_hitFlashColor.r, _hitFlashColor.g, _hitFlashColor.b, 0f);
        return overlayRenderer;
    }

    public void OnSpawn()
    {
        KillBottomAttackSequence();
        _currentHp              = _monsterData.Hp;
        _isDead                 = false;
        _isBottomAttacking      = false;
        _slowSecondsRemaining   = 0f;
        _slowPercent            = 0f;
        _dotStacks.Clear();
        _dotTickTimer           = 0f;
        if (_bodyCollider != null)
            _bodyCollider.enabled = true;
        _flashSecondsRemaining  = 0f;
        _lastStatusVisual       = StatusVisualType.None;
        _spriteRenderer.color   = _baseColor;
        if (_flashOverlayRenderer != null)
            _flashOverlayRenderer.color = new Color(_hitFlashColor.r, _hitFlashColor.g, _hitFlashColor.b, 0f);
        if (_blockFlashOverlayRenderer != null)
            _blockFlashOverlayRenderer.color = new Color(_hitFlashColor.r, _hitFlashColor.g, _hitFlashColor.b, 0f);
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
            {
                TakeDamage(tickDamage);
                Ball.RaiseHitMonster(this, tickDamage, false);
            }
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

        float speed = _monsterData.MoveSpeed;

        if (_slowSecondsRemaining > 0f)
        {
            speed *= (1f - _slowPercent);
        }

        float desiredDistance = speed * deltaTime;
        float safeDistance = WaveManager.Instance != null
            ? WaveManager.Instance.GetSafeDownwardDistance(this, desiredDistance)
            : desiredDistance;
        transform.position += Vector3.down * safeDistance;
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

    public bool TryGetProjectedColliderBounds(
        MonsterData data,
        Vector3 rootWorldPosition,
        Vector3 parentLossyScale,
        out Bounds bounds)
    {
        BoxCollider2D bodyCollider = _bodyCollider != null
            ? _bodyCollider
            : GetComponent<BoxCollider2D>();

        if (data == null ||
            bodyCollider == null ||
            !ColliderSizeMap.TryGetValue(data.BlockSize, out Vector2 localColliderSize))
        {
            bounds = default;
            return false;
        }

        Vector3 worldScale = Vector3.Scale(transform.localScale, parentLossyScale);
        Vector3 scaledOffset = Vector3.Scale(
            new Vector3(bodyCollider.offset.x, bodyCollider.offset.y, 0f),
            worldScale);
        Vector3 worldSize = new Vector3(
            Mathf.Abs(localColliderSize.x * worldScale.x),
            Mathf.Abs(localColliderSize.y * worldScale.y),
            1f);

        bounds = new Bounds(rootWorldPosition + scaledOffset, worldSize);
        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    public bool BeginBottomAttack(
        Transform target,
        float shakeDuration,
        float shakeStrength,
        int shakeVibrato,
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
                vibrato: shakeVibrato,
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

        bool isIceActive = _slowSecondsRemaining > 0f;
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

        _spriteRenderer.color = statusColor;

        if (_flashOverlayRenderer != null)
        {
            float flashAlpha = _flashSecondsRemaining > 0f ? 1f : 0f;
            Color flashColor = new Color(_hitFlashColor.r, _hitFlashColor.g, _hitFlashColor.b, flashAlpha);
            _flashOverlayRenderer.color = flashColor;

            if (_blockFlashOverlayRenderer != null)
                _blockFlashOverlayRenderer.color = flashColor;
        }
    }

    private void OnDestroy()
    {
        KillBottomAttackSequence();
    }
}
