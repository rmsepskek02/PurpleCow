using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text    _resultTitleText;
    [SerializeField] private TMP_Text    _finalScoreText;
    [SerializeField] private TMP_Text    _waveText;
    [SerializeField] private Button      _restartButton;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _slideDist    = 50f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Ease  _ease         = Ease.OutCubic;
    private Vector3 _originalPos;

    private void Awake()
    {
        _originalPos = transform.localPosition;
        _restartButton.onClick.AddListener(HandleRestartClicked);
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        _restartButton.onClick.RemoveListener(HandleRestartClicked);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Result)
            return;

        bool isSuccess = GameManager.Instance.IsLastGameSuccess;
        _resultTitleText.text = isSuccess ? "SUCCESS" : "GAME OVER";
        _resultTitleText.color = isSuccess
            ? new Color(1f, 0.62f, 0.12f)
            : new Color(0.72f, 0.16f, 0.14f);
        _finalScoreText.text  = $"최종 점수: {UIManager.Instance.Score}";
        if (_waveText != null)
            _waveText.text = $"도달 웨이브: {WaveManager.Instance.CurrentWaveNumber} / {WaveManager.Instance.TotalWaves}";
    }

    private void HandleRestartClicked()
    {
        GameManager.Instance.RestartGame();
    }

    public void Show()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;
        transform.localPosition     = _originalPos + Vector3.down * _slideDist;
        _canvasGroup.alpha          = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOLocalMoveY(_originalPos.y, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(1f, _animDuration));
        seq.OnComplete(() => { _canvasGroup.blocksRaycasts = true; _canvasGroup.interactable = true; });
    }

    public void Hide()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() => { transform.localPosition = _originalPos; });
    }
}
