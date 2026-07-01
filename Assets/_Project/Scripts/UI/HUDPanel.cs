using DG.Tweening;
using TMPro;
using UnityEngine;

public class HUDPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text    _waveText;
    [SerializeField] private TMP_Text    _scoreText;
    [SerializeField] private TMP_Text    _progressText;
    [SerializeField] private GameObject  _launchReadyIndicator;
    [SerializeField] private CanvasGroup _launchReadyCanvasGroup;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _slideDist    = 50f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Ease  _ease         = Ease.OutCubic;
    private Vector3 _originalPos;

    private int _totalWaves;

    private void Awake()
    {
        _originalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        WaveManager.OnWaveStarted         += HandleWaveStarted;
        WaveManager.OnMonsterCountChanged  += HandleMonsterCountChanged;
        BallLauncher.OnAllBallsReturned   += HandleAllBallsReturned;
        UIManager.OnScoreChanged          += HandleScoreChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted         -= HandleWaveStarted;
        WaveManager.OnMonsterCountChanged  -= HandleMonsterCountChanged;
        BallLauncher.OnAllBallsReturned   -= HandleAllBallsReturned;
        UIManager.OnScoreChanged          -= HandleScoreChanged;
    }

    private void Start()
    {
        _totalWaves = WaveManager.Instance.TotalWaves;
        UpdateScore(0);
        SetLaunchIndicatorVisible(false);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        _waveText.text = $"WAVE {waveNumber} / {_totalWaves}";
        SetLaunchIndicatorVisible(true);
    }

    private void HandleAllBallsReturned()
    {
        SetLaunchIndicatorVisible(false);
    }

    private void HandleMonsterCountChanged(int remaining, int total)
    {
        int percent = total > 0 ? Mathf.RoundToInt((float)(total - remaining) / total * 100f) : 0;
        _progressText.text = $"{percent}%";
    }

    private void HandleScoreChanged(int score)
    {
        UpdateScore(score);
    }

    private void UpdateScore(int score)
    {
        _scoreText.text = $"처치: {score}";
    }

    private void SetLaunchIndicatorVisible(bool visible)
    {
        if (_launchReadyCanvasGroup != null)
            _launchReadyCanvasGroup.alpha = visible ? 1f : 0f;
        else
            _launchReadyIndicator.SetActive(visible);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;
        transform.localPosition     = _originalPos + Vector3.down * _slideDist;
        _canvasGroup.alpha          = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_originalPos.y, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(1f, _animDuration));
        seq.OnComplete(() => { _canvasGroup.blocksRaycasts = true; _canvasGroup.interactable = true; });
    }

    public void Hide()
    {
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() => { transform.localPosition = _originalPos; gameObject.SetActive(false); });
    }
}
