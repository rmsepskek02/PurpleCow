using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PausePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _stageText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private SkillSlotGroup _activeSlotGroup;
    [SerializeField] private SkillSlotGroup _passiveSlotGroup;
    [SerializeField] private float _duration = 0.2f;

    private void Awake()
    {
        _continueButton.onClick.AddListener(Hide);
        SetVisible(false);
    }

    private void OnDestroy() => _continueButton.onClick.RemoveListener(Hide);

    public void Show()
    {
        _stageText.text = "Stage 1  (Normal)";
        if (SkillManager.Instance != null)
        {
            _activeSlotGroup.UpdateActiveSlots(
                new List<BallSkillBase>(SkillManager.Instance.EquippedActiveSkills));
            _passiveSlotGroup.UpdatePassiveSlots(
                new List<PassiveSkillBase>(SkillManager.Instance.EquippedPassiveSkills));
        }
        Time.timeScale = 0f;
        SetVisible(true);
        transform.localScale = Vector3.one * 0.96f;
        transform.DOScale(1f, _duration).SetUpdate(true);
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true);
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true)
            .OnComplete(() => SetVisible(false));
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
