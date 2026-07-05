using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActiveSkillButton : MonoBehaviour
{
    [SerializeField] private int _skillIndex;
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private TMP_Text _nameText;

    private PlayerActiveSkillManager _manager;

    private void Start()
    {
        _manager = PlayerActiveSkillManager.Instance;
        if (_manager == null)
        {
            _button.interactable = false;
            return;
        }

        _button.onClick.AddListener(HandleClick);
        _manager.OnCooldownChanged += HandleCooldownChanged;

        PlayerActiveSkillData skill = _manager.GetSkill(_skillIndex);
        if (skill == null)
        {
            _button.interactable = false;
            return;
        }

        if (_icon != null)
        {
            _icon.sprite = skill.Icon;
            _icon.enabled = skill.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = skill.DisplayName;

        RefreshCooldown(_manager.GetRemainingCooldown(_skillIndex), skill.Cooldown);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);

        if (_manager != null)
            _manager.OnCooldownChanged -= HandleCooldownChanged;
    }

    private void HandleClick()
    {
        _manager.TryUseSkill(_skillIndex);
    }

    private void HandleCooldownChanged(int index, float remaining, float total)
    {
        if (index == _skillIndex)
            RefreshCooldown(remaining, total);
    }

    private void RefreshCooldown(float remaining, float total)
    {
        bool isCoolingDown = remaining > 0f;
        _button.interactable = !isCoolingDown;

        if (_cooldownOverlay != null)
            _cooldownOverlay.fillAmount = total > 0f ? remaining / total : 0f;

        if (_cooldownText != null)
        {
            _cooldownText.gameObject.SetActive(isCoolingDown);
            _cooldownText.text =
                isCoolingDown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        }
    }
}
