using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDPanel : MonoBehaviour
{
    [SerializeField] private string   _stageName = "1. 깊은 숲";
    [SerializeField] private TMP_Text _stageNameText;
    [SerializeField] private Image    _stageProgressFillImage;
    [SerializeField] private TMP_Text _progressText;

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
        WaveManager.OnStageKillProgressChanged += HandleStageKillProgressChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnStageKillProgressChanged -= HandleStageKillProgressChanged;
    }

    private void Start()
    {
        _stageNameText.text = _stageName;
    }

    private void HandleStageKillProgressChanged(int killedSoFar, int totalInStage)
    {
        float ratio   = totalInStage > 0 ? (float)killedSoFar / totalInStage : 0f;
        int   percent = Mathf.RoundToInt(ratio * 100f);

        _stageProgressFillImage.fillAmount = ratio;
        _progressText.text = $"{percent}%";
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
