using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _stageTitleText;
    [SerializeField] private Slider _stageProgressSlider;
    [SerializeField] private TMP_Text _stageProgressText;
    [SerializeField] private Slider _xpSlider;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _successTestButton;
    [SerializeField] private Button _failureTestButton;
    [SerializeField] private PausePanel _pausePanel;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _slideDist    = 50f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Ease  _ease         = Ease.OutCubic;
    private Vector3 _originalPos;

    private void Awake()
    {
        _originalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        WaveManager.OnWaveStarted         += HandleWaveStarted;
        CharacterManager.OnXpChanged += HandleXpChanged;
        CharacterManager.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted         -= HandleWaveStarted;
        CharacterManager.OnXpChanged -= HandleXpChanged;
        CharacterManager.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        _stageTitleText.text = "1. 깊은 숲";
        _pauseButton.onClick.AddListener(_pausePanel.Show);
        if (_successTestButton != null)
            _successTestButton.onClick.AddListener(HandleSuccessTestClicked);
        if (_failureTestButton != null)
            _failureTestButton.onClick.AddListener(HandleFailureTestClicked);
        RefreshAll();
    }

    private void OnDestroy()
    {
        _pauseButton.onClick.RemoveListener(_pausePanel.Show);
        if (_successTestButton != null)
            _successTestButton.onClick.RemoveListener(HandleSuccessTestClicked);
        if (_failureTestButton != null)
            _failureTestButton.onClick.RemoveListener(HandleFailureTestClicked);
    }

    private static void HandleSuccessTestClicked()
        => GameManager.Instance.EndGame(true);

    private static void HandleFailureTestClicked()
        => GameManager.Instance.EndGame(false);

    private void RefreshAll()
    {
        HandleWaveStarted(WaveManager.Instance.CurrentWaveNumber);
        CharacterManager character = CharacterManager.Instance;
        HandleXpChanged(character.CurrentXp, character.RequiredXp);
        HandleLevelUp(character.CurrentLevel);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        float progress = WaveManager.Instance.StageProgress;
        _stageProgressSlider.value = progress;
        _stageProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    private void HandleXpChanged(int current, int required)
    {
        _xpSlider.value = required > 0 ? (float)current / required : 1f;
    }

    private void HandleLevelUp(int level) => _levelText.text = level.ToString();

    public void SetCharacterProgressVisible(bool visible)
    {
        if (_xpSlider != null)
            _xpSlider.gameObject.SetActive(visible);

        if (_levelText != null)
            _levelText.transform.parent.gameObject.SetActive(visible);
    }

    public void Show()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
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
        transform.DOKill();
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() => { transform.localPosition = _originalPos; });
    }
}
