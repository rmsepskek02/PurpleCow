using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCardUI : MonoBehaviour
{
    [SerializeField] private Image       _iconImage;
    [SerializeField] private TMP_Text   _nameText;
    [SerializeField] private TMP_Text   _descriptionText;
    [SerializeField] private TMP_Text   _typeText;
    [SerializeField] private Button     _selectButton;
    [SerializeField] private CanvasGroup _canvasGroup;

    private SkillData          _currentData;
    private Action<SkillData>  _onSelected;

    private void Awake()
    {
        _selectButton.onClick.AddListener(HandleSelectClicked);
    }

    private void OnDestroy()
    {
        _selectButton.onClick.RemoveListener(HandleSelectClicked);
    }

    public void Setup(SkillData data, Action<SkillData> onSelected)
    {
        _currentData          = data;
        _onSelected           = onSelected;
        _iconImage.sprite     = data.Icon;
        _nameText.text        = data.SkillName;
        _descriptionText.text = data.Description;
        _typeText.text        = data.SkillType == SkillType.Active ? "액티브" : "패시브";
    }

    private void HandleSelectClicked()
    {
        _onSelected?.Invoke(_currentData);
    }

    public void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
