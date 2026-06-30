using System;
using UnityEngine;

public class BallLauncher : Singleton<BallLauncher>
{
    [SerializeField] private Ball _ballPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private Transform _launchPoint;

    private ObjectPool<Ball> _ballPool;
    private Vector2 _launchDirection;
    private bool _canLaunch;
    private int _activeBallCount;

    public static event Action OnAllBallsReturned;

    protected override void Awake()
    {
        base.Awake();
        _ballPool = new ObjectPool<Ball>(_ballPrefab, _poolParent, _initialPoolSize);
    }

    private void OnEnable()
    {
        InputHandler.Instance.OnDrag += HandleDrag;
        InputHandler.Instance.OnRelease += HandleRelease;
        GameManager.Instance.OnGameStateChanged += HandleGameState;
    }

    private void OnDisable()
    {
        InputHandler.Instance.OnDrag -= HandleDrag;
        InputHandler.Instance.OnRelease -= HandleRelease;
        GameManager.Instance.OnGameStateChanged -= HandleGameState;
    }

    private void HandleDrag(Vector2 direction)
    {
        _launchDirection = direction;
    }

    private void HandleRelease()
    {
        if (_canLaunch)
        {
            LaunchBall();
        }
    }

    private void LaunchBall()
    {
        Ball ball = _ballPool.Get();
        ball.transform.position = _launchPoint.position;
        ball.Launch(_launchDirection);
        SkillManager.Instance.ApplySkillToBall(ball);
        _activeBallCount++;
    }

    public void LaunchSubBalls(Vector2 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Ball ball = _ballPool.Get();
            ball.transform.position = origin;
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            // 아래 방향 제외 (y > 0 보정)
            if (randomDir.y < 0) randomDir.y = -randomDir.y;
            ball.Launch(randomDir);
            _activeBallCount++;
        }
    }

    private void HandleGameState(GameManager.GameState state)
    {
        _canLaunch = state == GameManager.GameState.Playing;
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
