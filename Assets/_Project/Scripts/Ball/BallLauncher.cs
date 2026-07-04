using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLauncher : Singleton<BallLauncher>
{
    [SerializeField] private Ball _ballPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private int _normalBallCount = 5;
    [SerializeField] private float _rosterLaunchInterval = 0.1f;

    private ObjectPool<Ball> _ballPool;
    private Vector2 _launchDirection = Vector2.up;
    private int _activeBallCount;
    private GameManager.GameState _currentGameState;

    private readonly List<BallRosterEntry> _roster = new List<BallRosterEntry>();

    public static event Action OnAllBallsReturned;

    public Vector2 LaunchDirection => _launchDirection;

    public Vector2 LaunchOrigin => (Vector2)CharacterAimController.Instance.transform.position
        + LaunchDirection.normalized * CharacterAimController.Instance.WeaponLength;

    public Vector2 ReturnPoint => CharacterAimController.Instance.BodyPosition;

    public Vector2 CharacterPosition => CharacterAimController.Instance.transform.position;

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
        entry.Ball.transform.position = LaunchOrigin;
        entry.Ball.Launch(direction);
        ApplyRosterSkill(entry);
        _activeBallCount++;
    }

    // 귀환(Ground 충돌 후 ReturnPoint 도달)한 로스터 볼을 최신 조준 방향으로 재발사한다.
    public void RelaunchBall(Ball ball)
    {
        if (_currentGameState != GameManager.GameState.Playing)
            return;

        BallRosterEntry entry = _roster.Find(e => e.Ball == ball);
        if (entry == null)
            return;

        ball.PrepareForRelaunch();
        ball.Launch(_launchDirection);
        ApplyRosterSkill(entry);
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

    public void LaunchSubBalls(Vector2 origin, int count, float damage = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            Ball ball = _ballPool.Get();
            ball.transform.position = origin;
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            // 아래 방향 제외 (y > 0 보정)
            if (randomDir.y < 0) randomDir.y = -randomDir.y;
            ball.Launch(randomDir);
            if (damage > 0f) ball.SetSubBallDamage(damage);
            _activeBallCount++;
        }
    }

    public void ReturnBall(Ball ball)
    {
        _ballPool.Return(ball);
        _activeBallCount--;

        if (_activeBallCount == 0)
        {
            OnAllBallsReturned?.Invoke();
        }
    }
}
