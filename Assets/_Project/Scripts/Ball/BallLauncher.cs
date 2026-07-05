using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLauncher : Singleton<BallLauncher>
{
    [SerializeField] private Ball _ballPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private Transform _launchPoint;
    [SerializeField] private int _normalBallCount = 5;
    [SerializeField] private float _rosterLaunchInterval = 0.1f;

    private ObjectPool<Ball> _ballPool;
    private Vector2 _launchDirection = Vector2.up;
    private int _activeBallCount;
    private float _speedMultiplier = 1f;
    private GameManager.GameState _currentGameState;
    private Coroutine _relaunchCoroutine;

    private readonly List<BallRosterEntry> _roster = new List<BallRosterEntry>();
    private readonly List<Ball> _activeBalls = new List<Ball>();
    private readonly Queue<Ball> _relaunchQueue = new Queue<Ball>();
    private readonly HashSet<Ball> _queuedBalls = new HashSet<Ball>();
    private readonly Dictionary<Ball, SkillData> _cloneSkills = new Dictionary<Ball, SkillData>();

    public static event Action OnAllBallsReturned;

    public Transform LaunchPoint     => _launchPoint;
    public Vector2   LaunchDirection => _launchDirection;

    // 로스터 항목: 볼 개체 1개가 영구적으로 유지하는 타입 정체성.
    // SkillData가 null이면 노말볼, 아니면 해당 특수볼 타입(스킬)을 의미한다.
    private class BallRosterEntry
    {
        public SkillData SkillData;
        public Ball       Ball;
    }

    protected override void Awake()
    {
        base.Awake();
        _ballPool = new ObjectPool<Ball>(_ballPrefab, _poolParent, _initialPoolSize);

        // 볼끼리 물리적으로 충돌(튕겨나감)하지 않도록 "Ball" 레이어끼리의 충돌을 전역적으로 비활성화한다.
        // Wall/Ground/Monster는 Default 레이어에 그대로 남아있으므로 이 호출의 영향을 받지 않는다.
        int ballLayer = LayerMask.NameToLayer("Ball");
        Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true);
    }

    private void Start()
    {
        StartCoroutine(CoInitializeRoster());
    }

    private void OnEnable()
    {
        InputHandler.OnDrag += HandleDrag;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        InputHandler.OnDrag -= HandleDrag;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleDrag(Vector2 direction)
    {
        _launchDirection = direction;
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        _currentGameState = state;
    }

    // 게임 시작 즉시(터치 무관) 노말볼 5개를 기본 방향(Vector2.up)으로 자동 발사해 사이클을 시작한다.
    // 5개를 동시에 발사하지 않고 _rosterLaunchInterval 간격으로 순차 발사한다.
    private IEnumerator CoInitializeRoster()
    {
        for (int i = 0; i < _normalBallCount; i++)
        {
            Ball ball = _ballPool.Get();
            var entry = new BallRosterEntry { SkillData = null, Ball = ball };
            _roster.Add(entry);
            LaunchRosterEntry(entry, _launchDirection);

            if (i < _normalBallCount - 1)
                yield return new WaitForSeconds(_rosterLaunchInterval);
        }
    }

    // 3택지에서 신규 특수볼 타입을 선택했을 때 로스터에 볼 1개를 추가하고 즉시 사이클에 합류시킨다.
    public void AddBallToRoster(SkillData skillData)
    {
        Ball ball = _ballPool.Get();
        var entry = new BallRosterEntry { SkillData = skillData, Ball = ball };
        _roster.Add(entry);
        LaunchRosterEntry(entry, _launchDirection);
    }

    private void LaunchRosterEntry(BallRosterEntry entry, Vector2 direction)
    {
        entry.Ball.transform.position = _launchPoint.position;
        entry.Ball.SetSpeedMultiplier(_speedMultiplier);
        entry.Ball.Launch(direction);
        ApplyRosterSkill(entry);
        RegisterActiveBall(entry.Ball);
    }

    // 귀환(Ground 충돌 후 LaunchPoint 도달)한 로스터 볼을 최신 조준 방향으로 재발사한다.
    public void HandleBallRecovered(Ball ball)
    {
        ball.ParkAtLaunchPoint(_launchPoint.position);

        if (ball.IsClone && ball.ConsumeCloneReturn())
        {
            ReturnBall(ball);
            return;
        }

        if (_currentGameState != GameManager.GameState.Playing || !_queuedBalls.Add(ball))
            return;

        _relaunchQueue.Enqueue(ball);
        if (_relaunchCoroutine == null)
            _relaunchCoroutine = StartCoroutine(CoRelaunchQueuedBalls());
    }

    public bool IsRosterMember(Ball ball)
    {
        return _roster.Exists(e => e.Ball == ball);
    }

    private static void ApplyRosterSkill(BallRosterEntry entry)
    {
        if (entry.SkillData != null)
            entry.Ball.AddSkill(SkillFactory.CreateActiveSkill(entry.SkillData));
    }

    private IEnumerator CoRelaunchQueuedBalls()
    {
        var interval = new WaitForSeconds(_rosterLaunchInterval);

        while (_relaunchQueue.Count > 0)
        {
            Ball ball = _relaunchQueue.Dequeue();
            _queuedBalls.Remove(ball);
            RelaunchQueuedBall(ball);
            yield return interval;
        }

        _relaunchCoroutine = null;
    }

    private void RelaunchQueuedBall(Ball ball)
    {
        if (_currentGameState != GameManager.GameState.Playing || !_activeBalls.Contains(ball))
            return;

        ball.PrepareForRelaunch();
        ball.SetSpeedMultiplier(_speedMultiplier);
        ball.Launch(_launchDirection);

        BallRosterEntry entry = _roster.Find(e => e.Ball == ball);
        if (entry != null)
        {
            ApplyRosterSkill(entry);
            return;
        }

        if (_cloneSkills.TryGetValue(ball, out SkillData skillData) && skillData != null)
            ball.AddSkill(SkillFactory.CreateActiveSkill(skillData));
    }

    public void LaunchRosterClones(int returnCount)
    {
        StartCoroutine(CoLaunchRosterClones(new List<BallRosterEntry>(_roster), returnCount));
    }

    private IEnumerator CoLaunchRosterClones(List<BallRosterEntry> originals, int returnCount)
    {
        var interval = new WaitForSeconds(_rosterLaunchInterval);

        for (int i = 0; i < originals.Count; i++)
        {
            BallRosterEntry original = originals[i];
            Ball clone = _ballPool.Get();
            clone.transform.position = _launchPoint.position;
            clone.ConfigureClone(returnCount);
            clone.SetSpeedMultiplier(_speedMultiplier);
            _cloneSkills[clone] = original.SkillData;
            clone.Launch(_launchDirection);

            if (original.SkillData != null)
                clone.AddSkill(SkillFactory.CreateActiveSkill(original.SkillData));

            RegisterActiveBall(clone);

            if (i < originals.Count - 1)
                yield return interval;
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);

        for (int i = 0; i < _activeBalls.Count; i++)
            _activeBalls[i].SetSpeedMultiplier(_speedMultiplier);
    }

    public void LaunchSubBalls(Vector2 origin, int count, float damage = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            Ball ball = _ballPool.Get();
            ball.transform.position = origin;
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            // 아래 방향 제외 (y > 0 보정)
            if (randomDir.y < 0) randomDir.y = -randomDir.y;
            ball.SetSpeedMultiplier(_speedMultiplier);
            ball.Launch(randomDir);
            if (damage > 0f) ball.SetSubBallDamage(damage);
            RegisterActiveBall(ball);
        }
    }

    public void ReturnBall(Ball ball)
    {
        _queuedBalls.Remove(ball);
        _cloneSkills.Remove(ball);
        bool wasActive = _activeBalls.Remove(ball);
        _ballPool.Return(ball);
        if (wasActive)
            _activeBallCount = Mathf.Max(0, _activeBallCount - 1);

        if (_activeBallCount == 0)
        {
            OnAllBallsReturned?.Invoke();
        }
    }

    private void RegisterActiveBall(Ball ball)
    {
        if (_activeBalls.Contains(ball))
            return;

        _activeBalls.Add(ball);
        _activeBallCount++;
    }
}
