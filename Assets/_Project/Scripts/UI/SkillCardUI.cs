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
    [SerializeField] private TMP_Text   _damageText;
    [SerializeField] private Button     _selectButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _newText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private GameObject _damageRoot;

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
        _iconImage.preserveAspect = true;
        _iconImage.enabled    = data.Icon != null;
        _nameText.text        = data.SkillName;
        _descriptionText.text = data.Description;
        _typeText.text        = data.SkillType == SkillType.Active ? "액티브" : "패시브";
        if (_background != null)
            _background.color = data.SkillType == SkillType.Active
                ? new Color(0.38f, 0.16f, 0.16f)
                : new Color(0.14f, 0.36f, 0.37f);
        if (_newText != null)
            _newText.text = data.CurrentLevel == 0 ? "New!" : string.Empty;
        if (_levelText != null)
            _levelText.text = data.CurrentLevel >= data.MaxLevel - 1
                ? "Max"
                : $"Lv.{data.CurrentLevel + 1}";

        bool isActive = data.SkillType == SkillType.Active;
        if (_damageRoot != null)
            _damageRoot.SetActive(isActive);
        if (_damageText != null)
        {
            if (_damageRoot == null)
                _damageText.gameObject.SetActive(isActive);
            if (isActive)
                _damageText.text = data.CurrentLevelData.BallDamage.ToString("0");
        }
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
