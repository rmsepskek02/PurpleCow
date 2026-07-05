using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterXpBar : MonoBehaviour
{
    [SerializeField] private Slider   _slider;
    [SerializeField] private TMP_Text _levelText;

    private void OnEnable()
    {
        CharacterManager.OnXpChanged += UpdateXp;
        CharacterManager.OnLevelUp   += UpdateLevel;
    }

    private void OnDisable()
    {
        CharacterManager.OnXpChanged -= UpdateXp;
        CharacterManager.OnLevelUp   -= UpdateLevel;
    }

    private void Start()
    {
        if (CharacterManager.Instance == null) return;
        UpdateXp(CharacterManager.Instance.CurrentXp, CharacterManager.Instance.RequiredXp);
        UpdateLevel(CharacterManager.Instance.CurrentLevel);
    }

    private void UpdateXp(int current, int required)
    {
        _slider.value = required > 0 ? (float)current / required : 0f;
    }

    private void UpdateLevel(int level)
    {
        if (_levelText != null) _levelText.text = level.ToString();
    }
}
